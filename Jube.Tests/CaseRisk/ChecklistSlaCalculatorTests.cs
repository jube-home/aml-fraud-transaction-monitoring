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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.CaseRisk.Analytics;
using Xunit;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class ChecklistSlaCalculatorTests
    {
        private static readonly DateTime Now = new(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Guid Note = Guid.NewGuid();
        private static readonly Guid Contact = Guid.NewGuid();
        private static readonly Guid Review = Guid.NewGuid();
        private static readonly Guid Triage = Guid.NewGuid();
        private static readonly Guid SignOff = Guid.NewGuid();

        private static DateTime At(int hour, int minute) => new(2026, 1, 10, hour, minute, 0, DateTimeKind.Utc);

        private static ChecklistItemFact Fact(Guid item, Guid list, string listName, string itemName, DateTime shown,
            DateTime? due, DateTime? completed = null, bool hidden = false, DateTime? breached = null) =>
            new(item, list, listName, itemName, completed.HasValue, hidden, shown, due, completed, breached);

        private static List<ChecklistItemFact> Fixture() =>
        [
            Fact(Note, Triage, "Triage", "Note", At(9, 0), At(10, 0), At(9, 30)),
            Fact(Note, Triage, "Triage", "Note", At(9, 0), At(10, 0), At(10, 30)),
            Fact(Note, Triage, "Triage", "Note", At(9, 0), At(10, 0), At(9, 50)),
            Fact(Note, Triage, "Triage", "Note", At(11, 0), At(11, 30)),
            Fact(Note, Triage, "Triage", "Note", At(11, 5), At(13, 5)),
            Fact(Note, Triage, "Triage", "Note", At(8, 0), At(9, 0), hidden: true),
            Fact(Contact, Triage, "Triage", "Contact", At(9, 0), null, At(10, 0)),
            Fact(Contact, Triage, "Triage", "Contact", At(11, 0), null),
            Fact(Review, SignOff, "Sign-off", "Review", At(10, 0), At(11, 0), breached: At(11, 1))
        ];

        [Fact]
        public void ShownCompletedWithinLateOpenAndBreachedAreCountedPerItem()
        {
            var report = ChecklistSlaCalculator.Compute(Fixture(), Now);

            var note = report.Items.Single(i => i.ItemGuid == Note);

            note.Shown.Should().Be(6);
            note.Hidden.Should().Be(1);
            note.Completed.Should().Be(3);
            note.CompletedWithinSla.Should().Be(2);
            note.CompletedLate.Should().Be(1);
            note.Open.Should().Be(2);
            note.Breached.Should().Be(1);
        }

        [Fact]
        public void MedianAndNinetiethPercentileMinutesRunFromFirstShownToCompletion()
        {
            var note = ChecklistSlaCalculator.Compute(Fixture(), Now).Items.Single(i => i.ItemGuid == Note);

            note.MedianMinutes.Should().Be(50);
            note.P90Minutes.Should().BeApproximately(82, 1e-9);
        }

        [Fact]
        public void AttainmentIsTheShareOfTimedCompletionsWithinTheSla()
        {
            var report = ChecklistSlaCalculator.Compute(Fixture(), Now);

            report.Items.Single(i => i.ItemGuid == Note).Attainment.Should().BeApproximately(2.0 / 3.0, 1e-9);
            report.Attainment.Should().BeApproximately(2.0 / 3.0, 1e-9);
        }

        [Fact]
        public void TheTargetIsTheSlaOfTheMostRecentlyShownInstance()
        {
            var report = ChecklistSlaCalculator.Compute(Fixture(), Now);

            report.Items.Single(i => i.ItemGuid == Note).TargetMinutes.Should().Be(120);
            report.Items.Single(i => i.ItemGuid == Review).TargetMinutes.Should().Be(60);
        }

        [Fact]
        public void AnItemWithNoSlaCountsAsCompletedOnlyAndIsNeverBreached()
        {
            var contact = ChecklistSlaCalculator.Compute(Fixture(), Now).Items.Single(i => i.ItemGuid == Contact);

            contact.TargetMinutes.Should().BeNull();
            contact.Completed.Should().Be(1);
            contact.CompletedWithinSla.Should().Be(0);
            contact.CompletedLate.Should().Be(0);
            contact.Attainment.Should().BeNull();
            contact.Open.Should().Be(1);
            contact.Breached.Should().Be(0);
            contact.MedianMinutes.Should().Be(60);
            contact.P90Minutes.Should().Be(60);
        }

        [Fact]
        public void AnOpenItemWithARecordedBreachIsBreachedEvenBeforeItsDueDateIsComparedWithTheClock()
        {
            var review = ChecklistSlaCalculator.Compute(Fixture(), At(10, 30)).Items.Single(i => i.ItemGuid == Review);

            review.Breached.Should().Be(1);
        }

        [Fact]
        public void HiddenItemsAreNeverOpenOrBreached()
        {
            var note = ChecklistSlaCalculator.Compute(
                [Fact(Note, Triage, "Triage", "Note", At(8, 0), At(9, 0), hidden: true)], Now).Items.Single();

            note.Shown.Should().Be(1);
            note.Hidden.Should().Be(1);
            note.Open.Should().Be(0);
            note.Breached.Should().Be(0);
            note.MedianMinutes.Should().BeNull();
        }

        [Fact]
        public void TheWorstItemsComeFirstAndTheTotalsAddUp()
        {
            var report = ChecklistSlaCalculator.Compute(Fixture(), Now);

            report.Items.Select(i => i.ItemGuid).Should().Equal(Note, Review, Contact);
            report.Shown.Should().Be(9);
            report.Completed.Should().Be(4);
            report.CompletedWithinSla.Should().Be(2);
            report.CompletedLate.Should().Be(1);
            report.Breached.Should().Be(2);
        }

        [Fact]
        public void NoFactsGiveAnEmptyReportWithNoAttainment()
        {
            var report = ChecklistSlaCalculator.Compute([], Now);

            report.Items.Should().BeEmpty();
            report.Shown.Should().Be(0);
            report.Attainment.Should().BeNull();
        }
    }
}
