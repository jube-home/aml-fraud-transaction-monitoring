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

using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    internal enum TreeLeafStyle
    {
        Probability,
        Value,
        Size
    }

    internal sealed record NodeArrays(
        int[] Feature,
        double[] Threshold,
        int[] Left,
        int[] Right,
        double[]? Value,
        int[]? Size)
    {
        public int Count => Feature.Length;
    }

    internal sealed class SnapshotTrees
    {
        public static readonly IReadOnlyList<string> BinaryClasses = ["Class 0", "Class 1"];
        private readonly IReadOnlyList<string> names;
        private readonly bool strictLess;
        private readonly TreeLeafStyle style;

        public SnapshotTrees(IReadOnlyList<string> names, TreeLeafStyle style, bool strictLess = false)
        {
            this.names = names;
            this.style = style;
            this.strictLess = strictLess;
            SplitWeights = new double[names.Count];
        }

        public double[] SplitWeights { get; }
        public long Nodes { get; private set; }

        public static NodeArrays ByFeatureIndex(JToken tree, string thresholdKey, string? valueKey,
            string? sizeKey = null)
        {
            var feature = ModelJson.Integers(tree, "featureIndex") ??
                          throw ModelJson.Inconsistent("a tree has no feature indexes");

            return Checked(new NodeArrays(feature,
                ModelJson.Doubles(tree, thresholdKey) ?? throw ModelJson.Inconsistent("a tree has no " + thresholdKey),
                ModelJson.Integers(tree, "left") ?? throw ModelJson.Inconsistent("a tree has no left children"),
                ModelJson.Integers(tree, "right") ?? throw ModelJson.Inconsistent("a tree has no right children"),
                valueKey is null ? null : ModelJson.Doubles(tree, valueKey),
                sizeKey is null ? null : ModelJson.Integers(tree, sizeKey)));
        }

        public static NodeArrays ByLeafFlag(JToken tree, string valueKey)
        {
            if (ModelJson.Get(tree, "isLeaf") is not JArray flags ||
                flags.Any(f => f.Type != JTokenType.Boolean))
            {
                throw ModelJson.Inconsistent("a tree has no leaf flags");
            }

            var attribute = ModelJson.Integers(tree, "attributeIndex") ??
                            throw ModelJson.Inconsistent("a tree has no attribute indexes");

            if (attribute.Length != flags.Count)
            {
                throw ModelJson.Inconsistent("the tree node arrays must all be the same length");
            }

            var feature = attribute.Select((a, i) => flags[i].Value<bool>() ? -1 : a).ToArray();

            return Checked(new NodeArrays(feature,
                ModelJson.Doubles(tree, "threshold") ?? throw ModelJson.Inconsistent("a tree has no thresholds"),
                ModelJson.Integers(tree, "leftChild") ?? throw ModelJson.Inconsistent("a tree has no left children"),
                ModelJson.Integers(tree, "rightChild") ?? throw ModelJson.Inconsistent("a tree has no right children"),
                ModelJson.Doubles(tree, valueKey), null));
        }

        private static NodeArrays Checked(NodeArrays a)
        {
            if (a.Count == 0 || a.Threshold.Length != a.Count || a.Left.Length != a.Count ||
                a.Right.Length != a.Count ||
                (a.Value is not null && a.Value.Length != a.Count) || (a.Size is not null && a.Size.Length != a.Count))
            {
                throw ModelJson.Inconsistent("the tree node arrays must all be the same length");
            }

            return a;
        }

        public TreeNode Read(NodeArrays a, double weight = 1)
        {
            Nodes += a.Count;
            var seen = new bool[a.Count];

            return Node(a, 0, 0, seen, weight);
        }

        private TreeNode Node(NodeArrays a, int index, int depth, bool[] seen, double weight)
        {
            if (depth > 200)
            {
                throw ModelJson.Inconsistent("the tree is nested too deeply");
            }

            if (index < 0 || index >= a.Count || seen[index])
            {
                throw ModelJson.Inconsistent("a tree child index is out of range or shared");
            }

            seen[index] = true;
            var value = a.Value is not null && double.IsFinite(a.Value[index]) ? a.Value[index] : (double?)null;
            var samples = a.Size is not null ? a.Size[index] : (double?)null;

            var counts = style == TreeLeafStyle.Probability && value is { } p
                ? new[] { Math.Clamp(1 - p, 0, 1), Math.Clamp(p, 0, 1) }
                : null;

            var feature = a.Feature[index];

            if (feature < 0)
            {
                return TreeNode.Leaf(samples, counts, style == TreeLeafStyle.Probability ? null : value);
            }

            if (feature >= names.Count)
            {
                throw ModelJson.Inconsistent("a split refers to variable " + feature + " but there are " +
                                             names.Count);
            }

            var threshold = a.Threshold[index];

            if (!double.IsFinite(threshold))
            {
                throw ModelJson.Inconsistent("a split threshold is not finite");
            }

            SplitWeights[feature] += weight;
            var name = names[feature];

            var split = TreeNode.Split(name, threshold, Node(a, a.Left[index], depth + 1, seen, weight),
                Node(a, a.Right[index], depth + 1, seen, weight), samples, counts,
                value: style == TreeLeafStyle.Probability ? null : value);

            return strictLess ? split with { Condition = name + " < " + ModelJson.Format(threshold) } : split;
        }

        public FeatureImportance[] Importances()
        {
            var total = SplitWeights.Sum();

            return total > 0
                ? names.Select((n, i) => new FeatureImportance(n, SplitWeights[i] / total)).Where(i => i.Value > 0)
                    .ToArray()
                : [];
        }

        public TreeDiagram Single(NodeArrays tree, ModelDiagramOptions options, string title, string subtitle,
            string description, string valueName)
        {
            var root = Read(tree);
            options.RequireWithin(Nodes, "tree nodes");

            return new TreeDiagram(new TreeDiagramSpec
            {
                Options = options.ToChartOptions(title, subtitle, description),
                Root = root,
                ClassNames = style == TreeLeafStyle.Probability ? BinaryClasses : [],
                ValueName = valueName
            });
        }

        public ForestOfTrees Forest(IReadOnlyList<NodeArrays> trees, IReadOnlyList<double>? weights,
            ModelDiagramOptions options, string title, string description, string valueName, string importanceName)
        {
            if (trees.Count == 0)
            {
                throw ModelJson.Inconsistent("the ensemble has no trees");
            }

            if (weights is not null && weights.Count != trees.Count)
            {
                throw ModelJson.Inconsistent("the ensemble has " + weights.Count + " tree weights for " + trees.Count +
                                             " trees");
            }

            const int shown = 6;
            var specs = new List<TreeDiagramSpec>();
            var classes = style == TreeLeafStyle.Probability ? BinaryClasses : [];

            for (var t = 0; t < trees.Count; t++)
            {
                var weight = weights is null ? 1 : weights[t];
                var root = Read(trees[t], double.IsFinite(weight) && weight > 0 ? weight : 0);

                if (t < shown)
                {
                    specs.Add(new TreeDiagramSpec { Root = root, ClassNames = classes, ValueName = valueName });
                }
            }

            return new ForestOfTrees(new ForestOfTreesSpec
            {
                Options = options.ToChartOptions(title, trees.Count + " trees, " + Nodes + " nodes in total",
                    description),
                Trees = specs,
                ClassNames = classes,
                Importances = Importances(),
                TotalTrees = trees.Count,
                MaxTrees = shown,
                ImportanceName = importanceName
            });
        }
    }
}