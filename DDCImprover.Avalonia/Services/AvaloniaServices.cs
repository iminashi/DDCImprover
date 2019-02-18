using Avalonia.Controls;
using DDCImprover.Core.Services;
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

        public Task<string[]> OpenFileDialog(string title, FileFilter filter, bool multiSelect)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filters = GetFilters(filter),
                AllowMultiple = multiSelect,
            };

            return openFileDialog.ShowAsync(parentWindow);
        }

        public void NotifyUser(string message, string caption)
        {
            MessageBox messageBox = new MessageBox(message, caption);
            messageBox.Show();
        }

        private List<FileDialogFilter> GetFilters(FileFilter filter)
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
    }
}
