/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
using Jube.Engine.BackgroundTasks.TaskStarters.Metrics;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.PatroniAdministrator;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Patroni
{
    public sealed class PatroniAdministratorService
    {
        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private PatroniAdministratorService(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, string userName,
            int tenantRegistryId, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.dynamicEnvironment = dynamicEnvironment;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;
        public static bool RequiresLandlord => true;

        public static Task<PatroniAdministratorService> CreateAsync(DbContext dbContext, string? userName,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, dynamicEnvironment, log, stringLocalizerFactory,
                serviceChangeBus, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<PatroniAdministratorService> CreateAsync(DbContext dbContext, string? userName,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(PatroniAdministratorResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("PatroniAdministrator.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[PatroniAdministratorResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PatroniAdministrator.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[PatroniAdministratorResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            if (!permissionValidation.Landlord)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PatroniAdministrator.Create: permission denied (landlord only) user={userName}");
                }

                throw new ForbiddenException(strings[PatroniAdministratorResources.PermissionDenied], permissions);
            }

            return new PatroniAdministratorService(dynamicEnvironment, userName, resolvedTenantRegistryId.Value,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Reads every discoverable Patroni member's status RIGHT NOW over its own REST API -- " +
                     "role, state, timeline, lag, pending restart -- not the background sampler's last-" +
                     "stored row (see PatroniMemberStatusList), which can be up to a minute stale. " +
                     "Read-only.")]
        [ServiceOperation("PatroniClusterStatusNow", OperationKind.Read, Idempotent = true)]
        public async Task<List<global::Jube.Data.Poco.PatroniMemberStatus>> GetClusterStatusNowAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PatroniAdministrator", "ClusterStatusNow", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            var endpoints = await new EtcdPatroniDiscovery().DiscoverPatroniEndpointsAsync(dynamicEnvironment, token)
                .ConfigureAwait(false);

            if (endpoints.Count == 0)
            {
                op.Rows(0);

                return [];
            }

            using var client = new PatroniClient(dynamicEnvironment.AppSettings("EtcdClientUsername"),
                dynamicEnvironment.AppSettings("EtcdClientPassword"));

            var statuses = await client.GetClusterStatusAsync(endpoints, token).ConfigureAwait(false);
            op.Rows(statuses.Count);

            return statuses.ToList();
        }

        [Description("Reads a Patroni member's own resolved dynamic configuration (DCS-stored config " +
                     "merged with whatever is local to that node) as plain JSON text. Read-only, no " +
                     "mutation of any kind.")]
        [ServiceOperation("PatroniConfigGet", OperationKind.Read, Idempotent = true)]
        public async Task<string?> GetConfigAsync(
            [Description("The Patroni member endpoint to read, e.g. \"patroni1:8008\" -- see " +
                         "PatroniClusterStatusNow or PatroniMemberStatusList for live endpoint names.")]
            string endpoint,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PatroniAdministrator", "ConfigGet", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            using var client = new PatroniClient(dynamicEnvironment.AppSettings("EtcdClientUsername"),
                dynamicEnvironment.AppSettings("EtcdClientPassword"));

            var config = await client.GetConfigAsync(endpoint, token).ConfigureAwait(false);
            op.Rows(config != null ? 1 : 0);

            if (config == null && log.IsWarnEnabled)
            {
                log.Warn($"PatroniAdministrator.ConfigGet: {endpoint} returned nothing. {client.LastError}");
            }

            return config;
        }
    }
}