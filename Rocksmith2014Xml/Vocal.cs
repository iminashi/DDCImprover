using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Vocal : IHasTimeCode, IXmlSerializable
    {
        public float Time { get; set; }
        public byte Note { get; set; }
        public float Length { get; set; }
        public string Lyric { get; set; } = string.Empty;

        public Vocal() { }

        public Vocal(float time, float length, string lyric, byte note = 60)
        {
            Time = time;
            Note = note;
            Length = length;
            Lyric = lyric;
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
                    case "length":
                        Length = float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "lyric":
                        Lyric = reader.Value;
                        break;
                }
            }

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Time.ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("note", Note.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("length", Length.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("lyric", Lyric);
        }

        #endregion
    }
}
