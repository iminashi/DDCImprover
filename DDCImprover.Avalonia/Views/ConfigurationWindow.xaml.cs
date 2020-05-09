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
        public static double ProcessorCount { get; set; } = Environment.ProcessorCount;

        private readonly MainWindow parentWindow;

        public ConfigurationWindow()
        {
            InitializeComponent();
        }

        public ConfigurationWindow(ConfigurationWindowViewModel viewModel, MainWindow parent)
        {
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif

            parentWindow = parent;
            parentWindow.ConfigWindowOpen = true;

            DataContext = viewModel;

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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            parentWindow.ConfigWindowOpen = false;
        }
    }
}
