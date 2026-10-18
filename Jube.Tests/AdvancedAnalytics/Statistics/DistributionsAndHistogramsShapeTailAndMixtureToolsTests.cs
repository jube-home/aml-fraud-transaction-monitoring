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
    public sealed class DistributionsAndHistogramsShapeTailAndMixtureToolsTests
    {
        private static readonly DistributionShapeAndTailTools shape = new();
        private static readonly MixtureModelTools mixture = new();
        private static readonly UnivariateDistributionTools univariate = new();

        private static double[] OneToN(int n)
        {
            return [.. Enumerable.Range(1, n).Select(i => (double)i)];
        }

        private static double[] QuantileGrid(DistributionKind kind, double[] parameters, int n)
        {
            var probabilities = Enumerable.Range(0, n).Select(i => (i + 0.5) / n).ToArray();

            return [.. univariate.DistributionQuantile(kind, parameters, probabilities).Values.Select(v => v!.Value)];
        }

        private static double[] TwoClusters()
        {
            return
            [
                .. QuantileGrid(DistributionKind.Normal, [0.0, 1.0], 150),
                .. QuantileGrid(DistributionKind.Normal, [10.0, 1.0], 150)
            ];
        }

        [Fact]
        [Trait("Covers", "NormalityShapeSummary")]
        public void ShapeSummaryMatchesTheHandComputedSkewnessKurtosisAndJarqueBera()
        {
            var result = shape.NormalityShapeSummary([2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0]);

            result.SampleSize.Should().Be(8);
            result.Mean.Should().BeApproximately(5.0, 1e-12);
            result.Skewness.Should().BeApproximately(0.8184875533567997, 1e-9);
            result.ExcessKurtosis.Should().BeApproximately(0.940625, 1e-9);
            result.SkewnessStandardError.Should().BeApproximately(0.7521014330903549, 1e-9);
            result.ExcessKurtosisStandardError.Should().BeApproximately(1.4808804789742462, 1e-9);
            result.SkewnessZ.Should().BeApproximately(1.0882675093353655, 1e-9);
            result.ExcessKurtosisZ.Should().BeApproximately(0.6351795525399443, 1e-9);
            result.JarqueBeraStatistic.Should().BeApproximately(0.5901692708333333, 1e-9);
            result.JarqueBeraPValue.Should().BeApproximately(0.7444685763913046, 1e-6);
            result.Verdict.Should().StartWith("No evidence against a normal shape");
        }

        [Fact]
        [Trait("Covers", "NormalityShapeSummary")]
        public void ShapeSummaryFlagsSkewAndTailWeight()
        {
            var skewed = shape.NormalityShapeSummary(QuantileGrid(DistributionKind.Exponential, [1.0], 300));
            skewed.Skewness.Should().BeGreaterThan(1.5);
            skewed.Verdict.Should().Contain("right skewed");
            skewed.JarqueBeraPValue.Should().BeLessThan(1e-6);

            var uniform = shape.NormalityShapeSummary(QuantileGrid(DistributionKind.UniformContinuous,
                [0.0, 1.0], 300));
            uniform.Skewness.Should().BeApproximately(0.0, 1e-9);
            uniform.ExcessKurtosis.Should().BeApproximately(-1.2, 0.01);
            uniform.Verdict.Should().Contain("lighter tailed");

            var normal = shape.NormalityShapeSummary(QuantileGrid(DistributionKind.Normal, [0.0, 1.0], 300));
            normal.Skewness.Should().BeApproximately(0.0, 1e-9);
            normal.Verdict.Should().StartWith("No evidence");
        }

        [Fact]
        [Trait("Covers", "NormalityShapeSummary")]
        public void ShapeSummaryRejectsConstantTinyAndMissingData()
        {
            Action constant = () => shape.NormalityShapeSummary([3.0, 3.0, 3.0, 3.0]);
            Action tiny = () => shape.NormalityShapeSummary([1.0, 2.0, 3.0]);
            Action nan = () => shape.NormalityShapeSummary([1.0, 2.0, 3.0, double.NaN]);

            constant.Should().Throw<ArgumentException>().WithMessage("*identical*");
            tiny.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");

            var small = shape.NormalityShapeSummary([1.0, 2.0, 3.0, 5.0]);
            small.JarqueBeraStatistic.Should().BeNull();
            small.Verdict.Should().Contain("Too few");
        }

        [Fact]
        [Trait("Covers", "TailExceedanceByQuantile")]
        public void QuantileExceedanceMatchesHandComputationForOneToOneHundred()
        {
            var result = shape.TailExceedanceByQuantile(OneToN(100), [0.9]);

            var row = result.Rows[0];
            row.Level.Should().Be(0.9);
            row.Threshold.Should().BeApproximately(90.1, 1e-9);
            row.ExceedanceCount.Should().Be(10);
            row.ExceedanceFraction.Should().BeApproximately(0.1, 1e-12);
            row.MeanExcess.Should().BeApproximately(5.4, 1e-9);
            row.MeanOfExceedances.Should().BeApproximately(95.5, 1e-9);
            row.MaximumExcess.Should().BeApproximately(9.9, 1e-9);
            result.SampleSize.Should().Be(100);
        }

        [Fact]
        [Trait("Covers", "TailExceedanceByQuantile")]
        public void QuantileExceedanceLowerTailAndValidation()
        {
            var lower = shape.TailExceedanceByQuantile(OneToN(100), [0.1], TailDirection.Lower);
            lower.Rows[0].Threshold.Should().BeApproximately(10.9, 1e-9);
            lower.Rows[0].ExceedanceCount.Should().Be(10);
            lower.Rows[0].MeanExcess.Should().BeApproximately(5.4, 1e-9);
            lower.Rows[0].MeanOfExceedances.Should().BeApproximately(5.5, 1e-9);

            Action zero = () => shape.TailExceedanceByQuantile(OneToN(10), [0.0]);
            Action one = () => shape.TailExceedanceByQuantile(OneToN(10), [1.0]);
            Action empty = () => shape.TailExceedanceByQuantile(OneToN(10), []);

            zero.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("levels");
            one.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("levels");
            empty.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("levels");
        }

        [Fact]
        [Trait("Covers", "TailExceedanceByThreshold")]
        public void ThresholdExceedanceReportsCountsAndMeanExcess()
        {
            var result = shape.TailExceedanceByThreshold(OneToN(100), [50.0, 1000.0, 0.0]);

            result.Rows[0].ExceedanceCount.Should().Be(50);
            result.Rows[0].Level.Should().BeApproximately(0.5, 1e-12);
            result.Rows[0].MeanExcess.Should().BeApproximately(25.5, 1e-9);
            result.Rows[0].MeanOfExceedances.Should().BeApproximately(75.5, 1e-9);
            result.Rows[0].MaximumExcess.Should().BeApproximately(50.0, 1e-9);
            result.Rows[1].ExceedanceCount.Should().Be(0);
            result.Rows[1].MeanExcess.Should().BeNull();
            result.Rows[2].ExceedanceCount.Should().Be(100);

            var lower = shape.TailExceedanceByThreshold(OneToN(100), [10.5], TailDirection.Lower);
            lower.Rows[0].ExceedanceCount.Should().Be(10);
            lower.Rows[0].MeanExcess.Should().BeApproximately(5.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "TailExceedanceByThreshold")]
        public void ThresholdExceedanceValidatesInput()
        {
            Action nan = () => shape.TailExceedanceByThreshold(OneToN(5), [double.NaN]);
            Action empty = () => shape.TailExceedanceByThreshold([], [1.0]);

            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("thresholds");
            empty.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
        }

        [Fact]
        [Trait("Covers", "FitGeneralizedParetoTail")]
        public void ExponentialTailGivesShapeNearZeroAndTheClosedFormReturnLevel()
        {
            var data = QuantileGrid(DistributionKind.Exponential, [1.0], 1000);

            var tail = shape.FitGeneralizedParetoTail(data, 0.9, [0.99, 0.999]);

            tail.ExceedanceCount.Should().Be(100);
            tail.Threshold.Should().BeApproximately(-Math.Log(0.1), 0.05);
            tail.Scale.Should().BeApproximately(1.0, 0.15);
            tail.Shape.Should().BeApproximately(0.0, 0.12);
            tail.ReturnLevels[0].Should().BeApproximately(-Math.Log(0.01), 0.15);
            tail.ReturnLevels[1].Should().BeApproximately(-Math.Log(0.001), 0.35);
            tail.TailKind.Should().Contain("exponential");
            tail.SampleSize.Should().Be(1000);
        }

        [Fact]
        [Trait("Covers", "FitGeneralizedParetoTail")]
        public void HeavyTailGivesPositiveShape()
        {
            var data = QuantileGrid(DistributionKind.Pareto, [1.0, 2.0], 1000);

            var tail = shape.FitGeneralizedParetoTail(data);

            tail.Shape.Should().BeApproximately(0.5, 0.15);
            tail.TailKind.Should().Contain("Heavy");
            tail.ReturnLevels.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "FitGeneralizedParetoTail")]
        public void GeneralizedParetoTailValidatesItsArguments()
        {
            var data = QuantileGrid(DistributionKind.Exponential, [1.0], 200);

            Action fewValues = () => shape.FitGeneralizedParetoTail(OneToN(50));
            Action lowReturn = () => shape.FitGeneralizedParetoTail(data, 0.9, [0.8]);
            Action tooFewExceedances = () => shape.FitGeneralizedParetoTail(data, 0.99);
            Action badQuantile = () => shape.FitGeneralizedParetoTail(data, 1.0);

            fewValues.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            lowReturn.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("returnProbabilities");
            tooFewExceedances.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("thresholdQuantile");
            badQuantile.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("thresholdQuantile");
        }

        [Fact]
        [Trait("Covers", "FitGaussianMixture1D")]
        public void TwoSeparatedClustersAreRecoveredAsTwoComponents()
        {
            var fit = mixture.FitGaussianMixture1D(TwoClusters(), 2);

            fit.Components.Should().Be(2);
            fit.ComponentSummaries[0].Mean.Should().BeApproximately(0.0, 0.1);
            fit.ComponentSummaries[1].Mean.Should().BeApproximately(10.0, 0.1);
            fit.ComponentSummaries[0].Weight.Should().BeApproximately(0.5, 0.01);
            fit.ComponentSummaries[0].StandardDeviation.Should().BeApproximately(1.0, 0.1);
            fit.ComponentSummaries[1].StandardDeviation.Should().BeApproximately(1.0, 0.1);
            fit.ComponentSummaries.Select(c => c.AssignedCount).Should().Equal(150, 150);
            fit.Assignments.Take(150).Should().OnlyContain(a => a == 0);
            fit.Assignments.Skip(150).Should().OnlyContain(a => a == 1);
            fit.ParameterCount.Should().Be(5);
            fit.Aic.Should().BeApproximately(10 - 2 * fit.LogLikelihood, 1e-9);
            fit.Bic.Should().BeApproximately(5 * Math.Log(300) - 2 * fit.LogLikelihood, 1e-9);
            fit.Responsibilities.Should().BeNull();
            fit.Converged.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "FitGaussianMixture1D")]
        public void SingleComponentEqualsTheNormalMaximumLikelihoodAndResponsibilitiesAreOptional()
        {
            var data = QuantileGrid(DistributionKind.Normal, [5.0, 2.0], 100);

            var fit = mixture.FitGaussianMixture1D(data, 1, 1, 300, 1e-7, 2, true);

            fit.ComponentSummaries[0].Mean.Should().BeApproximately(5.0, 1e-6);
            fit.ComponentSummaries[0].Weight.Should().BeApproximately(1.0, 1e-9);
            fit.ParameterCount.Should().Be(2);
            fit.Responsibilities!.Length.Should().Be(100);
            fit.Responsibilities.Should().OnlyContain(r => Math.Abs(r.Sum() - 1.0) < 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitGaussianMixture1D")]
        public void MixtureFitIsReproducibleForASeedAndValidatesInput()
        {
            var a = mixture.FitGaussianMixture1D(TwoClusters(), 2, 7);
            var b = mixture.FitGaussianMixture1D(TwoClusters(), 2, 7);
            a.LogLikelihood.Should().Be(b.LogLikelihood);

            Action tooMany = () => mixture.FitGaussianMixture1D(OneToN(20), 6);
            Action constant = () => mixture.FitGaussianMixture1D([.. Enumerable.Repeat(3.0, 30)], 2);
            Action tiny = () => mixture.FitGaussianMixture1D(OneToN(5), 1);
            Action badRestarts = () => mixture.FitGaussianMixture1D(TwoClusters(), 2, 1, 300, 1e-7, 0);
            Action badTolerance = () => mixture.FitGaussianMixture1D(TwoClusters(), 2, 1, 300, 5.0);
            Action nan = () => mixture.FitGaussianMixture1D([.. OneToN(20), double.NaN], 2);

            tooMany.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("components");
            constant.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            tiny.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            badRestarts.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("restarts");
            badTolerance.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("tolerance");
            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
        }

        [Fact]
        [Trait("Covers", "SelectGaussianMixtureComponents")]
        public void ComponentSelectionPicksTwoForTwoSeparatedClusters()
        {
            var result = mixture.SelectGaussianMixtureComponents(TwoClusters(), 4);

            result.Rows.Select(r => r.Components).Should().Equal(1, 2, 3, 4);
            result.BestByBic.Should().Be(2);
            result.Rows[0].ParameterCount.Should().Be(2);
            result.Rows[1].ParameterCount.Should().Be(5);
            result.Rows[1].Bic.Should().BeLessThan(result.Rows[0].Bic);
            result.Rows[1].LogLikelihood.Should().BeGreaterThan(result.Rows[0].LogLikelihood);
        }

        [Fact]
        [Trait("Covers", "SelectGaussianMixtureComponents")]
        public void ComponentSelectionStopsWhenValuesRunOut()
        {
            var result = mixture.SelectGaussianMixtureComponents(QuantileGrid(DistributionKind.Normal,
                [0.0, 1.0], 12), 6);

            result.Rows.Length.Should().BeLessThanOrEqualTo(2);
            result.Notes.Should().Contain(n => n.Contains("too few"));

            Action bad = () => mixture.SelectGaussianMixtureComponents(TwoClusters(), 0);
            bad.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("maximumComponents");
        }

        [Fact]
        [Trait("Covers", "FitMixtureModel")]
        public void LognormalMixtureRecoversTwoLogScaleGroups()
        {
            var low = QuantileGrid(DistributionKind.Lognormal, [0.0, 0.3], 150);
            var high = QuantileGrid(DistributionKind.Lognormal, [3.0, 0.3], 150);

            var fit = mixture.FitMixtureModel([.. low, .. high], DistributionKind.Lognormal, 2);

            fit.ComponentDistribution.Should().Be("Lognormal");
            fit.ParameterNames.Should().Equal("location", "shape");
            fit.Weights[0].Should().BeApproximately(0.5, 0.01);
            fit.ComponentParameters[0][0].Should().BeApproximately(0.0, 0.1);
            fit.ComponentParameters[1][0].Should().BeApproximately(3.0, 0.1);
            fit.ComponentParameters[0][1].Should().BeApproximately(0.3, 0.1);
            fit.ParameterCount.Should().Be(2 * 2 + 1);
            fit.Assignments.Take(150).Should().OnlyContain(a => a == 0);
            fit.Assignments.Skip(150).Should().OnlyContain(a => a == 1);
            fit.Aic.Should().BeApproximately(2 * 5 - 2 * fit.LogLikelihood, 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitMixtureModel")]
        public void MixtureModelRejectsUnsupportedFamiliesAndBadData()
        {
            Action numeric = () => mixture.FitMixtureModel(OneToN(50), DistributionKind.Laplace, 2);
            Action discrete = () => mixture.FitMixtureModel(OneToN(50), DistributionKind.Poisson, 2);

            Action negative = () => mixture.FitMixtureModel([.. OneToN(50).Select(v => v - 25)],
                DistributionKind.Lognormal, 2);

            Action tooMany = () => mixture.FitMixtureModel(OneToN(50), DistributionKind.Normal, 9);

            numeric.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("componentDistribution");
            discrete.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("componentDistribution");
            negative.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            tooMany.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("components");
        }

        [Fact]
        [Trait("Covers", "EvaluateMixtureDistribution")]
        public void MixtureEvaluationMatchesTheHandComputedDensityCumulativeAndResponsibilities()
        {
            var result = mixture.EvaluateMixtureDistribution(DistributionKind.Normal, [0.5, 0.5],
                [[0.0, 1.0], [10.0, 1.0]], [0.0, 5.0, 10.0]);

            result.Density[0].Should().BeApproximately(0.19947114020071635, 1e-9);
            result.Cumulative[1].Should().BeApproximately(0.5, 1e-9);
            result.ComponentResponsibilities[0][0].Should().BeApproximately(1.0, 1e-12);
            result.ComponentResponsibilities[1][0].Should().BeApproximately(0.5, 1e-9);
            result.ComponentResponsibilities[2][1].Should().BeApproximately(1.0, 1e-12);
            result.ComponentResponsibilities.Should().OnlyContain(r => Math.Abs(r.Sum() - 1.0) < 1e-9);
            result.Points.Should().Equal(0.0, 5.0, 10.0);
        }

        [Fact]
        [Trait("Covers", "EvaluateMixtureDistribution")]
        public void MixtureEvaluationValidatesWeightsAndParameters()
        {
            Action badSum = () => mixture.EvaluateMixtureDistribution(DistributionKind.Normal, [0.5, 0.6],
                [[0.0, 1.0], [1.0, 1.0]], [0.0]);

            Action mismatch = () => mixture.EvaluateMixtureDistribution(DistributionKind.Normal, [0.5, 0.5],
                [[0.0, 1.0]], [0.0]);

            Action badParameter = () => mixture.EvaluateMixtureDistribution(DistributionKind.Normal,
                [0.5, 0.5], [[0.0, 1.0], [1.0, -1.0]], [0.0]);

            badSum.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("weights");
            mismatch.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("componentParameters");
            badParameter.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("componentParameters");

            var gamma = mixture.EvaluateMixtureDistribution(DistributionKind.Exponential, [1.0],
                [[2.0]], [1.0]);

            gamma.Density[0].Should().BeApproximately(2.0 * Math.Exp(-2.0), 1e-9);
            gamma.Cumulative[0].Should().BeApproximately(1 - Math.Exp(-2.0), 1e-9);
        }
    }
}