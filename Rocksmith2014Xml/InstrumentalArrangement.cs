using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using XmlUtils;

namespace Rocksmith2014Xml
{
    /// <summary>
    /// Represents a Rocksmith 2014 instrumental arrangement.
    /// </summary>
    [XmlRoot("song")]
    public class InstrumentalArrangement : IXmlSerializable
    {
        internal static bool UseAbridgedXml { get; set; }

        /// <summary>
        /// A list of comments found after the root node of the XML file.
        /// </summary>
        public List<RSXmlComment> XmlComments { get; } = new List<RSXmlComment>();

        /// <summary>
        /// The version of the file. Default: 8
        /// </summary>
        public byte Version { get; set; } = 8;

        /// <summary>
        /// The name of the arrangement: Lead, Rhythm, Combo or Bass.
        /// </summary>
        public string? Arrangement { get; set; }

        public int Part { get; set; }

        public int CentOffset { get; set; }

        /// <summary>
        /// The length of the arrangement in milliseconds.
        /// </summary>
        public int SongLength { get; set; }

        /// <summary>
        /// The average tempo of the arrangement in beats per minute.
        /// </summary>
        public float AverageTempo { get; set; } = 120.000f;

        public Tuning Tuning { get; set; } = new Tuning();
        public int Capo { get; set; }
        public string? Title { get; set; }
        public string? TitleSort { get; set; }
        public string? ArtistName { get; set; }
        public string? ArtistNameSort { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumNameSort { get; set; }
        public int AlbumYear { get; set; }
        public string? AlbumArt { get; set; }

        // Other metadata:
        //
        // Offset - Handled automatically.
        // WaveFilePath - Used only in official files.
        // InternalName - Used only in official files.
        // CrowdSpeed - Completely purposeless since it does not have an equivalent in the SNG files or manifest files.
        //              The crowd speed is controlled with events e0, e1 and e2.

        public int StartBeat => Ebeats.Count > 0 ? Ebeats[0].Time : 0;

        /// <summary>
        /// Contains various metadata about the arrangement.
        /// </summary>
        public ArrangementProperties ArrangementProperties { get; set; } = new ArrangementProperties();

        public string? LastConversionDateTime { get; set; }

        public PhraseCollection Phrases { get; set; } = new PhraseCollection();
        public PhraseIterationCollection PhraseIterations { get; set; } = new PhraseIterationCollection();
        public NewLinkedDiffCollection NewLinkedDiffs { get; set; } = new NewLinkedDiffCollection();
        public XmlCountList<LinkedDiff>? LinkedDiffs { get; set; }
        public XmlCountList<PhraseProperty>? PhraseProperties { get; set; }
        public ChordTemplateCollection ChordTemplates { get; set; } = new ChordTemplateCollection();
        public EbeatCollection Ebeats { get; set; } = new EbeatCollection();

        /// <summary>
        /// The name of the tone that the arrangement starts with.
        /// </summary>
        public string? ToneBase { get; set; }
        public string? ToneA { get; set; }
        public string? ToneB { get; set; }
        public string? ToneC { get; set; }
        public string? ToneD { get; set; }

        public ToneCollection? ToneChanges { get; set; }

        public SectionCollection Sections { get; set; } = new SectionCollection();
        public EventCollection Events { get; set; } = new EventCollection();

        /// <summary>
        /// Contains the transcription of the arrangement (i.e. only the highest difficulty level of all the phrases).
        /// </summary>
        public Level? TranscriptionTrack { get; set; }

        /// <summary>
        /// The difficulty levels of the arrangement.
        /// </summary>
        public LevelCollection Levels { get; set; } = new LevelCollection();

        /// <summary>
        /// Saves this Rocksmith 2014 arrangement into the given file.
        /// </summary>
        /// <param name="fileName">The target file name.</param>
        /// <param name="writeAbridgedXml">Controls whether to write an abridged XML file or not. Default: true.</param>
        public void Save(string fileName, bool writeAbridgedXml = true)
        {
            UseAbridgedXml = writeAbridgedXml;

            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using XmlWriter writer = XmlWriter.Create(fileName, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("song");
            ((IXmlSerializable)this).WriteXml(writer);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Loads a Rocksmith 2014 arrangement from the given file name.
        /// </summary>
        /// <param name="fileName">The file name of a Rocksmith 2014 instrumental arrangement.</param>
        /// <returns>A Rocksmith 2014 arrangement parsed from the XML file.</returns>
        public static InstrumentalArrangement Load(string fileName)
        {
            var settings = new XmlReaderSettings
            {
                IgnoreComments = false,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(fileName, settings);

            reader.MoveToContent();
            var arr = new InstrumentalArrangement();
            ((IXmlSerializable)arr).ReadXml(reader);
            return arr;
        }

        /// <summary>
        /// Loads a Rocksmith 2014 arrangement from the given file name on a background thread.
        /// </summary>
        /// <param name="fileName">The file name of a Rocksmith 2014 instrumental arrangement.</param>
        /// <returns>A Rocksmith 2014 arrangement parsed from the XML file.</returns>
        public static Task<InstrumentalArrangement> LoadAsync(string fileName)
            => Task.Run(() => Load(fileName));

        /// <summary>
        /// Reads the tone names from the given file using an XmlReader.
        /// </summary>
        /// <param name="fileName">The file name of a Rocksmith 2014 instrumental arrangement.</param>
        /// <returns>An array of five tone names, the first being the base tone. Null represents the absence of a tone name.</returns>
        public static string?[] ReadToneNames(string fileName)
        {
            using XmlReader reader = XmlReader.Create(fileName);

            reader.MoveToContent();

            if (reader.LocalName != "song")
                throw new InvalidOperationException("Expected root node of the XML file to be \"song\", instead found: " + reader.LocalName);

            var toneNames = new string?[5];

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "tonebase":
                            toneNames[0] = reader.ReadElementContentAsString();
                            break;
                        case "tonea":
                            toneNames[1] = reader.ReadElementContentAsString();
                            break;
                        case "toneb":
                            toneNames[2] = reader.ReadElementContentAsString();
                            break;
                        case "tonec":
                            toneNames[3] = reader.ReadElementContentAsString();
                            break;
                        case "toned":
                            toneNames[4] = reader.ReadElementContentAsString();
                            break;
                        // The tone names should come before the sections.
                        case "sections":
                        case "levels":
                            return toneNames;
                    }
                }
            }

            return toneNames;
        }

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Version = byte.Parse(reader.GetAttribute("version"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();

            // Only preserve comments directly after the root element
            while (reader.NodeType == XmlNodeType.Comment)
            {
                XmlComments.Add(new RSXmlComment(reader.Value));
                reader.Read();
            }

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                switch (reader.LocalName)
                {
                    case "title":
                        Title = reader.ReadElementContentAsString();
                        break;
                    case "arrangement":
                        Arrangement = reader.ReadElementContentAsString();
                        break;
                    case "part":
                        Part = int.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "centOffset":
                        CentOffset = int.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "songLength":
                        SongLength = Utils.TimeCodeFromFloatString(reader.ReadElementContentAsString());
                        break;
                    case "songNameSort":
                        TitleSort = reader.ReadElementContentAsString();
                        break;
                    case "averageTempo":
                        AverageTempo = float.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "tuning":
                        ((IXmlSerializable)Tuning).ReadXml(reader);
                        break;
                    case "capo":
                        Capo = int.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;

                    case "artistName":
                        ArtistName = reader.ReadElementContentAsString();
                        break;
                    case "artistNameSort":
                        ArtistNameSort = reader.ReadElementContentAsString();
                        break;
                    case "albumName":
                        AlbumName = reader.ReadElementContentAsString();
                        break;
                    case "albumNameSort":
                        AlbumNameSort = reader.ReadElementContentAsString();
                        break;
                    case "albumYear":
                        string content = reader.ReadElementContentAsString();
                        if(!string.IsNullOrEmpty(content))
                            AlbumYear = int.Parse(content, NumberFormatInfo.InvariantInfo);
                        break;
                    case "albumArt":
                        AlbumArt = reader.ReadElementContentAsString();
                        break;

                    case "arrangementProperties":
                        ArrangementProperties = new ArrangementProperties();
                        ((IXmlSerializable)ArrangementProperties).ReadXml(reader);
                        break;

                    case "lastConversionDateTime":
                        LastConversionDateTime = reader.ReadElementContentAsString();
                        break;

                    case "phrases":
                        ((IXmlSerializable)Phrases).ReadXml(reader);
                        break;
                    case "phraseIterations":
                        ((IXmlSerializable)PhraseIterations).ReadXml(reader);
                        break;
                    case "newLinkedDiffs":
                        ((IXmlSerializable)NewLinkedDiffs).ReadXml(reader);
                        break;
                    case "linkedDiffs":
                        LinkedDiffs = new XmlCountList<LinkedDiff>();
                        ((IXmlSerializable)LinkedDiffs).ReadXml(reader);
                        break;
                    case "phraseProperties":
                        PhraseProperties = new XmlCountList<PhraseProperty>();
                        ((IXmlSerializable)PhraseProperties).ReadXml(reader);
                        break;
                    case "chordTemplates":
                        ((IXmlSerializable)ChordTemplates).ReadXml(reader);
                        break;
                    case "ebeats":
                        ((IXmlSerializable)Ebeats).ReadXml(reader);
                        break;

                    case "tonebase":
                        ToneBase = reader.ReadElementContentAsString();
                        break;
                    case "tonea":
                        ToneA = reader.ReadElementContentAsString();
                        break;
                    case "toneb":
                        ToneB = reader.ReadElementContentAsString();
                        break;
                    case "tonec":
                        ToneC = reader.ReadElementContentAsString();
                        break;
                    case "toned":
                        ToneD = reader.ReadElementContentAsString();
                        break;
                    case "tones":
                        ToneChanges = new ToneCollection();
                        ((IXmlSerializable)ToneChanges).ReadXml(reader);
                        break;

                    case "sections":
                        ((IXmlSerializable)Sections).ReadXml(reader);
                        break;
                    case "events":
                        ((IXmlSerializable)Events).ReadXml(reader);
                        break;
                    case "transcriptionTrack":
                        TranscriptionTrack = new Level();
                        ((IXmlSerializable)TranscriptionTrack).ReadXml(reader);
                        break;
                    case "levels":
                        ((IXmlSerializable)Levels).ReadXml(reader);
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
            writer.WriteAttributeString("version", Version.ToString(NumberFormatInfo.InvariantInfo));

            if (XmlComments.Count > 0)
            {
                foreach (var comment in XmlComments)
                {
                    writer.WriteComment(comment.Value);
                }
            }

            int firstBeat = 0;
            if (Ebeats.Count > 0)
                firstBeat = Ebeats[0].Time;

            writer.WriteElementString("title", Title);
            writer.WriteElementString("arrangement", Arrangement);
            writer.WriteElementString("part", Part.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("offset", (firstBeat / -1000f).ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("centOffset", CentOffset.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("songLength", Utils.TimeCodeToString(SongLength));

            if (TitleSort != null)
            {
                writer.WriteElementString("songNameSort", TitleSort);
            }

            writer.WriteElementString("startBeat", Utils.TimeCodeToString(firstBeat));
            writer.WriteElementString("averageTempo", AverageTempo.ToString("F3", NumberFormatInfo.InvariantInfo));

            if (Tuning != null)
            {
                writer.WriteStartElement("tuning");
                ((IXmlSerializable)Tuning).WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteElementString("capo", Capo.ToString(NumberFormatInfo.InvariantInfo));

            writer.WriteElementString("artistName", ArtistName);
            if (ArtistNameSort != null)
            {
                writer.WriteElementString("artistNameSort", ArtistNameSort);
            }

            writer.WriteElementString("albumName", AlbumName);
            if (AlbumNameSort != null)
            {
                writer.WriteElementString("albumNameSort", AlbumNameSort);
            }

            writer.WriteElementString("albumYear", AlbumYear.ToString(NumberFormatInfo.InvariantInfo));

            if (AlbumArt != null)
            {
                writer.WriteElementString("albumArt", AlbumArt);
            }

            writer.WriteElementString("crowdSpeed", "1");

            writer.WriteStartElement("arrangementProperties");
            ((IXmlSerializable)ArrangementProperties).WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteElementString("lastConversionDateTime", LastConversionDateTime);

            writer.WriteStartElement("phrases");
            ((IXmlSerializable)Phrases).WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("phraseIterations");
            ((IXmlSerializable)PhraseIterations).WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("newLinkedDiffs");
            ((IXmlSerializable)NewLinkedDiffs).WriteXml(writer);
            writer.WriteEndElement();

            if (LinkedDiffs != null)
            {
                writer.WriteStartElement("linkedDiffs");
                ((IXmlSerializable)LinkedDiffs).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (PhraseProperties != null)
            {
                writer.WriteStartElement("phraseProperties");
                ((IXmlSerializable)PhraseProperties).WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("chordTemplates");
            ((IXmlSerializable)ChordTemplates).WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("ebeats");
            ((IXmlSerializable)Ebeats).WriteXml(writer);
            writer.WriteEndElement();

            if (ToneBase != null) writer.WriteElementString("tonebase", ToneBase);
            if (ToneA != null) writer.WriteElementString("tonea", ToneA);
            if (ToneB != null) writer.WriteElementString("toneb", ToneB);
            if (ToneC != null) writer.WriteElementString("tonec", ToneC);
            if (ToneD != null) writer.WriteElementString("toned", ToneD);

            if (ToneChanges != null)
            {
                writer.WriteStartElement("tones");
                ((IXmlSerializable)ToneChanges).WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("sections");
            ((IXmlSerializable)Sections).WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("events");
            ((IXmlSerializable)Events).WriteXml(writer);
            writer.WriteEndElement();

            if (TranscriptionTrack != null)
            {
                writer.WriteStartElement("transcriptionTrack");
                ((IXmlSerializable)TranscriptionTrack).WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("levels");
            ((IXmlSerializable)Levels).WriteXml(writer);
            writer.WriteEndElement();
        }

        #endregion
    }
}
