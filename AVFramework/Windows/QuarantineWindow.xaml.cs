using AVFramework.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AVFramework.Windows
{
    public partial class QuarantineWindow : Window
    {
        private List<QuarantineItem> quarantinedFiles;

        public QuarantineWindow()
        {
            InitializeComponent();
            LoadQuarantinedFiles();
        }

        private void LoadQuarantinedFiles()
        {
            quarantinedFiles = QuarantineManager.GetQuarantinedFiles();
            QuarantineList.ItemsSource = quarantinedFiles;
        }

        private async void RestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = QuarantineList.SelectedItem as QuarantineItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Выберите файл для восстановления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Вы уверены, что хотите восстановить этот файл?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (await QuarantineManager.RestoreFile(selectedItem))
                {
                    MessageBox.Show("Файл успешно восстановлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadQuarantinedFiles();
                }
                else
                {
                    MessageBox.Show("Не удалось восстановить файл", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = QuarantineList.SelectedItem as QuarantineItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Выберите файл для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить этот файл? Это действие нельзя отменить.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (await QuarantineManager.DeleteFile(selectedItem))
                {
                    MessageBox.Show("Файл успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadQuarantinedFiles();
                }
                else
                {
                    MessageBox.Show("Не удалось удалить файл", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 