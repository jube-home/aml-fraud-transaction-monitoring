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
using Jube.Dto.Repository.EntityAnalysisModelGraphRecall;
using Jube.Dto.Validation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.EntityApproval;
using Jube.Service.Exceptions.Repository.EntityAnalysisModelGraphRecall;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.EntityAnalysisModelGraphRecall;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;
using RecallRepository = Jube.Data.Repository.EntityAnalysisModelGraphRecallRepository;

namespace Jube.Service.Repository.EntityAnalysisModelGraphRecall
{
    public sealed class EntityAnalysisModelGraphRecallService
    {
        private static readonly int[] listPermissions = [15];
        private static readonly int[] readPermissions = [15];
        private static readonly int[] writePermissions = [15];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RecallRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelGraphRecallDtoValidator validator;
        private readonly DbContext dbContext;

        private EntityAnalysisModelGraphRecallService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new RecallRepository(dbContext, userName);
            validator = new EntityAnalysisModelGraphRecallDtoValidator(repository, strings);
            this.dbContext = dbContext;
        }

        public static IReadOnlyList<int> ListPermissions => listPermissions;

        public static IReadOnlyList<int> ReadPermissions => readPermissions;

        public static IReadOnlyList<int> WritePermissions => writePermissions;

        public static Task<EntityAnalysisModelGraphRecallService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelGraphRecallService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelGraphRecallResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelGraphRecall.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelGraphRecallResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"EntityAnalysisModelGraphRecall.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelGraphRecallResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelGraphRecallService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every graph recall entity visible to the caller's tenant. Unbounded; prefer ListAsync for agent " +
                     "and tool use.")]
        public async Task<List<EntityAnalysisModelGraphRecallDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.List", listPermissions);
                var dtos = EntityAnalysisModelGraphRecallMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelGraphRecall.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists graph recall entities visible to the caller's tenant, Id-ascending, capped at 'take' rows. Call again " +
                     "with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelGraphRecallDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.List", listPermissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var page = EntityAnalysisModelGraphRecallMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .Take(clampedTake)
                    .ToList();
                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelGraphRecallDto>(page);
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
                log.Error($"EntityAnalysisModelGraphRecall.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over graph recall entities may use, with each field's type, the " +
                     "operators allowed for it and what it means. Use them as rule ids in EntityAnalysisModelGraphRecallFilter and EntityAnalysisModelGraphRecallCount.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "FilterFields", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.FilterFields", listPermissions);
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<EntityAnalysisModelGraphRecallDto>();
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
                log.Error($"EntityAnalysisModelGraphRecall.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the graph recall entities in the caller's tenant matching query builder JSON (the same format as the rule " +
                     "builder, over the fields from EntityAnalysisModelGraphRecallFilterFields), ordered by id and capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last returned Id. Invalid " +
                     "JSON is not an error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<EntityAnalysisModelGraphRecallDto>> FilterAsync(
            [Description("Query builder JSON selecting the graph recall entities, using the fields from EntityAnalysisModelGraphRecallFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "Filter", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.Filter", listPermissions);
                var rows = EntityAnalysisModelGraphRecallMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelGraphRecall.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the graph recall entities in the caller's tenant matching query builder JSON (over the fields from " +
                     "EntityAnalysisModelGraphRecallFilterFields; empty counts all), optionally broken down by the values of one field. " +
                     "Invalid JSON is not an error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description("Query builder JSON selecting the graph recall entities, using the fields from EntityAnalysisModelGraphRecallFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("A field from EntityAnalysisModelGraphRecallFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "Count", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.Count", listPermissions);
                var rows = EntityAnalysisModelGraphRecallMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelGraphRecall.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the graph recall entities of one model, Id-ascending, visible to the caller's tenant.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallListByEntityAnalysisModelId", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelGraphRecallDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Server-assigned identifier of the parent model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "ListByEntityAnalysisModelId", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.ListByEntityAnalysisModelId", listPermissions);
                var dtos = EntityAnalysisModelGraphRecallMapper.ToDto(
                    await repository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelGraphRecall.ListByEntityAnalysisModelId: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single graph recall entity by Id, or null if it does not exist or is not visible to the " +
                     "caller's tenant.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelGraphRecallDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the graph recall entity.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.Get", readPermissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

                return EntityAnalysisModelGraphRecallMapper.ToDto(entity);
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
                log.Error($"EntityAnalysisModelGraphRecall.Get: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the request fields of one model a graph recall entity may name, with whether each is " +
                     "flagged Archive Key, which the graph build requires for the value to exist as a node.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelGraphRecallFieldDto>> FieldsAsync(
            [Description("The model id.")] int entityAnalysisModelId, CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "Fields", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.Fields", writePermissions);
                var rows = await dbContext.EntityAnalysisModelRequestXpath
                    .Where(x => x.EntityAnalysisModelId == entityAnalysisModelId
                                && x.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                                && (x.Deleted == null || x.Deleted == 0) && x.Active == 1)
                    .OrderBy(x => x.Name).ToListAsync(token).ConfigureAwait(false);
                var dtos = rows.Where(r => !string.IsNullOrWhiteSpace(r.Name)).Select(r => new EntityAnalysisModelGraphRecallFieldDto
                {
                    Key = r.Name,
                    ReportTable = r.ReportTable == 1,
                    Label = r.ReportTable == 1 ? r.Name : $"{r.Name} (not flagged Archive Key)"
                }).ToList();
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
                log.Error($"EntityAnalysisModelGraphRecall.Fields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a graph recall entity: a payload field of the model whose value is read from the graph at " +
                     "invocation as a typed object under Graph.{Name} in rules.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallCreate", OperationKind.Write, Idempotent = false)]
        public async Task<EntityAnalysisModelGraphRecallDto> InsertAsync(EntityAnalysisModelGraphRecallDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelGraphRecall.Insert", writePermissions);
                using var nameGate = await NameGate.EnterAsync($"{tenantRegistryId}|EntityAnalysisModelGraphRecall|{model.Name}", token)
                    .ConfigureAwait(false);
                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelGraphRecall.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.InsertAsync(EntityAnalysisModelGraphRecallMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[EntityAnalysisModelGraphRecallResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelGraphRecall.Insert: created Id={saved.Id} name={saved.Name} user={userName}");
                }

                return EntityAnalysisModelGraphRecallMapper.ToDto(saved);
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
                log.Error($"EntityAnalysisModelGraphRecall.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates a graph recall entity without saving it, running every check a create (Id 0) or an update (any " +
                     "other Id) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The graph recall entity to validate.")]
            EntityAnalysisModelGraphRecallDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelGraphRecall.Validate", writePermissions);
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
                log.Error($"EntityAnalysisModelGraphRecall.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing graph recall entity. The row must exist, be visible to the caller's tenant, and " +
                     "not be locked. Changing the keys does not rebuild edges already derived.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<EntityAnalysisModelGraphRecallDto> UpdateAsync(EntityAnalysisModelGraphRecallDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelGraphRecall.Update", writePermissions);
                using var nameGate = await NameGate.EnterAsync($"{tenantRegistryId}|EntityAnalysisModelGraphRecall|{model.Name}", token)
                    .ConfigureAwait(false);
                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelGraphRecall.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.UpdateAsync(EntityAnalysisModelGraphRecallMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[EntityAnalysisModelGraphRecallResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelGraphRecall.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return EntityAnalysisModelGraphRecallMapper.ToDto(saved);
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
                    log.Warn($"EntityAnalysisModelGraphRecall.Update: id={model?.Id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[EntityAnalysisModelGraphRecallResources.NotFound]);
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
                log.Error($"EntityAnalysisModelGraphRecall.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a graph recall entity. Edges already derived stay until the next retention run. " +
                     "Reversible only by direct database action.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the graph recall entity to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.Delete", writePermissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelGraphRecall.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"EntityAnalysisModelGraphRecall.Delete: id={id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[EntityAnalysisModelGraphRecallResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelGraphRecall.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Resubmits a rejected graph recall entity for approval again, unchanged, opening a fresh approval " +
                     "cycle without requiring an edit. Use after a rejection was explained and the maker still wants the " +
                     "same change (including a delete) approved.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallResubmit", OperationKind.Write, Idempotent = false)]
        public async Task<EntityAnalysisModelGraphRecallDto> ResubmitAsync(
            [Description("Server-assigned identifier of the graph recall entity to resubmit.")]
            int id,
            [Description("Optional note explaining why this is being resubmitted.")]
            string? note = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "Resubmit", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.Resubmit", writePermissions);

                Data.Poco.EntityAnalysisModelGraphRecall saved;

                try
                {
                    saved = await repository.ResubmitAsync(id, note, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException)
                {
                    throw new NotFoundException(strings[EntityAnalysisModelGraphRecallResources.NotFound]);
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
                    log.Info($"EntityAnalysisModelGraphRecall.Resubmit: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return EntityAnalysisModelGraphRecallMapper.ToDto(saved);
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
                log.Error($"EntityAnalysisModelGraphRecall.Resubmit: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Retracts a rejected graph recall entity, restoring it to its last approved version (including " +
                     "undeleting it, if the rejected change was a delete) without opening a new approval cycle. Use when " +
                     "the maker agrees with the rejection and wants to abandon the change.")]
        [ServiceOperation("EntityAnalysisModelGraphRecallRetract", OperationKind.Write, Idempotent = false)]
        public async Task<EntityAnalysisModelGraphRecallDto> RetractAsync(
            [Description("Server-assigned identifier of the graph recall entity to retract.")]
            int id,
            [Description("Optional note explaining why this is being retracted.")]
            string? note = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGraphRecall", "Retract", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelGraphRecall.Retract", writePermissions);

                Data.Poco.EntityAnalysisModelGraphRecall saved;

                try
                {
                    saved = await repository.RetractAsync(id, note, token: token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException)
                {
                    throw new NotFoundException(strings[EntityAnalysisModelGraphRecallResources.NotFound]);
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
                    log.Info($"EntityAnalysisModelGraphRecall.Retract: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return EntityAnalysisModelGraphRecallMapper.ToDto(saved);
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
                log.Error($"EntityAnalysisModelGraphRecall.Retract: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private static ApprovalRefusedException Refused(string message)
        {
            return new ApprovalRefusedException(message, [message]);
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

            throw new ForbiddenException(strings[EntityAnalysisModelGraphRecallResources.PermissionDenied], specs);
        }
    }
}
