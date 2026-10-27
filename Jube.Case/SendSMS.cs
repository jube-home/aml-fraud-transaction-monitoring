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
    using System.Web;
    using DynamicEnvironment;
    using log4net;

    public class SendSms(DynamicEnvironment dynamicEnvironment, ILog log)
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public async Task SendAsync(string notificationDestination, string notificationBody,
            CancellationToken token = default)
        {
            await TrySendAsync(notificationDestination, notificationBody, token).ConfigureAwait(false);
        }

        public async Task<DispatchResult> TrySendAsync(string notificationDestination, string notificationBody,
            CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(notificationDestination) || String.IsNullOrWhiteSpace(notificationBody))
            {
                return DispatchResult.Failure("There was no destination or message to send.");
            }

            var apiKey = dynamicEnvironment.AppSettings("ClickatellAPIKey");

            var sanitizedDestination = HttpUtility.UrlEncode(
                notificationDestination.Replace("+", "").Replace(" ", ""));

            var encodedBody = HttpUtility.UrlEncode(notificationBody);

            var clickatellUrl =
                $"https://platform.clickatell.com/messages/http/send?apiKey={apiKey}&to={sanitizedDestination}&content={encodedBody}";

            try
            {
                using var response = await httpClient.GetAsync(clickatellUrl, token).ConfigureAwait(false);
                var status = (int)response.StatusCode;

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info($"Notification Dispatch: Clickatell response: {content}");
                }

                return DispatchResult.Success(status);
            }
            catch (OperationCanceledException)
            {
                log.Warn("Notification Dispatch: send operation cancelled.");

                return DispatchResult.Failure("The send was cancelled.");
            }
            catch (Exception ex)
            {
                log.Error($"Notification Dispatch: failed to send the SMS request. Error: {ex.GetType().Name}: {ex.Message}");

                return DispatchResult.Failure($"{ex.GetType().Name}: {ex.Message}",
                    ex is HttpRequestException { StatusCode: { } code } ? (int)code : null);
            }
        }
    }
}
