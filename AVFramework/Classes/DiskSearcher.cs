using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace AVFramework.Classes
{
    public class DiskSearcher
    {
        private readonly ILogger _logger;
        private readonly SearchOptions _options;

        public delegate void SearchProgressHandler(string currentDirectory, int processedFiles, int totalFiles, double progressPercentage);

        public class SearchOptions
        {
            public string[] ExcludedDirectories { get; set; } = new[] { "System Volume Information", "$Recycle.Bin", "Recovery" };
            public string[] ExcludedExtensions { get; set; } = new[] { ".tmp", ".temp", ".log" };
            public long MinFileSize { get; set; } = 0;
            public long MaxFileSize { get; set; } = long.MaxValue;
            public bool SkipHiddenFiles { get; set; } = true;
            public bool SkipSystemFiles { get; set; } = true;
        }

        public DiskSearcher(ILogger logger = null, SearchOptions options = null)
        {
            _logger = logger ?? new DefaultLogger();
            _options = options ?? new SearchOptions();
        }

        // Статический метод для обратной совместимости
        public static List<string> GetAllFiles(string rootDirectory)
        {
            var searcher = new DiskSearcher();
            return searcher.GetAllFilesInstance(rootDirectory);
        }

        // Синхронный метод для обратной совместимости
        public List<string> GetAllFilesInstance(string rootDirectory)
        {
            if (string.IsNullOrEmpty(rootDirectory))
                throw new ArgumentNullException(nameof(rootDirectory));

            if (!Directory.Exists(rootDirectory))
                throw new DirectoryNotFoundException($"Директория не найдена: {rootDirectory}");

            var allFiles = new List<string>();
            try
            {
                _logger.LogInfo($"Начало поиска файлов в директории: {rootDirectory}");
                GetFilesRecursive(rootDirectory, allFiles);
                _logger.LogInfo($"Поиск файлов завершен. Найдено файлов: {allFiles.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при поиске файлов: {ex.Message}", ex);
            }
            return allFiles;
        }

        private void GetFilesRecursive(string currentDirectory, List<string> allFiles)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(currentDirectory))
                {
                    if (ShouldIncludeFile(file))
                    {
                        allFiles.Add(file);
                    }
                }

                foreach (var directory in Directory.EnumerateDirectories(currentDirectory))
                {
                    if (!ShouldSkipDirectory(directory))
                    {
                        GetFilesRecursive(directory, allFiles);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning($"Нет доступа к директории: {currentDirectory}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обработке директории {currentDirectory}: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetAllFilesAsync(
            string rootDirectory,
            SearchProgressHandler progressHandler = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(rootDirectory))
                throw new ArgumentNullException(nameof(rootDirectory));

            if (!Directory.Exists(rootDirectory))
                throw new DirectoryNotFoundException($"Директория не найдена: {rootDirectory}");

            var allFiles = new List<string>();
            var processedFiles = 0;
            var totalFiles = 0;

            try
            {
                _logger.LogInfo($"Начало поиска файлов в директории: {rootDirectory}");
                
                // Сначала подсчитаем общее количество файлов
                totalFiles = await CountFilesAsync(rootDirectory, cancellationToken);
                _logger.LogInfo($"Всего найдено файлов для сканирования: {totalFiles}");

                // Затем собираем файлы
                await GetFilesRecursiveAsync(rootDirectory, allFiles, processedFiles, totalFiles, progressHandler, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo("Поиск файлов отменен пользователем");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при поиске файлов: {ex.Message}", ex);
            }

            _logger.LogInfo($"Поиск файлов завершен. Найдено файлов: {allFiles.Count}");
            return allFiles;
        }

        private async Task<int> CountFilesAsync(string directory, CancellationToken cancellationToken)
        {
            var count = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (ShouldIncludeFile(file))
                        count++;
                }

                foreach (var subDir in Directory.EnumerateDirectories(directory))
                {
                    if (!ShouldSkipDirectory(subDir))
                    {
                        count += await CountFilesAsync(subDir, cancellationToken);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning($"Нет доступа к директории: {directory}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при подсчете файлов в {directory}: {ex.Message}", ex);
            }
            return count;
        }

        // Исправленный метод без ref параметра
        private async Task GetFilesRecursiveAsync(
            string currentDirectory,
            List<string> allFiles,
            int processedFiles,
            int totalFiles,
            SearchProgressHandler progressHandler,
            CancellationToken cancellationToken)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(currentDirectory))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (ShouldIncludeFile(file))
                    {
                        allFiles.Add(file);
                        var newProcessedFiles = processedFiles + 1;
                        var progress = (double)newProcessedFiles / totalFiles * 100;
                        progressHandler?.Invoke(currentDirectory, newProcessedFiles, totalFiles, progress);
                        
                        // Рекурсивно обрабатываем подкаталоги
                        foreach (var directory in Directory.EnumerateDirectories(currentDirectory))
                        {
                            if (!ShouldSkipDirectory(directory))
                            {
                                await GetFilesRecursiveAsync(directory, allFiles, newProcessedFiles, totalFiles, progressHandler, cancellationToken);
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning($"Нет доступа к директории: {currentDirectory}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обработке директории {currentDirectory}: {ex.Message}", ex);
            }
        }

        private bool ShouldIncludeFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                // Проверка размера файла
                if (fileInfo.Length < _options.MinFileSize || fileInfo.Length > _options.MaxFileSize)
                    return false;

                // Проверка расширения
                if (_options.ExcludedExtensions.Contains(fileInfo.Extension.ToLower()))
                    return false;

                // Проверка атрибутов файла
                if (_options.SkipHiddenFiles && fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                    return false;

                if (_options.SkipSystemFiles && fileInfo.Attributes.HasFlag(FileAttributes.System))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при проверке файла {filePath}: {ex.Message}", ex);
                return false;
            }
        }

        private bool ShouldSkipDirectory(string directoryPath)
        {
            try
            {
                var dirInfo = new DirectoryInfo(directoryPath);
                return _options.ExcludedDirectories.Contains(dirInfo.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при проверке директории {directoryPath}: {ex.Message}", ex);
                return true;
            }
        }
    }
}
