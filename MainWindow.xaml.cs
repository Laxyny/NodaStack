using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ModernWpf.Controls;
using NodaStack.Pages;

namespace NodaStack
{
    public partial class MainWindow : Window
    {
        // Services
        private ProjectManager projectManager;
        private ConfigurationManager configManager;
        private LogManager logManager;
        
        // Pages
        private DashboardPage _dashboardPage;
        private MonitoringPage _monitoringPage;
        private BackupPage _backupPage;
        private SettingsPage _settingsPage;
        
        // État des services (Centralisé ici pour l'instant)
        private bool apacheIsRunning = false;
        private bool phpIsRunning = false;
        private bool mysqlIsRunning = false;
        private bool phpmyadminIsRunning = false;

        public MainWindow()
        {
            InitializeComponent();

            // Init Managers
            ThemeManager.Initialize(false);
            configManager = new ConfigurationManager();
            projectManager = new ProjectManager();
            logManager = new LogManager();

            // Init Pages
            _dashboardPage = new DashboardPage();
            _dashboardPage.ServiceToggleRequested += DashboardPage_ServiceToggleRequested;
            _monitoringPage = new MonitoringPage();
            _backupPage = new BackupPage();
            _settingsPage = new SettingsPage();

            // Initial Check
            CheckInitialContainerStatus();
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // Naviguer vers Dashboard par défaut
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(_settingsPage);
            }
            else if (args.SelectedItem is NavigationViewItem item)
            {
                switch (item.Tag.ToString())
                {
                    case "dashboard":
                        ContentFrame.Navigate(_dashboardPage);
                        UpdateDashboardState(); // Refresh UI
                        break;
                    case "monitoring":
                        ContentFrame.Navigate(_monitoringPage);
                        break;
                    case "backups":
                        ContentFrame.Navigate(_backupPage);
                        break;
                }
            }
        }

        // =================================================================================
        // LOGIQUE SERVICES
        // =================================================================================

        private async void DashboardPage_ServiceToggleRequested(object? sender, string service)
        {
            switch (service)
            {
                case "apache": await ToggleApache(); break;
                case "php": await TogglePhp(); break;
                case "mysql": await ToggleMysql(); break;
                case "phpmyadmin": await TogglePma(); break;
            }
        }

        private void UpdateDashboardState()
        {
            _dashboardPage.UpdateServiceStatus("apache", apacheIsRunning);
            _dashboardPage.UpdateServiceStatus("php", phpIsRunning);
            _dashboardPage.UpdateServiceStatus("mysql", mysqlIsRunning);
            _dashboardPage.UpdateServiceStatus("phpmyadmin", phpmyadminIsRunning);
        }

        private async Task ToggleApache()
            {
                var config = configManager.GetConfiguration();
            if (!apacheIsRunning)
            {
                await RunProcessAsync($"docker run -d --rm -p {config.ApachePort}:80 -v \"{projectManager.ProjectsPath}:/var/www/html\" --name nodastack_apache nodastack_apache");
                    apacheIsRunning = true;
            }
            else
            {
                await RunProcessAsync("docker stop nodastack_apache");
                    apacheIsRunning = false;
            }
            UpdateDashboardState();
        }

        private async Task TogglePhp()
        {
            var config = configManager.GetConfiguration();
            if (!phpIsRunning)
            {
                await RunProcessAsync($"docker run -d --rm -p {config.PhpPort}:8000 -v \"{projectManager.ProjectsPath}:/var/www/html\" --name nodastack_php nodastack_php");
                    phpIsRunning = true;
            }
            else
            {
                await RunProcessAsync("docker stop nodastack_php");
                    phpIsRunning = false;
            }
            UpdateDashboardState();
        }

        private async Task ToggleMysql()
        {
            var config = configManager.GetConfiguration();
            if (!mysqlIsRunning)
            {
                await RunProcessAsync($"docker run -d --rm -p {config.MySqlPort}:3306 --name nodastack_mysql nodastack_mysql");
                    mysqlIsRunning = true;
            }
            else
            {
                await RunProcessAsync("docker stop nodastack_mysql");
                        mysqlIsRunning = false;
            }
            UpdateDashboardState();
        }

        private async Task TogglePma()
        {
            var config = configManager.GetConfiguration();
            if (!phpmyadminIsRunning)
            {
                await RunProcessAsync($"docker run -d --rm -p {config.PhpMyAdminPort}:80 --name nodastack_phpmyadmin --link nodastack_mysql -e PMA_HOST=nodastack_mysql nodastack_phpmyadmin");
                    phpmyadminIsRunning = true;
            }
            else
            {
                await RunProcessAsync("docker stop nodastack_phpmyadmin");
                    phpmyadminIsRunning = false;
            }
            UpdateDashboardState();
        }

        private async Task RunProcessAsync(string command)
        {
            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C {command}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    var p = Process.Start(psi);
                    p?.WaitForExit();
                }
                catch (Exception ex) { Debug.WriteLine(ex.Message); }
            });
        }

        // =================================================================================
        // DOCKER MANAGEMENT
        // =================================================================================

        public async Task BuildDockerImages()
        {
            // Determine root path (where Docker folder is)
            // Assuming run from bin/Debug/net9.0-windows, we need to go up
            // Or use projectManager.ProjectsPath parent? No, that's user data.
            // Let's try to find "Docker" folder relative to AppDomain.CurrentDomain.BaseDirectory
            
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..")); // Adjust based on build depth
            
            // If not found, try current dir (portable mode)
            if (!Directory.Exists(Path.Combine(projectRoot, "Docker")))
            {
                projectRoot = Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(Path.Combine(projectRoot, "Docker")))
            {
                MessageBox.Show($"Docker folder not found in {projectRoot}", "Error");
                return;
            }

            await RunProcessAsync($"docker build -t nodastack_apache \"{Path.Combine(projectRoot, "Docker", "apache")}\"");
            await RunProcessAsync($"docker build -t nodastack_php \"{Path.Combine(projectRoot, "Docker", "php")}\"");
            await RunProcessAsync($"docker build -t nodastack_mysql \"{Path.Combine(projectRoot, "Docker", "mysql")}\"");
            await RunProcessAsync($"docker build -t nodastack_phpmyadmin \"{Path.Combine(projectRoot, "Docker", "phpmyadmin")}\"");
        }

        private async void CheckInitialContainerStatus()
        {
            // Check if images exist, if not, maybe prompt? 
            // For now, assume they exist or user will click Rebuild in Settings if things fail.
            // Real check logic could go here.
            
            UpdateDashboardState();
            await Task.CompletedTask;
        }

        // =================================================================================
        // UPDATES (Pour ConfigurationWindow)
        // =================================================================================

        public void CheckForUpdatesManually(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Update check stub.", "Info");
        }

        public async Task DownloadAndInstallUpdate()
        {
            await Task.Delay(100);
            MessageBox.Show("Update install stub.", "Info");
        }

        // =================================================================================
        // ACTIONS
        // =================================================================================

        private void OpenConfiguration()
        {
            var configWindow = new ConfigurationWindow(configManager);
            configWindow.Owner = this;
            configWindow.ShowDialog();
        }

        private void OpenMonitoring()
        {
            var monitoringWindow = new MonitoringWindow();
            monitoringWindow.Owner = this;
            monitoringWindow.Show();
        }

        private void OpenBackups()
        {
            var backupWindow = new BackupWindow();
            backupWindow.Owner = this;
            backupWindow.ShowDialog();
        }

        public void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            projectManager.CreateProject("NewProject_" + DateTime.Now.Ticks);
        }

        public void ViewApache_Direct(ProjectInfo project)
        {
            var config = configManager.GetConfiguration();
            try { Process.Start(new ProcessStartInfo($"http://localhost:{config.ApachePort}/{project.Name}") { UseShellExecute = true }); } catch { }
        }
    }
}
