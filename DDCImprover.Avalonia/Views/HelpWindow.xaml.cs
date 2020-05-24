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
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
