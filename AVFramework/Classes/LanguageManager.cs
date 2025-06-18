using System;
using System.Globalization;
using System.Windows;

namespace AVFramework.Classes
{
    public class LanguageManager
    {
        private static LanguageManager _instance;
        public static LanguageManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LanguageManager();
                return _instance;
            }
        }

        public event EventHandler LanguageChanged;

        private LanguageManager() { }

        public void ChangeLanguage(string languageCode)
        {
            try
            {
                var culture = new CultureInfo(languageCode);
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                
                // Обновляем строки ресурсов
                var resources = Application.Current.Resources;
                if (languageCode == "ru")
                {
                    resources["ChooseFile"] = "Выбрать файл";
                    resources["Scan"] = "Сканировать";
                    resources["Settings"] = "Настройки";
                    resources["Quarantine"] = "Карантин";
                    resources["Reports"] = "Отчеты";
                }
                else if (languageCode == "en")
                {
                    resources["ChooseFile"] = "Choose File";
                    resources["Scan"] = "Scan";
                    resources["Settings"] = "Settings";
                    resources["Quarantine"] = "Quarantine";
                    resources["Reports"] = "Reports";
                }

                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при смене языка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 