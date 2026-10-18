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
    public sealed class LiftChartBuilder : ChartBuilder<LiftChartBuilder>
    {
        private bool cumulative = true;
        private IReadOnlyList<DecileRow> deciles = [];
        private bool values;

        public LiftChartBuilder()
        {
            Options = new ChartOptions { Width = 640, Height = 480 };
        }

        public LiftChartBuilder Deciles(IEnumerable<DecileRow> decileRows)
        {
            deciles = [.. decileRows];

            return this;
        }

        public LiftChartBuilder FromScores(IEnumerable<bool> actual, IEnumerable<double> scores, int bins = 10,
            IEnumerable<double>? weights = null)
        {
            deciles = PerformanceMath.Deciles([.. actual], [.. scores], bins, weights?.ToArray());

            return this;
        }

        public LiftChartBuilder Cumulative(bool show = true)
        {
            cumulative = show;

            return this;
        }

        public LiftChartBuilder ShowValues(bool show = true)
        {
            values = show;

            return this;
        }

        public LiftChart Build()
        {
            return new LiftChart(new LiftChartSpec
            {
                Options = Options,
                Deciles = deciles,
                ShowCumulative = cumulative,
                ShowValues = values
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}