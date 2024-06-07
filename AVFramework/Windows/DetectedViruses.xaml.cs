using AVFramework.Classes;
using AVFramework.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using YaraSharp;

namespace AVFramework.Windows
{
    /// <summary>
    /// Логика взаимодействия для DetectedViruses.xaml
    /// </summary>
    public partial class DetectedViruses : Window
    {
        private Dictionary<List<YSMatches>, string> ProbableViruses;
        public DetectedViruses(Dictionary<List<YSMatches>, string> probableViruses)
        {
            InitializeComponent();
            ProbableViruses = probableViruses;
            DataContext = this;
            foreach (var item in ProbableViruses)
            {
                VirusesLB.Items.Add(item.Value);
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
            File.Delete(ProbableViruses.ElementAt(index).Value);           
            MessageBox.Show("Файл удален");
            Event(1, index);
        }

        private void RemainButtonClick_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int index = VirusesLB.Items.IndexOf(button.DataContext);
            
            if (MessageBox.Show("Вы уверены что хотите оставить данный файл?", "Действие с файлом", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes )
            {
                VirusesLB.Items.RemoveAt(index);
                MessageBox.Show("Файл занесен в список разрешенных!");
                File.AppendAllText("WhiteList.txt", ProbableViruses.ElementAt(index).Value + "\n");
                Event(2,index);
            }
        }

        private void Event(int action, int index)
        {
            try
            {
                // добавление данных
                using (YaraScannerEntities db = new YaraScannerEntities())
                {
                    ComputerInfo infoComputer = new ComputerInfo();
                    infoComputer = InfoComputer.GetComputerInfo();
                    var ySMatches = ProbableViruses.ElementAt(index).Key;
                    var Identifier = ySMatches[0].Rule.Identifier.ToString();
                    events newEvent = new events()
                    {

                        computer_id = db.computers.FirstOrDefault(u => u.name == infoComputer.Name)?.id,
                        ip_address = infoComputer.IpAddress,
                        virus_id = db.viruses.FirstOrDefault(u => u.name == Identifier)?.id,
                        event_date = infoComputer.Time,
                        action_id = action
                    };

                    // добавляем их в бд
                    db.events.Add(newEvent);
                    db.SaveChanges();
                }
                ProbableViruses.Clear();
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка записи в базу данных");
            }
            
        }
    }
}
