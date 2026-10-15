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
    public sealed class TreeDiagramBuilder : ChartBuilder<TreeDiagramBuilder>
    {
        private IReadOnlyList<string> classes = [];
        private string impurity = "impurity";
        private int maxDepth = 5;
        private TreeOrientation orientation = TreeOrientation.Auto;
        private TreeNode? root;
        private string value = "value";

        public TreeDiagramBuilder Root(TreeNode rootNode)
        {
            root = rootNode;

            return this;
        }

        public TreeDiagramBuilder Classes(IEnumerable<string> names)
        {
            classes = [.. names];

            return this;
        }

        public TreeDiagramBuilder ImpurityName(string name)
        {
            impurity = name;

            return this;
        }

        public TreeDiagramBuilder ValueName(string name)
        {
            value = name;

            return this;
        }

        public TreeDiagramBuilder MaxDepth(int depth)
        {
            maxDepth = depth;

            return this;
        }

        public TreeDiagramBuilder Orientation(TreeOrientation orientationValue)
        {
            orientation = orientationValue;

            return this;
        }

        public TreeDiagram Build()
        {
            return new TreeDiagram(new TreeDiagramSpec
            {
                Options = Options, Root = root, ClassNames = classes, ImpurityName = impurity, ValueName = value,
                MaxDepth = maxDepth, Orientation = orientation
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}