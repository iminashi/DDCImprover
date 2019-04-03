using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DDCImprover.Core
{
    public partial class XMLProcessor : INotifyPropertyChanged, IEquatable<XMLProcessor>
    {
        /// <summary>
        /// Number of progress steps reported (GeneratingDD, PostProcessing, Completed)
        /// </summary>
        public const int ProgressSteps = 3;
        internal const int TempMeasureNumber = 65535;

        public static Configuration Preferences;
        public static string Version;

        private static readonly bool UseWine = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private ImproverStatus _status;

        public ImproverStatus Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    NotifyPropertyChanged(nameof(StatusHumanized));
                }
            }
        }

        private static readonly Dictionary<ImproverStatus, string> humanizedStatusStrings = new Dictionary<ImproverStatus, string>
        {
            { ImproverStatus.Idle, "Load OK" },
            { ImproverStatus.PreProcessing, "Preprocessing" },
            { ImproverStatus.GeneratingDD, "Executing DDC" },
            { ImproverStatus.PostProcessing, "Postprocessing" },
            { ImproverStatus.Completed, "Completed" },
            { ImproverStatus.ProcessingError, "Error Processing" },
            { ImproverStatus.LoadError, "Error Loading" },
            { ImproverStatus.DDCError, "DDC Error" },
        };

        public string StatusHumanized => humanizedStatusStrings[_status];

        private string _logText = "";

        public string LogViewText
        {
            get => _logText;
            set
            {
                if (_logText != value)
                {
                    _logText = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public readonly List<ImproverMessage> StatusMessages = new List<ImproverMessage>();

        /// <summary>
        /// XML filename without path, with extension.
        /// </summary>
        public string XMLFileName { get; private set; }
        public string ArtistName { get; private set; }
        public string SongTitle { get; private set; }
        public string ArrangementType { get; private set; }
        public string LogFileFullPath { get; private set; }

        private XMLPreProcessor preProcessor;
        private XMLPostProcessor postProcessor;

        private RS2014Song originalSong;
        private RS2014Song DDCSong;

        private StreamWriter logStreamWriter;

        public string XMLFileFullPath { get; private set; }
        private string TempXMLFileFullPath { get; set; }
        private string DDCXMLFileFullPath { get; set; }
        private string ManualDDXMLFileFullPath { get; set; }

        internal readonly List<Anchor> NGAnchors = new List<Anchor>();
        internal readonly List<Ebeat> addedBeats = new List<Ebeat>();

        private bool isNonDDFile = true;

        public XMLProcessor(string XMLFilePath)
        {
            SetupFilePaths(XMLFilePath);

            bool readingSuccessful = false;

            try
            {
                readingSuccessful = ReadSongMetaData();
            }
            catch (NullReferenceException)
            {
                const string errorMessage = "If a valid RS2014 guitar/bass arrangement, the file may be corrupt.";
                Debug.WriteLine(Path.GetFileName(XMLFileFullPath) + ": " + errorMessage);

                StatusMessages.Add(new ImproverMessage(errorMessage, MessageType.Error));
            }
            catch (Exception e)
            {
                Debug.WriteLine(Path.GetFileName(XMLFileFullPath) + ": " + e.Message);

                StatusMessages.Add(new ImproverMessage(e.Message, MessageType.Error));
            }

            if (!readingSuccessful)
                Status = ImproverStatus.LoadError;
        }

        private void SetupFilePaths(string xmlFilePath)
        {
            XMLFileFullPath = xmlFilePath;

            FileInfo xmlFileInfo = new FileInfo(XMLFileFullPath);

            XMLFileName = xmlFileInfo.Name;

            TempXMLFileFullPath = Path.Combine(xmlFileInfo.Directory.FullName, "ORIGINAL_" + XMLFileName);
            DDCXMLFileFullPath = Path.Combine(xmlFileInfo.Directory.FullName, "DDC_" + XMLFileName);
            ManualDDXMLFileFullPath = Path.Combine(xmlFileInfo.Directory.FullName, "DD_" + XMLFileName);

            LogFileFullPath = Path.Combine(Configuration.LogDirectory, XMLFileName + ".log");
        }

        private void Log(string message)
        {
            if (Preferences.EnableLogging)
            {
                logStreamWriter?.WriteLine(message);
            }
        }

        private bool TryOpenLogFile()
        {
            try
            {
                logStreamWriter = File.CreateText(LogFileFullPath);

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Could not open log file ({LogFileFullPath}).");
                Debug.WriteLine(e.Message);

                return false;
            }
        }

        /// <summary>
        /// Applies workarounds, executes DDC and saves processed file.
        /// </summary>
        /// <param name="progress">Progress reporter.</param>
        public void ProcessFile(IProgress<ProgressValue> progress)
        {
            if (Status == ImproverStatus.LoadError)
                return;

            if (Preferences.EnableLogging)
            {
                int tries = 0;
                while (!TryOpenLogFile())
                {
                    if (++tries > 10)
                        break;

                    LogFileFullPath = Regex.Replace(LogFileFullPath, @"_*\d*\.log$", $"_{tries}.log");
                }
            }

            try
            {
                StatusMessages.Clear();
                LogViewText = "";

                Log($"Processing file {XMLFileFullPath}");

                Status = ImproverStatus.PreProcessing;

                // Preprocess
                preProcessor = new XMLPreProcessor(this);
                preProcessor.Process();
                SavePreprocessedFile();

                // Generate DD
                if (isNonDDFile)
                {
                    progress?.Report(ProgressValue.OneStep);

                    Status = ImproverStatus.GeneratingDD;
                    GenerateDD();

                    progress?.Report(ProgressValue.OneStep);
                }
                else
                {
                    progress?.Report(ProgressValue.TwoSteps);
                }

                LoadDDCXMLFile();

                Status = ImproverStatus.PostProcessing;

                // Postprocess
                postProcessor = new XMLPostProcessor(this);
                postProcessor.Process();
                SavePostprocessedFile();
            }
            catch (DDCException e)
            {
                Log("ERROR: " + e.Message);
                StatusMessages.Add(new ImproverMessage(e.Message, MessageType.Error));

                Status = ImproverStatus.DDCError;
            }
            catch (Exception e)
            {
                //if (Debugger.IsAttached)
                //    Debugger.Break();

                Log("ERROR: " + e.Message);
                Log(e.StackTrace);

                StatusMessages.Add(new ImproverMessage(e.Message, MessageType.Error));
                Status = ImproverStatus.ProcessingError;
            }
            finally
            {
                if (File.Exists(TempXMLFileFullPath))
                {
                    // Delete processed pre-DDC XML file
                    if (File.Exists(XMLFileFullPath))
                    {
                        File.Delete(XMLFileFullPath);
                    }

                    // Restore original XML file
                    File.Move(TempXMLFileFullPath, XMLFileFullPath);
                }

                if (Preferences.EnableLogging && logStreamWriter != null)
                {
                    LogViewText = "View";
                    logStreamWriter.Close();
                }

                // Processing completed without errors if status is PostProcessing
                if (Status == ImproverStatus.PostProcessing)
                {
                    Status = ImproverStatus.Completed;
                    progress?.Report(ProgressValue.OneStep);
                }
                else
                {
                    progress?.Report(ProgressValue.Error);
                }

                Cleanup();
            }
        }

        private void Cleanup()
        {
            NGAnchors.Clear();
            addedBeats.Clear();

            preProcessor = null;
            postProcessor = null;

            originalSong = null;
            DDCSong = null;
        }

        private bool ReadSongMetaData()
        {
            using (XmlReader reader = XmlReader.Create(XMLFileFullPath))
            {
                reader.MoveToContent();

                if (reader.LocalName != "song")
                {
                    StatusMessages.Add(new ImproverMessage("This file is not a valid Rocksmith XML file.", MessageType.Error));
                    return false;
                }

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "title")
                        {
                            SongTitle = reader.ReadElementContentAsString();
                        }
                        else if (reader.Name == "artistName")
                        {
                            ArtistName = reader.ReadElementContentAsString();
                        }
                        else if (reader.Name == "arrangementProperties")
                        {
                            var arrProp = XNode.ReadFrom(reader) as XElement;
                            ArrangementType = arrProp.Attribute("pathLead").Value == "1" ? "Lead" :
                                              arrProp.Attribute("pathRhythm").Value == "1" ? "Rhythm" :
                                              arrProp.Attribute("pathBass").Value == "1" ? "Bass" : "N/A";

                            if (arrProp.Attribute("bonusArr").Value == "1")
                                ArrangementType = "Bonus " + ArrangementType;
                            else if (arrProp.Attribute("represent").Value == "0")
                                ArrangementType = "Alt. " + ArrangementType;

                            // Finished reading everything needed
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public ImproverStatus LoadXMLFile()
        {
            try
            {
                originalSong = RS2014Song.Load(XMLFileFullPath);
                isNonDDFile = originalSong.Levels.Count == 1;
                Status = ImproverStatus.Idle;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(Path.GetFileName(XMLFileFullPath) + ": " + ex.Message);

                StatusMessages.Add(new ImproverMessage(ex.Message, MessageType.Error));
                Status = ImproverStatus.LoadError;
            }

            return Status;
        }

        private void LoadDDCXMLFile()
        {
            if (isNonDDFile)
            {
                if (Preferences.OverwriteOriginalFile)
                    DDCSong = RS2014Song.Load(XMLFileFullPath);
                else
                    DDCSong = RS2014Song.Load(DDCXMLFileFullPath);
            }
            else
            {
                DDCSong = originalSong;
            }
        }

        private void SavePreprocessedFile()
        {
            if (!Preferences.OverwriteOriginalFile)
            {
                // Prefix original file with "ORIGINAL_"
                File.Move(XMLFileFullPath, TempXMLFileFullPath);
            }

            // Save processed file using original filename
            originalSong.Save(XMLFileFullPath);
        }

        private void SavePostprocessedFile()
        {
            string path = isNonDDFile ? DDCXMLFileFullPath : ManualDDXMLFileFullPath;
            if (Preferences.OverwriteOriginalFile)
            {
                path = XMLFileFullPath;
            }
            else if(isNonDDFile && File.Exists(DDCXMLFileFullPath))
            {
                File.Delete(DDCXMLFileFullPath);
            }

            DDCSong.XmlComments.RemoveAll(x => x.CommentType == CommentType.DDCImprover);
            DDCSong.XmlComments.Add(new RSXmlComment($" DDC Improver {Version} "));

            DDCSong.Save(path, Preferences.WriteAbridgedXmlFiles);

            Log($"Processed DD file saved as {path}");
        }

        private void GenerateDD()
        {
            if (!File.Exists(Preferences.DDCExecutablePath))
                throw new FileNotFoundException("Could not find DDC executable", Preferences.DDCExecutablePath);

            string arguments = $"\"{XMLFileName}\" -l {Preferences.DDCPhraseLength} -t N";

            if (!string.IsNullOrEmpty(Preferences.DDCRampupFile) && Preferences.DDCRampupFile != "ddc_default")
            {
                string rmpPath = Path.Combine(Path.GetDirectoryName(Preferences.DDCExecutablePath), $"{Preferences.DDCRampupFile}.xml");
                arguments += $" -m \"{rmpPath}\"";
            }
            if (!string.IsNullOrEmpty(Preferences.DDCConfigFile) && Preferences.DDCConfigFile != "ddc_default")
            {
                string cfgPath = Path.Combine(Path.GetDirectoryName(Preferences.DDCExecutablePath), $"{Preferences.DDCConfigFile}.cfg");
                arguments += $" -c \"{cfgPath}\"";
            }

            if(Preferences.OverwriteOriginalFile)
            {
                arguments += " -p Y";
            }

            using (Process ddcProcess = new Process())
            {
                ddcProcess.StartInfo.UseShellExecute = false;
                ddcProcess.StartInfo.CreateNoWindow = true;

                if (UseWine)
                {
                    ddcProcess.StartInfo.FileName = "wine";
                    ddcProcess.StartInfo.Arguments = $"\"{Preferences.DDCExecutablePath}\" {arguments}";
                }
                else
                {
                    ddcProcess.StartInfo.FileName = Preferences.DDCExecutablePath;
                    ddcProcess.StartInfo.Arguments = arguments;
                }

                ddcProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(XMLFileFullPath);

                Log($"Executing 'ddc.exe {arguments}'");

                ddcProcess.Start();
                ddcProcess.WaitForExit();

                switch (ddcProcess.ExitCode)
                {
                    case 0:
                        Log($"DDC exit code {ddcProcess.ExitCode}: Exited normally.");
                        break;
                    case 1:
                        throw new DDCException($"DDC exit code {ddcProcess.ExitCode}: System Error.");
                    case 2:
                        throw new DDCException($"DDC exit code {ddcProcess.ExitCode}: Application error.");
                    default:
                        throw new DDCException($"DDC exit code {ddcProcess.ExitCode}: Undefined.");
                }
            }
        }

        public bool Equals(XMLProcessor other)
            => XMLFileFullPath.Equals(other?.XMLFileFullPath);

        public override int GetHashCode()
            => XMLFileFullPath.GetHashCode();
    }
}
