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

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Engine.AdvancedAnalytics.Regression;
using log4net;
using Jube.Engine.AdvancedAnalytics.RScript.Models;

namespace Jube.Engine.AdvancedAnalytics.RScript
{
    public class RServerClient : IDisposable
    {
        public const string HealthPath = "/health";
        public const string PackagesPath = "/packages";
        public const string ValidatePath = "/validate";
        public const string ExecutePath = "/execute";
        private readonly Uri baseUri;
        private readonly bool ownsHttpClient;
        private readonly HttpClient httpClient;
        private bool disposed;

        public RServerClient(HttpClient httpClient, string baseUrl)
        {
            this.httpClient = httpClient;
            ownsHttpClient = false;
            baseUri = new Uri(baseUrl, UriKind.Absolute);
        }

        public RServerClient(string baseUrl, int maxConnections, bool validateSsl, int timeoutMilliseconds)
        {
            var httpClientHandler = new HttpClientHandler
            {
                MaxConnectionsPerServer = maxConnections
            };

            if (!validateSsl)
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

            httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds)
            };

            ownsHttpClient = true;
            baseUri = new Uri(baseUrl, UriKind.Absolute);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (ownsHttpClient)
            {
                httpClient.Dispose();
            }
        }

        public static RServerClient FromSettings(Func<string, string> appSettings)
        {
            var url = appSettings("RServerUrl");

            var maxConnections = int.TryParse(appSettings("RServerMaxConnections"), out var parsedConnections)
                ? parsedConnections
                : 10;

            var validateSsl = !string.Equals(appSettings("RServerValidateSsl"), "False",
                StringComparison.OrdinalIgnoreCase);

            var timeout = int.TryParse(appSettings("RServerHttpTimeout"), out var parsedTimeout)
                ? parsedTimeout
                : 330000;

            return new RServerClient(url, maxConnections, validateSsl, timeout);
        }

        public Task<RServerHealthResult> HealthAsync(ILog log = null, CancellationToken token = default)
        {
            return GetAsync<RServerHealthResult>(HealthPath, log, token);
        }

        public Task<RServerPackagesResult> PackagesAsync(ILog log = null, CancellationToken token = default)
        {
            return GetAsync<RServerPackagesResult>(PackagesPath, log, token);
        }

        public Task<RServerValidateResult> ValidateAsync(string script, ILog log = null,
            CancellationToken token = default)
        {
            return PostAsync<RServerValidateResult>(ValidatePath, new RServerValidateRequest(script), log, token);
        }

        public Task<RServerExecuteResult> ExecuteAsync(string script, double? timeoutSeconds,
            string userName = null, IReadOnlyList<RServerDataframeRequest> dataframes = null,
            IReadOnlyList<RServerVariableRequest> variables = null, ILog log = null,
            CancellationToken token = default)
        {
            return PostAsync<RServerExecuteResult>(ExecutePath,
                new RServerExecuteRequest(script, timeoutSeconds, userName, dataframes, variables), log,
                token);
        }

        private async Task<T> GetAsync<T>(string path, ILog log, CancellationToken token)
        {
            var uri = new Uri(baseUri, path);

            if (log is { IsInfoEnabled: true })
            {
                log.Info($"R Server Client: is about to call {uri}.");
            }

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = uri
            };

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, token)
                .ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (log is { IsInfoEnabled: true })
            {
                log.Info($"R Server Client: has received a response from {uri} with status {response.StatusCode}.");
            }

            return AdvancedAnalyticsEvidenceJson.Deserialise<T>(body)!;
        }

        private async Task<T> PostAsync<T>(string path, object payload, ILog log, CancellationToken token)
        {
            var uri = new Uri(baseUri, path);

            if (log is { IsInfoEnabled: true })
            {
                log.Info($"R Server Client: is about to call {uri}.");
            }

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Content = new StringContent(AdvancedAnalyticsEvidenceJson.Serialise(payload), Encoding.UTF8,
                    "application/json")
            };

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, token)
                .ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (log is { IsInfoEnabled: true })
            {
                log.Info($"R Server Client: has received a response from {uri} with status {response.StatusCode}.");
            }

            return AdvancedAnalyticsEvidenceJson.Deserialise<T>(body)!;
        }

        // ReSharper disable ClassNeverInstantiated.Global
        // ReSharper disable ClassNeverInstantiated.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        // ReSharper disable NotAccessedPositionalProperty.Global
        // ReSharper disable NotAccessedPositionalProperty.Local
        // ReSharper disable RedundantAttributeUsageProperty
        private sealed record RServerValidateRequest(string Script);

        private sealed record RServerExecuteRequest(
            string Script,
            double? TimeoutSeconds,
            string UserName,
            IReadOnlyList<RServerDataframeRequest> Dataframes,
            IReadOnlyList<RServerVariableRequest> Variables);
    }
}