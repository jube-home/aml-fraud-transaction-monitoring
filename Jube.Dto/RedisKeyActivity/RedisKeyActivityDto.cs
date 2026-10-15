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

namespace Jube.Dto.RedisKeyActivity
{
    public sealed record RedisKeyActivityDto(
        [property: Description("The key name, as a small SCAN sample saw it -- not the whole keyspace, see " +
                               "RedisKeyActivityService's own cap.")]
        string Key,
        [property: Description("The key's Redis type (string, list, set, zset, hash, stream).")]
        string Type,
        [property: Description("Seconds since this key was last accessed (OBJECT IDLETIME) -- a large value " +
                               "on a key you expected to be hot is the signature of a cache-key design " +
                               "problem (wrong key shape, a reader bypassing the cache, ...). Null when the " +
                               "server could not answer (key expired between SCAN and this read).")]
        long? IdleTimeSeconds,
        [property: Description("Approximate logarithmic access frequency counter (OBJECT FREQ) -- only " +
                               "meaningful when maxmemory-policy is one of the LFU policies (allkeys-lfu/" +
                               "volatile-lfu); null under any other policy, since Redis itself refuses the " +
                               "command rather than returning a meaningless number.")]
        long? Frequency,
        [property: Description("Seconds until this key expires, or null if it has no TTL set.")]
        long? TimeToLiveSeconds);
}