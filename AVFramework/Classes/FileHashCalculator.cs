using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AVFramework.Classes
{
    public class FileHashCalculator
    {
        private readonly ILogger _logger;
        private const int DefaultBufferSize = 81920; // 80KB

        public delegate void HashProgressHandler(long bytesProcessed, long totalBytes, double progressPercentage);

        public enum HashAlgorithmType
        {
            MD5,
            SHA1,
            SHA256,
            SHA512
        }

        public FileHashCalculator(ILogger logger = null)
        {
            _logger = logger ?? new DefaultLogger();
        }

        // Синхронный метод для обратной совместимости
        public static string CalculateFileHash(string filePath)
        {
            var calculator = new FileHashCalculator();
            return calculator.CalculateFileHashInstance(filePath);
        }

        // Синхронный метод для обратной совместимости
        public string CalculateFileHashInstance(string filePath)
        {
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++)
                    {
                        sb.Append(hash[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при вычислении хеша файла {filePath}: {ex.Message}", ex);
                return "Ошибка вычисления хеша";
            }
        }

        public async Task<string> CalculateFileHashAsync(
            string filePath,
            HashAlgorithmType algorithmType = HashAlgorithmType.SHA256,
            int bufferSize = DefaultBufferSize,
            HashProgressHandler progressHandler = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}");

            try
            {
                _logger.LogInfo($"Начало вычисления хеша для файла: {filePath}");
                
                using (var hashAlgorithm = CreateHashAlgorithm(algorithmType))
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var fileInfo = new FileInfo(filePath);
                    var totalBytes = fileInfo.Length;
                    var bytesProcessed = 0L;
                    var buffer = new byte[bufferSize];
                    var bytesRead = 0;

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        hashAlgorithm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                        bytesProcessed += bytesRead;

                        var progress = (double)bytesProcessed / totalBytes * 100;
                        progressHandler?.Invoke(bytesProcessed, totalBytes, progress);
                    }

                    hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
                    var hash = hashAlgorithm.Hash;
                    var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                    _logger.LogInfo($"Хеш файла {filePath} успешно вычислен: {hashString}");
                    return hashString;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo("Вычисление хеша отменено пользователем");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при вычислении хеша файла {filePath}: {ex.Message}", ex);
                throw;
            }
        }

        private HashAlgorithm CreateHashAlgorithm(HashAlgorithmType algorithmType)
        {
            switch (algorithmType)
            {
                case HashAlgorithmType.MD5:
                    return System.Security.Cryptography.MD5.Create();
                case HashAlgorithmType.SHA1:
                    return System.Security.Cryptography.SHA1.Create();
                case HashAlgorithmType.SHA256:
                    return System.Security.Cryptography.SHA256.Create();
                case HashAlgorithmType.SHA512:
                    return System.Security.Cryptography.SHA512.Create();
                default:
                    throw new ArgumentException($"Неподдерживаемый алгоритм хеширования: {algorithmType}");
            }
        }

        public static string GetAlgorithmName(HashAlgorithmType algorithmType)
        {
            switch (algorithmType)
            {
                case HashAlgorithmType.MD5:
                    return "MD5";
                case HashAlgorithmType.SHA1:
                    return "SHA1";
                case HashAlgorithmType.SHA256:
                    return "SHA256";
                case HashAlgorithmType.SHA512:
                    return "SHA512";
                default:
                    throw new ArgumentException($"Неподдерживаемый алгоритм хеширования: {algorithmType}");
            }
        }
    }
} 