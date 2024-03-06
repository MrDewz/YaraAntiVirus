using AVFramework.Classes;
using AVFramework.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using YaraSharp;

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

        Dictionary<string, object> externals = new Dictionary<string, object>()
            {
                { "filename", string.Empty },
                { "filepath", string.Empty },
                { "extension", string.Empty }
            };
        YSInstance yaraInstance = new YSInstance();

        YSContext context = new YSContext();

        YSCompiler compiler = new YSCompiler(null);

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
                        System.Windows.MessageBox.Show("Files found: " + testfiles.Count.ToString(), "Message");
                        DisplayRulesLoad();
                        Scan();
                    }
                }
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Ошибка");
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
                    Scan();
                }
            }
        }

        private void Scan()
        {
            //progress bar реализовать с помощью backgroundWorker => https://wpf-tutorial.com/ru/65/дополнительные-элементы/элемент-управления-progressbar/
            try
            {
                YaraScanner yaraScanner = new YaraScanner();
                for (int i = 0; i < testfiles.Count; i++)
                {
                    CurrentTask.Text = $"Сейчас сканируется - {testfiles[i]}";
                    //yaraScanner.ScanFile(testfiles[i]).Start();
                    //System.Windows.MessageBox.Show($"File {testfiles[i]} is {yaraScanner.ScanFile(testfiles[i])} virus");
                    //_i++;
                    //ScanPG.Value += _i;

                    //Scan(_i);                   
                    //yaraScanner.ScanFile(testfiles[i]).Wait();
                    bool result = yaraScanner.ScanFile(testfiles[i], compiler);

                    LogBox.AppendText($"File {testfiles[i]} is {result} virus\n");
                    ScanPG.Value++;

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

                    //if (System.Windows.MessageBox.Show("Хотите о",
                    //    "Обновление",
                    //    MessageBoxButton.YesNo,
                    //    MessageBoxImage.Information) == MessageBoxResult.Yes)
                    //{

                    //}
                }
                compiler.Dispose();

            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Ошибка сканирования");
                throw;
            }
        }

        private async Task LoadRules()
        {
            try
            {
                compiler = yaraInstance.CompileFromFiles(ruleFilenames, externals);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Ошибка загрузки сигнатур");
                throw;
            }
        }

        private void DisplayRulesLoad()
        {
            string[] LoadingBar = { "[\\]", "[|]", "[/]", "[--]" };
            int i = 0;
            CurrentTask.Text = "Loading virus signature database, please wait...";
            Task.Run(LoadRules);
            LoadRules();
            while (!LoadRules().IsCompleted)
            {
                CurrentTask.Text = "Loading virus signature database, please wait..." + LoadingBar[i];
                i++;
                if (i == 4)
                {
                    i = 0;
                }
            }
        }

        private void AutoRunCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AutoRun.SetAutoRun(true);
        }

        private void AutoRunCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AutoRun.SetAutoRun(false);
        }
    }
}
