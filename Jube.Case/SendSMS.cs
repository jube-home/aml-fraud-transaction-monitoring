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
            if (String.IsNullOrWhiteSpace(notificationDestination) || String.IsNullOrWhiteSpace(notificationBody))
            {
                return;
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
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info($"Notification Dispatch: Clickatell response: {content}");
                }
            }
            catch (OperationCanceledException)
            {
                log.Warn("Notification Dispatch: send operation cancelled.");
            }
            catch (Exception ex)
            {
                log.Error($"Notification Dispatch: failed to send Clickatell request to {clickatellUrl}. Error: {ex}");
            }
        }
    }
}