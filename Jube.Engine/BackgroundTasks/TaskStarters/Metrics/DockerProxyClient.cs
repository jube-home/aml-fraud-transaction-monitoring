/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public sealed class DockerProxyClient(HttpClient httpClient) : IDisposable
    {
        public void Dispose()
        {
            httpClient.Dispose();
        }

        public Task<JArray> GetContainersAsync(CancellationToken token = default)
        {
            return GetJArrayAsync("/containers/json?all=true", token);
        }

        public Task<JObject> GetContainerStatsAsync(string containerId, CancellationToken token = default)
        {
            return GetJObjectAsync($"/containers/{containerId}/stats?stream=false", token);
        }

        public Task<JObject> InspectContainerAsync(string containerId, CancellationToken token = default)
        {
            return GetJObjectAsync($"/containers/{containerId}/json", token);
        }

        public Task<JObject> GetInfoAsync(CancellationToken token = default)
        {
            return GetJObjectAsync("/info", token);
        }

        public Task<JObject> GetVersionAsync(CancellationToken token = default)
        {
            return GetJObjectAsync("/version", token);
        }

        public Task<JObject> GetSystemDfAsync(CancellationToken token = default)
        {
            return GetJObjectAsync("/system/df", token);
        }

        public async Task<List<JObject>> GetEventsAsync(string since, string until,
            CancellationToken token = default)
        {
            var text = await GetStringAsync($"/events?since={since}&until={until}", token).ConfigureAwait(false);

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
                catch (Newtonsoft.Json.JsonException)
                {
                }
            }

            return results;
        }

        private async Task<JObject> GetJObjectAsync(string dockerPath, CancellationToken token)
        {
            var text = await GetStringAsync(dockerPath, token).ConfigureAwait(false);

            return text == null ? null : JObject.Parse(text);
        }

        private async Task<JArray> GetJArrayAsync(string dockerPath, CancellationToken token)
        {
            var text = await GetStringAsync(dockerPath, token).ConfigureAwait(false);

            return text == null ? null : JArray.Parse(text);
        }

        private async Task<string> GetStringAsync(string dockerPath, CancellationToken token)
        {
            try
            {
                using var response = await httpClient
                    .GetAsync($"/proxy?path={Uri.EscapeDataString(dockerPath)}", token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                return null;
            }
        }
    }
}