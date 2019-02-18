using System;

namespace DDCImprover.Core
{
    public sealed class ImproverMessage : IComparable<ImproverMessage>
    {
        public readonly string Message;
        public readonly MessageType Type;
        public readonly float TimeCode;

        public ImproverMessage(string message, MessageType messageType, float timeCode = 0f)
        {
            Message = message;
            Type = messageType;
            TimeCode = timeCode;
        }

        public int CompareTo(ImproverMessage other)
            => TimeCode.CompareTo(other.TimeCode);
    }

    public enum MessageType
    {
        Error,
        Issue,
        Warning
    }
}
