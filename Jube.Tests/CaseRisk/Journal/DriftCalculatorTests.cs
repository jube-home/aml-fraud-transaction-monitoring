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

using System.Linq;
using FluentAssertions;
using Jube.CaseRisk.Journal;
using Xunit;
using static Jube.Test.CaseRisk.Journal.JournalEvents;

namespace Jube.Test.CaseRisk.Journal
{
    [Trait("Category", "Unit")]
    public sealed class DriftCalculatorTests
    {
        private static readonly string[] amount = ["Amount"];

        [Fact]
        public void AShiftedRecentHalfIsFlaggedWithTheDirectionOfTheShift()
        {
            var values = Enumerable.Range(0, 30).Select(i => 100.0 + i % 5)
                .Concat(Enumerable.Range(0, 30).Select(i => 300.0 + i % 5)).ToArray();

            var report = DriftCalculator.Compute(Daily(values), amount);
            var drift = report.Fields.Single();

            drift.Drifted.Should().BeTrue();
            drift.Statistic.Should().BeApproximately(1, 1e-9);
            drift.RecentMedian.Should().BeGreaterThan(drift.PriorMedian);
            drift.Shift.Should().BeGreaterThan(10);
            report.Prior.Should().Be(30);
            report.Recent.Should().Be(30);
        }

        [Fact]
        public void TheSameDistributionInBothHalvesIsNotFlagged()
        {
            var values = Enumerable.Range(0, 60).Select(i => 100.0 + i % 7).ToArray();

            DriftCalculator.Compute(Daily(values), amount).Fields.Single().Drifted.Should().BeFalse();
        }

        [Fact]
        public void FewerThanFortyTransactionsOrTwentyPerSideGivesNoJudgement()
        {
            DriftCalculator.Compute(Daily(Enumerable.Repeat(1.0, 39).ToArray()), amount).Fields.Should().BeEmpty();
            DriftCalculator.Compute(Daily(Enumerable.Repeat(1.0, 39).ToArray()), amount).SplitAt.Should().BeNull();

            var sparse = Enumerable.Range(0, 60).Select(i => i < 30 || i % 3 == 0
                ? Event(i + 1, Start.AddDays(i), 1)
                : Event(i + 1, Start.AddDays(i))).ToList();

            DriftCalculator.Compute(sparse, amount).Fields.Should().BeEmpty();
        }

        [Fact]
        public void TheKolmogorovDistanceIsZeroForIdenticalSamplesAndOneForSeparatedOnes()
        {
            DriftCalculator.Kolmogorov([1, 2, 3], [1, 2, 3]).Should().Be(0);
            DriftCalculator.Kolmogorov([1, 2, 3], [10, 11, 12]).Should().Be(1);
            DriftCalculator.Kolmogorov([1, 2, 3, 4], [3, 4, 5, 6]).Should().BeApproximately(0.5, 1e-9);
        }
    }
}
