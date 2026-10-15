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

using System.Net;
using System.Text;
using Jube.Engine.BackgroundTasks.TaskStarters.Metrics;
using Jube.Service.Etcd.Models;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Etcd
{
    public sealed class EtcdClient(string? username, string? password) : IDisposable
    {
        private readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private readonly EtcdMetricSampler sampler = new(username, password);
        private string? cachedToken;
        public string? LastError { get; private set; }

        public void Dispose()
        {
            httpClient.Dispose();
            sampler.Dispose();
        }

        public async Task<global::Jube.Data.Poco.EtcdMemberStatus?> GetClusterStatusAsync(string endpoint,
            CancellationToken token = default)
        {
            var (status, _) = await sampler.SampleAsync(endpoint, token).ConfigureAwait(false);
            LastError = sampler.LastError;

            return status;
        }

        public async Task<bool> ClearAlarmAsync(string endpoint, CancellationToken token = default)
        {
            LastError = null;
            var baseUrl = $"http://{endpoint}";

            var payload = await PostJsonAsync(baseUrl, $"{baseUrl}/v3/maintenance/alarm",
                "{\"action\":\"DEACTIVATE\"}", token).ConfigureAwait(false);

            return payload != null;
        }

        public async Task<bool> DefragAsync(string endpoint, CancellationToken token = default)
        {
            LastError = null;
            var baseUrl = $"http://{endpoint}";

            var payload = await PostJsonAsync(baseUrl, $"{baseUrl}/v3/maintenance/defragment", "{}", token)
                .ConfigureAwait(false);

            return payload != null;
        }

        public async Task<IReadOnlyList<EtcdMember>> MemberListAsync(string endpoint, CancellationToken token = default)
        {
            LastError = null;
            var baseUrl = $"http://{endpoint}";

            var payload = await PostJsonAsync(baseUrl, $"{baseUrl}/v3/cluster/member/list", "{}", token)
                .ConfigureAwait(false);

            if (payload?["members"] is not JArray members)
            {
                return [];
            }

            return members.Select(member => new EtcdMember(
                (string?)member["ID"] ?? string.Empty,
                (string?)member["name"],
                member["peerURLs"]?.Select(u => (string)u!).ToArray() ?? [],
                member["clientURLs"]?.Select(u => (string)u!).ToArray() ?? [],
                (bool?)member["isLearner"] ?? false)).ToList();
        }

        public async Task<EtcdMaintenanceStatus?> MaintenanceStatusAsync(string endpoint,
            CancellationToken token = default)
        {
            LastError = null;
            var baseUrl = $"http://{endpoint}";

            var payload = await PostJsonAsync(baseUrl, $"{baseUrl}/v3/maintenance/status", "{}", token)
                .ConfigureAwait(false);

            if (payload is null)
            {
                return null;
            }

            return new EtcdMaintenanceStatus(
                endpoint,
                (string?)payload["version"],
                (long?)payload["dbSize"],
                (long?)payload["dbSizeInUse"],
                (string?)payload["leader"],
                (long?)payload["raftIndex"],
                (long?)payload["raftTerm"],
                (long?)payload["raftAppliedIndex"],
                (bool?)payload["isLearner"]);
        }

        public async Task<EtcdKeyspaceHash?> HashKvAsync(string endpoint, CancellationToken token = default)
        {
            LastError = null;
            var baseUrl = $"http://{endpoint}";

            var payload = await PostJsonAsync(baseUrl, $"{baseUrl}/v3/maintenance/hashkv", "{\"revision\":0}", token)
                .ConfigureAwait(false);

            if (payload is null)
            {
                return null;
            }

            return new EtcdKeyspaceHash(endpoint, (long?)payload["hash"], (long?)payload["compact_revision"]);
        }

        private async Task<string> AuthenticateAsync(string baseUrl, CancellationToken token)
        {
            var body = $"{{\"name\":\"{username}\",\"password\":\"{password}\"}}";
            using var content = new StringContent(body, Encoding.UTF8, "application/json");

            using var response = await httpClient.PostAsync($"{baseUrl}/v3/auth/authenticate", content, token)
                .ConfigureAwait(false);

            var text = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (!EtcdApiResponse.TryRead(response.StatusCode, text, out var payload, out var error))
            {
                LastError ??= $"authenticating as {username} at {baseUrl} returned {error}";

                return null!;
            }

            return (string)payload["token"]!;
        }

        private async Task<JObject?> PostJsonAsync(string baseUrl, string url, string body,
            CancellationToken token, bool retryOnUnauthorized = true)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                if (!string.IsNullOrEmpty(username))
                {
                    cachedToken ??= await AuthenticateAsync(baseUrl, token).ConfigureAwait(false);

                    if (cachedToken != null)
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", cachedToken);
                    }
                }

                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                using var response = await httpClient.SendAsync(request, token).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Unauthorized && retryOnUnauthorized)
                {
                    cachedToken = null;

                    return await PostJsonAsync(baseUrl, url, body, token, retryOnUnauthorized: false)
                        .ConfigureAwait(false);
                }

                var text = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (EtcdApiResponse.TryRead(response.StatusCode, text, out var payload, out var error))
                {
                    return payload;
                }

                LastError ??= $"{url} returned {error}";

                return null;
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                return null;
            }
        }
    }
}