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
using Jube.Dto.Repository.CaseWorkflowXPath;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.CaseWorkflowXPath;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowXPath;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.CaseWorkflowXPathRepository;

namespace Jube.Service.Repository.CaseWorkflowXPath
{
    public sealed class CaseWorkflowXPathService
    {
        private static readonly int[] permissions = [20];
        private static readonly int[] activeOnlyPermissions = [20, 1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowXPathDtoValidator validator;

        private CaseWorkflowXPathService(DbContext dbContext, string userName, int tenantRegistryId,
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
            validator = new CaseWorkflowXPathDtoValidator(repository, strings);
        }

        public static Task<CaseWorkflowXPathService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowXPathService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowXPathResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowXPath.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowXPathResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowXPath.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowXPathResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowXPathService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Case Workflow XPath extraction visible to the caller's tenant, across all " +
                     "parent Case Workflows. Unbounded -- prefer ListAsync for agent/tool use.")]
        public async Task<List<CaseWorkflowXPathDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPath", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPath.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowXPath.List", permissions);
                var dtos = CaseWorkflowXPathMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowXPath.List: {dtos.Count} rows user={userName}");
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
                log.Error($"CaseWorkflowXPath.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow XPath extractions visible to the caller's tenant, Id-ascending, " +
                     "capped at 'take' rows. Call again with 'afterId' set to the last Id returned to page " +
                     "further.")]
        [ServiceOperation("CaseWorkflowXPathList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<CaseWorkflowXPathDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPath", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPath.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowXPath.List", permissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var all = CaseWorkflowXPathMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<CaseWorkflowXPathDto>(page);
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
                log.Error($"CaseWorkflowXPath.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Case Workflow XPath extractions belonging to a parent Case Workflow, visible to " +
                     "the caller's tenant, Id-ascending, including inactive ones.")]
        public async Task<List<CaseWorkflowXPathDto>> GetByCaseWorkflowIdAsync(int caseWorkflowId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPath", "ListByWorkflow", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPath.ListByWorkflow: entry caseWorkflowId={caseWorkflowId} " +
                          $"user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowXPath.ListByWorkflow", permissions);
                var dtos = CaseWorkflowXPathMapper.ToDto(
                    await repository.GetByCasesWorkflowIdOrderByIdDescAsync(caseWorkflowId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowXPath.ListByWorkflow: {dtos.Count} rows " +
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
                log.Error($"CaseWorkflowXPath.ListByWorkflow: unexpected failure " +
                          $"caseWorkflowId={caseWorkflowId} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active, Drill-enabled Case Workflow XPath extractions belonging to a parent Case " +
                     "Workflow that the caller holds a role grant on, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowXPathListByWorkflowIdActiveDrillOnly", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowXPathDto>> GetByCasesWorkflowIdActiveOnlyAsync(
            [Description("Identifier of the parent Case Workflow.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPath", "ListByWorkflowIdActiveDrillOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPath.ListByWorkflowIdActiveDrillOnly: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowXPath.ListByWorkflowIdActiveDrillOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowXPathMapper.ToDto(
                    await repository.GetByCasesWorkflowIdActiveDrillOnlyAsync(id, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowXPath.ListByWorkflowIdActiveDrillOnly: {dtos.Count} rows id={id} " +
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
                log.Error($"CaseWorkflowXPath.ListByWorkflowIdActiveDrillOnly: unexpected failure id={id} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active, Drill-enabled Case Workflow XPath extractions belonging to a parent Case " +
                     "Workflow (identified by the workflow's Guid) that the caller holds a role grant on, " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowXPathListByWorkflowGuidActiveDrillOnly", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<CaseWorkflowXPathDto>> GetByCasesWorkflowGuidActiveOnlyAsync(
            [Description("Guid of the parent Case Workflow.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPath", "ListByWorkflowGuidActiveDrillOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPath.ListByWorkflowGuidActiveDrillOnly: entry guid={guid} " +
                          $"user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowXPath.ListByWorkflowGuidActiveDrillOnly", activeOnlyPermissions);
                var dtos = CaseWorkflowXPathMapper.ToDto(
                    await repository.GetByCasesWorkflowGuidActiveDrillOnlyAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowXPath.ListByWorkflowGuidActiveDrillOnly: {dtos.Count} rows " +
                              $"guid={guid} user={userName}");
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
                log.Error($"CaseWorkflowXPath.ListByWorkflowGuidActiveDrillOnly: unexpected failure guid={guid} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Case Workflow XPath extraction by Id, or null if it doesn't exist or " +
                     "isn't visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowXPathGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowXPathDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Case Workflow XPath extraction.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPath", "Get", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPath.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowXPath.Get", permissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                var dto = CaseWorkflowXPathMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowXPath.Get: id={id} not found or not visible to tenant user={userName}");
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
                log.Error($"CaseWorkflowXPath.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Case Workflow XPath extraction under a parent Case Workflow.")]
        [ServiceOperation("CaseWorkflowXPathCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowXPathDto> InsertAsync(CaseWorkflowXPathDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPath", "Insert", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPath.Insert: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowXPath.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowXPath.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(CaseWorkflowXPathMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowXPath.Insert: created Id={saved.Id} name={saved.Name} " +
                             $"user={userName}");
                }

                return CaseWorkflowXPathMapper.ToDto(saved);
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
                log.Error($"CaseWorkflowXPath.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Updates an existing Case Workflow XPath extraction. The row must exist, be visible to " +
                     "the caller's tenant, and not be locked.")]
        [ServiceOperation("CaseWorkflowXPathUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseWorkflowXPathDto> UpdateAsync(CaseWorkflowXPathDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPath", "Update", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPath.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowXPath.Update", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowXPath.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.UpdateAsync(CaseWorkflowXPathMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowXPath.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return CaseWorkflowXPathMapper.ToDto(saved);
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
                    log.Warn($"CaseWorkflowXPath.Update: id={model?.Id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowXPathResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowXPath.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Case Workflow XPath extraction. The row must exist, be visible to the " +
                     "caller's tenant, and not be locked. Reversible only by direct database action.")]
        [ServiceOperation("CaseWorkflowXPathDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Case Workflow XPath extraction to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowXPath", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowXPath.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowXPath.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowXPath.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"CaseWorkflowXPath.Delete: id={id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[CaseWorkflowXPathResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseWorkflowXPath.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[CaseWorkflowXPathResources.PermissionDenied], specs);
        }
    }
}