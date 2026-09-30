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
using Jube.Dto.Query.EntityAnalysisModelOverrideQuery;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelOverrideQuery;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.EntityAnalysisModelOverrideQuery
{
    public sealed class EntityAnalysisModelOverrideQueryService
    {
        private static readonly int[] permissions = [2];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelOverrideQueryService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
        }

        public static Task<EntityAnalysisModelOverrideQueryService> CreateAsync(DbContext dbContext,
            string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelOverrideQueryService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelOverrideQueryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelOverrideQuery.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelOverrideQueryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await Data.Repository.UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelOverrideQuery.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelOverrideQueryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelOverrideQueryService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("For a given override key (the name of a request XPath with override enabled) and " +
                     "override key value, lists every Entity Analysis Model in the caller's tenant that " +
                     "supports override on that key, flagging whether an active, unexpired override " +
                     "currently exists for the value and when it expires. Read-only and tenant scoped.")]
        [ServiceOperation("EntityAnalysisModelOverrideQueryGet", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelOverrideQueryDto>> GetAsync(
            [Description("Name of the request XPath (override key) to look up overrides for.")]
            string overrideKey,
            [Description("Value of the override key to look up overrides for.")]
            string overrideKeyValue,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverrideQuery", "Get", userName,
                tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelOverrideQuery.Get: entry user={userName} overrideKey={overrideKey}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverrideQuery.Get");
                var query = new global::Jube.Data.Query.GetEntityAnalysisModelOverrideQuery(dbContext, userName);
                var dtos = EntityAnalysisModelOverrideQueryMapper.ToDto(
                    await query.ExecuteAsync(overrideKey, overrideKeyValue, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelOverrideQuery.Get: {dtos.Count} rows user={userName}");
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
                log.Error($"EntityAnalysisModelOverrideQuery.Get: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("For a given override key (the name of a request XPath with override enabled), lists " +
                     "every value currently held against that key across the caller's tenant, with the override " +
                     "kind, who created it and when it expires. Use this to monitor what is overridden on a key " +
                     "rather than testing one value at a time. Read-only and tenant scoped.")]
        [ServiceOperation("EntityAnalysisModelOverrideQueryGetKeys", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelOverrideKeyQueryDto>> GetKeysAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverrideQuery", "GetKeys", userName,
                tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelOverrideQuery.GetKeys: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverrideQuery.GetKeys");
                var query = new global::Jube.Data.Query.GetEntityAnalysisModelOverrideQuery(dbContext, userName);
                var dtos = EntityAnalysisModelOverrideQueryMapper.ToDto(
                    await query.ExecuteKeysAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelOverrideQuery.GetKeys: {dtos.Count} rows user={userName}");
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
                log.Error($"EntityAnalysisModelOverrideQuery.GetKeys: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [ServiceOperation("EntityAnalysisModelOverrideQueryGetValues", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelOverrideValueQueryDto>> GetValuesAsync(
            [Description("Name of the request XPath (override key) to list overridden values for.")]
            string overrideKey,
            [Description("Maximum number of values to return. Clamped to between 1 and 1000.")]
            int limit = 250,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelOverrideQuery", "GetValues", userName,
                tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelOverrideQuery.GetValues: entry user={userName} overrideKey={overrideKey}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelOverrideQuery.GetValues");
                var query = new global::Jube.Data.Query.GetEntityAnalysisModelOverrideQuery(dbContext, userName);
                var dtos = EntityAnalysisModelOverrideQueryMapper.ToDto(
                    await query.ExecuteValuesAsync(overrideKey, Math.Clamp(limit, 1, 1000), token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelOverrideQuery.GetValues: {dtos.Count} rows user={userName}");
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
                log.Error($"EntityAnalysisModelOverrideQuery.GetValues: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(strings[EntityAnalysisModelOverrideQueryResources.PermissionDenied],
                permissions);
        }
    }
}