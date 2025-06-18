using AVFramework.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Documents;
using YaraSharp;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ProgressBar = System.Windows.Controls.ProgressBar;
using RichTextBox = System.Windows.Controls.RichTextBox;
using AVFramework.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using NAudio.Wave;
using WMPLib;

namespace AVFramework.Windows
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool firstScan = true;

        private bool isDelete = false;

        private static string baseDirectory = Environment.CurrentDirectory;
        private static string soundDirectory = Path.Combine(baseDirectory, "Sound");
        private static string alertSoundPath = Path.Combine(soundDirectory, "donkey221.wav");
        private static string tempWavPath = Path.Combine(Path.GetTempPath(), "temp_alert.wav");

        private static List<string> ruleFilenames;

        private static List<string> testfiles;
        //ProbableViruses нужен чтоб передать информацию об обнаруженных вирусах в окно DetectedViruses
        private static Dictionary<List<YSMatches>, string> ProbableViruses = new Dictionary<List<YSMatches>, string>();

        private WindowState prevState;

        private YSInstance yaraInstance = new YSInstance();

        private YSContext context = new YSContext();//вызывает MemoryLeak

        private YSCompiler compiler = new YSCompiler(null);

        // AutoRunClass добавляет автозагрузку
        private AutoRunClass autoRun = new AutoRunClass("YaraScanner");

        private bool isDarkTheme = false;

        private SoundPlayer soundPlayer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSoundPlayer();
            ruleFilenames = Directory.EnumerateFiles(Path.Combine(baseDirectory, "Rules"), "*.yar", SearchOption.AllDirectories).ToList();
         
            {
              
            }
            StartListeningForFlashDrive();
            ChangeTheme();
        }

        private void InitializeSoundPlayer()
        {
            try
            {
                if (File.Exists(alertSoundPath))
                {
                    soundPlayer = new SoundPlayer(alertSoundPath);
                    soundPlayer.Load();
                }
                else
                {
                    MessageBox.Show("Файл звука не найден: " + alertSoundPath);
                }
            }
            catch (Exception ex)
            {
                Logging.ErrorLog(ex);
            }
        }

        private void AppendText(string text)
        {
            if (LogBox != null)
            {
                LogBox.Document.Blocks.Add(new Paragraph(new Run(text)));
                LogBox.ScrollToEnd();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var openFolderDialog = new FolderBrowserDialog())
                {
                    DialogResult result = openFolderDialog.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(openFolderDialog.SelectedPath))
                    {
                        CurrentTask.Text = "Загрузка базы сигнатур вирусов, пожалуйста, подождите...";
                        LogBox.Document.Blocks.Clear();
                        testfiles = Directory.EnumerateFiles(openFolderDialog.SelectedPath, "*", SearchOption.AllDirectories).ToList();                     
                        MessageBox.Show(
                            $"Файлов найдено: {testfiles.Count}", 
                            "Сообщение");
                        DisplayRulesLoad();

                        BackgroundWorker worker = new BackgroundWorker();
                        worker.WorkerReportsProgress = true;
                        worker.DoWork += Scan;
                        worker.ProgressChanged += worker_ProgressChanged;
                        worker.RunWorkerAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка");
                Logging.ErrorLog(ex);
                throw;
            }
        }

        private void ChooseFileBtn_Click(object sender, RoutedEventArgs e)
        {
            LogBox.Document.Blocks.Clear();
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    CurrentTask.Text = "Загрузка базы сигнатур вирусов, пожалуйста, подождите...";
                    testfiles = openFileDialog.FileNames.ToList();
                    DisplayRulesLoad();

                    BackgroundWorker worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = true;
                    worker.DoWork += Scan;
                    worker.ProgressChanged += worker_ProgressChanged;
                    worker.RunWorkerAsync();
                }
            }
        }

        private void PlayAlertSound()
        {
            try
            {
                if (soundPlayer != null)
                {
                    soundPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                Logging.ErrorLog(ex);
            }
        }

        private async void Scan(object sender, DoWorkEventArgs e)
        {
            try
            {
                firstScan = false;
                using (var yaraScanner = new YaraScanner())
                using (var compiler = yaraInstance.CompileFromFiles(ruleFilenames, null))
                {
                    ProbableViruses.Clear();
                    int totalFiles = testfiles.Count;
                    int processedFiles = 0;

                    var results = await yaraScanner.ScanFilesAsync(testfiles, compiler, (currentFile, processed, total) =>
                    {
                        processedFiles++;
                        Dispatcher.Invoke(() =>
                        {
                            UpdateProgress(processedFiles, totalFiles, currentFile);
                            LogBox.AppendText($"Сканирование файла {processedFiles} из {totalFiles}: {currentFile}\n");
                        });
                    });

                    foreach (var result in results)
                    {
                        ProbableViruses.Add(result.Value, result.Key);
                        ScanHistoryManager.AddScanResult(result.Key, "Обнаружен вирус");
                        Dispatcher.Invoke(() =>
                        {
                            LogBox.AppendText($"Файл {result.Key} содержит вирус\n");
                        });
                    }

                    Dispatcher.Invoke(() =>
                    {
                        CurrentTask.Text = "Сканирование завершено";
                        if (ProbableViruses.Count > 0)
                        {
                            PlayAlertSound();
                            MessageBox.Show($"Сканирование завершено! Вирусов найдено: {ProbableViruses.Count}");
                            DetectedViruses detectedVirusesWindow = new DetectedViruses(ProbableViruses);
                            detectedVirusesWindow.Show();
                        }
                        else
                        {
                            MessageBox.Show("Сканирование завершено! Вирусов не обнаружено.");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка при сканировании: {ex.Message}");
                    Logging.ErrorLog(ex);
                });
            }
        }

        private void LoadRules()
        {
            try
            {
                if (firstScan)
                {
                    compiler?.Dispose();
                    compiler = yaraInstance.CompileFromFiles(ruleFilenames, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сигнатур: {ex.Message}", 
                    "Ошибка", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                LogBox.AppendText($"Ошибка загрузки сигнатур: {ex.Message}\n");
                Logging.ErrorLog(ex);
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void DisplayRulesLoad()
        {
            string[] loadingAnimation = { "[\\]", "[|]", "[/]", "[--]" };
            int i = 0;

            Dispatcher.Invoke(() =>
            {
                CurrentTask.Text = "Загрузка базы сигнатур вирусов, пожалуйста, подождите...";
            });

            Thread thread = new Thread(new ThreadStart(LoadRules));
            thread.Start();

            while (thread.IsAlive)
            {
                CurrentTask.Text = "Загрузка базы сигнатур вирусов, пожалуйста, подождите..." + loadingAnimation[i];
                i++;
                if (i == 4)
                {
                    i = 0;
                }
            }
            thread.Join();
        }

        private void AutoRunCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            autoRun.SetAutoRun(true);
        }

        private void AutoRunCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (isDelete)
            {
                isDelete = false;
                return;
            }
            autoRun.SetAutoRun(false);
        }

        private void DeleteAutoRunBtn_Click(object sender, RoutedEventArgs e)
        {
            autoRun.DeleteAutoRun();

          
            {
               
            }
            MessageBox.Show("Успешно удалено!");
        }

        private void VirusReport_Click(object sender, RoutedEventArgs e)
        {
            var reportWindow = new ReportVirusWindow();
            reportWindow.Owner = this;
            reportWindow.ShowDialog();
        }

        private void ScanHistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new ScanHistoryWindow();
            historyWindow.Owner = this;
            historyWindow.Show();
        }

        private void StartListeningForFlashDrive()
        {
            // Создаем новый экземпляр класса ManagementEventWatcher
            ManagementEventWatcher watcher = new ManagementEventWatcher();

            // Указываем запрос WMI для отслеживания подключения флешек
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");

            // Устанавливаем запрос для watcher
            watcher.Query = query;

            // Добавляем обработчик события
            watcher.EventArrived += new EventArrivedEventHandler(HandleEvent);

            // Начинаем отслеживание
            watcher.Start();
        }

        private void HandleEvent(object sender, EventArrivedEventArgs e)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in drives)
            {
                if (drive.DriveType == DriveType.Removable)
                {
                    ScanFlashDrive(drive.Name);
                }
            }
        }

        private void ScanFlashDrive(string driveName)
        {
            try
            {
                testfiles = null;
                testfiles = DiskSearcher.GetAllFiles(driveName);              

                if (testfiles == null)
                {
                    throw new Exception("testfiles is null");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            Dispatcher.Invoke(() =>
            {
                DisplayRulesLoad();

                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = true;
                worker.DoWork += Scan;
                worker.ProgressChanged += worker_ProgressChanged;
                worker.RunWorkerAsync();
            });

        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                Hide();
            else
                prevState = WindowState;
        }

        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = prevState;
        }

        private void TaskBarClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AdminAuthBtn_Click(object sender, RoutedEventArgs e)
        {
            //ComputerRegistrationWindow computerRegistrationWindow = new ComputerRegistrationWindow();
            //computerRegistrationWindow.ShowDialog();
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
                // Применяем новые настройки
                ApplySettings();
            }
        }

        private void ApplySettings()
        {
            var settings = Properties.Settings.Default;
            // Применяем настройки сканирования
            // TODO: Реализовать применение настроек в логике сканирования
        }

        private void UpdateProgress(int currentFile, int totalFiles, string currentFileName)
        {
            // Обновляем общий прогресс
            double overallProgress = (double)currentFile / totalFiles * 100;
            OverallProgressBar.Value = overallProgress;
            OverallProgressText.Text = $"Общий прогресс: {overallProgress:F1}%";

            // Обновляем детальный прогресс
            DetailedProgressText.Text = $"Текущий файл: {currentFileName}";
        }

        private void ScanBtn_Click(object sender, RoutedEventArgs e)
        {
            Button_Click(sender, e);
        }

        private void QuarantineBtn_Click(object sender, RoutedEventArgs e)
        {
            var quarantineWindow = new QuarantineWindow();
            quarantineWindow.Owner = this;
            quarantineWindow.ShowDialog();
        }

        private void ReportsBtn_Click(object sender, RoutedEventArgs e)
        {
            ScanHistoryBtn_Click(sender, e);
        }

        private void ChangeTheme()
        {
            var dict = new ResourceDictionary();
            if (isDarkTheme)
            {
                dict.Source = new Uri("/AVFramework;component/Themes/DarkTheme.xaml", UriKind.Relative);
            }
            else
            {
                dict.Source = new Uri("/AVFramework;component/Themes/LightTheme.xaml", UriKind.Relative);
            }

            System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            ChangeTheme();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (soundPlayer != null)
            {
                soundPlayer.Dispose();
                soundPlayer = null;
            }
            // Удаляем временный WAV файл
            try
            {
                if (File.Exists(tempWavPath))
                {
                    File.Delete(tempWavPath);
                }
            }
            catch { }
        }
    }
}
