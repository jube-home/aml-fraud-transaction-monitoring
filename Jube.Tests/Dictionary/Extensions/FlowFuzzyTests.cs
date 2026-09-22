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
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowFuzzyTests
    {
        private static readonly List<string> names = ["John Smith", "Maria Garcia"];

        [Fact]
        public void FuzzyBestScoreBecomesTheNumericValueOfTheFlow()
        {
            var flow = "Jon Smith".Start().FuzzyBestScore(names, "similarity");

            flow.Value.Should().BeApproximately(0.9, 0.0001);
            flow.Outcome.Should().Be(FlowOutcome.Undecided);
        }

        [Fact]
        public void FuzzyBestScoreFeedsTheNumericSteps()
        {
            "Jon Smith".Start().FuzzyBestScore(names, "jw").MatchGreaterOrEqual(0.9).ToBoolean().Should().BeTrue();
            "Bob Marley".Start().FuzzyBestScore(names, "jw").MatchGreaterOrEqual(0.9).ToBoolean().Should().BeFalse();
        }

        [Fact]
        public void FuzzyMatchCountBecomesAnIntegerValue()
        {
            var list = new List<string> { "John Smith", "john smith", "Bob" };

            "John Smith".Start().FuzzyMatchCount(list, "exact").Value.Should().Be(2);
            "John Smith".Start().FuzzyMatchCount(list, "exact").MatchGreaterOrEqual(2).ToBoolean().Should().BeTrue();
        }

        [Fact]
        public void FuzzyBestMatchBecomesAStringValueThatCanBeTestedFurther()
        {
            var flow = "Jon Smith".Start().FuzzyBestMatch(names, null);

            flow.Value.Should().Be("John Smith");
            flow.MatchEqual("John Smith").ToBoolean().Should().BeTrue();
        }

        [Fact]
        public void FuzzyBestMatchIsNullWhenNothingIsClose()
        {
            "Zzzz".Start().FuzzyBestMatch(names, "exact").Value.Should().BeNull();
        }

        [Fact]
        public void ADecidedFlowSkipsTheExpensiveFuzzyTransformers()
        {
            var flow = "Jon Smith".Start().Reject().FuzzyBestScore(names, null);

            flow.Outcome.Should().Be(FlowOutcome.Rejected);
            flow.Value.Should().Be(0d);
            "Jon Smith".Start().Reject().FuzzyMatchCount(names, null).Value.Should().Be(0);
            "Jon Smith".Start().Reject().FuzzyBestMatch(names, null).Value.Should().BeNull();
        }

        [Fact]
        public void TheScoreAndLabelSurviveAFuzzyTransformer()
        {
            var flow = "Jon Smith".Start().AddScore(2).Label("screen").FuzzyBestScore(names, null);

            flow.Score.Should().Be(2d);
            flow.Label.Should().Be("screen");
        }

        [Fact]
        public void AScreeningPipelineReadsLeftToRight()
        {
            var matched = "Dr. Jon Smith".Start()
                .RequireNotEqual("")
                .MatchSomehowInListWith(names, "notitles,jw=0.9")
                .ToBoolean();

            matched.Should().BeTrue();
        }

        [Fact]
        public void APipelineCanEscalateFromExactThroughToFuzzy()
        {
            var flow = "Jon Smith".Start()
                .MatchNormalisedInList(names)
                .MatchDamerauInList(names, 1)
                .MatchSomehowInList(names);

            flow.Outcome.Should().Be(FlowOutcome.Matched);
        }

        [Fact]
        public void AWatchlistHitCanBeRequiredBeforeAnotherFieldIsTested()
        {
            var flow = "Jon Smith".Start()
                .RequireSomehowInList(names)
                .Against(1000d)
                .MatchGreater(500);

            flow.Outcome.Should().Be(FlowOutcome.Matched);

            "Bob Marley".Start().RequireSomehowInList(names).Against(1000d).MatchGreater(500).Outcome
                .Should().Be(FlowOutcome.Rejected);
        }
    }
}
