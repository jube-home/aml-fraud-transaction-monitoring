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

using System;
using System.Linq;
using FluentAssertions;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Xunit;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Diagrams
{
    public class TreeDiagramTests
    {
        private static bool Overlaps(PlacedTreeNode a, PlacedTreeNode b)
        {
            return Math.Abs(a.X - b.X) < (a.Width + b.Width) / 2 - 0.001 &&
                   Math.Abs(a.Y - b.Y) < (a.Height + b.Height) / 2 - 0.001;
        }

        private static void NoOverlap(TreeLayoutResult layout)
        {
            for (var i = 0; i < layout.Nodes.Count; i++)
            {
                for (var j = i + 1; j < layout.Nodes.Count; j++)
                {
                    Overlaps(layout.Nodes[i], layout.Nodes[j]).Should().BeFalse("nodes " + i + " and " + j);
                }
            }
        }

        [Fact]
        public void BalancedBinaryTreeIsSymmetric()
        {
            var layout = DiagramSamples.Balanced(3).Orientation(TreeOrientation.TopDown).Build().Layout();
            layout.Nodes.Should().HaveCount(15);
            layout.Edges.Should().HaveCount(14);
            var root = layout.Nodes[0];

            foreach (var level in layout.Nodes.GroupBy(n => n.Depth))
            {
                var xs = level.Select(n => n.X - root.X).OrderBy(x => x).ToArray();

                for (var i = 0; i < xs.Length; i++)
                {
                    xs[i].Should().BeApproximately(-xs[xs.Length - 1 - i], 0.001);
                }
            }

            var leaves = layout.Nodes.Where(n => n.Kind == TreeNodeKind.Leaf).OrderBy(n => n.X).ToArray();
            leaves.Should().HaveCount(8);
            var gaps = leaves.Zip(leaves.Skip(1), (a, b) => b.X - a.X).ToArray();
            gaps.Max().Should().BeApproximately(gaps.Min(), 0.001);
        }

        [Fact]
        public void ParentsAreCentredOverTheirChildren()
        {
            var layout = DiagramSamples.Balanced(3).Orientation(TreeOrientation.TopDown).Build().Layout();

            foreach (var group in layout.Edges.GroupBy(e => e.From))
            {
                var kids = group.Select(e => layout.Nodes[e.To].X).ToArray();
                layout.Nodes[group.Key].X.Should().BeApproximately((kids.Min() + kids.Max()) / 2, 0.001);
            }
        }

        [Fact]
        public void LayersAreOrderedByDepthAndNodesNeverOverlap()
        {
            foreach (var builder in new[]
                     {
                         DiagramSamples.FraudTree(), DiagramSamples.DeepTree(), DiagramSamples.RegressionTree(),
                         DiagramSamples.Balanced(4)
                     })
            {
                var layout = builder.Build().Layout();
                NoOverlap(layout);

                layout.Edges.Should().OnlyContain(e => layout.Nodes[e.To].Depth == layout.Nodes[e.From].Depth + 1 &&
                                                       layout.Nodes[e.To].Parent == e.From);

                foreach (var e in layout.Edges)
                {
                    var flow = layout.Orientation == TreeOrientation.LeftRight
                        ? layout.Nodes[e.To].X - layout.Nodes[e.From].X
                        : layout.Nodes[e.To].Y - layout.Nodes[e.From].Y;
                    flow.Should().BeGreaterThan(0);
                }
            }
        }

        [Fact]
        public void LeftRightOrientationFlowsHorizontally()
        {
            var layout = DiagramSamples.Balanced(3).Orientation(TreeOrientation.LeftRight).Build().Layout();
            layout.Orientation.Should().Be(TreeOrientation.LeftRight);
            layout.Edges.Should().OnlyContain(e => layout.Nodes[e.To].X > layout.Nodes[e.From].X);
            NoOverlap(layout);
        }

        [Fact]
        public void DepthCapCollapsesSubtreesIntoCountedTriangles()
        {
            var layout = DiagramSamples.DeepTree().Build().Layout();
            layout.DepthShown.Should().Be(4);
            var collapsed = layout.Nodes.Where(n => n.Kind == TreeNodeKind.Collapsed).ToArray();
            collapsed.Should().NotBeEmpty();
            collapsed.Should().OnlyContain(n => n.Depth == 4 && n.HiddenNodes > 1 && n.HiddenLeaves >= 1);
            layout.Nodes.Should().OnlyContain(n => n.Depth <= 4);
            var svg = DiagramSamples.DeepTree().Render();
            svg.Should().Contain("class=\"tri\"").And.Contain("nodes collapsed into");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void TooManyDrawnNodesReducesTheDepthGracefully()
        {
            var layout = Diagram.Tree().Classes(["a", "b"]).Root(DiagramSamples.BalancedNode(10)).MaxDepth(10).Build()
                .Layout();

            layout.Nodes.Count.Should().BeLessThanOrEqualTo(DiagramLimits.MaxTreeDrawnNodes);
            layout.DepthShown.Should().BeLessThan(10);
            layout.TotalNodes.Should().Be(2047);
            layout.Width.Should().BeLessThan(3800);
            layout.Height.Should().BeLessThan(3800);
        }

        [Fact]
        public void EdgesCarryYesNoLabelsAndSampleFractions()
        {
            var layout = DiagramSamples.FraudTree().Build().Layout();
            layout.Edges.Select(e => e.Label).Distinct().Should().BeEquivalentTo("yes", "no");
            layout.Edges[0].Fraction.Should().BeApproximately(0.25, 0.0001);
            var svg = DiagramSamples.FraudTree().Render();
            svg.Should().Contain(">yes<").And.Contain(">no<").And.Contain("Samples: 500 (25% of root)");
            svg.Should().Contain("channel in {ecommerce, atm, wire}");
            svg.Should().Contain("amount_z ≤ 1.8");
        }

        [Fact]
        public void RegressionTreesShowValuesWithoutALegend()
        {
            var svg = DiagramSamples.RegressionTree().Render();
            svg.Should().Contain("spend 212.4").And.Contain("variance 44.1").And.NotContain("class=\"lg ");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void ASingleLeafTreeRenders()
        {
            var svg = Diagram.Tree().Root(TreeNode.Leaf(10, [7, 3])).Classes(["a", "b"]).Render();
            SvgAssert.IsSafe(svg, false);
            svg.Should().Contain("a 70%");
        }

        [Fact]
        public void HoverPathRulesTooltipsAndFocusAreEmitted()
        {
            var svg = DiagramSamples.FraudTree().Render();
            svg.Should().Contain(":has(.nd.n1:hover,.nd.n1:focus-visible)");
            svg.Should().Contain(".nd.tr0:not(.n1):not(.j1):not(.a1)");
            svg.Should().Contain("class=\"tp tn1\"").And.Contain("class=\"tp tx0\"");
            svg.Should().Contain("nd tr0 s0");
            svg.Should().Contain("tabindex=\"0\"");
            svg.Should().Contain("Impurity (gini)");
        }

        [Fact]
        public void RenderingIsDeterministic()
        {
            DiagramSamples.DeepTree().Render().Should().Be(DiagramSamples.DeepTree().Render());
        }

        [Fact]
        public void HugeInputThrowsDiagramTooLarge()
        {
            var node = TreeNode.Leaf(1);

            for (var i = 0; i < 210_000; i++)
            {
                node = new TreeNode { Feature = "f", Threshold = 1, Branches = [new TreeBranch("yes", node)] };
            }

            var act = () => Diagram.Tree().Root(node).Render();
            act.Should().Throw<DiagramTooLargeException>().Which.Limit.Should().Be("tree nodes");
        }

        [Fact]
        public void ImpossibleBranchingAndClassCountsAreNotSupported()
        {
            var wide = new TreeNode
            {
                Feature = "f",
                Branches = [.. Enumerable.Range(0, 13).Select(i => new TreeBranch("b" + i, TreeNode.Leaf(1)))]
            };

            var branches = () => Diagram.Tree().Root(wide).Render();
            branches.Should().Throw<DiagramNotSupportedException>().WithMessage("*13 branches*");

            var classes = () =>
                Diagram.Tree().Root(TreeNode.Leaf(1)).Classes(Enumerable.Range(0, 9).Select(i => "c" + i)).Render();

            classes.Should().Throw<DiagramNotSupportedException>().WithMessage("*more than 8 classes*");
        }

        [Fact]
        public void MalformedSpecificationsThrowSpecificationExceptions()
        {
            var none = () => Diagram.Tree().Render();
            none.Should().Throw<ChartSpecificationException>();
            var depth = () => Diagram.Tree().Root(TreeNode.Leaf(1)).MaxDepth(0).Render();
            depth.Should().Throw<ChartSpecificationException>();
        }
    }
}