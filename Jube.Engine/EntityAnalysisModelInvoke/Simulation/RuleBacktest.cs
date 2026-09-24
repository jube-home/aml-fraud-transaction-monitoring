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

namespace Jube.Engine.EntityAnalysisModelInvoke.Simulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Parser;

    public sealed record BacktestRow(
        Guid EntityAnalysisModelInstanceEntryGuid,
        string EntryKeyValue,
        DateTime? ReferenceDate,
        JObject Archive,
        IReadOnlyCollection<string> Tags);

    public sealed record BacktestSample(
        Guid EntityAnalysisModelInstanceEntryGuid,
        string EntryKeyValue,
        DateTime? ReferenceDate,
        bool Fired,
        bool Positive,
        string Error);

    public sealed record BacktestPlan(
        int EntityAnalysisModelId,
        RuleParseResult Rule,
        int RuleParseType,
        bool Reprocessing,
        RuleParseResult Filter,
        Func<IReadOnlyDictionary<string, object>, bool> Class,
        IReadOnlyList<string> TagNames,
        IReadOnlyList<InvocationContextField> Fields,
        Dictionary<string, List<string>> Lists,
        int SampleSize);

    public sealed record BacktestValue(string Name, string Value);

    public sealed record BacktestExplanation(
        Guid EntityAnalysisModelInstanceEntryGuid,
        string EntryKeyValue,
        DateTime? ReferenceDate,
        IReadOnlyList<string> Tags,
        bool PassedFilter,
        string FilterError,
        bool Fired,
        string RuleError,
        bool? Positive,
        IReadOnlyList<BacktestValue> Values);

    public sealed class RuleBacktest(BacktestPlan plan)
    {
        public const string TagNamespace = "Tag";

        public BacktestResult Result { get; } = new() { ClassDefined = plan.Class != null };

        public static BacktestOutcome Classify(bool fired, bool positive)
        {
            return (fired, positive) switch
            {
                (true, true) => BacktestOutcome.TruePositive,
                (true, false) => BacktestOutcome.FalsePositive,
                (false, true) => BacktestOutcome.FalseNegative,
                _ => BacktestOutcome.TrueNegative
            };
        }

        public static IReadOnlyDictionary<string, object> ClassValues(InvocationContext context,
            IEnumerable<string> tagNames, IReadOnlyCollection<string> tags)
        {
            ArgumentNullException.ThrowIfNull(context);

            var values = context.Values.ToDictionary(v => v.Key, v => v.Value.Value, StringComparer.Ordinal);
            foreach (var tag in tagNames ?? [])
            {
                values[$"{TagNamespace}.{tag}"] = tags.Contains(tag);
            }

            return values;
        }

        public static async Task<BacktestExplanation> ExplainAsync(BacktestPlan plan, BacktestRow row,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentNullException.ThrowIfNull(row);

            var context = InvocationContextBuilder.FromArchive(plan.EntityAnalysisModelId,
                row.EntityAnalysisModelInstanceEntryGuid, row.Archive, plan.Fields);
            var inputs = RuleRunner.ToInputs(context, plan.Lists);

            var passed = true;
            string filterError = null;
            if (plan.Filter != null)
            {
                var filter = await RuleRunner.RunAsync(plan.Filter, RuleParse.GatewayRule, true, inputs, token)
                    .ConfigureAwait(false);
                filterError = filter.Error == null ? null : $"{filter.Error.GetType().Name}: {filter.Error.Message}";
                passed = filter.Error == null && filter.Value is true;
            }

            var run = await RuleRunner.RunAsync(plan.Rule, plan.RuleParseType, plan.Reprocessing, inputs, token)
                .ConfigureAwait(false);
            var names = plan.Rule.References.Select(r => r.CompletionName)
                .Concat(plan.Filter?.References.Select(r => r.CompletionName) ?? [])
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal)
                .Select(n => new BacktestValue(n, context.Values.TryGetValue(n, out var value)
                    ? InvocationContextBuilder.FormatValue(value.Value)
                    : null))
                .ToList();

            return new BacktestExplanation(row.EntityAnalysisModelInstanceEntryGuid, row.EntryKeyValue,
                row.ReferenceDate, row.Tags.ToList(), passed, filterError, run.Error == null && run.Value is true,
                run.Error == null ? null : $"{run.Error.GetType().Name}: {run.Error.Message}",
                plan.Class?.Invoke(ClassValues(context, plan.TagNames, row.Tags)), names);
        }

        public async Task<bool> AddAsync(IEnumerable<BacktestRow> rows, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(rows);

            foreach (var row in rows)
            {
                token.ThrowIfCancellationRequested();
                Result.Scanned++;

                var context = InvocationContextBuilder.FromArchive(plan.EntityAnalysisModelId,
                    row.EntityAnalysisModelInstanceEntryGuid, row.Archive, plan.Fields);
                var inputs = RuleRunner.ToInputs(context, plan.Lists);

                if (plan.Filter != null)
                {
                    var filter = await RuleRunner.RunAsync(plan.Filter, RuleParse.GatewayRule, true, inputs, token)
                        .ConfigureAwait(false);
                    Result.DurationMicroseconds += filter.DurationMicroseconds;
                    if (filter.Error != null)
                    {
                        Result.FilterErrors++;
                        Result.FilteredOut++;
                        if (Abort(filter))
                        {
                            return false;
                        }

                        continue;
                    }

                    if (filter.Value is not true)
                    {
                        Result.FilteredOut++;
                        continue;
                    }
                }

                var run = await RuleRunner.RunAsync(plan.Rule, plan.RuleParseType, plan.Reprocessing, inputs, token)
                    .ConfigureAwait(false);
                Result.Evaluated++;
                Result.DurationMicroseconds += run.DurationMicroseconds;
                Track(row);

                var fired = run.Error == null && run.Value is true;
                var positive = plan.Class != null && plan.Class(ClassValues(context, plan.TagNames, row.Tags));
                string error = null;

                if (run.Error != null)
                {
                    Result.RuntimeErrors++;
                    error = $"{run.Error.GetType().Name}: {run.Error.Message}";
                    AddSample(Result.ErrorSamples, row, false, positive, error);
                }

                if (fired)
                {
                    Result.Fired++;
                }
                else
                {
                    Result.NotFired++;
                }

                if (plan.Class != null)
                {
                    Tally(row, fired, positive, error);
                }

                if (Abort(run))
                {
                    return false;
                }
            }

            return true;
        }

        private bool Abort(RuleRunResult run)
        {
            if (!run.TimedOut)
            {
                return false;
            }

            Result.Aborted = true;
            Result.AbortReason = "A rule took longer than the time limit on one transaction, so the backtest " +
                                 "stopped there; the engine has no time limit, so a rule this slow would hold " +
                                 "up every invocation.";
            return true;
        }

        private void Track(BacktestRow row)
        {
            foreach (var tag in row.Tags.Distinct(StringComparer.Ordinal))
            {
                Result.TagsInSample[tag] = Result.TagsInSample.TryGetValue(tag, out var count) ? count + 1 : 1;
            }

            if (!row.ReferenceDate.HasValue)
            {
                return;
            }

            if (Result.EarliestReferenceDate == null || row.ReferenceDate < Result.EarliestReferenceDate)
            {
                Result.EarliestReferenceDate = row.ReferenceDate;
            }

            if (Result.LatestReferenceDate == null || row.ReferenceDate > Result.LatestReferenceDate)
            {
                Result.LatestReferenceDate = row.ReferenceDate;
            }
        }

        private void Tally(BacktestRow row, bool fired, bool positive, string error)
        {
            if (positive)
            {
                Result.Positives++;
            }

            switch (Classify(fired, positive))
            {
                case BacktestOutcome.TruePositive:
                    Result.TruePositives++;
                    AddSample(Result.TruePositiveSamples, row, true, true, error);
                    break;
                case BacktestOutcome.FalsePositive:
                    Result.FalsePositives++;
                    AddSample(Result.FalsePositiveSamples, row, true, false, error);
                    break;
                case BacktestOutcome.FalseNegative:
                    Result.FalseNegatives++;
                    AddSample(Result.FalseNegativeSamples, row, false, true, error);
                    break;
                default:
                    Result.TrueNegatives++;
                    break;
            }
        }

        private void AddSample(List<BacktestSample> samples, BacktestRow row, bool fired, bool positive,
            string error)
        {
            if (samples.Count < plan.SampleSize)
            {
                samples.Add(new BacktestSample(row.EntityAnalysisModelInstanceEntryGuid, row.EntryKeyValue,
                    row.ReferenceDate, fired, positive, error));
            }
        }
    }
}