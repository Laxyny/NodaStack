using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace NodaStack
{
    public partial class MainWindow : Window
    {

        private bool apacheIsRunning = false;
        private bool phpIsRunning = false;
        private bool mysqlIsRunning = false;
        private bool phpmyadminIsRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            CheckInitialContainerStatus();
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

                    StartPhpMyAdmin();
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

                    Log("Stopping nodastack_mysql...");
                    RunProcess("docker stop nodastack_mysql", () =>
                    {
                        mysqlIsRunning = false;
                        Dispatcher.Invoke(() => StartMySQLButton.Content = "Start MySQL");
                        Log("MySQL container stopped.");
                    });
                });
            }
        }

        private async void StartPhpMyAdmin()
        {
            if (!phpmyadminIsRunning)
            {
                Log("Building nodastack_phpmyadmin...");
                await RunProcessAsync("docker build -t nodastack_phpmyadmin ./Docker/phpmyadmin");

                Log("Launching nodastack_phpmyadmin container...");
                RunProcess("docker run -d --rm -p 8081:80 --name nodastack_phpmyadmin --link nodastack_mysql " +
                        "-e PMA_HOST=nodastack_mysql -e PMA_USER=root -e PMA_PASSWORD= -e MYSQL_ALLOW_EMPTY_PASSWORD=yes " +
                        "nodastack_phpmyadmin", () =>
                {
                    phpmyadminIsRunning = true;
                    Dispatcher.Invoke(() => StartPhpMyAdminButton.Content = "Stop phpMyAdmin");
                    Log("phpMyAdmin container started.");
                });
            }
            else
            {
                Log("phpMyAdmin container is already running.");
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
    }
}
