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
    using System.Threading.Tasks;
    using BackgroundTasks.Context;
    using Data.Context;
    using Data.Repository;
    using Dto.Hold;

    public static class HoldLegRecorder
    {
        private static readonly TimeSpan[] Backoff =
        [
            TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16),
            TimeSpan.FromSeconds(32)
        ];

        public static void Record(Context context, Guid entityAnalysisModelInstanceEntryGuid, ResponseHoldLeg leg)
        {
            _ = Task.Run(() => RecordAsync(context, entityAnalysisModelInstanceEntryGuid, leg));
        }

        private static async Task RecordAsync(Context context, Guid entityAnalysisModelInstanceEntryGuid,
            ResponseHoldLeg leg)
        {
            var token = context.Services.TaskCoordinator.CancellationToken;

            try
            {
                for (var attempt = 0;; attempt++)
                {
                    if (await TryRecordAsync(context, entityAnalysisModelInstanceEntryGuid, leg, token)
                            .ConfigureAwait(false))
                    {
                        return;
                    }

                    if (attempt >= Backoff.Length)
                    {
                        context.Services.Log.Warn(
                            $"Hold Leg: the archive for {entityAnalysisModelInstanceEntryGuid} did not appear in time, so the {leg.KindId} leg outcome was not recorded.");

                        return;
                    }

                    await Task.Delay(Backoff[attempt], token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation HoldLegRecorder: {ex.Message}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error(
                    $"Hold Leg: failed to record the {leg.KindId} leg for {entityAnalysisModelInstanceEntryGuid} as {ex}");
            }
        }

        private static async Task<bool> TryRecordAsync(Context context, Guid entityAnalysisModelInstanceEntryGuid,
            ResponseHoldLeg leg, System.Threading.CancellationToken token)
        {
            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

            try
            {
                var update = await new ArchiveRepository(dbContext).UpdateHoldAsync(
                    entityAnalysisModelInstanceEntryGuid, hold =>
                    {
                        hold.Legs.Add(leg);

                        return Task.FromResult(hold);
                    }, token).ConfigureAwait(false);

                if (update.Archive == null)
                {
                    return false;
                }

                if (update.Hold != null)
                {
                    await new CaseRepository(dbContext).UpdateArchiveJsonAsync(entityAnalysisModelInstanceEntryGuid,
                        update.Archive.Json, token).ConfigureAwait(false);
                }

                return true;
            }
            finally
            {
                try
                {
                    await dbContext.CloseAsync().ConfigureAwait(false);
                }
                finally
                {
                    await dbContext.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
