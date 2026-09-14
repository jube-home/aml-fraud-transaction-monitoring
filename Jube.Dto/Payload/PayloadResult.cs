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

namespace Jube.Dto.Payload
{
    public sealed record PayloadResult<T>(
        [property: Description(
            "Rows matching the filter, ordered per sortField/sortDirection, most recent first when neither is given, capped at 'take'.")]
        IReadOnlyList<T> Rows,
        [property: Description("Total rows matching the filter, ignoring 'take'.")]
        long Total,
        [property: Description(
            "Summary statistics for this DTO's continuous/measured numeric columns (durations, byte counts, sums, per-interval tallies -- int, long or double), computed over the full filtered set (capped at 100000 rows), independent of 'take'. Categorical numeric columns (ids, codes, sequence numbers) are excluded even though numeric. Empty when the DTO has no qualifying columns.")]
        PayloadStatistics Statistics);
}