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

namespace Jube.Engine.Helpers
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using BackgroundTasks.Context;
    using Dto.Hold;
    using Dto.Options;
    using EntityAnalysisModelInvoke.Helpers;
    using EntityAnalysisModelInvoke.Models;

    public static class DispatchNotificationHelpers
    {
        private static readonly HttpClient HttpClient = new();

        public static async Task DispatchNotificationAsync(Context context, Notification notification,
            CancellationToken token = default)
        {
            try
            {
                if (notification.NotificationTypeId == 1)
                {
                    var sent = await SendMail.SendAsync(
                        notification.NotificationDestination,
                        notification.NotificationSubject,
                        notification.NotificationBody,
                        context.Services.Log,
                        context.Services.DynamicEnvironment,
                        token
                    ).ConfigureAwait(false);

                    RecordLeg(context, notification, ResponseHoldLegKind.Email, sent);

                    if (context.Services.Log.IsInfoEnabled)
                    {
                        context.Services.Log.Info(
                            $"Notification Dispatch: Sent via email. Subject: {notification.NotificationSubject}, " +
                            $"Body: {notification.NotificationBody}, Destination: {notification.NotificationDestination}."
                        );
                    }

                    return;
                }

                var apiKey = context.Services.DynamicEnvironment.AppSettings("ClickatellAPIKey");

                var destination = HttpUtility.UrlEncode(notification.NotificationDestination?
                    .Replace("+", "").Replace(" ", ""));

                var message = HttpUtility.UrlEncode(notification.NotificationBody);

                var clickatellUrl =
                    $"https://platform.clickatell.com/messages/http/send?apiKey={apiKey}&to={destination}&content={message}";

                context.Services.Log.Info($"Notification Dispatch: Sending Clickatell message to {destination}.");

                try
                {
                    using var response = await HttpClient.GetAsync(clickatellUrl, token).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                    RecordLeg(context, notification, ResponseHoldLegKind.Sms, true);

                    if (context.Services.Log.IsInfoEnabled)
                    {
                        context.Services.Log.Info($"Clickatell response: {content}");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    RecordLeg(context, notification, ResponseHoldLegKind.Sms, false);

                    if (context.Services.Log.IsInfoEnabled)
                    {
                        context.Services.Log.Error(
                            $"Notification Dispatch: Failed to send Clickatell message to {destination}. Error: {ex}");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Error($"DispatchNotificationAsync: Unexpected error: {ex}");
                }
            }
        }

        private static void RecordLeg(Context context, Notification notification, ResponseHoldLegKind kind,
            bool sent)
        {
            if (!notification.EntityAnalysisModelInstanceEntryGuid.HasValue)
            {
                return;
            }

            HoldLegRecorder.Record(context, notification.EntityAnalysisModelInstanceEntryGuid.Value,
                new ResponseHoldLeg
                {
                    KindId = (int)kind,
                    OutcomeId = (int)(sent ? ResponseHoldLegOutcome.Sent : ResponseHoldLegOutcome.Failed),
                    Date = DateTime.UtcNow
                });
        }
    }
}