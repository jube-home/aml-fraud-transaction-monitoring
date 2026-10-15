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

using Jube.Service.Agent.Models;
using Jube.Service.Etcd;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEtcdAdministrator(List<ServiceToolDescriptor> tools)
        {
            tools.Add(
                new ServiceToolDescriptor(
                    "EtcdClusterStatusNow", OperationKind.Read, true, false,
                    "Reads etcd's current member status, health, alarms and version RIGHT NOW over its own " +
                    "REST API for every discoverable endpoint -- not the background sampler's last-stored " +
                    "row (see EtcdMemberStatusList), which can be up to a minute stale.",
                    EtcdAdministratorService.RequiredPermissions, EtcdAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "EtcdClearAlarm", OperationKind.Write, false, true,
                    "Clears every currently-raised etcd alarm on the given endpoint -- acknowledges the " +
                    "flag, does not fix whatever raised it (e.g. free the disk first for NOSPACE). " +
                    "Mutating.",
                    EtcdAdministratorService.RequiredPermissions, EtcdAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "EtcdDefragment", OperationKind.Write, false, true,
                    "Defragments one etcd member's own backend database file to reclaim freed space. " +
                    "Briefly blocks that member -- call it on one member at a time, never the whole " +
                    "cluster at once. Mutating.",
                    EtcdAdministratorService.RequiredPermissions, EtcdAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "EtcdMemberList", OperationKind.Read, true, false,
                    "Reads etcd's own authoritative membership list from the given endpoint -- member " +
                    "id, name, peer/client URLs, and voting vs learner status. Any one reachable " +
                    "member answers for the whole cluster, since membership is Raft-replicated state.",
                    EtcdAdministratorService.RequiredPermissions, EtcdAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "EtcdMaintenanceStatus", OperationKind.Read, true, false,
                    "Reads one etcd member's raft term/index, current believed leader, and backend " +
                    "database size both total and in-use -- the figure to check before running " +
                    "EtcdDefragment, rather than running it blind.",
                    EtcdAdministratorService.RequiredPermissions, EtcdAdministratorService.RequiresLandlord));

            tools.Add(
                new ServiceToolDescriptor(
                    "EtcdHashKv", OperationKind.Read, true, false,
                    "Reads one etcd member's own keyspace consistency hash at its current revision, " +
                    "for comparing across members to catch a diverged member -- two members at the " +
                    "same revision reporting different hashes have disagreed about the keyspace.",
                    EtcdAdministratorService.RequiredPermissions, EtcdAdministratorService.RequiresLandlord));
        }
    }
}