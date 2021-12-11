using LogAnalyzer.Counters;
using LogAnalyzer.Messages;
using LogAnalyzer.Messages.DevCons;
using LogAnalyzer.Messages.Enums;
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
        public static List<BaseMessage> ErrorMessages = new List<BaseMessage>();
        public static List<BaseMessage> DebugMessages = new List<BaseMessage>();

        public static void Insert<T>(T obj)
        {
            int index;
            var newMessage = obj as BaseMessage;

            if (newMessage.MessageType < Program.LogLevel) return;

            BaseMessage existedMessage;

            switch (obj.GetType().BaseType.Name)
            {
                case "DevCon":
                    index = DeviceConnectionMessages.FindIndex(t => (obj as DevCon).Equals(t));
                    if (index == -1) {
                        DeviceConnectionMessages.Add(obj as DevCon);
                        return; }
                    existedMessage = DeviceConnectionMessages[index];
                    DeviceConnectionMessages[index] = existedMessage.Concat(newMessage) as DevCon;
                    break;

                default:
                    switch (newMessage.MessageType)
                    {
                        case MessageType.DEBUG:
                            index = DebugMessages.FindIndex(t => newMessage.Equals(t));
                            if (index == -1)
                            {
                                DebugMessages.Add(obj as BaseMessage);
                                return;
                            }
                            existedMessage = DebugMessages[index];
                            DebugMessages[index] = existedMessage.Concat(newMessage);
                            break;

                        default:
                            index = ErrorMessages.FindIndex(t => newMessage.Equals(t));
                            if (index == -1) {
                                ErrorMessages.Add(obj as BaseMessage);
                                return; }
                            existedMessage = ErrorMessages[index];
                            ErrorMessages[index] = existedMessage.Concat(newMessage);
                            break;
                    }

                    break;
            }
        }
    }
}
