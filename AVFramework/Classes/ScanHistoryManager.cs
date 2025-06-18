using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AVFramework.Classes
{
    public static class ScanHistoryManager
    {
        private static readonly string HistoryFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AVFramework",
            "scan_history.csv");

        public static List<ScanHistoryItem> GetHistory()
        {
            try
            {
                if (!File.Exists(HistoryFilePath))
                    return new List<ScanHistoryItem>();

                var lines = File.ReadAllLines(HistoryFilePath);
                return lines.Skip(1) // Пропускаем заголовок
                           .Select(line => line.Split(','))
                           .Where(parts => parts.Length >= 3)
                           .Select(parts => new ScanHistoryItem
                           {
                               ScanDate = DateTime.Parse(parts[0]),
                               FilePath = parts[1],
                               Result = parts[2]
                           })
                           .ToList();
            }
            catch (Exception)
            {
                return new List<ScanHistoryItem>();
            }
        }

        public static void AddToHistory(ScanHistoryItem item)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryFilePath));
                
                if (!File.Exists(HistoryFilePath))
                {
                    File.WriteAllText(HistoryFilePath, "ScanDate,FilePath,Result\n");
                }

                File.AppendAllText(HistoryFilePath, 
                    $"{item.ScanDate:yyyy-MM-dd HH:mm:ss},{item.FilePath},{item.Result}\n");
            }
            catch (Exception)
            {
                // Игнорируем ошибки записи в историю
            }
        }

        public static void ClearHistory()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    File.Delete(HistoryFilePath);
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки удаления файла
            }
        }

        public static void ExportHistory(string exportPath)
        {
            try
            {
                var history = GetHistory();
                var lines = new List<string> { "ScanDate,FilePath,Result" };
                lines.AddRange(history.Select(item => 
                    $"{item.ScanDate:yyyy-MM-dd HH:mm:ss},{item.FilePath},{item.Result}"));
                
                File.WriteAllLines(exportPath, lines);
            }
            catch (Exception)
            {
                throw new Exception("Не удалось экспортировать историю сканирования");
            }
        }

        public static void AddScanResult(string filePath, string result)
        {
            var item = new ScanHistoryItem
            {
                ScanDate = DateTime.Now,
                FilePath = filePath,
                Result = result
            };

            AddToHistory(item);
        }
    }

    public class ScanHistoryItem
    {
        public DateTime ScanDate { get; set; }
        public string FilePath { get; set; }
        public string Result { get; set; }
    }
} 