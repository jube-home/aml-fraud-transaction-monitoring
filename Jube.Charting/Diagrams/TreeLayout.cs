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

using Jube.Charting.Diagrams.Models;
using Jube.Charting.Text;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Diagrams
{
    public static class TreeLayout
    {
        public static TreeLayoutResult Compute(TreeDiagramSpec spec, bool compact)
        {
            if (spec.Root is null)
            {
                throw new ChartSpecificationException("A tree diagram needs a root node.");
            }

            if (spec.ClassNames.Count > 8)
            {
                throw new DiagramNotSupportedException("a tree with more than 8 classes cannot be coloured legibly; " +
                                                       "group the minor classes before drawing.");
            }

            if (spec.MaxDepth < 1 || spec.MaxDepth > 20)
            {
                throw new ChartSpecificationException("Tree MaxDepth must be between 1 and 20.");
            }

            var total = Inspect(spec.Root);
            var limit = "tree nodes drawn";
            long actual = DiagramLimits.MaxTreeDrawnNodes + 1;
            long maximum = DiagramLimits.MaxTreeDrawnNodes;

            for (var depth = spec.MaxDepth; depth >= 1; depth--)
            {
                var nodes = new List<PlacedTreeNode>();
                var edges = new List<PlacedTreeEdge>();
                var ok = Build(spec, compact, spec.Root, -1, 0, depth, nodes, edges);

                if (!ok)
                {
                    limit = "tree nodes drawn";
                    actual = DiagramLimits.MaxTreeDrawnNodes + 1;
                    maximum = DiagramLimits.MaxTreeDrawnNodes;
                    continue;
                }

                var result = Arrange(spec, compact, nodes, edges, depth, total);

                if (result is { Width: <= MaxWidth, Height: <= MaxWidth })
                {
                    return result;
                }

                limit = "tree extent in pixels";
                actual = (long)Math.Ceiling(Math.Max(result.Width, result.Height));
                maximum = (long)MaxWidth;
            }

            throw new DiagramTooLargeException(limit, actual, maximum,
                "The tree cannot be laid out legibly even collapsed to one level; prune it first.");
        }

        private const double MaxWidth = 3800;

        private static int Inspect(TreeNode root)
        {
            var count = 0;
            var stack = new Stack<TreeNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                count++;

                if (count > DiagramLimits.MaxTreeInputNodes)
                {
                    throw new DiagramTooLargeException("tree nodes", count, DiagramLimits.MaxTreeInputNodes,
                        "Draw a pruned tree or a summary.");
                }

                if (node.Branches.Count > DiagramLimits.MaxTreeBranches)
                {
                    throw new DiagramNotSupportedException("a split with " + node.Branches.Count +
                                                           " branches cannot be drawn legibly (maximum " +
                                                           DiagramLimits.MaxTreeBranches + ").");
                }

                foreach (var b in node.Branches)
                {
                    stack.Push(b.Node);
                }
            }

            return count;
        }

        private static bool Build(TreeDiagramSpec spec, bool compact, TreeNode node, int parent, int depth,
            int maxDepth,
            List<PlacedTreeNode> nodes, List<PlacedTreeEdge> edges)
        {
            if (nodes.Count >= DiagramLimits.MaxTreeDrawnNodes)
            {
                return false;
            }

            var index = nodes.Count;
            var collapsed = !node.IsLeaf && depth >= maxDepth;
            var kind = node.IsLeaf ? TreeNodeKind.Leaf : collapsed ? TreeNodeKind.Collapsed : TreeNodeKind.Split;
            var (w, h) = Size(spec, compact, node, kind);
            var hiddenNodes = 0;
            var hiddenLeaves = 0;

            if (collapsed)
            {
                (hiddenNodes, hiddenLeaves) = Count(node);
            }

            nodes.Add(new PlacedTreeNode(index, parent, depth, kind, node, 0, 0, w, h, hiddenNodes, hiddenLeaves,
                node.Samples ?? 0));

            if (collapsed)
            {
                return true;
            }

            foreach (var branch in node.Branches)
            {
                var child = nodes.Count;
                var rootSamples = spec.Root!.Samples;

                var fraction = rootSamples is > 0 && branch.Node.Samples is { } s
                    ? Math.Clamp(s / rootSamples.Value, 0, 1)
                    : double.NaN;

                edges.Add(new PlacedTreeEdge(index, child, branch.Label, fraction));

                if (!Build(spec, compact, branch.Node, index, depth + 1, maxDepth, nodes, edges))
                {
                    return false;
                }
            }

            return true;
        }

        private static (int Nodes, int Leaves) Count(TreeNode root)
        {
            var nodes = 0;
            var leaves = 0;
            var stack = new Stack<TreeNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var n = stack.Pop();
                nodes++;

                if (n.IsLeaf)
                {
                    leaves++;
                }

                foreach (var b in n.Branches)
                {
                    stack.Push(b.Node);
                }
            }

            return (nodes, leaves);
        }

        private static (double W, double H) Size(TreeDiagramSpec spec, bool compact, TreeNode node, TreeNodeKind kind)
        {
            if (compact)
            {
                return kind == TreeNodeKind.Collapsed ? (16, 14) : kind == TreeNodeKind.Leaf ? (13, 13) : (14, 14);
            }

            if (kind == TreeNodeKind.Collapsed)
            {
                return (64, 48);
            }

            var line1 = kind == TreeNodeKind.Leaf ? TreeText.Prediction(node, spec) : TreeText.Condition(node);
            var line2 = TreeText.Stats(node, spec);
            var width = Math.Max(TextMetrics.Width(line1, 11, true), TextMetrics.Width(line2, 10.5)) + 18;

            return (Math.Clamp(width, 92, 230), 46);
        }

        private static TreeLayoutResult Arrange(TreeDiagramSpec spec, bool compact, List<PlacedTreeNode> nodes,
            List<PlacedTreeEdge> edges, int depth, int total)
        {
            var top = Orient(compact, nodes, edges, TreeOrientation.TopDown, depth, total);

            if (spec.Orientation == TreeOrientation.TopDown)
            {
                return top;
            }

            if (spec.Orientation == TreeOrientation.Auto)
            {
                return top.Width <= 1600 || compact
                    ? top
                    : Orient(compact, nodes, edges, TreeOrientation.LeftRight, depth, total);
            }

            return Orient(compact, nodes, edges, TreeOrientation.LeftRight, depth, total);
        }

        private static TreeLayoutResult Orient(bool compact, List<PlacedTreeNode> nodes,
            List<PlacedTreeEdge> edges, TreeOrientation orientation, int depth, int total)
        {
            var horizontal = orientation == TreeOrientation.LeftRight;
            var n = nodes.Count;
            var children = new List<int>[n];

            for (var i = 0; i < n; i++)
            {
                children[i] = [];
            }

            foreach (var e in edges)
            {
                children[e.From].Add(e.To);
            }

            var breadth = nodes.Select(p => horizontal ? p.Height : p.Width).ToArray();
            var levelSize = new double[depth + 2];

            foreach (var p in nodes)
            {
                levelSize[p.Depth] = Math.Max(levelSize[p.Depth], horizontal ? p.Width : p.Height);
            }

            var gap = compact ? 5.0 : horizontal ? 10.0 : 14.0;
            var offset = new double[n];
            Place(0, children, breadth, gap, offset);
            var centre = new double[n];
            Absolute(0, 0, children, offset, centre);
            var min = double.PositiveInfinity;
            var max = double.NegativeInfinity;

            for (var i = 0; i < n; i++)
            {
                min = Math.Min(min, centre[i] - breadth[i] / 2);
                max = Math.Max(max, centre[i] + breadth[i] / 2);
            }

            var levelGap = compact ? 22.0 : horizontal ? 74.0 : 52.0;
            var levelStart = new double[depth + 2];
            var pos = 0.0;

            for (var d = 0; d <= depth; d++)
            {
                levelStart[d] = pos;
                pos += levelSize[d] + levelGap;
            }

            var depthExtent = pos - levelGap;
            var placed = new List<PlacedTreeNode>(n);

            foreach (var p in nodes)
            {
                var b = centre[p.Index] - min;
                var d = levelStart[p.Depth] + levelSize[p.Depth] / 2;

                placed.Add(horizontal
                    ? p with { X = d, Y = b, Width = p.Width, Height = p.Height }
                    : p with { X = b, Y = d });
            }

            var breadthExtent = max - min;

            return new TreeLayoutResult(placed, edges, horizontal ? depthExtent : breadthExtent,
                horizontal ? breadthExtent : depthExtent, orientation, depth, total, compact);
        }

        private static List<(double L, double R)> Place(int i, List<int>[] children, double[] breadth, double gap,
            double[] offset)
        {
            var kids = children[i];
            var own = (-breadth[i] / 2, breadth[i] / 2);

            if (kids.Count == 0)
            {
                return [own];
            }

            var accumulated = new List<(double L, double R)>();
            var positions = new double[kids.Count];

            for (var k = 0; k < kids.Count; k++)
            {
                var c = Place(kids[k], children, breadth, gap, offset);

                if (k == 0)
                {
                    positions[0] = 0;
                    accumulated = [.. c];
                    continue;
                }

                var shift = double.NegativeInfinity;

                for (var d = 0; d < Math.Min(accumulated.Count, c.Count); d++)
                {
                    shift = Math.Max(shift, accumulated[d].R + gap - c[d].L);
                }

                positions[k] = shift;

                for (var d = 0; d < c.Count; d++)
                {
                    var l = c[d].L + shift;
                    var r = c[d].R + shift;

                    if (d < accumulated.Count)
                    {
                        accumulated[d] = (Math.Min(accumulated[d].L, l), Math.Max(accumulated[d].R, r));
                    }
                    else
                    {
                        accumulated.Add((l, r));
                    }
                }
            }

            var centre = (positions[0] + positions[^1]) / 2;

            for (var k = 0; k < kids.Count; k++)
            {
                offset[kids[k]] = positions[k] - centre;
            }

            var contour = new List<(double L, double R)>(accumulated.Count + 1) { own };

            foreach (var (l, r) in accumulated)
            {
                contour.Add((l - centre, r - centre));
            }

            return contour;
        }

        private static void Absolute(int i, double parentCentre, List<int>[] children, double[] offset, double[] centre)
        {
            centre[i] = parentCentre + offset[i];

            foreach (var c in children[i])
            {
                Absolute(c, centre[i], children, offset, centre);
            }
        }
    }
}