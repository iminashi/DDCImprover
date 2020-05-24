using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DDCImprover.Avalonia
{
    public class MessageBox : Window
    {
        public MessageBox()
        {
            InitializeComponent();
        }

        public MessageBox(string message, string caption, Window parent)
        {
            InitializeComponent();

            Owner = parent;
            Title = caption;
            this.FindControl<TextBlock>("messageText").Text = message;
            this.FindControl<Button>("okButton").Click += (s, e) => Close();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
