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
using FluentValidation.Results;
using Jube.Data.Concurrency;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.CaseWorkflowChecklist;
using Jube.Dto.Validation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Repository.CaseWorkflowChecklist;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowChecklist;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.CaseWorkflowChecklistRepository;

namespace Jube.Service.Repository.CaseWorkflowChecklist
{
    public sealed class CaseWorkflowChecklistService
    {
        private const string Area = "CaseWorkflowChecklist";
        private static readonly int[] permissions = [19];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowChecklistDtoValidator validator;

        private CaseWorkflowChecklistService(DbContext dbContext, string userName, int tenantRegistryId,
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
            validator = new CaseWorkflowChecklistDtoValidator(repository, strings);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseWorkflowChecklistService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowChecklistService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowChecklistResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowChecklistResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowChecklistResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowChecklistService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Case Workflow checklist visible to the caller's tenant, ordered by Priority " +
                     "ascending then name.")]
        [ServiceOperation("CaseWorkflowChecklistList", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowChecklistDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.List");

                var dtos = CaseWorkflowChecklistMapper.ToDto(await repository.GetAllAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);

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
                log.Error($"{Area}.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the checklists carried by a Case Workflow Status, ordered by Priority ascending then " +
                     "name, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowChecklistListByCaseWorkflowStatusId", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowChecklistDto>> GetByCaseWorkflowStatusIdAsync(
            [Description("Server-assigned identifier of the Case Workflow Status to list checklists for.")]
            int caseWorkflowStatusId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "ListByCaseWorkflowStatusId", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.ListByCaseWorkflowStatusId");

                var dtos = CaseWorkflowChecklistMapper.ToDto(await repository
                    .GetByCaseWorkflowStatusIdAsync(caseWorkflowStatusId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);

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
                log.Error($"{Area}.ListByCaseWorkflowStatusId: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Case Workflow checklist by Id, or null if it doesn't exist or isn't " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowChecklistGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowChecklistDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the checklist.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.Get");

                return CaseWorkflowChecklistMapper.ToDto(await repository.GetByIdAsync(id, token)
                    .ConfigureAwait(false));
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
                log.Error($"{Area}.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a Case Workflow checklist: a named group of items carried by one Case Workflow " +
                     "Status, ordered by Priority. Every item shown on a case in the status must be ticked " +
                     "before the case can leave the status or be closed.")]
        [ServiceOperation("CaseWorkflowChecklistCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowChecklistDto> InsertAsync(CaseWorkflowChecklistDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Insert");

                await ResolveStatusAsync(model, token).ConfigureAwait(false);
                await ValidateOrThrowAsync(model, "Insert", token).ConfigureAwait(false);

                var saved = await repository.InsertAsync(CaseWorkflowChecklistMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.Insert: created Id={saved.Id} user={userName}");
                }

                return CaseWorkflowChecklistMapper.ToDto(saved);
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
                log.Error($"{Area}.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Validates a Case Workflow checklist without saving it, running every check a create " +
                     "(Id 0) or an update (any other Id) would run, and returns each failure. Nothing is stored.")]
        [ServiceOperation("CaseWorkflowChecklistValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Case Workflow checklist to validate.")]
            CaseWorkflowChecklistDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Validate");

                await ResolveStatusAsync(model, token).ConfigureAwait(false);

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
                log.Error($"{Area}.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Case Workflow checklist. The row must exist and be visible to the " +
                     "caller's tenant; the status that carries it cannot be changed.")]
        [ServiceOperation("CaseWorkflowChecklistUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseWorkflowChecklistDto> UpdateAsync(CaseWorkflowChecklistDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Update");

                var existing = await repository.GetByIdAsync(model.Id, token).ConfigureAwait(false);

                if (existing == null)
                {
                    throw new KeyNotFoundException();
                }

                model.CaseWorkflowStatusGuid = existing.CaseWorkflowStatusGuid;
                model.CaseWorkflowStatusId = existing.CaseWorkflowStatusId;

                await ValidateOrThrowAsync(model, "Update", token).ConfigureAwait(false);

                var saved = await repository.UpdateAsync(CaseWorkflowChecklistMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return CaseWorkflowChecklistMapper.ToDto(saved);
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

                throw new NotFoundException(strings[CaseWorkflowChecklistResources.NotFound]);
            }
            catch (VersionConflictException)
            {
                op.Outcome("conflict");
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
                log.Error($"{Area}.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Case Workflow checklist. Reversible only by direct database action.")]
        [ServiceOperation("CaseWorkflowChecklistDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the checklist to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.Delete");

                var existing = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

                if (existing == null)
                {
                    throw new KeyNotFoundException();
                }

                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.Delete: soft-deleted Id={id} user={userName}");
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

                throw new NotFoundException(strings[CaseWorkflowChecklistResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"{Area}.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private async Task ResolveStatusAsync(CaseWorkflowChecklistDto model, CancellationToken token)
        {
            if (model.CaseWorkflowStatusGuid != Guid.Empty || model.CaseWorkflowStatusId <= 0)
            {
                return;
            }

            var status = await repository.GetStatusByIdAsync(model.CaseWorkflowStatusId, token)
                .ConfigureAwait(false);

            if (status != null)
            {
                model.CaseWorkflowStatusGuid = status.Guid;
            }
        }

        private async Task ValidateOrThrowAsync(CaseWorkflowChecklistDto model, string operation,
            CancellationToken token)
        {
            var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

            if (results.IsValid)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{Area}.{operation}: validation failed user={userName} " +
                         $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
            }

            throw new DtoValidationException(results);
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

            throw new ForbiddenException(strings[CaseWorkflowChecklistResources.PermissionDenied], permissions);
        }
    }
}
