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
        public int Time { get; set; }

        public Event(string code, int time)
        {
            Code = code;
            Time = time;
        }

        public Event() { }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: {Code}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Time = Utils.TimeCodeFromFloatString(reader.GetAttribute("time"));
            Code = reader.GetAttribute("code");

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("code", Code);
        }

        #endregion
    }
}