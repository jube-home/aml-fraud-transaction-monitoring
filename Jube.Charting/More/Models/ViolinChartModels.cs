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

namespace Jube.Charting.More.Models
{
    public sealed record DistributionGroup(string Name, IReadOnlyList<double> Values, string? Color = null);

    public sealed record ViolinChartSpec
    {
        public ChartOptions Options { get; init; } = new();
        public IReadOnlyList<DistributionGroup> Groups { get; init; } = [];
        public bool Horizontal { get; init; }
        public double? Bandwidth { get; init; }
        public double BandwidthAdjust { get; init; } = 1;
        public DensityScale Scale { get; init; } = DensityScale.Area;
        public bool ShowBox { get; init; } = true;
        public bool ShowPoints { get; init; }
        public Func<double, string>? ValueFormat { get; init; }
    }
}