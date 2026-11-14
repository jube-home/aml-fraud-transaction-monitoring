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
using Jube.Dto.Repository.CaseSearchFilter;
using Jube.Resources;
using Jube.Dto.Validation;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.Repository.CaseSearchFilter;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseSearchFilter;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.CaseSearchFilter
{
    public sealed class CaseSearchFilterService
    {
        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseSearchFilterRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseSearchFilterDtoValidator validator;

        private CaseSearchFilterService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new CaseSearchFilterRepository(dbContext, userName, tenantRegistryId);
            validator = new CaseSearchFilterDtoValidator(repository, strings);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseSearchFilterService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseSearchFilterService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseSearchFilterResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseSearchFilter.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseSearchFilterResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseSearchFilter.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseSearchFilterResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseSearchFilterService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the saved searches the caller can run on the Cases page: the ones they own plus the " +
                     "ones other users have shared with the caller's role, for Case Workflows the caller's role " +
                     "is granted. Optionally limited to one Case Workflow. Ordered by name.")]
        [ServiceOperation("CaseSearchFilterList", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseSearchFilterDto>> ListAsync(
            [Description("Guid of a Case Workflow to limit the list to; omit for every Case Workflow.")]
            Guid? caseWorkflowGuid = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSearchFilter", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseSearchFilter.List: entry caseWorkflowGuid={caseWorkflowGuid} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseSearchFilter.List");

                var rows = await repository.ListVisibleAsync(caseWorkflowGuid, token).ConfigureAwait(false);
                var dtos = rows.Select(s => CaseSearchFilterMapper.ToDto(s, s.CaseWorkflow.Guid, userName)).ToList();
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
                log.Error($"CaseSearchFilter.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns one saved search by Guid when the caller owns it or it is shared with the " +
                     "caller's role. Fails as not found otherwise.")]
        [ServiceOperation("CaseSearchFilterGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseSearchFilterDto> GetByGuidAsync(
            [Description("Guid of the saved search.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSearchFilter", "Get", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseSearchFilter.Get: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseSearchFilter.Get");

                var row = await repository.GetVisibleByGuidAsync(guid, token).ConfigureAwait(false);

                if (row == null)
                {
                    throw new NotFoundException(strings[CaseSearchFilterResources.NotFound]);
                }

                op.Entity(row.Id);
                op.Version(row.Version);

                return CaseSearchFilterMapper.ToDto(row, row.CaseWorkflow.Guid, userName);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
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
                log.Error($"CaseSearchFilter.Get: unexpected failure guid={guid} user={userName}", ex);
                throw;
            }
        }

        [Description("Saves a new search for the caller against a Case Workflow they are granted. The name must " +
                     "be unique among the caller's own searches for that Case Workflow, ignoring case. The search " +
                     "is private unless a role to share it with is given. SIDE EFFECTS: creates a record.")]
        [ServiceOperation("CaseSearchFilterCreate", OperationKind.Write)]
        public async Task<CaseSearchFilterDto> InsertAsync(CaseSearchFilterDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSearchFilter", "Insert", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseSearchFilter.Insert: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseSearchFilter.Insert");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    LogInvalid("Insert", results);
                    throw new DtoValidationException(results);
                }

                var caseWorkflowId = await repository
                    .GetAccessibleCaseWorkflowIdAsync(model.CaseWorkflowGuid, token).ConfigureAwait(false);

                var poco = CaseSearchFilterMapper.ToPoco(model, caseWorkflowId!.Value);
                poco.Guid = Guid.Empty;

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.InsertAsync(poco, token),
                        r => new DtoValidationException(r), strings[CaseSearchFilterResources.NameAlreadyExists])
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Created();

                return CaseSearchFilterMapper.ToDto(saved, model.CaseWorkflowGuid, userName);
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
                log.Error($"CaseSearchFilter.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Validates a saved search without storing it, running every check a create (empty Guid) or " +
                     "an update (an existing Guid) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("CaseSearchFilterValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(CaseSearchFilterDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSearchFilter", "Validate", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseSearchFilter.Validate: entry guid={model?.Guid} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseSearchFilter.Validate");

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
                log.Error($"CaseSearchFilter.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Changes one of the caller's own saved searches: its name, builder JSON and the role it is " +
                     "shared with. Only the owner can change a search, and it cannot be moved to another Case " +
                     "Workflow. A stale Version is refused. SIDE EFFECTS: overwrites the stored search.")]
        [ServiceOperation("CaseSearchFilterUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseSearchFilterDto> UpdateAsync(CaseSearchFilterDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSearchFilter", "Update", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseSearchFilter.Update: entry guid={model?.Guid} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseSearchFilter.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    LogInvalid("Update", results);
                    throw new DtoValidationException(results);
                }

                var existing = await repository.GetOwnedByGuidAsync(model.Guid, token).ConfigureAwait(false);

                if (existing == null)
                {
                    throw new NotFoundException(strings[CaseSearchFilterResources.NotFound]);
                }

                if (existing.CaseWorkflow.Guid != model.CaseWorkflowGuid)
                {
                    throw new DtoValidationException(new ValidationResult([
                        new ValidationFailure(nameof(CaseSearchFilterDto.CaseWorkflowGuid),
                            strings[CaseSearchFilterResources.CaseWorkflowCannotChange])
                        {
                            ErrorCode = "CaseWorkflowGuidCannotChange"
                        }
                    ]));
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.UpdateAsync(CaseSearchFilterMapper.ToPoco(model, existing.CaseWorkflowId),
                            token),
                        r => new DtoValidationException(r), strings[CaseSearchFilterResources.NameAlreadyExists])
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Updated();

                return CaseSearchFilterMapper.ToDto(saved, model.CaseWorkflowGuid, userName);
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
                throw new NotFoundException(strings[CaseSearchFilterResources.NotFound]);
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseSearchFilter.Update: unexpected failure guid={model?.Guid} user={userName}", ex);
                throw;
            }
        }

        [Description("Permanently deletes one of the caller's own saved searches. Only the owner can delete a " +
                     "search; a search shared with a role disappears for everyone it was shared with. Cannot be " +
                     "undone.")]
        [ServiceOperation("CaseSearchFilterDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Guid of the saved search to delete.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSearchFilter", "Delete", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseSearchFilter.Delete: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseSearchFilter.Delete");

                var id = await repository.DeleteAsync(guid, token).ConfigureAwait(false);

                op.Entity(id);
                op.Deleted();
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (KeyNotFoundException)
            {
                op.Outcome("not_found");
                throw new NotFoundException(strings[CaseSearchFilterResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseSearchFilter.Delete: unexpected failure guid={guid} user={userName}", ex);
                throw;
            }
        }

        private void LogInvalid(string operation, ValidationResult results)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn($"CaseSearchFilter.{operation}: validation failed user={userName} " +
                         $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
            }
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

            throw new ForbiddenException(strings[CaseSearchFilterResources.PermissionDenied], permissions);
        }
    }
}
