using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace AVFramework.Classes
{
    public class ComputerInfo
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public DateTime Time { get; set; }
    }

    public class InfoComputer
    {
        public static ComputerInfo GetComputerInfo()
        {
            ComputerInfo result = new ComputerInfo();

            // Получение имени компьютера
            string computerName = Environment.MachineName;
            result.Name = computerName;

            // Получение IP-адреса
            string ipAddress = GetLocalIPAddress();
            result.IpAddress = ipAddress;

            // Получение текущей даты и времени
            DateTime currentTime = DateTime.Now;
            result.Time = currentTime;

            return result;
        }

        // Функция для получения локального IP-адреса
        static string GetLocalIPAddress()
        {
            string ipAddress = "";
            try
            {
                // Получаем все локальные IP-адреса
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // Выбираем первый IPv4 адрес
                foreach (IPAddress ip in localIPs)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = ip.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return ipAddress;
        }
    }
}
