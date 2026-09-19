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
using Jube.Dto.Repository.CaseWorkflowMacro;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.CaseWorkflowMacro;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowMacro;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.CaseWorkflowMacroRepository;

namespace Jube.Service.Repository.CaseWorkflowMacro
{
    public sealed class CaseWorkflowMacroService
    {
        private static readonly int[] permissions = [24];
        private static readonly int[] activeOnlyPermissions = [24, 1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowMacroDtoValidator validator;

        private CaseWorkflowMacroService(DbContext dbContext, string userName, int tenantRegistryId,
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
            validator = new CaseWorkflowMacroDtoValidator(repository, strings);
        }

        public static Task<CaseWorkflowMacroService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowMacroService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowMacroResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowMacro.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowMacroResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowMacro.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowMacroResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowMacroService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Case Workflow Macro visible to the caller's tenant, across all parent Case " +
                     "Workflows. Unbounded -- prefer ListAsync for agent/tool use.")]
        public async Task<List<CaseWorkflowMacroDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowMacro", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowMacro.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowMacro.List", permissions);
                var dtos = CaseWorkflowMacroMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowMacro.List: {dtos.Count} rows user={userName}");
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
                log.Error($"CaseWorkflowMacro.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow Macros visible to the caller's tenant, Id-ascending, capped at " +
                     "'take' rows. Call again with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("CaseWorkflowMacroList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<CaseWorkflowMacroDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowMacro", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowMacro.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowMacro.List", permissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var all = CaseWorkflowMacroMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<CaseWorkflowMacroDto>(page);
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
                log.Error($"CaseWorkflowMacro.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow Macros belonging to a parent Case Workflow, visible to the caller's " +
                     "tenant, Id-ascending, including inactive ones.")]
        public async Task<List<CaseWorkflowMacroDto>> GetByCaseWorkflowIdAsync(int caseWorkflowId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowMacro", "ListByWorkflow", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowMacro.ListByWorkflow: entry caseWorkflowId={caseWorkflowId} " +
                          $"user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowMacro.ListByWorkflow", permissions);
                var dtos = CaseWorkflowMacroMapper.ToDto(
                    await repository.GetByCasesWorkflowIdOrderByIdAsync(caseWorkflowId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowMacro.ListByWorkflow: {dtos.Count} rows " +
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
                log.Error($"CaseWorkflowMacro.ListByWorkflow: unexpected failure " +
                          $"caseWorkflowId={caseWorkflowId} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Case Workflow Macros belonging to a parent Case Workflow that the caller " +
                     "holds a role grant on, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowMacroListByWorkflowIdActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowMacroDto>> GetByCasesWorkflowIdActiveOnlyAsync(
            [Description("Identifier of the parent Case Workflow.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowMacro", "ListByWorkflowIdActiveOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowMacro.ListByWorkflowIdActiveOnly: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowMacro.ListByWorkflowIdActiveOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowMacroMapper.ToDto(
                    await repository.GetByCasesWorkflowIdActiveOnlyAsync(id, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowMacro.ListByWorkflowIdActiveOnly: {dtos.Count} rows id={id} " +
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
                log.Error($"CaseWorkflowMacro.ListByWorkflowIdActiveOnly: unexpected failure id={id} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Case Workflow Macros belonging to a parent Case Workflow (identified by " +
                     "the workflow's Guid) that the caller holds a role grant on, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowMacroListByWorkflowGuidActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowMacroDto>> GetByCasesWorkflowGuidActiveOnlyAsync(
            [Description("Guid of the parent Case Workflow.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowMacro", "ListByWorkflowGuidActiveOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowMacro.ListByWorkflowGuidActiveOnly: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowMacro.ListByWorkflowGuidActiveOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowMacroMapper.ToDto(
                    await repository.GetByCasesWorkflowGuidActiveOnlyAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowMacro.ListByWorkflowGuidActiveOnly: {dtos.Count} rows guid={guid} " +
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
                log.Error($"CaseWorkflowMacro.ListByWorkflowGuidActiveOnly: unexpected failure guid={guid} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Case Workflow Macro by Id, or null if it doesn't exist or isn't " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowMacroGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowMacroDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Case Workflow Macro.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowMacro", "Get", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowMacro.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowMacro.Get", permissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                var dto = CaseWorkflowMacroMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowMacro.Get: id={id} not found or not visible to tenant " +
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
                log.Error($"CaseWorkflowMacro.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Case Workflow Macro under a parent Case Workflow.")]
        [ServiceOperation("CaseWorkflowMacroCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowMacroDto> InsertAsync(CaseWorkflowMacroDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowMacro", "Insert", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowMacro.Insert: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowMacro.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowMacro.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(CaseWorkflowMacroMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowMacro.Insert: created Id={saved.Id} name={saved.Name} " +
                             $"user={userName}");
                }

                return CaseWorkflowMacroMapper.ToDto(saved);
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
                log.Error($"CaseWorkflowMacro.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Updates an existing Case Workflow Macro. The row must exist, be visible to the " +
                     "caller's tenant, and not be locked.")]
        [ServiceOperation("CaseWorkflowMacroUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseWorkflowMacroDto> UpdateAsync(CaseWorkflowMacroDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowMacro", "Update", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowMacro.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowMacro.Update", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowMacro.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.UpdateAsync(CaseWorkflowMacroMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowMacro.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return CaseWorkflowMacroMapper.ToDto(saved);
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
                    log.Warn($"CaseWorkflowMacro.Update: id={model?.Id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowMacroResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowMacro.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Case Workflow Macro. The row must exist, be visible to the caller's " +
                     "tenant, and not be locked. Reversible only by direct database action.")]
        [ServiceOperation("CaseWorkflowMacroDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Case Workflow Macro to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowMacro", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowMacro.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowMacro.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowMacro.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"CaseWorkflowMacro.Delete: id={id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowMacroResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowMacro.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[CaseWorkflowMacroResources.PermissionDenied], specs);
        }
    }
}