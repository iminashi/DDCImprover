using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Tone : IXmlSerializable, IHasTimeCode
    {
        public float Time { get; set; }
        public byte Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public Tone() { }

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
    }
}