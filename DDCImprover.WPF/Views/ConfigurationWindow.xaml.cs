using DDCImprover.Core;
using DDCImprover.Core.ViewModels;

using System;
using System.Windows;

namespace DDCImprover.WPF
{
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        public ConfigurationWindow(ConfigurationWindowViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }

        // Save configuration when the window is closed
        protected override void OnClosed(EventArgs e)
        {
            XMLProcessor.Preferences.Save();

            base.OnClosed(e);
        }
    }
}
