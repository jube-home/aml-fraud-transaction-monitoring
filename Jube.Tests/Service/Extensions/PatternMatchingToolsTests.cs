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
using FluentAssertions;
using Jube.Service.Extensions.PatternMatching;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class PatternMatchingToolsTests
    {
        [Fact]
        [Trait("Covers", "RegexIsMatch")]
        [Trait("Covers", "RegexFindAll")]
        [Trait("Covers", "RegexCapture")]
        [Trait("Covers", "RegexCountMatches")]
        [Trait("Covers", "RegexReplaceAll")]
        public void RegexTools()
        {
            PatternMatchingTools.RegexIsMatch("ACME Ltd", "acme", true).Should().BeTrue();
            PatternMatchingTools.RegexFindAll("a1b22c333", "[0-9]+").Should().Equal("1", "22", "333");
            PatternMatchingTools.RegexCapture("order-12345-x", "order-([0-9]+)").Should().Be("12345");
            PatternMatchingTools.RegexCountMatches("a1b22c333", "[0-9]+").Should().Be(3);

            PatternMatchingTools.RegexReplaceAll("2026-09-23", "([0-9]+)-([0-9]+)-([0-9]+)", "$3/$2/$1").Should()
                .Be("23/09/2026");
        }

        [Fact]
        [Trait("Covers", "RegexIsMatch")]
        public void RegexToolsExplainBadAndSlowPatterns()
        {
            var invalid = () => PatternMatchingTools.RegexIsMatch("x", "([a-");
            invalid.Should().Throw<ArgumentException>().WithMessage("*not a valid regular expression*");
            var slow = () => PatternMatchingTools.RegexIsMatch(new string('a', 5000) + "!", "^(a+)+$");
            slow.Should().Throw<ArgumentException>().WithMessage("*too long*");
        }

        [Fact]
        [Trait("Covers", "EscapeForRegex")]
        [Trait("Covers", "WildcardIsMatch")]
        public void EscapingAndWildcards()
        {
            var escaped = PatternMatchingTools.EscapeForRegex("1.5*2");
            PatternMatchingTools.RegexIsMatch("x1.5*2y", escaped).Should().BeTrue();
            PatternMatchingTools.RegexIsMatch("x15552y", escaped).Should().BeFalse();
            PatternMatchingTools.WildcardIsMatch("INV-2026-0042", "INV-2026-*").Should().BeTrue();
            PatternMatchingTools.WildcardIsMatch("INV-2025-0042", "INV-2026-*").Should().BeFalse();
            PatternMatchingTools.WildcardIsMatch("A7", "?#").Should().BeTrue();
        }
    }
}