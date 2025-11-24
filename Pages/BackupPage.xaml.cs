using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ModernWpf.Controls;

namespace NodaStack.Pages
{
    public partial class BackupPage : System.Windows.Controls.Page
    {
        private BackupManager backupManager;
        private BackupInfo? selectedBackup;

        public BackupPage()
        {
            InitializeComponent();
            backupManager = new BackupManager(new ConfigurationManager(), new LogManager());
            LoadBackups();
        }

        private void LoadBackups()
        {
            try
            {
                BackupsGrid.ItemsSource = backupManager.GetBackupList();
            }
            catch { }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadBackups();

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Zip files (*.zip)|*.zip",
                Title = "Import Backup"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string destFile = Path.Combine(backupManager.GetBackupDirectory(), Path.GetFileName(openFileDialog.FileName));
                    File.Copy(openFileDialog.FileName, destFile, true);
                    LoadBackups();
                    MessageBox.Show("Backup imported successfully.", "Success");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing: {ex.Message}", "Error");
                }
            }
        }

        private void BackupsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedBackup = BackupsGrid.SelectedItem as BackupInfo;
            
            if (selectedBackup != null)
            {
                DetailsCard.Opacity = 1;
                DetailsCard.IsEnabled = true;
                
                DetailName.Text = selectedBackup.Name;
                DetailDate.Text = selectedBackup.CreatedAt.ToString("g");
                DetailSize.Text = FormatFileSize(selectedBackup.FileSize);
            }
            else
            {
                DetailsCard.Opacity = 0.5;
                DetailsCard.IsEnabled = false;
                
                DetailName.Text = "-";
                DetailDate.Text = "-";
                DetailSize.Text = "-";
            }
        }

        private async void CreateFullBackup_Click(object sender, RoutedEventArgs e)
        {
            var name = BackupNameBox.Text.Trim();
            await backupManager.CreateFullBackupAsync(name);
            LoadBackups();
            BackupNameBox.Text = "";
        }

        private async void CreateConfigBackup_Click(object sender, RoutedEventArgs e)
        {
            var name = BackupNameBox.Text.Trim();
            await backupManager.CreateConfigBackupAsync(name);
            LoadBackups();
            BackupNameBox.Text = "";
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBackup == null) return;

            var result = MessageBox.Show($"Restore '{selectedBackup.Name}'? Current data will be overwritten.", "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await backupManager.RestoreBackupAsync(selectedBackup.FilePath);
                MessageBox.Show("Restore completed.", "Success");
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBackup == null) return;

            var saveFileDialog = new SaveFileDialog
            {
                FileName = Path.GetFileName(selectedBackup.FilePath),
                Filter = "Zip files (*.zip)|*.zip"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(selectedBackup.FilePath, saveFileDialog.FileName, true);
                    MessageBox.Show("Backup exported successfully.", "Success");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting: {ex.Message}", "Error");
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBackup == null) return;

            var result = MessageBox.Show($"Delete '{selectedBackup.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                backupManager.DeleteBackup(selectedBackup.FilePath);
                LoadBackups();
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
