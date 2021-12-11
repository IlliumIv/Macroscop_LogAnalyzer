namespace LogAnalyzer.Messages
{
    class NetworkServer : BaseMessage
    {
        protected override int messageOffset => 2;
        public NetworkServer(string[] messageStrings) : base(messageStrings)
        {
            Instance.Insert(this);
        }
    }
}
