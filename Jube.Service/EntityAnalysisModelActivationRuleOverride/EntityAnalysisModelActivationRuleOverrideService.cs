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
using Jube.Dto.EntityAnalysisModelActivationRuleOverride;
using Jube.Dto.Overrides;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelActivationRuleOverride;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelActivationRuleOverride;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelActivationRuleOverride
{
    using ActivationRuleOverridePoco = Data.Poco.EntityAnalysisModelActivationRuleOverride;

    public sealed class EntityAnalysisModelActivationRuleOverrideService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [2];
        private static readonly int[] forceOverridePermissions = [62];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelActivationRuleOverrideRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelActivationRuleOverrideDtoValidator validator;

        private EntityAnalysisModelActivationRuleOverrideService(DbContext dbContext, string userName,
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
            repository = new EntityAnalysisModelActivationRuleOverrideRepository(dbContext, userName);
            validator = new EntityAnalysisModelActivationRuleOverrideDtoValidator(
                new EntityAnalysisModelRepository(dbContext, userName),
                new EntityAnalysisModelActivationRuleRepository(dbContext, userName), strings);
        }

        public static Task<EntityAnalysisModelActivationRuleOverrideService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelActivationRuleOverrideService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelActivationRuleOverrideResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelActivationRuleOverride.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelActivationRuleOverrideResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelActivationRuleOverride.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelActivationRuleOverrideResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelActivationRuleOverrideService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Activation-Rule-scoped Override row visible to the calling user's tenant. " +
                     "Unbounded -- intended for the administrative page, not for agent tooling (use the bounded " +
                     "list operation instead). Note this returns raw rows including soft-deleted and expired " +
                     "ones -- a pre-existing quirk of the underlying query, see the migration report.")]
        public async Task<List<EntityAnalysisModelActivationRuleOverrideDto>> GetAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelActivationRuleOverride.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.List");
                var dtos = EntityAnalysisModelActivationRuleOverrideMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelActivationRuleOverride.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"EntityAnalysisModelActivationRuleOverride.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelActivationRuleOverride.List: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists Activation-Rule-scoped Overrides belonging to the given Model, scoped to the " +
                     "calling user's tenant. Excludes soft-deleted rows and rows past their Delete Expiry Date.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideGetByEntityAnalysisModelGuid",
            OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelActivationRuleOverrideDto>> GetByEntityAnalysisModelGuidAsync(
            [Description("Guid identifier of the parent Model.")]
            Guid entityAnalysisModelGuid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride",
                "ListByEntityAnalysisModelGuid", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelActivationRuleOverride.ListByEntityAnalysisModelGuid: entry entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.ListByEntityAnalysisModelGuid");
                var dtos = EntityAnalysisModelActivationRuleOverrideMapper.ToDto(await repository
                    .GetByEntityAnalysisModelGuidOrderByIdAsync(entityAnalysisModelGuid, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelActivationRuleOverride.ListByEntityAnalysisModelGuid: {dtos.Count} rows entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}");
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
                        $"EntityAnalysisModelActivationRuleOverride.ListByEntityAnalysisModelGuid: cancelled entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleOverride.ListByEntityAnalysisModelGuid: unexpected failure entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Activation-Rule-scoped Override by its numeric identifier, scoped to the " +
                     "calling user's tenant. Returns null when the row does not exist, is soft-deleted, is past " +
                     "its Delete Expiry Date, or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelActivationRuleOverrideDto?> GetByIdAsync(
            [Description("Numeric identifier of the Override.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelActivationRuleOverride.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.Get");
                var overrideRow = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (overrideRow == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"EntityAnalysisModelActivationRuleOverride.Get: id={id} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(overrideRow.Id);
                return EntityAnalysisModelActivationRuleOverrideMapper.ToDto(overrideRow);
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
                    log.Debug($"EntityAnalysisModelActivationRuleOverride.Get: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleOverride.Get: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists Activation-Rule-scoped Overrides for the caller's tenant, ordered by id, capped " +
                     "at 'take' rows (max 200). If 'more' is true, call again with 'afterId' set to the last " +
                     "returned Id to continue.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelActivationRuleOverrideDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride", "ListPaged",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelActivationRuleOverride.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelActivationRuleOverrideDto>(
                    EntityAnalysisModelActivationRuleOverrideMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelActivationRuleOverride.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleOverride.ListPaged: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Activation Rule " +
                     "Overrides may use, with each field's type, the operators allowed for " +
                     "it and what it means. Use them as rule ids in " +
                     "EntityAnalysisModelActivationRuleOverrideFilter and " +
                     "EntityAnalysisModelActivationRuleOverrideCount.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideFilterFields", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride", "FilterFields",
                userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelActivationRuleOverride.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.FilterFields");
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<EntityAnalysisModelActivationRuleOverrideDto>();
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
                log.Error(
                    $"EntityAnalysisModelActivationRuleOverride.FilterFields: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns the Activation Rule Overrides in the caller's tenant matching " +
                     "query builder JSON (the same format as the rule builder, over the fields " +
                     "from EntityAnalysisModelActivationRuleOverrideFilterFields), ordered " +
                     "by id and capped at 'take' rows (max 200). If 'more' is true, call again " +
                     "with 'afterId' set to the last returned Id to continue. Invalid JSON is " +
                     "not an error: Valid is false and Errors gives each problem with its JSON " +
                     "path.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<EntityAnalysisModelActivationRuleOverrideDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Activation Rule Overrides, using the fields from EntityAnalysisModelActivationRuleOverrideFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelActivationRuleOverride.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.Filter");
                var rows = EntityAnalysisModelActivationRuleOverrideMapper.ToDto(await repository.GetAsync(token)
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
                log.Error($"EntityAnalysisModelActivationRuleOverride.Filter: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Counts the Activation Rule Overrides in the caller's tenant matching " +
                     "query builder JSON (over the fields from " +
                     "EntityAnalysisModelActivationRuleOverrideFilterFields; empty counts " +
                     "all), optionally broken down by the values of one field. Invalid JSON is " +
                     "not an error: Valid is false and Errors gives each problem with its JSON " +
                     "path.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Activation Rule Overrides, using the fields from EntityAnalysisModelActivationRuleOverrideFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from EntityAnalysisModelActivationRuleOverrideFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelActivationRuleOverride.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.Count");
                var rows = EntityAnalysisModelActivationRuleOverrideMapper.ToDto(await repository.GetAsync(token)
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
                log.Error($"EntityAnalysisModelActivationRuleOverride.Count: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Registers a new Activation-Rule-scoped Override against a Model/Activation Rule in the " +
                     "caller's tenant. Not idempotent -- calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideCreate", OperationKind.Write,
            Idempotent = false)]
        public async Task<ActivationRuleOverridePoco> InsertAsync(
            [Description("The Override to create.")]
            EntityAnalysisModelActivationRuleOverrideDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelActivationRuleOverride.Create: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.Create");
                EnsureForceOverridePermitted("EntityAnalysisModelActivationRuleOverride.Create", model.OverrideKind);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleOverride.Create: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertAsync(EntityAnalysisModelActivationRuleOverrideMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelActivationRuleOverride.Create: created Id={saved.Id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelActivationRuleOverride.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelActivationRuleOverride.Create: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Validates an Activation Rule Override without saving it, running every check a create " +
                     "(Id 0) or an update (any other Id) would run, and returns each failure. Nothing is " +
                     "stored or changed.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideValidate", OperationKind.Read,
            Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Activation Rule Override to validate.")]
            EntityAnalysisModelActivationRuleOverrideDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride", "Validate", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelActivationRuleOverride.Validate: entry id={model.Id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.Validate");

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
                log.Error($"EntityAnalysisModelActivationRuleOverride.Validate: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Toggles the Override for the given Model/Activation Rule/OverrideKey/" +
                     "OverrideKeyValue combination in the caller's tenant: if a matching row already exists " +
                     "it is soft-deleted (override turned off), otherwise a new row is created (override " +
                     "turned on). NOT idempotent -- calling this twice in succession with the same arguments " +
                     "turns the override on then off. The DTO's own Active field is ignored; only row " +
                     "existence matters.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideUpdate", OperationKind.Write,
            Idempotent = false)]
        public async Task<ActivationRuleOverridePoco> UpdateAsync(
            [Description("The Override toggle request. If Id is non-zero it selects the row directly; " +
                         "otherwise EntityAnalysisModelGuid/OverrideKey/OverrideKeyValue/" +
                         "EntityAnalysisModelActivationRuleName select it. Identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            EntityAnalysisModelActivationRuleOverrideDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelActivationRuleOverride.Update: entry id={model.Id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.Update");
                EnsureForceOverridePermitted("EntityAnalysisModelActivationRuleOverride.Update", model.OverrideKind);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleOverride.Update: validation failed id={model.Id} " +
                            $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                ActivationRuleOverridePoco saved;
                try
                {
                    saved = await repository
                        .UpdateAsync(EntityAnalysisModelActivationRuleOverrideMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleOverride.Update: id={model.Id} not found or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Override was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelActivationRuleOverride.Update: toggled Id={saved.Id} user={userName}");
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
                    log.Debug(
                        $"EntityAnalysisModelActivationRuleOverride.Update: cancelled id={model.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleOverride.Update: unexpected failure id={model.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Updates the Delete Expiry Date of an existing, currently-active Activation-Rule-scoped " +
                     "Override matched by Model/Activation Rule/OverrideKey/OverrideKeyValue in the " +
                     "caller's tenant. Idempotent -- setting the same date twice has no further effect beyond " +
                     "incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideUpdateDeleteExpiryDate", OperationKind.Write,
            Idempotent = true)]
        public async Task<EntityAnalysisModelActivationRuleOverrideDto> UpdateDeleteExpiryDateAsync(
            [Description("EntityAnalysisModelGuid/OverrideKey/OverrideKeyValue/" +
                         "EntityAnalysisModelActivationRuleName select the row; DeleteExpiryDate is the new " +
                         "value (must be in the future, or null to clear it). Identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            EntityAnalysisModelActivationRuleOverrideDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride",
                "UpdateDeleteExpiryDate", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelActivationRuleOverride.UpdateDeleteExpiryDate: entry entityAnalysisModelGuid={model.EntityAnalysisModelGuid} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.UpdateDeleteExpiryDate");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleOverride.UpdateDeleteExpiryDate: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                ActivationRuleOverridePoco saved;
                try
                {
                    saved = await repository.UpdateDeleteExpiryDateAsync(model.EntityAnalysisModelGuid,
                        model.OverrideKey, model.OverrideKeyValue, model.EntityAnalysisModelActivationRuleName,
                        model.DeleteExpiryDate?.UtcDateTime, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleOverride.UpdateDeleteExpiryDate: no active Override matched " +
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
                        $"EntityAnalysisModelActivationRuleOverride.UpdateDeleteExpiryDate: Id={saved.Id} version->{saved.Version} user={userName}");
                }

                return EntityAnalysisModelActivationRuleOverrideMapper.ToDto(saved);
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
                        $"EntityAnalysisModelActivationRuleOverride.UpdateDeleteExpiryDate: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleOverride.UpdateDeleteExpiryDate: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Updates the Override Kind of an existing, currently-active Override matched " +
                     "by EntityAnalysisModelGuid/OverrideKey/OverrideKeyValue/EntityAnalysisModelActivationRuleName in the caller's tenant. " +
                     "Suppress mutes the bound Activation Rules' consequences; Force treats them as " +
                     "matched without evaluating them, which also applies where the Activation Rule " +
                     "has been retired. Idempotent beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideUpdateOverrideKind", OperationKind.Write,
            Idempotent = true)]
        public async Task<EntityAnalysisModelActivationRuleOverrideDto> UpdateOverrideKindAsync(
            [Description(
                "EntityAnalysisModelGuid/OverrideKey/OverrideKeyValue/EntityAnalysisModelActivationRuleName select the row; "
                + "OverrideKind is the new value. Identity/tenant/audit fields are "
                + "server-owned and ignored.")]
            EntityAnalysisModelActivationRuleOverrideDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride",
                "UpdateOverrideKind", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelActivationRuleOverride.UpdateOverrideKind: entry entityAnalysisModelGuid={model.EntityAnalysisModelGuid} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.UpdateOverrideKind");
                EnsureForceOverridePermitted("EntityAnalysisModelActivationRuleOverride.UpdateOverrideKind",
                    model.OverrideKind);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleOverride.UpdateOverrideKind: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                ActivationRuleOverridePoco saved;
                try
                {
                    saved = await repository.UpdateOverrideKindAsync(model.EntityAnalysisModelGuid,
                        model.OverrideKey, model.OverrideKeyValue,
                        model.EntityAnalysisModelActivationRuleName, (byte)model.OverrideKind,
                        token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleOverride.UpdateOverrideKind: no active Override matched " +
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
                        $"EntityAnalysisModelActivationRuleOverride.UpdateOverrideKind: Id={saved.Id} version->{saved.Version} user={userName}");
                }

                return EntityAnalysisModelActivationRuleOverrideMapper.ToDto(saved);
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
                        $"EntityAnalysisModelActivationRuleOverride.UpdateOverrideKind: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleOverride.UpdateOverrideKind: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes an Activation-Rule-scoped Override in the caller's tenant by its Id. Reversible " +
                     "at the data level, but treat as destructive -- the Override immediately stops applying " +
                     "on transaction invocation.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideDelete", OperationKind.Delete,
            Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Override to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverride", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelActivationRuleOverride.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverride.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleOverride.Delete: id={id} not found, already deleted, expired, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Override was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelActivationRuleOverride.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug(
                        $"EntityAnalysisModelActivationRuleOverride.Delete: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleOverride.Delete: unexpected failure id={id} user={userName}",
                    ex);
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

            throw new ForbiddenException(
                strings[EntityAnalysisModelActivationRuleOverrideResources.ForceOverridePermissionDenied],
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

            throw new ForbiddenException(
                strings[EntityAnalysisModelActivationRuleOverrideResources.PermissionDenied], permissions);
        }
    }
}