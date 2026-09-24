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

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Engine.EntityAnalysisModelInvoke.Simulation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context.Extensions.AbstractionRulesWithSearchKeys;
    using Dictionary;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using log4net;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using Parser;

    public sealed record AbstractionSettings(
        int Id,
        string Name,
        string SearchKey,
        string SearchFunctionKey,
        int AggregationFunctionType,
        string IntervalType,
        int IntervalValue,
        bool EnableOffset,
        int OffsetType,
        int OffsetValue)
    {
        public static AbstractionSettings FromRecord(int id, string name, string searchKey, string searchFunctionKey,
            int? searchFunctionTypeId, string searchInterval, int? searchValue, bool? offset, int? offsetTypeId,
            int? offsetValue)
        {
            return new AbstractionSettings(id, name?.Replace(" ", "_"), searchKey, searchFunctionKey,
                searchFunctionTypeId ?? 1, searchInterval ?? "d", searchInterval != null ? searchValue ?? 0 : 0,
                offset == true, offsetTypeId ?? 0, offsetValue ?? 0);
        }

        public EntityAnalysisModelAbstractionRule ToEngineRule()
        {
            return new EntityAnalysisModelAbstractionRule
            {
                Id = Id,
                Name = Name,
                SearchKey = SearchKey,
                SearchFunctionKey = SearchFunctionKey,
                AbstractionRuleAggregationFunctionType = AggregationFunctionType,
                AbstractionRuleAggregationFunctionIntervalType = IntervalType,
                AbstractionHistoryIntervalValue = IntervalValue,
                EnableOffset = EnableOffset,
                OffsetType = OffsetType,
                OffsetValue = OffsetValue,
                Search = true
            };
        }
    }

    public sealed record SearchKeySettings(
        string SearchKey,
        string TtlInterval,
        int TtlIntervalValue,
        int FetchLimit,
        bool CacheBacked);

    public sealed record AbstractionSimulationResult(
        int DocumentsEvaluated,
        int Matched,
        int InWindow,
        DateTime WindowFrom,
        DateTime WindowTo,
        bool WindowShortenedBySearchKey,
        double? Value,
        IReadOnlyList<DictionaryNoBoxing<string>> MatchedInWindow,
        Exception Error);

    public static class AbstractionAggregationSimulator
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AbstractionAggregationSimulator));

        public static async Task<AbstractionSimulationResult> SimulateAsync(RuleParseResult parsed,
            AbstractionSettings rule, SearchKeySettings searchKey, RuleRunInputs current,
            IEnumerable<DictionaryNoBoxing<string>> history, DateTime referenceDate, string referenceDateName,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(parsed);
            ArgumentNullException.ThrowIfNull(rule);
            ArgumentNullException.ThrowIfNull(searchKey);
            ArgumentNullException.ThrowIfNull(current);

            if (!current.Data.ContainsKey(referenceDateName))
            {
                current.Data.TryAdd(referenceDateName, referenceDate);
            }

            var documents = history.ToList();
            documents.Add(current.Data);

            var windowFrom = AbstractionWindow.FromDate(rule.IntervalType, rule.IntervalValue, searchKey.TtlInterval,
                searchKey.TtlIntervalValue, referenceDate);
            var shortened = AbstractionWindow.SearchKeyShortensTheWindow(rule.IntervalType, rule.IntervalValue,
                searchKey.TtlInterval, searchKey.TtlIntervalValue, referenceDate);

            List<DictionaryNoBoxing<string>> matches;
            try
            {
                var match = await RuleRunner.Sandbox
                    .GetDelegateAsync<EntityAnalysisModelAbstractionRule.Match>(parsed, token).ConfigureAwait(false);
                matches = await RuleRunner.Sandbox
                    .RunAsync(() => documents.FindAll(d => match(d, current.Lists, current.Kvp, log)), token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new AbstractionSimulationResult(documents.Count, 0, 0, windowFrom, referenceDate, shortened,
                    null, [], ex);
            }

            var inWindow = matches.FindAll(x =>
                x[referenceDateName].AsDateTime() >= windowFrom && x[referenceDateName].AsDateTime() <= referenceDate);

            var value = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                new EntityAnalysisModelInstanceEntryPayload { Payload = current.Data, ReferenceDate = referenceDate },
                new ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>> { [rule.Id] = inWindow },
                rule.ToEngineRule(), log);

            return new AbstractionSimulationResult(documents.Count, matches.Count, inWindow.Count, windowFrom,
                referenceDate, shortened, value, inWindow, null);
        }
    }
}