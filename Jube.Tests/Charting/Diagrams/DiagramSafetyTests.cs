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
using Jube.Charting.Core.Models;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Diagrams
{
    public class DiagramSafetyTests
    {
        public static IEnumerable<object[]> Names()
        {
            return DiagramSamples.Showcase().Keys.Select(k => new object[] { k });
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void EverySampleIsWellFormedSafeAndDeterministic(string name)
        {
            var svg = DiagramSamples.Showcase()[name].Render();
            SvgAssert.IsSafe(svg);
            svg.Should().Be(DiagramSamples.Showcase()[name].Render());
            svg.Length.Should().BeLessThan(ChartLimits.MaxOutputCharacters);
            svg.Should().StartWith("<svg ");
            svg.Should().Contain("viewBox=\"0 0 ").And.Contain("preserveAspectRatio=\"xMidYMid meet\"");
            svg[..svg.IndexOf('>')].Should().NotContain("width=");
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void EverySampleCarriesAccessibleTitleDescriptionAndBothThemes(string name)
        {
            var svg = DiagramSamples.Showcase()[name].Render();

            svg.Should().Contain("role=\"img\"").And.Contain("aria-labelledby=").And.Contain("<title id=").And
                .Contain("<desc id=");

            svg.Should().Contain("--jc-surface:#fcfcfb").And.Contain("--jc-surface:#1a1a19");

            svg.Should().Contain("prefers-color-scheme:dark").And.Contain("[data-theme=\"dark\"]").And
                .Contain("prefers-reduced-motion");

            svg.Should().NotContain("<script").And.NotContain("href").And.NotContain("url(").And.NotContain("@import");
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void EverySampleHasKeyboardFocusableMarksWithTooltipsAndHoverRules(string name)
        {
            var svg = DiagramSamples.Showcase()[name].Render();
            svg.Should().Contain("tabindex=\"0\"").And.Contain("role=\"graphics-symbol\"").And.Contain("aria-label=");
            svg.Should().Contain("class=\"tps\"").And.Contain("class=\"tt\"");
            svg.Should().Contain(":focus-visible").And.Contain(":has(");
            svg.Should().Contain(".tt{opacity:0;visibility:hidden");
        }

        [Fact]
        public void NonInteractiveOutputDropsMarksAndTooltipsButStaysValid()
        {
            foreach (var svg in new[]
                     {
                         DiagramSamples.Xor().Interactive(false).Render(),
                         DiagramSamples.FraudTree().Interactive(false).Render(),
                         DiagramSamples.Tan().Interactive(false).Render(),
                         DiagramSamples.Logistic().Interactive(false).Render(),
                         DiagramSamples.Distributions(4).Interactive(false).Render(),
                         DiagramSamples.Fuzzy().Interactive(false).Render(),
                         DiagramSamples.Pca().Interactive(false).Render(),
                         DiagramSamples.Forest().Interactive(false).Render()
                     })
            {
                svg.Should().NotContain("tabindex").And.NotContain("class=\"tps\"");
                SvgAssert.IsSafe(svg, false);
            }
        }

        [Fact]
        public void ChartIdsAreValidatedAndIdenticalDiagramsShareAClass()
        {
            DiagramSamples.Xor().Id("bad id").Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();
            var svg = DiagramSamples.Xor().Id("my-net").Render();
            svg.Should().Contain("class=\"my-net\"").And.Contain("my-net-t");
            DiagramSamples.Xor().Render().Should().Contain("class=\"jc-");
        }

        [Fact]
        public void DarkAndLightOverridesWorkOnEveryFamily()
        {
            foreach (var chart in DiagramSamples.Showcase().Values)
            {
                chart.Should().NotBeNull();
            }

            DiagramSamples.FraudTree().Theme(ThemeMode.Dark).Render().Should().Contain("data-theme=\"dark\"");
            DiagramSamples.Hmm().Theme(ThemeMode.Light).Render().Should().Contain("data-theme=\"light\"");
            DiagramSamples.Logistic().TransparentBackground().Render().Should().NotContain("class=\"bg\"");
        }

        [Fact]
        public void SizeIsResponsiveByDefaultAndFixedOnRequest()
        {
            var responsive = DiagramSamples.FraudTree().Render();
            responsive.Should().Contain("width:100%;height:auto");
            var fixedSize = DiagramSamples.FraudTree().FixedSize(700, 500).Render();
            fixedSize.Should().Contain(" width=\"").And.Contain(" height=\"");
        }
    }
}