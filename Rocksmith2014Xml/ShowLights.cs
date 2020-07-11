using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Rocksmith2014Xml
{
    /// <summary>
    /// Represents a list of show lights that can be saved into an XML file.
    /// </summary>
    public sealed class ShowLights : List<ShowLight>
    {
        /// <summary>
        /// Saves this show light list into an XML file.
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
            Utils.SerializeWithCount(this, "showlights", "showlight", writer);
        }

        /// <summary>
        /// Loads a list of show lights from an XML file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <returns>A list of show lights deserialized from the file.</returns>
        public static ShowLights Load(string fileName)
        {
            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(fileName, settings);

            reader.MoveToContent();
            var showLights = new ShowLights();
            Utils.DeserializeCountList(showLights, reader);
            return showLights;
        }
    }
}
