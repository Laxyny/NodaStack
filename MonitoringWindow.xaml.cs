using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NodaStack.Core.Models;
using NodaStack.Converters;
using static NodaStack.NotificationManager;

namespace NodaStack
{
    public partial class MonitoringWindow : Window
    {
        private readonly MonitoringService monitoringService;
        private readonly DispatcherTimer updateTimer;
        private readonly List<ServiceStatus> services = new();
        private readonly List<PortStatus> ports = new();
        private readonly List<MonitoringLogEntry> logs = new();
        private SystemMetrics systemMetrics = new();

        public MonitoringWindow()
        {
            InitializeComponent();
            
            // Initialize monitoring service
            monitoringService = new MonitoringService();
            monitoringService.ServiceUpdated += OnServiceUpdated;
            monitoringService.PortUpdated += OnPortUpdated;
            monitoringService.LogAdded += OnLogAdded;
            monitoringService.SystemMetricsUpdated += OnSystemMetricsUpdated;

            // Setup update timer
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            // Setup event handlers
            SetupEventHandlers();
            
            // Load initial data
            LoadInitialData();
            
            // Setup filters
            SetupFilters();
        }

        private void SetupEventHandlers()
        {
            RefreshButton.Click += RefreshButton_Click;
            ClearLogsButton.Click += ClearLogsButton_Click;
            ScanPortsButton.Click += ScanPortsButton_Click;
            LevelFilterComboBox.SelectionChanged += LevelFilterComboBox_SelectionChanged;
            ServiceFilterComboBox.SelectionChanged += ServiceFilterComboBox_SelectionChanged;
        }

        private void SetupFilters()
        {
            LevelFilterComboBox.SelectedIndex = 0;
            ServiceFilterComboBox.SelectedIndex = 0;
        }

        private void LoadInitialData()
        {
            try
            {
                // Load services
                var serviceList = monitoringService.GetServices();
                services.Clear();
                services.AddRange(serviceList);
                ServicesListView.ItemsSource = services;

                // Load ports
                var portList = monitoringService.GetPorts();
                ports.Clear();
                ports.AddRange(portList);
                PortsListView.ItemsSource = ports;

                // Load logs
                var logList = monitoringService.GetLogs();
                logs.Clear();
                logs.AddRange(logList);
                LogsListView.ItemsSource = logs;

                // Load system metrics
                systemMetrics = monitoringService.GetSystemMetrics();
                UpdateSystemMetricsDisplay();

                LastUpdatedText.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initial data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnServiceUpdated(ServiceStatus service)
        {
            Dispatcher.Invoke(() =>
            {
                // Log pour déboguer
                var existingService = services.FirstOrDefault(s => s.Name == service.Name);
                if (existingService != null)
                {
                    var index = services.IndexOf(existingService);
                    services[index] = service;
                    // Log pour déboguer
                    System.Diagnostics.Debug.WriteLine($"Updated service {service.Name}: {service.Status} (CPU: {service.CpuUsage}%, Memory: {service.MemoryUsage} bytes)");
                }
                else
                {
                    services.Add(service);
                    // Log pour déboguer
                    System.Diagnostics.Debug.WriteLine($"Added service {service.Name}: {service.Status} (CPU: {service.CpuUsage}%, Memory: {service.MemoryUsage} bytes)");
                }

                ServicesListView.Items.Refresh();
                LastUpdatedText.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
            });
        }

        private void OnPortUpdated(PortStatus port)
        {
            Dispatcher.Invoke(() =>
            {
                var existingPort = ports.FirstOrDefault(p => p.Port == port.Port);
                if (existingPort != null)
                {
                    var index = ports.IndexOf(existingPort);
                    ports[index] = port;
                }
                else
                {
                    ports.Add(port);
                }

                PortsListView.Items.Refresh();
            });
        }

        private void OnLogAdded(MonitoringLogEntry log)
        {
            Dispatcher.Invoke(() =>
            {
                logs.Add(log);
                
                // Keep only last 1000 logs
                if (logs.Count > 1000)
                {
                    logs.RemoveAt(0);
                }

                LogsListView.Items.Refresh();
                ApplyLogFilters();
            });
        }

        private void OnSystemMetricsUpdated(SystemMetrics metrics)
        {
            Dispatcher.Invoke(() =>
            {
                systemMetrics = metrics;
                UpdateSystemMetricsDisplay();
            });
        }

        private void UpdateSystemMetricsDisplay()
        {
            try
            {
                // CPU Usage
                CpuUsageText.Text = $"{systemMetrics.TotalCpuUsage:F1}%";
                CpuProgressBar.Value = systemMetrics.TotalCpuUsage;

                // Memory Usage
                var memoryMB = systemMetrics.TotalMemoryUsage / (1024 * 1024);
                var totalMemoryMB = (systemMetrics.TotalMemoryUsage + systemMetrics.AvailableMemory) / (1024 * 1024);
                var memoryPercentage = totalMemoryMB > 0 ? (memoryMB / totalMemoryMB) * 100 : 0;
                
                MemoryUsageText.Text = $"{memoryMB:F0} MB";
                MemoryProgressBar.Value = memoryPercentage;

                // Disk Usage
                var usedDiskGB = (systemMetrics.TotalDiskSpace - systemMetrics.AvailableDiskSpace) / (1024 * 1024 * 1024);
                var totalDiskGB = systemMetrics.TotalDiskSpace / (1024 * 1024 * 1024);
                var diskPercentage = totalDiskGB > 0 ? (usedDiskGB / totalDiskGB) * 100 : 0;
                
                DiskUsageText.Text = $"{usedDiskGB:F1} GB";
                DiskProgressBar.Value = diskPercentage;

                // Connections
                ConnectionsText.Text = systemMetrics.ActiveConnections.ToString();
            }
            catch (Exception)
            {
                // Handle any display errors silently
            }
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            LastUpdatedText.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadInitialData();
                NotificationManager.ShowNotification("Monitoring refreshed successfully!", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logs.Clear();
                LogsListView.Items.Refresh();
                NotificationManager.ShowNotification("Logs cleared successfully!", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ScanPortsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ScanPortsButton.IsEnabled = false;
                ScanPortsButton.Content = "🔍 Scanning...";

                if (int.TryParse(StartPortTextBox.Text, out int startPort) && 
                    int.TryParse(EndPortTextBox.Text, out int endPort))
                {
                    if (startPort > endPort)
                    {
                        MessageBox.Show("Start port must be less than end port.", "Invalid Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (endPort - startPort > 1000)
                    {
                        var result = MessageBox.Show("You're about to scan a large port range. This may take a while. Continue?", 
                            "Large Port Range", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes)
                            return;
                    }

                    var results = await monitoringService.ScanPortsAsync(startPort, endPort);
                    
                    ports.Clear();
                    ports.AddRange(results);
                    PortsListView.Items.Refresh();

                    var openPorts = results.Count(p => p.IsOpen);
                    NotificationManager.ShowNotification($"Port scan completed! Found {openPorts} open ports.", "Success");
                }
                else
                {
                    MessageBox.Show("Please enter valid port numbers.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning ports: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ScanPortsButton.IsEnabled = true;
                ScanPortsButton.Content = "🔍 Scan Ports";
            }
        }

        private void LevelFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyLogFilters();
        }

        private void ServiceFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyLogFilters();
        }

        private void ApplyLogFilters()
        {
            try
            {
                var filteredLogs = logs.AsEnumerable();

                // Apply level filter
                if (LevelFilterComboBox.SelectedItem is ComboBoxItem levelItem && levelItem.Content.ToString() != "All")
                {
                    var selectedLevel = levelItem.Content.ToString();
                    filteredLogs = filteredLogs.Where(log => string.Equals(log.Level, selectedLevel, StringComparison.OrdinalIgnoreCase));
                }

                // Apply service filter
                if (ServiceFilterComboBox.SelectedItem is ComboBoxItem serviceItem && serviceItem.Content.ToString() != "All")
                {
                    var selectedService = serviceItem.Content.ToString();
                    filteredLogs = filteredLogs.Where(log => string.Equals(log.Service, selectedService, StringComparison.OrdinalIgnoreCase));
                }

                LogsListView.ItemsSource = filteredLogs.ToList();
            }
            catch (Exception)
            {
                // Handle filter errors silently
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            updateTimer?.Stop();
            monitoringService?.Dispose();
            base.OnClosing(e);
        }
    }
} 