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

using System.Diagnostics;
using System.Threading.Tasks;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class ActivationRulesExtensions
    {
        public static async Task<Context> ExecuteActivationsAsync(this Context context)
        {
            context.TraceLog(
                $"will now process {context.EntityAnalysisModel.Collections.ModelActivationRules.Count} Activation Rules.");

            var stopwatch = Stopwatch.StartNew();

            var (activationRuleCount, createCase, prevailingActivationRuleId, items)
                = await context.IterateAndProcessAsync(context.EntityAnalysisModel.Services.CacheService,
                        context.AvailableEntityAnalysisModels, context.EntityAnalysisModel.Services.RabbitMqChannel)
                    .ConfigureAwait(false);

            context.ActivationRuleFinishResponseElevation(context.EntityAnalysisModelInstanceEntryPayload
                .ResponseElevation.Value);
            context.ActivationRuleResponseElevationAddToCounters();
            context.UpdateContextStateWithActivationRulesOutcome(activationRuleCount, prevailingActivationRuleId,
                createCase);

            stopwatch.Stop();

            if (context.LogSampled)
            {
                var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                    new InvokeStagePerformance();

                stages.Activation = new StageTiming<ActivationRuleTiming>
                {
                    DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency)),
                    Items = items
                };
            }

            context.TraceLog(
                $"has added the response elevation for use in bidding against other models if called by model inheritance.");

            return context;
        }
    }
}