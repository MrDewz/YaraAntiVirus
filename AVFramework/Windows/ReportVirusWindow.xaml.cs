using AVFramework.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AVFramework.Windows
{
    /// <summary>
    /// Логика взаимодействия для ReportVirusWindow.xaml
    /// </summary>
    public partial class ReportVirusWindow : Window
    {
        string FilePath;
        public ReportVirusWindow()
        {
            InitializeComponent();
        }

        private void ChooseFileBtn_Click(object sender, RoutedEventArgs e)
        {
            string filePath;
            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                    FilePath = filePath;
                    CurrentFile.Text += filePath;
                    CurrentFile.Visibility = Visibility.Visible;
                } 
            }
        }

        private async void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MailClass mail = new MailClass();
                string description = new TextRange(DescriptionRTB.Document.ContentStart, DescriptionRTB.Document.ContentEnd).Text;
                if (string.IsNullOrWhiteSpace(FilePath))
                {
                    MessageBox.Show("Пожалуйста выберите файл!");
                    return;
                }
                if (string.IsNullOrWhiteSpace(description))
                {
                    MessageBox.Show("Пожалуйста опишите причину!");
                    return;
                }
                else
                {
                    var list = InfoComputer.GetComputerInfo();
                    //string compInfo = $"IP Address: {list[0]},Computer Name: {list[1]},Current Date and Time: {list[2]}";
                    await mail.SendMail("Репорт вируса", description, FilePath);
                    MessageBox.Show("Сообщение отправлено!");
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                Logging.ErrorLog(ex);
                throw;
            }

        }
    }
}
