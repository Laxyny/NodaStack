using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace NodaStack
{
    public partial class MainWindow : Window
    {

        private bool apacheIsRunning = false;
        private bool phpIsRunning = false;
        private bool mysqlIsRunning = false;
        private bool phpmyadminIsRunning = false;
        private ProjectManager projectManager;

        public MainWindow()
        {
            InitializeComponent();
            projectManager = new ProjectManager();
            CheckInitialContainerStatus();
            RefreshProjectsList();
            ProjectsListView.SelectionChanged += ProjectsListView_SelectionChanged;
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                LogBox.ScrollToEnd();
            });
        }

        private async void StartApache_Click(object sender, RoutedEventArgs e)
        {
            if (!apacheIsRunning)
            {
                Log("Building nodastack_apache...");
                await RunProcessAsync("docker build -t nodastack_apache ./Docker/apache");

                Log("Launching nodastack_apache container...");
                RunProcess("docker run -d --rm -p 8080:80 --name nodastack_apache nodastack_apache", () =>
                {
                    apacheIsRunning = true;
                    Dispatcher.Invoke(() => StartApacheButton.Content = "Stop Apache");
                    Log("Apache container started.");
                    UpdateIndicator("apache", apacheIsRunning);
                });
            }
            else
            {
                Log("Stopping nodastack_apache container...");
                RunProcess("docker stop nodastack_apache", () =>
                {
                    apacheIsRunning = false;
                    Dispatcher.Invoke(() => StartApacheButton.Content = "Start Apache");
                    Log("Apache container stopped.");
                    UpdateIndicator("apache", apacheIsRunning);
                });
            }
        }

        private async void StartPHP_Click(object sender, RoutedEventArgs e)
        {
            if (!phpIsRunning)
            {
                Log("Building nodastack_php...");
                await RunProcessAsync("docker build -t nodastack_php ./Docker/php");

                Log("Launching nodastack_php container...");
                RunProcess("docker run -d --rm -p 8000:8000 --name nodastack_php nodastack_php", () =>
                {
                    phpIsRunning = true;
                    Dispatcher.Invoke(() => StartPHPButton.Content = "Stop PHP");
                    Log("PHP container started.");
                    UpdateIndicator("php", phpIsRunning);
                });
            }
            else
            {
                Log("Stopping nodastack_php container...");
                RunProcess("docker stop nodastack_php", () =>
                {
                    phpIsRunning = false;
                    Dispatcher.Invoke(() => StartPHPButton.Content = "Start PHP");
                    Log("PHP container stopped.");
                    UpdateIndicator("php", phpIsRunning);
                });
            }
        }

        private async void StartMySQL_Click(object sender, RoutedEventArgs e)
        {
            if (!mysqlIsRunning)
            {
                Log("Building nodastack_mysql...");
                await RunProcessAsync("docker build -t nodastack_mysql ./Docker/mysql");

                Log("Launching nodastack_mysql container...");
                RunProcess("docker run -d --rm -p 3306:3306 --name nodastack_mysql nodastack_mysql", () =>
                {
                    mysqlIsRunning = true;
                    Dispatcher.Invoke(() => StartMySQLButton.Content = "Stop MySQL");
                    Log("MySQL container started.");
                    UpdateIndicator("mysql", mysqlIsRunning);

                });
            }
            else
            {
                Log("Stopping nodastack_phpmyadmin...");
                RunProcess("docker stop nodastack_phpmyadmin", () =>
                {
                    phpmyadminIsRunning = false;
                    Dispatcher.Invoke(() => StartPhpMyAdminButton.Content = "Start phpMyAdmin");
                    Log("phpMyAdmin container stopped.");
                    UpdateIndicator("phpmyadmin", phpmyadminIsRunning);

                    Log("Stopping nodastack_mysql...");
                    RunProcess("docker stop nodastack_mysql", () =>
                    {
                        mysqlIsRunning = false;
                        Dispatcher.Invoke(() => StartMySQLButton.Content = "Start MySQL");
                        Log("MySQL container stopped.");
                        UpdateIndicator("mysql", mysqlIsRunning);
                    });
                });
            }
        }

        private async void StartPhpMyAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (!phpmyadminIsRunning)
            {
                if (!mysqlIsRunning)
                {
                    Log("MySQL must be running before starting phpMyAdmin.");
                    return;
                }

                Log("Building nodastack_phpmyadmin...");
                await RunProcessAsync("docker build -t nodastack_phpmyadmin ./Docker/phpmyadmin");

                Log("Launching nodastack_phpmyadmin container...");
                RunProcess("docker run -d --rm -p 8081:80 --name nodastack_phpmyadmin --link nodastack_mysql " +
                        "-e PMA_HOST=nodastack_mysql -e PMA_USER=root -e PMA_PASSWORD= -e MYSQL_ALLOW_EMPTY_PASSWORD=yes " +
                        "nodastack_phpmyadmin", () =>
                {
                    phpmyadminIsRunning = true;
                    UpdateIndicator("phpmyadmin", true);
                    Dispatcher.Invoke(() => StartPhpMyAdminButton.Content = "Stop phpMyAdmin");
                    Log("phpMyAdmin container started.");
                    UpdateIndicator("phpmyadmin", phpmyadminIsRunning);
                });
            }
            else
            {
                Log("Stopping nodastack_phpmyadmin...");
                RunProcess("docker stop nodastack_phpmyadmin", () =>
                {
                    phpmyadminIsRunning = false;
                    UpdateIndicator("phpmyadmin", false);
                    Dispatcher.Invoke(() => StartPhpMyAdminButton.Content = "Start phpMyAdmin");
                    Log("phpMyAdmin container stopped.");
                    UpdateIndicator("phpmyadmin", phpmyadminIsRunning);
                });
            }
        }

        private void OpenApache_Click(object sender, RoutedEventArgs e)
        {
            if (apacheIsRunning)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "http://localhost:8080",
                        UseShellExecute = true
                    });
                    Log("Opening Apache in browser...");
                }
                catch (Exception ex)
                {
                    Log($"Error opening Apache: {ex.Message}");
                }
            }
        }

        private void OpenPhp_Click(object sender, RoutedEventArgs e)
        {
            if (phpIsRunning)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "http://localhost:8000",
                        UseShellExecute = true
                    });
                    Log("Opening PHP in browser...");
                }
                catch (Exception ex)
                {
                    Log($"Error opening PHP: {ex.Message}");
                }
            }
        }

        private void OpenMySql_Click(object sender, RoutedEventArgs e)
        {
            if (mysqlIsRunning)
            {
                try
                {
                    string connectionString = "Server=localhost;Port=3306;Database=nodastack;Uid=root;Pwd=;";
                    System.Windows.Clipboard.SetText(connectionString);
                    Log("MySQL connection string copied to clipboard!");
                    Log("Connection: Server=localhost;Port=3306;Database=nodastack;Uid=root;Pwd=;");
                }
                catch (Exception ex)
                {
                    Log($"Error copying connection string: {ex.Message}");
                }
            }
        }

        private void OpenPhpMyAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (phpmyadminIsRunning)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "http://localhost:8081",
                        UseShellExecute = true
                    });
                    Log("Opening phpMyAdmin in browser...");
                }
                catch (Exception ex)
                {
                    Log($"Error opening phpMyAdmin: {ex.Message}");
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
                            Log("ERROR: " + e.Data);
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
                Log("Exception: " + ex.Message);
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
                                Log("ERROR: " + e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    Log("Exception: " + ex.Message);
                }
            });
        }

        private async void CheckInitialContainerStatus()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Apache
                    CheckContainerStatus("nodastack_apache", (isRunning) =>
                    {
                        if (isRunning)
                        {
                            apacheIsRunning = true;
                            UpdateIndicator("apache", true);
                            Dispatcher.Invoke(() => StartApacheButton.Content = "Stop Apache");
                            Log("Apache container already running.");
                        }
                    });

                    // PHP
                    CheckContainerStatus("nodastack_php", (isRunning) =>
                    {
                        if (isRunning)
                        {
                            phpIsRunning = true;
                            UpdateIndicator("php", true);
                            Dispatcher.Invoke(() => StartPHPButton.Content = "Stop PHP");
                            Log("PHP container already running.");
                        }
                    });

                    // MySQL
                    CheckContainerStatus("nodastack_mysql", (isRunning) =>
                    {
                        if (isRunning)
                        {
                            mysqlIsRunning = true;
                            UpdateIndicator("mysql", true);
                            Dispatcher.Invoke(() => StartMySQLButton.Content = "Stop MySQL");
                            Log("MySQL container already running.");
                        }
                    });

                    // phpMyAdmin
                    CheckContainerStatus("nodastack_phpmyadmin", (isRunning) =>
                    {
                        if (isRunning)
                        {
                            phpmyadminIsRunning = true;
                            UpdateIndicator("phpmyadmin", true);
                            Dispatcher.Invoke(() => StartPhpMyAdminButton.Content = "Stop phpMyAdmin");
                            Log("phpMyAdmin container already running.");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log("Exception during status check: " + ex.Message);
                }
            });
        }

        private void CheckContainerStatus(string containerName, Action<bool> callback)
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
                callback(isRunning);
            }
            catch (Exception ex)
            {
                Log($"Exception during {containerName} status check: " + ex.Message);
            }
        }

        private void UpdateIndicator(string serviceName, bool isRunning)
        {
            Dispatcher.Invoke(() =>
            {
                Brush color = isRunning ? Brushes.LimeGreen : Brushes.Red;

                switch (serviceName.ToLower())
                {
                    case "apache":
                        ApacheIndicator.Fill = color;
                        OpenApacheButton.IsEnabled = isRunning;
                        ViewApacheButton.IsEnabled = isRunning && ProjectsListView.SelectedItem != null;
                        break;
                    case "php":
                        PhpIndicator.Fill = color;
                        OpenPhpButton.IsEnabled = isRunning;
                        ViewPhpButton.IsEnabled = isRunning && ProjectsListView.SelectedItem != null;
                        break;
                    case "mysql":
                        MySqlIndicator.Fill = color;
                        OpenMySqlButton.IsEnabled = isRunning;
                        break;
                    case "phpmyadmin":
                        PhpMyAdminIndicator.Fill = color;
                        OpenPhpMyAdminButton.IsEnabled = isRunning;
                        break;
                }
            });
        }

        private void RefreshProjectsList()
        {
            var projects = projectManager.GetProjects();
            ProjectsListView.ItemsSource = projects;

            Log($"Found {projects.Count} project(s) in www/ directory");
        }

        private void ProjectsListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            bool hasSelection = ProjectsListView.SelectedItem != null;
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
            var projectName = NewProjectTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(projectName) || projectName == "Project name...")
            {
                MessageBox.Show("Please enter a project name.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (projectManager.CreateProject(projectName))
            {
                Log($"Project '{projectName}' created successfully");
                NewProjectTextBox.Text = "Project name...";
                NewProjectTextBox.Foreground = Brushes.Gray;
                RefreshProjectsList();
            }
            else
            {
                Log($"Failed to create project '{projectName}' (already exists or invalid name)");
                MessageBox.Show("Failed to create project. It may already exist or the name is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshProjects_Click(object sender, RoutedEventArgs e)
        {
            RefreshProjectsList();
        }

        private void OpenProjectsFolder_Click(object sender, RoutedEventArgs e)
        {
            projectManager.OpenProjectsDirectory();
            Log("Opened projects directory in explorer");
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListView.SelectedItem is ProjectInfo project)
            {
                projectManager.OpenProjectInExplorer(project.Name);
                Log($"Opened project '{project.Name}' in explorer");
            }
        }

        private void ViewApache_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListView.SelectedItem is ProjectInfo project)
            {
                if (apacheIsRunning)
                {
                    projectManager.OpenProjectInBrowser(project.Name, true);
                    Log($"Opening project '{project.Name}' via Apache");
                }
                else
                {
                    MessageBox.Show("Apache must be running to view projects.", "Apache Not Running", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ViewPhp_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListView.SelectedItem is ProjectInfo project)
            {
                if (phpIsRunning)
                {
                    projectManager.OpenProjectInBrowser(project.Name, false);
                    Log($"Opening project '{project.Name}' via PHP server");
                }
                else
                {
                    MessageBox.Show("PHP server must be running to view projects.", "PHP Not Running", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    if (projectManager.DeleteProject(project.Name))
                    {
                        Log($"Project '{project.Name}' deleted successfully");
                        RefreshProjectsList();
                    }
                    else
                    {
                        Log($"Failed to delete project '{project.Name}'");
                        MessageBox.Show("Failed to delete project.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
