using System;

namespace LogAnalyzer.Messages.DevCons
{
    class DevConDebug : DevCon
    {
        public DevConDebug(string[] messageStrings) : base(messageStrings) { }

        public override bool IsSameMessage(object message)
        {
            try
            {
                var m = (DevConDebug)message;

                return this.messageRawBody.Length >= 3 && m.messageRawBody.Length >= 3
                    ? this.messageRawBody[2] == m.messageRawBody[2]
                    : base.IsSameMessage(message);
            }
            catch (InvalidCastException) { return false; };
        }
    }
}
