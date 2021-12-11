namespace LogAnalyzer.Messages
{
    class AppConstruct : BaseMessage
    {
        protected override int messageOffset => 2;
        public AppConstruct(string[] messageStrings) : base(messageStrings)
        {
            Instance.Insert(this);
        }
    }
}
