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

namespace Jube.Dto.PostgresStatementStatistics
{
    [GridEndpoint("PostgresStatementStatistics", Title = "Postgres statement statistics", DefaultSort = "", Snapshot = true)]
    [GridSearch(Label = "User, database or query text contains")]
    public sealed record PostgresStatementStatisticsDto(
        [property:
            Description(
                "pg_stat_statements' own hash identifying this normalised query shape -- the same query text with different literal values shares one QueryId. Identifies a shape, not a quantity, so it is excluded from this DTO's summary Statistics despite being numeric.")]
        [property: ListColumn(Order = 10)]
        long QueryId,
        [property: Description("Database this query shape was executed against.")]
        [property: ListColumn(Order = 20, Title = "Database")]
        string? DatabaseName,
        [property: Description("Role name that executed this query shape.")]
        [property: ListColumn(Order = 30, Title = "User")]
        string? UserName,
        [property:
            Description(
                "The normalised query text -- literal values replaced with $1, $2, etc, so many calls with different literals collapse into one row. Visible here to the same extent Postgres' own role-based visibility rules allow for the role this connection authenticates as -- a non-superuser role without pg_read_all_stats only ever sees its own queries in full.")]
        [property: ListColumn(Order = 160)]
        string? Query,
        [property:
            Description(
                "Number of times this query shape has been executed, accumulated since the extension was created or last reset.")]
        [property: ListColumn(Order = 40, Format = GridFormat.Integer)]
        long Calls,
        [property: Description("Total rows retrieved or affected across every call.")]
        [property: ListColumn(Order = 50, Format = GridFormat.Integer)]
        long Rows,
        [property: Description("Total time spent executing this query shape, summed across every call.")]
        [property: ListColumn(Order = 60, Title = "Total Time", Unit = "ms", Format = GridFormat.Decimal)]
        double TotalExecTimeMilliseconds,
        [property:
            Description(
                "TotalExecTimeMilliseconds / Calls -- the headline 'how expensive is this query on average' figure.")]
        [property: ListColumn(Order = 70, Title = "Mean Time", Unit = "ms", Format = GridFormat.Decimal)]
        double MeanExecTimeMilliseconds,
        [property: ListColumn(Order = 80, Title = "Min Time", Unit = "ms", Format = GridFormat.Decimal)]
        double MinExecTimeMilliseconds,
        [property: ListColumn(Order = 90, Title = "Max Time", Unit = "ms", Format = GridFormat.Decimal)]
        double MaxExecTimeMilliseconds,
        [property:
            Description(
                "Standard deviation of execution time across calls -- a large value alongside a low MeanExecTimeMilliseconds means this query shape is usually cheap but occasionally very expensive (a classic sign of a plan that only sometimes picks a bad index, or contention that only sometimes bites), which the mean alone hides.")]
        [property: ListColumn(Order = 100, Title = "Stddev Time", Unit = "ms", Format = GridFormat.Decimal)]
        double StddevExecTimeMilliseconds,
        [property:
            Description(
                "Shared buffer bytes served from cache across every call. Compare against SharedBlksReadBytes for this query shape's cache-hit ratio -- a low ratio for a frequently-run query is a strong 'needs an index, or shared_buffers is too small' signal.")]
        [property: ListColumn(Order = 110, Title = "Cache Hit", Unit = "B", Format = GridFormat.Integer)]
        long SharedBlksHitBytes,
        [property: Description("Shared buffer bytes read from disk (not served from cache) across every call.")]
        [property: ListColumn(Order = 120, Title = "Disk Read", Unit = "B", Format = GridFormat.Integer)]
        long SharedBlksReadBytes,
        [property:
            Description(
                "Bytes read back from temporary files across every call -- the read side of a sort or hash that spilled to disk because it did not fit in work_mem. See TempBlksWrittenBytes for the write side; non-zero here for a hot query is a direct 'raise work_mem, or fix the query' signal.")]
        [property: ListColumn(Order = 130, Title = "Temp Read", Unit = "B", Format = GridFormat.Integer)]
        long TempBlksReadBytes,
        [property:
            Description(
                "Bytes written to temporary files across every call -- the write side of the same spill-to-disk described on TempBlksReadBytes.")]
        [property: ListColumn(Order = 140, Title = "Temp Written", Unit = "B", Format = GridFormat.Integer)]
        long TempBlksWrittenBytes,
        [property:
            Description(
                "WAL bytes generated by this query shape across every call -- a heavy-write query shape's actual replication/archiving cost.")]
        [property: ListColumn(Order = 150, Title = "WAL", Unit = "B", Format = GridFormat.Integer)]
        long WalBytes);
}