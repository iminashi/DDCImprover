using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class PhraseIteration : IXmlSerializable, IHasTimeCode
    {
        public uint Time { get; set; }
        public int PhraseId { get; set; }
        public string? Variation { get; set; }

        public HeroLevelCollection? HeroLevels { get; set; }

        public PhraseIteration() { }

        public PhraseIteration(uint time, int phraseId)
        {
            Time = time;
            PhraseId = phraseId;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: Phrase ID: {PhraseId}, Variation: \"{Variation}\"";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Time = Utils.TimeCodeFromFloatString(reader.GetAttribute("time"));
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
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("phraseId", PhraseId.ToString(NumberFormatInfo.InvariantInfo));
            if(!(InstrumentalArrangement.UseAbridgedXml && string.IsNullOrEmpty(Variation)))
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