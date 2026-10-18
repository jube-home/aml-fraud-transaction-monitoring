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
    public sealed class PromotionHistoryChartBuilder : ChartBuilder<PromotionHistoryChartBuilder>
    {
        private readonly List<PromotionPoint> promotions = [];
        private bool best = true;
        private bool complexity = true;

        public PromotionHistoryChartBuilder()
        {
            Options = new ChartOptions { Width = 1000, Height = 460 };
        }

        public PromotionHistoryChartBuilder Promotion(DateTime date, double score,
            double complexityValue = double.NaN, double trials = double.NaN, double trialsToDate = double.NaN,
            bool active = false)
        {
            promotions.Add(new PromotionPoint(date, score, complexityValue, trials, trialsToDate, active));

            return this;
        }

        public PromotionHistoryChartBuilder Promotions(IEnumerable<PromotionPoint> promotionPoints)
        {
            promotions.AddRange(promotionPoints);

            return this;
        }

        public PromotionHistoryChartBuilder BestSoFar(bool show = true)
        {
            best = show;

            return this;
        }

        public PromotionHistoryChartBuilder Complexity(bool show = true)
        {
            complexity = show;

            return this;
        }

        public PromotionHistoryChart Build()
        {
            return new PromotionHistoryChart(new PromotionHistoryChartSpec
            {
                Options = Options,
                Promotions = [.. promotions],
                ShowBestSoFar = best,
                ShowComplexity = complexity
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}