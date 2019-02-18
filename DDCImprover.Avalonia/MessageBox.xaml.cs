﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DDCImprover.Avalonia
{
    public class MessageBox : Window
    {
        public MessageBox(string message, string caption)
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            Title = caption;
            this.FindControl<TextBlock>("messageText").Text = message;
            this.FindControl<Button>("okButton").Click += (s, e) => Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
