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
                });
            }
            else
            {
                Log("Stopping nodastack_mysql container...");
                RunProcess("docker stop nodastack_mysql", () =>
                {
                    mysqlIsRunning = false;
                    Dispatcher.Invoke(() => StartMySQLButton.Content = "Start MySQL");
                    Log("MySQL container stopped.");
                });
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
                    if (e.Data != null) Log("ERROR: " + e.Data);
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
                        if (e.Data != null)
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
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/C docker ps --filter \"name=nodastack_apache\" --format \"{{.Names}}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    var process = new Process { StartInfo = psi };
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output) && output.Contains("nodastack_apache"))
                    {
                        apacheIsRunning = true;
                        Log("Apache container already running.");
                    }
                }
                catch (Exception ex)
                {
                    Log("Exception during status check: " + ex.Message);
                }
            });
        }
    }
}
