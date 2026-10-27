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
using System.Text.RegularExpressions;
using System.Xml;
using FluentAssertions;
using Jube.Dto.Payload;
using Jube.Service.Charts;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class WatcherSummaryChartRendererTests
    {
        private static WatcherSummaryChartRequest Request(params WatcherSummaryRule[] rules)
        {
            return new WatcherSummaryChartRequest(false, null, null, rules);
        }

        [Fact]
        public void EachRuleGetsAColumnAndTheElevationIsALineOnItsOwnAxis()
        {
            var svg = WatcherSummaryChartRenderer.Render(Request(new WatcherSummaryRule("High value", 5, 12.5),
                new WatcherSummaryRule("Velocity", 2, 3)));

            new XmlDocument { XmlResolver = null }.Invoking(d => d.LoadXml(svg)).Should().NotThrow();
            svg.Should().Contain("High value").And.Contain("Velocity");
            svg.Should().Contain("Aggregate response elevation");
        }

        [Fact]
        public void NoRulesTooManyRulesOrNonFiniteFiguresAreRefused()
        {
            var tooMany = Enumerable.Range(0, WatcherSummaryChartRenderer.MaximumRules + 1)
                .Select(i => new WatcherSummaryRule("r" + i, 1, 1)).ToArray();

            var none = () => WatcherSummaryChartRenderer.Render(Request());
            var many = () => WatcherSummaryChartRenderer.Render(Request(tooMany));
            var infinite = () =>
                WatcherSummaryChartRenderer.Render(Request(new WatcherSummaryRule("x", 1, double.NaN)));

            none.Should().Throw<ArgumentException>();
            many.Should().Throw<ArgumentException>();
            infinite.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ColoursAreTheCallersAndAHostileNameIsEscaped()
        {
            var request = new WatcherSummaryChartRequest(true, "#cc6e38", "#ec5e0a",
                [new WatcherSummaryRule("<script>alert(1)</script>", 1, 1)]);

            var svg = WatcherSummaryChartRenderer.Render(request);

            svg.Should().ContainEquivalentOf("#cc6e38");
            svg.Should().NotContainEquivalentOf("<script");
            Regex.IsMatch(svg, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase).Should().BeFalse();

            var bad = () => WatcherSummaryChartRenderer.Render(new WatcherSummaryChartRequest(false, "red",
                null, [new WatcherSummaryRule("x", 1, 1)]));

            bad.Should().Throw<ArgumentException>();
        }
    }
}
