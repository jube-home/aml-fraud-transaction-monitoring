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

using Jube.Charting.Core.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Charting.Diagrams.Models
{
    public sealed record TreeBranch(string? Label, TreeNode Node);

    public sealed record TreeNode
    {
        public string? Feature { get; init; }
        public double? Threshold { get; init; }
        public IReadOnlyList<string>? Categories { get; init; }
        public string? Condition { get; init; }
        public double? Samples { get; init; }
        public IReadOnlyList<double>? ClassCounts { get; init; }
        public double? Value { get; init; }
        public double? Impurity { get; init; }
        public string? Prediction { get; init; }
        public IReadOnlyList<TreeBranch> Branches { get; init; } = [];
        public bool IsLeaf => Branches.Count == 0;

        public static TreeNode Split(string feature, double threshold, TreeNode yes, TreeNode no,
            double? samples = null,
            IReadOnlyList<double>? classCounts = null, double? impurity = null, double? value = null)
        {
            return new TreeNode
            {
                Feature = feature, Threshold = threshold, Samples = samples, ClassCounts = classCounts,
                Impurity = impurity, Value = value, Branches = [new TreeBranch("yes", yes), new TreeBranch("no", no)]
            };
        }

        public static TreeNode SplitOn(string feature, IReadOnlyList<string> categories, TreeNode yes, TreeNode no,
            double? samples = null, IReadOnlyList<double>? classCounts = null, double? impurity = null)
        {
            return new TreeNode
            {
                Feature = feature, Categories = categories, Samples = samples, ClassCounts = classCounts,
                Impurity = impurity, Branches = [new TreeBranch("yes", yes), new TreeBranch("no", no)]
            };
        }

        public static TreeNode Leaf(double? samples = null, IReadOnlyList<double>? classCounts = null,
            double? value = null, string? prediction = null, double? impurity = null)
        {
            return new TreeNode
            {
                Samples = samples, ClassCounts = classCounts, Value = value, Prediction = prediction,
                Impurity = impurity
            };
        }
    }

    public sealed record TreeDiagramSpec
    {
        public ChartOptions Options { get; init; } = new();
        public TreeNode? Root { get; init; }
        public IReadOnlyList<string> ClassNames { get; init; } = [];
        public string ImpurityName { get; init; } = "impurity";
        public string ValueName { get; init; } = "value";
        public int MaxDepth { get; init; } = 5;
        public TreeOrientation Orientation { get; init; } = TreeOrientation.Auto;
    }
}