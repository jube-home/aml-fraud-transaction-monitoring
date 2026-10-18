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
using Jube.Charting.Theme;
using Xunit;

namespace Jube.Test.Charting
{
    public class ThemeTests
    {
        [Theory]
        [InlineData("light")]
        [InlineData("dark")]
        public void TextTokensMeetContrastOnTheirSurfaces(string mode)
        {
            var v = mode == "light" ? Palette.Light : Palette.Dark;
            Contrast.Ratio(v.Text, v.Surface).Should().BeGreaterOrEqualTo(7);
            Contrast.Ratio(v.TextSecondary, v.Surface).Should().BeGreaterOrEqualTo(4.5);
            Contrast.Ratio(v.TextMuted, v.Surface).Should().BeGreaterOrEqualTo(4.5);
            Contrast.Ratio(v.Text, v.Tooltip).Should().BeGreaterOrEqualTo(7);
            Contrast.Ratio(v.TextMuted, v.Tooltip).Should().BeGreaterOrEqualTo(4.5);
        }

        [Theory]
        [InlineData("light")]
        [InlineData("dark")]
        public void OnColourTextMeetsContrastOnEveryRampCell(string mode)
        {
            var v = mode == "light" ? Palette.Light : Palette.Dark;

            foreach (var fill in v.Sequential.Concat(v.Diverging).Concat(v.Categorical))
            {
                Contrast.Ratio(v.OnColor(fill), fill).Should().BeGreaterOrEqualTo(3.5, fill);
            }
        }

        [Theory]
        [InlineData("light")]
        [InlineData("dark")]
        public void CategoricalSeriesAreVisibleAgainstTheSurface(string mode)
        {
            var v = mode == "light" ? Palette.Light : Palette.Dark;

            foreach (var colour in v.Categorical)
            {
                Contrast.Ratio(colour, v.Surface).Should().BeGreaterOrEqualTo(2.0, colour);
            }
        }

        [Fact]
        public void PalettesHaveTheDocumentedShape()
        {
            Palette.Light.Categorical.Should().HaveCount(8);
            Palette.Dark.Categorical.Should().HaveCount(8);
            Palette.Light.Sequential.Should().HaveCount(9);
            Palette.Light.Diverging.Should().HaveCount(11);
            Palette.Light.Diverging[5].Should().Be("#f0efec");
            Palette.Dark.Diverging[5].Should().Be("#383835");
        }

        [Fact]
        public void SequentialRampsAreMonotonicInLightness()
        {
            var light = Palette.Light.Sequential.Select(c => Rgb.Parse(c).Luminance()).ToArray();

            for (var i = 1; i < light.Length; i++)
            {
                light[i].Should().BeLessThan(light[i - 1]);
            }

            var dark = Palette.Dark.Sequential.Select(c => Rgb.Parse(c).Luminance()).ToArray();

            for (var i = 1; i < dark.Length; i++)
            {
                dark[i].Should().BeGreaterThan(dark[i - 1]);
            }
        }

        [Fact]
        public void DivergingArmsMeetInANeutralMidpoint()
        {
            foreach (var v in new[] { Palette.Light, Palette.Dark })
            {
                var mid = Rgb.Parse(v.Diverging[5]);
                (mid.R == mid.G || Math.Abs(mid.R - mid.G) < 6).Should().BeTrue();
                Rgb.Parse(v.Diverging[0]).B.Should().BeGreaterThan(Rgb.Parse(v.Diverging[0]).R);
                Rgb.Parse(v.Diverging[10]).R.Should().BeGreaterThan(Rgb.Parse(v.Diverging[10]).B);
            }
        }

        [Fact]
        public void ColourParsingAcceptsOnlyHex()
        {
            Rgb.TryParse("#a1b2c3", out _).Should().BeTrue();
            Rgb.TryParse("#abc", out var c).Should().BeTrue();
            c.Hex.Should().Be("#aabbcc");
            Rgb.TryParse("blue", out _).Should().BeFalse();
            Rgb.TryParse("#12", out _).Should().BeFalse();
            Rgb.TryParse(null, out _).Should().BeFalse();
        }
    }
}