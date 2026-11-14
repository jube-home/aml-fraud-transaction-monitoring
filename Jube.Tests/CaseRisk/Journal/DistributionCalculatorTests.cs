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
    public sealed class DistributionCalculatorTests
    {
        private static readonly string[] amount = ["Amount"];

        [Fact]
        public void EveryValueLandsInExactlyOneBinAndTheBinsStayWithinTheirLimits()
        {
            var events = Daily(Enumerable.Range(1, 200).Select(i => (double)(i % 37)).ToArray());

            var report = DistributionCalculator.Compute(events, amount).Single();

            report.Bins.Sum(bin => bin.Count).Should().Be(200);
            report.Bins.Count.Should().BeInRange(DistributionCalculator.MinimumBins, DistributionCalculator.MaximumBins);
            report.Bins[0].From.Should().Be(report.Min);
            report.Bins[^1].To.Should().BeApproximately(report.Max, 1e-9);
        }

        [Fact]
        public void FewValuesStillGetTheMinimumNumberOfBins()
        {
            var report = DistributionCalculator.Compute(Daily(1, 2, 3, 4, 5, 6), amount).Single();

            report.Bins.Should().HaveCount(DistributionCalculator.MinimumBins);
            report.Bins.Sum(bin => bin.Count).Should().Be(6);
        }

        [Fact]
        public void ATightClusterWithOneExtremeNeverMakesMoreThanTheMaximumNumberOfBins()
        {
            var values = Enumerable.Repeat(10.0, 100).Concat(Enumerable.Range(0, 100).Select(i => 10 + i * 0.001))
                .Concat([1_000_000]).ToArray();

            var report = DistributionCalculator.Compute(Daily(values), amount).Single();

            report.Bins.Should().HaveCount(DistributionCalculator.MaximumBins);
            report.Bins.Sum(bin => bin.Count).Should().Be(201);
        }

        [Fact]
        public void AConstantFieldIsOneBinRatherThanADivisionByZero()
        {
            var report = DistributionCalculator.Compute(Daily(4, 4, 4, 4), amount).Single();

            report.Bins.Should().ContainSingle().Which.Count.Should().Be(4);
        }

        [Fact]
        public void TheBoxUsesTukeyFencesAndCountsTheOutliers()
        {
            var events = Daily(10, 11, 12, 13, 14, 15, 16, 17, 18, 100);

            var box = DistributionCalculator.Compute(events, amount).Single().Box;

            box.Q1.Should().BeApproximately(12.25, 1e-9);
            box.Q3.Should().BeApproximately(16.75, 1e-9);
            box.UpperFence.Should().BeApproximately(16.75 + 1.5 * 4.5, 1e-9);
            box.Outliers.Should().Be(1);
        }

        [Fact]
        public void TheLatestValueIsRankedAgainstTheWholeHistory()
        {
            var events = Daily(1, 2, 3, 4, 5, 6, 7, 8, 9, 5);

            var report = DistributionCalculator.Compute(events, amount).Single();

            report.Latest.Should().Be(5);
            report.LatestPercentile.Should().BeApproximately(0.6, 1e-9);
        }

        [Fact]
        public void AFieldWithNoValuesProducesNoReport()
        {
            DistributionCalculator.Compute(Daily(1, 2), ["Missing"]).Should().BeEmpty();
        }

        [Fact]
        public void TheMeanAndTheEdgesComeFromTheSortedValues()
        {
            var report = DistributionCalculator.Compute(Daily(9, 1, 5), amount).Single();

            report.Min.Should().Be(1);
            report.Max.Should().Be(9);
            report.Mean.Should().BeApproximately(5, 1e-9);
            report.Median.Should().Be(5);
        }
    }
}
