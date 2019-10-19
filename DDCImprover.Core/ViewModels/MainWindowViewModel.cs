﻿using DDCImprover.Core.Services;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DDCImprover.Core.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public ObservableCollectionExtended<XMLProcessor> XMLProcessors { get; } = new ObservableCollectionExtended<XMLProcessor>();

        public string ProgramTitle { get; }

        public IObservable<Unit> ShouldDisplayProcessingMessages { get; private set; }

        #region Fields

        private readonly IPlatformSpecificServices services;

        private readonly Stopwatch stopwatch = new Stopwatch();

        #endregion

        #region Reactive Commands

        public ReactiveCommand<Unit, Unit> Process { get; private set; }
        public ReactiveCommand<Unit, Unit> AddFiles { get; private set; }
        public ReactiveCommand<bool, Unit> OpenFiles { get; private set; }
        public ReactiveCommand<Unit, Unit> CloseFile { get; private set; }
        public ReactiveCommand<Unit, Unit> CloseAll { get; private set; }

        #endregion

        #region Reactive Properties

        [Reactive]
        public int ProcessingProgress { get; private set; }

        [Reactive]
        public bool ErrorDuringProcessing { get; private set; }

        [Reactive]
        public int ProgressMaximum { get; private set; }

        [Reactive]
        public string StatusbarMessage { get; private set; }

        [Reactive]
        public string StatusbarMessageTooltip { get; private set; }

        [Reactive]
        public IList SelectedItems { get; set; }

        #endregion

        #region Observables as Properties

        public extern bool IsProcessingFiles { [ObservableAsProperty]get; }

        #endregion

        public MainWindowViewModel(IPlatformSpecificServices services)
        {
            // TODO: Remove?
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            this.services = services;

            CreateReactiveCommands();

            SetupObservables();

            // Ensure that the log directory exists
            Directory.CreateDirectory(Configuration.LogDirectory);

            ProgramTitle = "DDC Improver " + Program.Version;

            try
            {
                XMLProcessor.Preferences = Configuration.Load();
            }
            catch
            {
                // Use default preferences
                XMLProcessor.Preferences = new Configuration();
            }
        }

        private void CreateReactiveCommands()
        {
            AddFiles = ReactiveCommand.CreateFromTask(_ => LoadFiles_Implementation(addingFiles: true));

            var canOpenOrCloseAll = this.WhenAnyValue(
                x => x.IsProcessingFiles,
                (processingFiles) => !processingFiles);

            OpenFiles = ReactiveCommand.CreateFromTask<bool>(LoadFiles_Implementation, canOpenOrCloseAll);

            CloseAll = ReactiveCommand.Create(CloseAllCommand_Implementation, canOpenOrCloseAll);

            var canClose = this.WhenAnyValue(
                x => x.SelectedItems,
                x => x.IsProcessingFiles,
                (items, processingFiles) => items?.Count > 0 && !processingFiles);

            CloseFile = ReactiveCommand.Create(CloseFile_Implementation, canClose);

            var canProcess = this.WhenAnyValue(
                x => x.XMLProcessors.Count,
                (count) => count > 0);

            Process = ReactiveCommand.CreateFromTask(ProcessCommand_Implementation, canProcess);
        }

        private void SetupObservables()
        {
            // Keep maximum value of progressbar up to date
            this.WhenAnyValue(x => x.XMLProcessors.Count)
                .Where(c => c > 0)
                .Subscribe(count => ProgressMaximum = count * XMLProcessor.ProgressSteps);

            Process.IsExecuting.ToPropertyEx(this, x => x.IsProcessingFiles, false);

            ShouldDisplayProcessingMessages = Process.Where(_ => XMLProcessors.Sum(processor => processor.StatusMessages.Count) > 0);
        }

        /// <summary>
        /// Checks if DDC executable exists and prompts the user for its location if not found.
        /// </summary>
        /// <returns></returns>
        public async Task<DDCExecutableCheckResult> CheckDDCExecutable()
        {
            if (!File.Exists(XMLProcessor.Preferences.DDCExecutablePath))
            {
                string[] filenames = await services
                    .OpenFileDialog(
                        "Select DDC Executable",
                        FileFilter.DDCExecutable,
                        multiSelect: false)
                    .ConfigureAwait(false);

                if (filenames?.Length > 0)
                {
                    XMLProcessor.Preferences.DDCExecutablePath = filenames[0];
                    return DDCExecutableCheckResult.LocationChanged;
                }
                else
                {
                    ShowInStatusbar("Please set the DDC executable path in the configuration.");
                    return DDCExecutableCheckResult.NotSet;
                }
            }

            return DDCExecutableCheckResult.Found;
        }

        /// <summary>
        /// Removes currently selected file(s) from the XML processor list.
        /// </summary>
        private void CloseFile_Implementation()
        {
            if (SelectedItems == null)
                return;

            foreach (var processor in SelectedItems.Cast<XMLProcessor>().ToArray())
            {
                XMLProcessors.Remove(processor);
            }
        }

        private void CloseAllCommand_Implementation()
        {
            XMLProcessors.Clear();
            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();
        }

        private async Task LoadFiles_Implementation(bool addingFiles = false)
        {
            if (!addingFiles && IsProcessingFiles)
                return;

            var fileNames = await services
                .OpenFileDialog(
                    "Select RS2014 XML Arrangement(s)",
                    FileFilter.RSXmlFiles,
                    multiSelect: true);

            if (fileNames?.Length > 0)
            {
                if (!addingFiles)
                {
                    XMLProcessors.Clear();
                }

                await AddFilesAsync(fileNames).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates XML Processors and adds them to the list.
        /// </summary>
        /// <param name="filenames"></param>
        public async Task AddFilesAsync(IEnumerable<string> filenames)
        {
#if DEBUG
            stopwatch.Restart();
#endif

            string errorMessage = string.Empty;
            var skippedFiles = new ConcurrentBag<string>();
            var currentlyLoaded = XMLProcessors.ToArray();
            var blockingCollection = new BlockingCollection<XMLProcessor>();
            var loadingErrorMessageLock = new object();
            var cd = new CompositeDisposable(XMLProcessors.SuspendCount(), blockingCollection);

            // Consumer
            blockingCollection.GetConsumingEnumerable()
                    .ToObservable(TaskPoolScheduler.Default)
                    .Buffer(5)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(
                        onNext: xmlProcessors => XMLProcessors.AddRange(xmlProcessors),
                        onCompleted: () => cd.Dispose()
                     );

            // Producers
            var loadingTasks = filenames.Select(fileFullPath => Task.Run(() =>
            {
                if (!fileFullPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string filename = Path.GetFileName(fileFullPath);

                if (filename.Contains("VOCAL") || filename.StartsWith("DDC") || filename.StartsWith("DD_"))
                {
                    skippedFiles.Add(filename);
                    return;
                }

                // Skip files that are already loaded
                if (currentlyLoaded.Any(f => f.XMLFileFullPath == fileFullPath))
                {
                    skippedFiles.Add(filename);
                    return;
                }

                var processor = new XMLProcessor(fileFullPath);
                if (processor.Status == ImproverStatus.LoadError)
                {
                    lock (loadingErrorMessageLock)
                    {
                        errorMessage += $"Error loading file {processor.XMLFileName}:{Environment.NewLine}{processor.StatusMessages[0].Message}{Environment.NewLine}";
                    }
                    return;
                }

                blockingCollection.Add(processor);
            }));

            var completeTasks = Task.WhenAll(loadingTasks.ToArray())
                                    .ContinueWith(_ => blockingCollection.CompleteAdding());

            await completeTasks;

#if DEBUG
            stopwatch.Stop();
            ShowInStatusbar($"Loading time: {stopwatch.ElapsedMilliseconds} ms");
#endif

            if (skippedFiles.Count > 0)
            {
                UpdateStatusBarForSkippedFiles(skippedFiles);
            }

            if (errorMessage.Length > 0)
                services.NotifyUser(errorMessage, "Error");
        }

        /// <summary>
        /// Creates a tooltip from skipped filenames for the statusbar message.
        /// </summary>
        /// <param name="skippedFiles"></param>
        private void UpdateStatusBarForSkippedFiles(ConcurrentBag<string> skippedFiles)
        {
            string tooltip = skippedFiles.Aggregate(
                new StringBuilder("Skipped files:" + Environment.NewLine),
                (sb, fileName) => sb.AppendLine().Append(fileName))
                .ToString();

            ShowInStatusbar($"{skippedFiles.Count} file{(skippedFiles.Count == 1 ? "" : "s")} skipped", tooltip);
        }

        private void InitializeProcessing()
        {
            ShowInStatusbar("Processing...");

            stopwatch.Restart();

            ProcessingProgress = 0;
            ErrorDuringProcessing = false;
        }

        private async Task ProcessCommand_Implementation()
        {
            if (IsProcessingFiles)
                return;

            InitializeProcessing();

            // Progress reporter that will run on the UI thread
            var progressReporter = new Progress<ProgressValue>(value =>
            {
                if (value == ProgressValue.Error)
                {
                    ErrorDuringProcessing = true;
                }
                else
                {
                    ProcessingProgress += (int)value;
                }
            });

            var options = new ParallelOptions { MaxDegreeOfParallelism = XMLProcessor.Preferences.MaxThreads };
            var partitioner = Partitioner.Create(SetupXMLProcessors(), EnumerablePartitionerOptions.NoBuffering);

            await Task.Run(() => Parallel.ForEach(
                partitioner,
                options,
                (xmlProcessor) => xmlProcessor.ProcessFile(progressReporter)))
                .ConfigureAwait(false);

            /*using (var blockingCollection = new BlockingCollection<XMLProcessor>(XMLProcessors.Count))
            {
                var producer = Task.Run(() =>
                {
                    foreach (var xmlProcessor in XMLProcessors.Where(x => x.Status != ImproverStatus.LoadError).ToArray())
                    {
                        if (xmlProcessor.LoadXMLFile() != ImproverStatus.LoadError)
                        {
                            blockingCollection.Add(xmlProcessor);
                        }
                    }

                    blockingCollection.CompleteAdding();
                });

                var consumers = Enumerable.Range(0, XMLProcessor.Preferences.MaxThreads)
                    .Select(_ => Task.Run(() =>
                    {
                        foreach(var xmlProcessor in blockingCollection.GetConsumingEnumerable())
                        {
                            xmlProcessor.ProcessFile(progressReporter);
                        }
                    }))
                    .ToArray();

                await producer;

                await Task.WhenAll(consumers).ConfigureAwait(false);
            }*/

            ProcessingCompleted();
        }

        private IEnumerable<XMLProcessor> SetupXMLProcessors()
        {
            foreach (var xmlProcessor in XMLProcessors.ToArray())
            {
                if (xmlProcessor.LoadXMLFile() != ImproverStatus.LoadError)
                {
                    yield return xmlProcessor;
                }
            }
        }

        private void ProcessingCompleted()
        {
            stopwatch.Stop();

            bool isSeconds = false;
            double timeElapsed = stopwatch.ElapsedMilliseconds;
            if (timeElapsed >= 7000)
            {
                isSeconds = true;
                timeElapsed = Math.Round(timeElapsed / 1000, 1);
            }

            ShowInStatusbar($"Processing completed in {timeElapsed}{(isSeconds ? "s" : "ms")}");

            if (XMLProcessor.Preferences.CheckForArrIdReset)
            {
                PhraseLevelRepository.UpdateRepository();
            }
        }

        public void RemoveViewLogTexts()
        {
            foreach (var xmlProcessor in XMLProcessors)
            {
                xmlProcessor.LogViewText = "";
            }
        }

        /// <summary>
        /// Sets the message to be displayed in the statusbar.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="tooltip"></param>
        private void ShowInStatusbar(string message, string tooltip = null)
        {
            StatusbarMessage = message;
            StatusbarMessageTooltip = tooltip;
        }
    }
}
