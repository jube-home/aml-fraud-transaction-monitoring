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

using System.Text.Json;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;

namespace Jube.Service.Query.EntityRiskRating
{
    public static class EntityRiskReviewJob
    {
        public const int WritePermission = 18;

        public static async Task<int> RunOnceAsync(DbContext dbContext, DateTime now,
            CancellationToken token = default)
        {
            var ratings = new EntityRiskRatingRepository(dbContext);
            var holders = new SanctionRescreenRepository(dbContext);
            var notifications = new UserNotificationRepository(dbContext);
            var notified = 0;

            foreach (var due in await ratings.CountDueByTenantAsync(now, token).ConfigureAwait(false))
            {
                var arguments = JsonSerializer.Serialize(new { count = due.Due });

                foreach (var user in await holders.ListPermissionHoldersAsync(due.TenantRegistryId, WritePermission,
                             token).ConfigureAwait(false))
                {
                    if (await notifications.InsertIfNewAsync(new UserNotification
                        {
                            Guid = Guid.NewGuid(),
                            TenantRegistryId = due.TenantRegistryId,
                            RecipientUser = user,
                            KindId = (short)NotificationKind.EntityRiskReviewDue,
                            PriorityId = (short)NotificationPriority.Normal,
                            ArgumentsJson = arguments,
                            DedupeKey = $"entity-risk-review:{due.TenantRegistryId}:{now:yyyyMMdd}",
                            CreatedDate = now
                        }, token).ConfigureAwait(false))
                    {
                        notified++;
                    }
                }
            }

            return notified;
        }
    }
}
