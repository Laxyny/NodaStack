using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using ModernWpf.Controls;
using System.Diagnostics;
using System.Reflection;
using NodaStack.Services;
using NodaStack.Views.Windows;
using NodaStack.Core.Models;

namespace NodaStack.Pages
{
    public partial class SettingsPage : System.Windows.Controls.Page
    {
        private ConfigurationManager configManager;

        private UpdateManager _updateManager;

        public SettingsPage()
        {
            InitializeComponent();
            configManager = new ConfigurationManager();
            _updateManager = new UpdateManager();
            LoadSettings();
        }

        public async void StartAutoUpdate()
        {
             CheckUpdates_Click(null, null);
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdatesButton.IsEnabled = false;
            UpdateStatusText.Text = "Checking for updates...";
            UpdateProgressBar.Visibility = Visibility.Visible;
            UpdateProgressBar.IsIndeterminate = true;

            try
            {
                var (hasUpdate, info) = await _updateManager.CheckForUpdatesAsync();

                UpdateProgressBar.IsIndeterminate = false;
                UpdateProgressBar.Value = 0;

                if (hasUpdate && info != null)
                {
                    UpdateStatusText.Text = $"New version found: {info.Version}. Downloading...";
                    CheckUpdatesButton.Content = "Downloading...";
                    
                    var progress = new Progress<double>(p => UpdateProgressBar.Value = p);
                    await _updateManager.DownloadAndInstallAsync(info, progress);
                }
                else
                {
                    UpdateStatusText.Text = "You are up to date.";
                    UpdateProgressBar.Visibility = Visibility.Collapsed;
                    CheckUpdatesButton.Content = "Check for Updates";
                    
                    await Task.Delay(2000);
                    UpdateStatusText.Text = "Check for the latest version of NodaStack.";
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = "Error checking updates.";
                MessageBox.Show(ex.Message, "Update Error");
                UpdateProgressBar.Visibility = Visibility.Collapsed;
            }
            finally
            {
                CheckUpdatesButton.IsEnabled = true;
                if (CheckUpdatesButton.Content.ToString() == "Downloading...")
                     CheckUpdatesButton.Content = "Check for Updates";
            }
        }

        private void LoadSettings()
        {
            var config = configManager.GetConfiguration();
            ApachePortBox.Value = config.Ports["Apache"];
            PhpPortBox.Value = config.Ports["PHP"];
            MysqlPortBox.Value = config.Ports["MySQL"];
            PmaPortBox.Value = config.Ports.TryGetValue("phpMyAdmin", out int pma) ? pma : 8081;
            MailHogWebPortBox.Value = config.Ports.TryGetValue("MailHogWeb", out int mh) ? mh : 8025;
            
            AutoStartSwitch.IsOn = config.Settings.AutoStartServices;
            DarkModeSwitch.IsOn = config.Settings.DarkMode;
            ProjectsPathBox.Text = config.Settings.ProjectsPath;

            MinimizeToTraySwitch.IsOn = config.Settings.MinimizeToTray;
            StartMinimizedSwitch.IsOn = config.Settings.StartMinimized;
            KeepDockerRunningSwitch.IsOn = config.Settings.KeepDockerRunning;

            // Load App Version
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                AppVersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
            }
        }

        private void BrowsePath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select the directory where your web projects are stored.";
                dialog.UseDescriptionForTitle = true;
                dialog.ShowNewFolderButton = true;
                
                if (!string.IsNullOrWhiteSpace(ProjectsPathBox.Text))
                {
                    dialog.SelectedPath = ProjectsPathBox.Text;
                }

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    ProjectsPathBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate and Get Ports
                int GetPort(ModernWpf.Controls.NumberBox box, int defaultPort)
                {
                    if (double.IsNaN(box.Value)) return defaultPort;
                    return (int)box.Value;
                }

                int apache = GetPort(ApachePortBox, 8080);
                int php = GetPort(PhpPortBox, 8000);
                int mysql = GetPort(MysqlPortBox, 3306);
                int pma = GetPort(PmaPortBox, 8081);
                int mailhogWeb = GetPort(MailHogWebPortBox, 8025);

                if (apache < 1 || apache > 65535 || php < 1 || php > 65535 || 
                    mysql < 1 || mysql > 65535 || pma < 1 || pma > 65535 || 
                    mailhogWeb < 1 || mailhogWeb > 65535)
                {
                    MessageBox.Show("All ports must be between 1 and 65535.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newPorts = new Dictionary<string, int>
                {
                    { "Apache", apache },
                    { "PHP", php },
                    { "MySQL", mysql },
                    { "phpMyAdmin", pma },
                    { "MailHogWeb", mailhogWeb },
                    { "MailHogSMTP", configManager.Configuration.Ports.TryGetValue("MailHogSMTP", out int smtp) ? smtp : 1025 }
                };
                configManager.UpdatePorts(newPorts);

                // Update Settings
                var settings = configManager.Configuration.Settings;
                settings.AutoStartServices = AutoStartSwitch.IsOn;
                settings.DarkMode = DarkModeSwitch.IsOn;
                
                // Validate Projects Path
                if (!string.IsNullOrWhiteSpace(ProjectsPathBox.Text))
                {
                    settings.ProjectsPath = ProjectsPathBox.Text;
                }

                settings.MinimizeToTray = MinimizeToTraySwitch.IsOn;
                settings.StartMinimized = StartMinimizedSwitch.IsOn;
                settings.KeepDockerRunning = KeepDockerRunningSwitch.IsOn;
                
                configManager.UpdateSettings(settings);
                configManager.SaveConfiguration();

                // Notify Main Window to reload config and apply changes immediately
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.ReloadConfiguration();
                }

                StatusText.Text = "Settings saved successfully!";
                MessageBox.Show("Settings saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = Task.Delay(3000).ContinueWith(_ => Dispatcher.Invoke(() => StatusText.Text = ""));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DarkModeSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch ts)
            {
                ThemeManager.IsDarkTheme = ts.IsOn;
            }
        }

        private async void RebuildImages_Click(object sender, RoutedEventArgs e)
        {
            RebuildButton.IsEnabled = false;
            var progress = new Progress<string>(status => 
            {
                BuildStatusText.Text = status;
                BuildStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            });

            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    await mainWindow.BuildDockerImages(progress);
                    BuildStatusText.Text = "All images rebuilt successfully!";
                    BuildStatusText.Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("SuccessBrush");
                    MessageBox.Show("Docker images rebuilt successfully!", "Success");
                }
            }
            catch (Exception ex)
            {
                BuildStatusText.Text = "Build failed. Check Docker Desktop.";
                BuildStatusText.Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("ErrorBrush");
                MessageBox.Show($"Error building images: {ex.Message}\nPlease ensure Docker Desktop is running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RebuildButton.IsEnabled = true;
            }
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }
    }
}
