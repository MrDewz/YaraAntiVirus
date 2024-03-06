using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace AVFramework.Windows
{
    /// <summary>
    /// Логика взаимодействия для DetectedViruses.xaml
    /// </summary>
    public partial class DetectedViruses : Window
    {
        private List<string> ProbableViruses;
        public DetectedViruses(List<string> probableViruses)
        {
            InitializeComponent();
            ProbableViruses = probableViruses;
            DataContext = this;
            foreach (var item in ProbableViruses)
            {
                VirusesLB.Items.Add(item);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ProbableViruses.Clear();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int index = VirusesLB.Items.IndexOf(button.DataContext);
            VirusesLB.Items.RemoveAt(index);
            File.Delete(ProbableViruses[index]);
        }

        private void RemainButtonClick_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int index = VirusesLB.Items.IndexOf(button.DataContext);
            VirusesLB.Items.RemoveAt(index);
        }
    }
}
