using System;
using System.IO;
using System.Xml.Serialization;
using XmlUtils;

namespace DDCImprover.Core
{
    [XmlRoot(Namespace = "")]
    public class Configuration
    {
        public static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        public static string ConfigFileName = "DDCImprover.config.xml";

        public string DDCExecutablePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ddc", "ddc.exe");
        public int DDCPhraseLength { get; set; } = 256;
        public string DDCRampupFile { get; set; } = "ddc_default";
        public string DDCConfigFile { get; set; } = "ddc_default";

        public int MaxThreads { get; set; } = Environment.ProcessorCount;
        public bool AddCrowdEvents { get; set; } = true;
        public bool AdjustHandshapes { get; set; } = true;
        public bool CheckXML { get; set; } = true;
        public bool CheckForArrIdReset { get; set; } = false;
        public bool EnableLogging { get; set; } = true;
        public bool FixChordNames { get; set; } = true;
        public bool FixOneLevelPhrases { get; set; } = true;
        public bool PreserveENDPhraseLocation { get; set; } = false;
        public bool RestoreFirstNoguitarSection { get; set; } = false;
        public bool RestoreNoguitarSectionAnchors { get; set; } = true;
        public bool RemoveAnchorPlaceholderNotes { get; set; } = true;
        public bool RemoveBeatsPastAudioEnd { get; set; } = true;
        public bool RemoveHighDensityStatuses { get; set; } = true;
        public bool RemoveTimeSignatureEvents { get; set; } = false;
        public bool RemoveTranscriptionTrack { get; set; } = false;
        public bool DisplayTimesInSeconds { get; set; } = true;
        public bool WriteAbridgedXmlFiles { get; set; } = true;
        public bool OverwriteOriginalFile { get; set; } = false;

        public static void LoadConfiguration()
        {
            try
            {
                XMLProcessor.Preferences = Load();
            }
            catch
            {
                // Use default preferences
                XMLProcessor.Preferences = new Configuration();
            }
        }

        /// <summary>
        /// Deserializes configuration from an XML file.
        /// </summary>
        private static Configuration Load()
        {
            if (string.IsNullOrEmpty(ConfigFileName))
                throw new InvalidOperationException("Configuration filename is not set.");

            Configuration cfg = new Configuration();
            ReflectionConfig.LoadFromXml(ConfigFileName, cfg);
            cfg.ValidateValues();

            return cfg;
        }

        /// <summary>
        /// Serializes configuration into an XML file.
        /// </summary>
        public void Save()
        {
            if (string.IsNullOrEmpty(ConfigFileName))
                throw new InvalidOperationException("Configuration filename is not set.");

            ReflectionConfig.SaveToXml(ConfigFileName, this);
        }

        private void ValidateValues()
        {
            if (MaxThreads < 1 || MaxThreads > Environment.ProcessorCount)
                MaxThreads = Environment.ProcessorCount;

            if (DDCPhraseLength < 8)
                DDCPhraseLength = 8;
            else if (DDCPhraseLength > 256)
                DDCPhraseLength = 256;

            if (string.IsNullOrWhiteSpace(DDCRampupFile))
                DDCRampupFile = "ddc_default";

            if (string.IsNullOrWhiteSpace(DDCConfigFile))
                DDCConfigFile = "ddc_default";
        }
    }
}
