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
using System.Xml;
using FluentAssertions;
using Jube.Dto.Payload;
using Jube.Service.Charts;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class BacktestChartRendererTests
    {
        private static BacktestChartRequest Request(bool classDefined = true, params BacktestChartTag[] tags)
        {
            return new BacktestChartRequest(false, 1000, 800, 120, 100, 80, 40, 20, 660, classDefined, tags);
        }

        private static void WellFormed(string svg)
        {
            new XmlDocument { XmlResolver = null }.Invoking(d => d.LoadXml(svg)).Should().NotThrow();
        }

        [Fact]
        public void TheFunnelShowsReadKeptAndFiredAndSurvivesNothingBeingRead()
        {
            var svg = BacktestChartRenderer.Funnel(Request());

            WellFormed(svg);
            svg.Should().Contain("Read").And.Contain("Kept by the filter").And.Contain("Fired");

            var empty = BacktestChartRenderer.Funnel(new BacktestChartRequest(false, 0, 0, 0, 0, 0, 0, 0, 0, false, []));

            WellFormed(empty);
        }

        [Fact]
        public void TheConfusionMatrixNeedsAClassAndPutsTheFourCountsInTheRightCells()
        {
            var svg = BacktestChartRenderer.Confusion(Request());

            WellFormed(svg);
            svg.Should().Contain("Fired?").And.Contain("Positive?");

            new Action(() => BacktestChartRenderer.Confusion(Request(false))).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TheRatesAreComputedFromTheCountsAndOnlyTheFiredRateWithoutAClass()
        {
            var withClass = BacktestChartRenderer.Rates(Request());
            var withoutClass = BacktestChartRenderer.Rates(Request(false));

            withClass.Should().Contain("Precision, Rate").And.Contain("Recall, Rate").And.Contain("F1, Rate")
                .And.Contain("Specificity, Rate");
            withClass.Should().Contain("66.7%");
            withoutClass.Should().NotContain("Precision, Rate");
            withoutClass.Should().Contain("Fired rate, Rate");
        }

        [Fact]
        public void TheTagsAreTheLargestFirstCappedAndEmptyIsRefused()
        {
            var tags = Enumerable.Range(0, 40).Select(i => new BacktestChartTag("Tag" + i, i + 1)).ToArray();
            var svg = BacktestChartRenderer.Tags(Request(true, tags));

            svg.Should().Contain("Tag39").And.NotContain(">Tag0<");

            new Action(() => BacktestChartRenderer.Tags(Request())).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void InconsistentFiguresAreRefusedAndHostileTagNamesAreEscaped()
        {
            var inconsistent = new BacktestChartRequest(false, 10, 20, 5, 0, 0, 0, 0, 0, false, []);

            new Action(() => BacktestChartRenderer.Funnel(inconsistent)).Should().Throw<ArgumentException>();
            new Action(() => BacktestChartRenderer.Funnel(Request() with { Fired = -1 })).Should()
                .Throw<ArgumentException>();

            var hostile = BacktestChartRenderer.Tags(Request(true, new BacktestChartTag("<script>alert(1)</script>", 3)));

            hostile.Should().NotContainEquivalentOf("<script");
            WellFormed(hostile);
        }
    }
}
