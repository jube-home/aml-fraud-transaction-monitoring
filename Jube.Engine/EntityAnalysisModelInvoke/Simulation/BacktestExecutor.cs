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
    using Data.Context;
    using Data.Query;
    using Data.Query.Models;
    using Data.QueryBuilder;
    using Data.Repository;
    using log4net;
    using Newtonsoft.Json.Linq;
    using Parser;

    public sealed record BacktestSpecification(
        int EntityAnalysisModelId,
        string RuleType,
        string RuleText,
        string FilterJson,
        string FilterRuleText,
        string ClassJson,
        DateTime? From,
        DateTime? To,
        long Limit,
        int SampleSize);

    public sealed record BacktestError(
        string Property,
        string Code,
        string Message,
        int? Line = null,
        int? Start = null,
        int? Length = null);

    public sealed record BacktestProgress(long Scanned, long Evaluated, long Limit);

    public sealed record BacktestExecution(BacktestResult Result, IReadOnlyList<BacktestError> Errors)
    {
        public bool Valid => Result != null && Errors.Count == 0;
    }

    public sealed record BacktestField(string Name, BuilderFieldType Type);

    public static class BacktestExecutor
    {
        private const int MaxSampleSize = 50;

        private static readonly ILog log = LogManager.GetLogger(typeof(BacktestExecutor));

        private static readonly Dictionary<string, (int ParseType, bool Reprocessing)> ruleTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["GatewayRule"] = (RuleParse.GatewayRule, false),
                ["ActivationRule"] = (RuleParse.ActivationRule, false),
                ["ReprocessingRule"] = (RuleParse.GatewayRule, true)
            };

        public static IReadOnlyCollection<string> RuleTypes => ruleTypes.Keys;

        public static async Task<IReadOnlyList<BacktestField>> FilterFieldsAsync(DbContext dbContext,
            int tenantRegistryId, int entityAnalysisModelId, CancellationToken token = default)
        {
            return (await BuilderFieldsAsync(dbContext, tenantRegistryId, entityAnalysisModelId,
                    RuleParse.GatewayRule, token).ConfigureAwait(false))
                .Select(f => new BacktestField(f.Key, f.Value.Type)).OrderBy(f => f.Name, StringComparer.Ordinal)
                .ToList();
        }

        public static async Task<IReadOnlyList<BacktestField>> ClassFieldsAsync(DbContext dbContext,
            int tenantRegistryId, int entityAnalysisModelId, CancellationToken token = default)
        {
            return (await ClassCatalogueAsync(dbContext, tenantRegistryId, entityAnalysisModelId, token)
                    .ConfigureAwait(false)).Fields.Values
                .Select(f => new BacktestField(f.Id, f.Type)).OrderBy(f => f.Name, StringComparer.Ordinal)
                .ToList();
        }

        public static async Task<(BacktestPlan Plan, IReadOnlyList<BacktestError> Errors)> PrepareAsync(
            DbContext dbContext, int tenantRegistryId, BacktestSpecification specification,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(specification);

            var errors = new List<BacktestError>();
            if (!ruleTypes.TryGetValue(specification.RuleType ?? string.Empty, out var ruleType))
            {
                return (null, [
                    new BacktestError("RuleType", "RuleTypeInvalid",
                        $"{specification.RuleType} cannot be backtested; use {string.Join(", ", ruleTypes.Keys)}.")
                ]);
            }

            if (specification.From > specification.To)
            {
                return (null, [new BacktestError("From", "RangeInvalid", "From must be earlier than To.")]);
            }

            if (string.IsNullOrWhiteSpace(specification.RuleText))
            {
                return (null, [new BacktestError("RuleText", "RuleTextRequired", "A rule to backtest is required.")]);
            }

            var modelId = specification.EntityAnalysisModelId;
            var rule = await CompileAsync(dbContext, tenantRegistryId, modelId, specification.RuleText,
                ruleType.ParseType, ruleType.Reprocessing, "RuleText", errors, token).ConfigureAwait(false);

            RuleParseResult filter = null;
            if (!string.IsNullOrWhiteSpace(specification.FilterRuleText))
            {
                filter = await CompileAsync(dbContext, tenantRegistryId, modelId, specification.FilterRuleText,
                    RuleParse.GatewayRule, true, "FilterRuleText", errors, token).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(specification.FilterJson))
            {
                var parsed = BuilderProfile.Parse(specification.FilterJson, await BuilderFieldsAsync(dbContext,
                        tenantRegistryId, modelId, RuleParse.GatewayRule, token).ConfigureAwait(false),
                    BuilderTarget.RuleText);

                errors.AddRange(parsed.Errors.Select(e => new BacktestError("Filter" + e.Path[1..], e.Code,
                    e.Message)));

                if (parsed.Valid)
                {
                    filter = await CompileAsync(dbContext, tenantRegistryId, modelId,
                        BuilderRuleTranslator.Translate(parsed.Group), RuleParse.GatewayRule, true, "Filter",
                        errors, token).ConfigureAwait(false);
                }
            }

            Func<IReadOnlyDictionary<string, object>, bool> classPredicate = null;
            var classNames = new List<string>();

            var catalogue = await ClassCatalogueAsync(dbContext, tenantRegistryId, modelId, token)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(specification.ClassJson))
            {
                var parsed = BuilderProfile.Parse(specification.ClassJson, catalogue.Fields);

                errors.AddRange(parsed.Errors.Select(e => new BacktestError("Class" + e.Path[1..], e.Code,
                    e.Message)));

                if (parsed.Valid)
                {
                    classPredicate = BuilderFilter.CompileValues(parsed.Group);
                    classNames.AddRange(FieldIds(parsed.Group));
                }
            }

            if (errors.Count > 0)
            {
                return (null, errors);
            }

            var fields = (await new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext,
                        tenantRegistryId)
                    .ExecuteAsync(modelId, RuleParse.ActivationRule, false, token).ConfigureAwait(false))
                .Where(f => f.JQueryBuilderDataType != "list" && f.Name.Contains('.'))
                .GroupBy(f => f.Name)
                .Select(g => new InvocationContextField(g.Key, g.First().Group, g.First().JQueryBuilderDataType))
                .ToList();

            var needed = rule.References.Select(r => r.CompletionName)
                .Concat(filter?.References.Select(r => r.CompletionName) ?? [])
                .Concat(classNames)
                .ToHashSet(StringComparer.Ordinal);

            fields = fields.Where(f => needed.Contains(f.Name)).ToList();

            var lists = await new GetModelListsQuery(dbContext, tenantRegistryId).ExecuteAsync(modelId, token)
                .ConfigureAwait(false);

            return (new BacktestPlan(modelId, rule, ruleType.ParseType, ruleType.Reprocessing, filter,
                classPredicate, catalogue.TagNames, fields, lists,
                Math.Clamp(specification.SampleSize, 0, MaxSampleSize)), []);
        }

        public static async Task<BacktestExecution> ExecuteAsync(DbContext dbContext, int tenantRegistryId,
            BacktestSpecification specification, int pageSize, Func<BacktestProgress, Task<bool>> onPage,
            CancellationToken token = default, DbContext archive = null)
        {
            var (plan, errors) = await PrepareAsync(dbContext, tenantRegistryId, specification, token)
                .ConfigureAwait(false);
            if (plan == null)
            {
                return new BacktestExecution(null, errors);
            }

            var backtest = new RuleBacktest(plan);
            var query = new GetArchiveBacktestSampleQuery(archive ?? dbContext, tenantRegistryId);
            var limit = Math.Max(1, specification.Limit);
            var size = Math.Max(1, pageSize);

            ArchiveBacktestSampleRow after = null;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                var remaining = limit - backtest.Result.Scanned;
                if (remaining <= 0)
                {
                    backtest.Result.LimitReached = true;
                    break;
                }

                var take = (int)Math.Min(size, remaining);
                var page = await query.ExecutePageAsync(plan.EntityAnalysisModelId, specification.From,
                    specification.To, take, after, token).ConfigureAwait(false);
                if (page.Count == 0)
                {
                    break;
                }

                after = page[^1];
                if (!await backtest.AddAsync(page.Select(ToRow), token).ConfigureAwait(false))
                {
                    break;
                }

                if (onPage != null && !await onPage(new BacktestProgress(backtest.Result.Scanned,
                        backtest.Result.Evaluated, limit)).ConfigureAwait(false))
                {
                    backtest.Result.Aborted = true;
                    backtest.Result.AbortReason = "The backtest was stopped on request.";
                    break;
                }

                if (page.Count < take)
                {
                    break;
                }
            }

            return new BacktestExecution(backtest.Result, []);
        }

        public static async Task<(BacktestExplanation Explanation, IReadOnlyList<BacktestError> Errors)>
            ExplainAsync(DbContext dbContext, int tenantRegistryId, BacktestSpecification specification,
                Guid entityAnalysisModelInstanceEntryGuid, CancellationToken token = default,
                DbContext archive = null)
        {
            var (plan, errors) = await PrepareAsync(dbContext, tenantRegistryId, specification, token)
                .ConfigureAwait(false);

            if (plan == null)
            {
                return (null, errors);
            }

            var row = await new GetArchiveBacktestSampleQuery(archive ?? dbContext, tenantRegistryId)
                .ExecuteEntryAsync(plan.EntityAnalysisModelId, entityAnalysisModelInstanceEntryGuid, token)
                .ConfigureAwait(false);

            if (row == null)
            {
                return (null, [
                    new BacktestError("EntityAnalysisModelInstanceEntryGuid", "ArchiveNotFound",
                        "The transaction is not in this model's archive.")
                ]);
            }

            return (await RuleBacktest.ExplainAsync(plan, ToRow(row), token).ConfigureAwait(false), []);
        }

        private static IEnumerable<string> FieldIds(BuilderGroup group)
        {
            foreach (var node in group.Rules)
            {
                switch (node)
                {
                    case BuilderGroup nested:
                        foreach (var id in FieldIds(nested))
                        {
                            yield return id;
                        }

                        break;
                    case BuilderRule rule:
                        yield return rule.Id;
                        break;
                }
            }
        }

        private static BacktestRow ToRow(ArchiveBacktestSampleRow row)
        {
            return new BacktestRow(row.EntityAnalysisModelInstanceEntryGuid, row.EntryKeyValue, row.ReferenceDate,
                JObject.Parse(row.Json), row.Tags);
        }

        private static async Task<RuleParseResult> CompileAsync(DbContext dbContext, int tenantRegistryId,
            int modelId, string text, int parseType, bool reprocessing, string property, List<BacktestError> errors,
            CancellationToken token)
        {
            var environment = await new GetRuleParseEnvironmentQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(modelId, parseType, token).ConfigureAwait(false);

            var parsed = RuleParse.Execute(text ?? string.Empty, parseType, environment, log,
                RuleParse.EngineReferences(parseType), false, null, true, reprocessing);

            if (parsed.Compiled)
            {
                return parsed;
            }

            errors.AddRange(parsed.ErrorSpans.Count == 0
                ? [new BacktestError(property, "RuleScriptInvalid", parsed.Message ?? string.Empty)]
                : parsed.ErrorSpans.Select(s => new BacktestError(property, "RuleScriptInvalid", s.Message, s.Line,
                    s.Start, s.Length)));

            return null;
        }

        private static async Task<IReadOnlyDictionary<string, BuilderField>> BuilderFieldsAsync(DbContext dbContext,
            int tenantRegistryId, int modelId, int parseType, CancellationToken token)
        {
            var completions = await new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext,
                    tenantRegistryId)
                .ExecuteAsync(modelId, parseType, false, token).ConfigureAwait(false);

            return BuilderRuleTranslator.Fields(completions.Select(c => (c.Name, c.JQueryBuilderDataType)));
        }

        private static async Task<(IReadOnlyDictionary<string, BuilderField> Fields, List<string> TagNames)>
            ClassCatalogueAsync(DbContext dbContext, int tenantRegistryId, int modelId, CancellationToken token)
        {
            var fields = (await BuilderFieldsAsync(dbContext, tenantRegistryId, modelId, RuleParse.ActivationRule,
                    token).ConfigureAwait(false))
                .Where(f => f.Value.Type != BuilderFieldType.List)
                .ToDictionary(f => f.Key, f => f.Value, StringComparer.Ordinal);

            var tagNames = (await new EntityAnalysisModelTagRepository(dbContext, tenantRegistryId)
                    .GetByEntityAnalysisModelIdOrderByNameAsync(modelId, token).ConfigureAwait(false))
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .Select(t => t.Name)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var tag in tagNames)
            {
                var id = $"{RuleBacktest.TagNamespace}.{tag}";
                fields[id] = new BuilderField(id, BuilderFieldType.Boolean);
            }

            return (fields, tagNames);
        }
    }
}