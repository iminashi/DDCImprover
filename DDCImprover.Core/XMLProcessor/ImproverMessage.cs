using System;

namespace DDCImprover.Core
{
    public sealed class ImproverMessage : IComparable<ImproverMessage>, IEquatable<ImproverMessage>
    {
        public string Message { get; }
        public MessageType Type { get; }
        public float TimeCode { get; }

        public ImproverMessage(string message, MessageType messageType, float timeCode = 0f)
        {
            Message = message;
            Type = messageType;
            TimeCode = timeCode;
        }

        public int CompareTo(ImproverMessage other)
            => TimeCode.CompareTo(other.TimeCode);

        public bool Equals(ImproverMessage other)
            => Message.Equals(other.Message) && TimeCode.Equals(other.TimeCode);

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return Equals(obj as ImproverMessage);
        }

        public override int GetHashCode()
            => Message.GetHashCode();
    }

    public enum MessageType
    {
        Error,
        Issue,
        Warning
    }
}
