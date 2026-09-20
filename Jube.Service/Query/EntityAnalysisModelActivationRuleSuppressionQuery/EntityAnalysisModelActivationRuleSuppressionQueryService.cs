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
using Jube.Dto.Query.EntityAnalysisModelActivationRuleSuppressionQuery;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelActivationRuleSuppressionQuery;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.EntityAnalysisModelActivationRuleSuppressionQuery
{
    public sealed class EntityAnalysisModelActivationRuleSuppressionQueryService
    {
        private static readonly int[] permissions = [2];

        private readonly ILog auditLog;
        private readonly global::Jube.Data.Query.GetEntityAnalysisModelActivationRuleSuppressionQuery query;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelActivationRuleSuppressionQueryService(DbContext dbContext, string userName,
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
            query = new global::Jube.Data.Query.GetEntityAnalysisModelActivationRuleSuppressionQuery(dbContext,
                userName);
        }

        public static Task<EntityAnalysisModelActivationRuleSuppressionQueryService> CreateAsync(DbContext dbContext,
            string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelActivationRuleSuppressionQueryService> CreateAsync(
            DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings =
                stringLocalizerFactory.Create(typeof(EntityAnalysisModelActivationRuleSuppressionQueryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "EntityAnalysisModelActivationRuleSuppressionQuery.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelActivationRuleSuppressionQueryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelActivationRuleSuppressionQuery.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelActivationRuleSuppressionQueryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelActivationRuleSuppressionQueryService(dbContext, userName,
                resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("For an Entity Analysis Model, lists every Activation Rule that has Suppression enabled " +
                     "(where the model also has a Request XPath named after the suppression key with Suppression " +
                     "enabled), each flagged with whether a live suppression currently exists for the given " +
                     "suppression key and value. Read-only and scoped to the caller's tenant.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleSuppressionQueryGet", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelActivationRuleSuppressionQueryDto>> GetAsync(
            [Description("Guid of the Entity Analysis Model.")]
            Guid entityAnalysisModelGuid,
            [Description("Suppression key, being the name of a Request XPath with Suppression enabled.")]
            string? suppressionKey,
            [Description("Suppression key value to check for an active suppression.")]
            string? suppressionKeyValue,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRuleSuppressionQuery", "Get", userName,
                tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelActivationRuleSuppressionQuery.Get: entry user={userName} model={entityAnalysisModelGuid}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelActivationRuleSuppressionQuery.Get");
                token.ThrowIfCancellationRequested();
                var dtos = EntityAnalysisModelActivationRuleSuppressionQueryMapper.ToDto(await query
                    .ExecuteAsync(entityAnalysisModelGuid, suppressionKey ?? string.Empty,
                        suppressionKeyValue ?? string.Empty, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelActivationRuleSuppressionQuery.Get: {dtos.Count} rows user={userName}");
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
                log.Error($"EntityAnalysisModelActivationRuleSuppressionQuery.Get: unexpected failure user={userName}",
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
                strings[EntityAnalysisModelActivationRuleSuppressionQueryResources.PermissionDenied], permissions);
        }
    }
}