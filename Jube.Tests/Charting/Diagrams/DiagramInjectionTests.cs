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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Charting.Interfaces;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Xunit;

namespace Jube.Test.Charting.Diagrams
{
    public class DiagramInjectionTests
    {
        public static IEnumerable<object[]> Hostile()
        {
            return InjectionTests.Hostile();
        }

        public static IEnumerable<ISvgChart> Diagrams(string t)
        {
            var second = t + "b";

            yield return Diagram.Network().Title(t).Subtitle(t).Description(t)
                .Input([t, second], t).Hidden([t + "1", "h2"], [[1.0, -1], [0.5, 0.2]], t,
                    [0.1, t.Length > 0 ? 0.2 : 0.3], t)
                .Output([t], [[1.0, 2]], t);

            yield return Diagram.Network().Input([t]).Output([second], null);

            yield return Diagram.Tree().Title(t).Subtitle(t).Classes([t, second]).ImpurityName(t).ValueName(t).Root(
                new TreeNode
                {
                    Feature = t, Threshold = 1, Samples = 10, ClassCounts = [6, 4], Impurity = 0.4, Condition = null,
                    Branches =
                    [
                        new TreeBranch(t, TreeNode.Leaf(6, [5, 1], prediction: t)),
                        new TreeBranch(second,
                            TreeNode.SplitOn(t, [t, second], TreeNode.Leaf(2, [1, 1]), TreeNode.Leaf(2, [0, 2]), 4,
                                [1, 3]))
                    ]
                });

            yield return Diagram.Tree().Root(new TreeNode
            {
                Condition = t, Samples = 3,
                Branches = [new TreeBranch(t, TreeNode.Leaf(1, value: 2, prediction: second))]
            }).MaxDepth(1);

            yield return Diagram.Graph().Title(t).Subtitle(t).WeightName(t).Node("a", t, GraphNodeRole.Class, t)
                .Node("b", second, GraphNodeRole.Latent, t)
                .Node("c", t).Edge("a", "b", 1, label: t).Edge("b", "c", -1)
                .Edge("a", "c", 0.5, label: second);

            yield return Diagram.Graph().Title(t).Node("s1", t, GraphNodeRole.State, t)
                .Node("s2", second, GraphNodeRole.State)
                .Edge("s1", "s1", probability: 0.5, label: t).Edge("s1", "s2", probability: 0.5)
                .Edge("s2", "s1", probability: 1);

            yield return Diagram.Coefficients().Title(t).Subtitle(t).EstimateName(t).OddsRatios()
                .Item(t, 1, 0.5, 1.5, 0.01).Item(second, -1, -2, -0.5, 0.2).Item(t + "c", 0.3);

            yield return Diagram.Forest().Title(t).Classes([t, second]).ImportanceName(t).TotalTrees(5)
                .Tree(new TreeNode
                {
                    Feature = t, Threshold = 2, Samples = 4, ClassCounts = [2, 2],
                    Branches =
                    [
                        new TreeBranch("yes", TreeNode.Leaf(2, [2, 0])), new TreeBranch("no", TreeNode.Leaf(2, [0, 2]))
                    ]
                })
                .Importance(t, 0.7).Importance(second, 0.3);

            yield return Diagram.Distributions().Title(t)
                .Panel(t, DistributionSeries.Normal(t, 0, 1),
                    DistributionSeries.Histogram(second, [-1.0, 0, 1], [0.2, 0.4]))
                .Panel(second, DistributionSeries.Normal(t, 1, 1), DistributionSeries.Normal(second, 2, 1));

            yield return Diagram.Fuzzy().Title(t).Subtitle(t)
                .Variable(t, 0, 10, FuzzySet.Triangular(t, 0, 5, 10), FuzzySet.Gaussian(second, 5, 2))
                .Variable(second, 0, 1, FuzzySet.Sigmoid(t, 0.5, 9))
                .Rule(new FuzzyTerm(second, t), 0.5, false, new FuzzyTerm(t, t), new FuzzyTerm(t, second));

            yield return Diagram.Components().Title(t).Features([t, second, "c"])
                .Component(t, 0.6, [1.0, -0.5, 0.2]).Component(second, 0.3, [0.1, 0.9, -0.4]);

            yield return Diagram.Components().Title(t).Features([t, second])
                .Cluster(t, 4, [1.0, -1]).Cluster(second, 2, [-0.5, 0.5]).ValueName(t);
        }

        [Theory]
        [MemberData(nameof(Hostile))]
        public void HostileTextNeverProducesMarkup(string text)
        {
            foreach (var chart in Diagrams(text))
            {
                var svg = chart.Render();
                SvgAssert.IsSafe(svg, false);
                svg.Length.Should().BeLessThan(2_000_000);
            }
        }

        [Fact]
        public void ScriptTextAppearsOnlyEscapedInEveryDiagram()
        {
            foreach (var chart in Diagrams("<script>alert(1)</script>"))
            {
                var svg = chart.Render();
                svg.Should().Contain("&lt;script&gt;");
                svg.Should().NotContain("<script");
            }
        }

        [Fact]
        public void CdataTerminatorsAndQuotesAreEscapedInEveryDiagram()
        {
            foreach (var chart in Diagrams("a]]>b\"c'd&e"))
            {
                var svg = chart.Render();
                svg.Should().NotContain("]]>");
                svg.Should().Contain("&quot;").And.Contain("&amp;");
                SvgAssert.IsSafe(svg, false);
            }
        }

        [Fact]
        public void ControlCharactersAndBidirectionalOverridesAreRemovedInEveryDiagram()
        {
            foreach (var chart in Diagrams("a\u0000b\u0001c‮d"))
            {
                var svg = chart.Render();
                svg.Any(c => c < 0x20 && c != '\t' && c != '\n' && c != '\r').Should().BeFalse();
                svg.Should().NotContain("‮");
                svg.Should().Contain("abcd");
            }
        }

        [Theory]
        [InlineData("</style><script>alert(1)</script>")]
        [InlineData("\"><foreignObject><body onload=alert(1)></foreignObject>")]
        public void StylesheetBreakoutAttemptsCannotEscapeTheStyleElement(string text)
        {
            foreach (var chart in Diagrams(text))
            {
                var svg = chart.Render();
                var document = SvgAssert.WellFormed(svg);
                document.Descendants().Count(e => e.Name.LocalName == "style").Should().Be(1);
                svg.Should().NotContain("<script").And.NotContain("<foreignObject");
            }
        }
    }
}