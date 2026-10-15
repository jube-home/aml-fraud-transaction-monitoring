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

using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Jube.AskJooby.Tools.Diff;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class DiffToolTests
    {
        [Fact]
        public void IdenticalInputsReturnAnEmptyArray()
        {
            var result = DiffTool.DiffText("same\ntext\nhere", "same\ntext\nhere");

            using var document = JsonDocument.Parse(result);
            document.RootElement.GetArrayLength().Should().Be(0);
        }

        [Fact]
        public void AChangedLineIsReportedAsReplacedWithBothLineNumbersAndBothTexts()
        {
            var result = DiffTool.DiffText("a\nb\nc", "a\nB CHANGED\nc");

            using var document = JsonDocument.Parse(result);
            document.RootElement.GetArrayLength().Should().Be(1);

            var hunk = document.RootElement[0];
            hunk.GetProperty("type").GetString().Should().Be("replaced");
            hunk.GetProperty("lineOld").GetInt32().Should().Be(2);
            hunk.GetProperty("lineNew").GetInt32().Should().Be(2);
            hunk.GetProperty("textOld").GetString().Should().Be("b");
            hunk.GetProperty("textNew").GetString().Should().Be("B CHANGED");
        }

        [Fact]
        public void APureDeletionHasNoNewSideAndNoLineNew()
        {
            var result = DiffTool.DiffText("keep\ngone\nkeep too", "keep\nkeep too");

            using var document = JsonDocument.Parse(result);
            document.RootElement.GetArrayLength().Should().Be(1);

            var hunk = document.RootElement[0];
            hunk.GetProperty("type").GetString().Should().Be("deleted");
            hunk.GetProperty("lineOld").GetInt32().Should().Be(2);
            hunk.GetProperty("textOld").GetString().Should().Be("gone");

            hunk.TryGetProperty("lineNew", out _).Should().BeFalse(
                "null fields are omitted entirely, not serialised as null, to keep the array cheap to read");

            hunk.TryGetProperty("textNew", out _).Should().BeFalse();
        }

        [Fact]
        public void APureInsertionHasNoOldSideAndNoLineOld()
        {
            var result = DiffTool.DiffText("keep\nkeep too", "keep\nnew line\nkeep too");

            using var document = JsonDocument.Parse(result);
            document.RootElement.GetArrayLength().Should().Be(1);

            var hunk = document.RootElement[0];
            hunk.GetProperty("type").GetString().Should().Be("inserted");
            hunk.GetProperty("lineNew").GetInt32().Should().Be(2);
            hunk.GetProperty("textNew").GetString().Should().Be("new line");
            hunk.TryGetProperty("lineOld", out _).Should().BeFalse();
            hunk.TryGetProperty("textOld", out _).Should().BeFalse();
        }

        [Fact]
        public void AnUnequalSizedChangedBlockPairsPositionallyThenSpillsIntoInserts()
        {
            var result = DiffTool.DiffText("a\nb\nc\nd", "a\nX\nY\nZ\nd");

            using var document = JsonDocument.Parse(result);
            var hunks = document.RootElement.EnumerateArray().ToList();

            hunks.Should().HaveCount(3);
            hunks[0].GetProperty("type").GetString().Should().Be("replaced");
            hunks[0].GetProperty("textOld").GetString().Should().Be("b");
            hunks[0].GetProperty("textNew").GetString().Should().Be("X");
            hunks[1].GetProperty("type").GetString().Should().Be("replaced");
            hunks[1].GetProperty("textOld").GetString().Should().Be("c");
            hunks[1].GetProperty("textNew").GetString().Should().Be("Y");
            hunks[2].GetProperty("type").GetString().Should().Be("inserted");
            hunks[2].GetProperty("textNew").GetString().Should().Be("Z");
        }

        [Fact]
        public void ASingleTrailingNewlineOnOneSideIsNotReportedAsAChange()
        {
            var result = DiffTool.DiffText("one\ntwo\n", "one\ntwo");

            using var document = JsonDocument.Parse(result);

            document.RootElement.GetArrayLength().Should().Be(0,
                "a trailing newline is the file terminator, not an extra blank line -- both inputs " +
                "describe the same two lines");
        }

        [Fact]
        public void ASecondTrailingNewlineIsStillARealBlankLineAndIsReported()
        {
            var result = DiffTool.DiffText("one\ntwo", "one\ntwo\n\n");

            using var document = JsonDocument.Parse(result);

            document.RootElement.GetArrayLength().Should().BeGreaterThan(0,
                "only exactly one trailing newline is stripped as the terminator -- a second one is " +
                "a genuine blank line that was added");
        }

        [Fact]
        public void ComparisonIsCaseAndWhitespaceExact()
        {
            var result = DiffTool.DiffText("Line", "line");

            using var document = JsonDocument.Parse(result);

            document.RootElement.GetArrayLength().Should().Be(1,
                "case is not ignored -- a diff tool that silently treated 'Line' and 'line' as the " +
                "same would hide a real change");
        }

        [Fact]
        public void OversizedInputReturnsAPlainRefusalNotJson()
        {
            var huge = new string('x', 200_001);

            var result = DiffTool.DiffText(huge, "short");

            result.Should().Contain("too large to diff");

            Action act = () => JsonDocument.Parse(result);
            act.Should().Throw<JsonException>();
        }

        [Fact]
        public void NullInputsAreTreatedAsEmptyRatherThanThrowing()
        {
            var result = DiffTool.DiffText(null!, "one line");

            using var document = JsonDocument.Parse(result);
            document.RootElement.GetArrayLength().Should().Be(1);
            document.RootElement[0].GetProperty("type").GetString().Should().Be("inserted");
        }

        [Fact]
        public void MoreChangedLinesThanTheCapEndsWithATruncatedEntryInsteadOfGoingOnForever()
        {
            var oldLines = Enumerable.Range(0, 600).Select(i => $"old-{i}");
            var newLines = Enumerable.Range(0, 600).Select(i => $"new-{i}");

            var result = DiffTool.DiffText(string.Join('\n', oldLines), string.Join('\n', newLines));

            using var document = JsonDocument.Parse(result);
            var hunks = document.RootElement.EnumerateArray().ToList();

            hunks.Last().GetProperty("type").GetString().Should().Be("truncated");
            hunks.Last().GetProperty("message").GetString().Should().Contain("Stopped after");
        }
    }
}