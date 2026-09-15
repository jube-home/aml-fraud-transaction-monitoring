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

using System.Net.Sockets;
using System.Text;
using Jube.Data.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Monitoring
{
    public sealed class DockerApiClient : IDisposable
    {
        private readonly HttpClient httpClient;

        public DockerApiClient(string dockerSocketPath)
        {
            var handler = new SocketsHttpHandler
            {
                ConnectCallback = async (_, cancellationToken) =>
                {
                    var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    try
                    {
                        await socket.ConnectAsync(new UnixDomainSocketEndPoint(dockerSocketPath), cancellationToken)
                            .ConfigureAwait(false);
                        return new NetworkStream(socket, true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };

            httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost"),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public async Task<JArray?> GetContainersAsync()
        {
            var text = await GetStringAsync("/containers/json?all=true").ConfigureAwait(false);
            return text == null ? null : JArray.Parse(text);
        }

        public async Task<JObject?> GetContainerStatsAsync(string containerId)
        {
            var text = await GetStringAsync($"/containers/{containerId}/stats?stream=false").ConfigureAwait(false);
            return text == null ? null : JObject.Parse(text);
        }

        public async Task<JObject?> InspectContainerAsync(string containerId)
        {
            var text = await GetStringAsync($"/containers/{containerId}/json").ConfigureAwait(false);
            return text == null ? null : JObject.Parse(text);
        }

        public async Task<JObject?> GetInfoAsync()
        {
            var text = await GetStringAsync("/info").ConfigureAwait(false);
            return text == null ? null : JObject.Parse(text);
        }

        public async Task<bool> IsSwarmManagerAsync()
        {
            var info = await GetInfoAsync().ConfigureAwait(false);
            return (bool?)info?["Swarm"]?["ControlAvailable"] ?? false;
        }

        public async Task<JArray?> GetRunningTasksForServiceAsync(string serviceName)
        {
            var filters = JsonConvert.SerializeObject(new Dictionary<string, string[]>
            {
                ["service"] = [serviceName],
                ["desired-state"] = ["running"]
            });
            var text = await GetStringAsync($"/tasks?filters={Uri.EscapeDataString(filters)}").ConfigureAwait(false);
            return text == null ? null : JArray.Parse(text);
        }

        public async Task<JObject?> GetVersionAsync()
        {
            var text = await GetStringAsync("/version").ConfigureAwait(false);
            return text == null ? null : JObject.Parse(text);
        }

        public async Task<JObject?> GetSystemDfAsync()
        {
            var text = await GetStringAsync("/system/df").ConfigureAwait(false);
            return text == null ? null : JObject.Parse(text);
        }

        public async Task<List<(ContainerLogStreamType StreamType, string Line)>?> GetContainerLogsAsync(
            string containerId, string since)
        {
            byte[]? raw;
            try
            {
                using var response = await httpClient
                    .GetAsync($"/containers/{containerId}/logs?stdout=true&stderr=true&timestamps=true&since={since}")
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                raw = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                return null;
            }

            using var stdoutBytes = new MemoryStream();
            using var stderrBytes = new MemoryStream();

            var offset = 0;
            while (offset + 8 <= raw.Length)
            {
                var streamTypeByte = raw[offset];
                var length = (raw[offset + 4] << 24) | (raw[offset + 5] << 16) | (raw[offset + 6] << 8) |
                             raw[offset + 7];
                offset += 8;

                if (length < 0 || offset + length > raw.Length)
                {
                    break;
                }

                (streamTypeByte == 2 ? stderrBytes : stdoutBytes).Write(raw, offset, length);
                offset += length;
            }

            var results = new List<(ContainerLogStreamType StreamType, string Line)>();
            foreach (var line in Encoding.UTF8.GetString(stdoutBytes.ToArray())
                         .Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                results.Add((ContainerLogStreamType.Stdout, line));
            }

            foreach (var line in Encoding.UTF8.GetString(stderrBytes.ToArray())
                         .Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                results.Add((ContainerLogStreamType.Stderr, line));
            }

            return results;
        }

        public async Task<List<JObject>?> GetEventsAsync(string since, string until)
        {
            var text = await GetStringAsync($"/events?since={since}&until={until}").ConfigureAwait(false);
            if (text == null)
            {
                return null;
            }

            var results = new List<JObject>();
            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    results.Add(JObject.Parse(line));
                }
                catch (JsonException)
                {
                    //Ignored
                }
            }

            return results;
        }

        private async Task<string?> GetStringAsync(string path)
        {
            try
            {
                using var response = await httpClient.GetAsync(path).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}