﻿using AVFramework.Classes;
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
            try
            {
                Button button = sender as Button;
                int index = VirusesLB.Items.IndexOf(button.DataContext);
                VirusesLB.Items.RemoveAt(index);
                File.Delete(ProbableViruses.ElementAt(index).Value);
                MessageBox.Show("Файл удален");
                //ProbableViruses.Remove(ProbableViruses.ElementAt(index).Key);
                Event(1, index);
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось удалить! Возможно путь до файла изменился");
            }
        }

        private void RemainButtonClick_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                int index = VirusesLB.Items.IndexOf(button.DataContext);

                if (MessageBox.Show("Вы уверены что хотите оставить данный файл?", "Действие с файлом", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    VirusesLB.Items.RemoveAt(index);
                    MessageBox.Show("Файл занесен в список разрешенных!");
                    
                    var yaraScanner = new YaraScanner();
                    yaraScanner.AddToWhiteList(ProbableViruses.ElementAt(index).Value);
                    
                    Event(2, index);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось разрешить!");
            }
        }

        private async void QuarantineButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                int index = VirusesLB.Items.IndexOf(button.DataContext);

                if (MessageBox.Show("Вы уверены, что хотите поместить файл в карантин?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    string filePath = ProbableViruses.ElementAt(index).Value;
                    if (await QuarantineManager.QuarantineFile(filePath))
                    {
                        VirusesLB.Items.RemoveAt(index);
                        MessageBox.Show("Файл успешно помещен в карантин!");
                        Event(3, index);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось поместить файл в карантин!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при помещении в карантин: {ex.Message}");
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
                    ProbableViruses.Remove(ProbableViruses.ElementAt(index).Key);
                    // добавляем их в бд
                    db.events.Add(newEvent);
                    db.SaveChanges();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка записи в базу данных");
            }
        }
    }
}
