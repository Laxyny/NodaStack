using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NodaStack
{
    public partial class MonitoringWindow : Window
    {
        private ContainerMonitor? containerMonitor;
        private readonly LogManager logManager;
        private readonly List<ContainerInfo> containerInfos = new();
        private readonly List<PortInfo> portInfos = new();
        private bool isInitialized = false;

        public MonitoringWindow(LogManager logManager)
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(ThemeManager.IsDarkTheme);
            this.logManager = logManager;

            Loaded += MonitoringWindow_Loaded;
        }

        private void MonitoringWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                isInitialized = true;

                containerMonitor = new ContainerMonitor(OnContainerUpdated);
                logManager.OnLogAdded += OnLogAdded;

                LoadInitialData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing monitoring: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadInitialData()
        {
            try
            {
                RefreshLogs();
                ScanPorts_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initial data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnContainerUpdated(string containerName, ContainerInfo info)
        {
            if (!isInitialized) return;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    var existing = containerInfos.FirstOrDefault(c => c.Name == containerName);
                    if (existing != null)
                    {
                        containerInfos.Remove(existing);
                    }

                    containerInfos.Add(info);
                    ContainersListView.ItemsSource = null;
                    ContainersListView.ItemsSource = containerInfos;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating container: {ex.Message}");
            }
        }

        private void OnLogAdded(LogEntry entry)
        {
            if (!isInitialized) return;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    RefreshLogs();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding log: {ex.Message}");
            }
        }

        private void RefreshLogs()
        {
            if (!isInitialized) return;

            try
            {
                var filterLevel = GetSelectedLogLevel();
                var filterService = GetSelectedService();

                var logs = logManager.GetLogs(filterLevel, filterService);

                if (LogsListView != null)
                {
                    LogsListView.ItemsSource = logs.OrderByDescending(l => l.Timestamp).Take(500).ToList();
                }
            }
            catch (Exception ex)
            {
                if (isInitialized)
                {
                    MessageBox.Show($"Error refreshing logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private LogLevel? GetSelectedLogLevel()
        {
            try
            {
                if (LogLevelComboBox?.SelectedItem is ComboBoxItem selectedItem)
                {
                    var content = selectedItem.Content?.ToString();
                    return content switch
                    {
                        "Error" => LogLevel.Error,
                        "Warning" => LogLevel.Warning,
                        "Info" => LogLevel.Info,
                        "Debug" => LogLevel.Debug,
                        _ => null
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string? GetSelectedService()
        {
            try
            {
                if (ServiceComboBox?.SelectedItem is ComboBoxItem selectedItem)
                {
                    var content = selectedItem.Content?.ToString();
                    return content == "All" ? null : content;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshLogs();
                ScanPorts_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logManager.ClearLogs();
                RefreshLogs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogLevelFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized) return;

            try
            {
                RefreshLogs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ServiceFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized) return;

            try
            {
                RefreshLogs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ScanPorts_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                var startPortText = StartPortTextBox?.Text ?? "8000";
                var endPortText = EndPortTextBox?.Text ?? "9000";

                if (!int.TryParse(startPortText, out int startPort) ||
                    !int.TryParse(endPortText, out int endPort))
                {
                    MessageBox.Show("Invalid port range", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var ports = Enumerable.Range(startPort, endPort - startPort + 1);
                var portStatuses = await PortChecker.CheckPortsAsync(ports);
                var servicePorts = PortChecker.GetServicePorts();

                portInfos.Clear();

                foreach (var kvp in portStatuses)
                {
                    var service = servicePorts.FirstOrDefault(sp => sp.Value == kvp.Key).Key ?? "";

                    portInfos.Add(new PortInfo
                    {
                        Port = kvp.Key,
                        Status = kvp.Value ? "Available" : "In Use",
                        Service = service
                    });
                }

                if (PortsListView != null)
                {
                    PortsListView.ItemsSource = null;
                    PortsListView.ItemsSource = portInfos;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning ports: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                isInitialized = false;
                containerMonitor?.Dispose();
                if (logManager != null)
                {
                    logManager.OnLogAdded -= OnLogAdded;
                }
            }
            catch
            {
                // Ignore disposal errors
            }
            base.OnClosed(e);
        }
    }

    public class PortInfo
    {
        public int Port { get; set; }
        public string Status { get; set; } = "";
        public string Service { get; set; } = "";
    }
}