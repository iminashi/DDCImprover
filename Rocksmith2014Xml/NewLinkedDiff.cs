using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class NewLinkedDiff : IXmlSerializable
    {
        public sbyte LevelBreak { get; set; } = -1;
        public string? Ratio { get; set; }

        public int PhraseCount => PhraseIds.Count;
        public List<int> PhraseIds { get; set; } = new List<int>();

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            LevelBreak = sbyte.Parse(reader.GetAttribute("levelBreak"), NumberFormatInfo.InvariantInfo);
            Ratio = reader.GetAttribute("ratio");

            if(!reader.IsEmptyElement && reader.ReadToDescendant("nld_phrase"))
            {
                while(reader.NodeType != XmlNodeType.EndElement)
                {
                    PhraseIds.Add(int.Parse(reader.GetAttribute("id"), NumberFormatInfo.InvariantInfo));
                    reader.Read();
                }

                reader.ReadEndElement();
            }
            else
            {
                reader.ReadStartElement();
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("levelBreak", LevelBreak.ToString(NumberFormatInfo.InvariantInfo));
            if (Ratio is null)
                writer.WriteAttributeString("ratio", "1.000");
            else
                writer.WriteAttributeString("ratio", Ratio);
            writer.WriteAttributeString("phraseCount", PhraseCount.ToString(NumberFormatInfo.InvariantInfo));

            if (PhraseCount > 0)
            {
                foreach (var id in PhraseIds)
                {
                    writer.WriteStartElement("nld_phrase");
                    writer.WriteAttributeString("id", id.ToString(NumberFormatInfo.InvariantInfo));
                    writer.WriteEndElement();
                }
            }
        }

        #endregion
    }
}