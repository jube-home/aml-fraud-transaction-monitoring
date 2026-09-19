/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Jube.Data.Poco;

namespace Jube.Monitoring
{
    public sealed class HaProxyReachabilityProbeSampler(HttpClient httpClient, string haProxyTasksHostname)
    {
        private static readonly TimeSpan tcpConnectTimeout = TimeSpan.FromSeconds(5);

        public async Task<List<HaProxyReachabilityProbe>> SampleAsync()
        {
            var results = new List<HaProxyReachabilityProbe>();

            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(haProxyTasksHostname).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return results;
            }

            foreach (var address in addresses)
            {
                results.Add(await ProbeHttpAsync(address, 5001, "JubeUi").ConfigureAwait(false));
                results.Add(await ProbeHttpAsync(address, 5002, "JubeApi").ConfigureAwait(false));
                results.Add(await ProbeTcpAsync(address, 5432, "PostgresPrimary").ConfigureAwait(false));
                results.Add(await ProbeTcpAsync(address, 5433, "PostgresReplica").ConfigureAwait(false));
            }

            return results;
        }

        private async Task<HaProxyReachabilityProbe> ProbeHttpAsync(IPAddress address, int port, string target)
        {
            var haProxyAddress = $"{address}:{port}";
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var response = await httpClient
                    .GetAsync($"http://{FormatHost(address)}:{port}/api/ready").ConfigureAwait(false);
                stopwatch.Stop();

                return new HaProxyReachabilityProbe
                {
                    Target = target,
                    HaProxyAddress = haProxyAddress,
                    Success = response.IsSuccessStatusCode,
                    ConnectMicroseconds = ToMicroseconds(stopwatch),
                    HttpStatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                return new HaProxyReachabilityProbe
                {
                    Target = target,
                    HaProxyAddress = haProxyAddress,
                    Success = false,
                    ConnectMicroseconds = ToMicroseconds(stopwatch),
                    ErrorMessage = Truncate(ex.Message)
                };
            }
        }

        private static async Task<HaProxyReachabilityProbe> ProbeTcpAsync(IPAddress address, int port,
            string target)
        {
            var haProxyAddress = $"{address}:{port}";
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(address, port);
                var timeoutTask = Task.Delay(tcpConnectTimeout);
                if (await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false) == timeoutTask)
                {
                    throw new TimeoutException($"Connect to {haProxyAddress} timed out after {tcpConnectTimeout}.");
                }

                await connectTask.ConfigureAwait(false);
                stopwatch.Stop();

                return new HaProxyReachabilityProbe
                {
                    Target = target,
                    HaProxyAddress = haProxyAddress,
                    Success = true,
                    ConnectMicroseconds = ToMicroseconds(stopwatch)
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                return new HaProxyReachabilityProbe
                {
                    Target = target,
                    HaProxyAddress = haProxyAddress,
                    Success = false,
                    ConnectMicroseconds = ToMicroseconds(stopwatch),
                    ErrorMessage = Truncate(ex.Message)
                };
            }
        }

        private static long ToMicroseconds(Stopwatch stopwatch)
        {
            return (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
        }

        private static string Truncate(string message)
        {
            return message.Length > 1024 ? message[..1024] : message;
        }

        private static string FormatHost(IPAddress address)
        {
            return address.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{address}]" : address.ToString();
        }
    }
}