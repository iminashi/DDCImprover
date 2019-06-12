using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

#pragma warning disable RCS1213 // Remove unused member declaration.

namespace DDCImprover.Avalonia
{
    /// <summary>
    /// Workaround to get event handler working in data template.
    /// </summary>
    internal class EventStackPanel : StackPanel
    {
        private void LogLink_MouseButtonUp(object sender, PointerReleasedEventArgs e)
        {
            string logFilePath = (sender as TextBlock)?.Tag as string;

            if (File.Exists(logFilePath))
            {
                Window logWin = new Window
                {
                    Width = 700,
                    Height = 600,
                    Title = logFilePath,
                    Content = new TextBox
                    {
                        BorderThickness = new Thickness(0.0),
                        IsReadOnly = true,
                        Text = File.ReadAllText(logFilePath),
                    }
                };

                logWin.Show();

                e.Handled = true;
            }
        }
    }
}
