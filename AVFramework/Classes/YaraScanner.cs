using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using YaraSharp;

namespace AVFramework
{
    public static class Extensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (chunkSize <= 0) throw new ArgumentException("Размер чанка должен быть больше 0", nameof(chunkSize));

            var chunk = new List<T>(chunkSize);
            foreach (var item in source)
            {
                chunk.Add(item);
                if (chunk.Count == chunkSize)
                {
                    yield return chunk;
                    chunk = new List<T>(chunkSize);
                }
            }
            if (chunk.Any())
            {
                yield return chunk;
            }
        }
    }

    public class YaraScanner : IDisposable
    {
        private static readonly ConcurrentDictionary<string, List<YSMatches>> _scanCache = new ConcurrentDictionary<string, List<YSMatches>>();
        private static HashSet<string> _whiteList = new HashSet<string>();
        private static readonly object _whiteListLock = new object();
        private const int DEFAULT_MAX_CACHE_SIZE = 20;
        private const int DEFAULT_BATCH_SIZE = 2;
        private bool _disposed = false;
        private readonly string _whiteListPath;
        private readonly int _maxCacheSize;
        private readonly int _batchSize;
        private readonly ILogger _logger;

        public delegate void ScanProgressHandler(string currentFile, int processedFiles, int totalFiles);

        public YaraScanner(string whiteListPath = "WhiteList.txt", int maxCacheSize = DEFAULT_MAX_CACHE_SIZE, 
            int batchSize = DEFAULT_BATCH_SIZE, ILogger logger = null)
        {
            _whiteListPath = whiteListPath;
            _maxCacheSize = maxCacheSize;
            _batchSize = batchSize;
            _logger = logger ?? new DefaultLogger();
            
            LoadWhiteListAsync().Wait();
        }

        private async Task LoadWhiteListAsync()
        {
            try
            {
                if (File.Exists(_whiteListPath))
                {
                    var lines = File.ReadAllLines(_whiteListPath);
                    _whiteList = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
                    _logger.LogInfo($"Загружен белый список из {_whiteListPath}. Количество записей: {_whiteList.Count}");
                }
                else
                {
                    _logger.LogWarning($"Файл белого списка {_whiteListPath} не найден. Создаем новый.");
                    File.WriteAllText(_whiteListPath, string.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при загрузке белого списка: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<YSMatches>> ScanFileAsync(string filePath, YSCompiler compiler)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(YaraScanner));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (compiler == null) throw new ArgumentNullException(nameof(compiler));

            try
            {
                // Проверяем кэш
                if (_scanCache.TryGetValue(filePath, out var cachedResult))
                {
                    _logger.LogDebug($"Найдено в кэше: {filePath}");
                    return cachedResult;
                }

                // Проверяем белый список
                if (IsInWhiteList(filePath))
                {
                    _logger.LogDebug($"Файл в белом списке: {filePath}");
                    return new List<YSMatches>();
                }

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Файл не существует: {filePath}");
                    return new List<YSMatches>();
                }

                // Проверяем доступ к файлу
                try
                {
                    using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // Файл доступен для чтения
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogError($"Нет прав доступа к файлу: {filePath}");
                    return new List<YSMatches>();
                }
                catch (IOException ex)
                {
                    _logger.LogError($"Файл заблокирован или используется: {filePath}. Ошибка: {ex.Message}");
                    return new List<YSMatches>();
                }

                using (var scanner = new YSScanner(compiler.GetRules(), null))
                {
                    try
                    {
                        var results = await Task.Run(() => scanner.ScanFile(filePath));
                        
                        // Ограничиваем размер кэша
                        if (_scanCache.Count >= _maxCacheSize)
                        {
                            var oldestKey = _scanCache.Keys.First();
                            _scanCache.TryRemove(oldestKey, out _);
                        }
                        
                        _scanCache.TryAdd(filePath, results);
                        
                        if (results.Any())
                        {
                            _logger.LogWarning($"Обнаружены совпадения в файле: {filePath}");
                        }
                        
                        return results;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Ошибка при сканировании файла {filePath}: {ex.Message}", ex);
                        return new List<YSMatches>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при сканировании файла {filePath}: {ex.Message}", ex);
                return new List<YSMatches>();
            }
        }

        public async Task<Dictionary<string, List<YSMatches>>> ScanFilesAsync(
            IEnumerable<string> files, 
            YSCompiler compiler, 
            ScanProgressHandler progressHandler = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(YaraScanner));
            if (files == null) throw new ArgumentNullException(nameof(files));
            if (compiler == null) throw new ArgumentNullException(nameof(compiler));

            var results = new ConcurrentDictionary<string, List<YSMatches>>();
            var fileBatches = files.Chunk(_batchSize);
            var totalFiles = files.Count();
            var processedFiles = 0;

            _logger.LogInfo($"Начало сканирования {totalFiles} файлов");

            foreach (var batch in fileBatches)
            {
                var tasks = batch.Select(async file =>
                {
                    processedFiles++;
                    progressHandler?.Invoke(file, processedFiles, totalFiles);
                    
                    var matches = await ScanFileAsync(file, compiler);
                    if (matches != null && matches.Any())
                    {
                        results.TryAdd(file, matches);
                    }
                });

                await Task.WhenAll(tasks);
                
                // Оптимизация памяти
                if (processedFiles % (_batchSize * 5) == 0)
                {
                    GC.Collect(2, GCCollectionMode.Optimized, true);
                }
                
                // Очищаем результаты при превышении лимита кэша
                if (results.Count > _maxCacheSize)
                {
                    var oldResults = results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    results.Clear();
                    foreach (var result in oldResults.Take(_maxCacheSize))
                    {
                        results.TryAdd(result.Key, result.Value);
                    }
                    oldResults.Clear();
                }
            }

            _logger.LogInfo($"Сканирование завершено. Обработано файлов: {processedFiles}");
            return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private bool IsInWhiteList(string filePath)
        {
            lock (_whiteListLock)
            {
                return _whiteList.Contains(filePath);
            }
        }

        public void AddToWhiteList(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            lock (_whiteListLock)
            {
                _whiteList.Add(filePath);
            }

            try
            {
                File.AppendAllText(_whiteListPath, filePath + Environment.NewLine);
                _logger.LogInfo($"Добавлен в белый список: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при добавлении в белый список: {ex.Message}", ex);
                throw;
            }
        }

        public async Task AddToWhiteListAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            lock (_whiteListLock)
            {
                _whiteList.Add(filePath);
            }

            try
            {
                File.AppendAllText(_whiteListPath, filePath + Environment.NewLine);
                _logger.LogInfo($"Добавлен в белый список: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при добавлении в белый список: {ex.Message}", ex);
                throw;
            }
        }

        public void ClearCache()
        {
            _scanCache.Clear();
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            _logger.LogInfo("Кэш сканирования очищен");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ClearCache();
                    _whiteList.Clear();
                }
                _disposed = true;
            }
        }

        ~YaraScanner()
        {
            Dispose(false);
        }
    }

    public interface ILogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception ex = null);
        void LogDebug(string message);
    }

    public class DefaultLogger : ILogger
    {
        public void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[WARNING] {message}");
        }

        public void LogError(string message, Exception ex = null)
        {
            Console.WriteLine($"[ERROR] {message}");
            if (ex != null)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }

        public void LogDebug(string message)
        {
            Console.WriteLine($"[DEBUG] {message}");
        }
    }
}
