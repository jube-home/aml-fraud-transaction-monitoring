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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Charting.Charts.Models
{
    internal sealed class HeatModel
    {
        public required ChartOptions Options { get; init; }
        public required string Kind { get; init; }
        public required IReadOnlyList<string> Rows { get; init; }
        public required IReadOnlyList<string> Cols { get; init; }
        public required double[][] Values { get; init; }
        public bool Diverging { get; init; }
        public required (double Min, double Max) Domain { get; init; }
        public required Func<int, int, string[]> CellText { get; init; }
        public required Func<int, int, IReadOnlyList<TipRow>> Tip { get; init; }
        public required Func<double, string> LegendFormat { get; init; }
        public string Detail { get; init; } = string.Empty;
        public string? LegendTitle { get; init; }
    }
}