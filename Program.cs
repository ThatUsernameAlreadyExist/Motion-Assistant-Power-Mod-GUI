using Avalonia;
using PmGui.Models;
using System;
using System.Collections.Generic;


namespace PmGui
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Check if arguments are provided
            if (args.Length == 0)
            {
                // No arguments - exit without running
                return;
            }
            
            // If "debug" argument, run normally
            // If any other argument, run in hidden mode
            bool isDebugMode = args.Length > 0 && args[0].Equals("debug", StringComparison.OrdinalIgnoreCase);
            
            if (isDebugMode)
            {
                // Run with normal desktop lifetime (window will be shown)
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            else
            {
                // Run in hidden mode - set a flag and handle window visibility in App
                Environment.SetEnvironmentVariable("MAPM_031125_HIDDEN_MODE", "true");
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            var options = new Win32PlatformOptions
            {
                CompositionMode = new List<Win32CompositionMode> { Win32CompositionMode.DirectComposition },
            };

            // Force software rendering if HW is disabled
            if (!GlobalSettings.Instance.UseHwRendering)
            {
                options.RenderingMode = new List<Win32RenderingMode> { Win32RenderingMode.Software };
            }

            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(options)
                .LogToTrace()
                .WithInterFont();
        }
    }
}