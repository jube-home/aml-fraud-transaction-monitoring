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
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Charting;
using Xunit;

namespace Jube.Test.Charting.More
{
    public class MoreChartTests
    {
        public static IEnumerable<object[]> Names()
        {
            return MoreSamples.All().Keys.Select(k => new object[] { k });
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void EverySampleIsWellFormedAndSafe(string name)
        {
            MoreAssert.IsSafe(MoreSamples.All()[name].Render());
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void EverySampleIsDeterministic(string name)
        {
            MoreSamples.All()[name].Render().Should().Be(MoreSamples.All()[name].Render());
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void EverySampleHasHoverCssKeyboardFocusAndAria(string name)
        {
            var svg = MoreSamples.All()[name].Render();
            svg.Should().Contain(":hover");
            svg.Should().Contain("tabindex=\"0\"");
            svg.Should().Contain("role=\"img\"");
            svg.Should().Contain("aria-labelledby");
            svg.Should().Contain("prefers-color-scheme:dark");
            svg.Should().Contain("[data-theme=\"dark\"]");
            Regex.IsMatch(svg, "<title id=\"[^\"]+-t\">").Should().BeTrue();
            svg.Should().Contain("prefers-reduced-motion");
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void EverySampleRendersInBothForcedThemes(string name)
        {
            var svg = MoreSamples.All()[name].Render();
            svg.Should().NotContain("NaN");
            svg.Length.Should().BeLessThan(ChartLimits.MaxOutputCharacters);
        }
    }
}