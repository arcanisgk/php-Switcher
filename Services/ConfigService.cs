using System;
using System.IO;
using System.Text.Json;
using PhpSwitcher.Models;

namespace PhpSwitcher.Services
{
    public class ConfigService
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PHPSwitcher");
        
        private static readonly string ConfigFile = Path.Combine(AppDataPath, "config.json");
        
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        
        public static AppConfig LoadConfiguration()
        {
            InitializeAppDirectories();
            
            if (File.Exists(ConfigFile))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFile);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                    if (config != null)
                    {
                        Console.WriteLine("Configuration loaded successfully.");
                        return config;
                    }
                    else
                    {
                        Console.WriteLine("Configuration deserialized as null. Using default configuration.");
                        return new AppConfig();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading configuration: {ex.Message}");
                    Console.WriteLine("Using default configuration.");
                    return new AppConfig();
                }
            }
            else
            {
                Console.WriteLine("No configuration file found. Using default configuration.");
                return new AppConfig();
            }
        }
        
        public static void SaveConfiguration(AppConfig config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(ConfigFile, json);
                Console.WriteLine("Configuration saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }
        
        private static void InitializeAppDirectories()
        {
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
                Console.WriteLine($"Created application data directory: {AppDataPath}");
            }
        }
        
        public static string GetLogsDirectory()
        {
            string logsDir = Path.Combine(AppDataPath, "logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            return logsDir;
        }
    }
}