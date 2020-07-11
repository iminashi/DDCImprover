using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Rocksmith2014Xml
{
    /// <summary>
    /// Represents a list of vocals that can be saved into an XML file.
    /// </summary>
    public sealed class Vocals : List<Vocal>
    {
        /// <summary>
        /// Saves this vocals list into an XML file.
        /// </summary>
        /// <param name="fileName">The target filename.</param>
        public void Save(string fileName)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using XmlWriter writer = XmlWriter.Create(fileName, settings);

            writer.WriteStartDocument();
            Utils.SerializeWithCount(this, "vocals", "vocal", writer);
        }

        /// <summary>
        /// Loads a list of vocals from an XML file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <returns>A list of vocals deserialized from the file.</returns>
        public static Vocals Load(string fileName)
        {
            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(fileName, settings);

            reader.MoveToContent();
            var vocals = new Vocals();
            Utils.DeserializeCountList(vocals, reader);
            return vocals;
        }
    }
}
