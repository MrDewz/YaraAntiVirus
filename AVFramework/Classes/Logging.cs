using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVFramework.Classes
{
    public static class Logging
    {
        public static void ErrorLog(Exception exception)
        {
            const string path = "error.log";
            File.WriteAllText(path, exception.ToString());
        }
    }
}
