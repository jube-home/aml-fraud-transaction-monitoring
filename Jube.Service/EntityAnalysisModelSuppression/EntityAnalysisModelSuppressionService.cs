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
using Jube.Dto.EntityAnalysisModelSuppression;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelSuppression;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelSuppression;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelSuppression
{
    using SuppressionPoco = Data.Poco.EntityAnalysisModelSuppression;

    public sealed class EntityAnalysisModelSuppressionService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [2];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelSuppressionRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelSuppressionDtoValidator validator;

        private EntityAnalysisModelSuppressionService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new EntityAnalysisModelSuppressionRepository(dbContext, userName);
            validator = new EntityAnalysisModelSuppressionDtoValidator(
                new EntityAnalysisModelRepository(dbContext, userName), strings);
        }

        public static Task<EntityAnalysisModelSuppressionService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelSuppressionService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelSuppressionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("EntityAnalysisModelSuppression.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(strings[EntityAnalysisModelSuppressionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"EntityAnalysisModelSuppression.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(strings[EntityAnalysisModelSuppressionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelSuppressionService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Suppression row visible to the calling user's tenant. Unbounded -- intended for " +
                     "the administrative page, not for agent tooling (use the bounded list operation instead). " +
                     "Note this returns raw rows including soft-deleted and expired ones -- a pre-existing quirk " +
                     "of the underlying query, see the migration report.")]
        public async Task<List<EntityAnalysisModelSuppressionDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSuppression", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelSuppression.List: entry user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelSuppression.List");
                var dtos = EntityAnalysisModelSuppressionMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelSuppression.List: {dtos.Count} rows user={userName}");

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
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelSuppression.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelSuppression.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Suppressions belonging to the given Model, scoped to the calling user's tenant. " +
                     "Excludes soft-deleted rows and rows past their Delete Expiry Date.")]
        [ServiceOperation("EntityAnalysisModelSuppressionGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelSuppressionDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSuppression", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelSuppression.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelSuppression.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelSuppressionMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdAsync(entityAnalysisModelId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelSuppression.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");

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
                        $"EntityAnalysisModelSuppression.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelSuppression.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Suppression by its numeric identifier, scoped to the calling user's tenant. " +
                     "Returns null when the row does not exist, is soft-deleted, is past its Delete Expiry Date, " +
                     "or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelSuppressionGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelSuppressionDto?> GetByIdAsync(
            [Description("Numeric identifier of the Suppression.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSuppression", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelSuppression.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelSuppression.Get");
                var suppression = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (suppression == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"EntityAnalysisModelSuppression.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(suppression.Id);
                return EntityAnalysisModelSuppressionMapper.ToDto(suppression);
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
                    log.Debug($"EntityAnalysisModelSuppression.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelSuppression.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Suppressions for the caller's tenant, ordered by id, capped at 'take' rows (max 200). " +
                     "If 'more' is true, call again with 'afterId' set to the last returned Id to continue.")]
        [ServiceOperation("EntityAnalysisModelSuppressionList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelSuppressionDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSuppression", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelSuppression.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelSuppression.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelSuppressionDto>(
                    EntityAnalysisModelSuppressionMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelSuppression.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelSuppression.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Suppression against a Model in the caller's tenant. Not idempotent -- " +
                     "calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelSuppressionCreate", OperationKind.Write, Idempotent = false)]
        public async Task<SuppressionPoco> InsertAsync(
            [Description("The Suppression to create.")]
            EntityAnalysisModelSuppressionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSuppression", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelSuppression.Create: entry user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelSuppression.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelSuppression.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(EntityAnalysisModelSuppressionMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelSuppression.Create: created Id={saved.Id} user={userName}");

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
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelSuppression.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelSuppression.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Toggles the Suppression for the given Model/SuppressionKey/SuppressionKeyValue combination " +
                     "in the caller's tenant: if a matching row already exists it is soft-deleted (suppression " +
                     "turned off), otherwise a new row is created (suppression turned on). NOT idempotent -- " +
                     "calling this twice in succession with the same arguments turns the suppression on then " +
                     "off. The DTO's own Active field is ignored; only row existence matters.")]
        [ServiceOperation("EntityAnalysisModelSuppressionUpdate", OperationKind.Write, Idempotent = false)]
        public async Task<SuppressionPoco> UpdateAsync(
            [Description("The Suppression toggle request. If Id is non-zero it selects the row directly; " +
                         "otherwise EntityAnalysisModelGuid/SuppressionKey/SuppressionKeyValue select it. " +
                         "Identity/tenant/audit fields are server-owned and ignored.")]
            EntityAnalysisModelSuppressionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSuppression", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelSuppression.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelSuppression.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelSuppression.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                SuppressionPoco saved;
                try
                {
                    saved = await repository.UpdateAsync(EntityAnalysisModelSuppressionMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelSuppression.Update: id={model.Id} not found or not visible to tenant user={userName}");

                    throw new NotFoundException("The Suppression was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelSuppression.Update: toggled Id={saved.Id} user={userName}");

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
                    log.Debug($"EntityAnalysisModelSuppression.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelSuppression.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Updates the Delete Expiry Date of an existing, currently-active Suppression matched by " +
                     "Model/SuppressionKey/SuppressionKeyValue in the caller's tenant. Idempotent -- setting the " +
                     "same date twice has no further effect beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelSuppressionUpdateDeleteExpiryDate", OperationKind.Write,
            Idempotent = true)]
        public async Task<EntityAnalysisModelSuppressionDto> UpdateDeleteExpiryDateAsync(
            [Description("EntityAnalysisModelGuid/SuppressionKey/SuppressionKeyValue select the row; " +
                         "DeleteExpiryDate is the new value (must be in the future, or null to clear it). " +
                         "Identity/tenant/audit fields are server-owned and ignored.")]
            EntityAnalysisModelSuppressionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSuppression", "UpdateDeleteExpiryDate",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelSuppression.UpdateDeleteExpiryDate: entry entityAnalysisModelGuid={model?.EntityAnalysisModelGuid} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelSuppression.UpdateDeleteExpiryDate");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelSuppression.UpdateDeleteExpiryDate: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                SuppressionPoco saved;
                try
                {
                    saved = await repository.UpdateDeleteExpiryDateAsync(model.EntityAnalysisModelGuid,
                        model.SuppressionKey, model.SuppressionKeyValue, model.DeleteExpiryDate?.UtcDateTime,
                        token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelSuppression.UpdateDeleteExpiryDate: no active Suppression matched " +
                            $"entityAnalysisModelGuid={model.EntityAnalysisModelGuid} user={userName}");

                    throw new NotFoundException("The Suppression was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelSuppression.UpdateDeleteExpiryDate: Id={saved.Id} version->{saved.Version} user={userName}");

                return EntityAnalysisModelSuppressionMapper.ToDto(saved)!;
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
                        $"EntityAnalysisModelSuppression.UpdateDeleteExpiryDate: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelSuppression.UpdateDeleteExpiryDate: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes a Suppression in the caller's tenant by its Id. Reversible at the data level, but " +
                     "treat as destructive -- the Suppression immediately stops applying on transaction " +
                     "invocation.")]
        [ServiceOperation("EntityAnalysisModelSuppressionDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Suppression to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSuppression", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelSuppression.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelSuppression.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelSuppression.Delete: id={id} not found, already deleted, expired, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Suppression was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelSuppression.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelSuppression.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelSuppression.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions)) return;

            if (log.IsWarnEnabled)
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");

            throw new ForbiddenException(strings[EntityAnalysisModelSuppressionResources.PermissionDenied],
                permissions);
        }
    }
}