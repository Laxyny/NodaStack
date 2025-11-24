using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NodaStack.Core.Models;

namespace NodaStack.Pages
{
    public partial class MonitoringPage : System.Windows.Controls.Page
    {
        private readonly MonitoringService monitoringService;
        private readonly DispatcherTimer updateTimer;
        private readonly List<MonitoringLogEntry> logs = new();
        private SystemMetrics systemMetrics = new();

        public MonitoringPage()
        {
            InitializeComponent();

            monitoringService = new MonitoringService();
            monitoringService.LogAdded += OnLogAdded;
            monitoringService.SystemMetricsUpdated += OnSystemMetricsUpdated;

            updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            LoadInitialData();
        }

        private void LoadInitialData()
        {
            try
            {
                // Initial logs
                logs.Clear();
                logs.AddRange(monitoringService.GetLogs());
                ApplyLogFilters();

                // Initial metrics
                systemMetrics = monitoringService.GetSystemMetrics();
                UpdateSystemMetricsDisplay();
            }
            catch { }
        }

        private void OnLogAdded(MonitoringLogEntry log)
        {
            Dispatcher.Invoke(() =>
            {
                logs.Add(log);
                if (logs.Count > 1000) logs.RemoveAt(0);
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
                CpuUsageText.Text = $"{systemMetrics.TotalCpuUsage:F1}%";
                CpuProgressBar.Value = systemMetrics.TotalCpuUsage;

                var memoryMB = systemMetrics.TotalMemoryUsage / (1024 * 1024);
                var totalMemoryMB = (systemMetrics.TotalMemoryUsage + systemMetrics.AvailableMemory) / (1024 * 1024);
                var memPercent = totalMemoryMB > 0 ? (memoryMB * 100.0 / totalMemoryMB) : 0;
                MemoryUsageText.Text = $"{memoryMB:F0} MB";
                MemoryProgressBar.Value = memPercent;

                var usedDiskGB = (systemMetrics.TotalDiskSpace - systemMetrics.AvailableDiskSpace) / (1024 * 1024 * 1024);
                DiskUsageText.Text = $"{usedDiskGB:F1} GB";
                // Approx disk % - assuming total disk space is consistent
                var totalDiskGB = systemMetrics.TotalDiskSpace / (1024 * 1024 * 1024);
                DiskProgressBar.Value = totalDiskGB > 0 ? (usedDiskGB * 100.0 / totalDiskGB) : 0;
            }
            catch { }
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            // Periodic refresh if needed, mainly metrics are event driven
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadInitialData();

        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            logs.Clear();
            ApplyLogFilters();
        }

        private void LevelFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyLogFilters();
        private void ServiceFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyLogFilters();

        private void ApplyLogFilters()
        {
            // Ensure controls are initialized before accessing
            if (LogsListView == null || LevelFilterComboBox == null || ServiceFilterComboBox == null) return;

            var filtered = logs.AsEnumerable();

            if (LevelFilterComboBox.SelectedItem is ComboBoxItem lItem && lItem.Content.ToString() != "All")
            {
                filtered = filtered.Where(l => l.Level.Equals(lItem.Content.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            if (ServiceFilterComboBox.SelectedItem is ComboBoxItem sItem && sItem.Content.ToString() != "All")
            {
                filtered = filtered.Where(l => l.Service.Equals(sItem.Content.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            LogsListView.ItemsSource = filtered.OrderByDescending(l => l.Timestamp).ToList();
        }

        // Cleanup
        public void Dispose()
        {
            updateTimer?.Stop();
        }
    }
}

