using System;
using System.IO;
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
using System.Security.Policy;
//using System.Windows.Forms;

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
            RefreshListBox();
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

        private void RefreshListBox()
        {
            foreach (var item in ProbableViruses)
            {
                VirusesLB.Items.Add(item);
            }
        }
    }
}
