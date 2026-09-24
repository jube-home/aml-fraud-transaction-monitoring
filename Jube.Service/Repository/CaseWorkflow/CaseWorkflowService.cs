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
using Jube.Dto.Repository.CaseWorkflow;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.Repository.CaseWorkflow;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Parser.Dependency;
using Jube.Validations.Dependency;
using Jube.Validations.Repository.CaseWorkflow;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.CaseWorkflowRepository;

namespace Jube.Service.Repository.CaseWorkflow
{
    public sealed class CaseWorkflowService
    {
        private static readonly int[] permissions = [18];
        private static readonly int[] listByModelPermissions = [17, 18];
        private static readonly int[] activeOnlyPermissions = [18, 1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly ModelEntityDeleteValidator deleteValidator;
        private readonly CaseWorkflowDtoValidator validator;

        private CaseWorkflowService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, IStringLocalizer dependencyStrings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new RuleRepository(dbContext, userName);
            validator = new CaseWorkflowDtoValidator(repository, strings);
            deleteValidator = new ModelEntityDeleteValidator(dbContext, tenantRegistryId, userName,
                dependencyStrings);
        }

        public static Task<CaseWorkflowService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowResources));
            var dependencyStrings = stringLocalizerFactory.Create(typeof(ModelDependencyResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflow.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflow.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings, dependencyStrings);
        }

        [Description("Lists every Case Workflow visible to the caller's tenant, across all parent models. " +
                     "Unbounded -- prefer ListAsync for agent/tool use.")]
        public async Task<List<CaseWorkflowDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflow.List", permissions);
                var dtos = CaseWorkflowMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflow.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"CaseWorkflow.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflow.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflows visible to the caller's tenant, Id-ascending, capped at 'take' rows. " +
                     "Call again with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("CaseWorkflowList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<CaseWorkflowDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("CaseWorkflow.List", permissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var all = CaseWorkflowMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<CaseWorkflowDto>(page);
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
                log.Error($"CaseWorkflow.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Case Workflows may use, " +
                     "with each field's type, the operators allowed for it and what it means. " +
                     "Use them as rule ids in CaseWorkflowFilter and CaseWorkflowCount.")]
        [ServiceOperation("CaseWorkflowFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflow.FilterFields", permissions);
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<CaseWorkflowDto>();
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
                log.Error($"CaseWorkflow.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Case Workflows in the caller's tenant matching query builder " +
                     "JSON (the same format as the rule builder, over the fields from " +
                     "CaseWorkflowFilterFields), ordered by id and capped at 'take' rows (max " +
                     "200). If 'more' is true, call again with 'afterId' set to the last " +
                     "returned Id to continue. Invalid JSON is not an error: Valid is false and " +
                     "Errors gives each problem with its JSON path.")]
        [ServiceOperation("CaseWorkflowFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<CaseWorkflowDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Case Workflows, using the fields from CaseWorkflowFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflow.Filter", permissions);
                var rows = CaseWorkflowMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"CaseWorkflow.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Case Workflows in the caller's tenant matching query builder " +
                     "JSON (over the fields from CaseWorkflowFilterFields; empty counts all), " +
                     "optionally broken down by the values of one field. Invalid JSON is not an " +
                     "error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("CaseWorkflowCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Case Workflows, using the fields from CaseWorkflowFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from CaseWorkflowFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflow.Count", permissions);
                var rows = CaseWorkflowMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"CaseWorkflow.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflows belonging to a parent model, visible to the caller's tenant, " +
                     "Id-ascending, including inactive ones.")]
        public async Task<List<CaseWorkflowDto>> GetByEntityAnalysisModelIdAsync(int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "ListByModel", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.ListByModel: entry entityAnalysisModelId={entityAnalysisModelId} " +
                          $"user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflow.ListByModel", listByModelPermissions);
                var dtos = CaseWorkflowMapper.ToDto(
                    await repository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflow.ListByModel: {dtos.Count} rows " +
                              $"entityAnalysisModelId={entityAnalysisModelId} user={userName}");
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
                log.Error($"CaseWorkflow.ListByModel: unexpected failure " +
                          $"entityAnalysisModelId={entityAnalysisModelId} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Case Workflows belonging to a parent model that the caller holds a role " +
                     "grant on, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowListByModelActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowDto>> GetByEntityAnalysisModelIdActiveOnlyAsync(
            [Description("Identifier of the parent model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "ListByModelActiveOnly", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.ListByModelActiveOnly: entry entityAnalysisModelId={entityAnalysisModelId} " +
                          $"user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflow.ListByModelActiveOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowMapper.ToDto(
                    await repository.GetByEntityAnalysisModelIdActiveOnlyAsync(entityAnalysisModelId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflow.ListByModelActiveOnly: {dtos.Count} rows " +
                              $"entityAnalysisModelId={entityAnalysisModelId} user={userName}");
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
                log.Error($"CaseWorkflow.ListByModelActiveOnly: unexpected failure " +
                          $"entityAnalysisModelId={entityAnalysisModelId} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Case Workflows belonging to a parent model (identified by the model's Guid) " +
                     "that the caller holds a role grant on, visible to the caller's tenant.")]
        public async Task<List<CaseWorkflowDto>> GetByEntityAnalysisModelGuidActiveOnlyAsync(
            Guid entityAnalysisModelGuid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "ListByModelGuidActiveOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug("CaseWorkflow.ListByModelGuidActiveOnly: entry " +
                          $"entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflow.ListByModelGuidActiveOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowMapper.ToDto(
                    await repository.GetByEntityAnalysisModelGuidActiveOnlyAsync(entityAnalysisModelGuid, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflow.ListByModelGuidActiveOnly: {dtos.Count} rows " +
                              $"entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}");
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
                log.Error("CaseWorkflow.ListByModelGuidActiveOnly: unexpected failure " +
                          $"entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Case Workflow by Id, or null if it doesn't exist or isn't visible to the " +
                     "caller's tenant.")]
        [ServiceOperation("CaseWorkflowGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Case Workflow.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflow.Get", permissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                var dto = CaseWorkflowMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflow.Get: id={id} not found or not visible to tenant user={userName}");
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
                log.Error($"CaseWorkflow.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Case Workflow under a parent model.")]
        [ServiceOperation("CaseWorkflowCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowDto> InsertAsync(CaseWorkflowDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.Insert: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflow.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflow.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.InsertAsync(CaseWorkflowMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[CaseWorkflowResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflow.Insert: created Id={saved.Id} name={saved.Name} user={userName}");
                }

                return CaseWorkflowMapper.ToDto(saved);
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
                log.Error($"CaseWorkflow.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates a Case Workflow without saving it, running every check a create (Id 0) or an " +
                     "update (any other Id) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("CaseWorkflowValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Case Workflow to validate.")]
            CaseWorkflowDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflow.Validate", permissions);

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
                log.Error($"CaseWorkflow.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Case Workflow. The row must exist, be visible to the caller's tenant, " +
                     "and not be locked.")]
        [ServiceOperation("CaseWorkflowUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseWorkflowDto> UpdateAsync(CaseWorkflowDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflow.Update", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflow.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.UpdateAsync(CaseWorkflowMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[CaseWorkflowResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflow.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return CaseWorkflowMapper.ToDto(saved);
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
                    log.Warn($"CaseWorkflow.Update: id={model?.Id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflow.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Case Workflow. The row must exist, be visible to the caller's tenant, and " +
                     "not be locked. Reversible only by direct database action.")]
        [ServiceOperation("CaseWorkflowDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Case Workflow to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflow", "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflow.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflow.Delete", permissions);
                var existing = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (existing != null)
                {
                    var dependents = await deleteValidator
                        .ValidateAsync(
                            new ModelEntityDelete(ModelEntityKind.CaseWorkflow, id, existing.EntityAnalysisModelId),
                            token)
                        .ConfigureAwait(false);
                    if (!dependents.IsValid)
                    {
                        throw new DtoValidationException(dependents);
                    }
                }

                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflow.Delete: soft-deleted Id={id} user={userName}");
                }
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
                    log.Warn($"CaseWorkflow.Delete: id={id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflow.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[CaseWorkflowResources.PermissionDenied], specs);
        }
    }
}