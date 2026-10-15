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

namespace Jube.Charting.Performance.Models
{
    public sealed record MetricTile(
        string Name,
        double Value,
        string? Detail = null,
        string? Description = null,
        string? Format = null);

    public sealed record MetricGroup(string Title, IReadOnlyList<MetricTile> Tiles);

    public sealed record MetricsPanelChartSpec
    {
        public ChartOptions Options { get; init; } = new() { Width = 720, Height = 400 };
        public IReadOnlyList<MetricGroup> Groups { get; init; } = [];
        public int Columns { get; init; } = 4;
    }
}