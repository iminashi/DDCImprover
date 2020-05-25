using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DDCImprover.Core
{
    public sealed class Program
    {
        private static string? version;

        public static string Version
        {
            get
            {
                if (version is string)
                    return version;

                var thisAsm = Assembly.GetExecutingAssembly();
                var ver = thisAsm.GetName().Version;
                version = $"v{ver.Major}.{ver.Minor}.{ver.Build}";

                return version;
            }
        }

        public static string Title { get; } = "DDC Improver " + Version;
        public static string AppDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory;
        public static string AppDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ddc-improver");
        public static string DDCExecutablePath { get; } = Path.Combine(AppDirectory, "ddc", "ddc.exe");
        public static string LogDirectory { get; } = Path.Combine(AppDataPath, "logs");
        public static string ConfigFileName { get; } = Path.Combine(AppDataPath, "config.xml");

        public static bool UseWine { get; } = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private static string? _wineExecutable;
        public static string GetWineExecutable()
        {
            if (_wineExecutable is string)
                return _wineExecutable;

            try
            {
                using Process which = new Process();

                which.StartInfo.UseShellExecute = false;
                which.StartInfo.FileName = "/usr/bin/which";
                which.StartInfo.Arguments = "wine";
                which.StartInfo.RedirectStandardOutput = true;

                which.Start();
                which.WaitForExit();

                string result = which.StandardOutput.ReadToEnd();

                if (!string.IsNullOrEmpty(result))
                    return _wineExecutable = result;
                else
                    return _wineExecutable = "/usr/local/bin/wine";
            }
            catch (Exception)
            {
                return _wineExecutable = "wine";
            }
        }
    }
}
