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
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    [Trait("Category", "Unit")]
    public sealed class DistributionsAndHistogramsFittingToolsTests
    {
        private static readonly double rayleighUnbiasedCorrectionForThree = 2.0 * Math.Sqrt(3.0) / 3.3233509704478426;
        private static readonly DistributionFittingTools fitting = new();
        private static readonly UnivariateDistributionTools univariate = new();

        private static double[] QuantileGrid(DistributionKind kind, double[] parameters, int n)
        {
            var probabilities = Enumerable.Range(0, n).Select(i => (i + 0.5) / n).ToArray();

            return [.. univariate.DistributionQuantile(kind, parameters, probabilities).Values.Select(v => v!.Value)];
        }

        [Fact]
        [Trait("Covers", "FitDistribution")]
        public void FitExponentialToOneTwoThreeGivesRateOneHalfAndTheClosedFormLikelihood()
        {
            var fit = fitting.FitDistribution([1.0, 2.0, 3.0], DistributionKind.Exponential);

            fit.Distribution.Should().Be("Exponential");
            fit.ParameterNames.Should().Equal("rate");
            fit.Parameters[0].Should().BeApproximately(0.5, 1e-9);
            fit.LogLikelihood.Should().BeApproximately(3 * Math.Log(0.5) - 0.5 * 6.0, 1e-9);
            fit.EstimatedParameterCount.Should().Be(1);
            fit.Aic.Should().BeApproximately(2 - 2 * fit.LogLikelihood!.Value, 1e-9);
            fit.Bic.Should().BeApproximately(Math.Log(3) - 2 * fit.LogLikelihood!.Value, 1e-9);
            fit.Mean.Should().BeApproximately(2.0, 1e-9);
            fit.SampleSize.Should().Be(3);
            fit.Estimated.Should().Equal(true);
            fit.Method.Should().Contain("Library");
            fit.KolmogorovSmirnovStatistic.Should().BeInRange(0.0, 1.0);
            fit.Notes.Should().Contain(n => n.Contains("Fewer than 20"));
        }

        [Fact]
        [Trait("Covers", "FitDistribution")]
        public void FitNormalRecoversTheSampleMean()
        {
            var fit = fitting.FitDistribution([1.0, 2.0, 3.0, 4.0, 5.0], DistributionKind.Normal);

            fit.Parameters[0].Should().BeApproximately(3.0, 1e-9);
            fit.Parameters[1].Should().BeInRange(Math.Sqrt(2.0) - 1e-9, Math.Sqrt(2.5) + 1e-9);
            fit.Median.Should().BeApproximately(3.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitDistribution")]
        public void FitDiscreteDistributionsMatchTheirClosedFormEstimators()
        {
            fitting.FitDistribution([2.0, 3.0, 4.0, 5.0, 6.0], DistributionKind.Poisson).Parameters[0]
                .Should().BeApproximately(4.0, 1e-9);

            fitting.FitDistribution([0.0, 1.0, 2.0, 3.0, 4.0], DistributionKind.Geometric).Parameters[0]
                .Should().BeApproximately(1.0 / 3.0, 1e-9);

            fitting.FitDistribution([0.0, 1.0, 1.0, 0.0, 1.0], DistributionKind.Bernoulli).Parameters[0]
                .Should().BeApproximately(0.6, 1e-9);

            var uniform = fitting.FitDistribution([1.0, 6.0, 3.0, 4.0], DistributionKind.UniformDiscrete);
            uniform.Parameters[0].Should().BeApproximately(1.0, 1e-9);
            uniform.Parameters[1].Should().BeApproximately(6.0, 1e-9);
            uniform.IsDiscrete.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "FitDistribution")]
        public void FitContinuousClosedFormEstimatorsMatchHandComputation()
        {
            var pareto = fitting.FitDistribution([1.0, 2.0, 4.0], DistributionKind.Pareto);
            pareto.Parameters[0].Should().BeApproximately(1.0, 1e-9);
            pareto.Parameters[1].Should().BeApproximately(3.0 / (3.0 * Math.Log(2.0)), 1e-9);

            var lognormal = fitting.FitDistribution([1.0, Math.E, Math.E * Math.E], DistributionKind.Lognormal);
            lognormal.Parameters[0].Should().BeApproximately(1.0, 1e-9);

            var uniform = fitting.FitDistribution([1.0, 5.0, 3.0], DistributionKind.UniformContinuous);
            uniform.Parameters[0].Should().BeApproximately(1.0, 1e-9);
            uniform.Parameters[1].Should().BeApproximately(5.0, 1e-9);

            var rayleigh = fitting.FitDistribution([1.0, 2.0, 3.0], DistributionKind.Rayleigh);

            rayleigh.Parameters[0].Should()
                .BeApproximately(Math.Sqrt(14.0 / 6.0) * rayleighUnbiasedCorrectionForThree, 1e-6);
        }

        [Theory]
        [InlineData(DistributionKind.Gamma, new[] { 2.0, 3.0 }, 0.25)]
        [InlineData(DistributionKind.Weibull, new[] { 1.5, 2.0 }, 0.1)]
        [InlineData(DistributionKind.Beta, new[] { 2.0, 5.0 }, 0.1)]
        [InlineData(DistributionKind.Gumbel, new[] { 1.0, 2.0 }, 0.1)]
        [InlineData(DistributionKind.Cauchy, new[] { 1.0, 2.0 }, 0.15)]
        [InlineData(DistributionKind.InverseGaussian, new[] { 2.0, 3.0 }, 0.2)]
        [Trait("Covers", "FitDistribution")]
        public void FitRecoversParametersFromAQuantileGridOfTheSameDistribution(DistributionKind kind,
            double[] truth, double relativeTolerance)
        {
            var data = QuantileGrid(kind, truth, 400);

            var fit = fitting.FitDistribution(data, kind);

            for (var i = 0; i < truth.Length; i++)
            {
                fit.Parameters[i].Should().BeApproximately(truth[i], Math.Abs(truth[i]) * relativeTolerance);
            }
        }

        [Theory]
        [InlineData(DistributionKind.Laplace, new[] { 1.0, 2.0 })]
        [InlineData(DistributionKind.Logistic, new[] { 1.0, 2.0 })]
        [InlineData(DistributionKind.SkewNormal, new[] { 0.0, 1.0, 3.0 })]
        [InlineData(DistributionKind.InverseGamma, new[] { 4.0, 2.0 })]
        [InlineData(DistributionKind.LogLogistic, new[] { 2.0, 4.0 })]
        [InlineData(DistributionKind.Kumaraswamy, new[] { 2.0, 3.0 })]
        [InlineData(DistributionKind.FoldedNormal, new[] { 1.0, 1.0 })]
        [InlineData(DistributionKind.GeneralizedNormal, new[] { 0.0, 1.0, 1.5 })]
        [Trait("Covers", "FitDistribution")]
        public void NumericalFitRecoversParametersFromAQuantileGrid(DistributionKind kind, double[] truth)
        {
            var data = QuantileGrid(kind, truth, 400);

            var fit = fitting.FitDistribution(data, kind);

            fit.Method.Should().Contain("Numerical");
            fit.Estimated.Should().OnlyContain(e => e);

            for (var i = 0; i < truth.Length; i++)
            {
                fit.Parameters[i].Should().BeApproximately(truth[i], Math.Max(Math.Abs(truth[i]) * 0.15, 0.15));
            }
        }

        [Fact]
        [Trait("Covers", "FitDistribution")]
        public void FitLaplaceAgreesWithMedianAndMeanAbsoluteDeviationClosedForm()
        {
            var fit = fitting.FitDistribution([1.0, 2.0, 3.0, 4.0, 10.0], DistributionKind.Laplace);

            fit.Parameters[0].Should().BeApproximately(3.0, 0.02);
            fit.Parameters[1].Should().BeApproximately(2.2, 0.02);
        }

        [Fact]
        [Trait("Covers", "FitDistribution")]
        public void FitBinomialHoldsTheNumberOfTrialsFixedAndEstimatesTheProbability()
        {
            var fit = fitting.FitDistribution([3.0, 4.0, 5.0, 4.0, 4.0], DistributionKind.Binomial,
                ["trials"], [10.0]);

            fit.Parameters[0].Should().Be(10.0);
            fit.Parameters[1].Should().BeApproximately(0.4, 1e-4);
            fit.Estimated.Should().Equal(false, true);
            fit.EstimatedParameterCount.Should().Be(1);
            fit.Notes.Should().Contain(n => n.Contains("trials was held fixed"));

            var defaulted = fitting.FitDistribution([3.0, 4.0, 5.0, 4.0, 4.0], DistributionKind.Binomial);
            defaulted.Parameters[0].Should().Be(5.0);
            defaulted.Parameters[1].Should().BeApproximately(0.8, 1e-4);
        }

        [Fact]
        [Trait("Covers", "FitDistribution")]
        public void FitGeneralizedParetoHoldsTheLocationAtTheSmallestObservation()
        {
            var data = QuantileGrid(DistributionKind.GeneralizedPareto, [0.0, 1.0, 0.2], 500);

            var fit = fitting.FitDistribution(data, DistributionKind.GeneralizedPareto, ["location"],
                [0.0]);

            fit.Parameters[0].Should().Be(0.0);
            fit.Parameters[1].Should().BeApproximately(1.0, 0.15);
            fit.Parameters[2].Should().BeApproximately(0.2, 0.12);
            fit.Estimated.Should().Equal(false, true, true);
        }

        [Fact]
        [Trait("Covers", "FitDistribution")]
        public void FitDistributionExplainsBadInputs()
        {
            Action negative = () => fitting.FitDistribution([1.0, -2.0, 3.0], DistributionKind.Lognormal);

            Action unknownName = () => fitting.FitDistribution([1.0, 2.0, 3.0], DistributionKind.Binomial,
                ["nonsense"], [1.0]);

            Action lengthMismatch = () => fitting.FitDistribution([1.0, 2.0, 3.0], DistributionKind.Binomial,
                ["trials"], [1.0, 2.0]);

            Action notFittable = () => fitting.FitDistribution([1.0, 2.0, 3.0], DistributionKind.Levy);
            Action single = () => fitting.FitDistribution([1.0], DistributionKind.Normal);
            Action nan = () => fitting.FitDistribution([1.0, double.NaN], DistributionKind.Normal);
            Action constant = () => fitting.FitDistribution([2.0, 2.0, 2.0], DistributionKind.Gamma);

            Action allFixed = () => fitting.FitDistribution([1.0, 2.0, 3.0], DistributionKind.Laplace,
                ["location", "scale"], [1.0, 1.0]);

            Action fractional = () => fitting.FitDistribution([1.5, 2.0], DistributionKind.Poisson);

            negative.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            negative.Should().Throw<ArgumentException>().WithMessage("*greater than 0*");
            unknownName.Should().Throw<ArgumentException>().WithMessage("*trials, probability*");
            lengthMismatch.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("fixedParameterValues");
            notFittable.Should().Throw<ArgumentException>().WithMessage("*not available*");
            single.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            constant.Should().Throw<ArgumentException>();
            allFixed.Should().Throw<ArgumentException>().WithMessage("*nothing to estimate*");
            fractional.Should().Throw<ArgumentException>().WithMessage("*whole numbers*");
        }

        [Fact]
        [Trait("Covers", "FitDistributionAndScoreValues")]
        public void FitAndScoreGivesTailProbabilitiesUnderTheFittedDistribution()
        {
            var reference = new[] { 1.0, 2.0, 3.0 };

            var result = fitting.FitDistributionAndScoreValues(reference, DistributionKind.Exponential,
                [2.0, 20.0]);

            result.Fit.Parameters[0].Should().BeApproximately(0.5, 1e-9);
            result.UpperTail[0].Should().BeApproximately(Math.Exp(-1.0), 1e-9);
            result.LowerTail[0].Should().BeApproximately(1 - Math.Exp(-1.0), 1e-9);
            result.UpperTail[1].Should().BeApproximately(Math.Exp(-10.0), 1e-12);
            result.TwoSided[1].Should().BeApproximately(2 * Math.Exp(-10.0), 1e-12);
            result.Density[0].Should().BeApproximately(0.5 * Math.Exp(-1.0), 1e-9);
            result.Values.Should().Equal(2.0, 20.0);
        }

        [Fact]
        [Trait("Covers", "FitDistributionAndScoreValues")]
        public void FitAndScoreRejectsEmptyScoringValues()
        {
            Action act = () => fitting.FitDistributionAndScoreValues([1.0, 2.0, 3.0],
                DistributionKind.Exponential, []);

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("valuesToScore");
        }

        [Fact]
        [Trait("Covers", "RankDistributionFits")]
        public void RankPutsTheGeneratingExponentialFirstByAic()
        {
            var data = QuantileGrid(DistributionKind.Exponential, [1.0], 300);

            var ranking = fitting.RankDistributionFits(data);

            ranking.RankedBy.Should().Be("Aic");
            ranking.SampleSize.Should().Be(300);
            ranking.Candidates.Length.Should().BeGreaterThan(10);
            ranking.Candidates[0].Distribution.Should().Be("Exponential");
            ranking.Candidates.Select(c => c.Rank).Should().Equal(Enumerable.Range(1, ranking.Candidates.Length));
            ranking.Candidates.Select(c => c.Aic).Should().BeInAscendingOrder();
            ranking.Candidates[0].KolmogorovSmirnovStatistic.Should().BeLessThan(0.05);
            ranking.Candidates[0].AicRank.Should().Be(1);

            ranking.Candidates.Single(c => c.Distribution == "Normal").Aic.Should()
                .BeGreaterThan(ranking.Candidates[0].Aic);

            ranking.Candidates.Should().OnlyContain(c => !c.IsDiscrete);
        }

        [Fact]
        [Trait("Covers", "RankDistributionFits")]
        public void RankOfCountDataUsesDiscreteCandidatesAndFindsThePoisson()
        {
            var data = QuantileGrid(DistributionKind.Poisson, [4.0], 300);

            var ranking = fitting.RankDistributionFits(data);

            ranking.Candidates.Should().OnlyContain(c => c.IsDiscrete);
            ranking.Candidates.Take(2).Select(c => c.Distribution).Should().Contain("Poisson");
            ranking.Notes.Should().Contain(n => n.Contains("counts"));
        }

        [Fact]
        [Trait("Covers", "RankDistributionFits")]
        public void RankHonoursExplicitCandidatesCriterionAndReportsFailures()
        {
            var data = QuantileGrid(DistributionKind.Normal, [0.0, 1.0], 200);

            var ranking = fitting.RankDistributionFits(data,
                [DistributionKind.Lognormal, DistributionKind.Normal, DistributionKind.Laplace],
                FitRankCriterion.Bic);

            ranking.RankedBy.Should().Be("Bic");
            ranking.Failed.Should().ContainSingle(f => f.StartsWith("Lognormal"));
            ranking.Candidates.Select(c => c.Distribution).Should().Equal("Normal", "Laplace");
            ranking.Candidates[0].BicRank.Should().Be(1);
            ranking.Candidates[0].Bic.Should().BeLessThan(ranking.Candidates[1].Bic);
        }

        [Fact]
        [Trait("Covers", "RankDistributionFits")]
        public void RankByAverageAndByEachStatisticOrdersCandidates()
        {
            var data = QuantileGrid(DistributionKind.Exponential, [1.0], 300);
            var candidates = new[] { DistributionKind.Exponential, DistributionKind.Normal, DistributionKind.Gamma };

            foreach (var criterion in Enum.GetValues<FitRankCriterion>())
            {
                var ranking = fitting.RankDistributionFits(data, candidates, criterion);

                ranking.Candidates.Length.Should().Be(3);
                ranking.Candidates.Select(c => c.Rank).Should().Equal(1, 2, 3);
                ranking.Candidates[2].Distribution.Should().Be("Normal");
            }
        }

        [Fact]
        [Trait("Covers", "RankDistributionFits")]
        public void RankRejectsMixedKindsAndTinySamples()
        {
            var data = QuantileGrid(DistributionKind.Exponential, [1.0], 50);

            Action mixed = () => fitting.RankDistributionFits(data,
                [DistributionKind.Exponential, DistributionKind.Poisson]);

            Action tiny = () => fitting.RankDistributionFits([1.0, 2.0]);
            Action nan = () => fitting.RankDistributionFits([1.0, 2.0, 3.0, 4.0, double.NaN]);

            mixed.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("candidates");
            tiny.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
        }

        [Fact]
        [Trait("Covers", "QuantileQuantilePlotPoints")]
        public void QuantileQuantilePlotOfNormalDataAgainstNormalIsAStraightLine()
        {
            var data = QuantileGrid(DistributionKind.Normal, [10.0, 3.0], 100);

            var plot = fitting.QuantileQuantilePlotPoints(data, DistributionKind.Normal, [0.0, 1.0]);

            plot.Correlation.Should().BeGreaterThan(0.9999);
            plot.LineSlope.Should().BeApproximately(3.0, 0.02);
            plot.LineIntercept.Should().BeApproximately(10.0, 0.02);
            plot.SampleValues.Length.Should().Be(100);
            plot.SampleValues.Should().BeInAscendingOrder();
            plot.PlottingPositions[0].Should().BeApproximately(0.5 / 100, 1e-12);
            plot.PlottingPosition.Should().Be("Hazen");
        }

        [Fact]
        [Trait("Covers", "QuantileQuantilePlotPoints")]
        public void QuantileQuantilePlotUsesTheRequestedPlottingPositionAndFitsWhenParametersAreOmitted()
        {
            var plot = fitting.QuantileQuantilePlotPoints([5.0, 1.0, 3.0, 2.0, 4.0], DistributionKind.Normal,
                [3.0, 1.0], PlottingPosition.Blom);

            plot.SampleValues.Should().Equal(1.0, 2.0, 3.0, 4.0, 5.0);
            plot.PlottingPositions[2].Should().BeApproximately(0.5, 1e-12);
            plot.PlottingPositions[0].Should().BeApproximately(0.625 / 5.25, 1e-12);
            plot.TheoreticalQuantiles[2].Should().BeApproximately(3.0, 1e-9);
            (plot.TheoreticalQuantiles[0]! + plot.TheoreticalQuantiles[4]!).Should().BeApproximately(6.0, 1e-9);

            var fitted = fitting.QuantileQuantilePlotPoints([1.0, 2.0, 3.0, 4.0, 5.0, 6.0],
                DistributionKind.Exponential, null, PlottingPosition.Weibull);
            fitted.PlottingPositions[0].Should().BeApproximately(1.0 / 7.0, 1e-12);
            fitted.Parameters[0].Should().BeApproximately(1.0 / 3.5, 1e-9);
        }

        [Fact]
        [Trait("Covers", "QuantileQuantilePlotPoints")]
        public void QuantileQuantilePlotThinsLongSamplesEvenlyAndValidatesArguments()
        {
            var data = QuantileGrid(DistributionKind.Normal, [0.0, 1.0], 1000);

            var plot = fitting.QuantileQuantilePlotPoints(data, DistributionKind.Normal, null, PlottingPosition.Hazen,
                10);

            plot.SampleValues.Length.Should().Be(10);
            plot.SampleValues[0].Should().Be(data.Min());
            plot.SampleValues[9].Should().Be(data.Max());
            plot.Correlation.Should().BeGreaterThan(0.999);

            Action tooFewPoints = () => fitting.QuantileQuantilePlotPoints(data, DistributionKind.Normal, null,
                PlottingPosition.Hazen, 5);

            Action tooFewValues = () => fitting.QuantileQuantilePlotPoints([1.0, 2.0], DistributionKind.Normal);

            Action badParameters = () => fitting.QuantileQuantilePlotPoints(data, DistributionKind.Normal,
                [0.0]);

            tooFewPoints.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("maxPoints");
            tooFewValues.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            badParameters.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("parameters");
        }

        [Fact]
        [Trait("Covers", "ProbabilityProbabilityPlotPoints")]
        public void ProbabilityProbabilityPlotOfMatchingDistributionLiesOnTheDiagonal()
        {
            var data = QuantileGrid(DistributionKind.Exponential, [2.0], 100);

            var plot = fitting.ProbabilityProbabilityPlotPoints(data, DistributionKind.Exponential,
                [2.0]);

            plot.Correlation.Should().BeGreaterThan(0.99999);
            plot.TheoreticalProbabilities[0].Should().BeApproximately(0.005, 1e-6);
            plot.EmpiricalProbabilities[99].Should().BeApproximately(0.995, 1e-9);
            plot.TheoreticalProbabilities.Select(p => p!.Value).Should().BeInAscendingOrder();
            plot.LineSlope.Should().BeNull();
        }

        [Fact]
        [Trait("Covers", "ProbabilityProbabilityPlotPoints")]
        public void ProbabilityProbabilityPlotOfAMismatchedDistributionDeviatesFromTheDiagonal()
        {
            var data = QuantileGrid(DistributionKind.Exponential, [1.0], 200);

            var wrong = fitting.ProbabilityProbabilityPlotPoints(data, DistributionKind.Normal, [5.0, 0.5]);
            var right = fitting.ProbabilityProbabilityPlotPoints(data, DistributionKind.Exponential);

            wrong.Correlation.Should().BeLessThan(right.Correlation!.Value);
            wrong.TheoreticalProbabilities[0].Should().BeApproximately(0.0, 1e-9);

            Action nan = () => fitting.ProbabilityProbabilityPlotPoints([1.0, 2.0, double.NaN],
                DistributionKind.Normal);

            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
        }
    }
}