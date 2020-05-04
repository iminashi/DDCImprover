using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Event : IXmlSerializable, IHasTimeCode, IEquatable<Event>
    {
        public string Code { get; set; }
        public float Time { get; set; }

        public Event(string code, float time)
        {
            Code = code;
            Time = time;
        }

        public Event() { }

        public override string ToString()
            => $"{Time:F3}: {Code}";

        #region IXmlSerializable Implementation

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Time = float.Parse(reader.GetAttribute("time"), NumberFormatInfo.InvariantInfo);
            Code = reader.GetAttribute("code");

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Time.ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("code", Code);
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is Event other && Equals(other);

        public bool Equals(Event other)
        {
            if (ReferenceEquals(this, other))
                return true;

            return !(other is null)
                && Utils.TimeEqualToMilliseconds(Time, other.Time)
                && Code == other.Code;
        }

        public override int GetHashCode()
            => (Time, Code).GetHashCode();

        public static bool operator ==(Event left, Event right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(Event left, Event right)
            => !(left == right);

        #endregion
    }
}