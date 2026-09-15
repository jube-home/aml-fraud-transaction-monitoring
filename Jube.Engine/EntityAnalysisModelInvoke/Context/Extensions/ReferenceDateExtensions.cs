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
using System.Diagnostics;
using System.Threading.Tasks;
using Jube.Cache;
using Jube.Engine.EntityAnalysisModelInvoke.Exceptions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.TaskCancellation.TaskHelper;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class ReferenceDateExtensions
    {
        public static async Task<Context> CheckIntegrityAndUpsertAsync(this Context context, CacheService cacheService)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var referenceDate = await cacheService.CacheReferenceDateRepository
                    .GetReferenceDateAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                        context.EntityAnalysisModel.Instance.Guid).ConfigureAwait(false);

                context.TraceLog(
                    $"is checking reference date integrity, latest stored reference date is {referenceDate}.");

                if (context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate > DateTime.UtcNow)
                {
                    context.TraceLog(
                        $"has rejected reference date {context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate} because it is in the future.");

                    throw new ReferenceDateInFutureException();
                }

                if (context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate < referenceDate)
                {
                    context.TraceLog(
                        $"has reference date {context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate} older than the latest stored reference date {referenceDate}, so it will not be upserted.");

                    return context;
                }

                context.TraceLog(
                    $"is upserting reference date {context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate}.");

                context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                    TaskType.UpsertReferenceDateAsync,
                    async () => await
                        cacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                            context.EntityAnalysisModel.Instance.TenantRegistryId,
                            context.EntityAnalysisModel.Instance.Guid,
                            context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate), context.Log));

                return context;
            }
            finally
            {
                stopwatch.Stop();

                if (context.LogSampled)
                {
                    var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                        new InvokeStagePerformance();

                    stages.CheckIntegrityAndUpsert = new StageDuration
                    {
                        DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency))
                    };
                }
            }
        }
    }
}