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

namespace Jube.Case
{
    using System.Text;
    using log4net;

    public static class SendHttpEndpoint
    {
        private static readonly HttpClient httpClient = new();

        private static string SafeHost(string httpEndpoint)
        {
            return Uri.TryCreate(httpEndpoint, UriKind.Absolute, out var uri) ? uri.Host : "unparseable-endpoint";
        }

        private static void AddCredential(HttpRequestMessage request, string? headerName, string? headerValue)
        {
            if (!string.IsNullOrEmpty(headerName) && !string.IsNullOrEmpty(headerValue))
            {
                request.Headers.TryAddWithoutValidation(headerName, headerValue);
            }
        }

        public static async Task<DispatchResult> PostAsync(string httpEndpoint, string body, ILog log,
            string? headerName = null, string? headerValue = null, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(httpEndpoint))
            {
                return DispatchResult.Failure("There was no endpoint to call.");
            }

            var correlationId = Guid.NewGuid();

            try
            {
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                content.Headers.Add("X-Correlation-Id", correlationId.ToString());
                using var request = new HttpRequestMessage(HttpMethod.Post, httpEndpoint) { Content = content };
                AddCredential(request, headerName, headerValue);
                using var response = await httpClient.SendAsync(request, token).ConfigureAwait(false);
                var status = (int)response.StatusCode;

                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                return DispatchResult.Success(status, correlationId);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Failed to dispatch to host {SafeHost(httpEndpoint)} for verb POST, correlationId={correlationId}: {ex.GetType().Name}: {ex.Message}");

                return DispatchResult.Failure($"{ex.GetType().Name}: {ex.Message}",
                    ex is HttpRequestException { StatusCode: { } code } ? (int)code : null, correlationId);
            }
        }

        public static async Task<DispatchResult> GetAsync(string httpEndpoint, ILog log,
            string? headerName = null, string? headerValue = null, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(httpEndpoint))
            {
                return DispatchResult.Failure("There was no endpoint to call.");
            }

            var correlationId = Guid.NewGuid();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, httpEndpoint);
                request.Headers.Add("X-Correlation-Id", correlationId.ToString());
                AddCredential(request, headerName, headerValue);
                using var response = await httpClient.SendAsync(request, token).ConfigureAwait(false);
                var status = (int)response.StatusCode;

                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                return DispatchResult.Success(status, correlationId);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Failed to dispatch to host {SafeHost(httpEndpoint)} for verb GET, correlationId={correlationId}: {ex.GetType().Name}: {ex.Message}");

                return DispatchResult.Failure($"{ex.GetType().Name}: {ex.Message}",
                    ex is HttpRequestException { StatusCode: { } code } ? (int)code : null, correlationId);
            }
        }
    }
}