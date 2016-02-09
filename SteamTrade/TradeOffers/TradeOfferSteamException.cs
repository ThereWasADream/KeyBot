using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace SteamTrade
{
    [Serializable]
    public class TradeOfferSteamException : Exception
    {
        private int _errorCode = 0;

        public int ErrorCode
        {
            get
            {
                return _errorCode;
            }
            private set { _errorCode = value; }
        }

        public TradeOfferSteamException()
        {
        }

        public TradeOfferSteamException(string message)
            : base(message)
        {
            var errorCodeStr = message.Substring(Math.Max(0, message.Length - 5));
            var matches = Regex.Match(errorCodeStr, @"\(([^)]+)\)");
            if (matches.Groups.Count > 1)
            {
                int.TryParse(matches.Groups[1].Value, out _errorCode);
            }
        }

        public TradeOfferSteamException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected TradeOfferSteamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {            
        }

        public TradeOfferSteamException(string message, int errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ErrorCode", _errorCode);
            base.GetObjectData(info, context);
        }
    }
}
