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
                // Try to load from embedded resources
                return Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
                // Return a default system icon if the application icon cannot be loaded
                return SystemIcons.Application;
            }
        }
        
        // Get the PHP logo for the splash screen
        public static Image? GetPhpLogo()
        {
            try
            {
                // Try to load from embedded resources or file
                string? assemblyLocation = Assembly.GetExecutingAssembly().Location;
                string? directoryName = Path.GetDirectoryName(assemblyLocation);
                
                if (directoryName != null)
                {
                    string logoPath = Path.Combine(
                        directoryName,
                        "Resources", "php-logo.png");
                    
                    if (File.Exists(logoPath))
                    {
                        return Image.FromFile(logoPath);
                    }
                }
                
                // Return null if the logo cannot be found
                return null;
            }
            catch
            {
                // Return null if the logo cannot be loaded
                return null;
            }
        }
    }
}