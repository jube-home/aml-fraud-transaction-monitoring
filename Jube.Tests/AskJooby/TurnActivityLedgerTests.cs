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

using System.Collections.Generic;
using FluentAssertions;
using Jube.AskJooby.Observability;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class TurnActivityLedgerTests
    {
        [Fact]
        public void AQuietTurnSaysNothingAboutReasoningAtAll()
        {
            var ledger = new TurnActivityLedger();

            ledger.RecordToolCall();

            ledger.Describe().Should().NotContain("reasoned anyway",
                "a turn where the switch worked must not pay tokens to report that it worked");
        }

        [Fact]
        public void AModelThatReasonsWithTheSwitchOffIsReportedRatherThanOnlyCounted()
        {
            var ledger = new TurnActivityLedger();

            ledger.NoteUnrequestedReasoning();
            ledger.NoteUnrequestedReasoning();

            ledger.UnrequestedReasoningUpdates.Should().Be(2);

            ledger.Describe().Should().Contain("reasoned anyway on 2 update(s)",
                "this was counted and discarded for as long as the detector existed, which left "
                + "the one question an operator asks -- is it still thinking? -- unanswerable");
        }

        [Fact]
        public void HandBacksAreCountedAcrossTheTurn()
        {
            var ledger = new TurnActivityLedger();

            ledger.HandBacks.Should().Be(0);

            ledger.NoteHandBack();
            ledger.NoteHandBack();

            ledger.HandBacks.Should().Be(2,
                "the second hand-back in a turn is the one refused, so the count has to see both");
        }

        [Fact]
        public void ATurnHasNoPlanUntilOneIsRecorded()
        {
            var turn = new TurnActivityLedger();

            turn.HasPlan.Should().BeFalse();

            turn.RecordPlan("1. Find it -> researcher. Check: it is named.", new HashSet<string>());

            turn.HasPlan.Should().BeTrue();
        }

        [Fact]
        public void UnplannedHandoversAreCountedAcrossTheTurn()
        {
            var turn = new TurnActivityLedger();

            turn.NoteUnplannedHandover();
            turn.NoteUnplannedHandover();

            turn.UnplannedHandovers.Should().Be(2);
        }

        [Fact]
        public void AStablePlanIsKeptAgainstThatStableAndNotTheRootPlan()
        {
            var turn = new TurnActivityLedger();

            turn.RecordStablePlan("model-risk", "1. Inspect -> advanced-analytics-model-inspector. Check: it is read.");

            turn.HasStablePlan("model-risk").Should().BeTrue();
            turn.HasStablePlan("case-analyst").Should().BeFalse();
            turn.HasPlan.Should().BeFalse();
        }
    }
}