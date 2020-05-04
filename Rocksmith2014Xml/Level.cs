using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Level : IXmlSerializable
    {
        public sbyte Difficulty { get; set; }
        public NoteCollection Notes { get; set; }
        public ChordCollection Chords { get; set; }
        public AnchorCollection Anchors { get; set; }
        public HandShapeCollection HandShapes { get; set; }

        public Level()
        {
            Difficulty = -1;
            Notes = new NoteCollection();
            Chords = new ChordCollection();
            Anchors = new AnchorCollection();
            HandShapes = new HandShapeCollection();
        }

        public override string ToString()
            => $"Difficulty: {Difficulty}, Notes: {Notes.Count}, Chords: {Chords.Count}, Handshapes: {HandShapes.Count}, Anchors: {Anchors.Count}";

        #region IXmlSerializable Implementation

        XmlSchema IXmlSerializable.GetSchema() => null;

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