using System;

namespace AVFramework
{
    public class QuarantineItem
    {
        public string FileName { get; set; }
        public string OriginalPath { get; set; }
        public string QuarantinePath { get; set; }
        public DateTime QuarantineDate { get; set; }
        public string QuarantineDateString => QuarantineDate.ToString("dd.MM.yyyy HH:mm:ss");
    }
} 