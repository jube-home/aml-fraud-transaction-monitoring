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
using Jube.Dto.OpenTelemetryLogCounter;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.OpenTelemetryLogCounter;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.OpenTelemetryLogCounter;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.OpenTelemetryLogCounter
{
    using LogCounterPoco = Data.Poco.OpenTelemetryLogCounter;

    public sealed class OpenTelemetryLogCounterService
    {
        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly OpenTelemetryLogCounterRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly OpenTelemetryLogCounterDtoValidator validator;

        private OpenTelemetryLogCounterService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new OpenTelemetryLogCounterRepository(dbContext, userName);
            validator = new OpenTelemetryLogCounterDtoValidator(strings);
        }

        public static Task<OpenTelemetryLogCounterService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<OpenTelemetryLogCounterService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(OpenTelemetryLogCounterResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("OpenTelemetryLogCounter.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[OpenTelemetryLogCounterResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"OpenTelemetryLogCounter.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[OpenTelemetryLogCounterResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new OpenTelemetryLogCounterService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every OpenTelemetryLogCounter rule, most recent first. Not scoped to any tenant.")]
        [ServiceOperation("OpenTelemetryLogCounterList", OperationKind.Read, Idempotent = true)]
        public async Task<List<OpenTelemetryLogCounterDto>> ListAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryLogCounter", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryLogCounter.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("OpenTelemetryLogCounter.List");
                var dtos = OpenTelemetryLogCounterMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
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
                log.Error($"OpenTelemetryLogCounter.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns one OpenTelemetryLogCounter rule by its numeric identifier. Returns null when the " +
                     "row does not exist or is soft-deleted.")]
        [ServiceOperation("OpenTelemetryLogCounterGet", OperationKind.Read, Idempotent = true)]
        public async Task<OpenTelemetryLogCounterDto?> GetByIdAsync(
            [Description("Numeric identifier of the row.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryLogCounter", "Get", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryLogCounter.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("OpenTelemetryLogCounter.Get");
                var row = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (row == null)
                {
                    return null;
                }

                op.Entity(row.Id);
                return OpenTelemetryLogCounterMapper.ToDto(row);
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
                log.Error($"OpenTelemetryLogCounter.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new OpenTelemetryLogCounter rule. Not idempotent -- calling twice creates " +
                     "two rows.")]
        [ServiceOperation("OpenTelemetryLogCounterCreate", OperationKind.Write, Idempotent = false)]
        public async Task<LogCounterPoco> InsertAsync(
            [Description("The rule to create.")] OpenTelemetryLogCounterDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryLogCounter", "Create", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryLogCounter.Create: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("OpenTelemetryLogCounter.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"OpenTelemetryLogCounter.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(OpenTelemetryLogCounterMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"OpenTelemetryLogCounter.Create: created Id={saved.Id} user={userName}");
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
                log.Error($"OpenTelemetryLogCounter.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Validates an Open Telemetry Log Counter without saving it, running every check a create " +
                     "(Id 0) or an update (any other Id) would run, and returns each failure. Nothing is " +
                     "stored or changed.")]
        [ServiceOperation("OpenTelemetryLogCounterValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Open Telemetry Log Counter to validate.")]
            OpenTelemetryLogCounterDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryLogCounter", "Validate", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryLogCounter.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("OpenTelemetryLogCounter.Validate");

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
                log.Error($"OpenTelemetryLogCounter.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing OpenTelemetryLogCounter rule in place. Idempotent.")]
        [ServiceOperation("OpenTelemetryLogCounterUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<LogCounterPoco> UpdateAsync(
            [Description("The rule to update. Id selects the row; identity/audit fields are server-owned and " +
                         "ignored.")]
            OpenTelemetryLogCounterDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryLogCounter", "Update", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryLogCounter.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("OpenTelemetryLogCounter.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"OpenTelemetryLogCounter.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                LogCounterPoco saved;
                try
                {
                    saved = await repository.UpdateAsync(OpenTelemetryLogCounterMapper.ToPoco(model), token)
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
                    log.Info($"OpenTelemetryLogCounter.Update: updated Id={saved.Id} user={userName}");
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
                log.Error($"OpenTelemetryLogCounter.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes an OpenTelemetryLogCounter rule by its Id. Soft-delete -- the rule immediately " +
                     "stops being checked against incoming log lines.")]
        [ServiceOperation("OpenTelemetryLogCounterDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the row to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("OpenTelemetryLogCounter", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"OpenTelemetryLogCounter.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("OpenTelemetryLogCounter.Delete");

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
                    log.Info($"OpenTelemetryLogCounter.Delete: soft-deleted Id={id} user={userName}");
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
                log.Error($"OpenTelemetryLogCounter.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[OpenTelemetryLogCounterResources.PermissionDenied], permissions);
        }
    }
}