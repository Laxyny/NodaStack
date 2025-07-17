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
            var config = configManager.Configuration;

            ApachePortTextBox.Text = config.Ports["Apache"].ToString();
            PhpPortTextBox.Text = config.Ports["PHP"].ToString();
            MySqlPortTextBox.Text = config.Ports["MySQL"].ToString();
            PhpMyAdminPortTextBox.Text = config.Ports["phpMyAdmin"].ToString();

            SetComboBoxValue(PhpVersionComboBox, config.Versions["PHP"]);
            SetComboBoxValue(MySqlVersionComboBox, config.Versions["MySQL"]);
            SetComboBoxValue(ApacheVersionComboBox, config.Versions["Apache"]);

            AutoStartServicesCheckBox.IsChecked = config.Settings.AutoStartServices;
            ShowNotificationsCheckBox.IsChecked = config.Settings.ShowNotifications;
            EnableLoggingCheckBox.IsChecked = config.Settings.EnableLogging;
            DarkModeCheckBox.IsChecked = config.Settings.DarkMode;
            AutoRefreshProjectsCheckBox.IsChecked = config.Settings.AutoRefreshProjects;
            EnableSslCheckBox.IsChecked = config.Settings.EnableSsl;
            EnableAutoUpdatesCheckBox.IsChecked = config.Settings.AutoCheckUpdates;
            AutoInstallUpdatesCheckBox.IsChecked = config.Settings.AutoInstallUpdates;
            MySqlPasswordBox.Password = config.Settings.MySqlPassword;
            MySqlDefaultDbTextBox.Text = config.Settings.MySqlDefaultDatabase;
            ProjectsPathTextBox.Text = config.Settings.ProjectsPath;

            _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                Dispatcher.Invoke(() => CheckPorts_Click(null, null));
            });
        }

        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content?.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
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
                            statusTextBlock.Foreground = Brushes.Green;
                            portTextBox.Background = Brushes.White;
                        }
                        else
                        {
                            statusTextBlock.Text = "✗ Port in use";
                            statusTextBlock.Foreground = Brushes.Red;
                            portTextBox.Background = Brushes.LightPink;
                        }
                    });
                });
            }
            else
            {
                statusTextBlock.Text = "✗ Invalid port";
                statusTextBlock.Foreground = Brushes.Red;
                portTextBox.Background = Brushes.LightPink;
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
            var currentPath = ProjectsPathTextBox.Text;

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
                    { "PHP", GetComboBoxValue(PhpVersionComboBox) },
                    { "MySQL", GetComboBoxValue(MySqlVersionComboBox) },
                    { "Apache", GetComboBoxValue(ApacheVersionComboBox) }
                };
                configManager.UpdateVersions(newVersions);

                var newSettings = new NodaStackSettings
                {
                    AutoStartServices = AutoStartServicesCheckBox.IsChecked ?? false,
                    ShowNotifications = ShowNotificationsCheckBox.IsChecked ?? true,
                    EnableLogging = EnableLoggingCheckBox.IsChecked ?? true,
                    DarkMode = DarkModeCheckBox.IsChecked ?? false,
                    AutoRefreshProjects = AutoRefreshProjectsCheckBox.IsChecked ?? true,
                    EnableSsl = EnableSslCheckBox.IsChecked ?? false,
                    MySqlPassword = MySqlPasswordBox.Password,
                    MySqlDefaultDatabase = MySqlDefaultDbTextBox.Text,
                    ProjectsPath = ProjectsPathTextBox.Text,
                    DefaultBrowser = "default",
                    LogRetentionDays = 7,
                    AutoCheckUpdates = EnableAutoUpdatesCheckBox.IsChecked ?? true,
                    AutoInstallUpdates = AutoInstallUpdatesCheckBox.IsChecked ?? false,
                    Language = "en"
                };
                configManager.UpdateSettings(newSettings);

                MessageBox.Show("Configuration saved successfully!", "Configuration Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetComboBoxValue(ComboBox comboBox)
        {
            return ((ComboBoxItem?)comboBox.SelectedItem)?.Content?.ToString() ?? "8.2";
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
                        DownloadUpdateButton.IsEnabled = true;
                    }
                    else
                    {
                        LatestVersionText.Foreground = new SolidColorBrush(Colors.Black);
                    }
                }
                else
                {
                    LatestVersionText.Text = "Unknown";
                    LatestVersionText.Foreground = new SolidColorBrush(Colors.Gray);
                }
            }
            catch (Exception ex)
            {
                LatestVersionText.Text = "Error checking";
                LatestVersionText.Foreground = new SolidColorBrush(Colors.Red);
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
    }
}