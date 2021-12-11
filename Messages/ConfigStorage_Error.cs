namespace LogAnalyzer.Messages
{
    class ConfigStorage_Error : BaseMessage
    {
        public override string Message
        {
            get
            {
                return messageRawBody.Length > messageOffset ?
                    $"{messageRawBody[messageOffset]} {messageRawBody[messageOffset + 1]}" : base.Message;
            }
        }

        protected override int messageOffset => 1;
        public ConfigStorage_Error(string[] messageStrings) : base(messageStrings)
        {
            Instance.Insert(this);
        }
    }
}
