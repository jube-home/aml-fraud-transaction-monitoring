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
    public sealed class ForestOfTreesBuilder : ChartBuilder<ForestOfTreesBuilder>
    {
        private readonly List<FeatureImportance> importances = [];
        private readonly List<TreeDiagramSpec> trees = [];
        private IReadOnlyList<string> classes = [];
        private int depth = 4;
        private string importanceName = "Importance";
        private int max = 6;
        private int total;

        public ForestOfTreesBuilder Tree(TreeNode root)
        {
            trees.Add(new TreeDiagramSpec { Root = root });

            return this;
        }

        public ForestOfTreesBuilder Tree(TreeDiagramSpec spec)
        {
            trees.Add(spec);

            return this;
        }

        public ForestOfTreesBuilder Classes(IEnumerable<string> names)
        {
            classes = [.. names];

            return this;
        }

        public ForestOfTreesBuilder Importance(string feature, double value)
        {
            importances.Add(new FeatureImportance(feature, value));

            return this;
        }

        public ForestOfTreesBuilder ImportanceName(string name)
        {
            importanceName = name;

            return this;
        }

        public ForestOfTreesBuilder TotalTrees(int totalTrees)
        {
            total = totalTrees;

            return this;
        }

        public ForestOfTreesBuilder MaxTrees(int maxTreeCount)
        {
            max = maxTreeCount;

            return this;
        }

        public ForestOfTreesBuilder TreeDepth(int treeDepth)
        {
            depth = treeDepth;

            return this;
        }

        public ForestOfTrees Build()
        {
            return new ForestOfTrees(new ForestOfTreesSpec
            {
                Options = Options, Trees = [.. trees], ClassNames = classes,
                Importances = [.. importances],
                TotalTrees = total, MaxTrees = max, TreeDepth = depth, ImportanceName = importanceName
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}