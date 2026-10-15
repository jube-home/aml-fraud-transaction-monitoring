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
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.RedisSentinelAdministrator;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.RedisSentinel
{
    public sealed class RedisSentinelAdministratorService
    {
        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private RedisSentinelAdministratorService(DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            string userName, int tenantRegistryId, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus)
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

        public static Task<RedisSentinelAdministratorService> CreateAsync(DbContext dbContext, string? userName,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, dynamicEnvironment, log, stringLocalizerFactory,
                serviceChangeBus, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<RedisSentinelAdministratorService> CreateAsync(DbContext dbContext,
            string? userName, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(RedisSentinelAdministratorResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("RedisSentinelAdministrator.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[RedisSentinelAdministratorResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"RedisSentinelAdministrator.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[RedisSentinelAdministratorResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            if (!permissionValidation.Landlord)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"RedisSentinelAdministrator.Create: permission denied (landlord only) user={userName}");
                }

                throw new ForbiddenException(strings[RedisSentinelAdministratorResources.PermissionDenied],
                    permissions);
            }

            return new RedisSentinelAdministratorService(dynamicEnvironment, userName,
                resolvedTenantRegistryId.Value, log, auditLog, serviceChangeBus);
        }

        [Description("Reads every master/slave/sentinel Sentinel currently reports RIGHT NOW (SENTINEL " +
                     "MASTERS/SLAVES/SENTINELS), not the background sampler's last-stored row (see " +
                     "RedisSentinelStatusList), which can be up to a minute stale. Read-only. Empty when " +
                     "Sentinel is not configured (RedisConnectionString carries no ServiceName).")]
        [ServiceOperation("RedisSentinelClusterStatusNow", OperationKind.Read, Idempotent = true)]
        public async Task<IReadOnlyList<global::Jube.Data.Poco.RedisSentinelStatus>> GetClusterStatusNowAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RedisSentinelAdministrator", "ClusterStatusNow", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            using var client = BuildClient();
            var statuses = await client.GetClusterStatusNowAsync(token).ConfigureAwait(false);
            op.Rows(statuses.Count);

            if (statuses.Count == 0 && log.IsWarnEnabled)
            {
                log.Warn($"RedisSentinelAdministrator.ClusterStatusNow: nothing returned. {client.LastError}");
            }

            return statuses;
        }

        [Description("Reads whether enough Sentinels currently agree a failover of the given master " +
                     "could actually succeed right now (SENTINEL CKQUORUM) -- the real pre-flight check " +
                     "before anyone reaches for RedisSentinelFailover. Returns Sentinel's own reply text " +
                     "verbatim, e.g. \"OK 3 usable Sentinels\" or a NOQUORUM message explaining the gap. " +
                     "Read-only.")]
        [ServiceOperation("RedisSentinelCheckQuorum", OperationKind.Read, Idempotent = true)]
        public async Task<string?> CheckQuorumAsync(
            [Description("The Sentinel-monitored master's name, e.g. \"mymaster\" -- see " +
                         "RedisSentinelClusterStatusNow for the names Sentinel currently knows.")]
            string masterName,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RedisSentinelAdministrator", "CheckQuorum", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            using var client = BuildClient();
            var reply = await client.CheckQuorumAsync(masterName, token).ConfigureAwait(false);
            op.Rows(reply is null ? 0 : 1);

            return reply;
        }

        [Description("Reads Sentinel's own current, authoritative address for the given master " +
                     "(SENTINEL GET-MASTER-ADDR-BY-NAME), as \"host:port\" -- null if Sentinel does not " +
                     "know this master name. Read-only.")]
        [ServiceOperation("RedisSentinelGetMasterAddress", OperationKind.Read, Idempotent = true)]
        public async Task<string?> GetMasterAddressAsync(
            [Description("The Sentinel-monitored master's name, e.g. \"mymaster\".")]
            string masterName,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RedisSentinelAdministrator", "GetMasterAddress", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            using var client = BuildClient();
            var address = await client.GetMasterAddressAsync(masterName, token).ConfigureAwait(false);
            op.Rows(address is null ? 0 : 1);

            return address;
        }

        [Description("Forces a manual failover of the given master right now (SENTINEL FAILOVER) -- the " +
                     "same action Sentinel's own design exists to let an operator trigger under a real " +
                     "incident. Check RedisSentinelCheckQuorum first: a failover attempted without quorum " +
                     "will not succeed. Sentinel answers once the failover has been started, not once it " +
                     "has finished -- follow up with RedisSentinelClusterStatusNow to confirm the new " +
                     "master. Mutating.")]
        [ServiceOperation("RedisSentinelFailover", OperationKind.Write)]
        public async Task<bool> FailoverAsync(
            [Description("The Sentinel-monitored master's name to fail over, e.g. \"mymaster\".")]
            string masterName,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RedisSentinelAdministrator", "Failover", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            using var client = BuildClient();
            var started = await client.FailoverAsync(masterName, token).ConfigureAwait(false);
            op.Rows(started ? 1 : 0);

            if (!started && log.IsWarnEnabled)
            {
                log.Warn($"RedisSentinelAdministrator.Failover: {masterName} did not start. {client.LastError}");
            }

            return started;
        }

        [Description("Forgets Sentinel's current state of every master matching the given glob pattern " +
                     "(SENTINEL RESET), forcing Sentinel to rediscover it from scratch. Self-healing and " +
                     "non-destructive to data -- Sentinel simply re-learns what it already knew -- but " +
                     "still a real, mutating call against a live Sentinel. Returns how many masters were " +
                     "reset.")]
        [ServiceOperation("RedisSentinelReset", OperationKind.Write)]
        public async Task<long> ResetAsync(
            [Description("Glob pattern matched against monitored master names, e.g. \"*\" for every " +
                         "master or \"mymaster\" for one specific master.")]
            string pattern,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RedisSentinelAdministrator", "Reset", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            using var client = BuildClient();
            var resetCount = await client.ResetAsync(pattern, token).ConfigureAwait(false);
            op.Rows((int)resetCount);

            return resetCount;
        }

        private RedisSentinelClient BuildClient()
        {
            return new RedisSentinelClient(dynamicEnvironment.AppSettings("RedisConnectionString"));
        }
    }
}