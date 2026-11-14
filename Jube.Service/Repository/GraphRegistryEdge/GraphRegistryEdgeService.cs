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
using Jube.Dto.Repository.GraphRegistryEdge;
using Jube.Dto.Validation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.Repository.GraphRegistryEdge;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.GraphRegistryEdge;
using log4net;
using Microsoft.Extensions.Localization;
using Jube.Data.Query.GetGraphReportKeysQuery;
using EdgeRepository = Jube.Data.Repository.GraphRegistryEdgeRepository;

namespace Jube.Service.Repository.GraphRegistryEdge
{
    public sealed class GraphRegistryEdgeService
    {
        private static readonly int[] listPermissions = [49, 50, 1];
        private static readonly int[] readPermissions = [49, 50, 1];
        private static readonly int[] writePermissions = [50];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EdgeRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly GraphRegistryEdgeDtoValidator validator;
        private readonly GetGraphReportKeysQuery reportKeysQuery;

        private GraphRegistryEdgeService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new EdgeRepository(dbContext, userName);
            validator = new GraphRegistryEdgeDtoValidator(repository, strings);
            reportKeysQuery = new GetGraphReportKeysQuery(dbContext);
        }

        public static IReadOnlyList<int> ListPermissions => listPermissions;

        public static IReadOnlyList<int> ReadPermissions => readPermissions;

        public static IReadOnlyList<int> WritePermissions => writePermissions;

        public static Task<GraphRegistryEdgeService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<GraphRegistryEdgeService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(GraphRegistryEdgeResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("GraphRegistryEdge.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[GraphRegistryEdgeResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GraphRegistryEdge.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[GraphRegistryEdgeResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new GraphRegistryEdgeService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Graph Registry Edge visible to the caller's tenant. Unbounded; prefer ListAsync for agent " +
                     "and tool use.")]
        public async Task<List<GraphRegistryEdgeDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryEdge.List", listPermissions);
                var dtos = GraphRegistryEdgeMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"GraphRegistryEdge.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Graph Registry Edges visible to the caller's tenant, Id-ascending, capped at 'take' rows. Call again " +
                     "with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("GraphRegistryEdgeList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<GraphRegistryEdgeDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryEdge.List", listPermissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var page = GraphRegistryEdgeMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .Take(clampedTake)
                    .ToList();
                op.Rows(page.Count);

                return new PagedResult<GraphRegistryEdgeDto>(page);
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
                log.Error($"GraphRegistryEdge.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Graph Registry Edges may use, with each field's type, the " +
                     "operators allowed for it and what it means. Use them as rule ids in GraphRegistryEdgeFilter and GraphRegistryEdgeCount.")]
        [ServiceOperation("GraphRegistryEdgeFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "FilterFields", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryEdge.FilterFields", listPermissions);
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<GraphRegistryEdgeDto>();
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
                log.Error($"GraphRegistryEdge.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Graph Registry Edges in the caller's tenant matching query builder JSON (the same format as the rule " +
                     "builder, over the fields from GraphRegistryEdgeFilterFields), ordered by id and capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last returned Id. Invalid " +
                     "JSON is not an error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("GraphRegistryEdgeFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<GraphRegistryEdgeDto>> FilterAsync(
            [Description("Query builder JSON selecting the Graph Registry Edges, using the fields from GraphRegistryEdgeFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "Filter", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryEdge.Filter", listPermissions);
                var rows = GraphRegistryEdgeMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"GraphRegistryEdge.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Graph Registry Edges in the caller's tenant matching query builder JSON (over the fields from " +
                     "GraphRegistryEdgeFilterFields; empty counts all), optionally broken down by the values of one field. " +
                     "Invalid JSON is not an error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("GraphRegistryEdgeCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description("Query builder JSON selecting the Graph Registry Edges, using the fields from GraphRegistryEdgeFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("A field from GraphRegistryEdgeFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "Count", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryEdge.Count", listPermissions);
                var rows = GraphRegistryEdgeMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"GraphRegistryEdge.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the edge definitions of one Graph Registry, Id-ascending, visible to the caller's tenant.")]
        [ServiceOperation("GraphRegistryEdgeListByGraphRegistryId", OperationKind.Read, Idempotent = true)]
        public async Task<List<GraphRegistryEdgeDto>> GetByGraphRegistryIdAsync(
            [Description("Server-assigned identifier of the parent Graph Registry.")]
            int graphRegistryId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "ListByGraphRegistryId", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryEdge.ListByGraphRegistryId", listPermissions);
                var dtos = GraphRegistryEdgeMapper.ToDto(
                    await repository.GetByGraphRegistryIdOrderByIdAsync(graphRegistryId, token).ConfigureAwait(false));
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
                log.Error($"GraphRegistryEdge.ListByGraphRegistryId: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Graph Registry Edge by Id, or null if it does not exist or is not visible to the " +
                     "caller's tenant.")]
        [ServiceOperation("GraphRegistryEdgeGet", OperationKind.Read, Idempotent = true)]
        public async Task<GraphRegistryEdgeDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Graph Registry Edge.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryEdge.Get", readPermissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

                return GraphRegistryEdgeMapper.ToDto(entity);
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
                log.Error($"GraphRegistryEdge.Get: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the report keys an edge definition may name: the request fields flagged Archive Key on the " +
                     "tenant's active models (the names the engine writes to Archive Key), with the models each is " +
                     "flagged on and the models where it exists unflagged, so a key that would miss some models is " +
                     "visible before it is chosen.")]
        [ServiceOperation("GraphRegistryEdgeKeys", OperationKind.Read, Idempotent = true)]
        public async Task<List<GraphRegistryEdgeKeyDto>> KeysAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "Keys", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryEdge.Keys", writePermissions);
                var rows = await reportKeysQuery.ExecuteAsync(tenantRegistryId, token).ConfigureAwait(false);
                var dtos = rows.GroupBy(r => r.Key, StringComparer.Ordinal)
                    .OrderBy(g => g.Key, StringComparer.Ordinal)
                    .Select(g =>
                    {
                        var flagged = g.Where(r => r.ReportTable).Select(r => r.ModelName).Distinct()
                            .OrderBy(n => n, StringComparer.Ordinal).ToList();
                        var unflagged = g.Where(r => !r.ReportTable).Select(r => r.ModelName).Distinct()
                            .Where(n => !flagged.Contains(n)).OrderBy(n => n, StringComparer.Ordinal).ToList();

                        return new GraphRegistryEdgeKeyDto
                        {
                            Key = g.Key,
                            Models = flagged,
                            UnflaggedModels = unflagged,
                            Label = flagged.Count == 0
                                ? $"{g.Key} (not flagged Archive Key)"
                                : $"{g.Key} ({string.Join(", ", flagged)})"
                        };
                    })
                    .Where(d => d.Models.Count > 0 || d.UnflaggedModels.Count > 0)
                    .ToList();
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
                log.Error($"GraphRegistryEdge.Keys: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Graph Registry Edge: one kind of edge derived from two report keys that occur on the " +
                     "same archived transaction.")]
        [ServiceOperation("GraphRegistryEdgeCreate", OperationKind.Write, Idempotent = false)]
        public async Task<GraphRegistryEdgeDto> InsertAsync(GraphRegistryEdgeDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("GraphRegistryEdge.Insert", writePermissions);
                using var nameGate = await NameGate.EnterAsync($"{tenantRegistryId}|GraphRegistryEdge|{model.Name}", token)
                    .ConfigureAwait(false);
                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"GraphRegistryEdge.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.InsertAsync(GraphRegistryEdgeMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[GraphRegistryEdgeResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"GraphRegistryEdge.Insert: created Id={saved.Id} name={saved.Name} user={userName}");
                }

                return GraphRegistryEdgeMapper.ToDto(saved);
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
                log.Error($"GraphRegistryEdge.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates a Graph Registry Edge without saving it, running every check a create (Id 0) or an update (any " +
                     "other Id) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("GraphRegistryEdgeValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Graph Registry Edge to validate.")]
            GraphRegistryEdgeDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("GraphRegistryEdge.Validate", writePermissions);
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
                log.Error($"GraphRegistryEdge.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Graph Registry Edge. The row must exist, be visible to the caller's tenant, and " +
                     "not be locked. Changing the keys does not rebuild edges already derived.")]
        [ServiceOperation("GraphRegistryEdgeUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<GraphRegistryEdgeDto> UpdateAsync(GraphRegistryEdgeDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("GraphRegistryEdge.Update", writePermissions);
                using var nameGate = await NameGate.EnterAsync($"{tenantRegistryId}|GraphRegistryEdge|{model.Name}", token)
                    .ConfigureAwait(false);
                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"GraphRegistryEdge.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.UpdateAsync(GraphRegistryEdgeMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[GraphRegistryEdgeResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"GraphRegistryEdge.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return GraphRegistryEdgeMapper.ToDto(saved);
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
                    log.Warn($"GraphRegistryEdge.Update: id={model?.Id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[GraphRegistryEdgeResources.NotFound]);
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
                log.Error($"GraphRegistryEdge.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Graph Registry Edge. Edges already derived stay until the next retention run. " +
                     "Reversible only by direct database action.")]
        [ServiceOperation("GraphRegistryEdgeDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Graph Registry Edge to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("GraphRegistryEdge", "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("GraphRegistryEdge.Delete", writePermissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"GraphRegistryEdge.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"GraphRegistryEdge.Delete: id={id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[GraphRegistryEdgeResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"GraphRegistryEdge.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[GraphRegistryEdgeResources.PermissionDenied], specs);
        }
    }
}
