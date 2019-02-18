using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public readonly struct NLDPhrase
    {
        public readonly int Id;

        public NLDPhrase(int id)
        {
            Id = id;
        }
    }

    public sealed class NewLinkedDiff : IXmlSerializable
    {
        public int LevelBreak { get; set; } = -1;
        public string Ratio { get; set; }

        public int PhraseCount => Phrases?.Count ?? 0;
        public List<NLDPhrase> Phrases { get; set; }

        #region IXmlSerializable Implementation

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            LevelBreak = int.Parse(reader.GetAttribute("levelBreak"), NumberFormatInfo.InvariantInfo);
            Ratio = reader.GetAttribute("ratio");

            if(!reader.IsEmptyElement && reader.ReadToDescendant("nld_phrase"))
            {
                Phrases = new List<NLDPhrase>();

                while(reader.NodeType != XmlNodeType.EndElement)
                {
                    Phrases.Add(new NLDPhrase(int.Parse(reader.GetAttribute("id"), NumberFormatInfo.InvariantInfo)));
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
            if (Ratio == null)
                writer.WriteAttributeString("ratio", "1.000");
            else
                writer.WriteAttributeString("ratio", Ratio);
            writer.WriteAttributeString("phraseCount", PhraseCount.ToString(NumberFormatInfo.InvariantInfo));

            if (PhraseCount > 0)
            {
                foreach (var nld in Phrases)
                {
                    writer.WriteStartElement("nld_phrase");
                    writer.WriteAttributeString("id", nld.Id.ToString(NumberFormatInfo.InvariantInfo));
                    writer.WriteEndElement();
                }
            }
        }

        #endregion
    }
}