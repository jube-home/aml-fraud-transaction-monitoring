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
using System.Linq;
using FluentAssertions;
using Jube.CaseRisk.Analytics;
using Xunit;
using static Jube.Test.CaseRisk.FactBuilder;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class CaseAnalyticsQualityTests
    {
        private static readonly Guid Noisy = Guid.Parse("00000000-0000-0000-0000-00000000aa01");
        private static readonly Guid Sharp = Guid.Parse("00000000-0000-0000-0000-00000000aa02");

        [Fact]
        public void RulesAreRankedByTheEffortTheirFalsePositivesCostAndShowTheirPrecision()
        {
            var facts = new[]
            {
                Fact(1, 0, 60, 2, Noisy, "Noisy rule"),
                Fact(2, 0, 120, 2, Noisy, "Noisy rule"),
                Fact(3, 0, 30, 1, Noisy, "Noisy rule"),
                Fact(4, 0, 30, 1, Sharp, "Sharp rule"),
                Fact(5, 0, 90, 1, Sharp, "Sharp rule"),
                Fact(6, 0, 45, 2, Sharp, "Sharp rule"),
                Fact(7, 0, 20, 0, Sharp, "Sharp rule"),
                Fact(8, rule: Sharp, ruleName: "Sharp rule"),
                Fact(9, 0, 10, 2)
            };

            var rules = RuleEffectivenessCalculator.Compute(facts);

            rules.Select(r => r.Name).Should().Equal("Noisy rule", "Sharp rule", "Not created by a rule");

            var noisy = rules[0];
            noisy.Cases.Should().Be(3);
            noisy.Confirmed.Should().Be(1);
            noisy.NotConfirmed.Should().Be(2);
            noisy.Precision.Should().BeApproximately(1.0 / 3, 1e-9);
            noisy.WastedMinutes.Should().Be(180);
            noisy.MeanHandlingMinutes.Should().BeApproximately(70, 1e-9);

            var sharp = rules[1];
            sharp.Open.Should().Be(1);
            sharp.NoOutcome.Should().Be(1);
            sharp.Precision.Should().BeApproximately(2.0 / 3, 1e-9);
            sharp.WastedMinutes.Should().Be(45);

            rules.Sum(r => r.ShareOfWastedEffort).Should().BeApproximately(1, 1e-9);
            noisy.ShareOfWastedEffort.Should().BeApproximately(180.0 / 235, 1e-9);
            rules[2].RuleGuid.Should().BeNull();
        }

        [Fact]
        public void ARuleWithNoDecidedCasesHasNoPrecisionRatherThanZero()
        {
            var rules = RuleEffectivenessCalculator.Compute([Fact(1, rule: Sharp), Fact(2, 0, 10, 0, Sharp)]);

            rules.Single().Precision.Should().BeNull();
        }

        private static CaseFact[] Scored(params (double Priority, double Likelihood, bool Confirmed, double Created)[] rows) =>
            rows.Select((r, i) => Fact(i + 1, r.Created, r.Created + 60, r.Confirmed ? 1 : 2, priority: (int)r.Priority,
                likelihood: r.Likelihood)).ToArray();

        [Fact]
        public void APerfectOrderingFindsEveryConfirmedCaseFirstAndABackwardsOneFindsThemLast()
        {
            var perfect = Scored(
                (90, 0.9, true, 1), (80, 0.8, true, 2), (70, 0.7, true, 3), (20, 0.2, false, 4), (10, 0.1, false, 5),
                (5, 0.05, false, 6));
            var backwards = Scored(
                (90, 0.9, false, 1), (80, 0.8, false, 2), (70, 0.7, false, 3), (20, 0.2, true, 4), (10, 0.1, true, 5),
                (5, 0.05, true, 6));

            var good = RankingQualityCalculator.Compute(perfect);
            var bad = RankingQualityCalculator.Compute(backwards);

            good.PriorityAuc.Should().Be(1);
            good.LikelihoodAuc.Should().Be(1);
            good.Confirmed.Should().Be(3);
            good.BaseRate.Should().Be(0.5);
            good.PriorityLiftTop30.Should().Be(2);
            good.PriorityGains[10].Found.Should().Be(1);
            bad.PriorityAuc.Should().Be(0);
            bad.PriorityGains[10].Found.Should().Be(0);
        }

        [Fact]
        public void ThePriorityOrderingIsComparedWithWorkingTheOldestFirstAndTiesCountHalf()
        {
            var facts = Scored(
                (50, 0.5, true, 1), (50, 0.5, false, 2), (50, 0.5, true, 3), (50, 0.5, false, 4));

            var report = RankingQualityCalculator.Compute(facts);

            report.PriorityAuc.Should().Be(0.5);
            report.OldestFirstAuc.Should().NotBeNull();
            report.OldestFirstGains.Should().HaveCount(21);
            report.OldestFirstGains[0].Should().Be(new GainsPoint(0, 0));
            report.OldestFirstGains[^1].Should().Be(new GainsPoint(1, 1));
        }

        [Fact]
        public void TheRankingIsOnlyCalledSufficientWithEnoughOutcomesOfBothKinds()
        {
            var few = RankingQualityCalculator.Compute(Scored((1, 0.1, true, 1), (2, 0.2, false, 2)));
            few.Sufficient.Should().BeFalse();

            var rows = Enumerable.Range(1, 40).Select(i => Fact(i, i, i + 60, i % 4 == 0 ? 1 : 2, priority: i,
                likelihood: i / 100.0)).ToArray();

            RankingQualityCalculator.Compute(rows).Sufficient.Should().BeTrue();

            var oneSided = Enumerable.Range(1, 40).Select(i => Fact(i, i, i + 60, 2, priority: i)).ToArray();
            var onlyFalse = RankingQualityCalculator.Compute(oneSided);
            onlyFalse.Sufficient.Should().BeFalse();
            onlyFalse.PriorityAuc.Should().BeNull();
        }

        [Fact]
        public void OnlyClosedCasesWithAnOutcomeAreScoredAndOpenOrUndecidedOnesAreLeftOut()
        {
            var report = RankingQualityCalculator.Compute(
            [
                Fact(1, 0, 10, 1, priority: 80, likelihood: 0.8),
                Fact(2, 0, 10, 2, priority: 20, likelihood: 0.2),
                Fact(3, 0, 10, priority: 99, likelihood: 0.99),
                Fact(4, priority: 99, likelihood: 0.99),
                Fact(5, 0, 10, 1)
            ]);

            report.Outcomes.Should().Be(3);
            report.Confirmed.Should().Be(2);
            report.PriorityAuc.Should().Be(1);
        }

        [Fact]
        public void CalibrationComparesPredictedWithObservedAndScoresTheBrierAndExpectedError()
        {
            var facts = new[]
            {
                Fact(1, 0, 10, 1, likelihood: 0.95), Fact(2, 0, 10, 1, likelihood: 0.9),
                Fact(3, 0, 10, 2, likelihood: 0.92), Fact(4, 0, 10, 2, likelihood: 0.05),
                Fact(5, 0, 10, 2, likelihood: 0.08), Fact(6, 0, 10, 2, likelihood: 0.02)
            };

            var report = RankingQualityCalculator.Compute(facts);

            report.Calibration.Should().HaveCount(2);
            var low = report.Calibration.Single(b => b.From == 0);
            low.Cases.Should().Be(3);
            low.Observed.Should().Be(0);
            low.Predicted.Should().BeApproximately(0.05, 1e-9);
            var high = report.Calibration.Single(b => b.From == 0.9);
            high.Cases.Should().Be(3);
            high.Observed.Should().BeApproximately(2.0 / 3, 1e-9);
            report.BrierScore.Should().BeApproximately(
                (0.05 * 0.05 + 0.1 * 0.1 + 0.92 * 0.92 + 0.05 * 0.05 + 0.08 * 0.08 + 0.02 * 0.02) / 6, 1e-9);
            report.ExpectedCalibrationError.Should().BeApproximately(
                (3 * Math.Abs(0.05 - 0) + 3 * Math.Abs(0.9233333333 - 2.0 / 3)) / 6, 1e-6);
        }

        [Fact]
        public void ALikelihoodOfExactlyOneFallsInTheTopBinAndNothingIsDroppedFromCalibration()
        {
            var bins = RankingQualityCalculator.Calibration([(1.0, true), (0.0, false), (0.5, true)]);

            bins.Sum(b => b.Cases).Should().Be(3);
            bins.Last().To.Should().Be(1);
        }
    }
}
