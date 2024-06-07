using System;
using System.IO;

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
