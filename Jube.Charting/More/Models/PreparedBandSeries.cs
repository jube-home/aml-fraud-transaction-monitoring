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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Charting.More.Models
{
    internal sealed class PreparedBandSeries
    {
        public required string Name { get; init; }
        public required BandSeriesStyle Style { get; init; }
        public required bool Dashed { get; init; }
        public double? ForecastFrom { get; init; }
        public required List<double> X { get; init; }
        public required List<double> Y { get; init; }
        public required List<string> BandLabels { get; init; }
        public required List<List<double>> Lower { get; init; }
        public required List<List<double>> Upper { get; init; }
        public required List<bool> BandHidden { get; init; }
        public required List<double>? YErrLow { get; init; }
        public required List<double>? YErrHigh { get; init; }
        public required List<double>? XErrLow { get; init; }
        public required List<double>? XErrHigh { get; init; }
        public bool LineLike => Style != BandSeriesStyle.Points;
        public bool HasPoints => Style is BandSeriesStyle.Points or BandSeriesStyle.LineAndPoints;
    }
}