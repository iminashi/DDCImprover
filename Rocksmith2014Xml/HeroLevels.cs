using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    /// <summary>
    /// Represents the hero levels of a phrase iteration.
    /// </summary>
    public sealed class HeroLevels : IXmlSerializable
    {
        /// <summary>
        /// The difficulty level for easy.
        /// </summary>
        public sbyte Easy { get; set; } = -1;

        /// <summary>
        /// The difficulty level for medium.
        /// </summary>
        public sbyte Medium { get; set; } = -1;

        /// <summary>
        /// The difficulty level for hard.
        /// </summary>
        public sbyte Hard { get; set; } = -1;

        /// <summary>
        /// Creates a new hero levels.
        /// </summary>
        public HeroLevels() { }

        /// <summary>
        /// Creates a new hero levels with the given difficulty levels.
        /// </summary>
        /// <param name="easy">The difficulty level for easy.</param>
        /// <param name="medium">The difficulty level for medium.</param>
        /// <param name="hard">The difficulty level for hard.</param>
        public HeroLevels(sbyte easy, sbyte medium, sbyte hard)
        {
            Easy = easy;
            Medium = medium;
            Hard = hard;
        }

        public override string ToString()
            => $"Easy: {Easy}, Medium: {Medium}, Hard: {Hard}";

        /// <summary>
        /// Gets the number of defined hero levels.
        /// </summary>
        public int Count =>
            (Easy == -1 ? 0 : 1) +
            (Medium == -1 ? 0 : 1) +
            (Hard == -1 ? 0 : 1);

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (!reader.IsEmptyElement && reader.ReadToDescendant("heroLevel"))
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    sbyte hero = sbyte.Parse(reader.GetAttribute("hero"), NumberFormatInfo.InvariantInfo);
                    sbyte difficulty = sbyte.Parse(reader.GetAttribute("difficulty"), NumberFormatInfo.InvariantInfo);

                    switch (hero)
                    {
                        case 1: Easy = difficulty; break;
                        case 2: Medium = difficulty; break;
                        case 3: Hard = difficulty; break;
                    }
                    reader.ReadStartElement();
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
            writer.WriteAttributeString("count", Count.ToString());

            if (Easy != -1)
            {
                writer.WriteStartElement("heroLevel");
                writer.WriteAttributeString("hero", "1");
                writer.WriteAttributeString("difficulty", Easy.ToString(NumberFormatInfo.InvariantInfo));
                writer.WriteEndElement();
            }

            if (Medium != -1)
            {
                writer.WriteStartElement("heroLevel");
                writer.WriteAttributeString("hero", "2");
                writer.WriteAttributeString("difficulty", Medium.ToString(NumberFormatInfo.InvariantInfo));
                writer.WriteEndElement();
            }

            if (Hard != -1)
            {
                writer.WriteStartElement("heroLevel");
                writer.WriteAttributeString("hero", "3");
                writer.WriteAttributeString("difficulty", Hard.ToString(NumberFormatInfo.InvariantInfo));
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}
