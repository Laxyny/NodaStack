using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NodaStack.Services
{
    public class TunnelService
    {
        private readonly string _authToken;
        private readonly LogManager? _logManager;
        private const string ContainerName = "nodastack_ngrok";

        public TunnelService(string authToken, LogManager? logManager = null)
        {
            _authToken = authToken;
            _logManager = logManager;
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Debug.WriteLine($"TunnelService: {message}");
            _logManager?.Log(message, level, "Ngrok");
        }

        public async Task<string> StartTunnelAsync(string name, int port, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                Log("No ngrok authentication token configured", LogLevel.Error);
                throw new InvalidOperationException("Ngrok authentication token required. Please configure your token in the settings.");
            }

            Log($"Starting a tunnel for {name} on port {port}", LogLevel.Info);

            try
            {
                Log("Removing old ngrok containers", LogLevel.Info);
                await Process
                    .Start(new ProcessStartInfo("docker", $"rm -f {ContainerName}")
                    {
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })!
                    .WaitForExitAsync(ct);

                Log("Checking ngrok image", LogLevel.Info);
                var pullProcess = Process.Start(new ProcessStartInfo("docker", "pull ngrok/ngrok")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                await pullProcess!.WaitForExitAsync(ct);

                Log("Starting ngrok container", LogLevel.Info);
                var runArgs = $"run -d --rm --name {ContainerName} -p 4040:4040 " +
                  "--add-host=host.docker.internal:host-gateway " +
                  $"-e NGROK_AUTHTOKEN={_authToken} ngrok/ngrok http " +
                  $"--region=eu --host-header=rewrite host.docker.internal:{port}";
                using var runProc = Process.Start(new ProcessStartInfo("docker", runArgs)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (runProc == null)
                {
                    Log("Unable to start docker process", LogLevel.Error);
                    throw new InvalidOperationException("Failed to start ngrok container.");
                }

                var containerId = (await runProc.StandardOutput.ReadLineAsync())?.Trim();
                await runProc.WaitForExitAsync(ct);
                if (runProc.ExitCode != 0 || string.IsNullOrEmpty(containerId))
                {
                    var err = await runProc.StandardError.ReadToEndAsync();
                    Log($"docker run failed: {err.Trim()}", LogLevel.Error);
                    throw new InvalidOperationException("docker run failed: " + err.Trim());
                }

                Log($"ngrok container started with ID: {containerId}", LogLevel.Info);

                using var psProc = Process.Start(new ProcessStartInfo("docker", $"ps --filter name={ContainerName} --format \"{{{{.Names}}}}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                })!;
                var runningName = (await psProc.StandardOutput.ReadToEndAsync()).Trim();
                await psProc.WaitForExitAsync(ct);
                if (string.Compare(runningName, ContainerName, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    Log("ngrok container is not running after creation", LogLevel.Error);
                    throw new InvalidOperationException("ngrok container not running after creation.");
                }

                Log("Waiting for ngrok API initialization", LogLevel.Info);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);

                const int maxAttempts = 8;
                var apiUrl = "http://127.0.0.1:4040/api/tunnels";
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    try
                    {
                        Log($"Attempting to reach ngrok API ({attempt + 1}/{maxAttempts})", LogLevel.Info);
                        var json = await client.GetStringAsync(apiUrl, ct);

                        Log("API response received, parsing JSON", LogLevel.Info);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("tunnels", out var tunnels) &&
                            tunnels.ValueKind == JsonValueKind.Array &&
                            tunnels.GetArrayLength() > 0)
                        {
                            var first = tunnels[0];
                            if (first.TryGetProperty("public_url", out var urlProp) &&
                                !string.IsNullOrEmpty(urlProp.GetString()))
                            {
                                string url = urlProp.GetString()!;
                                Log($"Tunnel created successfully: {url}", LogLevel.Info);
                                return url;
                            }
                            Log("Missing or empty public_url in response", LogLevel.Error);
                            throw new InvalidOperationException("Missing or empty public_url.");
                        }

                        Log("No tunnel available in ngrok response", LogLevel.Error);
                        throw new InvalidOperationException("No tunnel available in ngrok response.");
                    }
                    catch (Exception ex) when (attempt + 1 < maxAttempts)
                    {
                        Log($"Attempt #{attempt + 1} failed: {ex.Message}", LogLevel.Warning);
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)), ct);
                    }
                }

                Log($"Unable to reach ngrok API after {maxAttempts} attempts", LogLevel.Error);
                throw new InvalidOperationException($"Unable to reach ngrok API after {maxAttempts} attempts.");
            }
            catch (Exception ex)
            {
                Log($"Error while creating tunnel: {ex.Message}", LogLevel.Error);
                StopTunnel();
                throw;
            }
        }

        public void StopTunnel()
        {
            try
            {
                Log("Stopping ngrok tunnel", LogLevel.Info);
                Process.Start(new ProcessStartInfo("docker", $"stop {ContainerName}")
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                Log($"Error while stopping tunnel: {ex.Message}", LogLevel.Warning);
            }
        }

        public async Task<bool> TestNgrokAuthTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var testProcess = Process.Start(new ProcessStartInfo("docker", $"run --rm -e NGROK_AUTHTOKEN={token} ngrok/ngrok config check")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (testProcess == null)
                    return false;

                await testProcess.WaitForExitAsync();
                var output = await testProcess.StandardOutput.ReadToEndAsync();
                var error = await testProcess.StandardError.ReadToEndAsync();

                return testProcess.ExitCode == 0 && !error.Contains("invalid token");
            }
            catch
            {
                return false;
            }
        }
    }
}