using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogAnalyzer.Counters
{
    class ArchCounters : LogCounter
    {
        protected override string dateTimeFormat => "dd.MM HH:mm:ss";
        protected override int dataOffset => 0;
        protected override int timeOffset => 2;

        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] An { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] Enq { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] Shr { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] ArcQSize { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] ArchQSizeMb { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] Wr { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] Fr { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] DbL { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] DbM { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] DbH { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public int[] DbU { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public double[] AvgShrSpeedIn5Sec { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public double[] AvgShrSpeedIn30Sec { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public double[] WriteSpeed { get; private set; }
        [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))] public bool[] ZeroSpeedAlarm { get; private set; }

        public ArchCounters() : base() { }

        protected override void ExtractData(string[] dataString, int index)
        {
            int i = 1;
            var s = dataString[i][(dataString[i].IndexOf("=") + 1)..];
            An[index] = int.Parse(s[..s.IndexOf("Enq")]);
            Enq[index] = int.Parse(dataString[i][(dataString[i++].IndexOf("E") + 4)..]);
            Shr[index] = int.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            ArcQSize[index] = int.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            ArchQSizeMb[index] = int.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            Wr[index] = int.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            Fr[index] = int.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            DbL[index] = int.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            DbM[index] = int.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            DbH[index] = int.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            DbU[index] = int.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            AvgShrSpeedIn5Sec[index] = double.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            AvgShrSpeedIn30Sec[index] = double.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            WriteSpeed[index] = double.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
            ZeroSpeedAlarm[index] = bool.Parse(dataString[i][(dataString[i++].IndexOf("=") + 1)..]);
        }
    }
}
