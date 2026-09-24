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
using Jube.Data.Repository;
using Jube.Dto.Filter;
using Jube.Dto.Validation;
using Jube.Dto.Repository.CaseWorkflowAction;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.Repository.CaseWorkflowAction;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowAction;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.CaseWorkflowActionRepository;

namespace Jube.Service.Repository.CaseWorkflowAction
{
    public sealed class CaseWorkflowActionService
    {
        private static readonly int[] permissions = [22];
        private static readonly int[] activeOnlyPermissions = [22, 1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowActionDtoValidator validator;

        private CaseWorkflowActionService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new RuleRepository(dbContext, userName);
            validator = new CaseWorkflowActionDtoValidator(repository, strings);
        }

        public static Task<CaseWorkflowActionService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowActionService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowActionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowAction.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowActionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowAction.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowActionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowActionService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Case Workflow Action visible to the caller's tenant, across all parent Case " +
                     "Workflows. Unbounded -- prefer ListAsync for agent/tool use.")]
        public async Task<List<CaseWorkflowActionDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowAction.List", permissions);
                var dtos = CaseWorkflowActionMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowAction.List: {dtos.Count} rows user={userName}");
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowAction.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow Actions visible to the caller's tenant, Id-ascending, capped at " +
                     "'take' rows. Call again with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("CaseWorkflowActionList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<CaseWorkflowActionDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowAction.List", permissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var all = CaseWorkflowActionMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<CaseWorkflowActionDto>(page);
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
                log.Error($"CaseWorkflowAction.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Case Workflow Actions " +
                     "may use, with each field's type, the operators allowed for it and what it " +
                     "means. Use them as rule ids in CaseWorkflowActionFilter and " +
                     "CaseWorkflowActionCount.")]
        [ServiceOperation("CaseWorkflowActionFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowAction.FilterFields", permissions);
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<CaseWorkflowActionDto>();
                op.Rows(result.Count);
                return result;
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
                log.Error($"CaseWorkflowAction.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Case Workflow Actions in the caller's tenant matching query " +
                     "builder JSON (the same format as the rule builder, over the fields from " +
                     "CaseWorkflowActionFilterFields), ordered by id and capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last " +
                     "returned Id to continue. Invalid JSON is not an error: Valid is false and " +
                     "Errors gives each problem with its JSON path.")]
        [ServiceOperation("CaseWorkflowActionFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<CaseWorkflowActionDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Case Workflow Actions, using the fields from CaseWorkflowActionFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowAction.Filter", permissions);
                var rows = CaseWorkflowActionMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                var result = DtoFilter.Filter(rows, builderJson, take, afterId, d => d.Id);
                op.Rows(result.Items.Count);
                return result;
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
                log.Error($"CaseWorkflowAction.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Case Workflow Actions in the caller's tenant matching query " +
                     "builder JSON (over the fields from CaseWorkflowActionFilterFields; empty " +
                     "counts all), optionally broken down by the values of one field. Invalid " +
                     "JSON is not an error: Valid is false and Errors gives each problem with " +
                     "its JSON path.")]
        [ServiceOperation("CaseWorkflowActionCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Case Workflow Actions, using the fields from CaseWorkflowActionFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from CaseWorkflowActionFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowAction.Count", permissions);
                var rows = CaseWorkflowActionMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                var result = DtoFilter.Count(rows, builderJson, groupBy);
                op.Rows(result.Count);
                return result;
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
                log.Error($"CaseWorkflowAction.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow Actions belonging to a parent Case Workflow, visible to the caller's " +
                     "tenant, Id-ascending, including inactive ones.")]
        public async Task<List<CaseWorkflowActionDto>> GetByCaseWorkflowIdAsync(int caseWorkflowId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "ListByWorkflow", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.ListByWorkflow: entry caseWorkflowId={caseWorkflowId} " +
                          $"user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowAction.ListByWorkflow", permissions);
                var dtos = CaseWorkflowActionMapper.ToDto(
                    await repository.GetByCasesWorkflowIdOrderByIdAsync(caseWorkflowId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowAction.ListByWorkflow: {dtos.Count} rows " +
                              $"caseWorkflowId={caseWorkflowId} user={userName}");
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowAction.ListByWorkflow: unexpected failure " +
                          $"caseWorkflowId={caseWorkflowId} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Case Workflow Actions belonging to a parent Case Workflow that the caller " +
                     "holds a role grant on, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowActionListByWorkflowIdActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowActionDto>> GetByCasesWorkflowIdActiveOnlyAsync(
            [Description("Identifier of the parent Case Workflow.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "ListByWorkflowIdActiveOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.ListByWorkflowIdActiveOnly: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowAction.ListByWorkflowIdActiveOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowActionMapper.ToDto(
                    await repository.GetByCasesWorkflowIdActiveOnlyAsync(id, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowAction.ListByWorkflowIdActiveOnly: {dtos.Count} rows id={id} " +
                              $"user={userName}");
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowAction.ListByWorkflowIdActiveOnly: unexpected failure id={id} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Case Workflow Actions belonging to a parent Case Workflow (identified by " +
                     "the workflow's Guid) that the caller holds a role grant on, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowActionListByWorkflowGuidActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowActionDto>> GetByCasesWorkflowGuidActiveOnlyAsync(
            [Description("Guid of the parent Case Workflow.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "ListByWorkflowGuidActiveOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.ListByWorkflowGuidActiveOnly: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowAction.ListByWorkflowGuidActiveOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowActionMapper.ToDto(
                    await repository.GetByCasesWorkflowGuidActiveOnlyAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowAction.ListByWorkflowGuidActiveOnly: {dtos.Count} rows guid={guid} " +
                              $"user={userName}");
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowAction.ListByWorkflowGuidActiveOnly: unexpected failure guid={guid} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Case Workflow Action by Id, or null if it doesn't exist or isn't " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowActionGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowActionDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Case Workflow Action.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "Get", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowAction.Get", permissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                var dto = CaseWorkflowActionMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowAction.Get: id={id} not found or not visible to tenant user={userName}");
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
                log.Error($"CaseWorkflowAction.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Case Workflow Action under a parent Case Workflow.")]
        [ServiceOperation("CaseWorkflowActionCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowActionDto> InsertAsync(CaseWorkflowActionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "Insert", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.Insert: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowAction.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowAction.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.InsertAsync(CaseWorkflowActionMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[CaseWorkflowActionResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowAction.Insert: created Id={saved.Id} name={saved.Name} user={userName}");
                }

                return CaseWorkflowActionMapper.ToDto(saved);
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
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowAction.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates a Case Workflow Action without saving it, running every check a create (Id 0) " +
                     "or an update (any other Id) would run, and returns each failure. Nothing is stored or " +
                     "changed.")]
        [ServiceOperation("CaseWorkflowActionValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Case Workflow Action to validate.")]
            CaseWorkflowActionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "Validate", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowAction.Validate", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                op.Rows(results.Errors.Count);
                return ValidationResultMapper.ToDto(results);
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
                log.Error($"CaseWorkflowAction.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Case Workflow Action. The row must exist, be visible to the caller's " +
                     "tenant, and not be locked.")]
        [ServiceOperation("CaseWorkflowActionUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseWorkflowActionDto> UpdateAsync(CaseWorkflowActionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "Update", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowAction.Update", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowAction.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.UpdateAsync(CaseWorkflowActionMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[CaseWorkflowActionResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowAction.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return CaseWorkflowActionMapper.ToDto(saved);
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
            catch (KeyNotFoundException)
            {
                op.Outcome("not_found");
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowAction.Update: id={model?.Id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowActionResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowAction.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Case Workflow Action. The row must exist, be visible to the caller's " +
                     "tenant, and not be locked. Reversible only by direct database action.")]
        [ServiceOperation("CaseWorkflowActionDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Case Workflow Action to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowAction", "Delete", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowAction.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowAction.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowAction.Delete: soft-deleted Id={id} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (KeyNotFoundException)
            {
                op.Outcome("not_found");
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowAction.Delete: id={id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowActionResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowAction.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op, int[] specs)
        {
            if (permissionValidation.Validate(specs))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");
            }

            throw new ForbiddenException(strings[CaseWorkflowActionResources.PermissionDenied], specs);
        }
    }
}