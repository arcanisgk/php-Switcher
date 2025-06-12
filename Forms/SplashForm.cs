using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using PhpSwitcher.Models;
using PhpSwitcher.Services;
using PhpSwitcher.Utils;

namespace PhpSwitcher.Forms
{
    public partial class SplashForm : Form
    {
        private readonly AppConfig _config;
        private bool _mainAppStarted = false;
        
        public SplashForm()
        {
            InitializeComponent();
            
            // Load configuration
            _config = ConfigService.LoadConfiguration();
            
            // Set version label
            versionLabel.Text = "v1.0.1";
            
            // Start initialization after a short delay
            var timer = new System.Windows.Forms.Timer
            {
                Interval = 500
            };
            timer.Tick += async (sender, e) =>
            {
                timer.Stop();
                await PerformInitializationAsync();
            };
            timer.Start();
        }
        
        private async Task PerformInitializationAsync()
        {
            try
            {
                // Step 1: Verify administrator privileges
                statusLabel.Text = "Verifying administrator privileges...";
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 20;
                Refresh();
                LogService.Log("Verifying administrator privileges...", LogLevel.DEBUG);
                
                if (!AdminUtils.IsRunningAsAdmin())
                {
                    LogService.Log("Application is not running with administrative privileges. Restarting...", LogLevel.WARNING);
                    AdminUtils.RestartAsAdmin(_config.ShowConsole, _config.DevMode);
                    return;
                }
                
                LogService.Log("Running with administrative privileges.", LogLevel.DEBUG);
                
                // Step 2: Check if it's the first run and configure PHP directory
                statusLabel.Text = "Checking application configuration...";
                progressBar.Value = 40;
                Refresh();
                LogService.Log("Checking if this is the first run...", LogLevel.DEBUG);
                
                if (_config.FirstRun)
                {
                    LogService.Log("First run detected. Prompting for PHP versions directory...", LogLevel.WARNING);
                    
                    var dialogResult = MessageBox.Show(
                        $"Welcome to PHP Switcher!{Environment.NewLine}{Environment.NewLine}" +
                        $"Would you like to use the default directory for PHP versions ({_config.PhpVersionsDirectory})?{Environment.NewLine}{Environment.NewLine}" +
                        "Click 'Yes' to use the default directory, or 'No' to choose a different directory.",
                        "PHP Switcher - First Run Setup",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    
                    if (dialogResult == DialogResult.No)
                    {
                        using var folderBrowser = new FolderBrowserDialog
                        {
                            Description = "Select directory for PHP versions",
                            ShowNewFolderButton = true,
                            SelectedPath = _config.PhpVersionsDirectory
                        };
                        
                        if (folderBrowser.ShowDialog() == DialogResult.OK)
                        {
                            _config.PhpVersionsDirectory = folderBrowser.SelectedPath;
                            LogService.Log($"Custom PHP versions directory selected: {_config.PhpVersionsDirectory}", LogLevel.DEBUG);
                        }
                        else
                        {
                            LogService.Log($"No directory selected. Using default: {_config.PhpVersionsDirectory}", LogLevel.WARNING);
                        }
                    }
                    else
                    {
                        LogService.Log($"Using default PHP versions directory: {_config.PhpVersionsDirectory}", LogLevel.DEBUG);
                    }
                    
                    // Create the PHP versions directory if it doesn't exist
                    if (!System.IO.Directory.Exists(_config.PhpVersionsDirectory))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(_config.PhpVersionsDirectory);
                            LogService.Log($"Created PHP versions directory: {_config.PhpVersionsDirectory}", LogLevel.DEBUG);
                        }
                        catch (Exception ex)
                        {
                            LogService.Log($"Error creating PHP versions directory: {ex.Message}", LogLevel.ERROR);
                            MessageBox.Show(
                                $"Failed to create PHP versions directory: {_config.PhpVersionsDirectory}{Environment.NewLine}{Environment.NewLine}Error: {ex.Message}",
                                "PHP Switcher - Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }
                    
                    // Mark as not first run anymore and save configuration
                    _config.FirstRun = false;
                    ConfigService.SaveConfiguration(_config);
                }
                else
                {
                    LogService.Log($"Not first run. Using configured PHP versions directory: {_config.PhpVersionsDirectory}", LogLevel.DEBUG);
                }
                
                // Step 3: Synchronize installed PHP versions with configuration
                statusLabel.Text = "Synchronizing installed PHP versions...";
                progressBar.Value = 50;
                Refresh();
                LogService.Log("Synchronizing installed PHP versions with configuration...", LogLevel.DEBUG);
                
                // Scan the PHP versions directory for installed versions
                SynchronizeInstalledVersions();
                
                // Step 4: Fetch available PHP versions
                statusLabel.Text = "Searching for Official PHP package on php.net archives...";
                progressBar.Value = 60;
                Refresh();
                LogService.Log("Fetching available PHP versions from windows.php.net...", LogLevel.DEBUG);
                
                // Check if we need to update the PHP versions list
                bool updateNeeded = true;
                
                // In the final release, we should only update once a week
                if (_config.LastUpdated.HasValue)
                {
                    var daysSinceUpdate = (DateTime.Now - _config.LastUpdated.Value).Days;
                    
                    // For development, always update (comment out this condition in final release)
                    // updateNeeded = daysSinceUpdate >= 7;
                    
                    LogService.Log($"Last update was {daysSinceUpdate} days ago. Update needed: {updateNeeded}", LogLevel.DEBUG);
                }
                
                if (updateNeeded)
                {
                    try
                    {
                        var phpVersions = await PhpVersionService.FetchAvailablePhpVersionsAsync();
                        
                        if (phpVersions != null && phpVersions.Count > 0)
                        {
                            // Update configuration with available versions
                            _config.AvailableVersions = phpVersions;
                            _config.LastUpdated = DateTime.Now;
                            ConfigService.SaveConfiguration(_config);
                            
                            LogService.Log($"Found {phpVersions.Count} unique PHP versions available for download.", LogLevel.DEBUG);
                        }
                        else
                        {
                            LogService.Log("Failed to fetch PHP versions or no versions found.", LogLevel.WARNING);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Log($"Error fetching PHP versions: {ex.Message}", LogLevel.ERROR);
                        // Continue even if this fails - we can try again later
                        LogService.Log("Warning: Failed to fetch PHP versions. Will try again later.", LogLevel.WARNING);
                    }
                }
                else
                {
                    LogService.Log($"Using cached PHP versions from last update on {_config.LastUpdated}", LogLevel.WARNING);
                }
                
                // Initialization completed successfully
                statusLabel.Text = "Initialization complete. Starting main application...";
                progressBar.Value = 100;
                Refresh();
                LogService.Log("Initialization complete. Starting main application...", LogLevel.DEBUG);
                
                // Wait a moment to show the completion message
                await Task.Delay(1000);
                
                // Start the main application
                if (!_mainAppStarted)
                {
                    _mainAppStarted = true;
                    
                    // Create and show the main form
                    var mainForm = new MainForm(_config);
                    mainForm.FormClosed += (s, e) => Application.Exit();
                    mainForm.Show();
                    
                    // Hide this form
                    Hide();
                }
            }
            catch (Exception ex)
            {
                LogService.LogCriticalError("Error during initialization", ex);
                MessageBox.Show(
                    $"An error occurred during initialization: {ex.Message}",
                    "PHP Switcher - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                
                Application.Exit();
            }
        }
        
        private void InitializeComponent()
        {
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.statusLabel = new System.Windows.Forms.Label();
            this.titleLabel = new System.Windows.Forms.Label();
            this.subtitleLabel = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(50, 180);
            this.progressBar.MarqueeAnimationSpeed = 30;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(400, 20);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 0;
            // 
            // statusLabel
            // 
            this.statusLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.statusLabel.ForeColor = System.Drawing.Color.LightGray;
            this.statusLabel.Location = new System.Drawing.Point(0, 210);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(500, 20);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Starting...";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // titleLabel
            // 
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(0, 50);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(500, 50);
            this.titleLabel.TabIndex = 2;
            this.titleLabel.Text = "PHP Switcher";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // subtitleLabel
            // 
            this.subtitleLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.subtitleLabel.ForeColor = System.Drawing.Color.LightGray;
            this.subtitleLabel.Location = new System.Drawing.Point(0, 100);
            this.subtitleLabel.Name = "subtitleLabel";
            this.subtitleLabel.Size = new System.Drawing.Size(500, 30);
            this.subtitleLabel.TabIndex = 3;
            this.subtitleLabel.Text = "PHP Version Manager for Windows";
            this.subtitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // versionLabel
            // 
            this.versionLabel.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.versionLabel.ForeColor = System.Drawing.Color.Gray;
            this.versionLabel.Location = new System.Drawing.Point(0, 240);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(480, 20);
            this.versionLabel.TabIndex = 4;
            this.versionLabel.Text = "v1.0.0";
            this.versionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Location = new System.Drawing.Point(218, 20);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(64, 64);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.logoPictureBox.TabIndex = 5;
            this.logoPictureBox.TabStop = false;
            // 
            // SplashForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ClientSize = new System.Drawing.Size(500, 300);
            this.Controls.Add(this.logoPictureBox);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.subtitleLabel);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SplashForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PHP Switcher";
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.ResumeLayout(false);
        }
        
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subtitleLabel;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.PictureBox logoPictureBox;
        
        /// <summary>
        /// Synchronizes the installed PHP versions in the file system with the configuration.
        /// This ensures that all PHP versions installed in the configured directory are properly
        /// tracked in the application configuration.
        /// </summary>
        private void SynchronizeInstalledVersions()
        {
            try
            {
                var phpVersionsDir = _config.PhpVersionsDirectory;
                if (!Directory.Exists(phpVersionsDir))
                {
                    LogService.Log($"PHP versions directory not found: {phpVersionsDir}", LogLevel.WARNING);
                    return;
                }
                
                LogService.Log($"Scanning directory for PHP installations: {phpVersionsDir}", LogLevel.DEBUG);
                var directories = Directory.GetDirectories(phpVersionsDir);
                int foundVersions = 0;
                int addedVersions = 0;
                
                // Check if C:\php exists and is a symbolic link to determine active version
                string activePhpPath = null;
                if (Directory.Exists(@"C:\php"))
                {
                    var dirInfo = new DirectoryInfo(@"C:\php");
                    if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        activePhpPath = dirInfo.LinkTarget;
                        LogService.Log($"Found active PHP symbolic link pointing to: {activePhpPath}", LogLevel.DEBUG);
                    }
                }
                
                foreach (var dir in directories)
                {
                    // Check if this is a PHP directory by looking for php.exe
                    var phpExe = Path.Combine(dir, "php.exe");
                    if (File.Exists(phpExe))
                    {
                        foundVersions++;
                        
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
                                    InstallDate = File.GetCreationTime(dir),
                                    IsActive = (activePhpPath != null && activePhpPath.Equals(dir, StringComparison.OrdinalIgnoreCase))
                                };
                                
                                _config.InstalledVersions.Add(newInstalledVersion);
                                addedVersions++;
                                
                                LogService.Log($"Added PHP {version} {(isNTS ? "(NTS)" : "(TS)")} to installed versions", LogLevel.DEBUG);
                            }
                            else
                            {
                                // Update installation path and active status if needed
                                if (existingVersion.InstallPath != dir)
                                {
                                    existingVersion.InstallPath = dir;
                                    LogService.Log($"Updated path for PHP {version} {(isNTS ? "(NTS)" : "(TS)")}: {dir}", LogLevel.DEBUG);
                                }
                                
                                // Update active status based on symbolic link
                                bool shouldBeActive = (activePhpPath != null && activePhpPath.Equals(dir, StringComparison.OrdinalIgnoreCase));
                                if (existingVersion.IsActive != shouldBeActive)
                                {
                                    existingVersion.IsActive = shouldBeActive;
                                    LogService.Log($"Updated active status for PHP {version} {(isNTS ? "(NTS)" : "(TS)")}: {shouldBeActive}", LogLevel.DEBUG);
                                }
                            }
                        }
                        else
                        {
                            LogService.Log($"Could not extract version information from directory name: {dirName}", LogLevel.WARNING);
                        }
                    }
                }
                
                // Remove versions from configuration that no longer exist in the file system
                var versionsToRemove = _config.InstalledVersions
                    .Where(v => !Directory.Exists(v.InstallPath))
                    .ToList();
                
                foreach (var version in versionsToRemove)
                {
                    _config.InstalledVersions.Remove(version);
                    LogService.Log($"Removed PHP {version.Version} {(version.IsNTS ? "(NTS)" : "(TS)")} from configuration as it no longer exists", LogLevel.DEBUG);
                }
                
                // Save the updated configuration
                ConfigService.SaveConfiguration(_config);
                
                LogService.Log($"Synchronization complete. Found {foundVersions} PHP installations, added {addedVersions} to configuration, removed {versionsToRemove.Count} from configuration.", LogLevel.DEBUG);
            }
            catch (Exception ex)
            {
                LogService.Log($"Error synchronizing installed PHP versions: {ex.Message}", LogLevel.ERROR);
            }
        }
    }
}