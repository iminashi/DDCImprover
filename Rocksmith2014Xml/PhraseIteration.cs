using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class PhraseIteration : IXmlSerializable, IHasTimeCode
    {
        public float Time { get; set; }
        public int PhraseId { get; set; }
        public string Variation { get; set; }

        public HeroLevelCollection HeroLevels { get; set; }

        public override string ToString()
            => $"{Time:F3}: Phrase ID: {PhraseId}, Variation: \"{Variation}\"";

        #region IXmlSerializable Implementation

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Time = float.Parse(reader.GetAttribute("time"), NumberFormatInfo.InvariantInfo);
            PhraseId = int.Parse(reader.GetAttribute("phraseId"), NumberFormatInfo.InvariantInfo);
            Variation = reader.GetAttribute("variation");

            if (!reader.IsEmptyElement && reader.ReadToDescendant("heroLevels"))
            {
                HeroLevels = new HeroLevelCollection();

                ((IXmlSerializable)HeroLevels).ReadXml(reader);

                reader.ReadEndElement(); // </phraseIteration>
            }
            else
            {
                reader.ReadStartElement();
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Time.ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("phraseId", PhraseId.ToString(NumberFormatInfo.InvariantInfo));
            if(!(RS2014Song.UseAbridgedXml && string.IsNullOrEmpty(Variation)))
                writer.WriteAttributeString("variation", Variation);

            if (HeroLevels?.Count > 0)
            {
                writer.WriteStartElement("heroLevels");
                ((IXmlSerializable)HeroLevels).WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}