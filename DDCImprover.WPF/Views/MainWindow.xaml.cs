using DDCImprover.Core;
using DDCImprover.ViewModels;
using DDCImprover.ViewModels.Services;

using ReactiveUI;

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.

namespace DDCImprover.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IViewFor<MainWindowViewModel>
    {
        public MainWindowViewModel ViewModel { get; set; }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MainWindowViewModel)value;
        }

        private readonly ConfigurationWindowViewModel configViewModel;
        private readonly WPFServices services;

        public MainWindow()
        {
            InitializeComponent();

            services = new WPFServices();
            configViewModel = new ConfigurationWindowViewModel();
            DataContext = ViewModel = new MainWindowViewModel(services, configViewModel);

            // Change mouse cursor when processing files
            this.WhenAnyValue(x => x.ViewModel.IsProcessingFiles)
                .ObserveOnDispatcher()
                .Subscribe(processing => Cursor = processing ? Cursors.AppStarting : Cursors.Arrow);

            // Show child windows when needed
            ViewModel.OpenChildWindow
                .ObserveOnDispatcher()
                .Subscribe(ShowChildWindow);

            // Close file(s) when user clicks delete
            Observable.FromEventPattern<KeyEventArgs>(filesListView, "KeyUp")
                .Where(e => e.EventArgs.Key == Key.Delete && (e.Sender as ListView)?.SelectedItem != null)
                .Select(_ => Unit.Default)
                .InvokeCommand(ViewModel.CloseFile);

            // Set listview to sort by filename
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ViewModel.XMLProcessors);
            view.SortDescriptions.Add(new SortDescription(nameof(XMLProcessor.XMLFileName), ListSortDirection.Ascending));

            ViewModel.RemoveDD.IsExecuting
                .ObserveOnDispatcher()
                .Subscribe(executing => Cursor = executing ? Cursors.AppStarting : Cursors.Arrow);

            Focus();
        }

        private void ShowChildWindow(WindowType windowType)
        {
            switch (windowType)
            {
                case WindowType.Configuration:
                    new ConfigurationWindow(configViewModel) { Owner = this }.ShowDialog();
                    break;
                case WindowType.Help:
                    new HelpWindow { Owner = this }.Show();
                    break;
                case WindowType.ProcessingMessages:
                    ShowProcessingMessages();
                    break;
            }
        }

        // Get any status messages and show them in a new window
        private void ShowProcessingMessages()
        {
            FlowDocument statusMessagesFlowDoc = GenerateStatusMessagesFlowDocument();

            if (statusMessagesFlowDoc.Blocks.Count > 0)
            {
                Window messagesWindow = new Window
                {
                    Owner = this,
                    Title = "Processing Messages",
                    Height = 450,
                    Width = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new FlowDocumentScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Document = statusMessagesFlowDoc
                    }
                };

                // Activate the main window when the messages window is closed
                messagesWindow.Closing += (s, e) => Activate();

                messagesWindow.Show();
            }
        }

        private FlowDocument GenerateStatusMessagesFlowDocument()
        {
            FlowDocument doc = new FlowDocument();

            foreach (XMLProcessor xmlProcessor in ViewModel.XMLProcessors.Where(x => x.StatusMessages.Count > 0))
            {
                Paragraph title = new Paragraph
                {
                    FontSize = 20,
                    Margin = new Thickness(0, 10, 0, 5)
                };

                title.Inlines.Add(new Run(xmlProcessor.SongTitle));

                // Add arrangement type unless it is "N/A"
                if (!xmlProcessor.ArrangementType.StartsWith("N"))
                    title.Inlines.Add(new Run($" ({xmlProcessor.ArrangementType})"));

                Run xmlFile = new Run($" [{xmlProcessor.XMLFileName}]")
                {
                    FontSize = 13,
                    Foreground = Brushes.Gray
                };
                title.Inlines.Add(xmlFile);
                doc.Blocks.Add(title);

                foreach (var messageType in (MessageType[])Enum.GetValues(typeof(MessageType)))
                {
                    var messages = from message in xmlProcessor.StatusMessages
                                   where message.Type == messageType
                                   select message.Message;

                    if (messages.Any())
                    {
                        Paragraph messageListTitle = new Paragraph(new Run($"{messageType}s: "))
                        {
                            FontSize = 18,
                            FontWeight = FontWeight.FromOpenTypeWeight(600),
                            Margin = new Thickness(10, 0, 0, 0)
                        };
                        doc.Blocks.Add(messageListTitle);

                        List messageList = new List
                        {
                            Margin = new Thickness(15, 5, 0, 15)
                        };

                        foreach (string message in messages)
                        {
                            messageList.ListItems.Add(new ListItem(new Paragraph(new Run(message))));
                        }
                        doc.Blocks.Add(messageList);
                    }
                }
            }
            return doc;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = ViewModel.IsProcessingFiles && MessageBox.Show(
                "Processing is in progress.\n\nAre you sure you want to quit?",
                Title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No;
        }

        private void LogLink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string? logFilePath = (sender as TextBlock)?.Tag as string;

            if (File.Exists(logFilePath))
            {
                Window logWin = new Window
                {
                    Width = 700,
                    Height = 600,
                    Owner = this,
                    Title = logFilePath,
                    Content = new TextBox
                    {
                        FontFamily = new FontFamily("Consolas"),
                        IsReadOnly = true,
                        Text = File.ReadAllText(logFilePath),
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        //HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    }
                };

                // Activate the main window when the log window is closed
                logWin.Closing += (s, e) => Activate();

                TextOptions.SetTextFormattingMode(logWin, TextFormattingMode.Display);

                logWin.Show();

                e.Handled = true;
            }
        }

        #region Drag & Drop for ListView

        private void FilesList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private async void FilesList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] fileNames)
            {
                await ViewModel.AddFilesAsync(fileNames);

                e.Effects = DragDropEffects.Copy;
            }
        }

        #endregion

        private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Set to null if nothing is selected
            ViewModel.SelectedItems = (filesListView.SelectedIndex == -1) ? null : filesListView.SelectedItems;
        }
    }
}
