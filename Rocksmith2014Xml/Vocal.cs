using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Vocal : IHasTimeCode, IXmlSerializable
    {
        public uint Time { get; set; }
        public byte Note { get; set; }
        public uint Length { get; set; }
        public string Lyric { get; set; } = string.Empty;

        public Vocal() { }

        public Vocal(uint time, uint length, string lyric, byte note = 60)
        {
            Time = time;
            Note = note;
            Length = length;
            Lyric = lyric;
        }

        public override string ToString()
            => $"Time: {Utils.TimeCodeToString(Time)}, Length: {Utils.TimeCodeToString(Length)}: {Lyric}";

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
                        Time = Utils.TimeCodeFromFloatString(reader.Value); //float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "note":
                        Note = byte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "length":
                        Length = Utils.TimeCodeFromFloatString(reader.Value); //float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
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
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("note", Note.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("length", Utils.TimeCodeToString(Length));
            writer.WriteAttributeString("lyric", Lyric);
        }

        #endregion
    }
}
