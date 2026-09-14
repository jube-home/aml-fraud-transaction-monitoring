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

namespace Jube.Dto.PostgresTableStatistics
{
    public sealed record PostgresTableStatisticsDto(
        [property: Description("Postgres schema the table belongs to.")]
        string SchemaName,
        [property: Description("Table name.")] string TableName,
        [property: Description("Cumulative sequential (full table) scans since Postgres statistics were last reset.")]
        long SequentialScans,
        [property: Description("Cumulative rows read via sequential scans since Postgres statistics were last reset.")]
        long SequentialTuplesRead,
        [property: Description("Cumulative index scans against this table since Postgres statistics were last reset.")]
        long IndexScans,
        [property: Description("Cumulative rows fetched via index scans since Postgres statistics were last reset.")]
        long IndexTuplesFetched,
        [property: Description("Estimated number of live rows currently in the table.")]
        long LiveTupleCount,
        [property:
            Description(
                "Estimated number of dead (not-yet-vacuumed) rows currently in the table -- a high figure relative to LiveTupleCount suggests vacuum isn't keeping up.")]
        long DeadTupleCount,
        [property: Description("On-disk size of the table itself, in bytes, excluding indexes.")]
        long TableSizeBytes,
        [property: Description("Combined on-disk size of every index on this table, in bytes.")]
        long IndexesSizeBytes,
        [property: Description("Total on-disk size of the table including indexes and TOAST data, in bytes.")]
        long TotalSizeBytes,
        [property: Description("When this table was last manually VACUUMed; null if never.")]
        DateTime? LastVacuum,
        [property: Description("When this table was last VACUUMed by autovacuum; null if never.")]
        DateTime? LastAutoVacuum,
        [property: Description("When this table was last manually ANALYZEd; null if never.")]
        DateTime? LastAnalyze,
        [property: Description("When this table was last ANALYZEd by autovacuum; null if never.")]
        DateTime? LastAutoAnalyze,
        [property:
            Description(
                "A simple heuristic flag: true when this table has more sequential scans than index scans and a non-trivial row count (over 1000 live rows) -- a signal worth investigating for a missing index, not a verdict.")]
        bool LikelyMissingIndex);
}