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

using Jube.Charting.Core;
using Jube.Charting.Performance.Models;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Performance
{
    public sealed class GainsChartBuilder : ChartBuilder<GainsChartBuilder>
    {
        private readonly List<GainsCurve> curves = [];
        private bool absolute;
        private bool deciles = true;
        private bool perfect = true;
        private bool random = true;

        public GainsChartBuilder()
        {
            Options = new ChartOptions { Width = 640, Height = 480 };
        }

        public GainsChartBuilder Curve(string name, IEnumerable<double> population, IEnumerable<double> captured,
            IEnumerable<double>? thresholds = null, double? prevalence = null, string? color = null)
        {
            curves.Add(new GainsCurve(name, [.. population], [.. captured], thresholds?.ToArray(),
                prevalence,
                color));

            return this;
        }

        public GainsChartBuilder FromScores(string name, IEnumerable<bool> actual, IEnumerable<double> scores,
            IEnumerable<double>? weights = null, string? color = null)
        {
            var a = actual.ToArray();
            var g = PerformanceMath.Gains(a, [.. scores], weights?.ToArray());
            var total = g.Positives + g.Negatives;

            curves.Add(new GainsCurve(name, g.Population, g.Captured, g.Thresholds,
                total > 0 ? g.Positives / total : null, color, g.Positives));

            return this;
        }

        public GainsChartBuilder Absolute(bool value = true)
        {
            absolute = value;

            return this;
        }

        public GainsChartBuilder ShowRandom(bool show = true)
        {
            random = show;

            return this;
        }

        public GainsChartBuilder ShowPerfect(bool show = true)
        {
            perfect = show;

            return this;
        }

        public GainsChartBuilder DecileMarks(bool show = true)
        {
            deciles = show;

            return this;
        }

        public GainsChart Build()
        {
            return new GainsChart(new GainsChartSpec
            {
                Options = Options,
                Curves = [.. curves],
                ShowRandom = random,
                ShowPerfect = perfect,
                ShowDecileMarks = deciles,
                Absolute = absolute
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}