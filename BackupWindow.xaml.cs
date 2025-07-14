using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NodaStack
{
    public partial class BackupWindow : Window
    {
        private readonly BackupManager backupManager;
        private readonly LogManager logManager;
        private List<BackupInfo> backups;
        private BackupInfo selectedBackup;

        public BackupWindow(BackupManager backupManager, LogManager logManager)
        {
            InitializeComponent();
            this.backupManager = backupManager;
            this.logManager = logManager;
            this.backups = new List<BackupInfo>();

            LoadBackups();
        }

        private void LoadBackups()
        {
            try
            {
                backups = backupManager.GetBackupList();
                BackupDataGrid.ItemsSource = backups;

                StatusTextBlock.Text = $"Loaded {backups.Count} backup(s)";
            }
            catch (Exception ex)
            {
                logManager.Log($"Error loading backups: {ex.Message}", LogLevel.Error, "Backup");
                StatusTextBlock.Text = "Error loading backups";
            }
        }

        private async void CreateFullBackupButton_Click(object sender, RoutedEventArgs e)
        {
            await CreateBackupAsync(BackupType.Full);
        }

        private async void CreateConfigBackupButton_Click(object sender, RoutedEventArgs e)
        {
            await CreateBackupAsync(BackupType.Configuration);
        }

        private async Task CreateBackupAsync(BackupType backupType)
        {
            try
            {
                var backupName = BackupNameTextBox.Text.Trim();

                SetOperationInProgress(true, $"Creating {backupType.ToString().ToLower()} backup...");

                bool success = false;

                await Task.Run(async () =>
                {
                    if (backupType == BackupType.Full)
                    {
                        success = await backupManager.CreateFullBackupAsync(backupName);
                    }
                    else if (backupType == BackupType.Configuration)
                    {
                        success = await backupManager.CreateConfigBackupAsync(backupName);
                    }
                });

                if (success)
                {
                    StatusTextBlock.Text = $"{backupType} backup created successfully";
                    BackupNameTextBox.Clear();
                    LoadBackups();
                }
                else
                {
                    StatusTextBlock.Text = $"Failed to create {backupType.ToString().ToLower()} backup";
                }
            }
            catch (Exception ex)
            {
                logManager.Log($"Error creating backup: {ex.Message}", LogLevel.Error, "Backup");
                StatusTextBlock.Text = "Error creating backup";
            }
            finally
            {
                SetOperationInProgress(false);
            }
        }

        private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBackup == null)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to restore the backup '{selectedBackup.Name}'?\n\nThis will overwrite current configuration and projects.",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    SetOperationInProgress(true, "Restoring backup...");

                    bool success = false;
                    await Task.Run(async () =>
                    {
                        success = await backupManager.RestoreBackupAsync(selectedBackup.FilePath);
                    });

                    if (success)
                    {
                        StatusTextBlock.Text = "Backup restored successfully";
                        MessageBox.Show("Backup restored successfully. Please restart NodaStack to apply changes.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        StatusTextBlock.Text = "Failed to restore backup";
                    }
                }
                catch (Exception ex)
                {
                    logManager.Log($"Error restoring backup: {ex.Message}", LogLevel.Error, "Backup");
                    StatusTextBlock.Text = "Error restoring backup";
                }
                finally
                {
                    SetOperationInProgress(false);
                }
            }
        }

        private void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBackup == null)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete the backup '{selectedBackup.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (backupManager.DeleteBackup(selectedBackup.FilePath))
                    {
                        StatusTextBlock.Text = "Backup deleted successfully";
                        LoadBackups();
                        ClearBackupDetails();
                    }
                    else
                    {
                        StatusTextBlock.Text = "Failed to delete backup";
                    }
                }
                catch (Exception ex)
                {
                    logManager.Log($"Error deleting backup: {ex.Message}", LogLevel.Error, "Backup");
                    StatusTextBlock.Text = "Error deleting backup";
                }
            }
        }

        private void ImportBackupButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Backup Files (*.zip)|*.zip|All Files (*.*)|*.*",
                Title = "Select Backup File to Import"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups");
                    var fileName = Path.GetFileName(openFileDialog.FileName);
                    var targetPath = Path.Combine(backupDirectory, fileName);

                    if (File.Exists(targetPath))
                    {
                        var result = MessageBox.Show(
                            $"A backup with the name '{fileName}' already exists. Do you want to overwrite it?",
                            "Backup Exists",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                            return;
                    }

                    File.Copy(openFileDialog.FileName, targetPath, true);
                    StatusTextBlock.Text = "Backup imported successfully";
                    LoadBackups();
                }
                catch (Exception ex)
                {
                    logManager.Log($"Error importing backup: {ex.Message}", LogLevel.Error, "Backup");
                    StatusTextBlock.Text = "Error importing backup";
                }
            }
        }

        private void ExportBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBackup == null)
                return;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Backup Files (*.zip)|*.zip|All Files (*.*)|*.*",
                Title = "Export Backup",
                FileName = selectedBackup.Name + ".zip"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(selectedBackup.FilePath, saveFileDialog.FileName, true);
                    StatusTextBlock.Text = "Backup exported successfully";
                }
                catch (Exception ex)
                {
                    logManager.Log($"Error exporting backup: {ex.Message}", LogLevel.Error, "Backup");
                    StatusTextBlock.Text = "Error exporting backup";
                }
            }
        }

        private void BackupDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedBackup = BackupDataGrid.SelectedItem as BackupInfo;

            bool hasSelection = selectedBackup != null;
            RestoreBackupButton.IsEnabled = hasSelection;
            DeleteBackupButton.IsEnabled = hasSelection;
            ExportBackupButton.IsEnabled = hasSelection;

            if (hasSelection)
            {
                ShowBackupDetails(selectedBackup);
            }
            else
            {
                ClearBackupDetails();
            }
        }

        private void ShowBackupDetails(BackupInfo backup)
        {
            BackupDetailsPanel.Children.Clear();

            var details = new[]
            {
                ("Name:", backup.Name),
                ("Type:", backup.Type.ToString()),
                ("Created:", backup.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                ("File Size:", FormatFileSize(backup.FileSize)),
                ("Backup Size:", FormatFileSize(backup.Size)),
                ("Path:", backup.FilePath)
            };

            foreach (var (label, value) in details)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

                panel.Children.Add(new TextBlock
                {
                    Text = label,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    Width = 80
                });

                panel.Children.Add(new TextBlock
                {
                    Text = value,
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    TextWrapping = TextWrapping.Wrap
                });

                BackupDetailsPanel.Children.Add(panel);
            }
        }

        private void ClearBackupDetails()
        {
            BackupDetailsPanel.Children.Clear();
            BackupDetailsPanel.Children.Add(new TextBlock
            {
                Text = "No backup selected",
                Foreground = System.Windows.Media.Brushes.Gray,
                FontStyle = FontStyles.Italic
            });
        }

        private void SetOperationInProgress(bool inProgress, string message = "")
        {
            CreateFullBackupButton.IsEnabled = !inProgress;
            CreateConfigBackupButton.IsEnabled = !inProgress;
            RestoreBackupButton.IsEnabled = !inProgress && selectedBackup != null;
            DeleteBackupButton.IsEnabled = !inProgress && selectedBackup != null;
            ImportBackupButton.IsEnabled = !inProgress;
            ExportBackupButton.IsEnabled = !inProgress && selectedBackup != null;
            RefreshButton.IsEnabled = !inProgress;

            if (inProgress)
            {
                StatusTextBlock.Text = message;
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.IsIndeterminate = true;
            }
            else
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressBar.IsIndeterminate = false;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int suffixIndex = 0;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:0.##} {suffixes[suffixIndex]}";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadBackups();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}