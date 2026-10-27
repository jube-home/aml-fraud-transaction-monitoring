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
    public sealed class NotableCalculatorTests
    {
        private static readonly string[] amount = ["Amount"];

        private static System.Collections.Generic.List<Jube.CaseRisk.Journal.JournalEvent> WithOutlier()
        {
            var baseline = Enumerable.Range(0, 30).Select(i => 100.0 + i % 5).ToList();
            baseline.Add(5000);

            return Daily(baseline.ToArray());
        }

        [Fact]
        public void OnlyTheValueFarFromItsOwnNormalIsNotableAndItNamesTheField()
        {
            var report = NotableCalculator.Compute(WithOutlier(), amount);

            var row = report.Rows.Should().ContainSingle().Subject;

            row.Id.Should().Be(31);
            row.Worst.Should().Be("Amount");
            row.Score.Should().Be(RobustStatistics.MaxZ);
            row.Reasons.Single().Value.Should().Be(5000);
        }

        [Fact]
        public void ARaisedThresholdLeavesFewerRowsAndALowerOneMore()
        {
            var events = WithOutlier();

            NotableCalculator.Compute(events, amount, threshold: 6).Rows.Should().BeEmpty();
            NotableCalculator.Compute(events, amount, threshold: 0.5).Rows.Count.Should().BeGreaterThan(1);
        }

        [Fact]
        public void TheWorstRowsComeFirstAndTheLimitCapsThem()
        {
            var events = Daily(Enumerable.Repeat(100.0, 40).Concat([900, 1000, 2000]).ToArray());

            var report = NotableCalculator.Compute(events, amount, threshold: 2, maximumRows: 2);

            report.Rows.Should().HaveCount(2);
            report.Rows.Select(row => row.Id).Should().Equal(43, 42);
        }

        [Fact]
        public void FewerThanTwentyValuesAreFlaggedAsLowConfidenceAndFewerThanFiveAreNeverJudged()
        {
            var few = Daily(1, 2, 3);
            var some = Daily(Enumerable.Range(0, 10).Select(i => (double)i).Concat([500]).ToArray());

            NotableCalculator.Compute(few, amount).Rows.Should().BeEmpty();
            NotableCalculator.Compute(some, amount).LowConfidenceFields.Should().Equal("Amount");
            NotableCalculator.Compute(WithOutlier(), amount).LowConfidenceFields.Should().BeEmpty();
        }

        [Fact]
        public void EachFieldKeepsItsOwnNorm()
        {
            var events = Enumerable.Range(0, 25).Select(i => Event(i + 1, Start.AddDays(i), 100 + i % 3, other: 5000 + i % 3))
                .ToList();

            var report = NotableCalculator.Compute(events, ["Amount", "Other"]);

            report.Norms.Single(norm => norm.Field == "Amount").Median.Should().BeApproximately(101, 1);
            report.Norms.Single(norm => norm.Field == "Other").Median.Should().BeApproximately(5001, 1);
            report.Rows.Should().BeEmpty();
        }
    }
}
