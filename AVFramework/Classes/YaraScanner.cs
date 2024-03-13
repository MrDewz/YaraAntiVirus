using AVFramework;
using AVFramework.Classes;
using AVFramework.Entity;
using System.Linq;
using YaraSharp;

namespace AVFramework
{
    public class YaraScanner
    {
        private YSCompiler _compiler;
        //private YSContext _context;
        private YSInstance _yaraInstance = new YSInstance();

        //public YaraScanner()
        //{

        //_compiler = _yaraInstance.CompileFromFiles(ruleFilenames, externals);
        //LoadRules(ruleFiles);
        //_context = new YSContext();
        //}

        //public void LoadRules(string[] ruleFiles)
        //{
        //    foreach (var ruleFile in ruleFiles)
        //    {
        //        _compiler.AddFile(ruleFile);              
        //    }
        //}

        public bool ScanFile(string filePath, YSCompiler compiler)
        {
            //YSReport compilerErrors = _compiler.GetErrors();
            //YSReport compilerWarnings = _compiler.GetWarnings();
            _compiler = compiler;
            YSScanner scanner = new YSScanner(_compiler.GetRules(), null);
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
