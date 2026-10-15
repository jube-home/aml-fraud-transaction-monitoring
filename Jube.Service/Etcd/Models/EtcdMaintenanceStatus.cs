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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Etcd.Models
{
    public sealed record EtcdMaintenanceStatus(
        [property: Description("The member endpoint this status was read from, e.g. \"etcd1:2379\".")]
        string Endpoint,
        [property: Description("The etcd server version string this member reports.")]
        string? Version,
        [property: Description("Total size on disk of this member's backend database file, in bytes.")]
        long? DbSizeBytes,
        [property: Description("How much of DbSizeBytes is actually in use -- the gap between this and " +
                               "DbSizeBytes is what EtcdDefragment reclaims; a small gap means " +
                               "defragmenting this member would not help.")]
        long? DbSizeInUseBytes,
        [property: Description("etcd's own member id (decimal string) of the member this one currently " +
                               "believes is the Raft leader.")]
        string? Leader,
        [property: Description("This member's current Raft index.")]
        long? RaftIndex,
        [property: Description("This member's current Raft term.")]
        long? RaftTerm,
        [property: Description("The Raft index this member has actually applied to its own state " +
                               "machine -- lagging behind RaftIndex means this member is catching up.")]
        long? RaftAppliedIndex,
        [property: Description("Whether this member is a learner (non-voting) rather than a full " +
                               "voting member.")]
        bool? IsLearner);
}