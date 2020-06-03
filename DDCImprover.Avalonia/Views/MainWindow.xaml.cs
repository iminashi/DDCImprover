using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using DDCImprover.Core;
using DDCImprover.Core.Services;
using DDCImprover.Core.ViewModels;

using ReactiveUI;

using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.

namespace DDCImprover.Avalonia.Views
{
    public class MainWindow : Window, IViewFor<MainWindowViewModel>
    {
        public MainWindowViewModel ViewModel { get; set; }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MainWindowViewModel)value;
        }

        private readonly ConfigurationWindowViewModel configViewModel;
        private readonly AvaloniaServices services;

        public MainWindow()
        {
            InitializeComponent();

            services = new AvaloniaServices(this);
            configViewModel = new ConfigurationWindowViewModel();
            DataContext = ViewModel = new MainWindowViewModel(services, configViewModel);

            // Change mouse cursor when processing files
            this.WhenAnyValue(x => x.ViewModel.IsProcessingFiles)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(processing => Cursor = processing ? new Cursor(StandardCursorType.AppStarting) : new Cursor(StandardCursorType.Arrow));

            // Show child windows when needed
            ViewModel.OpenChildWindow
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async type => await ShowChildWindow(type));

            this.FindControl<ListBox>("listBox").SelectionChanged += (s, arg) => ViewModel.SelectedItems = (s as ListBox)!.SelectedItems;

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private async Task ShowChildWindow(WindowType windowType)
        {
            switch (windowType)
            {
                case WindowType.Configuration:
                    ConfigurationWindow configWindow = new ConfigurationWindow(configViewModel);

                    await configWindow.ShowDialog(this);
                    break;
                case WindowType.Help:
                    new HelpWindow().Show();
                    break;
                case WindowType.ProcessingMessages:
                    ShowProcessingMessages();
                    break;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyModifiers == KeyModifiers.None)
            {
                if (e.Key == Key.F5)
                    Observable.Return(Unit.Default).InvokeCommand(ViewModel.ProcessFiles);
                else if (e.Key == Key.F1)
                    _ = ShowChildWindow(WindowType.Help);
            }
        }

        private void ShowProcessingMessages()
        {
            Window messagesWindow = new Window
            {
                Title = "Processing Messages",
                Icon = this.Icon,
                Height = 450,
                Width = 700,
                Content = GenerateStatusMessage()
            };
            messagesWindow.Show();
        }

        private ScrollViewer GenerateStatusMessage()
        {
            StackPanel mainPanel = new StackPanel();
            ScrollViewer viewer = new ScrollViewer
            {
                Content = mainPanel
            };

            foreach (XMLProcessor xmlProcessor in ViewModel.XMLProcessors.Where(x => x.StatusMessages.Count > 0))
            {
                StackPanel titlePanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5, 5, 0, 5)
                };

                TextBlock title = new TextBlock
                {
                    Text = xmlProcessor.SongTitle,
                    FontSize = 20,
                    VerticalAlignment = VerticalAlignment.Center
                };
                titlePanel.Children.Add(title);

                // Add arrangement type unless it is "N/A"
                if (!xmlProcessor.ArrangementType.StartsWith("N"))
                {
                    TextBlock arrangement = new TextBlock
                    {
                        FontSize = 20,
                        Text = $" ({xmlProcessor.ArrangementType})",
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    titlePanel.Children.Add(arrangement);
                }

                TextBlock filename = new TextBlock
                {
                    Text = $" [{xmlProcessor.XMLFileName}]",
                    FontSize = 13,
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center
                };

                titlePanel.Children.Add(filename);
                mainPanel.Children.Add(titlePanel);

                foreach (MessageType messageType in Enum.GetValues(typeof(MessageType)))
                {
                    var messages = from message in xmlProcessor.StatusMessages
                                   where message.Type == messageType
                                   select message.Message;

                    if (messages.Any())
                    {
                        TextBlock messageListTitle = new TextBlock
                        {
                            Text = $"{messageType}s: ",
                            FontWeight = FontWeight.Bold,
                            FontSize = 18,
                            Margin = new Thickness(10, 0, 0, 0)
                        };

                        mainPanel.Children.Add(messageListTitle);

                        TextBox txtBox = new TextBox
                        {
                            IsReadOnly = true,
                            BorderBrush = null,
                            BorderThickness = new Thickness(0.0),
                            Margin = new Thickness(15, 5, 0, 10),
                            FontSize = 16.0
                        };

                        foreach (string message in messages)
                        {
                            txtBox.Text += $"• {message}{Environment.NewLine}";
                        }

                        mainPanel.Children.Add(txtBox);
                    }
                }
            }

            return viewer;
        }

#pragma warning disable RCS1213 // Remove unused member declaration.

        private void LogLink_MouseButtonUp(object sender, PointerReleasedEventArgs e)
        {
            string? logFilePath = (sender as TextBlock)?.Tag as string;

            if (File.Exists(logFilePath))
            {
                Window logWin = new Window
                {
                    Width = 700,
                    Height = 600,
                    Title = logFilePath,
                    Content = new TextBox
                    {
                        BorderThickness = new Thickness(0.0),
                        IsReadOnly = true,
                        Text = File.ReadAllText(logFilePath),
                    }
                };

                logWin.Show();

                e.Handled = true;
            }
        }
    }
}
