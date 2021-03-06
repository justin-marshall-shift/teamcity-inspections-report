﻿using teamcity_inspections_report.Common;

namespace teamcity_inspections_report.Duplicates
{
    public class Duplicate
    {
        public int Cost { get; set; }
        public string Key { get; set; }
        public Fragment[] Fragments { get; set; }
    }

    public class Fragment
    {
        public string FileName { get; set; }
        public Range Offset { get; set; }
        public Range Lines { get; set; }
        public string Text { get; set; }
    }
}
