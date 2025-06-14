using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace PhpSwitcher.Models
{
    public class AppConfig
    {
        public bool FirstRun { get; set; } = true;
        public string PhpVersionsDirectory { get; set; } = Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") ?? "C:", "php-versions");
        public List<PhpVersion> AvailableVersions { get; set; } = new List<PhpVersion>();
        public List<InstalledPhpVersion> InstalledVersions { get; set; } = new List<InstalledPhpVersion>();
        public DateTime? LastUpdated { get; set; } = null;
        public bool ShowConsole { get; set; } = false;
        public bool DevMode { get; set; } = false;
        
        /// <summary>
        /// Indicates whether a background update of PHP versions is in progress.
        /// This is not serialized to JSON.
        /// </summary>
        [JsonIgnore]
        public bool IsBackgroundUpdateInProgress { get; set; } = false;
        
        /// <summary>
        /// Gets the active PHP version from the installed versions list.
        /// </summary>
        /// <returns>The active PHP version, or null if no version is active.</returns>
        public InstalledPhpVersion? GetActiveVersion()
        {
            return InstalledVersions.FirstOrDefault(v => v.IsActive);
        }
    }

    public class PhpVersion
    {
        public string Version { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool IsNTS { get; set; }
        public bool IsX64 { get; set; }
        public string DisplayName => $"PHP {Version} {(IsNTS ? "(Non-Thread Safe)" : "(Thread Safe)")} {(IsX64 ? "(x64)" : "(x86)")}";
        public string DownloadUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("last-patch")]
        public bool IsLastPatch { get; set; }
    }

    public class InstalledPhpVersion
    {
        public string Version { get; set; } = string.Empty;
        public bool IsNTS { get; set; }
        public bool IsX64 { get; set; }
        public string InstallPath { get; set; } = string.Empty;
        public DateTime InstallDate { get; set; }
        public bool IsActive { get; set; }
        
        public string DisplayName => $"PHP {Version} {(IsNTS ? "(Non-Thread Safe)" : "(Thread Safe)")} {(IsX64 ? "(x64)" : "(x86)")}";
    }
}