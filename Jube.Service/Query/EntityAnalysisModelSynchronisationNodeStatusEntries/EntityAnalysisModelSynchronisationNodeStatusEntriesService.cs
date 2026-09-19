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
using Jube.Dto.Query.EntityAnalysisModelSynchronisationNodeStatusEntries;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelSynchronisationNodeStatusEntries;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.EntityAnalysisModelSynchronisationNodeStatusEntries
{
    public sealed class EntityAnalysisModelSynchronisationNodeStatusEntriesService
    {
        private static readonly int[] permissions = [5];

        private readonly ILog auditLog;
        private readonly global::Jube.Data.Query.GetEntityAnalysisModelSynchronisationNodeStatusEntriesQuery query;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelSynchronisationNodeStatusEntriesService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            query = new global::Jube.Data.Query.GetEntityAnalysisModelSynchronisationNodeStatusEntriesQuery(
                dbContext, userName);
        }

        public static Task<EntityAnalysisModelSynchronisationNodeStatusEntriesService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelSynchronisationNodeStatusEntriesService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings =
                stringLocalizerFactory.Create(typeof(EntityAnalysisModelSynchronisationNodeStatusEntriesResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "EntityAnalysisModelSynchronisationNodeStatusEntries.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelSynchronisationNodeStatusEntriesResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await Data.Repository.UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelSynchronisationNodeStatusEntries.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelSynchronisationNodeStatusEntriesResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelSynchronisationNodeStatusEntriesService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the synchronisation status of every model-synchronisation node of the caller's " +
                     "tenant that has sent a heartbeat within the last hour, reporting whether a scheduled " +
                     "synchronisation is pending and whether the instance is currently available. Returns null " +
                     "(no content) when the tenant has never scheduled a synchronisation. Read-only.")]
        [ServiceOperation("EntityAnalysisModelSynchronisationNodeStatusEntriesGet", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelSynchronisationNodeStatusEntriesDto>?> GetAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSynchronisationNodeStatusEntries", "Get",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelSynchronisationNodeStatusEntries.Get: entry user={userName}");
            }

            try
            {
                EnsurePermitted("EntityAnalysisModelSynchronisationNodeStatusEntries.Get");
                var result = await query.ExecuteAsync(token).ConfigureAwait(false);
                if (result is null)
                {
                    op.Rows(0);
                    return null;
                }

                var dtos = EntityAnalysisModelSynchronisationNodeStatusEntriesMapper.ToDto(result);
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelSynchronisationNodeStatusEntries.Get: {dtos.Count} rows user={userName}");
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
                log.Error(
                    $"EntityAnalysisModelSynchronisationNodeStatusEntries.Get: unexpected failure user={userName}",
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
                strings[EntityAnalysisModelSynchronisationNodeStatusEntriesResources.PermissionDenied],
                permissions);
        }
    }
}