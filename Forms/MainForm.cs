using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PhpSwitcher.Models;
using PhpSwitcher.Services;
using PhpSwitcher.Utils;

namespace PhpSwitcher.Forms
{
    public partial class MainForm : Form
    {
        private readonly AppConfig _config;
        private readonly Label _activePhpLabel;
        private readonly Label _statusLabel;
        private readonly ProgressBar _progressBar;
        
        public MainForm(AppConfig config)
        {
            _config = config;
            
            InitializeComponent();
            
            // Set up status bar
            _statusLabel = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };
            
            _activePhpLabel = new Label
            {
                Text = "PHP: Not active",
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Width = 200,
                Padding = new Padding(0, 0, 5, 0)
            };
            
            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Right,
                Width = 150,
                Visible = false
            };
            
            statusStrip.Items.Add(new ToolStripControlHost(_statusLabel));
            statusStrip.Items.Add(new ToolStripControlHost(_progressBar));
            statusStrip.Items.Add(new ToolStripControlHost(_activePhpLabel));
            
            // Check active PHP version
            CheckActivePhpVersion();
            
            // Initialize tabs
            InitializeInstalledVersionsTab();
            InitializeAvailableVersionsTab();
            InitializeSettingsTab();
            
            // Set the first tab as selected
            tabControl.SelectedIndex = 0;
            
            // If in development mode, show indicator
            if (_config.DevMode)
            {
                var devModeIndicator = new Label
                {
                    Text = "DEVELOPMENT MODE ACTIVE",
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.Red,
                    BackColor = Color.Yellow,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(250, 30),
                    Location = new Point(160, 160)
                };
                settingsTab.Controls.Add(devModeIndicator);
            }
        }
        
        private void CheckActivePhpVersion()
        {
            try
            {
                // Check if C:\php exists
                if (Directory.Exists(@"C:\php"))
                {
                    // Check if php.exe exists
                    if (File.Exists(@"C:\php\php.exe"))
                    {
                        // Run PHP version command
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
                        process.WaitForExit();
                        
                        // Extract version from output
                        if (output.Contains("PHP"))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(output, @"PHP (\d+\.\d+\.\d+)");
                            if (match.Success)
                            {
                                _activePhpLabel.Text = $"PHP: {match.Groups[1].Value} active";
                            }
                        }
                    }
                    else
                    {
                        _activePhpLabel.Text = "PHP: Not active (missing executable)";
                    }
                }
                else
                {
                    _activePhpLabel.Text = "PHP: Not active";
                }
            }
            catch (Exception ex)
            {
                LogService.Log($"Error checking active PHP version: {ex.Message}", LogLevel.ERROR);
                _activePhpLabel.Text = "PHP: Status unknown";
            }
        }
        
        private void InitializeInstalledVersionsTab()
        {
            // Add a ListView to display installed PHP versions
            var installedVersionsListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Size = new Size(730, 425),
                Location = new Point(10, 10)
            };
            
            // Add columns to the ListView
            installedVersionsListView.Columns.Add("Version", 100);
            installedVersionsListView.Columns.Add("Type", 150);
            installedVersionsListView.Columns.Add("Installation Path", 250);
            installedVersionsListView.Columns.Add("Install Date", 120);
            installedVersionsListView.Columns.Add("Status", 100);
            
            // Populate the ListView with installed PHP versions
            UpdateInstalledVersionsList(installedVersionsListView);
            
            installedTab.Controls.Add(installedVersionsListView);
            
            // Add an activate button
            var activateButton = new Button
            {
                Text = "Activate Selected Version",
                Size = new Size(200, 30),
                Location = new Point(10, 445)
            };
            
            activateButton.Click += (sender, e) =>
            {
                if (installedVersionsListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show(
                        "Please select a PHP version to activate.",
                        "PHP Switcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                
                if (installedVersionsListView.SelectedItems.Count > 1)
                {
                    MessageBox.Show(
                        "Please select only one PHP version to activate.",
                        "PHP Switcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                
                var selectedVersion = installedVersionsListView.SelectedItems[0].Tag as InstalledPhpVersion;
                if (selectedVersion == null)
                {
                    MessageBox.Show(
                        "Error: Selected item does not contain valid version information.",
                        "PHP Switcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                
                // Check if already active
                if (selectedVersion.IsActive)
                {
                    MessageBox.Show(
                        $"PHP {selectedVersion.Version} is already active.",
                        "PHP Switcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                
                // Activate the selected version
                _statusLabel.Text = $"Activating PHP {selectedVersion.Version}...";
                Application.DoEvents();
                
                var result = PhpVersionService.ActivatePhpVersion(selectedVersion);
                
                if (result)
                {
                    // Update the installed versions list
                    foreach (var version in _config.InstalledVersions)
                    {
                        version.IsActive = (version.Version == selectedVersion.Version && version.IsNTS == selectedVersion.IsNTS);
                    }
                    
                    ConfigService.SaveConfiguration(_config);
                    UpdateInstalledVersionsList(installedVersionsListView);
                    
                    // Update status label to show completion
                    _statusLabel.Text = "Ready";
                    
                    // Update active PHP label
                    CheckActivePhpVersion();
                    
                    MessageBox.Show(
                        $"PHP {selectedVersion.Version} has been activated successfully.",
                        "PHP Switcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    // Update status label to show failure
                    _statusLabel.Text = "Failed to activate PHP version";
                    
                    MessageBox.Show(
                        $"Failed to activate PHP {selectedVersion.Version}. Please check the logs for details.",
                        "PHP Switcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };
            
            installedTab.Controls.Add(activateButton);
            
            // Add a remove button
            var removeButton = new Button
            {
                Text = "Remove Selected Version",
                Size = new Size(200, 30),
                Location = new Point(220, 445)
            };
            
            removeButton.Click += (sender, e) =>
            {
                if (installedVersionsListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show(
                        "Please select a PHP version to remove.",
                        "PHP Switcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                
                // Get selected versions
                var selectedVersions = new List<InstalledPhpVersion>();
                foreach (ListViewItem item in installedVersionsListView.SelectedItems)
                {
                    var version = item.Tag as InstalledPhpVersion;
                    if (version != null)
                    {
                        selectedVersions.Add(version);
                    }
                }
                
                // Check if any selected version is active
                bool activeVersionSelected = selectedVersions.Any(v => v.IsActive);
                
                if (activeVersionSelected)
                {
                    var confirmResult = MessageBox.Show(
                        "One or more selected versions are currently active. Removing an active version may cause issues with PHP functionality.\n\nDo you want to continue?",
                        "PHP Switcher - Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    
                    if (confirmResult == DialogResult.No)
                    {
                        return;
                    }
                }
                
                // Confirm removal
                var versionList = string.Join("\n", selectedVersions.Select(v => $"- PHP {v.Version} {(v.IsNTS ? "(Non-Thread Safe)" : "(Thread Safe)")}"));
                var confirmMessage = $"You are about to remove the following PHP version(s):\n\n{versionList}\n\nThis will delete all files in the installation directories. Do you want to continue?";
                
                var confirmResult2 = MessageBox.Show(
                    confirmMessage,
                    "PHP Switcher - Confirm Removal",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (confirmResult2 == DialogResult.No)
                {
                    return;
                }
                
                // Process each selected version
                int successCount = 0;
                int failCount = 0;
                
                foreach (var version in selectedVersions)
                {
                    try
                    {
                        _statusLabel.Text = $"Removing PHP {version.Version}...";
                        Application.DoEvents();
                        
                        // Check if directory exists
                        if (Directory.Exists(version.InstallPath))
                        {
                            // Remove directory
                            Directory.Delete(version.InstallPath, true);
                            
                            // Check if this was the active version
                            if (version.IsActive)
                            {
                                // Remove symbolic link if it points to this version
                                if (Directory.Exists(@"C:\php"))
                                {
                                    var dirInfo = new DirectoryInfo(@"C:\php");
                                    if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                                    {
                                        var target = dirInfo.LinkTarget;
                                        if (target == version.InstallPath)
                                        {
                                            Directory.Delete(@"C:\php");
                                        }
                                    }
                                }
                            }
                            
                            successCount++;
                        }
                        else
                        {
                            LogService.Log($"Installation directory not found: {version.InstallPath}", LogLevel.WARNING);
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Log($"Error removing PHP version: {ex.Message}", LogLevel.ERROR);
                        failCount++;
                    }
                }
                
                // Update configuration
                _config.InstalledVersions = _config.InstalledVersions
                    .Where(v => !selectedVersions.Any(sv => sv.Version == v.Version && sv.IsNTS == v.IsNTS))
                    .ToList();
                
                ConfigService.SaveConfiguration(_config);
                
                // Update UI
                UpdateInstalledVersionsList(installedVersionsListView);
                
                // Update active PHP label
                CheckActivePhpVersion();
                
                // Show summary
                if (successCount > 0 && failCount == 0)
                {
                    _statusLabel.Text = $"Successfully removed {successCount} PHP version(s).";
                    MessageBox.Show(
                        $"Successfully removed {successCount} PHP version(s).",
                        "PHP Switcher - Removal Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else if (successCount > 0 && failCount > 0)
                {
                    _statusLabel.Text = $"Removed {successCount} PHP version(s). {failCount} removal(s) failed.";
                    MessageBox.Show(
                        $"Removed {successCount} PHP version(s). {failCount} removal(s) failed.",
                        "PHP Switcher - Removal Partially Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else if (successCount == 0 && failCount > 0)
                {
                    _statusLabel.Text = "Failed to remove any PHP versions.";
                    MessageBox.Show(
                        "Failed to remove any PHP versions. Please check the logs for error details.",
                        "PHP Switcher - Removal Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };
            
            installedTab.Controls.Add(removeButton);
            
            // Add a refresh button
            var refreshInstalledButton = new Button
            {
                Text = "Refresh List",
                Size = new Size(100, 30),
                Location = new Point(635, 445)
            };
            
            refreshInstalledButton.Click += (sender, e) =>
            {
                _statusLabel.Text = "Refreshing installed PHP versions...";
                Application.DoEvents();
                
                // Scan the PHP versions directory for installed versions
                var phpVersionsDir = _config.PhpVersionsDirectory;
                if (Directory.Exists(phpVersionsDir))
                {
                    var directories = Directory.GetDirectories(phpVersionsDir);
                    
                    foreach (var dir in directories)
                    {
                        // Check if this is a PHP directory by looking for php.exe
                        var phpExe = Path.Combine(dir, "php.exe");
                        if (File.Exists(phpExe))
                        {
                            // Try to extract version information from directory name
                            var dirName = Path.GetFileName(dir);
                            var match = System.Text.RegularExpressions.Regex.Match(dirName, @"php-(\d+\.\d+\.\d+)(?:-nts)?");
                            if (match.Success)
                            {
                                var version = match.Groups[1].Value;
                                var isNTS = dirName.Contains("-nts");
                                
                                // Check if this version is already in the configuration
                                var existingVersion = _config.InstalledVersions.FirstOrDefault(
                                    v => v.Version == version && v.IsNTS == isNTS);
                                
                                if (existingVersion == null)
                                {
                                    // Add to configuration
                                    var newInstalledVersion = new InstalledPhpVersion
                                    {
                                        Version = version,
                                        IsNTS = isNTS,
                                        InstallPath = dir,
                                        InstallDate = DateTime.Now,
                                        IsActive = false
                                    };
                                    
                                    _config.InstalledVersions.Add(newInstalledVersion);
                                }
                                else
                                {
                                    // Update installation path if needed
                                    if (existingVersion.InstallPath != dir)
                                    {
                                        existingVersion.InstallPath = dir;
                                    }
                                }
                            }
                        }
                    }
                    
                    ConfigService.SaveConfiguration(_config);
                    UpdateInstalledVersionsList(installedVersionsListView);
                    
                    _statusLabel.Text = "Installed PHP versions refreshed.";
                }
                else
                {
                    MessageBox.Show(
                        $"PHP versions directory not found: {phpVersionsDir}",
                        "PHP Switcher - Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };
            
            installedTab.Controls.Add(refreshInstalledButton);
        }
        
        private void UpdateInstalledVersionsList(ListView installedVersionsListView)
        {
            installedVersionsListView.Items.Clear();
            
            // Remove any existing "no versions" label
            foreach (Control control in installedTab.Controls)
            {
                if (control is Label label && label.Text.Contains("No PHP versions installed"))
                {
                    installedTab.Controls.Remove(label);
                    label.Dispose();
                    break;
                }
            }
            
            if (_config.InstalledVersions != null && _config.InstalledVersions.Count > 0)
            {
                // Sort versions by version number (descending) and then by type (TS first, then NTS)
                var sortedVersions = _config.InstalledVersions
                    .OrderByDescending(v => 
                    {
                        string[] versionParts = v.Version.Split('.');
                        return int.Parse(versionParts[0]) * 10000 + int.Parse(versionParts[1]) * 100 + int.Parse(versionParts[2]);
                    })
                    .ThenBy(v => v.IsNTS) // False (TS) comes before True (NTS)
                    .ToList();
                
                foreach (var version in sortedVersions)
                {
                    var item = new ListViewItem(version.Version);
                    item.SubItems.Add(version.IsNTS ? "Non-Thread Safe" : "Thread Safe");
                    item.SubItems.Add(version.InstallPath);
                    item.SubItems.Add(version.InstallDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    item.SubItems.Add(version.IsActive ? "Active" : "Inactive");
                    
                    if (version.IsActive)
                    {
                        item.BackColor = Color.FromArgb(230, 255, 230);  // Light green
                        item.Font = new Font(item.Font, FontStyle.Bold);
                    }
                    
                    // Add tooltip information
                    item.ToolTipText = $"PHP {version.Version} {(version.IsNTS ? "(Non-Thread Safe)" : "(Thread Safe)")}\n" +
                                      $"Installed: {version.InstallDate.ToString("yyyy-MM-dd HH:mm:ss")}\n" +
                                      $"Path: {version.InstallPath}\n" +
                                      $"Status: {(version.IsActive ? "Active" : "Inactive")}";
                    
                    item.Tag = version;
                    installedVersionsListView.Items.Add(item);
                }
                
                // Add a count label
                var countLabel = new Label
                {
                    Text = $"Total installed versions: {sortedVersions.Count}",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.Gray,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Size = new Size(200, 20),
                    Location = new Point(430, 450)
                };
                
                // Remove any existing count label
                foreach (Control control in installedTab.Controls)
                {
                    if (control is Label label && label.Text.StartsWith("Total installed versions:"))
                    {
                        installedTab.Controls.Remove(label);
                        label.Dispose();
                        break;
                    }
                }
                
                installedTab.Controls.Add(countLabel);
            }
            else
            {
                var noVersionsLabel = new Label
                {
                    Text = "No PHP versions installed. Go to the 'Available Versions' tab to download and install PHP.",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.Red,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(740, 30),
                    Location = new Point(10, 150)
                };
                
                installedTab.Controls.Add(noVersionsLabel);
            }
            
            // Log the update for traceability
            LogService.Log($"Updated installed versions list. Found {(_config.InstalledVersions?.Count ?? 0)} installed versions.", LogLevel.DEBUG);
        }
        
        private void InitializeAvailableVersionsTab()
        {
            // Add a ListView to display available PHP versions
            var availableVersionsListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Size = new Size(730, 425),
                Location = new Point(10, 10),
                MultiSelect = true
            };
            
            // Add columns to the ListView
            availableVersionsListView.Columns.Add("Version", 100);
            availableVersionsListView.Columns.Add("Type", 150);
            availableVersionsListView.Columns.Add("Filename", 250);
            availableVersionsListView.Columns.Add("Status", 100);
            availableVersionsListView.Columns.Add("Installation Status", 120);
            
            // Populate the ListView with available PHP versions - only showing latest patch for each minor version
            if (_config.AvailableVersions != null && _config.AvailableVersions.Count > 0)
            {
                // Filter to only show versions with last-patch = true
                var latestVersions = _config.AvailableVersions.Where(v => v.IsLastPatch).ToList();
                
                LogService.Log($"Showing only latest patch versions. Total versions: {_config.AvailableVersions.Count}, Latest patch versions: {latestVersions.Count}", LogLevel.DEBUG);
                
                foreach (var version in latestVersions)
                {
                    var item = new ListViewItem(version.Version);
                    item.SubItems.Add(version.IsNTS ? "Non-Thread Safe" : "Thread Safe");
                    item.SubItems.Add(version.FileName);
                    item.SubItems.Add("Available");
                    
                    // Check if this version is installed
                    var isInstalled = PhpVersionService.IsVersionInstalled(_config, version.Version, version.IsNTS);
                    item.SubItems.Add(isInstalled ? "Installed" : "");
                    
                    item.Tag = version;
                    availableVersionsListView.Items.Add(item);
                }
            }
            else
            {
                var noVersionsLabel = new Label
                {
                    Text = "No PHP versions found. Please check your internet connection and try again.",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.Red,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(740, 30),
                    Location = new Point(10, 150)
                };
                
                availableTab.Controls.Add(noVersionsLabel);
            }
            
            availableTab.Controls.Add(availableVersionsListView);
            
            // Add a refresh button for available versions
            var refreshButton = new Button
            {
                Text = "Refresh Available Versions",
                Size = new Size(200, 30),
                Location = new Point(10, 445)
            };
            
            refreshButton.Click += async (sender, e) =>
            {
                LogService.Log("===== REFRESH BUTTON CLICKED =====", LogLevel.DEBUG);
                
                // Clear the ListView immediately
                availableVersionsListView.Items.Clear();
                
                // Create and show a loading indicator
                var loadingLabel = new Label
                {
                    Text = "Refreshing available PHP versions... Please wait.",
                    AutoSize = true,
                    Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold),
                    ForeColor = Color.Blue,
                    Location = new Point(10, 150),
                    BackColor = Color.LightYellow,
                    BorderStyle = BorderStyle.FixedSingle,
                    Padding = new Padding(10)
                };
                
                // Add the loading indicator to the tab
                availableTab.Controls.Add(loadingLabel);
                
                // Update the interface to show the loading indicator
                Application.DoEvents();
                
                // Update status
                _statusLabel.Text = "Refreshing available PHP versions...";
                
                try
                {
                    // Fetch available PHP versions
                    var phpVersions = await PhpVersionService.FetchAvailablePhpVersionsAsync();
                    
                    if (phpVersions != null && phpVersions.Count > 0)
                    {
                        // Update configuration with available versions
                        _config.AvailableVersions = phpVersions;
                        _config.LastUpdated = DateTime.Now;
                        ConfigService.SaveConfiguration(_config);
                        
                        // Remove the loading label
                        availableTab.Controls.Remove(loadingLabel);
                        loadingLabel.Dispose();
                        
                        // Clear the ListView
                        availableVersionsListView.Items.Clear();
                        
                        // Filter to only show versions with last-patch = true
                        var latestVersions = phpVersions.Where(v => v.IsLastPatch).ToList();
                        
                        LogService.Log($"Showing only latest patch versions. Total versions: {phpVersions.Count}, Latest patch versions: {latestVersions.Count}", LogLevel.DEBUG);
                        
                        // Group versions by major.minor for better organization
                        var groupedVersions = latestVersions
                            .GroupBy(v => {
                                string[] parts = v.Version.Split('.');
                                return $"{parts[0]}.{parts[1]}";
                            })
                            .OrderByDescending(g => {
                                string[] parts = g.Key.Split('.');
                                return int.Parse(parts[0]) * 100 + int.Parse(parts[1]);
                            });
                        
                        foreach (var group in groupedVersions)
                        {
                            // Sort versions within group by version number (descending) and then by type (TS first, then NTS)
                            var sortedVersions = group
                                .OrderByDescending(v => {
                                    string[] parts = v.Version.Split('.');
                                    return int.Parse(parts[0]) * 10000 + int.Parse(parts[1]) * 100 + int.Parse(parts[2]);
                                })
                                .ThenBy(v => v.IsNTS); // False (TS) comes before True (NTS)
                            
                            foreach (var version in sortedVersions)
                            {
                                var item = new ListViewItem(version.Version);
                                item.SubItems.Add(version.IsNTS ? "Non-Thread Safe" : "Thread Safe");
                                item.SubItems.Add(version.FileName);
                                item.SubItems.Add("Available");
                                
                                // Check if this version is installed
                                var isInstalled = PhpVersionService.IsVersionInstalled(_config, version.Version, version.IsNTS);
                                item.SubItems.Add(isInstalled ? "Installed" : "");
                                
                                // Add visual indicators
                                if (isInstalled)
                                {
                                    item.BackColor = Color.FromArgb(240, 240, 255); // Light blue
                                    item.Font = new Font(item.Font, FontStyle.Regular);
                                }
                                
                                // Add tooltip information
                                item.ToolTipText = $"PHP {version.Version} {(version.IsNTS ? "(Non-Thread Safe)" : "(Thread Safe)")}\n" +
                                                  $"Filename: {version.FileName}\n" +
                                                  $"Status: {(isInstalled ? "Already Installed" : "Available for download")}";
                                
                                item.Tag = version;
                                availableVersionsListView.Items.Add(item);
                            }
                        }
                        
                        // Add a count label
                        var countLabel = new Label
                        {
                            Text = $"Showing latest patch versions: {latestVersions.Count} of {phpVersions.Count} total versions",
                            Font = new Font("Segoe UI", 9),
                            ForeColor = Color.Gray,
                            TextAlign = ContentAlignment.MiddleLeft,
                            Size = new Size(350, 20),
                            Location = new Point(430, 450)
                        };
                        
                        // Remove any existing count label
                        foreach (Control control in availableTab.Controls)
                        {
                            if (control is Label label && label.Text.StartsWith("Showing latest patch versions:"))
                            {
                                availableTab.Controls.Remove(label);
                                label.Dispose();
                                break;
                            }
                        }
                        
                        availableTab.Controls.Add(countLabel);
                        
                        _statusLabel.Text = "Available PHP versions refreshed.";
                    }
                    else
                    {
                        // Remove the loading label
                        availableTab.Controls.Remove(loadingLabel);
                        loadingLabel.Dispose();
                        
                        // Show error message
                        var errorLabel = new Label
                        {
                            Text = "Failed to fetch PHP versions. Please check your internet connection and try again.",
                            Font = new Font("Segoe UI", 10),
                            ForeColor = Color.Red,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Size = new Size(740, 30),
                            Location = new Point(10, 150)
                        };
                        
                        availableTab.Controls.Add(errorLabel);
                        
                        _statusLabel.Text = "Failed to refresh available PHP versions.";
                    }
                }
                catch (Exception ex)
                {
                    // Remove the loading label
                    availableTab.Controls.Remove(loadingLabel);
                    loadingLabel.Dispose();
                    
                    LogService.Log($"Error refreshing PHP versions: {ex.Message}", LogLevel.ERROR);
                    
                    // Show error message
                    var errorLabel = new Label
                    {
                        Text = $"Error refreshing PHP versions: {ex.Message}",
                        Font = new Font("Segoe UI", 10),
                        ForeColor = Color.Red,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Size = new Size(740, 30),
                        Location = new Point(10, 150)
                    };
                    
                    availableTab.Controls.Add(errorLabel);
                    
                    _statusLabel.Text = "Error refreshing available PHP versions.";
                }
                
                LogService.Log("===== REFRESH BUTTON CLICK COMPLETED =====", LogLevel.DEBUG);
            };
            
            availableTab.Controls.Add(refreshButton);
            
            // Add a download button for available versions
            var downloadButton = new Button
            {
                Text = "Download Selected Version(s)",
                Size = new Size(200, 30),
                Location = new Point(220, 445)
            };
            
            downloadButton.Click += async (sender, e) =>
            {
                if (availableVersionsListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show(
                        "Please select at least one PHP version to download.",
                        "PHP Switcher",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                
                // Get selected versions
                var selectedVersions = new List<PhpVersion>();
                foreach (ListViewItem item in availableVersionsListView.SelectedItems)
                {
                    selectedVersions.Add((PhpVersion)item.Tag);
                }
                
                // Confirm download
                var versionList = string.Join("\n", selectedVersions.Select(v => $"- {v.DisplayName}"));
                var confirmMessage = $"You are about to download and install the following PHP version(s):\n\n{versionList}\n\nDo you want to continue?";
                
                var confirmResult = MessageBox.Show(
                    confirmMessage,
                    "PHP Switcher - Confirm Download",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (confirmResult == DialogResult.No)
                {
                    return;
                }
                
                // Disable the button during download
                downloadButton.Enabled = false;
                
                // Process each selected version
                int successCount = 0;
                int failCount = 0;
                
                foreach (var version in selectedVersions)
                {
                    // Check if already installed
                    var isInstalled = PhpVersionService.IsVersionInstalled(_config, version.Version, version.IsNTS);
                    
                    if (isInstalled)
                    {
                        var confirmOverwrite = MessageBox.Show(
                            $"PHP {version.DisplayName} is already installed. Do you want to reinstall it?",
                            "PHP Switcher - Version Already Installed",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                        
                        if (confirmOverwrite == DialogResult.No)
                        {
                            continue;
                        }
                    }
                    
                    // Show progress bar
                    _progressBar.Visible = true;
                    _progressBar.Value = 0;
                    
                    // Download and install
                    var progress = new Progress<(int percentage, string status, double? speed)>(update =>
                    {
                        _progressBar.Value = update.percentage;
                        _statusLabel.Text = update.status;
                        Application.DoEvents();
                    });
                    
                    var result = await Task.Run(() => PhpVersionService.DownloadPhpVersionAsync(version, _config.PhpVersionsDirectory, progress));
                    
                    if (result)
                    {
                        // Add to installed versions
                        var newInstalledVersion = new InstalledPhpVersion
                        {
                            Version = version.Version,
                            IsNTS = version.IsNTS,
                            InstallPath = Path.Combine(_config.PhpVersionsDirectory, $"php-{version.Version}{(version.IsNTS ? "-nts" : "")}"),
                            InstallDate = DateTime.Now,
                            IsActive = false
                        };
                        
                        // Check if version already exists in installed versions
                        var existingIndex = _config.InstalledVersions.FindIndex(
                            v => v.Version == version.Version && v.IsNTS == version.IsNTS);
                        
                        if (existingIndex >= 0)
                        {
                            _config.InstalledVersions[existingIndex] = newInstalledVersion;
                        }
                        else
                        {
                            _config.InstalledVersions.Add(newInstalledVersion);
                        }
                        
                        ConfigService.SaveConfiguration(_config);
                        
                        // Ask if user wants to activate this version
                        var activateResult = MessageBox.Show(
                            $"PHP {version.DisplayName} has been successfully installed.\n\nDo you want to activate this version now?",
                            "PHP Switcher - Installation Complete",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                        
                        if (activateResult == DialogResult.Yes)
                        {
                            // Activate this PHP version
                            _statusLabel.Text = $"Activating PHP {version.Version}...";
                            Application.DoEvents();
                            
                            var activationResult = PhpVersionService.ActivatePhpVersion(newInstalledVersion);
                            if (activationResult)
                            {
                                // Update the active status
                                foreach (var installedVersion in _config.InstalledVersions)
                                {
                                    installedVersion.IsActive = (installedVersion.Version == version.Version && installedVersion.IsNTS == version.IsNTS);
                                }
                                
                                ConfigService.SaveConfiguration(_config);
                                
                                _statusLabel.Text = $"PHP {version.Version} is now active.";
                                
                                // Update active PHP label
                                CheckActivePhpVersion();
                            }
                            else
                            {
                                _statusLabel.Text = $"Failed to activate PHP {version.Version}.";
                            }
                        }
                        else
                        {
                            _statusLabel.Text = $"PHP {version.Version} installed successfully.";
                        }
                        
                        // Update the ListView to show installed status
                        foreach (ListViewItem item in availableVersionsListView.Items)
                        {
                            var itemVersion = (PhpVersion)item.Tag;
                            if (itemVersion.Version == version.Version && itemVersion.IsNTS == version.IsNTS)
                            {
                                item.SubItems[4].Text = "Installed";
                            }
                        }
                        
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                    
                    // Hide progress bar
                    _progressBar.Visible = false;
                }
                
                // Re-enable the button
                downloadButton.Enabled = true;
                
                // Show summary
                if (successCount > 0 && failCount == 0)
                {
                    _statusLabel.Text = $"Successfully installed {successCount} PHP version(s).";
                    MessageBox.Show(
                        $"Successfully installed {successCount} PHP version(s).",
                        "PHP Switcher - Installation Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else if (successCount > 0 && failCount > 0)
                {
                    _statusLabel.Text = $"Installed {successCount} PHP version(s). {failCount} installation(s) failed.";
                    MessageBox.Show(
                        $"Installed {successCount} PHP version(s). {failCount} installation(s) failed.",
                        "PHP Switcher - Installation Partially Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else if (successCount == 0 && failCount > 0)
                {
                    _statusLabel.Text = "Failed to install any PHP versions.";
                    MessageBox.Show(
                        "Failed to install any PHP versions. Please check the logs for error details.",
                        "PHP Switcher - Installation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };
            
            availableTab.Controls.Add(downloadButton);
        }
        
        private void InitializeSettingsTab()
        {
            // Add settings to the Settings tab
            var settingsDirectoryLabel = new Label
            {
                Text = "PHP Versions Directory:",
                Font = new Font("Segoe UI", 9),
                Size = new Size(150, 20),
                Location = new Point(10, 20)
            };
            settingsTab.Controls.Add(settingsDirectoryLabel);
            
            var settingsDirectoryTextBox = new TextBox
            {
                Text = _config.PhpVersionsDirectory,
                Size = new Size(450, 20),
                Location = new Point(160, 20),
                ReadOnly = true
            };
            settingsTab.Controls.Add(settingsDirectoryTextBox);
            
            var settingsDirectoryButton = new Button
            {
                Text = "Change",
                Size = new Size(100, 23),
                Location = new Point(620, 19)
            };
            
            settingsDirectoryButton.Click += (sender, e) =>
            {
                using var folderBrowser = new FolderBrowserDialog
                {
                    Description = "Select directory for PHP versions",
                    ShowNewFolderButton = true,
                    SelectedPath = _config.PhpVersionsDirectory
                };
                
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    var newDirectory = folderBrowser.SelectedPath;
                    
                    // Confirm directory change
                    var confirmResult = MessageBox.Show(
                        $"Are you sure you want to change the PHP versions directory to:\n\n{newDirectory}\n\nThis will require moving any existing PHP installations to the new directory.",
                        "PHP Switcher - Confirm Directory Change",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    
                    if (confirmResult == DialogResult.Yes)
                    {
                        // Create the directory if it doesn't exist
                        if (!Directory.Exists(newDirectory))
                        {
                            try
                            {
                                Directory.CreateDirectory(newDirectory);
                                LogService.Log($"Created new PHP versions directory: {newDirectory}", LogLevel.DEBUG);
                            }
                            catch (Exception ex)
                            {
                                LogService.Log($"Error creating new PHP versions directory: {ex.Message}", LogLevel.ERROR);
                                MessageBox.Show(
                                    $"Failed to create new PHP versions directory: {newDirectory}\n\nError: {ex.Message}",
                                    "PHP Switcher - Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                                return;
                            }
                        }
                        
                        // Update the configuration
                        _config.PhpVersionsDirectory = newDirectory;
                        ConfigService.SaveConfiguration(_config);
                        
                        // Update the UI
                        settingsDirectoryTextBox.Text = newDirectory;
                        
                        MessageBox.Show(
                            $"PHP versions directory has been changed to:\n\n{newDirectory}",
                            "PHP Switcher - Directory Changed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            };
            
            settingsTab.Controls.Add(settingsDirectoryButton);
            
            // Add last updated information
            var lastUpdatedLabel = new Label
            {
                Text = "Last Updated:",
                Font = new Font("Segoe UI", 9),
                Size = new Size(150, 20),
                Location = new Point(10, 50)
            };
            settingsTab.Controls.Add(lastUpdatedLabel);
            
            var lastUpdatedValueLabel = new Label
            {
                Text = _config.LastUpdated.HasValue ? _config.LastUpdated.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never",
                Font = new Font("Segoe UI", 9),
                Size = new Size(450, 20),
                Location = new Point(160, 50)
            };
            settingsTab.Controls.Add(lastUpdatedValueLabel);
            
            // Add Debug and Development Mode section
            var debugModeLabel = new Label
            {
                Text = "Debug/Dev Mode:",
                Font = new Font("Segoe UI", 9),
                Size = new Size(150, 20),
                Location = new Point(10, 80)
            };
            settingsTab.Controls.Add(debugModeLabel);
            
            var debugModeButton = new Button
            {
                Text = "Restart in Debug Mode",
                Size = new Size(200, 30),
                Location = new Point(160, 75)
            };
            
            debugModeButton.Click += (sender, e) =>
            {
                LogService.Log("Restarting application in debug mode...", LogLevel.WARNING);
                
                // Restart with debug flag
                AdminUtils.RestartAsAdmin(true, _config.DevMode);
            };
            
            settingsTab.Controls.Add(debugModeButton);
            
            // Add Development Mode button
            var devModeButton = new Button
            {
                Text = "Restart in Development Mode",
                Size = new Size(200, 30),
                Location = new Point(370, 75),
                BackColor = Color.FromArgb(255, 192, 192)
            };
            
            devModeButton.Click += (sender, e) =>
            {
                LogService.Log("Restarting application in DEVELOPMENT mode...", LogLevel.WARNING);
                
                // Restart with dev flag
                AdminUtils.RestartAsAdmin(_config.ShowConsole, true);
            };
            
            settingsTab.Controls.Add(devModeButton);
            
            // Add a description for debug mode
            var debugModeDescLabel = new Label
            {
                Text = "Debug mode shows the console window with detailed logs for troubleshooting.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Size = new Size(450, 20),
                Location = new Point(160, 110)
            };
            settingsTab.Controls.Add(debugModeDescLabel);
            
            // Add a description for development mode
            var devModeDescLabel = new Label
            {
                Text = "Development mode enables additional features for developers and shows more detailed logs.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Size = new Size(450, 20),
                Location = new Point(160, 130)
            };
            settingsTab.Controls.Add(devModeDescLabel);
        }
        
        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.installedTab = new System.Windows.Forms.TabPage();
            this.availableTab = new System.Windows.Forms.TabPage();
            this.settingsTab = new System.Windows.Forms.TabPage();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.exitButton = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.installedTab);
            this.tabControl.Controls.Add(this.availableTab);
            this.tabControl.Controls.Add(this.settingsTab);
            this.tabControl.Location = new System.Drawing.Point(20, 20);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(750, 505);
            this.tabControl.TabIndex = 0;
            // 
            // installedTab
            // 
            this.installedTab.Location = new System.Drawing.Point(4, 24);
            this.installedTab.Name = "installedTab";
            this.installedTab.Padding = new System.Windows.Forms.Padding(3);
            this.installedTab.Size = new System.Drawing.Size(742, 477);
            this.installedTab.TabIndex = 0;
            this.installedTab.Text = "Installed Versions";
            this.installedTab.UseVisualStyleBackColor = true;
            // 
            // availableTab
            // 
            this.availableTab.Location = new System.Drawing.Point(4, 24);
            this.availableTab.Name = "availableTab";
            this.availableTab.Padding = new System.Windows.Forms.Padding(3);
            this.availableTab.Size = new System.Drawing.Size(742, 477);
            this.availableTab.TabIndex = 1;
            this.availableTab.Text = "Available Versions";
            this.availableTab.UseVisualStyleBackColor = true;
            // 
            // settingsTab
            // 
            this.settingsTab.Location = new System.Drawing.Point(4, 24);
            this.settingsTab.Name = "settingsTab";
            this.settingsTab.Padding = new System.Windows.Forms.Padding(3);
            this.settingsTab.Size = new System.Drawing.Size(742, 477);
            this.settingsTab.TabIndex = 2;
            this.settingsTab.Text = "Settings";
            this.settingsTab.UseVisualStyleBackColor = true;
            // 
            // statusStrip
            // 
            this.statusStrip.Location = new System.Drawing.Point(0, 539);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(784, 22);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "statusStrip1";
            // 
            // exitButton
            // 
            this.exitButton.Location = new System.Drawing.Point(680, 560);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(100, 30);
            this.exitButton.TabIndex = 2;
            this.exitButton.Text = "Exit";
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.exitButton);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.tabControl);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PHP Switcher";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        private void exitButton_Click(object sender, EventArgs e)
        {
            LogService.Log("Application closed by user.", LogLevel.WARNING);
            Close();
        }
        
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogService.Log("Application is shutting down...", LogLevel.WARNING);
        }
        
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage installedTab;
        private System.Windows.Forms.TabPage availableTab;
        private System.Windows.Forms.TabPage settingsTab;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.Button exitButton;
    }
}