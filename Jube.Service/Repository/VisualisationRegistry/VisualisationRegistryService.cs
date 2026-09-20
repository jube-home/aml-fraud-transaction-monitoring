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
using Jube.Dto.Repository.VisualisationRegistry;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.Repository.VisualisationRegistry;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.VisualisationRegistry;
using log4net;
using Microsoft.Extensions.Localization;
using RegistryRepository = Jube.Data.Repository.VisualisationRegistryRepository;

namespace Jube.Service.Repository.VisualisationRegistry
{
    public sealed class VisualisationRegistryService
    {
        private static readonly int[] listPermissions = [18, 31, 32, 33];
        private static readonly int[] readPermissions = [31, 28, 1];
        private static readonly int[] showInDirectoryPermissions = [28];
        private static readonly int[] writePermissions = [31];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RegistryRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly VisualisationRegistryDtoValidator validator;

        private VisualisationRegistryService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new RegistryRepository(dbContext, userName);
            validator = new VisualisationRegistryDtoValidator(repository, strings);
        }

        public static Task<VisualisationRegistryService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<VisualisationRegistryService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(VisualisationRegistryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("VisualisationRegistry.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[VisualisationRegistryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"VisualisationRegistry.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[VisualisationRegistryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new VisualisationRegistryService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Visualisation Registry visible to the caller's tenant. Unbounded -- prefer " +
                     "ListAsync for agent/tool use.")]
        public async Task<List<VisualisationRegistryDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistry", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistry.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistry.List", listPermissions);
                var dtos = VisualisationRegistryMapper.ToDto(await repository.GetOrderByIdAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"VisualisationRegistry.List: {dtos.Count} rows user={userName}");
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
                log.Error($"VisualisationRegistry.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Visualisation Registries visible to the caller's tenant, Id-ascending, capped at " +
                     "'take' rows. Call again with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("VisualisationRegistryList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<VisualisationRegistryDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistry", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistry.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistry.List", listPermissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var all = VisualisationRegistryMapper.ToDto(await repository.GetOrderByIdAsync(token)
                        .ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<VisualisationRegistryDto>(page);
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
                log.Error($"VisualisationRegistry.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Visualisation Registry by Id, or null if it doesn't exist or isn't " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("VisualisationRegistryGet", OperationKind.Read, Idempotent = true)]
        public async Task<VisualisationRegistryDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Visualisation Registry.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistry", "Get", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistry.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistry.Get", readPermissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                var dto = VisualisationRegistryMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"VisualisationRegistry.Get: id={id} not found or not visible to tenant " +
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
                log.Error($"VisualisationRegistry.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single active Visualisation Registry by Guid, for embedded recall, provided " +
                     "the caller holds a role grant on it. Returns null if it doesn't exist, isn't active, isn't " +
                     "visible to the caller's tenant, or the caller has no role grant.")]
        [ServiceOperation("VisualisationRegistryGetByGuidActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<VisualisationRegistryDto?> GetByGuidActiveOnlyAsync(
            [Description("Server-assigned globally-unique identifier of the Visualisation Registry.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistry", "GetByGuidActiveOnly", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistry.GetByGuidActiveOnly: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistry.GetByGuidActiveOnly", readPermissions);
                var entity = await repository.GetByGuidActiveOnlyAsync(guid, token).ConfigureAwait(false);
                var dto = VisualisationRegistryMapper.ToDto(entity);
                if (entity == null && log.IsDebugEnabled)
                {
                    log.Debug($"VisualisationRegistry.GetByGuidActiveOnly: guid={guid} not found, not active, " +
                              $"not visible, or no role grant user={userName}");
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
                log.Error($"VisualisationRegistry.GetByGuidActiveOnly: unexpected failure guid={guid} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Visualisation Registries flagged Show In Directory that the caller holds a " +
                     "role grant on, newest first, for the Visualisation Directory page.")]
        [ServiceOperation("VisualisationRegistryListByShowInDirectory", OperationKind.Read, Idempotent = true)]
        public async Task<List<VisualisationRegistryDto>> GetByShowInDirectoryAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistry", "ListByShowInDirectory", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistry.ListByShowInDirectory: entry user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistry.ListByShowInDirectory", showInDirectoryPermissions);
                var dtos = VisualisationRegistryMapper.ToDto(
                    await repository.GetByShowInDirectoryActiveOrderByIdDescAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"VisualisationRegistry.ListByShowInDirectory: {dtos.Count} rows user={userName}");
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
                log.Error($"VisualisationRegistry.ListByShowInDirectory: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Visualisation Registry, the wrapper that specifies the recall canvas for " +
                     "its Datasources.")]
        [ServiceOperation("VisualisationRegistryCreate", OperationKind.Write, Idempotent = false)]
        public async Task<VisualisationRegistryDto> InsertAsync(VisualisationRegistryDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistry", "Insert", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistry.Insert: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("VisualisationRegistry.Insert", writePermissions);
                using var nameGate = await NameGate
                    .EnterAsync($"{tenantRegistryId}|VisualisationRegistry|{model.Name}", token)
                    .ConfigureAwait(false);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"VisualisationRegistry.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.InsertAsync(VisualisationRegistryMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[VisualisationRegistryResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"VisualisationRegistry.Insert: created Id={saved.Id} name={saved.Name} " +
                             $"user={userName}");
                }

                return VisualisationRegistryMapper.ToDto(saved);
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
                log.Error($"VisualisationRegistry.Insert: unexpected failure user={userName} name={model?.Name}",
                    ex);
                throw;
            }
        }

        [Description("Updates an existing Visualisation Registry. The row must exist, be visible to the " +
                     "caller's tenant, and not be locked.")]
        [ServiceOperation("VisualisationRegistryUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<VisualisationRegistryDto> UpdateAsync(VisualisationRegistryDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistry", "Update", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistry.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("VisualisationRegistry.Update", writePermissions);
                using var nameGate = await NameGate
                    .EnterAsync($"{tenantRegistryId}|VisualisationRegistry|{model.Name}", token)
                    .ConfigureAwait(false);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"VisualisationRegistry.Update: validation failed user={userName} " +
                                 $"id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.UpdateAsync(VisualisationRegistryMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[VisualisationRegistryResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"VisualisationRegistry.Update: Id={saved.Id} version={saved.Version} " +
                             $"user={userName}");
                }

                return VisualisationRegistryMapper.ToDto(saved);
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
                    log.Warn($"VisualisationRegistry.Update: id={model?.Id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[VisualisationRegistryResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistry.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Visualisation Registry. The row must exist, be visible to the caller's " +
                     "tenant, and not be locked. Reversible only by direct database action.")]
        [ServiceOperation("VisualisationRegistryDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Visualisation Registry to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistry", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistry.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistry.Delete", writePermissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"VisualisationRegistry.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"VisualisationRegistry.Delete: id={id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[VisualisationRegistryResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistry.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[VisualisationRegistryResources.PermissionDenied], specs);
        }
    }
}