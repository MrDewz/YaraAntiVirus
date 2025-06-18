using System;
using System.Windows;
using System.Windows.Controls;
using AVFramework.Classes;
using Microsoft.Win32;

namespace AVFramework.Windows
{
    /// <summary>
    /// Логика взаимодействия для ScanHistoryWindow.xaml
    /// </summary>
    public partial class ScanHistoryWindow : Window
    {
        public ScanHistoryWindow()
        {
            InitializeComponent();
            LoadHistory();
        }

        private void LoadHistory()
        {
            try
            {
                HistoryListView.ItemsSource = ScanHistoryManager.GetHistory();
                StatusText.Text = "История загружена";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка загрузки истории: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки истории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OverallProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            StatusText.Text = $"Прогресс: {Math.Round(e.NewValue)}%";
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Вы уверены, что хотите очистить историю сканирования?", 
                                           "Подтверждение", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    ScanHistoryManager.ClearHistory();
                    LoadHistory();
                    StatusText.Text = "История очищена";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка очистки истории: {ex.Message}";
                MessageBox.Show($"Ошибка очистки истории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                    Title = "Экспорт истории сканирования"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ScanHistoryManager.ExportHistory(saveDialog.FileName);
                    StatusText.Text = "История экспортирована";
                    MessageBox.Show("История успешно экспортирована", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка экспорта истории: {ex.Message}";
                MessageBox.Show($"Ошибка экспорта истории: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
