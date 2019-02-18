using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DDCImprover.Core.ViewModels;

namespace DDCImprover.Avalonia.Views
{
    public class ConfigurationWindow : Window
    {
        /// <summary>
        /// Indicates whether the user clicked the 'Delete All Logs' button.
        /// </summary>
        public bool LogsCleared => viewModel.LogsCleared;

        public static double ProcessorCount { get; set; } = Environment.ProcessorCount;

        private readonly ConfigurationWindowViewModel viewModel;
        private readonly MainWindow parentWindow;

        public ConfigurationWindow(ConfigurationWindowViewModel viewModel, MainWindow parent)
        {
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif

            parentWindow = parent;
            parentWindow.ConfigWindowOpen = true;

            DataContext = this.viewModel = viewModel;

            this.viewModel.LogsCleared = false;

            this.FindControl<Button>("closeButton").Click += Close_Click;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void HandleClosed()
        {
            base.HandleClosed();

            parentWindow.ConfigWindowOpen = false;
        }
    }
}
