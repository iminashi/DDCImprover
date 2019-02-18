using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using DDCImprover.Avalonia.Views;

namespace DDCImprover.Avalonia
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            BuildAvaloniaApp().Start<MainWindow>();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToDebug();
    }
}
