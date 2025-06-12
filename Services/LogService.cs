using System;
using System.IO;

namespace PhpSwitcher.Services
{
    public enum LogLevel
    {
        INFO,
        DEBUG,
        WARNING,
        ERROR
    }
    
    public class LogService
    {
        private static readonly string LogPath;
        
        static LogService()
        {
            string logsDir = ConfigService.GetLogsDirectory();
            LogPath = Path.Combine(logsDir, "php-switcher.log");
            
            // Initialize log file
            File.AppendAllText(LogPath, $"{DateTime.Now} - Initializing PHP Switcher\r\n");
        }
        
        public static void Log(string message, LogLevel level = LogLevel.INFO)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"{timestamp} [{level}] - {message}";
            
            File.AppendAllText(LogPath, logMessage + Environment.NewLine);
            
            // Also show in console if not information
            if (level != LogLevel.INFO)
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = level switch
                {
                    LogLevel.ERROR => ConsoleColor.Red,
                    LogLevel.WARNING => ConsoleColor.Yellow,
                    LogLevel.DEBUG => ConsoleColor.Cyan,
                    _ => ConsoleColor.White
                };
                
                Console.WriteLine(logMessage);
                Console.ForegroundColor = originalColor;
            }
        }
        
        public static void LogCriticalError(string message, Exception? exception = null)
        {
            Log($"CRITICAL ERROR: {message}", LogLevel.ERROR);
            
            if (exception != null)
            {
                Log($"Exception type: {exception.GetType().FullName}", LogLevel.ERROR);
                Log($"Exception message: {exception.Message}", LogLevel.ERROR);
                Log($"StackTrace: {exception.StackTrace}", LogLevel.ERROR);
                
                // Log additional information about the error
                Log("Additional error information:", LogLevel.ERROR);
                Log($"Source: {exception.Source}", LogLevel.ERROR);
                
                // Log system information
                Log("System information:", LogLevel.ERROR);
                Log($"OS: {Environment.OSVersion}", LogLevel.ERROR);
                Log($"64-bit OS: {Environment.Is64BitOperatingSystem}", LogLevel.ERROR);
                Log($"64-bit process: {Environment.Is64BitProcess}", LogLevel.ERROR);
                Log($"Culture: {System.Threading.Thread.CurrentThread.CurrentCulture.Name}", LogLevel.ERROR);
            }
            
            // Log the path of the log file for reference
            Console.WriteLine($"Detailed error information has been logged to: {LogPath}");
        }
    }
}