using LiveCharts.Wpf;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlClient;

namespace Reporter
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public SeriesCollection SeriesCollection { get; set; }
        public List<string> Labels { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            List<string> names = ConnectToDB();

                //new List<string> { "Name1", "Name2", "Name1", "Name3", "Name2", "Name2", "Name3" }; // Пример списка с именами

            var nameCounts = names.GroupBy(x => x)
                                  .Select(group => new NameCount { Name = group.Key, Count = group.Count() })
                                  .ToList();

            Labels = nameCounts.Select(x => x.Name).ToList();

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Количество",
                    Values = new ChartValues<int>(nameCounts.Select(x => x.Count))
                }
            };
        }
        private List<string> ConnectToDB()
        {

            // Строка подключения к вашей базе данных
            string connectionString = "Server=DESKTOP-D07LP9M\\SQLEXPRESS;Database=YaraAV;Trusted_Connection=True;";
            List<string> names = new List<string>();
            try
            {
                // Установка соединения с базой данных
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Пример выполнения SQL-запроса
                    string sqlQuery = "SELECT * FROM VirusFound";
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                //Console.WriteLine($"Поле1: {reader["testid"]}, Поле2: {reader["test"]}");
                                names.Add(reader.GetString(1));
                            }
                        }
                    }
                }
                return names;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
                throw;
                //return null;
            }
        }
    }

    public class NameCount
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
