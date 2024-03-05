using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YaraSharp;

namespace AVFramework
{
    public class YaraScanner
    {
        private YSCompiler _compiler;
        //private YSContext _context;
        private YSInstance _yaraInstance = new YSInstance();

        public YaraScanner(YSCompiler compiler)
        {
            _compiler = compiler;
            //_compiler = _yaraInstance.CompileFromFiles(ruleFilenames, externals);
            //LoadRules(ruleFiles);
            //_context = new YSContext();
        }

        //public void LoadRules(string[] ruleFiles)
        //{
        //    foreach (var ruleFile in ruleFiles)
        //    {
        //        _compiler.AddFile(ruleFile);              
        //    }
        //}

        public bool ScanFile(string filePath)
        {
            //YSReport compilerErrors = _compiler.GetErrors();
            //YSReport compilerWarnings = _compiler.GetWarnings();

            YSScanner scanner = new YSScanner(_compiler.GetRules(), null);
            //var f = scanner.ScanFile(filePath);
            bool result = scanner.ScanFile(filePath).Any(r => r.Rule.Identifier == "WarningRule");
            //scanner.Dispose();
            //_compiler.Dispose();
            return result;
          
        }
    }
}
