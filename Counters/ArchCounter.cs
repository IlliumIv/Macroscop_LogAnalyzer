using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogAnalyzer.Counters
{
    public class ArchCounter : BaseCounter
    {
        protected override string dateTimeFormat => "dd.MM HH:mm:ss";
        protected override int timeOffset => 2;

        public int An { get; private set; }
        public int Enq { get; private set; }
        public int Shr { get; private set; }
        public int ArcQSize { get; private set; }
        public int ArchQSizeMb { get; private set; }
        public int Wr { get; private set; }
        public int Fr { get; private set; }
        public int DbL { get; private set; }
        public int DbM { get; private set; }
        public int DbH { get; private set; }
        public int DbU { get; private set; }
        public double AvgShrSpeedIn5Sec { get; private set; }
        public double AvgShrSpeedIn30Sec { get; private set; }
        public double WriteSpeed { get; private set; }
        public bool ZeroSpeedAlarm { get; private set; }

        protected override void ExtractData(string[] data)
        {
            int i = 1;
            var s = data[i][(data[i].IndexOf("=") + 1)..];
            An = int.Parse(s[..s.IndexOf("Enq")]);
            Enq = int.Parse(data[i][(data[i++].IndexOf("E") + 4)..]);
            Shr = int.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            ArcQSize = int.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            ArchQSizeMb = int.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            Wr = int.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            Fr = int.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            DbL = int.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            DbM = int.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            DbH = int.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            DbU = int.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            AvgShrSpeedIn5Sec = double.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            AvgShrSpeedIn30Sec = double.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            WriteSpeed = double.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
            ZeroSpeedAlarm = bool.Parse(data[i][(data[i++].IndexOf("=") + 1)..]);
        }
    }
}
