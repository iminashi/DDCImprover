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
    [XmlRoot("song")]
    public class RS2014Song : IXmlSerializable
    {
        internal static bool UseAbridgedXml;

        public readonly List<RSXmlComment> XmlComments = new List<RSXmlComment>();

        private byte Version { get; set; } = 8;
        public string Arrangement { get; set; }
        //public string WaveFilePath { get; set; }
        public int Part { get; set; }
        //public float Offset { get; set; } // Handled automatically
        public int CentOffset { get; set; }
        public float SongLength { get; set; }
        //public string InternalName { get; set; }
        public float AverageTempo { get; set; } = 120.000f;
        public Tuning Tuning { get; set; } = new Tuning();
        public int Capo { get; set; }
        public string Title { get; set; }
        public string TitleSort { get; set; }
        public string ArtistName { get; set; }
        public string ArtistNameSort { get; set; }
        public string AlbumName { get; set; }
        public string AlbumNameSort { get; set; }
        public int AlbumYear { get; set; }
        public string AlbumArt { get; set; }
        //public int CrowdSpeed { get; set; } = 1; // Pointless

        public float StartBeat => Ebeats?[0].Time ?? 0.0f;

        public ArrangementProperties ArrangementProperties { get; set; }

        public string LastConversionDateTime { get; set; }

        public PhraseCollection Phrases { get; set; }
        public PhraseIterationCollection PhraseIterations { get; set; }
        public NewLinkedDiffCollection NewLinkedDiffs { get; set; }
        public XmlCountList<LinkedDiff> LinkedDiffs { get; set; }
        public XmlCountList<PhraseProperty> PhraseProperties { get; set; }
        public ChordTemplateCollection ChordTemplates { get; set; }
        //fretHandMuteTemplates
        public EbeatCollection Ebeats { get; set; }

        public string ToneBase { get; set; }
        public string ToneA { get; set; }
        public string ToneB { get; set; }
        public string ToneC { get; set; }
        public string ToneD { get; set; }

        public ToneCollection Tones { get; set; }
        public SectionCollection Sections { get; set; }
        public EventCollection Events { get; set; }
        public Level TranscriptionTrack { get; set; }
        public LevelCollection Levels { get; set; }

        public void Save(string filename, bool writeAbridgedXml = true)
        {
            UseAbridgedXml = writeAbridgedXml;

            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using XmlWriter writer = XmlWriter.Create(filename, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("song");
            ((IXmlSerializable)this).WriteXml(writer);
            writer.WriteEndElement();
        }

        public static RS2014Song Load(string filename)
        {
            var settings = new XmlReaderSettings
            {
                IgnoreComments = false,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(filename, settings);

            reader.MoveToContent();
            RS2014Song song = new RS2014Song();
            ((IXmlSerializable)song).ReadXml(reader);
            return song;
        }

        public static Task<RS2014Song> LoadAsync(string filename)
        {
            return Task.Run(() => Load(filename));
        }

        #region IXmlSerializable Implementation

        XmlSchema IXmlSerializable.GetSchema() => null;

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
                    //case "wavefilepath":
                    //  WaveFilePath = reader.Value;
                    //    break;
                    case "part":
                        Part = int.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "centOffset":
                        CentOffset = int.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "songLength":
                        SongLength = float.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "songNameSort":
                        TitleSort = reader.ReadElementContentAsString();
                        break;
                    case "averageTempo":
                        AverageTempo = float.Parse(reader.ReadElementContentAsString(), NumberFormatInfo.InvariantInfo);
                        break;
                    case "tuning":
                        Tuning = new Tuning();
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
                        Phrases = new PhraseCollection();
                        ((IXmlSerializable)Phrases).ReadXml(reader);
                        break;
                    case "phraseIterations":
                        PhraseIterations = new PhraseIterationCollection();
                        ((IXmlSerializable)PhraseIterations).ReadXml(reader);
                        break;
                    case "newLinkedDiffs":
                        NewLinkedDiffs = new NewLinkedDiffCollection();
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
                        ChordTemplates = new ChordTemplateCollection();
                        ((IXmlSerializable)ChordTemplates).ReadXml(reader);
                        break;
                    case "ebeats":
                        Ebeats = new EbeatCollection();
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
                        Tones = new ToneCollection();
                        ((IXmlSerializable)Tones).ReadXml(reader);
                        break;

                    case "sections":
                        Sections = new SectionCollection();
                        ((IXmlSerializable)Sections).ReadXml(reader);
                        break;
                    case "events":
                        Events = new EventCollection();
                        ((IXmlSerializable)Events).ReadXml(reader);
                        break;
                    case "transcriptionTrack":
                        TranscriptionTrack = new Level();
                        ((IXmlSerializable)TranscriptionTrack).ReadXml(reader);
                        break;
                    case "levels":
                        Levels = new LevelCollection();
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

            float firstBeat = 0.0f;
            if (Ebeats?.Count > 0)
                firstBeat = Ebeats[0].Time;

            writer.WriteElementString("title", Title);
            writer.WriteElementString("arrangement", Arrangement);
            //writer.WriteElementString("wavefilepath", WaveFilePath);
            writer.WriteElementString("part", Part.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("offset", (-firstBeat).ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("centOffset", CentOffset.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteElementString("songLength", SongLength.ToString("F3", NumberFormatInfo.InvariantInfo));
            //writer.WriteElementString("internalName", InternalName);

            if (TitleSort != null)
            {
                writer.WriteElementString("songNameSort", TitleSort);
            }

            writer.WriteElementString("startBeat", firstBeat.ToString("F3", NumberFormatInfo.InvariantInfo));
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

            if (ArrangementProperties != null)
            {
                writer.WriteStartElement("arrangementProperties");
                ((IXmlSerializable)ArrangementProperties).WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteElementString("lastConversionDateTime", LastConversionDateTime);

            if (Phrases != null)
            {
                writer.WriteStartElement("phrases");
                ((IXmlSerializable)Phrases).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (PhraseIterations != null)
            {
                writer.WriteStartElement("phraseIterations");
                ((IXmlSerializable)PhraseIterations).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (NewLinkedDiffs != null)
            {
                writer.WriteStartElement("newLinkedDiffs");
                ((IXmlSerializable)NewLinkedDiffs).WriteXml(writer);
                writer.WriteEndElement();
            }

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

            if (ChordTemplates != null)
            {
                writer.WriteStartElement("chordTemplates");
                ((IXmlSerializable)ChordTemplates).WriteXml(writer);
                writer.WriteEndElement();
            }

            //fretHandMuteTemplates

            if (Ebeats != null)
            {
                writer.WriteStartElement("ebeats");
                ((IXmlSerializable)Ebeats).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (ToneBase != null) writer.WriteElementString("tonebase", ToneBase);
            if (ToneA != null) writer.WriteElementString("tonea", ToneA);
            if (ToneB != null) writer.WriteElementString("toneb", ToneB);
            if (ToneC != null) writer.WriteElementString("tonec", ToneC);
            if (ToneD != null) writer.WriteElementString("toned", ToneD);

            if (Tones != null)
            {
                writer.WriteStartElement("tones");
                ((IXmlSerializable)Tones).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (Sections != null)
            {
                writer.WriteStartElement("sections");
                ((IXmlSerializable)Sections).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (Events != null)
            {
                writer.WriteStartElement("events");
                ((IXmlSerializable)Events).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (TranscriptionTrack != null)
            {
                writer.WriteStartElement("transcriptionTrack");
                ((IXmlSerializable)TranscriptionTrack).WriteXml(writer);
                writer.WriteEndElement();
            }

            if (Levels != null)
            {
                writer.WriteStartElement("levels");
                ((IXmlSerializable)Levels).WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}
