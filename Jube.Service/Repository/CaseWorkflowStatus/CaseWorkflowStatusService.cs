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
using Jube.Dto.Repository.CaseWorkflowStatus;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.CaseWorkflowStatus;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowStatus;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.CaseWorkflowStatusRepository;

namespace Jube.Service.Repository.CaseWorkflowStatus
{
    public sealed class CaseWorkflowStatusService
    {
        private static readonly int[] permissions = [19];
        private static readonly int[] activeOnlyPermissions = [19, 1];
        private static readonly int[] listByWorkflowPermissions = [17, 19, 25, 1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowStatusDtoValidator validator;

        private CaseWorkflowStatusService(DbContext dbContext, string userName, int tenantRegistryId,
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
            validator = new CaseWorkflowStatusDtoValidator(repository, strings);
        }

        public static Task<CaseWorkflowStatusService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowStatusService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowStatusResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowStatus.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowStatusResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowStatus.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowStatusResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowStatusService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Case Workflow Status visible to the caller's tenant, across all parent Case " +
                     "Workflows. Unbounded -- prefer ListAsync for agent/tool use.")]
        public async Task<List<CaseWorkflowStatusDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.List", permissions);
                var dtos = CaseWorkflowStatusMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowStatus.List: {dtos.Count} rows user={userName}");
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
                log.Error($"CaseWorkflowStatus.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow Statuses visible to the caller's tenant, Id-ascending, capped at " +
                     "'take' rows. Call again with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("CaseWorkflowStatusList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<CaseWorkflowStatusDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.List", permissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var all = CaseWorkflowStatusMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<CaseWorkflowStatusDto>(page);
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
                log.Error($"CaseWorkflowStatus.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Case Workflow Statuses " +
                     "may use, with each field's type, the operators allowed for it and what it " +
                     "means. Use them as rule ids in CaseWorkflowStatusFilter and " +
                     "CaseWorkflowStatusCount.")]
        [ServiceOperation("CaseWorkflowStatusFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.FilterFields", permissions);
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<CaseWorkflowStatusDto>();
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
                log.Error($"CaseWorkflowStatus.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Case Workflow Statuses in the caller's tenant matching query " +
                     "builder JSON (the same format as the rule builder, over the fields from " +
                     "CaseWorkflowStatusFilterFields), ordered by id and capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last " +
                     "returned Id to continue. Invalid JSON is not an error: Valid is false and " +
                     "Errors gives each problem with its JSON path.")]
        [ServiceOperation("CaseWorkflowStatusFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<CaseWorkflowStatusDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Case Workflow Statuses, using the fields from CaseWorkflowStatusFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.Filter", permissions);
                var rows = CaseWorkflowStatusMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"CaseWorkflowStatus.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Case Workflow Statuses in the caller's tenant matching query " +
                     "builder JSON (over the fields from CaseWorkflowStatusFilterFields; empty " +
                     "counts all), optionally broken down by the values of one field. Invalid " +
                     "JSON is not an error: Valid is false and Errors gives each problem with " +
                     "its JSON path.")]
        [ServiceOperation("CaseWorkflowStatusCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Case Workflow Statuses, using the fields from CaseWorkflowStatusFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from CaseWorkflowStatusFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.Count", permissions);
                var rows = CaseWorkflowStatusMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"CaseWorkflowStatus.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow Statuses belonging to a parent Case Workflow, visible to any caller " +
                     "holding an Activation Rule, Case Workflow Status, Case Workflow Filter, or Case " +
                     "permission, Id-ascending, including inactive ones.")]
        public async Task<List<CaseWorkflowStatusDto>> GetByCaseWorkflowIdAsync(int caseWorkflowId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "ListByWorkflow", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.ListByWorkflow: entry caseWorkflowId={caseWorkflowId} " +
                          $"user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.ListByWorkflow", listByWorkflowPermissions);
                var dtos = CaseWorkflowStatusMapper.ToDto(
                    await repository.GetByCasesWorkflowIdOrderByIdAsync(caseWorkflowId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowStatus.ListByWorkflow: {dtos.Count} rows " +
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
                log.Error($"CaseWorkflowStatus.ListByWorkflow: unexpected failure " +
                          $"caseWorkflowId={caseWorkflowId} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow Statuses belonging to a parent Case Workflow (identified by the " +
                     "workflow's Guid), visible to any caller holding an Activation Rule, Case Workflow Status, " +
                     "Case Workflow Filter, or Case permission, including inactive ones.")]
        [ServiceOperation("CaseWorkflowStatusListByWorkflowGuid", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowStatusDto>> GetByCaseWorkflowGuidAsync(
            [Description("Guid of the parent Case Workflow.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "ListByWorkflowGuid", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.ListByWorkflowGuid: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.ListByWorkflowGuid", listByWorkflowPermissions);
                var dtos = CaseWorkflowStatusMapper.ToDto(
                    await repository.GetByCasesWorkflowGuidAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowStatus.ListByWorkflowGuid: {dtos.Count} rows guid={guid} " +
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
                log.Error($"CaseWorkflowStatus.ListByWorkflowGuid: unexpected failure guid={guid} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Case Workflow Statuses belonging to a parent Case Workflow that the caller " +
                     "holds a role grant on, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowStatusListByWorkflowIdActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowStatusDto>> GetByCasesWorkflowIdActiveOnlyAsync(
            [Description("Identifier of the parent Case Workflow.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "ListByWorkflowIdActiveOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.ListByWorkflowIdActiveOnly: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.ListByWorkflowIdActiveOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowStatusMapper.ToDto(
                    await repository.GetByCasesWorkflowIdActiveOnlyAsync(id, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowStatus.ListByWorkflowIdActiveOnly: {dtos.Count} rows id={id} " +
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
                log.Error($"CaseWorkflowStatus.ListByWorkflowIdActiveOnly: unexpected failure id={id} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Case Workflow Statuses belonging to a parent Case Workflow (identified by " +
                     "the workflow's Guid) that the caller holds a role grant on, visible to the caller's " +
                     "tenant.")]
        [ServiceOperation("CaseWorkflowStatusListByWorkflowGuidActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowStatusDto>> GetByCasesWorkflowGuidActiveOnlyAsync(
            [Description("Guid of the parent Case Workflow.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "ListByWorkflowGuidActiveOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.ListByWorkflowGuidActiveOnly: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.ListByWorkflowGuidActiveOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowStatusMapper.ToDto(
                    await repository.GetByCasesWorkflowGuidActiveOnlyAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowStatus.ListByWorkflowGuidActiveOnly: {dtos.Count} rows guid={guid} " +
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
                log.Error($"CaseWorkflowStatus.ListByWorkflowGuidActiveOnly: unexpected failure guid={guid} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Case Workflow Status by Id, or null if it doesn't exist or isn't " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowStatusGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowStatusDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Case Workflow Status.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "Get", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.Get", permissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                var dto = CaseWorkflowStatusMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowStatus.Get: id={id} not found or not visible to tenant " +
                              $"user={userName}");
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
                log.Error($"CaseWorkflowStatus.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Case Workflow Status under a parent Case Workflow.")]
        [ServiceOperation("CaseWorkflowStatusCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowStatusDto> InsertAsync(CaseWorkflowStatusDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "Insert", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.Insert: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowStatus.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowStatus.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(CaseWorkflowStatusMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowStatus.Insert: created Id={saved.Id} name={saved.Name} " +
                             $"user={userName}");
                }

                return CaseWorkflowStatusMapper.ToDto(saved);
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
                log.Error($"CaseWorkflowStatus.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates a Case Workflow Status without saving it, running every check a create (Id 0) " +
                     "or an update (any other Id) would run, and returns each failure. Nothing is stored or " +
                     "changed.")]
        [ServiceOperation("CaseWorkflowStatusValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Case Workflow Status to validate.")]
            CaseWorkflowStatusDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "Validate", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowStatus.Validate", permissions);

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
                log.Error($"CaseWorkflowStatus.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Case Workflow Status. The row must exist, be visible to the " +
                     "caller's tenant, and not be locked.")]
        [ServiceOperation("CaseWorkflowStatusUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseWorkflowStatusDto> UpdateAsync(CaseWorkflowStatusDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "Update", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowStatus.Update", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowStatus.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.UpdateAsync(CaseWorkflowStatusMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowStatus.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return CaseWorkflowStatusMapper.ToDto(saved);
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
                    log.Warn($"CaseWorkflowStatus.Update: id={model?.Id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowStatusResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowStatus.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Case Workflow Status. The row must exist, be visible to the caller's " +
                     "tenant, and not be locked. Reversible only by direct database action.")]
        [ServiceOperation("CaseWorkflowStatusDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Case Workflow Status to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowStatus", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowStatus.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowStatus.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowStatus.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"CaseWorkflowStatus.Delete: id={id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowStatusResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowStatus.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[CaseWorkflowStatusResources.PermissionDenied], specs);
        }
    }
}