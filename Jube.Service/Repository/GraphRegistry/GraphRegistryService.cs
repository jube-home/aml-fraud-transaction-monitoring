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
using Jube.Data.Concurrency;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Filter;
using Jube.Dto.Repository.GraphRegistry;
using Jube.Dto.Validation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.Repository.GraphRegistry;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.GraphRegistry;
using log4net;
using Microsoft.Extensions.Localization;
using RegistryRepository = Jube.Data.Repository.GraphRegistryRepository;

namespace Jube.Service.Repository.GraphRegistry
{
    public sealed class GraphRegistryService
    {
        private static readonly int[] listPermissions = [49, 50, 1];
        private static readonly int[] readPermissions = [49, 50, 1];
        private static readonly int[] writePermissions = [50];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RegistryRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly GraphRegistryDtoValidator validator;

        private GraphRegistryService(DbContext dbContext, string userName, int tenantRegistryId,
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
            validator = new GraphRegistryDtoValidator(repository, strings);
        }

        public static IReadOnlyList<int> DirectoryPermissions => [];

        public static IReadOnlyList<int> ListPermissions => listPermissions;

        public static IReadOnlyList<int> ReadPermissions => readPermissions;

        public static IReadOnlyList<int> WritePermissions => writePermissions;

        public static Task<GraphRegistryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<GraphRegistryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(GraphRegistryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("GraphRegistry.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[GraphRegistryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GraphRegistry.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[GraphRegistryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new GraphRegistryService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Graph Registry visible to the caller's tenant. Unbounded; prefer ListAsync for agent " +
                     "and tool use.")]
        public async Task<List<GraphRegistryDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistry.List", listPermissions);
                var dtos = GraphRegistryMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"GraphRegistry.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Graph Registrys visible to the caller's tenant, Id-ascending, capped at 'take' rows. Call again " +
                     "with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("GraphRegistryList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<GraphRegistryDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistry.List", listPermissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var page = GraphRegistryMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .Take(clampedTake)
                    .ToList();
                op.Rows(page.Count);

                return new PagedResult<GraphRegistryDto>(page);
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
                log.Error($"GraphRegistry.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Graph Registrys may use, with each field's type, the " +
                     "operators allowed for it and what it means. Use them as rule ids in GraphRegistryFilter and GraphRegistryCount.")]
        [ServiceOperation("GraphRegistryFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "FilterFields", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistry.FilterFields", listPermissions);
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<GraphRegistryDto>();
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
                log.Error($"GraphRegistry.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Graph Registrys in the caller's tenant matching query builder JSON (the same format as the rule " +
                     "builder, over the fields from GraphRegistryFilterFields), ordered by id and capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last returned Id. Invalid " +
                     "JSON is not an error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("GraphRegistryFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<GraphRegistryDto>> FilterAsync(
            [Description("Query builder JSON selecting the Graph Registrys, using the fields from GraphRegistryFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "Filter", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistry.Filter", listPermissions);
                var rows = GraphRegistryMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"GraphRegistry.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Graph Registrys in the caller's tenant matching query builder JSON (over the fields from " +
                     "GraphRegistryFilterFields; empty counts all), optionally broken down by the values of one field. " +
                     "Invalid JSON is not an error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("GraphRegistryCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description("Query builder JSON selecting the Graph Registrys, using the fields from GraphRegistryFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("A field from GraphRegistryFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "Count", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistry.Count", listPermissions);
                var rows = GraphRegistryMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"GraphRegistry.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Graph Registry by Id, or null if it does not exist or is not visible to the " +
                     "caller's tenant.")]
        [ServiceOperation("GraphRegistryGet", OperationKind.Read, Idempotent = true)]
        public async Task<GraphRegistryDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Graph Registry.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistry.Get", readPermissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

                return GraphRegistryMapper.ToDto(entity);
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
                log.Error($"GraphRegistry.Get: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Graph Registry by Guid, or null if it does not exist or is not visible to the " +
                     "caller's tenant.")]
        [ServiceOperation("GraphRegistryGetByGuid", OperationKind.Read, Idempotent = true)]
        public async Task<GraphRegistryDto?> GetByGuidAsync(
            [Description("Globally-unique identifier of the Graph Registry.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "GetByGuid", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistry.GetByGuid", readPermissions);
                var entity = await repository.GetByGuidAsync(guid, token).ConfigureAwait(false);

                return GraphRegistryMapper.ToDto(entity);
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
                log.Error($"GraphRegistry.GetByGuid: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single active Graph Registry by Guid, for the Graph page and the case Network section, " +
                     "provided the caller holds a role grant on it. Returns null if it does not exist, is not active, " +
                     "is not visible to the caller's tenant, or the caller has no role grant.")]
        [ServiceOperation("GraphRegistryGetByGuidActiveOnly", OperationKind.Read, Idempotent = true)]
        public async Task<GraphRegistryDto?> GetByGuidActiveOnlyAsync(
            [Description("Globally-unique identifier of the Graph Registry.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "GetByGuidActiveOnly", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistry.GetByGuidActiveOnly", readPermissions);
                var entity = await repository.GetByGuidActiveOnlyAsync(guid, token).ConfigureAwait(false);

                return GraphRegistryMapper.ToDto(entity);
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
                log.Error($"GraphRegistry.GetByGuidActiveOnly: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists active Graph Registries shown in the Directory that the caller holds a role grant on, for " +
                     "the Graph page.")]
        [ServiceOperation("GraphRegistryListByShowInDirectory", OperationKind.Read, Idempotent = true)]
        public async Task<List<GraphRegistryDto>> GetByShowInDirectoryAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "ListByShowInDirectory", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                var dtos = GraphRegistryMapper.ToDto(
                    await repository.GetByDirectoryActiveOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"GraphRegistry.ListByShowInDirectory: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Graph Registry, the named graph configuration whose edge definitions derive a tenant's " +
                     "network from the archived report keys.")]
        [ServiceOperation("GraphRegistryCreate", OperationKind.Write, Idempotent = false)]
        public async Task<GraphRegistryDto> InsertAsync(GraphRegistryDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("GraphRegistry.Insert", writePermissions);
                using var nameGate = await NameGate.EnterAsync($"{tenantRegistryId}|GraphRegistry|{model.Name}", token)
                    .ConfigureAwait(false);
                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"GraphRegistry.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.InsertAsync(GraphRegistryMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[GraphRegistryResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"GraphRegistry.Insert: created Id={saved.Id} name={saved.Name} user={userName}");
                }

                return GraphRegistryMapper.ToDto(saved);
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
                log.Error($"GraphRegistry.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates a Graph Registry without saving it, running every check a create (Id 0) or an update (any " +
                     "other Id) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("GraphRegistryValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Graph Registry to validate.")]
            GraphRegistryDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("GraphRegistry.Validate", writePermissions);
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
                log.Error($"GraphRegistry.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Graph Registry. The row must exist, be visible to the caller's tenant, and not " +
                     "be locked.")]
        [ServiceOperation("GraphRegistryUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<GraphRegistryDto> UpdateAsync(GraphRegistryDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("GraphRegistry.Update", writePermissions);
                using var nameGate = await NameGate.EnterAsync($"{tenantRegistryId}|GraphRegistry|{model.Name}", token)
                    .ConfigureAwait(false);
                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"GraphRegistry.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.UpdateAsync(GraphRegistryMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[GraphRegistryResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"GraphRegistry.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return GraphRegistryMapper.ToDto(saved);
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
                    log.Warn($"GraphRegistry.Update: id={model?.Id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[GraphRegistryResources.NotFound]);
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
                log.Error($"GraphRegistry.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Graph Registry. The row must exist, be visible to the caller's tenant, and not be " +
                     "locked. Reversible only by direct database action.")]
        [ServiceOperation("GraphRegistryDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Graph Registry to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistry", "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistry.Delete", writePermissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"GraphRegistry.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"GraphRegistry.Delete: id={id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[GraphRegistryResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"GraphRegistry.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[GraphRegistryResources.PermissionDenied], specs);
        }
    }
}
