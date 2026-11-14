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
using Jube.Data.Query;
using Jube.Data.Query.GetCaseSuppressionActiveOverrideQuery;
using Jube.Data.Query.GetCaseSuppressionOverrideStateQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Overrides;
using Jube.Dto.Repository.CaseSuppression;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions;
using Jube.Service.Exceptions.Repository.CaseSuppression;
using Jube.Service.Observability;
using Jube.Service.Query.CaseSuppressionCandidates;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseSuppression;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;
using ModelOverrideDto = Jube.Dto.EntityAnalysisModelOverride.EntityAnalysisModelOverrideDto;
using ModelOverrideService = Jube.Service.EntityAnalysisModelOverride.EntityAnalysisModelOverrideService;
using RuleOverrideDto = Jube.Dto.EntityAnalysisModelActivationRuleOverride.EntityAnalysisModelActivationRuleOverrideDto;
using RuleOverrideService =
    Jube.Service.EntityAnalysisModelActivationRuleOverride.EntityAnalysisModelActivationRuleOverrideService;
using SuppressionPoco = Jube.Data.Poco.CaseSuppression;

namespace Jube.Service.Repository.CaseSuppression
{
    public sealed class CaseSuppressionService
    {
        public const int ApprovePermission = 70;
        public const int ForcePermission = 62;
        public const int AwaitingLimit = 200;

        private static readonly int[] permissions = [1];
        private static readonly int[] requestPermissions = [1, 2];
        private static readonly int[] approvePermissions = [ApprovePermission];
        private static readonly int[] forcePermissions = [ForcePermission];
        private readonly int approvalsRequired;
        private readonly int maxDurationDays;
        private readonly int pendingLapseDays;
        private readonly ILog auditLog;
        private readonly CaseSuppressionCandidatesService candidates;
        private readonly DbContext dbContext;
        private readonly CaseSuppressionDecisionDtoValidator decisionValidator;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseSuppressionRepository repository;
        private readonly CaseSuppressionRequestDtoValidator requestValidator;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseSuppressionService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizerFactory stringLocalizerFactory, IStringLocalizer strings, int approvalsRequired,
            int maxDurationDays, int pendingLapseDays, CaseSuppressionCandidatesService candidates)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.strings = strings;
            this.approvalsRequired = approvalsRequired;
            this.maxDurationDays = maxDurationDays;
            this.pendingLapseDays = pendingLapseDays;
            this.candidates = candidates;

            repository = new CaseSuppressionRepository(dbContext, userName);
            requestValidator = new CaseSuppressionRequestDtoValidator(strings, maxDurationDays);
            decisionValidator = new CaseSuppressionDecisionDtoValidator(strings);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseSuppressionService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseSuppressionService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
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

            var candidates = await CaseSuppressionCandidatesService.CreateAsync(dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, dynamicEnvironment, auditLog, token).ConfigureAwait(false);

            return new CaseSuppressionService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, stringLocalizerFactory, strings,
                ApprovalsRequiredResolver.Resolve(dynamicEnvironment.AppSettings("ApprovalsRequired")),
                CaseSuppressionLimits.MaxDurationDays(dynamicEnvironment.AppSettings("CaseSuppressionMaxDurationDays")),
                CaseSuppressionLimits.PendingLapseDays(dynamicEnvironment.AppSettings("CaseSuppressionPendingLapseDays")),
                candidates);
        }

        [Description("Requests that a key and value be suppressed, or forced, on models and activation rules, " +
                     "from a case. Nothing changes in the engine until enough different people with the approval " +
                     "permission approve the request. The value is never taken freely: for the case key itself it " +
                     "is the case key value, and for any other override key it must be a value the case journal " +
                     "carries, proved by the archive ids of the transactions ticked as evidence, which the server " +
                     "verifies. A reason and an expiry in the future are required. Forcing needs permission 62. " +
                     "When an agent makes the request it is flagged as written by AskJooby. Not idempotent.")]
        [ServiceOperation("CaseSuppressionRequest", OperationKind.Write, Idempotent = false)]
        public async Task<CaseSuppressionDto> RequestAsync(CaseSuppressionRequestDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppression", "Request", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsureRequester("CaseSuppression.Request");

                var results = await requestValidator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    foreach (var error in results.Errors)
                    {
                        error.AttemptedValue = null;
                        error.FormattedMessagePlaceholderValues = new Dictionary<string, object>();
                    }

                    throw new DtoValidationException(results);
                }

                var scope = await candidates.LoadScopeAsync(model.CaseId, model.Merge, token).ConfigureAwait(false);
                candidates.EnsureKeyEnabled(scope, model.OverrideKey);

                var isCaseKey = string.Equals(model.OverrideKey, scope.Case.CaseKey, StringComparison.Ordinal);

                if (model.PathId == (int)CaseSuppressionPath.CaseKey && !isCaseKey)
                {
                    throw new RefusedException(strings[CaseSuppressionResources.KeyNotInJournal]);
                }

                var value = candidates.ResolveValue(scope, model.OverrideKey, model.OverrideKeyValue);
                var archiveIds = model.ArchiveIds.Distinct().ToList();

                if (model.PathId == (int)CaseSuppressionPath.Transaction && archiveIds.Count == 0)
                {
                    throw new RefusedException(strings[CaseSuppressionResources.EvidenceRequired]);
                }

                var evidence = archiveIds.Count == 0
                    ? []
                    : await candidates.VerifyAsync(scope, model.OverrideKey, value, archiveIds, token)
                        .ConfigureAwait(false);

                var expiry = model.ExpiryDate!.Value.UtcDateTime;
                var targets = await ValidateTargetsAsync(model, value, expiry, token).ConfigureAwait(false);
                var counts = await candidates.CountsAsync(scope, model.OverrideKey, value, token)
                    .ConfigureAwait(false);

                var saved = await repository.InsertAsync(new SuppressionPoco
                    {
                        CaseId = scope.Case.Id,
                        CaseKey = scope.Case.CaseKey,
                        CaseKeyValue = scope.Case.CaseKeyValue,
                        OverrideKey = model.OverrideKey,
                        OverrideKeyValue = value,
                        PathId = model.PathId,
                        Reason = HtmlSanitiser.Sanitise(model.Reason.Trim()),
                        ExpiryDate = expiry,
                        SourceId = (int)(AgentInvocationScope.Active
                            ? CaseSuppressionSource.AskJooby
                            : CaseSuppressionSource.User),
                        CountInCase = counts.CountInCase,
                        FirstSeenDate = counts.FirstSeenDate?.UtcDateTime,
                        LastSeenDate = counts.LastSeenDate?.UtcDateTime,
                        WiderCountStateId = counts.WiderCountStateId,
                        WiderTransactions = counts.WiderTransactions,
                        WiderKeys = counts.WiderKeys,
                        WiderCapped = (byte)(counts.WiderCapped ? 1 : 0)
                    }, targets,
                    evidence.Select(e => new CaseSuppressionTransaction
                    {
                        ArchiveId = e.ArchiveId, ReferenceDate = e.ReferenceDate?.UtcDateTime, LaneKey = e.LaneKey
                    }).ToList(), token).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Created();

                return (await BuildAsync([saved], false, token).ConfigureAwait(false))[0];
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        public async Task<CaseSuppressionDto> RetractAsync(CaseSuppressionDecisionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppression", "Retract", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                var current = await StartAsync(model, [1], token).ConfigureAwait(false);

                if (current.StateId != (int)CaseSuppressionState.Pending)
                {
                    throw new RefusedException(strings[CaseSuppressionResources.NotRetractable]);
                }

                RequireMaker(current);

                var saved = await InTransactionAsync(current, async locked =>
                {
                    if (locked.StateId != (int)CaseSuppressionState.Pending)
                    {
                        throw new RefusedException(strings[CaseSuppressionResources.NotRetractable]);
                    }

                    await repository.AddApprovalAsync(locked.Id, locked.Version, CaseSuppressionDecision.Retracted,
                        Clean(model!.Note), token).ConfigureAwait(false);

                    return await repository.UpdateStateAsync(locked, CaseSuppressionState.Retracted, locked.Version,
                        true, null, token).ConfigureAwait(false);
                }, token).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Updated();

                return (await BuildAsync([saved], true, token).ConfigureAwait(false))[0];
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        public async Task<CaseSuppressionDto> ResubmitAsync(CaseSuppressionDecisionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppression", "Resubmit", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                var current = await StartAsync(model, [1], token).ConfigureAwait(false);
                RequireMaker(current);
                EnsureRequester("CaseSuppression.Resubmit");

                var saved = await InTransactionAsync(current, async locked =>
                {
                    if (locked.StateId is not ((int)CaseSuppressionState.Rejected or
                        (int)CaseSuppressionState.Retracted))
                    {
                        throw new RefusedException(strings[CaseSuppressionResources.NotResubmittable]);
                    }

                    var expiry = model!.ExpiryDate?.UtcDateTime ?? locked.ExpiryDate;

                    if (expiry <= DateTime.UtcNow)
                    {
                        throw new RefusedException(strings[CaseSuppressionResources.ExpiryPassed]);
                    }

                    RequireWithinMaximum(expiry);

                    var targets = await repository.GetTargetsAsync([locked.Id], token).ConfigureAwait(false);

                    if (targets.Any(t => t.OverrideKindId == (int)EntityAnalysisModelOverrideKind.Force)
                        && !permissionValidation.Validate(forcePermissions))
                    {
                        throw new RefusedException(strings[CaseSuppressionResources.ForcePermissionRequired]);
                    }

                    var version = locked.Version + 1;

                    await repository.AddApprovalAsync(locked.Id, version, CaseSuppressionDecision.Resubmitted,
                        Clean(model.Note), token).ConfigureAwait(false);

                    return await repository.UpdateStateAsync(locked, CaseSuppressionState.Pending, version, false,
                        expiry, token).ConfigureAwait(false);
                }, token).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Updated();

                return (await BuildAsync([saved], true, token).ConfigureAwait(false))[0];
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        public async Task<CaseSuppressionDto> ApproveAsync(CaseSuppressionDecisionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppression", "Approve", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                var current = await StartAsync(model, approvePermissions, token).ConfigureAwait(false);

                RequirePendingAndNotOwn(current);

                var saved = await InTransactionAsync(current, async locked =>
                {
                    RequirePendingAndNotOwn(locked);
                    RequireLive(locked);

                    var approvals = await repository.GetApprovalsAsync([locked.Id], token).ConfigureAwait(false);

                    if (approvals.Any(a => a.SuppressionVersion == locked.Version
                                           && a.DecisionId == (int)CaseSuppressionDecision.Approved
                                           && Same(a.CreatedUser, userName)))
                    {
                        throw new RefusedException(strings[CaseSuppressionResources.AlreadyApproved]);
                    }

                    await repository.AddApprovalAsync(locked.Id, locked.Version, CaseSuppressionDecision.Approved,
                        Clean(model!.Note), token).ConfigureAwait(false);

                    var recorded = approvals.Count(a => a.SuppressionVersion == locked.Version
                                                        && a.DecisionId == (int)CaseSuppressionDecision.Approved) + 1;

                    if (recorded < approvalsRequired)
                    {
                        return locked;
                    }

                    await ApplyAsync(locked, token).ConfigureAwait(false);

                    return await repository.UpdateStateAsync(locked, CaseSuppressionState.Approved, locked.Version,
                        true, null, token).ConfigureAwait(false);
                }, token).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Updated();

                return (await BuildAsync([saved], true, token).ConfigureAwait(false))[0];
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        public async Task<CaseSuppressionDto> RejectAsync(CaseSuppressionDecisionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppression", "Reject", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                var current = await StartAsync(model, approvePermissions, token).ConfigureAwait(false);

                RequirePendingAndNotOwn(current);

                var saved = await InTransactionAsync(current, async locked =>
                {
                    RequirePendingAndNotOwn(locked);

                    await repository.AddApprovalAsync(locked.Id, locked.Version, CaseSuppressionDecision.Rejected,
                        Clean(model!.Note), token).ConfigureAwait(false);

                    return await repository.UpdateStateAsync(locked, CaseSuppressionState.Rejected, locked.Version,
                        true, null, token).ConfigureAwait(false);
                }, token).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Updated();

                return (await BuildAsync([saved], true, token).ConfigureAwait(false))[0];
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        public async Task<CaseSuppressionDto> GetByIdAsync(int id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppression", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsureAny([1, ApprovePermission], "CaseSuppression.Get");

                var current = await repository.GetByIdAsync(id, token).ConfigureAwait(false) ??
                              throw new NotFoundException(strings[CaseSuppressionResources.SuppressionNotFound]);

                op.Entity(id);

                return (await BuildAsync([current], true, token).ConfigureAwait(false))[0];
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        [Description("Lists the suppression requests made from one case, newest first, with their targets, the " +
                     "evidence transactions, every decision, the counts when the request was made, and what each " +
                     "is now: pending, approved, rejected, retracted, expired or removed. Only cases the caller " +
                     "can see. Read-only.")]
        [ServiceOperation("CaseSuppressionListByCase", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseSuppressionDto>> ListByCaseAsync(
            [Description("The case whose suppression requests are listed.")] int caseId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppression", "ListByCase", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsureAny(permissions, "CaseSuppression.ListByCase");

                var rows = await repository.GetByCaseIdAsync(caseId, token).ConfigureAwait(false);
                var dtos = await BuildAsync(rows, false, token).ConfigureAwait(false);
                op.Rows(dtos.Count);

                return dtos;
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        public async Task<List<CaseSuppressionDto>> ListAwaitingAsync(int limit, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppression", "ListAwaiting", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsureAny(approvePermissions, "CaseSuppression.ListAwaiting");

                var rows = await repository.GetAwaitingAsync(Math.Clamp(limit, 1, AwaitingLimit),
                        DateTime.UtcNow.AddDays(-pendingLapseDays), token)
                    .ConfigureAwait(false);
                var dtos = await BuildAsync(rows, false, token).ConfigureAwait(false);
                op.Rows(dtos.Count);

                return dtos;
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        public async Task<List<CaseSuppressionVersionDto>> GetVersionsAsync(int id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSuppression", "Versions", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsureAny([1, ApprovePermission], "CaseSuppression.Versions");

                _ = await repository.GetByIdAsync(id, token).ConfigureAwait(false) ??
                    throw new NotFoundException(strings[CaseSuppressionResources.SuppressionNotFound]);

                op.Entity(id);

                var versions = (await repository.GetVersionsAsync(id, token).ConfigureAwait(false)).Select(v =>
                    new CaseSuppressionVersionDto
                    {
                        Version = v.Version,
                        StateId = v.StateId,
                        Reason = v.Reason,
                        ExpiryDate = new DateTimeOffset(DateTime.SpecifyKind(v.ExpiryDate, DateTimeKind.Utc)),
                        UpdatedUser = v.UpdatedUser,
                        UpdatedDate = v.UpdatedDate.HasValue
                            ? new DateTimeOffset(DateTime.SpecifyKind(v.UpdatedDate.Value, DateTimeKind.Utc))
                            : null
                    }).ToList();

                op.Rows(versions.Count);

                return versions;
            }
            catch (Exception ex) when (Outcome(op, ex))
            {
                throw;
            }
        }

        private async Task<SuppressionPoco> StartAsync(CaseSuppressionDecisionDto? model, int[] required,
            CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(model);
            EnsureAny(required, "CaseSuppression.Decide");

            var results = await decisionValidator.ValidateAsync(model, token).ConfigureAwait(false);

            if (!results.IsValid)
            {
                foreach (var error in results.Errors)
                {
                    error.AttemptedValue = null;
                    error.FormattedMessagePlaceholderValues = new Dictionary<string, object>();
                }

                throw new DtoValidationException(results);
            }

            return await repository.GetByIdAsync(model.Id, token).ConfigureAwait(false) ??
                   throw new NotFoundException(strings[CaseSuppressionResources.SuppressionNotFound]);
        }

        private async Task<SuppressionPoco> InTransactionAsync(SuppressionPoco current,
            Func<SuppressionPoco, Task<SuppressionPoco>> change, CancellationToken token)
        {
            await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

            try
            {
                await repository.LockAsync(current.Id, token).ConfigureAwait(false);

                var locked = await repository.GetByIdAsync(current.Id, token).ConfigureAwait(false) ??
                             throw new NotFoundException(strings[CaseSuppressionResources.SuppressionNotFound]);

                var saved = await change(locked).ConfigureAwait(false);

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);

                return saved;
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(token).ConfigureAwait(false);

                throw;
            }
        }

        private async Task ApplyAsync(SuppressionPoco suppression, CancellationToken token)
        {
            if (suppression.ExpiryDate <= DateTime.UtcNow)
            {
                throw new RefusedException(strings[CaseSuppressionResources.ExpiryPassed]);
            }

            var scope = await candidates.LoadScopeAsync(suppression.CaseId, null, token).ConfigureAwait(false);
            candidates.EnsureKeyEnabled(scope, suppression.OverrideKey);

            var targets = await repository.GetTargetsAsync([suppression.Id], token).ConfigureAwait(false);
            var makerName = suppression.CreatedUser ?? string.Empty;

            ModelOverrideService modelService;
            RuleOverrideService ruleService;

            try
            {
                modelService = await ModelOverrideService.CreateAsync(dbContext, makerName, log,
                    stringLocalizerFactory, serviceChangeBus, token).ConfigureAwait(false);
                ruleService = await RuleOverrideService.CreateAsync(dbContext, makerName, log,
                    stringLocalizerFactory, serviceChangeBus, token).ConfigureAwait(false);
            }
            catch (ServiceException)
            {
                throw new RefusedException(strings[CaseSuppressionResources.MakerNoLongerPermitted]);
            }

            var query = new GetCaseSuppressionActiveOverrideQuery(dbContext);
            var expiry = new DateTimeOffset(DateTime.SpecifyKind(suppression.ExpiryDate, DateTimeKind.Utc));

            foreach (var target in targets)
            {
                var existing = await query.ExecuteAsync(target.EntityAnalysisModelGuid, suppression.OverrideKey,
                    suppression.OverrideKeyValue, target.RuleName, token).ConfigureAwait(false);

                int overrideId;

                if (existing is not null)
                {
                    if (existing.OverrideKindId != target.OverrideKindId)
                    {
                        throw new RefusedException(string.Format(
                            strings[CaseSuppressionResources.OverrideKindConflict], Describe(target)));
                    }

                    overrideId = existing.Id;
                }
                else
                {
                    overrideId = await InsertOverrideAsync(suppression, target, expiry, modelService, ruleService,
                        token).ConfigureAwait(false);
                }

                await repository.MarkTargetAppliedAsync(target.Id, overrideId, token).ConfigureAwait(false);
            }
        }

        private async Task<int> InsertOverrideAsync(SuppressionPoco suppression, CaseSuppressionTarget target,
            DateTimeOffset expiry, ModelOverrideService modelService, RuleOverrideService ruleService,
            CancellationToken token)
        {
            try
            {
                var kind = (EntityAnalysisModelOverrideKind)target.OverrideKindId;

                if (string.IsNullOrEmpty(target.RuleName))
                {
                    var modelDto = new ModelOverrideDto
                    {
                        EntityAnalysisModelGuid = target.EntityAnalysisModelGuid,
                        OverrideKey = suppression.OverrideKey,
                        OverrideKeyValue = suppression.OverrideKeyValue,
                        DeleteExpiryDate = expiry,
                        OverrideKind = kind
                    };

                    var createdModel = await modelService.InsertAsync(modelDto, token).ConfigureAwait(false);
                    await modelService.UpdateDeleteExpiryDateAsync(modelDto, token).ConfigureAwait(false);

                    return createdModel.Id;
                }

                var ruleDto = new RuleOverrideDto
                {
                    EntityAnalysisModelGuid = target.EntityAnalysisModelGuid,
                    OverrideKey = suppression.OverrideKey,
                    OverrideKeyValue = suppression.OverrideKeyValue,
                    EntityAnalysisModelActivationRuleName = target.RuleName,
                    DeleteExpiryDate = expiry,
                    OverrideKind = kind
                };

                var createdRule = await ruleService.InsertAsync(ruleDto, token).ConfigureAwait(false);
                await ruleService.UpdateDeleteExpiryDateAsync(ruleDto, token).ConfigureAwait(false);

                return createdRule.Id;
            }
            catch (ServiceException ex) when (ex.Code == "PermissionDenied")
            {
                throw new RefusedException(strings[CaseSuppressionResources.MakerNoLongerPermitted]);
            }
            catch (ServiceException ex) when (ex is not RefusedException)
            {
                throw new RefusedException(string.Format(strings[CaseSuppressionResources.ApplyFailed],
                    ex.Message));
            }
        }

        private async Task<List<CaseSuppressionTarget>> ValidateTargetsAsync(CaseSuppressionRequestDto model,
            string value, DateTime expiry, CancellationToken token)
        {
            var modelService = await ModelOverrideService.CreateAsync(dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, token).ConfigureAwait(false);
            var ruleService = await RuleOverrideService.CreateAsync(dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, token).ConfigureAwait(false);

            var targets = new List<CaseSuppressionTarget>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var offset = new DateTimeOffset(DateTime.SpecifyKind(expiry, DateTimeKind.Utc));

            foreach (var target in model.Targets)
            {
                var rule = string.IsNullOrWhiteSpace(target.RuleName) ? null : target.RuleName.Trim();

                if (!seen.Add($"{target.EntityAnalysisModelGuid}|{rule}"))
                {
                    continue;
                }

                if (target.OverrideKindId == (int)EntityAnalysisModelOverrideKind.Force
                    && !permissionValidation.Validate(forcePermissions))
                {
                    throw new RefusedException(strings[CaseSuppressionResources.ForcePermissionRequired]);
                }

                var name = await dbContext.EntityAnalysisModel
                    .Where(w => w.Guid == target.EntityAnalysisModelGuid && w.TenantRegistryId == tenantRegistryId
                                                                         && (w.Deleted == 0 || w.Deleted == null))
                    .Select(s => s.Name).FirstOrDefaultAsync(token).ConfigureAwait(false);

                if (name is null)
                {
                    throw new RefusedException(string.Format(strings[CaseSuppressionResources.TargetInvalid],
                        target.EntityAnalysisModelGuid));
                }

                var kind = (EntityAnalysisModelOverrideKind)target.OverrideKindId;

                var failures = rule is null
                    ? (await modelService.ValidateAsync(new ModelOverrideDto
                    {
                        EntityAnalysisModelGuid = target.EntityAnalysisModelGuid,
                        OverrideKey = model.OverrideKey,
                        OverrideKeyValue = value,
                        DeleteExpiryDate = offset,
                        OverrideKind = kind
                    }, token).ConfigureAwait(false)).Errors
                    : (await ruleService.ValidateAsync(new RuleOverrideDto
                    {
                        EntityAnalysisModelGuid = target.EntityAnalysisModelGuid,
                        OverrideKey = model.OverrideKey,
                        OverrideKeyValue = value,
                        EntityAnalysisModelActivationRuleName = rule,
                        DeleteExpiryDate = offset,
                        OverrideKind = kind
                    }, token).ConfigureAwait(false)).Errors;

                if (failures.Count > 0)
                {
                    throw new RefusedException(string.Format(strings[CaseSuppressionResources.TargetInvalid],
                        $"{(rule is null ? name : $"{name} / {rule}")}: {failures[0].Message}"));
                }

                targets.Add(new CaseSuppressionTarget
                {
                    EntityAnalysisModelGuid = target.EntityAnalysisModelGuid,
                    EntityAnalysisModelName = name,
                    RuleName = rule,
                    OverrideKindId = target.OverrideKindId
                });
            }

            return targets;
        }

        private async Task<List<CaseSuppressionDto>> BuildAsync(IReadOnlyList<SuppressionPoco> rows, bool includeNow,
            CancellationToken token)
        {
            if (rows.Count == 0)
            {
                return [];
            }

            var ids = rows.Select(r => r.Id).ToList();
            var targets = await repository.GetTargetsAsync(ids, token).ConfigureAwait(false);
            var transactions = await repository.GetTransactionsAsync(ids, token).ConfigureAwait(false);
            var approvals = await repository.GetApprovalsAsync(ids, token).ConfigureAwait(false);

            var applied = targets.Where(t => t.AppliedOverrideId.HasValue).ToList();

            var states = await new GetCaseSuppressionOverrideStateQuery(dbContext).ExecuteAsync(
                applied.Where(t => string.IsNullOrEmpty(t.RuleName)).Select(t => t.AppliedOverrideId!.Value)
                    .ToList(),
                applied.Where(t => !string.IsNullOrEmpty(t.RuleName)).Select(t => t.AppliedOverrideId!.Value)
                    .ToList(), token).ConfigureAwait(false);

            var canApprove = permissionValidation.Validate(approvePermissions);
            var now = DateTime.UtcNow;
            var result = new List<CaseSuppressionDto>();

            foreach (var row in rows)
            {
                var state = (CaseSuppressionState)row.StateId;
                var own = targets.Where(t => t.CaseSuppressionId == row.Id).ToList();

                var lapsed = CaseSuppressionLapse.IsLapsed(row, now, pendingLapseDays);

                var standings = own.Select(t => CaseSuppressionLapse.Apply(row,
                    CaseSuppressionStandingResolver.ResolveTarget(state, row.ExpiryDate, t, states, now), now,
                    pendingLapseDays)).ToList();

                var mine = approvals.Where(a => a.CaseSuppressionId == row.Id).ToList();
                var current = mine.Where(a => a.SuppressionVersion == row.Version
                                              && a.DecisionId == (int)CaseSuppressionDecision.Approved).ToList();
                var maker = Same(row.CreatedUser, userName);

                var dto = CaseSuppressionMapper.ToDto(row,
                    CaseSuppressionLapse.Apply(row, CaseSuppressionStandingResolver.Resolve(state, standings), now,
                        pendingLapseDays),
                    own.Select((t, i) => CaseSuppressionMapper.ToDto(t, standings[i])).ToList(),
                    transactions.Where(t => t.CaseSuppressionId == row.Id).Select(CaseSuppressionMapper.ToDto)
                        .ToList(),
                    mine.Select(CaseSuppressionMapper.ToDto).ToList(), approvalsRequired, current.Count,
                    canApprove && state == CaseSuppressionState.Pending && !lapsed && !maker
                    && !current.Any(a => Same(a.CreatedUser, userName)),
                    state == CaseSuppressionState.Pending && maker,
                    state is CaseSuppressionState.Rejected or CaseSuppressionState.Retracted && maker);

                if (includeNow)
                {
                    dto.Now = await NowAsync(row, token).ConfigureAwait(false);
                }

                result.Add(dto);
            }

            return result;
        }

        private async Task<CaseSuppressionCountsDto?> NowAsync(SuppressionPoco row, CancellationToken token)
        {
            try
            {
                var scope = await candidates.LoadScopeAsync(row.CaseId, null, token).ConfigureAwait(false);

                return await candidates.CountsAsync(scope, row.OverrideKey, row.OverrideKeyValue, token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is ServiceException or InvalidOperationException)
            {
                return null;
            }
        }

        private void RequirePendingAndNotOwn(SuppressionPoco current)
        {
            if (current.StateId != (int)CaseSuppressionState.Pending)
            {
                throw new RefusedException(strings[CaseSuppressionResources.NotPending]);
            }

            if (Same(current.CreatedUser, userName))
            {
                throw new RefusedException(strings[CaseSuppressionResources.OwnRequest]);
            }
        }

        private void RequireLive(SuppressionPoco current)
        {
            var now = DateTime.UtcNow;

            if (current.ExpiryDate <= now)
            {
                throw new RefusedException(strings[CaseSuppressionResources.ExpiryPassed]);
            }

            if (CaseSuppressionLapse.IsLapsed(current, now, pendingLapseDays))
            {
                throw new RefusedException(strings[CaseSuppressionResources.RequestLapsed]);
            }

            RequireWithinMaximum(current.ExpiryDate);
        }

        private void RequireWithinMaximum(DateTime expiry)
        {
            if (expiry > DateTime.UtcNow.AddDays(maxDurationDays))
            {
                throw new RefusedException(string.Format(strings[CaseSuppressionResources.ExpiryTooFar],
                    maxDurationDays));
            }
        }

        private static string? Clean(string? note)
        {
            return string.IsNullOrWhiteSpace(note) ? null : HtmlSanitiser.Sanitise(note.Trim());
        }

        private void RequireMaker(SuppressionPoco current)
        {
            if (!Same(current.CreatedUser, userName))
            {
                throw new RefusedException(strings[CaseSuppressionResources.NotMaker]);
            }
        }

        private static bool Same(string? left, string? right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static string Describe(CaseSuppressionTarget target)
        {
            return string.IsNullOrEmpty(target.RuleName)
                ? target.EntityAnalysisModelName ?? string.Empty
                : $"{target.EntityAnalysisModelName} / {target.RuleName}";
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
                    log.Error($"CaseSuppression: unexpected failure user={userName}", ex);
                    break;
            }

            return false;
        }

        private void EnsureRequester(string op)
        {
            if (permissionValidation.Validate([1]) && permissionValidation.Validate([2]))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName}");
            }

            throw new ForbiddenException(strings[CaseSuppressionResources.PermissionDenied], requestPermissions);
        }

        private void EnsureAny(int[] required, string op)
        {
            if (permissionValidation.Validate(required))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName}");
            }

            throw new ForbiddenException(strings[CaseSuppressionResources.PermissionDenied], required);
        }
    }
}
