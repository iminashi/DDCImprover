using DDCImprover.Core;
using DDCImprover.ViewModels.Services;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Rocksmith2014.XML;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.

namespace DDCImprover.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        public ObservableCollectionExtended<XMLProcessor> XMLProcessors { get; } = new ObservableCollectionExtended<XMLProcessor>();

        public string ProgramTitle { get; } = Program.Title;

        public IObservable<WindowType> OpenChildWindow { get; private set; }

        #region Fields

        private readonly IPlatformSpecificServices services;

        private readonly Stopwatch stopwatch = new Stopwatch();

        #endregion

        #region Reactive Commands

        public ReactiveCommand<Unit, Unit> ProcessFiles { get; private set; }
        public ReactiveCommand<Unit, Unit> AddFiles { get; private set; }
        public ReactiveCommand<bool, Unit> OpenFiles { get; private set; }
        public ReactiveCommand<Unit, Unit> CloseFile { get; private set; }
        public ReactiveCommand<Unit, Unit> CloseAll { get; private set; }
        public ReactiveCommand<Unit, Unit> RemoveDD { get; private set; }
        public ReactiveCommand<Unit, Unit> OpenFolder { get; private set; }
        public ReactiveCommand<WindowType, WindowType> ShowWindow { get; private set; }
        public ReactiveCommand<Unit, Unit> OpenGitHubPage { get; private set; }
        public ReactiveCommand<Unit, Unit> Exit { get; private set; }

        #endregion

        #region Reactive Properties

        [Reactive]
        public int ProcessingProgress { get; private set; }

        [Reactive]
        public bool ErrorDuringProcessing { get; private set; }

        [Reactive]
        public int ProgressMaximum { get; private set; }

        [Reactive]
        public string StatusbarMessage { get; private set; } = string.Empty;

        [Reactive]
        public string? StatusbarMessageTooltip { get; private set; }

        [Reactive]
        public IList? SelectedItems { get; set; }

        [Reactive]
        public bool MatchPhrasesToSections { get; set; }

        [Reactive]
        public bool DeleteTranscriptionTrack { get; set; } = true;

        #endregion

        #region Observables as Properties

        public extern bool IsProcessingFiles { [ObservableAsProperty]get; }

        #endregion

        public MainWindowViewModel(IPlatformSpecificServices services, ConfigurationWindowViewModel configViewModel)
        {
            // TODO: Remove?
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            this.services = services;

            CreateReactiveCommands();

            SetupObservables();

            // Reset "view log" text if user clears log files
            configViewModel.LogsCleared.Subscribe(_ => RemoveViewLogTexts());

            // Ensure that application data and log directories exist
            Directory.CreateDirectory(Program.AppDataPath);
            Directory.CreateDirectory(Program.LogDirectory);
        }

        [MemberNotNull(nameof(AddFiles), nameof(OpenFiles), nameof(CloseAll), nameof(RemoveDD),
                       nameof(CloseFile), nameof(ProcessFiles), nameof(OpenFolder), nameof(OpenGitHubPage),
                       nameof(Exit), nameof(ShowWindow))]
        private void CreateReactiveCommands()
        {
            AddFiles = ReactiveCommand.CreateFromTask(_ => LoadFilesImpl(addingFiles: true));

            var canOpenOrCloseAll = this.WhenAnyValue(
                x => x.IsProcessingFiles,
                (processingFiles) => !processingFiles);

            OpenFiles = ReactiveCommand.CreateFromTask<bool>(LoadFilesImpl, canOpenOrCloseAll);

            CloseAll = ReactiveCommand.Create(CloseAllImpl, canOpenOrCloseAll);

            RemoveDD = ReactiveCommand.CreateFromTask(RemoveDDImpl);

            var canClose = this.WhenAnyValue(
                x => x.SelectedItems,
                x => x.IsProcessingFiles,
                (items, processingFiles) => items?.Count > 0 && !processingFiles);

            CloseFile = ReactiveCommand.Create(CloseFileImpl, canClose);

            var canProcess = this.WhenAnyValue(
                x => x.XMLProcessors.Count,
                (count) => count > 0);

            ProcessFiles = ReactiveCommand.CreateFromTask(ProcessFilesImpl, canProcess);

            var canOpen = this.WhenAnyValue(
                x => x.SelectedItems,
                items => items?.Count > 0);

            OpenFolder = ReactiveCommand.Create(OpenFolderImpl, canOpen);

            OpenGitHubPage = ReactiveCommand.Create(() => "https://github.com/iminashi/DDCImprover".StartAsProcess());

            Exit = ReactiveCommand.Create(() => services.ExitApplication());

            ShowWindow = ReactiveCommand.Create<WindowType, WindowType>(type => type);
        }

        [MemberNotNull(nameof(OpenChildWindow))]
        private void SetupObservables()
        {
            // Keep the maximum value of the progressbar up to date
            this.WhenAnyValue(x => x.XMLProcessors.Count)
                .Where(c => c > 0)
                .Subscribe(count => ProgressMaximum = count * XMLProcessor.ProgressSteps);

            ProcessFiles.IsExecuting.ToPropertyEx(this, x => x.IsProcessingFiles, deferSubscription: false);

            OpenChildWindow = ShowWindow.
                Merge(
                    ProcessFiles
                        .Where(_ => XMLProcessors.Sum(processor => processor.StatusMessages.Count) > 0)
                        .Select(_ => WindowType.ProcessingMessages)
                    );
        }

        /// <summary>
        /// Opens the containing folder for all the selected files.
        /// </summary>
        private void OpenFolderImpl()
        {
            if (SelectedItems is not null)
            {
                foreach (string path in SelectedItems.Cast<XMLProcessor>().Select(x => x.XMLFileFullPath).Distinct())
                {
                    Path.GetDirectoryName(path)?.StartAsProcess();
                }
            }
        }

        /// <summary>
        /// Removes the currently selected file(s) from the XML processor list.
        /// </summary>
        private void CloseFileImpl()
        {
            if (SelectedItems is null)
                return;

            foreach (var processor in SelectedItems.Cast<XMLProcessor>().ToArray())
            {
                XMLProcessors.Remove(processor);
            }

            SelectedItems = null;
        }

        /// <summary>
        /// Removes dynamic difficulty levels from the files the user chooses.
        /// </summary>
        private async Task RemoveDDImpl()
        {
            var fileNames = await services
                .OpenFileDialog(
                    "Select RS2014 XML File(s) to Remove DD From",
                    FileFilter.RSXmlFiles,
                    multiSelect: true)
                .ConfigureAwait(false);

            if (fileNames?.Length > 0)
            {
                ShowInStatusbar("Removing DD...");

#if DEBUG
                Stopwatch stopwatch = Stopwatch.StartNew();
#endif

                await Task.Run(() => Parallel.ForEach(
                    fileNames,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 4)
                    },
                    async fn =>
                    {
                        var arrangement = InstrumentalArrangement.Load(fn);
                        await DDRemover.RemoveDD(arrangement, MatchPhrasesToSections, DeleteTranscriptionTrack).ConfigureAwait(false);
                        string oldFileName = Path.GetFileName(fn);
                        string newFileName = oldFileName.StartsWith("DDC_") ?
                            oldFileName.Substring(4) :
                            oldFileName;
                        arrangement.Save(Path.Combine(Path.GetDirectoryName(fn)!, "NDD_" + newFileName));
                    }));

                string files = (fileNames.Length == 1) ? "File" : "Files";
                string statusText = $"Removing DD completed. {files} saved with NDD_ prefix.";
#if DEBUG
                statusText += " Elapsed: " + stopwatch.ElapsedMilliseconds;
#endif
                ShowInStatusbar(statusText);
            }
        }

        private void CloseAllImpl()
        {
            XMLProcessors.Clear();
            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();
        }

        private async Task LoadFilesImpl(bool addingFiles = false)
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
        /// Creates the XML Processors and adds them to the XMLProcessors collection.
        /// </summary>
        /// <param name="filenames">A list of the names of XML files to load.</param>
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

            if (!skippedFiles.IsEmpty)
            {
                UpdateStatusBarForSkippedFiles(skippedFiles);
            }

            if (errorMessage.Length > 0)
                services.NotifyUser(errorMessage, "Error");
        }

        /// <summary>
        /// Creates a tooltip from skipped filenames for the statusbar message.
        /// </summary>
        /// <param name="skippedFiles">Collection of filenames that were skipped.</param>
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

        private async Task ProcessFilesImpl()
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

            ShowElapsedTime();

            if (XMLProcessor.Preferences.CheckForArrIdReset)
            {
                PhraseLevelRepository.UpdateRepository();
            }
        }

        /// <summary>
        /// Loads the XML files for each processor.
        /// </summary>
        /// <returns>All XML processors that were loaded successfully.</returns>
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

        /// <summary>
        /// Shows the time it took to process the files in the statusbar.
        /// </summary>
        private void ShowElapsedTime()
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
        }

        /// <summary>
        /// Removes the "View" text from the Log column for all opened files.
        /// </summary>
        public void RemoveViewLogTexts()
        {
            foreach (var xmlProcessor in XMLProcessors)
            {
                xmlProcessor.LogViewText = string.Empty;
            }
        }

        /// <summary>
        /// Sets the message to be displayed in the statusbar.
        /// </summary>
        /// <param name="message">The message to show in the statusbar.</param>
        /// <param name="tooltip">The tooltip for the message.</param>
        private void ShowInStatusbar(string message, string? tooltip = null)
        {
            StatusbarMessage = message;
            StatusbarMessageTooltip = tooltip;
        }
    }
}
