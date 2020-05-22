using DDCImprover.Core.Services;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DDCImprover.WPF
{
    internal sealed class WPFServices : IPlatformSpecificServices
    {
        private readonly Dictionary<FileFilter, string> FilterStrings = new Dictionary<FileFilter, string>
        {
            { FileFilter.DDCExecutable, "DDC Executable|ddc.exe" },
            { FileFilter.RSXmlFiles, "RS2014 XML Files|*RS2.xml|XML Files|*.xml" }
        };

        public async Task<string[]> OpenFileDialog(string title, FileFilter filter, bool multiSelect)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filter = FilterStrings[filter],
                Multiselect = multiSelect
            };

            bool? filesSelected = await Dispatcher.CurrentDispatcher.InvokeAsync(openFileDialog.ShowDialog);

            return filesSelected == true ? openFileDialog.FileNames : null;
        }

        public void NotifyUser(string message, string caption)
        {
            MessageBoxImage icon = (caption == "Error") ? MessageBoxImage.Error : MessageBoxImage.None;
            MessageBox.Show(message, caption, MessageBoxButton.OK, icon);
        }

        public void ExitApplication() => Application.Current.Shutdown();
    }
}
