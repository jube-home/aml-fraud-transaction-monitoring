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
using Jube.AskJooby.Validation.Models;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ResponseValidatorTests
    {
        [Fact]
        public void IsApprovedWhenStatusIsPassed()
        {
            var result = ResponseValidator.ParseResult("STATUS: PASSED\nREASON:");

            result.Approved.Should().BeTrue();
            result.Feedback.Should().BeNull();
        }

        [Fact]
        public void TrimsAndIgnoresCaseWhenMatchingPassed()
        {
            var result = ResponseValidator.ParseResult("  status: passed  \nreason:  \n");

            result.Approved.Should().BeTrue();
        }

        [Fact]
        public void ReturnsTheReasonAsFeedbackWhenStatusIsFailed()
        {
            var result = ResponseValidator.ParseResult(
                "STATUS: FAILED\nREASON: The reply claims EnableMigration defaults to False, which no Bite backs up.");

            result.Approved.Should().BeFalse();
            result.Feedback.Should().Contain("EnableMigration");
        }

        [Fact]
        public void TreatsAnUnparseableResponseAsFailedUsingTheRawTextAsFeedback()
        {
            var result = ResponseValidator.ParseResult("The reply looks fine to me.");

            result.Approved.Should().BeFalse();
            result.Feedback.Should().Contain("looks fine to me");
        }

        [Fact]
        public void ParsesKindOmissionWhenTheValidatorFoundNothingEitherWay()
        {
            var result = ResponseValidator.ParseResult(
                "STATUS: FAILED\nREASON: No Bite confirms or contradicts this.\nKIND: OMISSION");

            result.Approved.Should().BeFalse();
            result.FailureKind.Should().Be(ValidationFailureKind.Omission);
            result.Feedback.Should().Be("No Bite confirms or contradicts this.");
        }

        [Fact]
        public void ParsesKindErrorWhenABiteActivelyContradictsTheClaim()
        {
            var result = ResponseValidator.ParseResult(
                "STATUS: FAILED\nREASON: A Bite says the default is True, not False.\nKIND: ERROR");

            result.FailureKind.Should().Be(ValidationFailureKind.Error);
        }

        [Fact]
        public void DefaultsToErrorWhenKindIsMissingOrUnparseable()
        {
            var result = ResponseValidator.ParseResult("STATUS: FAILED\nREASON: Something is wrong.");

            result.FailureKind.Should().Be(ValidationFailureKind.Error,
                "an unparseable KIND must still get the existing revise-and-recheck treatment, " +
                "never silently skip a fix that might have worked");
        }

        [Fact]
        public void ApprovedResultsHaveNoFailureKind()
        {
            var result = ResponseValidator.ParseResult("STATUS: PASSED\nREASON:");

            result.FailureKind.Should().BeNull();
        }

        [Fact]
        public void BuildTaskIncludesTheQuestionDocsAndReply()
        {
            var task = ResponseValidator.BuildTask(
                "What is EnableMigration?", "EnableMigration controls auto-migration.", "It defaults to True.");

            task.Should().Contain("What is EnableMigration?");
            task.Should().Contain("EnableMigration controls auto-migration.");
            task.Should().Contain("It defaults to True.");
        }

        [Fact]
        public void BuildTaskTellsTheValidatorToSearchRatherThanTrustTheSuppliedDocsAlone()
        {
            var task = ResponseValidator.BuildTask("q", "docs", "reply");

            task.Should().Contain("search the Pasture yourself");
        }

        [Fact]
        public void BuildTaskFlagsEverythingAsUnconfirmedWhenNoDocsWereEverRetrieved()
        {
            var task = ResponseValidator.BuildTask("q", string.Empty, "reply invented from thin air");

            task.Should().Contain("without a single Pasture search");
            task.Should().Contain("reply invented from thin air");
        }

        [Fact]
        public void BuildTaskTellsTheValidatorCodeOutranksDocsOnConflict()
        {
            var task = ResponseValidator.BuildTask("q", "docs", "reply");

            task.Should().Contain("trust the code");
        }

        [Fact]
        public void BuildTaskAsksForAKindFieldDistinguishingOmissionFromError()
        {
            var task = ResponseValidator.BuildTask("q", "docs", "reply");

            task.Should().Contain("KIND:");
            task.Should().Contain("OMISSION");
            task.Should().Contain("ERROR");
        }

        [Fact]
        public void BuildTaskTellsTheValidatorNotToKeepRephrasingTheSameSearch()
        {
            var task = ResponseValidator.BuildTask("q", "docs", "reply");

            task.Should().Contain("two or three searches");
        }
    }
}