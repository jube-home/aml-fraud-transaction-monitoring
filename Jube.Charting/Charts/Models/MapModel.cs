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

using Jube.Charting.Core.Models;
using Jube.Charting.Maps;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Charting.Charts.Models
{
    internal sealed class MapModel
    {
        public required ChartOptions Options { get; init; }
        public required IReadOnlyDictionary<string, double> ValuesByIso3 { get; init; }
        public required IReadOnlyList<MapPoint> Points { get; init; }
        public required IReadOnlyList<string> Groups { get; init; }
        public required (double Min, double Max) Domain { get; init; }
        public required Func<double, string> Format { get; init; }
        public required string Detail { get; init; }
        public string? LegendTitle { get; init; }
        public bool HasValues => ValuesByIso3.Count > 0;
        public IReadOnlyList<MapCountry> Countries => WorldMap.Countries;
    }
}
