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
using Jube.Dto.Repository.UserInTenant;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.UserInTenant;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.UserInTenant;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.UserInTenantRepository;

namespace Jube.Service.Repository.UserInTenant
{
    public sealed class UserInTenantService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int callerTenantRegistryId;
        private readonly string userName;
        private readonly UserInTenantDtoValidator validator;

        private UserInTenantService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            callerTenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new RuleRepository(dbContext, userName);
            validator = new UserInTenantDtoValidator(repository, strings);
        }

        public static Task<UserInTenantService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<UserInTenantService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(UserInTenantResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("UserInTenant.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[UserInTenantResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await RuleRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"UserInTenant.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[UserInTenantResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new UserInTenantService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the users allocated to the caller's own tenant. A Landlord caller sees only the " +
                     "allocations within the tenant the Landlord is currently switched to, not every tenant.")]
        public async Task<List<UserInTenantDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserInTenant", "List", userName, callerTenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserInTenant.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("UserInTenant.List", permissions);
                var dtos = UserInTenantMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"UserInTenant.List: {dtos.Count} rows user={userName}");
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
                log.Error($"UserInTenant.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the users allocated to the caller's own tenant, capped at 'take' rows (max 200). " +
                     "If 'more' is true, call again with 'afterId' set to the last returned Id to continue.")]
        [ServiceOperation("UserInTenantList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<UserInTenantDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserInTenant", "ListPaged", userName, callerTenantRegistryId, auditLog,
                log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserInTenant.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("UserInTenant.ListPaged", permissions);

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"UserInTenant.ListPaged: {page.Count} rows user={userName}");
                }

                return new PagedResult<UserInTenantDto>(UserInTenantMapper.ToDto(page));
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
                log.Error($"UserInTenant.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the tenant allocation of the caller. Used to preselect the caller's current tenant " +
                     "in the tenant switcher; returns null when the resolved user has no allocation row.")]
        [ServiceOperation("UserInTenantGetCurrentTenantRegistry", OperationKind.Read, Idempotent = true)]
        public Task<UserInTenantDto> GetCurrentTenantRegistryAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserInTenant", "GetCurrentTenantRegistry", userName,
                callerTenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserInTenant.GetCurrentTenantRegistry: entry user={userName}");
            }

            try
            {
                EnsurePermitted("UserInTenant.GetCurrentTenantRegistry", permissions);
                var entity = repository.GetCurrentTenantRegistry();
                var dto = UserInTenantMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"UserInTenant.GetCurrentTenantRegistry: not found user={userName}");
                }

                return Task.FromResult(dto);
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
                log.Error($"UserInTenant.GetCurrentTenantRegistry: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Switches the caller to a different Tenant Registry. Restricted to a caller belonging to a " +
                     "Landlord tenant, which bypasses per-tenant authorisation entirely. Writes a switch-log audit " +
                     "row. Idempotent -- switching to the tenant the caller is already in has no further effect.")]
        [ServiceOperation("UserInTenantSwitchTenant", OperationKind.Write, Idempotent = true)]
        public async Task UpdateAsync(
            [Description("Identifier of the Tenant Registry to switch the caller into.")]
            int tenantRegistryId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserInTenant", "Update", userName, callerTenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserInTenant.Update: entry tenantRegistryId={tenantRegistryId} user={userName}");
            }

            try
            {
                EnsureLandlord("UserInTenant.Update");

                var model = new UserInTenantDto
                {
                    User = userName,
                    TenantRegistryId = tenantRegistryId
                };

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"UserInTenant.Update: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.UpdateAsync(userName, tenantRegistryId, token).ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"UserInTenant.Update: switched Id={saved.Id} tenantRegistryId={tenantRegistryId} " +
                             $"user={userName}");
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
                    log.Warn($"UserInTenant.Update: user={userName} not found, not visible, or locked");
                }

                throw new NotFoundException(strings[UserInTenantResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserInTenant.Update: unexpected failure tenantRegistryId={tenantRegistryId} " +
                          $"user={userName}", ex);
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

            throw new ForbiddenException(strings[UserInTenantResources.PermissionDenied], specs);
        }

        private void EnsureLandlord(string op)
        {
            if (permissionValidation.Landlord)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied (not landlord) user={userName}");
            }

            throw new ForbiddenException(strings[UserInTenantResources.PermissionDenied], []);
        }
    }
}