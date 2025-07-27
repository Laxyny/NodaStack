using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace NodaStack
{
    public partial class ConfigurationWindow : Window
    {
        private ConfigurationManager configManager;
        private NodaStackConfiguration? originalConfig;
        private CancellationTokenSource cancellationTokenSource = new();

        public ConfigurationWindow(ConfigurationManager configManager)
        {
            InitializeComponent();
            this.configManager = configManager;
            this.originalConfig = JsonClone(configManager.Configuration);

            LoadConfiguration();
            SetupEventHandlers();

            var assembly = Assembly.GetExecutingAssembly();
            var versionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            CurrentVersionText.Text = versionAttribute?.Version ?? "0.0.0.0";

            CheckLatestVersion();
        }

        private void LoadConfiguration()
        {
            var config = configManager.GetConfiguration();
            originalConfig = JsonClone(config);

            ApachePortTextBox.Text = config.Ports["Apache"].ToString();
            PhpPortTextBox.Text = config.Ports["PHP"].ToString();
            MySqlPortTextBox.Text = config.Ports["MySQL"].ToString();
            PhpMyAdminPortTextBox.Text = config.Ports["phpMyAdmin"].ToString();

            // Set version texts instead of combo boxes
            ApacheVersionText.Text = config.Versions["Apache"];
            PhpVersionText.Text = config.Versions["PHP"];
            MySqlVersionText.Text = config.Versions["MySQL"];

            AutoStartCheckBox.IsChecked = config.Settings.AutoStartServices;
            NotificationsCheckBox.IsChecked = config.Settings.ShowNotifications;
            DetailedLoggingCheckBox.IsChecked = config.Settings.EnableLogging;
            DarkModeCheckBox.IsChecked = config.Settings.DarkMode;
            AutoRefreshCheckBox.IsChecked = config.Settings.AutoRefreshProjects;
            SslSupportCheckBox.IsChecked = config.Settings.EnableSsl;
            
            MySqlPasswordBox.Password = config.Settings.MySqlPassword;
            MySqlDatabaseBox.Text = config.Settings.MySqlDefaultDatabase;
            ProjectsDirectoryBox.Text = config.Settings.ProjectsPath;

            // Load Behavior Settings
            MinimizeToTrayCheckBox.IsChecked = config.Settings.MinimizeToTray;
            StartMinimizedCheckBox.IsChecked = config.Settings.StartMinimized;
            ShowTrayNotificationsCheckBox.IsChecked = config.Settings.ShowTrayNotifications;
            AutoStartDockerCheckBox.IsChecked = config.Settings.AutoStartDocker;
            KeepDockerRunningCheckBox.IsChecked = config.Settings.KeepDockerRunning;

            // Set default status text
            ApachePortStatus.Text = "Checking...";
            PhpPortStatus.Text = "Checking...";
            MySqlPortStatus.Text = "Checking...";
            PhpMyAdminPortStatus.Text = "Checking...";

            _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                Dispatcher.Invoke(() => CheckPorts_Click(null, null));
            });
        }





        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Owner as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.CheckForUpdatesManually(sender, e);
            }
        }

        private void SetupEventHandlers()
        {
            ApachePortTextBox.TextChanged += (s, e) => DelayedPortCheck();
            PhpPortTextBox.TextChanged += (s, e) => DelayedPortCheck();
            MySqlPortTextBox.TextChanged += (s, e) => DelayedPortCheck();
            PhpMyAdminPortTextBox.TextChanged += (s, e) => DelayedPortCheck();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private Timer? portCheckTimer;
        private void DelayedPortCheck()
        {
            portCheckTimer?.Dispose();
            portCheckTimer = new Timer(async _ =>
            {
                await Task.Run(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        CheckPortStatus(ApachePortTextBox, ApachePortStatus);
                        CheckPortStatus(PhpPortTextBox, PhpPortStatus);
                        CheckPortStatus(MySqlPortTextBox, MySqlPortStatus);
                        CheckPortStatus(PhpMyAdminPortTextBox, PhpMyAdminPortStatus);
                    });
                });
            }, null, 500, Timeout.Infinite);
        }

        private void CheckPortStatus(TextBox portTextBox, TextBlock statusTextBlock)
        {
            if (int.TryParse(portTextBox.Text, out int port) && port > 0 && port <= 65535)
            {
                Task.Run(() =>
                {
                    bool isAvailable = configManager.IsPortAvailable(port);
                    Dispatcher.Invoke(() =>
                    {
                        if (isAvailable)
                        {
                            statusTextBlock.Text = "✓ Available";
                            statusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                            portTextBox.Background = Application.Current.Resources["TextBoxBackgroundBrush"] as SolidColorBrush ?? Brushes.White;
                        }
                        else
                        {
                            statusTextBlock.Text = "✗ Port in use";
                            statusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                            portTextBox.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Light red
                        }
                    });
                });
            }
            else
            {
                statusTextBlock.Text = "✗ Invalid port";
                statusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                portTextBox.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Light red
            }
        }

        private void CheckPorts_Click(object? sender, RoutedEventArgs? e)
        {
            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    CheckPortStatus(ApachePortTextBox, ApachePortStatus);
                    CheckPortStatus(PhpPortTextBox, PhpPortStatus);
                    CheckPortStatus(MySqlPortTextBox, MySqlPortStatus);
                    CheckPortStatus(PhpMyAdminPortTextBox, PhpMyAdminPortStatus);
                });
            });
        }

        private void ResetPorts_Click(object sender, RoutedEventArgs e)
        {
            var defaultConfig = new NodaStackConfiguration();

            ApachePortTextBox.Text = defaultConfig.Ports["Apache"].ToString();
            PhpPortTextBox.Text = defaultConfig.Ports["PHP"].ToString();
            MySqlPortTextBox.Text = defaultConfig.Ports["MySQL"].ToString();
            PhpMyAdminPortTextBox.Text = defaultConfig.Ports["phpMyAdmin"].ToString();

            DelayedPortCheck();
        }

        private void BrowseProjectsPath_Click(object sender, RoutedEventArgs e)
        {
            var currentPath = ProjectsDirectoryBox.Text;

            if (System.IO.Directory.Exists(currentPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", currentPath);
            }
            else
            {
                System.Diagnostics.Process.Start("explorer.exe", System.IO.Directory.GetCurrentDirectory());
            }

            MessageBox.Show("Please copy the desired directory path and paste it in the text box.", "Select Directory", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all settings to defaults?\n\nThis action cannot be undone.",
                "Reset All Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                configManager.ResetToDefaults();
                LoadConfiguration();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidatePorts())
                {
                    MessageBox.Show("Please fix port configuration issues before saving.", "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newPorts = new Dictionary<string, int>
                {
                    { "Apache", int.Parse(ApachePortTextBox.Text) },
                    { "PHP", int.Parse(PhpPortTextBox.Text) },
                    { "MySQL", int.Parse(MySqlPortTextBox.Text) },
                    { "phpMyAdmin", int.Parse(PhpMyAdminPortTextBox.Text) }
                };
                configManager.UpdatePorts(newPorts);

                var newVersions = new Dictionary<string, string>
                {
                    { "PHP", PhpVersionText.Text },
                    { "MySQL", MySqlVersionText.Text },
                    { "Apache", ApacheVersionText.Text }
                };
                configManager.UpdateVersions(newVersions);

                var newSettings = new NodaStackSettings
                {
                    AutoStartServices = AutoStartCheckBox.IsChecked ?? false,
                    ShowNotifications = NotificationsCheckBox.IsChecked ?? true,
                    EnableLogging = DetailedLoggingCheckBox.IsChecked ?? true,
                    DarkMode = DarkModeCheckBox.IsChecked ?? false,
                    AutoRefreshProjects = AutoRefreshCheckBox.IsChecked ?? true,
                    EnableSsl = SslSupportCheckBox.IsChecked ?? false,
                    MySqlPassword = MySqlPasswordBox.Password,
                    MySqlDefaultDatabase = MySqlDatabaseBox.Text,
                    ProjectsPath = ProjectsDirectoryBox.Text,
                    DefaultBrowser = "default",
                    LogRetentionDays = 7,
                    AutoCheckUpdates = true,
                    AutoInstallUpdates = false,
                    Language = "en",
                    
                    // Behavior Settings - IMPORTANT: Sauvegarder ces paramètres
                    MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false,
                    StartMinimized = StartMinimizedCheckBox.IsChecked ?? false,
                    ShowTrayNotifications = ShowTrayNotificationsCheckBox.IsChecked ?? true,
                    AutoStartDocker = AutoStartDockerCheckBox.IsChecked ?? false,
                    KeepDockerRunning = KeepDockerRunningCheckBox.IsChecked ?? true,
                };

                configManager.UpdateSettings(newSettings);
                
                // Sauvegarder explicitement la configuration
                configManager.SaveConfiguration();

                MessageBox.Show("Configuration saved successfully!", "Configuration Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private bool ValidatePorts()
        {
            var ports = new[]
            {
                ApachePortTextBox.Text,
                PhpPortTextBox.Text,
                MySqlPortTextBox.Text,
                PhpMyAdminPortTextBox.Text
            };

            var parsedPorts = new List<int>();

            foreach (var portText in ports)
            {
                if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
                {
                    return false;
                }

                if (parsedPorts.Contains(port))
                {
                    MessageBox.Show($"Port {port} is used multiple times. Each service must use a different port.", "Duplicate Port", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                parsedPorts.Add(port);
            }

            return true;
        }

        private NodaStackConfiguration? JsonClone(NodaStackConfiguration original)
        {
            var json = JsonSerializer.Serialize(original);
            return JsonSerializer.Deserialize<NodaStackConfiguration>(json);
        }

        protected override void OnClosed(EventArgs e)
        {
            cancellationTokenSource.Cancel();
            portCheckTimer?.Dispose();
            base.OnClosed(e);
        }

        private async void CheckLatestVersion()
        {
            try
            {
                var updateChecker = new UpdateChecker();
                var updateInfo = await updateChecker.CheckForUpdatesAsync();

                if (updateInfo != null)
                {
                    LatestVersionText.Text = updateInfo.Version;

                    if (updateInfo.IsUpdateAvailable)
                    {
                        LatestVersionText.Foreground = new SolidColorBrush(Colors.Green);
                        DownloadUpdateButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LatestVersionText.Foreground = new SolidColorBrush(Colors.Black);
                        DownloadUpdateButton.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    LatestVersionText.Text = "Unknown";
                    LatestVersionText.Foreground = new SolidColorBrush(Colors.Gray);
                    DownloadUpdateButton.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                LatestVersionText.Text = "Error checking";
                LatestVersionText.Foreground = new SolidColorBrush(Colors.Red);
                DownloadUpdateButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void DownloadAndInstallUpdate_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Owner as MainWindow;
            if (mainWindow != null)
            {
                await mainWindow.DownloadAndInstallUpdate();
            }
        }

        // Méthodes pour les nouveaux événements Click
        private void BrowseProjectsButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseProjectsPath_Click(sender, e);
        }

        private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdates_Click(sender, e);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetAll_Click(sender, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cancel_Click(sender, e);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save_Click(sender, e);
        }


    }
}