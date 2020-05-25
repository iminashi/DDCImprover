using DDCImprover.Core;

using System.Windows;
using System.Windows.Documents;

namespace DDCImprover.WPF
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private void FlowDocHyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is Hyperlink hyperlink)
            {
                hyperlink.NavigateUri.OriginalString.StartAsProcess();
                e.Handled = true;
            }
        }
    }
}
