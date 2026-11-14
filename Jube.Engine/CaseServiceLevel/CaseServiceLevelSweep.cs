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

namespace Jube.Engine.CaseServiceLevel
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto.Options;

    public static class CaseServiceLevelSweep
    {
        public const int SupervisionPermission = 18;

        private static readonly short[] dimensions =
        [
            CaseServiceLevelBreachRepository.FirstView,
            CaseServiceLevelBreachRepository.Ownership,
            CaseServiceLevelBreachRepository.Resolution,
            CaseServiceLevelBreachRepository.StatusDwell
        ];

        public static async Task<int> RunAsync(DbContext dbContext, int batchSize, CancellationToken token)
        {
            var breaches = new CaseServiceLevelBreachRepository(dbContext);
            var notifications = new UserNotificationRepository(dbContext);
            var recorded = 0;

            foreach (var dimension in dimensions)
            {
                var candidates = await breaches.FindUnrecordedAsync(dimension, batchSize, token)
                    .ConfigureAwait(false);

                foreach (var candidate in candidates)
                {
                    token.ThrowIfCancellationRequested();

                    if (!await breaches.RecordAsync(candidate.CaseId, dimension, token).ConfigureAwait(false))
                    {
                        continue;
                    }

                    recorded++;

                    var recipients = new HashSet<string>(StringComparer.Ordinal);

                    if (!string.IsNullOrWhiteSpace(candidate.LockedUser))
                    {
                        recipients.Add(candidate.LockedUser);
                    }

                    foreach (var supervisor in await breaches.ListSupervisorsAsync(candidate.TenantRegistryId,
                                     candidate.CaseWorkflowGuid, SupervisionPermission, token)
                                 .ConfigureAwait(false))
                    {
                        recipients.Add(supervisor);
                    }

                    foreach (var recipient in recipients)
                    {
                        await notifications.InsertIfNewAsync(new UserNotification
                        {
                            Guid = Guid.NewGuid(),
                            TenantRegistryId = candidate.TenantRegistryId,
                            RecipientUser = recipient,
                            KindId = (short)NotificationKind.CaseServiceLevelBreached,
                            PriorityId = (short)(dimension == CaseServiceLevelBreachRepository.Resolution
                                ? NotificationPriority.High
                                : NotificationPriority.Normal),
                            CaseId = candidate.CaseId,
                            ArgumentsJson = $"{{\"dimension\":{dimension}}}",
                            DedupeKey = $"case-sla:{candidate.CaseId}:{dimension}",
                            CreatedDate = DateTime.UtcNow
                        }, token).ConfigureAwait(false);
                    }
                }
            }

            return recorded;
        }
    }
}
