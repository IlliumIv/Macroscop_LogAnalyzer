using FindChannels.LogMessages;
using FindChannels.LogMessages.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace FindChannels.LogMessages
{
    abstract class LogMessage
    {
        // public long ID;
        private DateTime TimeStamp { get; }
        [JsonConverter(typeof(ArrayConverter))] public HashSet<DateTime> TimeStamps { get; } = new HashSet<DateTime>();
#nullable enable
        private string? Thread { get; }
#nullable disable
        [JsonConverter(typeof(ArrayConverter))] public HashSet<string> Threads { get; } = new HashSet<string>();
        public string Message { get => string.Join(Environment.NewLine, messageRawBody.Skip(messageOffset).Where(s => s.Length > 1)); }
        protected abstract int messageOffset { get; }
        public string Type { get => this.GetType().FullName; }
        public int Count { get => TimeStamps.Count; }
        protected string[] messageRawBody { get; }
        [JsonConverter(typeof(StringEnumConverter))] public MessageType MessageType { get; }
#nullable enable
        [JsonProperty(Order = -2)] public string? ChannelId { get; protected set; }
#nullable disable

        private readonly string dateTimeFormat = "yyyy-MM-dd HH:mm:ss,fff";
        private readonly string regexFormat_DateTime = @"([\d]{4}-.{2}-.{2} .{2}:.{2}:.{2},.{3}).*";
        private readonly string regexFormat_Thread = @"Thread[ =]*(.*)";
        private readonly string regexFormat_ChannelId = @"([\d\w]{8}-.{4}-.{4}-.{4}-.{12})";

        protected LogMessage(string [] messageStrings)
        {
            messageRawBody = messageStrings;

            var parameterExpression = new Regex(regexFormat_ChannelId);
            var parameterMatch = parameterExpression.Match(JsonConvert.SerializeObject(messageStrings));
            if (parameterMatch.Groups[1].Value.Length > 0) ChannelId = parameterMatch.Groups[1].Value;

            parameterExpression = new Regex(regexFormat_DateTime);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0)
            {
                TimeStamp = DateTime.ParseExact(parameterMatch.Groups[1].Value, dateTimeFormat, null);
                TimeStamps.Add(TimeStamp);
            }

            string str = messageStrings[0];
            int i = messageStrings[0].IndexOf("ChannelId"); if (i > 0) str = messageStrings[0].Substring(0, i);
            i = str.IndexOf(", Id"); if (i > 0) str = str.Substring(0, i);
            i = str.IndexOf("]"); if (i > 0) str = str.Substring(0, i);
            parameterExpression = new Regex(regexFormat_Thread);
            parameterMatch = parameterExpression.Match(str);
            if (parameterMatch.Groups[1].Value.Length > 0)
            {
                Thread = parameterMatch.Groups[1].Value;
                Threads.Add(Thread);
            }

            MessageType = GetMessageType(messageStrings[1]);

            Count_Messages();
        }

        protected MessageType GetMessageType(string value)
        {
            return value switch
            {
                "ERROR" => MessageType.ERROR,
                "EXCEPTION" => MessageType.EXCEPTION,
                "DEBUG" => MessageType.DEBUG,
                _ => MessageType.UNKNOWN,
            };
        }

        protected StreamFormatType? GetStreamFormat(string value)
        {
            return value switch
            {
                "MJPEG" => StreamFormatType.MJPEG,
                "H264" => StreamFormatType.H264,
                "H265" => StreamFormatType.H265,
                "MPEG4_Part2" => StreamFormatType.MPEG4_Part2,
                "MxPEG" => StreamFormatType.MxPEG,
                _ => null,
            };
        }

        protected SteamType? GetSteamType(string value)
        {
            return value switch
            {
                "MAIN" => SteamType.MAIN,
                "ALTERNATIVE" => SteamType.ALTERNATIVE,
                _ => null,
            };
        }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        public virtual bool Equals(object message)
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            return this.ChannelId == (message as LogMessage).ChannelId
                && IsSameMessage(message);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        protected virtual bool IsSameMessage(object message)
        {
            return this.Message == (message as LogMessage).Message;
        }

        protected virtual void Count_Messages()
        {
            var isInRange = true;
            if (Program.StartTime != null) isInRange = this.TimeStamp >= Program.StartTime;
            if (Program.EndTime != null) isInRange = isInRange && this.TimeStamp <= Program.EndTime;

            if (!isInRange) return;

            var i = Program.ChannelParams.FindIndex(t => t.Equals(this));

            switch (i)
            {
                case -1:
                    Program.ChannelParams.Add(this);
                    break;
                default:
                    var message = Program.ChannelParams[i];
                    message.TimeStamps.Add(this.TimeStamp);
                    if (this.Thread != null) message.Threads.Add(this.Thread);
                    Program.ChannelParams[i] = message;
                    break;
            }
        }

        public class ConsoleOutContractResolver : DefaultContractResolver
        {
            public static readonly ConsoleOutContractResolver Instance = new ConsoleOutContractResolver();

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (property.PropertyName == "ID"
                    || property.PropertyName == "TimeStamp"
                    || property.PropertyName == "TimeStamps"
                    || property.PropertyName == "Thread"
                    || property.PropertyName == "Id")
                    property.ShouldSerialize = instance => { return false; };

                return property;
            }
        }

        protected class ArrayConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(string[]);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteRawValue(JsonConvert.SerializeObject(value, Formatting.None));
            }
        }
    }
}
