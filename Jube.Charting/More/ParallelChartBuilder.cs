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
using Jube.Charting.More.Models;

namespace Jube.Charting.More
{
    public sealed class ParallelChartBuilder : ChartBuilder<ParallelChartBuilder>
    {
        private readonly List<ParallelDimension> dimensions = [];
        private IReadOnlyList<string>? groups;
        private IReadOnlyList<string>? labels;

        public ParallelChartBuilder Dimension(string name, IEnumerable<double> values, bool inverted = false,
            Func<double, string>? format = null)
        {
            dimensions.Add(new ParallelDimension(name, [.. values], null, inverted, null, format));

            return this;
        }

        public ParallelChartBuilder Dimension(string name, IEnumerable<double> values, double min, double max,
            bool inverted = false, Func<double, string>? format = null)
        {
            dimensions.Add(new ParallelDimension(name, [.. values], null, inverted, (min, max), format));

            return this;
        }

        public ParallelChartBuilder CategoricalDimension(string name, IEnumerable<string?> categories,
            bool inverted = false)
        {
            dimensions.Add(new ParallelDimension(name, null, [.. categories], inverted));

            return this;
        }

        public ParallelChartBuilder Groups(IEnumerable<string> groupNames)
        {
            groups = [.. groupNames];

            return this;
        }

        public ParallelChartBuilder RowLabels(IEnumerable<string> rowLabels)
        {
            labels = [.. rowLabels];

            return this;
        }

        public ParallelChart Build()
        {
            return new ParallelChart(new ParallelChartSpec
            {
                Options = Options,
                Dimensions = [.. dimensions],
                Groups = groups,
                RowLabels = labels
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}