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
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class CategoricalAndSurvivalSurvivalToolsTests
    {
        private const double Tight = 1e-9;

        private static readonly double[] freireichTreatedTimes =
            [6, 6, 6, 6, 7, 9, 10, 10, 11, 13, 16, 17, 19, 20, 22, 23, 25, 32, 32, 34, 35];

        private static readonly int[] freireichTreatedEvents =
            [1, 1, 1, 0, 1, 0, 1, 0, 0, 1, 1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0];

        private static readonly double[] freireichPlaceboTimes =
            [1, 1, 2, 2, 3, 4, 4, 5, 5, 8, 8, 8, 8, 11, 11, 12, 12, 15, 17, 22, 23];

        private static readonly double[] allFour = [1, 2, 3, 4];
        private static readonly int[] allEvents = [1, 1, 1, 1];
        private readonly SurvivalEstimationTools estimation = new();
        private readonly SurvivalModelTools models = new();

        private static double[] FreireichTimes()
        {
            return [.. freireichTreatedTimes, .. freireichPlaceboTimes];
        }

        private static int[] FreireichEvents()
        {
            return [.. freireichTreatedEvents, .. Enumerable.Repeat(1, freireichPlaceboTimes.Length)];
        }

        private static string[] FreireichGroups()
        {
            return
            [
                .. Enumerable.Repeat("6-MP", freireichTreatedTimes.Length),
                .. Enumerable.Repeat("Placebo", freireichPlaceboTimes.Length)
            ];
        }

        [Fact]
        [Trait("Covers", "EstimateKaplanMeierSurvival")]
        public void EstimateKaplanMeierSurvival_MatchesFreireichSixMercaptopurineCurve()
        {
            var result = estimation.EstimateKaplanMeierSurvival(freireichTreatedTimes, freireichTreatedEvents);

            result.Subjects.Should().Be(21);
            result.Events.Should().Be(9);
            result.Rows.Select(r => r.Time).Should().Equal(6, 7, 10, 13, 16, 22, 23);
            result.Rows[0].AtRisk.Should().Be(21);
            result.Rows[0].Survival.Should().BeApproximately(18d / 21d, Tight);
            result.Rows[0].StandardError.Should().BeApproximately(0.07636, 0.00001);
            result.Rows[0].Lower.Should().BeApproximately(0.6197179553, 1e-8);
            result.Rows[0].Upper.Should().BeApproximately(0.9515517477, 1e-8);
            result.Rows[1].AtRisk.Should().Be(17);
            result.Rows[1].Survival.Should().BeApproximately(0.806723, 1e-6);
            result.Rows[6].Survival.Should().BeApproximately(0.448179, 1e-6);
            result.MedianSurvivalTime.Should().Be(23d);
            result.MedianLower.Should().Be(13d);
            double.IsNaN(result.MedianUpper).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "EstimateKaplanMeierSurvival")]
        public void EstimateKaplanMeierSurvival_PlateauAtHalfTakesTheMidpointAndQuartilesFollow()
        {
            var result = estimation.EstimateKaplanMeierSurvival(allFour, allEvents);

            result.MedianSurvivalTime.Should().Be(2.5);
            result.LowerQuartileTime.Should().Be(1.5);
            result.UpperQuartileTime.Should().Be(3.5);
            result.Rows.Select(r => r.Survival).Should().Equal(0.75, 0.5, 0.25, 0.0);
            double.IsNaN(result.Rows[3].StandardError).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "EstimateKaplanMeierSurvival")]
        public void EstimateKaplanMeierSurvival_HandlesCensoringOnlyNaNAndInvalidInput()
        {
            var censored = estimation.EstimateKaplanMeierSurvival([1d, 2d, double.NaN], [0, 0, 1],
                0.95,
                SurvivalIntervalMethod.Plain, true);

            Action negative = () => estimation.EstimateKaplanMeierSurvival([-1d], [1]);
            Action mismatch = () => estimation.EstimateKaplanMeierSurvival([1d, 2d], [1]);
            Action badEvent = () => estimation.EstimateKaplanMeierSurvival([1d], [2]);
            Action empty = () => estimation.EstimateKaplanMeierSurvival([], []);

            censored.Rows.Should().HaveCount(2);
            censored.Rows.Should().OnlyContain(r => r.Survival == 1d);
            double.IsNaN(censored.MedianSurvivalTime).Should().BeTrue();
            censored.SkippedRows.Should().Be(1);
            negative.Should().Throw<ArgumentException>().WithParameterName("times");
            mismatch.Should().Throw<ArgumentException>();
            badEvent.Should().Throw<ArgumentException>().WithParameterName("events");
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "EstimateKaplanMeierSurvival")]
        public void EstimateKaplanMeierSurvival_ConfidenceMethodsOrderAsExpected()
        {
            var logLog = estimation.EstimateKaplanMeierSurvival(freireichTreatedTimes, freireichTreatedEvents);

            var plain = estimation.EstimateKaplanMeierSurvival(freireichTreatedTimes, freireichTreatedEvents, 0.95,
                SurvivalIntervalMethod.Plain);

            var thinned = estimation.EstimateKaplanMeierSurvival(freireichTreatedTimes, freireichTreatedEvents, 0.95,
                SurvivalIntervalMethod.Log, false, 3);

            plain.Rows[0].Lower.Should().BeApproximately(0.857143 - 1.959964 * 0.07636, 2e-5);
            logLog.Rows[0].Lower.Should().BeLessThan(plain.Rows[0].Lower);
            thinned.Rows.Should().HaveCount(3);
            thinned.Rows[^1].Time.Should().Be(23d);
        }

        [Fact]
        [Trait("Covers", "EstimateKaplanMeierByGroup")]
        public void EstimateKaplanMeierByGroup_MatchesFreireichMediansAndLogRank()
        {
            var result = estimation.EstimateKaplanMeierByGroup(FreireichTimes(), FreireichEvents(), FreireichGroups(),
                [6d, 100d]);

            result.Groups.Select(g => g.Group).Should().Equal("6-MP", "Placebo");
            result.Groups[0].MedianSurvivalTime.Should().Be(23d);
            result.Groups[1].MedianSurvivalTime.Should().Be(8d);
            result.Groups[0].Events.Should().Be(9);
            result.Groups[1].Events.Should().Be(21);
            result.Groups[0].SurvivalAtEvaluationTimes[0].Survival.Should().BeApproximately(18d / 21d, Tight);
            result.LogRankChiSquare.Should().BeApproximately(16.79, 0.05);
            result.LogRankDegreesOfFreedom.Should().Be(1);
            result.LogRankPValue.Should().BeLessThan(0.0001);
        }

        [Fact]
        [Trait("Covers", "EstimateKaplanMeierByGroup")]
        public void EstimateKaplanMeierByGroup_SingleGroupHasNoLogRankAndBlankGroupsAreDropped()
        {
            var result = estimation.EstimateKaplanMeierByGroup([1d, 2d, 3d], [1, 1, 1],
                ["g", "g", ""]);

            Action mismatch = () =>
                estimation.EstimateKaplanMeierByGroup([1d], [1], ["a", "b"]);

            Action badTimes = () =>
                estimation.EstimateKaplanMeierByGroup([1d], [1], ["a"], [-1d]);

            result.Groups.Should().HaveCount(1);
            double.IsNaN(result.LogRankChiSquare).Should().BeTrue();
            result.SkippedRows.Should().Be(1);
            mismatch.Should().Throw<ArgumentException>();
            badTimes.Should().Throw<ArgumentException>().WithParameterName("evaluationTimes");
        }

        [Fact]
        [Trait("Covers", "EstimateNelsonAalenCumulativeHazard")]
        public void EstimateNelsonAalenCumulativeHazard_MatchesHarmonicSumsByHand()
        {
            var result = estimation.EstimateNelsonAalenCumulativeHazard(allFour, allEvents);

            result.Rows.Select(r => r.CumulativeHazard).Should().BeEquivalentTo([
                0.25, 0.25 + 1d / 3d, 0.25 + 1d / 3d + 0.5, 0.25 + 1d / 3d + 0.5 + 1d
            ]);

            result.Rows[0].StandardError.Should().BeApproximately(0.25, Tight);
            result.Rows[1].StandardError.Should().BeApproximately(Math.Sqrt(1d / 16d + 1d / 9d), Tight);
            result.Rows[1].Survival.Should().BeApproximately(Math.Exp(-(0.25 + 1d / 3d)), Tight);
            result.Rows[0].Lower.Should().BeLessThan(0.25);
            result.Rows[0].Upper.Should().BeGreaterThan(0.25);
        }

        [Fact]
        [Trait("Covers", "EstimateNelsonAalenCumulativeHazard")]
        public void EstimateNelsonAalenCumulativeHazard_TiesAndCensoringAreCountedAtRisk()
        {
            var result =
                estimation.EstimateNelsonAalenCumulativeHazard([1d, 1d, 2d, 3d], [1, 1, 0, 1]);

            Action level = () => estimation.EstimateNelsonAalenCumulativeHazard(allFour, allEvents, 0d);

            result.Rows.Should().HaveCount(2);
            result.Rows[0].CumulativeHazard.Should().BeApproximately(0.5, Tight);
            result.Rows[1].AtRisk.Should().Be(1);
            result.Rows[1].CumulativeHazard.Should().BeApproximately(1.5, Tight);
            level.Should().Throw<ArgumentException>().WithParameterName("confidenceLevel");
        }

        [Fact]
        [Trait("Covers", "EvaluateSurvivalAtTimes")]
        public void EvaluateSurvivalAtTimes_ReadsTheStepFunctionAtRequestedTimes()
        {
            var result = estimation.EvaluateSurvivalAtTimes(freireichTreatedTimes, freireichTreatedEvents,
                [0d, 6d, 8d, 100d]);

            result.Points[0].Survival.Should().Be(1d);
            result.Points[0].AtRisk.Should().Be(21);
            result.Points[1].Survival.Should().BeApproximately(18d / 21d, Tight);
            result.Points[1].AtRisk.Should().Be(21);
            result.Points[2].Survival.Should().BeApproximately(0.806723, 1e-6);
            result.Points[3].Survival.Should().BeApproximately(0.448179, 1e-6);
            result.Points[3].AtRisk.Should().Be(0);
            result.Points[1].CumulativeProbabilityOfEvent.Should().BeApproximately(3d / 21d, Tight);
            result.Points[1].CumulativeHazard.Should().BeApproximately(3d / 21d, Tight);
            result.MaximumObservedTime.Should().Be(35d);
        }

        [Fact]
        [Trait("Covers", "EvaluateSurvivalAtTimes")]
        public void EvaluateSurvivalAtTimes_RequiresAtLeastOneValidTime()
        {
            Action none = () => estimation.EvaluateSurvivalAtTimes(allFour, allEvents, []);
            Action nan = () => estimation.EvaluateSurvivalAtTimes(allFour, allEvents, [double.NaN]);

            none.Should().Throw<ArgumentException>().WithParameterName("evaluationTimes");
            nan.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CalculateSurvivalTimeQuantiles")]
        public void CalculateSurvivalTimeQuantiles_ReturnsMedianAndQuartilesWithBrookmeyerCrowleyLimits()
        {
            var result = estimation.CalculateSurvivalTimeQuantiles(freireichTreatedTimes, freireichTreatedEvents,
                [0.5, 0.25]);

            result.Quantiles[0].Time.Should().Be(23d);
            result.Quantiles[0].Lower.Should().Be(13d);
            double.IsNaN(result.Quantiles[0].Upper).Should().BeTrue();
            result.Quantiles[0].SurvivalLevel.Should().Be(0.5);
            result.Quantiles[1].Time.Should().Be(13d);
        }

        [Fact]
        [Trait("Covers", "CalculateSurvivalTimeQuantiles")]
        public void CalculateSurvivalTimeQuantiles_UnreachableQuantileIsNaNAndBadProportionsRejected()
        {
            var result = estimation.CalculateSurvivalTimeQuantiles(freireichTreatedTimes, freireichTreatedEvents,
                [0.9]);

            Action zero = () => estimation.CalculateSurvivalTimeQuantiles(allFour, allEvents, [0d]);
            Action none = () => estimation.CalculateSurvivalTimeQuantiles(allFour, allEvents, []);

            double.IsNaN(result.Quantiles[0].Time).Should().BeTrue();
            zero.Should().Throw<ArgumentException>().WithParameterName("proportions");
            none.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CalculateRestrictedMeanSurvivalTime")]
        public void CalculateRestrictedMeanSurvivalTime_MatchesAreaUnderTheStepCurveByHand()
        {
            var result = estimation.CalculateRestrictedMeanSurvivalTime(allFour, allEvents, 4d);

            result.RestrictedMeanSurvivalTime.Should().BeApproximately(2.5, Tight);
            result.StandardError.Should().BeApproximately(Math.Sqrt(0.3125), Tight);
            result.LifetimeLost.Should().BeApproximately(1.5, Tight);
            result.HorizonBeyondLastObservation.Should().BeFalse();
            result.Lower.Should().BeLessThan(2.5);
        }

        [Fact]
        [Trait("Covers", "CalculateRestrictedMeanSurvivalTime")]
        public void CalculateRestrictedMeanSurvivalTime_HorizonBeyondFollowUpIsFlaggedAndInvalidHorizonRejected()
        {
            var result = estimation.CalculateRestrictedMeanSurvivalTime([1d, 2d], [0, 0], 10d);
            Action zero = () => estimation.CalculateRestrictedMeanSurvivalTime(allFour, allEvents, 0d);

            result.RestrictedMeanSurvivalTime.Should().BeApproximately(10d, Tight);
            result.HorizonBeyondLastObservation.Should().BeTrue();
            zero.Should().Throw<ArgumentException>().WithParameterName("horizon");
        }

        [Fact]
        [Trait("Covers", "CompareRestrictedMeanSurvivalTime")]
        public void CompareRestrictedMeanSurvivalTime_MatchesHandComputedDifferenceAndZ()
        {
            var times = new double[] { 1, 2, 3, 4, 2, 4, 6, 8 };
            var events = Enumerable.Repeat(1, 8).ToArray();
            var groups = new[] { "A", "A", "A", "A", "B", "B", "B", "B" };

            var result = estimation.CompareRestrictedMeanSurvivalTime(times, events, groups, 4d);

            result.RestrictedMeanFirst.Should().BeApproximately(2.5, Tight);
            result.RestrictedMeanSecond.Should().BeApproximately(3.5, Tight);
            result.Difference.Should().BeApproximately(-1d, Tight);
            result.DifferenceStandardError.Should().BeApproximately(Math.Sqrt(0.3125 + 0.1875), Tight);
            result.ZStatistic.Should().BeApproximately(-1d / Math.Sqrt(0.5), Tight);
            result.PValue.Should().BeApproximately(0.1573, 0.0005);
            result.Ratio.Should().BeApproximately(2.5 / 3.5, Tight);
        }

        [Fact]
        [Trait("Covers", "CompareRestrictedMeanSurvivalTime")]
        public void CompareRestrictedMeanSurvivalTime_RequiresExactlyTwoGroups()
        {
            Action three = () => estimation.CompareRestrictedMeanSurvivalTime([1d, 2d, 3d], [1, 1, 1],
                ["a", "b", "c"], 2d);

            Action one = () => estimation.CompareRestrictedMeanSurvivalTime([1d, 2d], [1, 1],
                ["a", "a"], 2d);

            three.Should().Throw<ArgumentException>().WithParameterName("groups");
            one.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "EstimateMedianFollowUpTime")]
        public void EstimateMedianFollowUpTime_ReversesTheRolesOfEventsAndCensoring()
        {
            var result = estimation.EstimateMedianFollowUpTime(allFour, [1, 0, 1, 0]);

            result.MedianFollowUpReverseKaplanMeier.Should().Be(4d);
            result.MedianObservationTime.Should().Be(2.5);
            result.MaximumObservationTime.Should().Be(4d);
            result.Censored.Should().Be(2);
        }

        [Fact]
        [Trait("Covers", "EstimateMedianFollowUpTime")]
        public void EstimateMedianFollowUpTime_NoCensoringMeansNoReverseMedian()
        {
            var result = estimation.EstimateMedianFollowUpTime(allFour, allEvents);

            double.IsNaN(result.MedianFollowUpReverseKaplanMeier).Should().BeTrue();
            result.Subjects.Should().Be(4);
        }

        [Fact]
        [Trait("Covers", "TestLogRankSurvivalDifference")]
        public void TestLogRankSurvivalDifference_MatchesFreireichObservedAndExpectedCounts()
        {
            var result =
                estimation.TestLogRankSurvivalDifference(FreireichTimes(), FreireichEvents(), FreireichGroups());

            result.Groups[0].Observed.Should().Be(9);
            result.Groups[0].Expected.Should().BeApproximately(19.25, 0.005);
            result.Groups[1].Observed.Should().Be(21);
            result.Groups[1].Expected.Should().BeApproximately(10.75, 0.005);
            result.ChiSquare.Should().BeApproximately(16.79, 0.05);
            result.DegreesOfFreedom.Should().Be(1);
            result.PValue.Should().BeApproximately(4.17e-5, 1e-5);
            result.HazardRatioFirstVersusSecond.Should().BeLessThan(0.5);
            result.HazardRatioLower.Should().BeLessThan(result.HazardRatioFirstVersusSecond);
            result.HazardRatioUpper.Should().BeGreaterThan(result.HazardRatioFirstVersusSecond);
        }

        [Theory]
        [InlineData(LogRankWeighting.LogRank, 0.4848762603, 0.7666666667)]
        [InlineData(LogRankWeighting.GehanBreslow, 0.4090909091, 3.0)]
        [InlineData(LogRankWeighting.TaroneWare, 0.4380445165, 1.4600741924)]
        [InlineData(LogRankWeighting.PetoPeto, 0.4090909091, 0.4285714286)]
        [Trait("Covers", "TestLogRankSurvivalDifference")]
        public void TestLogRankSurvivalDifference_WeightedStatisticsMatchHandComputedInterleavedSample(
            LogRankWeighting weighting, double chiSquare, double score)
        {
            var times = new double[] { 1, 3, 5, 2, 4, 6 };
            var events = Enumerable.Repeat(1, 6).ToArray();
            var groups = new[] { "A", "A", "A", "B", "B", "B" };

            var result = estimation.TestLogRankSurvivalDifference(times, events, groups, weighting);

            result.ChiSquare.Should().BeApproximately(chiSquare, 1e-8);
            result.Groups[0].WeightedObservedMinusExpected.Should().BeApproximately(score, 1e-8);
            result.Groups[0].Observed.Should().Be(3);
            result.Groups[0].Expected.Should().BeApproximately(2.2333333333, 1e-9);
        }

        [Fact]
        [Trait("Covers", "TestLogRankSurvivalDifference")]
        public void TestLogRankSurvivalDifference_FlemingHarringtonReducesToLogRankAndTargetsEarlyOrLate()
        {
            var times = new double[] { 1, 3, 5, 2, 4, 6 };
            var events = Enumerable.Repeat(1, 6).ToArray();
            var groups = new[] { "A", "A", "A", "B", "B", "B" };

            var plain = estimation.TestLogRankSurvivalDifference(times, events, groups);

            var zero = estimation.TestLogRankSurvivalDifference(times, events, groups,
                LogRankWeighting.FlemingHarrington, 0d, 0d);

            var late = estimation.TestLogRankSurvivalDifference(times, events, groups,
                LogRankWeighting.FlemingHarrington);

            var early = estimation.TestLogRankSurvivalDifference(times, events, groups,
                LogRankWeighting.FlemingHarrington, 1d, 0d);

            zero.ChiSquare.Should().BeApproximately(plain.ChiSquare, 1e-12);
            late.ChiSquare.Should().BeApproximately(0.3535911602, 1e-8);
            early.ChiSquare.Should().BeApproximately(0.4090909091, 1e-8);
        }

        [Fact]
        [Trait("Covers", "TestLogRankSurvivalDifference")]
        public void TestLogRankSurvivalDifference_ThreeIdenticalGroupsGiveZeroChiSquareOnTwoDegrees()
        {
            var times = new double[] { 1, 2, 3, 1, 2, 3, 1, 2, 3 };
            var events = new[] { 1, 0, 1, 1, 0, 1, 1, 0, 1 };
            var groups = new[] { "a", "a", "a", "b", "b", "b", "c", "c", "c" };

            var result = estimation.TestLogRankSurvivalDifference(times, events, groups);

            Action single = () =>
                estimation.TestLogRankSurvivalDifference([1d, 2d], [1, 1], ["a", "a"]);

            Action exponent = () => estimation.TestLogRankSurvivalDifference(times, events, groups,
                LogRankWeighting.FlemingHarrington, -1d);

            result.DegreesOfFreedom.Should().Be(2);
            result.ChiSquare.Should().BeApproximately(0d, 1e-9);
            double.IsNaN(result.HazardRatioFirstVersusSecond).Should().BeTrue();
            single.Should().Throw<ArgumentException>();
            exponent.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "FitCoxProportionalHazards")]
        public void FitCoxProportionalHazards_MatchesBreslowSolutionForFreireichLeukaemiaData()
        {
            var covariates = FreireichGroups().Select(g => new[] { g == "Placebo" ? 1d : 0d }).ToArray();

            var result = models.FitCoxProportionalHazards(covariates, FreireichTimes(), FreireichEvents(),
                ["Placebo"]);

            var row = result.Coefficients.Single();
            row.Coefficient.Should().BeApproximately(1.5091914, 0.001);
            row.StandardError.Should().BeApproximately(0.4095644, 0.001);
            row.HazardRatio.Should().BeApproximately(4.523072, 0.01);
            row.HazardRatioLower.Should().BeLessThan(row.HazardRatio);
            row.PValue.Should().BeLessThan(0.001);
            result.LogLikelihood.Should().BeApproximately(-86.3796221, 0.01);
            result.NullLogLikelihood.Should().BeApproximately(-93.9850505, 0.01);
            result.LikelihoodRatioChiSquare.Should().BeApproximately(15.2108568, 0.02);

            result.LikelihoodRatioChiSquare.Should()
                .BeApproximately(2d * (result.LogLikelihood - result.NullLogLikelihood), 1e-9);

            result.ConcordanceIndex.Should().BeApproximately(0.6900421, 0.001);
            result.Converged.Should().BeTrue();
            result.Events.Should().Be(30);
            result.Subjects.Should().Be(42);
            result.TieMethod.Should().Contain("Breslow");
        }

        [Fact]
        [Trait("Covers", "FitCoxProportionalHazards")]
        public void FitCoxProportionalHazards_PredictionsFollowTheProportionalHazardsIdentity()
        {
            var covariates = FreireichGroups().Select(g => new[] { g == "Placebo" ? 1d : 0d }).ToArray();

            var result = models.FitCoxProportionalHazards(covariates, FreireichTimes(), FreireichEvents(), null, 0.95,
                50, 1e-6, [[1d], [0d]], [5d, 10d, 100d]);

            var placebo = result.Predictions[0];
            var treated = result.Predictions[1];
            var hazardRatio = Math.Exp(result.Coefficients[0].Coefficient);
            placebo.RelativeHazard.Should().BeApproximately(treated.RelativeHazard * hazardRatio, 1e-9);

            for (var i = 0; i < 3; i++)
            {
                placebo.SurvivalAtPredictionTimes[i].Should()
                    .BeApproximately(Math.Pow(treated.SurvivalAtPredictionTimes[i], hazardRatio), 1e-9);
            }

            treated.SurvivalAtPredictionTimes[0].Should().BeGreaterThan(treated.SurvivalAtPredictionTimes[1]);
            result.BaselineRows.Select(r => r.CumulativeHazard).Should().BeInAscendingOrder();
            result.Coefficients[0].Name.Should().Be("Covariate1");
            result.CovariateMeans[0].Should().BeApproximately(21d / 42d, Tight);
        }

        [Fact]
        [Trait("Covers", "FitCoxProportionalHazards")]
        public void FitCoxProportionalHazards_RejectsBadInputAndWarnsOnFewEvents()
        {
            var covariates = new[] { new[] { 1d }, new[] { 0d }, new[] { 1d }, new[] { 0d } };
            var times = new double[] { 1, 2, 3, 4 };
            var events = new[] { 1, 1, 1, 0 };

            Action constant = () =>
                models.FitCoxProportionalHazards([[1d], [1d]], [1d, 2d],
                    [1, 1]);

            Action noEvents = () => models.FitCoxProportionalHazards(covariates, times, [0, 0, 0, 0]);

            Action ragged = () =>
                models.FitCoxProportionalHazards([[1d], [1d, 2d]], [1d, 2d],
                    [1, 1]);

            Action mismatch = () => models.FitCoxProportionalHazards(covariates, [1d], events);

            Action needTimes = () => models.FitCoxProportionalHazards(covariates, times, events, null, 0.95, 50, 1e-6,
                [[1d]]);

            var tooMany = Enumerable.Range(0, 10001).Select(i => new[] { (double)(i % 2) }).ToArray();

            Action big = () => models.FitCoxProportionalHazards(tooMany,
                [.. Enumerable.Range(0, 10001).Select(i => (double)i)],
                [.. Enumerable.Repeat(1, 10001)]);

            var few = models.FitCoxProportionalHazards(
                [[1d], [0d], [1d], [0d], [1d], [0d]],
                [1, 2, 3, 4, 5, 6], [1, 0, 1, 1, 0, 1]);

            constant.Should().Throw<ArgumentException>().WithMessage("*constant*");
            noEvents.Should().Throw<ArgumentException>();
            ragged.Should().Throw<ArgumentException>();
            mismatch.Should().Throw<ArgumentException>();
            needTimes.Should().Throw<ArgumentException>().WithParameterName("predictionTimes");
            big.Should().Throw<ArgumentException>().WithMessage("*10000*");
            few.Warnings.Should().Contain(w => w.Contains("events for"));
        }

        [Fact]
        [Trait("Covers", "FitCoxProportionalHazards")]
        public void FitCoxProportionalHazards_IsInvariantToTimeUnitsAndDropsIncompleteRows()
        {
            var covariates = FreireichGroups().Select(g => new[] { g == "Placebo" ? 1d : 0d }).ToArray();
            var scaled = FreireichTimes().Select(t => t * 1e-9).ToArray();
            var withNaN = covariates.Concat([[double.NaN]]).ToArray();

            var baseline = models.FitCoxProportionalHazards(covariates, FreireichTimes(), FreireichEvents());
            var tiny = models.FitCoxProportionalHazards(covariates, scaled, FreireichEvents());

            var dropped = models.FitCoxProportionalHazards(withNaN, [.. FreireichTimes(), 1d],
                [.. FreireichEvents(), 1]);

            tiny.Coefficients[0].Coefficient.Should().BeApproximately(baseline.Coefficients[0].Coefficient, 1e-6);
            dropped.SkippedRows.Should().Be(1);
            dropped.Coefficients[0].Coefficient.Should().BeApproximately(baseline.Coefficients[0].Coefficient, 1e-6);
        }

        [Fact]
        [Trait("Covers", "CalculateHarrellConcordanceIndex")]
        public void CalculateHarrellConcordanceIndex_PerfectReversedAndConstantRankings()
        {
            var perfect = models.CalculateHarrellConcordanceIndex(allFour, allEvents, [4d, 3d, 2d, 1d]);
            var reversed = models.CalculateHarrellConcordanceIndex(allFour, allEvents, [1d, 2d, 3d, 4d]);
            var constant = models.CalculateHarrellConcordanceIndex(allFour, allEvents, [1d, 1d, 1d, 1d]);

            perfect.ConcordanceIndex.Should().Be(1d);
            perfect.SomersD.Should().Be(1d);
            perfect.ComparablePairs.Should().Be(6d);
            reversed.ConcordanceIndex.Should().Be(0d);
            constant.ConcordanceIndex.Should().Be(0.5);
            constant.TiedRiskPairs.Should().Be(6d);
        }

        [Fact]
        [Trait("Covers", "CalculateHarrellConcordanceIndex")]
        public void CalculateHarrellConcordanceIndex_CensoredShorterCasesAreNotComparable()
        {
            var censored =
                models.CalculateHarrellConcordanceIndex([1d, 2d, 3d], [1, 0, 1], [3d, 2d, 1d]);

            var tiedTimes =
                models.CalculateHarrellConcordanceIndex([2d, 2d], [1, 0], [1d, 2d]);

            Action mismatch = () => models.CalculateHarrellConcordanceIndex(allFour, allEvents, [1d]);

            Action noPairs = () =>
                models.CalculateHarrellConcordanceIndex([double.NaN], [1], [1d]);

            var none = models.CalculateHarrellConcordanceIndex([1d, 2d], [0, 0], [1d, 2d]);

            censored.ComparablePairs.Should().Be(2d);
            censored.ConcordanceIndex.Should().Be(1d);
            tiedTimes.ConcordanceIndex.Should().Be(0d);
            double.IsNaN(none.ConcordanceIndex).Should().BeTrue();
            mismatch.Should().Throw<ArgumentException>();
            noPairs.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "FitExponentialSurvivalModel")]
        public void FitExponentialSurvivalModel_MatchesClosedFormMaximumLikelihood()
        {
            var result = models.FitExponentialSurvivalModel([1, 2, 3, 4, 5], [1, 1, 1, 1, 1],
                [3d]);

            result.Rate.Should().BeApproximately(1d / 3d, Tight);
            result.MeanLifetime.Should().BeApproximately(3d, Tight);
            result.MedianLifetime.Should().BeApproximately(Math.Log(2d) * 3d, Tight);
            result.TotalTimeAtRisk.Should().Be(15d);
            result.RateLower.Should().BeApproximately(1.6234863901 / 15d, 1e-6);
            result.RateUpper.Should().BeApproximately(11.6683320793 / 15d, 1e-6);
            result.SurvivalAtTimes[0].Survival.Should().BeApproximately(Math.Exp(-1d), Tight);
            result.MaximumAbsoluteDifferenceFromKaplanMeier.Should().BeApproximately(Math.Exp(-5d / 3d), 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitExponentialSurvivalModel")]
        public void FitExponentialSurvivalModel_NoEventsAndZeroTimeAreHandled()
        {
            var none = models.FitExponentialSurvivalModel([5d, 6d], [0, 0]);
            Action zero = () => models.FitExponentialSurvivalModel([0d, 0d], [1, 1]);
            Action badTime = () => models.FitExponentialSurvivalModel(allFour, allEvents, [-1d]);

            none.Rate.Should().Be(0d);
            double.IsNaN(none.MeanLifetime).Should().BeTrue();
            none.RateUpper.Should().BeApproximately(3.6888794541 / 11d, 1e-6);
            zero.Should().Throw<ArgumentException>();
            badTime.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CalculateIncidenceRatesByGroup")]
        public void CalculateIncidenceRatesByGroup_MatchesPersonTimeRatesAndExactRatio()
        {
            var times = new double[] { 10, 10, 10, 5, 5 };
            var events = new[] { 1, 0, 0, 1, 1 };
            var groups = new[] { "A", "A", "A", "B", "B" };

            var result = models.CalculateIncidenceRatesByGroup(times, events, groups);

            result.ReferenceGroup.Should().Be("A");
            result.Groups[0].Rate.Should().BeApproximately(1d / 30d, Tight);
            result.Groups[0].RateRatioVersusReference.Should().Be(1d);
            result.Groups[1].PersonTime.Should().Be(10d);
            result.Groups[1].Rate.Should().BeApproximately(0.2, Tight);
            result.Groups[1].RateRatioVersusReference.Should().BeApproximately(6d, Tight);
            result.Groups[1].PValueVersusReference.Should().BeApproximately(0.15625, 1e-9);
            result.Groups[1].RateRatioLower.Should().BeApproximately(0.31235, 0.001);

            result.Groups[1].RateLower.Should()
                .BeApproximately(1.6234863901 / 10d * 0d + 0.024220927854, 1e-6 + 0.0000001);

            result.Overall.Events.Should().Be(3);
            result.Overall.PersonTime.Should().Be(40d);
        }

        [Fact]
        [Trait("Covers", "CalculateIncidenceRatesByGroup")]
        public void CalculateIncidenceRatesByGroup_UnknownReferenceAndZeroPersonTime()
        {
            Action unknown = () =>
                models.CalculateIncidenceRatesByGroup([1d], [1], ["a"], "z");

            var zero = models.CalculateIncidenceRatesByGroup([0d, 5d], [1, 1], ["a", "b"]);

            unknown.Should().Throw<ArgumentException>().WithParameterName("referenceGroup");
            double.IsNaN(zero.Groups[0].Rate).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "TabulateHazardRateByInterval")]
        public void TabulateHazardRateByInterval_MatchesHandComputedPersonTime()
        {
            var times = new double[] { 1, 1, 2, 3, 3, 4, 5, 6, 7, 8 };
            var events = new[] { 1, 0, 1, 1, 0, 1, 0, 1, 0, 1 };

            var result = models.TabulateHazardRateByInterval(times, events, [0d, 4d]);

            result.Intervals[0].EnteringSubjects.Should().Be(10);
            result.Intervals[0].Events.Should().Be(3);
            result.Intervals[0].Censored.Should().Be(2);
            result.Intervals[0].PersonTime.Should().BeApproximately(30d, Tight);
            result.Intervals[0].HazardRate.Should().BeApproximately(0.1, Tight);
            result.Intervals[0].ConditionalEventProbability.Should().BeApproximately(1d - Math.Exp(-0.4), Tight);
            result.Intervals[1].EnteringSubjects.Should().Be(5);
            result.Intervals[1].PersonTime.Should().BeApproximately(10d, Tight);
            result.Intervals[1].HazardRate.Should().BeApproximately(0.3, Tight);
            double.IsNaN(result.Intervals[1].End).Should().BeTrue();
            double.IsNaN(result.Intervals[1].ConditionalEventProbability).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "TabulateHazardRateByInterval")]
        public void TabulateHazardRateByInterval_RejectsBadBreaks()
        {
            Action decreasing = () => models.TabulateHazardRateByInterval(allFour, allEvents, [0d, 5d, 3d]);
            Action negative = () => models.TabulateHazardRateByInterval(allFour, allEvents, [-1d]);
            Action none = () => models.TabulateHazardRateByInterval(allFour, allEvents, []);
            var empty = models.TabulateHazardRateByInterval(allFour, allEvents, [0d, 100d]);

            decreasing.Should().Throw<ArgumentException>().WithParameterName("breaks");
            negative.Should().Throw<ArgumentException>();
            none.Should().Throw<ArgumentException>();
            double.IsNaN(empty.Intervals[1].HazardRate).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "BuildActuarialLifeTable")]
        public void BuildActuarialLifeTable_MatchesHandComputedIntervals()
        {
            var times = new double[] { 1, 1, 2, 3, 3, 4, 5, 6, 7, 8 };
            var events = new[] { 1, 0, 1, 1, 0, 1, 0, 1, 0, 1 };

            var result = models.BuildActuarialLifeTable(times, events, [0d, 4d]);

            var first = result.Intervals[0];
            first.Entering.Should().Be(10);
            first.Withdrawn.Should().Be(2);
            first.Events.Should().Be(3);
            first.EffectiveAtRisk.Should().BeApproximately(9d, Tight);
            first.ConditionalProbabilityOfEvent.Should().BeApproximately(1d / 3d, Tight);
            first.SurvivalAtEnd.Should().BeApproximately(2d / 3d, Tight);
            first.SurvivalStandardError.Should().BeApproximately(0.1571348403, 1e-9);
            first.Hazard.Should().BeApproximately(0.1, Tight);
            var second = result.Intervals[1];
            second.EffectiveAtRisk.Should().BeApproximately(4d, Tight);
            second.ConditionalProbabilityOfEvent.Should().BeApproximately(0.75, Tight);
            second.SurvivalAtEnd.Should().BeApproximately(1d / 6d, Tight);
            double.IsNaN(second.Hazard).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "BuildActuarialLifeTable")]
        public void BuildActuarialLifeTable_EmptyIntervalsCarrySurvivalForward()
        {
            var result = models.BuildActuarialLifeTable([1d, 2d], [1, 0], [0d, 50d]);

            result.Intervals[1].Entering.Should().Be(0);
            double.IsNaN(result.Intervals[1].ConditionalProbabilityOfEvent).Should().BeTrue();
            result.Intervals[1].SurvivalAtEnd.Should().Be(result.Intervals[0].SurvivalAtEnd);
        }

        [Fact]
        [Trait("Covers", "CalculateCumulativeIncidenceCompetingRisks")]
        public void CalculateCumulativeIncidenceCompetingRisks_MatchesAalenJohansenByHand()
        {
            var result = models.CalculateCumulativeIncidenceCompetingRisks(allFour, [1, 2, 1, 0]);

            result.EventTypes.Should().Equal(1, 2);
            result.Rows.Should().HaveCount(3);
            result.Rows[0].CumulativeIncidenceByType.Should().Equal(0.25, 0d);
            result.Rows[1].CumulativeIncidenceByType[1].Should().BeApproximately(0.25, Tight);
            result.FinalCumulativeIncidenceByType[0].Should().BeApproximately(0.5, Tight);
            result.FinalCumulativeIncidenceByType[1].Should().BeApproximately(0.25, Tight);
            result.FinalOverallSurvival.Should().BeApproximately(0.25, Tight);

            (result.FinalCumulativeIncidenceByType.Sum() + result.FinalOverallSurvival).Should()
                .BeApproximately(1d, Tight);

            result.TotalEventsByType.Should().Equal(2, 1);
            result.Censored.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "CalculateCumulativeIncidenceCompetingRisks")]
        public void CalculateCumulativeIncidenceCompetingRisks_RejectsNegativeCodesAndNoEvents()
        {
            Action negative = () => models.CalculateCumulativeIncidenceCompetingRisks([1d], [-1]);
            Action none = () => models.CalculateCumulativeIncidenceCompetingRisks([1d, 2d], [0, 0]);
            Action mismatch = () => models.CalculateCumulativeIncidenceCompetingRisks([1d], [1, 1]);

            negative.Should().Throw<ArgumentException>().WithParameterName("eventTypes");
            none.Should().Throw<ArgumentException>();
            mismatch.Should().Throw<ArgumentException>();
        }
    }
}