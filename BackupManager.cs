using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NodaStack
{
    public class BackupManager
    {
        private readonly string backupDirectory;
        private readonly ConfigurationManager configManager;
        private readonly LogManager logManager;

        public BackupManager(ConfigurationManager configManager, LogManager logManager)
        {
            this.configManager = configManager;
            this.logManager = logManager;
            backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups");
            Directory.CreateDirectory(backupDirectory);
        }

        public async Task<bool> CreateFullBackupAsync(string backupName = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(backupName))
                {
                    backupName = $"backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                }

                var backupPath = Path.Combine(backupDirectory, $"{backupName}.zip");
                var tempDir = Path.Combine(Path.GetTempPath(), "nodastack_backup_temp");

                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);

                logManager.Log($"Creating full backup: {backupName}", LogLevel.Info, "Backup");

                await BackupConfigurationAsync(tempDir);
                await BackupProjectsAsync(tempDir);
                await BackupDatabaseAsync(tempDir);
                await BackupLogsAsync(tempDir);

                var backupInfo = new BackupInfo
                {
                    Name = backupName,
                    CreatedAt = DateTime.Now,
                    Type = BackupType.Full,
                    Size = CalculateDirectorySize(tempDir)
                };

                var infoPath = Path.Combine(tempDir, "backup_info.json");
                await File.WriteAllTextAsync(infoPath, JsonSerializer.Serialize(backupInfo, new JsonSerializerOptions { WriteIndented = true }));

                ZipFile.CreateFromDirectory(tempDir, backupPath);
                Directory.Delete(tempDir, true);

                logManager.Log($"Full backup created successfully: {backupPath}", LogLevel.Info, "Backup");
                return true;
            }
            catch (Exception ex)
            {
                logManager.Log($"Error creating backup: {ex.Message}", LogLevel.Error, "Backup");
                return false;
            }
        }

        public async Task<bool> CreateConfigBackupAsync(string backupName = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(backupName))
                {
                    backupName = $"config_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                }

                var backupPath = Path.Combine(backupDirectory, $"{backupName}.zip");
                var tempDir = Path.Combine(Path.GetTempPath(), "nodastack_config_backup_temp");

                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);

                await BackupConfigurationAsync(tempDir);

                var backupInfo = new BackupInfo
                {
                    Name = backupName,
                    CreatedAt = DateTime.Now,
                    Type = BackupType.Configuration,
                    Size = CalculateDirectorySize(tempDir)
                };

                var infoPath = Path.Combine(tempDir, "backup_info.json");
                await File.WriteAllTextAsync(infoPath, JsonSerializer.Serialize(backupInfo, new JsonSerializerOptions { WriteIndented = true }));

                ZipFile.CreateFromDirectory(tempDir, backupPath);
                Directory.Delete(tempDir, true);

                logManager.Log($"Configuration backup created: {backupPath}", LogLevel.Info, "Backup");
                return true;
            }
            catch (Exception ex)
            {
                logManager.Log($"Error creating config backup: {ex.Message}", LogLevel.Error, "Backup");
                return false;
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    logManager.Log($"Backup file not found: {backupPath}", LogLevel.Error, "Backup");
                    return false;
                }

                var tempDir = Path.Combine(Path.GetTempPath(), "nodastack_restore_temp");
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);

                ZipFile.ExtractToDirectory(backupPath, tempDir);

                var infoPath = Path.Combine(tempDir, "backup_info.json");
                if (File.Exists(infoPath))
                {
                    var backupInfo = JsonSerializer.Deserialize<BackupInfo>(await File.ReadAllTextAsync(infoPath));
                    logManager.Log($"Restoring backup: {backupInfo?.Name} (Type: {backupInfo?.Type})", LogLevel.Info, "Backup");
                }

                await RestoreConfigurationAsync(tempDir);
                await RestoreProjectsAsync(tempDir);
                await RestoreLogsAsync(tempDir);

                Directory.Delete(tempDir, true);

                logManager.Log("Backup restored successfully", LogLevel.Info, "Backup");
                return true;
            }
            catch (Exception ex)
            {
                logManager.Log($"Error restoring backup: {ex.Message}", LogLevel.Error, "Backup");
                return false;
            }
        }

        private async Task BackupConfigurationAsync(string tempDir)
        {
            var configDir = Path.Combine(tempDir, "configuration");
            Directory.CreateDirectory(configDir);

            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "nodastack-config.json");
            if (File.Exists(configPath))
            {
                File.Copy(configPath, Path.Combine(configDir, "nodastack-config.json"));
            }
        }

        private async Task BackupProjectsAsync(string tempDir)
        {
            var projectsDir = Path.Combine(tempDir, "projects");
            var sourceProjectsDir = Path.Combine(Directory.GetCurrentDirectory(), "www");

            if (Directory.Exists(sourceProjectsDir))
            {
                CopyDirectory(sourceProjectsDir, projectsDir);
            }
        }

        private async Task BackupDatabaseAsync(string tempDir)
        {
            try
            {
                var dbDir = Path.Combine(tempDir, "database");
                Directory.CreateDirectory(dbDir);

                var dumpPath = Path.Combine(dbDir, "mysql_dump.sql");

                var psi = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "exec nodastack_mysql mysqldump --single-transaction --routines --triggers --all-databases",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = Process.Start(psi);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        await File.WriteAllTextAsync(dumpPath, output);
                        logManager.Log("Database backup completed", LogLevel.Info, "Backup");
                    }
                    else
                    {
                        logManager.Log("Database backup skipped (MySQL not running)", LogLevel.Warning, "Backup");
                    }
                }
            }
            catch (Exception ex)
            {
                logManager.Log($"Database backup failed: {ex.Message}", LogLevel.Warning, "Backup");
            }
        }

        private async Task BackupLogsAsync(string tempDir)
        {
            var logsDir = Path.Combine(tempDir, "logs");
            var sourceLogsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");

            if (Directory.Exists(sourceLogsDir))
            {
                CopyDirectory(sourceLogsDir, logsDir);
            }
        }

        private async Task RestoreConfigurationAsync(string tempDir)
        {
            var configDir = Path.Combine(tempDir, "configuration");
            var configFile = Path.Combine(configDir, "nodastack-config.json");

            if (File.Exists(configFile))
            {
                var targetPath = Path.Combine(Directory.GetCurrentDirectory(), "nodastack-config.json");
                File.Copy(configFile, targetPath, true);
                configManager.LoadConfiguration();
            }
        }

        private async Task RestoreProjectsAsync(string tempDir)
        {
            var projectsDir = Path.Combine(tempDir, "projects");
            var targetProjectsDir = Path.Combine(Directory.GetCurrentDirectory(), "www");

            if (Directory.Exists(projectsDir))
            {
                if (Directory.Exists(targetProjectsDir))
                {
                    Directory.Delete(targetProjectsDir, true);
                }
                CopyDirectory(projectsDir, targetProjectsDir);
            }
        }

        private async Task RestoreLogsAsync(string tempDir)
        {
            var logsDir = Path.Combine(tempDir, "logs");
            var targetLogsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");

            if (Directory.Exists(logsDir))
            {
                if (Directory.Exists(targetLogsDir))
                {
                    Directory.Delete(targetLogsDir, true);
                }
                CopyDirectory(logsDir, targetLogsDir);
            }
        }

        public List<BackupInfo> GetBackupList()
        {
            var backups = new List<BackupInfo>();

            try
            {
                var backupFiles = Directory.GetFiles(backupDirectory, "*.zip");

                foreach (var file in backupFiles)
                {
                    try
                    {
                        using var archive = ZipFile.OpenRead(file);
                        var infoEntry = archive.GetEntry("backup_info.json");

                        if (infoEntry != null)
                        {
                            using var stream = infoEntry.Open();
                            using var reader = new StreamReader(stream);
                            var json = reader.ReadToEnd();
                            var backupInfo = JsonSerializer.Deserialize<BackupInfo>(json);

                            if (backupInfo != null)
                            {
                                backupInfo.FilePath = file;
                                backupInfo.FileSize = new FileInfo(file).Length;
                                backups.Add(backupInfo);
                            }
                        }
                        else
                        {
                            var fileInfo = new FileInfo(file);
                            backups.Add(new BackupInfo
                            {
                                Name = Path.GetFileNameWithoutExtension(file),
                                CreatedAt = fileInfo.CreationTime,
                                Type = BackupType.Unknown,
                                FilePath = file,
                                FileSize = fileInfo.Length
                            });
                        }
                    }
                    catch
                    {
                        // Skip corrupted backup files
                    }
                }
            }
            catch (Exception ex)
            {
                logManager.Log($"Error loading backup list: {ex.Message}", LogLevel.Error, "Backup");
            }

            return backups;
        }

        public bool DeleteBackup(string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    logManager.Log($"Backup deleted: {Path.GetFileName(backupPath)}", LogLevel.Info, "Backup");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                logManager.Log($"Error deleting backup: {ex.Message}", LogLevel.Error, "Backup");
                return false;
            }
        }

        private void CopyDirectory(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(targetPath);

            foreach (var file in Directory.GetFiles(sourcePath))
            {
                var fileName = Path.GetFileName(file);
                var targetFile = Path.Combine(targetPath, fileName);
                File.Copy(file, targetFile, true);
            }

            foreach (var directory in Directory.GetDirectories(sourcePath))
            {
                var directoryName = Path.GetFileName(directory);
                var targetDirectory = Path.Combine(targetPath, directoryName);
                CopyDirectory(directory, targetDirectory);
            }
        }

        private long CalculateDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            long size = 0;
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                size += new FileInfo(file).Length;
            }
            return size;
        }
    }

    public class BackupInfo
    {
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public BackupType Type { get; set; }
        public long Size { get; set; }
        public string FilePath { get; set; } = "";
        public long FileSize { get; set; }
    }

    public enum BackupType
    {
        Full,
        Configuration,
        Projects,
        Database,
        Unknown
    }
}