using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    /// <summary>
    /// Represents a difficulty level with notes, chords, anchors and hand shapes.
    /// </summary>
    public sealed class Level : IXmlSerializable
    {
        /// <summary>
        /// Gets or sets the difficulty level.
        /// </summary>
        public sbyte Difficulty { get; set; } = -1;

        /// <summary>
        /// A list of notes in the level.
        /// </summary>
        public List<Note> Notes { get; set; } = new List<Note>();

        /// <summary>
        /// A list of chords in the level.
        /// </summary>
        public List<Chord> Chords { get; set; } = new List<Chord>();

        /// <summary>
        /// A list of anchors in the level.
        /// </summary>
        public List<Anchor> Anchors { get; set; } = new List<Anchor>();

        /// <summary>
        /// A list of hand shapes in the level.
        /// </summary>
        public List<HandShape> HandShapes { get; set; } = new List<HandShape>();

        /// <summary>
        /// Creates a new level.
        /// </summary>
        public Level() { }

        /// <summary>
        /// Creates a new level with the given difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty level.</param>
        public Level(sbyte difficulty)
        {
            Difficulty = difficulty;
        }

        public override string ToString()
            => $"Difficulty: {Difficulty}, Notes: {Notes.Count}, Chords: {Chords.Count}, Handshapes: {HandShapes.Count}, Anchors: {Anchors.Count}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Difficulty = sbyte.Parse(reader.GetAttribute("difficulty"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                switch (reader.LocalName)
                {
                    case "notes":
                        Utils.DeserializeCountList(Notes, reader);
                        break;

                    case "chords":
                        Utils.DeserializeCountList(Chords, reader);
                        break;

                    case "anchors":
                        Utils.DeserializeCountList(Anchors, reader);
                        break;

                    case "handShapes":
                        Utils.DeserializeCountList(HandShapes, reader);
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.ReadEndElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("difficulty", Difficulty.ToString(NumberFormatInfo.InvariantInfo));

            Utils.SerializeWithCount(Notes, "notes", "note", writer);
            Utils.SerializeWithCount(Chords, "chords", "chord", writer);
            Utils.SerializeWithCount(Anchors, "anchors", "anchor", writer);
            Utils.SerializeWithCount(HandShapes, "handShapes", "handShape", writer);
        }

        #endregion
    }
}
