using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace NodaStack
{
    public partial class BackupWindow : Window
    {
        private BackupManager backupManager;
        private List<BackupInfo> backupList;
        private BackupInfo? selectedBackup;

        public BackupWindow()
        {
            InitializeComponent();
            backupManager = new BackupManager(new ConfigurationManager(), new LogManager());
            backupList = new List<BackupInfo>();
            LoadBackupHistory();
        }

        private void LoadBackupHistory()
        {
            try
            {
                backupList = backupManager.GetBackupList();
                BackupHistoryListView.ItemsSource = backupList;
                StatusText.Text = $"Loaded {backupList.Count} backup(s)";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading backups: {ex.Message}";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadBackupHistory();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool ValidateBackupName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true; // Auto-generated name is OK

            // Check for invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            if (name.Any(c => invalidChars.Contains(c)))
            {
                MessageBox.Show("Backup name contains invalid characters. Please use only letters, numbers, spaces, and common punctuation.", 
                    "Invalid Backup Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Check if name already exists
            var existingBackup = backupList.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existingBackup != null)
            {
                var result = MessageBox.Show($"A backup named '{name}' already exists. Do you want to replace it?", 
                    "Backup Name Exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            }

            return true;
        }

        private async void CreateFullBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string backupName = BackupNameTextBox.Text.Trim();
                if (!ValidateBackupName(backupName))
                    return;

                if (string.IsNullOrEmpty(backupName))
                {
                    backupName = $"backup_{DateTime.Now:yyyy-MM-dd_HH-mm}";
                }

                StatusText.Text = "Creating full backup...";
                CreateFullBackupButton.IsEnabled = false;
                CreateConfigBackupButton.IsEnabled = false;

                var success = await backupManager.CreateFullBackupAsync(backupName);
                
                if (success)
                {
                    StatusText.Text = "Full backup created successfully";
                    LoadBackupHistory();
                    BackupNameTextBox.Clear();
                    NotificationManager.ShowNotification("Backup Created", "Full backup completed successfully", NotificationType.Success);
                }
                else
                {
                    StatusText.Text = "Failed to create full backup";
                    NotificationManager.ShowNotification("Backup Failed", "Failed to create full backup", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error creating backup: {ex.Message}";
                NotificationManager.ShowNotification("Backup Error", $"Error creating backup: {ex.Message}", NotificationType.Error);
            }
            finally
            {
                CreateFullBackupButton.IsEnabled = true;
                CreateConfigBackupButton.IsEnabled = true;
            }
        }

        private async void CreateConfigBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string backupName = BackupNameTextBox.Text.Trim();
                if (!ValidateBackupName(backupName))
                    return;

                if (string.IsNullOrEmpty(backupName))
                {
                    backupName = $"config_backup_{DateTime.Now:yyyy-MM-dd_HH-mm}";
                }

                StatusText.Text = "Creating configuration backup...";
                CreateFullBackupButton.IsEnabled = false;
                CreateConfigBackupButton.IsEnabled = false;

                var success = await backupManager.CreateConfigBackupAsync(backupName);
                
                if (success)
                {
                    StatusText.Text = "Configuration backup created successfully";
                    LoadBackupHistory();
                    BackupNameTextBox.Clear();
                    NotificationManager.ShowNotification("Backup Created", "Configuration backup completed successfully", NotificationType.Success);
                }
                else
                {
                    StatusText.Text = "Failed to create configuration backup";
                    NotificationManager.ShowNotification("Backup Failed", "Failed to create configuration backup", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error creating config backup: {ex.Message}";
                NotificationManager.ShowNotification("Backup Error", $"Error creating config backup: {ex.Message}", NotificationType.Error);
            }
            finally
            {
                CreateFullBackupButton.IsEnabled = true;
                CreateConfigBackupButton.IsEnabled = true;
            }
        }

        private void ImportBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Backup File",
                    Filter = "Backup files (*.zip)|*.zip|All files (*.*)|*.*",
                    DefaultExt = "zip"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var sourcePath = openFileDialog.FileName;
                    var fileName = Path.GetFileName(sourcePath);
                    var targetPath = Path.Combine(backupManager.GetBackupDirectory(), fileName);

                    if (File.Exists(targetPath))
                    {
                        var result = MessageBox.Show(
                            "A backup with this name already exists. Do you want to replace it?",
                            "Backup Import",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                            return;
                    }

                    File.Copy(sourcePath, targetPath, true);
                    StatusText.Text = $"Backup imported successfully: {fileName}";
                    LoadBackupHistory();
                    NotificationManager.ShowNotification("Backup Imported", $"Backup '{fileName}' imported successfully", NotificationType.Success);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error importing backup: {ex.Message}";
                NotificationManager.ShowNotification("Import Error", $"Error importing backup: {ex.Message}", NotificationType.Error);
            }
        }

        private void ExportSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBackup == null)
            {
                MessageBox.Show("Please select a backup to export.", "Export Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Export Backup",
                    Filter = "Backup files (*.zip)|*.zip|All files (*.*)|*.*",
                    DefaultExt = "zip",
                    FileName = $"{selectedBackup.Name}.zip"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.Copy(selectedBackup.FilePath, saveFileDialog.FileName, true);
                    StatusText.Text = $"Backup exported successfully: {Path.GetFileName(saveFileDialog.FileName)}";
                    NotificationManager.ShowNotification("Backup Exported", $"Backup exported to {Path.GetFileName(saveFileDialog.FileName)}", NotificationType.Success);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error exporting backup: {ex.Message}";
                NotificationManager.ShowNotification("Export Error", $"Error exporting backup: {ex.Message}", NotificationType.Error);
            }
        }

        private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBackup == null)
            {
                MessageBox.Show("Please select a backup to restore.", "Restore Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to restore the backup '{selectedBackup.Name}'?\n\nThis will overwrite current configuration and data.",
                "Restore Backup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusText.Text = "Restoring backup...";
                    RestoreBackupButton.IsEnabled = false;
                    DeleteBackupButton.IsEnabled = false;

                    var success = await backupManager.RestoreBackupAsync(selectedBackup.FilePath);
                    
                    if (success)
                    {
                        StatusText.Text = "Backup restored successfully";
                        NotificationManager.ShowNotification("Backup Restored", "Backup restored successfully", NotificationType.Success);
                        MessageBox.Show("Backup restored successfully. The application may need to be restarted.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusText.Text = "Failed to restore backup";
                        NotificationManager.ShowNotification("Restore Failed", "Failed to restore backup", NotificationType.Error);
                        MessageBox.Show("Failed to restore backup. Please check the logs for details.", "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error restoring backup: {ex.Message}";
                    NotificationManager.ShowNotification("Restore Error", $"Error restoring backup: {ex.Message}", NotificationType.Error);
                    MessageBox.Show($"Error restoring backup: {ex.Message}", "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    RestoreBackupButton.IsEnabled = true;
                    DeleteBackupButton.IsEnabled = true;
                }
            }
        }

        private void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBackup == null)
            {
                MessageBox.Show("Please select a backup to delete.", "Delete Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the backup '{selectedBackup.Name}'?\n\nThis action cannot be undone.",
                "Delete Backup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = backupManager.DeleteBackup(selectedBackup.FilePath);
                    
                    if (success)
                    {
                        StatusText.Text = "Backup deleted successfully";
                        LoadBackupHistory();
                        selectedBackup = null;
                        NotificationManager.ShowNotification("Backup Deleted", "Backup deleted successfully", NotificationType.Success);
                    }
                    else
                    {
                        StatusText.Text = "Failed to delete backup";
                        NotificationManager.ShowNotification("Delete Failed", "Failed to delete backup", NotificationType.Error);
                    }
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error deleting backup: {ex.Message}";
                    NotificationManager.ShowNotification("Delete Error", $"Error deleting backup: {ex.Message}", NotificationType.Error);
                }
            }
        }

        private void BackupHistoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedBackup = BackupHistoryListView.SelectedItem as BackupInfo;
            
            ExportSelectedButton.IsEnabled = selectedBackup != null;
            RestoreBackupButton.IsEnabled = selectedBackup != null;
            DeleteBackupButton.IsEnabled = selectedBackup != null;

            UpdateBackupDetails();
        }

        private void UpdateBackupDetails()
        {
            if (selectedBackup == null)
            {
                BackupDetailsText.Text = "Select a backup to view details";
                return;
            }

            var details = $"Name: {selectedBackup.Name}\n" +
                         $"Type: {selectedBackup.Type}\n" +
                         $"Created: {selectedBackup.CreatedAt:dd/MM/yyyy HH:mm:ss}\n" +
                         $"Size: {FormatFileSize(selectedBackup.FileSize)}\n" +
                         $"Path: {selectedBackup.FilePath}";

            BackupDetailsText.Text = details;
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