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
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Data.Query.ArchiveKeyLookup;
using Jube.Data.Query.GetCaseKeyJournalRowsQuery;
using Jube.Data.Query.GetCaseSuppressionInForceQuery;
using Jube.Data.Query.GetCaseSuppressionOverrideKeysQuery;
using Jube.Data.Query.GetCaseSuppressionValuesQuery;
using Jube.Data.Query.GetCaseSuppressionWiderCountQuery;
using Jube.Data.Query.DynamicResultsSchema;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseSuppressionCandidates;
using Jube.Dto.Repository.CaseSuppression;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Repository.CaseSuppression;
using Jube.Service.Observability;
using Jube.Service.Query.ArchiveKeyFields;
using Jube.Service.Query.CaseKeyJournal;
using Jube.Service.Query.CaseSuppressionCandidates.Models;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseSuppression;
using Jube.Service.Repository.CaseSuppression.Models;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseSuppression;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseSuppressionCandidates
{
    public sealed class CaseSuppressionCandidatesService
    {
        public const int TransactionLimit = 200;
        public const int WiderValueLimit = 100;
        private const int InForceKeyLimit = 25;

        private static readonly int[] permissions = [1];
        private static readonly DateTime earliest = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly ArchiveKeyFieldResolver archiveKeys;
        private readonly ILog auditLog;
        private readonly CaseRepository caseRepository;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string reportConnectionString;
        private readonly CaseSuppressionRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseSuppressionCandidatesService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;

            caseRepository = new CaseRepository(dbContext, userName);
            repository = new CaseSuppressionRepository(dbContext, userName);
            reportConnectionString = dynamicEnvironment.AppSettings("ReportConnectionString")
                                     ?? dbContext.Connection.ConnectionString;
            archiveKeys = new ArchiveKeyFieldResolver(dbContext,
                ArchiveKeyFieldResolver.DeterministicEncryptor(dynamicEnvironment));
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseSuppressionCandidatesService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseSuppressionCandidatesService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseSuppressionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseSuppressionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseSuppressionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseSuppressionCandidatesService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment);
        }

        [Description("Lists the keys a suppression can be made on from a case: the case key of the case, with " +
                     "whether it is flagged Enable Override and so can be suppressed, then every other request " +
                     "XPath of the case workflow model that is. Read-only.")]
        public async Task<List<CaseSuppressionKeyDto>> KeysAsync(
            [Description("The case the suppression would be requested from.")] int caseId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppressionCandidates", "Keys", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("CaseSuppressionCandidates.Keys");

                var scope = await LoadScopeAsync(caseId, null, token).ConfigureAwait(false);
                var caseKey = scope.Case.CaseKey ?? string.Empty;
                var enabled = scope.EnabledKeys.Contains(caseKey, StringComparer.Ordinal);

                var keys = new List<CaseSuppressionKeyDto>
                {
                    new()
                    {
                        Name = caseKey,
                        IsCaseKey = true,
                        Enabled = enabled,
                        Reason = enabled ? null : nameof(CaseSuppressionResources.KeyNotOverrideEnabled)
                    }
                };

                keys.AddRange(scope.EnabledKeys.Where(k => !string.Equals(k, caseKey, StringComparison.Ordinal))
                    .Select(k => new CaseSuppressionKeyDto { Name = k, Enabled = true }));

                op.Rows(keys.Count);

                return keys;
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        [Description("Lists the distinct values of one override key across the whole journal of the case key " +
                     "of a case (and the merged journal keys when given), each with how many transactions in " +
                     "this case key carry it, first and last seen, and, where the archive index for the key is " +
                     "ready, how many transactions and distinct case keys carry it across the model archive. " +
                     "No filters apply. Read-only.")]
        [ServiceOperation("CaseSuppressionCandidateValues", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseSuppressionValueDto>> ValuesAsync(
            [Description("The case the suppression would be requested from.")] int caseId,
            [Description("The override key, a request XPath flagged Enable Override.")] string key,
            [Description("Optional merged journal keys as a JSON array of key and value pairs, at most three.")]
            string? merge,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppressionCandidates", "Values", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("CaseSuppressionCandidates.Values");

                var scope = await LoadScopeAsync(caseId, merge, token).ConfigureAwait(false);
                EnsureKeyEnabled(scope, key);

                var values = await ValuesForAsync(scope, key, token).ConfigureAwait(false);
                var caseKeyField = CaseKeyField(scope);

                foreach (var value in values.Take(WiderValueLimit))
                {
                    var overrideKey = archiveKeys.Resolve(Field(scope, key), value.Value);

                    if (caseKeyField is null || !overrideKey.Resolved)
                    {
                        continue;
                    }

                    var counted = await new GetCaseSuppressionWiderCountQuery(reportConnectionString, log)
                        .ExecuteAsync(scope.ModelId, userName, overrideKey.Predicate!, caseKeyField.Name,
                            caseKeyField.Column, token).ConfigureAwait(false);

                    value.WiderCountStateId = (int)CaseSuppressionWiderCount.Counted;
                    value.WiderTransactions = counted.Transactions;
                    value.WiderKeys = counted.Keys;
                    value.WiderCapped = counted.Capped;
                }

                op.Rows(values.Count);

                return values;
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        [Description("Lists the transactions in the journal of the case key of a case (and merged journal " +
                     "keys) that carry one value of an override key, newest first, at most 200 for each key: " +
                     "when, the response elevation and which visible rules fired. The archive id of a row is " +
                     "what is sent back as evidence. For the case key itself the value may be left empty. " +
                     "Read-only.")]
        [ServiceOperation("CaseSuppressionCandidateTransactions", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseSuppressionCandidateTransactionDto>> TransactionsAsync(
            [Description("The case the suppression would be requested from.")] int caseId,
            [Description("The override key.")] string key,
            [Description("The value of the key. May be empty when the key is the case key.")] string? value,
            [Description("Optional merged journal keys as a JSON array of key and value pairs, at most three.")]
            string? merge,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppressionCandidates", "Transactions", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("CaseSuppressionCandidates.Transactions");

                var scope = await LoadScopeAsync(caseId, merge, token).ConfigureAwait(false);
                EnsureKeyEnabled(scope, key);

                var resolved = ResolveValue(scope, key, value);
                var rows = new Dictionary<long, CaseSuppressionCandidateTransactionDto>();

                foreach (var lane in scope.Lanes)
                {
                    var found = await RowsAsync(scope, lane, key, resolved, null, TransactionLimit, token)
                        .ConfigureAwait(false);

                    foreach (var row in found)
                    {
                        if (rows.ContainsKey(row.Id))
                        {
                            continue;
                        }

                        var activations = JournalLensReader.VisibleActivations(ParseJson(row.Json));

                        rows[row.Id] = new CaseSuppressionCandidateTransactionDto
                        {
                            ArchiveId = row.Id,
                            ReferenceDate = row.ReferenceDate.HasValue
                                ? DynamicResultsSchema.ToUtcOffset(row.ReferenceDate.Value)
                                : null,
                            ResponseElevation = row.ResponseElevation,
                            ActivationCount = activations.Count,
                            Activations = string.Join(", ", activations),
                            LaneKey = lane.LaneKey
                        };
                    }
                }

                var result = rows.Values.OrderByDescending(r => r.ArchiveId).ToList();
                op.Rows(result.Count);

                return result;
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        [Description("Lists the overrides already in force, set from any case or on the Override page, for the " +
                     "values that appear in the journal of the case key of a case, for every override key. " +
                     "Read-only.")]
        public async Task<List<CaseSuppressionInForceDto>> InForceAsync(
            [Description("The case whose journal values are checked.")] int caseId,
            [Description("Optional merged journal keys as a JSON array of key and value pairs, at most three.")]
            string? merge,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppressionCandidates", "InForce", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("CaseSuppressionCandidates.InForce");

                var scope = await LoadScopeAsync(caseId, merge, token).ConfigureAwait(false);
                var pairs = new List<(string Key, string Value)>();

                foreach (var key in scope.EnabledKeys.Take(InForceKeyLimit))
                {
                    foreach (var value in await ValuesForAsync(scope, key, token).ConfigureAwait(false))
                    {
                        pairs.Add((key, value.Value));
                    }
                }

                var rows = await new GetCaseSuppressionInForceQuery(dbContext, tenantRegistryId)
                    .ExecuteAsync(pairs, token).ConfigureAwait(false);

                var origins = await repository.GetOriginsAsync(rows.Select(r => r.Id).Distinct().ToList(), token)
                    .ConfigureAwait(false);

                var result = rows.Select(r => new CaseSuppressionInForceDto
                {
                    Id = r.Id,
                    IsRule = r.IsRule,
                    OverrideKey = r.OverrideKey,
                    OverrideKeyValue = r.OverrideKeyValue,
                    EntityAnalysisModelName = r.EntityAnalysisModelName,
                    RuleName = r.RuleName,
                    OverrideKindId = r.OverrideKindId,
                    ExpiryDate = r.DeleteExpiryDate.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(r.DeleteExpiryDate.Value, DateTimeKind.Utc))
                        : null,
                    CreatedUser = r.CreatedUser,
                    CreatedDate = r.CreatedDate.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(r.CreatedDate.Value, DateTimeKind.Utc))
                        : null,
                    OriginCaseId = origins.FirstOrDefault(o => o.AppliedOverrideId == r.Id && o.IsRule == r.IsRule)
                        ?.CaseId
                }).ToList();

                op.Rows(result.Count);

                return result;
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        [Description("Builds the reason a suppression request starts with, from facts only: what is suppressed, " +
                     "for which key and value, how many transactions on this case carry it, when, how widely it " +
                     "is carried, and the expiry. The analyst can edit it. Never written by AI. Read-only.")]
        public async Task<CaseSuppressionReasonDraftDto> ReasonDraftAsync(
            [Description("The request being drafted. The reason itself is ignored.")] CaseSuppressionRequestDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppressionCandidates", "ReasonDraft", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseSuppressionCandidates.ReasonDraft");

                var scope = await LoadScopeAsync(model.CaseId, model.Merge, token).ConfigureAwait(false);
                EnsureKeyEnabled(scope, model.OverrideKey);

                var value = ResolveValue(scope, model.OverrideKey, model.OverrideKeyValue);
                var counts = await CountsAsync(scope, model.OverrideKey, value, token).ConfigureAwait(false);
                var targets = new List<string>();

                foreach (var target in model.Targets)
                {
                    var name = await dbContext.EntityAnalysisModel
                        .Where(w => w.Guid == target.EntityAnalysisModelGuid
                                    && w.TenantRegistryId == tenantRegistryId
                                    && (w.Deleted == 0 || w.Deleted == null))
                        .Select(s => s.Name).FirstOrDefaultAsync(token).ConfigureAwait(false);

                    if (name is null)
                    {
                        continue;
                    }

                    targets.Add(string.IsNullOrWhiteSpace(target.RuleName) ? name : $"{name} / {target.RuleName}");
                }

                var kinds = (model.Targets).Select(t => t.OverrideKindId).ToList();

                var draft = CaseSuppressionReasonBuilder.Build(new CaseSuppressionReasonFacts(
                    scope.Case.Id, scope.Case.CaseKey, scope.Case.CaseKeyValue, model.OverrideKey, value, targets,
                    kinds.Contains(1), kinds.Contains(0), counts.CountInCase, counts.FirstSeenDate?.UtcDateTime,
                    counts.LastSeenDate?.UtcDateTime, counts.WiderCountStateId == (int)CaseSuppressionWiderCount.Counted,
                    counts.WiderTransactions ?? 0, counts.WiderKeys ?? 0, counts.WiderCapped,
                    model.ExpiryDate?.UtcDateTime));

                return new CaseSuppressionReasonDraftDto { Reason = draft };
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        public async Task<CaseSuppressionScope> LoadScopeAsync(int caseId, string? merge,
            CancellationToken token = default)
        {
            var lanes = JournalMerge.Parse(merge);

            if (!JournalMerge.IsValid(lanes))
            {
                throw new DtoValidationException(new ValidationResult(
                [
                    new ValidationFailure(nameof(CaseSuppressionRequestDto.Merge),
                        strings[CaseSuppressionResources.MergeInvalid])
                    {
                        ErrorCode = "MergeInvalid"
                    }
                ]));
            }

            var existing = await caseRepository.GetByIdActiveOnlyAsync(caseId, token).ConfigureAwait(false);

            if (existing is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseSuppression: caseId={caseId} not found or not visible user={userName}");
                }

                throw new NotFoundException(strings[CaseSuppressionResources.CaseNotFound]);
            }

            var modelId = await dbContext.CaseWorkflow
                .Where(w => w.Guid == existing.CaseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (modelId is null)
            {
                throw new NotFoundException(strings[CaseSuppressionResources.CaseNotFound]);
            }

            var enabled = (await new GetCaseSuppressionOverrideKeysQuery(dbContext)
                .ExecuteAsync(modelId.Value, token).ConfigureAwait(false)).Select(k => k.Name).ToList();

            var fields = await archiveKeys.FieldsAsync(modelId.Value, token).ConfigureAwait(false);
            var requested = new List<JournalLane>
            {
                new(existing.CaseKey ?? string.Empty, existing.CaseKeyValue ?? string.Empty)
            };

            requested.AddRange(lanes ?? []);

            var predicates = await JournalLanes.ResolveAsync(archiveKeys, modelId.Value, requested,
                strings, token, nameof(CaseSuppressionRequestDto.Merge), true).ConfigureAwait(false);

            var all = requested.Select((lane, index) => new CaseSuppressionLane(lane.Key, lane.Value,
                index == 0 ? null : lane.Key, predicates[index])).ToList();

            return new CaseSuppressionScope(existing, modelId.Value, all, fields, enabled);
        }

        public void EnsureKeyEnabled(CaseSuppressionScope scope, string key)
        {
            if (!scope.EnabledKeys.Contains(key, StringComparer.Ordinal))
            {
                throw new RefusedException(strings[CaseSuppressionResources.KeyNotOverrideEnabled]);
            }

            if (!(scope.Fields.FirstOrDefault(f => string.Equals(f.Name, key, StringComparison.Ordinal))
                    ?.IsArchiveKey ?? false))
            {
                throw new RefusedException(strings[CaseSuppressionResources.KeyNotArchiveKey]);
            }
        }

        private static ArchiveKeyField Field(CaseSuppressionScope scope, string key)
        {
            return scope.Fields.First(f => string.Equals(f.Name, key, StringComparison.Ordinal));
        }

        private static ArchiveKeyField? CaseKeyField(CaseSuppressionScope scope)
        {
            return scope.Fields.FirstOrDefault(f => f.IsArchiveKey
                                                    && string.Equals(f.Name, scope.Case.CaseKey, StringComparison.Ordinal));
        }

        public string ResolveValue(CaseSuppressionScope scope, string key, string? value)
        {
            var isCaseKey = string.Equals(key, scope.Case.CaseKey, StringComparison.Ordinal);

            if (isCaseKey)
            {
                var caseValue = scope.Case.CaseKeyValue ?? string.Empty;

                if (!string.IsNullOrEmpty(value) && !string.Equals(value, caseValue, StringComparison.Ordinal))
                {
                    throw new RefusedException(strings[CaseSuppressionResources.KeyNotInJournal]);
                }

                if (caseValue.Length > CaseSuppressionRequestDtoValidator.MaxValueLength)
                {
                    throw new RefusedException(strings[CaseSuppressionResources.CaseKeyValueTooLong]);
                }

                return caseValue;
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new RefusedException(strings[CaseSuppressionResources.KeyNotInJournal]);
            }

            return value;
        }

        public async Task<CaseSuppressionCountsDto> CountsAsync(CaseSuppressionScope scope, string key, string value,
            CancellationToken token = default)
        {
            var values = await ValuesForAsync(scope, key, token).ConfigureAwait(false);
            var match = values.FirstOrDefault(v => string.Equals(v.Value, value, StringComparison.Ordinal));

            var counts = new CaseSuppressionCountsDto
            {
                CountInCase = match?.Count ?? 0,
                FirstSeenDate = match?.FirstSeenDate,
                LastSeenDate = match?.LastSeenDate,
                WiderCountStateId = (int)CaseSuppressionWiderCount.NotIndexed
            };

            var caseKeyField = CaseKeyField(scope);
            var overrideKey = archiveKeys.Resolve(Field(scope, key), value);

            if (caseKeyField is null || !overrideKey.Resolved)
            {
                return counts;
            }

            var wider = await new GetCaseSuppressionWiderCountQuery(reportConnectionString, log)
                .ExecuteAsync(scope.ModelId, userName, overrideKey.Predicate!, caseKeyField.Name, caseKeyField.Column,
                    token).ConfigureAwait(false);

            counts.WiderCountStateId = (int)CaseSuppressionWiderCount.Counted;
            counts.WiderTransactions = wider.Transactions;
            counts.WiderKeys = wider.Keys;
            counts.WiderCapped = wider.Capped;

            return counts;
        }

        public async Task<List<CaseSuppressionTransactionDto>> VerifyAsync(CaseSuppressionScope scope, string key,
            string value, IReadOnlyCollection<long> archiveIds, CancellationToken token = default)
        {
            var found = new Dictionary<long, CaseSuppressionTransactionDto>();

            foreach (var lane in scope.Lanes)
            {
                var rows = await RowsAsync(scope, lane, key, value, archiveIds, archiveIds.Count + 1, token)
                    .ConfigureAwait(false);

                foreach (var row in rows.Where(r => !found.ContainsKey(r.Id)))
                {
                    found[row.Id] = new CaseSuppressionTransactionDto
                    {
                        ArchiveId = row.Id,
                        ReferenceDate = row.ReferenceDate.HasValue
                            ? DynamicResultsSchema.ToUtcOffset(row.ReferenceDate.Value)
                            : null,
                        LaneKey = lane.LaneKey
                    };
                }
            }

            if (archiveIds.Any(id => !found.ContainsKey(id)))
            {
                throw new RefusedException(strings[CaseSuppressionResources.EvidenceNotInScope]);
            }

            return archiveIds.Select(id => found[id]).ToList();
        }

        private async Task<List<CaseSuppressionValueDto>> ValuesForAsync(CaseSuppressionScope scope, string key,
            CancellationToken token)
        {
            var query = new GetCaseSuppressionValuesQuery(reportConnectionString, log);
            var merged = new Dictionary<string, CaseSuppressionValueDto>(StringComparer.Ordinal);

            foreach (var lane in scope.Lanes)
            {
                var found = await query.ExecuteAsync(scope.ModelId, userName, lane.Predicate, key,
                    Field(scope, key).Column, token).ConfigureAwait(false);

                foreach (var row in found.Where(r => !string.IsNullOrEmpty(r.Value)))
                {
                    if (!merged.TryGetValue(row.Value, out var entry))
                    {
                        entry = new CaseSuppressionValueDto
                        {
                            Value = row.Value,
                            WiderCountStateId = (int)CaseSuppressionWiderCount.NotIndexed
                        };
                        merged[row.Value] = entry;
                    }

                    entry.Count += row.Count;
                    entry.Lanes.Add(new CaseSuppressionLaneCountDto
                    {
                        LaneKey = lane.LaneKey ?? string.Empty, LaneValue = lane.Value, Count = row.Count
                    });

                    var first = row.First.HasValue ? Utc(row.First.Value) : (DateTimeOffset?)null;
                    var last = row.Last.HasValue ? Utc(row.Last.Value) : (DateTimeOffset?)null;

                    entry.FirstSeenDate = Earlier(entry.FirstSeenDate, first);
                    entry.LastSeenDate = Later(entry.LastSeenDate, last);
                }
            }

            return merged.Values.OrderByDescending(v => v.Count).ThenByDescending(v => v.LastSeenDate)
                .ThenBy(v => v.Value, StringComparer.Ordinal).Take(GetCaseSuppressionValuesQuery.ValueCap).ToList();
        }

        private Task<List<GetCaseKeyJournalRowsQueryDto>> RowsAsync(CaseSuppressionScope scope,
            CaseSuppressionLane lane, string key, string value, IReadOnlyCollection<long>? archiveIds, int limit,
            CancellationToken token)
        {
            var overrideKey = archiveKeys.Resolve(Field(scope, key), value);

            if (!overrideKey.Resolved)
            {
                throw new RefusedException(strings[CaseSuppressionResources.KeyValueInvalid]);
            }

            string Filter(List<object> tokens)
            {
                var predicate = ArchiveKeyLookupSql.Exists(overrideKey.Predicate!, "a", ArchiveKeyLookupSql.Ordered(tokens));

                if (archiveIds is null)
                {
                    return predicate;
                }

                var placeholders = new List<string>();

                foreach (var id in archiveIds)
                {
                    tokens.Add(id);
                    placeholders.Add($"(@{tokens.Count})");
                }

                return $"{predicate} and a.\"Id\" in ({string.Join(", ", placeholders)})";
            }

            return new GetCaseKeyJournalRowsQuery(reportConnectionString, log).ExecuteAsync(scope.ModelId, userName,
                lane.Predicate, earliest, DateTime.UtcNow.AddYears(1), 0, 0, limit, token, Filter);
        }

        private static JObject ParseJson(string? json)
        {
            try
            {
                return JObject.Parse(json ?? "{}");
            }
            catch (JsonException)
            {
                return new JObject();
            }
        }

        private static DateTimeOffset Utc(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }

        private static DateTimeOffset? Earlier(DateTimeOffset? current, DateTimeOffset? next)
        {
            return current is null ? next : next is null ? current : next < current ? next : current;
        }

        private static DateTimeOffset? Later(DateTimeOffset? current, DateTimeOffset? next)
        {
            return current is null ? next : next is null ? current : next > current ? next : current;
        }

        private bool Outcome(OperationScope op, Exception ex)
        {
            switch (ex)
            {
                case ForbiddenException:
                    op.Outcome("forbidden");
                    break;
                case DtoValidationException:
                    op.Outcome("invalid");
                    break;
                case NotFoundException:
                    op.Outcome("not_found");
                    break;
                case RefusedException:
                    op.Outcome("refused");
                    break;
                case OperationCanceledException:
                    op.Outcome("cancelled");
                    break;
                default:
                    op.Error(ex);
                    log.Error($"CaseSuppressionCandidates: unexpected failure user={userName}", ex);
                    break;
            }

            return false;
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName}");
            }

            throw new ForbiddenException(strings[CaseSuppressionResources.PermissionDenied], permissions);
        }
    }
}
