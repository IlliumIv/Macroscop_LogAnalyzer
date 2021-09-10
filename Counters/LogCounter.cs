using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogAnalyzer.Counters
{
    abstract class LogCounter
    {
        // [JsonConverter(typeof(Messages.LogMessage.ArrayConverter))]
        public DateTime[] DateTimes { get; set; }
        protected string[] rawData { get; private set; }
        protected abstract string dateTimeFormat { get; }
        protected abstract int dataOffset { get; }
        protected abstract int timeOffset { get; }

        protected LogCounter() { }

        public void Append(string[] data)
        {
            int i = 0;
            var d = data.Skip(dataOffset).ToArray();

            if (rawData == null)
            {
                rawData = d;
            }
            else
            {
                i = rawData.Length;
                rawData = rawData.Concat(d).ToArray();
            }

            CreateOrExpandProperties();

            for (int j = i; j < rawData.Length; j++)
            {
                var dataString = rawData[j].Split("\t");
                DateTimes[j] = ExtractCounterTime(dataString[0]);
                ExtractData(dataString, j);
            }
        }


        protected virtual DateTime ExtractCounterTime(string timeString)
        {
            return DateTime.ParseExact(timeString[timeOffset..], dateTimeFormat, null);
        }

        protected abstract void ExtractData(string[] dataString, int index);

        protected virtual void CreateOrExpandProperties()
        {
            var type = this.GetType();
            var props = type.GetProperties();

            for (int i = 0; i < props.Length; i++)
            {
                if (props[i].PropertyType.BaseType != typeof(Array)) continue;
                var arr = Array.CreateInstance(type.GetProperty(props[i].Name).PropertyType.GetElementType(), rawData.Length);
                Array some = (Array)props[i].GetValue(this);
                if (some != null) some.CopyTo(arr, 0);
                props[i].SetValue(this, arr);
            }
        }
    }
}
