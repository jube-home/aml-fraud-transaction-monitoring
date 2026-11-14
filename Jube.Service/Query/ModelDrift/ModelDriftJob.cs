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

using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;

namespace Jube.Service.Query.ModelDrift
{
    public sealed record ModelDriftOptions(int RecentDays = 7, int BaselineDays = 28, int MinimumSamples = 200,
        int MaximumSamples = 50000, int Bins = 10);

    public sealed record ModelDriftResult(int Checked, int Significant, int Notified);

    public static class ModelDriftJob
    {
        public const int ModelPermission = 6;

        public static async Task<ModelDriftResult> RunOnceAsync(DbContext dbContext, DateTime now,
            ModelDriftOptions options, CancellationToken token = default)
        {
            var repository = new ModelDriftRepository(dbContext);
            var rescreen = new SanctionRescreenRepository(dbContext);
            var notifications = new UserNotificationRepository(dbContext);
            var recentFrom = now.AddDays(-options.RecentDays);
            var baselineFrom = recentFrom.AddDays(-options.BaselineDays);
            var observed = 0;
            var significant = 0;
            var notified = 0;

            foreach (var model in await repository.ListModelsWithRecentArchiveAsync(recentFrom, token)
                         .ConfigureAwait(false))
            {
                foreach (var source in new[] { (short)ModelDriftSource.Exhaustive,
                             (short)ModelDriftSource.AdvancedAnalytics })
                {
                    foreach (var name in await repository.ListScoreNamesAsync(model.ModelId, source, recentFrom, token)
                                 .ConfigureAwait(false))
                    {
                        token.ThrowIfCancellationRequested();

                        var current = await repository.ListScoresAsync(model.ModelId, source, name, recentFrom,
                            now.AddMinutes(1), options.MaximumSamples, token).ConfigureAwait(false);
                        var baseline = await repository.ListScoresAsync(model.ModelId, source, name, baselineFrom,
                            recentFrom, options.MaximumSamples, token).ConfigureAwait(false);

                        if (current.Count < options.MinimumSamples || baseline.Count < options.MinimumSamples)
                        {
                            continue;
                        }

                        var psi = PopulationStability.Compute(baseline, current, options.Bins);
                        var status = PopulationStability.Status(psi);
                        var previous = await repository.LatestStatusAsync(model.ModelId, source, name, token)
                            .ConfigureAwait(false);

                        await repository.InsertObservationAsync(new ModelDriftObservation
                        {
                            TenantRegistryId = model.TenantRegistryId,
                            EntityAnalysisModelId = model.ModelId,
                            SourceId = source,
                            ScoreName = name.Length > 500 ? name[..500] : name,
                            ObservedDate = now,
                            BaselineCount = baseline.Count,
                            CurrentCount = current.Count,
                            Psi = psi,
                            StatusId = status
                        }, token).ConfigureAwait(false);

                        observed++;

                        if (status != (short)ModelDriftStatus.Significant)
                        {
                            continue;
                        }

                        significant++;

                        if (previous == (short)ModelDriftStatus.Significant)
                        {
                            continue;
                        }

                        var arguments = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            model = model.Name, source, score = name, psi = Math.Round(psi, 3)
                        });

                        foreach (var user in await rescreen.ListPermissionHoldersAsync(model.TenantRegistryId,
                                     ModelPermission, token).ConfigureAwait(false))
                        {
                            if (await notifications.InsertIfNewAsync(new UserNotification
                                {
                                    Guid = Guid.NewGuid(),
                                    TenantRegistryId = model.TenantRegistryId,
                                    RecipientUser = user,
                                    KindId = (short)NotificationKind.ModelDrift,
                                    PriorityId = (short)NotificationPriority.High,
                                    ArgumentsJson = arguments,
                                    DedupeKey = $"model-drift:{model.ModelId}:{source}:{name}:{now:yyyyMMdd}",
                                    CreatedDate = now
                                }, token).ConfigureAwait(false))
                            {
                                notified++;
                            }
                        }
                    }
                }
            }

            return new ModelDriftResult(observed, significant, notified);
        }
    }
}
