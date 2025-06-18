using AVFramework.Classes;
using System;
using System.Windows;
using System.Windows.Documents;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Controls;
using System.Drawing.Imaging;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace AVFramework.Windows
{
    /// <summary>
    /// Логика взаимодействия для ReportVirusWindow.xaml
    /// </summary>
    public partial class ReportVirusWindow : Window
    {
        private List<string> filePaths = new List<string>();
        private bool isSending = false;
        private CancellationTokenSource cancellationTokenSource;
        private const long MAX_FILE_SIZE = 50 * 1024 * 1024; // 50 MB
        private readonly string[] DANGEROUS_EXTENSIONS = { ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs" };
        private readonly string DRAFTS_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AVFramework", "Drafts");
        private readonly string HISTORY_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AVFramework", "History");
        private const int BUFFER_SIZE = 8192; // Оптимальный размер буфера для операций с файлами
        private readonly string ENCRYPTION_KEY = "YourSecretKey123"; // В реальном приложении используйте безопасное хранение ключа

        public ReportVirusWindow()
        {
            InitializeComponent();
            UpdateSendButtonState();
            InitializeFolders();
            
            // Очищаем временные файлы при запуске
            CleanupTempFiles();
            
            // Устанавливаем приоритет процесса
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            }
            catch { /* Игнорируем ошибки при установке приоритета */ }

            // Устанавливаем начальный текст
            CurrentTask.Text = "Готов к отправке";
        }

        private void InitializeFolders()
        {
            try
            {
                string draftsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Drafts");
                if (!Directory.Exists(draftsFolder))
                {
                    Directory.CreateDirectory(draftsFolder);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при инициализации папок: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CleanupTempFiles()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                var tempFiles = Directory.GetFiles(tempPath, "screenshot_*.png");
                foreach (var file in tempFiles)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                    }
                    catch { /* Игнорируем ошибки при удалении временных файлов */ }
                }
            }
            catch { /* Игнорируем ошибки при очистке временных файлов */ }
        }

        private string CalculateFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private bool IsFileModified(string filePath, string originalHash)
        {
            try
            {
                string currentHash = CalculateFileHash(filePath);
                return currentHash != originalHash;
            }
            catch
            {
                return true; // Если не можем проверить, считаем файл измененным
            }
        }

        private string EncryptData(string data)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    byte[] key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32).Substring(0, 32));
                    aes.Key = key;
                    aes.IV = new byte[16];

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(data);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch
            {
                return data; // В случае ошибки возвращаем исходные данные
            }
        }

        private string DecryptData(string encryptedData)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    byte[] key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32).Substring(0, 32));
                    aes.Key = key;
                    aes.IV = new byte[16];

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedData)))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch
            {
                return encryptedData; // В случае ошибки возвращаем исходные данные
            }
        }

        private bool CheckSuspiciousActivity(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var process = Process.GetCurrentProcess();
                
                // Проверяем, не запущен ли файл как процесс
                var runningProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(filePath));
                if (runningProcesses.Length > 0)
                {
                    return true;
                }

                // Проверяем права доступа
                var fileSecurity = File.GetAccessControl(filePath);
                if (fileSecurity.AreAccessRulesProtected)
                {
                    return true;
                }

                // Проверяем атрибуты файла
                if ((fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                    (fileInfo.Attributes & FileAttributes.System) == FileAttributes.System)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return true; // Если не можем проверить, считаем файл подозрительным
            }
        }

        private async void ChooseFileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isSending) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Все файлы|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                await Task.Run(() =>
                {
                    foreach (string file in dialog.FileNames)
                    {
                        if (ValidateFile(file))
                        {
                            if (CheckSuspiciousActivity(file))
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    var result = System.Windows.MessageBox.Show(
                                        $"Файл {Path.GetFileName(file)} имеет признаки подозрительной активности. Вы уверены, что хотите отправить его?",
                                        "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        filePaths.Add(file);
                                    }
                                });
                            }
                            else
                            {
                                filePaths.Add(file);
                            }
                        }
                    }
                });
                
                await Dispatcher.InvokeAsync(() => UpdateFileList());
            }
        }

        private bool ValidateFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > MAX_FILE_SIZE)
                {
                    System.Windows.MessageBox.Show($"Файл {fileInfo.Name} слишком большой. Максимальный размер: 50 МБ", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (DANGEROUS_EXTENSIONS.Contains(fileInfo.Extension.ToLower()))
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Файл {fileInfo.Name} имеет потенциально опасное расширение. Вы уверены, что хотите отправить его?",
                        "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    return result == MessageBoxResult.Yes;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при проверке файла: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void UpdateFileList()
        {
            FilesListBox.ItemsSource = null;
            FilesListBox.ItemsSource = filePaths.Select(path => new { FileName = Path.GetFileName(path) });
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (isSending) return;

            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag is string filePath)
            {
                filePaths.Remove(filePath);
                UpdateFileList();
            }
        }

        private async void AddScreenshotBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isSending) return;

            var button = sender as System.Windows.Controls.Button;
            if (button == null) return;

            try
            {
                string screenshotPath = await Task.Run(() => CaptureScreen());
                if (!string.IsNullOrEmpty(screenshotPath))
                {
                    filePaths.Add(screenshotPath);
                    await Dispatcher.InvokeAsync(() => UpdateFileList());
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при создании скриншота: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string CaptureScreen()
        {
            string screenshotPath = Path.Combine(
                Path.GetTempPath(),
                $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            using (Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                }
                bitmap.Save(screenshotPath, ImageFormat.Png);
            }

            return screenshotPath;
        }

        private void TemplateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isSending) return;

            var comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                string template = selectedItem.Content.ToString();
                string description = "";

                switch (template)
                {
                    case "Подозрительная активность":
                        description = "Обнаружена подозрительная активность в системе. Файл демонстрирует необычное поведение.";
                        break;
                    case "Сетевая активность":
                        description = "Файл пытается установить сетевое соединение или отправляет данные.";
                        break;
                    case "Неизвестный процесс":
                        description = "Обнаружен неизвестный процесс, который может представлять угрозу.";
                        break;
                    case "Подозрительный файл":
                        description = "Файл имеет признаки вредоносного ПО или подозрительного поведения.";
                        break;
                }

                DescriptionRTB.Document.Blocks.Clear();
                DescriptionRTB.Document.Blocks.Add(new Paragraph(new Run(description)));
            }
        }

        private async void SaveDraftBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isSending) return;

            var button = sender as System.Windows.Controls.Button;
            if (button == null) return;

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Черновик отчета|*.draft",
                    Title = "Сохранить черновик",
                    InitialDirectory = DRAFTS_FOLDER
                };

                if (dialog.ShowDialog() == true)
                {
                    var draft = new
                    {
                        Files = filePaths,
                        Description = new TextRange(DescriptionRTB.Document.ContentStart, DescriptionRTB.Document.ContentEnd).Text,
                        Template = (TemplateComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(draft);
                    string encrypted = EncryptData(json);
                    await Task.Run(() => File.WriteAllText(dialog.FileName, encrypted));
                    System.Windows.MessageBox.Show("Черновик сохранен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при сохранении черновика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isSending)
            {
                var button = sender as System.Windows.Controls.Button;
                if (button == null) return;

                var result = System.Windows.MessageBox.Show(
                    "Вы уверены, что хотите отменить отправку?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    cancellationTokenSource?.Cancel();
                    isSending = false;
                    UpdateSendButtonState();
                    ProgressBar.Visibility = Visibility.Collapsed;
                    ResetWindowState();
                }
            }
            else
            {
                Close();
            }
        }

        private void UpdateSendButtonState()
        {
            SendBtn.Content = isSending ? "Отправка..." : "Отправить";
            SendBtn.IsEnabled = !isSending;
            ChooseFileBtn.IsEnabled = !isSending;
            TemplateComboBox.IsEnabled = !isSending;
            DescriptionRTB.IsEnabled = !isSending;
            SaveDraftBtn.IsEnabled = !isSending;
        }

        private void ResetWindowState()
        {
            filePaths.Clear();
            UpdateFileList();
            DescriptionRTB.Document.Blocks.Clear();
            TemplateComboBox.SelectedIndex = -1;
            ProgressBar.Value = 0;
            ProgressBar.Visibility = Visibility.Collapsed;
            isSending = false;
            UpdateSendButtonState();
        }

        private void UpdateCurrentTaskText()
        {
            CurrentTask.Text = "Готов к отправке";
        }

        private async void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isSending) return;

            var button = sender as System.Windows.Controls.Button;
            if (button == null) return;

            if (filePaths.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Выберите файлы для отправки.", 
                    "Предупреждение", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return;
            }

            var description = new TextRange(DescriptionRTB.Document.ContentStart, DescriptionRTB.Document.ContentEnd).Text.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                System.Windows.MessageBox.Show(
                    "Введите описание проблемы.", 
                    "Предупреждение", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return;
            }

            isSending = true;
            UpdateSendButtonState();
            ProgressBar.Value = 0;
            ProgressBar.Visibility = Visibility.Visible;
            CurrentTask.Text = "Отправка отчета...";

            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                var mailClass = new MailClass();
                
                await Task.Run(async () =>
                {
                    try
                    {
                        await mailClass.SendMailAsync(filePaths, description, progress =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ProgressBar.Value = progress;
                                if (progress < 30)
                                    CurrentTask.Text = "Подключение к серверу...";
                                else if (progress < 60)
                                    CurrentTask.Text = "Аутентификация...";
                                else if (progress < 90)
                                    CurrentTask.Text = "Отправка файлов...";
                                else
                                    CurrentTask.Text = "Завершение...";
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                });

                await Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show(
                        "Отчет успешно отправлен!", 
                        "Успех", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"Ошибка при отправке: {ex.Message}", 
                        "Ошибка", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    isSending = false;
                    UpdateSendButtonState();
                    ProgressBar.Visibility = Visibility.Collapsed;
                    UpdateCurrentTaskText();
                    ResetWindowState();
                });
            }
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            /*
            var aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
            */
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            CleanupTempFiles();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
