using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Event : IXmlSerializable, IHasTimeCode
    {
        public string Code { get; set; } = string.Empty;
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

        XmlSchema? IXmlSerializable.GetSchema() => null;

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
    }
}