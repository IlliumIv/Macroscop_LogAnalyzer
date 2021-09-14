namespace LogAnalyzer.Messages
{
    class Error : LogMessage
    {
        protected override int messageOffset => 2;
        public Error(string[] messageStrings) : base(messageStrings)
        {
            Count_Messages();
        }
    }
}
