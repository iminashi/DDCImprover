using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class PhraseProperty : IXmlSerializable
    {
        public int PhraseId { get; set; }
        public int Redundant { get; set; }
        public int LevelJump { get; set; }
        public int Empty { get; set; }
        public int Difficulty { get; set; }

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            PhraseId = int.Parse(reader.GetAttribute("phraseId"), NumberFormatInfo.InvariantInfo);
            Redundant = int.Parse(reader.GetAttribute("redundant"), NumberFormatInfo.InvariantInfo);
            LevelJump = int.Parse(reader.GetAttribute("levelJump"), NumberFormatInfo.InvariantInfo);
            Empty = int.Parse(reader.GetAttribute("empty"), NumberFormatInfo.InvariantInfo);
            Difficulty = int.Parse(reader.GetAttribute("difficulty"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("phraseId", PhraseId.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("redundant", Redundant.ToString(NumberFormatInfo.InvariantInfo));

            writer.WriteAttributeString("levelJump", LevelJump.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("empty", Empty.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("difficulty", Difficulty.ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion
    }
}