using AVFramework;
using AVFramework.Classes;
using AVFramework.Entity;
using System.Linq;
using YaraSharp;

namespace AVFramework
{
    public class YaraScanner
    {

        //Memory Leak
        public bool ScanFile(string filePath, YSCompiler compiler)
        {
            //YSReport compilerErrors = _compiler.GetErrors();
            //YSReport compilerWarnings = compiler.GetWarnings();
            YSScanner scanner = new YSScanner(compiler.GetRules(), null);
            var results = scanner.ScanFile(filePath);
            //var f = scanner.ScanFile(filePath);
            bool result = results.Any(r => r.Rule.Identifier == "WarningRule");

            if (result)
            { 
                // добавление данных
                using (YaraAVEntities db = new YaraAVEntities())
                {
                    ComputerInfo infoComputer = new ComputerInfo();
                    infoComputer = InfoComputer.GetComputerInfo();
                    VirusFound VirusFound = new VirusFound()
                    {
                        ComputerName = infoComputer.Name,
                        Ip = infoComputer.IpAddress,
                        DateTime = infoComputer.Time,
                        RuleName = results[0].Rule.Strings.ToString()
                    }; 

                    // добавляем их в бд
                    db.VirusFound.Add(VirusFound);
                    db.SaveChanges();
                }
            }
            scanner.Dispose();
            //_compiler.Dispose();
            return result;

        }
    }
}
