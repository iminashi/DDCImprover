using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class ShowLight : IHasTimeCode, IXmlSerializable
    {
        public float Time { get; set; }
        public byte Note { get; set; }

        public ShowLight() { }

        public ShowLight(float time, byte note)
        {
            Time = time;
            Note = note;
        }

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
                        Time = float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
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
            writer.WriteAttributeString("time", Time.ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("note", Note.ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion
    }
}
