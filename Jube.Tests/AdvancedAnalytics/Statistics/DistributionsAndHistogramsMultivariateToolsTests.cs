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

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    [Trait("Category", "Unit")]
    public sealed class DistributionsAndHistogramsMultivariateToolsTests
    {
        private const double StandardBivariateNormalDensityAtOrigin = 0.15915494309189535;
        private const double DirichletTwoThreeFiveDensityAtItsMean = 8.505;
        private const double ChiSquareThreeDfDensityAtThree = 0.15418032980376928;
        private const double InverseWishartFiveDfDensityAtHalf = 0.5534766632274595;
        private const double VonMisesFisherCircleKappaOneDensityAtMean = 0.34171048862346315;
        private static readonly MultivariateDistributionTools tools = new();
        private static readonly double[][] identity2 = [[1.0, 0.0], [0.0, 1.0]];

        private static double[][] Rows(params double[][] rows)
        {
            return rows;
        }

        [Fact]
        [Trait("Covers", "MultivariateNormalDensity")]
        public void StandardBivariateNormalDensityAtTheOriginIsOneOverTwoPi()
        {
            var result = tools.MultivariateNormalDensity([0.0, 0.0], identity2,
                Rows([0.0, 0.0], [1.0, 1.0]));

            result.Density[0].Should().BeApproximately(StandardBivariateNormalDensityAtOrigin, 1e-12);
            result.LogDensity[0].Should().BeApproximately(-Math.Log(2 * Math.PI), 1e-12);
            result.MahalanobisDistance[0].Should().BeApproximately(0.0, 1e-12);
            result.Density[1].Should().BeApproximately(StandardBivariateNormalDensityAtOrigin * Math.Exp(-1.0), 1e-12);
            result.MahalanobisDistance[1].Should().BeApproximately(Math.Sqrt(2.0), 1e-12);
        }

        [Fact]
        [Trait("Covers", "MultivariateNormalDensity")]
        public void CorrelatedNormalDensityUsesTheInverseCovariance()
        {
            var covariance = Rows([2.0, 1.0], [1.0, 2.0]);

            var result = tools.MultivariateNormalDensity([0.0, 0.0], covariance, Rows([1.0, 1.0]));

            result.MahalanobisDistance[0].Should().BeApproximately(Math.Sqrt(2.0 / 3.0), 1e-12);
            var expected = Math.Exp(-0.5 * 2.0 / 3.0) / (2 * Math.PI * Math.Sqrt(3.0));
            result.Density[0].Should().BeApproximately(expected, 1e-12);
        }

        [Fact]
        [Trait("Covers", "MultivariateNormalDensity")]
        public void NormalDensityValidatesShapeSymmetryAndDefiniteness()
        {
            Action mismatch = () => tools.MultivariateNormalDensity([0.0], identity2, Rows([0.0]));

            Action asymmetric = () => tools.MultivariateNormalDensity([0.0, 0.0],
                Rows([1.0, 0.5], [0.0, 1.0]), Rows([0.0, 0.0]));
            Action singular = () => tools.MultivariateNormalDensity([0.0, 0.0],
                Rows([1.0, 1.0], [1.0, 1.0]), Rows([0.0, 0.0]));

            Action badPoint = () => tools.MultivariateNormalDensity([0.0, 0.0], identity2,
                Rows([0.0, 0.0, 0.0]));

            Action nan = () => tools.MultivariateNormalDensity([0.0, 0.0], identity2,
                Rows([double.NaN, 0.0]));

            Action noPoints = () => tools.MultivariateNormalDensity([0.0, 0.0], identity2,
                []);

            mismatch.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("mean");
            asymmetric.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("covariance");
            singular.Should().Throw<ArgumentException>().WithMessage("*not positive definite*");
            badPoint.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("points");
            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("points");
            noPoints.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("points");
        }

        [Fact]
        [Trait("Covers", "MultivariateNormalMahalanobisDistance")]
        public void MahalanobisDistanceAndChiSquareTailFollowTheClosedForms()
        {
            var result = tools.MultivariateNormalMahalanobisDistance([0.0, 0.0], identity2,
                Rows([1.0, 1.0], [0.0, 0.0], [3.0, 4.0]));

            result.Dimension.Should().Be(2);
            result.SquaredDistances[0].Should().BeApproximately(2.0, 1e-12);
            result.Distances[0].Should().BeApproximately(Math.Sqrt(2.0), 1e-12);
            result.ChiSquareTailProbability[0].Should().BeApproximately(Math.Exp(-1.0), 1e-9);
            result.ChiSquareTailProbability[1].Should().BeApproximately(1.0, 1e-12);
            result.Distances[2].Should().BeApproximately(5.0, 1e-12);
            result.ChiSquareTailProbability[2].Should().BeApproximately(Math.Exp(-12.5), 1e-12);
        }

        [Fact]
        [Trait("Covers", "MahalanobisOutlierScoresFromSample")]
        public void OutlierScoresRankAnObviousOutlierFirst()
        {
            var data = Rows([1.0, 2.0], [2.0, 3.0], [3.0, 5.0], [4.0, 4.0],
                [5.0, 6.0], [6.0, 5.0], [3.0, 4.0], [4.0, 5.0]);

            var scored = tools.MahalanobisOutlierScoresFromSample(data, Rows([3.6, 4.5], [20.0, -5.0]));

            scored.Distances.Length.Should().Be(2);
            scored.Distances[1].Should().BeGreaterThan(scored.Distances[0] * 5);
            scored.ChiSquareTailProbability[1].Should().BeLessThan(1e-6);
            scored.ChiSquareTailProbability[0].Should().BeGreaterThan(0.9);

            tools.MahalanobisOutlierScoresFromSample(data).Distances.Length.Should().Be(8);
        }

        [Fact]
        [Trait("Covers", "MahalanobisOutlierScoresFromSample")]
        public void OutlierScoresSumOfSquaredDistancesEqualsNMinusOneTimesPForTheSample()
        {
            var data = Rows([1.0, 2.0], [2.0, 3.0], [3.0, 5.0], [4.0, 4.0],
                [5.0, 6.0]);

            var own = tools.MahalanobisOutlierScoresFromSample(data);

            own.SquaredDistances.Sum().Should().BeApproximately(4.0 * 2.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "MahalanobisOutlierScoresFromSample")]
        public void OutlierScoresRejectSingularOrTooSmallSamples()
        {
            Action singular = () => tools.MahalanobisOutlierScoresFromSample(Rows([1.0, 2.0],
                [2.0, 4.0], [3.0, 6.0], [4.0, 8.0]));

            Action tooFewRows = () => tools.MahalanobisOutlierScoresFromSample(Rows([1.0, 2.0, 3.0],
                [2.0, 4.0, 1.0], [3.0, 6.0, 2.0]));

            Action ragged = () => tools.MahalanobisOutlierScoresFromSample(Rows([1.0, 2.0], [2.0],
                [3.0, 6.0]));

            singular.Should().Throw<ArgumentException>().WithMessage("*positive definite*");
            tooFewRows.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("data");
            ragged.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("data");
        }

        [Fact]
        [Trait("Covers", "FitMultivariateNormal")]
        public void FitMatchesHandComputedMeanCovarianceCorrelationAndLikelihood()
        {
            var data = Rows([1.0, 2.0], [2.0, 3.0], [3.0, 5.0], [4.0, 4.0],
                [5.0, 6.0]);

            var fit = tools.FitMultivariateNormal(data);

            fit.SampleSize.Should().Be(5);
            fit.Dimension.Should().Be(2);
            fit.Mean.Should().Equal(3.0, 4.0);
            fit.Covariance[0][0].Should().BeApproximately(2.5, 1e-12);
            fit.Covariance[1][1].Should().BeApproximately(2.5, 1e-12);
            fit.Covariance[0][1].Should().BeApproximately(2.25, 1e-12);
            fit.Correlation[0][1].Should().BeApproximately(0.9, 1e-12);
            fit.StandardDeviations[0].Should().BeApproximately(Math.Sqrt(2.5), 1e-12);
            fit.LogLikelihood.Should().BeApproximately(-13.619010974363375, 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitMultivariateNormal")]
        public void FitRejectsTooFewRowsAndMissingValues()
        {
            Action few = () => tools.FitMultivariateNormal(Rows([1.0, 2.0], [2.0, 3.0]));

            Action nan = () => tools.FitMultivariateNormal(Rows([1.0, 2.0], [2.0, double.NaN],
                [3.0, 1.0], [4.0, 5.0]));

            few.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("data");
            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("data");
        }

        [Fact]
        [Trait("Covers", "MultivariateNormalRandomSample")]
        public void NormalSampleIsReproducibleWithTheRightMeanAndCovariance()
        {
            var covariance = Rows([2.0, 1.0], [1.0, 2.0]);

            var a = tools.MultivariateNormalRandomSample([1.0, -1.0], covariance, 20000, 5);
            var b = tools.MultivariateNormalRandomSample([1.0, -1.0], covariance, 20000, 5);

            a.Rows.Select(r => r[0]).Should().Equal(b.Rows.Select(r => r[0]));
            a.SampleMean[0].Should().BeApproximately(1.0, 0.05);
            a.SampleMean[1].Should().BeApproximately(-1.0, 0.05);
            var fit = tools.FitMultivariateNormal(a.Rows);
            fit.Covariance[0][0].Should().BeApproximately(2.0, 0.1);
            fit.Covariance[0][1].Should().BeApproximately(1.0, 0.1);
            a.Seed.Should().Be(5);

            Action zero = () => tools.MultivariateNormalRandomSample([0.0, 0.0], identity2, 0);
            zero.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("count");
        }

        [Fact]
        [Trait("Covers", "MultivariateNormalOrthantProbability")]
        public void OrthantProbabilitiesMatchClosedFormsInOneAndTwoDimensions()
        {
            var one = tools.MultivariateNormalOrthantProbability([0.0], Rows([1.0]),
                Rows([1.0]));
            one.LowerOrthantProbability[0].Should().BeApproximately(0.8413447460685429, 1e-6);
            one.UpperOrthantProbability[0].Should().BeApproximately(0.15865525393145707, 1e-6);

            var independent = tools.MultivariateNormalOrthantProbability([0.0, 0.0], identity2,
                Rows([0.0, 0.0]));
            independent.LowerOrthantProbability[0].Should().BeApproximately(0.25, 1e-4);

            var correlated = tools.MultivariateNormalOrthantProbability([0.0, 0.0],
                Rows([1.0, 0.5], [0.5, 1.0]), Rows([0.0, 0.0]));
            correlated.LowerOrthantProbability[0].Should().BeApproximately(1.0 / 3.0, 1e-3);
            correlated.UpperOrthantProbability[0].Should().BeApproximately(1.0 / 3.0, 1e-3);
        }

        [Fact]
        [Trait("Covers", "MultivariateNormalOrthantProbability")]
        public void OrthantProbabilityLimitsDimensionality()
        {
            var mean = new double[9];

            var covariance = Enumerable.Range(0, 9).Select(i => Enumerable.Range(0, 9)
                .Select(j => i == j ? 1.0 : 0.0).ToArray()).ToArray();

            Action tooBig = () => tools.MultivariateNormalOrthantProbability(mean, covariance, Rows(mean));

            tooBig.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("mean");
        }

        [Fact]
        [Trait("Covers", "DirichletSummary")]
        public void DirichletSummaryMatchesTheClosedFormMomentsAndCovariance()
        {
            var summary = tools.DirichletSummary([2.0, 3.0, 5.0]);

            summary.Total.Should().Be(10.0);
            summary.Mean[0].Should().BeApproximately(0.2, 1e-12);
            summary.Mean[1].Should().BeApproximately(0.3, 1e-12);
            summary.Mean[2].Should().BeApproximately(0.5, 1e-12);
            summary.Variance[0].Should().BeApproximately(2.0 * 8.0 / (100.0 * 11.0), 1e-12);
            summary.Covariance[0][1].Should().BeApproximately(-6.0 / 1100.0, 1e-12);
            summary.Covariance[0][0].Should().BeApproximately(summary.Variance[0], 1e-12);

            Action negative = () => tools.DirichletSummary([2.0, -1.0]);
            Action single = () => tools.DirichletSummary([2.0]);
            negative.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("concentrations");
            single.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("concentrations");
        }

        [Fact]
        [Trait("Covers", "DirichletDensity")]
        public void DirichletDensityAtTheMeanMatchesTheHandComputedValue()
        {
            var result = tools.DirichletDensity([2.0, 3.0, 5.0], Rows([0.2, 0.3, 0.5]));

            result.Values[0].Should().BeApproximately(DirichletTwoThreeFiveDensityAtItsMean, 1e-9);

            var uniform = tools.DirichletDensity([1.0, 1.0, 1.0], Rows([0.2, 0.3, 0.5]));
            uniform.Values[0].Should().BeApproximately(2.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "DirichletDensity")]
        public void DirichletDensityRejectsPointsOffTheSimplex()
        {
            Action sum = () => tools.DirichletDensity([2.0, 3.0], Rows([0.5, 0.6]));
            Action zero = () => tools.DirichletDensity([2.0, 3.0], Rows([0.0, 1.0]));
            Action width = () => tools.DirichletDensity([2.0, 3.0], Rows([0.5, 0.25, 0.25]));

            sum.Should().Throw<ArgumentException>().WithMessage("*simplex*");
            zero.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("points");
            width.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("points");
        }

        [Fact]
        [Trait("Covers", "DirichletRandomSample")]
        public void DirichletSampleRowsSumToOneAndAverageToTheMean()
        {
            var a = tools.DirichletRandomSample([2.0, 3.0, 5.0], 20000, 9);
            var b = tools.DirichletRandomSample([2.0, 3.0, 5.0], 20000, 9);

            a.Rows.Should().OnlyContain(r => Math.Abs(r.Sum() - 1.0) < 1e-9 && r.All(v => v > 0));
            a.SampleMean[0].Should().BeApproximately(0.2, 0.005);
            a.SampleMean[1].Should().BeApproximately(0.3, 0.005);
            a.SampleMean[2].Should().BeApproximately(0.5, 0.005);
            a.Rows[0].Should().Equal(b.Rows[0]);
            a.Rows[19999].Should().Equal(b.Rows[19999]);
        }

        [Fact]
        [Trait("Covers", "FitDirichlet")]
        public void DirichletFitRecoversTheConcentrationsOfALargeSample()
        {
            var sample = tools.DirichletRandomSample([2.0, 3.0, 5.0], 5000, 4);

            var fit = tools.FitDirichlet(sample.Rows);

            fit.SampleSize.Should().Be(5000);
            fit.Concentrations[0].Should().BeApproximately(2.0, 0.25);
            fit.Concentrations[1].Should().BeApproximately(3.0, 0.35);
            fit.Concentrations[2].Should().BeApproximately(5.0, 0.6);
            fit.Total.Should().BeApproximately(10.0, 1.0);
            fit.Mean.Sum().Should().BeApproximately(1.0, 1e-9);
            fit.LogLikelihood.Should().BeGreaterThan(double.NegativeInfinity);
        }

        [Fact]
        [Trait("Covers", "FitDirichlet")]
        public void DirichletFitRejectsRowsThatAreNotProportions()
        {
            Action notOne = () => tools.FitDirichlet(Rows([0.5, 0.6], [0.4, 0.6], [0.3, 0.7]));
            Action zero = () => tools.FitDirichlet(Rows([0.0, 1.0], [0.4, 0.6], [0.3, 0.7]));
            Action few = () => tools.FitDirichlet(Rows([0.5, 0.5]));

            notOne.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("data");
            zero.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("data");
            few.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("data");
        }

        [Fact]
        [Trait("Covers", "MultinomialSummary")]
        public void MultinomialSummaryMatchesTheClosedForms()
        {
            var summary = tools.MultinomialSummary(3, [0.5, 0.3, 0.2]);

            summary.Mean[0].Should().BeApproximately(1.5, 1e-12);
            summary.Mean[1].Should().BeApproximately(0.9, 1e-12);
            summary.Mean[2].Should().BeApproximately(0.6, 1e-12);
            summary.Variance[0].Should().BeApproximately(0.75, 1e-12);
            summary.Variance[1].Should().BeApproximately(0.63, 1e-12);
            summary.Covariance[0][1].Should().BeApproximately(-0.45, 1e-12);
            summary.Trials.Should().Be(3);

            Action badSum = () => tools.MultinomialSummary(3, [0.5, 0.6]);
            Action noTrials = () => tools.MultinomialSummary(0, [0.5, 0.5]);
            badSum.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("probabilities");
            noTrials.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("trials");
        }

        [Fact]
        [Trait("Covers", "MultinomialProbability")]
        public void MultinomialProbabilityMatchesTheFactorialFormula()
        {
            var result = tools.MultinomialProbability(3, [0.5, 0.3, 0.2],
                Rows([1.0, 1.0, 1.0], [3.0, 0.0, 0.0], [2.0, 2.0, 0.0]));

            result.Probability[0].Should().BeApproximately(0.18, 1e-12);
            result.Probability[1].Should().BeApproximately(0.125, 1e-12);
            result.Probability[2].Should().Be(0.0);
            result.LogProbability[0].Should().BeApproximately(Math.Log(0.18), 1e-9);
            result.LogProbability[2].Should().BeNull();

            Action fractional = () => tools.MultinomialProbability(3, [0.5, 0.5], Rows([1.5, 1.5]));
            fractional.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("counts");
        }

        [Fact]
        [Trait("Covers", "MultinomialRandomSample")]
        public void MultinomialSampleRowsSumToTheTrialsAndAverageToTheMean()
        {
            var a = tools.MultinomialRandomSample(10, [0.5, 0.3, 0.2], 20000, 3);
            var b = tools.MultinomialRandomSample(10, [0.5, 0.3, 0.2], 20000, 3);

            a.Rows.Should().OnlyContain(r => Math.Abs(r.Sum() - 10.0) < 1e-12 && r.All(v => v >= 0));
            a.SampleMean[0].Should().BeApproximately(5.0, 0.05);
            a.SampleMean[1].Should().BeApproximately(3.0, 0.05);
            a.SampleMean[2].Should().BeApproximately(2.0, 0.05);
            a.Rows[100].Should().Equal(b.Rows[100]);

            var certain = tools.MultinomialRandomSample(4, [1.0, 0.0], 5);
            certain.Rows.Should().OnlyContain(r => r[0] == 4.0 && r[1] == 0.0);
        }

        [Fact]
        [Trait("Covers", "WishartSummary")]
        public void WishartSummaryMatchesTheClosedFormMeanAndVariance()
        {
            var summary = tools.WishartSummary(5, identity2);

            summary.Mean[0].Should().Equal(5.0, 0.0);
            summary.Mean[1].Should().Equal(0.0, 5.0);
            summary.VarianceOfEntries[0][0].Should().BeApproximately(10.0, 1e-9);
            summary.VarianceOfEntries[0][1].Should().BeApproximately(5.0, 1e-9);
            summary.VarianceOfEntries[1][1].Should().BeApproximately(10.0, 1e-9);
            summary.DegreesOfFreedom.Should().Be(5.0);

            var scaled = tools.WishartSummary(4, Rows([2.0, 0.5], [0.5, 1.0]));
            scaled.Mean[0][0].Should().BeApproximately(8.0, 1e-9);
            scaled.Mean[0][1].Should().BeApproximately(2.0, 1e-9);
            scaled.VarianceOfEntries[0][1].Should().BeApproximately(4.0 * (0.25 + 2.0), 1e-9);
        }

        [Fact]
        [Trait("Covers", "WishartSummary")]
        public void WishartRejectsTooFewDegreesAndBadScale()
        {
            Action few = () => tools.WishartSummary(1, identity2);
            Action singular = () => tools.WishartSummary(3, Rows([1.0, 1.0], [1.0, 1.0]));

            few.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("degreesOfFreedom");
            singular.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("scale");
        }

        [Fact]
        [Trait("Covers", "WishartDensity")]
        public void OneByOneWishartIsAChiSquare()
        {
            var result = tools.WishartDensity(3, Rows([1.0]), Rows([3.0]));

            result.Density.Should().BeApproximately(ChiSquareThreeDfDensityAtThree, 1e-9);
            result.LogDensity.Should().BeApproximately(Math.Log(ChiSquareThreeDfDensityAtThree), 1e-9);
        }

        [Fact]
        [Trait("Covers", "WishartDensity")]
        public void WishartDensityIsZeroForAMatrixThatIsNotPositiveDefinite()
        {
            var result = tools.WishartDensity(3, identity2, Rows([1.0, 2.0], [2.0, 1.0]));

            result.Density.Should().Be(0.0);
            result.LogDensity.Should().BeNull();

            Action size = () => tools.WishartDensity(3, identity2, Rows([1.0]));
            size.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("matrix");
        }

        [Fact]
        [Trait("Covers", "WishartRandomSample")]
        public void WishartSamplesAreSymmetricPositiveDefiniteAndAverageToDegreesTimesScale()
        {
            var a = tools.WishartRandomSample(5, identity2, 4000, 2);
            var b = tools.WishartRandomSample(5, identity2, 4000, 2);

            a.Matrices.Length.Should().Be(4000);

            a.Matrices.Should().OnlyContain(m => Math.Abs(m[0][1] - m[1][0]) < 1e-9 && m[0][0] > 0 &&
                                                 m[0][0] * m[1][1] - m[0][1] * m[1][0] > 0);

            a.MeanMatrix[0][0].Should().BeApproximately(5.0, 0.25);
            a.MeanMatrix[1][1].Should().BeApproximately(5.0, 0.25);
            a.MeanMatrix[0][1].Should().BeApproximately(0.0, 0.25);
            a.Matrices[7][0][0].Should().Be(b.Matrices[7][0][0]);

            Action many = () => tools.WishartRandomSample(5, identity2, 100000);
            many.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("count");
        }

        [Fact]
        [Trait("Covers", "InverseWishartSummary")]
        public void InverseWishartSummaryMatchesTheOneByOneClosedForms()
        {
            var summary = tools.InverseWishartSummary(5.0, Rows([1.0]));

            summary.Mean[0][0].Should().BeApproximately(1.0 / 3.0, 1e-9);
            summary.VarianceOfEntries[0][0].Should().BeApproximately(2.0 / 9.0, 1e-9);

            var matrix = tools.InverseWishartSummary(6.0, identity2);
            matrix.Mean[0][0].Should().BeApproximately(1.0 / 3.0, 1e-9);
            matrix.Mean[0][1].Should().BeApproximately(0.0, 1e-9);

            Action few = () => tools.InverseWishartSummary(2.0, identity2);
            few.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("degreesOfFreedom");
        }

        [Fact]
        [Trait("Covers", "InverseWishartDensity")]
        public void InverseWishartDensityMatchesTheOneByOneClosedForm()
        {
            var result = tools.InverseWishartDensity(5.0, Rows([1.0]), Rows([0.5]));

            result.Density.Should().BeApproximately(InverseWishartFiveDfDensityAtHalf, 1e-9);
            result.LogDensity.Should().BeApproximately(Math.Log(InverseWishartFiveDfDensityAtHalf), 1e-9);

            var zero = tools.InverseWishartDensity(4.0, identity2, Rows([1.0, 2.0], [2.0, 1.0]));
            zero.Density.Should().Be(0.0);
        }

        [Fact]
        [Trait("Covers", "VonMisesFisherDensity")]
        public void CircularVonMisesFisherDensityMatchesTheBesselClosedForm()
        {
            var result = tools.VonMisesFisherDensity([1.0, 0.0], 1.0,
                Rows([1.0, 0.0], [0.0, 1.0], [3.0, 0.0]));

            result.Concentration.Should().Be(1.0);
            result.MeanDirection.Should().Equal(1.0, 0.0);
            result.Density[0].Should().BeApproximately(VonMisesFisherCircleKappaOneDensityAtMean, 1e-7);
            result.Density[1].Should().BeApproximately(1.0 / (2 * Math.PI * 1.2660658777520084), 1e-7);
            result.Density[2].Should().BeApproximately(result.Density[0]!.Value, 1e-12);

            var uniform = tools.VonMisesFisherDensity([0.0, 2.0], 0.0, Rows([1.0, 1.0]));
            uniform.Density[0].Should().BeApproximately(1.0 / (2 * Math.PI), 1e-9);
            uniform.MeanDirection.Should().Equal(0.0, 1.0);
        }

        [Fact]
        [Trait("Covers", "VonMisesFisherDensity")]
        public void VonMisesFisherDensityRejectsOddDimensionsAndZeroDirections()
        {
            Action odd = () => tools.VonMisesFisherDensity([1.0, 0.0, 0.0], 1.0, Rows([1.0, 0.0, 0.0]));
            Action zero = () => tools.VonMisesFisherDensity([0.0, 0.0], 1.0, Rows([1.0, 0.0]));
            Action negative = () => tools.VonMisesFisherDensity([1.0, 0.0], -1.0, Rows([1.0, 0.0]));

            odd.Should().Throw<ArgumentException>().WithMessage("*even number*");
            zero.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("meanDirection");
            negative.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("concentration");
        }

        [Fact]
        [Trait("Covers", "VonMisesFisherRandomSample")]
        public void SphericalVonMisesFisherSamplesAreUnitVectorsWithTheMeanResultantLength()
        {
            var sample = tools.VonMisesFisherRandomSample([0.0, 0.0, 1.0], 5.0, 4000, 8);
            var again = tools.VonMisesFisherRandomSample([0.0, 0.0, 1.0], 5.0, 4000, 8);

            sample.Rows.Should().OnlyContain(r => Math.Abs(Math.Sqrt(r.Sum(v => v * v)) - 1.0) < 1e-9);
            sample.SampleMean[2].Should().BeApproximately(1.0 / Math.Tanh(5.0) - 1.0 / 5.0, 0.03);
            Math.Abs(sample.SampleMean[0]).Should().BeLessThan(0.05);
            sample.Rows[3].Should().Equal(again.Rows[3]);

            Action flat = () => tools.VonMisesFisherRandomSample([1.0, 0.0], 1.0, 10);
            flat.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("meanDirection");
        }

        [Fact]
        [Trait("Covers", "UniformSphereRandomSample")]
        public void SphereSamplesLieOnTheSurfaceAndAverageToTheCentre()
        {
            var sample = tools.UniformSphereRandomSample([1.0, 2.0, 3.0], 2.0, 4000, 6);

            sample.Rows.Should().OnlyContain(r =>
                Math.Abs(Math.Sqrt(Math.Pow(r[0] - 1.0, 2) + Math.Pow(r[1] - 2.0, 2) + Math.Pow(r[2] - 3.0, 2)) - 2.0)
                < 1e-9);

            sample.SampleMean[0].Should().BeApproximately(1.0, 0.1);
            sample.SampleMean[1].Should().BeApproximately(2.0, 0.1);
            sample.SampleMean[2].Should().BeApproximately(3.0, 0.1);

            Action radius = () => tools.UniformSphereRandomSample([0.0, 0.0], 0.0, 10);
            radius.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("radius");
        }

        [Fact]
        [Trait("Covers", "UniformBallRandomSample")]
        public void BallSamplesLieInsideAndHaveTwoThirdsMeanRadiusInTwoDimensions()
        {
            var sample = tools.UniformBallRandomSample([0.0, 0.0], 3.0, 20000, 6);

            var radii = sample.Rows.Select(r => Math.Sqrt(r[0] * r[0] + r[1] * r[1])).ToArray();
            radii.Should().OnlyContain(r => r <= 3.0 + 1e-9);
            radii.Average().Should().BeApproximately(2.0, 0.05);
            sample.SampleMean[0].Should().BeApproximately(0.0, 0.06);

            Action count = () => tools.UniformBallRandomSample([0.0, 0.0], 1.0, 0);
            count.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("count");
        }
    }
}