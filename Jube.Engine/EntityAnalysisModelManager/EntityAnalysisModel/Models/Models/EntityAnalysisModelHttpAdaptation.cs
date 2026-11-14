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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Jube.ResilientHttp;
    using Jube.ResilientHttp.Extensions;
    using log4net;
    using Newtonsoft.Json;
    using EntityAnalysisModelInstanceEntryPayload =
        EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.
        EntityAnalysisModelInstanceEntryPayload;

    public class EntityAnalysisModelHttpAdaptation
    {
        public const long MaximumResponseBytes = 4 * 1024 * 1024;

        private readonly HttpClient httpClient;
        private Uri uri;

        public EntityAnalysisModelHttpAdaptation(int maxConnections, bool validateSsl, int timeout,
            bool allowInsecureTargets = true, int? tenantRegistryId = null,
            AdaptationHttpClientCache clientCache = null)
        {
            httpClient = tenantRegistryId.HasValue
                ? (clientCache ?? AdaptationHttpClientCache.Shared).Acquire(tenantRegistryId.Value, maxConnections,
                    validateSsl, timeout, allowInsecureTargets)
                : AdaptationHttpClientCache.Create(maxConnections, validateSsl, timeout, allowInsecureTargets);
        }

        public int Id { get; init; }
        public string Name { get; set; }
        public bool ResponsePayload { get; set; }
        public bool ReportTable { get; set; }
        public double Priority { get; set; }
        public string BreakerKey { get; set; }
        public HttpResilienceOptions Resilience { get; set; }

        public string HttpEndpoint
        {
            get { return uri.ToString(); }
            set { uri = new Uri(value); }
        }

        public static bool IsPlainPath(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint) || endpoint[0] != '/')
            {
                return false;
            }

            if (endpoint.Any(c => char.IsControl(c) || char.IsWhiteSpace(c) || c == '\\'))
            {
                return false;
            }

            var decoded = Uri.UnescapeDataString(endpoint.Split('?', '#')[0]);

            if (decoded.Any(c => char.IsControl(c) || c == '\\'))
            {
                return false;
            }

            return !decoded.Split('/').Any(segment => segment is "." or "..");
        }

        public async Task<string> PostAsync(EntityAnalysisModelInstanceEntryPayload payload,
            JsonSerializerSettings jsonSerializerSettings, ILog log)
        {
            if (log.IsInfoEnabled)
            {
                log.Info($"R Plumber Hook: Is about to send to {uri}.");
            }

            var stringContent = new StringContent(
                JsonConvert.SerializeObject(payload, jsonSerializerSettings),
                Encoding.UTF8,
                "application/json");

            var valueString = Resilience == null
                ? await SendAsync(stringContent, log, CancellationToken.None).ConfigureAwait(false)
                : await ResilientHttpExecutor.Shared.ExecuteAsync(BreakerKey ?? HttpEndpoint, Resilience,
                    ct => SendAsync(stringContent, log, ct)).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"R Plumber Hook: Has received {valueString.Length} characters from {uri}. This is returned unparsed, as the HTTP Adaptation Protocol response shape (bare number or Adaptation object) is decided by the caller.");
            }

            return valueString;
        }

        private async Task<string> SendAsync(StringContent stringContent, ILog log, CancellationToken token)
        {
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Content = stringContent
            };

            using var task = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, token)
                .ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"R Plumber Hook: Has received data from {uri} with status {task.StatusCode}.");
            }

            task.ThrowIfUnsuccessful();

            return await task.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        }
    }
}
