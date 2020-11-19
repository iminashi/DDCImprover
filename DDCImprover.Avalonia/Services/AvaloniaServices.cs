using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using DDCImprover.ViewModels.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DDCImprover.Avalonia
{
    internal sealed class AvaloniaServices : IPlatformSpecificServices
    {
        private readonly Window parentWindow;

        public AvaloniaServices(Window parent)
        {
            parentWindow = parent;
        }

        public async Task<string[]> OpenFileDialog(string title, FileFilter filter, bool multiSelect)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filters = GetFilters(filter),
                AllowMultiple = multiSelect,
            };

            return await openFileDialog.ShowAsync(parentWindow);
        }

        public void NotifyUser(string message, string caption)
        {
            MessageBox messageBox = new MessageBox(message, caption, parentWindow);
            messageBox.Show();
        }

        private static List<FileDialogFilter> GetFilters(FileFilter filter)
        {
            var filters = new List<FileDialogFilter>();

            switch (filter)
            {
                case FileFilter.DDCExecutable:
                    filters.Add(new FileDialogFilter { Name = "Executable Files", Extensions = new List<string> { "exe" } });
                    break;

                case FileFilter.RSXmlFiles:
                    filters.Add(new FileDialogFilter { Name = "XML Files", Extensions = new List<string> { "xml" } });
                    break;

                default:
                    throw new InvalidOperationException("Unknown file filter.");
            }

            return filters;
        }

        public void ExitApplication()
            => (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.Shutdown();
    }
}
