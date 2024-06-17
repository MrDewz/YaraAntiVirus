using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

            FileInfo fileInfo = new FileInfo("WhiteList.txt");
            if (results.Count > 0 && fileInfo.Exists)
            {
                var whiteFiles = File.ReadLines("WhiteList.txt");
                if (whiteFiles.Contains(filePath))
                {
                    results.Clear();
                }
            }
            
            scanner.Dispose();

            return results;

        }
    }
}
