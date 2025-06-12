using System;
using System.Diagnostics;
using System.Security.Principal;
using PhpSwitcher.Services;

namespace PhpSwitcher.Utils
{
    public static class AdminUtils
    {
        public static bool IsRunningAsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        
        public static void RestartAsAdmin(bool showConsole = false, bool devMode = false)
        {
            LogService.Log("Restarting application with administrative privileges...", LogLevel.WARNING);
            
            try
            {
                string arguments = "";
                
                if (showConsole)
                {
                    arguments += " --show-console";
                }
                
                if (devMode)
                {
                    arguments += " --dev";
                }
                
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Process.GetCurrentProcess().MainModule?.FileName ?? Application.ExecutablePath,
                    Arguments = arguments,
                    Verb = "runas" // This triggers the UAC prompt
                };
                
                Process.Start(startInfo);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                LogService.LogCriticalError("Failed to restart with administrative privileges", ex);
                throw;
            }
        }
        
        public static string GetElevationStatus()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                return "Elevated (Administrator)";
            }
            else
            {
                return "Not elevated (Standard user)";
            }
        }
    }
}