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
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class PrecisionRecallChartBuilder : ChartBuilder<PrecisionRecallChartBuilder>
    {
        private readonly List<PrecisionRecallCurve> curves = [];
        private bool threshold = true;

        public PrecisionRecallChartBuilder()
        {
            Options = new ChartOptions { Width = 520, Height = 480 };
        }

        public PrecisionRecallChartBuilder Curve(string name, IEnumerable<double> recall, IEnumerable<double> precision,
            IEnumerable<double>? thresholds = null, double? averagePrecision = null, double? prevalence = null,
            string? color = null)
        {
            curves.Add(new PrecisionRecallCurve(name, [.. recall], [.. precision], thresholds?.ToArray(),
                averagePrecision, prevalence, color));

            return this;
        }

        public PrecisionRecallChartBuilder FromScores(string name, IEnumerable<bool> actual, IEnumerable<double> scores,
            string? color = null)
        {
            var r = CurveMath.PrecisionRecall([.. actual], [.. scores]);

            curves.Add(new PrecisionRecallCurve(name, r.Recall, r.Precision, r.Thresholds, r.AveragePrecision,
                r.Prevalence, color));

            return this;
        }

        public PrecisionRecallChartBuilder ThresholdPoints(bool show = true)
        {
            threshold = show;

            return this;
        }

        public PrecisionRecallChart Build()
        {
            return new PrecisionRecallChart(new PrecisionRecallChartSpec
            {
                Options = Options,
                Curves = [.. curves],
                ShowThresholdPoints = threshold
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}