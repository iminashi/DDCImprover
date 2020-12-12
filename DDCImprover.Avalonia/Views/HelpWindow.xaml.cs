using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DDCImprover.Avalonia.Views
{
    public class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
