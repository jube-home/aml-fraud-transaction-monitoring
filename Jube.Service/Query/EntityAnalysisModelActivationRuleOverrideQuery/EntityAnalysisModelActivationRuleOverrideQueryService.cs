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
using Jube.Dto.Query.EntityAnalysisModelActivationRuleOverrideQuery;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelActivationRuleOverrideQuery;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.EntityAnalysisModelActivationRuleOverrideQuery
{
    public sealed class EntityAnalysisModelActivationRuleOverrideQueryService
    {
        private static readonly int[] permissions = [2];

        private readonly ILog auditLog;
        private readonly global::Jube.Data.Query.GetEntityAnalysisModelActivationRuleOverrideQuery query;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelActivationRuleOverrideQueryService(DbContext dbContext, string userName,
            int tenantRegistryId,
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
            query = new global::Jube.Data.Query.GetEntityAnalysisModelActivationRuleOverrideQuery(dbContext,
                userName);
        }

        public static Task<EntityAnalysisModelActivationRuleOverrideQueryService> CreateAsync(DbContext dbContext,
            string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelActivationRuleOverrideQueryService> CreateAsync(
            DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings =
                stringLocalizerFactory.Create(typeof(EntityAnalysisModelActivationRuleOverrideQueryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "EntityAnalysisModelActivationRuleOverrideQuery.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelActivationRuleOverrideQueryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelActivationRuleOverrideQuery.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelActivationRuleOverrideQueryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelActivationRuleOverrideQueryService(dbContext, userName,
                resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("For an Entity Analysis Model, lists every Activation Rule that has Override enabled " +
                     "(where the model also has a Request XPath named after the override key with Override " +
                     "enabled), each flagged with whether a live override currently exists for the given " +
                     "override key and value. Read-only and scoped to the caller's tenant.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleOverrideQueryGet", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelActivationRuleOverrideQueryDto>> GetAsync(
            [Description("Guid of the Entity Analysis Model.")]
            Guid entityAnalysisModelGuid,
            [Description("Override key, being the name of a Request XPath with Override enabled.")]
            string? overrideKey,
            [Description("Override key value to check for an active override.")]
            string? overrideKeyValue,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleOverrideQuery", "Get", userName,
                tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelActivationRuleOverrideQuery.Get: entry user={userName} model={entityAnalysisModelGuid}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleOverrideQuery.Get");
                token.ThrowIfCancellationRequested();
                var dtos = EntityAnalysisModelActivationRuleOverrideQueryMapper.ToDto(await query
                    .ExecuteAsync(entityAnalysisModelGuid, overrideKey ?? string.Empty,
                        overrideKeyValue ?? string.Empty, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelActivationRuleOverrideQuery.Get: {dtos.Count} rows user={userName}");
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
                log.Error($"EntityAnalysisModelActivationRuleOverrideQuery.Get: unexpected failure user={userName}",
                    ex);
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

            throw new ForbiddenException(
                strings[EntityAnalysisModelActivationRuleOverrideQueryResources.PermissionDenied], permissions);
        }
    }
}