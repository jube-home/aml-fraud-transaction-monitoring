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

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Training.Models;

namespace Jube.Engine.AdvancedAnalytics
{
    public sealed class AdvancedAnalyticsDatabaseTrainingTrace(
        DbContext dbContext,
        int tenantRegistryId,
        string userName) : IAdvancedAnalyticsTrainingTrace
    {
        public const string DurationKey = "durationMilliseconds";
        public const string TrainLossKey = "trainLoss";
        public const string ValidationLossKey = "validationLoss";
        public const string ScoreKey = "score";

        private readonly AdvancedAnalyticsTrainingRunIterationRepository iterations = new(dbContext, tenantRegistryId,
            userName);

        private readonly AdvancedAnalyticsTrainingRunMetricRepository metrics = new(dbContext, tenantRegistryId,
            userName);

        private readonly AdvancedAnalyticsTrainingRunRepository runs = new(dbContext, tenantRegistryId, userName);
        private readonly AdvancedAnalyticsTrainingRunStepRepository steps = new(dbContext, tenantRegistryId, userName);

        public async Task AppendAsync(AdvancedAnalyticsTrainingTraceEntry entry, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var run = await runs.GetByGuidAsync(entry.CorrelationKey, token).ConfigureAwait(false)
                      ?? throw new KeyNotFoundException();

            if (Enum.TryParse<AdvancedAnalyticsStep>(entry.Step, false, out var step)
                && Enum.IsDefined(step))
            {
                await AppendStepAsync(run, step, entry, token).ConfigureAwait(false);

                return;
            }

            await AppendIterationAsync(run, entry, token).ConfigureAwait(false);
        }

        private async Task AppendStepAsync(AdvancedAnalyticsTrainingRun run, AdvancedAnalyticsStep step,
            AdvancedAnalyticsTrainingTraceEntry entry, CancellationToken token)
        {
            var completed = DateTime.UtcNow;

            double? duration = entry.Metrics.TryGetValue(DurationKey, out var measured) && double.IsFinite(measured)
                ? measured
                : null;

            await steps.InsertAsync(new AdvancedAnalyticsTrainingRunStep
            {
                AdvancedAnalyticsTrainingRunId = run.Id,
                StepOrder = entry.Sequence,
                Step = step,
                StatusId = AdvancedAnalyticsStatus.Succeeded,
                StartedDate = duration.HasValue ? completed.AddMilliseconds(-duration.Value) : null,
                CompletedDate = completed,
                DurationMilliseconds = duration
            }, token).ConfigureAwait(false);

            var rows = entry.Metrics
                .Where(m => m.Key != DurationKey)
                .OrderBy(m => m.Key, StringComparer.Ordinal)
                .Select(m =>
                {
                    var (split, name) = SplitOf(m.Key);

                    return new AdvancedAnalyticsTrainingRunMetric
                    {
                        AdvancedAnalyticsTrainingRunId = run.Id,
                        Name = name,
                        Value = double.IsFinite(m.Value) ? m.Value : null,
                        Split = split
                    };
                })
                .ToList();

            if (rows.Count > 0)
            {
                await metrics.InsertRangeAsync(rows, token).ConfigureAwait(false);
            }
        }

        private Task AppendIterationAsync(AdvancedAnalyticsTrainingRun run,
            AdvancedAnalyticsTrainingTraceEntry entry, CancellationToken token)
        {
            return iterations.InsertAsync(new AdvancedAnalyticsTrainingRunIteration
            {
                AdvancedAnalyticsTrainingRunId = run.Id,
                IterationNumber = entry.Sequence,
                TrainLoss = Finite(entry.Metrics, TrainLossKey),
                ValidationLoss = Finite(entry.Metrics, ValidationLossKey),
                Score = Finite(entry.Metrics, ScoreKey),
                RecordedDate = DateTime.UtcNow
            }, token);
        }

        private static double? Finite(IReadOnlyDictionary<string, double> values, string key)
        {
            return values.TryGetValue(key, out var value) && double.IsFinite(value) ? value : null;
        }

        private static (AdvancedAnalyticsSplit Split, string Name) SplitOf(string key)
        {
            foreach (var split in new[]
                     {
                         AdvancedAnalyticsSplit.Train, AdvancedAnalyticsSplit.Validation, AdvancedAnalyticsSplit.Test
                     })
            {
                var prefix = split + ".";

                if (key.StartsWith(prefix, StringComparison.Ordinal) && key.Length > prefix.Length)
                {
                    return (split, key[prefix.Length..]);
                }
            }

            return (AdvancedAnalyticsSplit.Train, key);
        }
    }
}