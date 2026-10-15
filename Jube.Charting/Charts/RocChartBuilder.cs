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
    public sealed class RocChartBuilder : ChartBuilder<RocChartBuilder>
    {
        private readonly List<RocCurve> curves = [];
        private bool best = true;
        private bool chance = true;
        private bool threshold = true;

        public RocChartBuilder()
        {
            Options = new ChartOptions { Width = 520, Height = 480 };
        }

        public RocChartBuilder Curve(string name, IEnumerable<double> fpr, IEnumerable<double> tpr,
            IEnumerable<double>? thresholds = null, double? auc = null, string? color = null)
        {
            curves.Add(new RocCurve(name, [.. fpr], [.. tpr], thresholds?.ToArray(), auc, color));

            return this;
        }

        public RocChartBuilder FromScores(string name, IEnumerable<bool> actual, IEnumerable<double> scores,
            IEnumerable<double>? weights = null, string? color = null)
        {
            var r = CurveMath.Roc([.. actual], [.. scores], weights?.ToArray());
            curves.Add(new RocCurve(name, r.FalsePositiveRate, r.TruePositiveRate, r.Thresholds, r.Auc, color));

            return this;
        }

        public RocChartBuilder ShowChance(bool show = true)
        {
            chance = show;

            return this;
        }

        public RocChartBuilder ThresholdPoints(bool show = true)
        {
            threshold = show;

            return this;
        }

        public RocChartBuilder BestPoint(bool show = true)
        {
            best = show;

            return this;
        }

        public RocChart Build()
        {
            return new RocChart(new RocChartSpec
            {
                Options = Options,
                Curves = [.. curves],
                ShowChance = chance,
                ShowThresholdPoints = threshold,
                ShowBestPoint = best
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}