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
    public sealed class CorrelationCalculatorTests
    {
        private static System.Collections.Generic.List<JournalEvent> Pairs(System.Func<int, double> other)
        {
            return Enumerable.Range(0, 20).Select(i => Event(i + 1, Start.AddDays(i), i * 2.0 + 1, other: other(i)))
                .ToList();
        }

        [Fact]
        public void APerfectLineIsOneAndIsListedAsStrong()
        {
            var report = CorrelationCalculator.Compute(Pairs(i => i * 3.0), ["Amount", "Other"]);

            report.Matrix[0][1].Should().BeApproximately(1, 1e-9);
            report.Matrix[1][0].Should().BeApproximately(1, 1e-9);
            report.Matrix[0][0].Should().Be(1);
            report.Strong.Should().ContainSingle().Which.Pairs.Should().Be(20);
        }

        [Fact]
        public void ANegativeRelationshipKeepsItsSign()
        {
            var report = CorrelationCalculator.Compute(Pairs(i => 100 - i * 2.0), ["Amount", "Other"]);

            report.Matrix[0][1].Should().BeApproximately(-1, 1e-9);
            report.Strong.Single().R.Should().BeLessThan(0);
        }

        [Fact]
        public void AConstantFieldAndFieldsWithTooFewPairsAreLeftOut()
        {
            var constant = CorrelationCalculator.Compute(Pairs(_ => 4), ["Amount", "Other"]);
            var few = CorrelationCalculator.Compute(Daily(1, 2, 3), ["Amount"]);

            constant.Fields.Should().Equal("Amount");
            few.Fields.Should().BeEmpty();
        }

        [Fact]
        public void UnrelatedNoiseIsNotStrong()
        {
            var report = CorrelationCalculator.Compute(Pairs(i => (i * 7919) % 13), ["Amount", "Other"]);

            report.Strong.Should().BeEmpty();
        }

        [Fact]
        public void PearsonNeedsTenPairsAndVarianceOnBothSides()
        {
            CorrelationCalculator.Pearson(Pairs(i => i), "Amount", "Other").R.Should().NotBeNull();
            CorrelationCalculator.Pearson(Pairs(_ => 1), "Amount", "Other").R.Should().BeNull();
            CorrelationCalculator.Pearson(Daily(1, 2), "Amount", "Other").Pairs.Should().Be(0);
        }
    }
}
