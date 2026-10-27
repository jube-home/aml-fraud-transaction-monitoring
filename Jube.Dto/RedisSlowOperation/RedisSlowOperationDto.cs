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
using Jube.Dto.Forms;
using Jube.Dto.Options;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.RedisSlowOperation
{
    [GridEndpoint("RedisSlowOperation", Title = "Redis slow operation", DefaultSort = "occurredDate")]
    [GridSearch(Label = "Command, client address or name contains")]
    public sealed record RedisSlowOperationDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("Redis' own monotonic SLOWLOG sequence id for this entry.")]
        [property: ListColumn(Hidden = true)]
        long RedisSlowLogId,
        [property: Description("UTC timestamp the slow operation occurred, as reported by Redis.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property: Description("How long the operation took to execute, in microseconds.")]
        [property: ListColumn(Order = 20, Title = "Duration", Unit = "µs", Format = GridFormat.Integer)]
        long DurationMicroseconds,
        [property:
            Description(
                "The command and its arguments, space-joined -- kept in full for detail; see CommandName/KeyName to group or filter without parsing this.")]
        [property: ListColumn(Order = 30)]
        string Command,
        [property:
            Description(
                "The command verb (e.g. SET, HGETALL, EVAL), pulled out of Command -- group by this to see which command type is actually slow.")]
        [property: ListColumn(Hidden = true)]
        string CommandName,
        [property:
            Description(
                "Best-effort key name, the argument after the command verb. Correct for most commands; not meaningful for multi-key or scripted commands (MSET, EVAL, and similar) -- check the full Command for those.")]
        [property: ListColumn(Hidden = true)]
        string KeyName,
        [property: Description("The address of the client that issued the command, if Redis reported one.")]
        [property: ListColumn(Order = 40)]
        string ClientAddress,
        [property: Description("The name of the client that issued the command, if one was set and Redis reported it.")]
        [property: ListColumn(Order = 50)]
        string ClientName,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 70, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node that flushed this row.")]
        [property: ListColumn(Order = 60)]
        string Instance);
}