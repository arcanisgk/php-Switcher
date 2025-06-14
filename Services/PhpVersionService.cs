using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PhpSwitcher.Models;

namespace PhpSwitcher.Services
{
    public class PhpVersionService
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        static PhpVersionService()
        {
            // Add a user agent to avoid being blocked
            HttpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public static bool IsVersionInstalled(AppConfig config, string version, bool isNTS, bool? isX64 = null)
        {
            // Check if the version exists in the InstalledVersions list
            if (config.InstalledVersions != null && config.InstalledVersions.Count > 0)
            {
                foreach (var installedVersion in config.InstalledVersions)
                {
                    if (installedVersion.Version == version && installedVersion.IsNTS == isNTS)
                    {
                        // If architecture is specified, check it too
                        if (isX64.HasValue)
                        {
                            if (installedVersion.IsX64 == isX64.Value)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            // If architecture is not specified, just match version and thread safety
                            return true;
                        }
                    }
                }
            }

            // Also check if the directory exists in the PHP versions directory
            string versionDir = Path.Combine(config.PhpVersionsDirectory, $"php-{version}{(isNTS ? "-nts" : "")}");
            if (Directory.Exists(versionDir))
            {
                // If architecture is not specified, just check if directory exists
                if (!isX64.HasValue)
                {
                    return true;
                }
                
                // If architecture is specified, check if php.exe is the right architecture
                string phpExePath = Path.Combine(versionDir, "php.exe");
                if (File.Exists(phpExePath))
                {
                    try
                    {
                        using (var stream = new FileStream(phpExePath, FileMode.Open, FileAccess.Read))
                        {
                            stream.Seek(0x3C, SeekOrigin.Begin);
                            var peOffset = new byte[4];
                            stream.Read(peOffset, 0, 4);
                            var offset = BitConverter.ToInt32(peOffset, 0);
                            
                            stream.Seek(offset + 4, SeekOrigin.Begin);
                            var machineType = new byte[2];
                            stream.Read(machineType, 0, 2);
                            
                            // 0x8664 is the machine type for x64
                            bool fileIsX64 = BitConverter.ToUInt16(machineType, 0) == 0x8664;
                            
                            return fileIsX64 == isX64.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Log($"Error determining architecture for PHP {version}: {ex.Message}. Assuming not installed.", LogLevel.WARNING);
                        return false;
                    }
                }
            }

            return false;
        }

        public static bool TestInternetConnection()
        {
            try
            {
                // First try with Google, which is more reliable
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Head, "https://www.google.com");
                    using var response = HttpClient.Send(request);
                    response.EnsureSuccessStatusCode();
                    LogService.Log("Internet connection verified successfully (Google)", LogLevel.DEBUG);
                    return true;
                }
                catch (Exception ex)
                {
                    LogService.Log($"Failed to connect to Google: {ex.Message}", LogLevel.WARNING);
                    // Continue with other methods
                }

                // Try with windows.php.net
                try
                {
                    using var ping = new System.Net.NetworkInformation.Ping();
                    var result = ping.Send("windows.php.net");
                    if (result.Status == System.Net.NetworkInformation.IPStatus.Success)
                    {
                        LogService.Log("Internet connection verified successfully (windows.php.net)", LogLevel.DEBUG);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogService.Log($"Failed to connect to windows.php.net: {ex.Message}", LogLevel.WARNING);
                }

                // Try with another reliable site
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Head, "https://www.microsoft.com");
                    using var response = HttpClient.Send(request);
                    response.EnsureSuccessStatusCode();
                    LogService.Log("Internet connection verified successfully (Microsoft)", LogLevel.DEBUG);
                    return true;
                }
                catch (Exception ex)
                {
                    LogService.Log($"Failed to connect to Microsoft: {ex.Message}", LogLevel.WARNING);
                }

                // If we get here, all attempts failed
                LogService.Log("All internet connection tests failed", LogLevel.ERROR);
                return false;
            }
            catch (Exception ex)
            {
                LogService.Log($"Error in TestInternetConnection: {ex.Message}", LogLevel.ERROR);
                return false;
            }
        }

        public static async Task<List<PhpVersion>?> FetchAvailablePhpVersionsAsync()
        {
            LogService.Log("Verifying internet connection...", LogLevel.DEBUG);

            // Check internet connection before attempting to fetch versions
            if (!TestInternetConnection())
            {
                LogService.Log("No internet connection. Please check your connection and try again.", LogLevel.ERROR);
                return null;
            }

            LogService.Log("Fetching available PHP versions from windows.php.net...", LogLevel.DEBUG);

            try
            {
                // Fetch from the archives (contains all versions including the latest ones)
                string archivesUrl = "https://windows.php.net/downloads/releases/archives/";
                LogService.Log($"Fetching content from archives: {archivesUrl}", LogLevel.DEBUG);

                // Create a cancellation token with a timeout to prevent hanging
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                // Get the response from archives with timeout
                string response;
                try
                {
                    response = await HttpClient.GetStringAsync(archivesUrl, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    LogService.Log("Request to windows.php.net timed out after 30 seconds.", LogLevel.ERROR);
                    return null;
                }

                LogService.Log($"Archives response received. Length: {response.Length} characters", LogLevel.DEBUG);

                // Verify that the response is not empty
                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new Exception("Received empty response from windows.php.net");
                }

                // Save the raw HTML response to a file for inspection (only in debug mode)
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    string responseFilePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "PHPSwitcher", "response.html");
                    File.WriteAllText(responseFilePath, response);
                    LogService.Log($"Raw response saved to {responseFilePath} for inspection", LogLevel.DEBUG);
                }

                // Parse the HTML response to extract PHP version links
                List<PhpVersion> phpVersions = new List<PhpVersion>();
                Dictionary<string, bool> uniqueVersions = new Dictionary<string, bool>();  // Dictionary to track unique versions
                Dictionary<string, int> minorVersions = new Dictionary<string, int>();   // Dictionary to track latest patch for each minor version

                // Regular expression to match PHP version zip files for all formats
                // This comprehensive pattern matches:
                // - Both NTS and TS versions
                // - All compiler versions (VC6, VC9, VC11, VC14, VC15, vs16, vs17, etc.)
                // - Both x64 and x86 architectures
                // - All PHP versions including 5.x, 7.x, and 8.x
                string pattern = @"(php-(\d+\.\d+\.\d+)(?:-nts)?-Win32(?:-(?:VC\d+|vc\d+|vs\d+|VS\d+))?(?:-(?:x64|x86))?\.zip)";

                LogService.Log($"Searching for pattern: {pattern}", LogLevel.DEBUG);

                // Use a more efficient regex approach with compiled regex
                var regex = new Regex(pattern, RegexOptions.Compiled);
                var matches = regex.Matches(response);

                LogService.Log($"Raw matches found: {matches.Count}", LogLevel.DEBUG);

                // Verify that matches were found
                if (matches.Count == 0)
                {
                    // Try with an alternative pattern that includes href attribute
                    string alternativePattern = @"href=""[^""]*/(php-(\d+\.\d+\.\d+)(?:-nts)?-Win32(?:-(?:VC\d+|vc\d+|vs\d+|VS\d+))?(?:-(?:x64|x86))?\.zip)""";
                    LogService.Log($"No matches found with primary pattern. Trying alternative pattern: {alternativePattern}", LogLevel.WARNING);
                    regex = new Regex(alternativePattern, RegexOptions.Compiled);
                    matches = regex.Matches(response);
                    LogService.Log($"Alternative pattern matches found: {matches.Count}", LogLevel.WARNING);

                    if (matches.Count == 0)
                    {
                        throw new Exception("No PHP versions found in the response. The website structure may have changed.");
                    }
                }

                // Process matches in parallel for better performance
                var minorVersionsLock = new object();
                
                // First pass: identify the latest patch for each minor version
                Parallel.ForEach(matches.Cast<Match>(), match =>
                {
                    string version = match.Groups[2].Value;
                    string[] versionParts = version.Split('.');
                    string minorVersion = $"{versionParts[0]}.{versionParts[1]}";
                    int patchVersion = int.Parse(versionParts[2]);

                    lock (minorVersionsLock)
                    {
                        if (!minorVersions.ContainsKey(minorVersion) || patchVersion > minorVersions[minorVersion])
                        {
                            minorVersions[minorVersion] = patchVersion;
                        }
                    }
                });

                LogService.Log("Identified latest patches for minor versions:", LogLevel.DEBUG);
                foreach (var minorVersion in minorVersions.Keys)
                {
                    LogService.Log($"  {minorVersion} -> {minorVersions[minorVersion]}", LogLevel.DEBUG);
                }

                var phpVersionsLock = new object();
                var uniqueVersionsLock = new object();
                
                // Second pass: create version objects with last-patch property
                Parallel.ForEach(matches.Cast<Match>(), match =>
                {
                    string filename = match.Groups[1].Value;
                    string version = match.Groups[2].Value;
                    bool isNts = filename.Contains("-nts");
                    bool isX64 = filename.Contains("-x64");
                    bool isX86 = filename.Contains("-x86") || (!isX64 && !filename.Contains("-x"));

                    // Create a unique key that includes architecture information
                    string uniqueKey = $"{version}-{isNts}-{(isX64 ? "x64" : "x86")}";

                    // Process the version
                    string[] versionParts = version.Split('.');
                    string minorVersion = $"{versionParts[0]}.{versionParts[1]}";
                    int patchVersion = int.Parse(versionParts[2]);

                    // Check if this is the latest patch for its minor version
                    bool isLatestPatch;
                    lock (minorVersionsLock)
                    {
                        isLatestPatch = (patchVersion == minorVersions[minorVersion]);
                    }

                    // All versions are downloaded from the archives URL
                    string downloadUrl = $"https://windows.php.net/downloads/releases/archives/{filename}";

                    var versionInfo = new PhpVersion
                    {
                        Version = version,
                        FileName = filename,
                        IsNTS = isNts,
                        IsX64 = isX64,
                        DownloadUrl = downloadUrl,
                        IsLastPatch = isLatestPatch
                    };

                    lock (phpVersionsLock)
                    {
                        // Check if this exact version (including architecture) is already in the list
                        bool alreadyExists;
                        lock (uniqueVersionsLock)
                        {
                            alreadyExists = uniqueVersions.ContainsKey(uniqueKey);
                        }

                        if (!alreadyExists)
                        {
                            phpVersions.Add(versionInfo);
                            
                            lock (uniqueVersionsLock)
                            {
                                uniqueVersions[uniqueKey] = true;
                            }
                            
                            LogService.Log($"Added version: {versionInfo.DisplayName} ({(isX64 ? "x64" : "x86")}, last-patch: {isLatestPatch})", LogLevel.DEBUG);
                        }
                    }
                });

                // Sort versions by version number (descending)
                phpVersions = phpVersions.OrderByDescending(v =>
                {
                    string[] versionParts = v.Version.Split('.');
                    return int.Parse(versionParts[0]) * 10000 + int.Parse(versionParts[1]) * 100 + int.Parse(versionParts[2]);
                }).ToList();

                LogService.Log($"Sorted versions. Total count: {phpVersions.Count}", LogLevel.DEBUG);

                // Count versions with last-patch = true
                int latestPatchCount = phpVersions.Count(v => v.IsLastPatch);
                LogService.Log($"Versions with last-patch = true: {latestPatchCount}", LogLevel.DEBUG);

                // Debug output for the first 5 sorted versions
                LogService.Log("First 5 sorted PHP versions:", LogLevel.DEBUG);
                for (int i = 0; i < Math.Min(5, phpVersions.Count); i++)
                {
                    var version = phpVersions[i];
                    LogService.Log($"  {i+1}. {version.DisplayName} (last-patch: {version.IsLastPatch})", LogLevel.DEBUG);
                }

                // Save the results to a JSON file for inspection (only in debug mode)
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    string outputFile = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "PHPSwitcher", "php-versions.json");
                    File.WriteAllText(outputFile, System.Text.Json.JsonSerializer.Serialize(phpVersions, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                    LogService.Log($"All versions saved to {outputFile} for inspection.", LogLevel.DEBUG);
                }

                return phpVersions;
            }
            catch (Exception ex)
            {
                LogService.Log($"Error fetching PHP versions: {ex.Message}", LogLevel.ERROR);
                return null;
            }
        }

        public static async Task<bool> DownloadPhpVersionAsync(PhpVersion versionInfo, string phpVersionsDirectory,
            IProgress<(int percentage, string status, double? speed)> progress)
        {
            try
            {
                // Update status immediately
                progress.Report((0, $"Starting download of {versionInfo.DisplayName}...", null));

                LogService.Log($"Starting download of {versionInfo.DisplayName} from {versionInfo.DownloadUrl}", LogLevel.DEBUG);

                // Create temp directory if it doesn't exist
                string tempDir = Path.Combine(Path.GetTempPath(), "PHPSwitcher");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                // Define the download path
                string downloadPath = Path.Combine(tempDir, versionInfo.FileName);

                // Update status to show connection is being established
                progress.Report((0, "Establishing connection with the server...", null));

                // Start download
                DateTime downloadStartTime = DateTime.Now;
                DateTime lastUpdateTime = downloadStartTime;
                long totalBytes = 0;
                long downloadedBytes = 0;

                try
                {
                    // First make a HEAD request to get the file size
                    using (var headRequest = new HttpRequestMessage(HttpMethod.Head, versionInfo.DownloadUrl))
                    {
                        using var headResponse = await HttpClient.SendAsync(headRequest);
                        headResponse.EnsureSuccessStatusCode();

                        if (headResponse.Content.Headers.ContentLength.HasValue)
                        {
                            totalBytes = headResponse.Content.Headers.ContentLength.Value;
                        }
                    }

                    // Update status to show download is starting
                    progress.Report((0, $"Starting download of {versionInfo.DisplayName}...", null));

                    // Start the actual download
                    using (var response = await HttpClient.GetAsync(versionInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        // Get the content stream
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            // Create file stream to write to
                            using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                            {
                                // Create buffer for reading
                                byte[] buffer = new byte[8192];
                                int bytesRead;

                                // Read and write in chunks, updating progress
                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    downloadedBytes += bytesRead;

                                    // Update progress
                                    DateTime currentTime = DateTime.Now;
                                    double elapsedSeconds = (currentTime - downloadStartTime).TotalSeconds;

                                    if (totalBytes > 0)
                                    {
                                        int percentComplete = (int)Math.Min(100, Math.Floor((double)downloadedBytes / totalBytes * 100));

                                        // Only update UI every 0.5 seconds to avoid flickering
                                        if ((currentTime - lastUpdateTime).TotalSeconds >= 0.5)
                                        {
                                            if (elapsedSeconds > 0)
                                            {
                                                double bytesPerSecond = downloadedBytes / elapsedSeconds;
                                                double speedMBps = bytesPerSecond / (1024 * 1024);

                                                if (totalBytes > 0)
                                                {
                                                    double totalMB = totalBytes / (1024 * 1024);
                                                    double downloadedMB = downloadedBytes / (1024 * 1024);

                                                    // Calculate ETA
                                                    long remainingBytes = totalBytes - downloadedBytes;
                                                    double etaSeconds = bytesPerSecond > 0 ? remainingBytes / bytesPerSecond : 0;
                                                    TimeSpan etaTimespan = TimeSpan.FromSeconds(etaSeconds);
                                                    string etaFormatted = etaSeconds > 0
                                                        ? etaSeconds > 60
                                                            ? $"{etaTimespan.Minutes}m {etaTimespan.Seconds}s"
                                                            : $"{etaTimespan.Seconds}s"
                                                        : "< 1s";

                                                    progress.Report((percentComplete,
                                                        $"Downloading {versionInfo.DisplayName}... {Math.Round(downloadedMB, 1)}MB / {Math.Round(totalMB, 1)}MB (ETA: {etaFormatted})",
                                                        speedMBps));
                                                }
                                                else
                                                {
                                                    double downloadedMB = downloadedBytes / (1024 * 1024);
                                                    progress.Report((percentComplete,
                                                        $"Downloading {versionInfo.DisplayName}... {Math.Round(downloadedMB, 1)}MB",
                                                        speedMBps));
                                                }
                                            }
                                            else
                                            {
                                                progress.Report((percentComplete, $"Downloading {versionInfo.DisplayName}...", null));
                                            }

                                            lastUpdateTime = currentTime;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Update status
                    progress.Report((100, "Download completed. Verifying file...", null));
                }
                catch (Exception ex)
                {
                    LogService.Log($"Error during download: {ex.Message}", LogLevel.ERROR);
                    progress.Report((0, $"Error during download: {ex.Message}", null));
                    return false;
                }

                // Verify download
                if (File.Exists(downloadPath))
                {
                    var fileInfo = new FileInfo(downloadPath);
                    if (fileInfo.Length > 1024 * 1024) // > 1MB
                    {
                        progress.Report((100, "Download completed. Verifying file...", null));

                        // Verify ZIP file
                        try
                        {
                            using (ZipArchive zip = ZipFile.OpenRead(downloadPath))
                            {
                                progress.Report((0, "File verified. Extracting...", null));

                                // Create directory for PHP version
                                string versionDirName = $"php-{versionInfo.Version}{(versionInfo.IsNTS ? "-nts" : "")}";
                                string extractPath = Path.Combine(phpVersionsDirectory, versionDirName);

                                // Check if directory already exists
                                if (Directory.Exists(extractPath))
                                {
                                    // Directory will be overwritten during extraction
                                    Directory.Delete(extractPath, true);
                                }

                                // Create the directory
                                Directory.CreateDirectory(extractPath);

                                // Extract the ZIP file
                                progress.Report((0, $"Extracting files to {extractPath}...", null));

                                int totalEntries = zip.Entries.Count;
                                int currentEntry = 0;

                                foreach (ZipArchiveEntry entry in zip.Entries)
                                {
                                    currentEntry++;
                                    int percentComplete = (int)Math.Min(100, Math.Floor((double)currentEntry / totalEntries * 100));

                                    if (currentEntry % 20 == 0)
                                    {
                                        progress.Report((percentComplete, $"Extracting files... {percentComplete}% ({currentEntry} of {totalEntries})", null));
                                    }

                                    string destinationPath = Path.Combine(extractPath, entry.FullName);
                                    string destinationDir = Path.GetDirectoryName(destinationPath);

                                    if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                                    {
                                        Directory.CreateDirectory(destinationDir);
                                    }

                                    if (!entry.FullName.EndsWith("/"))
                                    {
                                        entry.ExtractToFile(destinationPath, true);
                                    }
                                }
                            }

                            // Clean up the downloaded file
                            File.Delete(downloadPath);

                            return true;
                        }
                        catch (Exception ex)
                        {
                            LogService.Log($"Error extracting ZIP file: {ex.Message}", LogLevel.ERROR);
                            progress.Report((0, $"Error extracting ZIP file: {ex.Message}", null));
                            return false;
                        }
                    }
                    else
                    {
                        LogService.Log($"Downloaded file is too small: {fileInfo.Length} bytes", LogLevel.ERROR);
                        progress.Report((0, "Error: Downloaded file is too small.", null));
                        return false;
                    }
                }
                else
                {
                    LogService.Log($"Download failed: File not found at {downloadPath}", LogLevel.ERROR);
                    progress.Report((0, "Error: Download failed.", null));
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogService.Log($"Error downloading PHP: {ex.Message}", LogLevel.ERROR);
                progress.Report((0, $"Error downloading PHP: {ex.Message}", null));
                return false;
            }
        }

        public static bool ActivatePhpVersion(InstalledPhpVersion versionInfo)
        {
            try
            {
                string phpPath = versionInfo.InstallPath;

                // Check if the directory exists
                if (!Directory.Exists(phpPath))
                {
                    LogService.Log($"PHP directory not found: {phpPath}", LogLevel.ERROR);
                    return false;
                }

                // Check if C:\php exists and if it's a symbolic link
                string phpSymlinkPath = @"C:\php";
                bool phpSymlinkExists = Directory.Exists(phpSymlinkPath);
                bool isSymlink = false;

                if (phpSymlinkExists)
                {
                    // Check if it's a symbolic link
                    var dirInfo = new DirectoryInfo(phpSymlinkPath);
                    isSymlink = dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);

                    if (!isSymlink)
                    {
                        // It's a real directory, not a symlink
                        string backupPath = $@"C:\php_backup_{DateTime.Now:yyyyMMdd_HHmmss}";

                        // Backup the directory
                        try
                        {
                            LogService.Log($"Backing up C:\\php to {backupPath}", LogLevel.WARNING);
                            DirectoryCopy(phpSymlinkPath, backupPath, true);
                            Directory.Delete(phpSymlinkPath, true);
                        }
                        catch (Exception ex)
                        {
                            LogService.Log($"Error backing up PHP directory: {ex.Message}", LogLevel.ERROR);
                            return false;
                        }
                    }
                    else
                    {
                        // It's already a symlink, just remove it
                        LogService.Log($"Removing existing symbolic link at {phpSymlinkPath}", LogLevel.WARNING);
                        Directory.Delete(phpSymlinkPath);
                    }
                }

                // Create symbolic link
                try
                {
                    LogService.Log($"Creating symbolic link from {phpSymlinkPath} to {phpPath}", LogLevel.DEBUG);
                    CreateSymbolicLink(phpSymlinkPath, phpPath);
                }
                catch (Exception ex)
                {
                    LogService.Log($"Error creating symbolic link: {ex.Message}", LogLevel.ERROR);
                    return false;
                }

                // Check if C:\php is in the PATH
                string path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
                if (!path.Contains(@"C:\php"))
                {
                    // Add to PATH
                    try
                    {
                        LogService.Log("Adding C:\\php to system PATH", LogLevel.DEBUG);
                        Environment.SetEnvironmentVariable("Path", $"{path};C:\\php", EnvironmentVariableTarget.Machine);
                    }
                    catch (Exception ex)
                    {
                        LogService.Log($"Error updating PATH: {ex.Message}", LogLevel.ERROR);
                        return false;
                    }
                }

                // Verify activation by running php -v
                try
                {
                    // Verify PHP executable exists
                    if (!File.Exists(@"C:\php\php.exe"))
                    {
                        LogService.Log("PHP executable not found at C:\\php\\php.exe", LogLevel.ERROR);
                        return false;
                    }

                    // Run PHP version command with error handling
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = @"C:\php\php.exe",
                        Arguments = "-v",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        LogService.Log($"Error running PHP: {error}", LogLevel.ERROR);
                        return false;
                    }

                    if (Regex.IsMatch(output, @"PHP (\d+\.\d+\.\d+)"))
                    {
                        string detectedVersion = Regex.Match(output, @"PHP (\d+\.\d+\.\d+)").Groups[1].Value;
                        LogService.Log($"PHP {detectedVersion} is now active", LogLevel.DEBUG);
                        return true;
                    }
                    else
                    {
                        LogService.Log($"Failed to verify PHP version: {output}", LogLevel.ERROR);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogService.Log($"Error verifying PHP version: {ex.Message}", LogLevel.ERROR);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogService.Log($"Error activating PHP version: {ex.Message}", LogLevel.ERROR);
                return false;
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags = 1);
    }
}
