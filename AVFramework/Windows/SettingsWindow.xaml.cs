using System.Windows;
using System.Windows.Controls;
using AVFramework.Properties;

namespace AVFramework.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Загрузка сохраненных настроек
            var settings = Settings.Default;
            ScanArchivesCheckBox.IsChecked = settings.ScanArchives;
            ScanHiddenFilesCheckBox.IsChecked = settings.ScanHiddenFiles;
            ScanSystemFilesCheckBox.IsChecked = settings.ScanSystemFiles;
            UseHeuristicsCheckBox.IsChecked = settings.UseHeuristics;
            ShowNotificationsCheckBox.IsChecked = settings.ShowNotifications;
            PlaySoundCheckBox.IsChecked = settings.PlaySound;
            AutoUpdateCheckBox.IsChecked = settings.AutoUpdate;
            
            // Установка частоты обновления
            switch (settings.UpdateFrequency)
            {
                case "Ежедневно":
                    UpdateFrequencyComboBox.SelectedIndex = 0;
                    break;
                case "Еженедельно":
                    UpdateFrequencyComboBox.SelectedIndex = 1;
                    break;
                case "Ежемесячно":
                    UpdateFrequencyComboBox.SelectedIndex = 2;
                    break;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Сохранение настроек
            var settings = Settings.Default;
            settings.ScanArchives = ScanArchivesCheckBox.IsChecked ?? false;
            settings.ScanHiddenFiles = ScanHiddenFilesCheckBox.IsChecked ?? false;
            settings.ScanSystemFiles = ScanSystemFilesCheckBox.IsChecked ?? false;
            settings.UseHeuristics = UseHeuristicsCheckBox.IsChecked ?? false;
            settings.ShowNotifications = ShowNotificationsCheckBox.IsChecked ?? false;
            settings.PlaySound = PlaySoundCheckBox.IsChecked ?? false;
            settings.AutoUpdate = AutoUpdateCheckBox.IsChecked ?? false;
            
            // Сохранение частоты обновления
            switch (UpdateFrequencyComboBox.SelectedIndex)
            {
                case 0:
                    settings.UpdateFrequency = "Ежедневно";
                    break;
                case 1:
                    settings.UpdateFrequency = "Еженедельно";
                    break;
                case 2:
                    settings.UpdateFrequency = "Ежемесячно";
                    break;
            }
            
            settings.Save();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
} 