using AVFramework;
using AVFramework.Classes;
using AVFramework.Entity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YaraSharp;

namespace AVFramework
{
    public class YaraScanner
    {

        //Memory Leak
        public List<YSMatches> ScanFile(string filePath, YSCompiler compiler)
        {

            YSScanner scanner = new YSScanner(compiler.GetRules(), null);
            List<YSMatches> results = scanner.ScanFile(filePath);
            //bool result = results.Any(r => r.Rule.Identifier == "WarningRule");
            FileInfo fileInfo = new FileInfo("WhiteList.txt");
            if (results.Count > 0 && fileInfo.Exists) {
                
                var all = System.IO.File.ReadLines("WhiteList.txt");
                if (all.Contains(filePath))
                {
                    results.Clear();
                }
            }
            scanner.Dispose();
            //_compiler.Dispose();
            return results;

        }
    }
}
