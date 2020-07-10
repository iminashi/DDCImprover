using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Section : IXmlSerializable, IHasTimeCode
    {
        public string Name { get; set; } = string.Empty;
        public short Number { get; set; }
        public int Time { get; set; }

        public Section(string name, int startTime, short number)
        {
            Name = name;
            Number = number;
            Time = startTime;
        }

        public Section() { }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: {Name} #{Number}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Name = reader.GetAttribute("name");
            Number = short.Parse(reader.GetAttribute("number"), NumberFormatInfo.InvariantInfo);
            Time = Utils.TimeCodeFromFloatString(reader.GetAttribute("startTime"));

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("number", Number.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("startTime", Utils.TimeCodeToString(Time));
        }

        #endregion
    }
}