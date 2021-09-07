namespace FindChannels.LogMessages
{
    class Error : LogMessage
    {
        protected override int messageOffset => 2;
        public Error(string[] messageStrings) : base(messageStrings) { }
    }
}
