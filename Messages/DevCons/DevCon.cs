using LogAnalyzer.Messages.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogAnalyzer.Messages.DevCons
{
    public abstract class DevCon : BaseMessage
    {
        protected override int messageOffset => offset;
        private int offset = 2;

        // private readonly string regexFormatChannelId = @"ChannelId[ =]*([\d\w]{8}-.{4}-.{4}-.{4}-.{12})[ ,]*";
        private readonly string regexFormatSsType = @"SStype[ =]*(.*?)[ ,]*";
        private readonly string regexFormatSsFunctions = @"SSFunctions[ =]*(.*), DevType";
        private readonly string regexFormatDevType = @"DevType[ =]*([\d\w ]*)[ ,]*";
        private readonly string regexFormatIp = @"Ip = ([\d\w.:]*)[ ,]*";
        private readonly string regexFormatSteamType = @"SteamType[ =]*(\w*)[ ,]*";
        private readonly string regexFormatStreamFormat = @"StreamFormat[ =]*([\d\w]*)";

#nullable enable
        public string? ChannelId { get; protected set; }
        public string? SsType { get; }
        [JsonConverter(typeof(FormattingNoneConverter))] public string[]? SsFunctions { get; }
        public string? DevType { get; }
        public string? Address { get; }
        [JsonConverter(typeof(StringEnumConverter))] public SteamType? SteamType { get; }
        [JsonConverter(typeof(StringEnumConverter))] public StreamFormatType? StreamFormat { get; }
#nullable disable

        public DevCon(string[] messageStrings) : base(messageStrings)
        {
            Regex parameterExpression;
            Match parameterMatch;

            parameterExpression = new Regex(regexFormatSsType);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) SsType = parameterMatch.Groups[1].Value;

            parameterExpression = new Regex(regexFormatSsFunctions);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) SsFunctions = parameterMatch.Groups[1].Value.Replace(" ", "").Split(",");

            parameterExpression = new Regex(regexFormatDevType);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) DevType = parameterMatch.Groups[1].Value;

            parameterExpression = new Regex(regexFormatIp);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) Address = parameterMatch.Groups[1].Value;

            parameterExpression = new Regex(regexFormatSteamType);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) SteamType = GetSteamType(parameterMatch.Groups[1].Value);

            parameterExpression = new Regex(regexFormatStreamFormat);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) StreamFormat = GetStreamFormat(parameterMatch.Groups[1].Value);

            if (messageStrings.Length >= 3)
            {
                parameterExpression = new Regex(@"\{([\d\w]{8}-.{4}-.{4}-.{4}-.{12})\}");
                parameterMatch = parameterExpression.Match(messageStrings[2]);
                if (parameterMatch.Groups[1].Value.Length > 0) offset = 3;
            }

            Instance.Insert(this);
        }

        public override bool Equals(object message)
        {
            try {
                var m = (DevCon)message;

                var ssFunctionsComparer =
                    this.SsFunctions == null || m.SsFunctions == null
                    ? this.SsFunctions == null && m.SsFunctions == null
                    : Enumerable.SequenceEqual(this.SsFunctions, m.SsFunctions);

                return this.ChannelId == m.ChannelId &&
                    this.SsType == m.SsType &&
                    ssFunctionsComparer &&
                    this.DevType == m.DevType &&
                    this.Address == m.Address &&
                    this.SteamType == m.SteamType &&
                    this.StreamFormat == m.StreamFormat &&
                    base.Equals(message);
            } catch (InvalidCastException) { return false; };
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
