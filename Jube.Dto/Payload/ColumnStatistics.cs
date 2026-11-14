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

// ReSharper disable NotAccessedPositionalProperty.Global

using MessagePack;

namespace Jube.Dto.Payload
{
    [MessagePackObject]
    public sealed record ColumnStatistics(
        [property: Key(0)] double Min,
        [property: Key(1)] double Max,
        [property: Key(2)] double Mean,
        [property: Key(3)] double Median,
        [property: Key(4)] double StandardDeviation,
        [property: Key(5)] IReadOnlyList<HistogramBucket> Histogram);
}