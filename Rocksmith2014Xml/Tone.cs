using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Tone : IXmlSerializable, IHasTimeCode
    {
        public int Time { get; set; }
        public byte Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public Tone() { }

        public Tone(string name, int time, byte id)
        {
            Name = name;
            Time = time;
            Id = id;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: {Name}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Time = Utils.TimeCodeFromFloatString(reader.GetAttribute("time"));
            string? id = reader.GetAttribute("id");
            if(!string.IsNullOrEmpty(id))
                Id = byte.Parse(id, NumberFormatInfo.InvariantInfo);
            Name = reader.GetAttribute("name");

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("id", Id.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("name", Name);
        }

        #endregion
    }
}