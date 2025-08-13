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

using System.Collections.Generic;
using log4net;

namespace Jube.App.Code
{
    public class Notification
    {
        private readonly DynamicEnvironment.DynamicEnvironment _dynamicEnvironment;
        private readonly ILog _log;

        public Notification(ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            _dynamicEnvironment = dynamicEnvironment;
            _log = log;
        }

        public void Send(int notificationType, string notificationDestination, string notificationSubject,
            string notificationBody, Dictionary<string, string> values
        )
        {
            var notificationTokenization = new Tokenisation();

            var replacedNotificationDestination = notificationDestination;
            if (!string.IsNullOrEmpty(replacedNotificationDestination))
            {
                var notificationDestinationTokens =
                    notificationTokenization.ReturnTokens(replacedNotificationDestination);
                foreach (var notificationToken in notificationDestinationTokens)
                    if (values.TryGetValue(notificationToken, out var value))
                    {
                        var notificationReplaceToken = $"[@{notificationToken}@]";
                        replacedNotificationDestination =
                            replacedNotificationDestination.Replace(notificationReplaceToken,
                                value);
                    }
            }

            var replacedNotificationSubject = notificationSubject;
            if (!string.IsNullOrEmpty(replacedNotificationSubject))
            {
                var notificationSubjectTokens = notificationTokenization.ReturnTokens(replacedNotificationSubject);
                foreach (var notificationToken in notificationSubjectTokens)
                    if (values.TryGetValue(notificationToken, out var value))
                    {
                        var notificationReplaceToken = $"[@{notificationToken}@]";
                        replacedNotificationSubject =
                            replacedNotificationSubject.Replace(notificationReplaceToken, value);
                    }
            }

            var replacedNotificationBody = notificationBody;
            if (!string.IsNullOrEmpty(replacedNotificationBody))
            {
                var notificationBodyTokens = notificationTokenization.ReturnTokens(replacedNotificationBody);
                foreach (var notificationToken in notificationBodyTokens)
                    if (values.TryGetValue(notificationToken, out var value))
                    {
                        var notificationReplaceToken = $"[@{notificationToken}@]";
                        replacedNotificationBody =
                            replacedNotificationBody.Replace(notificationReplaceToken, value);
                    }
            }

            if (notificationType == 1)
            {
                var sendMail = new SendMail(_dynamicEnvironment, _log);
                sendMail.Send(replacedNotificationDestination, replacedNotificationSubject,
                    replacedNotificationBody);
            }
            else
            {
                var sendSms = new SendSms(_dynamicEnvironment, _log);
                sendSms.Send(replacedNotificationDestination, replacedNotificationBody);
            }
        }
    }
}