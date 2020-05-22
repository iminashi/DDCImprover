using System;
using System.Threading.Tasks;

namespace DDCImprover.Core.Services
{
    public interface IPlatformSpecificServices
    {
        Task<string[]> OpenFileDialog(string title, FileFilter filter, bool multiSelect);

        void NotifyUser(string message, string caption);

        void ExitApplication();
    }
}
