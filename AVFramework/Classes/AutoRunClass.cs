using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        public bool SetAutoRun(bool autorun)
        {
            try
            {
                if (!autorun)
                    RegistryKey.DeleteValue(Name);
                else
                    if (RegistryKey.GetValue(Name) == null)
                        RegistryKey.SetValue(Name, ExePath);
                    else
                        return false;

                RegistryKey.Flush();
                RegistryKey.Close();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
