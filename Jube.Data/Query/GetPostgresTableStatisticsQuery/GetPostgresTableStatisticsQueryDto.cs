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

using System;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Data.Query.GetPostgresTableStatisticsQuery
{
    public sealed class GetPostgresTableStatisticsQueryDto
    {
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public long? SequentialScans { get; set; }
        public long? SequentialTuplesRead { get; set; }
        public long? IndexScans { get; set; }
        public long? IndexTuplesFetched { get; set; }
        public long? LiveTupleCount { get; set; }
        public long? DeadTupleCount { get; set; }
        public long? TableSizeBytes { get; set; }
        public long? IndexesSizeBytes { get; set; }
        public long? TotalSizeBytes { get; set; }
        public DateTime? LastVacuum { get; set; }
        public DateTime? LastAutoVacuum { get; set; }
        public DateTime? LastAnalyze { get; set; }
        public DateTime? LastAutoAnalyze { get; set; }
        public bool? LikelyMissingIndex { get; set; }
    }
}