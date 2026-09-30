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
using Jube.Dto.EntityAnalysisModelOverride;
using Jube.Dto.Overrides;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelOverride;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelOverride;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelOverride
{
    using OverridePoco = Data.Poco.EntityAnalysisModelOverride;

    public sealed class EntityAnalysisModelOverrideService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [2];
        private static readonly int[] forceOverridePermissions = [62];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelOverrideRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelOverrideDtoValidator validator;

        private EntityAnalysisModelOverrideService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new EntityAnalysisModelOverrideRepository(dbContext, userName);
            validator = new EntityAnalysisModelOverrideDtoValidator(
                new EntityAnalysisModelRepository(dbContext, userName), strings);
        }

        public static Task<EntityAnalysisModelOverrideService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelOverrideService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelOverrideResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelOverride.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelOverrideResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelOverride.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelOverrideResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelOverrideService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Override row visible to the calling user's tenant. Unbounded -- intended for " +
                     "the administrative page, not for agent tooling (use the bounded list operation instead). " +
                     "Note this returns raw rows including soft-deleted and expired ones -- a pre-existing quirk " +
                     "of the underlying query, see the migration report.")]
        public async Task<List<EntityAnalysisModelOverrideDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverride", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelOverride.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.List");
                var dtos = EntityAnalysisModelOverrideMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelOverride.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"EntityAnalysisModelOverride.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelOverride.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Overrides belonging to the given Model, scoped to the calling user's tenant. " +
                     "Excludes soft-deleted rows and rows past their Delete Expiry Date.")]
        [ServiceOperation("EntityAnalysisModelOverrideGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelOverrideDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverride", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelOverride.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelOverrideMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdAsync(entityAnalysisModelId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelOverride.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");
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
                        $"EntityAnalysisModelOverride.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelOverride.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Override by its numeric identifier, scoped to the calling user's tenant. " +
                     "Returns null when the row does not exist, is soft-deleted, is past its Delete Expiry Date, " +
                     "or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelOverrideGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelOverrideDto?> GetByIdAsync(
            [Description("Numeric identifier of the Override.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverride", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelOverride.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.Get");
                var overrideRow = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (overrideRow == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"EntityAnalysisModelOverride.Get: id={id} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(overrideRow.Id);
                return EntityAnalysisModelOverrideMapper.ToDto(overrideRow);
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
                    log.Debug($"EntityAnalysisModelOverride.Get: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelOverride.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Overrides for the caller's tenant, ordered by id, capped at 'take' rows (max 200). " +
                     "If 'more' is true, call again with 'afterId' set to the last returned Id to continue.")]
        [ServiceOperation("EntityAnalysisModelOverrideList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelOverrideDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverride", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelOverride.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelOverrideDto>(
                    EntityAnalysisModelOverrideMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelOverride.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelOverride.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Overrides may use, " +
                     "with each field's type, the operators allowed for it and what it means. " +
                     "Use them as rule ids in EntityAnalysisModelOverrideFilter and " +
                     "EntityAnalysisModelOverrideCount.")]
        [ServiceOperation("EntityAnalysisModelOverrideFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverride", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelOverride.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.FilterFields");
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<EntityAnalysisModelOverrideDto>();
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
                log.Error($"EntityAnalysisModelOverride.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Overrides in the caller's tenant matching query builder " +
                     "JSON (the same format as the rule builder, over the fields from " +
                     "EntityAnalysisModelOverrideFilterFields), ordered by id and capped at " +
                     "'take' rows (max 200). If 'more' is true, call again with 'afterId' set " +
                     "to the last returned Id to continue. Invalid JSON is not an error: Valid " +
                     "is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelOverrideFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<EntityAnalysisModelOverrideDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Overrides, using the fields from EntityAnalysisModelOverrideFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverride", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelOverride.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.Filter");
                var rows = EntityAnalysisModelOverrideMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelOverride.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Overrides in the caller's tenant matching query builder " +
                     "JSON (over the fields from EntityAnalysisModelOverrideFilterFields; " +
                     "empty counts all), optionally broken down by the values of one field. " +
                     "Invalid JSON is not an error: Valid is false and Errors gives each " +
                     "problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelOverrideCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Overrides, using the fields from EntityAnalysisModelOverrideFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from EntityAnalysisModelOverrideFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverride", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelOverride.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.Count");
                var rows = EntityAnalysisModelOverrideMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
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
                log.Error($"EntityAnalysisModelOverride.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Override against a Model in the caller's tenant. Not idempotent -- " +
                     "calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelOverrideCreate", OperationKind.Write, Idempotent = false)]
        public async Task<OverridePoco> InsertAsync(
            [Description("The Override to create.")]
            EntityAnalysisModelOverrideDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("EntityAnalysisModelOverride", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelOverride.Create: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.Create");
                EnsureForceOverridePermitted("EntityAnalysisModelOverride.Create", model.OverrideKind);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelOverride.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(EntityAnalysisModelOverrideMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelOverride.Create: created Id={saved.Id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelOverride.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelOverride.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Validates a Override without saving it, running every check a create (Id 0) or an " +
                     "update (any other Id) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("EntityAnalysisModelOverrideValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Override to validate.")]
            EntityAnalysisModelOverrideDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("EntityAnalysisModelOverride", "Validate", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelOverride.Validate: entry id={model.Id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.Validate");

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
                log.Error($"EntityAnalysisModelOverride.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Toggles the Override for the given Model/OverrideKey/OverrideKeyValue combination " +
                     "in the caller's tenant: if a matching row already exists it is soft-deleted (override " +
                     "turned off), otherwise a new row is created (override turned on). NOT idempotent -- " +
                     "calling this twice in succession with the same arguments turns the override on then " +
                     "off. The DTO's own Active field is ignored; only row existence matters.")]
        [ServiceOperation("EntityAnalysisModelOverrideUpdate", OperationKind.Write, Idempotent = false)]
        public async Task<OverridePoco> UpdateAsync(
            [Description("The Override toggle request. If Id is non-zero it selects the row directly; " +
                         "otherwise EntityAnalysisModelGuid/OverrideKey/OverrideKeyValue select it. " +
                         "Identity/tenant/audit fields are server-owned and ignored.")]
            EntityAnalysisModelOverrideDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("EntityAnalysisModelOverride", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelOverride.Update: entry id={model.Id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.Update");
                EnsureForceOverridePermitted("EntityAnalysisModelOverride.Update", model.OverrideKind);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelOverride.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                OverridePoco saved;
                try
                {
                    saved = await repository.UpdateAsync(EntityAnalysisModelOverrideMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelOverride.Update: id={model.Id} not found or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Override was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelOverride.Update: toggled Id={saved.Id} user={userName}");
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
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelOverride.Update: cancelled id={model.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelOverride.Update: unexpected failure id={model.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Updates the Delete Expiry Date of an existing, currently-active Override matched by " +
                     "Model/OverrideKey/OverrideKeyValue in the caller's tenant. Idempotent -- setting the " +
                     "same date twice has no further effect beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelOverrideUpdateDeleteExpiryDate", OperationKind.Write,
            Idempotent = true)]
        public async Task<EntityAnalysisModelOverrideDto> UpdateDeleteExpiryDateAsync(
            [Description("EntityAnalysisModelGuid/OverrideKey/OverrideKeyValue select the row; " +
                         "DeleteExpiryDate is the new value (must be in the future, or null to clear it). " +
                         "Identity/tenant/audit fields are server-owned and ignored.")]
            EntityAnalysisModelOverrideDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("EntityAnalysisModelOverride", "UpdateDeleteExpiryDate",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelOverride.UpdateDeleteExpiryDate: entry entityAnalysisModelGuid={model.EntityAnalysisModelGuid} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.UpdateDeleteExpiryDate");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelOverride.UpdateDeleteExpiryDate: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                OverridePoco saved;
                try
                {
                    saved = await repository.UpdateDeleteExpiryDateAsync(model.EntityAnalysisModelGuid,
                        model.OverrideKey, model.OverrideKeyValue, model.DeleteExpiryDate?.UtcDateTime,
                        token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelOverride.UpdateDeleteExpiryDate: no active Override matched " +
                            $"entityAnalysisModelGuid={model.EntityAnalysisModelGuid} user={userName}");
                    }

                    throw new NotFoundException("The Override was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelOverride.UpdateDeleteExpiryDate: Id={saved.Id} version->{saved.Version} user={userName}");
                }

                return EntityAnalysisModelOverrideMapper.ToDto(saved);
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
                {
                    log.Debug(
                        $"EntityAnalysisModelOverride.UpdateDeleteExpiryDate: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelOverride.UpdateDeleteExpiryDate: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes a Override in the caller's tenant by its Id. Reversible at the data level, but " +
                     "treat as destructive -- the Override immediately stops applying on transaction " +
                     "invocation.")]
        [ServiceOperation("EntityAnalysisModelOverrideDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Override to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverride", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelOverride.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverride.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelOverride.Delete: id={id} not found, already deleted, expired, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Override was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelOverride.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelOverride.Delete: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelOverride.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsureForceOverridePermitted(string op, EntityAnalysisModelOverrideKind overrideKind)
        {
            if (overrideKind != EntityAnalysisModelOverrideKind.Force)
            {
                return;
            }

            if (permissionValidation.Validate(forceOverridePermissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn(
                    $"{op}: force override permission denied user={userName} specs=[{string.Join(",", forceOverridePermissions)}]");
            }

            throw new ForbiddenException(strings[EntityAnalysisModelOverrideResources.ForceOverridePermissionDenied],
                forceOverridePermissions);
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[EntityAnalysisModelOverrideResources.PermissionDenied],
                permissions);
        }
    }
}