namespace LogAnalyzer.Messages
{
    class AppConstruct : LogMessage
    {
        protected override int messageOffset => 2;
        public AppConstruct(string[] messageStrings) : base(messageStrings)
        {
            Count_Messages();
        }
    }
}
