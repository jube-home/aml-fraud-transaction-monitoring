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
using Jube.Dto.EntityAnalysisModelDictionaryKvp;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelDictionaryKvp;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelDictionaryKvp;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelDictionaryKvp
{
    using DictionaryKvpPoco = Data.Poco.EntityAnalysisModelDictionaryKvp;

    public sealed class EntityAnalysisModelDictionaryKvpService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [4];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelDictionaryKvpRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelDictionaryKvpDtoValidator validator;

        private EntityAnalysisModelDictionaryKvpService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new EntityAnalysisModelDictionaryKvpRepository(dbContext, userName);
            validator = new EntityAnalysisModelDictionaryKvpDtoValidator(
                new EntityAnalysisModelDictionaryRepository(dbContext, userName), strings);
        }

        public static Task<EntityAnalysisModelDictionaryKvpService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelDictionaryKvpService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelDictionaryKvpResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("EntityAnalysisModelDictionaryKvp.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelDictionaryKvpResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"EntityAnalysisModelDictionaryKvp.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelDictionaryKvpResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelDictionaryKvpService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Key Value Pair visible to the calling user's tenant. Unbounded -- intended for " +
                     "the administrative page, not for agent tooling (use the bounded list operation instead).")]
        public async Task<List<EntityAnalysisModelDictionaryKvpDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelDictionaryKvp", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelDictionaryKvp.List: entry user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelDictionaryKvp.List");
                var dtos = EntityAnalysisModelDictionaryKvpMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelDictionaryKvp.List: {dtos.Count} rows user={userName}");

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
                    log.Debug($"EntityAnalysisModelDictionaryKvp.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelDictionaryKvp.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the Key Value Pairs belonging to the given Dictionary, ordered by Id, scoped to the " +
                     "calling user's tenant. Excludes soft-deleted pairs and pairs past their Delete Expiry Date.")]
        [ServiceOperation("EntityAnalysisModelDictionaryKvpGetByEntityAnalysisModelDictionaryId",
            OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelDictionaryKvpDto>> GetByEntityAnalysisModelDictionaryIdAsync(
            [Description("Numeric identifier of the parent Dictionary.")]
            int entityAnalysisModelDictionaryId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelDictionaryKvp",
                "ListByEntityAnalysisModelDictionaryId", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelDictionaryKvp.ListByEntityAnalysisModelDictionaryId: entry entityAnalysisModelDictionaryId={entityAnalysisModelDictionaryId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelDictionaryKvp.ListByEntityAnalysisModelDictionaryId");
                var dtos = EntityAnalysisModelDictionaryKvpMapper.ToDto(await repository
                    .GetByEntityAnalysisModelDictionaryIdOrderByIdAsync(entityAnalysisModelDictionaryId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelDictionaryKvp.ListByEntityAnalysisModelDictionaryId: {dtos.Count} rows entityAnalysisModelDictionaryId={entityAnalysisModelDictionaryId} user={userName}");

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
                        $"EntityAnalysisModelDictionaryKvp.ListByEntityAnalysisModelDictionaryId: cancelled entityAnalysisModelDictionaryId={entityAnalysisModelDictionaryId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelDictionaryKvp.ListByEntityAnalysisModelDictionaryId: unexpected failure entityAnalysisModelDictionaryId={entityAnalysisModelDictionaryId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Key Value Pair, scoped to the calling user's tenant. Note: the underlying " +
                     "query matches on the parent Dictionary's Id, not the pair's own Id -- a pre-existing quirk " +
                     "carried forward from the legacy repository. Returns null when no matching row is visible " +
                     "to the caller.")]
        [ServiceOperation("EntityAnalysisModelDictionaryKvpGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelDictionaryKvpDto?> GetByIdAsync(
            [Description("Numeric identifier matched against the pair's parent Dictionary Id (see remarks).")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelDictionaryKvp", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelDictionaryKvp.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelDictionaryKvp.Get");
                var dictionaryKvp = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (dictionaryKvp == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"EntityAnalysisModelDictionaryKvp.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(dictionaryKvp.Id);
                return EntityAnalysisModelDictionaryKvpMapper.ToDto(dictionaryKvp);
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
                    log.Debug($"EntityAnalysisModelDictionaryKvp.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelDictionaryKvp.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Key Value Pairs for the caller's tenant, ordered by id, capped at 'take' rows (max " +
                     "200). If 'more' is true, call again with 'afterId' set to the last returned Id to continue.")]
        [ServiceOperation("EntityAnalysisModelDictionaryKvpList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelDictionaryKvpDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelDictionaryKvp", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelDictionaryKvp.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelDictionaryKvp.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelDictionaryKvpDto>(
                    EntityAnalysisModelDictionaryKvpMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelDictionaryKvp.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelDictionaryKvp.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Key Value Pair under a Dictionary in the caller's tenant. Not idempotent " +
                     "-- calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelDictionaryKvpCreate", OperationKind.Write, Idempotent = false)]
        public async Task<DictionaryKvpPoco> InsertAsync(
            [Description("The Key Value Pair to create.")]
            EntityAnalysisModelDictionaryKvpDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelDictionaryKvp", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelDictionaryKvp.Create: entry user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelDictionaryKvp.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelDictionaryKvp.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertAsync(EntityAnalysisModelDictionaryKvpMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelDictionaryKvp.Create: created Id={saved.Id} user={userName}");

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
                    log.Debug($"EntityAnalysisModelDictionaryKvp.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelDictionaryKvp.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Key Value Pair in the caller's tenant, identified by its Id. " +
                     "Idempotent -- repeating the same update has no further effect beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelDictionaryKvpUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<DictionaryKvpPoco> UpdateAsync(
            [Description("The Key Value Pair to update. Id selects the row; identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            EntityAnalysisModelDictionaryKvpDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelDictionaryKvp", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelDictionaryKvp.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelDictionaryKvp.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelDictionaryKvp.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                DictionaryKvpPoco saved;
                try
                {
                    saved = await repository
                        .UpdateAsync(EntityAnalysisModelDictionaryKvpMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelDictionaryKvp.Update: id={model.Id} not found, deleted, expired, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Key Value Pair was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelDictionaryKvp.Update: Id={saved.Id} version->{saved.Version} user={userName}");

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
                    log.Debug($"EntityAnalysisModelDictionaryKvp.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelDictionaryKvp.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes a Key Value Pair in the caller's tenant by its Id. Reversible at the data level, " +
                     "but treat as destructive -- the pair immediately stops being available for lookup during " +
                     "model invocation.")]
        [ServiceOperation("EntityAnalysisModelDictionaryKvpDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Key Value Pair to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelDictionaryKvp", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelDictionaryKvp.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelDictionaryKvp.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelDictionaryKvp.Delete: id={id} not found, already deleted, expired, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Key Value Pair was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelDictionaryKvp.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelDictionaryKvp.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelDictionaryKvp.Delete: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions)) return;

            if (log.IsWarnEnabled)
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");

            throw new ForbiddenException(strings[EntityAnalysisModelDictionaryKvpResources.PermissionDenied],
                permissions);
        }
    }
}