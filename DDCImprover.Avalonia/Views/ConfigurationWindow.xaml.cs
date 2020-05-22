using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using DDCImprover.Core.ViewModels;

using System;

namespace DDCImprover.Avalonia.Views
{
    public class ConfigurationWindow : Window
    {
        public static double ProcessorCount { get; set; } = Environment.ProcessorCount;

        public ConfigurationWindow()
        {
            InitializeComponent();
        }

        public ConfigurationWindow(ConfigurationWindowViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;

            this.FindControl<Button>("CloseButton").Click += (s, e) => Close();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
