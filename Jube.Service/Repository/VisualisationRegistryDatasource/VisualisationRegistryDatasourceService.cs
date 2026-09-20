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
using Jube.Data.Validation;
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.Repository.VisualisationRegistryDatasource;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.VisualisationRegistryDatasource;
using log4net;
using Microsoft.Extensions.Localization;
using DatasourcePoco = Jube.Data.Poco.VisualisationRegistryDatasource;

namespace Jube.Service.Repository.VisualisationRegistryDatasource
{
    public sealed class VisualisationRegistryDatasourceService
    {
        private const int MaxListTake = 200;
        private static readonly int[] listPermissions = [33];
        private static readonly int[] readPermissions = [33];
        private static readonly int[] readByParentPermissions = [33, 28, 1];
        private static readonly int[] writePermissions = [33];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly VisualisationRegistryDatasourceRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly VisualisationRegistryDatasourceDtoValidator validator;

        private VisualisationRegistryDatasourceService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.dynamicEnvironment = dynamicEnvironment;
            repository = new VisualisationRegistryDatasourceRepository(dbContext, userName);
            var visualisationRegistryRepository = new VisualisationRegistryRepository(dbContext, tenantRegistryId);
            validator = new VisualisationRegistryDatasourceDtoValidator(repository, visualisationRegistryRepository,
                strings);
        }

        public static Task<VisualisationRegistryDatasourceService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<VisualisationRegistryDatasourceService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(VisualisationRegistryDatasourceResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("VisualisationRegistryDatasource.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[VisualisationRegistryDatasourceResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"VisualisationRegistryDatasource.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[VisualisationRegistryDatasourceResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new VisualisationRegistryDatasourceService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment);
        }

        [Description("Lists every Datasource visible to the calling user's tenant. Unbounded -- intended for " +
                     "the administrative page, not for agent tooling (use the bounded list operation instead).")]
        public async Task<List<VisualisationRegistryDatasourceDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasource", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryDatasource.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "VisualisationRegistryDatasource.List");
                var dtos = VisualisationRegistryDatasourceMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"VisualisationRegistryDatasource.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"VisualisationRegistryDatasource.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistryDatasource.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Datasources belonging to the given Visualisation Registry, ordered by Id, scoped to " +
                     "the calling user's tenant.")]
        [ServiceOperation("VisualisationRegistryDatasourceGetByVisualisationRegistryId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<VisualisationRegistryDatasourceDto>> GetByVisualisationRegistryIdAsync(
            [Description("Numeric identifier of the parent Visualisation Registry.")]
            int visualisationRegistryId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasource", "ListByVisualisationRegistryId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"VisualisationRegistryDatasource.ListByVisualisationRegistryId: entry visualisationRegistryId={visualisationRegistryId} user={userName}");
            }

            try
            {
                EnsurePermitted(readByParentPermissions,
                    "VisualisationRegistryDatasource.ListByVisualisationRegistryId");
                var dtos = VisualisationRegistryDatasourceMapper.ToDto(await repository
                    .GetByVisualisationRegistryIdOrderByIdAsync(visualisationRegistryId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"VisualisationRegistryDatasource.ListByVisualisationRegistryId: {dtos.Count} rows visualisationRegistryId={visualisationRegistryId} user={userName}");
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
                        $"VisualisationRegistryDatasource.ListByVisualisationRegistryId: cancelled visualisationRegistryId={visualisationRegistryId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"VisualisationRegistryDatasource.ListByVisualisationRegistryId: unexpected failure visualisationRegistryId={visualisationRegistryId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists active Datasources belonging to the given Visualisation Registry that the calling " +
                     "user's Role assignments grant visibility to, ordered by Priority. Used by the live " +
                     "dashboard renderer.")]
        [ServiceOperation("VisualisationRegistryDatasourceGetByVisualisationRegistryIdActiveOnly",
            OperationKind.Read, Idempotent = true)]
        public async Task<List<VisualisationRegistryDatasourceDto>> GetByVisualisationRegistryIdActiveOnlyAsync(
            [Description("Numeric identifier of the parent Visualisation Registry.")]
            int visualisationRegistryId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasource",
                "ListByVisualisationRegistryIdActiveOnly", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"VisualisationRegistryDatasource.ListByVisualisationRegistryIdActiveOnly: entry visualisationRegistryId={visualisationRegistryId} user={userName}");
            }

            try
            {
                EnsurePermitted(readByParentPermissions,
                    "VisualisationRegistryDatasource.ListByVisualisationRegistryIdActiveOnly");
                var dtos = VisualisationRegistryDatasourceMapper.ToDto(await repository
                    .GetByVisualisationRegistryIdActiveOnlyAsync(visualisationRegistryId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"VisualisationRegistryDatasource.ListByVisualisationRegistryIdActiveOnly: {dtos.Count} rows visualisationRegistryId={visualisationRegistryId} user={userName}");
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
                        $"VisualisationRegistryDatasource.ListByVisualisationRegistryIdActiveOnly: cancelled visualisationRegistryId={visualisationRegistryId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"VisualisationRegistryDatasource.ListByVisualisationRegistryIdActiveOnly: unexpected failure visualisationRegistryId={visualisationRegistryId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Datasource by its numeric identifier, scoped to the calling user's tenant. " +
                     "Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("VisualisationRegistryDatasourceGet", OperationKind.Read, Idempotent = true)]
        public async Task<VisualisationRegistryDatasourceDto?> GetByIdAsync(
            [Description("Numeric identifier of the Datasource.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasource", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryDatasource.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(readPermissions, "VisualisationRegistryDatasource.Get");
                var datasource = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (datasource == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"VisualisationRegistryDatasource.Get: id={id} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(datasource.Id);
                return VisualisationRegistryDatasourceMapper.ToDto(datasource);
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
                    log.Debug($"VisualisationRegistryDatasource.Get: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistryDatasource.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Datasources for the caller's tenant, ordered by id, capped at 'take' rows (max " +
                     "200). If 'more' is true, call again with 'afterId' set to the last returned Id to " +
                     "continue.")]
        [ServiceOperation("VisualisationRegistryDatasourceList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<VisualisationRegistryDatasourceDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasource", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"VisualisationRegistryDatasource.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "VisualisationRegistryDatasource.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<VisualisationRegistryDatasourceDto>(
                    VisualisationRegistryDatasourceMapper.ToDto(page));
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
                    log.Debug($"VisualisationRegistryDatasource.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistryDatasource.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Datasource under a Visualisation Registry in the caller's tenant. The " +
                     "SQL Command is validated against the reporting connection before the row is persisted. " +
                     "Not idempotent -- calling twice creates two rows.")]
        [ServiceOperation("VisualisationRegistryDatasourceCreate", OperationKind.Write, Idempotent = false)]
        public async Task<DatasourcePoco> InsertAsync(
            [Description("The Datasource to create.")]
            VisualisationRegistryDatasourceDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasource", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryDatasource.Create: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "VisualisationRegistryDatasource.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"VisualisationRegistryDatasource.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                DatasourcePoco saved;
                try
                {
                    saved = await UniqueViolation.GuardAsync(() => repository.InsertWithValidationAsync(
                                VisualisationRegistryDatasourceMapper.ToPoco(model), log,
                                dynamicEnvironment.AppSettings("ReportConnectionString") ??
                                dbContext.Connection.ConnectionString,
                                dynamicEnvironment.ParserAssertSelectOnly(), token),
                            r => new DtoValidationException(r),
                            strings[VisualisationRegistryDatasourceResources.NameAlreadyExists])
                        .ConfigureAwait(false);
                }
                catch (SqlValidationFailed ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"VisualisationRegistryDatasource.Create: SQL validation failed user={userName}",
                            ex);
                    }

                    throw new SqlValidationFailedException(ex.Message, ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"VisualisationRegistryDatasource.Create: created Id={saved.Id} name={saved.Name} " +
                        $"user={userName}");
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
            catch (SqlValidationFailedException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"VisualisationRegistryDatasource.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistryDatasource.Create: unexpected failure user={userName} " +
                          $"name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Updates an existing Datasource in the caller's tenant, identified by its Id. The SQL " +
                     "Command is re-validated against the reporting connection before the row is persisted. " +
                     "Idempotent -- repeating the same update has no further effect beyond incrementing Version.")]
        [ServiceOperation("VisualisationRegistryDatasourceUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<DatasourcePoco> UpdateAsync(
            [Description("The Datasource to update. Id selects the row; identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            VisualisationRegistryDatasourceDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasource", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryDatasource.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "VisualisationRegistryDatasource.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"VisualisationRegistryDatasource.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                DatasourcePoco saved;
                var poco = VisualisationRegistryDatasourceMapper.ToPoco(model);
                try
                {
                    saved = await UniqueViolation.GuardAsync(() => repository.UpdateWithValidationAsync(poco, log,
                                dynamicEnvironment.AppSettings("ReportConnectionString") ??
                                dbContext.Connection.ConnectionString,
                                dynamicEnvironment.ParserAssertSelectOnly(), token),
                            r => new DtoValidationException(r),
                            strings[VisualisationRegistryDatasourceResources.NameAlreadyExists])
                        .ConfigureAwait(false);
                }
                catch (SqlValidationFailed ex)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"VisualisationRegistryDatasource.Update: SQL validation failed id={model.Id} " +
                                 $"user={userName}", ex);
                    }

                    throw new SqlValidationFailedException(ex.Message, ex);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"VisualisationRegistryDatasource.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Datasource was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"VisualisationRegistryDatasource.Update: Id={saved.Id} version->{saved.Version} user={userName}");
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
            catch (SqlValidationFailedException)
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
                    log.Debug($"VisualisationRegistryDatasource.Update: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"VisualisationRegistryDatasource.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes a Datasource in the caller's tenant by its Id. Reversible at the data level, but " +
                     "treat as destructive -- the Datasource immediately stops rendering on the dashboard.")]
        [ServiceOperation("VisualisationRegistryDatasourceDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Datasource to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasource", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryDatasource.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(writePermissions, "VisualisationRegistryDatasource.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"VisualisationRegistryDatasource.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Datasource was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"VisualisationRegistryDatasource.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"VisualisationRegistryDatasource.Delete: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"VisualisationRegistryDatasource.Delete: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        private void EnsurePermitted(int[] specs, string op)
        {
            if (permissionValidation.Validate(specs))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");
            }

            throw new ForbiddenException(strings[VisualisationRegistryDatasourceResources.PermissionDenied], specs);
        }
    }
}