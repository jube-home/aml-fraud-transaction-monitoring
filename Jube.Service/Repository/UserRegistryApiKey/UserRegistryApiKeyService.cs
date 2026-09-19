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
using Jube.ApiTokensCache;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.UserRegistryApiKey;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.UserRegistryApiKey;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.UserRegistryApiKey;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.UserRegistryApiKeyRepository;

namespace Jube.Service.Repository.UserRegistryApiKey
{
    public sealed class UserRegistryApiKeyService
    {
        private static readonly int[] permissions = [35];

        private readonly ILog auditLog;
        private readonly IUserRegistryApiKeyCacheInvalidator cacheInvalidator;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly UserRegistryRepository userRegistryRepository;
        private readonly string userName;
        private readonly UserRegistryApiKeyDtoValidator validator;

        private UserRegistryApiKeyService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IUserRegistryApiKeyCacheInvalidator cacheInvalidator)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.dynamicEnvironment = dynamicEnvironment;
            this.cacheInvalidator = cacheInvalidator;
            repository = new RuleRepository(dbContext, userName);
            userRegistryRepository = new UserRegistryRepository(dbContext, userName);
            validator = new UserRegistryApiKeyDtoValidator(userRegistryRepository, strings);
        }

        public static Task<UserRegistryApiKeyService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IUserRegistryApiKeyCacheInvalidator cacheInvalidator, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, cacheInvalidator, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<UserRegistryApiKeyService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IUserRegistryApiKeyCacheInvalidator cacheInvalidator, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(UserRegistryApiKeyResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("UserRegistryApiKey.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[UserRegistryApiKeyResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"UserRegistryApiKey.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[UserRegistryApiKeyResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new UserRegistryApiKeyService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment, cacheInvalidator);
        }

        [Description("Lists the API keys issued against a UserRegistry (service account), newest-inserted " +
                     "order as stored, scoped to the caller's tenant. ApiKeyDisplay carries only the first 8 " +
                     "characters of each key -- the full key is never returned by this operation.")]
        [ServiceOperation("UserRegistryApiKeyListByUserRegistryId", OperationKind.Read, Idempotent = true)]
        public async Task<List<UserRegistryApiKeyDto>> GetByUserRegistryIdAsync(
            [Description("Identifier of the UserRegistry (service account) to list API keys for.")]
            int userRegistryId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistryApiKey", "ListByUserRegistryId", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistryApiKey.ListByUserRegistryId: entry userRegistryId={userRegistryId} " +
                          $"user={userName}");
            }

            try
            {
                EnsurePermitted("UserRegistryApiKey.ListByUserRegistryId");
                var dtos = UserRegistryApiKeyMapper.ToDto(
                    await repository.GetByUserRegistryIdAsync(userRegistryId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"UserRegistryApiKey.ListByUserRegistryId: {dtos.Count} rows " +
                              $"userRegistryId={userRegistryId} user={userName}");
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserRegistryApiKey.ListByUserRegistryId: unexpected failure " +
                          $"userRegistryId={userRegistryId} user={userName}", ex);
                throw;
            }
        }

        [Description("Issues a new API key for a UserRegistry (service account). The key's HMAC-signed body " +
                     "and SHA-256 hash are computed server-side; only the hash and an 8-character display " +
                     "prefix are persisted. The one and only time the full plaintext key is available is in the " +
                     "response ApiKeyDisplay of this call -- it cannot be recovered afterwards, so the caller " +
                     "must capture it immediately. Deliberately not registered as an agent tool: an agent " +
                     "invoking this would permanently capture the plaintext key into its own transcript/logs, " +
                     "which is worse than the intended one-time-human-reveal design.")]
        public async Task<UserRegistryApiKeyDto> InsertAsync(UserRegistryApiKeyDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistryApiKey", "Insert", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistryApiKey.Insert: entry user={userName} " +
                          $"userRegistryId={model?.UserRegistryId}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("UserRegistryApiKey.Insert");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"UserRegistryApiKey.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var userRegistry = await userRegistryRepository.GetByIdAsync(model.UserRegistryId, token)
                    .ConfigureAwait(false);
                if (userRegistry is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"UserRegistryApiKey.Insert: userRegistryId={model.UserRegistryId} not found or " +
                                 $"not visible user={userName}");
                    }

                    throw new NotFoundException(strings[UserRegistryApiKeyResources.UserRegistryNotFound]);
                }

                var apiHmacKey = dynamicEnvironment.AppSettings("ApiHmacKey");
                var generated = ApiKeyHelper.Generate(userRegistry.Name, apiHmacKey);

                if (!ApiKeyHelper.TryParse(generated.ApiKey, apiHmacKey, out var reparsed) ||
                    reparsed?.ApiKeyHash != generated.ApiKeyHash)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"UserRegistryApiKey.Insert: generated key failed its own round-trip " +
                                 $"integrity check user={userName}");
                    }

                    throw new KeyIntegrityException(strings[UserRegistryApiKeyResources.KeyIntegrityFailure]);
                }

                var poco = UserRegistryApiKeyMapper.ToPoco(model);
                poco.ApiKey = generated.ApiKeyHash;
                poco.ApiKeyDisplay = generated.ApiKeyDisplay;

                var saved = await repository.InsertAsync(poco, token).ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Created();

                await PublishCreatedSafelyAsync(generated.ApiKeyHash, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info($"UserRegistryApiKey.Insert: created Id={saved.Id} " +
                             $"userRegistryId={model.UserRegistryId} user={userName}");
                }

                var dto = UserRegistryApiKeyMapper.ToDto(saved);
                dto.ApiKeyDisplay = generated.ApiKey;
                return dto;
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
                op.Outcome("not_found");
                throw;
            }
            catch (KeyIntegrityException)
            {
                op.Outcome("key_integrity_failure");
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
                log.Error($"UserRegistryApiKey.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Revokes an API key. A key that no longer exists at all is treated as already revoked and " +
                     "returns successfully as a no-op; a key that exists but belongs to another tenant is " +
                     "reported as not found rather than revoked, to avoid revoking another tenant's key.")]
        [ServiceOperation("UserRegistryApiKeyDelete", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the API key grant to revoke.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserRegistryApiKey", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserRegistryApiKey.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("UserRegistryApiKey.Delete");

                var existingAnyTenant = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (existingAnyTenant is null)
                {
                    op.Outcome("noop_already_gone");
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"UserRegistryApiKey.Delete: id={id} already absent, no-op user={userName}");
                    }

                    return;
                }

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException)
                {
                    op.Outcome("noop_already_gone");
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"UserRegistryApiKey.Delete: id={id} exists but not visible to tenant, " +
                                 $"treated as absent user={userName}");
                    }

                    return;
                }

                op.Entity(id);
                op.Deleted();

                await PublishRemovedSafelyAsync(existingAnyTenant.ApiKey, token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info($"UserRegistryApiKey.Delete: soft-deleted Id={id} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
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
                log.Error($"UserRegistryApiKey.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private async Task PublishCreatedSafelyAsync(string apiKeyHash, CancellationToken token)
        {
            try
            {
                await cacheInvalidator.PublishCreatedAsync(apiKeyHash, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("UserRegistryApiKey.Insert: cache invalidation publish failed", ex);
                }
            }
        }

        private async Task PublishRemovedSafelyAsync(string apiKeyHash, CancellationToken token)
        {
            try
            {
                await cacheInvalidator.PublishRemovedAsync(apiKeyHash, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("UserRegistryApiKey.Delete: cache invalidation publish failed", ex);
                }
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

            throw new ForbiddenException(strings[UserRegistryApiKeyResources.PermissionDenied], permissions);
        }
    }
}