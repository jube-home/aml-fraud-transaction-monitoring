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
using Jube.Charting.Diagrams.Models;

namespace Jube.Charting.Diagrams
{
    public sealed class CoefficientDiagramBuilder : ChartBuilder<CoefficientDiagramBuilder>
    {
        private readonly List<CoefficientItem> items = [];
        private double alpha = 0.05;
        private double confidence = 0.95;
        private bool log;
        private int max = 40;
        private string name = "Coefficient";
        private bool odds;
        private CoefficientSort sort = CoefficientSort.Magnitude;

        public CoefficientDiagramBuilder Item(string itemName, double estimate, double? low = null,
            double? high = null, double? pValue = null, double? standardError = null)
        {
            items.Add(new CoefficientItem(itemName, estimate)
                { Low = low, High = high, PValue = pValue, StandardError = standardError });

            return this;
        }

        public CoefficientDiagramBuilder Sort(CoefficientSort sortOrder)
        {
            sort = sortOrder;

            return this;
        }

        public CoefficientDiagramBuilder LogScale(bool on = true)
        {
            log = on;

            return this;
        }

        public CoefficientDiagramBuilder OddsRatios(bool on = true)
        {
            odds = on;

            return this;
        }

        public CoefficientDiagramBuilder ConfidenceLevel(double level)
        {
            confidence = level;

            return this;
        }

        public CoefficientDiagramBuilder SignificanceLevel(double level)
        {
            alpha = level;

            return this;
        }

        public CoefficientDiagramBuilder EstimateName(string estimateName)
        {
            name = estimateName;

            return this;
        }

        public CoefficientDiagramBuilder MaxRows(int rows)
        {
            max = rows;

            return this;
        }

        public CoefficientDiagram Build()
        {
            return new CoefficientDiagram(new CoefficientDiagramSpec
            {
                Options = Options, Items = [.. items], Sort = sort, LogScale = log, ShowOddsRatios = odds,
                ConfidenceLevel = confidence, SignificanceLevel = alpha, EstimateName = name, MaxRows = max
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}