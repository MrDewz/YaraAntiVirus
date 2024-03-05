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
    /// Логика взаимодействия для DetectedViruses.xaml
    /// </summary>
    public partial class DetectedViruses : Window
    {
        private List<string> ProbableViruses;
        public DetectedViruses(List<string> probableViruses)
        {
            InitializeComponent();
            ProbableViruses = probableViruses;
            VirusesLB.ItemsSource = ProbableViruses;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ProbableViruses.Clear();
        }
    }
}
