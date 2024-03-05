//using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
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

        private static List<string> ProbableViruses;

        Dictionary<string, object> externals = new Dictionary<string, object>()
            {
                { "filename", string.Empty },
                { "filepath", string.Empty },
                { "extension", string.Empty }
            };
        YSInstance yaraInstance = new YSInstance();

        YSContext context = new YSContext();

        YSCompiler compiler = new YSCompiler(null);

        public MainWindow()
        {
            InitializeComponent();
            ruleFilenames = Directory.GetFiles(Path.Combine(BaseDirectory, "Rules"), "*.yar", SearchOption.AllDirectories).ToList();
            //testfiles = Directory.GetFiles(Path.Combine(BaseDirectory, "TestData"), "*.txt", SearchOption.AllDirectories).ToList();
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

        private async void ChooseFileBtn_Click(object sender, RoutedEventArgs e)
        { 
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
                YaraScanner yaraScanner = new YaraScanner(compiler);
                for (int i = 0; i < testfiles.Count; i++)
                {
                    CurrentTask.Text = $"Сейчас сканируется - {testfiles[i]}";
                    //yaraScanner.ScanFile(testfiles[i]).Start();
                    //System.Windows.MessageBox.Show($"File {testfiles[i]} is {yaraScanner.ScanFile(testfiles[i])} virus");
                    //_i++;
                    //ScanPG.Value += _i;

                    //Scan(_i);                   
                    //yaraScanner.ScanFile(testfiles[i]).Wait();
                    bool result = yaraScanner.ScanFile(testfiles[i]);
                    LogBox.AppendText($"File {testfiles[i]} is {result} virus\n");
                    if (result)
                    {
                        ProbableViruses.Add(testfiles[i]);
                    }
                    ScanPG.Value++;
                }
                System.Windows.MessageBox.Show($"Сканирование завершено! Вирусов найдено {ProbableViruses.Count}");
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
    }
}
