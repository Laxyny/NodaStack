using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace NodaStack
{
    public class UpdateInstaller
    {
        private readonly string _tempDir;
        private readonly LogManager _logManager;

        public UpdateInstaller(LogManager logManager)
        {
            _logManager = logManager;
            _tempDir = Path.Combine(Path.GetTempPath(), "NodaStackUpdates");

            if (!Directory.Exists(_tempDir))
            {
                Directory.CreateDirectory(_tempDir);
            }
        }

        public async Task<bool> DownloadAndInstallUpdate(string downloadUrl)
        {
            try
            {
                string installerName = "NodaStack-Setup.exe";
                string installerPath = Path.Combine(_tempDir, installerName);

                if (File.Exists(installerPath))
                {
                    File.Delete(installerPath);
                }

                _logManager?.Log($"Downloading update from {downloadUrl}", LogLevel.Info, "Updates");

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                    {
                        NoCache = true
                    };

                    using (var response = await client.GetAsync(downloadUrl))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                    }
                }

                _logManager?.Log("Update downloaded successfully", LogLevel.Info, "Updates");

                bool confirmInstall = MessageBox.Show(
                    "NodaStack update has been downloaded. Install now? (The application will close during installation)",
                    "Update Ready",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes;

                if (confirmInstall)
                {
                    _logManager?.Log("Starting update installation", LogLevel.Info, "Updates");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = installerPath,
                        UseShellExecute = true
                    });

                    Application.Current.Shutdown();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logManager?.Log($"Update installation failed: {ex.Message}", LogLevel.Error, "Updates");
                MessageBox.Show(
                    $"Failed to download or install update: {ex.Message}",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }
    }
}