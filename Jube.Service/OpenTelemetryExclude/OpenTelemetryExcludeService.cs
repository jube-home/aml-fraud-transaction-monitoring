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
using Jube.Dto.Validation;
using Jube.Dto.OpenTelemetryExclude;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.OpenTelemetryExclude;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.OpenTelemetryExclude;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.OpenTelemetryExclude
{
    using ExcludePoco = Data.Poco.OpenTelemetryExclude;

    public sealed class OpenTelemetryExcludeService
    {
        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly OpenTelemetryExcludeRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly OpenTelemetryExcludeDtoValidator validator;

        private OpenTelemetryExcludeService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new OpenTelemetryExcludeRepository(dbContext, userName);
            validator = new OpenTelemetryExcludeDtoValidator(strings);
        }

        public static Task<OpenTelemetryExcludeService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<OpenTelemetryExcludeService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(OpenTelemetryExcludeResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("OpenTelemetryExclude.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[OpenTelemetryExcludeResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"OpenTelemetryExclude.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[OpenTelemetryExcludeResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new OpenTelemetryExcludeService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every OpenTelemetryExclude row, most recent first. Not scoped to any tenant.")]
        [ServiceOperation("OpenTelemetryExcludeList", OperationKind.Read, Idempotent = true)]
        public async Task<List<OpenTelemetryExcludeDto>> ListAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryExclude", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryExclude.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("OpenTelemetryExclude.List");
                var dtos = OpenTelemetryExcludeMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
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
                log.Error($"OpenTelemetryExclude.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns one OpenTelemetryExclude row by its numeric identifier. Returns null when the row " +
                     "does not exist or is soft-deleted.")]
        [ServiceOperation("OpenTelemetryExcludeGet", OperationKind.Read, Idempotent = true)]
        public async Task<OpenTelemetryExcludeDto?> GetByIdAsync(
            [Description("Numeric identifier of the row.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryExclude", "Get", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryExclude.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("OpenTelemetryExclude.Get");
                var row = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (row == null)
                {
                    return null;
                }

                op.Entity(row.Id);
                return OpenTelemetryExcludeMapper.ToDto(row);
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
                log.Error($"OpenTelemetryExclude.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new OpenTelemetryExclude row. Not idempotent -- calling twice creates two " +
                     "rows.")]
        [ServiceOperation("OpenTelemetryExcludeCreate", OperationKind.Write, Idempotent = false)]
        public async Task<ExcludePoco> InsertAsync(
            [Description("The row to create.")] OpenTelemetryExcludeDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryExclude", "Create", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryExclude.Create: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("OpenTelemetryExclude.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"OpenTelemetryExclude.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(OpenTelemetryExcludeMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"OpenTelemetryExclude.Create: created Id={saved.Id} user={userName}");
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"OpenTelemetryExclude.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Validates an Open Telemetry Exclude without saving it, running every check a create (Id " +
                     "0) or an update (any other Id) would run, and returns each failure. Nothing is stored or " +
                     "changed.")]
        [ServiceOperation("OpenTelemetryExcludeValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Open Telemetry Exclude to validate.")]
            OpenTelemetryExcludeDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryExclude", "Validate", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryExclude.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("OpenTelemetryExclude.Validate");

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
                log.Error($"OpenTelemetryExclude.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing OpenTelemetryExclude row in place. Idempotent.")]
        [ServiceOperation("OpenTelemetryExcludeUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<ExcludePoco> UpdateAsync(
            [Description("The row to update. Id selects the row; identity/audit fields are server-owned and " +
                         "ignored.")]
            OpenTelemetryExcludeDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryExclude", "Update", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryExclude.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("OpenTelemetryExclude.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"OpenTelemetryExclude.Update: validation failed id={model.Id} user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                ExcludePoco saved;
                try
                {
                    saved = await repository.UpdateAsync(OpenTelemetryExcludeMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new NotFoundException("The row was not found.", ex);
                }
                catch (InvalidOperationException ex)
                {
                    throw new LockedException("The row is locked and cannot be updated.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"OpenTelemetryExclude.Update: updated Id={saved.Id} user={userName}");
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
            catch (LockedException)
            {
                op.Outcome("locked");
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
                log.Error($"OpenTelemetryExclude.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Deletes an OpenTelemetryExclude row by its Id. Soft-delete -- the excluded instrument " +
                     "immediately becomes eligible for export again (subject to any other matching exclusion).")]
        [ServiceOperation("OpenTelemetryExcludeDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the row to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryExclude", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryExclude.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("OpenTelemetryExclude.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new NotFoundException("The row was not found.", ex);
                }
                catch (InvalidOperationException ex)
                {
                    throw new LockedException("The row is locked and cannot be deleted.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"OpenTelemetryExclude.Delete: soft-deleted Id={id} user={userName}");
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
            catch (LockedException)
            {
                op.Outcome("locked");
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
                log.Error($"OpenTelemetryExclude.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Landlord)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied (landlord only) user={userName}");
            }

            throw new ForbiddenException(strings[OpenTelemetryExcludeResources.PermissionDenied], permissions);
        }
    }
}