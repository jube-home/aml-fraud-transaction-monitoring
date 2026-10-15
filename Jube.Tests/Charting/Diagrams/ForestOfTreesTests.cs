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

using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Jube.Charting.Diagrams;
using Xunit;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Diagrams
{
    public class ForestOfTreesTests
    {
        [Fact]
        public void OnlyTheFirstTreesAreDrawnInCompactMode()
        {
            var layouts = DiagramSamples.Forest(6, 4).Build().Layouts();
            layouts.Should().HaveCount(6);
            layouts.Should().OnlyContain(l => l.Compact && l.DepthShown <= 4);
            DiagramSamples.Forest(3).Build().Layouts().Should().HaveCount(3);
            var capped = DiagramSamples.Forest(6).MaxTrees(2).Build().Layouts();
            capped.Should().HaveCount(2);
        }

        [Fact]
        public void EachTreeGetsItsOwnPanelAndHoverScope()
        {
            var svg = DiagramSamples.Forest().Render();
            SvgAssert.IsSafe(svg, false);
            svg.Should().Contain(">Tree 1<").And.Contain(">Tree 4<").And.NotContain(">Tree 5<");
            svg.Should().Contain("nd tr0 ").And.Contain("nd tr3 ").And.NotContain("nd tr4 ");
            var layouts = DiagramSamples.Forest().Build().Layouts();
            Regex.Matches(svg, "class=\"tp tn\\d+\"").Count.Should().Be(layouts.Sum(l => l.Nodes.Count));
            Regex.Matches(svg, "class=\"tp tx\\d+\"").Count.Should().Be(layouts.Sum(l => l.Edges.Count));
            svg.Should().Contain(".nd.tr0:not(.n0)").And.Contain(".nd.tr1:not(.n" + layouts[0].Nodes.Count + ")");
        }

        [Fact]
        public void ImportanceBarsAreSortedAndProportional()
        {
            var svg = DiagramSamples.Forest().Render();
            var doc = XDocument.Parse(svg);

            var widths = doc.Descendants()
                .Where(e => e.Name.LocalName == "rect" && (string)e.Attribute("class")! == "rb")
                .Select(e =>
                    double.Parse((string)e.Attribute("width")!, CultureInfo.InvariantCulture))
                .ToArray();

            widths.Should().HaveCount(8).And.BeInDescendingOrder();
            (widths[1] / widths[0]).Should().BeApproximately(0.22 / 0.31, 0.01);
            svg.Should().Contain("first 4 of 100 trees drawn").And.Contain("Mean decrease in impurity");
        }

        [Fact]
        public void ImportancesBeyondTheCapAreTruncatedToTheLargest()
        {
            var b = DiagramSamples.Forest();

            for (var i = 0; i < 30; i++)
            {
                b.Importance("extra" + i, 0.001 * i);
            }

            Regex.Matches(b.Render(), "class=\"rb\"").Count.Should().Be(12);
        }

        [Fact]
        public void ForestRenderingIsDeterministic()
        {
            DiagramSamples.Forest().Render().Should().Be(DiagramSamples.Forest().Render());
        }

        [Fact]
        public void InvalidForestsAreRejected()
        {
            Diagram.Forest().Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();

            DiagramSamples.Forest().MaxTrees(99).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();
            DiagramSamples.Forest().TreeDepth(0).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            DiagramSamples.Forest().Importance("bad", -1).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            DiagramSamples.Forest().Classes(Enumerable.Range(0, 9).Select(i => "c" + i)).Invoking(b => b.Render())
                .Should().Throw<DiagramNotSupportedException>();
        }
    }
}