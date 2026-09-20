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
using System.Globalization;
using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Data.Repository;
using Jube.Dto.Repository.Case;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.Case;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.Case;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EngineCaseProcessing = Jube.Engine.BackgroundTasks.TaskStarters.Case.CaseProcessing;
using EngineCreateCase = Jube.Engine.EntityAnalysisModelInvoke.Models.CaseManagement.CreateCase;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using EngineNotification = global::Jube.Case.Notification;
using EngineSendHttpEndpoint = global::Jube.Case.SendHttpEndpoint;
using RulePoco = Jube.Data.Poco.Case;
using RuleRepository = Jube.Data.Repository.CaseRepository;

namespace Jube.Service.Repository.Case
{
    using EntryPayload = Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
        .EntityAnalysisModelInstanceEntryPayload;
    using EntryPayloadExtensions =
        Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
        .Extensions.EntityAnalysisModelInstanceEntryPayloadExtensions;

    public sealed class CaseService
    {
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly RuleRepository repository;
        private readonly CaseEventRepository caseEventRepository;
        private readonly CaseWorkflowRepository caseWorkflowRepository;
        private readonly CaseWorkflowStatusRepository caseWorkflowStatusRepository;
        private readonly GetExistingCasePriorityQuery existingCasePriorityQuery;
        private readonly GetLastArchiveJsonByEntityAnalysisModelIdAndCaseKeyValueQuery lastArchiveJsonQuery;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly EngineJsonSerializationHelper jsonSerializationHelper;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseDtoValidator validator;
        private readonly CreateCaseValidator createCaseValidator;

        private CaseService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper)
        {
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
            caseEventRepository = new CaseEventRepository(dbContext, userName);
            caseWorkflowRepository = new CaseWorkflowRepository(dbContext, userName);
            caseWorkflowStatusRepository = new CaseWorkflowStatusRepository(dbContext, userName);
            existingCasePriorityQuery = new GetExistingCasePriorityQuery(dbContext);
            lastArchiveJsonQuery = new GetLastArchiveJsonByEntityAnalysisModelIdAndCaseKeyValueQuery(dbContext);
            validator = new CaseDtoValidator(strings);
            createCaseValidator = new CreateCaseValidator(strings);
        }

        public static Task<CaseService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                jsonSerializationHelper, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, ILog auditLog,
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
                auditLog, serviceChangeBus, strings, dynamicEnvironment, jsonSerializationHelper);
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

                var archive = await lastArchiveJsonQuery
                    .ExecuteAsync(
                        caseWorkflow.EntityAnalysisModelId ??
                        throw new NotFoundException(strings[CaseResources.CaseWorkflowNotFound]),
                        model.CaseKey ?? string.Empty, model.CaseKeyValue ?? string.Empty,
                        token).ConfigureAwait(false);
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
                    !await LinqToDB.AsyncExtensions.AnyAsync(dbContext.UserInTenant,
                            u => u.User == model.LockedUser && u.TenantRegistryId == tenantRegistryId, token)
                        .ConfigureAwait(false))
                {
                    throw new DtoValidationException(new FluentValidation.Results.ValidationResult(
                    [
                        new FluentValidation.Results.ValidationFailure(nameof(CaseDto.LockedUser),
                            strings[CaseResources.LockedUserInvalid]) { ErrorCode = "LockedUserInTenant" }
                    ]));
                }

                var caseEvents = new List<Data.Poco.CaseEvent>();

                if (existing.ClosedStatusId is 0 or 1 or 2)
                {
                    switch (model.ClosedStatusId)
                    {
                        case 3:
                            existing.ClosedUser = userName;
                            existing.ClosedDate = DateTime.UtcNow;

                            caseEvents.Add(new Data.Poco.CaseEvent
                            {
                                CaseEventTypeId = 5,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                After = model.ClosedDate?.ToString("o", CultureInfo.InvariantCulture),
                                CreatedDate = existing.ClosedDate,
                                CreatedUser = userName
                            });
                            break;
                        case 2:
                            caseEvents.Add(new Data.Poco.CaseEvent
                            {
                                CaseEventTypeId = 12,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                Before = existing.ClosedDate.ToString(),
                                After = model.ClosedDate?.ToString("o", CultureInfo.InvariantCulture),
                                CreatedDate = DateTime.UtcNow,
                                CreatedUser = userName
                            });
                            break;
                        case 1:
                            caseEvents.Add(new Data.Poco.CaseEvent
                            {
                                CaseEventTypeId = 13,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                Before = existing.ClosedDate.ToString(),
                                After = model.ClosedDate?.ToString("o", CultureInfo.InvariantCulture),
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
                            caseEvents.Add(new Data.Poco.CaseEvent
                            {
                                CaseEventTypeId = 12,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                Before = existing.ClosedDate.ToString(),
                                After = model.ClosedDate?.ToString("o", CultureInfo.InvariantCulture),
                                CreatedDate = DateTime.UtcNow,
                                CreatedUser = userName
                            });
                            break;
                        case 1:
                            caseEvents.Add(new Data.Poco.CaseEvent
                            {
                                CaseEventTypeId = 13,
                                CaseId = existing.Id,
                                CaseKey = existing.CaseKey,
                                CaseKeyValue = existing.CaseKeyValue,
                                Before = existing.ClosedDate.ToString(),
                                After = model.ClosedDate?.ToString("o", CultureInfo.InvariantCulture),
                                CreatedDate = DateTime.UtcNow,
                                CreatedUser = userName
                            });
                            break;
                    }
                }

                existing.ClosedStatusId = model.ClosedStatusId;

                if (existing.LockedUser != model.LockedUser)
                {
                    caseEvents.Add(new Data.Poco.CaseEvent
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
                    caseEvents.Add(new Data.Poco.CaseEvent
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
                    caseEvents.Add(new Data.Poco.CaseEvent
                    {
                        CaseEventTypeId = 10,
                        CaseId = existing.Id,
                        CaseKey = existing.CaseKey,
                        CaseKeyValue = existing.CaseKeyValue,
                        Before = existing.DiaryDate.ToString(),
                        After = model.DiaryDate?.ToString("o", CultureInfo.InvariantCulture),
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = userName
                    });
                }

                existing.DiaryDate = model.DiaryDate?.UtcDateTime;

                if (existing.Diary is 0 or null)
                {
                    if (model.Diary)
                    {
                        caseEvents.Add(new Data.Poco.CaseEvent
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

                    caseEvents.Add(new Data.Poco.CaseEvent
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

                    if (model.Payload is not null &&
                        (caseWorkflowStatus.EnableNotification == 1 || caseWorkflowStatus.EnableHttpEndpoint == 1))
                    {
                        var payload = JsonConvert
                            .DeserializeObject<EntryPayload>(
                                existing.Json, jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                        if (caseWorkflowStatus.EnableNotification == 1 && payload is not null)
                        {
                            var notificationSubject =
                                EntryPayloadExtensions.ReplaceTokens(
                                    payload, caseWorkflowStatus.NotificationSubject);
                            var notificationDestination =
                                EntryPayloadExtensions.ReplaceTokens(
                                    payload, caseWorkflowStatus.NotificationDestination);
                            var notificationBody =
                                EntryPayloadExtensions.ReplaceTokens(
                                    payload, caseWorkflowStatus.NotificationBody);

                            var notification = new EngineNotification(
                                log, dynamicEnvironment);

                            await notification.SendAsync(caseWorkflowStatus.NotificationTypeId ?? 1,
                                    notificationDestination, notificationSubject, notificationBody, token)
                                .ConfigureAwait(false);
                        }

                        if (caseWorkflowStatus.EnableHttpEndpoint == 1 && payload is not null)
                        {
                            var endpoint =
                                EntryPayloadExtensions.ReplaceTokens(
                                    payload, caseWorkflowStatus.HttpEndpoint);

                            if (caseWorkflowStatus.HttpEndpointTypeId == 1)
                            {
                                await EngineSendHttpEndpoint.PostAsync(endpoint,
                                        PreparePostBodyString(existing, payload, caseWorkflowStatus), log)
                                    .ConfigureAwait(false);
                            }
                            else
                            {
                                await EngineSendHttpEndpoint.GetAsync(endpoint, log).ConfigureAwait(false);
                            }
                        }
                    }
                }

                existing.CaseWorkflowStatusGuid = model.CaseWorkflowStatusGuid;

                if (existing.Rating != model.Rating)
                {
                    caseEvents.Add(new Data.Poco.CaseEvent
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

                var saved = await repository.UpdateCaseAsync(existing, token).ConfigureAwait(false);
                await caseEventRepository.BulkInsertAsync(caseEvents, token).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Updated();

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
            catch (ConflictException)
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