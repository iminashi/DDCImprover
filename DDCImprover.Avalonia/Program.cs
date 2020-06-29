using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;

namespace DDCImprover.Avalonia
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
            => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
#if MACOS && DEBUG
                // For testing on a macOS virtual machine
                .With(new AvaloniaNativePlatformOptions { UseGpu = false })
#endif
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToDebug();
    }
}
