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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Window = System.Windows.Window;

namespace AVFramework
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string BaseDirectory = Environment.CurrentDirectory;

        private static List<string> ruleFilenames;

        private static List<string> testfiles;

        private static List<string> ProbableViruses = new List<string>();

        //Dictionary<string, object> externals = new Dictionary<string, object>()
        //    {
        //        { "filename", string.Empty },
        //        { "filepath", string.Empty },
        //        { "extension", string.Empty }
        //    };
        YSInstance YaraInstance = new YSInstance();

        YSContext context = new YSContext();

        YSCompiler Compiler = new YSCompiler(null);

        

        AutoRunClass AutoRun = new AutoRunClass("YaraScanner");

        public MainWindow()
        {
            InitializeComponent();
            ruleFilenames = Directory.GetFiles(Path.Combine(BaseDirectory, "Rules"), "*.yar", SearchOption.AllDirectories).ToList();
            AutoRunCheckBox.IsChecked = AutoRun.IsAutoRun();
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
                        System.Windows.MessageBox.Show("Files found: " + testfiles.Count.ToString(), "Message");
                        DisplayRulesLoad();

                        BackgroundWorker worker = new BackgroundWorker();
                        worker.WorkerReportsProgress = true;
                        worker.DoWork += Scan;
                        worker.ProgressChanged += worker_ProgressChanged;
                        worker.RunWorkerAsync();

                        //Scan();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка");
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

                    //Scan();
                }
            }
        }

        private void Scan(object sender, DoWorkEventArgs e)
        {
            //progress bar реализовать с помощью backgroundWorker => https://wpf-tutorial.com/ru/65/дополнительные-элементы/элемент-управления-progressbar/
            try
            {
                YaraScanner yaraScanner = new YaraScanner();
                for (int i = 0; i < testfiles.Count; i++)
                {

                    Dispatcher.Invoke(() =>
                    {
                        CurrentTask.Text = $"Сейчас сканируется - {testfiles[i]}";
                    });

                    bool result = yaraScanner.ScanFile(testfiles[i], Compiler);

                    Dispatcher.Invoke(() =>
                    {
                        LogBox.AppendText($"File {testfiles[i]} is {result} virus\n");
                    });

                    (sender as BackgroundWorker).ReportProgress(i);

                    if (result)
                    {
                        ProbableViruses.Add(testfiles[i]);
                    }
                }
                System.Windows.MessageBox.Show($"Сканирование завершено! Вирусов найдено {ProbableViruses.Count}");
                if (ProbableViruses.Count > 0)
                {
                    DetectedViruses detectedVirusesWindow = new DetectedViruses(ProbableViruses);
                    detectedVirusesWindow.Show();
                }
                Compiler.Dispose();

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка сканирования");
                Logging.ErrorLog(ex);
                throw;
            }
        }

        private void LoadRules()
        {
            try
            {
                if (Compiler != null)
                {
                    Compiler = YaraInstance.CompileFromFiles(ruleFilenames, null);
                }
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Ошибка загрузки сигнатур");
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
            CurrentTask.Text = "Loading virus signature database, please wait...";

            Thread thread = new Thread(new ThreadStart(LoadRules));
            thread.Start();
            //LoadRules();
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
            AutoRun.SetAutoRun(true);
        }

        private void AutoRunCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AutoRun.SetAutoRun(false);
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
            // Получаем информацию о подключенном устройстве
            //ManagementBaseObject obj = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            //string driveName = obj["DriveName"].ToString();

            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in drives)
            {
                if (drive.DriveType == DriveType.Removable)
                {
                    ScanFlashDrive(drive.Name);
                }
            }


            // Проверяем, является ли подключенное устройство флешкой (можно добавить дополнительные проверки)
            //if (driveName.StartsWith("D:") || driveName.StartsWith("E:") || driveName.StartsWith("F:"))
            //{
            //    // Запускаем сканирование

            //}
        }

        private void ScanFlashDrive(string driveName)
        {
            // Например, можно использовать стороннюю библиотеку или вызвать внешнюю программу для сканирования
            // Например, для запуска внешней программы можно использовать класс Process:
            // Process.Start("путь_к_вашей_программе_сканирования", driveName);


#if WINDOWS10_0_17763_0_OR_GREATER
     new ToastContentBuilder()
            .AddText($"Сканирование флешки {driveName} запущено!")
            .AddText("Check this out, The Enchantments in Washington!")
            .Show();           
#endif

            // Здесь просто пример вывода сообщения о сканировании
            //// Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
            //new ToastContentBuilder()
            //.AddText($"Сканирование флешки {driveName} запущено!")
            //.AddText("Check this out, The Enchantments in Washington!")
            //.Show(); // Not seeing the Show() method? Make sure you have version 7.0, and if you're using .NET 6 (or later), then your TFM must be net6.0-windows10.0.17763.0 or greater
        }
    }
}
