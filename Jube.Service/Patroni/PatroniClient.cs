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

using Jube.Engine.BackgroundTasks.TaskStarters.Metrics;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Patroni
{
    public sealed class PatroniClient(string? etcdUsername, string? etcdPassword) : IDisposable
    {
        private readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private readonly PatroniMetricSampler sampler = new(etcdUsername, etcdPassword);
        public string? LastError { get; private set; }

        public void Dispose()
        {
            httpClient.Dispose();
            sampler.Dispose();
        }

        public async Task<IReadOnlyList<global::Jube.Data.Poco.PatroniMemberStatus>> GetClusterStatusAsync(
            IReadOnlyList<string> endpoints, CancellationToken token = default)
        {
            var (statuses, _) = await sampler.SampleAsync(endpoints, token).ConfigureAwait(false);
            LastError = sampler.LastError;

            return statuses;
        }

        public async Task<string?> GetConfigAsync(string endpoint, CancellationToken token = default)
        {
            try
            {
                LastError = null;

                var text = await httpClient.GetStringAsync($"http://{endpoint}/config", token)
                    .ConfigureAwait(false);

                JObject.Parse(text);

                return text;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LastError = $"{endpoint}/config returned {ex.Message}";

                return null;
            }
        }
    }
}