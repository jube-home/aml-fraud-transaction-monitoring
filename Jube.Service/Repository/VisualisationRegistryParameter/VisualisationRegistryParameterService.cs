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
using Jube.Dto.Repository.VisualisationRegistryParameter;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.Repository.VisualisationRegistryParameter;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.VisualisationRegistryParameter;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.VisualisationRegistryParameterRepository;
using VisualisationRegistryParameterPoco = Jube.Data.Poco.VisualisationRegistryParameter;

namespace Jube.Service.Repository.VisualisationRegistryParameter
{
    public sealed class VisualisationRegistryParameterService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [32];
        private static readonly int[] activeOnlyPermissions = [32, 28, 1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly VisualisationRegistryParameterDtoValidator validator;

        private VisualisationRegistryParameterService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new RuleRepository(dbContext, userName);
            validator = new VisualisationRegistryParameterDtoValidator(
                new VisualisationRegistryRepository(dbContext, userName), repository, strings);
        }

        public static Task<VisualisationRegistryParameterService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<VisualisationRegistryParameterService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(VisualisationRegistryParameterResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("VisualisationRegistryParameter.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[VisualisationRegistryParameterResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"VisualisationRegistryParameter.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[VisualisationRegistryParameterResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new VisualisationRegistryParameterService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Visualisation Registry Parameter visible to the calling user's tenant. " +
                     "Unbounded -- intended for the administrative page, not for agent tooling (use the bounded " +
                     "list operation instead).")]
        public async Task<List<VisualisationRegistryParameterDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameter", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryParameter.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryParameter.List", permissions);
                var dtos = VisualisationRegistryParameterMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"VisualisationRegistryParameter.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"VisualisationRegistryParameter.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistryParameter.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns one Visualisation Registry Parameter by its Id, scoped to the calling user's " +
                     "tenant. Returns null when no matching row is visible to the caller.")]
        [ServiceOperation("VisualisationRegistryParameterGet", OperationKind.Read, Idempotent = true)]
        public async Task<VisualisationRegistryParameterDto?> GetByIdAsync(
            [Description("Numeric identifier of the Parameter.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameter", "Get", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryParameter.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryParameter.Get", permissions);
                var visualisationRegistryParameter = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (visualisationRegistryParameter == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"VisualisationRegistryParameter.Get: id={id} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(visualisationRegistryParameter.Id);
                return VisualisationRegistryParameterMapper.ToDto(visualisationRegistryParameter);
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
                    log.Debug($"VisualisationRegistryParameter.Get: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistryParameter.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the Parameters belonging to a given Visualisation Registry, ordered by Id, scoped " +
                     "to the calling user's tenant. Includes inactive Parameters.")]
        [ServiceOperation("VisualisationRegistryParameterGetByVisualisationRegistryId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<VisualisationRegistryParameterDto>> GetByVisualisationRegistryIdAsync(
            [Description("Numeric identifier of the parent Visualisation Registry.")]
            int visualisationRegistryId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameter", "ListByVisualisationRegistryId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"VisualisationRegistryParameter.ListByVisualisationRegistryId: entry visualisationRegistryId={visualisationRegistryId} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryParameter.ListByVisualisationRegistryId", activeOnlyPermissions);
                var dtos = VisualisationRegistryParameterMapper.ToDto(await repository
                    .GetByVisualisationRegistryIdOrderByIdAsync(visualisationRegistryId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"VisualisationRegistryParameter.ListByVisualisationRegistryId: {dtos.Count} rows visualisationRegistryId={visualisationRegistryId} user={userName}");
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
                        $"VisualisationRegistryParameter.ListByVisualisationRegistryId: cancelled visualisationRegistryId={visualisationRegistryId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"VisualisationRegistryParameter.ListByVisualisationRegistryId: unexpected failure visualisationRegistryId={visualisationRegistryId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists the active Parameters belonging to a given Visualisation Registry, ordered by Id, " +
                     "visible only when the caller holds a role grant on both the parent Visualisation Registry " +
                     "and the Parameter itself, scoped to the calling user's tenant.")]
        [ServiceOperation("VisualisationRegistryParameterGetByVisualisationRegistryIdActiveOnly", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<VisualisationRegistryParameterDto>> GetByVisualisationRegistryIdActiveOnlyAsync(
            [Description("Numeric identifier of the parent Visualisation Registry.")]
            int visualisationRegistryId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameter",
                "ListByVisualisationRegistryIdActiveOnly", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"VisualisationRegistryParameter.ListByVisualisationRegistryIdActiveOnly: entry visualisationRegistryId={visualisationRegistryId} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryParameter.ListByVisualisationRegistryIdActiveOnly",
                    activeOnlyPermissions);
                var dtos = VisualisationRegistryParameterMapper.ToDto(await repository
                    .GetByVisualisationRegistryIdActiveOnlyAsync(visualisationRegistryId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"VisualisationRegistryParameter.ListByVisualisationRegistryIdActiveOnly: {dtos.Count} rows visualisationRegistryId={visualisationRegistryId} user={userName}");
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
                        $"VisualisationRegistryParameter.ListByVisualisationRegistryIdActiveOnly: cancelled visualisationRegistryId={visualisationRegistryId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"VisualisationRegistryParameter.ListByVisualisationRegistryIdActiveOnly: unexpected failure visualisationRegistryId={visualisationRegistryId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists Visualisation Registry Parameters for the caller's tenant, ordered by id, capped at " +
                     "'take' rows (max 200). If 'more' is true, call again with 'afterId' set to the last " +
                     "returned Id to continue.")]
        [ServiceOperation("VisualisationRegistryParameterList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<VisualisationRegistryParameterDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameter", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"VisualisationRegistryParameter.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryParameter.ListPaged", permissions);

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<VisualisationRegistryParameterDto>(
                    VisualisationRegistryParameterMapper.ToDto(page));
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
                    log.Debug($"VisualisationRegistryParameter.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistryParameter.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Parameter under a Visualisation Registry in the caller's tenant. Not " +
                     "idempotent -- calling twice creates two rows.")]
        [ServiceOperation("VisualisationRegistryParameterCreate", OperationKind.Write, Idempotent = false)]
        public async Task<VisualisationRegistryParameterPoco> InsertAsync(
            [Description("The Parameter to create.")]
            VisualisationRegistryParameterDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameter", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryParameter.Create: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("VisualisationRegistryParameter.Create", permissions);
                using var nameGate = await NameGate.EnterAsync(
                        $"{tenantRegistryId}|VisualisationRegistryParameter|{model.VisualisationRegistryId}|{model.Name}",
                        token)
                    .ConfigureAwait(false);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"VisualisationRegistryParameter.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await UniqueViolation.GuardAsync(
                        () => repository.InsertAsync(VisualisationRegistryParameterMapper.ToPoco(model), token),
                        r => new DtoValidationException(r),
                        strings[VisualisationRegistryParameterResources.NameAlreadyExists])
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"VisualisationRegistryParameter.Create: created Id={saved.Id} user={userName}");
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
                    log.Debug($"VisualisationRegistryParameter.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistryParameter.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Parameter in the caller's tenant, identified by its Id. Idempotent -- " +
                     "repeating the same update has no further effect beyond incrementing Version. Fails when " +
                     "the Parameter is locked.")]
        [ServiceOperation("VisualisationRegistryParameterUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<VisualisationRegistryParameterPoco> UpdateAsync(
            [Description("The Parameter to update. Id selects the row; identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            VisualisationRegistryParameterDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameter", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryParameter.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("VisualisationRegistryParameter.Update", permissions);
                using var nameGate = await NameGate.EnterAsync(
                        $"{tenantRegistryId}|VisualisationRegistryParameter|{model.VisualisationRegistryId}|{model.Name}",
                        token)
                    .ConfigureAwait(false);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"VisualisationRegistryParameter.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                VisualisationRegistryParameterPoco saved;
                try
                {
                    saved = await UniqueViolation.GuardAsync(
                            () => repository.UpdateAsync(VisualisationRegistryParameterMapper.ToPoco(model), token),
                            r => new DtoValidationException(r),
                            strings[VisualisationRegistryParameterResources.NameAlreadyExists])
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"VisualisationRegistryParameter.Update: id={model.Id} not found, deleted, locked, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Visualisation Registry Parameter was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"VisualisationRegistryParameter.Update: Id={saved.Id} version->{saved.Version} user={userName}");
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
                    log.Debug($"VisualisationRegistryParameter.Update: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"VisualisationRegistryParameter.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes a Parameter in the caller's tenant by its Id. Soft-delete -- the Parameter " +
                     "immediately stops being available for collection or reporting. Fails when the Parameter " +
                     "is locked.")]
        [ServiceOperation("VisualisationRegistryParameterDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Parameter to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryParameter", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryParameter.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryParameter.Delete", permissions);

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"VisualisationRegistryParameter.Delete: id={id} not found, already deleted, locked, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Visualisation Registry Parameter was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"VisualisationRegistryParameter.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"VisualisationRegistryParameter.Delete: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistryParameter.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[VisualisationRegistryParameterResources.PermissionDenied], specs);
        }
    }
}