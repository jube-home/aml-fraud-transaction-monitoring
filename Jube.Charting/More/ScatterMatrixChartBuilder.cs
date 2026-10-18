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
    public sealed class ScatterMatrixChartBuilder : ChartBuilder<ScatterMatrixChartBuilder>
    {
        private readonly List<MatrixVariable> variables = [];
        private int bins = 10;
        private IReadOnlyList<string>? groups;
        private MatrixUpper upper = MatrixUpper.Correlation;

        public ScatterMatrixChartBuilder Variable(string name, IEnumerable<double> values)
        {
            variables.Add(new MatrixVariable(name, [.. values]));

            return this;
        }

        public ScatterMatrixChartBuilder Groups(IEnumerable<string> groupNames)
        {
            groups = [.. groupNames];

            return this;
        }

        public ScatterMatrixChartBuilder Upper(MatrixUpper upperMode)
        {
            upper = upperMode;

            return this;
        }

        public ScatterMatrixChartBuilder Bins(int binCount)
        {
            bins = binCount;

            return this;
        }

        public ScatterMatrixChart Build()
        {
            return new ScatterMatrixChart(new ScatterMatrixSpec
            {
                Options = Options,
                Variables = [.. variables],
                Groups = groups,
                Upper = upper,
                Bins = bins
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}