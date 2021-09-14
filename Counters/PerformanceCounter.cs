using System;
using System.Collections.Generic;
using System.Text;

namespace LogAnalyzer.Counters
{
    public class PerformanceCounter : LogCounter
    {
        protected override string dateTimeFormat => "dd.MM.yyyy H:mm:ss";
        protected override int timeOffset => 0;

        public double TotalCpuUsage { get; private set; }
        public double PrivateCpuUsage { get; private set; }
        public int PrivateMemoryUsage { get; private set; }
        public int GCMemoryUsage { get; private set; }
        public int Threads { get; private set; }

        protected override void ExtractData(string[] dataString)
        {
            int i = 1;
            TotalCpuUsage = double.Parse(dataString[i++]);
            PrivateCpuUsage = double.Parse(dataString[i++]);
            PrivateMemoryUsage = int.Parse(dataString[i++]);
            GCMemoryUsage = int.Parse(dataString[i++]);
            Threads = int.Parse(dataString[i++]);
        }
    }
}
