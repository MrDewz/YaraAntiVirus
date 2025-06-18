using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AVFramework
{
    public static class QuarantineManager
    {
        private static readonly string QuarantineFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AVFramework",
            "Quarantine"
        );
        private static readonly string QuarantineInfoFile = Path.Combine(QuarantineFolder, "quarantine_info.json");
        private static List<QuarantineItem> quarantinedFiles = new List<QuarantineItem>();

        static QuarantineManager()
        {
            InitializeQuarantine();
        }

        private static void InitializeQuarantine()
        {
            if (!Directory.Exists(QuarantineFolder))
            {
                Directory.CreateDirectory(QuarantineFolder);
            }

            if (File.Exists(QuarantineInfoFile))
            {
                LoadQuarantineInfo();
            }
        }

        private static void LoadQuarantineInfo()
        {
            try
            {
                string json = File.ReadAllText(QuarantineInfoFile);
                quarantinedFiles = JsonSerializer.Deserialize<List<QuarantineItem>>(json) ?? new List<QuarantineItem>();
            }
            catch
            {
                quarantinedFiles = new List<QuarantineItem>();
            }
        }

        private static void SaveQuarantineInfo()
        {
            string json = JsonSerializer.Serialize(quarantinedFiles);
            File.WriteAllText(QuarantineInfoFile, json);
        }

        public static async Task<bool> QuarantineFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                string fileName = Path.GetFileName(filePath);
                string quarantinePath = Path.Combine(QuarantineFolder, fileName);

                // Перемещаем файл в карантин
                File.Move(filePath, quarantinePath);

                // Добавляем информацию о файле
                var item = new QuarantineItem
                {
                    FileName = fileName,
                    OriginalPath = filePath,
                    QuarantinePath = quarantinePath,
                    QuarantineDate = DateTime.Now
                };

                quarantinedFiles.Add(item);
                SaveQuarantineInfo();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> RestoreFile(QuarantineItem item)
        {
            try
            {
                if (!File.Exists(item.QuarantinePath))
                    return false;

                // Восстанавливаем файл
                File.Move(item.QuarantinePath, item.OriginalPath);

                // Удаляем информацию о файле
                quarantinedFiles.Remove(item);
                SaveQuarantineInfo();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> DeleteFile(QuarantineItem item)
        {
            try
            {
                if (!File.Exists(item.QuarantinePath))
                    return false;

                // Удаляем файл
                File.Delete(item.QuarantinePath);

                // Удаляем информацию о файле
                quarantinedFiles.Remove(item);
                SaveQuarantineInfo();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<QuarantineItem> GetQuarantinedFiles()
        {
            return new List<QuarantineItem>(quarantinedFiles);
        }
    }
} 