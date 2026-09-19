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
using Jube.Dto.Repository.RoleRegistryPermission;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.RoleRegistryPermission;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.RoleRegistryPermission;
using log4net;
using Microsoft.Extensions.Localization;
using RulePoco = Jube.Data.Poco.RoleRegistryPermission;
using RuleRepository = Jube.Data.Repository.RoleRegistryPermissionRepository;

namespace Jube.Service.Repository.RoleRegistryPermission
{
    public sealed class RoleRegistryPermissionService
    {
        private static readonly int[] permissions = [36];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly RoleRegistryPermissionDtoValidator validator;

        private RoleRegistryPermissionService(DbContext dbContext, string userName, int tenantRegistryId,
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
            validator = new RoleRegistryPermissionDtoValidator(repository, strings);
        }

        public static Task<RoleRegistryPermissionService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<RoleRegistryPermissionService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(RoleRegistryPermissionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("RoleRegistryPermission.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[RoleRegistryPermissionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"RoleRegistryPermission.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[RoleRegistryPermissionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new RoleRegistryPermissionService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Role Registry Permission grant visible to the caller's tenant, across all " +
                     "parent Role Registries. Unbounded -- prefer ListAsync for agent/tool use.")]
        public async Task<List<RoleRegistryPermissionDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistryPermission", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistryPermission.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("RoleRegistryPermission.List");
                var dtos = RoleRegistryPermissionMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"RoleRegistryPermission.List: {dtos.Count} rows user={userName}");
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
                log.Error($"RoleRegistryPermission.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Role Registry Permission grants visible to the caller's tenant, Id-ascending, " +
                     "capped at 'take' rows. Call again with 'afterId' set to the last Id returned to page " +
                     "further.")]
        [ServiceOperation("RoleRegistryPermissionList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<RoleRegistryPermissionDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistryPermission", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistryPermission.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("RoleRegistryPermission.List");
                var clampedTake = Math.Clamp(take, 1, 200);
                var all = RoleRegistryPermissionMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<RoleRegistryPermissionDto>(page);
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
                log.Error($"RoleRegistryPermission.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Role Registry Permission grant by Id, or null if it doesn't exist or " +
                     "isn't visible to the caller's tenant.")]
        [ServiceOperation("RoleRegistryPermissionGet", OperationKind.Read, Idempotent = true)]
        public async Task<RoleRegistryPermissionDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Role Registry Permission grant.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistryPermission", "Get", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistryPermission.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("RoleRegistryPermission.Get");
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                var dto = RoleRegistryPermissionMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"RoleRegistryPermission.Get: id={id} not found or not visible to tenant " +
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
                log.Error($"RoleRegistryPermission.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Grants a Permission Specification to a Role Registry.")]
        [ServiceOperation("RoleRegistryPermissionCreate", OperationKind.Write, Idempotent = false)]
        public async Task<RoleRegistryPermissionDto> InsertAsync(RoleRegistryPermissionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistryPermission", "Insert", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistryPermission.Insert: entry user={userName} " +
                          $"roleRegistryId={model?.RoleRegistryId} " +
                          $"permissionSpecificationId={model?.PermissionSpecificationId}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("RoleRegistryPermission.Insert");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"RoleRegistryPermission.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(RoleRegistryPermissionMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"RoleRegistryPermission.Insert: created Id={saved.Id} user={userName}");
                }

                return RoleRegistryPermissionMapper.ToDto(saved);
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
                log.Error($"RoleRegistryPermission.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Role Registry Permission grant. The row must exist, be visible to " +
                     "the caller's tenant, and not be locked.")]
        [ServiceOperation("RoleRegistryPermissionUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<RoleRegistryPermissionDto> UpdateAsync(RoleRegistryPermissionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistryPermission", "Update", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistryPermission.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("RoleRegistryPermission.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"RoleRegistryPermission.Update: validation failed user={userName} " +
                                 $"id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                RulePoco saved;
                try
                {
                    saved = await repository.UpdateAsync(RoleRegistryPermissionMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"RoleRegistryPermission.Update: id={model.Id} not found, not visible, or " +
                                 $"locked user={userName}");
                    }

                    throw new NotFoundException(strings[RoleRegistryPermissionResources.NotFound]);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"RoleRegistryPermission.Update: Id={saved.Id} version={saved.Version} " +
                             $"user={userName}");
                }

                return RoleRegistryPermissionMapper.ToDto(saved);
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
                log.Error($"RoleRegistryPermission.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Removes a Role Registry Permission grant. The row must exist, be visible to the " +
                     "caller's tenant, and not be locked. Soft-delete, reversible only by direct database " +
                     "action.")]
        [ServiceOperation("RoleRegistryPermissionDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Role Registry Permission grant to remove.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistryPermission", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistryPermission.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("RoleRegistryPermission.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"RoleRegistryPermission.Delete: id={id} not found, not visible, or locked " +
                                 $"user={userName}");
                    }

                    throw new NotFoundException(strings[RoleRegistryPermissionResources.NotFound]);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"RoleRegistryPermission.Delete: soft-deleted Id={id} user={userName}");
                }
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
                log.Error($"RoleRegistryPermission.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
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

            throw new ForbiddenException(strings[RoleRegistryPermissionResources.PermissionDenied], permissions);
        }
    }
}