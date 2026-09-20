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
using Jube.Dto.Repository.UserRegistry;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Concurrency;
using Jube.Service.Exceptions.Repository.UserRegistry;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.UserRegistry;
using log4net;
using Microsoft.Extensions.Localization;
using RulePoco = Jube.Data.Poco.UserRegistry;

namespace Jube.Service.Repository.UserRegistry
{
    public sealed class UserRegistryService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [35];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly UserRegistryRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly UserRegistryDtoValidator validator;

        private UserRegistryService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.dynamicEnvironment = dynamicEnvironment;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new UserRegistryRepository(dbContext, userName);
            var roleRegistryRepository = new RoleRegistryRepository(dbContext, userName);
            validator = new UserRegistryDtoValidator(repository, roleRegistryRepository, strings);
        }

        public static Task<UserRegistryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<UserRegistryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(UserRegistryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("UserRegistry.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[UserRegistryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"UserRegistry.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[UserRegistryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new UserRegistryService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings, dynamicEnvironment);
        }

        public static UserRegistryDto ToDto(RulePoco userRegistry)
        {
            return UserRegistryMapper.ToDto(userRegistry);
        }

        [Description("Lists every user account visible to the calling user's tenant. Unbounded -- intended for " +
                     "the administrative page, not for agent tooling (use the bounded list operation instead).")]
        public async Task<List<UserRegistryDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistry", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistry.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("UserRegistry.List");
                var dtos = UserRegistryMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"UserRegistry.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"UserRegistry.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserRegistry.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the user accounts assigned to a given Role Registry, scoped to the calling user's " +
                     "tenant.")]
        [ServiceOperation("UserRegistryListByRoleRegistryGuid", OperationKind.Read, Idempotent = true)]
        public async Task<List<UserRegistryDto>> GetByRoleRegistryGuidAsync(
            [Description("Guid identifier of the parent Role Registry.")]
            Guid roleRegistryGuid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistry", "ListByRoleRegistryGuid", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"UserRegistry.ListByRoleRegistryGuid: entry roleRegistryGuid={roleRegistryGuid} user={userName}");
            }

            try
            {
                EnsurePermitted("UserRegistry.ListByRoleRegistryGuid");
                var dtos = UserRegistryMapper.ToDto(await repository
                    .GetByRoleRegistryGuidAsync(roleRegistryGuid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"UserRegistry.ListByRoleRegistryGuid: {dtos.Count} rows roleRegistryGuid={roleRegistryGuid} user={userName}");
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
                        $"UserRegistry.ListByRoleRegistryGuid: cancelled roleRegistryGuid={roleRegistryGuid} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"UserRegistry.ListByRoleRegistryGuid: unexpected failure roleRegistryGuid={roleRegistryGuid} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one user account by its numeric identifier, scoped to the calling user's tenant. " +
                     "Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("UserRegistryGet", OperationKind.Read, Idempotent = true)]
        public async Task<UserRegistryDto?> GetByIdAsync(
            [Description("Numeric identifier of the user account.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistry", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistry.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("UserRegistry.Get");
                var userRegistry = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (userRegistry == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug($"UserRegistry.Get: id={id} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(userRegistry.Id);
                return UserRegistryMapper.ToDto(userRegistry);
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
                    log.Debug($"UserRegistry.Get: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserRegistry.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists user accounts for the caller's tenant, ordered by id, capped at 'take' rows (max " +
                     "200). If 'more' is true, call again with 'afterId' set to the last returned Id to continue.")]
        [ServiceOperation("UserRegistryList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<UserRegistryDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistry", "ListPaged", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistry.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("UserRegistry.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<UserRegistryDto>(UserRegistryMapper.ToDto(page));
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
                    log.Debug($"UserRegistry.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserRegistry.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new user account under a Role Registry in the caller's tenant. Not idempotent " +
                     "-- calling twice creates two rows. The account has no usable password until a password " +
                     "reset is performed.")]
        [ServiceOperation("UserRegistryCreate", OperationKind.Write, Idempotent = false)]
        public async Task<RulePoco> InsertAsync(
            [Description("The user account to create.")]
            UserRegistryDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistry", "Create", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistry.Create: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("UserRegistry.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"UserRegistry.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var newPoco = UserRegistryMapper.ToPoco(model);

                newPoco.PasswordLocked = 0;
                newPoco.InheritedId = null;
                var saved = await UniqueViolation.GuardAsync(() => repository.InsertAsync(newPoco, token),
                        r => new DtoValidationException(r), strings[UserRegistryResources.NameAlreadyExists])
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"UserRegistry.Create: created Id={saved.Id} name={saved.Name} user={userName}");
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
                    log.Debug($"UserRegistry.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserRegistry.Create: unexpected failure user={userName} name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Updates an existing user account in the caller's tenant, identified by its Id. Idempotent " +
                     "-- repeating the same update has no further effect beyond incrementing Version. The " +
                     "existing password and its expiry are always preserved; use the password reset operation to " +
                     "change them.")]
        [ServiceOperation("UserRegistryUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<RulePoco> UpdateAsync(
            [Description("The user account to update. Id selects the row; identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            UserRegistryDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistry", "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistry.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("UserRegistry.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"UserRegistry.Update: validation failed id={model.Id} user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                RulePoco saved;
                try
                {
                    var poco = UserRegistryMapper.ToPoco(model);
                    if (model.PasswordLocked is null)
                    {
                        var current = await repository.GetByIdAsync(model.Id, token).ConfigureAwait(false);
                        if (current is null)
                        {
                            throw new KeyNotFoundException();
                        }

                        poco.PasswordLocked = current.PasswordLocked;
                    }

                    saved = await UniqueViolation.GuardAsync(() => repository.UpdateAsync(poco, token),
                            r => new DtoValidationException(r), strings[UserRegistryResources.NameAlreadyExists])
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"UserRegistry.Update: id={model.Id} not found, deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The User was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"UserRegistry.Update: Id={saved.Id} version->{saved.Version} user={userName}");
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
                    log.Debug($"UserRegistry.Update: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserRegistry.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Issues a new temporary password for a user account in the caller's tenant, clears any " +
                     "password lockout, and returns the temporary password in clear text exactly once. Not " +
                     "registered as an agent tool: the response carries a live credential that must never be " +
                     "surfaced to a model.")]
        public async Task<UserRegistryPasswordResponseDto> UpdatePasswordAsync(UserRegistryPasswordResetDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistry", "UpdatePassword", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistry.UpdatePassword: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("UserRegistry.UpdatePassword");

                var userRegistry = await repository.GetByIdAsync(model.Id, token).ConfigureAwait(false);
                if (userRegistry == null)
                {
                    throw new KeyNotFoundException();
                }

                var generatedPassword = Data.Security.HashPassword.CreateSecurePassword(16);
                var response = new UserRegistryPasswordResponseDto
                {
                    Password = generatedPassword,
                    PasswordExpiryDate = DateTime.UtcNow
                };

                var password = generatedPassword;
                if (model.WirePasswordHash)
                {
                    password = Data.Security.HashPassword.Sha256(password + userRegistry.Name);
                }

                var hashedPassword = Data.Security.HashPassword.Argon2(password,
                    dynamicEnvironment.AppSettings("PasswordHashingKey"));

                await repository.SetPasswordAsync(model.Id, hashedPassword, response.PasswordExpiryDate?.UtcDateTime,
                    model.WirePasswordHash, token).ConfigureAwait(false);

                op.Entity(model.Id);
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"UserRegistry.UpdatePassword: reset Id={model.Id} user={userName}");
                }

                return response;
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
                    log.Debug($"UserRegistry.UpdatePassword: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserRegistry.UpdatePassword: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a user account in the caller's tenant by its Id. Reversible at the data " +
                     "level, but treat as destructive -- the account immediately loses the ability to " +
                     "authenticate.")]
        [ServiceOperation("UserRegistryDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the user account to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistry", "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistry.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("UserRegistry.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"UserRegistry.Delete: id={id} not found, already deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The User was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"UserRegistry.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"UserRegistry.Delete: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserRegistry.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Ends every browser and bearer-token (JWT) login session of a user in the caller's tenant: " +
                     "each token issued before now is refused on its next request and the user must log in " +
                     "again. API keys are not affected (they are revoked separately). Not registered as an " +
                     "agent tool.")]
        public async Task RevokeTokensAsync(int id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistry", "RevokeTokens", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistry.RevokeTokens: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("UserRegistry.RevokeTokens");

                try
                {
                    var revokedUserName = await repository
                        .RevokeTokensAsync(id, TimeProvider.System.GetUtcNow(), token)
                        .ConfigureAwait(false);

                    await UserLogout.UserLogoutRecorder.RecordAsync(dbContext, log,
                        new UserLogout.UserLogoutEntry(revokedUserName,
                            UserLogout.UserLogoutReason.RevokedByAdministrator,
                            UserLogout.UserLogoutOutcome.Revoked, CutByUser: userName)).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"UserRegistry.RevokeTokens: id={id} not found or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The User was not found.", ex);
                }

                op.Entity(id);

                if (log.IsInfoEnabled)
                {
                    log.Info($"UserRegistry.RevokeTokens: revoked the sessions of Id={id} user={userName}");
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserRegistry.RevokeTokens: unexpected failure id={id} user={userName}", ex);
                throw;
            }
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

            throw new ForbiddenException(strings[UserRegistryResources.PermissionDenied], permissions);
        }
    }
}