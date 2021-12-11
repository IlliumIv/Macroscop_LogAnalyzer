using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LogAnalyzer.Counters
{
    public abstract class BaseCounter
    {
        // [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))]
        public DateTime TimeStamp { get; set; }
        protected string rawData { get; private set; }
        protected abstract string dateTimeFormat { get; }
        // protected abstract int dataOffset { get; }
        protected abstract int timeOffset { get; }

        public BaseCounter() { }

        public virtual bool TryExtract<T>(string data, ref T result)
        {
            rawData = data;

            var isInRange = true;
            var dataString = rawData.Split("\t");
            TimeStamp = DateTime.ParseExact(dataString[0][timeOffset..], dateTimeFormat, null);

            if (Program.StartTime != null) isInRange = this.TimeStamp >= Program.StartTime;
            if (Program.EndTime != null) isInRange = isInRange && this.TimeStamp <= Program.EndTime;

            ExtractData(dataString);
            return isInRange;
        }

        protected abstract void ExtractData(string[] dataString);
    }
}
