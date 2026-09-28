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
        private static string SafeHost(string httpEndpoint)
        {
            return Uri.TryCreate(httpEndpoint, UriKind.Absolute, out var uri) ? uri.Host : "unparseable-endpoint";
        }

        public static async Task PostAsync(string httpEndpoint, string body, ILog log)
        {
            if (String.IsNullOrEmpty(httpEndpoint))
            {
                return;
            }

            using var client = new HttpClient();
            var correlationId = Guid.NewGuid();

            try
            {
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                content.Headers.Add("X-Correlation-Id", correlationId.ToString());
                using var response = await client.PostAsync(httpEndpoint, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Failed to dispatch to host {SafeHost(httpEndpoint)} for verb POST, correlationId={correlationId}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        public static async Task GetAsync(string httpEndpoint, ILog log)
        {
            if (String.IsNullOrEmpty(httpEndpoint))
            {
                return;
            }

            using var client = new HttpClient();
            var correlationId = Guid.NewGuid();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, httpEndpoint);
                request.Headers.Add("X-Correlation-Id", correlationId.ToString());
                using var response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Failed to dispatch to host {SafeHost(httpEndpoint)} for verb GET, correlationId={correlationId}: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}