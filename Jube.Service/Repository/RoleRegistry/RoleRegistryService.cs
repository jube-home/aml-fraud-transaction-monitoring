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
using Jube.Dto.Repository.RoleRegistry;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.RoleRegistry;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.RoleRegistry;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.RoleRegistryRepository;

namespace Jube.Service.Repository.RoleRegistry
{
    public sealed class RoleRegistryService
    {
        private static readonly int[] permissions = [34];

        private static readonly int[] listPermissions =
            [34, 35, 36, 6, 18, 19, 20, 21, 22, 23, 24, 25, 31, 32, 33];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly RoleRegistryDtoValidator validator;

        private RoleRegistryService(DbContext dbContext, string userName, int tenantRegistryId,
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
            validator = new RoleRegistryDtoValidator(repository, strings);
        }

        public static Task<RoleRegistryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<RoleRegistryService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(RoleRegistryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("RoleRegistry.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[RoleRegistryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"RoleRegistry.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[RoleRegistryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new RoleRegistryService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Role visible to the caller's tenant. Unbounded -- prefer ListAsync for " +
                     "agent/tool use.")]
        public async Task<List<RoleRegistryDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistry", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistry.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("RoleRegistry.List", listPermissions);
                var dtos = RoleRegistryMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"RoleRegistry.List: {dtos.Count} rows user={userName}");
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
                log.Error($"RoleRegistry.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Roles visible to the caller's tenant, Id-ascending, capped at 'take' rows. Call " +
                     "again with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("RoleRegistryList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<RoleRegistryDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistry", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistry.List: entry user={userName} take={take} afterId={afterId}");
            }

            try
            {
                EnsurePermitted("RoleRegistry.List", listPermissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var all = RoleRegistryMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .ToList();
                var page = all.Take(clampedTake).ToList();
                op.Rows(page.Count);

                return new PagedResult<RoleRegistryDto>(page);
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
                log.Error($"RoleRegistry.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Role Registrys may use, " +
                     "with each field's type, the operators allowed for it and what it means. " +
                     "Use them as rule ids in RoleRegistryFilter and RoleRegistryCount.")]
        [ServiceOperation("RoleRegistryFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistry", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistry.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted("RoleRegistry.FilterFields", listPermissions);
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<RoleRegistryDto>();
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
                log.Error($"RoleRegistry.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Role Registrys in the caller's tenant matching query builder " +
                     "JSON (the same format as the rule builder, over the fields from " +
                     "RoleRegistryFilterFields), ordered by id and capped at 'take' rows (max " +
                     "200). If 'more' is true, call again with 'afterId' set to the last " +
                     "returned Id to continue. Invalid JSON is not an error: Valid is false and " +
                     "Errors gives each problem with its JSON path.")]
        [ServiceOperation("RoleRegistryFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<RoleRegistryDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Role Registrys, using the fields from RoleRegistryFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistry", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistry.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("RoleRegistry.Filter", listPermissions);
                var rows = RoleRegistryMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"RoleRegistry.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Role Registrys in the caller's tenant matching query builder " +
                     "JSON (over the fields from RoleRegistryFilterFields; empty counts all), " +
                     "optionally broken down by the values of one field. Invalid JSON is not an " +
                     "error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("RoleRegistryCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Role Registrys, using the fields from RoleRegistryFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from RoleRegistryFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistry", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistry.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted("RoleRegistry.Count", listPermissions);
                var rows = RoleRegistryMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"RoleRegistry.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Role by Id, or null if it doesn't exist or isn't visible to the " +
                     "caller's tenant.")]
        [ServiceOperation("RoleRegistryGet", OperationKind.Read, Idempotent = true)]
        public async Task<RoleRegistryDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Role.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistry", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistry.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("RoleRegistry.Get", permissions);
                var dto = RoleRegistryMapper.ToDto(await repository.GetByIdAsync(id, token).ConfigureAwait(false));
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (dto is null && log.IsDebugEnabled)
                {
                    log.Debug($"RoleRegistry.Get: id={id} not found or not visible to tenant user={userName}");
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
                log.Error($"RoleRegistry.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Role in the caller's tenant.")]
        [ServiceOperation("RoleRegistryCreate", OperationKind.Write, Idempotent = false)]
        public async Task<RoleRegistryDto> InsertAsync(RoleRegistryDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistry", "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistry.Insert: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("RoleRegistry.Insert", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"RoleRegistry.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(RoleRegistryMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"RoleRegistry.Insert: created Id={saved.Id} name={saved.Name} user={userName}");
                }

                return RoleRegistryMapper.ToDto(saved);
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
                log.Error($"RoleRegistry.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates a Role Registry without saving it, running every check a create (Id 0) or an " +
                     "update (any other Id) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("RoleRegistryValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Role Registry to validate.")]
            RoleRegistryDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistry", "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistry.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("RoleRegistry.Validate", permissions);

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
                log.Error($"RoleRegistry.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Role. The row must exist, be visible to the caller's tenant, and not " +
                     "be locked.")]
        [ServiceOperation("RoleRegistryUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<RoleRegistryDto> UpdateAsync(RoleRegistryDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistry", "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistry.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("RoleRegistry.Update", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"RoleRegistry.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.UpdateAsync(RoleRegistryMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"RoleRegistry.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return RoleRegistryMapper.ToDto(saved);
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
                    log.Warn($"RoleRegistry.Update: id={model?.Id} not found, not visible, or locked " +
                             $"user={userName}");
                }

                throw new NotFoundException(strings[RoleRegistryResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"RoleRegistry.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Role. The row must exist, be visible to the caller's tenant, and not be " +
                     "locked. Reversible only by direct database action.")]
        [ServiceOperation("RoleRegistryDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Role to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RoleRegistry", "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"RoleRegistry.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("RoleRegistry.Delete", permissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"RoleRegistry.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"RoleRegistry.Delete: id={id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[RoleRegistryResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"RoleRegistry.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[RoleRegistryResources.PermissionDenied], specs);
        }
    }
}