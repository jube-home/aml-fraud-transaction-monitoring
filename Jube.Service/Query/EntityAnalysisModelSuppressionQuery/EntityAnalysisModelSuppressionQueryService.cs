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
using Jube.Dto.Query.EntityAnalysisModelSuppressionQuery;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelSuppressionQuery;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.EntityAnalysisModelSuppressionQuery
{
    public sealed class EntityAnalysisModelSuppressionQueryService
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

        private EntityAnalysisModelSuppressionQueryService(DbContext dbContext, string userName, int tenantRegistryId,
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

        public static Task<EntityAnalysisModelSuppressionQueryService> CreateAsync(DbContext dbContext,
            string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelSuppressionQueryService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelSuppressionQueryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelSuppressionQuery.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelSuppressionQueryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await Data.Repository.UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelSuppressionQuery.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelSuppressionQueryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelSuppressionQueryService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("For a given suppression key (the name of a request XPath with suppression enabled) and " +
                     "suppression key value, lists every Entity Analysis Model in the caller's tenant that " +
                     "supports suppression on that key, flagging whether an active, unexpired suppression " +
                     "currently exists for the value and when it expires. Read-only and tenant scoped.")]
        [ServiceOperation("EntityAnalysisModelSuppressionQueryGet", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelSuppressionQueryDto>> GetAsync(
            [Description("Name of the request XPath (suppression key) to look up suppressions for.")]
            string suppressionKey,
            [Description("Value of the suppression key to look up suppressions for.")]
            string suppressionKeyValue,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSuppressionQuery", "Get", userName,
                tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelSuppressionQuery.Get: entry user={userName} suppressionKey={suppressionKey}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelSuppressionQuery.Get");
                var query = new global::Jube.Data.Query.GetEntityAnalysisModelSuppressionQuery(dbContext, userName);
                var dtos = EntityAnalysisModelSuppressionQueryMapper.ToDto(
                    await query.ExecuteAsync(suppressionKey, suppressionKeyValue, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelSuppressionQuery.Get: {dtos.Count} rows user={userName}");
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
                log.Error($"EntityAnalysisModelSuppressionQuery.Get: unexpected failure user={userName}", ex);
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

            throw new ForbiddenException(strings[EntityAnalysisModelSuppressionQueryResources.PermissionDenied],
                permissions);
        }
    }
}