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
    public sealed record EtcdMember(
        [property: Description("etcd's own uint64 member id, as a decimal string (etcd's gRPC-gateway " +
                               "serialises it that way to avoid JSON number precision loss).")]
        string Id,
        [property: Description("The member's configured name.")]
        string? Name,
        [property: Description("URLs this member advertises to its peers for cluster (Raft) traffic.")]
        string[] PeerUrls,
        [property: Description("URLs clients should use to talk to this member's own client API.")]
        string[] ClientUrls,
        [property: Description("Whether this member is a learner (a non-voting member still catching " +
                               "up, added ahead of being promoted to a full voting member) rather than " +
                               "a full voting member.")]
        bool IsLearner);
}