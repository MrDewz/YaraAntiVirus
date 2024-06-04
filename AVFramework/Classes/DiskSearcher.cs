using System;
using System.Collections.Generic;
using System.IO;

namespace AVFramework.Classes
{
    public class DiskSearcher
    {
        public static List<string> GetAllFiles(string rootDirectory)
        {
            List<string> allFiles = new List<string>();

            try
            {
                // Используем рекурсивный обход каталогов
                GetFilesRecursive(rootDirectory, allFiles);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine($"Access denied to {rootDirectory}: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while accessing {rootDirectory}: {e.Message}");
            }

            return allFiles;
        }

        private static void GetFilesRecursive(string currentDirectory, List<string> allFiles)
        {
            try
            {
                // Добавляем все файлы из текущего каталога
                foreach (var file in Directory.EnumerateFiles(currentDirectory))
                {
                    allFiles.Add(file);
                }

                // Рекурсивно обходим все подкаталоги
                foreach (var directory in Directory.EnumerateDirectories(currentDirectory))
                {
                    GetFilesRecursive(directory, allFiles);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Пропускаем каталоги, к которым нет доступа
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while accessing {currentDirectory}: {e.Message}");
            }
        }
    }
}
