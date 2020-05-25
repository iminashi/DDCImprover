﻿using DDCImprover.Core.Services;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace DDCImprover.Core.ViewModels
{
    public class ConfigurationWindowViewModel : ReactiveObject
    {
        public Configuration Config { get; set; }

        #region Reactive Properties

        private readonly Subject<Unit> logsCleared = new Subject<Unit>();

        public IObservable<Unit> LogsCleared { get; }

        [Reactive]
        public string LogsDeletedText { get; private set; } = string.Empty;

        [Reactive]
        public List<string> DDCConfigFiles { get; private set; }

        [Reactive]
        public List<string> DDCRampupFiles { get; private set; }

        #endregion

        public ReactiveCommand<Unit, Unit> DeleteLogs { get; }

        private readonly IPlatformSpecificServices services;

        public ConfigurationWindowViewModel(IPlatformSpecificServices services)
        {
            this.services = services;

            Configuration.LoadConfiguration();
            Config = XMLProcessor.Preferences;

            LogsCleared = logsCleared.AsObservable();

            DeleteLogs = ReactiveCommand.Create(DeleteLogs_Impl);

            EnumerateDDCSettings();
        }

        public void EnumerateDDCSettings()
        {
            if (File.Exists(Program.DDCExecutablePath))
            {
                DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(Program.DDCExecutablePath));

                DDCConfigFiles = (from file in di.GetFiles("*.cfg")
                                  orderby file.Name
                                  select file.Name.Replace(".cfg", ""))
                                  .ToList();

                DDCRampupFiles = (from file in di.GetFiles("*.xml")
                                  where !file.Name.Contains("dd_remover") // Exclude the dd_remover ramp-up file
                                  orderby file.Name
                                  select file.Name.Replace(".xml", ""))
                                  .ToList();
            }
        }

        private void DeleteLogs_Impl()
        {
            int filesDeleted = 0;

            if (Directory.Exists(Program.LogDirectory))
            {
                foreach (string filename in Directory.GetFiles(Program.LogDirectory, "*.log"))
                {
                    try
                    {
                        File.Delete(filename);
                        filesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("Failed to delete log file: " + ex.Message);
                    }
                }

                logsCleared.OnNext(Unit.Default);
                LogsDeletedText = $"{filesDeleted} file{(filesDeleted == 1 ? "" : "s")} deleted.";
            }
        }
    }
}
