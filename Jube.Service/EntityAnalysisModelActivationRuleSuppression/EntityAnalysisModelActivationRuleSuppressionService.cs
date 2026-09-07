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
using Jube.Dto.EntityAnalysisModelActivationRuleSuppression;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelActivationRuleSuppression;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelActivationRuleSuppression;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelActivationRuleSuppression
{
    using ActivationRuleSuppressionPoco = Data.Poco.EntityAnalysisModelActivationRuleSuppression;

    public sealed class EntityAnalysisModelActivationRuleSuppressionService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [2];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelActivationRuleSuppressionRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelActivationRuleSuppressionDtoValidator validator;

        private EntityAnalysisModelActivationRuleSuppressionService(DbContext dbContext, string userName,
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
            repository = new EntityAnalysisModelActivationRuleSuppressionRepository(dbContext, userName);
            validator = new EntityAnalysisModelActivationRuleSuppressionDtoValidator(
                new EntityAnalysisModelRepository(dbContext, userName),
                new EntityAnalysisModelActivationRuleRepository(dbContext, userName), strings);
        }

        public static Task<EntityAnalysisModelActivationRuleSuppressionService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelActivationRuleSuppressionService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelActivationRuleSuppressionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("EntityAnalysisModelActivationRuleSuppression.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelActivationRuleSuppressionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"EntityAnalysisModelActivationRuleSuppression.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelActivationRuleSuppressionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelActivationRuleSuppressionService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Activation-Rule-scoped Suppression row visible to the calling user's tenant. " +
                     "Unbounded -- intended for the administrative page, not for agent tooling (use the bounded " +
                     "list operation instead). Note this returns raw rows including soft-deleted and expired " +
                     "ones -- a pre-existing quirk of the underlying query, see the migration report.")]
        public async Task<List<EntityAnalysisModelActivationRuleSuppressionDto>> GetAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleSuppression", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelActivationRuleSuppression.List: entry user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleSuppression.List");
                var dtos = EntityAnalysisModelActivationRuleSuppressionMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelActivationRuleSuppression.List: {dtos.Count} rows user={userName}");

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
                    log.Debug($"EntityAnalysisModelActivationRuleSuppression.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelActivationRuleSuppression.List: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists Activation-Rule-scoped Suppressions belonging to the given Model, scoped to the " +
                     "calling user's tenant. Excludes soft-deleted rows and rows past their Delete Expiry Date.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleSuppressionGetByEntityAnalysisModelGuid",
            OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelActivationRuleSuppressionDto>> GetByEntityAnalysisModelGuidAsync(
            [Description("Guid identifier of the parent Model.")]
            Guid entityAnalysisModelGuid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleSuppression",
                "ListByEntityAnalysisModelGuid", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelActivationRuleSuppression.ListByEntityAnalysisModelGuid: entry entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleSuppression.ListByEntityAnalysisModelGuid");
                var dtos = EntityAnalysisModelActivationRuleSuppressionMapper.ToDto(await repository
                    .GetByEntityAnalysisModelGuidOrderByIdAsync(entityAnalysisModelGuid, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelActivationRuleSuppression.ListByEntityAnalysisModelGuid: {dtos.Count} rows entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}");

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
                        $"EntityAnalysisModelActivationRuleSuppression.ListByEntityAnalysisModelGuid: cancelled entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleSuppression.ListByEntityAnalysisModelGuid: unexpected failure entityAnalysisModelGuid={entityAnalysisModelGuid} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Activation-Rule-scoped Suppression by its numeric identifier, scoped to the " +
                     "calling user's tenant. Returns null when the row does not exist, is soft-deleted, is past " +
                     "its Delete Expiry Date, or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleSuppressionGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelActivationRuleSuppressionDto?> GetByIdAsync(
            [Description("Numeric identifier of the Suppression.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleSuppression", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelActivationRuleSuppression.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleSuppression.Get");
                var suppression = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (suppression == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"EntityAnalysisModelActivationRuleSuppression.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(suppression.Id);
                return EntityAnalysisModelActivationRuleSuppressionMapper.ToDto(suppression);
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
                    log.Debug($"EntityAnalysisModelActivationRuleSuppression.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleSuppression.Get: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists Activation-Rule-scoped Suppressions for the caller's tenant, ordered by id, capped " +
                     "at 'take' rows (max 200). If 'more' is true, call again with 'afterId' set to the last " +
                     "returned Id to continue.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleSuppressionList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelActivationRuleSuppressionDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleSuppression", "ListPaged",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelActivationRuleSuppression.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleSuppression.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelActivationRuleSuppressionDto>(
                    EntityAnalysisModelActivationRuleSuppressionMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelActivationRuleSuppression.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleSuppression.ListPaged: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Registers a new Activation-Rule-scoped Suppression against a Model/Activation Rule in the " +
                     "caller's tenant. Not idempotent -- calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleSuppressionCreate", OperationKind.Write,
            Idempotent = false)]
        public async Task<ActivationRuleSuppressionPoco> InsertAsync(
            [Description("The Suppression to create.")]
            EntityAnalysisModelActivationRuleSuppressionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleSuppression", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelActivationRuleSuppression.Create: entry user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelActivationRuleSuppression.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleSuppression.Create: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertAsync(EntityAnalysisModelActivationRuleSuppressionMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelActivationRuleSuppression.Create: created Id={saved.Id} user={userName}");

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
                    log.Debug($"EntityAnalysisModelActivationRuleSuppression.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelActivationRuleSuppression.Create: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Toggles the Suppression for the given Model/Activation Rule/SuppressionKey/" +
                     "SuppressionKeyValue combination in the caller's tenant: if a matching row already exists " +
                     "it is soft-deleted (suppression turned off), otherwise a new row is created (suppression " +
                     "turned on). NOT idempotent -- calling this twice in succession with the same arguments " +
                     "turns the suppression on then off. The DTO's own Active field is ignored; only row " +
                     "existence matters.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleSuppressionUpdate", OperationKind.Write,
            Idempotent = false)]
        public async Task<ActivationRuleSuppressionPoco> UpdateAsync(
            [Description("The Suppression toggle request. If Id is non-zero it selects the row directly; " +
                         "otherwise EntityAnalysisModelGuid/SuppressionKey/SuppressionKeyValue/" +
                         "EntityAnalysisModelActivationRuleName select it. Identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            EntityAnalysisModelActivationRuleSuppressionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleSuppression", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelActivationRuleSuppression.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelActivationRuleSuppression.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleSuppression.Update: validation failed id={model.Id} " +
                            $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                ActivationRuleSuppressionPoco saved;
                try
                {
                    saved = await repository
                        .UpdateAsync(EntityAnalysisModelActivationRuleSuppressionMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleSuppression.Update: id={model.Id} not found or not visible to tenant user={userName}");

                    throw new NotFoundException("The Suppression was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelActivationRuleSuppression.Update: toggled Id={saved.Id} user={userName}");

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
                        $"EntityAnalysisModelActivationRuleSuppression.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleSuppression.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Updates the Delete Expiry Date of an existing, currently-active Activation-Rule-scoped " +
                     "Suppression matched by Model/Activation Rule/SuppressionKey/SuppressionKeyValue in the " +
                     "caller's tenant. Idempotent -- setting the same date twice has no further effect beyond " +
                     "incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleSuppressionUpdateDeleteExpiryDate", OperationKind.Write,
            Idempotent = true)]
        public async Task<EntityAnalysisModelActivationRuleSuppressionDto> UpdateDeleteExpiryDateAsync(
            [Description("EntityAnalysisModelGuid/SuppressionKey/SuppressionKeyValue/" +
                         "EntityAnalysisModelActivationRuleName select the row; DeleteExpiryDate is the new " +
                         "value (must be in the future, or null to clear it). Identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            EntityAnalysisModelActivationRuleSuppressionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleSuppression",
                "UpdateDeleteExpiryDate", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelActivationRuleSuppression.UpdateDeleteExpiryDate: entry entityAnalysisModelGuid={model?.EntityAnalysisModelGuid} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelActivationRuleSuppression.UpdateDeleteExpiryDate");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleSuppression.UpdateDeleteExpiryDate: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                ActivationRuleSuppressionPoco saved;
                try
                {
                    saved = await repository.UpdateDeleteExpiryDateAsync(model.EntityAnalysisModelGuid,
                        model.SuppressionKey, model.SuppressionKeyValue, model.EntityAnalysisModelActivationRuleName,
                        model.DeleteExpiryDate?.UtcDateTime, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleSuppression.UpdateDeleteExpiryDate: no active Suppression matched " +
                            $"entityAnalysisModelGuid={model.EntityAnalysisModelGuid} user={userName}");

                    throw new NotFoundException("The Suppression was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelActivationRuleSuppression.UpdateDeleteExpiryDate: Id={saved.Id} version->{saved.Version} user={userName}");

                return EntityAnalysisModelActivationRuleSuppressionMapper.ToDto(saved)!;
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
                        $"EntityAnalysisModelActivationRuleSuppression.UpdateDeleteExpiryDate: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleSuppression.UpdateDeleteExpiryDate: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes an Activation-Rule-scoped Suppression in the caller's tenant by its Id. Reversible " +
                     "at the data level, but treat as destructive -- the Suppression immediately stops applying " +
                     "on transaction invocation.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleSuppressionDelete", OperationKind.Delete,
            Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Suppression to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleSuppression", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelActivationRuleSuppression.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleSuppression.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelActivationRuleSuppression.Delete: id={id} not found, already deleted, expired, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Suppression was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelActivationRuleSuppression.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug(
                        $"EntityAnalysisModelActivationRuleSuppression.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRuleSuppression.Delete: unexpected failure id={id} user={userName}",
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
                strings[EntityAnalysisModelActivationRuleSuppressionResources.PermissionDenied], permissions);
        }
    }
}