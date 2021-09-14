using LogAnalyzer.Counters;
using LogAnalyzer.Messages;
using LogAnalyzer.Messages.DevCons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LogAnalyzer
{
    public static class Instance
    {
        public static List<ArchCounter> ArchCounters = new List<ArchCounter>();
        public static List<PerformanceCounter> Performances = new List<PerformanceCounter>();
        public static List<DevCon> DeviceConnectionMessages = new List<DevCon>();
        public static List<LogMessage> ChannelParams = new List<LogMessage>();

        public static void Insert<T>(T obj)
        {
            int index;
            var newMessage = obj as LogMessage;
            LogMessage existedMessage;

            switch (obj.GetType().BaseType.Name)
            {
                case "DevCon":
                    index = DeviceConnectionMessages.FindIndex(t => (obj as DevCon).Equals(t));

                    if (index == -1)
                    {
                        DeviceConnectionMessages.Add(obj as DevCon);
                        return;
                    }

                    existedMessage = DeviceConnectionMessages[index];
                    DeviceConnectionMessages[index] = existedMessage.Concat(newMessage) as DevCon;

                    break;
                default:

                    index = ChannelParams.FindIndex(t => newMessage.Equals(t));
                    if (index == -1)
                    {
                        ChannelParams.Add(obj as LogMessage);
                        return;
                    }

                    existedMessage = ChannelParams[index];
                    ChannelParams[index] = existedMessage.Concat(newMessage);

                    break;
            }
        }
    }
}
