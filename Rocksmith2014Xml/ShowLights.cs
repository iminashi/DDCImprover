using System.Text;
using System.Xml;
using System.Xml.Serialization;

using XmlUtils;

namespace Rocksmith2014Xml
{
    public sealed class ShowLights : XmlCountListEx<ShowLight>
    {
        public ShowLights() : base("showlight")
        {
        }

        public void Save(string filename)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using XmlWriter writer = XmlWriter.Create(filename, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("showlights");
            ((IXmlSerializable)this).WriteXml(writer);
            writer.WriteEndElement();
        }

        public static ShowLights Load(string filename)
        {
            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(filename, settings);

            reader.MoveToContent();
            ShowLights showLights = new ShowLights();
            ((IXmlSerializable)showLights).ReadXml(reader);
            return showLights;
        }
    }
}
