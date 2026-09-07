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
using Jube.Dto.EntityAnalysisModelReprocessingRuleInstance;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelReprocessingRuleInstance;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelReprocessingRuleInstance;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelReprocessingRuleInstance
{
    using ReprocessingRuleInstancePoco = Data.Poco.EntityAnalysisModelReprocessingRuleInstance;

    public sealed class EntityAnalysisModelReprocessingRuleInstanceService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [26];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelReprocessingRuleInstanceRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelReprocessingRuleInstanceDtoValidator validator;

        private EntityAnalysisModelReprocessingRuleInstanceService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new EntityAnalysisModelReprocessingRuleInstanceRepository(dbContext, userName);
            validator = new EntityAnalysisModelReprocessingRuleInstanceDtoValidator(
                new EntityAnalysisModelReprocessingRuleRepository(dbContext, userName), strings);
        }

        public static Task<EntityAnalysisModelReprocessingRuleInstanceService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelReprocessingRuleInstanceService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelReprocessingRuleInstanceResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("EntityAnalysisModelReprocessingRuleInstance.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelReprocessingRuleInstanceResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"EntityAnalysisModelReprocessingRuleInstance.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelReprocessingRuleInstanceResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelReprocessingRuleInstanceService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Reprocessing Rule instance visible to the calling user's tenant. Unbounded -- " +
                     "intended for the administrative page, not for agent tooling (use the bounded list operation " +
                     "instead).")]
        public async Task<List<EntityAnalysisModelReprocessingRuleInstanceDto>> GetAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRuleInstance", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelReprocessingRuleInstance.List: entry user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelReprocessingRuleInstance.List");
                var dtos = EntityAnalysisModelReprocessingRuleInstanceMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelReprocessingRuleInstance.List: {dtos.Count} rows user={userName}");

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
                    log.Debug($"EntityAnalysisModelReprocessingRuleInstance.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelReprocessingRuleInstance.List: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists Reprocessing Rule instances belonging to the given Reprocessing Rule, scoped to the " +
                     "calling user's tenant, newest first.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleInstanceGetByEntityAnalysisModelReprocessingId",
            OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelReprocessingRuleInstanceDto>>
            GetByEntityAnalysisModelReprocessingIdAsync(
                [Description("Numeric identifier of the parent Reprocessing Rule.")]
                int entityAnalysisModelReprocessingId,
                CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRuleInstance",
                "ListByEntityAnalysisModelReprocessingId", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelReprocessingRuleInstance.ListByEntityAnalysisModelReprocessingId: entry entityAnalysisModelReprocessingId={entityAnalysisModelReprocessingId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelReprocessingRuleInstance.ListByEntityAnalysisModelReprocessingId");
                var dtos = EntityAnalysisModelReprocessingRuleInstanceMapper.ToDto(await repository
                    .GetByEntityAnalysisModelsReprocessingRuleIdAsync(entityAnalysisModelReprocessingId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelReprocessingRuleInstance.ListByEntityAnalysisModelReprocessingId: {dtos.Count} rows entityAnalysisModelReprocessingId={entityAnalysisModelReprocessingId} user={userName}");

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
                    log.Debug(
                        $"EntityAnalysisModelReprocessingRuleInstance.ListByEntityAnalysisModelReprocessingId: cancelled entityAnalysisModelReprocessingId={entityAnalysisModelReprocessingId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelReprocessingRuleInstance.ListByEntityAnalysisModelReprocessingId: unexpected failure entityAnalysisModelReprocessingId={entityAnalysisModelReprocessingId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Reprocessing Rule instance by its numeric identifier, scoped to the calling " +
                     "user's tenant. Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleInstanceGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelReprocessingRuleInstanceDto?> GetByIdAsync(
            [Description("Numeric identifier of the Reprocessing Rule instance.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRuleInstance", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelReprocessingRuleInstance.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelReprocessingRuleInstance.Get");
                var reprocessingRuleInstance = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (reprocessingRuleInstance == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"EntityAnalysisModelReprocessingRuleInstance.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(reprocessingRuleInstance.Id);
                return EntityAnalysisModelReprocessingRuleInstanceMapper.ToDto(reprocessingRuleInstance);
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
                    log.Debug($"EntityAnalysisModelReprocessingRuleInstance.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelReprocessingRuleInstance.Get: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists Reprocessing Rule instances for the caller's tenant, ordered by id, capped at " +
                     "'take' rows (max 200). If 'more' is true, call again with 'afterId' set to the last " +
                     "returned Id to continue.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleInstanceList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelReprocessingRuleInstanceDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRuleInstance", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelReprocessingRuleInstance.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelReprocessingRuleInstance.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelReprocessingRuleInstanceDto>(
                    EntityAnalysisModelReprocessingRuleInstanceMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelReprocessingRuleInstance.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelReprocessingRuleInstance.ListPaged: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Registers a new Reprocessing Rule instance under a Reprocessing Rule in the caller's " +
                     "tenant. Not idempotent -- calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleInstanceCreate", OperationKind.Write,
            Idempotent = false)]
        public async Task<ReprocessingRuleInstancePoco> InsertAsync(
            [Description("The Reprocessing Rule instance to create.")]
            EntityAnalysisModelReprocessingRuleInstanceDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRuleInstance", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelReprocessingRuleInstance.Create: entry user={userName} entityAnalysisModelReprocessingRuleId={model?.EntityAnalysisModelReprocessingRuleId}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelReprocessingRuleInstance.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelReprocessingRuleInstance.Create: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertAsync(EntityAnalysisModelReprocessingRuleInstanceMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelReprocessingRuleInstance.Create: created Id={saved.Id} " +
                        $"entityAnalysisModelReprocessingRuleId={saved.EntityAnalysisModelReprocessingRuleId} user={userName}");

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
                    log.Debug($"EntityAnalysisModelReprocessingRuleInstance.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelReprocessingRuleInstance.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Triggers a new reprocessing run for a Reprocessing Rule in the caller's tenant: creates a " +
                     "new instance at StatusId 0 (Not Allocated). Fails if the Reprocessing Rule already has an " +
                     "uncompleted instance (StatusId not 4).")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleInstanceInsertByExistingUpdateUncompleted",
            OperationKind.Write, Idempotent = false)]
        public async Task<ReprocessingRuleInstancePoco> InsertByExistingUpdateUncompletedAsync(
            [Description("The Reprocessing Rule instance to create; only EntityAnalysisModelReprocessingRuleId " +
                         "is meaningful, every other field is server-assigned.")]
            EntityAnalysisModelReprocessingRuleInstanceDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRuleInstance",
                "CreateByExistingUpdateUncompleted", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelReprocessingRuleInstance.CreateByExistingUpdateUncompleted: entry user={userName} entityAnalysisModelReprocessingRuleId={model?.EntityAnalysisModelReprocessingRuleId}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelReprocessingRuleInstance.CreateByExistingUpdateUncompleted");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelReprocessingRuleInstance.CreateByExistingUpdateUncompleted: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                ReprocessingRuleInstancePoco saved;
                try
                {
                    saved = await repository
                        .InsertByExistingUpdateUncompletedAsync(
                            EntityAnalysisModelReprocessingRuleInstanceMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelReprocessingRuleInstance.CreateByExistingUpdateUncompleted: entityAnalysisModelReprocessingRuleId={model.EntityAnalysisModelReprocessingRuleId} already has an uncompleted instance user={userName}");

                    throw new ConflictException(
                        strings[EntityAnalysisModelReprocessingRuleInstanceResources.AlreadyHasActiveInstance], ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelReprocessingRuleInstance.CreateByExistingUpdateUncompleted: created Id={saved.Id} " +
                        $"entityAnalysisModelReprocessingRuleId={saved.EntityAnalysisModelReprocessingRuleId} user={userName}");

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
            catch (ConflictException)
            {
                op.Outcome("conflict");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelReprocessingRuleInstance.CreateByExistingUpdateUncompleted: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelReprocessingRuleInstance.CreateByExistingUpdateUncompleted: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Updates an existing Reprocessing Rule instance in the caller's tenant, identified by its " +
                     "Id. NOT idempotent in the usual sense -- the row is superseded rather than updated in " +
                     "place: a new Id is assigned, the old row is soft-deleted, and CreatedUser/CreatedDate are " +
                     "reset to the caller/now. See the migration report.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleInstanceUpdate", OperationKind.Write,
            Idempotent = false)]
        public async Task<ReprocessingRuleInstancePoco> UpdateAsync(
            [Description("The Reprocessing Rule instance to update. Id selects the row; identity/tenant/audit " +
                         "fields are server-owned and ignored.")]
            EntityAnalysisModelReprocessingRuleInstanceDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRuleInstance", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelReprocessingRuleInstance.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelReprocessingRuleInstance.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelReprocessingRuleInstance.Update: validation failed id={model.Id} " +
                            $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                ReprocessingRuleInstancePoco saved;
                try
                {
                    saved = await repository
                        .UpdateAsync(EntityAnalysisModelReprocessingRuleInstanceMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelReprocessingRuleInstance.Update: id={model.Id} not found, deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Reprocessing Rule instance was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version);
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelReprocessingRuleInstance.Update: superseded Id={model.Id} with new Id={saved.Id} version->{saved.Version} user={userName}");

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
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelReprocessingRuleInstance.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelReprocessingRuleInstance.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes a Reprocessing Rule instance in the caller's tenant by its Id.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleInstanceDelete", OperationKind.Delete,
            Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Reprocessing Rule instance to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRuleInstance", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelReprocessingRuleInstance.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelReprocessingRuleInstance.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelReprocessingRuleInstance.Delete: id={id} not found, already deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Reprocessing Rule instance was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelReprocessingRuleInstance.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelReprocessingRuleInstance.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelReprocessingRuleInstance.Delete: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions)) return;

            if (log.IsWarnEnabled)
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");

            throw new ForbiddenException(
                strings[EntityAnalysisModelReprocessingRuleInstanceResources.PermissionDenied], permissions);
        }
    }
}