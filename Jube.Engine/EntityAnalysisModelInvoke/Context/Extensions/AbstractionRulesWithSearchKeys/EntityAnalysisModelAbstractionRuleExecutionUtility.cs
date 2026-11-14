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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using log4net;
using StackExchange.Redis;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys
{
    public class Execute
    {
        public DistinctSearchKey DistinctSearchKey { get; init; }
        public DictionaryNoBoxing<string> CachePayloadDocument { get; init; }
        public EntityAnalysisModel EntityAnalysisModel { get; init; }
        public EntityAnalysisModelInstanceEntryPayload EntityAnalysisModelInstanceEntryPayload { get; init; }
        public PooledDictionary<string, double> EntityInstanceEntryDictionaryKvPs { get; init; }

        public ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>> AbstractionRuleMatches { get; init; } =
            new();

        public ILog Log { get; init; }
        public List<RedisValue> SortedSetKeys { get; init; }
        public Dictionary<string, DictionaryNoBoxing<string>> PayloadMap { get; init; }
        public Context Context { get; init; }

        public Task StartAsync()
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    var documents = new List<DictionaryNoBoxing<string>>(SortedSetKeys.Count);

                    foreach (var key in SortedSetKeys.Select(k => k.ToString()))
                    {
                        if (PayloadMap.TryGetValue(key, out var payload))
                        {
                            documents.Add(payload);
                        }
                    }

                    Context.TraceLog(
                        $"for grouping key {DistinctSearchKey.SearchKey} has parsed {documents.Count} from the database. Elapsed {sw.ElapsedMilliseconds}.");

                    documents.Add(CachePayloadDocument);

                    Context.TraceLog(
                        $"has created a filter for cache where {DistinctSearchKey.SearchKey} has added the current transaction to the records, so there are now {documents.Count} records for evaluation. The records will now be matched against the Abstraction rules where this {DistinctSearchKey.SearchKey} is expressed and the rule is marked as a history rule (else it will be done later as a basic rule). Elapsed {sw.ElapsedMilliseconds}.");

                    var abstractionRuleMatches = new Dictionary<int, List<DictionaryNoBoxing<string>>>();

                    var resolver = new AbstractionMatchResolver(documents, CachePayloadDocument,
                        Context.Snapshot.EntityAnalysisModelLists, EntityInstanceEntryDictionaryKvPs,
                        Context.EntityAnalysisModelInstanceEntryPayload.Svirfd, Log);

                    var rulesToEvaluate = Context.Snapshot.ModelAbstractionRules
                        .FindAll(x => x.SearchKey == DistinctSearchKey.SearchKey && x.Search);

                    foreach (var evaluateAbstractionRule in rulesToEvaluate)
                    {
                        try
                        {
                            Context.TraceLog(
                                $"will process Abstraction Rule {evaluateAbstractionRule.Id}. Elapsed {sw.ElapsedMilliseconds}.");

                            var fromDate = GetFromDate(evaluateAbstractionRule);

                            var finalMatches = resolver.Resolve(evaluateAbstractionRule,
                                EntityAnalysisModel.References.ReferenceDateName, fromDate,
                                EntityAnalysisModelInstanceEntryPayload.ReferenceDate, out var matched,
                                out var reused);

                            if (reused)
                            {
                                Context.TraceLog(
                                    $"abstraction rule id {evaluateAbstractionRule.Id} reuse matches from logic cache [{matched}] for logic hash {evaluateAbstractionRule.LogicHash}. Elapsed {sw.ElapsedMilliseconds}.");
                            }
                            else
                            {
                                Context.TraceLog(
                                    $"abstraction rule id {evaluateAbstractionRule.Id} logic hash {evaluateAbstractionRule.LogicHash} run now and added to logic cache - {matched} matched. Elapsed {sw.ElapsedMilliseconds}.");
                            }

                            abstractionRuleMatches[evaluateAbstractionRule.Id] = finalMatches;

                            Context.TraceLog(
                                $"abstraction rule id {evaluateAbstractionRule.Id} has {finalMatches.Count} final matches. Elapsed {sw.ElapsedMilliseconds}.");
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            Context.TraceLog(
                                $"abstraction rule id {evaluateAbstractionRule.Id} exception {ex}. Elapsed {sw.ElapsedMilliseconds}.");
                        }
                    }

                    foreach (var kvp in abstractionRuleMatches)
                    {
                        AbstractionRuleMatches[kvp.Key] = kvp.Value;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Context.TraceLog(
                        $"has produced an error for grouping key {DistinctSearchKey.SearchKey} as {ex}. Elapsed {sw.ElapsedMilliseconds}.");
                }
                finally
                {
                    Context.TraceLog(
                        $"has concluded for grouping key {DistinctSearchKey.SearchKey}. Elapsed {sw.ElapsedMilliseconds}.");
                }

                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        private DateTime GetFromDate(EntityAnalysisModelAbstractionRule evaluateAbstractionRule)
        {
            return AbstractionWindow.FromDate(evaluateAbstractionRule.AbstractionRuleAggregationFunctionIntervalType,
                evaluateAbstractionRule.AbstractionHistoryIntervalValue, DistinctSearchKey.SearchKeyTtlInterval,
                DistinctSearchKey.SearchKeyTtlIntervalValue, EntityAnalysisModelInstanceEntryPayload.ReferenceDate);
        }
    }
}