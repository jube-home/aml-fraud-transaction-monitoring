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

using FluentAssertions;
using Jube.Charting.Text;
using Xunit;

namespace Jube.Test.Charting
{
    public class TextMetricsTests
    {
        [Fact]
        public void WidthGrowsWithLengthAndFontSize()
        {
            TextMetrics.Width("abcd", 12).Should().BeGreaterThan(TextMetrics.Width("ab", 12));
            TextMetrics.Width("abcd", 24).Should().BeApproximately(TextMetrics.Width("abcd", 12) * 2, 1e-9);
            TextMetrics.Width(string.Empty, 12).Should().Be(0);
            TextMetrics.Width(null, 12).Should().Be(0);
        }

        [Fact]
        public void NarrowGlyphsAreNarrowerThanWideOnes()
        {
            TextMetrics.Width("iiii", 12).Should().BeLessThan(TextMetrics.Width("WWWW", 12));
            TextMetrics.Width("日本", 12).Should().BeGreaterThan(TextMetrics.Width("ab", 12));
        }

        [Fact]
        public void TruncationAddsAnEllipsisAndRespectsTheWidth()
        {
            var text = "The quick brown fox jumps over the lazy dog";
            var truncated = TextMetrics.Truncate(text, 80, 12);
            truncated.Should().EndWith("…");
            TextMetrics.Width(truncated, 12).Should().BeLessOrEqualTo(80.01);
            TextMetrics.Truncate("short", 200, 12).Should().Be("short");
        }

        [Fact]
        public void TruncationNeverSplitsSurrogatePairsOrClusters()
        {
            var truncated = TextMetrics.Truncate("😀😀😀😀😀😀😀😀😀😀", 40, 12);
            truncated.Should().EndWith("…");

            foreach (var c in truncated)
            {
                if (char.IsHighSurrogate(c))
                {
                    truncated.IndexOf(c).Should().BeLessThan(truncated.Length - 2);
                }
            }

            var e = TextMetrics.Truncate("éééééééé", 30, 12);
            e.Replace("…", string.Empty).Should().NotStartWith("́");
        }

        [Fact]
        public void TinyWidthsGiveEmptyOrEllipsis()
        {
            TextMetrics.Truncate("hello", 1, 12).Length.Should().BeLessOrEqualTo(1);
        }
    }
}