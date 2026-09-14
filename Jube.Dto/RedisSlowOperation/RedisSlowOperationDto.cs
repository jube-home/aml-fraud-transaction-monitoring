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

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.RedisSlowOperation
{
    public sealed record RedisSlowOperationDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("Redis' own monotonic SLOWLOG sequence id for this entry.")]
        long RedisSlowLogId,
        [property: Description("UTC timestamp the slow operation occurred, as reported by Redis.")]
        DateTime OccurredDate,
        [property: Description("How long the operation took to execute, in microseconds.")]
        long DurationMicroseconds,
        [property:
            Description(
                "The command and its arguments, space-joined -- kept in full for detail; see CommandName/KeyName to group or filter without parsing this.")]
        string Command,
        [property:
            Description(
                "The command verb (e.g. SET, HGETALL, EVAL), pulled out of Command -- group by this to see which command type is actually slow.")]
        string CommandName,
        [property:
            Description(
                "Best-effort key name, the argument after the command verb. Correct for most commands; not meaningful for multi-key or scripted commands (MSET, EVAL, and similar) -- check the full Command for those.")]
        string KeyName,
        [property: Description("The address of the client that issued the command, if Redis reported one.")]
        string ClientAddress,
        [property: Description("The name of the client that issued the command, if one was set and Redis reported it.")]
        string ClientName,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node that flushed this row.")]
        string Instance);
}