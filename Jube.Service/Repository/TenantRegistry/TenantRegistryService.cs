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
using Jube.Dto.Repository.TenantRegistry;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.TenantRegistry;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.TenantRegistry;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.TenantRegistryRepository;

namespace Jube.Service.Repository.TenantRegistry
{
    public sealed class TenantRegistryService
    {
        private const int MaxListTake = 200;

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly TenantRegistryDtoValidator validator;

        private TenantRegistryService(DbContext dbContext, string userName, int tenantRegistryId,
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
            validator = new TenantRegistryDtoValidator(repository, strings);
        }

        public static Task<TenantRegistryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<TenantRegistryService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(TenantRegistryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("TenantRegistry.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[TenantRegistryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"TenantRegistry.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[TenantRegistryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new TenantRegistryService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every registered Tenant. Landlord-only: the acting user's own tenant must be " +
                     "designated Landlord, since a Tenant cannot see or manage other Tenants. Unbounded -- " +
                     "prefer ListAsync for agent/tool use.")]
        public async Task<List<TenantRegistryDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("TenantRegistry", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"TenantRegistry.List: entry user={userName}");
            }

            try
            {
                EnsureLandlord("TenantRegistry.List");
                var dtos = TenantRegistryMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"TenantRegistry.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"TenantRegistry.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"TenantRegistry.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists registered Tenants whose Name contains the given case-insensitive filter text, or " +
                     "every Tenant when the filter is blank. Backs the Kendo grid 'contains' filter on the " +
                     "Tenants page. Landlord-only.")]
        public async Task<List<TenantRegistryDto>> GetByFilterAsync(
            [Description("Case-insensitive substring to match against Name; blank matches every row.")]
            string? filter,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("TenantRegistry", "ListByFilter", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"TenantRegistry.ListByFilter: entry filter={filter} user={userName}");
            }

            try
            {
                EnsureLandlord("TenantRegistry.ListByFilter");
                var dtos = TenantRegistryMapper.ToDto(
                    await repository.GetByFilterAsync(filter ?? string.Empty, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"TenantRegistry.ListByFilter: {dtos.Count} rows filter={filter} user={userName}");
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
                    log.Debug($"TenantRegistry.ListByFilter: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"TenantRegistry.ListByFilter: unexpected failure filter={filter} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists registered Tenants, Id-ascending, capped at 'take' rows. Call again with 'afterId' " +
                     "set to the last Id returned to page further. Landlord-only.")]
        [ServiceOperation("TenantRegistryList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<TenantRegistryDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("TenantRegistry", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"TenantRegistry.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsureLandlord("TenantRegistry.List");
                var clampedTake = Math.Clamp(take, 1, MaxListTake);
                var all = TenantRegistryMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<TenantRegistryDto>(page);
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
                log.Error($"TenantRegistry.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Tenant by Id, or null if it doesn't exist. Landlord-only.")]
        [ServiceOperation("TenantRegistryGet", OperationKind.Read, Idempotent = true)]
        public async Task<TenantRegistryDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Tenant.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("TenantRegistry", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"TenantRegistry.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsureLandlord("TenantRegistry.Get");
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                var dto = TenantRegistryMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"TenantRegistry.Get: id={id} not found user={userName}");
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
                log.Error($"TenantRegistry.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Tenant. Landlord-only.")]
        [ServiceOperation("TenantRegistryCreate", OperationKind.Write, Idempotent = false)]
        public async Task<TenantRegistryDto> InsertAsync(TenantRegistryDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("TenantRegistry", "Insert", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"TenantRegistry.Insert: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsureLandlord("TenantRegistry.Insert");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"TenantRegistry.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(TenantRegistryMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"TenantRegistry.Insert: created Id={saved.Id} name={saved.Name} user={userName}");
                }

                return TenantRegistryMapper.ToDto(saved);
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
                log.Error($"TenantRegistry.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Updates an existing Tenant. The row must exist and not be locked. Landlord-only.")]
        [ServiceOperation("TenantRegistryUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<TenantRegistryDto> UpdateAsync(TenantRegistryDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("TenantRegistry", "Update", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"TenantRegistry.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsureLandlord("TenantRegistry.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"TenantRegistry.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.UpdateAsync(TenantRegistryMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"TenantRegistry.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return TenantRegistryMapper.ToDto(saved);
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
                    log.Warn($"TenantRegistry.Update: id={model?.Id} not found or locked user={userName}");
                }

                throw new NotFoundException(strings[TenantRegistryResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"TenantRegistry.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Tenant. The row must exist and not be locked. Reversible only by direct " +
                     "database action. Landlord-only.")]
        [ServiceOperation("TenantRegistryDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Tenant to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("TenantRegistry", "Delete", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"TenantRegistry.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsureLandlord("TenantRegistry.Delete");
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"TenantRegistry.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"TenantRegistry.Delete: id={id} not found or locked user={userName}");
                }

                throw new NotFoundException(strings[TenantRegistryResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"TenantRegistry.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsureLandlord(string op)
        {
            if (permissionValidation.Landlord)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied (caller's tenant is not Landlord) user={userName}");
            }

            throw new ForbiddenException(strings[TenantRegistryResources.PermissionDenied]);
        }
    }
}