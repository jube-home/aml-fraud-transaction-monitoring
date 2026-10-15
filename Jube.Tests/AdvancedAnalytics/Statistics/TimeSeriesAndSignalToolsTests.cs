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
using Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal;
using Xunit;
using Accord.Statistics.Distributions.Univariate;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class TimeSeriesAndSignalToolsTests
    {
        private readonly SeriesAnomalyTools anomaly = new();

        private static double[] StepWithNoise(params (int Length, double Level)[] parts)
        {
            var result = new List<double>();
            var index = 0;

            foreach (var (length, level) in parts)
            {
                for (var i = 0; i < length; i++)
                {
                    result.Add(level + (index * 7 % 5 - 2) * 0.05);
                    index++;
                }
            }

            return [.. result];
        }

        [Fact]
        [Trait("Covers", "CusumChangeDetection")]
        public void CusumAlarmsThreePointsAfterUpwardStepByHandComputation()
        {
            var v = Enumerable.Repeat(0.0, 10).Concat(Enumerable.Repeat(3.0, 5)).ToArray();

            var result = anomaly.CusumChangeDetection(v, 0, 1);

            result.UpperSums[10].Should().BeApproximately(2.5, 1e-12);
            result.UpperSums[11].Should().BeApproximately(5.0, 1e-12);
            result.AlarmIndices.Should().Equal(12);
            result.AlarmDirections.Should().Equal("Up");
            result.EstimatedChangeStarts.Should().Equal(10);
            result.LowerSums.Should().OnlyContain(s => s == 0);
        }

        [Fact]
        [Trait("Covers", "CusumChangeDetection")]
        public void CusumDetectsDownwardShiftWithEstimatedBaselineAndValidatesConstantBaseline()
        {
            var v = Enumerable.Range(0, 20).Select(i => i % 2 == 0 ? 1.0 : -1.0)
                .Concat(Enumerable.Repeat(-4.0, 8)).ToArray();

            var result = anomaly.CusumChangeDetection(v, baselineCount: 20);

            result.AlarmDirections.First().Should().Be("Down");
            result.AlarmIndices.First().Should().BeInRange(20, 24);
            result.Target.Should().BeApproximately(0, 1e-12);
            Action constant = () => anomaly.CusumChangeDetection(new double[12]);
            constant.Should().Throw<ArgumentException>().WithParameterName("standardDeviation");
            Action badH = () => anomaly.CusumChangeDetection(v, 0, 1, 0, 0.5, 0);
            badH.Should().Throw<ArgumentException>().WithParameterName("hSigma");
        }

        [Fact]
        [Trait("Covers", "PageHinkleyDriftDetection")]
        public void PageHinkleyAlarmsOnStepUpAndStepDownByHand()
        {
            var up = anomaly.PageHinkleyDriftDetection([0.0, 0, 0, 10, 10, 10]);
            up.AlarmIndices.Should().Equal(3);
            up.AlarmDirections.Should().Equal("Up");
            up.IncreaseStatistic[3].Should().BeApproximately(7.5, 1e-12);

            var down = anomaly.PageHinkleyDriftDetection([10.0, 10, 10, 0, 0, 0]);
            down.AlarmIndices.Should().Equal(3);
            down.AlarmDirections.Should().Equal("Down");
        }

        [Fact]
        [Trait("Covers", "PageHinkleyDriftDetection")]
        public void PageHinkleyConstantSeriesIsQuietAndDeltaAbsorbsSmallChange()
        {
            anomaly.PageHinkleyDriftDetection([.. Enumerable.Repeat(4.0, 30)]).AlarmIndices.Should().BeEmpty();
            anomaly.PageHinkleyDriftDetection([0.0, 0, 0, 1, 1, 1], 2, 1).AlarmIndices.Should().BeEmpty();
            Action bad = () => anomaly.PageHinkleyDriftDetection(oneToFive, -1);
            bad.Should().Throw<ArgumentException>().WithParameterName("delta");
        }

        [Fact]
        [Trait("Covers", "EwmaControlChart")]
        public void EwmaFirstLimitMatchesClosedFormAndTwoSigmaShiftAlarmsAtThirdPoint()
        {
            var first = anomaly.EwmaControlChart([1.0, 0, 0, 0], 0.2, 3, 0, 1);

            first.Ewma[0].Should().BeApproximately(0.2, 1e-12);
            first.UpperLimits[0].Should().BeApproximately(0.6, 1e-12);
            first.LowerLimits[0].Should().BeApproximately(-0.6, 1e-12);
            first.AlarmIndices.Should().BeEmpty();

            var shift = anomaly.EwmaControlChart([2.0, 2, 2, 2, 2], 0.2, 3, 0, 1);
            shift.AlarmIndices.First().Should().Be(2);
            shift.Ewma[2].Should().BeApproximately(2 * (1 - Math.Pow(0.8, 3)), 1e-12);
        }

        [Fact]
        [Trait("Covers", "EwmaControlChart")]
        public void EwmaValidatesParametersAndStaysQuietInControl()
        {
            anomaly.EwmaControlChart([0.1, -0.1, 0.05, -0.05, 0.0, 0.1], 0.2, 3, 0, 1).AlarmIndices.Should()
                .BeEmpty();

            Action badLambda = () => anomaly.EwmaControlChart(oneToFive, 0);
            badLambda.Should().Throw<ArgumentException>().WithParameterName("lambda");
            Action badWidth = () => anomaly.EwmaControlChart(oneToFive, 0.2, 0);
            badWidth.Should().Throw<ArgumentException>().WithParameterName("limitWidth");
        }

        [Fact]
        [Trait("Covers", "ShewhartControlChartAlarms")]
        public void ShewhartRuleOneFlagsPointBeyondThreeSigma()
        {
            var result = anomaly.ShewhartControlChartAlarms([0.0, 0, 0, 4, 0, 0], 0, 1);

            result.BeyondThreeSigma.Should().Equal(3);
            result.AlarmIndices.Should().Contain(3);
            result.ZScores[3].Should().Be(4);
        }

        [Fact]
        [Trait("Covers", "ShewhartControlChartAlarms")]
        public void ShewhartRunRulesTwoThreeAndFour()
        {
            anomaly.ShewhartControlChartAlarms([2.5, 0, 2.5], 0, 1).TwoOfThreeBeyondTwoSigma.Should().Equal(2);

            anomaly.ShewhartControlChartAlarms([1.5, 1.5, 0, 1.5, 1.5], 0, 1).FourOfFiveBeyondOneSigma.Should()
                .Equal(4);

            var eight = anomaly.ShewhartControlChartAlarms([.. Enumerable.Repeat(0.5, 9)], 0, 1);
            eight.EightInARowOneSide.Should().Equal(7, 8);

            anomaly.ShewhartControlChartAlarms([0.5, -0.5, 0.2, -0.2, 0.1], 0, 1).AlarmIndices.Should()
                .BeEmpty();
        }

        [Fact]
        [Trait("Covers", "SeriesSpikeDetection")]
        public void SpikeDetectionGlobalRobustScoreMatchesHandComputation()
        {
            var v = Enumerable.Range(1, 10).Select(i => (double)i).Concat([100.0]).ToArray();

            var result = anomaly.SeriesSpikeDetection(v, SpikeMethod.GlobalRobustZScore);

            result.SpikeIndices.Should().Equal(10);
            result.Scores[10].Should().BeApproximately(94.0 / (1.4826 * 3.0), 1e-9);
            result.SpikeValues.Should().Equal(100.0);
            anomaly.SeriesSpikeDetection(v, SpikeMethod.InterquartileFence, 30, 1.5).SpikeIndices.Should().Equal(10);
        }

        [Fact]
        [Trait("Covers", "SeriesSpikeDetection")]
        public void SpikeDetectionRollingMethodsFlagSpikeAndDipAgainstQuietHistory()
        {
            var v = Enumerable.Range(0, 30).Select(i => 10 + (i * 7 % 5 - 2) * 0.1).ToArray();
            v[20] = 25;
            v[25] = -5;

            var robust = anomaly.SeriesSpikeDetection(v, SpikeMethod.RollingRobustZScore, 10);
            var zscore = anomaly.SeriesSpikeDetection(v, SpikeMethod.RollingZScore, 10);

            robust.SpikeIndices.Should().Contain([20, 25]);
            zscore.SpikeIndices.Should().Contain(20);
            robust.Scores[25].Should().BeLessThan(0);
            robust.Scores[0].Should().Be(double.NaN);

            var constantBaseline =
                anomaly.SeriesSpikeDetection([5.0, 5, 5, 5, 5, 5, 9], SpikeMethod.RollingZScore, 5);
            constantBaseline.SpikeIndices.Should().Equal(6);
            constantBaseline.Scores[6].Should().Be(double.PositiveInfinity);
        }

        [Fact]
        [Trait("Covers", "LevelShiftScan")]
        public void LevelShiftScanLocatesStepAtIndexTwenty()
        {
            var v = StepWithNoise((20, 0), (20, 10));

            var result = anomaly.LevelShiftScan(v);

            result.ShiftIndices.Should().Equal(20);
            result.ShiftSizes[0].Should().BeApproximately(10, 0.1);
            result.ShiftScores[0].Should().BeGreaterThan(20);
        }

        [Fact]
        [Trait("Covers", "LevelShiftScan")]
        public void LevelShiftScanFindsNoShiftInStationaryNoiseAndValidatesWindow()
        {
            anomaly.LevelShiftScan(StepWithNoise((40, 3))).ShiftIndices.Should().BeEmpty();
            Action tooShort = () => anomaly.LevelShiftScan(new double[10], 8);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("window");

            var constantStep =
                anomaly.LevelShiftScan([.. Enumerable.Repeat(0.0, 10), .. Enumerable.Repeat(5.0, 10)], 5);
            constantStep.ShiftIndices.Should().Equal(10);
            constantStep.ShiftScores[0].Should().Be(double.PositiveInfinity);
        }

        [Fact]
        [Trait("Covers", "MeanShiftChangePoints")]
        public void MeanShiftFindsTwoChangePointsOfThreeLevelSeries()
        {
            var v = StepWithNoise((20, 0), (20, 10), (20, 0));

            var result = anomaly.MeanShiftChangePoints(v);

            result.ChangePointIndices.Should().Equal(20, 40);
            result.Segments.Should().HaveCount(3);
            result.Segments[0].Mean.Should().BeApproximately(0, 0.05);
            result.Segments[1].Mean.Should().BeApproximately(10, 0.05);
            result.Segments[1].Count.Should().Be(20);
            result.NoiseStandardDeviation.Should().BeGreaterThan(0);
        }

        [Fact]
        [Trait("Covers", "MeanShiftChangePoints")]
        public void MeanShiftFindsNothingInNoiseHonoursMaxAndValidates()
        {
            anomaly.MeanShiftChangePoints(StepWithNoise((60, 2))).ChangePointIndices.Should().BeEmpty();
            var capped = anomaly.MeanShiftChangePoints(StepWithNoise((20, 0), (20, 10), (20, 0)), 1);
            capped.ChangePointIndices.Should().HaveCount(1);
            Action tooShort = () => anomaly.MeanShiftChangePoints(new double[8]);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("minSegmentLength");
            Action bad = () => anomaly.MeanShiftChangePoints(new double[20], 0);
            bad.Should().Throw<ArgumentException>().WithParameterName("maxChangePoints");
        }

        [Fact]
        [Trait("Covers", "DrawdownAnalysis")]
        public void DrawdownMatchesHandComputedBalanceHistory()
        {
            var result = anomaly.DrawdownAnalysis([100.0, 120, 90, 110, 80, 130]);

            result.Drawdowns.Should().Equal(0, 0, -30, -10, -40, 0);
            result.MaximumDrawdown.Should().Be(40);
            result.MaximumDrawdownFraction.Should().BeApproximately(1.0 / 3.0, 1e-12);
            result.PeakIndex.Should().Be(1);
            result.TroughIndex.Should().Be(4);
            result.RecoveryIndex.Should().Be(5);
            result.LongestUnderwaterSteps.Should().Be(3);
            result.CurrentUnderwaterSteps.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "DrawdownAnalysis")]
        public void DrawdownWithoutRecoveryOrWithoutAnyFall()
        {
            var open = anomaly.DrawdownAnalysis([10.0, 12, 8]);
            open.RecoveryIndex.Should().Be(-1);
            open.CurrentUnderwaterSteps.Should().Be(1);

            var rising = anomaly.DrawdownAnalysis([1.0, 2, 3]);
            rising.MaximumDrawdown.Should().Be(0);
            rising.PeakIndex.Should().Be(-1);
            rising.MaximumDrawdownFraction.Should().Be(double.NaN);
            Action one = () => anomaly.DrawdownAnalysis([1.0]);
            one.Should().Throw<ArgumentException>().WithParameterName("levels");
        }

        private static readonly double[] oneToFive = [1, 2, 3, 4, 5];
        private readonly SeriesDependenceTools dependence = new();

        [Fact]
        [Trait("Covers", "AutocorrelationFunction")]
        public void AutocorrelationOfOneToFiveMatchesHandComputedBiasedEstimator()
        {
            var result = dependence.AutocorrelationFunction(oneToFive, 4);

            result.Lags.Should().Equal(0, 1, 2, 3, 4);
            result.Values.Should().HaveCount(5);
            result.Values[0].Should().BeApproximately(1.0, 1e-12);
            result.Values[1].Should().BeApproximately(0.4, 1e-9);
            result.Values[2].Should().BeApproximately(-0.1, 1e-9);
            result.Values[3].Should().BeApproximately(-0.4, 1e-9);
            result.Values[4].Should().BeApproximately(-0.4, 1e-9);
            result.WhiteNoiseBound.Should().BeApproximately(1.959963984540054 / Math.Sqrt(5), 1e-9);
        }

        [Fact]
        [Trait("Covers", "AutocorrelationFunction")]
        public void AutocorrelationOfPeriodicSeriesPeaksAtThePeriodAndFlagsSignificantLags()
        {
            var series = Enumerable.Range(0, 64).Select(i => Math.Sin(2 * Math.PI * i / 8)).ToArray();

            var result = dependence.AutocorrelationFunction(series, 16);

            result.Values[8].Should().BeGreaterThan(0.7);
            result.Values[4].Should().BeLessThan(-0.7);
            result.SignificantLags.Should().Contain([1, 4, 8]);
            result.BartlettBounds[1].Should().BeApproximately(result.WhiteNoiseBound, 1e-9);
            result.BartlettBounds[2].Should().BeGreaterThan(result.BartlettBounds[1]);
        }

        [Fact]
        [Trait("Covers", "AutocorrelationFunction")]
        public void AutocorrelationEdgeCases()
        {
            var constant = dependence.AutocorrelationFunction([3.0, 3.0, 3.0, 3.0], 2);
            constant.Values[0].Should().Be(1);
            constant.Values[1].Should().Be(double.NaN);
            constant.Values[2].Should().Be(double.NaN);

            var capped = dependence.AutocorrelationFunction([1.0, 5.0, 2.0], 50);
            capped.Lags.Should().Equal(0, 1, 2);

            var huge = dependence.AutocorrelationFunction([.. oneToFive.Select(v => v * 1e12 + 1e15)], 2);
            huge.Values[1].Should().BeApproximately(0.4, 1e-6);

            Action empty = () => dependence.AutocorrelationFunction([]);
            empty.Should().Throw<ArgumentException>().WithParameterName("values");
            Action nan = () => dependence.AutocorrelationFunction([1.0, double.NaN, 3.0]);
            nan.Should().Throw<ArgumentException>().WithParameterName("values");
            Action badLag = () => dependence.AutocorrelationFunction(oneToFive, 0);
            badLag.Should().Throw<ArgumentException>().WithParameterName("maxLag");
            Action badLevel = () => dependence.AutocorrelationFunction(oneToFive, 2, 1.0);
            badLevel.Should().Throw<ArgumentException>().WithParameterName("confidenceLevel");
        }

        [Fact]
        [Trait("Covers", "PartialAutocorrelationFunction")]
        public void PartialAutocorrelationOfOneToFiveMatchesDurbinLevinsonByHand()
        {
            var result = dependence.PartialAutocorrelationFunction(oneToFive, 2);

            result.Lags.Should().Equal(1, 2);
            result.Values[0].Should().BeApproximately(0.4, 1e-9);
            result.Values[1].Should().BeApproximately((-0.1 - 0.16) / (1 - 0.16), 1e-9);
        }

        [Fact]
        [Trait("Covers", "PartialAutocorrelationFunction")]
        public void PartialAutocorrelationOfAutoregressiveSeriesCutsOffAfterLagOne()
        {
            var random = new Random(4);
            var x = new double[400];

            for (var i = 1; i < x.Length; i++)
            {
                x[i] = 0.7 * x[i - 1] + (random.NextDouble() - 0.5);
            }

            var result = dependence.PartialAutocorrelationFunction(x, 5);

            result.Values[0].Should().BeApproximately(0.7, 0.1);
            Math.Abs(result.Values[2]).Should().BeLessThan(0.15);
            result.SignificantLags.Should().Contain(1);
        }

        [Fact]
        [Trait("Covers", "PartialAutocorrelationFunction")]
        public void PartialAutocorrelationConstantSeriesIsNaNAndShortSeriesRejected()
        {
            dependence.PartialAutocorrelationFunction([2.0, 2.0, 2.0, 2.0, 2.0], 2).Values
                .Should().OnlyContain(v => double.IsNaN(v));

            Action tooShort = () => dependence.PartialAutocorrelationFunction([1.0, 2.0, 3.0]);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "CrossCorrelationFunction")]
        public void CrossCorrelationOfSeriesWithItselfEqualsAutocorrelationByHand()
        {
            var result = dependence.CrossCorrelationFunction(oneToFive, oneToFive, 2);

            result.Lags.Should().Equal(-2, -1, 0, 1, 2);
            result.Correlations[2].Should().BeApproximately(1.0, 1e-12);
            result.Correlations[3].Should().BeApproximately(0.4, 1e-12);
            result.Correlations[1].Should().BeApproximately(0.4, 1e-12);
            result.Correlations[4].Should().BeApproximately(-0.1, 1e-12);
            result.BestLag.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "CrossCorrelationFunction")]
        public void CrossCorrelationFindsWhichSeriesLeads()
        {
            var x = new[] { 0.0, 1, 0, -1, 0, 2, 0, -2, 1, 0, -1, 3, 0, -3, 1, 0 };
            var y = new double[x.Length];

            for (var t = 3; t < x.Length; t++)
            {
                y[t] = x[t - 3];
            }

            var forward = dependence.CrossCorrelationFunction(x, y, 5);
            var reverse = dependence.CrossCorrelationFunction(y, x, 5);

            forward.BestLag.Should().Be(3);
            reverse.BestLag.Should().Be(-3);
            forward.BestCorrelation.Should().BeGreaterThan(0.8);
        }

        [Fact]
        [Trait("Covers", "CrossCorrelationFunction")]
        public void CrossCorrelationRejectsMismatchedLengthsAndHandlesConstantSeries()
        {
            Action mismatch = () => dependence.CrossCorrelationFunction(oneToFive, [1.0, 2.0, 3.0]);
            mismatch.Should().Throw<ArgumentException>().WithParameterName("y");

            var constant = dependence.CrossCorrelationFunction(oneToFive, [4.0, 4.0, 4.0, 4.0, 4.0], 1);
            constant.Correlations.Should().OnlyContain(v => double.IsNaN(v));
            constant.BestLag.Should().Be(0);
            constant.BestCorrelation.Should().Be(double.NaN);
        }

        [Fact]
        [Trait("Covers", "LjungBoxAutocorrelationTest")]
        public void LjungBoxMatchesClosedFormChiSquareTwoDegreesOfFreedom()
        {
            var result = dependence.LjungBoxAutocorrelationTest(oneToFive, 2);

            var expected = 5.0 * 7.0 * (0.16 / 4.0 + 0.01 / 3.0);
            result.Statistic.Should().BeApproximately(expected, 1e-9);
            result.DegreesOfFreedom.Should().Be(2);
            result.PValue.Should().BeApproximately(Math.Exp(-expected / 2), 1e-9);
            result.RejectsNoAutocorrelation.Should().BeFalse();
            result.StatisticByLag[1].Should().BeApproximately(expected, 1e-9);
            result.Test.Should().Be("Ljung-Box");
        }

        [Fact]
        [Trait("Covers", "LjungBoxAutocorrelationTest")]
        public void LjungBoxRejectsStrongAutocorrelationAndModelDegreesOfFreedomAreSubtracted()
        {
            var trend = Enumerable.Range(0, 60).Select(i => (double)i).ToArray();

            var result = dependence.LjungBoxAutocorrelationTest(trend, 5, 2);

            result.DegreesOfFreedom.Should().Be(3);
            result.RejectsNoAutocorrelation.Should().BeTrue();
            result.PValueByLag[0].Should().Be(double.NaN);
            Action tooMany = () => dependence.LjungBoxAutocorrelationTest(oneToFive, 5);
            tooMany.Should().Throw<ArgumentException>().WithParameterName("lags");
        }

        [Fact]
        [Trait("Covers", "BoxPierceAutocorrelationTest")]
        public void BoxPierceMatchesClosedFormChiSquareTwoDegreesOfFreedom()
        {
            var result = dependence.BoxPierceAutocorrelationTest(oneToFive, 2);

            result.Statistic.Should().BeApproximately(5 * (0.16 + 0.01), 1e-9);
            result.PValue.Should().BeApproximately(Math.Exp(-0.425), 1e-9);
            result.Test.Should().Be("Box-Pierce");
            Action badAlpha = () => dependence.BoxPierceAutocorrelationTest(oneToFive, 2, 0, 0);
            badAlpha.Should().Throw<ArgumentException>().WithParameterName("alpha");
        }

        [Fact]
        [Trait("Covers", "DurbinWatsonResidualAutocorrelation")]
        public void DurbinWatsonOfAlternatingResidualsIsThreeByHand()
        {
            var result = dependence.DurbinWatsonResidualAutocorrelation([1.0, -1, 1, -1]);

            result.Statistic.Should().BeApproximately(3.0, 1e-12);
            result.ImpliedFirstOrderAutocorrelation.Should().BeApproximately(-0.5, 1e-12);
            result.Reading.Should().Contain("Negative");
        }

        [Fact]
        [Trait("Covers", "DurbinWatsonResidualAutocorrelation")]
        public void DurbinWatsonPositiveCorrelationAndAllZeroResiduals()
        {
            dependence.DurbinWatsonResidualAutocorrelation([1.0, 1.1, 1.2, 1.3]).Reading
                .Should().Contain("Positive");

            dependence.DurbinWatsonResidualAutocorrelation([0.0, 0, 0]).Statistic.Should().Be(double.NaN);
        }

        [Fact]
        [Trait("Covers", "TurningPointRandomnessTest")]
        public void TurningPointTestMatchesHandComputedZScore()
        {
            var result = dependence.TurningPointRandomnessTest([1.0, 3, 2, 4, 3, 5]);

            result.Statistic.Should().Be(4);
            result.ExpectedUnderRandomness.Should().BeApproximately(8.0 / 3.0, 1e-12);
            result.StandardError.Should().BeApproximately(Math.Sqrt(67.0 / 90.0), 1e-12);
            result.ZScore.Should().BeApproximately((4 - 8.0 / 3.0) / Math.Sqrt(67.0 / 90.0), 1e-12);
            result.PValue.Should().BeApproximately(0.1223, 5e-4);
        }

        [Fact]
        [Trait("Covers", "TurningPointRandomnessTest")]
        public void TurningPointTestOfMonotoneSeriesRejectsRandomness()
        {
            var monotone = Enumerable.Range(0, 30).Select(i => (double)i * i).ToArray();

            var result = dependence.TurningPointRandomnessTest(monotone);

            result.Statistic.Should().Be(0);
            result.RejectsRandomness.Should().BeTrue();
            Action tooShort = () => dependence.TurningPointRandomnessTest([1.0, 2.0, 3.0]);
            tooShort.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "MedianRunsRandomnessTest")]
        public void MedianRunsTestMatchesHandComputedAlternatingCase()
        {
            var result = dependence.MedianRunsRandomnessTest([1.0, 2, 1, 2, 1, 2, 1, 2]);

            result.Statistic.Should().Be(8);
            result.ExpectedUnderRandomness.Should().BeApproximately(5.0, 1e-12);
            result.StandardError.Should().BeApproximately(Math.Sqrt(768.0 / 448.0), 1e-12);
            result.ZScore.Should().BeApproximately(3.0 / Math.Sqrt(768.0 / 448.0), 1e-12);
            result.PValue.Should().BeApproximately(0.02195, 5e-4);
        }

        [Fact]
        [Trait("Covers", "MedianRunsRandomnessTest")]
        public void MedianRunsTestOfTrendHasTwoRunsAndConstantSeriesIsRejected()
        {
            var trend = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();

            var result = dependence.MedianRunsRandomnessTest(trend);

            result.Statistic.Should().Be(2);
            result.RejectsRandomness.Should().BeTrue();
            Action constant = () => dependence.MedianRunsRandomnessTest([2.0, 2.0, 2.0, 2.0]);
            constant.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "MannKendallTrendTest")]
        public void MannKendallOfPerfectLinearSeriesMatchesHandComputedStatistics()
        {
            var result = dependence.MannKendallTrendTest(oneToFive);

            result.S.Should().Be(10);
            result.Variance.Should().BeApproximately(5.0 * 4 * 15 / 18.0, 1e-12);
            result.ZScore.Should().BeApproximately(9.0 / Math.Sqrt(50.0 / 3.0), 1e-12);
            result.PValue.Should().BeApproximately(0.02745, 5e-4);
            result.KendallTau.Should().BeApproximately(1.0, 1e-12);
            result.SenSlopePerStep.Should().BeApproximately(1.0, 1e-12);
            result.SenIntercept.Should().BeApproximately(1.0, 1e-12);
            result.Trend.Should().Be("Increasing");
        }

        [Fact]
        [Trait("Covers", "MannKendallTrendTest")]
        public void MannKendallDecreasingTiesAndLimits()
        {
            dependence.MannKendallTrendTest([9.0, 7, 5, 3, 1, 0, -2]).Trend.Should().Be("Decreasing");
            var flat = dependence.MannKendallTrendTest([2.0, 2.0, 2.0, 2.0]);
            flat.S.Should().Be(0);
            flat.Trend.Should().Be("NoTrend");
            Action tooLong = () => dependence.MannKendallTrendTest(new double[4001]);
            tooLong.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "ClassicalSeasonalDecomposition")]
        public void AdditiveDecompositionRecoversExactSeasonalPatternAndTrend()
        {
            var pattern = new[] { -2.0, 1, -1, 2 };
            var series = Enumerable.Range(0, 16).Select(t => 10 + t + pattern[t % 4]).ToArray();

            var result = dependence.ClassicalSeasonalDecomposition(series, 4);

            result.SeasonalIndices.Should().HaveCount(4);

            for (var p = 0; p < 4; p++)
            {
                result.SeasonalIndices[p].Should().BeApproximately(pattern[p], 1e-9);
            }

            result.Trend[5].Should().BeApproximately(15.0, 1e-9);
            result.Trend[0].Should().Be(double.NaN);
            result.Trend[1].Should().Be(double.NaN);
            result.Remainder[5].Should().BeApproximately(0.0, 1e-9);
            result.SeasonalStrength.Should().BeApproximately(1.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "ClassicalSeasonalDecomposition")]
        public void MultiplicativeDecompositionRecoversIndicesAndValidatesInput()
        {
            var pattern = new[] { 0.8, 1.2, 1.0 };
            var series = Enumerable.Range(0, 12).Select(t => 100.0 * pattern[t % 3]).ToArray();

            var result = dependence.ClassicalSeasonalDecomposition(series, 3, SeasonalModel.Multiplicative);

            result.SeasonalIndices[0].Should().BeApproximately(0.8, 1e-9);
            result.SeasonalIndices[1].Should().BeApproximately(1.2, 1e-9);
            result.Model.Should().Be("Multiplicative");

            Action negative = () => dependence.ClassicalSeasonalDecomposition([1.0, -2, 3, 4, 1, 2, 3, 4], 4,
                SeasonalModel.Multiplicative);

            negative.Should().Throw<ArgumentException>().WithParameterName("model");
            Action shortSeries = () => dependence.ClassicalSeasonalDecomposition([1.0, 2, 3, 4, 5, 6], 4);
            shortSeries.Should().Throw<ArgumentException>().WithParameterName("period");
        }

        [Fact]
        [Trait("Covers", "SeasonalityStrengthScore")]
        public void SeasonalityStrengthIsHighForCycleAndLowForNoise()
        {
            var pattern = new[] { -3.0, 0, 3, 0 };
            var cycle = Enumerable.Range(0, 40).Select(t => pattern[t % 4]).ToArray();
            var random = new Random(9);
            var noise = Enumerable.Range(0, 40).Select(_ => random.NextDouble()).ToArray();

            dependence.SeasonalityStrengthScore(cycle, 4).SeasonalStrength.Should().BeApproximately(1.0, 1e-9);
            dependence.SeasonalityStrengthScore(cycle, 4).Reading.Should().Contain("Strong");
            dependence.SeasonalityStrengthScore(noise, 4).SeasonalStrength.Should().BeLessThan(0.3);
        }

        [Fact]
        [Trait("Covers", "EstimateSeasonalPeriodByAutocorrelation")]
        public void EstimatedPeriodOfSineWithTrendIsTheTruePeriod()
        {
            var series = Enumerable.Range(0, 96).Select(t => 0.5 * t + 10 * Math.Sin(2 * Math.PI * t / 12)).ToArray();

            var result = dependence.EstimateSeasonalPeriodByAutocorrelation(series);

            result.Found.Should().BeTrue();
            result.Period.Should().Be(12);
            result.AutocorrelationAtPeriod.Should().BeGreaterThan(0.7);
        }

        [Fact]
        [Trait("Covers", "EstimateSeasonalPeriodByAutocorrelation")]
        public void EstimatedPeriodNotFoundForConstantOrPureTrendAndRejectsShortInput()
        {
            dependence.EstimateSeasonalPeriodByAutocorrelation([.. Enumerable.Range(0, 40).Select(_ => 5.0)])
                .Found.Should().BeFalse();

            dependence.EstimateSeasonalPeriodByAutocorrelation([.. Enumerable.Range(0, 40).Select(i => 2.0 * i)])
                .Found.Should().BeFalse();

            Action tooShort = () => dependence.EstimateSeasonalPeriodByAutocorrelation([1.0, 2, 3]);
            tooShort.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "EngleArchLmTest")]
        public void ArchLmOfPerfectlyAlternatingSquaresIsRowsTimesOneWithClosedFormPValue()
        {
            var e = new[] { 1.0, 2, 1, 2, 1, 2, 1, 2 };

            var result = dependence.EngleArchLmTest(e, 1);

            result.Statistic.Should().BeApproximately(7.0, 1e-9);
            result.PValue.Should().BeApproximately(0.008150, 1e-5);
            result.Significant.Should().BeTrue();
            result.DegreesOfFreedomNumerator.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "EngleArchLmTest")]
        public void ArchLmOfConstantMagnitudeIsNaNAndTooFewRowsRejected()
        {
            dependence.EngleArchLmTest([1.0, -1, 1, -1, 1, -1, 1, -1], 1).Statistic.Should().Be(double.NaN);
            Action tooFew = () => dependence.EngleArchLmTest([1.0, 2, 3, 4, 5], 3);
            tooFew.Should().Throw<ArgumentException>().WithParameterName("lags");
        }

        [Fact]
        [Trait("Covers", "GrangerCausalityTest")]
        public void GrangerDetectsLaggedCauseAndMatchesIndependentRegression()
        {
            var random = new Random(21);
            var x = Enumerable.Range(0, 80).Select(_ => random.NextDouble() - 0.5).ToArray();
            var y = new double[80];

            for (var t = 1; t < 80; t++)
            {
                y[t] = 0.3 * y[t - 1] + 2.0 * x[t - 1] + 0.05 * (random.NextDouble() - 0.5);
            }

            var forward = dependence.GrangerCausalityTest(x, y, 1);
            var reverse = dependence.GrangerCausalityTest(y, x, 1);

            forward.Significant.Should().BeTrue();
            forward.PValue.Should().BeLessThan(1e-6);
            forward.DegreesOfFreedomNumerator.Should().Be(1);
            forward.DegreesOfFreedomDenominator.Should().Be(79 - 2 - 1);
            reverse.Significant.Should().BeFalse();

            var expected = ReferenceGrangerF(x, y);
            forward.Statistic.Should().BeApproximately(expected, expected * 1e-6);
        }

        [Fact]
        [Trait("Covers", "GrangerCausalityTest")]
        public void GrangerPerfectFitGivesInfiniteFAndBadInputIsRejected()
        {
            var x = new[] { 1.0, 3, 2, 5, 4, 7, 6, 9, 8, 11 };
            var y = new double[x.Length];

            for (var t = 1; t < y.Length; t++)
            {
                y[t] = 2 * x[t - 1] + 1;
            }

            var perfect = dependence.GrangerCausalityTest(x, y, 1);
            perfect.PValue.Should().BeLessThan(1e-9);

            Action mismatch = () => dependence.GrangerCausalityTest(x, [1.0, 2, 3, 4, 5]);
            mismatch.Should().Throw<ArgumentException>().WithParameterName("effect");

            Action tooFew = () =>
                dependence.GrangerCausalityTest([1.0, 2, 3, 4, 5], [2.0, 1, 4, 3, 6]);

            tooFew.Should().Throw<ArgumentException>().WithParameterName("lags");
        }

        private static double ReferenceGrangerF(double[] x, double[] y)
        {
            var rows = y.Length - 1;
            var target = Enumerable.Range(1, rows).Select(t => y[t]).ToArray();
            var lagY = Enumerable.Range(0, rows).Select(t => y[t]).ToArray();
            var lagX = Enumerable.Range(0, rows).Select(t => x[t]).ToArray();

            var rssRestricted = SimpleRegressionRss(lagY, target);
            var rssFull = TwoRegressorRss(lagY, lagX, target);

            return (rssRestricted - rssFull) / (rssFull / (rows - 3));
        }

        private static double SimpleRegressionRss(double[] a, double[] target)
        {
            var n = a.Length;
            var ma = a.Average();
            var mt = target.Average();
            var slope = a.Zip(target, (p, q) => (p - ma) * (q - mt)).Sum() / a.Sum(p => (p - ma) * (p - ma));
            var intercept = mt - slope * ma;

            return Enumerable.Range(0, n).Sum(i => Math.Pow(target[i] - intercept - slope * a[i], 2));
        }

        private static double TwoRegressorRss(double[] a, double[] b, double[] target)
        {
            var n = a.Length;
            var ma = a.Average();
            var mb = b.Average();
            var mt = target.Average();
            var saa = a.Sum(p => (p - ma) * (p - ma));
            var sbb = b.Sum(p => (p - mb) * (p - mb));
            var sab = a.Zip(b, (p, q) => (p - ma) * (q - mb)).Sum();
            var sat = a.Zip(target, (p, q) => (p - ma) * (q - mt)).Sum();
            var sbt = b.Zip(target, (p, q) => (p - mb) * (q - mt)).Sum();
            var det = saa * sbb - sab * sab;
            var ca = (sat * sbb - sbt * sab) / det;
            var cb = (sbt * saa - sat * sab) / det;
            var intercept = mt - ca * ma - cb * mb;

            return Enumerable.Range(0, n).Sum(i => Math.Pow(target[i] - intercept - ca * a[i] - cb * b[i], 2));
        }

        private const double Z95 = 1.959963984540054;
        private readonly SeriesForecastingTools forecast = new();

        [Fact]
        [Trait("Covers", "BaselineSeriesForecast")]
        public void NaiveAndDriftForecastsMatchHandComputation()
        {
            var naive = forecast.BaselineSeriesForecast(oneToFive, 4);

            naive.Forecast.Should().Equal(5, 5, 5, 5);
            naive.ResidualStandardDeviation.Should().BeApproximately(1.0, 1e-12);
            (naive.Upper[3] - naive.Forecast[3]).Should().BeApproximately(Z95 * 2, 1e-9);
            var drift = forecast.BaselineSeriesForecast(oneToFive, 2, BaselineForecastMethod.Drift);
            drift.Forecast.Should().Equal(6, 7);
            drift.ResidualStandardDeviation.Should().BeApproximately(0, 1e-12);
            forecast.BaselineSeriesForecast(oneToFive, 1, BaselineForecastMethod.Mean).Forecast.Should().Equal(3);

            forecast.BaselineSeriesForecast(oneToFive, 1, BaselineForecastMethod.MovingAverage, 1, 2).Forecast.Should()
                .Equal(4.5);
        }

        [Fact]
        [Trait("Covers", "BaselineSeriesForecast")]
        public void SeasonalNaiveRepeatsLastCycleAndValidatesArguments()
        {
            var result = forecast.BaselineSeriesForecast([1.0, 2, 3, 1, 2, 3, 1, 2], 4,
                BaselineForecastMethod.SeasonalNaive, 3);

            result.Forecast.Should().Equal(3, 1, 2, 3);
            result.ResidualStandardDeviation.Should().BeApproximately(0, 1e-12);

            Action noPeriod = () =>
                forecast.BaselineSeriesForecast(oneToFive, 2, BaselineForecastMethod.SeasonalNaive);

            noPeriod.Should().Throw<ArgumentException>().WithParameterName("seasonalPeriod");
            Action horizon = () => forecast.BaselineSeriesForecast(oneToFive, 0);
            horizon.Should().Throw<ArgumentException>().WithParameterName("horizon");

            Action window = () =>
                forecast.BaselineSeriesForecast(oneToFive, 1, BaselineForecastMethod.MovingAverage, 1, 9);

            window.Should().Throw<ArgumentException>().WithParameterName("window");
            Action nan = () => forecast.BaselineSeriesForecast([1.0, double.NaN, 3], 1);
            nan.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "SimpleExponentialSmoothingForecast")]
        public void SimpleSmoothingWithFixedAlphaMatchesHandRecursion()
        {
            var result = forecast.SimpleExponentialSmoothingForecast([1.0, 2, 3], 2, 0.5);

            result.Forecast.Should().Equal(2.25, 2.25);
            result.ResidualStandardDeviation.Should().BeApproximately(Math.Sqrt(3.25 / 2), 1e-12);

            (result.Upper[1] - result.Forecast[1]).Should()
                .BeApproximately(Z95 * result.ResidualStandardDeviation * Math.Sqrt(1.25), 1e-9);

            result.ParameterValues[0].Should().Be(0.5);
        }

        [Fact]
        [Trait("Covers", "SimpleExponentialSmoothingForecast")]
        public void SimpleSmoothingEstimatesAlphaNearOneForRampAndRejectsBadAlpha()
        {
            var result =
                forecast.SimpleExponentialSmoothingForecast([.. Enumerable.Range(1, 10).Select(i => (double)i)],
                    1);

            result.ParameterValues[0].Should().BeGreaterThan(0.99);
            result.Forecast[0].Should().BeApproximately(10, 0.05);
            Action bad = () => forecast.SimpleExponentialSmoothingForecast(oneToFive, 1, 1.5);
            bad.Should().Throw<ArgumentException>().WithParameterName("alpha");
        }

        [Fact]
        [Trait("Covers", "HoltLinearTrendForecast")]
        public void HoltReproducesLinearSeriesExactly()
        {
            var ramp = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();

            var estimated = forecast.HoltLinearTrendForecast(ramp, 3);
            var fixedParameters = forecast.HoltLinearTrendForecast(ramp, 3, 0.5, 0.5);

            foreach (var r in new[] { estimated, fixedParameters })
            {
                r.Forecast[0].Should().BeApproximately(11, 1e-6);
                r.Forecast[1].Should().BeApproximately(12, 1e-6);
                r.Forecast[2].Should().BeApproximately(13, 1e-6);
                r.ResidualStandardDeviation.Should().BeApproximately(0, 1e-6);
            }
        }

        [Fact]
        [Trait("Covers", "HoltLinearTrendForecast")]
        public void HoltDampingFlattensTheTrendAndValidates()
        {
            var ramp = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();

            var damped = forecast.HoltLinearTrendForecast(ramp, 20, 0.5, 0.5, 0.5);

            damped.Forecast[19].Should().BeLessThan(20.5);
            damped.Method.Should().Be("DampedHoltLinearTrend");
            Action badPhi = () => forecast.HoltLinearTrendForecast(ramp, 1, 0.5, 0.5, 0);
            badPhi.Should().Throw<ArgumentException>().WithParameterName("phi");
            Action badBeta = () => forecast.HoltLinearTrendForecast(ramp, 1, 0.5, 2);
            badBeta.Should().Throw<ArgumentException>().WithParameterName("beta");
        }

        [Fact]
        [Trait("Covers", "HoltWintersSeasonalForecast")]
        public void HoltWintersReproducesExactlyPeriodicSeriesAdditiveAndMultiplicative()
        {
            var y = Enumerable.Range(0, 12).Select(t => new[] { 1.0, 3, 2 }[t % 3]).ToArray();

            var additive = forecast.HoltWintersSeasonalForecast(y, 4, 3);
            var multiplicative = forecast.HoltWintersSeasonalForecast(y, 4, 3, SeasonalModel.Multiplicative);

            additive.Forecast.Should().BeEquivalentTo([1.0, 3, 2, 1],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-6))
                    .WhenTypeIs<double>());
            multiplicative.Forecast.Should().BeEquivalentTo([1.0, 3, 2, 1],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-6))
                    .WhenTypeIs<double>());

            additive.Lower[0].Should().BeLessThanOrEqualTo(additive.Forecast[0]);
        }

        [Fact]
        [Trait("Covers", "HoltWintersSeasonalForecast")]
        public void HoltWintersCapturesTrendPlusSeasonAndValidates()
        {
            var pattern = new[] { 2.0, -1, -2, 1 };
            var y = Enumerable.Range(0, 24).Select(t => 10 + 0.5 * t + pattern[t % 4]).ToArray();

            var result = forecast.HoltWintersSeasonalForecast(y, 4, 4, SeasonalModel.Additive, 0.3, 0.2, 0.3);

            result.Forecast[0].Should().BeApproximately(10 + 0.5 * 24 + 2, 0.3);
            Action tooShort = () => forecast.HoltWintersSeasonalForecast([1.0, 2, 3, 4, 5, 6], 1, 4);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("seasonalPeriod");

            Action negative = () =>
                forecast.HoltWintersSeasonalForecast([1.0, -2, 3, 4, 1, 2, 3, 4], 1, 4,
                    SeasonalModel.Multiplicative);

            negative.Should().Throw<ArgumentException>().WithParameterName("model");
            Action badGamma = () => forecast.HoltWintersSeasonalForecast(y, 1, 4, SeasonalModel.Additive, 0.3, 0.2, 2);
            badGamma.Should().Throw<ArgumentException>().WithParameterName("gamma");
        }

        [Fact]
        [Trait("Covers", "LinearTrendForecast")]
        public void LinearTrendMatchesHandComputedRegressionAndTInterval()
        {
            var result = forecast.LinearTrendForecast([1.0, 3, 2, 5, 4], 1);

            result.ParameterValues[0].Should().BeApproximately(1.4, 1e-12);
            result.ParameterValues[1].Should().BeApproximately(0.8, 1e-12);
            result.Forecast[0].Should().BeApproximately(5.4, 1e-12);
            result.ResidualStandardDeviation.Should().BeApproximately(Math.Sqrt(1.2), 1e-12);

            (result.Upper[0] - result.Forecast[0]).Should()
                .BeApproximately(3.182446305 * Math.Sqrt(1.2) * Math.Sqrt(2.1), 1e-6);
        }

        [Fact]
        [Trait("Covers", "LinearTrendForecast")]
        public void LinearTrendOfExactLineHasZeroWidthAndNeedsThreePoints()
        {
            var result = forecast.LinearTrendForecast([.. Enumerable.Range(0, 6).Select(t => 2 + 3.0 * t)], 2);

            result.Forecast[0].Should().BeApproximately(20, 1e-9);
            result.Forecast[1].Should().BeApproximately(23, 1e-9);
            (result.Upper[1] - result.Lower[1]).Should().BeApproximately(0, 1e-6);
            Action tooShort = () => forecast.LinearTrendForecast([1.0, 2], 1);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "AutoregressiveModelFit")]
        public void YuleWalkerAndLeastSquaresFitsMatchHandComputationForOneToFive()
        {
            var yw = forecast.AutoregressiveModelFit(oneToFive, 1);
            var ls = forecast.AutoregressiveModelFit(oneToFive, 1, AutoregressiveEstimation.LeastSquares);

            yw.Coefficients[0].Should().BeApproximately(0.4, 1e-9);
            yw.InnovationVariance.Should().BeApproximately(1.68, 1e-9);
            yw.Mean.Should().BeApproximately(3, 1e-12);
            ls.Coefficients[0].Should().BeApproximately(1, 1e-9);
            ls.Intercept.Should().BeApproximately(1, 1e-9);
            ls.InnovationVariance.Should().BeApproximately(0, 1e-9);
            ls.Mean.Should().Be(double.NaN);
        }

        [Fact]
        [Trait("Covers", "AutoregressiveModelFit")]
        public void AutoregressiveFitRecoversSimulatedCoefficientAndValidatesOrder()
        {
            var random = new Random(3);
            var x = new double[1000];

            for (var t = 1; t < x.Length; t++)
            {
                x[t] = 0.7 * x[t - 1] + (random.NextDouble() - 0.5);
            }

            forecast.AutoregressiveModelFit(x, 1).Coefficients[0].Should().BeApproximately(0.7, 0.06);

            forecast.AutoregressiveModelFit(x, 2, AutoregressiveEstimation.LeastSquares).Coefficients[0].Should()
                .BeApproximately(0.7, 0.08);

            Action bad = () => forecast.AutoregressiveModelFit(x, 0);
            bad.Should().Throw<ArgumentException>().WithParameterName("order");
            Action tooShort = () => forecast.AutoregressiveModelFit([1.0, 2, 3], 3);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("order");
        }

        [Fact]
        [Trait("Covers", "AutoregressiveOrderSelection")]
        public void OrderSelectionFindsOneForArOneAndZeroForNoise()
        {
            var random = new Random(8);
            var ar = new double[600];

            for (var t = 1; t < ar.Length; t++)
            {
                ar[t] = 0.7 * ar[t - 1] + (random.NextDouble() - 0.5);
            }

            var noise = Enumerable.Range(0, 600).Select(_ => random.NextDouble()).ToArray();

            var result = forecast.AutoregressiveOrderSelection(ar, 5);

            result.Orders.Should().Equal(0, 1, 2, 3, 4, 5);
            result.BestOrderByBic.Should().Be(1);
            forecast.AutoregressiveOrderSelection(noise, 5).BestOrderByBic.Should().Be(0);
            Action tooLarge = () => forecast.AutoregressiveOrderSelection(new double[10], 5);
            tooLarge.Should().Throw<ArgumentException>().WithParameterName("maxOrder");
        }

        [Fact]
        [Trait("Covers", "AutoregressiveForecast")]
        public void YuleWalkerForecastRevertsTowardMeanByHand()
        {
            var result = forecast.AutoregressiveForecast(oneToFive, 2, 1);

            result.Forecast[0].Should().BeApproximately(3.8, 1e-9);
            result.Forecast[1].Should().BeApproximately(3.32, 1e-9);
            (result.Upper[0] - result.Forecast[0]).Should().BeApproximately(Z95 * Math.Sqrt(1.68), 1e-9);
            (result.Upper[1] - result.Forecast[1]).Should().BeApproximately(Z95 * Math.Sqrt(1.68 * (1 + 0.16)), 1e-9);
        }

        [Fact]
        [Trait("Covers", "AutoregressiveForecast")]
        public void LeastSquaresForecastContinuesExactRampAndValidates()
        {
            var result = forecast.AutoregressiveForecast(oneToFive, 3, 1, AutoregressiveEstimation.LeastSquares);

            result.Forecast.Should().BeEquivalentTo([6.0, 7, 8],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-6))
                    .WhenTypeIs<double>());

            Action bad = () => forecast.AutoregressiveForecast(oneToFive, 0, 1);
            bad.Should().Throw<ArgumentException>().WithParameterName("horizon");
            Action order = () => forecast.AutoregressiveForecast(oneToFive, 1, 0);
            order.Should().Throw<ArgumentException>().WithParameterName("order");
        }

        [Fact]
        [Trait("Covers", "ForecastAccuracyMetrics")]
        public void AccuracyMetricsMatchPublishedRegressionExample()
        {
            var result = forecast.ForecastAccuracyMetrics([3.0, -0.5, 2, 7], [2.5, 0.0, 2, 8],
                [1.0, 2, 3, 4]);

            result.MeanAbsoluteError.Should().BeApproximately(0.5, 1e-12);
            result.MeanSquaredError.Should().BeApproximately(0.375, 1e-12);
            result.RootMeanSquaredError.Should().BeApproximately(Math.Sqrt(0.375), 1e-12);
            result.MeanError.Should().BeApproximately(-0.25, 1e-12);
            result.MedianAbsoluteError.Should().BeApproximately(0.5, 1e-12);
            result.MeanAbsolutePercentageError.Should().BeApproximately(0.327381, 1e-6);
            result.SymmetricMeanAbsolutePercentageError.Should().BeApproximately(0.578788, 1e-6);
            result.MeanAbsoluteScaledError.Should().BeApproximately(0.5, 1e-12);
        }

        [Fact]
        [Trait("Covers", "ForecastAccuracyMetrics")]
        public void AccuracyMetricsZeroActualsPerfectForecastAndValidation()
        {
            var zero = forecast.ForecastAccuracyMetrics([0.0, 2], [1.0, 2]);
            zero.PercentageErrorsSkipped.Should().Be(1);
            zero.MeanAbsolutePercentageError.Should().Be(0);
            zero.MeanAbsoluteScaledError.Should().Be(double.NaN);

            var perfect = forecast.ForecastAccuracyMetrics([0.0, 0], [0.0, 0]);
            perfect.SymmetricMeanAbsolutePercentageError.Should().Be(0);
            perfect.MeanAbsolutePercentageError.Should().Be(double.NaN);

            var seasonal =
                forecast.ForecastAccuracyMetrics([1.0], [2.0], [1.0, 5, 1, 5, 1], 2);

            seasonal.MeanAbsoluteScaledError.Should().Be(double.NaN);
            Action mismatch = () => forecast.ForecastAccuracyMetrics([1.0, 2], [1.0]);
            mismatch.Should().Throw<ArgumentException>().WithParameterName("forecast");
        }

        [Fact]
        [Trait("Covers", "HoldOutForecastComparison")]
        public void HoldOutComparisonRanksTrendMethodsAboveNaiveOnRamp()
        {
            var ramp = Enumerable.Range(1, 24).Select(i => (double)i).ToArray();

            var result = forecast.HoldOutForecastComparison(ramp, 4);

            result.Scores[0].RootMeanSquaredError.Should().BeLessThan(1e-4);
            var naive = result.Scores.Single(s => s.Method == "Naive");
            naive.RootMeanSquaredError.Should().BeApproximately(Math.Sqrt(7.5), 1e-9);
            result.BestMethodByRmse.Should().Be(result.Scores[0].Method);
            result.TrainingLength.Should().Be(20);
            result.Scores.Select(s => s.RootMeanSquaredError).Should().BeInAscendingOrder();
        }

        [Fact]
        [Trait("Covers", "HoldOutForecastComparison")]
        public void HoldOutComparisonUsesSeasonalMethodsForPeriodicSeriesAndValidates()
        {
            var pattern = new[] { 5.0, 1, 9, 3 };
            var y = Enumerable.Range(0, 32).Select(t => pattern[t % 4]).ToArray();

            var result = forecast.HoldOutForecastComparison(y, 4, 4);

            result.Scores.Select(s => s.Method).Should().Contain(["SeasonalNaive", "HoltWintersAdditive"]);

            result.Scores.Single(s => s.Method == "SeasonalNaive").RootMeanSquaredError.Should()
                .BeApproximately(0, 1e-9);

            Action big = () => forecast.HoldOutForecastComparison(y, 20);
            big.Should().Throw<ArgumentException>().WithParameterName("holdOut");
            Action shortSeries = () => forecast.HoldOutForecastComparison([1.0, 2, 3], 1);
            shortSeries.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        private static readonly double[][] hmmTransitions = [[0.7, 0.3], [0.4, 0.6]];
        private static readonly double[][] hmmEmissions = [[0.9, 0.1], [0.2, 0.8]];
        private static readonly double[] hmmInitial = [0.5, 0.5];
        private static readonly int[] hmmObservations = [0, 0, 1, 1, 0];
        private static readonly double[] gaussianMeans = [0.0, 10.0];
        private static readonly double[] gaussianDeviations = [1.0, 1.0];
        private static readonly double[][] gaussianTransitions = [[0.9, 0.1], [0.1, 0.9]];
        private static readonly double[] gaussianObservations = [0.1, -0.3, 9.8, 10.2, 0.2];
        private readonly HiddenMarkovTools hmm = new();

        private static (double LogLikelihood, int[] BestPath, double BestLog, double[][] Posterior) BruteForce(
            int length, double[][] transitions, double[] initial, Func<int, int, double> emission)
        {
            var states = transitions.Length;
            var total = 0.0;
            var best = double.NegativeInfinity;
            var bestPath = new int[length];
            var posterior = Enumerable.Range(0, length).Select(_ => new double[states]).ToArray();
            var pathCount = (int)Math.Pow(states, length);

            for (var code = 0; code < pathCount; code++)
            {
                var path = new int[length];
                var c = code;

                for (var t = 0; t < length; t++)
                {
                    path[t] = c % states;
                    c /= states;
                }

                var p = initial[path[0]] * emission(0, path[0]);

                for (var t = 1; t < length; t++)
                {
                    p *= transitions[path[t - 1]][path[t]] * emission(t, path[t]);
                }

                total += p;

                if (Math.Log(p) > best)
                {
                    best = Math.Log(p);
                    bestPath = path;
                }

                for (var t = 0; t < length; t++)
                {
                    posterior[t][path[t]] += p;
                }
            }

            foreach (var row in posterior)
            {
                var sum = row.Sum();

                for (var i = 0; i < row.Length; i++)
                {
                    row[i] /= sum;
                }
            }

            return (Math.Log(total), bestPath, best, posterior);
        }

        private static double NormalDensity(double x, double mean, double sd)
        {
            return Math.Exp(-(x - mean) * (x - mean) / (2 * sd * sd)) / (sd * Math.Sqrt(2 * Math.PI));
        }

        private static int[][] SimulateDiscrete(int length, int seed)
        {
            var random = new Random(seed);
            var symbols = new int[length];
            var state = 0;

            for (var t = 0; t < length; t++)
            {
                if (t > 0 && random.NextDouble() > 0.95)
                {
                    state = 1 - state;
                }

                var matches = random.NextDouble() < 0.95;
                symbols[t] = matches ? state : 1 - state;
            }

            return [symbols];
        }

        private static double[] SimulateGaussian(int length, int seed)
        {
            var random = new Random(seed);
            var values = new double[length];
            var state = 0;

            for (var t = 0; t < length; t++)
            {
                if (t > 0 && random.NextDouble() > 0.95)
                {
                    state = 1 - state;
                }

                var u1 = 1 - random.NextDouble();
                var u2 = random.NextDouble();
                var z = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
                values[t] = (state == 0 ? 0.0 : 10.0) + z;
            }

            return values;
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovDiscreteDecode")]
        public void DiscreteViterbiMatchesBruteForceEnumerationOfAllPaths()
        {
            var expected = BruteForce(5, hmmTransitions, hmmInitial, (t, s) => hmmEmissions[s][hmmObservations[t]]);

            var result = hmm.HiddenMarkovDiscreteDecode(hmmObservations, hmmTransitions, hmmEmissions, hmmInitial);

            result.StatePath.Should().Equal(expected.BestPath);
            result.LogLikelihoodOfPath.Should().BeApproximately(expected.BestLog, 1e-9);
            result.Observations.Should().Be(5);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovDiscreteDecode")]
        public void DiscreteDecodeValidatesModelAndObservations()
        {
            Action badSymbol = () =>
                hmm.HiddenMarkovDiscreteDecode([0, 2], hmmTransitions, hmmEmissions, hmmInitial);

            badSymbol.Should().Throw<ArgumentException>().WithParameterName("observations");

            Action badRow = () => hmm.HiddenMarkovDiscreteDecode(hmmObservations,
                [[0.5, 0.4], [0.4, 0.6]], hmmEmissions, hmmInitial);

            badRow.Should().Throw<ArgumentException>().WithParameterName("transitions");

            Action badEmission = () =>
                hmm.HiddenMarkovDiscreteDecode(hmmObservations, hmmTransitions, [[0.9, 0.1]],
                    hmmInitial);

            badEmission.Should().Throw<ArgumentException>().WithParameterName("emissions");

            Action badInitial = () =>
                hmm.HiddenMarkovDiscreteDecode(hmmObservations, hmmTransitions, hmmEmissions, [0.9, 0.9]);

            badInitial.Should().Throw<ArgumentException>().WithParameterName("initial");

            Action empty = () =>
                hmm.HiddenMarkovDiscreteDecode([], hmmTransitions, hmmEmissions, hmmInitial);

            empty.Should().Throw<ArgumentException>().WithParameterName("observations");
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovDiscreteLogLikelihood")]
        public void DiscreteLogLikelihoodMatchesBruteForceAndStepProbabilitiesSumToTotal()
        {
            var expected = BruteForce(5, hmmTransitions, hmmInitial, (t, s) => hmmEmissions[s][hmmObservations[t]]);

            var result =
                hmm.HiddenMarkovDiscreteLogLikelihood(hmmObservations, hmmTransitions, hmmEmissions, hmmInitial);

            result.LogLikelihood.Should().BeApproximately(expected.LogLikelihood, 1e-9);
            result.StepLogProbabilities.Sum().Should().BeApproximately(expected.LogLikelihood, 1e-9);
            result.AverageLogLikelihoodPerObservation.Should().BeApproximately(expected.LogLikelihood / 5, 1e-9);
            result.StepLogProbabilities[0].Should().BeApproximately(Math.Log(0.5 * 0.9 + 0.5 * 0.2), 1e-9);
            result.StepLogProbabilities[result.MostSurprisingIndex].Should().Be(result.StepLogProbabilities.Min());
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovDiscreteLogLikelihood")]
        public void DiscreteLogLikelihoodImpossibleSymbolIsMinusInfinity()
        {
            var deterministic = new[] { new[] { 1.0, 0 }, new[] { 0.0, 1 } };
            var emissions = new[] { new[] { 1.0, 0 }, new[] { 0.0, 1 } };

            var result =
                hmm.HiddenMarkovDiscreteLogLikelihood([0, 1], deterministic, emissions, [1.0, 0]);

            double.IsNegativeInfinity(result.LogLikelihood).Should().BeTrue();
            var single = hmm.HiddenMarkovDiscreteLogLikelihood([0], hmmTransitions, hmmEmissions, hmmInitial);
            single.LogLikelihood.Should().BeApproximately(Math.Log(0.55), 1e-12);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovDiscretePosterior")]
        public void DiscretePosteriorMatchesBruteForceMarginals()
        {
            var expected = BruteForce(5, hmmTransitions, hmmInitial, (t, s) => hmmEmissions[s][hmmObservations[t]]);

            var result = hmm.HiddenMarkovDiscretePosterior(hmmObservations, hmmTransitions, hmmEmissions, hmmInitial);

            for (var t = 0; t < 5; t++)
            {
                result.StateProbabilities[t].Sum().Should().BeApproximately(1, 1e-9);
                result.StateProbabilities[t][0].Should().BeApproximately(expected.Posterior[t][0], 1e-9);
                result.StateProbabilities[t][1].Should().BeApproximately(expected.Posterior[t][1], 1e-9);
            }

            result.LogLikelihood.Should().BeApproximately(expected.LogLikelihood, 1e-9);
            result.MostLikelyStates.Should().HaveCount(5);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovDiscretePosterior")]
        public void DiscretePosteriorOfSingleObservationIsBayesRule()
        {
            var result = hmm.HiddenMarkovDiscretePosterior([0], hmmTransitions, hmmEmissions, hmmInitial);

            result.StateProbabilities[0][0].Should().BeApproximately(0.45 / 0.55, 1e-12);
            result.MostLikelyStates.Should().Equal(0);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovDiscreteNextSymbolDistribution")]
        public void DiscreteNextSymbolFollowsFilteredBeliefPushedThroughModel()
        {
            var expected = BruteForce(5, hmmTransitions, hmmInitial, (t, s) => hmmEmissions[s][hmmObservations[t]]);
            var filtered = expected.Posterior[4];

            var next = new[]
            {
                filtered[0] * 0.7 + filtered[1] * 0.4,
                filtered[0] * 0.3 + filtered[1] * 0.6
            };

            var symbol0 = next[0] * 0.9 + next[1] * 0.2;

            var result =
                hmm.HiddenMarkovDiscreteNextSymbolDistribution(hmmObservations, hmmTransitions, hmmEmissions,
                    hmmInitial);

            result.FilteredStateProbabilities[0].Should().BeApproximately(filtered[0], 1e-9);
            result.NextStateProbabilities[0].Should().BeApproximately(next[0], 1e-9);
            result.NextSymbolProbabilities[0].Should().BeApproximately(symbol0, 1e-9);
            result.NextSymbolProbabilities.Sum().Should().BeApproximately(1, 1e-9);
            result.MostLikelyNextSymbol.Should().Be(symbol0 >= 0.5 ? 0 : 1);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovDiscreteNextSymbolDistribution")]
        public void DiscreteNextSymbolRejectsBadInput()
        {
            Action empty = () =>
                hmm.HiddenMarkovDiscreteNextSymbolDistribution([], hmmTransitions, hmmEmissions,
                    hmmInitial);

            empty.Should().Throw<ArgumentException>().WithParameterName("observations");

            Action badShape = () =>
                hmm.HiddenMarkovDiscreteNextSymbolDistribution([0], [[1.0]], hmmEmissions,
                    hmmInitial);
            badShape.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianDecode")]
        public void GaussianViterbiMatchesBruteForceEnumeration()
        {
            var expected = BruteForce(5, gaussianTransitions, hmmInitial,
                (t, s) => NormalDensity(gaussianObservations[t], gaussianMeans[s], gaussianDeviations[s]));

            var result = hmm.HiddenMarkovGaussianDecode(gaussianObservations, gaussianTransitions, gaussianMeans,
                gaussianDeviations, hmmInitial);

            result.StatePath.Should().Equal(0, 0, 1, 1, 0);
            result.StatePath.Should().Equal(expected.BestPath);
            result.LogLikelihoodOfPath.Should().BeApproximately(expected.BestLog, 1e-9);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianDecode")]
        public void GaussianDecodeValidatesDeviationsAndShapes()
        {
            Action badSd = () => hmm.HiddenMarkovGaussianDecode(gaussianObservations, gaussianTransitions,
                gaussianMeans, [1.0, 0], hmmInitial);

            badSd.Should().Throw<ArgumentException>().WithParameterName("standardDeviations");

            Action badMeans = () => hmm.HiddenMarkovGaussianDecode(gaussianObservations, gaussianTransitions,
                [0.0], gaussianDeviations, hmmInitial);

            badMeans.Should().Throw<ArgumentException>().WithParameterName("means");

            Action nan = () => hmm.HiddenMarkovGaussianDecode([double.NaN], gaussianTransitions, gaussianMeans,
                gaussianDeviations, hmmInitial);

            nan.Should().Throw<ArgumentException>().WithParameterName("observations");

            Action tooManyStates = () => hmm.HiddenMarkovGaussianDecode(gaussianObservations,
                [.. Enumerable.Range(0, 13).Select(_ => Enumerable.Repeat(1.0 / 13, 13).ToArray())],
                new double[13], [.. Enumerable.Repeat(1.0, 13)], [.. Enumerable.Repeat(1.0 / 13, 13)]);

            tooManyStates.Should().Throw<ArgumentException>().WithParameterName("transitions");
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianLogLikelihood")]
        public void GaussianLogLikelihoodMatchesBruteForce()
        {
            var expected = BruteForce(5, gaussianTransitions, hmmInitial,
                (t, s) => NormalDensity(gaussianObservations[t], gaussianMeans[s], gaussianDeviations[s]));

            var result = hmm.HiddenMarkovGaussianLogLikelihood(gaussianObservations, gaussianTransitions,
                gaussianMeans, gaussianDeviations, hmmInitial);

            result.LogLikelihood.Should().BeApproximately(expected.LogLikelihood, 1e-9);
            result.StepLogProbabilities.Sum().Should().BeApproximately(expected.LogLikelihood, 1e-9);
            result.MostSurprisingIndex.Should().BeInRange(0, 4);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianLogLikelihood")]
        public void GaussianLogLikelihoodFlagsOutlierAsMostSurprising()
        {
            var series = new[] { 0.1, -0.2, 0.3, 40.0, 0.0 };

            var result = hmm.HiddenMarkovGaussianLogLikelihood(series, gaussianTransitions, gaussianMeans,
                gaussianDeviations, hmmInitial);

            result.MostSurprisingIndex.Should().Be(3);

            var single = hmm.HiddenMarkovGaussianLogLikelihood([0.0], gaussianTransitions, gaussianMeans,
                gaussianDeviations, hmmInitial);

            single.LogLikelihood.Should()
                .BeApproximately(Math.Log(0.5 * NormalDensity(0, 0, 1) + 0.5 * NormalDensity(0, 10, 1)), 1e-12);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianPosterior")]
        public void GaussianPosteriorMatchesBruteForceMarginals()
        {
            var expected = BruteForce(5, gaussianTransitions, hmmInitial,
                (t, s) => NormalDensity(gaussianObservations[t], gaussianMeans[s], gaussianDeviations[s]));

            var result = hmm.HiddenMarkovGaussianPosterior(gaussianObservations, gaussianTransitions, gaussianMeans,
                gaussianDeviations, hmmInitial);

            for (var t = 0; t < 5; t++)
            {
                result.StateProbabilities[t][0].Should().BeApproximately(expected.Posterior[t][0], 1e-9);
                result.StateProbabilities[t][1].Should().BeApproximately(expected.Posterior[t][1], 1e-9);
            }

            result.MostLikelyStates.Should().Equal(0, 0, 1, 1, 0);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianPosterior")]
        public void GaussianPosteriorOfSingleObservationIsBayesRule()
        {
            var result = hmm.HiddenMarkovGaussianPosterior([5.0], gaussianTransitions, gaussianMeans,
                gaussianDeviations, hmmInitial);

            result.StateProbabilities[0][0].Should().BeApproximately(0.5, 1e-12);

            var near = hmm.HiddenMarkovGaussianPosterior([1.0], gaussianTransitions, gaussianMeans,
                gaussianDeviations, hmmInitial);
            near.StateProbabilities[0][0].Should().BeGreaterThan(0.999);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianNextValueDistribution")]
        public void GaussianNextValueMomentsFollowMixtureFormulas()
        {
            var expected = BruteForce(5, gaussianTransitions, hmmInitial,
                (t, s) => NormalDensity(gaussianObservations[t], gaussianMeans[s], gaussianDeviations[s]));

            var filtered = expected.Posterior[4];
            var next = new[] { filtered[0] * 0.9 + filtered[1] * 0.1, filtered[0] * 0.1 + filtered[1] * 0.9 };
            var mean = next[1] * 10;
            var variance = next[0] * 1 + next[1] * (1 + 100) - mean * mean;

            var result = hmm.HiddenMarkovGaussianNextValueDistribution(gaussianObservations, gaussianTransitions,
                gaussianMeans, gaussianDeviations, hmmInitial);

            result.FilteredStateProbabilities[0].Should().BeApproximately(filtered[0], 1e-9);
            result.NextStateProbabilities[1].Should().BeApproximately(next[1], 1e-9);
            result.ExpectedNextValue.Should().BeApproximately(mean, 1e-9);
            result.NextValueStandardDeviation.Should().BeApproximately(Math.Sqrt(variance), 1e-9);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianNextValueDistribution")]
        public void GaussianNextValueOfStickySingleRegimeApproachesRegimeMean()
        {
            var sticky = new[] { new[] { 1.0, 0 }, new[] { 0.0, 1 } };

            var result = hmm.HiddenMarkovGaussianNextValueDistribution([10.0, 9.5, 10.5], sticky,
                gaussianMeans, gaussianDeviations, hmmInitial);

            result.ExpectedNextValue.Should().BeApproximately(10, 1e-6);
            result.NextValueStandardDeviation.Should().BeApproximately(1, 1e-6);

            Action empty = () => hmm.HiddenMarkovGaussianNextValueDistribution([], sticky,
                gaussianMeans, gaussianDeviations, hmmInitial);

            empty.Should().Throw<ArgumentException>().WithParameterName("observations");
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovDiscreteTrain")]
        public void DiscreteTrainingRecoversTwoWellSeparatedRegimes()
        {
            var sequences = SimulateDiscrete(3000, 11);

            var result = hmm.HiddenMarkovDiscreteTrain(sequences, 2);

            result.States.Should().Be(2);
            result.Symbols.Should().Be(2);
            var pure = result.Emissions.Select(row => Math.Max(row[0], row[1])).ToArray();
            pure.Should().OnlyContain(p => p > 0.9 && p < 0.99);
            result.Transitions[0][0].Should().BeGreaterThan(0.9);
            result.Transitions[1][1].Should().BeGreaterThan(0.9);
            result.Transitions.Concat(result.Emissions).Should().OnlyContain(row => Math.Abs(row.Sum() - 1) < 1e-9);
            result.LogLikelihood.Should().BeGreaterThan(-2000);
            result.Bic.Should().BeGreaterThan(-2 * result.LogLikelihood);
            result.TrainingObservations.Should().Be(3000);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovDiscreteTrain")]
        public void DiscreteTrainingIsDeterministicForSeedAndValidatesArguments()
        {
            var sequences = SimulateDiscrete(600, 3);

            var first = hmm.HiddenMarkovDiscreteTrain(sequences, 2, 0, 2, 30, 1e-6, 5);
            var second = hmm.HiddenMarkovDiscreteTrain(sequences, 2, 0, 2, 30, 1e-6, 5);

            second.LogLikelihood.Should().Be(first.LogLikelihood);

            second.Transitions.Select(r => r.ToArray()).Should()
                .BeEquivalentTo(first.Transitions.Select(r => r.ToArray()));

            var single = hmm.HiddenMarkovDiscreteTrain(sequences, 1);
            single.Transitions[0][0].Should().BeApproximately(1, 1e-9);
            Action noStates = () => hmm.HiddenMarkovDiscreteTrain(sequences, 0);
            noStates.Should().Throw<ArgumentException>().WithParameterName("states");
            Action tooMany = () => hmm.HiddenMarkovDiscreteTrain(sequences, 13);
            tooMany.Should().Throw<ArgumentException>().WithParameterName("states");
            Action badCodes = () => hmm.HiddenMarkovDiscreteTrain([[0, 1, 5]], 2, 3);
            badCodes.Should().Throw<ArgumentException>().WithParameterName("symbols");
            Action negative = () => hmm.HiddenMarkovDiscreteTrain([[0, -1, 1]], 2);
            negative.Should().Throw<ArgumentException>().WithParameterName("sequences");
            Action oneSymbol = () => hmm.HiddenMarkovDiscreteTrain([[0, 0, 0, 0]], 2);
            oneSymbol.Should().Throw<ArgumentException>().WithParameterName("symbols");
            Action tooShort = () => hmm.HiddenMarkovDiscreteTrain([[0, 1]], 2);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("states");
            Action badRestarts = () => hmm.HiddenMarkovDiscreteTrain(sequences, 2, 0, 0);
            badRestarts.Should().Throw<ArgumentException>().WithParameterName("restarts");
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianTrain")]
        public void GaussianTrainingRecoversRegimeMeansDeviationsAndPersistence()
        {
            var series = SimulateGaussian(2000, 7);

            var result = hmm.HiddenMarkovGaussianTrain([series], 2);

            result.Means[0].Should().BeApproximately(0, 0.3);
            result.Means[1].Should().BeApproximately(10, 0.3);
            result.StandardDeviations[0].Should().BeApproximately(1, 0.15);
            result.StandardDeviations[1].Should().BeApproximately(1, 0.15);
            result.Transitions[0][0].Should().BeApproximately(0.95, 0.04);
            result.Transitions[1][1].Should().BeApproximately(0.95, 0.04);
            result.Transitions.Should().OnlyContain(row => Math.Abs(row.Sum() - 1) < 1e-9);
            result.Converged.Should().BeTrue();
            result.Aic.Should().BeApproximately(2 * 7 - 2 * result.LogLikelihood, 1e-9);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianTrain")]
        public void GaussianTrainingSingleRegimeMatchesSampleMomentsAndValidates()
        {
            var series = SimulateGaussian(400, 2);

            var one = hmm.HiddenMarkovGaussianTrain([series], 1);

            one.Means[0].Should().BeApproximately(series.Average(), 1e-6);
            var sampleSd = Math.Sqrt(series.Sum(x => Math.Pow(x - series.Average(), 2)) / (series.Length - 1));
            one.StandardDeviations[0].Should().BeApproximately(sampleSd, 1e-6);
            var again = hmm.HiddenMarkovGaussianTrain([series], 2, 2, 40, 1e-6, 9);

            hmm.HiddenMarkovGaussianTrain([series], 2, 2, 40, 1e-6, 9).LogLikelihood.Should()
                .Be(again.LogLikelihood);

            Action constant = () => hmm.HiddenMarkovGaussianTrain([[2.0, 2, 2, 2, 2, 2]], 2);
            constant.Should().Throw<ArgumentException>().WithParameterName("sequences");
            Action tooShort = () => hmm.HiddenMarkovGaussianTrain([[1.0, 2]], 2);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("sequences");
            Action nan = () => hmm.HiddenMarkovGaussianTrain([[1.0, double.NaN, 3, 4, 5]], 2);
            nan.Should().Throw<ArgumentException>();
            Action badFraction = () => hmm.HiddenMarkovGaussianTrain([series], 2, 1, 10, 1e-6, 1, 1.5);
            badFraction.Should().Throw<ArgumentException>().WithParameterName("minimumStandardDeviationFraction");
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianSelectStateCount")]
        public void StateSelectionPicksTwoRegimesByBicForTwoRegimeData()
        {
            var series = SimulateGaussian(1500, 13);

            var result = hmm.HiddenMarkovGaussianSelectStateCount([series], 3);

            result.Candidates.Select(c => c.States).Should().Equal(1, 2, 3);
            result.BestStatesByBic.Should().Be(2);
            result.BestModel.States.Should().Be(2);
            result.Candidates[1].LogLikelihood.Should().BeGreaterThan(result.Candidates[0].LogLikelihood);
            result.Candidates[0].Parameters.Should().Be(2);
            result.Candidates[1].Parameters.Should().Be(7);
        }

        [Fact]
        [Trait("Covers", "HiddenMarkovGaussianSelectStateCount")]
        public void StateSelectionSingleRegimeLogLikelihoodMatchesClosedFormAndValidates()
        {
            var series = new[] { 1.0, 2, 3, 4, 5, 6, 7, 8 };
            var mean = series.Average();
            var variance = series.Sum(x => (x - mean) * (x - mean)) / series.Length;

            var result = hmm.HiddenMarkovGaussianSelectStateCount([series], 1);

            result.Candidates.Should().HaveCount(1);

            result.Candidates[0].LogLikelihood.Should()
                .BeApproximately(-series.Length / 2.0 * (Math.Log(2 * Math.PI * variance) + 1), 1e-9);

            result.BestStatesByBic.Should().Be(1);
            result.BestModel.Means[0].Should().BeApproximately(mean, 1e-12);

            Action constant = () =>
                hmm.HiddenMarkovGaussianSelectStateCount([[3.0, 3, 3, 3, 3, 3, 3, 3]], 2);

            constant.Should().Throw<ArgumentException>().WithParameterName("sequences");
            Action badStates = () => hmm.HiddenMarkovGaussianSelectStateCount([series], 0);
            badStates.Should().Throw<ArgumentException>().WithParameterName("states");
        }

        private static readonly double[][] twoStateChain = [[0.9, 0.1], [0.5, 0.5]];
        private readonly MarkovChainTools markov = new();
        private readonly SequencePatternTools patterns = new();

        private static string[] Chars(string s)
        {
            return [.. s.Select(c => c.ToString())];
        }

        [Fact]
        [Trait("Covers", "MarkovChainTransitionMatrix")]
        public void TransitionMatrixEstimateMatchesHandCountedTable()
        {
            var result = markov.MarkovChainTransitionMatrix(Chars("AABABBA"));

            result.States.Should().Equal("A", "B");
            result.StateCounts.Should().Equal(4, 3);
            result.TransitionCounts[0].Should().Equal(1, 2);
            result.TransitionCounts[1].Should().Equal(2, 1);
            result.TransitionProbabilities[0][1].Should().BeApproximately(2.0 / 3, 1e-12);
            result.TransitionProbabilities[1][0].Should().BeApproximately(2.0 / 3, 1e-12);
            result.StationaryDistribution[0].Should().BeApproximately(0.5, 1e-9);
            result.StationaryIsUnique.Should().BeTrue();
            result.ExpectedReturnSteps[0].Should().BeApproximately(2.0, 1e-9);
            result.MostLikelyNextState.Should().Equal("B", "A");

            result.EntropyRateBits.Should()
                .BeApproximately(-(1.0 / 3 * Math.Log(1.0 / 3, 2) + 2.0 / 3 * Math.Log(2.0 / 3, 2)), 1e-9);

            result.Transitions.Should().Be(6);
        }

        [Fact]
        [Trait("Covers", "MarkovChainTransitionMatrix")]
        public void TransitionMatrixAbsorbingStateAndMissingExitsAndSmoothing()
        {
            var absorbing = markov.MarkovChainTransitionMatrix(Chars("ABCCC"));
            absorbing.AbsorbingStates.Should().Equal("C");
            absorbing.StationaryDistribution.Should().Equal(0.0, 0.0, 1.0);
            absorbing.ExpectedReturnSteps[2].Should().BeApproximately(1, 1e-12);
            absorbing.ExpectedReturnSteps[0].Should().Be(double.PositiveInfinity);

            var noExit = markov.MarkovChainTransitionMatrix(Chars("AB"));
            noExit.StatesWithoutOutgoingTransitions.Should().Equal("B");
            var smoothed = markov.MarkovChainTransitionMatrix(Chars("AB"), 1);
            smoothed.StatesWithoutOutgoingTransitions.Should().BeEmpty();
            smoothed.TransitionProbabilities[0][1].Should().BeApproximately(2.0 / 3, 1e-12);
            smoothed.TransitionProbabilities[1][0].Should().BeApproximately(0.5, 1e-12);

            Action shortSequence = () => markov.MarkovChainTransitionMatrix(["A"]);
            shortSequence.Should().Throw<ArgumentException>().WithParameterName("sequence");
            Action nullLabel = () => markov.MarkovChainTransitionMatrix(["A", null!, "B"]);
            nullLabel.Should().Throw<ArgumentException>().WithParameterName("sequence");
            Action badSmoothing = () => markov.MarkovChainTransitionMatrix(Chars("ABAB"), -1);
            badSmoothing.Should().Throw<ArgumentException>().WithParameterName("smoothing");
        }

        [Fact]
        [Trait("Covers", "MarkovChainStationaryDistribution")]
        public void StationaryDistributionOfTwoStateChainIsFiveSixthsOneSixth()
        {
            var result = markov.MarkovChainStationaryDistribution(twoStateChain);

            result.StationaryDistribution[0].Should().BeApproximately(5.0 / 6, 1e-12);
            result.StationaryDistribution[1].Should().BeApproximately(1.0 / 6, 1e-12);
            result.IsUnique.Should().BeTrue();
            result.RecurrentClasses.Should().Be(1);
            result.ExpectedReturnSteps[0].Should().BeApproximately(1.2, 1e-12);
            result.ExpectedReturnSteps[1].Should().BeApproximately(6.0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "MarkovChainStationaryDistribution")]
        public void StationaryDistributionPeriodicReducibleAndInvalidMatrices()
        {
            var cycle = markov.MarkovChainStationaryDistribution([
                [0.0, 1, 0], [0.0, 0, 1], [1.0, 0, 0]
            ]);

            cycle.StationaryDistribution.Should().OnlyContain(v => Math.Abs(v - 1.0 / 3) < 1e-12);

            var split = markov.MarkovChainStationaryDistribution([[1.0, 0], [0.0, 1]]);
            split.IsUnique.Should().BeFalse();
            split.RecurrentClasses.Should().Be(2);
            split.AbsorbingStates.Should().Equal(0, 1);
            split.StationaryDistribution[0].Should().BeApproximately(0.5, 1e-9);

            var transient = markov.MarkovChainStationaryDistribution([[0.5, 0.5], [0.0, 1]]);
            transient.StationaryDistribution.Should().Equal(0.0, 1.0);
            transient.TransientStates.Should().Equal(0);

            markov.MarkovChainStationaryDistribution([[1.0]]).StationaryDistribution.Should()
                .Equal(1.0);

            Action badSum = () =>
                markov.MarkovChainStationaryDistribution([[0.5, 0.4], [0.5, 0.5]]);

            badSum.Should().Throw<ArgumentException>().WithParameterName("transitionMatrix");

            Action notSquare = () =>
                markov.MarkovChainStationaryDistribution([[1.0, 0], [1.0]]);

            notSquare.Should().Throw<ArgumentException>().WithParameterName("transitionMatrix");

            Action negative = () =>
                markov.MarkovChainStationaryDistribution([[1.5, -0.5], [0.5, 0.5]]);

            negative.Should().Throw<ArgumentException>().WithParameterName("transitionMatrix");
        }

        [Fact]
        [Trait("Covers", "MarkovChainMultiStepProbabilities")]
        public void MultiStepProbabilitiesMatchSquaredMatrixByHand()
        {
            var result = markov.MarkovChainMultiStepProbabilities(twoStateChain, 2, 0);

            result.StepMatrix[0][0].Should().BeApproximately(0.86, 1e-12);
            result.StepMatrix[0][1].Should().BeApproximately(0.14, 1e-12);
            result.StepMatrix[1][0].Should().BeApproximately(0.70, 1e-12);
            result.StepMatrix[1][1].Should().BeApproximately(0.30, 1e-12);

            result.Distribution.Should().BeEquivalentTo([0.86, 0.14],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-12))
                    .WhenTypeIs<double>());
        }

        [Fact]
        [Trait("Covers", "MarkovChainMultiStepProbabilities")]
        public void MultiStepProbabilitiesZeroStepsLongRunAndValidation()
        {
            markov.MarkovChainMultiStepProbabilities(twoStateChain, 0, 1).Distribution.Should().Equal(0.0, 1.0);

            markov.MarkovChainMultiStepProbabilities(twoStateChain, 1, -1, [0.5, 0.5]).Distribution[0]
                .Should().BeApproximately(0.7, 1e-12);

            markov.MarkovChainMultiStepProbabilities(twoStateChain, 200, 1).Distribution[0].Should()
                .BeApproximately(5.0 / 6, 1e-9);

            Action neither = () => markov.MarkovChainMultiStepProbabilities(twoStateChain, 1);
            neither.Should().Throw<ArgumentException>().WithParameterName("startState");
            Action both = () => markov.MarkovChainMultiStepProbabilities(twoStateChain, 1, 0, [0.5, 0.5]);
            both.Should().Throw<ArgumentException>().WithParameterName("startState");
            Action badSteps = () => markov.MarkovChainMultiStepProbabilities(twoStateChain, -1, 0);
            badSteps.Should().Throw<ArgumentException>().WithParameterName("steps");
            Action badStart = () => markov.MarkovChainMultiStepProbabilities(twoStateChain, 1, -1, [0.9, 0.9]);
            badStart.Should().Throw<ArgumentException>().WithParameterName("startDistribution");
        }

        [Fact]
        [Trait("Covers", "MarkovChainSequenceSurprise")]
        public void SequenceSurpriseMatchesHandComputedSmoothedProbabilities()
        {
            var result = markov.MarkovChainSequenceSurprise(Chars("ABABAB"), Chars("ABAA"));

            result.SurprisalBits[0].Should().BeApproximately(-Math.Log(0.8, 2), 1e-12);
            result.SurprisalBits[1].Should().BeApproximately(-Math.Log(0.75, 2), 1e-12);
            result.SurprisalBits[2].Should().BeApproximately(-Math.Log(0.2, 2), 1e-12);
            result.MostSurprisingTransition.Should().Be(2);
            result.UnseenTransitions.Should().Be(1);

            result.AverageBitsPerTransition.Should()
                .BeApproximately((-Math.Log(0.8, 2) - Math.Log(0.75, 2) - Math.Log(0.2, 2)) / 3, 1e-12);

            result.Perplexity.Should().BeApproximately(Math.Pow(2, result.AverageBitsPerTransition), 1e-12);
            result.TotalLogLikelihood.Should().BeApproximately(Math.Log(0.8) + Math.Log(0.75) + Math.Log(0.2), 1e-12);
        }

        [Fact]
        [Trait("Covers", "MarkovChainSequenceSurprise")]
        public void SequenceSurpriseHandlesNewStatesAndValidation()
        {
            var result = markov.MarkovChainSequenceSurprise(Chars("ABAB"), Chars("ABZ"));

            result.UnseenTransitions.Should().Be(1);
            result.SurprisalBits[1].Should().BeGreaterThan(result.SurprisalBits[0]);
            Action noSmooth = () => markov.MarkovChainSequenceSurprise(Chars("ABAB"), Chars("ABAB"), 0);
            noSmooth.Should().Throw<ArgumentException>().WithParameterName("smoothing");
            Action shortTest = () => markov.MarkovChainSequenceSurprise(Chars("ABAB"), ["A"]);
            shortTest.Should().Throw<ArgumentException>().WithParameterName("testSequence");
        }

        [Fact]
        [Trait("Covers", "MarkovChainMeanFirstPassageTimes")]
        public void MeanFirstPassageTimesOfTwoStateChainMatchGeometricFormulas()
        {
            var result = markov.MarkovChainMeanFirstPassageTimes(twoStateChain);

            result.MeanFirstPassageSteps[0][1].Should().BeApproximately(10.0, 1e-9);
            result.MeanFirstPassageSteps[1][0].Should().BeApproximately(2.0, 1e-9);
            result.MeanFirstPassageSteps[0][0].Should().BeApproximately(1.2, 1e-9);
            result.MeanFirstPassageSteps[1][1].Should().BeApproximately(6.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "MarkovChainMeanFirstPassageTimes")]
        public void MeanFirstPassageTimesUnreachableTargetsAreInfinite()
        {
            var result = markov.MarkovChainMeanFirstPassageTimes([[1.0, 0], [0.0, 1]]);

            result.MeanFirstPassageSteps[0][1].Should().Be(double.PositiveInfinity);
            result.MeanFirstPassageSteps[1][0].Should().Be(double.PositiveInfinity);
            result.MeanFirstPassageSteps[0][0].Should().Be(1);

            var leaky = markov.MarkovChainMeanFirstPassageTimes([
                [0.5, 0.25, 0.25], [0.0, 1, 0], [0.0, 0, 1]
            ]);

            leaky.MeanFirstPassageSteps[0][1].Should().Be(double.PositiveInfinity);
            leaky.MeanFirstPassageSteps[0][2].Should().Be(double.PositiveInfinity);

            var chain = markov.MarkovChainMeanFirstPassageTimes([
                [0.5, 0.5, 0], [0.0, 0, 1], [0.0, 0, 1]
            ]);
            chain.MeanFirstPassageSteps[0][1].Should().BeApproximately(2.0, 1e-9);
            chain.MeanFirstPassageSteps[0][2].Should().BeApproximately(3.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "MarkovChainAbsorptionAnalysis")]
        public void GamblersRuinAbsorptionMatchesClassicalResults()
        {
            var p = new[]
            {
                new[] { 1.0, 0, 0, 0, 0 }, new[] { 0.5, 0, 0.5, 0, 0 }, new[] { 0.0, 0.5, 0, 0.5, 0 },
                new[] { 0.0, 0, 0.5, 0, 0.5 }, new[] { 0.0, 0, 0, 0, 1 }
            };

            var result = markov.MarkovChainAbsorptionAnalysis(p);

            result.TransientStates.Should().Equal(1, 2, 3);
            result.AbsorbingStates.Should().Equal(0, 4);
            result.ExpectedStepsToAbsorption[0].Should().BeApproximately(3, 1e-9);
            result.ExpectedStepsToAbsorption[1].Should().BeApproximately(4, 1e-9);
            result.ExpectedStepsToAbsorption[2].Should().BeApproximately(3, 1e-9);
            result.AbsorptionProbabilities[0][1].Should().BeApproximately(0.25, 1e-9);
            result.AbsorptionProbabilities[1][1].Should().BeApproximately(0.5, 1e-9);
            result.AbsorptionProbabilities[2][1].Should().BeApproximately(0.75, 1e-9);
            result.ExpectedVisits[1][1].Should().BeApproximately(2.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "MarkovChainAbsorptionAnalysis")]
        public void AbsorptionAnalysisRejectsChainsWithoutAbsorbingStructure()
        {
            Action none = () => markov.MarkovChainAbsorptionAnalysis(twoStateChain);
            none.Should().Throw<ArgumentException>().WithParameterName("transitionMatrix");

            Action mixed = () => markov.MarkovChainAbsorptionAnalysis([
                [0.5, 0.5, 0, 0], [0.5, 0.5, 0, 0], [0.0, 0, 0.5, 0.5], [0.0, 0, 0, 1]
            ]);
            mixed.Should().Throw<ArgumentException>();
            var direct = markov.MarkovChainAbsorptionAnalysis([[0.0, 1], [0.0, 1]]);
            direct.ExpectedStepsToAbsorption.Should().Equal(1.0);
            direct.AbsorptionProbabilities[0][0].Should().BeApproximately(1, 1e-12);
        }

        [Fact]
        [Trait("Covers", "MarkovDependenceTest")]
        public void DependenceTestOfPerfectlyAlternatingSequenceHasChiSquareEqualToTransitions()
        {
            var result = markov.MarkovDependenceTest(Chars("ABABABABABAB"));

            result.ChiSquareStatistic.Should().BeApproximately(11.0, 1e-9);
            result.DegreesOfFreedom.Should().Be(1);
            result.PValue.Should().BeApproximately(0.000911, 2e-5);
            result.CramersV.Should().BeApproximately(1.0, 1e-9);
            result.Dependent.Should().BeTrue();
            result.Transitions.Should().Be(11);
        }

        [Fact]
        [Trait("Covers", "MarkovDependenceTest")]
        public void DependenceTestOfNearIndependentAndDegenerateSequences()
        {
            var independent = markov.MarkovDependenceTest(Chars("AABBAABBAABB"));
            independent.ChiSquareStatistic.Should().BeApproximately(0.110, 5e-3);
            independent.Dependent.Should().BeFalse();

            Action constant = () => markov.MarkovDependenceTest(Chars("AAAAAAAAAAAA"));
            constant.Should().Throw<ArgumentException>().WithParameterName("sequence");
            Action shortSequence = () => markov.MarkovDependenceTest(Chars("ABAB"));
            shortSequence.Should().Throw<ArgumentException>().WithParameterName("sequence");
        }

        [Fact]
        [Trait("Covers", "NGramFrequencyTable")]
        public void NGramCountsMatchHandCount()
        {
            var bigrams = patterns.NGramFrequencyTable(Chars("ababa"));

            bigrams.NGrams[0].Should().Equal("a", "b");
            bigrams.NGrams[1].Should().Equal("b", "a");
            bigrams.Counts.Should().Equal(2, 2);
            bigrams.Shares.Should().Equal(0.5, 0.5);
            bigrams.TotalNGrams.Should().Be(4);
            bigrams.DistinctNGrams.Should().Be(2);
            var unigrams = patterns.NGramFrequencyTable(Chars("ababa"), 1);
            unigrams.NGrams[0].Should().Equal("a");
            unigrams.Counts.Should().Equal(3, 2);
        }

        [Fact]
        [Trait("Covers", "NGramFrequencyTable")]
        public void NGramTopKLimitsAndValidation()
        {
            patterns.NGramFrequencyTable(Chars("abcabcab"), 2, 1).NGrams.Should().HaveCount(1);
            patterns.NGramFrequencyTable(Chars("abc"), 3).TotalNGrams.Should().Be(1);
            Action tooLong = () => patterns.NGramFrequencyTable(Chars("ab"), 3);
            tooLong.Should().Throw<ArgumentException>().WithParameterName("sequence");
            Action bad = () => patterns.NGramFrequencyTable(Chars("ab"), 0);
            bad.Should().Throw<ArgumentException>().WithParameterName("n");
        }

        [Fact]
        [Trait("Covers", "SymbolSequenceEntropyProfile")]
        public void EntropyProfileOfAlternatingSequenceHasOneBitAndZeroConditionalEntropy()
        {
            var result = patterns.SymbolSequenceEntropyProfile(Chars("abababab"), 2);

            result.BlockEntropiesBits[0].Should().BeApproximately(1.0, 1e-12);

            result.BlockEntropiesBits[1].Should()
                .BeApproximately(-(4.0 / 7 * Math.Log(4.0 / 7, 2) + 3.0 / 7 * Math.Log(3.0 / 7, 2)), 1e-12);

            result.ConditionalEntropiesBits[0].Should().BeApproximately(1.0, 1e-12);
            result.ConditionalEntropiesBits[1].Should().BeApproximately(0.0, 1e-12);
            result.NormalisedFirstOrderEntropy.Should().BeApproximately(1.0, 1e-12);
            result.Redundancy.Should().BeApproximately(0.0, 1e-12);
            result.AlphabetSize.Should().Be(2);
        }

        [Fact]
        [Trait("Covers", "SymbolSequenceEntropyProfile")]
        public void EntropyProfileOfConstantAndCyclicSequences()
        {
            var constant = patterns.SymbolSequenceEntropyProfile(Chars("aaaaaa"));
            constant.BlockEntropiesBits[0].Should().Be(0);
            constant.NormalisedFirstOrderEntropy.Should().Be(double.NaN);

            var cyclic = patterns.SymbolSequenceEntropyProfile(Chars("abcdabcdabcd"), 1);
            cyclic.BlockEntropiesBits[0].Should().BeApproximately(2.0, 1e-12);
            cyclic.ConditionalEntropiesBits.Should().HaveCount(1);
            patterns.SymbolSequenceEntropyProfile(Chars("abc"), 10).BlockEntropiesBits.Should().HaveCount(2);
            Action bad = () => patterns.SymbolSequenceEntropyProfile(Chars("abc"), 0);
            bad.Should().Throw<ArgumentException>().WithParameterName("maxOrder");
        }

        [Fact]
        [Trait("Covers", "LempelZivSequenceComplexity")]
        public void LempelZivClassicExampleHasSixPhrases()
        {
            var result = patterns.LempelZivSequenceComplexity(Chars("0001101001000101"));

            result.PhraseCount.Should().Be(6);
            result.NormalisedComplexity.Should().BeApproximately(6 * 4.0 / 16, 1e-12);
            result.AlphabetSize.Should().Be(2);
            result.Length.Should().Be(16);
        }

        [Fact]
        [Trait("Covers", "LempelZivSequenceComplexity")]
        public void LempelZivOfConstantShortAndPeriodicSequences()
        {
            patterns.LempelZivSequenceComplexity(Chars("aaaa")).PhraseCount.Should().Be(2);
            patterns.LempelZivSequenceComplexity(Chars("aaaa")).NormalisedComplexity.Should().Be(double.NaN);
            patterns.LempelZivSequenceComplexity(Chars("ab")).PhraseCount.Should().Be(2);
            var periodic = patterns.LempelZivSequenceComplexity(Chars(string.Concat(Enumerable.Repeat("abc", 40))));
            periodic.NormalisedComplexity.Should().BeLessThan(0.2);
            Action bad = () => patterns.LempelZivSequenceComplexity(["a"]);
            bad.Should().Throw<ArgumentException>().WithParameterName("sequence");
        }

        [Fact]
        [Trait("Covers", "RunLengthEncodeSequence")]
        public void RunLengthEncodingMatchesHandComputation()
        {
            var result = patterns.RunLengthEncodeSequence(Chars("aabccc"));

            result.Symbols.Should().Equal("a", "b", "c");
            result.Lengths.Should().Equal(2, 1, 3);
            result.StartIndices.Should().Equal(0, 2, 3);
            result.RunCount.Should().Be(3);
            result.LongestRun.Should().Be(3);
            result.MeanRunLength.Should().Be(2);
        }

        [Fact]
        [Trait("Covers", "RunLengthEncodeSequence")]
        public void RunLengthEncodingSingleSymbolAndValidation()
        {
            var single = patterns.RunLengthEncodeSequence(["x"]);
            single.Lengths.Should().Equal(1);
            patterns.RunLengthEncodeSequence(Chars("aaaa")).RunCount.Should().Be(1);
            Action empty = () => patterns.RunLengthEncodeSequence([]);
            empty.Should().Throw<ArgumentException>().WithParameterName("sequence");
        }

        [Fact]
        [Trait("Covers", "ThresholdRunLengths")]
        public void ThresholdRunsMatchHandComputation()
        {
            var result = patterns.ThresholdRunLengths([1.0, 5, 6, 2, 7, 8, 9, 1], 4);

            result.StartIndices.Should().Equal(1, 4);
            result.Lengths.Should().Equal(2, 3);
            result.LongestRun.Should().Be(3);
            result.MeanRunLength.Should().Be(2.5);
            result.ShareOfPointsInRuns.Should().BeApproximately(5.0 / 8, 1e-12);
            patterns.ThresholdRunLengths([4.0, 5], 4, ThresholdComparison.AtOrAbove).Lengths.Should().Equal(2);

            patterns.ThresholdRunLengths([1.0, 5, 0], 4, ThresholdComparison.Below).StartIndices.Should()
                .Equal(0, 2);

            patterns.ThresholdRunLengths([4.0, 5], 4, ThresholdComparison.AtOrBelow).Lengths.Should().Equal(1);
        }

        [Fact]
        [Trait("Covers", "ThresholdRunLengths")]
        public void ThresholdRunsNaNBreaksStreaksAndNoHits()
        {
            patterns.ThresholdRunLengths([5.0, double.NaN, 6], 4).Lengths.Should().Equal(1, 1);
            var none = patterns.ThresholdRunLengths([1.0, 2], 10);
            none.RunCount.Should().Be(0);
            none.MeanRunLength.Should().Be(double.NaN);
            Action bad = () => patterns.ThresholdRunLengths([1.0], double.NaN);
            bad.Should().Throw<ArgumentException>().WithParameterName("threshold");
        }

        [Fact]
        [Trait("Covers", "PiecewiseAggregateApproximation")]
        public void PiecewiseAggregateApproximationMatchesExactAndFractionalHandComputation()
        {
            patterns.PiecewiseAggregateApproximation([1.0, 2, 3, 4, 5, 6], 3).Values.Should()
                .Equal(1.5, 3.5, 5.5);

            var fractional = patterns.PiecewiseAggregateApproximation([1.0, 2, 3, 4], 3).Values;
            fractional[0].Should().BeApproximately(1.25, 1e-12);
            fractional[1].Should().BeApproximately(2.5, 1e-12);
            fractional[2].Should().BeApproximately(3.75, 1e-12);
        }

        [Fact]
        [Trait("Covers", "PiecewiseAggregateApproximation")]
        public void PiecewiseAggregateApproximationBoundaries()
        {
            patterns.PiecewiseAggregateApproximation([1.0, 2, 3], 3).Values.Should().Equal(1, 2, 3);
            patterns.PiecewiseAggregateApproximation([1.0, 2, 3], 1).Values.Should().Equal(2);
            Action tooMany = () => patterns.PiecewiseAggregateApproximation([1.0, 2], 3);
            tooMany.Should().Throw<ArgumentException>().WithParameterName("segments");
        }

        [Fact]
        [Trait("Covers", "SymbolicAggregateApproximation")]
        public void SaxOfLinearRampGivesAbcdWithPublishedBreakpoints()
        {
            var result = patterns.SymbolicAggregateApproximation([1.0, 2, 3, 4, 5, 6, 7, 8], 4);

            result.Word.Should().Be("abcd");
            result.Breakpoints[0].Should().BeApproximately(-0.6745, 1e-4);
            result.Breakpoints[1].Should().BeApproximately(0.0, 1e-9);
            result.Breakpoints[2].Should().BeApproximately(0.6745, 1e-4);
            result.Mean.Should().BeApproximately(4.5, 1e-12);
            result.StandardDeviation.Should().BeApproximately(Math.Sqrt(5.25), 1e-12);
            result.Paa[0].Should().BeApproximately(-3.0 / Math.Sqrt(5.25), 1e-12);
            patterns.SymbolicAggregateApproximation([1.0, 2, 3, 4, 5, 6, 7, 8], 4, 3).Word.Should().Be("aacc");
        }

        [Fact]
        [Trait("Covers", "SymbolicAggregateApproximation")]
        public void SaxConstantSeriesRawModeAndValidation()
        {
            patterns.SymbolicAggregateApproximation([3.0, 3, 3, 3], 2).Word.Should().Be("cc");
            patterns.SymbolicAggregateApproximation([-5.0, 5], 2, 4, false).Word.Should().Be("ad");
            Action badAlphabet = () => patterns.SymbolicAggregateApproximation([1.0, 2], 2, 1);
            badAlphabet.Should().Throw<ArgumentException>().WithParameterName("alphabetSize");
            Action badSegments = () => patterns.SymbolicAggregateApproximation([1.0, 2], 5);
            badSegments.Should().Throw<ArgumentException>().WithParameterName("segments");
        }

        [Fact]
        [Trait("Covers", "DynamicTimeWarpingDistance")]
        public void DtwMatchesHandFilledCostMatrix()
        {
            var result = patterns.DynamicTimeWarpingDistance([1.0, 2, 3], [2.0, 2, 2, 3, 4]);

            result.Distance.Should().BeApproximately(2.0, 1e-12);
            result.Path.First().Should().Equal(0, 0);
            result.Path.Last().Should().Equal(2, 4);
            result.PathLength.Should().Be(result.Path.Length);
            result.NormalisedDistance.Should().BeApproximately(2.0 / result.PathLength, 1e-12);

            for (var i = 1; i < result.Path.Length; i++)
            {
                (result.Path[i][0] - result.Path[i - 1][0]).Should().BeInRange(0, 1);
                (result.Path[i][1] - result.Path[i - 1][1]).Should().BeInRange(0, 1);
            }
        }

        [Fact]
        [Trait("Covers", "DynamicTimeWarpingDistance")]
        public void DtwIdenticalSquaredCostBandAndValidation()
        {
            patterns.DynamicTimeWarpingDistance(oneToFive, oneToFive).Distance.Should().Be(0);

            patterns.DynamicTimeWarpingDistance([0.0, 4], [0.0, 2, 4]).Distance.Should()
                .BeApproximately(2, 1e-12);

            patterns.DynamicTimeWarpingDistance([0.0, 4], [0.0, 2, 4], -1, true).Distance.Should()
                .BeApproximately(4, 1e-12);

            var band = patterns.DynamicTimeWarpingDistance([1.0, 2, 3], [3.0, 2, 1], 0);
            band.Distance.Should().BeApproximately(4, 1e-12);
            band.PathLength.Should().Be(3);
            Action bad = () => patterns.DynamicTimeWarpingDistance(oneToFive, oneToFive, -5);
            bad.Should().Throw<ArgumentException>().WithParameterName("bandRadius");
            Action nan = () => patterns.DynamicTimeWarpingDistance([double.NaN], oneToFive);
            nan.Should().Throw<ArgumentException>().WithParameterName("x");
        }

        [Fact]
        [Trait("Covers", "SymbolSequenceEditDistance")]
        public void EditDistanceMatchesHandComputation()
        {
            var result = patterns.SymbolSequenceEditDistance(Chars("abc"), Chars("ac"));

            result.Distance.Should().Be(1);
            result.NormalisedDistance.Should().BeApproximately(1.0 / 3, 1e-12);
            result.Similarity.Should().BeApproximately(2.0 / 3, 1e-12);
            patterns.SymbolSequenceEditDistance(Chars("kitten"), Chars("sitting")).Distance.Should().Be(3);
        }

        [Fact]
        [Trait("Covers", "SymbolSequenceEditDistance")]
        public void EditDistanceEmptyAndIdenticalSequences()
        {
            patterns.SymbolSequenceEditDistance([], Chars("ab")).Distance.Should().Be(2);
            patterns.SymbolSequenceEditDistance([], Chars("ab")).NormalisedDistance.Should().Be(1);
            var both = patterns.SymbolSequenceEditDistance([], []);
            both.Distance.Should().Be(0);
            both.Similarity.Should().Be(1);
            patterns.SymbolSequenceEditDistance(Chars("abc"), Chars("abc")).Distance.Should().Be(0);
            Action nullSymbol = () => patterns.SymbolSequenceEditDistance(["a", null!], Chars("a"));
            nullSymbol.Should().Throw<ArgumentException>().WithParameterName("first");
        }

        private readonly SpectralSignalTools spectral = new();

        private static double[] Cosine(int n, int bin, double amplitude = 1.0, double phase = 0)
        {
            return [.. Enumerable.Range(0, n).Select(t => amplitude * Math.Cos(2 * Math.PI * bin * t / n + phase))];
        }

        [Fact]
        [Trait("Covers", "FourierTransformOfSeries")]
        public void FourierTransformOfSampledCosineMatchesClosedForm()
        {
            var result = spectral.FourierTransformOfSeries([1.0, 0, -1, 0]);

            result.Real.Should().HaveCount(4);
            result.Real[0].Should().BeApproximately(0, 1e-12);
            result.Real[1].Should().BeApproximately(2, 1e-12);
            result.Real[3].Should().BeApproximately(2, 1e-12);
            result.Imaginary.Should().OnlyContain(v => Math.Abs(v) < 1e-12);
            result.Frequencies.Should().Equal(0, 0.25, 0.5);
            result.Magnitudes[1].Should().BeApproximately(2, 1e-12);
            result.Amplitudes[1].Should().BeApproximately(1, 1e-12);
            result.Amplitudes[2].Should().BeApproximately(0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "FourierTransformOfSeries")]
        public void FourierTransformOfRampOfLengthFiveMatchesAnalyticFormulaAndScalesFrequencies()
        {
            var result = spectral.FourierTransformOfSeries(oneToFive, 60);

            result.Real[0].Should().BeApproximately(15, 1e-9);
            var omega = 2 * Math.PI / 5;
            var denominatorReal = Math.Cos(omega) - 1;
            var denominatorImaginary = -Math.Sin(omega);
            var norm = denominatorReal * denominatorReal + denominatorImaginary * denominatorImaginary;
            result.Real[1].Should().BeApproximately(5 * denominatorReal / norm, 1e-9);
            result.Imaginary[1].Should().BeApproximately(-5 * denominatorImaginary / norm, 1e-9);
            result.Frequencies[1].Should().BeApproximately(1.0 / 300, 1e-15);

            Cosine(16, 2, 3).Let(x => spectral.FourierTransformOfSeries(x).Amplitudes[2]).Should()
                .BeApproximately(3, 1e-9);

            Action bad = () => spectral.FourierTransformOfSeries(oneToFive, 0);
            bad.Should().Throw<ArgumentException>().WithParameterName("samplingIntervalSeconds");
        }

        [Fact]
        [Trait("Covers", "InverseFourierTransform")]
        public void InverseFourierTransformRoundTripsForOddAndEvenLengths()
        {
            foreach (var x in new[] { oneToFive, [3.0, -1, 4, 1, -5, 9, 2, 6] })
            {
                var forward = spectral.FourierTransformOfSeries(x);

                var back = spectral.InverseFourierTransform(forward.Real, forward.Imaginary);

                for (var i = 0; i < x.Length; i++)
                {
                    back.Real[i].Should().BeApproximately(x[i], 1e-9);
                    back.Imaginary[i].Should().BeApproximately(0, 1e-9);
                }
            }
        }

        [Fact]
        [Trait("Covers", "InverseFourierTransform")]
        public void InverseFourierTransformRejectsMismatchedParts()
        {
            Action bad = () => spectral.InverseFourierTransform([1.0, 2], [0.0]);
            bad.Should().Throw<ArgumentException>().WithParameterName("imaginary");
            var delta = spectral.InverseFourierTransform([1.0, 1, 1, 1], [0.0, 0, 0, 0]);
            delta.Real[0].Should().BeApproximately(1, 1e-12);
            delta.Real[1].Should().BeApproximately(0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "PowerSpectrumOfSeries")]
        public void PowerSpectrumOfUnitCosineOnBinTwoIsNOverTwoAtPeriodEight()
        {
            var result = spectral.PowerSpectrumOfSeries(Cosine(16, 2));

            result.Power[2].Should().BeApproximately(8.0, 1e-9);
            result.Power.Where((_, k) => k != 2).Should().OnlyContain(p => p < 1e-9);
            result.PeakFrequency.Should().BeApproximately(0.125, 1e-12);
            result.PeakPeriod.Should().BeApproximately(8, 1e-9);
            result.TotalPowerExcludingDc.Should().BeApproximately(8.0, 1e-9);
            result.FrequencyResolution.Should().BeApproximately(1.0 / 16, 1e-12);
            spectral.PowerSpectrumOfSeries(Cosine(16, 2), 3600).PeakPeriod.Should().BeApproximately(28800, 1e-6);
        }

        [Fact]
        [Trait("Covers", "PowerSpectrumOfSeries")]
        public void PowerSpectrumWindowDetrendAndConstantSeries()
        {
            var trendPlusCycle = Cosine(64, 4).Select((v, t) => v + 0.5 * t).ToArray();

            var linear =
                spectral.PowerSpectrumOfSeries(trendPlusCycle, 1, SpectralWindow.Hann, SpectralDetrend.Linear);
            linear.PeakPeriod.Should().BeApproximately(16, 1e-9);
            linear.Window.Should().Be("Hann");

            var constant = spectral.PowerSpectrumOfSeries([5.0, 5, 5, 5, 5, 5]);
            constant.TotalPowerExcludingDc.Should().BeLessThan(1e-20);
            Action tooShort = () => spectral.PowerSpectrumOfSeries([1.0, 2, 3]);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "WelchPowerSpectrum")]
        public void WelchSpectrumAveragesSevenSegmentsAndFindsPeriodEight()
        {
            var result = spectral.WelchPowerSpectrum(Cosine(128, 16), 32);

            result.Segments.Should().Be(7);
            result.PeakPeriod.Should().BeApproximately(8, 1e-9);
            result.FrequencyResolution.Should().BeApproximately(1.0 / 32, 1e-12);
            result.Frequencies.Should().HaveCount(17);
        }

        [Fact]
        [Trait("Covers", "WelchPowerSpectrum")]
        public void WelchSpectrumValidatesSegmentAndOverlap()
        {
            Action tooLong = () => spectral.WelchPowerSpectrum(new double[20], 32);
            tooLong.Should().Throw<ArgumentException>().WithParameterName("segmentLength");
            Action tooShort = () => spectral.WelchPowerSpectrum(new double[20], 4);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("segmentLength");
            Action overlap = () => spectral.WelchPowerSpectrum(new double[40], 16, 1.0);
            overlap.Should().Throw<ArgumentException>().WithParameterName("overlapFraction");
        }

        [Fact]
        [Trait("Covers", "DominantPeriodsOfSeries")]
        public void DominantPeriodsRankTwoCyclesWithPowerSharesInAmplitudeSquaredRatio()
        {
            var x = Cosine(32, 4, 2).Zip(Cosine(32, 8), (a, b) => a + b).ToArray();

            var result = spectral.DominantPeriodsOfSeries(x, 2, 1, SpectralDetrend.None);

            result.Periods.Should().Equal(8.0, 4.0);
            result.PeriodsInSamples.Should().Equal(8.0, 4.0);
            result.PowerShares[0].Should().BeApproximately(0.8, 1e-9);
            result.PowerShares[1].Should().BeApproximately(0.2, 1e-9);
            result.Frequencies[0].Should().BeApproximately(0.125, 1e-12);
            spectral.DominantPeriodsOfSeries(x, 1, 1, SpectralDetrend.None).Periods.Should().Equal(8.0);
        }

        [Fact]
        [Trait("Covers", "DominantPeriodsOfSeries")]
        public void DominantPeriodOfTrendedSeriesAndUnitScaling()
        {
            var x = Enumerable.Range(0, 96).Select(t => 0.5 * t + 10 * Math.Sin(2 * Math.PI * t / 12)).ToArray();

            spectral.DominantPeriodsOfSeries(x).Periods[0].Should().BeApproximately(12, 1e-9);
            spectral.DominantPeriodsOfSeries(x, 1, 3600).Periods[0].Should().BeApproximately(12 * 3600, 1e-6);

            spectral.DominantPeriodsOfSeries([4.0, 4, 4, 4, 4], 3, 1, SpectralDetrend.Mean).Periods.Should()
                .BeEmpty();

            Action bad = () => spectral.DominantPeriodsOfSeries(x, 0);
            bad.Should().Throw<ArgumentException>().WithParameterName("topK");
        }

        [Fact]
        [Trait("Covers", "SpectralSummary")]
        public void SpectralSummaryOfPureCycleIsPeakyAndOfImpulseIsFlat()
        {
            var cycle = spectral.SpectralSummary(Cosine(32, 3));
            cycle.NormalisedSpectralEntropy.Should().BeLessThan(1e-6);
            cycle.SpectralFlatness.Should().BeLessThan(1e-6);
            cycle.DominantFrequency.Should().BeApproximately(3.0 / 32, 1e-12);
            cycle.DominantPeriod.Should().BeApproximately(32.0 / 3, 1e-9);
            cycle.DominantPowerShare.Should().BeApproximately(1, 1e-9);
            cycle.CentroidFrequency.Should().BeApproximately(3.0 / 32, 1e-9);
            cycle.RolloffFrequency.Should().BeApproximately(3.0 / 32, 1e-9);

            var impulse = new double[15];
            impulse[0] = 1;
            var flat = spectral.SpectralSummary(impulse, 1, SpectralDetrend.None);
            flat.NormalisedSpectralEntropy.Should().BeApproximately(1, 1e-9);
            flat.SpectralEntropyBits.Should().BeApproximately(Math.Log(7, 2), 1e-9);
            flat.SpectralFlatness.Should().BeApproximately(1, 1e-9);
        }

        [Fact]
        [Trait("Covers", "SpectralSummary")]
        public void SpectralSummaryOfConstantSeriesIsUndefined()
        {
            var result = spectral.SpectralSummary([2.0, 2, 2, 2, 2]);

            result.TotalPowerExcludingDc.Should().Be(0);
            result.SpectralEntropyBits.Should().Be(double.NaN);
            result.DominantPeriod.Should().Be(double.NaN);
        }

        [Fact]
        [Trait("Covers", "FisherPeriodicityTest")]
        public void FisherGMatchesClosedFormPValueForNinetyPercentDominance()
        {
            var x = Cosine(8, 1, 3).Zip(Cosine(8, 2, Math.Sqrt(0.5)), (a, b) => a + b)
                .Zip(Cosine(8, 3, Math.Sqrt(0.5)), (a, b) => a + b).ToArray();

            var result = spectral.FisherPeriodicityTest(x);

            result.GStatistic.Should().BeApproximately(0.9, 1e-9);
            result.PValue.Should().BeApproximately(3 * 0.1 * 0.1, 1e-9);
            result.Significant.Should().BeTrue();
            result.PeakBin.Should().Be(1);
            result.PeakPeriodInSamples.Should().BeApproximately(8, 1e-12);
            result.TestedFrequencies.Should().Be(3);
        }

        [Fact]
        [Trait("Covers", "FisherPeriodicityTest")]
        public void FisherTestDoesNotFlagNoiseAndConstantIsUndefined()
        {
            var random = new Random(5);
            var noise = Enumerable.Range(0, 64).Select(_ => random.NextDouble()).ToArray();

            spectral.FisherPeriodicityTest(noise).Significant.Should().BeFalse();
            spectral.FisherPeriodicityTest([.. Enumerable.Repeat(1.0, 16)]).PValue.Should().Be(double.NaN);

            var strong =
                spectral.FisherPeriodicityTest([.. Cosine(64, 5).Select((v, i) => v + 0.01 * (i * 7 % 5))]);
            strong.Significant.Should().BeTrue();
            strong.PeakBin.Should().Be(5);
        }

        [Fact]
        [Trait("Covers", "LinearConvolution")]
        public void ConvolutionMatchesHandComputedFullSameAndValid()
        {
            var x = new[] { 1.0, 2, 3 };
            var k = new[] { 0.0, 1, 0.5 };

            spectral.LinearConvolution(x, k).Should().Equal(0, 1, 2.5, 4, 1.5);
            spectral.LinearConvolution(x, k, ConvolutionMode.Same).Should().Equal(1, 2.5, 4);
            spectral.LinearConvolution(x, k, ConvolutionMode.Valid).Should().Equal(2.5);
        }

        [Fact]
        [Trait("Covers", "LinearConvolution")]
        public void ConvolutionWithMovingAverageKernelSmoothsAndValidatesKernelLength()
        {
            var smoothed = spectral.LinearConvolution([1.0, 1, 1, 1, 1], [1.0 / 3, 1.0 / 3, 1.0 / 3],
                ConvolutionMode.Valid);

            smoothed.Should().HaveCount(3);
            smoothed.Should().OnlyContain(v => Math.Abs(v - 1) < 1e-12);
            spectral.LinearConvolution([7.0], [2.0]).Should().Equal(14);

            Action longer = () =>
                spectral.LinearConvolution([1.0, 2], [1.0, 2, 3], ConvolutionMode.Same);

            longer.Should().Throw<ArgumentException>().WithParameterName("kernel");
            Action empty = () => spectral.LinearConvolution([], [1.0]);
            empty.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "TemplateMatchNormalisedCrossCorrelation")]
        public void TemplateMatchFindsExactAndRescaledCopies()
        {
            var template = new[] { 1.0, 2, 3, 2, 1 };
            var signal = new double[24];

            for (var i = 0; i < 5; i++)
            {
                signal[5 + i] = template[i];
                signal[14 + i] = 2 * template[i] + 3;
            }

            for (var i = 0; i < 24; i++)
            {
                if (i < 14 || i >= 19)
                {
                    signal[i] += i >= 19 ? 3 : 0;
                }
            }

            var result = spectral.TemplateMatchNormalisedCrossCorrelation(signal, template, 2);

            result.BestOffsets.Should().BeEquivalentTo([5, 14]);
            result.BestScores.Should().OnlyContain(s => s > 0.999999);
            result.Scores.Should().HaveCount(20);
            result.TemplateLength.Should().Be(5);
        }

        [Fact]
        [Trait("Covers", "TemplateMatchNormalisedCrossCorrelation")]
        public void TemplateMatchConstantWindowsAreNaNAndInputsValidated()
        {
            var result = spectral.TemplateMatchNormalisedCrossCorrelation(new double[10], [1.0, 2, 3]);

            result.Scores.Should().OnlyContain(s => double.IsNaN(s));
            result.BestOffsets.Should().BeEmpty();

            Action constant = () =>
                spectral.TemplateMatchNormalisedCrossCorrelation(new double[10], [2.0, 2, 2]);

            constant.Should().Throw<ArgumentException>().WithParameterName("template");

            Action longTemplate = () =>
                spectral.TemplateMatchNormalisedCrossCorrelation([1.0, 2, 3], [1.0, 2, 3, 4]);

            longTemplate.Should().Throw<ArgumentException>().WithParameterName("template");

            Action badScore = () =>
                spectral.TemplateMatchNormalisedCrossCorrelation([1.0, 2, 3], [1.0, 2, 3], 1, 2);

            badScore.Should().Throw<ArgumentException>().WithParameterName("minScore");
        }

        [Fact]
        [Trait("Covers", "HilbertEnvelopeAndPhase")]
        public void HilbertOfCosineGivesUnitEnvelopeSineQuadratureAndConstantFrequency()
        {
            foreach (var n in new[] { 16, 15 })
            {
                var x = Cosine(n, 2);

                var result = spectral.HilbertEnvelopeAndPhase(x);

                for (var i = 0; i < n; i++)
                {
                    result.QuadratureComponent[i].Should().BeApproximately(Math.Sin(2 * Math.PI * 2 * i / n), 1e-9);
                    result.Envelope[i].Should().BeApproximately(1.0, 1e-9);
                    result.InstantaneousFrequency[i].Should().BeApproximately(2.0 / n, 1e-9);
                }
            }
        }

        [Fact]
        [Trait("Covers", "HilbertEnvelopeAndPhase")]
        public void HilbertFrequencyScalesWithSamplingIntervalAndPhaseIsUnwrapped()
        {
            var result = spectral.HilbertEnvelopeAndPhase(Cosine(16, 2), 0.5);

            result.InstantaneousFrequency[5].Should().BeApproximately(0.25, 1e-9);
            result.Phase[15].Should().BeGreaterThan(2 * Math.PI);
            Action bad = () => spectral.HilbertEnvelopeAndPhase([1.0, 2, 3]);
            bad.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "DiscreteCosineTransformSeries")]
        public void DctMatchesClosedFormOrthonormalCoefficients()
        {
            var constant = spectral.DiscreteCosineTransformSeries([1.0, 1, 1, 1]);
            constant[0].Should().BeApproximately(2.0, 1e-12);
            constant.Skip(1).Should().OnlyContain(v => Math.Abs(v) < 1e-12);

            var result = spectral.DiscreteCosineTransformSeries([1.0, 2, 3, 4]);

            for (var k = 0; k < 4; k++)
            {
                var expected = (k == 0 ? Math.Sqrt(1.0 / 4) : Math.Sqrt(2.0 / 4)) *
                               Enumerable.Range(0, 4).Sum(n => (n + 1) * Math.Cos(Math.PI * (2 * n + 1) * k / 8.0));

                result[k].Should().BeApproximately(expected, 1e-12);
            }
        }

        [Fact]
        [Trait("Covers", "DiscreteCosineTransformSeries")]
        public void DctInverseRoundTripsAndPreservesEnergy()
        {
            var x = new[] { 3.0, -1, 4, 1, -5, 9, 2, 6 };

            var coefficients = spectral.DiscreteCosineTransformSeries(x);
            var back = spectral.DiscreteCosineTransformSeries(coefficients, true);

            for (var i = 0; i < x.Length; i++)
            {
                back[i].Should().BeApproximately(x[i], 1e-12);
            }

            coefficients.Sum(c => c * c).Should().BeApproximately(x.Sum(v => v * v), 1e-9);
            Action bad = () => spectral.DiscreteCosineTransformSeries([1.0]);
            bad.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "HaarWaveletDecomposition")]
        public void HaarDecompositionOfFourValuesMatchesHandComputation()
        {
            var one = spectral.HaarWaveletDecomposition([4.0, 2, 5, 5], 1);
            one.Approximation.Should().Equal(3, 5);
            one.Details[0].Should().Equal(1, 0);

            var two = spectral.HaarWaveletDecomposition([4.0, 2, 5, 5], 2);
            two.Approximation.Should().Equal(4);
            two.Details[0].Should().Equal(1, 0);
            two.Details[1].Should().Equal(-1);
            two.DetailEnergyShares[0].Should().BeApproximately(2.0 / 6, 1e-12);
            two.DetailEnergyShares[1].Should().BeApproximately(4.0 / 6, 1e-12);
        }

        [Fact]
        [Trait("Covers", "HaarWaveletDecomposition")]
        public void HaarDecompositionRejectsIndivisibleLengthAndConstantHasNoDetail()
        {
            Action bad = () => spectral.HaarWaveletDecomposition(new double[6], 2);
            bad.Should().Throw<ArgumentException>().WithParameterName("values");
            Action levels = () => spectral.HaarWaveletDecomposition(new double[8], 0);
            levels.Should().Throw<ArgumentException>().WithParameterName("levels");
            var flat = spectral.HaarWaveletDecomposition([.. Enumerable.Repeat(3.0, 8)]);
            flat.Approximation.Should().Equal(3);
            flat.Details.SelectMany(d => d).Should().OnlyContain(d => d == 0);
        }

        [Fact]
        [Trait("Covers", "HaarWaveletDenoise")]
        public void HaarDenoiseWithHugeThresholdKeepsOnlyApproximations()
        {
            spectral.HaarWaveletDenoise([4.0, 2, 5, 5], 1, 100).Denoised.Should().Equal(3, 3, 5, 5);
            var two = spectral.HaarWaveletDenoise([4.0, 2, 5, 5], 2, 100);
            two.Denoised.Should().Equal(4, 4, 4, 4);
            two.ZeroedCoefficients.Should().Be(3);
            two.DetailCoefficients.Should().Be(3);
        }

        [Fact]
        [Trait("Covers", "HaarWaveletDenoise")]
        public void HaarDenoiseZeroThresholdIsLosslessAndUniversalThresholdCleansNoisyStep()
        {
            var x = new[] { 3.0, -1, 4, 1, -5, 9, 2, 6 };
            var lossless = spectral.HaarWaveletDenoise(x, 3, 0);

            for (var i = 0; i < x.Length; i++)
            {
                lossless.Denoised[i].Should().BeApproximately(x[i], 1e-12);
            }

            var clean = Enumerable.Range(0, 64).Select(i => i < 32 ? 0.0 : 10.0).ToArray();
            var noisy = clean.Select((v, i) => v + (i % 2 == 0 ? 0.5 : -0.5)).ToArray();
            var result = spectral.HaarWaveletDenoise(noisy);
            var noisyError = noisy.Select((v, i) => Math.Abs(v - clean[i])).Sum();
            var cleanedError = result.Denoised.Select((v, i) => Math.Abs(v - clean[i])).Sum();
            cleanedError.Should().BeLessThan(noisyError * 0.1);
            result.NoiseStandardDeviation.Should().BeGreaterThan(0);
            Action badThreshold = () => spectral.HaarWaveletDenoise(x, 3, -1);
            badThreshold.Should().Throw<ArgumentException>().WithParameterName("threshold");
        }

        [Fact]
        [Trait("Covers", "FrequencyDomainFilterSeries")]
        public void FrequencyFiltersIsolateTheCorrectCosine()
        {
            var slow = Cosine(32, 2);
            var fast = Cosine(32, 8, 0.5);
            var mixed = slow.Zip(fast, (a, b) => a + b).ToArray();

            var low = spectral.FrequencyDomainFilterSeries(mixed, FrequencyFilterKind.LowPass, 0.1);
            var high = spectral.FrequencyDomainFilterSeries(mixed, FrequencyFilterKind.HighPass, 0.1);
            var band = spectral.FrequencyDomainFilterSeries(mixed, FrequencyFilterKind.BandPass, 0.2, 0.3);
            var stop = spectral.FrequencyDomainFilterSeries(mixed, FrequencyFilterKind.BandStop, 0.2, 0.3);

            for (var i = 0; i < 32; i++)
            {
                low.Filtered[i].Should().BeApproximately(slow[i], 1e-9);
                high.Filtered[i].Should().BeApproximately(fast[i], 1e-9);
                band.Filtered[i].Should().BeApproximately(fast[i], 1e-9);
                stop.Filtered[i].Should().BeApproximately(slow[i], 1e-9);
                low.Removed[i].Should().BeApproximately(fast[i], 1e-9);
            }

            low.KeptFrequencyBins.Should().Be(7);
            low.TotalFrequencyBins.Should().Be(32);
        }

        [Fact]
        [Trait("Covers", "FrequencyDomainFilterSeries")]
        public void FrequencyFilterHighPassRemovesMeanAndValidatesCutoffs()
        {
            var shifted = Cosine(16, 2).Select(v => v + 5).ToArray();

            var result = spectral.FrequencyDomainFilterSeries(shifted, FrequencyFilterKind.HighPass, 0.01);

            result.Filtered.Average().Should().BeApproximately(0, 1e-9);
            Action noHigh = () => spectral.FrequencyDomainFilterSeries(shifted, FrequencyFilterKind.BandPass, 0.1);
            noHigh.Should().Throw<ArgumentException>().WithParameterName("highCutoff");
            Action negative = () => spectral.FrequencyDomainFilterSeries(shifted, FrequencyFilterKind.LowPass, -1);
            negative.Should().Throw<ArgumentException>().WithParameterName("lowCutoff");
        }

        [Fact]
        [Trait("Covers", "ZeroCrossingAnalysis")]
        public void ZeroCrossingsOfAlternatingSeriesAreAtHalfSamples()
        {
            var result = spectral.ZeroCrossingAnalysis([1.0, -1, 1, -1]);

            result.Count.Should().Be(3);
            result.Positions.Should().Equal(0.5, 1.5, 2.5);
            result.RatePerSample.Should().BeApproximately(1.0, 1e-12);
            result.MeanCyclePeriodInSamples.Should().BeApproximately(2.0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "ZeroCrossingAnalysis")]
        public void ZeroCrossingsAtAnotherLevelAndEdgeCases()
        {
            var level = spectral.ZeroCrossingAnalysis([0.0, 2, 4, 2, 0], 1);
            level.Positions.Should().Equal(0.5, 3.5);
            level.MeanCyclePeriodInSamples.Should().BeApproximately(6, 1e-12);

            spectral.ZeroCrossingAnalysis([1.0, 2, 3]).Count.Should().Be(0);
            spectral.ZeroCrossingAnalysis([-1.0, 2, 3]).MeanCyclePeriodInSamples.Should().Be(double.NaN);
            Action bad = () => spectral.ZeroCrossingAnalysis([1.0, 2], double.NaN);
            bad.Should().Throw<ArgumentException>().WithParameterName("level");
        }

        [Fact]
        [Trait("Covers", "PeakDetectionInSeries")]
        public void PeakDetectionMatchesHandComputedPeaksAndProminences()
        {
            var x = new[] { 0.0, 2, 0, 3, 0, 1, 0 };

            var result = spectral.PeakDetectionInSeries(x);

            result.Indices.Should().Equal(1, 3, 5);
            result.Values.Should().Equal(2, 3, 1);
            result.Prominences.Should().Equal(2, 3, 1);
            spectral.PeakDetectionInSeries(x, double.NaN, 1.5).Indices.Should().Equal(1, 3);
            spectral.PeakDetectionInSeries(x, 2).Indices.Should().Equal(1, 3);
            spectral.PeakDetectionInSeries(x, double.NaN, 0, 3).Indices.Should().Equal(3);
        }

        [Fact]
        [Trait("Covers", "PeakDetectionInSeries")]
        public void PeakDetectionTroughsPlateausAndNoPeaks()
        {
            var troughs = spectral.PeakDetectionInSeries([3.0, 1, 3, 0, 3], double.NaN, 0, 1, true);
            troughs.Indices.Should().Equal(1, 3);
            troughs.Values.Should().Equal(1, 0);
            troughs.Prominences.Should().Equal(2, 3);

            spectral.PeakDetectionInSeries([0.0, 1, 1, 0]).Indices.Should().Equal(1);
            spectral.PeakDetectionInSeries([1.0, 2, 3, 4]).Indices.Should().BeEmpty();
            Action bad = () => spectral.PeakDetectionInSeries([1.0, 2, 1], double.NaN, -1);
            bad.Should().Throw<ArgumentException>().WithParameterName("minProminence");
        }

        private const double NewYear2021 = 1609459200.0;
        private readonly TransactionTimingTools timing = new();

        [Fact]
        [Trait("Covers", "ParseTimestampsToEpochSeconds")]
        public void ParseIsoTimestampsMatchesKnownUnixTimes()
        {
            var result = timing.ParseTimestampsToEpochSeconds([
                "1970-01-01T00:00:00Z", "2021-01-01T00:00:00Z", "2021-01-01T01:00:00+01:00",
                "2021-01-01T00:00:00", "2021-01-01T00:00:00.5Z"
            ]);

            result[0].Should().Be(0);
            result[1].Should().Be(NewYear2021);
            result[2].Should().Be(NewYear2021);
            result[3].Should().Be(NewYear2021);
            result[4].Should().BeApproximately(NewYear2021 + 0.5, 1e-6);
        }

        [Fact]
        [Trait("Covers", "ParseTimestampsToEpochSeconds")]
        public void ParseIsoTimestampsNamesTheBadPosition()
        {
            Action bad = () => timing.ParseTimestampsToEpochSeconds(["2021-01-01T00:00:00Z", "not a date"]);
            bad.Should().Throw<ArgumentException>().WithParameterName("isoTimestamps").WithMessage("*position 1*");
            Action empty = () => timing.ParseTimestampsToEpochSeconds([]);
            empty.Should().Throw<ArgumentException>().WithParameterName("isoTimestamps");
        }

        [Fact]
        [Trait("Covers", "FormatEpochSecondsAsIso")]
        public void FormatEpochSecondsProducesIsoUtcAndRoundTrips()
        {
            var result = timing.FormatEpochSecondsAsIso([0.0, NewYear2021, NewYear2021 + 0.5]);

            result.Should().Equal("1970-01-01T00:00:00Z", "2021-01-01T00:00:00Z", "2021-01-01T00:00:00.5Z");
            timing.ParseTimestampsToEpochSeconds(result).Should().Equal(0.0, NewYear2021, NewYear2021 + 0.5);
        }

        [Fact]
        [Trait("Covers", "FormatEpochSecondsAsIso")]
        public void FormatEpochSecondsRejectsMillisecondsAndNaN()
        {
            Action millis = () => timing.FormatEpochSecondsAsIso([1.6e12]);
            millis.Should().Throw<ArgumentException>().WithParameterName("epochSeconds");
            Action nan = () => timing.FormatEpochSecondsAsIso([double.NaN]);
            nan.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "TimestampCalendarFeatures")]
        public void CalendarFeaturesOfNewYear2021AreAFridayAndHonourOffsets()
        {
            var utc = timing.TimestampCalendarFeatures([NewYear2021]);
            utc.Hour.Should().Equal(0);
            utc.DayOfWeekIso.Should().Equal(5);
            utc.DayOfMonth.Should().Equal(1);
            utc.Month.Should().Equal(1);
            utc.Year.Should().Equal(2021);
            utc.DayOfYear.Should().Equal(1);
            utc.IsWeekend.Should().Equal(false);

            timing.TimestampCalendarFeatures([NewYear2021], 120).Hour.Should().Equal(2);
            var west = timing.TimestampCalendarFeatures([NewYear2021], -60);
            west.Year.Should().Equal(2020);
            west.Month.Should().Equal(12);
            west.DayOfMonth.Should().Equal(31);
            west.DayOfYear.Should().Equal(366);
            west.DayOfWeekIso.Should().Equal(4);
            west.SecondOfDay[0].Should().Be(23 * 3600);
        }

        [Fact]
        [Trait("Covers", "TimestampCalendarFeatures")]
        public void CalendarFeaturesFlagWeekendAndRejectBadOffset()
        {
            var weekend = timing.TimestampCalendarFeatures([NewYear2021 + 86400, NewYear2021 + 2 * 86400]);

            weekend.IsWeekend.Should().Equal(true, true);
            weekend.DayOfWeekIso.Should().Equal(6, 7);
            Action bad = () => timing.TimestampCalendarFeatures([NewYear2021], 5000);
            bad.Should().Throw<ArgumentException>().WithParameterName("utcOffsetMinutes");
        }

        [Fact]
        [Trait("Covers", "InterArrivalTimeStatistics")]
        public void InterArrivalStatisticsMatchHandComputationAndIgnoreInputOrder()
        {
            var result = timing.InterArrivalTimeStatistics([30.0, 0, 60, 10]);

            result.Gaps.Should().Equal(10, 20, 30);
            result.MeanGapSeconds.Should().Be(20);
            result.MedianGapSeconds.Should().Be(20);
            result.StandardDeviationSeconds.Should().BeApproximately(10, 1e-12);
            result.CoefficientOfVariation.Should().BeApproximately(0.5, 1e-12);
            result.Burstiness.Should().BeApproximately(-1.0 / 3.0, 1e-12);
            result.EventsPerHour.Should().BeApproximately(180, 1e-9);
            result.MinimumGapSeconds.Should().Be(10);
            result.MaximumGapSeconds.Should().Be(30);
        }

        [Fact]
        [Trait("Covers", "InterArrivalTimeStatistics")]
        public void InterArrivalStatisticsTwoEventsAndDuplicates()
        {
            var two = timing.InterArrivalTimeStatistics([0.0, 60]);
            two.StandardDeviationSeconds.Should().Be(double.NaN);
            two.MeanGapSeconds.Should().Be(60);

            var same = timing.InterArrivalTimeStatistics([5.0, 5, 5]);
            same.Gaps.Should().Equal(0, 0);
            same.EventsPerHour.Should().Be(double.NaN);
            Action one = () => timing.InterArrivalTimeStatistics([5.0]);
            one.Should().Throw<ArgumentException>().WithParameterName("epochSeconds");
        }

        [Fact]
        [Trait("Covers", "DetectTimeGaps")]
        public void DetectTimeGapsFindsTheLongPauseByMedianMultipleAndAbsoluteThreshold()
        {
            var t = new[] { 0.0, 1, 2, 3, 100, 101 };

            var byMultiple = timing.DetectTimeGaps(t);
            byMultiple.Gaps.Should().HaveCount(1);
            byMultiple.ThresholdSeconds.Should().Be(5);
            byMultiple.Gaps[0].StartIndex.Should().Be(3);
            byMultiple.Gaps[0].EndIndex.Should().Be(4);
            byMultiple.Gaps[0].DurationSeconds.Should().Be(97);
            timing.DetectTimeGaps(t, 50).Gaps.Should().HaveCount(1);
            timing.DetectTimeGaps(t, 200).Gaps.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "DetectTimeGaps")]
        public void DetectTimeGapsRejectsUnsortedInput()
        {
            Action unsorted = () => timing.DetectTimeGaps([5.0, 1]);
            unsorted.Should().Throw<ArgumentException>().WithParameterName("epochSeconds");
            Action badThreshold = () => timing.DetectTimeGaps([1.0, 2], -3);
            badThreshold.Should().Throw<ArgumentException>().WithParameterName("minGapSeconds");
        }

        [Fact]
        [Trait("Covers", "ResampleEventsToBuckets")]
        public void ResampleEventsToBucketsCountsSumsAndKeepsEmptyBuckets()
        {
            var result = timing.ResampleEventsToBuckets([0.0, 1, 2, 10, 11], [1.0, 2, 3, 4, 5], 5);

            result.BucketStartEpochSeconds.Should().Equal(0, 5, 10);
            result.Counts.Should().Equal(3, 0, 2);
            result.Sums.Should().Equal(6, 0, 9);
            result.Means.Should().Equal(2, double.NaN, 4.5);
            result.Minimums.Should().Equal(1, double.NaN, 4);
            result.Maximums.Should().Equal(3, double.NaN, 5);
            result.EmptyBuckets.Should().Be(1);

            timing.ResampleEventsToBuckets([0.0, 1, 2, 10, 11], [1.0, 2, 3, 4, 5], 5, 0, false)
                .Counts.Should().Equal(3, 2);
        }

        [Fact]
        [Trait("Covers", "ResampleEventsToBuckets")]
        public void ResampleEventsToBucketsHandlesNegativeTimesUnsortedInputAndLimits()
        {
            var negative = timing.ResampleEventsToBuckets([0.0, -1], null, 5);
            negative.BucketStartEpochSeconds.Should().Equal(-5, 0);
            negative.Counts.Should().Equal(1, 1);
            negative.Sums.Should().OnlyContain(v => double.IsNaN(v));

            var origin = timing.ResampleEventsToBuckets([7.0], null, 5, 2);
            origin.BucketStartEpochSeconds.Should().Equal(7);
            Action tooMany = () => timing.ResampleEventsToBuckets([0.0, 1e9], null, 1);
            tooMany.Should().Throw<ArgumentException>().WithParameterName("bucketSeconds");
            Action mismatch = () => timing.ResampleEventsToBuckets([0.0, 1], [1.0], 5);
            mismatch.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "TrailingWindowVelocity")]
        public void TrailingWindowVelocityUsesHalfOpenWindowsByHand()
        {
            var t = new[] { 0.0, 5, 10, 20 };
            var v = new[] { 1.0, 2, 3, 4 };

            var result = timing.TrailingWindowVelocity(t, 10, v);

            result.Counts.Should().Equal(1, 2, 2, 1);
            result.Sums.Should().Equal(1, 3, 5, 4);
            result.RatesPerHour.Should().Equal(360, 720, 720, 360);
            result.SecondsSincePreviousEvent.Should().Equal(double.NaN, 5, 5, 10);
            var excluded = timing.TrailingWindowVelocity(t, 10, v, false);
            excluded.Counts.Should().Equal(0, 1, 1, 0);
            excluded.Sums.Should().Equal(0, 1, 2, 0);
        }

        [Fact]
        [Trait("Covers", "TrailingWindowVelocity")]
        public void TrailingWindowVelocityDuplicateTimestampsAndValidation()
        {
            timing.TrailingWindowVelocity([5.0, 5, 5], 10).Counts.Should().Equal(1, 2, 3);
            Action unsorted = () => timing.TrailingWindowVelocity([5.0, 1], 10);
            unsorted.Should().Throw<ArgumentException>().WithParameterName("epochSeconds");
            Action badWindow = () => timing.TrailingWindowVelocity([1.0], 0);
            badWindow.Should().Throw<ArgumentException>().WithParameterName("windowSeconds");
            Action mismatch = () => timing.TrailingWindowVelocity([1.0, 2], 5, [1.0]);
            mismatch.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "MultiWindowVelocityFeatures")]
        public void MultiWindowVelocityCountsEachWindowAtTheSnapshotTime()
        {
            var t = new[] { 0.0, 50, 90, 100 };
            var v = new[] { 1.0, 2, 3, 4 };

            var last = timing.MultiWindowVelocityFeatures(t, [10.0, 60, 1000], v);
            last.Counts.Should().Equal(1, 3, 4);
            last.Sums.Should().Equal(4, 9, 10);
            last.SecondsSinceLastEvent.Should().Be(0);
            last.AsOfEpochSeconds.Should().Be(100);

            var earlier = timing.MultiWindowVelocityFeatures(t, [10.0], v, 95);
            earlier.Counts.Should().Equal(1);
            earlier.SecondsSinceLastEvent.Should().Be(5);
            earlier.TotalEventsUpToAsOf.Should().Be(3);
            earlier.RatesPerHour[0].Should().BeApproximately(360, 1e-9);
        }

        [Fact]
        [Trait("Covers", "MultiWindowVelocityFeatures")]
        public void MultiWindowVelocityBeforeAnyEventAndBadWindows()
        {
            var before = timing.MultiWindowVelocityFeatures([100.0, 200], [60.0], null, 50);
            before.Counts.Should().Equal(0);
            before.SecondsSinceLastEvent.Should().Be(double.NaN);
            Action bad = () => timing.MultiWindowVelocityFeatures([1.0], [0.0]);
            bad.Should().Throw<ArgumentException>().WithParameterName("windowsSeconds");
        }

        [Fact]
        [Trait("Covers", "WindowRateChangeTest")]
        public void RateChangeMatchesClosedFormPoissonTail()
        {
            var events = Enumerable.Range(0, 10).Select(i => 100.0 + 10 * i)
                .Concat([195.0, 196, 197, 198, 200]).ToArray();

            var result = timing.WindowRateChangeTest(events, 10, 100);

            result.RecentCount.Should().Be(5);
            result.BaselineCount.Should().Be(10);
            result.ExpectedRecentCount.Should().BeApproximately(1.0, 1e-12);
            result.RateRatio.Should().BeApproximately(5.0, 1e-12);
            var expected = 1 - Math.Exp(-1) * (1 + 1 + 0.5 + 1.0 / 6 + 1.0 / 24);
            result.PValueIncrease.Should().BeApproximately(expected, 1e-9);
            result.Reading.Should().Contain("higher");
        }

        [Fact]
        [Trait("Covers", "WindowRateChangeTest")]
        public void RateChangeSilentBaselineAndDecrease()
        {
            var silent = timing.WindowRateChangeTest([195.0, 196, 197, 198, 199, 200], 10, 100);
            silent.BaselineCount.Should().Be(0);
            silent.RateRatio.Should().Be(double.PositiveInfinity);
            silent.PValueIncrease.Should().BeLessThan(0.01);

            var baseline = Enumerable.Range(0, 60).Select(i => 100.0 + 1.5 * i).ToArray();
            var drop = timing.WindowRateChangeTest([.. baseline, 300.0], 100, 100, 300);
            drop.PValueDecrease.Should().BeLessThan(0.01);
            Action bad = () => timing.WindowRateChangeTest([1.0], 0, 5);
            bad.Should().Throw<ArgumentException>().WithParameterName("recentSeconds");
        }

        [Fact]
        [Trait("Covers", "EventBurstDetection")]
        public void BurstDetectionFindsTwoBurstsByHand()
        {
            var t = new[] { 0.0, 1, 2, 100, 200, 201, 202, 203 };

            var result = timing.EventBurstDetection(t, 5, 3);

            result.Bursts.Should().HaveCount(2);
            result.Bursts[0].StartIndex.Should().Be(0);
            result.Bursts[0].EndIndex.Should().Be(2);
            result.Bursts[0].EventCount.Should().Be(3);
            result.Bursts[1].StartIndex.Should().Be(4);
            result.Bursts[1].EndIndex.Should().Be(7);
            result.Bursts[1].MaximumWindowCount.Should().Be(4);
            result.Bursts[1].PValue.Should().BeLessThan(0.001);
            result.BaselineRatePerSecond.Should().BeApproximately(7.0 / 203, 1e-12);
        }

        [Fact]
        [Trait("Covers", "EventBurstDetection")]
        public void BurstDetectionFindsNothingInRegularStreamAndValidates()
        {
            timing.EventBurstDetection([.. Enumerable.Range(0, 20).Select(i => i * 60.0)], 10, 2).Bursts.Should()
                .BeEmpty();

            timing.EventBurstDetection([5.0], 10, 2).Bursts.Should().BeEmpty();
            Action bad = () => timing.EventBurstDetection([1.0, 2], 10, 1);
            bad.Should().Throw<ArgumentException>().WithParameterName("minEvents");
        }

        [Fact]
        [Trait("Covers", "SessionizeEvents")]
        public void SessionizeSplitsOnGapsLongerThanTimeout()
        {
            var result = timing.SessionizeEvents([0.0, 10, 20, 200, 210, 1000], 100);

            result.SessionIds.Should().Equal(0, 0, 0, 1, 1, 2);
            result.Sessions.Select(s => s.EventCount).Should().Equal(3, 2, 1);
            result.Sessions.Select(s => s.DurationSeconds).Should().Equal(20, 10, 0);
            result.Sessions[1].StartIndex.Should().Be(3);
        }

        [Fact]
        [Trait("Covers", "SessionizeEvents")]
        public void SessionizeGapEqualToTimeoutStaysInSession()
        {
            timing.SessionizeEvents([0.0, 100], 100).Sessions.Should().HaveCount(1);
            timing.SessionizeEvents([7.0], 100).SessionIds.Should().Equal(0);
            Action bad = () => timing.SessionizeEvents([1.0], 0);
            bad.Should().Throw<ArgumentException>().WithParameterName("timeoutSeconds");
        }

        [Fact]
        [Trait("Covers", "CalendarCycleProfile")]
        public void HourProfileOfUniformDayHasMaximumEntropyAndZeroChiSquare()
        {
            var t = Enumerable.Range(0, 24).Select(h => NewYear2021 + h * 3600.0).ToArray();

            var result = timing.CalendarCycleProfile(t);

            result.Counts.Should().OnlyContain(c => c == 1);
            result.EntropyBits.Should().BeApproximately(Math.Log(24, 2), 1e-9);
            result.NormalisedEntropy.Should().BeApproximately(1.0, 1e-9);
            result.ChiSquareStatistic.Should().BeApproximately(0, 1e-9);
            result.PValue.Should().BeApproximately(1.0, 1e-6);
            result.DegreesOfFreedom.Should().Be(23);
        }

        [Fact]
        [Trait("Covers", "CalendarCycleProfile")]
        public void CalendarProfileConcentratedActivityAndOtherCycles()
        {
            var night = Enumerable.Range(0, 30).Select(d => NewYear2021 + d * 86400.0 + 3 * 3600).ToArray();
            var hours = timing.CalendarCycleProfile(night);
            hours.PeakLabel.Should().Be("03");
            hours.NormalisedEntropy.Should().BeApproximately(0, 1e-12);
            hours.PValue.Should().BeLessThan(1e-6);
            timing.CalendarCycleProfile(night, CalendarCycle.HourOfDay, 120).PeakLabel.Should().Be("05");

            timing.CalendarCycleProfile([NewYear2021], CalendarCycle.DayOfWeek).PeakLabel.Should()
                .Be("Friday");
            timing.CalendarCycleProfile([NewYear2021], CalendarCycle.MonthOfYear).PeakLabel.Should()
                .Be("January");

            timing.CalendarCycleProfile([NewYear2021], CalendarCycle.DayOfMonth).Labels.Should().HaveCount(31);
            Action bad = () => timing.CalendarCycleProfile([NewYear2021], CalendarCycle.HourOfDay, 9999);
            bad.Should().Throw<ArgumentException>().WithParameterName("utcOffsetMinutes");
        }

        [Fact]
        [Trait("Covers", "EventPeriodicityScore")]
        public void PeriodicityOfClockworkEventsIsPerfect()
        {
            var result = timing.EventPeriodicityScore([0.0, 10, 20, 30, 40]);

            result.MedianIntervalSeconds.Should().Be(10);
            result.CoefficientOfVariation.Should().Be(0);
            result.ShareWithinTolerance.Should().Be(1);
            result.Reading.Should().Contain("Highly regular");
        }

        [Fact]
        [Trait("Covers", "EventPeriodicityScore")]
        public void PeriodicityOfIrregularEventsIsLowAndInputIsSorted()
        {
            var result = timing.EventPeriodicityScore([40.0, 0, 1, 3, 10, 11]);

            result.ShareWithinTolerance.Should().BeLessThan(0.5);
            result.Reading.Should().Be("Irregular timing.");
            Action tooFew = () => timing.EventPeriodicityScore([1.0, 2]);
            tooFew.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CountDispersionIndexTest")]
        public void DispersionIndexMatchesClosedFormChiSquareTwoDegreesOfFreedom()
        {
            var result = timing.CountDispersionIndexTest([2.0, 4, 6]);

            result.Mean.Should().Be(4);
            result.Variance.Should().Be(4);
            result.DispersionIndex.Should().Be(1);
            result.ChiSquareStatistic.Should().Be(2);
            result.DegreesOfFreedom.Should().Be(2);
            result.PValueOverdispersed.Should().BeApproximately(Math.Exp(-1), 1e-9);
            result.PValueUnderdispersed.Should().BeApproximately(1 - Math.Exp(-1), 1e-9);
        }

        [Fact]
        [Trait("Covers", "CountDispersionIndexTest")]
        public void DispersionOfBurstyAndConstantCountsAndValidation()
        {
            timing.CountDispersionIndexTest([0.0, 0, 0, 0, 0, 0, 0, 0, 0, 60]).Reading.Should()
                .Contain("Overdispersed");

            timing.CountDispersionIndexTest([5.0, 5, 5, 5, 5, 5, 5, 5]).Reading.Should()
                .Contain("Underdispersed");

            Action zeros = () => timing.CountDispersionIndexTest([0.0, 0, 0]);
            zeros.Should().Throw<ArgumentException>().WithParameterName("counts");
            Action negative = () => timing.CountDispersionIndexTest([1.0, -1, 3]);
            negative.Should().Throw<ArgumentException>().WithParameterName("counts");
        }

        private static readonly double naN = double.NaN;
        private readonly SeriesTransformTools transform = new();

        [Fact]
        [Trait("Covers", "DifferenceSeries")]
        public void DifferencingSquaresGivesOddNumbersAndSecondDifferencesAreConstantTwo()
        {
            var squares = new[] { 1.0, 4, 9, 16, 25 };

            transform.DifferenceSeries(squares).Should().Equal(naN, 3, 5, 7, 9);
            transform.DifferenceSeries(squares, 2).Should().Equal(naN, naN, 2, 2, 2);
            transform.DifferenceSeries(squares, 1, 2).Should().Equal(naN, naN, 8, 12, 16);
        }

        [Fact]
        [Trait("Covers", "DifferenceSeries")]
        public void DifferencingPropagatesNaNAndRejectsBadArguments()
        {
            transform.DifferenceSeries([1.0, naN, 4, 7]).Should().Equal(naN, naN, naN, 3);
            transform.DifferenceSeries([5.0]).Should().Equal(naN);
            Action badOrder = () => transform.DifferenceSeries(oneToFive, 0);
            badOrder.Should().Throw<ArgumentException>().WithParameterName("order");
            Action empty = () => transform.DifferenceSeries([]);
            empty.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "LagSeries")]
        public void LagAndLeadShiftValuesAndPadWithNaN()
        {
            var x = new[] { 1.0, 2, 3, 4 };

            transform.LagSeries(x, 2).Should().Equal(naN, naN, 1, 2);
            transform.LagSeries(x, -1).Should().Equal(2, 3, 4, naN);
            transform.LagSeries(x, 0).Should().Equal(x);
            transform.LagSeries(x, 10).Should().OnlyContain(v => double.IsNaN(v));
        }

        [Fact]
        [Trait("Covers", "LaggedFeatureMatrix")]
        public void LaggedFeatureMatrixBuildsHandComputedRows()
        {
            var result = transform.LaggedFeatureMatrix([1.0, 2, 3, 4, 5, 6], [1, 2]);

            result.RowIndices.Should().Equal(2, 3, 4, 5);
            result.Targets.Should().Equal(3, 4, 5, 6);
            result.Features[0].Should().Equal(2, 1);
            result.Features[3].Should().Equal(5, 4);
        }

        [Fact]
        [Trait("Covers", "LaggedFeatureMatrix")]
        public void LaggedFeatureMatrixValidatesLags()
        {
            Action none = () => transform.LaggedFeatureMatrix(oneToFive, []);
            none.Should().Throw<ArgumentException>().WithParameterName("lags");
            Action zero = () => transform.LaggedFeatureMatrix(oneToFive, [0]);
            zero.Should().Throw<ArgumentException>().WithParameterName("lags");
            Action tooLong = () => transform.LaggedFeatureMatrix(oneToFive, [5]);
            tooLong.Should().Throw<ArgumentException>().WithParameterName("lags");
        }

        [Fact]
        [Trait("Covers", "CumulativeSeries")]
        public void CumulativeOperationsMatchHandComputation()
        {
            transform.CumulativeSeries([1.0, 2, 3, 4]).Should().Equal(1, 3, 6, 10);

            transform.CumulativeSeries([1.0, 2, 3, 4], CumulativeOperation.Product).Should()
                .Equal(1, 2, 6, 24);

            transform.CumulativeSeries([3.0, 1, 4, 1, 5], CumulativeOperation.Maximum).Should()
                .Equal(3, 3, 4, 4, 5);
            transform.CumulativeSeries([3.0, 1, 4, 1, 5], CumulativeOperation.Minimum).Should()
                .Equal(3, 1, 1, 1, 1);

            transform.CumulativeSeries([2.0, 4, 6], CumulativeOperation.Mean).Should().Equal(2, 3, 4);
        }

        [Fact]
        [Trait("Covers", "CumulativeSeries")]
        public void CumulativeSumPropagatesNaNWhileMaximumIgnoresIt()
        {
            transform.CumulativeSeries([1.0, naN, 3]).Should().Equal(1, naN, naN);

            transform.CumulativeSeries([naN, 2.0, naN, 1], CumulativeOperation.Maximum).Should()
                .Equal(naN, 2, 2, 2);

            transform.CumulativeSeries([1e15, 1.0, 1, 1, 1]).Last().Should().BeApproximately(1e15 + 4, 0.5);
        }

        [Fact]
        [Trait("Covers", "PercentChangeSeries")]
        public void PercentChangeMatchesHandComputationAndZeroBaselineIsNaN()
        {
            var result = transform.PercentChangeSeries([100.0, 110, 99]);

            result[0].Should().Be(naN);
            result[1].Should().BeApproximately(0.10, 1e-12);
            result[2].Should().BeApproximately(-0.10, 1e-12);
            transform.PercentChangeSeries([0.0, 5]).Should().Equal(naN, naN);
            transform.PercentChangeSeries([1.0, 2, 4, 8], 2)[2].Should().BeApproximately(3.0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "LogReturnSeries")]
        public void LogReturnOfExponentialGrowthIsConstantOne()
        {
            var e = Math.E;

            var result = transform.LogReturnSeries([1.0, e, e * e]);

            result[0].Should().Be(naN);
            result[1].Should().BeApproximately(1.0, 1e-12);
            result[2].Should().BeApproximately(1.0, 1e-12);
            transform.LogReturnSeries([1.0, -2, 3]).Should().Equal(naN, naN, naN);
        }

        [Fact]
        [Trait("Covers", "SimpleMovingAverageSeries")]
        public void SimpleMovingAverageTrailingCenteredAndPartial()
        {
            var x = new[] { 1.0, 2, 3, 4, 5 };

            transform.SimpleMovingAverageSeries(x, 3).Should().Equal(naN, naN, 2, 3, 4);
            transform.SimpleMovingAverageSeries(x, 3, WindowAlignment.Centered).Should().Equal(naN, 2, 3, 4, naN);
            transform.SimpleMovingAverageSeries(x, 3, WindowAlignment.Trailing, 1).Should().Equal(1, 1.5, 2, 3, 4);
            transform.SimpleMovingAverageSeries(x, 2, WindowAlignment.Leading).Should().Equal(1.5, 2.5, 3.5, 4.5, naN);
        }

        [Fact]
        [Trait("Covers", "SimpleMovingAverageSeries")]
        public void SimpleMovingAverageSkipsMissingAndRejectsBadWindow()
        {
            transform.SimpleMovingAverageSeries([1.0, naN, 3, 5], 3, WindowAlignment.Trailing, 2)
                .Should().Equal(naN, naN, 2, 4);

            Action bad = () => transform.SimpleMovingAverageSeries(oneToFive, 0);
            bad.Should().Throw<ArgumentException>().WithParameterName("window");
        }

        [Fact]
        [Trait("Covers", "WeightedMovingAverageSeries")]
        public void WeightedMovingAverageUsesOldestToNewestWeights()
        {
            var result = transform.WeightedMovingAverageSeries([1.0, 2, 3, 4], [1.0, 2, 3]);

            result[0].Should().Be(naN);
            result[1].Should().Be(naN);
            result[2].Should().BeApproximately(14.0 / 6.0, 1e-12);
            result[3].Should().BeApproximately(20.0 / 6.0, 1e-12);
            Action zero = () => transform.WeightedMovingAverageSeries(oneToFive, [1.0, -1]);
            zero.Should().Throw<ArgumentException>().WithParameterName("weights");
        }

        [Fact]
        [Trait("Covers", "ExponentiallyWeightedSmoothingSeries")]
        public void ExponentialMovingAverageRecursiveAndAdjustedFormsMatchHandComputation()
        {
            var x = new[] { 1.0, 2, 3 };

            transform.ExponentiallyWeightedSmoothingSeries(x, 0.5).Should().Equal(1, 1.5, 2.25);
            var adjusted = transform.ExponentiallyWeightedSmoothingSeries(x, 0.5, naN, true);
            adjusted[1].Should().BeApproximately(5.0 / 3.0, 1e-12);
            adjusted[2].Should().BeApproximately(4.25 / 1.75, 1e-12);
            transform.ExponentiallyWeightedSmoothingSeries(x, naN, 3).Should().Equal(1, 1.5, 2.25);
        }

        [Fact]
        [Trait("Covers", "ExponentiallyWeightedSmoothingSeries")]
        public void ExponentialMovingAverageRejectsAmbiguousParameters()
        {
            Action both = () => transform.ExponentiallyWeightedSmoothingSeries(oneToFive, 0.5, 3);
            both.Should().Throw<ArgumentException>();
            Action neither = () => transform.ExponentiallyWeightedSmoothingSeries(oneToFive);
            neither.Should().Throw<ArgumentException>();
            Action badAlpha = () => transform.ExponentiallyWeightedSmoothingSeries(oneToFive, 1.5);
            badAlpha.Should().Throw<ArgumentException>().WithParameterName("alpha");
            transform.ExponentiallyWeightedSmoothingSeries([7.0, 7, 7], 1.0).Should().Equal(7, 7, 7);
        }

        [Fact]
        [Trait("Covers", "ExponentialMovingStatisticsSeries")]
        public void ExponentialMovingStatisticsMatchHandComputedRecursion()
        {
            var result = transform.ExponentialMovingStatisticsSeries([1.0, 2, 3], 0.5);

            result.Means.Should().Equal(1, 1.5, 2.25);
            result.StandardDeviations[0].Should().Be(0);
            result.StandardDeviations[1].Should().BeApproximately(0.5, 1e-12);
            result.StandardDeviations[2].Should().BeApproximately(Math.Sqrt(0.6875), 1e-12);
            result.Alpha.Should().Be(0.5);
        }

        [Fact]
        [Trait("Covers", "ExponentialMovingStatisticsSeries")]
        public void ExponentialMovingStatisticsOfConstantHasZeroSpread()
        {
            var result = transform.ExponentialMovingStatisticsSeries([4.0, 4, 4, 4], naN, 5);

            result.StandardDeviations.Should().OnlyContain(v => v == 0);
            result.Alpha.Should().BeApproximately(1.0 / 3.0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "RollingStatisticSeries")]
        public void RollingStatisticsOverWindowOfThreeMatchHandComputation()
        {
            var x = new[] { 1.0, 3, 2, 5, 4 };

            transform.RollingStatisticSeries(x, 3)[2].Should().BeApproximately(2.0, 1e-12);
            transform.RollingStatisticSeries(x, 3, WindowStatistic.Sum)[2].Should().Be(6);
            transform.RollingStatisticSeries(x, 3, WindowStatistic.Minimum)[3].Should().Be(2);
            transform.RollingStatisticSeries(x, 3, WindowStatistic.Maximum)[3].Should().Be(5);
            transform.RollingStatisticSeries(x, 3, WindowStatistic.Median)[3].Should().Be(3);

            transform.RollingStatisticSeries(x, 3, WindowStatistic.StandardDeviation)[2].Should()
                .BeApproximately(1.0, 1e-12);
            transform.RollingStatisticSeries(x, 3, WindowStatistic.Variance)[2].Should().BeApproximately(1.0, 1e-12);
            transform.RollingStatisticSeries(x, 3, WindowStatistic.Range)[3].Should().Be(3);
            transform.RollingStatisticSeries(x, 3, WindowStatistic.MedianAbsoluteDeviation)[2].Should().Be(1);

            transform.RollingStatisticSeries(x, 3, WindowStatistic.Quantile)[2]
                .Should().BeApproximately(2.0, 1e-9);
            transform.RollingStatisticSeries(x, 3, WindowStatistic.CoefficientOfVariation)[2].Should()
                .BeApproximately(0.5, 1e-12);
            transform.RollingStatisticSeries(x, 3, WindowStatistic.Skewness)[2].Should().BeApproximately(0.0, 1e-9);
            transform.RollingStatisticSeries(x, 3).Take(2).Should().OnlyContain(v => double.IsNaN(v));
        }

        [Fact]
        [Trait("Covers", "RollingStatisticSeries")]
        public void RollingStatisticsIgnoreMissingAndCountFiniteValues()
        {
            var x = new[] { 1.0, naN, 3 };

            transform.RollingStatisticSeries(x, 3, WindowStatistic.Mean, WindowAlignment.Trailing, 2)[2].Should()
                .Be(2);

            transform.RollingStatisticSeries(x, 3)[2].Should().Be(naN);
            transform.RollingStatisticSeries(x, 3, WindowStatistic.Count)[2].Should().Be(2);

            transform.RollingStatisticSeries([5.0, 6], 2, WindowStatistic.StandardDeviation,
                WindowAlignment.Trailing, 1)[0].Should().Be(naN);

            Action badQuantile = () =>
                transform.RollingStatisticSeries(x, 2, WindowStatistic.Quantile, WindowAlignment.Trailing, 0, 1.5);

            badQuantile.Should().Throw<ArgumentException>().WithParameterName("quantile");
        }

        [Fact]
        [Trait("Covers", "ExpandingStatisticSeries")]
        public void ExpandingStatisticsMatchHandComputation()
        {
            var x = new[] { 1.0, 2, 3, 4 };

            transform.ExpandingStatisticSeries(x).Should().Equal(1, 1.5, 2, 2.5);
            transform.ExpandingStatisticSeries(x, WindowStatistic.Maximum).Should().Equal(1, 2, 3, 4);
            transform.ExpandingStatisticSeries(x, WindowStatistic.Median).Should().Equal(1, 1.5, 2, 2.5);
            var std = transform.ExpandingStatisticSeries(x, WindowStatistic.StandardDeviation);
            std[0].Should().Be(naN);
            std[1].Should().BeApproximately(Math.Sqrt(0.5), 1e-12);
            std[3].Should().BeApproximately(Math.Sqrt(5.0 / 3.0), 1e-12);
            transform.ExpandingStatisticSeries(x, WindowStatistic.Sum).Should().Equal(1, 3, 6, 10);
        }

        [Fact]
        [Trait("Covers", "ExpandingStatisticSeries")]
        public void ExpandingStatisticsHandleMissingAndMinPeriods()
        {
            var x = new[] { naN, 2.0, naN, 4 };

            transform.ExpandingStatisticSeries(x).Should().Equal(naN, 2, 2, 3);
            transform.ExpandingStatisticSeries(x, WindowStatistic.Count).Should().Equal(0, 1, 1, 2);
            transform.ExpandingStatisticSeries(x, WindowStatistic.Mean, 2).Should().Equal(naN, naN, naN, 3);
            Action tooLong = () => transform.ExpandingStatisticSeries(new double[20001], WindowStatistic.Median);
            tooLong.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "RollingCorrelationSeries")]
        public void RollingCorrelationOfProportionalAndOppositeSeriesIsPlusAndMinusOne()
        {
            var x = new[] { 1.0, 2, 3, 4, 5, 6 };
            var doubled = x.Select(v => 2 * v).ToArray();
            var reversed = x.Select(v => -v).ToArray();

            var positive = transform.RollingCorrelationSeries(x, doubled, 3);
            var negative = transform.RollingCorrelationSeries(x, reversed, 3);

            positive.Take(2).Should().OnlyContain(v => double.IsNaN(v));
            positive.Skip(2).Should().OnlyContain(v => Math.Abs(v - 1.0) < 1e-12);
            negative.Skip(2).Should().OnlyContain(v => Math.Abs(v + 1.0) < 1e-12);
        }

        [Fact]
        [Trait("Covers", "RollingCorrelationSeries")]
        public void RollingCorrelationConstantWindowIsNaNAndMismatchRejected()
        {
            var result = transform.RollingCorrelationSeries([1.0, 1, 1, 2], [1.0, 2, 3, 4], 3);

            result[2].Should().Be(naN);
            Action mismatch = () => transform.RollingCorrelationSeries(oneToFive, [1.0, 2, 3], 3);
            mismatch.Should().Throw<ArgumentException>().WithParameterName("y");
            Action smallWindow = () => transform.RollingCorrelationSeries(oneToFive, oneToFive, 2);
            smallWindow.Should().Throw<ArgumentException>().WithParameterName("window");
        }

        [Fact]
        [Trait("Covers", "RollingZScoreSeries")]
        public void RollingZScoreOfSpikeUsesPrecedingWindowOnly()
        {
            var result = transform.RollingZScoreSeries([1.0, 2, 3, 4, 100], 4);

            result.Take(4).Should().OnlyContain(v => double.IsNaN(v));
            result[4].Should().BeApproximately((100 - 2.5) / Math.Sqrt(5.0 / 3.0), 1e-9);
        }

        [Fact]
        [Trait("Covers", "RollingZScoreSeries")]
        public void RollingZScoreConstantBaselineIsNaNAndIncludeCurrentChangesResult()
        {
            transform.RollingZScoreSeries([5.0, 5, 5, 5, 9], 4)[4].Should().Be(naN);
            var included = transform.RollingZScoreSeries([1.0, 2, 3], 3, true);
            included[2].Should().BeApproximately(1.0, 1e-12);
            Action bad = () => transform.RollingZScoreSeries(oneToFive, 1);
            bad.Should().Throw<ArgumentException>().WithParameterName("window");
        }

        [Fact]
        [Trait("Covers", "DetrendSeries")]
        public void DetrendLinearSeriesRecoversInterceptAndSlope()
        {
            var x = Enumerable.Range(0, 10).Select(t => 2.0 + 3.0 * t).ToArray();

            var result = transform.DetrendSeries(x);

            result.Intercept.Should().BeApproximately(2.0, 1e-9);
            result.SlopePerStep.Should().BeApproximately(3.0, 1e-9);
            result.Detrended.Should().OnlyContain(v => Math.Abs(v) < 1e-9);
            result.RSquared.Should().BeApproximately(1.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "DetrendSeries")]
        public void DetrendMeanAndPolynomialMatchClosedForms()
        {
            transform.DetrendSeries([1.0, 2, 3], DetrendMethod.Mean).Detrended.Should().Equal(-1, 0, 1);

            var squares = Enumerable.Range(0, 12).Select(t => 1.0 + 0.5 * t * t).ToArray();
            var polynomial = transform.DetrendSeries(squares, DetrendMethod.Polynomial);
            polynomial.Detrended.Should().OnlyContain(v => Math.Abs(v) < 1e-7);
            polynomial.Intercept.Should().Be(naN);

            Action badDegree = () => transform.DetrendSeries(squares, DetrendMethod.Polynomial, 9);
            badDegree.Should().Throw<ArgumentException>().WithParameterName("degree");
            Action tooShort = () => transform.DetrendSeries([1.0, 2, 4], DetrendMethod.Polynomial);
            tooShort.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "FillMissingValuesInSeries")]
        public void FillLinearInterpolationForwardBackwardNearestAndConstant()
        {
            var gap = new[] { 1.0, naN, naN, 4 };

            var linear = transform.FillMissingValuesInSeries(gap);
            linear.Filled.Should().Equal(1, 2, 3, 4);
            linear.FilledIndices.Should().Equal(1, 2);
            linear.RemainingMissing.Should().Be(0);
            transform.FillMissingValuesInSeries(gap, MissingFillMethod.ForwardFill).Filled.Should().Equal(1, 1, 1, 4);
            transform.FillMissingValuesInSeries(gap, MissingFillMethod.BackwardFill).Filled.Should().Equal(1, 4, 4, 4);
            transform.FillMissingValuesInSeries(gap, MissingFillMethod.NearestValue).Filled.Should().Equal(1, 1, 4, 4);

            transform.FillMissingValuesInSeries(gap, MissingFillMethod.Constant, -7).Filled.Should()
                .Equal(1, -7, -7, 4);

            transform.FillMissingValuesInSeries(gap, MissingFillMethod.SeriesMean).Filled[1].Should().Be(2.5);
            transform.FillMissingValuesInSeries(gap, MissingFillMethod.SeriesMedian).Filled[2].Should().Be(2.5);
        }

        [Fact]
        [Trait("Covers", "FillMissingValuesInSeries")]
        public void FillEdgeGapsAndAllMissing()
        {
            var edges = transform.FillMissingValuesInSeries([naN, 2.0, naN]);

            edges.Filled.Should().Equal(2, 2, 2);
            var forward = transform.FillMissingValuesInSeries([naN, 2.0, naN], MissingFillMethod.ForwardFill);
            forward.Filled.Should().Equal(naN, 2, 2);
            forward.RemainingMissing.Should().Be(1);
            transform.FillMissingValuesInSeries([naN, naN]).RemainingMissing.Should().Be(2);

            transform.FillMissingValuesInSeries([naN, double.PositiveInfinity, 3]).Filled.Should()
                .Equal(3, 3, 3);
        }

        [Fact]
        [Trait("Covers", "MedianFilterSeries")]
        public void MedianFilterRemovesSpikesAndReplicatesEdges()
        {
            transform.MedianFilterSeries([1.0, 9, 2, 8, 3]).Should().Equal(1, 2, 8, 3, 3);
            transform.MedianFilterSeries([5.0, 5, 100, 5, 5]).Should().Equal(5, 5, 5, 5, 5);
        }

        [Fact]
        [Trait("Covers", "MedianFilterSeries")]
        public void MedianFilterRejectsEvenWindowAndKeepsConstant()
        {
            Action even = () => transform.MedianFilterSeries(oneToFive, 4);
            even.Should().Throw<ArgumentException>().WithParameterName("window");
            transform.MedianFilterSeries([2.0]).Should().Equal(2);
        }

        [Fact]
        [Trait("Covers", "HampelOutlierFilter")]
        public void HampelFlagsSingleSpikeAndReplacesWithLocalMedian()
        {
            var result = transform.HampelOutlierFilter([1.0, 1, 1, 100, 1, 1, 1], 2);

            result.OutlierIndices.Should().Equal(3);
            result.Cleaned.Should().Equal(1, 1, 1, 1, 1, 1, 1);
            result.OutlierScores[0].Should().Be(double.PositiveInfinity);
        }

        [Fact]
        [Trait("Covers", "HampelOutlierFilter")]
        public void HampelLeavesCleanSeriesAlone()
        {
            var clean = Enumerable.Range(0, 20).Select(i => Math.Sin(i / 3.0)).ToArray();

            var result = transform.HampelOutlierFilter(clean, 3, 5);

            result.OutlierIndices.Should().BeEmpty();
            result.Cleaned.Should().Equal(clean);
            Action badSigma = () => transform.HampelOutlierFilter(clean, 3, 0);
            badSigma.Should().Throw<ArgumentException>().WithParameterName("nSigmas");
        }

        [Fact]
        [Trait("Covers", "SavitzkyGolayFilter")]
        public void SavitzkyGolayWindowFiveOrderTwoUsesTextbookCoefficients()
        {
            var impulse = new double[11];
            impulse[5] = 1;

            var result = transform.SavitzkyGolayFilter(impulse);

            result[5].Should().BeApproximately(17.0 / 35, 1e-9);
            result[4].Should().BeApproximately(12.0 / 35, 1e-9);
            result[6].Should().BeApproximately(12.0 / 35, 1e-9);
            result[3].Should().BeApproximately(-3.0 / 35, 1e-9);
            result[7].Should().BeApproximately(-3.0 / 35, 1e-9);
        }

        [Fact]
        [Trait("Covers", "SavitzkyGolayFilter")]
        public void SavitzkyGolayReproducesPolynomialAndItsDerivativesIncludingEdges()
        {
            var squares = Enumerable.Range(0, 9).Select(t => (double)t * t).ToArray();

            var smooth = transform.SavitzkyGolayFilter(squares);
            var first = transform.SavitzkyGolayFilter(squares, 5, 2, 1);
            var second = transform.SavitzkyGolayFilter(squares, 5, 2, 2, 2.0);

            for (var i = 0; i < 9; i++)
            {
                smooth[i].Should().BeApproximately(squares[i], 1e-8);
                first[i].Should().BeApproximately(2.0 * i, 1e-8);
                second[i].Should().BeApproximately(2.0 / 4.0, 1e-8);
            }
        }

        [Fact]
        [Trait("Covers", "SavitzkyGolayFilter")]
        public void SavitzkyGolayValidatesParameters()
        {
            Action even = () => transform.SavitzkyGolayFilter(new double[10], 4);
            even.Should().Throw<ArgumentException>().WithParameterName("windowLength");
            Action order = () => transform.SavitzkyGolayFilter(new double[10], 5, 5);
            order.Should().Throw<ArgumentException>().WithParameterName("polynomialOrder");
            Action deriv = () => transform.SavitzkyGolayFilter(new double[10], 5, 2, 3);
            deriv.Should().Throw<ArgumentException>().WithParameterName("derivative");
            Action shortSeries = () => transform.SavitzkyGolayFilter(new double[4]);
            shortSeries.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "BoxCoxTransformSeries")]
        public void BoxCoxWithGivenLambdaMatchesDefinition()
        {
            var x = new[] { 1.0, 2, 3 };

            transform.BoxCoxTransformSeries(x, 2).Values.Should().Equal(0, 1.5, 4);
            var log = transform.BoxCoxTransformSeries(x, 0);
            log.Values[1].Should().BeApproximately(Math.Log(2), 1e-12);
            log.LambdaEstimated.Should().BeFalse();
        }

        [Fact]
        [Trait("Covers", "BoxCoxTransformSeries")]
        public void BoxCoxEstimatesLambdaNearZeroForLogNormalQuantilesAndRejectsNonPositive()
        {
            var n = 200;

            var quantiles = Enumerable.Range(1, n)
                .Select(i => Math.Exp(NormalDistribution.Standard
                    .InverseDistributionFunction((i - 0.5) / n))).ToArray();

            var result = transform.BoxCoxTransformSeries(quantiles);

            result.LambdaEstimated.Should().BeTrue();
            Math.Abs(result.Lambda).Should().BeLessThan(0.1);
            Action nonPositive = () => transform.BoxCoxTransformSeries([1.0, 0, 2]);
            nonPositive.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "KalmanLocalLevelFilter")]
        public void KalmanWithZeroProcessNoiseIsTheRunningMeanWithHandComputedVariances()
        {
            var result = transform.KalmanLocalLevelFilter([1.0, 2, 3], 0, 1);

            result.FilteredLevels.Should().Equal(1, 1.5, 2);
            result.FilteredVariances[1].Should().BeApproximately(0.5, 1e-12);
            result.FilteredVariances[2].Should().BeApproximately(1.0 / 3.0, 1e-12);
            result.OneStepPredictions[2].Should().BeApproximately(1.5, 1e-12);
            result.Innovations[1].Should().BeApproximately(1.0, 1e-12);
            result.StandardisedInnovations[1].Should().BeApproximately(1.0 / Math.Sqrt(2), 1e-12);
            result.SteadyStateGain.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "KalmanLocalLevelFilter")]
        public void KalmanSteadyStateGainForUnitSignalToNoiseIsTheInverseGoldenRatio()
        {
            var result = transform.KalmanLocalLevelFilter([1.0, 2, 3, 4], 1, 1);

            result.SteadyStateGain.Should().BeApproximately((Math.Sqrt(5) - 1) / 2, 1e-12);
            result.LogLikelihood.Should().BeLessThan(0);
            Action badR = () => transform.KalmanLocalLevelFilter(oneToFive, 1, 0);
            badR.Should().Throw<ArgumentException>().WithParameterName("measurementVariance");
            Action badQ = () => transform.KalmanLocalLevelFilter(oneToFive, -1, 1);
            badQ.Should().Throw<ArgumentException>().WithParameterName("processVariance");
        }

        [Fact]
        [Trait("Covers", "SeriesProfile")]
        public void SeriesProfileSummarisesMissingRunsAndMonotonicity()
        {
            var result = transform.SeriesProfile([1.0, 2, naN, 4, 4, 4]);

            result.Length.Should().Be(6);
            result.MissingCount.Should().Be(1);
            result.Minimum.Should().Be(1);
            result.Maximum.Should().Be(4);
            result.Mean.Should().BeApproximately(3.0, 1e-12);
            result.LongestRunOfEqualValues.Should().Be(3);
            result.IsMonotonicIncreasing.Should().BeTrue();
            result.IsMonotonicDecreasing.Should().BeFalse();
            result.FirstValue.Should().Be(1);
            result.LastValue.Should().Be(4);
            result.Notes.Should().Contain(n => n.Contains("missing"));
        }

        [Fact]
        [Trait("Covers", "SeriesProfile")]
        public void SeriesProfileHandlesConstantAllMissingAndSeasonalSeries()
        {
            var constant = transform.SeriesProfile([3.0, 3, 3, 3]);
            constant.IsConstant.Should().BeTrue();
            constant.Lag1Autocorrelation.Should().Be(naN);

            var empty = transform.SeriesProfile([naN, naN]);
            empty.MissingCount.Should().Be(2);
            empty.Mean.Should().Be(naN);

            var seasonal =
                transform.SeriesProfile([.. Enumerable.Range(0, 60).Select(t => Math.Sin(2 * Math.PI * t / 6))]);
            seasonal.SuggestedSeasonalPeriod.Should().Be(6);
            var trend = transform.SeriesProfile([.. Enumerable.Range(0, 30).Select(t => 2.0 * t + 1)]);
            trend.LinearSlopePerStep.Should().BeApproximately(2.0, 1e-9);
            trend.MannKendallPValue.Should().BeLessThan(0.001);
            trend.ZeroCount.Should().Be(0);
        }
    }
}