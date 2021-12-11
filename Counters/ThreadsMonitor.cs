using System;
using System.Collections.Generic;
using System.Text;

namespace LogAnalyzer.Counters
{
    class ThreadsMonitor : BaseCounter
    {
        protected override string dateTimeFormat => "yyyy-MM-dd HH:mm:ss,fff";

        protected override int timeOffset => 0;

        public override bool TryExtract<T>(string data, ref T result)
        {
            return base.TryExtract(data, ref result);
        }

        protected override void ExtractData(string[] dataString)
        {
            throw new NotImplementedException();
        }
    }
}
