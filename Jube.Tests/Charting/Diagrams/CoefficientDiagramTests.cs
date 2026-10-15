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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Xunit;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Diagrams
{
    public class CoefficientDiagramTests
    {
        [Fact]
        public void RowsAreSortedByMagnitudeByDefault()
        {
            var rows = DiagramSamples.Logistic().Build().Rows(out var total);
            total.Should().Be(10);
            rows.Select(r => Math.Abs(r.Estimate)).Should().BeInDescendingOrder();
            rows[0].Name.Should().Be("amount_z");
        }

        [Fact]
        public void SortModesOrderRowsAsDeclared()
        {
            DiagramSamples.Logistic().Sort(CoefficientSort.Estimate).Build().Rows(out _).Select(r => r.Estimate)
                .Should().BeInDescendingOrder();
            DiagramSamples.Logistic().Sort(CoefficientSort.PValue).Build().Rows(out _).Select(r => r.PValue).Should()
                .BeInAscendingOrder();

            DiagramSamples.Logistic().Sort(CoefficientSort.None).Build().Rows(out _).First().Name.Should()
                .Be("amount_z");
            DiagramSamples.Logistic().Sort(CoefficientSort.None).Build().Rows(out _).Last().Name.Should()
                .Be("salary_deposit_regular");
        }

        [Fact]
        public void SignificanceComesFromPValuesOrTheIntervalOrIsAssumedWhenUnknown()
        {
            var rows = DiagramSamples.Logistic().Build().Rows(out _);
            rows.Single(r => r.Name == "night_time").Significant.Should().BeFalse();
            rows.Single(r => r.Name == "geo_mismatch").Significant.Should().BeTrue();
            var byInterval = Diagram.Coefficients().Item("a", 1, 0.5, 1.5).Item("b", 1, -0.5, 1.5).Build().Rows(out _);
            byInterval.Single(r => r.Name == "a").Significant.Should().BeTrue();
            byInterval.Single(r => r.Name == "b").Significant.Should().BeFalse();
            Diagram.Coefficients().Item("a", 1).Build().Rows(out _).Single().Significant.Should().BeTrue();
        }

        [Fact]
        public void IntervalsAreDerivedFromStandardErrors()
        {
            var row = Diagram.Coefficients().Item("a", 1.0, standardError: 0.5).Build().Rows(out _).Single();
            row.Low.Should().BeApproximately(0.02, 0.001);
            row.High.Should().BeApproximately(1.98, 0.001);
            var log = Diagram.Coefficients().LogScale().Item("a", 2.0, standardError: 0.1).Build().Rows(out _).Single();
            log.Low.Should().BeApproximately(2 * Math.Exp(-0.1959964), 1e-6);
        }

        [Fact]
        public void OddsRatioColumnExponentiatesTheCoefficients()
        {
            var svg = DiagramSamples.Logistic().Render();
            svg.Should().Contain("Odds ratio").And.Contain("4.137 [3.254, 5.259]");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void LogScaleDrawsTheReferenceAtOne()
        {
            var svg = DiagramSamples.OddsRatios().Render();
            svg.Should().Contain("log scale");
            SvgAssert.IsSafe(svg, false);
            var rows = DiagramSamples.OddsRatios().Build().Rows(out _);
            rows.Single(r => r.Name == "card_present").Positive.Should().BeFalse();
            rows.Single(r => r.Name == "amount_z").Positive.Should().BeTrue();
        }

        [Fact]
        public void FilledAndHollowMarkersFollowSignificance()
        {
            var svg = DiagramSamples.Logistic().Render();
            Regex.Matches(svg, "class=\"dmd hollow\"").Count.Should().Be(3);
            Regex.Matches(svg, "class=\"dmd\"").Count.Should().Be(7);
            svg.Should().Contain("hollow markers are not significant at p &lt; 0.05");
        }

        [Fact]
        public void MarkersAreOrderedLikeTheirEstimates()
        {
            var svg = DiagramSamples.Logistic().Sort(CoefficientSort.None).Render();
            var doc = XDocument.Parse(svg);

            var xs = doc.Descendants()
                .Where(e => e.Name.LocalName == "path" && ((string)e.Attribute("class")!).StartsWith("dmd"))
                .Select(e => double.Parse(((string)e.Attribute("d")!).Split(' ')[0].TrimStart('M'),
                    CultureInfo.InvariantCulture)).ToArray();

            var estimates = DiagramSamples.Logistic().Sort(CoefficientSort.None).Build().Rows(out _)
                .Select(r => r.Estimate).ToArray();

            xs.Should().HaveCount(estimates.Length);

            for (var i = 0; i < xs.Length; i++)
            {
                for (var j = 0; j < xs.Length; j++)
                {
                    (xs[i] < xs[j]).Should().Be(estimates[i] < estimates[j]);
                }
            }
        }

        [Fact]
        public void LargeInputsAreElidedToTheRowCapWithANote()
        {
            var b = Diagram.Coefficients().MaxRows(20);

            for (var i = 0; i < 100; i++)
            {
                b.Item("f" + i, i / 10.0);
            }

            var svg = b.Render();
            svg.Should().Contain("showing 20 of 100 coefficients");
            b.Build().Rows(out var total).Should().HaveCount(20);
            total.Should().Be(100);
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void EnormousInputThrowsDiagramTooLarge()
        {
            var b = Diagram.Coefficients();

            for (var i = 0; i < 2001; i++)
            {
                b.Item("f" + i, 1);
            }

            b.Invoking(x => x.Render()).Should().Throw<DiagramTooLargeException>().Which.Limit.Should()
                .Be("coefficients");
        }

        [Fact]
        public void MalformedSpecificationsAreRejected()
        {
            Diagram.Coefficients().Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();

            Diagram.Coefficients().Item("a", double.NaN).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Coefficients().LogScale().Item("a", -1).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Coefficients().Item("a", 1, 2, 3).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Coefficients().Item("a", 1, 0.5).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Coefficients().Item("a", 1, pValue: 1.5).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Coefficients().Item("a", 1).ConfidenceLevel(0.8).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();
            Diagram.Coefficients().Item("a", 1).MaxRows(500).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();
        }

        [Fact]
        public void HoverRowsTooltipsAndFocusAreEmitted()
        {
            var svg = DiagramSamples.Logistic().Render();
            svg.Should().Contain("class=\"tp tr0\"").And.Contain("p-value: &lt;0.001").And.Contain("Odds ratio: ");
            svg.Should().Contain(":has(.mk.r0:hover,.mk.r0:focus-visible) .tp.tr0 .tt");
            svg.Should().Contain(":has(.rw:hover) .rw:not(:hover)");
            svg.Should().Contain("tabindex=\"0\"");
            svg.Should().Contain("Not significant");
        }

        [Fact]
        public void RenderingIsDeterministic()
        {
            DiagramSamples.Logistic().Render().Should().Be(DiagramSamples.Logistic().Render());
        }
    }
}