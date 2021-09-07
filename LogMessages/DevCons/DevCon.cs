﻿using FindChannels.LogMessages.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace FindChannels.LogMessages.DevCons
{
    class DevCon : LogMessage
    {
        protected override int messageOffset => offset;
        private readonly int offset = 2;

        // private readonly string regexFormat_ChannelId = @"ChannelId[ =]*([\d\w]{8}-.{4}-.{4}-.{4}-.{12})[ ,]*";
        private readonly string regexFormat_SsType = @"SStype[ =]*(.*?)[ ,]*";
        private readonly string regexFormat_SsFunctions = @"SSFunctions[ =]*(.*), DevType";
        private readonly string regexFormat_DevType = @"DevType[ =]*([\d\w ]*)[ ,]*";
        private readonly string regexFormat_Ip = @"Ip = ([\d\w.:]*)[ ,]*";
        private readonly string regexFormat_SteamType = @"SteamType[ =]*(\w*)[ ,]*";
        private readonly string regexFormat_StreamFormat = @"StreamFormat[ =]*([\d\w]*)";

#nullable enable
        public string? SsType { get; }
        [JsonConverter(typeof(ArrayConverter))] public string[]? SsFunctions { get; }
        public string? DevType { get; }
        public string? Address { get; }
        [JsonConverter(typeof(StringEnumConverter))] public SteamType? SteamType { get; }
        [JsonConverter(typeof(StringEnumConverter))] public StreamFormatType? StreamFormat { get; }
#nullable disable

        public DevCon(string[] messageStrings) : base(messageStrings)
        {
            Regex parameterExpression;
            Match parameterMatch;

            parameterExpression = new Regex(regexFormat_SsType);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) SsType = parameterMatch.Groups[1].Value;

            parameterExpression = new Regex(regexFormat_SsFunctions);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) SsFunctions = parameterMatch.Groups[1].Value.Replace(" ", "").Split(",");

            parameterExpression = new Regex(regexFormat_DevType);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) DevType = parameterMatch.Groups[1].Value;

            parameterExpression = new Regex(regexFormat_Ip);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) Address = parameterMatch.Groups[1].Value;

            parameterExpression = new Regex(regexFormat_SteamType);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) SteamType = GetSteamType(parameterMatch.Groups[1].Value);

            parameterExpression = new Regex(regexFormat_StreamFormat);
            parameterMatch = parameterExpression.Match(messageStrings[0]);
            if (parameterMatch.Groups[1].Value.Length > 0) StreamFormat = GetStreamFormat(parameterMatch.Groups[1].Value);

            if (messageStrings.Length >= 3)
            {
                parameterExpression = new Regex(@"\{([\d\w]{8}-.{4}-.{4}-.{4}-.{12})\}");
                parameterMatch = parameterExpression.Match(messageStrings[2]);
                if (parameterMatch.Groups[1].Value.Length > 0) offset = 3;
            }
        }

        public override bool Equals(object message)
        {
            var m = (DevCon)message;

            var ssFunctionsComparer = 
                this.SsFunctions == null || m.SsFunctions == null
                ? this.SsFunctions == null && m.SsFunctions == null
                : Enumerable.SequenceEqual(this.SsFunctions, m.SsFunctions);

            return this.SsType == m.SsType &&
                ssFunctionsComparer &&
                this.DevType == m.DevType &&
                this.Address == m.Address &&
                this.SteamType == m.SteamType &&
                this.StreamFormat == m.StreamFormat &&
                base.Equals(message);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}