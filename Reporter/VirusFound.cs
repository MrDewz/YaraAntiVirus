using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter
{
    public partial class VirusFound
    {
        public int Id { get; set; }
        public string ComputerName { get; set; }
        public string Ip { get; set; }
        public Nullable<System.DateTime> DateTime { get; set; }
        public string RuleName { get; set; }
    }
}
