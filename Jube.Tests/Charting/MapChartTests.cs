/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Exceptions;
using Xunit;

namespace Jube.Test.Charting
{
    [Trait("Category", "Unit")]
    public sealed class MapChartTests
    {
        private static readonly string[] Codes = ["FRA", "DEU", "BRA", "USA", "IND"];
        private static readonly double[] Values = [1, 5, 9, 4, 7];

        [Fact]
        public void ACountryMapIsWellFormedAndSafe()
        {
            var svg = Chart.Map().Countries(Codes, Values).Render();

            SvgAssert.WellFormed(svg);
            SvgAssert.IsSafe(svg);
        }

        [Fact]
        public void EveryCountryWithAValueIsColouredByItsBinAndTheRestAreNeutral()
        {
            var svg = Chart.Map().Countries(Codes, Values).Render();

            Regex.Matches(svg, "class=\"cell m q\\d+\"").Count.Should().Be(Codes.Length);
            Regex.Matches(svg, "class=\"cell m na\"").Count.Should().BeGreaterThan(100);
        }

        [Fact]
        public void AStaticMapLabelsItsColouredCountriesBecauseItHasNoTooltips()
        {
            var svg = Chart.Map().Countries(Codes, Values).Interactive(false).Render();

            svg.Should().Contain(">France<").And.Contain(">Brazil<");
            svg.Should().NotContain("tabindex");
        }

        [Fact]
        public void AStaticMapLabelsNoMoreThanTwelveCountries()
        {
            var many = new[] { "FRA", "DEU", "BRA", "USA", "IND", "CHN", "AUS", "ZAF", "RUS", "CAN", "MEX", "ARG", "EGY", "NGA" };

            var svg = Chart.Map().Countries(many, many.Select((_, i) => (double)i)).Interactive(false).Render();

            Regex.Matches(svg, "class=\"lbl ctr mid\"").Count.Should().Be(12);
        }

        [Fact]
        public void ACountryMapNamesTheCountryAndTheValueInItsTooltip()
        {
            var svg = Chart.Map().Countries(["FRA"], [42]).Render();

            svg.Should().Contain("France").And.Contain("42");
        }

        [Theory]
        [InlineData("FRA")]
        [InlineData("fra")]
        [InlineData("FR")]
        [InlineData("France")]
        [InlineData(" france ")]
        public void ACountryIsMatchedByIsoCodeTwoLetterCodeOrNameWhateverTheCase(string code)
        {
            var svg = Chart.Map().Countries([code], [3]).Render();

            Regex.Matches(svg, "class=\"cell m q\\d+\"").Count.Should().Be(1);
        }

        [Theory]
        [InlineData("UK", "United Kingdom")]
        [InlineData("United States", "United States of America")]
        [InlineData("Czech Republic", "Czechia")]
        public void CommonAliasesFindTheirCountry(string alias, string expected)
        {
            Chart.Map().Countries([alias], [1]).Render().Should().Contain(expected);
        }

        [Fact]
        public void ACodeThatMatchesNoCountryIsLeftOutAndSaidSoInTheDescription()
        {
            var svg = Chart.Map().Countries(["FRA", "ZZZ", "Atlantis"], [1, 2, 3]).Render();

            Regex.Matches(svg, "class=\"cell m q\\d+\"").Count.Should().Be(1);
            svg.Should().Contain("2 values did not match a country");
        }

        [Fact]
        public void TheSameCountryTwiceIsRefused()
        {
            var act = () => Chart.Map().Countries(["FRA", "FR"], [1, 2]).Render();

            act.Should().Throw<ChartSpecificationException>().WithMessage("*France*more than once*");
        }

        [Fact]
        public void NonFiniteCountryValuesAreSkippedNotDrawn()
        {
            var svg = Chart.Map().Countries(["FRA", "DEU"], [double.NaN, 2]).Render();

            Regex.Matches(svg, "class=\"cell m q\\d+\"").Count.Should().Be(1);
            svg.Should().NotContain("NaN");
        }

        [Fact]
        public void PointsAreDrawnAsDotsAndSizedPointsAsBubbles()
        {
            var plain = Chart.Map().Points([51.5, 40.7], [-0.1, -74.0]).Render();
            var sized = Chart.Map().Points([51.5, 40.7], [-0.1, -74.0], [10, 40]).Render();

            Regex.Matches(plain, "class=\"pt\"").Count.Should().Be(2);
            Regex.Matches(sized, "class=\"bub\"").Count.Should().Be(2);
        }

        [Fact]
        public void TheLargerPointGetsTheLargerBubble()
        {
            var svg = Chart.Map().Points([51.5, 40.7], [-0.1, -74.0], [10, 40]).Render();

            var radii = Regex.Matches(svg, "class=\"bub\"[^>]*r=\"([\\d.]+)\"").Select(m => double.Parse(m.Groups[1].Value,
                System.Globalization.CultureInfo.InvariantCulture)).ToArray();

            radii.Should().HaveCount(2);
            radii[1].Should().BeGreaterThan(radii[0]);
        }

        [Fact]
        public void SeveralGroupsGetALegendAndOneGroupDoesNot()
        {
            var several = Chart.Map().Points([51.5, 40.7], [-0.1, -74.0], null, null, ["Offices", "Agents"]).Render();
            var one = Chart.Map().Points([51.5, 40.7], [-0.1, -74.0], null, null, ["Offices", "Offices"]).Render();

            several.Should().Contain("Offices").And.Contain("Agents");
            Regex.Matches(several, "class=\"sr s\\d\"").Count.Should().Be(2);
            one.Should().NotContain("Agents");
        }

        [Fact]
        public void APointNearAPoleOutsideTheDrawnLatitudesIsSkippedNotMisplaced()
        {
            var svg = Chart.Map().Points([-80, 10], [0, 0]).Render();

            Regex.Matches(svg, "class=\"pt\"").Count.Should().Be(1);
        }

        [Theory]
        [InlineData(91, 0)]
        [InlineData(0, 181)]
        [InlineData(double.NaN, 0)]
        public void AnImpossibleCoordinateIsRefused(double latitude, double longitude)
        {
            var act = () => Chart.Map().Point(latitude, longitude).Render();

            act.Should().Throw<ChartSpecificationException>().WithMessage("*latitude*longitude*");
        }

        [Fact]
        public void MismatchedPointColumnsAreRefused()
        {
            var act = () => Chart.Map().Points([1.0, 2.0], [1.0]);

            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void CountriesAndPointsCanShareOneMap()
        {
            var svg = Chart.Map().Countries(["FRA"], [1]).Point(48.9, 2.4, 5, "Paris").Render();

            SvgAssert.WellFormed(svg);
            svg.Should().Contain("Paris").And.Contain("France");
        }

        [Fact]
        public void TheColourBarLegendIsOnlyDrawnWhenCountriesCarryValues()
        {
            var with = Chart.Map().Countries(["FRA", "DEU"], [1, 9]).LegendTitle("Score").Render();
            var without = Chart.Map().Point(48.9, 2.4).Render();

            with.Should().Contain("Score");
            Regex.Matches(without, "class=\"q\\d+\"").Count.Should().Be(0);
        }

        [Fact]
        public void BothStyleModesAreWellFormedAndOnlyInlineCarriesItsOwnStyle()
        {
            var external = Chart.Map().Countries(Codes, Values).ExternalStyles().Render();
            var inline = Chart.Map().Countries(Codes, Values).StyleMode(ChartStyleMode.Inline).Render();

            SvgAssert.WellFormed(external);
            SvgAssert.WellFormed(inline);
            external.Should().NotContain("<style");
            inline.Should().Contain("<style");
        }

        [Fact]
        public void TheDarkThemeIsMarkedOnTheRoot()
        {
            Chart.Map().Countries(Codes, Values).Theme(ThemeMode.Dark).Render().Should().Contain("data-theme=\"dark\"");
        }

        [Fact]
        public void TheOutputIsDeterministic()
        {
            Chart.Map().Countries(Codes, Values).Render().Should().Be(Chart.Map().Countries(Codes, Values).Render());
        }

        [Fact]
        public void AMapStaysWellInsideTheOutputLimit()
        {
            Chart.Map().Countries(Codes, Values).Render().Length.Should().BeLessThan(400_000);
        }

        [Fact]
        public void HostileLabelsAndTitlesCannotInjectMarkup()
        {
            var svg = Chart.Map().Title("<script>x</script>").Point(10, 10, 5, "\"><script>alert(1)</script>", "g\"onload=\"x")
                .Countries(["FRA"], [1]).Render();

            SvgAssert.WellFormed(svg);
            svg.Should().NotContain("<script");
        }

        [Fact]
        public void TheWorldOutlineHoldsEveryExpectedCountryOnceAndDropsAntarctica()
        {
            var svg = Chart.Map().Countries(["ATA", "FRA"], [1, 2]).Render();

            Regex.Matches(svg, "class=\"cell m (na|q\\d+)\"").Count.Should().BeInRange(170, 180);
            svg.Should().Contain("1 values did not match a country");
        }
    }
}
