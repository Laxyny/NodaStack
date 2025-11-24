using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using ModernWpf.Controls;

namespace NodaStack.Pages
{
    public partial class SettingsPage : System.Windows.Controls.Page
    {
        private ConfigurationManager configManager;

        public SettingsPage()
        {
            InitializeComponent();
            configManager = new ConfigurationManager();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var config = configManager.GetConfiguration();
            ApachePortBox.Value = config.Ports["Apache"];
            PhpPortBox.Value = config.Ports["PHP"];
            MysqlPortBox.Value = config.Ports["MySQL"];
            
            AutoStartSwitch.IsOn = config.Settings.AutoStartServices;
            DarkModeSwitch.IsOn = config.Settings.DarkMode;
            ProjectsPathBox.Text = config.Settings.ProjectsPath;
        }

        private void BrowsePath_Click(object sender, RoutedEventArgs e)
        {
            // For now simple stub
            MessageBox.Show("Browse folder dialog placeholder.");
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newPorts = new Dictionary<string, int>
                {
                    { "Apache", (int)ApachePortBox.Value },
                    { "PHP", (int)PhpPortBox.Value },
                    { "MySQL", (int)MysqlPortBox.Value },
                    { "phpMyAdmin", 8081 }
                };
                configManager.UpdatePorts(newPorts);

                var settings = configManager.Configuration.Settings;
                settings.AutoStartServices = AutoStartSwitch.IsOn;
                settings.DarkMode = DarkModeSwitch.IsOn;
                settings.ProjectsPath = ProjectsPathBox.Text;
                
                configManager.UpdateSettings(settings);
                configManager.SaveConfiguration();

                StatusText.Text = "Settings saved successfully!";
                _ = Task.Delay(3000).ContinueWith(_ => Dispatcher.Invoke(() => StatusText.Text = ""));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving: {ex.Message}", "Error");
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
    }
}
