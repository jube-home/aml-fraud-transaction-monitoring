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
    using DynamicEnvironment;
    using log4net;

    public class Notification(ILog log, DynamicEnvironment dynamicEnvironment)
    {
        public async Task SendAsync(int notificationType, string notificationDestination, string notificationSubject,
            string notificationBody, CancellationToken token = default
        )
        {
            await TrySendAsync(notificationType, notificationDestination, notificationSubject, notificationBody, true,
                token).ConfigureAwait(false);
        }

        public async Task<DispatchResult> TrySendAsync(int notificationType, string notificationDestination,
            string notificationSubject, string notificationBody, bool decodeBody, CancellationToken token = default)
        {
            if (notificationType == 1)
            {
                var sendMail = new SendMail(dynamicEnvironment, log);

                return sendMail.TrySend(notificationDestination, notificationSubject, notificationBody, decodeBody);
            }

            var sendSms = new SendSms(dynamicEnvironment, log);

            return await sendSms.TrySendAsync(notificationDestination, notificationBody, token)
                .ConfigureAwait(false);
        }
    }
}
