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

using System.ComponentModel;
using Jube.Cache;
using Jube.Cache.Redis.Models;
using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelAbstractionRule;
using Jube.Dto.Query.EntityAnalysisModelHistorySimulation;
using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Dto.Validation;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Parser;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelHistorySimulation;
using Jube.Service.Observability;
using Jube.Service.Query.EntityAnalysisModelInvocationContext;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.EntityAnalysisModelHistorySimulation
{
    public sealed class EntityAnalysisModelHistorySimulationService
    {
        private const int SampleSize = 20;
        private const int DefaultCacheFetchLimit = 100;

        private static readonly int[] permissions = [1, 40];

        private readonly ILog auditLog;
        private readonly CacheService? cacheService;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelHistorySimulationService(DbContext dbContext, CacheService? cacheService,
            string userName, int tenantRegistryId, PermissionValidation permissionValidation, ILog log,
            ILog auditLog, IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.cacheService = cacheService;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
        }

        public static Task<EntityAnalysisModelHistorySimulationService> CreateAsync(DbContext dbContext,
            CacheService? cacheService, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, cacheService, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelHistorySimulationService> CreateAsync(DbContext dbContext,
            CacheService? cacheService, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelHistorySimulationResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelHistorySimulation.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelHistorySimulationResources
                    .NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelHistorySimulation.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelHistorySimulationResources
                    .NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelHistorySimulationService(dbContext, cacheService, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Works out the value an abstraction rule would have for the transaction in an invocation " +
                     "context, exactly as the engine does: the history for the context's search key value is " +
                     "fetched from the cache (up to the engine's fetch limit), filtered by the rule, limited to " +
                     "the window (the shorter of the rule's interval and the search key's TTL) and aggregated by " +
                     "the rule's function. Returns the value, how many transactions were fetched, matched and in " +
                     "the window, the window itself and a sample of what was counted. For a cache-backed search " +
                     "key it returns the value the engine would read from the cache. Nothing is stored.")]
        [ServiceOperation("EntityAnalysisModelHistorySimulationAbstractionAggregation", OperationKind.Read,
            Idempotent = true)]
        public async Task<AbstractionSimulationResultDto> AbstractionAggregationAsync(
            [Description("The abstraction rule to simulate; it need not be saved.")]
            EntityAnalysisModelAbstractionRuleDto? rule,
            [Description("The invocation context of the transaction, built for the rule's model.")]
            InvocationContextDto? context,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelHistorySimulation", "AbstractionAggregation",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            try
            {
                ArgumentNullException.ThrowIfNull(rule);
                ArgumentNullException.ThrowIfNull(context);
                EnsurePermitted("EntityAnalysisModelHistorySimulation.AbstractionAggregation");
                var result = await SimulateAbstractionAsync(rule, context, token).ConfigureAwait(false);
                op.Entity(rule.EntityAnalysisModelId);
                op.Rows(result.InWindow);
                return result;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelHistorySimulation.AbstractionAggregation: unexpected failure " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Reads the value a TTL counter would have for the transaction in an invocation context, " +
                     "exactly as the engine reads it: a running count from the cache, or for an online " +
                     "aggregation counter the count over its window ending at the context's reference date. " +
                     "Nothing is stored or incremented.")]
        [ServiceOperation("EntityAnalysisModelHistorySimulationTtlCounterValue", OperationKind.Read,
            Idempotent = true)]
        public async Task<TtlCounterValueResultDto> TtlCounterValueAsync(
            [Description("Id of the TTL counter.")]
            int entityAnalysisModelTtlCounterId,
            [Description("The invocation context of the transaction, built for the counter's model.")]
            InvocationContextDto? context,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelHistorySimulation", "TtlCounterValue",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            try
            {
                ArgumentNullException.ThrowIfNull(context);
                EnsurePermitted("EntityAnalysisModelHistorySimulation.TtlCounterValue");
                var result = await ReadTtlCounterAsync(entityAnalysisModelTtlCounterId, context, token)
                    .ConfigureAwait(false);
                op.Entity(entityAnalysisModelTtlCounterId);
                return result;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelHistorySimulation.TtlCounterValue: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        private async Task<AbstractionSimulationResultDto> SimulateAbstractionAsync(
            EntityAnalysisModelAbstractionRuleDto rule, InvocationContextDto context, CancellationToken token)
        {
            var result = new AbstractionSimulationResultDto { SearchKey = rule.SearchKey ?? string.Empty };
            var model = await RequireModelAsync(rule.EntityAnalysisModelId, token).ConfigureAwait(false);

            if (context.EntityAnalysisModelId != model.Id)
            {
                return Refused(result, "Context", "ContextModelMismatch",
                    strings[EntityAnalysisModelHistorySimulationResources.ContextModelMismatch]);
            }

            if (!rule.Search)
            {
                return Refused(result, "Search", "NotASearchRule",
                    strings[EntityAnalysisModelHistorySimulationResources.NotASearchRule]);
            }

            var xPaths = (await new EntityAnalysisModelRequestXPathRepository(dbContext, tenantRegistryId)
                .GetByEntityAnalysisModelIdOrderByIdAsync(model.Id, token).ConfigureAwait(false)).ToList();
            var searchKeyXPath = xPaths.FirstOrDefault(x => x.Name == rule.SearchKey && x.Active == 1);
            if (searchKeyXPath == null)
            {
                return Refused(result, "SearchKey", "SearchKeyNotFound",
                    string.Format(strings[EntityAnalysisModelHistorySimulationResources.SearchKeyNotFound],
                        rule.SearchKey));
            }

            result.SearchKeyValue = ContextValue(context, "Payload." + rule.SearchKey);
            if (result.SearchKeyValue == null)
            {
                return Refused(result, "Context", "SearchKeyValueUnset",
                    string.Format(strings[EntityAnalysisModelHistorySimulationResources.ValueUnset],
                        "Payload." + rule.SearchKey));
            }

            if (cacheService == null)
            {
                return Refused(result, "Cache", "CacheUnavailable",
                    strings[EntityAnalysisModelHistorySimulationResources.CacheUnavailable]);
            }

            var settings = AbstractionSettings.FromRecord(rule.Id, rule.Name, rule.SearchKey, rule.SearchFunctionKey,
                rule.SearchFunctionTypeId, rule.SearchInterval, rule.SearchValue, rule.Offset, rule.OffsetTypeId,
                rule.OffsetValue);

            if (searchKeyXPath.SearchKeyCache == 1)
            {
                result.CacheBacked = true;
                var cached = await cacheService.CacheAbstractionRepository.GetAsync(tenantRegistryId, model.Guid,
                [
                    new EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue
                    {
                        AbstractionRuleName = settings.Name, SearchKey = settings.SearchKey,
                        SearchValue = result.SearchKeyValue
                    }
                ]).ConfigureAwait(false);
                result.Simulated = true;
                result.Value = InvocationContextBuilder.FormatValue(
                    cached.TryGetValue(settings.Name, out var value) ? value : 0d);
                return result;
            }

            var environment = await new GetRuleParseEnvironmentQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(model.Id, RuleParse.AbstractionRule, token).ConfigureAwait(false);
            var ruleText = rule.RuleScriptTypeId == 1 ? rule.BuilderRuleScript : rule.CoderRuleScript;
            var parsed = RuleParse.Execute(ruleText ?? string.Empty, RuleParse.AbstractionRule, environment, log,
                RuleParse.EngineReferences(RuleParse.AbstractionRule), false, rule.RuleScriptTypeId == 1, true);
            if (!parsed.Compiled)
            {
                result.Errors = parsed.ErrorSpans.Count == 0
                    ?
                    [
                        Error(rule.RuleScriptTypeId == 1 ? "BuilderRuleScript" : "CoderRuleScript",
                            "RuleScriptInvalid", parsed.Message ?? string.Empty)
                    ]
                    : parsed.ErrorSpans.Select(s => new ValidationErrorDto
                    {
                        PropertyName = rule.RuleScriptTypeId == 1 ? "BuilderRuleScript" : "CoderRuleScript",
                        ErrorCode = "RuleScriptInvalid", Message = s.Message, Line = s.Line, Start = s.Start,
                        Length = s.Length
                    }).ToList();
                return result;
            }

            result.FetchLimit = Math.Min(model.CacheFetchLimit ?? DefaultCacheFetchLimit,
                searchKeyXPath.SearchKeyFetchLimit ?? DefaultCacheFetchLimit);
            var exclude = context.EntityAnalysisModelInstanceEntryGuid ?? Guid.Empty;
            var keys = await cacheService.CachePayloadRepository.GetSortedSetKeysAsync(tenantRegistryId, model.Guid,
                settings.SearchKey, result.SearchKeyValue, result.FetchLimit, exclude).ConfigureAwait(false);
            var payloads = await cacheService.CachePayloadRepository.GetPayloadBatchAsync(tenantRegistryId,
                model.Guid, keys.Distinct().ToList(), exclude).ConfigureAwait(false);
            var parseIndex = xPaths.Where(x => x.Cache == 1 && x.CacheIndexId.HasValue)
                .GroupBy(x => x.CacheIndexId!.Value)
                .ToDictionary(g => g.Key, g => g.First().Name);
            var referenceDateName = model.ReferenceDateName ?? string.Empty;
            var decoded = CachedPayloadDecoder.Decode(payloads, parseIndex, referenceDateName);
            var history = keys.Select(k => decoded.GetValueOrDefault(k.ToString()))
                .Where(d => d != null)
                .ToList();

            result.DocumentsFetched = history.Count;
            result.FetchLimitReached = keys.Count >= result.FetchLimit;

            var lists = await new GetModelListsQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(model.Id, token).ConfigureAwait(false);
            var current = RuleRunner.ToInputs(EntityAnalysisModelInvocationContextMapper.ToContext(context), lists);
            var simulation = await AbstractionAggregationSimulator.SimulateAsync(parsed, settings,
                    new SearchKeySettings(settings.SearchKey, searchKeyXPath.SearchKeyTtlInterval ?? "d",
                        searchKeyXPath.SearchKeyTtlIntervalValue ?? 0, result.FetchLimit, false),
                    current, history, context.ReferenceDate ?? DateTime.UtcNow, referenceDateName, token)
                .ConfigureAwait(false);

            result.Simulated = simulation.Error == null;
            result.DocumentsEvaluated = simulation.DocumentsEvaluated;
            result.Matched = simulation.Matched;
            result.InWindow = simulation.InWindow;
            result.WindowFrom = simulation.WindowFrom;
            result.WindowTo = simulation.WindowTo;
            result.WindowShortenedBySearchKey = simulation.WindowShortenedBySearchKey;
            result.Value = InvocationContextBuilder.FormatValue(simulation.Value);
            result.RuntimeError = simulation.Error == null
                ? null
                : $"{simulation.Error.GetType().Name}: {simulation.Error.Message}";
            result.Sample = simulation.MatchedInWindow.Take(SampleSize)
                .Select(d => d.ToDictionary(kv => kv.Key, kv => (string?)kv.Value.AsString()))
                .ToList();
            return result;
        }

        private async Task<TtlCounterValueResultDto> ReadTtlCounterAsync(int entityAnalysisModelTtlCounterId,
            InvocationContextDto context, CancellationToken token)
        {
            var counter = await new EntityAnalysisModelTtlCounterRepository(dbContext, tenantRegistryId)
                .GetByIdAsync(entityAnalysisModelTtlCounterId, token).ConfigureAwait(false);
            if (counter?.EntityAnalysisModelId == null)
            {
                throw new NotFoundException(strings[EntityAnalysisModelHistorySimulationResources.TtlCounterNotFound]);
            }

            var model = await RequireModelAsync(counter.EntityAnalysisModelId.Value, token).ConfigureAwait(false);
            var result = new TtlCounterValueResultDto
            {
                Name = counter.Name ?? string.Empty,
                DataName = counter.TtlCounterDataName ?? string.Empty,
                OnlineAggregation = counter.OnlineAggregation == 1
            };

            if (context.EntityAnalysisModelId != model.Id)
            {
                result.Errors =
                [
                    Error("Context", "ContextModelMismatch",
                        strings[EntityAnalysisModelHistorySimulationResources.ContextModelMismatch])
                ];
                return result;
            }

            if (model.EnableTtlCounter != 1)
            {
                result.Errors =
                [
                    Error("EnableTtlCounter", "TtlCountersDisabled",
                        strings[EntityAnalysisModelHistorySimulationResources.TtlCountersDisabled])
                ];
                return result;
            }

            result.DataValue = ContextValue(context, "Payload." + counter.TtlCounterDataName);
            if (result.DataValue == null)
            {
                result.Errors =
                [
                    Error("Context", "DataValueUnset",
                        string.Format(strings[EntityAnalysisModelHistorySimulationResources.ValueUnset],
                            "Payload." + counter.TtlCounterDataName))
                ];
                return result;
            }

            if (cacheService == null)
            {
                result.Errors =
                [
                    Error("Cache", "CacheUnavailable",
                        strings[EntityAnalysisModelHistorySimulationResources.CacheUnavailable])
                ];
                return result;
            }

            double value;
            if (result.OnlineAggregation)
            {
                var referenceDate = context.ReferenceDate ?? DateTime.UtcNow;
                result.WindowFrom = TtlCounterExtensions.ApplyTtlCounterInterval(referenceDate,
                    counter.TtlCounterInterval, counter.TtlCounterValue ?? 0);
                value = await cacheService.CacheTtlCounterEntryRepository.GetAggregationPreferReplicaAsync(
                        tenantRegistryId, model.Guid, counter.Guid, result.DataName, result.DataValue,
                        result.WindowFrom.Value, referenceDate)
                    .ConfigureAwait(false);
            }
            else
            {
                value = await cacheService.CacheTtlCounterRepository.GetByNameDataNameDataValueAsync(
                        tenantRegistryId, model.Guid, counter.Guid, result.DataName, result.DataValue)
                    .ConfigureAwait(false);
            }

            result.Read = true;
            result.Value = InvocationContextBuilder.FormatValue(value);
            return result;
        }

        private static string? ContextValue(InvocationContextDto context, string name)
        {
            return context.Values.FirstOrDefault(v => v.Name == name)?.Value;
        }

        private async Task<Data.Poco.EntityAnalysisModel> RequireModelAsync(int entityAnalysisModelId,
            CancellationToken token)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, tenantRegistryId)
                .GetByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            if (model == null || model.TenantRegistryId != tenantRegistryId)
            {
                throw new NotFoundException(strings[EntityAnalysisModelHistorySimulationResources.ModelNotFound]);
            }

            return model;
        }

        private static AbstractionSimulationResultDto Refused(AbstractionSimulationResultDto result,
            string propertyName, string errorCode, string message)
        {
            result.Errors = [Error(propertyName, errorCode, message)];
            return result;
        }

        private static ValidationErrorDto Error(string propertyName, string errorCode, string message)
        {
            return new ValidationErrorDto { PropertyName = propertyName, ErrorCode = errorCode, Message = message };
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[EntityAnalysisModelHistorySimulationResources.PermissionDenied],
                permissions);
        }
    }
}