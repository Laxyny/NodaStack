using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;

namespace NodaStack
{
    public partial class MainWindow : Window
    {
        private bool apacheIsRunning = false;
        private bool phpIsRunning = false;
        private bool mysqlIsRunning = false;
        private bool phpmyadminIsRunning = false;
        private ProjectManager projectManager;
        private ConfigurationManager configManager;
        private LogManager logManager;
        private KeyboardShortcutManager shortcutManager;
        private StatusBarManager statusBarManager;

        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.Initialize(false);

            configManager = new ConfigurationManager();
            projectManager = new ProjectManager();
            logManager = new LogManager();
            shortcutManager = new KeyboardShortcutManager(this);

            if (MainStatusBar != null)
            {
                statusBarManager = new StatusBarManager(MainStatusBar);
            }
            else
            {
                // Log une erreur si MainStatusBar n'existe pas
                System.Diagnostics.Debug.WriteLine("MainStatusBar not found in XAML!");
            }

            logManager.OnLogAdded += (entry) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogBox.AppendText($"[{entry.Timestamp:HH:mm:ss}] {entry.Message}{Environment.NewLine}");
                    LogBox.ScrollToEnd();
                });
            };

            SetupKeyboardShortcuts();
            ApplyConfiguration();
            CheckInitialContainerStatus();
            RefreshProjectsList();

            ProjectsListView.SelectionChanged += ProjectsListView_SelectionChanged;
        }

        private void SetupKeyboardShortcuts()
        {
            shortcutManager.RegisterShortcut(Key.M, ModifierKeys.Control, () => Monitoring_Click(this, new RoutedEventArgs()));
            shortcutManager.RegisterShortcut(Key.OemComma, ModifierKeys.Control, () => Configuration_Click(this, new RoutedEventArgs()));
            shortcutManager.RegisterShortcut(Key.N, ModifierKeys.Control, () => CreateProject_Click(this, new RoutedEventArgs()));
            shortcutManager.RegisterShortcut(Key.R, ModifierKeys.Control, () => RefreshProjects_Click(this, new RoutedEventArgs()));
            shortcutManager.RegisterShortcut(Key.F5, () => RefreshProjects_Click(this, new RoutedEventArgs()));
            shortcutManager.RegisterShortcut(Key.Q, ModifierKeys.Control, () => Close());
            shortcutManager.RegisterShortcut(Key.F1, () => ShowHelp());

            shortcutManager.RegisterShortcut(Key.D1, ModifierKeys.Control, () => StartApache_Click(this, new RoutedEventArgs()));
            shortcutManager.RegisterShortcut(Key.D2, ModifierKeys.Control, () => StartPHP_Click(this, new RoutedEventArgs()));
            shortcutManager.RegisterShortcut(Key.D3, ModifierKeys.Control, () => StartMySQL_Click(this, new RoutedEventArgs()));
            shortcutManager.RegisterShortcut(Key.D4, ModifierKeys.Control, () => StartPhpMyAdmin_Click(this, new RoutedEventArgs()));
        }

        private void ShowHelp()
        {
            var helpMessage = "Keyboard Shortcuts:\n\n" +
                             "Ctrl+M: Open Monitoring\n" +
                             "Ctrl+,: Open Configuration\n" +
                             "Ctrl+N: Create New Project\n" +
                             "Ctrl+R / F5: Refresh Projects\n" +
                             "Ctrl+Q: Quit Application\n" +
                             "F1: Show Help";
            MessageBox.Show(helpMessage, "Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Log(string message, LogLevel level = LogLevel.Info, string service = "System")
        {
            logManager.Log(message, level, service);
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.IsDarkTheme = !ThemeManager.IsDarkTheme;

            var themeButton = sender as Button;
            if (themeButton != null)
            {
                var textBlock = themeButton.Content as TextBlock;
                if (textBlock != null)
                {
                    textBlock.Text = ThemeManager.IsDarkTheme ? "☀️" : "🌙";
                }
            }

            NotificationManager.ShowNotification(
                "Theme Changed",
                $"Switched to {(ThemeManager.IsDarkTheme ? "Dark" : "Light")} theme",
                NotificationType.Info
            );
        }

        private void Configuration_Click(object sender, RoutedEventArgs e)
        {
            OpenConfiguration();
        }

        private void Monitoring_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var monitoringWindow = new MonitoringWindow(logManager);
                monitoringWindow.Owner = this;
                monitoringWindow.Show();
                Log("Monitoring window opened", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log($"Error opening monitoring window: {ex.Message}", LogLevel.Error);
                NotificationManager.ShowNotification("Error", $"Failed to open monitoring: {ex.Message}", NotificationType.Error);
            }
        }

        private void OpenConfiguration()
        {
            try
            {
                var configWindow = new ConfigurationWindow(configManager);
                configWindow.Owner = this;
                var result = configWindow.ShowDialog();

                if (result == true)
                {
                    ApplyConfiguration();
                    Log("Configuration updated", LogLevel.Info);
                    NotificationManager.ShowNotification("Configuration", "Settings updated successfully", NotificationType.Success);
                }
            }
            catch (Exception ex)
            {
                Log($"Error opening configuration: {ex.Message}", LogLevel.Error);
                NotificationManager.ShowNotification("Error", $"Failed to open configuration: {ex.Message}", NotificationType.Error);
            }
        }

        private void ApplyConfiguration()
        {
            try
            {
                var config = configManager.GetConfiguration();

                ApachePortInfo.Text = $"Port: {config.ApachePort}";
                PhpPortInfo.Text = $"Port: {config.PhpPort}";
                MySqlPortInfo.Text = $"Port: {config.MySqlPort}";
                PhpMyAdminPortInfo.Text = $"Port: {config.PhpMyAdminPort}";

                Log("Configuration applied", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log($"Error applying configuration: {ex.Message}", LogLevel.Error);
            }
        }

        private async void StartApache_Click(object sender, RoutedEventArgs e)
        {
            var apachePort = configManager.GetConfiguration().ApachePort;
            var currentDir = Directory.GetCurrentDirectory();
            var wwwPath = Path.Combine(currentDir, "www");

            if (!apacheIsRunning)
            {
                statusBarManager.UpdateStatus("Starting Apache...");
                Log("Building nodastack_apache...", LogLevel.Info, "Apache");
                await RunProcessAsync("docker build -t nodastack_apache ./Docker/apache");

                Log("Launching nodastack_apache container...", LogLevel.Info, "Apache");
                RunProcess($"docker run -d --rm -p {apachePort}:80 -v \"{wwwPath}:/var/www/html\" --name nodastack_apache nodastack_apache", () =>
                {
                    apacheIsRunning = true;
                    UpdateServiceIndicators();
                    UpdateServiceButtons();
                    UpdateServicesCount();
                    Log($"Apache container started on port {apachePort}.", LogLevel.Info, "Apache");
                    NotificationManager.ShowNotification("Apache", "Apache started successfully", NotificationType.Success);
                    statusBarManager.UpdateStatus("Ready");
                });
            }
            else
            {
                statusBarManager.UpdateStatus("Stopping Apache...");
                Log("Stopping nodastack_apache container...", LogLevel.Info, "Apache");
                RunProcess("docker stop nodastack_apache", () =>
                {
                    apacheIsRunning = false;
                    UpdateServiceIndicators();
                    UpdateServiceButtons();
                    UpdateServicesCount();
                    Log("Apache container stopped.", LogLevel.Info, "Apache");
                    NotificationManager.ShowNotification("Apache", "Apache stopped successfully", NotificationType.Success);
                    statusBarManager.UpdateStatus("Ready");
                });
            }
        }

        private async void StartPHP_Click(object sender, RoutedEventArgs e)
        {
            var phpPort = configManager.GetConfiguration().PhpPort;
            var currentDir = Directory.GetCurrentDirectory();
            var wwwPath = Path.Combine(currentDir, "www");

            if (!phpIsRunning)
            {
                statusBarManager.UpdateStatus("Starting PHP...");
                Log("Building nodastack_php...", LogLevel.Info, "PHP");
                await RunProcessAsync("docker build -t nodastack_php ./Docker/php");

                Log("Launching nodastack_php container...", LogLevel.Info, "PHP");
                RunProcess($"docker run -d --rm -p {phpPort}:8000 -v \"{wwwPath}:/var/www/html\" --name nodastack_php nodastack_php", () =>
                {
                    phpIsRunning = true;
                    UpdateServiceIndicators();
                    UpdateServiceButtons();
                    UpdateServicesCount();
                    Log($"PHP container started on port {phpPort}.", LogLevel.Info, "PHP");
                    NotificationManager.ShowNotification("PHP", "PHP started successfully", NotificationType.Success);
                    statusBarManager.UpdateStatus("Ready");
                });
            }
            else
            {
                statusBarManager.UpdateStatus("Stopping PHP...");
                Log("Stopping nodastack_php container...", LogLevel.Info, "PHP");
                RunProcess("docker stop nodastack_php", () =>
                {
                    phpIsRunning = false;
                    UpdateServiceIndicators();
                    UpdateServiceButtons();
                    UpdateServicesCount();
                    Log("PHP container stopped.", LogLevel.Info, "PHP");
                    NotificationManager.ShowNotification("PHP", "PHP stopped successfully", NotificationType.Success);
                    statusBarManager.UpdateStatus("Ready");
                });
            }
        }

        private void UpdateServicesCount()
        {
            int runningServices = (apacheIsRunning ? 1 : 0) + (phpIsRunning ? 1 : 0) +
                                 (mysqlIsRunning ? 1 : 0) + (phpmyadminIsRunning ? 1 : 0);

            // DEBUG : Afficher les valeurs
            Log($"Service Count Debug: Apache={apacheIsRunning}, PHP={phpIsRunning}, MySQL={mysqlIsRunning}, phpMyAdmin={phpmyadminIsRunning}, Total={runningServices}", LogLevel.Info, "Debug");

            statusBarManager.UpdateServicesCount(runningServices, 4);
        }

        private async void StartMySQL_Click(object sender, RoutedEventArgs e)
        {
            var mysqlPort = configManager.GetConfiguration().MySqlPort;

            if (!mysqlIsRunning)
            {
                statusBarManager.UpdateStatus("Starting MySQL...");
                Log("Building nodastack_mysql...", LogLevel.Info, "MySQL");
                await RunProcessAsync("docker build -t nodastack_mysql ./Docker/mysql");

                Log("Launching nodastack_mysql container...", LogLevel.Info, "MySQL");
                RunProcess($"docker run -d --rm -p {mysqlPort}:3306 --name nodastack_mysql nodastack_mysql", () =>
                {
                    mysqlIsRunning = true;
                    UpdateServiceIndicators();
                    UpdateServiceButtons();
                    UpdateServicesCount();
                    Log($"MySQL container started on port {mysqlPort}.", LogLevel.Info, "MySQL");
                    NotificationManager.ShowNotification("MySQL", "MySQL started successfully", NotificationType.Success);
                    statusBarManager.UpdateStatus("Ready");
                });
            }
            else
            {
                statusBarManager.UpdateStatus("Stopping MySQL...");
                Log("Stopping nodastack_phpmyadmin...", LogLevel.Info, "MySQL");
                RunProcess("docker stop nodastack_phpmyadmin", () =>
                {
                    phpmyadminIsRunning = false;
                    UpdateServiceIndicators();
                    UpdateServiceButtons();
                    UpdateServicesCount();
                    Log("phpMyAdmin container stopped.", LogLevel.Info, "MySQL");

                    Log("Stopping nodastack_mysql...", LogLevel.Info, "MySQL");
                    RunProcess("docker stop nodastack_mysql", () =>
                    {
                        mysqlIsRunning = false;
                        UpdateServiceIndicators();
                        UpdateServiceButtons();
                        Log("MySQL container stopped.", LogLevel.Info, "MySQL");
                        NotificationManager.ShowNotification("MySQL", "MySQL stopped successfully", NotificationType.Success);
                        statusBarManager.UpdateStatus("Ready");
                    });
                });
            }
        }

        private async void StartPhpMyAdmin_Click(object sender, RoutedEventArgs e)
        {
            var phpmyadminPort = configManager.GetConfiguration().PhpMyAdminPort;

            if (!phpmyadminIsRunning)
            {
                if (!mysqlIsRunning)
                {
                    statusBarManager.UpdateStatus("Starting phpMyAdmin...");
                    Log("MySQL must be running before starting phpMyAdmin.", LogLevel.Warning, "phpMyAdmin");
                    NotificationManager.ShowNotification("Warning", "MySQL must be running before starting phpMyAdmin", NotificationType.Warning);
                    return;
                }

                Log("Building nodastack_phpmyadmin...", LogLevel.Info, "phpMyAdmin");
                await RunProcessAsync("docker build -t nodastack_phpmyadmin ./Docker/phpmyadmin");

                Log("Launching nodastack_phpmyadmin container...", LogLevel.Info, "phpMyAdmin");
                RunProcess($"docker run -d --rm -p {phpmyadminPort}:80 --name nodastack_phpmyadmin --link nodastack_mysql " +
                        "-e PMA_HOST=nodastack_mysql -e PMA_USER=root -e PMA_PASSWORD= -e MYSQL_ALLOW_EMPTY_PASSWORD=yes " +
                        "nodastack_phpmyadmin", () =>
                {
                    phpmyadminIsRunning = true;
                    UpdateServiceIndicators();
                    UpdateServiceButtons();
                    UpdateServicesCount();
                    Log($"phpMyAdmin container started on port {phpmyadminPort}.", LogLevel.Info, "phpMyAdmin");
                    NotificationManager.ShowNotification("phpMyAdmin", "phpMyAdmin started successfully", NotificationType.Success);
                    statusBarManager.UpdateStatus("Ready");
                });
            }
            else
            {
                statusBarManager.UpdateStatus("Stopping phpMyAdmin...");
                Log("Stopping nodastack_phpmyadmin...", LogLevel.Info, "phpMyAdmin");
                RunProcess("docker stop nodastack_phpmyadmin", () =>
                {
                    phpmyadminIsRunning = false;
                    UpdateServiceIndicators();
                    UpdateServiceButtons();
                    UpdateServicesCount();
                    Log("phpMyAdmin container stopped.", LogLevel.Info, "phpMyAdmin");
                    NotificationManager.ShowNotification("phpMyAdmin", "phpMyAdmin stopped successfully", NotificationType.Success);
                    statusBarManager.UpdateStatus("Ready");
                });
            }
        }

        private void UpdateServiceIndicators()
        {
            Dispatcher.Invoke(() =>
            {
                ApacheIndicator.Fill = apacheIsRunning ? Brushes.LimeGreen : Brushes.Red;
                PhpIndicator.Fill = phpIsRunning ? Brushes.LimeGreen : Brushes.Red;
                MySqlIndicator.Fill = mysqlIsRunning ? Brushes.LimeGreen : Brushes.Red;
                PhpMyAdminIndicator.Fill = phpmyadminIsRunning ? Brushes.LimeGreen : Brushes.Red;
            });
        }

        private void UpdateServiceButtons()
        {
            Dispatcher.Invoke(() =>
            {
                StartApacheButton.Content = CreateButtonContent(apacheIsRunning ? "⏹️" : "▶️",
                                                               apacheIsRunning ? "Stop Apache" : "Start Apache");
                StartPHPButton.Content = CreateButtonContent(phpIsRunning ? "⏹️" : "▶️",
                                                            phpIsRunning ? "Stop PHP" : "Start PHP");
                StartMySQLButton.Content = CreateButtonContent(mysqlIsRunning ? "⏹️" : "▶️",
                                                              mysqlIsRunning ? "Stop MySQL" : "Start MySQL");
                StartPhpMyAdminButton.Content = CreateButtonContent(phpmyadminIsRunning ? "⏹️" : "▶️",
                                                                   phpmyadminIsRunning ? "Stop phpMyAdmin" : "Start phpMyAdmin");

                OpenApacheButton.IsEnabled = apacheIsRunning;
                OpenPhpButton.IsEnabled = phpIsRunning;
                OpenMySqlButton.IsEnabled = mysqlIsRunning;
                OpenPhpMyAdminButton.IsEnabled = phpmyadminIsRunning;
            });
        }

        private StackPanel CreateButtonContent(string icon, string text)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var iconBlock = new TextBlock
            {
                Text = icon,
                FontSize = 12,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var textBlock = new TextBlock
            {
                Text = text
            };

            stackPanel.Children.Add(iconBlock);
            stackPanel.Children.Add(textBlock);

            return stackPanel;
        }

        private void OpenApache_Click(object sender, RoutedEventArgs e)
        {
            if (apacheIsRunning)
            {
                try
                {
                    var apachePort = configManager.GetConfiguration().ApachePort;
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"http://localhost:{apachePort}",
                        UseShellExecute = true
                    });
                    Log("Opening Apache in browser...", LogLevel.Info, "Apache");
                }
                catch (Exception ex)
                {
                    Log($"Error opening Apache: {ex.Message}", LogLevel.Error, "Apache");
                }
            }
        }

        private void OpenPhp_Click(object sender, RoutedEventArgs e)
        {
            if (phpIsRunning)
            {
                try
                {
                    var phpPort = configManager.GetConfiguration().PhpPort;
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"http://localhost:{phpPort}",
                        UseShellExecute = true
                    });
                    Log("Opening PHP in browser...", LogLevel.Info, "PHP");
                }
                catch (Exception ex)
                {
                    Log($"Error opening PHP: {ex.Message}", LogLevel.Error, "PHP");
                }
            }
        }

        private void OpenMySql_Click(object sender, RoutedEventArgs e)
        {
            if (mysqlIsRunning)
            {
                try
                {
                    var mysqlPort = configManager.GetConfiguration().MySqlPort;
                    string connectionString = $"Server=localhost;Port={mysqlPort};Database=nodastack;Uid=root;Pwd=;";
                    System.Windows.Clipboard.SetText(connectionString);
                    Log("MySQL connection string copied to clipboard!", LogLevel.Info, "MySQL");
                    NotificationManager.ShowNotification("MySQL", "Connection string copied to clipboard", NotificationType.Info);
                }
                catch (Exception ex)
                {
                    Log($"Error copying connection string: {ex.Message}", LogLevel.Error, "MySQL");
                }
            }
        }

        private void OpenPhpMyAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (phpmyadminIsRunning)
            {
                try
                {
                    var phpmyadminPort = configManager.GetConfiguration().PhpMyAdminPort;
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"http://localhost:{phpmyadminPort}",
                        UseShellExecute = true
                    });
                    Log("Opening phpMyAdmin in browser...", LogLevel.Info, "phpMyAdmin");
                }
                catch (Exception ex)
                {
                    Log($"Error opening phpMyAdmin: {ex.Message}", LogLevel.Error, "phpMyAdmin");
                }
            }
        }

        private void RunProcess(string command, Action? onSuccess = null)
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

                var process = new Process { StartInfo = psi };
                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null) Log(e.Data);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null && !string.IsNullOrWhiteSpace(e.Data))
                    {
                        if (!e.Data.Contains("No such container") || !command.Contains("docker stop"))
                            Log("ERROR: " + e.Data, LogLevel.Error);
                    }
                };
                process.Exited += (s, e) =>
                {
                    if (process.ExitCode == 0)
                        onSuccess?.Invoke();
                };

                process.EnableRaisingEvents = true;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex.Message, LogLevel.Error);
            }
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

                    var process = new Process { StartInfo = psi };
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) Log(e.Data); };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null && !string.IsNullOrWhiteSpace(e.Data))
                        {
                            if (!e.Data.StartsWith("#") && !e.Data.Contains("DONE") && !e.Data.Contains("exporting"))
                                Log("ERROR: " + e.Data, LogLevel.Error);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    Log("Exception: " + ex.Message, LogLevel.Error);
                }
            });
        }

        private async void CheckInitialContainerStatus()
        {
            statusBarManager.UpdateStatus("Checking container status...");

            await Task.Run(() =>
            {
                try
                {
                    // Apache
                    bool apacheRunning = CheckContainerStatusSync("nodastack_apache");
                    if (apacheRunning)
                    {
                        apacheIsRunning = true;
                        Dispatcher.Invoke(() =>
                        {
                            UpdateServiceIndicators();
                            UpdateServiceButtons();
                        });
                        Log("Apache container already running.", LogLevel.Info, "Apache");
                    }

                    // PHP
                    bool phpRunning = CheckContainerStatusSync("nodastack_php");
                    if (phpRunning)
                    {
                        phpIsRunning = true;
                        Dispatcher.Invoke(() =>
                        {
                            UpdateServiceIndicators();
                            UpdateServiceButtons();
                        });
                        Log("PHP container already running.", LogLevel.Info, "PHP");
                    }

                    // MySQL
                    bool mysqlRunning = CheckContainerStatusSync("nodastack_mysql");
                    if (mysqlRunning)
                    {
                        mysqlIsRunning = true;
                        Dispatcher.Invoke(() =>
                        {
                            UpdateServiceIndicators();
                            UpdateServiceButtons();
                        });
                        Log("MySQL container already running.", LogLevel.Info, "MySQL");
                    }

                    // phpMyAdmin
                    bool phpmyadminRunning = CheckContainerStatusSync("nodastack_phpmyadmin");
                    if (phpmyadminRunning)
                    {
                        phpmyadminIsRunning = true;
                        Dispatcher.Invoke(() =>
                        {
                            UpdateServiceIndicators();
                            UpdateServiceButtons();
                        });
                        Log("phpMyAdmin container already running.", LogLevel.Info, "phpMyAdmin");
                    }

                    Dispatcher.Invoke(() =>
                    {
                        UpdateServicesCount();
                        statusBarManager.UpdateStatus("Ready");
                    });
                }
                catch (Exception ex)
                {
                    Log("Exception during status check: " + ex.Message, LogLevel.Error);
                    Dispatcher.Invoke(() =>
                    {
                        statusBarManager.UpdateStatus("Error checking status");
                    });
                }
            });
        }

        private bool CheckContainerStatusSync(string containerName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C docker ps --filter \"name=^{containerName}$\" --format \"{{{{.Names}}}}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = psi };
                process.Start();

                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                bool isRunning = !string.IsNullOrEmpty(output) && output.Equals(containerName, StringComparison.OrdinalIgnoreCase);
                return isRunning;
            }
            catch (Exception ex)
            {
                Log($"Exception during {containerName} status check: " + ex.Message, LogLevel.Error);
                return false;
            }
        }

        private void RefreshProjectsList()
        {
            try
            {
                var projects = projectManager.GetProjects();
                ProjectsListView.ItemsSource = projects;
                Log($"Found {projects.Count} project(s) in www/ directory", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log($"Error loading projects: {ex.Message}", LogLevel.Error);
            }
        }

        private void ProjectsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var hasSelection = ProjectsListView.SelectedItem != null;
            OpenProjectButton.IsEnabled = hasSelection;
            ViewApacheButton.IsEnabled = hasSelection && apacheIsRunning;
            ViewPhpButton.IsEnabled = hasSelection && phpIsRunning;
            DeleteProjectButton.IsEnabled = hasSelection;
        }

        private void NewProjectTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (NewProjectTextBox.Text == "Project name..." && NewProjectTextBox.Foreground == Brushes.Gray)
            {
                NewProjectTextBox.Text = "";
                NewProjectTextBox.Foreground = Brushes.Black;
            }
        }

        private void NewProjectTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewProjectTextBox.Text))
            {
                NewProjectTextBox.Text = "Project name...";
                NewProjectTextBox.Foreground = Brushes.Gray;
            }
        }

        private void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var projectName = NewProjectTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(projectName) || projectName == "Project name...")
                {
                    NotificationManager.ShowNotification("Error", "Please enter a project name", NotificationType.Warning);
                    return;
                }

                if (projectManager.CreateProject(projectName))
                {
                    Log($"Project '{projectName}' created successfully", LogLevel.Info);
                    NewProjectTextBox.Text = "Project name...";
                    NewProjectTextBox.Foreground = Brushes.Gray;
                    RefreshProjectsList();
                    NotificationManager.ShowNotification("Success", $"Project '{projectName}' created successfully", NotificationType.Success);
                }
                else
                {
                    Log($"Failed to create project '{projectName}' (already exists or invalid name)", LogLevel.Error);
                    NotificationManager.ShowNotification("Error", "Failed to create project. It may already exist or the name is invalid.", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                Log($"Error creating project: {ex.Message}", LogLevel.Error);
                NotificationManager.ShowNotification("Error", $"Error creating project: {ex.Message}", NotificationType.Error);
            }
        }

        private void RefreshProjects_Click(object sender, RoutedEventArgs e)
        {
            RefreshProjectsList();
        }

        private void OpenProjectsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var projectsPath = projectManager.GetProjectsPath();
                Process.Start(new ProcessStartInfo(projectsPath) { UseShellExecute = true });
                Log($"Opened projects folder: {projectsPath}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log($"Error opening projects folder: {ex.Message}", LogLevel.Error);
                NotificationManager.ShowNotification("Error", $"Failed to open projects folder: {ex.Message}", NotificationType.Error);
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListView.SelectedItem is ProjectInfo project)
            {
                try
                {
                    var projectPath = Path.Combine(projectManager.GetProjectsPath(), project.Name);
                    Process.Start(new ProcessStartInfo(projectPath) { UseShellExecute = true });
                    Log($"Opened project: {project.Name}", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Log($"Error opening project: {ex.Message}", LogLevel.Error);
                    NotificationManager.ShowNotification("Error", $"Failed to open project: {ex.Message}", NotificationType.Error);
                }
            }
        }

        private void ViewApache_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListView.SelectedItem is ProjectInfo project)
            {
                if (apacheIsRunning)
                {
                    try
                    {
                        var url = $"http://localhost:{configManager.GetConfiguration().ApachePort}/{project.Name}";
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        Log($"Opening project '{project.Name}' via Apache", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Log($"Error opening project via Apache: {ex.Message}", LogLevel.Error);
                        NotificationManager.ShowNotification("Error", $"Failed to open project: {ex.Message}", NotificationType.Error);
                    }
                }
                else
                {
                    NotificationManager.ShowNotification("Warning", "Apache must be running to view projects", NotificationType.Warning);
                }
            }
        }

        private void ViewPhp_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListView.SelectedItem is ProjectInfo project)
            {
                if (phpIsRunning)
                {
                    try
                    {
                        var url = $"http://localhost:{configManager.GetConfiguration().PhpPort}/{project.Name}";
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        Log($"Opening project '{project.Name}' via PHP server", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Log($"Error opening project via PHP: {ex.Message}", LogLevel.Error);
                        NotificationManager.ShowNotification("Error", $"Failed to open project: {ex.Message}", NotificationType.Error);
                    }
                }
                else
                {
                    NotificationManager.ShowNotification("Warning", "PHP server must be running to view projects", NotificationType.Warning);
                }
            }
        }

        private void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListView.SelectedItem is ProjectInfo project)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the project '{project.Name}'?\n\nThis action cannot be undone.",
                                           "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (projectManager.DeleteProject(project.Name))
                        {
                            Log($"Project '{project.Name}' deleted successfully", LogLevel.Info);
                            RefreshProjectsList();
                            NotificationManager.ShowNotification("Success", $"Project '{project.Name}' deleted", NotificationType.Success);
                        }
                        else
                        {
                            Log($"Failed to delete project '{project.Name}'", LogLevel.Error);
                            NotificationManager.ShowNotification("Error", "Failed to delete project", NotificationType.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Error deleting project: {ex.Message}", LogLevel.Error);
                        NotificationManager.ShowNotification("Error", $"Error deleting project: {ex.Message}", NotificationType.Error);
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            shortcutManager?.ClearShortcuts();
            statusBarManager?.Dispose();
            base.OnClosed(e);
        }
    }
}