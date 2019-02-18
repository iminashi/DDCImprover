﻿using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Layout;
using DDCImprover.Core;
using DDCImprover.Core.ViewModels;
using ReactiveUI;
using System.Reactive;

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

        /// <summary>
        /// Workaround for ShowDialog freezing on Mac.
        /// </summary>
        public bool ConfigWindowOpen { get; set; }

        private readonly ConfigurationWindowViewModel configViewModel;
        private readonly AvaloniaServices services;
        private ConfigurationWindow configWindow;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public MainWindow()
        {
            InitializeComponent();

            services = new AvaloniaServices(this);

            DataContext = ViewModel = new MainWindowViewModel(services);

            configViewModel = new ConfigurationWindowViewModel(services, XMLProcessor.Preferences);

            // Change mouse cursor when processing files
            this.WhenAnyValue(x => x.ViewModel.IsProcessingFiles)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(processing => Cursor = processing ? new Cursor(StandardCursorType.AppStarting) : new Cursor(StandardCursorType.Arrow));

            // Reset "view log" text if user clears log files
            this.WhenAnyValue(x => x.configViewModel.LogsCleared)
                .Where(cleared => cleared)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => ViewModel.RemoveViewLogTexts());

            // Display processing messages when needed
            ViewModel.ShouldDisplayProcessingMessages
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => ShowProcessingMessages());

            // Check DDC executable when window is activated
            Observable.FromEventPattern<EventArgs>(this, "Activated")
                .Take(1) // Do only once
                .Subscribe(async _ =>
                {
                    var result = await ViewModel.CheckDDCExecutable();
                    if (result == DDCExecutableCheckResult.LocationChanged)
                        configViewModel.EnumerateDDCSettings();
                });

#if DEBUG
            this.AttachDevTools();
#endif
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if(e.Modifiers == InputModifiers.Control)
            {
                switch(e.Key)
                {
                    case Key.O:
                        Observable.Return(/*addingFiles:*/ false).InvokeCommand(ViewModel.OpenFiles);
                        break;
                    case Key.P:
                        Observable.Return(Unit.Default).InvokeCommand(ViewModel.Process);
                        break;
                    case Key.A:
                        Observable.Return(Unit.Default).InvokeCommand(ViewModel.AddFiles);
                        break;
                }
            }
            else if(e.Modifiers == InputModifiers.None)
            {
                if(e.Key == Key.F5)
                {
                    Observable.Return(Unit.Default).InvokeCommand(ViewModel.Process);
                }
                else if(e.Key == Key.F1)
                {
                    Help_Click(this, new RoutedEventArgs());
                }
            }
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
                            BorderThickness = 0.0,
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

        /*private string GenerateStatusMessageString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (XMLProcessor xmlProcessor in ViewModel.XMLProcessors.Where(x => x.StatusMessages.Count > 0))
            {
                stringBuilder.Append(xmlProcessor.SongTitle);

                // Add arrangement type unless it is "N/A"
                if (!xmlProcessor.ArrangementType.StartsWith("N"))
                    stringBuilder.Append(" (").Append(xmlProcessor.ArrangementType).Append(")");

                // Add [XML Filename]
                stringBuilder.Append(" [").Append(xmlProcessor.XMLFileName).Append("]").AppendLine(Environment.NewLine); // Add two newlines

                foreach (MessageType messageType in Enum.GetValues(typeof(MessageType)))
                {
                    var messages = from message in xmlProcessor.StatusMessages
                                   where message.Type == messageType
                                   select message.Message;

                    if (messages.Any())
                    {
                        stringBuilder.Append(messageType).AppendLine("s: ");

                        foreach (string message in messages)
                        {
                            stringBuilder.Append("• ").AppendLine(message);
                        }
                    }
                }
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }*/

        private void ShowProcessingMessages()
        {
            Window messagesWindow = new Window
            {
                Title = "Processing Messages",
                Icon = this.Icon,
                Height = 450,
                Width = 700,
                Content = GenerateStatusMessage()/*new TextBox
                {
                    Text = GenerateStatusMessageString(),
                    IsReadOnly = true
                }*/
            };
            messagesWindow.Show();
        }

#pragma warning disable RCS1213 // Remove unused member declaration.
        private void Exit_Click(object sender, RoutedEventArgs e)
            => Application.Current.Exit();

        private void Configuration_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigWindowOpen)
            {
                configWindow.Activate();
                return;
            }

            configWindow = new ConfigurationWindow(configViewModel, this);

            configWindow.Show();
        }
#pragma warning restore RCS1213 // Remove unused member declaration.

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();

            helpWindow.Show();
        }

        // Save configuration on exit.
        protected override void HandleClosed()
        {
            base.HandleClosed();

            XMLProcessor.Preferences.Save();
        }
    }
}
