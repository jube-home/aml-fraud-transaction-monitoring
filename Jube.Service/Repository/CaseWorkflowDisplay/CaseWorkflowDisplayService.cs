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
using Jube.Dto.Repository.CaseWorkflowDisplay;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.Repository.CaseWorkflowDisplay;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowDisplay;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.CaseWorkflowDisplayRepository;

namespace Jube.Service.Repository.CaseWorkflowDisplay
{
    public sealed class CaseWorkflowDisplayService
    {
        private static readonly int[] permissions = [23];
        private static readonly int[] activeOnlyPermissions = [23, 1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowDisplayDtoValidator validator;

        private CaseWorkflowDisplayService(DbContext dbContext, string userName, int tenantRegistryId,
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
            validator = new CaseWorkflowDisplayDtoValidator(repository, strings);
        }

        public static Task<CaseWorkflowDisplayService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowDisplayService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowDisplayResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowDisplay.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowDisplayResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowDisplay.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowDisplayResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowDisplayService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Case Workflow Display visible to the caller's tenant, across all parent Case " +
                     "Workflows. Unbounded -- prefer ListAsync for agent/tool use.")]
        public async Task<List<CaseWorkflowDisplayDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplay.List", permissions);
                var dtos = CaseWorkflowDisplayMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowDisplay.List: {dtos.Count} rows user={userName}");
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
                log.Error($"CaseWorkflowDisplay.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow Displays visible to the caller's tenant, Id-ascending, capped at " +
                     "'take' rows. Call again with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("CaseWorkflowDisplayList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<CaseWorkflowDisplayDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplay.List", permissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var all = CaseWorkflowDisplayMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<CaseWorkflowDisplayDto>(page);
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
                log.Error($"CaseWorkflowDisplay.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Case Workflow Displays " +
                     "may use, with each field's type, the operators allowed for it and what it " +
                     "means. Use them as rule ids in CaseWorkflowDisplayFilter and " +
                     "CaseWorkflowDisplayCount.")]
        [ServiceOperation("CaseWorkflowDisplayFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplay.FilterFields", permissions);
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<CaseWorkflowDisplayDto>();
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
                log.Error($"CaseWorkflowDisplay.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Case Workflow Displays in the caller's tenant matching query " +
                     "builder JSON (the same format as the rule builder, over the fields from " +
                     "CaseWorkflowDisplayFilterFields), ordered by id and capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last " +
                     "returned Id to continue. Invalid JSON is not an error: Valid is false and " +
                     "Errors gives each problem with its JSON path.")]
        [ServiceOperation("CaseWorkflowDisplayFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<CaseWorkflowDisplayDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Case Workflow Displays, using the fields from CaseWorkflowDisplayFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplay.Filter", permissions);
                var rows = CaseWorkflowDisplayMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"CaseWorkflowDisplay.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Case Workflow Displays in the caller's tenant matching query " +
                     "builder JSON (over the fields from CaseWorkflowDisplayFilterFields; empty " +
                     "counts all), optionally broken down by the values of one field. Invalid " +
                     "JSON is not an error: Valid is false and Errors gives each problem with " +
                     "its JSON path.")]
        [ServiceOperation("CaseWorkflowDisplayCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Case Workflow Displays, using the fields from CaseWorkflowDisplayFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from CaseWorkflowDisplayFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplay.Count", permissions);
                var rows = CaseWorkflowDisplayMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"CaseWorkflowDisplay.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow Displays belonging to a parent Case Workflow, visible to the caller's " +
                     "tenant, Id-ascending, including inactive ones.")]
        public async Task<List<CaseWorkflowDisplayDto>> GetByCaseWorkflowIdAsync(int caseWorkflowId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "ListByWorkflow", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.ListByWorkflow: entry caseWorkflowId={caseWorkflowId} " +
                          $"user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplay.ListByWorkflow", permissions);
                var dtos = CaseWorkflowDisplayMapper.ToDto(
                    await repository.GetByCasesWorkflowIdOrderByIdAsync(caseWorkflowId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowDisplay.ListByWorkflow: {dtos.Count} rows " +
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
                log.Error($"CaseWorkflowDisplay.ListByWorkflow: unexpected failure " +
                          $"caseWorkflowId={caseWorkflowId} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Case Workflow Displays belonging to a parent Case Workflow that the caller " +
                     "holds a role grant on, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowDisplayListByWorkflowIdActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowDisplayDto>> GetByCasesWorkflowIdActiveOnlyAsync(
            [Description("Identifier of the parent Case Workflow.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "ListByWorkflowIdActiveOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.ListByWorkflowIdActiveOnly: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplay.ListByWorkflowIdActiveOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowDisplayMapper.ToDto(
                    await repository.GetByCasesWorkflowIdActiveOnlyAsync(id, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowDisplay.ListByWorkflowIdActiveOnly: {dtos.Count} rows id={id} " +
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
                log.Error($"CaseWorkflowDisplay.ListByWorkflowIdActiveOnly: unexpected failure id={id} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Case Workflow Displays belonging to a parent Case Workflow (identified by " +
                     "the workflow's Guid) that the caller holds a role grant on, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowDisplayListByWorkflowGuidActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowDisplayDto>> GetByCasesWorkflowGuidActiveOnlyAsync(
            [Description("Guid of the parent Case Workflow.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "ListByWorkflowGuidActiveOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.ListByWorkflowGuidActiveOnly: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplay.ListByWorkflowGuidActiveOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowDisplayMapper.ToDto(
                    await repository.GetByCasesWorkflowGuidActiveOnlyAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowDisplay.ListByWorkflowGuidActiveOnly: {dtos.Count} rows guid={guid} " +
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
                log.Error($"CaseWorkflowDisplay.ListByWorkflowGuidActiveOnly: unexpected failure guid={guid} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Case Workflow Display by Id, or null if it doesn't exist or isn't " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowDisplayGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowDisplayDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Case Workflow Display.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "Get", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplay.Get", permissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                var dto = CaseWorkflowDisplayMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowDisplay.Get: id={id} not found or not visible to tenant " +
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
                log.Error($"CaseWorkflowDisplay.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Case Workflow Display under a parent Case Workflow.")]
        [ServiceOperation("CaseWorkflowDisplayCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowDisplayDto> InsertAsync(CaseWorkflowDisplayDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "Insert", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.Insert: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowDisplay.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowDisplay.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.InsertAsync(CaseWorkflowDisplayMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[CaseWorkflowDisplayResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowDisplay.Insert: created Id={saved.Id} name={saved.Name} " +
                             $"user={userName}");
                }

                return CaseWorkflowDisplayMapper.ToDto(saved);
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
                log.Error($"CaseWorkflowDisplay.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates a Case Workflow Display without saving it, running every check a create (Id 0) " +
                     "or an update (any other Id) would run, and returns each failure. Nothing is stored or " +
                     "changed.")]
        [ServiceOperation("CaseWorkflowDisplayValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Case Workflow Display to validate.")]
            CaseWorkflowDisplayDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "Validate", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowDisplay.Validate", permissions);

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
                log.Error($"CaseWorkflowDisplay.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Case Workflow Display. The row must exist, be visible to the " +
                     "caller's tenant, and not be locked.")]
        [ServiceOperation("CaseWorkflowDisplayUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseWorkflowDisplayDto> UpdateAsync(CaseWorkflowDisplayDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "Update", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowDisplay.Update", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowDisplay.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.UpdateAsync(CaseWorkflowDisplayMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[CaseWorkflowDisplayResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowDisplay.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return CaseWorkflowDisplayMapper.ToDto(saved);
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
                    log.Warn($"CaseWorkflowDisplay.Update: id={model?.Id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowDisplayResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowDisplay.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Case Workflow Display. The row must exist, be visible to the caller's " +
                     "tenant, and not be locked. Reversible only by direct database action.")]
        [ServiceOperation("CaseWorkflowDisplayDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Case Workflow Display to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplay", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowDisplay.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplay.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowDisplay.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"CaseWorkflowDisplay.Delete: id={id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowDisplayResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowDisplay.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[CaseWorkflowDisplayResources.PermissionDenied], specs);
        }
    }
}