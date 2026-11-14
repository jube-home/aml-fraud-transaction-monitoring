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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Engine.Sanctions.Models;
using SanctionEntry = Jube.Engine.Sanctions.Models.SanctionEntry;

namespace Jube.Engine.Sanctions
{
    public sealed record RescreenCheck(int TenantRegistryId, int EntityAnalysisModelId, string Field, int Distance,
        double? MaxDistanceRatio, double? MaxCoverageRatio, byte MatchAlgorithmId = 1,
        double MinimumSimilarity = 0.9, bool Transliterate = false);

    public static class SanctionRescreen
    {
        public const int SanctionsPermission = 29;

        public static async Task<SanctionRescreenRun> RunAsync(DbContext dbContext,
            IReadOnlyCollection<RescreenCheck> checks, ConcurrentDictionary<int, SanctionEntry> allEntries,
            ISet<int> changedEntryIds, ConcurrentDictionary<string, byte> stopTokens, int sinceDays,
            int maximumNames, DateTime now, int upToImportId, CancellationToken token = default)
        {
            var repository = new SanctionRescreenRepository(dbContext);
            var run = await repository.InsertRunAsync(upToImportId, token).ConfigureAwait(false);

            var changed = new ConcurrentDictionary<int, SanctionEntry>();

            foreach (var id in changedEntryIds)
            {
                if (allEntries.TryGetValue(id, out var entry))
                {
                    changed.TryAdd(id, entry);
                }
            }

            run.EntriesConsidered = changed.Count;
            var hitsByTenant = new Dictionary<int, int>();

            if (!changed.IsEmpty)
            {
                foreach (var check in checks)
                {
                    token.ThrowIfCancellationRequested();
                    run.ChecksScreened++;

                    var names = await repository.ListScreenedNamesAsync(check.EntityAnalysisModelId, check.Field,
                        now.AddDays(-sinceDays), maximumNames, token).ConfigureAwait(false);

                    var matcher = new LevenshteinDistance(check.MaxDistanceRatio, check.MaxCoverageRatio,
                        new SanctionMatchOptions(check.MatchAlgorithmId, check.MinimumSimilarity, check.Transliterate));

                    foreach (var name in names)
                    {
                        run.NamesScreened++;

                        foreach (var match in matcher.CheckMultipartString(name, check.Distance, changed, stopTokens))
                        {
                            if (SanctionExemptionSet.Shared.IsExempt(check.TenantRegistryId, name,
                                    match.SanctionEntry.SanctionEntrySourceId, match.SanctionEntry.SanctionEntryReference,
                                    now))
                            {
                                continue;
                            }

                            var inserted = await repository.InsertHitIfNewAsync(new SanctionRescreenHit
                            {
                                RunId = run.Id,
                                TenantRegistryId = check.TenantRegistryId,
                                EntityAnalysisModelId = check.EntityAnalysisModelId,
                                ScreenedName = Truncate(name, 500),
                                NameKey = Truncate(SanctionExemptionSet.NameKey(name), 500),
                                SanctionEntryId = match.SanctionEntry.SanctionEntryId,
                                SanctionEntrySourceId = match.SanctionEntry.SanctionEntrySourceId,
                                SanctionEntryReference = Truncate(match.SanctionEntry.SanctionEntryReference, 500),
                                ListedName = Truncate(string.Join(' ', match.SanctionEntry.SanctionElementValue), 500),
                                Distance = match.LevenshteinDistance,
                                CreatedDate = now
                            }, token).ConfigureAwait(false);

                            if (inserted)
                            {
                                run.Hits++;
                                hitsByTenant[check.TenantRegistryId] =
                                    hitsByTenant.GetValueOrDefault(check.TenantRegistryId) + 1;
                            }
                        }
                    }
                }
            }

            await repository.CompleteRunAsync(run, token).ConfigureAwait(false);
            await NotifyAsync(dbContext, repository, run, hitsByTenant, now, token).ConfigureAwait(false);

            return run;
        }

        private static async Task NotifyAsync(DbContext dbContext, SanctionRescreenRepository repository,
            SanctionRescreenRun run, Dictionary<int, int> hitsByTenant, DateTime now, CancellationToken token)
        {
            var notifications = new UserNotificationRepository(dbContext);

            foreach (var (tenant, count) in hitsByTenant)
            {
                foreach (var user in await repository.ListPermissionHoldersAsync(tenant, SanctionsPermission, token)
                             .ConfigureAwait(false))
                {
                    await notifications.InsertIfNewAsync(new UserNotification
                    {
                        Guid = Guid.NewGuid(),
                        TenantRegistryId = tenant,
                        RecipientUser = user,
                        KindId = (short)NotificationKind.SanctionRescreenHits,
                        PriorityId = (short)NotificationPriority.High,
                        ArgumentsJson = $"{{\"count\":{count},\"run\":{run.Id}}}",
                        DedupeKey = $"sanction-rescreen:{run.Id}",
                        CreatedDate = now
                    }, token).ConfigureAwait(false);
                }
            }
        }

        private static string Truncate(string value, int length)
        {
            return value is null ? string.Empty : value.Length <= length ? value : value[..length];
        }
    }
}
