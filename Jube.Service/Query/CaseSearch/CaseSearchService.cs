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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetCaseRiskMatrixQuery;
using Jube.Data.Query.GetCaseTriageCountQuery;
using Jube.Data.Query.GetCaseTriageEnrichmentQuery;
using Jube.Data.Query.GetCaseTriagePageQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseSearch;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseSearch;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.SessionCaseSearchCompiledSql;
using Jube.Service.Security;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Jube.Service.Query.CaseSearch
{
    public sealed class CaseSearchService
    {
        private const string EmptyRules = "{\"condition\":\"AND\",\"rules\":[]}";
        private const int MaxTake = 200;
        private const int SkimCandidates = 6;
        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly CaseSearchFilterRepository caseSearchFilterRepository;
        private readonly CaseWorkflowFilterRepository caseWorkflowFilterRepository;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string reportConnectionString;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly SessionCaseSearchCompiledSqlRepository sessionRepository;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseSearchService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            caseSearchFilterRepository = new CaseSearchFilterRepository(dbContext, userName, tenantRegistryId);
            caseWorkflowFilterRepository = new CaseWorkflowFilterRepository(dbContext, userName);
            sessionRepository = new SessionCaseSearchCompiledSqlRepository(dbContext, userName);

            reportConnectionString = dynamicEnvironment.AppSettings("ReportConnectionString")
                                     ?? dbContext.Connection.ConnectionString;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseSearchService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseSearchService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseSearchResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseSearchResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseSearchResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseSearchService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings, dynamicEnvironment);
        }

        [Description("Lists the cases of a Case Workflow for triage, one page at a time. Each case carries its latest " +
                     "risk (lane, likelihood, impact, Priority and the top driver), why it needs the caller again " +
                     "when it does (reopened, escalated, moved or changed by someone else, diary due, risk rose), " +
                     "and where it stands against its service levels. The default order puts live money and " +
                     "mandatory cases first, then cases returned to the caller, then everything else by lane and " +
                     "Priority. Only cases the caller's roles may see are listed.")]
        [ServiceOperation("CaseSearchRun", OperationKind.Read, Idempotent = true)]
        public async Task<CaseSearchPageDto> RunAsync(CaseSearchRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSearch", "Run", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var take = request.Take;

                if (take is < 1 or > MaxTake)
                {
                    throw new DtoValidationException(new FluentValidation.Results.ValidationResult(
                        [new FluentValidation.Results.ValidationFailure(nameof(request.Take),
                            strings[CaseSearchResources.TakeInvalid]) { ErrorCode = "TakeInvalid" }]));
                }

                var scope = await ResolveAsync(request, token).ConfigureAwait(false);
                var skip = Math.Max(0, request.Skip);

                var page = await new GetCaseTriagePageQuery(reportConnectionString, log).ExecuteAsync(scope.WhereSql,
                    scope.Tokens, scope.ExtraCondition, userName, request.Sort, take, skip, token)
                    .ConfigureAwait(false);

                var enrichment = await new GetCaseTriageEnrichmentQuery(reportConnectionString, log)
                    .ExecuteAsync(page.Rows.Select(r => r.CaseId).ToList(), token).ConfigureAwait(false);

                var now = DateTime.UtcNow;

                op.Rows(page.Rows.Count);

                return new CaseSearchPageDto
                {
                    Total = page.Total,
                    Take = take,
                    Skip = skip,
                    Sort = (int)request.Sort,
                    GeneratedAt = new DateTimeOffset(now, TimeSpan.Zero),
                    Rows = page.Rows.Select(r =>
                        CaseSearchMapper.ToDto(r, enrichment.GetValueOrDefault(r.CaseId), now)).ToList()
                };
            }
            catch (Exception ex) when (Classify(op, ex))
            {
                throw;
            }
        }

        [Description("Counts the cases behind every entry of the Cases page rail for a Case Workflow: the built-in " +
                     "views (my cases, returned to me, free for all, urgent, overdue, all open), the Case Workflow " +
                     "Filters an administrator defined that the caller's role is granted, and the searches the " +
                     "caller saved or that are shared with their role.")]
        [ServiceOperation("CaseSearchCounts", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseSearchCountDto>> CountsAsync(
            [Description("Guid of the Case Workflow.")]
            Guid caseWorkflowGuid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSearch", "Counts", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();
                await EnsureWorkflowAsync(caseWorkflowGuid, token).ConfigureAwait(false);

                var counts = new List<CaseSearchCountDto>();

                var everything = await CompileScopeAsync(caseWorkflowGuid, EmptyRules, EmptyRules, string.Empty, token)
                    .ConfigureAwait(false);

                foreach (var view in Enum.GetValues<CaseSearchView>())
                {
                    var scope = everything with { ExtraCondition = CaseSearchViewSql.Condition(view) };

                    counts.Add(new CaseSearchCountDto
                    {
                        Kind = "View",
                        Key = ((int)view).ToString(),
                        Name = EnumText.Of<CaseSearchView>((int)view)!,
                        Count = await CountAsync(scope, token).ConfigureAwait(false)
                    });
                }

                foreach (var filter in await caseWorkflowFilterRepository
                             .GetByCasesWorkflowGuidActiveOnlyAsync(caseWorkflowGuid, token).ConfigureAwait(false))
                {
                    counts.Add(await CountFilterAsync("Workflow", filter.Guid, filter.Name, false, caseWorkflowGuid,
                        filter.SelectJson, filter.FilterJson, token).ConfigureAwait(false));
                }

                foreach (var filter in await caseSearchFilterRepository.ListVisibleAsync(caseWorkflowGuid, token)
                             .ConfigureAwait(false))
                {
                    counts.Add(await CountFilterAsync("Own", filter.Guid, filter.Name,
                        filter.CreatedUser == userName, caseWorkflowGuid, filter.SelectJson, filter.FilterJson, token)
                        .ConfigureAwait(false));
                }

                op.Rows(counts.Count);

                return counts;
            }
            catch (Exception ex) when (Classify(op, ex))
            {
                throw;
            }
        }

        [Description("Returns the likelihood by impact grid of the cases a search matches, counting cases not yet " +
                     "assessed under band zero. It shows where the load sits: many Possible and Low cases suggest a " +
                     "noisy rule, and any Very likely and Severe case not yet in progress is a problem.")]
        [ServiceOperation("CaseSearchRiskMatrix", OperationKind.Read, Idempotent = true)]
        public async Task<CaseRiskMatrixDto> RiskMatrixAsync(CaseSearchRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSearch", "RiskMatrix", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var scope = await ResolveAsync(request, token).ConfigureAwait(false);

                var matrix = await new GetCaseRiskMatrixQuery(reportConnectionString, log).ExecuteAsync(
                    scope.WhereSql, scope.Tokens, scope.ExtraCondition, userName, token).ConfigureAwait(false);

                op.Rows(matrix.Cells.Count);

                return new CaseRiskMatrixDto
                {
                    Total = matrix.Cells.Sum(c => c.Cases),
                    Cells = matrix.Cells.Select(c => new CaseRiskMatrixCellDto
                    {
                        LikelihoodBand = c.LikelihoodBandId,
                        Impact = c.ImpactId,
                        Cases = c.Cases
                    }).OrderBy(c => c.LikelihoodBand).ThenBy(c => c.Impact).ToList()
                };
            }
            catch (Exception ex) when (Classify(op, ex))
            {
                throw;
            }
        }

        [Description("Takes the next case for the caller: the first, in triage order, that is open and either " +
                     "unlocked or already locked to the caller. SIDE EFFECTS: locks that case to the caller, which " +
                     "assigns it, records a skim event against it and records that the caller has now looked at it. " +
                     "Returns the case taken, the identifiers of the next cases to prefetch and how many remain. " +
                     "Returned cases come first.")]
        [ServiceOperation("CaseSearchSkim", OperationKind.Write, Idempotent = false)]
        public async Task<CaseSkimDto> SkimAsync(CaseSearchRequestDto? request, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSearch", "Skim", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var scope = await ResolveAsync(request, token).ConfigureAwait(false);
                var extra = Combine(scope.ExtraCondition, CaseSearchViewSql.Skimmable);

                var page = await new GetCaseTriagePageQuery(reportConnectionString, log).ExecuteAsync(scope.WhereSql,
                    scope.Tokens, extra, userName, CaseSearchSort.Triage, SkimCandidates, 0, token)
                    .ConfigureAwait(false);

                foreach (var candidate in page.Rows)
                {
                    var locked = await dbContext.Case.Where(w => w.Id == candidate.CaseId
                                                                 && (w.Locked == 0 || w.Locked == null
                                                                     || w.LockedUser == userName)
                                                                 && (w.ClosedStatusId == 0 || w.ClosedStatusId == 1))
                        .Set(s => s.Locked, (byte)1)
                        .Set(s => s.LockedUser, userName)
                        .Set(s => s.LockedDate, DateTime.UtcNow)
                        .UpdateAsync(token).ConfigureAwait(false);

                    if (locked == 0)
                    {
                        continue;
                    }

                    await new CaseEventRepository(dbContext, userName).InsertAsync(new CaseEvent
                    {
                        CaseId = candidate.CaseId,
                        CaseEventTypeId = 2,
                        CaseKey = candidate.CaseKey,
                        CaseKeyValue = candidate.CaseKeyValue
                    }, token).ConfigureAwait(false);

                    await CaseViewRecorder.RecordAsync(dbContext, userName, candidate.CaseId, token)
                        .ConfigureAwait(false);

                    var remaining = await new GetCaseTriageCountQuery(reportConnectionString, log).ExecuteAsync(
                        scope.WhereSql, scope.Tokens, extra, userName, token).ConfigureAwait(false);

                    candidate.Locked = true;
                    candidate.LockedUser = userName;
                    candidate.Mine = true;

                    var enrichment = await new GetCaseTriageEnrichmentQuery(reportConnectionString, log)
                        .ExecuteAsync([candidate.CaseId], token).ConfigureAwait(false);

                    op.Entity(candidate.CaseId);
                    op.Rows(1);

                    return new CaseSkimDto
                    {
                        Case = CaseSearchMapper.ToDto(candidate, enrichment.GetValueOrDefault(candidate.CaseId),
                            DateTime.UtcNow),
                        NextCaseIds = page.Rows.Where(r => r.CaseId != candidate.CaseId).Select(r => r.CaseId)
                            .Take(SkimCandidates - 1).ToList(),
                        Remaining = remaining
                    };
                }

                op.Rows(0);

                return new CaseSkimDto { Case = null, Remaining = 0 };
            }
            catch (Exception ex) when (Classify(op, ex))
            {
                throw;
            }
        }

        public async Task<List<CaseWorkflowChoiceDto>> WorkflowsAsync(CancellationToken token = default)
        {
            EnsurePermitted();

            var rows = await caseSearchFilterRepository.ListAccessibleWorkflowsAsync(token).ConfigureAwait(false);

            return rows.Select(r => new CaseWorkflowChoiceDto { Guid = r.Guid, Name = r.Name, ModelName = r.ModelName })
                .ToList();
        }

        internal async Task<CaseSearchRowDto?> GetRowAsync(Guid caseWorkflowGuid, int caseId,
            CancellationToken token = default)
        {
            var everything = await CompileScopeAsync(caseWorkflowGuid, EmptyRules, EmptyRules,
                "\"Case\".\"Id\" = " + caseId, token).ConfigureAwait(false);

            var page = await new GetCaseTriagePageQuery(reportConnectionString, log).ExecuteAsync(everything.WhereSql,
                everything.Tokens, everything.ExtraCondition, userName, CaseSearchSort.Triage, 1, 0, token)
                .ConfigureAwait(false);

            var row = page.Rows.FirstOrDefault();

            if (row == null)
            {
                return null;
            }

            var enrichment = await new GetCaseTriageEnrichmentQuery(reportConnectionString, log)
                .ExecuteAsync([caseId], token).ConfigureAwait(false);

            return CaseSearchMapper.ToDto(row, enrichment.GetValueOrDefault(caseId), DateTime.UtcNow);
        }

        internal async Task<(string WhereSql, List<object> Tokens)> EverythingAsync(Guid caseWorkflowGuid,
            CancellationToken token = default)
        {
            await EnsureWorkflowAsync(caseWorkflowGuid, token).ConfigureAwait(false);

            var scope = await CompileScopeAsync(caseWorkflowGuid, EmptyRules, EmptyRules, string.Empty, token)
                .ConfigureAwait(false);

            return (scope.WhereSql, scope.Tokens);
        }

        private async Task<CaseSearchCountDto> CountFilterAsync(string kind, Guid guid, string name, bool mine,
            Guid caseWorkflowGuid, string selectJson, string filterJson, CancellationToken token)
        {
            var dto = new CaseSearchCountDto { Kind = kind, Key = guid.ToString(), Name = name, Mine = mine };

            try
            {
                var scope = await CompileScopeAsync(caseWorkflowGuid, selectJson, filterJson, string.Empty, token)
                    .ConfigureAwait(false);

                dto.Count = await CountAsync(scope, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                dto.Failed = true;

                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseSearch.Counts: {kind} entry {guid} could not be counted user={userName}: {ex.Message}");
                }
            }

            return dto;
        }

        private Task<long> CountAsync(CaseSearchScope scope, CancellationToken token) =>
            new GetCaseTriageCountQuery(reportConnectionString, log).ExecuteAsync(scope.WhereSql, scope.Tokens,
                scope.ExtraCondition, userName, token);

        private async Task<CaseSearchScope> ViewScopeAsync(Guid caseWorkflowGuid, CaseSearchView view,
            CancellationToken token) =>
            await CompileScopeAsync(caseWorkflowGuid, EmptyRules, EmptyRules, CaseSearchViewSql.Condition(view), token)
                .ConfigureAwait(false);

        private async Task<CaseSearchScope> CompileScopeAsync(Guid caseWorkflowGuid, string? selectJson,
            string? filterJson, string extraCondition, CancellationToken token)
        {
            var compiled = await SessionCaseSearchCompiler.CompileAsync(dbContext, caseWorkflowGuid, null, selectJson,
                filterJson, userName, log, reportConnectionString, token, false, false).ConfigureAwait(false);

            return new CaseSearchScope(compiled.WhereSql,
                JsonConvert.DeserializeObject<List<object>>(compiled.FilterTokens ?? "[]") ?? [], extraCondition);
        }

        private async Task<CaseSearchScope> ResolveAsync(CaseSearchRequestDto request, CancellationToken token)
        {
            var chosen = new object?[] { request.View, request.CaseWorkflowFilterGuid, request.CaseSearchFilterGuid,
                request.SessionGuid }.Count(c => c != null);

            if (chosen > 1)
            {
                throw new DtoValidationException(new FluentValidation.Results.ValidationResult(
                [
                    new FluentValidation.Results.ValidationFailure(nameof(request.View),
                        strings[CaseSearchResources.ScopeInvalid]) { ErrorCode = "ScopeInvalid" }
                ]));
            }

            await EnsureWorkflowAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false);

            if (request.SessionGuid.HasValue)
            {
                var session = sessionRepository.GetByGuid(request.SessionGuid.Value);

                if (session == null || session.CaseWorkflowGuid != request.CaseWorkflowGuid)
                {
                    throw new NotFoundException(strings[CaseSearchResources.NotFound]);
                }

                return new CaseSearchScope(session.WhereSql,
                    JsonConvert.DeserializeObject<List<object>>(session.FilterTokens ?? "[]") ?? [], string.Empty);
            }

            if (request.CaseSearchFilterGuid.HasValue)
            {
                var saved = await caseSearchFilterRepository
                    .GetVisibleByGuidAsync(request.CaseSearchFilterGuid.Value, token).ConfigureAwait(false);

                if (saved == null || saved.CaseWorkflow.Guid != request.CaseWorkflowGuid)
                {
                    throw new NotFoundException(strings[CaseSearchResources.NotFound]);
                }

                return await CompileScopeAsync(request.CaseWorkflowGuid, saved.SelectJson, saved.FilterJson,
                    string.Empty, token).ConfigureAwait(false);
            }

            if (request.CaseWorkflowFilterGuid.HasValue)
            {
                var filter = (await caseWorkflowFilterRepository
                        .GetByCasesWorkflowGuidActiveOnlyAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false))
                    .FirstOrDefault(f => f.Guid == request.CaseWorkflowFilterGuid.Value);

                if (filter == null)
                {
                    throw new NotFoundException(strings[CaseSearchResources.NotFound]);
                }

                return await CompileScopeAsync(request.CaseWorkflowGuid, filter.SelectJson, filter.FilterJson,
                    string.Empty, token).ConfigureAwait(false);
            }

            return await ViewScopeAsync(request.CaseWorkflowGuid, request.View ?? CaseSearchView.All, token)
                .ConfigureAwait(false);
        }

        private async Task EnsureWorkflowAsync(Guid caseWorkflowGuid, CancellationToken token)
        {
            if (await caseSearchFilterRepository.GetAccessibleCaseWorkflowIdAsync(caseWorkflowGuid, token)
                    .ConfigureAwait(false) == null)
            {
                throw new NotFoundException(strings[CaseSearchResources.WorkflowNotFound]);
            }
        }

        private static string Combine(string first, string second) =>
            string.IsNullOrWhiteSpace(first) ? second : "(" + first + ") and (" + second + ")";

        private bool Classify(OperationScope op, Exception ex)
        {
            switch (ex)
            {
                case ForbiddenException:
                    op.Outcome("forbidden");
                    break;
                case NotFoundException:
                    op.Outcome("not_found");
                    break;
                case DtoValidationException:
                    op.Outcome("invalid");
                    break;
                case OperationCanceledException:
                    op.Outcome("cancelled");
                    break;
                default:
                    op.Error(ex);
                    log.Error($"CaseSearch: unexpected failure user={userName}", ex);
                    break;
            }

            return false;
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            throw new ForbiddenException(strings[CaseSearchResources.PermissionDenied], permissions);
        }
    }
}
