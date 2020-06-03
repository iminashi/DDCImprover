using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class ShowLight : IHasTimeCode, IXmlSerializable
    {
        public uint Time { get; set; }
        public byte Note { get; set; }

        public ShowLight() { }

        public ShowLight(uint time, byte note)
        {
            Time = time;
            Note = note;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: {Note}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                switch (reader.Name)
                {
                    case "time":
                        Time = Utils.TimeCodeFromFloatString(reader.Value);
                        break;
                    case "note":
                        Note = byte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                }
            }

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("note", Note.ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion
    }
}
