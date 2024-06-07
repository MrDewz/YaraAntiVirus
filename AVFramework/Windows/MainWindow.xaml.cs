using AVFramework.Classes;
using AVFramework.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Management;
using YaraSharp;
using Window = System.Windows.Window;
using MessageBox = System.Windows.MessageBox;

namespace AVFramework
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isDelete = false;

        private static string BaseDirectory = Environment.CurrentDirectory;

        private static List<string> ruleFilenames;

        private static List<string> testfiles;

        private static Dictionary<List<YSMatches>, string> ProbableViruses = new Dictionary<List<YSMatches>, string>();

        WindowState prevState;

        YSInstance yaraInstance = new YSInstance();//вызывает MemoryLeak

        YSContext context = new YSContext();

        YSCompiler compiler = new YSCompiler(null);        

        AutoRunClass autoRun = new AutoRunClass("YaraScanner");

        public MainWindow()
        {
            InitializeComponent();
            ruleFilenames = Directory.GetFiles(Path.Combine(BaseDirectory, "Rules"), "*.yar", SearchOption.AllDirectories).ToList();
            AutoRunCheckBox.IsChecked = autoRun.IsAutoRun();
            StartListeningForFlashDrive();
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
                        LogBox.Document.Blocks.Clear();
                        testfiles = Directory.GetFiles(openFolderDialog.SelectedPath, "*", SearchOption.AllDirectories).ToList();
                        ScanPG.Maximum = testfiles.Count;
                        MessageBox.Show("Files found: " + testfiles.Count.ToString(), "Message");
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
                    //Get the path of specified file
                    testfiles = openFileDialog.FileNames.ToList();
                    ScanPG.Maximum = testfiles.Count;
                    DisplayRulesLoad();

                    BackgroundWorker worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = true;
                    worker.DoWork += Scan;
                    worker.ProgressChanged += worker_ProgressChanged;
                    worker.RunWorkerAsync();
                }
            }
        }

        private void Scan(object sender, DoWorkEventArgs e)
        {
           
            try
            {
                YaraScanner yaraScanner = new YaraScanner();
                for (int i = 0; i < testfiles.Count; i++)
                {

                    Dispatcher.Invoke(() =>
                    {
                        CurrentTask.Text = $"Сейчас сканируется - {testfiles[i]}";
                    });

                    var result = yaraScanner.ScanFile(testfiles[i], compiler);
                    bool resultBool = false;

                    (sender as BackgroundWorker).ReportProgress(i);

                    if (result.Count > 0)
                    {
                        ProbableViruses.Add(result, testfiles[i]);
                        resultBool = true;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        LogBox.AppendText($"File {testfiles[i]} is {resultBool} virus\n");
                    });
                }
                MessageBox.Show($"Сканирование завершено! Вирусов найдено {ProbableViruses.Count}");
                if (ProbableViruses.Count > 0)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        DetectedViruses detectedVirusesWindow = new DetectedViruses(ProbableViruses);
                        detectedVirusesWindow.Show();
                    });
                }
                compiler.Dispose();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сканирования");
                Logging.ErrorLog(ex);
                throw;
            }
        }

        private void LoadRules()
        {
            try
            {
                if (compiler != null)
                {
                    compiler = yaraInstance.CompileFromFiles(ruleFilenames, null);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка загрузки сигнатур");
                throw;
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ScanPG.Value += 1;
        }

        private void DisplayRulesLoad()
        {
            string[] loadingAnimation = { "[\\]", "[|]", "[/]", "[--]" };
            int i = 0;

            Dispatcher.Invoke(() =>
            {
                CurrentTask.Text = "Loading virus signature database, please wait...";
            });

            Thread thread = new Thread(new ThreadStart(LoadRules));
            thread.Start();

            while (thread.IsAlive)
            {
                CurrentTask.Text = "Loading virus signature database, please wait..." + loadingAnimation[i];
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

            if (AutoRunCheckBox.IsChecked == true)
            {
                isDelete = true;
                AutoRunCheckBox.IsChecked = false;
            }    
            MessageBox.Show("Успешно удалено!");
        }

        private void VirusReport_Click(object sender, RoutedEventArgs e)
        {
            ReportVirusWindow reportVirusWindow = new ReportVirusWindow();
            reportVirusWindow.ShowDialog();
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
            // Например, можно использовать стороннюю библиотеку или вызвать внешнюю программу для сканирования
            // Например, для запуска внешней программы можно использовать класс Process:
            // Process.Start("путь_к_вашей_программе_сканирования", driveName);
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
                ScanPG.Maximum = testfiles.Count;
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
            this.Close();
        }
    }
}
