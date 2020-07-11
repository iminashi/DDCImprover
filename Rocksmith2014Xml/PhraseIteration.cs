﻿using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    /// <summary>
    /// Represents an iteration of a phrase.
    /// </summary>
    public sealed class PhraseIteration : IXmlSerializable, IHasTimeCode
    {
        /// <summary>
        /// The time code of the phrase iteration.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// The phrase ID.
        /// </summary>
        public int PhraseId { get; set; }

        /// <summary>
        /// A variation string. Unused.
        /// </summary>
        public string? Variation { get; set; }

        /// <summary>
        /// The hero levels of the phrase iteration.
        /// </summary>
        public HeroLevels? HeroLevels { get; set; }

        /// <summary>
        /// Creates a new phrase iteration.
        /// </summary>
        public PhraseIteration() { }

        /// <summary>
        /// Creates a new phrase iteration with the given properties.
        /// </summary>
        /// <param name="time">The time code of the phrase.</param>
        /// <param name="phraseId">The phrase ID.</param>
        public PhraseIteration(int time, int phraseId)
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
                HeroLevels = new HeroLevels();

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
            if (!(InstrumentalArrangement.UseAbridgedXml && string.IsNullOrEmpty(Variation)))
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