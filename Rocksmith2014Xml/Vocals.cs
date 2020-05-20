using System.Text;
using System.Xml;
using System.Xml.Serialization;

using XmlUtils;

namespace Rocksmith2014Xml
{
    public sealed class Vocals : XmlCountListEx<Vocal>
    {
        public Vocals() : base("vocal") { }

        public void Save(string filename)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using XmlWriter writer = XmlWriter.Create(filename, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("vocals");
            ((IXmlSerializable)this).WriteXml(writer);
            writer.WriteEndElement();
        }

        public static Vocals Load(string filename)
        {
            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(filename, settings);

            reader.MoveToContent();
            Vocals vocals = new Vocals();
            ((IXmlSerializable)vocals).ReadXml(reader);
            return vocals;
        }
    }
}
