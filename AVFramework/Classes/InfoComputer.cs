using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace CRUDapp
{
    public class InfoComputer
    {
        public static List<string> GetComputerInfo()
        {
            List<string> result = new List<string>();

            // Получение IP-адреса
            string ipAddress = GetLocalIPAddress();
            result.Add(ipAddress);

            // Получение имени компьютера
            string computerName = Environment.MachineName;
            result.Add(computerName);

            // Получение текущей даты и времени
            DateTime currentTime = DateTime.Now;
            result.Add(currentTime.ToString());

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
