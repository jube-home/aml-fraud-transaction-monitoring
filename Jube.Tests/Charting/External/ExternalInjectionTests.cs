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
using Jube.Charting;
using Jube.Test.Charting.Diagrams;
using Jube.Test.Charting.More;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.External
{
    public class ExternalInjectionTests
    {
        public static IEnumerable<object[]> Hostile()
        {
            return InjectionTests.Hostile();
        }

        [Theory]
        [MemberData(nameof(Hostile))]
        public void HostileTextNeverProducesMarkupInExternalMode(string text)
        {
            var charts = InjectionTests.Charts(text)
                .Concat(DiagramInjectionTests.Diagrams(text))
                .Concat(MoreInjectionTests.Charts(text));

            foreach (var chart in charts)
            {
                var svg = ExternalSamples.ToExternal(chart).Render();
                ExternalStyleTests.AssertExternalSafe(svg, false);
                svg.Length.Should().BeLessThan(2_000_000);
            }
        }

        [Fact]
        public void HostileIdsAreStillRejectedInExternalMode()
        {
            var act = () => Chart.Line().Series("s", [1.0, 2]).Id("a\"><script>").ExternalStyles().Render();
            act.Should().Throw<ChartSpecificationException>();
        }

        [Theory]
        [InlineData("url(#x)")]
        [InlineData("#fff;} </style><script>")]
        [InlineData("expression(alert(1))")]
        [InlineData("red")]
        public void InvalidColoursAreRejectedInExternalMode(string colour)
        {
            var act = () => Chart.Line().Series("s", [1.0, 2], colour).ExternalStyles().Render();
            act.Should().Throw<ChartSpecificationException>();
        }
    }
}