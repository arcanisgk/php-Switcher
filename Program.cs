using System;
using System.Windows.Forms;
using PhpSwitcher.Forms;
using PhpSwitcher.Services;
using PhpSwitcher.Utils;

namespace PhpSwitcher
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Parse command line arguments
                bool showConsole = args.Contains("--show-console") || args.Contains("-c");
                bool devMode = args.Contains("--dev") || args.Contains("-d");
                
                // Initialize logging
                LogService.Log("Application starting...", LogLevel.INFO);
                LogService.Log($"Command line arguments: {string.Join(" ", args)}", LogLevel.DEBUG);
                LogService.Log($"Show console: {showConsole}, Dev mode: {devMode}", LogLevel.DEBUG);
                
                // Check for administrator privileges
                if (!AdminUtils.IsRunningAsAdmin())
                {
                    LogService.Log("Application is not running with administrative privileges. Restarting...", LogLevel.WARNING);
                    AdminUtils.RestartAsAdmin(showConsole, devMode);
                    return;
                }
                
                LogService.Log("Running with administrative privileges.", LogLevel.INFO);
                LogService.Log($"Elevation status: {AdminUtils.GetElevationStatus()}", LogLevel.DEBUG);
                
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                
                // Load configuration
                var config = ConfigService.LoadConfiguration();
                
                // Update configuration with command line arguments
                config.ShowConsole = showConsole;
                config.DevMode = devMode;
                ConfigService.SaveConfiguration(config);
                
                // Start the application with the splash screen
                Application.Run(new SplashForm());
            }
            catch (Exception ex)
            {
                LogService.LogCriticalError("Unhandled exception in Main", ex);
                MessageBox.Show(
                    $"A critical error occurred: {ex.Message}\n\nPlease check the logs for more details.",
                    "PHP Switcher - Critical Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}