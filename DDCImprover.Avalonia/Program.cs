using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;

namespace DDCImprover.Avalonia
{
    internal static class Program
    {
        private static void Main(string[] args)
            => BuildAvaloniaApp()
            // For testing on a macOS virtual machine
            .With(new AvaloniaNativePlatformOptions { UseGpu = false })
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToDebug();
    }
}
