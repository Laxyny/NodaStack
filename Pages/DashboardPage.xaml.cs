using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ModernWpf.Controls;
using System.Diagnostics;
using NodaStack.Services;
using NodaStack.Helpers;
using NodaStack.Core.Models;

namespace NodaStack.Pages
{
    public partial class DashboardPage : System.Windows.Controls.Page
    {
        public event EventHandler<string>? ServiceToggleRequested;
        
        private ProjectManager _projectManager;

        public DashboardPage(ProjectManager? manager = null)
        {
            InitializeComponent();
            _projectManager = manager ?? new ProjectManager();
            LoadProjects();
        }

        private void LoadProjects()
        {
            try
            {
                var projects = _projectManager.GetProjects();
                ProjectsGrid.ItemsSource = projects;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading projects: {ex.Message}");
            }
        }

        public void UpdateServiceStatus(string service, bool isRunning)
        {
            switch (service.ToLower())
            {
                case "apache":
                    ApacheToggle.IsOn = isRunning;
                    ApacheStatusDot.Fill = isRunning ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ErrorBrush");
                    ApacheToggle.IsEnabled = true; 
                    break;
                case "php":
                    PhpToggle.IsOn = isRunning;
                    PhpStatusDot.Fill = isRunning ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ErrorBrush");
                    PhpToggle.IsEnabled = true;
                    break;
                case "mysql":
                    MysqlToggle.IsOn = isRunning;
                    MysqlStatusDot.Fill = isRunning ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ErrorBrush");
                    MysqlToggle.IsEnabled = true;
                    break;
                case "phpmyadmin":
                    PmaToggle.IsOn = isRunning;
                    PmaStatusDot.Fill = isRunning ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ErrorBrush");
                    PmaToggle.IsEnabled = true;
                    break;
                case "mailhog":
                    MailHogToggle.IsOn = isRunning;
                    MailHogStatusDot.Fill = isRunning ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ErrorBrush");
                    MailHogToggle.IsEnabled = true;
                    break;
            }
            
            EnableAllToggles();
        }

        public void DisableAllToggles()
        {
            ApacheToggle.IsEnabled = false;
            PhpToggle.IsEnabled = false;
            MysqlToggle.IsEnabled = false;
            PmaToggle.IsEnabled = false;
            MailHogToggle.IsEnabled = false;
        }

        public void EnableAllToggles()
        {
            ApacheToggle.IsEnabled = true;
            PhpToggle.IsEnabled = true;
            MysqlToggle.IsEnabled = true;
            PmaToggle.IsEnabled = true;
            MailHogToggle.IsEnabled = true;
        }

        private void ApacheToggle_Click(object sender, RoutedEventArgs e) 
        { 
            DisableAllToggles();
            ServiceToggleRequested?.Invoke(this, "apache"); 
        }
        private void PhpToggle_Click(object sender, RoutedEventArgs e) 
        { 
            DisableAllToggles();
            ServiceToggleRequested?.Invoke(this, "php"); 
        }
        private void MysqlToggle_Click(object sender, RoutedEventArgs e) 
        { 
            DisableAllToggles();
            ServiceToggleRequested?.Invoke(this, "mysql"); 
        }
        private void PmaToggle_Click(object sender, RoutedEventArgs e) 
        { 
            DisableAllToggles();
            ServiceToggleRequested?.Invoke(this, "phpmyadmin"); 
        }
        private void MailHogToggle_Click(object sender, RoutedEventArgs e) 
        { 
            DisableAllToggles();
            ServiceToggleRequested?.Invoke(this, "mailhog"); 
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).CreateProject_Click(sender, e);
            LoadProjects();
        }

        public void RefreshProjects_Click(object sender, RoutedEventArgs e)
        {
            LoadProjects();
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProjectInfo project)
            {
                ((MainWindow)Application.Current.MainWindow).ViewApache_Direct(project);
            }
        }

        private void BrowseProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProjectInfo project)
            {
                try
                {
                    Process.Start("explorer.exe", project.Path);
                }
                catch { }
            }
        }

        private void OpenVSCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProjectInfo project)
            {
                try
                {
                    var psi = new ProcessStartInfo("code", ".")
                    {
                        WorkingDirectory = project.Path,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("VS Code not found or error launching: " + ex.Message, "Error");
                }
            }
        }

        private void OpenTerminal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProjectInfo project)
            {
                try
                {
                    var psi = new ProcessStartInfo("cmd.exe", $"/k cd /d \"{project.Path}\"")
                    {
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch { }
            }
        }
    }
}
