using LogAnalyzer.Messages;
using LogAnalyzer.Messages.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace LogAnalyzer.Messages
{
    public abstract class BaseMessage
    {
        public long GlobalID;
        [JsonIgnore] public DateTime TimeStamp { get; }
        [JsonConverter(typeof(FormattingNoneConverter))] public Dictionary<DateTime, long[]> TimeStamps { get; } = new Dictionary<DateTime, long[]>();
#nullable enable
        [JsonIgnore] protected string? Thread { get; }
#nullable disable
        [JsonConverter(typeof(FormattingNoneConverter))] public Dictionary<string, long[]> Threads { get; } = new Dictionary<string, long[]>();
        public virtual string Message { get => string.Join(Environment.NewLine, messageRawBody.Skip(messageOffset).Where(s => s.Length > 1)); }

        // public string Message { get => messageRawBody[messageOffset]; }
        protected abstract int messageOffset { get; }
        public string Type { get => this.GetType().Name; }
        public int Count { get => TimeStamps.Values.Sum(item => item.Length); }
        public DateTime First { get => TimeStamps.Keys.Min(); }
        public DateTime Last { get => TimeStamps.Keys.Max(); }
        protected string[] messageRawBody { get; }
        [JsonConverter(typeof(StringEnumConverter))] public MessageType MessageType { get; }
#nullable enable
#nullable disable

        private readonly string dateTimeFormat = "yyyy-MM-dd HH:mm:ss,fff";
        private readonly string regexFormatDateTime = @"([\d]{4}-.{2}-.{2} .{2}:.{2}:.{2},.{3}).*";
        private readonly string regexFormatThread = @"Thread[ =]*(.*)";
        // private readonly string regexFormatChannelId = @"([\d\w]{8}-.{4}-.{4}-.{4}-.{12})";

        protected BaseMessage(string[] messageStrings)
        {
            GlobalID = Interlocked.Increment(ref Program.GlobalMessageID);
            messageRawBody = messageStrings;

            MessageType = GetMessageType(messageStrings[1]);
            if (MessageType < Program.LogLevel) return;

            string str = messageStrings[0];
            var parameterExpression = new Regex(regexFormatDateTime);
            var parameterMatch = parameterExpression.Match(str);
            if (parameterMatch.Groups[1].Value.Length > 0)
            {
                TimeStamp = DateTime.ParseExact(parameterMatch.Groups[1].Value, dateTimeFormat, null);
                TimeStamps.Add(TimeStamp, new long[1] { this.GlobalID });
            }

            var isInRange = true;
            if (Program.StartTime != null) isInRange = this.TimeStamp >= Program.StartTime;
            if (Program.EndTime != null) isInRange = isInRange && this.TimeStamp <= Program.EndTime;

            if (!isInRange) return;

            int i = messageStrings[0].IndexOf("ChannelId"); if (i > 0) str = messageStrings[0].Substring(0, i);
            i = str.IndexOf(", Id"); if (i > 0) str = str.Substring(0, i);
            i = str.IndexOf("]"); if (i > 0) str = str.Substring(0, i);
            parameterExpression = new Regex(regexFormatThread);
            parameterMatch = parameterExpression.Match(str);
            if (parameterMatch.Groups[1].Value.Length > 0)
            {
                Thread = parameterMatch.Groups[1].Value;
                Threads.Add(Thread, new long[1] { this.GlobalID });
            }
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

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public virtual bool Equals(object message)
        {
            return IsSameMessage(message);
        }

        protected virtual bool IsSameMessage(object message)
        {
            return this.Message == (message as BaseMessage).Message;
        }

        public BaseMessage Concat(BaseMessage objB)
        {
            try { this.TimeStamps.Add(objB.TimeStamp, new long[1] { objB.GlobalID }); }
            catch (ArgumentException)
            {
                var item = this.TimeStamps.First(item => item.Key == objB.TimeStamp);
                this.TimeStamps[item.Key] = item.Value.Concat(new long[1] { objB.GlobalID }).ToArray();
            }
            if (objB.Thread != null) try { this.Threads.Add(objB.Thread, new long[1] { objB.GlobalID }); }
                catch (ArgumentException)
                {
                    var item = this.Threads.First(item => item.Key == objB.Thread);
                    this.Threads[item.Key] = item.Value.Concat(new long[1] { objB.GlobalID }).ToArray();
                }

            return this;
        }

        public class ConsoleOutContractResolver : DefaultContractResolver
        {
            public static readonly ConsoleOutContractResolver Instance = new ConsoleOutContractResolver();

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (property.PropertyName == "GlobalID"
                    || property.PropertyName == "TimeStamps"
                    || property.PropertyName == "Threads")
                    property.ShouldSerialize = instance => { return false; };

                return property;
            }
        }

        public class FormattingNoneConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return true;
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
