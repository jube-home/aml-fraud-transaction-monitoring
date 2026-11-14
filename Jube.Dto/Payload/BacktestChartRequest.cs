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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

using MessagePack;

namespace Jube.Dto.Payload
{
    [MessagePackObject]
    public sealed record BacktestChartTag([property: Key(0)] string Name, [property: Key(1)] long Count);

    [MessagePackObject]
    public sealed record BacktestChartRequest(
        [property: Key(0)] bool Dark,
        [property: Key(1)] long Scanned,
        [property: Key(2)] long Evaluated,
        [property: Key(3)] long Fired,
        [property: Key(4)] long Positives,
        [property: Key(5)] long TruePositives,
        [property: Key(6)] long FalsePositives,
        [property: Key(7)] long FalseNegatives,
        [property: Key(8)] long TrueNegatives,
        [property: Key(9)] bool ClassDefined,
        [property: Key(10)] IReadOnlyList<BacktestChartTag> Tags);
}
