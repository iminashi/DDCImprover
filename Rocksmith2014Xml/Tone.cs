using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public struct Tone : IXmlSerializable, IEquatable<Tone>, IHasTimeCode
    {
        public float Time { get; private set; }
        public byte Id { get; private set; }
        public string Name { get; private set; }

        public Tone(string name, float time, byte id)
        {
            Name = name;
            Time = time;
            Id = id;
        }

        public override string ToString()
            => $"{Time:F3}: {Name}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Time = float.Parse(reader.GetAttribute("time"), NumberFormatInfo.InvariantInfo);
            string? id = reader.GetAttribute("id");
            if(!string.IsNullOrEmpty(id))
                Id = byte.Parse(id, NumberFormatInfo.InvariantInfo);
            Name = reader.GetAttribute("name");

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Time.ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("id", Id.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("name", Name);
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is Tone other && Equals(other);

        public bool Equals(Tone other)
        {
            return Name == other.Name
                && Id == other.Id
                && Utils.TimeEqualToMilliseconds(Time, other.Time);
        }

        public override int GetHashCode()
            => (Name, Id, Time).GetHashCode();

        public static bool operator ==(Tone left, Tone right)
            => left.Equals(right);

        public static bool operator !=(Tone left, Tone right)
            => !(left == right);

        #endregion
    }
}