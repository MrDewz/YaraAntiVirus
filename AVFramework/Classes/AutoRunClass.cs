using Microsoft.Win32;
using System;
using System.Reflection;

namespace AVFramework.Classes
{
    public class AutoRunClass
    {
        RegistryKey RegistryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
        
        RegistryKey RegistryIsEnable = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run");
        string ExePath = Assembly.GetExecutingAssembly().Location;

        string Name;

        public AutoRunClass(string name)
        {
            Name = name;
        }

        public bool IsAutoRun()
        {
            var status = RegistryIsEnable.GetValue(Name) as byte[];
            if (status[0] == 2)
                //&& RegistryKey.GetValue(Name) == true)
                return true;
            else
                return false;
        }

        public void SetAutoRun(bool autorun)
        {
            try
            {
                var status = RegistryIsEnable.GetValue(Name) as byte[];
                if (!autorun)
                    RegistryKey.DeleteValue(Name);
                else
                    if (RegistryKey.GetValue(Name) == null)
                {
                    RegistryKey.SetValue(Name, ExePath);
                    status[0] = 2;
                    RegistryIsEnable.SetValue(Name, status);
                }
                    
                else
                    return;

                RegistryKey.Flush();
               // RegistryKey.Close();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
