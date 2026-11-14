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
using Jube.Dto.Repository.EntityAnalysisModelSvirfd;
using Jube.Dto.Validation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.EntityApproval;
using Jube.Service.Exceptions.Repository.EntityAnalysisModelSvirfd;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.EntityAnalysisModelSvirfd;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;
using SvirfdRepository = Jube.Data.Repository.EntityAnalysisModelSvirfdRepository;

namespace Jube.Service.Repository.EntityAnalysisModelSvirfd
{
    public sealed class EntityAnalysisModelSvirfdService
    {
        private static readonly int[] listPermissions = [15];
        private static readonly int[] readPermissions = [15];
        private static readonly int[] writePermissions = [15];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly SvirfdRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelSvirfdDtoValidator validator;
        private readonly DbContext dbContext;

        private EntityAnalysisModelSvirfdService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new SvirfdRepository(dbContext, userName);
            validator = new EntityAnalysisModelSvirfdDtoValidator(repository, strings);
            this.dbContext = dbContext;
        }

        public static IReadOnlyList<int> ListPermissions => listPermissions;

        public static IReadOnlyList<int> ReadPermissions => readPermissions;

        public static IReadOnlyList<int> WritePermissions => writePermissions;

        public static Task<EntityAnalysisModelSvirfdService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelSvirfdService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelSvirfdResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelSvirfd.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelSvirfdResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"EntityAnalysisModelSvirfd.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelSvirfdResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelSvirfdService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Svirfd entry visible to the caller's tenant. Unbounded; prefer ListAsync for agent " +
                     "and tool use.")]
        public async Task<List<EntityAnalysisModelSvirfdDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.List", listPermissions);
                var dtos = EntityAnalysisModelSvirfdMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelSvirfd.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Svirfd entities visible to the caller's tenant, Id-ascending, capped at 'take' rows. Call again " +
                     "with 'afterId' set to the last Id returned to page further.")]
        [ServiceOperation("EntityAnalysisModelSvirfdList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelSvirfdDto>> ListAsync(
            [Description("Maximum rows to return, clamped to 200.")]
            int take = 50,
            [Description("Id to page after; omit for the first page.")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.List", listPermissions);
                var clampedTake = Math.Clamp(take, 1, 200);
                var page = EntityAnalysisModelSvirfdMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false))
                    .OrderBy(d => d.Id)
                    .Where(d => afterId == null || d.Id > afterId)
                    .Take(clampedTake)
                    .ToList();
                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelSvirfdDto>(page);
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
                log.Error($"EntityAnalysisModelSvirfd.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Svirfd entities may use, with each field's type, the " +
                     "operators allowed for it and what it means. Use them as rule ids in EntityAnalysisModelSvirfdFilter and EntityAnalysisModelSvirfdCount.")]
        [ServiceOperation("EntityAnalysisModelSvirfdFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "FilterFields", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.FilterFields", listPermissions);
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<EntityAnalysisModelSvirfdDto>();
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
                log.Error($"EntityAnalysisModelSvirfd.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Svirfd entities in the caller's tenant matching query builder JSON (the same format as the rule " +
                     "builder, over the fields from EntityAnalysisModelSvirfdFilterFields), ordered by id and capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last returned Id. Invalid " +
                     "JSON is not an error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelSvirfdFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<EntityAnalysisModelSvirfdDto>> FilterAsync(
            [Description("Query builder JSON selecting the Svirfd entities, using the fields from EntityAnalysisModelSvirfdFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "Filter", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.Filter", listPermissions);
                var rows = EntityAnalysisModelSvirfdMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelSvirfd.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Svirfd entities in the caller's tenant matching query builder JSON (over the fields from " +
                     "EntityAnalysisModelSvirfdFilterFields; empty counts all), optionally broken down by the values of one field. " +
                     "Invalid JSON is not an error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelSvirfdCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description("Query builder JSON selecting the Svirfd entities, using the fields from EntityAnalysisModelSvirfdFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("A field from EntityAnalysisModelSvirfdFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "Count", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.Count", listPermissions);
                var rows = EntityAnalysisModelSvirfdMapper.ToDto(await repository.GetOrderByIdAsync(token).ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelSvirfd.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the Svirfd entities of one model, Id-ascending, visible to the caller's tenant.")]
        [ServiceOperation("EntityAnalysisModelSvirfdListByEntityAnalysisModelId", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelSvirfdDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Server-assigned identifier of the parent model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "ListByEntityAnalysisModelId", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.ListByEntityAnalysisModelId", listPermissions);
                var dtos = EntityAnalysisModelSvirfdMapper.ToDto(
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
                log.Error($"EntityAnalysisModelSvirfd.ListByEntityAnalysisModelId: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Svirfd entry by Id, or null if it does not exist or is not visible to the " +
                     "caller's tenant.")]
        [ServiceOperation("EntityAnalysisModelSvirfdGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelSvirfdDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the Svirfd entry.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.Get", readPermissions);
                var entity = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

                return EntityAnalysisModelSvirfdMapper.ToDto(entity);
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
                log.Error($"EntityAnalysisModelSvirfd.Get: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the request fields of one model a Svirfd entry may name, with whether each has data " +
                     "type IP, which the lookup requires.")]
        [ServiceOperation("EntityAnalysisModelSvirfdFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelSvirfdFieldDto>> FieldsAsync(
            [Description("The model id.")] int entityAnalysisModelId, CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "Fields", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.Fields", writePermissions);
                var rows = await dbContext.EntityAnalysisModelRequestXpath
                    .Where(x => x.EntityAnalysisModelId == entityAnalysisModelId
                                && x.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                                && (x.Deleted == null || x.Deleted == 0) && x.Active == 1)
                    .OrderBy(x => x.Name).ToListAsync(token).ConfigureAwait(false);
                var dtos = rows.Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.DataTypeId == 8)
                    .Select(r => new EntityAnalysisModelSvirfdFieldDto
                    {
                        Key = r.Name,
                        Ip = true,
                        Label = r.Name
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
                log.Error($"EntityAnalysisModelSvirfd.Fields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a Svirfd entry: a payload field of the model, of data type IP, whose value is looked " +
                     "up for IP intelligence at invocation and read as a typed record under Svirfd.{Name} in rules.")]
        [ServiceOperation("EntityAnalysisModelSvirfdCreate", OperationKind.Write, Idempotent = false)]
        public async Task<EntityAnalysisModelSvirfdDto> InsertAsync(EntityAnalysisModelSvirfdDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelSvirfd.Insert", writePermissions);
                using var nameGate = await NameGate.EnterAsync($"{tenantRegistryId}|EntityAnalysisModelSvirfd|{model.Name}", token)
                    .ConfigureAwait(false);
                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelSvirfd.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.InsertAsync(EntityAnalysisModelSvirfdMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[EntityAnalysisModelSvirfdResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelSvirfd.Insert: created Id={saved.Id} name={saved.Name} user={userName}");
                }

                return EntityAnalysisModelSvirfdMapper.ToDto(saved);
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
                log.Error($"EntityAnalysisModelSvirfd.Insert: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates a Svirfd entry without saving it, running every check a create (Id 0) or an update (any " +
                     "other Id) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("EntityAnalysisModelSvirfdValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Svirfd entry to validate.")]
            EntityAnalysisModelSvirfdDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelSvirfd.Validate", writePermissions);
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
                log.Error($"EntityAnalysisModelSvirfd.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Svirfd entry. The row must exist, be visible to the caller's tenant, and " +
                     "not be locked. Changing the keys does not rebuild edges already derived.")]
        [ServiceOperation("EntityAnalysisModelSvirfdUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<EntityAnalysisModelSvirfdDto> UpdateAsync(EntityAnalysisModelSvirfdDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelSvirfd.Update", writePermissions);
                using var nameGate = await NameGate.EnterAsync($"{tenantRegistryId}|EntityAnalysisModelSvirfd|{model.Name}", token)
                    .ConfigureAwait(false);
                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelSvirfd.Update: validation failed user={userName} id={model.Id} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.UpdateAsync(EntityAnalysisModelSvirfdMapper.ToPoco(model), token),
                        r => new DtoValidationException(r), strings[EntityAnalysisModelSvirfdResources.NameAlreadyExists])
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelSvirfd.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return EntityAnalysisModelSvirfdMapper.ToDto(saved);
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
                    log.Warn($"EntityAnalysisModelSvirfd.Update: id={model?.Id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[EntityAnalysisModelSvirfdResources.NotFound]);
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
                log.Error($"EntityAnalysisModelSvirfd.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Svirfd entry. " +
                     "Reversible only by direct database action.")]
        [ServiceOperation("EntityAnalysisModelSvirfdDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the Svirfd entry to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.Delete", writePermissions);
                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelSvirfd.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Warn($"EntityAnalysisModelSvirfd.Delete: id={id} not found, not visible, or locked user={userName}");
                }

                throw new NotFoundException(strings[EntityAnalysisModelSvirfdResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelSvirfd.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Resubmits a rejected Svirfd entry for approval again, unchanged, opening a fresh approval " +
                     "cycle without requiring an edit. Use after a rejection was explained and the maker still wants the " +
                     "same change (including a delete) approved.")]
        [ServiceOperation("EntityAnalysisModelSvirfdResubmit", OperationKind.Write, Idempotent = false)]
        public async Task<EntityAnalysisModelSvirfdDto> ResubmitAsync(
            [Description("Server-assigned identifier of the Svirfd entry to resubmit.")]
            int id,
            [Description("Optional note explaining why this is being resubmitted.")]
            string? note = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "Resubmit", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.Resubmit", writePermissions);

                Data.Poco.EntityAnalysisModelSvirfd saved;

                try
                {
                    saved = await repository.ResubmitAsync(id, note, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException)
                {
                    throw new NotFoundException(strings[EntityAnalysisModelSvirfdResources.NotFound]);
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
                    log.Info($"EntityAnalysisModelSvirfd.Resubmit: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return EntityAnalysisModelSvirfdMapper.ToDto(saved);
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
                log.Error($"EntityAnalysisModelSvirfd.Resubmit: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Retracts a rejected Svirfd entry, restoring it to its last approved version (including " +
                     "undeleting it, if the rejected change was a delete) without opening a new approval cycle. Use when " +
                     "the maker agrees with the rejection and wants to abandon the change.")]
        [ServiceOperation("EntityAnalysisModelSvirfdRetract", OperationKind.Write, Idempotent = false)]
        public async Task<EntityAnalysisModelSvirfdDto> RetractAsync(
            [Description("Server-assigned identifier of the Svirfd entry to retract.")]
            int id,
            [Description("Optional note explaining why this is being retracted.")]
            string? note = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSvirfd", "Retract", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("EntityAnalysisModelSvirfd.Retract", writePermissions);

                Data.Poco.EntityAnalysisModelSvirfd saved;

                try
                {
                    saved = await repository.RetractAsync(id, note, token: token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException)
                {
                    throw new NotFoundException(strings[EntityAnalysisModelSvirfdResources.NotFound]);
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
                    log.Info($"EntityAnalysisModelSvirfd.Retract: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return EntityAnalysisModelSvirfdMapper.ToDto(saved);
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
                log.Error($"EntityAnalysisModelSvirfd.Retract: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[EntityAnalysisModelSvirfdResources.PermissionDenied], specs);
        }
    }
}
