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
using Jube.Service.Etcd.Models;
using Jube.Service.Exceptions.EtcdAdministrator;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Etcd
{
    public sealed class EtcdAdministratorService
    {
        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EtcdAdministratorService(DynamicEnvironment.DynamicEnvironment dynamicEnvironment, string userName,
            int tenantRegistryId, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.dynamicEnvironment = dynamicEnvironment;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;
        public static bool RequiresLandlord => true;

        public static Task<EtcdAdministratorService> CreateAsync(DbContext dbContext, string? userName,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, dynamicEnvironment, log, stringLocalizerFactory,
                serviceChangeBus, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EtcdAdministratorService> CreateAsync(DbContext dbContext, string? userName,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog log,
            IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EtcdAdministratorResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EtcdAdministrator.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EtcdAdministratorResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"EtcdAdministrator.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EtcdAdministratorResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            if (!permissionValidation.Landlord)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"EtcdAdministrator.Create: permission denied (landlord only) user={userName}");
                }

                throw new ForbiddenException(strings[EtcdAdministratorResources.PermissionDenied], permissions);
            }

            return new EtcdAdministratorService(dynamicEnvironment, userName, resolvedTenantRegistryId.Value, log,
                auditLog, serviceChangeBus, strings);
        }

        [Description("Reads etcd's current member status, health, alarms and version RIGHT NOW over its " +
                     "own REST API, for every endpoint EtcdEndpoints/discovery resolves -- not the " +
                     "background sampler's last-stored row (see EtcdMemberStatusList), which can be up to " +
                     "a minute stale. Read-only. Returns one status per reachable endpoint.")]
        [ServiceOperation("EtcdClusterStatusNow", OperationKind.Read, Idempotent = true)]
        public async Task<List<global::Jube.Data.Poco.EtcdMemberStatus>> GetClusterStatusNowAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EtcdAdministrator", "ClusterStatusNow", userName, tenantRegistryId,
                auditLog, log,
                serviceChangeBus);

            var endpoints = await new EtcdPatroniDiscovery().DiscoverEtcdEndpointsAsync(dynamicEnvironment, token)
                .ConfigureAwait(false);

            if (endpoints.Count == 0)
            {
                op.Rows(0);

                return [];
            }

            using var client = BuildClient();
            var results = new List<global::Jube.Data.Poco.EtcdMemberStatus>();

            foreach (var endpoint in endpoints)
            {
                var status = await client.GetClusterStatusAsync(endpoint, token).ConfigureAwait(false);

                if (status != null)
                {
                    results.Add(status);
                }
            }

            op.Rows(results.Count);

            return results;
        }

        [Description("Clears every currently-raised etcd alarm (NOSPACE/CORRUPT) on the given endpoint. " +
                     "An alarm is etcd's own stop sign -- e.g. NOSPACE makes it refuse writes -- and " +
                     "clearing it does not fix whatever raised it (free the disk space first); this only " +
                     "acknowledges and clears the flag once the underlying cause is actually resolved. " +
                     "Mutating; not idempotent in effect (clearing an already-clear alarm is a no-op but " +
                     "still a real call).")]
        [ServiceOperation("EtcdClearAlarm", OperationKind.Write)]
        public async Task<bool> ClearAlarmAsync(
            [Description("The etcd member endpoint to clear alarms on, e.g. \"etcd1:2379\" -- see " +
                         "EtcdClusterStatusNow or EtcdMemberStatusList for live endpoint names.")]
            string endpoint,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EtcdAdministrator", "ClearAlarm", userName, tenantRegistryId, auditLog,
                log,
                serviceChangeBus);

            using var client = BuildClient();
            var cleared = await client.ClearAlarmAsync(endpoint, token).ConfigureAwait(false);
            op.Rows(cleared ? 1 : 0);

            if (!cleared && log.IsWarnEnabled)
            {
                log.Warn($"EtcdAdministrator.ClearAlarm: {endpoint} did not clear. {client.LastError}");
            }

            return cleared;
        }

        [Description("Defragments the given etcd member's own backend database file, reclaiming space " +
                     "freed by compaction that etcd does not return to the OS on its own. Runs on one " +
                     "member only (call it again per member for a whole cluster) and briefly blocks that " +
                     "member while it runs -- do this one member at a time, never all at once, to keep " +
                     "quorum available throughout. Mutating.")]
        [ServiceOperation("EtcdDefragment", OperationKind.Write)]
        public async Task<bool> DefragmentAsync(
            [Description("The etcd member endpoint to defragment, e.g. \"etcd1:2379\".")]
            string endpoint,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EtcdAdministrator", "Defragment", userName, tenantRegistryId, auditLog,
                log,
                serviceChangeBus);

            using var client = BuildClient();
            var defragged = await client.DefragAsync(endpoint, token).ConfigureAwait(false);
            op.Rows(defragged ? 1 : 0);

            if (!defragged && log.IsWarnEnabled)
            {
                log.Warn($"EtcdAdministrator.Defragment: {endpoint} did not complete. {client.LastError}");
            }

            return defragged;
        }

        [Description("Reads etcd's own authoritative membership list (/v3/cluster/member/list) from " +
                     "the given endpoint -- member id, name, peer/client URLs, and whether each is a " +
                     "voting member or still a non-voting learner. Distinct from EtcdClusterStatusNow, " +
                     "which reports health/alarms/version per already-known endpoint but never asks " +
                     "etcd itself who its members actually are; any one reachable member can answer " +
                     "for the whole cluster, since membership is itself Raft-replicated state. " +
                     "Read-only.")]
        [ServiceOperation("EtcdMemberList", OperationKind.Read, Idempotent = true)]
        public async Task<IReadOnlyList<EtcdMember>> MemberListAsync(
            [Description("Any one reachable etcd member endpoint, e.g. \"etcd1:2379\".")]
            string endpoint,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EtcdAdministrator", "MemberList", userName, tenantRegistryId, auditLog,
                log,
                serviceChangeBus);

            using var client = BuildClient();
            var members = await client.MemberListAsync(endpoint, token).ConfigureAwait(false);
            op.Rows(members.Count);

            if (members.Count == 0 && log.IsWarnEnabled)
            {
                log.Warn($"EtcdAdministrator.MemberList: {endpoint} returned nothing. {client.LastError}");
            }

            return members;
        }

        [Description("Reads one etcd member's own /v3/maintenance/status -- raft term/index, the " +
                     "member it currently believes is leader, and backend database size both total " +
                     "and actually in use. The gap between total and in-use size is exactly what " +
                     "EtcdDefragment reclaims, so check this first rather than defragmenting blind. " +
                     "Read-only.")]
        [ServiceOperation("EtcdMaintenanceStatus", OperationKind.Read, Idempotent = true)]
        public async Task<EtcdMaintenanceStatus?> MaintenanceStatusAsync(
            [Description("The etcd member endpoint to read maintenance status from, e.g. \"etcd1:2379\".")]
            string endpoint,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EtcdAdministrator", "MaintenanceStatus", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            using var client = BuildClient();
            var status = await client.MaintenanceStatusAsync(endpoint, token).ConfigureAwait(false);
            op.Rows(status is null ? 0 : 1);

            if (status is null && log.IsWarnEnabled)
            {
                log.Warn($"EtcdAdministrator.MaintenanceStatus: {endpoint} did not answer. {client.LastError}");
            }

            return status;
        }

        [Description("Reads one etcd member's own keyspace consistency hash (/v3/maintenance/hashkv) " +
                     "at its current revision. Call this against every member and compare -- two " +
                     "members reporting the same CompactRevision but a different Hash have diverged, " +
                     "which neither a health check nor an alarm will ever show; that is the signal to " +
                     "reach for etcdctl directly. Read-only.")]
        [ServiceOperation("EtcdHashKv", OperationKind.Read, Idempotent = true)]
        public async Task<EtcdKeyspaceHash?> HashKvAsync(
            [Description("The etcd member endpoint to hash, e.g. \"etcd1:2379\".")]
            string endpoint,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EtcdAdministrator", "HashKv", userName, tenantRegistryId, auditLog,
                log,
                serviceChangeBus);

            using var client = BuildClient();
            var hash = await client.HashKvAsync(endpoint, token).ConfigureAwait(false);
            op.Rows(hash is null ? 0 : 1);

            if (hash is null && log.IsWarnEnabled)
            {
                log.Warn($"EtcdAdministrator.HashKv: {endpoint} did not answer. {client.LastError}");
            }

            return hash;
        }

        private EtcdClient BuildClient()
        {
            return new EtcdClient(dynamicEnvironment.AppSettings("EtcdClientUsername"),
                dynamicEnvironment.AppSettings("EtcdClientPassword"));
        }
    }
}