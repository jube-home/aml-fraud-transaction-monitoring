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

namespace Jube.Dto.PostgresIndexStatistics
{
    public sealed record PostgresIndexStatisticsDto(
        [property: Description("Postgres schema the index belongs to.")]
        string SchemaName,
        [property: Description("Name of the table this index is on.")]
        string TableName,
        [property: Description("Index name.")] string IndexName,
        [property: Description("Cumulative scans against this index since Postgres statistics were last reset.")]
        long IndexScans,
        [property: Description("Cumulative index entries read since Postgres statistics were last reset.")]
        long IndexTuplesRead,
        [property:
            Description("Cumulative table rows fetched via this index since Postgres statistics were last reset.")]
        long IndexTuplesFetched,
        [property: Description("On-disk size of this index, in bytes.")]
        long IndexSizeBytes,
        [property: Description("Whether this index enforces uniqueness.")]
        bool IsUnique,
        [property: Description("Whether this index backs the table's primary key.")]
        bool IsPrimary,
        [property:
            Description(
                "True when IndexScans is zero -- a candidate for removal, though a unique/primary-key index still enforces a constraint even at zero scans, so check IsUnique/IsPrimary before dropping one.")]
        bool IsUnused);
}