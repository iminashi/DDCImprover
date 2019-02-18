using System.Windows;

using DDCImprover.Core.ViewModels;

namespace DDCImprover.WPF
{
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        /// <summary>
        /// Indicates whether the user clicked the 'Delete All Logs' button.
        /// </summary>
        public bool LogsCleared => ViewModel.LogsCleared;

        private readonly ConfigurationWindowViewModel ViewModel;

        public ConfigurationWindow(ConfigurationWindowViewModel viewModel)
        {
            InitializeComponent();

            DataContext = ViewModel = viewModel;

            ViewModel.LogsCleared = false;
        }
    }
}
