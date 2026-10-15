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
    public sealed class CalibrationChartBuilder : ChartBuilder<CalibrationChartBuilder>
    {
        private readonly List<CalibrationCurve> curves = [];
        private bool counts = true;

        public CalibrationChartBuilder()
        {
            Options = new ChartOptions { Width = 520, Height = 560 };
        }

        public CalibrationChartBuilder Curve(string name, IEnumerable<double> meanPredicted,
            IEnumerable<double> fractionPositive, IEnumerable<double>? binCounts = null,
            IEnumerable<double>? binEdges = null,
            double? expectedCalibrationError = null, string? color = null)
        {
            curves.Add(new CalibrationCurve(name, [.. meanPredicted], [.. fractionPositive],
                binCounts?.ToArray(),
                binEdges?.ToArray(), expectedCalibrationError, color));

            return this;
        }

        public CalibrationChartBuilder FromProbabilities(string name, IEnumerable<bool> actual,
            IEnumerable<double> probabilities, int bins = 10, bool quantile = false, string? color = null)
        {
            var r = CurveMath.Calibration([.. actual], [.. probabilities], bins, quantile);

            curves.Add(new CalibrationCurve(name, r.MeanPredicted, r.FractionPositive, r.Counts, r.BinEdges,
                r.ExpectedCalibrationError, color));

            return this;
        }

        public CalibrationChartBuilder ShowCounts(bool show = true)
        {
            counts = show;

            return this;
        }

        public CalibrationChart Build()
        {
            return new CalibrationChart(new CalibrationChartSpec
            {
                Options = Options,
                Curves = [.. curves],
                ShowCounts = counts
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}