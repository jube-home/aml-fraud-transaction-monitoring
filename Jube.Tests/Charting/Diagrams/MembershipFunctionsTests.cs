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
    public class MembershipFunctionsTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(2.5, 0.5)]
        [InlineData(5, 1)]
        [InlineData(7.5, 0.5)]
        [InlineData(10, 0)]
        [InlineData(-3, 0)]
        [InlineData(12, 0)]
        public void TriangleMembershipMatchesTheDefinition(double x, double expected)
        {
            FuzzySet.Triangular("t", 0, 5, 10).Membership(x).Should().BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void ShouldersAndOtherShapesEvaluateCorrectly()
        {
            var trap = FuzzySet.Trapezoidal("t", 0, 0, 300, 1200);
            trap.Membership(0).Should().Be(1);
            trap.Membership(300).Should().Be(1);
            trap.Membership(750).Should().BeApproximately(0.5, 1e-9);
            trap.Membership(1201).Should().Be(0);
            FuzzySet.Triangular("t", 0, 0, 10).Membership(0).Should().Be(1);
            FuzzySet.Triangular("t", 0, 10, 10).Membership(10).Should().Be(1);
            FuzzySet.Gaussian("g", 3, 2).Membership(3).Should().Be(1);
            FuzzySet.Gaussian("g", 3, 2).Membership(5).Should().BeApproximately(Math.Exp(-0.5), 1e-9);
            FuzzySet.Sigmoid("s", 4, 2).Membership(4).Should().BeApproximately(0.5, 1e-9);
            FuzzySet.Sigmoid("s", 4, 2).Membership(10).Should().BeGreaterThan(0.99);
            var points = FuzzySet.Points("p", 0, 0, 10, 1, 20, 0.2);
            points.Membership(5).Should().BeApproximately(0.5, 1e-9);
            points.Membership(15).Should().BeApproximately(0.6, 1e-9);
            points.Membership(21).Should().Be(0);
        }

        [Fact]
        public void EverySetIsDrawnWithFuzzificationColumnsAndRuleLinks()
        {
            var svg = DiagramSamples.Fuzzy().Render();
            SvgAssert.IsSafe(svg, false);
            svg.Should().Contain("amount = 104.2").And.Contain("low: 1").And.Contain("medium: 0");
            svg.Should().Contain("IF amount is high AND velocity is fast THEN risk is high");
            svg.Should().Contain("OR velocity is normal").And.Contain("(weight 0.8)");
            svg.Should().Contain(".rl.q0:hover").And.Contain(":has(.pv0 .fz:hover) .pv0 .fz:not(:hover)");
            svg.Should().Contain("fz pv0 s0 u3").And.Contain("fz pv2 s2 u0");
            svg.Should().Contain("Trapezoidal(0, 0, 300, 1200)").And.Contain("Used by 1 rule");
        }

        [Fact]
        public void DeterministicOutput()
        {
            DiagramSamples.Fuzzy().Render().Should().Be(DiagramSamples.Fuzzy().Render());
        }

        [Fact]
        public void InvalidParametersAreSpecificationErrors()
        {
            Action<FuzzySet> render = set => Diagram.Fuzzy().Variable("v", 0, 10, set).Render();

            foreach (var set in new[]
                     {
                         FuzzySet.Triangular("t", 5, 1, 10), FuzzySet.Trapezoidal("t", 0, 5, 3, 10),
                         FuzzySet.Gaussian("g", 0, 0),
                         FuzzySet.Sigmoid("s", 0, 0), FuzzySet.Points("p", 0, 0), FuzzySet.Points("p", 0, 0, 0, 1),
                         FuzzySet.Points("p", 0, 2, 1, 1), FuzzySet.Gaussian("g", double.NaN, 1),
                         new FuzzySet("bad", FuzzyShape.Triangular, [1.0, 2.0])
                     })
            {
                render.Invoking(r => r(set)).Should().Throw<ChartSpecificationException>(set.Label + set.Shape);
            }

            Diagram.Fuzzy().Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();

            Diagram.Fuzzy().Variable("v", 5, 5, FuzzySet.Gaussian("g", 5, 1)).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Fuzzy().Variable("v", 0, 5).Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void RulesMustReferToDeclaredSets()
        {
            var b = Diagram.Fuzzy().Variable("v", 0, 10, FuzzySet.Gaussian("g", 5, 1))
                .Rule(new FuzzyTerm("v", "nope"), 1, true, new FuzzyTerm("v", "g"));

            b.Invoking(x => x.Render()).Should().Throw<ChartSpecificationException>().WithMessage("*not declared*");

            var empty = Diagram.Fuzzy().Variable("v", 0, 10, FuzzySet.Gaussian("g", 5, 1))
                .Rule(new FuzzyTerm("v", "g"), 1, true);

            empty.Invoking(x => x.Render()).Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void UnknownShapesAreNotSupported()
        {
            Diagram.Fuzzy().Variable("v", 0, 10, new FuzzySet("x", (FuzzyShape)42, [1.0])).Invoking(b => b.Render())
                .Should().Throw<DiagramNotSupportedException>();
        }

        [Fact]
        public void TooManyVariablesSetsOrRulesThrowDiagramTooLarge()
        {
            var vars = Diagram.Fuzzy();

            for (var i = 0; i < 13; i++)
            {
                vars.Variable("v" + i, 0, 1, FuzzySet.Gaussian("g", 0.5, 0.1));
            }

            vars.Invoking(b => b.Render()).Should().Throw<DiagramTooLargeException>().Which.Limit.Should()
                .Be("fuzzy variables");

            var sets = Enumerable.Range(0, 9).Select(i => FuzzySet.Gaussian("g" + i, i, 1)).ToArray();

            Diagram.Fuzzy().Variable("v", 0, 10, sets).Invoking(b => b.Render()).Should()
                .Throw<DiagramTooLargeException>().Which.Limit.Should().Be("fuzzy sets per variable");

            var rules = Diagram.Fuzzy().Variable("v", 0, 10, FuzzySet.Gaussian("g", 5, 1));

            for (var i = 0; i < 31; i++)
            {
                rules.Rule(new FuzzyTerm("v", "g"), 1, true, new FuzzyTerm("v", "g"));
            }

            rules.Invoking(b => b.Render()).Should().Throw<DiagramTooLargeException>().Which.Limit.Should()
                .Be("fuzzy rules");
        }
    }
}