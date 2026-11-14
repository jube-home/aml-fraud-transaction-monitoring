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
using Jube.CaseRisk;
using Jube.Dto.Options;
using Xunit;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class CaseRiskCalculatorTests
    {
        private static readonly CaseRiskSettings settings = new() { PriorStrength = 0.000001 };

        private static EvidenceItem Evidence(string label, double precision, string? group, string family = "Model",
            double ageDays = 0, double halfLifeDays = 0) =>
            new(family, label, "test", group, (int)Math.Round(precision * 1_000_000), 1_000_000, ageDays,
                halfLifeDays);

        private static readonly DateTime now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void TheWorkedExampleOfSpecSection4511ReproducesLikelihoodImpactAndPriority()
        {
            var inputs = new CaseRiskInputs(80_000, 1_000_000,
            [
                Evidence("StructNet v7 Very high", 0.31, "structuring"),
                Evidence("Structuring 9d v12", 0.18, "structuring", "Rule"),
                Evidence("2 hops from a confirmed true positive", 0.22, null, "Network"),
                Evidence("Customer risk tier High", 0.14, null, "Customer"),
                Evidence("Sanctions similarity 0.82", 0.09, null, "Sanctions")
            ],
            new ImpactInputs(3, 45_000, 0, 1),
            new LaneInputs(now, NextDeadline: now.AddDays(4.13), NextDeadlineLabel: "STR diary"));

            var result = CaseRiskCalculator.Assess(inputs, settings);

            result.Likelihood.BaseRate.Should().BeApproximately(0.08, 0.0001);
            result.Likelihood.Probability.Should().BeApproximately(0.56, 0.01);
            result.Likelihood.Band.Should().Be(CaseLikelihoodBand.Likely);
            result.Impact.Band.Should().Be(CaseImpactBand.High);
            result.Impact.MoneyBand.Should().Be(2);
            result.Lane.Lane.Should().Be(CaseRiskLane.Standard);
            result.Priority.Should().BeInRange(62, 63);
            result.ExpectedHarm.Should().BeApproximately(16.8, 0.3);

            result.Likelihood.Contributions.Select(c => c.Label).First().Should().Be("StructNet v7 Very high");
            result.Likelihood.Contributions.Single(c => c.Label == "Structuring 9d v12").SharesInputs.Should().BeTrue();
            result.Likelihood.Contributions.Single(c => c.Label == "Structuring 9d v12").Weight.Should().Be(0.3);
        }

        [Fact]
        public void ContributionsSumExactlyToTheLogOddsDistanceFromTheBaseRate()
        {
            var assessment = LikelihoodCalculator.Compute(0.08, 1_000_000,
            [
                Evidence("A", 0.31, "g"),
                Evidence("B", 0.18, "g"),
                Evidence("C", 0.22, null),
                Evidence("D", 0.03, null)
            ], settings);

            var total = LikelihoodCalculator.Logit(assessment.BaseRate) + assessment.Contributions.Sum(c => c.LogOdds);

            LikelihoodCalculator.Sigmoid(total).Should().BeApproximately(assessment.Probability, 1e-12);
        }

        [Fact]
        public void NegativeEvidenceLowersTheLikelihoodBelowTheBaseRate()
        {
            var assessment = LikelihoodCalculator.Compute(0.08, 1_000_000,
                [Evidence("Three prior false positives on this key", 0.01, null, "KeyHistory")], settings);

            assessment.Probability.Should().BeLessThan(0.08);
            assessment.Contributions.Single().LogOdds.Should().BeNegative();
        }

        [Fact]
        public void TheStrongestEvidenceInAGroupCountsInFullByMagnitudeEvenWhenItIsNegative()
        {
            var assessment = LikelihoodCalculator.Compute(0.2, 1_000_000,
            [
                Evidence("Weakly positive", 0.22, "g"),
                Evidence("Strongly negative", 0.02, "g")
            ], settings);

            assessment.Contributions.Single(c => c.Label == "Strongly negative").Weight.Should().Be(1);
            assessment.Contributions.Single(c => c.Label == "Weakly positive").Weight.Should().Be(settings.Rho);
        }

        [Fact]
        public void IndependentEvidenceAddsWhileSharedEvidenceIsDiscounted()
        {
            var independent = LikelihoodCalculator.Compute(0.08, 1_000_000,
                [Evidence("A", 0.3, null), Evidence("B", 0.3, null)], settings);
            var shared = LikelihoodCalculator.Compute(0.08, 1_000_000,
                [Evidence("A", 0.3, "g"), Evidence("B", 0.3, "g")], settings);

            independent.Probability.Should().BeGreaterThan(shared.Probability);
        }

        [Fact]
        public void EvidenceBehindFewClosedCasesIsShrunkTowardsTheBaseRate()
        {
            var sparse = new CaseRiskSettings();
            var thin = new EvidenceItem("Rule", "One lucky case", "test", null, 1, 1);
            var thick = new EvidenceItem("Rule", "Many cases", "test", null, 500, 1000);

            var thinAssessment = LikelihoodCalculator.Compute(0.08, 100, [thin], sparse);
            var thickAssessment = LikelihoodCalculator.Compute(0.08, 100, [thick], sparse);

            thinAssessment.Probability.Should().BeLessThan(thickAssessment.Probability);
            thinAssessment.Probability.Should().BeLessThan(0.2);
            thickAssessment.Probability.Should().BeGreaterThan(0.3);
        }

        [Fact]
        public void EvidenceWithNoClosedCasesBehindItIsIgnored()
        {
            var assessment = LikelihoodCalculator.Compute(0.08, 0,
                [new EvidenceItem("Rule", "Never closed", "test", null, 0, 0)], new CaseRiskSettings());

            assessment.Contributions.Should().BeEmpty();
            assessment.Probability.Should().BeApproximately(0.08, 1e-9);
            assessment.EvidenceCount.Should().Be(0);
        }

        [Fact]
        public void OldEvidenceDecaysByItsHalfLife()
        {
            var fresh = LikelihoodCalculator.Compute(0.08, 1_000_000, [Evidence("A", 0.4, null)], settings);
            var aged = LikelihoodCalculator.Compute(0.08, 1_000_000,
                [Evidence("A", 0.4, null, ageDays: 180, halfLifeDays: 180)], settings);

            aged.Contributions.Single().Decay.Should().BeApproximately(0.5, 1e-9);
            aged.Contributions.Single().LogOdds.Should().BeApproximately(fresh.Contributions.Single().LogOdds / 2,
                1e-9);
        }

        [Fact]
        public void TheBaseRateIsShrunkTowardsThePriorUntilEnoughCasesHaveClosed()
        {
            var defaults = new CaseRiskSettings();

            LikelihoodCalculator.EstimateBaseRate(0, 0, defaults).Should().Be(0.08);
            LikelihoodCalculator.EstimateBaseRate(5, 5, defaults).Should().BeApproximately((5 + 1.6) / 25, 1e-9);
            LikelihoodCalculator.EstimateBaseRate(5_000, 100_000, defaults).Should().BeApproximately(0.05, 0.001);
        }

        [Theory]
        [InlineData(0.10, CaseLikelihoodBand.Unlikely)]
        [InlineData(0.25, CaseLikelihoodBand.Possible)]
        [InlineData(0.499, CaseLikelihoodBand.Possible)]
        [InlineData(0.50, CaseLikelihoodBand.Likely)]
        [InlineData(0.75, CaseLikelihoodBand.VeryLikely)]
        public void TheBandsFollowTheDocumentedThresholds(double probability, CaseLikelihoodBand band)
        {
            LikelihoodCalculator.BandOf(probability).Should().Be(band);
        }

        [Fact]
        public void TheProbabilityNeverReachesCertaintyEitherWay()
        {
            var high = LikelihoodCalculator.Compute(0.5, 1_000_000,
                Enumerable.Range(0, 20).Select(i => Evidence("E" + i, 0.99, null)).ToList(), settings);
            var low = LikelihoodCalculator.Compute(0.5, 1_000_000,
                Enumerable.Range(0, 20).Select(i => Evidence("E" + i, 0.001, null)).ToList(), settings);

            high.Probability.Should().BeLessThan(1).And.BeApproximately(0.999, 1e-9);
            low.Probability.Should().BeGreaterThan(0).And.BeApproximately(0.001, 1e-9);
        }

        [Theory]
        [InlineData(3, null, 0, 0, 1, CaseImpactBand.Moderate)]
        [InlineData(3, 45_000.0, 0, 1, 1, CaseImpactBand.High)]
        [InlineData(2, 20_000_000.0, 0, 0, 1, CaseImpactBand.Severe)]
        [InlineData(5, 20_000_000.0, 1, 1, 1, CaseImpactBand.Severe)]
        [InlineData(1, 0.0, 0, 0, 1, CaseImpactBand.Minimal)]
        [InlineData(2, null, 0, 0, 5, CaseImpactBand.Severe)]
        public void ImpactIsTheLargerOfTypologyAndMoneyPlusAtMostOneUpliftEachCappedAtSevere(int typology,
            double? value, int exposure, int breadth, int floor, CaseImpactBand expected)
        {
            ImpactCalculator.Compute(new ImpactInputs(typology, value, exposure, breadth, floor), settings)
                .Band.Should().Be(expected);
        }

        [Fact]
        public void ImpactNeverDependsOnTheLikelihood()
        {
            var impact = ImpactCalculator.Compute(new ImpactInputs(4, null), settings);

            impact.Band.Should().Be(CaseImpactBand.High);
            typeof(ImpactInputs).GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Likelihood"));
        }

        [Theory]
        [InlineData(0.0, 0)]
        [InlineData(0.56 * 30, 62)]
        [InlineData(0.48 * 10, 38)]
        [InlineData(0.22 * 3, 11)]
        [InlineData(100.0, 100)]
        public void PriorityScalesExpectedHarmLogarithmicallyToZeroToOneHundred(double harm, int priority)
        {
            PriorityCalculator.Priority(harm, settings).Should().Be(priority);
        }

        [Fact]
        public void LaneOneBeatsEverythingAndKeepsTheHoldDeadline()
        {
            var hold = now.AddHours(6);

            var lane = LaneCalculator.Decide(new LaneInputs(now, hold, "Sanctions", now.AddDays(1)), settings);

            lane.Lane.Should().Be(CaseRiskLane.LiveMoney);
            lane.NextDeadline.Should().Be(hold);
            lane.Breached.Should().BeFalse();
        }

        [Fact]
        public void AMandatoryReasonPlacesTheCaseInLaneTwo()
        {
            LaneCalculator.Decide(new LaneInputs(now, MandatoryReason: "Confirmed sanctions match"), settings)
                .Lane.Should().Be(CaseRiskLane.Mandatory);
        }

        [Theory]
        [InlineData(1.0, CaseRiskLane.DeadlineNear, false)]
        [InlineData(2.0, CaseRiskLane.DeadlineNear, false)]
        [InlineData(2.1, CaseRiskLane.Standard, false)]
        [InlineData(-3.0, CaseRiskLane.DeadlineNear, true)]
        public void ADiaryMovesTheCaseIntoLaneThreeWithinTheThresholdAndAnOverdueOneNeverDropsBack(double days,
            CaseRiskLane lane, bool breached)
        {
            var result = LaneCalculator.Decide(new LaneInputs(now, NextDeadline: now.AddDays(days)), settings);

            result.Lane.Should().Be(lane);
            result.Breached.Should().Be(breached);
        }

        [Fact]
        public void NoDeadlineMeansTheStandardLaneWithNoDate()
        {
            var lane = LaneCalculator.Decide(new LaneInputs(now), settings);

            lane.Lane.Should().Be(CaseRiskLane.Standard);
            lane.NextDeadline.Should().BeNull();
        }

        [Fact]
        public void ALaneNeverChangesTheLikelihoodOrTheImpact()
        {
            var calm = CaseRiskCalculator.Assess(new CaseRiskInputs(1, 10, [], new ImpactInputs(3, null),
                new LaneInputs(now)), settings);
            var urgent = CaseRiskCalculator.Assess(new CaseRiskInputs(1, 10, [], new ImpactInputs(3, null),
                new LaneInputs(now, now.AddHours(1))), settings);

            urgent.Lane.Lane.Should().NotBe(calm.Lane.Lane);
            urgent.Likelihood.Probability.Should().Be(calm.Likelihood.Probability);
            urgent.Impact.Band.Should().Be(calm.Impact.Band);
            urgent.Priority.Should().Be(calm.Priority);
        }
    }
}
