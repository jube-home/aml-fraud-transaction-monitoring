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

using Jube.Data.Concurrency;
using System.ComponentModel;
using Jube.Case.Outbound;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelResponseElevationRange;
using Jube.Dto.Filter;
using Jube.Dto.Validation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.EntityAnalysisModelResponseElevationRange;
using Jube.Service.Exceptions.EntityApproval;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelResponseElevationRange;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelResponseElevationRange
{
    using RangePoco = Data.Poco.EntityAnalysisModelResponseElevationRange;

    public sealed class EntityAnalysisModelResponseElevationRangeService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [17];
        private static readonly int[] listByModelPermissions = [17, 1];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelResponseElevationRangeRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelResponseElevationRangeDtoValidator validator;

        private EntityAnalysisModelResponseElevationRangeService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, OutboundHostPolicy outboundPolicy)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new EntityAnalysisModelResponseElevationRangeRepository(dbContext, userName);

            validator = new EntityAnalysisModelResponseElevationRangeDtoValidator(repository, strings,
                new EntityAnalysisModelRepository(dbContext, userName), outboundPolicy);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;
        public static IReadOnlyList<int> ListByModelPermissions => listByModelPermissions;

        public static Task<EntityAnalysisModelResponseElevationRangeService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), dynamicEnvironment, token);
        }

        internal static async Task<EntityAnalysisModelResponseElevationRangeService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelResponseElevationRangeResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelResponseElevationRange.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelResponseElevationRangeResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"EntityAnalysisModelResponseElevationRange.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelResponseElevationRangeResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelResponseElevationRangeService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, OutboundHostPolicy.From(dynamicEnvironment));
        }

        [Description("Lists every Response Elevation Range visible to the calling user's tenant. Unbounded -- intended for the " +
                     "administrative page, not for agent tooling (use the bounded list operation instead).")]
        public async Task<List<EntityAnalysisModelResponseElevationRangeDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.List");

                var dtos = EntityAnalysisModelResponseElevationRangeMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelResponseElevationRange.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"EntityAnalysisModelResponseElevationRange.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelResponseElevationRange.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the Response Elevation Ranges belonging to the given Model that are Active, ordered by Name, scoped to " +
                     "the calling user's tenant. Used to populate the response elevation picker of an Activation " +
                     "Rule.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeGetByEntityAnalysisModelId", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelResponseElevationRangeDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelResponseElevationRange.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.ListByEntityAnalysisModelId", listByModelPermissions);

                var dtos = EntityAnalysisModelResponseElevationRangeMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByNameActiveOnlyAsync(entityAnalysisModelId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelResponseElevationRange.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");
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
                    log.Debug(
                        $"EntityAnalysisModelResponseElevationRange.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);

                log.Error(
                    $"EntityAnalysisModelResponseElevationRange.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);

                throw;
            }
        }

        [Description("Returns one Response Elevation Range by its numeric identifier, scoped to the calling user's tenant. Returns " +
                     "null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelResponseElevationRangeDto?> GetByIdAsync(
            [Description("Numeric identifier of the Response Elevation Range.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.Get");
                var range = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

                if (range == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"EntityAnalysisModelResponseElevationRange.Get: id={id} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(range.Id);

                return EntityAnalysisModelResponseElevationRangeMapper.ToDto(range);
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
                    log.Debug($"EntityAnalysisModelResponseElevationRange.Get: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelResponseElevationRange.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Returns one Response Elevation Range by its Guid, scoped to the calling user's tenant. Returns " +
                     "null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeGetByGuid", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelResponseElevationRangeDto?> GetByGuidAsync(
            [Description("Globally-addressable Guid of the Response Elevation Range.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "GetByGuid", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.GetByGuid: entry guid={guid} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.GetByGuid");
                var range = await repository.GetByGuidAsync(guid, token).ConfigureAwait(false);

                if (range == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"EntityAnalysisModelResponseElevationRange.GetByGuid: guid={guid} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(range.Id);

                return EntityAnalysisModelResponseElevationRangeMapper.ToDto(range);
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
                    log.Debug($"EntityAnalysisModelResponseElevationRange.GetByGuid: cancelled guid={guid} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelResponseElevationRange.GetByGuid: unexpected failure guid={guid} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Response Elevation Ranges for the caller's tenant, ordered by id, capped at 'take' rows (max 200). If " +
                     "'more' is true, call again with 'afterId' set to the last returned Id to continue.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelResponseElevationRangeDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            var clampedTake = Math.Clamp(take, 1, MaxListTake);

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelResponseElevationRange.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelResponseElevationRangeDto>(EntityAnalysisModelResponseElevationRangeMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelResponseElevationRange.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelResponseElevationRange.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Response Elevation Ranges may use, with each " +
                     "field's type, the operators allowed for it and what it means. Use them as " +
                     "rule ids in EntityAnalysisModelResponseElevationRangeFilter and EntityAnalysisModelResponseElevationRangeCount.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.FilterFields");
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<EntityAnalysisModelResponseElevationRangeDto>();
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
                log.Error($"EntityAnalysisModelResponseElevationRange.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Response Elevation Ranges in the caller's tenant matching query builder JSON (the " +
                     "same format as the rule builder, over the fields from " +
                     "EntityAnalysisModelResponseElevationRangeFilterFields), ordered by id and capped at 'take' " +
                     "rows (max 200). If 'more' is true, call again with 'afterId' set to the " +
                     "last returned Id to continue. Invalid JSON is not an error: Valid is " +
                     "false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<EntityAnalysisModelResponseElevationRangeDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Response Elevation Ranges, using the fields from EntityAnalysisModelResponseElevationRangeFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.Filter");
                var rows = EntityAnalysisModelResponseElevationRangeMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelResponseElevationRange.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Response Elevation Ranges in the caller's tenant matching query builder JSON (over " +
                     "the fields from EntityAnalysisModelResponseElevationRangeFilterFields; empty counts all), " +
                     "optionally broken down by the values of one field. Invalid JSON is not an " +
                     "error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Response Elevation Ranges, using the fields from EntityAnalysisModelResponseElevationRangeFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from EntityAnalysisModelResponseElevationRangeFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.Count");
                var rows = EntityAnalysisModelResponseElevationRangeMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelResponseElevationRange.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Response Elevation Range under a Model in the caller's tenant. Not idempotent -- calling twice " +
                     "creates two rows.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeCreate", OperationKind.Write, Idempotent = false)]
        public async Task<RangePoco> InsertAsync(
            [Description("The Response Elevation Range to create.")] EntityAnalysisModelResponseElevationRangeDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.Create: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelResponseElevationRange.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(EntityAnalysisModelResponseElevationRangeMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelResponseElevationRange.Create: created Id={saved.Id} name={saved.Name} user={userName}");
                }

                return saved;
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

                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelResponseElevationRange.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);

                log.Error($"EntityAnalysisModelResponseElevationRange.Create: unexpected failure user={userName} name={model?.Name}",
                    ex);

                throw;
            }
        }

        [Description("Validates a Response Elevation Range without saving it, running every check a create (Id 0) or an update (any " +
                     "other Id) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Response Elevation Range to validate.")] EntityAnalysisModelResponseElevationRangeDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "Validate", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.Validate");

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
                log.Error($"EntityAnalysisModelResponseElevationRange.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Response Elevation Range in the caller's tenant, identified by its Id. Idempotent -- " +
                     "repeating the same update has no further effect beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<RangePoco> UpdateAsync(
            [Description("The Response Elevation Range to update. Id selects the row; identity/tenant/audit fields are server-owned " +
                         "and ignored.")]
            EntityAnalysisModelResponseElevationRangeDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelResponseElevationRange.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                RangePoco saved;

                try
                {
                    saved = await repository.UpdateAsync(EntityAnalysisModelResponseElevationRangeMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelResponseElevationRange.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Response Elevation Range was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelResponseElevationRange.Update: Id={saved.Id} version->{saved.Version} user={userName}");
                }

                return saved;
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
                op.Outcome("notfound");
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

                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelResponseElevationRange.Update: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelResponseElevationRange.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Deletes a Response Elevation Range in the caller's tenant by its Id. Reversible at the data level, but treat " +
                     "as destructive -- Activation Rules whose level fell only in this range immediately stop sending out " +
                     "their response elevation.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Response Elevation Range to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelResponseElevationRange.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Response Elevation Range was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelResponseElevationRange.Delete: soft-deleted Id={id} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");

                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelResponseElevationRange.Delete: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelResponseElevationRange.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Resubmits a rejected Response Elevation Range for approval again, unchanged, opening a fresh approval cycle " +
                     "without requiring an edit. Use after a rejection was explained and the maker still wants " +
                     "the same change (including a delete) approved.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeResubmit", OperationKind.Write, Idempotent = false)]
        public async Task<RangePoco> ResubmitAsync(
            [Description("Numeric identifier of the Response Elevation Range to resubmit.")]
            int id,
            [Description("Optional note explaining why this is being resubmitted.")]
            string? note = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "Resubmit", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.Resubmit: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.Resubmit");

                RangePoco saved;
                try
                {
                    saved = await repository.ResubmitAsync(id, note, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new NotFoundException("The Response Elevation Range was not found.", ex);
                }
                catch (InvalidOperationException ex)
                {
                    throw Refused(ex.Message);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelResponseElevationRange.Resubmit: Id={saved.Id} version->{saved.Version} user={userName}");
                }

                return saved;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (ApprovalRefusedException)
            {
                op.Outcome("refused");
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
                log.Error($"EntityAnalysisModelResponseElevationRange.Resubmit: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Retracts a rejected Response Elevation Range, restoring it to its last approved version (including " +
                     "undeleting it, if the rejected change was a delete) without opening a new approval cycle. " +
                     "Use when the maker agrees with the rejection and wants to abandon the change.")]
        [ServiceOperation("EntityAnalysisModelResponseElevationRangeRetract", OperationKind.Write, Idempotent = false)]
        public async Task<RangePoco> RetractAsync(
            [Description("Numeric identifier of the Response Elevation Range to retract.")]
            int id,
            [Description("Optional note explaining why this is being retracted.")]
            string? note = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelResponseElevationRange", "Retract", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelResponseElevationRange.Retract: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelResponseElevationRange.Retract");

                RangePoco saved;
                try
                {
                    saved = await repository.RetractAsync(id, note, token: token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new NotFoundException("The Response Elevation Range was not found.", ex);
                }
                catch (InvalidOperationException ex)
                {
                    throw Refused(ex.Message);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelResponseElevationRange.Retract: Id={saved.Id} version->{saved.Version} user={userName}");
                }

                return saved;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (ApprovalRefusedException)
            {
                op.Outcome("refused");
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
                log.Error($"EntityAnalysisModelResponseElevationRange.Retract: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private static ApprovalRefusedException Refused(string message)
        {
            return new ApprovalRefusedException(message, [message]);
        }

        private void EnsurePermitted(string op) => EnsurePermitted(op, permissions);

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

            throw new ForbiddenException(strings[EntityAnalysisModelResponseElevationRangeResources.PermissionDenied], specs);
        }
    }
}