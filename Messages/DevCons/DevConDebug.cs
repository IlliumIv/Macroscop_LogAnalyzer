namespace LogAnalyzer.Messages.DevCons
{
    class DevConDebug : DevConError
    {
        public DevConDebug(string[] messageStrings) : base(messageStrings) { }

        protected override bool IsSameMessage(object message)
        {
            var m = (DevConDebug)message;

            return this.messageRawBody.Length >= 3 && m.messageRawBody.Length >= 3
                ? this.messageRawBody[2] == m.messageRawBody[2]
                : base.IsSameMessage(message);
        }
    }
}
