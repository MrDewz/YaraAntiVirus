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

        public void DeleteAutoRun()
        {
            RegistryKey.DeleteValue(Name, false);
        }

        public bool IsAutoRun()
        {
            var status = RegistryIsEnable.GetValue(Name) as byte[];
            //var status2 = RegistryKey.GetValue(Name);
            if (status[0] == 2)
                //&& RegistryKey.GetValue(Name) == true)
                return true;
            else
                return false;
        }

        public void SetAutoRun(bool enable)
        {
            try
            {
                if (RegistryKey.GetValue(Name) == null)
                    RegistryKey.SetValue(Name, ExePath);

                var status = RegistryIsEnable.GetValue(Name) as byte[];

                if (enable)
                {
                    status[0] = 2;
                    RegistryIsEnable.SetValue(Name, status);
                }

                else
                {                   
                    status[0] = 3;
                    RegistryIsEnable.SetValue(Name, status);
                }
                    
                RegistryKey.Flush();
                //RegistryKey.Close();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
