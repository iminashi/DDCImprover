﻿using Rocksmith2014.XML;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DDCImprover.Core
{
    public sealed class XMLProcessor : INotifyPropertyChanged, IEquatable<XMLProcessor>
    {
        /// <summary>
        /// Number of progress steps reported (GeneratingDD, PostProcessing, Completed)
        /// </summary>
        public const int ProgressSteps = 3;
        internal const short TempMeasureNumber = short.MaxValue;

        private static Configuration? preferences;

        public static Configuration Preferences
        {
            get => preferences ??= new Configuration();
            set => preferences = value;
        }

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

        private string _logViewText = "";

        public string LogViewText
        {
            get => _logViewText;
            set
            {
                if (_logViewText != value)
                {
                    _logViewText = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public List<ImproverMessage> StatusMessages { get; private set; } = new List<ImproverMessage>();

        /// <summary>
        /// XML filename without path, with extension.
        /// </summary>
        public string XMLFileName { get; private set; } = string.Empty;

        public string ArtistName { get; private set; } = string.Empty;
        public string SongTitle { get; private set; } = string.Empty;
        public string ArrangementType { get; private set; } = string.Empty;
        public string LogFileFullPath { get; private set; } = string.Empty;

        public InstrumentalArrangement? OriginalArrangement { get; private set; }
        public InstrumentalArrangement? DDCArrangement { get; private set; }

        public string XMLFileFullPath { get; private set; } = string.Empty;
        private string TempXMLFileFullPath { get; set; } = string.Empty;
        private string DDCXMLFileFullPath { get; set; } = string.Empty;
        private string ManualDDXMLFileFullPath { get; set; } = string.Empty;

        internal List<Anchor> NGAnchors { get; } = new List<Anchor>();
        internal List<Ebeat> AddedBeats { get; } = new List<Ebeat>();

        private XMLPreProcessor? preProcessor;
        private XMLPostProcessor? postProcessor;
        private StreamWriter? logStreamWriter;

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

        private bool ReadSongMetaData()
        {
            using XmlReader reader = XmlReader.Create(XMLFileFullPath);

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
                        var arrProp = (XElement)XNode.ReadFrom(reader);
                        ArrangementType = arrProp.Attribute("pathLead")?.Value == "1" ? "Lead" :
                                          arrProp.Attribute("pathRhythm")?.Value == "1" ? "Rhythm" :
                                          arrProp.Attribute("pathBass")?.Value == "1" ? "Bass" : "N/A";

                        if (arrProp.Attribute("bonusArr")?.Value == "1")
                            ArrangementType = "Bonus " + ArrangementType;
                        else if (arrProp.Attribute("represent")?.Value == "0")
                            ArrangementType = "Alt. " + ArrangementType;

                        // Finished reading everything needed
                        return true;
                    }
                }
            }

            StatusMessages.Add(new ImproverMessage(
                "Unable to read song metadata. " +
                "The file is missing one or more of these tags:\n" +
                "title, artistName, arrangementProperties", MessageType.Error));
            return false;
        }

        internal void SortStatusMessages()
        {
            StatusMessages = StatusMessages
                .Distinct()
                .OrderBy(m => m.TimeCode)
                .ToList();
        }

        private void SetupFilePaths(string xmlFilePath)
        {
            XMLFileFullPath = xmlFilePath;

            var xmlFileInfo = new FileInfo(XMLFileFullPath);
            var xmlFileDir = xmlFileInfo.Directory!.FullName;

            XMLFileName = xmlFileInfo.Name;

            TempXMLFileFullPath = Path.Combine(xmlFileDir, "ORIGINAL_" + XMLFileName);
            DDCXMLFileFullPath = Path.Combine(xmlFileDir, "DDC_" + XMLFileName);
            ManualDDXMLFileFullPath = Path.Combine(xmlFileDir, "DD_" + XMLFileName);

            LogFileFullPath = Path.Combine(Program.LogDirectory, XMLFileName + ".log");
        }

        private void Log(string message)
        {
            if (Preferences.EnableLogging)
            {
                logStreamWriter?.WriteLine(message);
            }
        }

        public ImproverStatus LoadXMLFile()
        {
            try
            {
                OriginalArrangement = InstrumentalArrangement.Load(XMLFileFullPath);
                isNonDDFile = OriginalArrangement.Levels.Count == 1;
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

        /// <summary>
        /// Applies workarounds, executes DDC and saves processed file.
        /// </summary>
        /// <param name="progress">Progress reporter.</param>
        public void ProcessFile(IProgress<ProgressValue> progress)
        {
            if (Status == ImproverStatus.LoadError)
                return;

            if (Preferences.EnableLogging)
                InitializeLogging();

            try
            {
                StatusMessages.Clear();
                LogViewText = string.Empty;

                Log($"Processing file {XMLFileFullPath}");

                Preprocess();

                if (isNonDDFile)
                    GenerateDD(progress);
                else
                    progress?.Report(ProgressValue.TwoSteps);

                LoadDDCXMLFile();

                Postprocess();
            }
            catch (DDCException e)
            {
                Log("ERROR: " + e.Message);
                StatusMessages.Add(new ImproverMessage(e.Message, MessageType.Error));

                Status = ImproverStatus.DDCError;
            }
            catch (Exception e)
            {
                Log("ERROR: " + e.Message);
                Log(e.StackTrace ?? "(No stack trace)");

                StatusMessages.Add(new ImproverMessage(e.Message, MessageType.Error));
                Status = ImproverStatus.ProcessingError;
            }
            finally
            {
                DeleteTempFiles();

                if (Preferences.EnableLogging && logStreamWriter is not null)
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

        private void InitializeLogging()
        {
            int tries = 0;
            while (!TryOpenLogFile())
            {
                if (++tries > 10)
                    break;

                LogFileFullPath = Regex.Replace(LogFileFullPath, @"_*\d*\.log$", $"_{tries}.log");
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

        private void Preprocess()
        {
            Status = ImproverStatus.PreProcessing;
            preProcessor = new XMLPreProcessor(this, Log);
            preProcessor.Process();
            SavePreprocessedFile();
        }

        private void GenerateDD(IProgress<ProgressValue> progress)
        {
            progress?.Report(ProgressValue.OneStep);

            Status = ImproverStatus.GeneratingDD;
            ExecuteDDC();

            progress?.Report(ProgressValue.OneStep);
        }

        private void Postprocess()
        {
            Status = ImproverStatus.PostProcessing;
            postProcessor = new XMLPostProcessor(this, preProcessor!, Log);
            postProcessor.Process();
            SavePostprocessedFile();
        }

        private void DeleteTempFiles()
        {
            if (File.Exists(TempXMLFileFullPath))
            {
                // Delete the preprocessed XML file
                if (File.Exists(XMLFileFullPath))
                {
                    File.Delete(XMLFileFullPath);
                }

                // Restore original XML file
                File.Move(TempXMLFileFullPath, XMLFileFullPath);
            }
        }

        private void Cleanup()
        {
            NGAnchors.Clear();
            AddedBeats.Clear();

            preProcessor = null;
            postProcessor = null;

            OriginalArrangement = null;
            DDCArrangement = null;
        }

        private void LoadDDCXMLFile()
        {
            if (isNonDDFile)
            {
                if (Preferences.OverwriteOriginalFile)
                    DDCArrangement = InstrumentalArrangement.Load(XMLFileFullPath);
                else
                    DDCArrangement = InstrumentalArrangement.Load(DDCXMLFileFullPath);

                Log($"{DDCArrangement.Levels.Count} difficulty levels generated.");
            }
            else
            {
                DDCArrangement = OriginalArrangement;
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
            OriginalArrangement!.Save(XMLFileFullPath);
        }

        private void SavePostprocessedFile()
        {
            string path = isNonDDFile ? DDCXMLFileFullPath : ManualDDXMLFileFullPath;
            if (Preferences.OverwriteOriginalFile)
            {
                path = XMLFileFullPath;
            }
            else if (isNonDDFile && File.Exists(DDCXMLFileFullPath))
            {
                File.Delete(DDCXMLFileFullPath);
            }

            DDCArrangement!.XmlComments.RemoveAll(x => x.CommentType == CommentType.DDCImprover);
            DDCArrangement.XmlComments.Insert(0, new RSXmlComment($" DDC Improver {Program.Version} "));

            DDCArrangement.Save(path, Preferences.WriteAbridgedXmlFiles);

            Log($"Processed DD file saved as {path}");
        }

        private void ExecuteDDC()
        {
            if (!File.Exists(Program.DDCExecutablePath))
                throw new FileNotFoundException("Could not find DDC executable", Program.DDCExecutablePath);

            string arguments = CreateDDCArguments();

            using Process ddcProcess = new Process();

            ddcProcess.StartInfo.UseShellExecute = false;
            ddcProcess.StartInfo.CreateNoWindow = true;
            ddcProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(XMLFileFullPath)!;

            if (Program.UseWine)
            {
                string wine = Program.GetWineExecutable();
                ddcProcess.StartInfo.FileName = wine;
                ddcProcess.StartInfo.Arguments = $"\"{Program.DDCExecutablePath}\" {arguments}";

                Log("Executing command: " + wine + " " + ddcProcess.StartInfo.Arguments);
            }
            else
            {
                ddcProcess.StartInfo.FileName = Program.DDCExecutablePath;
                ddcProcess.StartInfo.Arguments = arguments;

                Log($"Executing 'ddc.exe {arguments}'");
            }

            try
            {
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
            catch (Exception) when (Program.UseWine)
            {
                StatusMessages.Add(new ImproverMessage("Executing wine failed.", MessageType.Error));
            }
        }

        private string CreateDDCArguments()
        {
            string arguments = $"\"{XMLFileName}\" -l {Preferences.DDCPhraseLength} -t N";

            if (!string.IsNullOrEmpty(Preferences.DDCRampupFile) && Preferences.DDCRampupFile != "ddc_default")
            {
                string rmpPath = Path.Combine(Path.GetDirectoryName(Program.DDCExecutablePath)!, $"{Preferences.DDCRampupFile}.xml");
                arguments += $" -m \"{rmpPath}\"";
            }
            if (!string.IsNullOrEmpty(Preferences.DDCConfigFile) && Preferences.DDCConfigFile != "ddc_default")
            {
                string cfgPath = Path.Combine(Path.GetDirectoryName(Program.DDCExecutablePath)!, $"{Preferences.DDCConfigFile}.cfg");
                arguments += $" -c \"{cfgPath}\"";
            }

            if (Preferences.OverwriteOriginalFile)
            {
                arguments += " -p Y";
            }

            return arguments;
        }

        public bool Equals(XMLProcessor? other)
            => XMLFileFullPath.Equals(other?.XMLFileFullPath);

        public override int GetHashCode()
            => XMLFileFullPath.GetHashCode();
    }
}