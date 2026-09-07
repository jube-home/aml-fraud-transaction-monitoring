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
using Jube.Dto.EntityAnalysisModelReprocessingRule;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelReprocessingRule;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelReprocessingRule;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelReprocessingRule
{
    using ReprocessingRulePoco = Data.Poco.EntityAnalysisModelReprocessingRule;

    public sealed class EntityAnalysisModelReprocessingRuleService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [26];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelReprocessingRuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelReprocessingRuleDtoValidator validator;

        private EntityAnalysisModelReprocessingRuleService(DbContext dbContext, string userName,
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
            repository = new EntityAnalysisModelReprocessingRuleRepository(dbContext, userName);
            validator = new EntityAnalysisModelReprocessingRuleDtoValidator(repository, strings);
        }

        public static Task<EntityAnalysisModelReprocessingRuleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelReprocessingRuleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelReprocessingRuleResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("EntityAnalysisModelReprocessingRule.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelReprocessingRuleResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"EntityAnalysisModelReprocessingRule.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelReprocessingRuleResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelReprocessingRuleService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Reprocessing Rule visible to the calling user's tenant. Unbounded -- intended " +
                     "for the administrative page, not for agent tooling (use the bounded list operation " +
                     "instead). Note this returns raw rows including soft-deleted ones -- a pre-existing quirk " +
                     "of the underlying query, see the migration report.")]
        public async Task<List<EntityAnalysisModelReprocessingRuleDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRule", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelReprocessingRule.List: entry user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelReprocessingRule.List");
                var dtos = EntityAnalysisModelReprocessingRuleMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelReprocessingRule.List: {dtos.Count} rows user={userName}");

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
                    log.Debug($"EntityAnalysisModelReprocessingRule.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelReprocessingRule.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Reprocessing Rules belonging to the given Model, scoped to the calling user's " +
                     "tenant.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelReprocessingRuleDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRule",
                "ListByEntityAnalysisModelId", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelReprocessingRule.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelReprocessingRule.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelReprocessingRuleMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdAsync(entityAnalysisModelId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelReprocessingRule.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");

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
                        $"EntityAnalysisModelReprocessingRule.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelReprocessingRule.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Reprocessing Rule by its numeric identifier, scoped to the calling user's " +
                     "tenant. Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelReprocessingRuleDto?> GetByIdAsync(
            [Description("Numeric identifier of the Reprocessing Rule.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRule", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelReprocessingRule.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelReprocessingRule.Get");
                var reprocessingRule = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (reprocessingRule == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"EntityAnalysisModelReprocessingRule.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(reprocessingRule.Id);
                return EntityAnalysisModelReprocessingRuleMapper.ToDto(reprocessingRule);
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
                    log.Debug($"EntityAnalysisModelReprocessingRule.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelReprocessingRule.Get: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists Reprocessing Rules for the caller's tenant, ordered by id, capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last returned Id to " +
                     "continue. Note this includes soft-deleted rows -- a pre-existing quirk of the underlying " +
                     "query, see the migration report.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelReprocessingRuleDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRule", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelReprocessingRule.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelReprocessingRule.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelReprocessingRuleDto>(
                    EntityAnalysisModelReprocessingRuleMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelReprocessingRule.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelReprocessingRule.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Reprocessing Rule under a Model in the caller's tenant. Not idempotent -- " +
                     "calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleCreate", OperationKind.Write, Idempotent = false)]
        public async Task<ReprocessingRulePoco> InsertAsync(
            [Description("The Reprocessing Rule to create.")]
            EntityAnalysisModelReprocessingRuleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRule", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelReprocessingRule.Create: entry user={userName} name={model?.Name}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelReprocessingRule.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelReprocessingRule.Create: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertAsync(EntityAnalysisModelReprocessingRuleMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelReprocessingRule.Create: created Id={saved.Id} name={saved.Name} " +
                        $"user={userName}");

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
                    log.Debug($"EntityAnalysisModelReprocessingRule.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelReprocessingRule.Create: unexpected failure user={userName} " +
                          $"name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Updates an existing Reprocessing Rule in the caller's tenant, identified by its Id. " +
                     "NOT idempotent in the usual sense -- the row is superseded rather than updated in place: " +
                     "a new Id is assigned, the old row is soft-deleted, and CreatedUser/CreatedDate are reset " +
                     "to the caller/now. See the migration report.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleUpdate", OperationKind.Write, Idempotent = false)]
        public async Task<ReprocessingRulePoco> UpdateAsync(
            [Description("The Reprocessing Rule to update. Id selects the row; identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            EntityAnalysisModelReprocessingRuleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRule", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelReprocessingRule.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelReprocessingRule.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelReprocessingRule.Update: validation failed id={model.Id} " +
                            $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                ReprocessingRulePoco saved;
                try
                {
                    saved = await repository
                        .UpdateAsync(EntityAnalysisModelReprocessingRuleMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelReprocessingRule.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Reprocessing Rule was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelReprocessingRule.Update: superseded Id={model.Id} with new Id={saved.Id} version->{saved.Version} user={userName}");

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
                        $"EntityAnalysisModelReprocessingRule.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelReprocessingRule.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes a Reprocessing Rule in the caller's tenant by its Id. Reversible at the data " +
                     "level, but treat as destructive -- the Reprocessing Rule immediately stops being " +
                     "evaluated during reprocessing runs.")]
        [ServiceOperation("EntityAnalysisModelReprocessingRuleDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Reprocessing Rule to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelReprocessingRule", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelReprocessingRule.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelReprocessingRule.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelReprocessingRule.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Reprocessing Rule was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelReprocessingRule.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelReprocessingRule.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelReprocessingRule.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions)) return;

            if (log.IsWarnEnabled)
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");

            throw new ForbiddenException(strings[EntityAnalysisModelReprocessingRuleResources.PermissionDenied],
                permissions);
        }
    }
}
