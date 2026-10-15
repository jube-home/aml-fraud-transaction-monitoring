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
    public sealed class ComponentsDiagramBuilder : ChartBuilder<ComponentsDiagramBuilder>
    {
        private readonly List<ClusterItem> clusters = [];
        private readonly List<ComponentItem> components = [];
        private IReadOnlyList<string> features = [];
        private int maxColumns = 12;
        private int maxRows = 40;
        private string valueName = "Standardised value";

        public ComponentsDiagramBuilder Features(IEnumerable<string> names)
        {
            features = [.. names];

            return this;
        }

        public ComponentsDiagramBuilder Component(string name, double explainedVariance, IEnumerable<double> loadings)
        {
            components.Add(new ComponentItem(name, explainedVariance, [.. loadings]));

            return this;
        }

        public ComponentsDiagramBuilder Cluster(string name, double size, IEnumerable<double> centroid)
        {
            clusters.Add(new ClusterItem(name, size, [.. centroid]));

            return this;
        }

        public ComponentsDiagramBuilder MaxFeatureRows(int rows)
        {
            maxRows = rows;

            return this;
        }

        public ComponentsDiagramBuilder MaxComponentColumns(int columns)
        {
            maxColumns = columns;

            return this;
        }

        public ComponentsDiagramBuilder ValueName(string name)
        {
            valueName = name;

            return this;
        }

        public ComponentsDiagram Build()
        {
            return new ComponentsDiagram(new ComponentsDiagramSpec
            {
                Options = Options, Features = features, Components = [.. components],
                Clusters = [.. clusters],
                MaxFeatureRows = maxRows, MaxComponentColumns = maxColumns, ValueName = valueName
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}