using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NodaStack.Helpers
{
    public static class PortChecker
    {
        public static bool IsPortAvailable(int port)
        {
            try
            {
                var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

                foreach (var tcpInfo in tcpConnInfoArray)
                {
                    if (tcpInfo.Port == port)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<Dictionary<int, bool>> CheckPortsAsync(IEnumerable<int> ports)
        {
            var results = new Dictionary<int, bool>();

            await Task.Run(() =>
            {
                foreach (var port in ports)
                {
                    results[port] = IsPortAvailable(port);
                }
            });

            return results;
        }

        public static List<int> GetAvailablePorts(int startPort = 8000, int endPort = 9000)
        {
            var availablePorts = new List<int>();

            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortAvailable(port))
                {
                    availablePorts.Add(port);
                }
            }

            return availablePorts;
        }

        public static Dictionary<string, int> GetServicePorts()
        {
            return new Dictionary<string, int>
            {
                { "Apache", 8080 },
                { "PHP", 8000 },
                { "MySQL", 3306 },
                { "phpMyAdmin", 8081 }
            };
        }
    }
}