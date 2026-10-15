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

using FluentAssertions;
using Jube.AskJooby.Validation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ResponseReviserTests
    {
        [Fact]
        public void BuildTaskIncludesTheQuestionDraftFeedbackAndDocs()
        {
            var task = ResponseReviser.BuildTask(
                "What is EnableMigration?", "EnableMigration controls auto-migration.",
                "It defaults to False.", "1. Backwards default claimed.");

            task.Should().Contain("What is EnableMigration?");
            task.Should().Contain("EnableMigration controls auto-migration.");
            task.Should().Contain("It defaults to False.");
            task.Should().Contain("Backwards default claimed.");
        }

        [Fact]
        public void BuildTaskTellsTheReviserToSearchRatherThanDefendTheOriginalGuess()
        {
            var task = ResponseReviser.BuildTask("q", "docs", "draft", "feedback");

            task.Should().Contain("rather than defending or rewording your original guess");
            task.Should().Contain("a genuinely different angle");
        }

        [Fact]
        public void BuildTaskAsksForSeveralDifferentSearchAttemptsRatherThanOneRepeat()
        {
            var task = ResponseReviser.BuildTask("q", "docs", "draft", "feedback");

            task.Should().Contain("two or three real attempts, not one repeat of the original query");
        }

        [Fact]
        public void BuildTaskTellsTheReviserCodeOutranksDocsOnConflict()
        {
            var task = ResponseReviser.BuildTask("q", "docs", "draft", "feedback");

            task.Should().Contain("trust the code");
        }

        [Fact]
        public void BuildTaskNotesWhenTheOriginalHadNoDocsAtAll()
        {
            var task = ResponseReviser.BuildTask("q", string.Empty, "invented draft", "feedback");

            task.Should().Contain("without a single Pasture search");
        }
    }
}