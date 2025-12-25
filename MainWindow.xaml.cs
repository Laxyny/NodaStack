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
        
        // System Tray
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        
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
        private bool mailhogIsRunning = false;

        public MainWindow()
        {
            InitializeComponent();

            // Init Managers
            configManager = new ConfigurationManager();
            projectManager = new ProjectManager();
            logManager = new LogManager();

            // Apply Settings
            var config = configManager.GetConfiguration();
            
            // Theme
            ThemeManager.Initialize(false);
            if (config.Settings.DarkMode)
            {
                ThemeManager.IsDarkTheme = true;
            }

            // Projects Path
            if (!string.IsNullOrEmpty(config.Settings.ProjectsPath))
            {
                projectManager.UpdateProjectsPath(config.Settings.ProjectsPath);
            }

            // Init System Tray
                InitializeSystemTray();

            // Init Pages
            try
            {
                _dashboardPage = new DashboardPage(projectManager);
                _dashboardPage.ServiceToggleRequested += DashboardPage_ServiceToggleRequested;
                _monitoringPage = new MonitoringPage();
                _backupPage = new BackupPage();
                _settingsPage = new SettingsPage();
                }
                catch (Exception ex)
                {
                MessageBox.Show($"Error initializing pages: {ex.Message}\n{ex.InnerException?.Message}", "Startup Error");
                // Continue or exit? If pages fail, app is broken.
            }
            
            // Start Minimized
            if (config.Settings.StartMinimized)
            {
                this.WindowState = WindowState.Minimized;
                this.Hide();
            }

            // Initial Check
            CheckInitialContainerStatus();
            
            // Auto Update Check
            _ = CheckForUpdatesSilent();
        }

        private async Task CheckForUpdatesSilent()
        {
            try
            {
                var updateManager = new UpdateManager();
                var (hasUpdate, info) = await updateManager.CheckForUpdatesAsync();

                if (hasUpdate && info != null)
                {
                    var result = MessageBox.Show($"A new version ({info.Version}) of NodaStack is available.\nInstall it now?", 
                        "Update Available", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                         // Select Settings in NavView
                         NavView.SelectedItem = NavView.SettingsItem;
                         ContentFrame.Navigate(_settingsPage);
                         
                         // Wait a bit for UI to load then trigger
                         await Task.Delay(500);
                         _settingsPage.StartAutoUpdate();
                    }
                }
            }
            catch { /* Silent fail */ }
        }

        private void InitializeSystemTray()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            try 
            {
                 // Load icon from file since we are in a direct file context mostly
                 if (File.Exists("Assets/NodaStackLogo.ico"))
                    _notifyIcon.Icon = new System.Drawing.Icon("Assets/NodaStackLogo.ico");
                 else
                    _notifyIcon.Text = "NodaStack (No Icon)"; // Fallback
            } 
            catch { }
            
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "NodaStack - Local Server Environment";
            _notifyIcon.DoubleClick += (s, args) => 
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Open Dashboard", null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit NodaStack", null, (s, e) => { CleanupAndExit(); });
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            base.OnStateChanged(e);
        }
        
        protected override void OnClosed(EventArgs e)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            base.OnClosed(e);
        }

        private void CleanupAndExit()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
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

        private async void DashboardPage_ServiceToggleRequested(object? sender, string service)
        {
            switch (service)
            {
                case "apache": await ToggleApache(); break;
                case "php": await TogglePhp(); break;
                case "mysql": await ToggleMysql(); break;
                case "phpmyadmin": await TogglePma(); break;
                case "mailhog": await ToggleMailHog(); break;
            }
        }

        private void UpdateDashboardState()
        {
            _dashboardPage.UpdateServiceStatus("apache", apacheIsRunning);
            _dashboardPage.UpdateServiceStatus("php", phpIsRunning);
            _dashboardPage.UpdateServiceStatus("mysql", mysqlIsRunning);
            _dashboardPage.UpdateServiceStatus("phpmyadmin", phpmyadminIsRunning);
            _dashboardPage.UpdateServiceStatus("mailhog", mailhogIsRunning);
        }

        private async Task StopContainer(string containerName)
        {
            await RunProcessAsync($"docker stop {containerName}");
            
            await RunProcessAsync($"docker rm -f {containerName}");
        }

        private async Task ToggleApache()
        {
            var config = configManager.GetConfiguration();
            if (!apacheIsRunning)
            {
                var absoluteProjectsPath = Path.GetFullPath(projectManager.ProjectsPath);
                
                await StopContainer("nodastack_apache"); // Nettoyage préventif

                bool success = await RunProcessAsync($"docker run -d --restart=unless-stopped -p {config.ApachePort}:80 -v \"{absoluteProjectsPath}:/var/www/html\" --name nodastack_apache nodastack_apache");
                
                if (success)
                {
                    await Task.Delay(1000);
                    if (await IsContainerRunning("nodastack_apache"))
                {
                    apacheIsRunning = true;
            }
            else
            {
                        apacheIsRunning = false;
                        string logs = await GetProcessOutputAsync("docker logs --tail 5 nodastack_apache");
                        MessageBox.Show($"Apache failed to start. Logs:\n{logs}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to execute start command for Apache.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                await StopContainer("nodastack_apache");
                    apacheIsRunning = false;
            }
            UpdateDashboardState();
        }

        private async Task TogglePhp()
            {
                var config = configManager.GetConfiguration();
            if (!phpIsRunning)
            {
                var absoluteProjectsPath = Path.GetFullPath(projectManager.ProjectsPath);
                
                await StopContainer("nodastack_php");

                bool success = await RunProcessAsync($"docker run -d --restart=unless-stopped -p {config.PhpPort}:8000 -v \"{absoluteProjectsPath}:/var/www/html\" --name nodastack_php nodastack_php");
                
                if (success)
                {
                    await Task.Delay(1000);
                    if (await IsContainerRunning("nodastack_php"))
                {
                    phpIsRunning = true;
            }
            else
                {
                    phpIsRunning = false;
                        string logs = await GetProcessOutputAsync("docker logs --tail 5 nodastack_php");
                        MessageBox.Show($"PHP failed to start. Logs:\n{logs}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to execute start command for PHP.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                await StopContainer("nodastack_php");
                    phpIsRunning = false;
            }
            UpdateDashboardState();
        }

        private async Task ToggleMysql()
        {
            var config = configManager.GetConfiguration();
                if (!mysqlIsRunning)
                {
                await StopContainer("nodastack_mysql");

                bool success = await RunProcessAsync($"docker run -d --restart=unless-stopped -p {config.MySqlPort}:3306 -e MYSQL_ROOT_PASSWORD=root --name nodastack_mysql nodastack_mysql");
                
                if (success)
                {
                    await Task.Delay(2000); 
                    if (await IsContainerRunning("nodastack_mysql"))
                {
                    mysqlIsRunning = true;
            }
            else
            {
                        mysqlIsRunning = false;
                        string logs = await GetProcessOutputAsync("docker logs --tail 5 nodastack_mysql");
                        MessageBox.Show($"MySQL failed to start. Logs:\n{logs}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to execute start command for MySQL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                await StopContainer("nodastack_mysql");
                mysqlIsRunning = false;
            }
            UpdateDashboardState();
        }

        private async Task TogglePma()
        {
            var config = configManager.GetConfiguration();
            if (!phpmyadminIsRunning)
            {
                await StopContainer("nodastack_phpmyadmin");

                bool success = await RunProcessAsync($"docker run -d --restart=unless-stopped -p {config.PhpMyAdminPort}:80 --name nodastack_phpmyadmin --link nodastack_mysql -e PMA_HOST=nodastack_mysql nodastack_phpmyadmin");
                
                if (success)
                {
                    await Task.Delay(1000);
                    if (await IsContainerRunning("nodastack_phpmyadmin"))
                    {
                        phpmyadminIsRunning = true;
                    }
                    else
                    {
                        phpmyadminIsRunning = false;
                        string logs = await GetProcessOutputAsync("docker logs --tail 5 nodastack_phpmyadmin");
                        MessageBox.Show($"phpMyAdmin failed to start. Logs:\n{logs}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to execute start command for phpMyAdmin.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                await StopContainer("nodastack_phpmyadmin");
                phpmyadminIsRunning = false;
            }
            UpdateDashboardState();
        }

        private async Task ToggleMailHog()
        {
            var config = configManager.GetConfiguration();
            if (!mailhogIsRunning)
            {
                await StopContainer("nodastack_mailhog");

                // MailHog uses ports 1025 (SMTP) and 8025 (Web UI)
                bool success = await RunProcessAsync($"docker run -d --restart=unless-stopped -p {config.MailHogSmtpPort}:1025 -p {config.MailHogWebPort}:8025 --name nodastack_mailhog mailhog/mailhog");
                
                if (success)
                {
                    await Task.Delay(1000);
                    if (await IsContainerRunning("nodastack_mailhog"))
                    {
                        mailhogIsRunning = true;
                    }
                    else
                    {
                        mailhogIsRunning = false;
                        string logs = await GetProcessOutputAsync("docker logs --tail 5 nodastack_mailhog");
                        MessageBox.Show($"MailHog failed to start. Logs:\n{logs}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to execute start command for MailHog. Ensure you have internet connection to pull 'mailhog/mailhog' image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                await StopContainer("nodastack_mailhog");
                mailhogIsRunning = false;
            }
            UpdateDashboardState();
        }

        private async Task<bool> RunProcessAsync(string command)
        {
            return await Task.Run(async () =>
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

                    using (var p = new Process())
                    {
                        p.StartInfo = psi;
                        p.Start();
                        
                        if (p.StandardOutput == null || p.StandardError == null)
                        {
                             p.WaitForExit();
                             return p.ExitCode == 0;
                        }

                        var stdoutTask = p.StandardOutput.ReadToEndAsync();
                        var stderrTask = p.StandardError.ReadToEndAsync();

                        await Task.WhenAll(stdoutTask, stderrTask);
                        p.WaitForExit();

                        if (p.ExitCode != 0)
                        {
                            string error = await stderrTask;
                            Debug.WriteLine($"Command failed: {command}. Error: {error}");
                            return false;
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return false;
                }
            });
        }

        public async Task BuildDockerImages(IProgress<string> progress = null)
        {
            // Determine root path (where Docker folder is)
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

            try
            {
                progress?.Report("Building Apache image...");
                if (!await RunProcessAsync($"docker build -t nodastack_apache \"{Path.Combine(projectRoot, "Docker", "apache")}\""))
                    throw new Exception("Failed to build Apache image");

                progress?.Report("Building PHP image...");
                if (!await RunProcessAsync($"docker build -t nodastack_php \"{Path.Combine(projectRoot, "Docker", "php")}\""))
                    throw new Exception("Failed to build PHP image");

                progress?.Report("Building MySQL image...");
                if (!await RunProcessAsync($"docker build -t nodastack_mysql \"{Path.Combine(projectRoot, "Docker", "mysql")}\""))
                    throw new Exception("Failed to build MySQL image");

                progress?.Report("Building phpMyAdmin image...");
                if (!await RunProcessAsync($"docker build -t nodastack_phpmyadmin \"{Path.Combine(projectRoot, "Docker", "phpmyadmin")}\""))
                    throw new Exception("Failed to build phpMyAdmin image");

                if (Directory.Exists(Path.Combine(projectRoot, "Docker", "mailhog")))
                {
                    progress?.Report("Building MailHog image...");
                    if (!await RunProcessAsync($"docker build -t nodastack_mailhog \"{Path.Combine(projectRoot, "Docker", "mailhog")}\""))
                        throw new Exception("Failed to build MailHog image");
                }
                    
                progress?.Report("All images built successfully!");
            }
            catch (Exception ex)
            {
                progress?.Report($"Error: {ex.Message}");
                throw; // Re-throw to be caught by caller if needed
            }
        }

        private async void CheckInitialContainerStatus()
            {
                try
                {
                // Check Apache
                if (await IsContainerRunning("nodastack_apache")) apacheIsRunning = true;
                
                // Check PHP
                if (await IsContainerRunning("nodastack_php")) phpIsRunning = true;
                
                // Check MySQL
                if (await IsContainerRunning("nodastack_mysql")) mysqlIsRunning = true;
                
                // Check PMA
                if (await IsContainerRunning("nodastack_phpmyadmin")) phpmyadminIsRunning = true;
                
                // Check MailHog
                if (await IsContainerRunning("nodastack_mailhog")) mailhogIsRunning = true;
                }
                catch (Exception ex)
                {
                Debug.WriteLine("Error checking container status: " + ex.Message);
            }
            
            UpdateDashboardState();
        }

        private async Task<bool> IsContainerRunning(string containerName)
        {
            // Use 'docker ps' filtering by name to see if it's running
            // output will be the container name if running, empty otherwise
            string output = await GetProcessOutputAsync($"docker ps -q -f name={containerName}");
            return !string.IsNullOrWhiteSpace(output);
        }

        private async Task<string> GetProcessOutputAsync(string command)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C {command}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true, // Redirect error too to avoid popping up
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using (var p = new Process())
                    {
                        p.StartInfo = psi;
                        p.Start();

                        var stdoutTask = p.StandardOutput.ReadToEndAsync();
                        var stderrTask = p.StandardError.ReadToEndAsync();

                        await Task.WhenAll(stdoutTask, stderrTask);
                        p.WaitForExit();

                        return (await stdoutTask).Trim();
                    }
                }
                catch { return ""; }
            });
        }

        public void CheckForUpdatesManually(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Update check stub.", "Info");
        }

        public async Task DownloadAndInstallUpdate()
        {
            await Task.Delay(100);
            MessageBox.Show("Update install stub.", "Info");
        }

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

        public void ReloadConfiguration()
        {
            configManager.LoadConfiguration();
            var config = configManager.GetConfiguration();
            
            // Update Project Manager path
            projectManager.UpdateProjectsPath(config.Settings.ProjectsPath);
            
            // Refresh Dashboard Project List
            _dashboardPage.RefreshProjects_Click(null, null);
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
