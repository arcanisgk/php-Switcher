using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace PhpSwitcher.Resources
{
    public static class AppResources
    {
        // Default icon for the application
        public static Icon GetApplicationIcon()
        {
            try
            {
                // First try to load from the output directory (works in both debug and release)
                string iconPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources", "favicon.ico");
                
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
                
                // If that fails, try to extract from the executable itself
                // This should work if the ApplicationIcon property is set in the .csproj file
                string executablePath = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath))
                {
                    return Icon.ExtractAssociatedIcon(executablePath);
                }
                
                // If all else fails, use a system icon
                Console.WriteLine("Warning: Could not load application icon from any source. Using system icon.");
                return SystemIcons.Application;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading application icon: {ex.Message}");
                // Return a default system icon if the application icon cannot be loaded
                return SystemIcons.Application;
            }
        }
        
        // Get the PHP logo for the splash screen
        public static Image? GetPhpLogo()
        {
            try
            {
                // Try to load from the output directory (works in both debug and release)
                string logoPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources", "icon.png");
                
                if (File.Exists(logoPath))
                {
                    // Use FromFile with copy option to avoid file locking
                    return Image.FromFile(logoPath);
                }
                
                // If the file doesn't exist in the expected location, log a warning
                Console.WriteLine($"Warning: PHP logo not found at {logoPath}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading PHP logo: {ex.Message}");
                // Return null if the logo cannot be loaded
                return null;
            }
        }
    }
}