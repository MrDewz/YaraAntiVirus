using Microsoft.Win32;
using System;
using System.Reflection;

namespace AVFramework.Classes
{
    public class AutoRunClass
    {
        RegistryKey RegistryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");

        string ExePath = Assembly.GetExecutingAssembly().Location;

        string Name;

        public AutoRunClass(string name)
        {
            Name = name;
        }

        public bool IsAutoRun()
        {
            if (RegistryKey.GetValue(Name) != null)
                return true;
            else
                return false;
        }

        public void SetAutoRun(bool autorun)
        {
            try
            {
                if (!autorun)
                    RegistryKey.DeleteValue(Name);
                else
                    if (RegistryKey.GetValue(Name) == null)
                    RegistryKey.SetValue(Name, ExePath);
                else
                    return;

                RegistryKey.Flush();
                RegistryKey.Close();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
