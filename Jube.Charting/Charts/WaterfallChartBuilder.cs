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
    public sealed class WaterfallChartBuilder : ChartBuilder<WaterfallChartBuilder>
    {
        private readonly List<WaterfallStep> steps = [];
        private Func<double, string>? format;
        private bool showValues = true;

        public WaterfallChartBuilder Step(string label, double change)
        {
            steps.Add(new WaterfallStep(label, change));

            return this;
        }

        public WaterfallChartBuilder Total(string label, double? value = null)
        {
            steps.Add(new WaterfallStep(label, value, true));

            return this;
        }

        public WaterfallChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public WaterfallChartBuilder ShowValues(bool show = true)
        {
            showValues = show;

            return this;
        }

        public WaterfallChart Build()
        {
            return new WaterfallChart(new WaterfallChartSpec
            {
                Options = Options,
                Steps = [.. steps],
                ValueFormat = format,
                ShowValues = showValues
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}