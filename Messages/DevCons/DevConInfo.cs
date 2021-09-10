using System.Text.RegularExpressions;

namespace LogAnalyzer.Messages.DevCons
{
    class DevConInfo : DevConError
    {
        protected override int messageOffset => 1;

        private static readonly string regexFormatId = @", Id[ =]*([0-9]*).*";

        public static int? Id { get; private set; }

        public DevConInfo(string[] messageStrings) : base(ConcatFirstStringIfSplitted(messageStrings)) { }

        private static string[] ConcatFirstStringIfSplitted(string[] messageStrings)
        {
            var parameterExpression = new Regex(regexFormatId);
            var parameterMatch = parameterExpression.Match(messageStrings[1]);

            if (parameterMatch.Groups[1].Value.Length > 0)
            {
                Id = int.Parse(parameterMatch.Groups[1].Value);
                messageStrings[0] = messageStrings[0].Replace("]", parameterMatch.Value + "]");
                messageStrings[1] = messageStrings[1].Replace(parameterMatch.Value, ".");
            }

            return messageStrings;
        }
    }
}
