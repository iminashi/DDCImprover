using DDCImprover.Core.Services;
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
using System.Threading.Tasks;

namespace DDCImprover.Core.ViewModels
{
    public class ConfigurationWindowViewModel : ReactiveObject
    {
        public Configuration Config { get; set; }

        #region Reactive Properties

        private readonly Subject<Unit> logsCleared = new Subject<Unit>();

        public IObservable<Unit> LogsCleared { get; }

        [Reactive]
        public string LogsDeletedText { get; private set; }

        [Reactive]
        public List<string> DDCConfigFiles { get; private set; }

        [Reactive]
        public List<string> DDCRampupFiles { get; private set; }

        #endregion

        #region Reactive Commands

        public ReactiveCommand<Unit, Unit> DeleteLogs { get; }
        public ReactiveCommand<Unit, Unit> SelectDDCExecutable { get; }

        #endregion

        private readonly IPlatformSpecificServices services;

        public ConfigurationWindowViewModel(IPlatformSpecificServices services, Configuration config)
        {
            this.services = services;
            Config = config;

            LogsCleared = logsCleared.AsObservable();

            DeleteLogs = ReactiveCommand.Create(DeleteLogs_Impl);

            SelectDDCExecutable = ReactiveCommand.CreateFromTask(SelectDDCExecutable_Impl);

            EnumerateDDCSettings();
        }

        public void EnumerateDDCSettings()
        {
            if (File.Exists(XMLProcessor.Preferences.DDCExecutablePath))
            {
                DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(XMLProcessor.Preferences.DDCExecutablePath));

                DDCConfigFiles = (from file in di.GetFiles("*.cfg")
                                  orderby file.Name
                                  select file.Name.Replace(".cfg", ""))
                                  .ToList();

                DDCRampupFiles = (from file in di.GetFiles("*.xml")
                                  orderby file.Name
                                  select file.Name.Replace(".xml", ""))
                                  .ToList();
            }
        }

        private void DeleteLogs_Impl()
        {
            int filesDeleted = 0;

            if (Directory.Exists(Configuration.LogDirectory))
            {
                foreach (string filename in Directory.GetFiles(Configuration.LogDirectory, "*.log"))
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

        private async Task SelectDDCExecutable_Impl()
        {
            string[] filenames = await services
                .OpenFileDialog(
                    "Select DDC Executable",
                    FileFilter.DDCExecutable,
                    multiSelect: false)
                .ConfigureAwait(false);

            if (filenames?.Length == 1 && filenames[0].EndsWith("ddc.exe", StringComparison.OrdinalIgnoreCase))
            {
                XMLProcessor.Preferences.DDCExecutablePath = filenames[0];

                EnumerateDDCSettings();
            }
        }
    }
}
