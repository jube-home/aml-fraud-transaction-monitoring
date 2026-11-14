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

namespace Jube.Dto.Options
{
    using System.ComponentModel;

    public enum DatasourceDisplayKind
    {
        [Description("Column chart")] Column = 1,
        [Description("Bar chart")] Bar = 2,
        [Description("Stacked column chart")] StackedColumn = 3,
        [Description("Stacked bar chart")] StackedBar = 4,
        [Description("Percent stacked column chart")] PercentStackedColumn = 5,
        [Description("Percent stacked bar chart")] PercentStackedBar = 6,
        [Description("Line chart")] Line = 7,
        [Description("Area chart")] Area = 8,
        [Description("Stacked area chart")] StackedArea = 9,
        [Description("Percent stacked area chart")] PercentStackedArea = 10,
        [Description("Pie chart")] Pie = 11,
        [Description("Donut chart")] Donut = 12,
        [Description("Scatter or bubble chart")] Scatter = 13,
        [Description("Histogram")] Histogram = 14,
        [Description("Heatmap")] Heatmap = 15,
        [Description("Combination chart")] Combo = 16,
        [Description("Box plot")] BoxPlot = 17,
        [Description("Candlestick chart")] Candlestick = 18,
        [Description("Waterfall chart")] Waterfall = 19,
        [Description("Funnel chart")] Funnel = 20,
        [Description("Radar chart")] Radar = 21,
        [Description("Polar chart")] Polar = 22,
        [Description("Gauge")] Gauge = 23,
        [Description("Bullet chart")] Bullet = 24,
        [Description("Sparkline")] Sparkline = 25,
        [Description("Treemap")] Treemap = 26,
        [Description("Gantt chart")] Gantt = 27,
        [Description("Map")] Map = 28,
        [Description("Template")] Template = 29
    }
}
