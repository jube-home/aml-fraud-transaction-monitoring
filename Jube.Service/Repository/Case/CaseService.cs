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

using Jube.Data.Concurrency;
using System.ComponentModel;
using System.Globalization;
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.MarkCaseRiskRecheckQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.Case;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.CaseDispatch;
using Jube.Service.CaseDispatch.Models;
using Jube.Service.Exceptions.Repository.Case;
using Jube.Service.Observability;
using Jube.Service.Query.ArchiveKeyFields;
using Jube.Service.Query.CaseChecklist;
using Jube.Service.Query.CaseChecklist.Interfaces;
using Jube.Service.Query.CaseSupervision;
using Jube.Service.Repository.CaseWorkflowFormValidator;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.Case;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EngineCaseProcessing = Jube.Engine.BackgroundTasks.TaskStarters.Case.CaseProcessing;
using EngineCreateCase = Jube.Engine.EntityAnalysisModelInvoke.Models.CaseManagement.CreateCase;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using RulePoco = Jube.Data.Poco.Case;
using RuleRepository = Jube.Data.Repository.CaseRepository;

namespace Jube.Service.Repository.Case
{
    using Jube.Data.Query.GetExistingCasePriorityQuery;
    using Jube.Data.Query.GetLastArchiveJsonByEntityAnalysisModelIdAndCaseKeyValueQuery;
    using EntryPayload = EntityAnalysisModelInstanceEntryPayload;
    using EntryPayloadExtensions =
        EntityAnalysisModelInstanceEntryPayloadExtensions;

    public sealed class CaseService
    {
        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly CaseDispatcher dispatcher;
        private readonly CaseEventRepository caseEventRepository;
        private readonly Func<DbContext, ICaseChecklistReconciler>? checklistReconcilerFactory;
        private readonly CaseWorkflowRepository caseWorkflowRepository;
        private readonly CaseWorkflowStatusRepository caseWorkflowStatusRepository;
        private readonly CreateCaseValidator createCaseValidator;
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly GetExistingCasePriorityQuery existingCasePriorityQuery;
        private readonly EngineJsonSerializationHelper jsonSerializationHelper;
        private readonly GetLastArchiveJsonByEntityAnalysisModelIdAndCaseKeyValueQuery lastArchiveJsonQuery;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseDtoValidator validator;

        private CaseService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper,
            Func<DbContext, ICaseChecklistReconciler>? checklistReconcilerFactory)
        {
            this.checklistReconcilerFactory = checklistReconcilerFactory;
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.dynamicEnvironment = dynamicEnvironment;
            this.jsonSerializationHelper = jsonSerializationHelper;
            repository = new RuleRepository(dbContext, userName);
            dispatcher = new CaseDispatcher(dbContext, userName, tenantRegistryId, dynamicEnvironment, log);
            caseEventRepository = new CaseEventRepository(dbContext, userName);
            caseWorkflowRepository = new CaseWorkflowRepository(dbContext, userName);
            caseWorkflowStatusRepository = new CaseWorkflowStatusRepository(dbContext, userName);
            existingCasePriorityQuery = new GetExistingCasePriorityQuery(dbContext);
            lastArchiveJsonQuery = new GetLastArchiveJsonByEntityAnalysisModelIdAndCaseKeyValueQuery(dbContext);
            validator = new CaseDtoValidator(strings);
            createCaseValidator = new CreateCaseValidator(strings);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                jsonSerializationHelper, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static Task<CaseService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, ILog auditLog,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                jsonSerializationHelper, auditLog, null, token);
        }

        internal static async Task<CaseService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, ILog auditLog,
            Func<DbContext, ICaseChecklistReconciler>? checklistReconcilerFactory,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("Case.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Case.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation, log,
                auditLog, serviceChangeBus, strings, dynamicEnvironment, jsonSerializationHelper,
                checklistReconcilerFactory);
        }

        [Description("Lists every active case visible to the caller's tenant. Unbounded -- prefer ListAsync for " +
                     "agent/tool use.")]
        public async Task<List<CaseDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("Case", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"Case.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("Case.List");
                var dtos = CaseMapper.ToDto(await repository.GetAsyncActiveOnlyAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"Case.List: {dtos.Count} rows user={userName}");
                }

                return dtos;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");

                if (log.IsDebugEnabled)
                {
                    log.Debug($"Case.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"Case.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active cases visible to the caller's tenant, Id-ascending, capped at 'take' rows. " +
                     "Call again with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("CaseList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<CaseDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Case", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"Case.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("Case.List");
                var clampedTake = Math.Clamp(take, 1, 200);

                var all = CaseMapper.ToDto(await repository.GetAsyncActiveOnlyAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();

                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<CaseDto>(page);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
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
                log.Error($"Case.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single active case by Id, visible only if the caller's tenant and role can see " +
                     "its case workflow status. Returns null if not found or not visible.")]
        [ServiceOperation("CaseGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseDto?> GetByIdAsync(
            [Description("Identifier of the case.")]
            int id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("Case", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"Case.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("Case.Get");
                var dto = CaseMapper.ToDto(await repository.GetByIdActiveOnlyAsync(id, token).ConfigureAwait(false));

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (dto is not null)
                {
                    op.Entity(dto.Id);
                }

                if (log.IsDebugEnabled)
                {
                    log.Debug($"Case.Get: id={id} found={dto is not null} user={userName}");
                }

                return dto;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
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
                log.Error($"Case.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Inserts a case row directly from a full CaseDto. Legacy, low-level entry point kept for " +
                     "HTTP-contract parity -- CreateFromCaseKeyValueAsync is the real, supported way to raise a " +
                     "case, and this action has no known caller. Not registered as an agent tool.")]
        public async Task<RulePoco> InsertAsync(CaseDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("Case", "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"Case.Insert: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("Case.Insert");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var status = await caseWorkflowStatusRepository.GetByGuidAsync(model.CaseWorkflowStatusGuid, token)
                    .ConfigureAwait(false);

                var visibleStatuses = status?.CaseWorkflowId is null
                    ? []
                    : (await caseWorkflowStatusRepository
                        .GetByCasesWorkflowIdActiveOnlyAsync(status.CaseWorkflowId.Value, token)
                        .ConfigureAwait(false)).ToList();

                if (status is null || visibleStatuses.All(v => v.Guid != status.Guid))
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.Insert: status {model.CaseWorkflowStatusGuid} not found or not visible " +
                                 $"user={userName}");
                    }

                    throw new NotFoundException(strings[CaseResources.NotFound]);
                }

                var workflow = await caseWorkflowRepository
                    .GetByIdAsync(status.CaseWorkflowId ?? throw new NotFoundException(strings[CaseResources.NotFound]),
                        token)
                    .ConfigureAwait(false);

                if (workflow is null)
                {
                    throw new NotFoundException(strings[CaseResources.NotFound]);
                }

                var poco = CaseMapper.ToPoco(model);
                poco.CaseWorkflowGuid = workflow.Guid;
                poco.CreatedDate = DateTime.UtcNow;
                poco.EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid();
                poco.Json = "{}";
                poco.Locked = 0;
                poco.LockedUser = null;
                poco.ClosedStatusId = 0;
                poco.LastClosedStatus = 0;

                var saved = await repository.InsertAsync(poco, token).ConfigureAwait(false);

                await new CaseStatusTransitionRepository(dbContext).EnterAsync(saved.Id, saved.CaseWorkflowGuid,
                    saved.CaseWorkflowStatusGuid, CaseTransitionEnterReason.Created, userName,
                    saved.CreatedDate ?? DateTime.UtcNow, token).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"Case.Insert: created Id={saved.Id} user={userName}");
                }

                return saved;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DtoValidationException)
            {
                op.Outcome("invalid");
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
                log.Error($"Case.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Raises a new case from a matching archived transaction identified by CaseKey/CaseKeyValue, " +
                     "or promotes an existing open case for the same key if the requested status has a higher " +
                     "priority. Triggers CaseWorkflowStatus notification/HTTP-endpoint dispatch exactly as a " +
                     "human-driven case raise would. Not idempotent -- calling twice for the same key can result " +
                     "in different outcomes depending on the existing case's state.")]
        [ServiceOperation("CaseCreateFromCaseKeyValue", OperationKind.Write, Idempotent = false)]
        public async Task<CaseDto?> CreateFromCaseKeyValueAsync(
            [Description("The case workflow, target status and key/value identifying the transaction to raise.")]
            CreateCaseDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("Case", "CreateFromCaseKeyValue", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"Case.CreateFromCaseKeyValue: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("Case.CreateFromCaseKeyValue");

                var results = await createCaseValidator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.CreateFromCaseKeyValue: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var caseWorkflow = await caseWorkflowRepository
                    .GetByGuidActiveOnlyWithRoleAsync(model.CaseWorkflowGuid, token).ConfigureAwait(false);

                if (caseWorkflow is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.CreateFromCaseKeyValue: case workflow {model.CaseWorkflowGuid} not found " +
                                 $"or not visible user={userName}");
                    }

                    throw new NotFoundException(strings[CaseResources.CaseWorkflowNotFound]);
                }

                var existingCase = await existingCasePriorityQuery
                    .ExecuteAsync(model.CaseWorkflowGuid, model.CaseKey ?? string.Empty,
                        model.CaseKeyValue ?? string.Empty, token)
                    .ConfigureAwait(false);

                if (existingCase is not null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.CreateFromCaseKeyValue: case already exists for key={model.CaseKey} " +
                                 $"value={model.CaseKeyValue} user={userName}");
                    }

                    throw new ConflictException(strings[CaseResources.CaseAlreadyExists]);
                }

                var caseWorkflowStatuses = await caseWorkflowStatusRepository
                    .GetByCasesWorkflowGuidActiveOnlyAsync(model.CaseWorkflowGuid, token).ConfigureAwait(false);

                var caseWorkflowStatus =
                    caseWorkflowStatuses.FirstOrDefault(f => f.Guid == model.CaseWorkflowStatusGuid);

                if (caseWorkflowStatus is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.CreateFromCaseKeyValue: requested status {model.CaseWorkflowStatusGuid} " +
                                 $"does not belong to workflow {model.CaseWorkflowGuid} user={userName}");
                    }

                    throw new InvalidCaseWorkflowStatusException(strings[CaseResources.InvalidCaseWorkflowStatus]);
                }

                var modelId = caseWorkflow.EntityAnalysisModelId ??
                              throw new NotFoundException(strings[CaseResources.CaseWorkflowNotFound]);

                var caseKey = await new ArchiveKeyFieldResolver(dbContext,
                        ArchiveKeyFieldResolver.DeterministicEncryptor(dynamicEnvironment))
                    .ResolveAsync(modelId, model.CaseKey, model.CaseKeyValue, token).ConfigureAwait(false);

                if (!caseKey.Resolved)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.CreateFromCaseKeyValue: key={model.CaseKey} is not an archive key of " +
                                 $"model {modelId} ({caseKey.State}) user={userName}");
                    }

                    throw new NotFoundException(strings[CaseResources.CaseKeyNotArchiveKey]);
                }

                var archive = await lastArchiveJsonQuery.ExecuteAsync(modelId, caseKey.Predicate!, token)
                    .ConfigureAwait(false);

                if (archive is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.CreateFromCaseKeyValue: no transaction found for key={model.CaseKey} " +
                                 $"value={model.CaseKeyValue} user={userName}");
                    }

                    throw new NotFoundException(strings[CaseResources.ArchiveNotFound]);
                }

                var createCase = new EngineCreateCase
                {
                    TenantRegistryId = tenantRegistryId,
                    EntityAnalysisModelInstanceEntryGuid = archive.Value.EntityAnalysisModelInstanceEntryGuid,
                    CaseWorkflowGuid = model.CaseWorkflowGuid,
                    CaseWorkflowStatusGuid = model.CaseWorkflowStatusGuid,
                    CaseKey = model.CaseKey,
                    CaseKeyValue = model.CaseKeyValue,
                    SuspendBypass = false,
                    SuspendBypassDate = DateTime.UtcNow,
                    Json = archive.Value.Json
                };

                await EngineCaseProcessing.CreateAsync(dynamicEnvironment,
                    createCase, log, jsonSerializationHelper, token: token).ConfigureAwait(false);

                var existing = await existingCasePriorityQuery
                    .ExecuteAsync(model.CaseWorkflowGuid, model.CaseKey ?? string.Empty,
                        model.CaseKeyValue ?? string.Empty, token)
                    .ConfigureAwait(false);

                if (existing is null)
                {
                    log.Error("Case.CreateFromCaseKeyValue: manual case creation did not produce a case for Case " +
                              $"Workflow {model.CaseWorkflowGuid}, Case Key {model.CaseKey}, Case Key Value " +
                              $"{model.CaseKeyValue}.");

                    throw new CaseCreationFailedException(strings[CaseResources.CaseCreationFailed]);
                }

                var saved = await repository.GetByIdActiveOnlyAsync(existing.CaseId, token).ConfigureAwait(false);
                var dto = CaseMapper.ToDto(saved);

                if (saved is not null)
                {
                    op.Entity(saved.Id);
                    op.Created();
                }

                if (log.IsInfoEnabled)
                {
                    log.Info($"Case.CreateFromCaseKeyValue: created/promoted Id={saved?.Id} " +
                             $"key={model.CaseKey} value={model.CaseKeyValue} user={userName}");
                }

                return dto;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DtoValidationException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (InvalidCaseWorkflowStatusException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (ConflictException)
            {
                op.Outcome("conflict");
                throw;
            }
            catch (CaseCreationFailedException ex)
            {
                op.Error(ex);
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");

                if (log.IsDebugEnabled)
                {
                    log.Debug($"Case.CreateFromCaseKeyValue: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"Case.CreateFromCaseKeyValue: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates a case's closed status, lock, diary, rating and workflow status, writing one " +
                     "CaseEvent audit row per field that actually changed. A workflow-status change dispatches " +
                     "the new status's notification/HTTP-endpoint (using the Payload form snapshot) exactly as a " +
                     "human-driven update would. Idempotent for an unchanged payload; a changed payload can send " +
                     "notifications/webhooks on every call.")]
        [ServiceOperation("CaseUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<RulePoco> UpdateAsync(
            [Description("The case's desired closed status, lock, diary, rating, workflow status and form payload.")]
            CaseDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("Case", "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"Case.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("Case.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.Update: validation failed id={model.Id} user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var existing = await repository.GetByIdActiveOnlyAsync(model.Id, token).ConfigureAwait(false);

                if (existing is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.Update: id={model.Id} not found or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException(strings[CaseResources.NotFound]);
                }

                if (existing.Locked == 1 && !string.IsNullOrEmpty(existing.LockedUser) &&
                    !string.Equals(existing.LockedUser, userName, StringComparison.Ordinal))
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Case.Update: id={model.Id} is locked to another user user={userName}");
                    }

                    throw new ConflictException(strings[CaseResources.LockedByAnotherUser]);
                }

                if (!string.IsNullOrEmpty(model.LockedUser) &&
                    !string.Equals(model.LockedUser, existing.LockedUser, StringComparison.Ordinal) &&
                    !string.Equals(model.LockedUser, userName, StringComparison.Ordinal) &&
                    !(await CaseEligibleAnalysts.ListAsync(dbContext, tenantRegistryId, existing.CaseWorkflowGuid,
                            model.CaseWorkflowStatusGuid, token).ConfigureAwait(false))
                    .Contains(model.LockedUser, StringComparer.Ordinal))
                {
                    throw new DtoValidationException(new ValidationResult(
                    [
                        new ValidationFailure(nameof(CaseDto.LockedUser),
                            strings[CaseResources.LockedUserInvalid]) { ErrorCode = "LockedUserInTenant" }
                    ]));
                }

                var readLocked = existing.Locked;
                var readLockedUser = existing.LockedUser;

                CaseChecklistOverride? overrideToConsume = null;
                ICaseChecklistGate? checklistGate = null;
                var leavingStatus = existing.CaseWorkflowStatusGuid != model.CaseWorkflowStatusGuid;
                var closing = existing.ClosedStatusId is 0 or 1 or 2 && model.ClosedStatusId == 3;

                if (closing)
                {
                    await EnsureNoReportAwaitingAsync(existing.Id, token).ConfigureAwait(false);
                }

                if (leavingStatus || closing)
                {
                    checklistGate = BuildChecklistGate();

                    var open = await checklistGate.EvaluateAsync(existing.Id, userName, token).ConfigureAwait(false);

                    if (open.Count > 0)
                    {
                        overrideToConsume = await checklistGate.FindLiveOverrideAsync(existing.Id,
                            existing.CaseWorkflowStatusGuid, userName, token).ConfigureAwait(false);

                        if (overrideToConsume == null)
                        {
                            await checklistGate.RecordBlockedAsync(existing.Id, userName, open, token)
                                .ConfigureAwait(false);

                            var names = open.Take(5).Select(s => s.Name).ToList();

                            throw new ChecklistIncompleteException(
                                strings[CaseResources.ChecklistIncomplete, open.Count, string.Join(", ", names)],
                                open.Select(s => s.Name).ToList());
                        }
                    }
                }

                var caseEvents = new List<CaseEvent>();
                var diaryWasOn = existing.Diary == 1;
                var diaryWas = existing.DiaryDate;
                var wasClosed = existing.ClosedStatusId == 3;
                var statusChanged = existing.CaseWorkflowStatusGuid != model.CaseWorkflowStatusGuid;

                if (existing.ClosedStatusId is 0 or 1 or 2)
                {
                    switch (model.ClosedStatusId)
                    {
                        case 3:
                            existing.ClosedUser = userName;
                            existing.ClosedDate = DateTime.UtcNow;
                            existing.ClosedReasonId = model.ClosedReasonId == 0 ? null : model.ClosedReasonId;
                            existing.ClosedReasonNote = string.IsNullOrWhiteSpace(model.ClosedReasonNote)
                                ? null
                                : model.ClosedReasonNote.Trim();

                            caseEvents.Add(new CaseEvent
                            {
                                CaseEventTypeId = 5,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                After = IsoUtc(model.ClosedDate),
                                CreatedDate = existing.ClosedDate,
                                CreatedUser = userName
                            });

                            break;
                        case 2:
                            caseEvents.Add(new CaseEvent
                            {
                                CaseEventTypeId = 12,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                Before = IsoUtc(existing.ClosedDate),
                                After = IsoUtc(model.ClosedDate),
                                CreatedDate = DateTime.UtcNow,
                                CreatedUser = userName
                            });

                            break;
                        case 1:
                            caseEvents.Add(new CaseEvent
                            {
                                CaseEventTypeId = 13,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                Before = IsoUtc(existing.ClosedDate),
                                After = IsoUtc(model.ClosedDate),
                                CreatedDate = DateTime.UtcNow,
                                CreatedUser = userName
                            });

                            break;
                    }
                }
                else
                {
                    switch (model.ClosedStatusId)
                    {
                        case 2:
                            caseEvents.Add(new CaseEvent
                            {
                                CaseEventTypeId = 12,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                Before = IsoUtc(existing.ClosedDate),
                                After = IsoUtc(model.ClosedDate),
                                CreatedDate = DateTime.UtcNow,
                                CreatedUser = userName
                            });

                            break;
                        case 1:
                            caseEvents.Add(new CaseEvent
                            {
                                CaseEventTypeId = 13,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                Before = IsoUtc(existing.ClosedDate),
                                After = IsoUtc(model.ClosedDate),
                                CreatedDate = DateTime.UtcNow,
                                CreatedUser = userName
                            });

                            break;
                    }
                }

                existing.ClosedStatusId = model.ClosedStatusId;

                if (model.ClosedStatusId != 3)
                {
                    existing.ClosedReasonId = null;
                    existing.ClosedReasonNote = null;
                }

                if (existing.LockedUser != model.LockedUser)
                {
                    caseEvents.Add(new CaseEvent
                    {
                        CaseEventTypeId = 14,
                        CaseId = existing.Id,
                        CaseKey = existing.CaseKey,
                        CaseKeyValue = existing.CaseKeyValue,
                        Before = existing.LockedUser,
                        After = model.LockedUser,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = userName
                    });
                }

                existing.LockedUser = model.LockedUser;

                var wasLocked = existing.Locked == 1;

                if (!wasLocked && model.Locked)
                {
                    caseEvents.Add(new CaseEvent
                    {
                        CaseEventTypeId = 6,
                        CaseId = existing.Id,
                        CaseKey = existing.CaseKey,
                        CaseKeyValue = existing.CaseKeyValue,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = userName
                    });

                    existing.LockedDate = DateTime.UtcNow;
                    existing.LockedUser = userName;
                }
                else if (wasLocked && !model.Locked)
                {
                    existing.LockedDate = null;
                }

                existing.Locked = (byte)(model.Locked ? 1 : 0);

                if (!SameInstant(existing.DiaryDate, model.DiaryDate?.UtcDateTime))
                {
                    caseEvents.Add(new CaseEvent
                    {
                        CaseEventTypeId = 10,
                        CaseId = existing.Id,
                        CaseKey = existing.CaseKey,
                        CaseKeyValue = existing.CaseKeyValue,
                        Before = IsoUtc(existing.DiaryDate),
                        After = IsoUtc(model.DiaryDate),
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = userName
                    });
                }

                existing.DiaryDate = model.DiaryDate?.UtcDateTime;

                if (existing.Diary is 0 or null)
                {
                    if (model.Diary)
                    {
                        caseEvents.Add(new CaseEvent
                        {
                            CaseEventTypeId = 7,
                            CaseId = existing.Id,
                            CaseKey = existing.CaseKey,
                            CaseKeyValue = existing.CaseKeyValue,
                            CreatedDate = DateTime.UtcNow,
                            CreatedUser = userName
                        });

                        existing.DiaryUser = userName;
                    }
                }

                existing.Diary = (byte)(model.Diary ? 1 : 0);

                if (diaryWasOn != model.Diary || (model.Diary && !SameInstant(diaryWas, existing.DiaryDate)))
                {
                    existing.RiskCheckedDate = null;
                    existing.RiskRecheckTriggerId = (byte)CaseRiskTrigger.DiaryLaneThreshold;
                }

                if (!model.Diary)
                {
                    existing.DiaryTypeId = (byte)CaseDiaryType.Review;
                }

                Func<Task>? pendingDispatch = null;

                if (existing.CaseWorkflowStatusGuid != model.CaseWorkflowStatusGuid)
                {
                    var caseWorkflowStatuses = await caseWorkflowStatusRepository
                        .GetByCasesWorkflowGuidActiveOnlyAsync(existing.CaseWorkflowGuid, token)
                        .ConfigureAwait(false);

                    var caseWorkflowStatus =
                        caseWorkflowStatuses.FirstOrDefault(f => f.Guid == model.CaseWorkflowStatusGuid);

                    if (caseWorkflowStatus is null)
                    {
                        if (log.IsWarnEnabled)
                        {
                            log.Warn($"Case.Update: requested status {model.CaseWorkflowStatusGuid} does not " +
                                     $"belong to workflow {existing.CaseWorkflowGuid} id={model.Id} user={userName}");
                        }

                        throw new InvalidCaseWorkflowStatusException(
                            strings[CaseResources.InvalidCaseWorkflowStatus]);
                    }

                    caseEvents.Add(new CaseEvent
                    {
                        CaseEventTypeId = 9,
                        CaseId = existing.Id,
                        CaseKey = existing.CaseKey,
                        CaseKeyValue = existing.CaseKeyValue,
                        Before = existing.CaseWorkflowStatusGuid.ToString(),
                        After = model.CaseWorkflowStatusGuid.ToString(),
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = userName
                    });

                    var statusSource = new DispatchSource(CaseDispatchSourceKind.Status, caseWorkflowStatus.Id,
                        caseWorkflowStatus.Name ?? string.Empty, caseWorkflowStatus.Sql,
                        caseWorkflowStatus.EnableNotification == 1, caseWorkflowStatus.NotificationTypeId ?? 1,
                        caseWorkflowStatus.NotificationDestination, caseWorkflowStatus.NotificationSubject,
                        caseWorkflowStatus.NotificationBody, caseWorkflowStatus.EnableHttpEndpoint == 1,
                        caseWorkflowStatus.HttpEndpoint, caseWorkflowStatus.HttpEndpointTypeId ?? 1);

                    if (statusSource.Wanted && (statusSource.UsesSql || model.Payload is not null))
                    {
                        var movedCase = existing;
                        var movedStatus = caseWorkflowStatus;

                        pendingDispatch = async () =>
                        {
                            var payload = statusSource.UsesSql
                                ? null
                                : JsonConvert.DeserializeObject<EntryPayload>(movedCase.Json,
                                    jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                            await dispatcher.DispatchAsync(statusSource,
                                new DispatchCase(movedCase.Id, movedCase.CaseKey, movedCase.CaseKeyValue), null,
                                payload is null
                                    ? null
                                    : new LegacyRender(text => EntryPayloadExtensions.ReplaceTokens(payload, text),
                                        () => PreparePostBodyString(movedCase, payload, movedStatus)), null, token)
                                .ConfigureAwait(false);
                        };
                    }
                }

                existing.CaseWorkflowStatusGuid = model.CaseWorkflowStatusGuid;

                if (existing.Rating != model.Rating)
                {
                    caseEvents.Add(new CaseEvent
                    {
                        CaseEventTypeId = 11,
                        CaseId = existing.Id,
                        CaseKey = existing.CaseKey,
                        CaseKeyValue = existing.CaseKeyValue,
                        Before = existing.Rating.ToString(),
                        After = model.Rating.ToString(),
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = userName
                    });
                }

                existing.Rating = model.Rating;

                var lockHeld = await dbContext.Case.Where(c => c.Id == existing.Id && c.Locked == readLocked
                                                               && c.LockedUser == readLockedUser)
                    .Set(s => s.Locked, existing.Locked)
                    .Set(s => s.LockedUser, existing.LockedUser)
                    .Set(s => s.LockedDate, existing.LockedDate)
                    .UpdateAsync(token).ConfigureAwait(false);

                if (lockHeld == 0)
                {
                    throw new ConflictException(strings[CaseResources.LockedByAnotherUser]);
                }

                var saved = await repository.UpdateCaseAsync(existing, token).ConfigureAwait(false);
                await caseEventRepository.BulkInsertAsync(caseEvents, token).ConfigureAwait(false);

                if (closing)
                {
                    await MarkKeyForRiskRecheckAsync(existing, token).ConfigureAwait(false);
                }

                if (overrideToConsume != null && checklistGate != null)
                {
                    await checklistGate.ConsumeOverrideAsync(overrideToConsume, userName, token)
                        .ConfigureAwait(false);
                }

                var transitions = new CaseStatusTransitionRepository(dbContext);
                var changedAt = DateTime.UtcNow;
                var nowClosed = saved.ClosedStatusId == 3;

                if (wasClosed && !nowClosed)
                {
                    await transitions.EnterAsync(saved.Id, saved.CaseWorkflowGuid, saved.CaseWorkflowStatusGuid,
                        CaseTransitionEnterReason.Reopened, userName, changedAt, token).ConfigureAwait(false);
                }
                else
                {
                    if (statusChanged)
                    {
                        await transitions.EnterAsync(saved.Id, saved.CaseWorkflowGuid, saved.CaseWorkflowStatusGuid,
                            CaseTransitionEnterReason.StatusChanged, userName, changedAt, token)
                            .ConfigureAwait(false);
                    }

                    if (!wasClosed && nowClosed)
                    {
                        await transitions.CloseAsync(saved.Id, saved.ClosedDate ?? changedAt, userName, token)
                            .ConfigureAwait(false);
                    }
                }

                var claimed = !wasLocked && model.Locked;

                if (!nowClosed && (claimed || statusChanged))
                {
                    await ReconcileChecklistAsync(saved.Id, statusChanged ? "Status change" : "Claim", token)
                        .ConfigureAwait(false);
                }

                op.Entity(saved.Id);
                op.Updated();

                if (pendingDispatch != null)
                {
                    await pendingDispatch().ConfigureAwait(false);
                }

                if (log.IsInfoEnabled)
                {
                    log.Info($"Case.Update: updated Id={saved.Id} events={caseEvents.Count} user={userName}");
                }

                return saved;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DtoValidationException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (InvalidCaseWorkflowStatusException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (ChecklistIncompleteException)
            {
                op.Outcome("conflict");
                throw;
            }
            catch (ConflictException)
            {
                op.Outcome("conflict");
                throw;
            }
            catch (VersionConflictException)
            {
                op.Outcome("conflict");
                throw;
            }

            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");

                if (log.IsDebugEnabled)
                {
                    log.Debug($"Case.Update: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"Case.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        private async Task ReconcileChecklistAsync(int caseId, string trigger, CancellationToken token)
        {
            try
            {
                var reconciler = checklistReconcilerFactory?.Invoke(dbContext) ?? new CaseChecklistReconciler(dbContext,
                    FormValidatorRuntime.SqlRunner(dynamicEnvironment, userName, log), TimeProvider.System, log);

                var changes = await reconciler.ReconcileAsync(caseId, userName, trigger, token).ConfigureAwait(false);

                if (changes > 0)
                {
                    CaseChecklistChangeEvents.Publish(serviceChangeBus, tenantRegistryId, caseId, userName, log);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.Error($"Case.Update: the checklist of case {caseId} could not be reconciled after {trigger}.", e);
            }
        }

        private async Task EnsureNoReportAwaitingAsync(int caseId, CancellationToken token)
        {
            var open = await new CaseReportRepository(dbContext).CountOpenForCaseAsync(tenantRegistryId, caseId, token)
                .ConfigureAwait(false);

            if (open == 0 || permissionValidation.Validate([76]))
            {
                return;
            }

            throw new ConflictException(permissionValidation.Validate([75, 77])
                ? "A regulatory report for this case is awaiting approval or filing; an approver can close it."
                : "This case cannot be closed yet. Ask a supervisor to review it.");
        }

        private ICaseChecklistGate BuildChecklistGate()
        {
            var reconciler = new CaseChecklistReconciler(dbContext,
                FormValidatorRuntime.SqlRunner(dynamicEnvironment, userName, log), TimeProvider.System, log);

            return new CaseChecklistGate(dbContext, reconciler,
                () => DataConnectionDbContext.GetResilientDbContextDataConnection(
                    dynamicEnvironment.AppSettings("ConnectionString"), log), TimeProvider.System, log);
        }

        private string PreparePostBodyString(RulePoco existing,
            EntryPayload payload,
            Data.Poco.CaseWorkflowStatus caseWorkflowStatus)
        {
            var caseJObject = JObject.FromObject(existing, jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject["caseWorkflowStatusName"] = caseWorkflowStatus.Name;

            var payloadJObject = JObject.FromObject(payload, jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject.Remove("json");
            caseJObject["payload"] = payloadJObject;

            return caseJObject.ToString();
        }

        private async Task MarkKeyForRiskRecheckAsync(Jube.Data.Poco.Case closed, CancellationToken token)
        {
            try
            {
                await new MarkCaseRiskRecheckQuery(dbContext).ForOtherOpenCasesOnKeyAsync(closed.CaseWorkflowGuid,
                    closed.CaseKey, closed.CaseKeyValue, closed.Id, CaseRiskTrigger.LinkedKeyChanged, token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Case.Update: the other open cases on {closed.CaseKey} {closed.CaseKeyValue} could not " +
                          $"be marked for risk recheck user={userName}", ex);
            }
        }

        private static string? IsoUtc(DateTime? value) =>
            value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture)
                : null;

        private static string? IsoUtc(DateTimeOffset? value) => IsoUtc(value?.UtcDateTime);

        private static bool SameInstant(DateTime? stored, DateTime? requested)
        {
            if (stored is not { } storedValue || requested is not { } requestedValue)
            {
                return stored.HasValue == requested.HasValue;
            }

            return Math.Abs((storedValue - requestedValue).Ticks) < 10;
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

            throw new ForbiddenException(strings[CaseResources.PermissionDenied], permissions);
        }
    }
}