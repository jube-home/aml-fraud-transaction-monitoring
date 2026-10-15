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
    public sealed record EtcdKeyspaceHash(
        [property: Description("The member endpoint this hash was read from, e.g. \"etcd1:2379\".")]
        string Endpoint,
        [property: Description("The keyspace hash at CompactRevision -- compare this across members at " +
                               "the same CompactRevision; a mismatch means they have diverged.")]
        long? Hash,
        [property: Description("The revision this hash was computed at. Only hashes computed at the " +
                               "same revision are comparable across members.")]
        long? CompactRevision);
}