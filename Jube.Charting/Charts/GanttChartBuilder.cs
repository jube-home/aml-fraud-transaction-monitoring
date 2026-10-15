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

using Jube.Charting.Charts.Models;
using Jube.Charting.Core;

namespace Jube.Charting.Charts
{
    public sealed class GanttChartBuilder : ChartBuilder<GanttChartBuilder>
    {
        private readonly List<GanttTask> tasks = [];
        private DateTime? marker;
        private string? markerLabel;

        public GanttChartBuilder Task(string label, DateTime start, DateTime end, string? group = null,
            double progress = double.NaN)
        {
            tasks.Add(new GanttTask(label, start, end, group, progress));

            return this;
        }

        public GanttChartBuilder Milestone(string label, DateTime at, string? group = null)
        {
            tasks.Add(new GanttTask(label, at, at, group));

            return this;
        }

        public GanttChartBuilder Marker(DateTime at, string? label = null)
        {
            marker = at;
            markerLabel = label;

            return this;
        }

        public GanttChart Build()
        {
            return new GanttChart(new GanttChartSpec
                { Options = Options, Tasks = [.. tasks], Marker = marker, MarkerLabel = markerLabel });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}