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

namespace Jube.Dto.PostgresTableStatistics
{
    [GridEndpoint("PostgresTableStatistics", Title = "Postgres table statistics", DefaultSort = "", Snapshot = true)]
    public sealed record PostgresTableStatisticsDto(
        [property: Description("Postgres schema the table belongs to.")]
        [property: ListColumn(Order = 10, Title = "Schema")]
        string SchemaName,
        [property: ListColumn(Order = 20, Title = "Table")]
        [property: Description("Table name.")] string TableName,
        [property: Description("Cumulative sequential (full table) scans since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 40, Format = GridFormat.Integer)]
        long SequentialScans,
        [property: Description("Cumulative rows read via sequential scans since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 50, Format = GridFormat.Integer)]
        long SequentialTuplesRead,
        [property: Description("Cumulative index scans against this table since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 60, Format = GridFormat.Integer)]
        long IndexScans,
        [property: Description("Cumulative rows fetched via index scans since Postgres statistics were last reset.")]
        [property: ListColumn(Order = 70, Format = GridFormat.Integer)]
        long IndexTuplesFetched,
        [property: Description("Estimated number of live rows currently in the table.")]
        [property: ListColumn(Order = 80, Title = "Live Tuples", Format = GridFormat.Integer)]
        long LiveTupleCount,
        [property:
            Description(
                "Estimated number of dead (not-yet-vacuumed) rows currently in the table -- a high figure relative to LiveTupleCount suggests vacuum isn't keeping up.")]
        [property: ListColumn(Order = 90, Title = "Dead Tuples", Format = GridFormat.Integer)]
        long DeadTupleCount,
        [property: Description("On-disk size of the table itself, in bytes, excluding indexes.")]
        [property: ListColumn(Order = 100, Title = "Table Size", Unit = "B", Format = GridFormat.Integer)]
        long TableSizeBytes,
        [property: Description("Combined on-disk size of every index on this table, in bytes.")]
        [property: ListColumn(Order = 110, Title = "Indexes Size", Unit = "B", Format = GridFormat.Integer)]
        long IndexesSizeBytes,
        [property: Description("Total on-disk size of the table including indexes and TOAST data, in bytes.")]
        [property: ListColumn(Order = 120, Title = "Total Size", Unit = "B", Format = GridFormat.Integer)]
        long TotalSizeBytes,
        [property: Description("When this table was last manually VACUUMed; null if never.")]
        [property: ListColumn(Order = 130)]
        DateTime? LastVacuum,
        [property: Description("When this table was last VACUUMed by autovacuum; null if never.")]
        [property: ListColumn(Order = 140, Title = "Last Autovacuum")]
        DateTime? LastAutoVacuum,
        [property: Description("When this table was last manually ANALYZEd; null if never.")]
        [property: ListColumn(Order = 150)]
        DateTime? LastAnalyze,
        [property: Description("When this table was last ANALYZEd by autovacuum; null if never.")]
        [property: ListColumn(Order = 160, Title = "Last Autoanalyze")]
        DateTime? LastAutoAnalyze,
        [property:
            Description(
                "A simple heuristic flag: true when this table has more sequential scans than index scans and a non-trivial row count (over 1000 live rows) -- a signal worth investigating for a missing index, not a verdict.")]
        [property: ListColumn(Order = 30)]
        bool LikelyMissingIndex);
}