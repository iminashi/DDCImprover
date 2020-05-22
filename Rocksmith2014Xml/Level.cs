using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Level : IXmlSerializable
    {
        public sbyte Difficulty { get; set; } = -1;
        public NoteCollection Notes { get; set; } = new NoteCollection();
        public ChordCollection Chords { get; set; } = new ChordCollection();
        public AnchorCollection Anchors { get; set; } = new AnchorCollection();
        public HandShapeCollection HandShapes { get; set; } = new HandShapeCollection();

        public Level() { }

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

            while(reader.NodeType != XmlNodeType.EndElement)
            {
                switch(reader.LocalName)
                {
                    case "notes":
                        ((IXmlSerializable)Notes).ReadXml(reader);
                        break;

                    case "chords":
                        ((IXmlSerializable)Chords).ReadXml(reader);
                        break;

                    case "anchors":
                        ((IXmlSerializable)Anchors).ReadXml(reader);
                        break;

                    case "handShapes":
                        ((IXmlSerializable)HandShapes).ReadXml(reader);
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

            if (Notes != null)
            {
                writer.WriteStartElement("notes");
                ((IXmlSerializable)Notes).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (Chords != null)
            {
                writer.WriteStartElement("chords");
                ((IXmlSerializable)Chords).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (Anchors != null)
            {
                writer.WriteStartElement("anchors");
                ((IXmlSerializable)Anchors).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (HandShapes != null)
            {
                writer.WriteStartElement("handShapes");
                ((IXmlSerializable)HandShapes).WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}