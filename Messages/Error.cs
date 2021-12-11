﻿namespace LogAnalyzer.Messages
{
    class Error : BaseMessage
    {
        protected override int messageOffset => 2;
        public Error(string[] messageStrings) : base(messageStrings)
        {
            Instance.Insert(this);
        }
    }
}
