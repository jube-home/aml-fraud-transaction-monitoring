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
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class RelationshipsAndRegressionCurveTests
    {
        private static readonly double[] puromycinConcentration =
            [0.02, 0.02, 0.06, 0.06, 0.11, 0.11, 0.22, 0.22, 0.56, 0.56, 1.10, 1.10];

        private static readonly double[] puromycinRate = [76, 47, 97, 107, 123, 139, 159, 152, 191, 201, 207, 200];
        private readonly NonlinearCurveTools tools = new();

        private static double[] Grid(double from, double to, int count)
        {
            return [.. Enumerable.Range(0, count).Select(i => from + (to - from) * i / (count - 1))];
        }

        private static double[] Truth(NonlinearCurveFamily family)
        {
            return family switch
            {
                NonlinearCurveFamily.Exponential => [2.0, 0.4],
                NonlinearCurveFamily.AsymptoticExponential => [10.0, 8.0, 0.6],
                NonlinearCurveFamily.SaturatingExponential => [8.0, 0.7],
                NonlinearCurveFamily.Power => [2.5, 0.6],
                NonlinearCurveFamily.Logarithmic => [1.5, 2.0],
                NonlinearCurveFamily.Logistic3 => [100.0, 1.2, 1.5],
                NonlinearCurveFamily.Logistic4 => [5.0, 60.0, 1.0, -1.0],
                NonlinearCurveFamily.Gompertz => [50.0, 4.0, 0.8],
                NonlinearCurveFamily.MichaelisMenten => [120.0, 2.0],
                NonlinearCurveFamily.Hill => [90.0, 2.0, 2.0],
                NonlinearCurveFamily.GaussianPeak => [4.0, 2.0, 1.5],
                _ => [0.5, 0.25]
            };
        }

        private static double Evaluate(NonlinearCurveFamily family, double[] t, double x)
        {
            return family switch
            {
                NonlinearCurveFamily.Exponential => t[0] * Math.Exp(t[1] * x),
                NonlinearCurveFamily.AsymptoticExponential => t[0] - t[1] * Math.Exp(-t[2] * x),
                NonlinearCurveFamily.SaturatingExponential => t[0] * (1 - Math.Exp(-t[1] * x)),
                NonlinearCurveFamily.Power => t[0] * Math.Pow(x, t[1]),
                NonlinearCurveFamily.Logarithmic => t[0] + t[1] * Math.Log(x),
                NonlinearCurveFamily.Logistic3 => t[0] / (1 + Math.Exp(-t[1] * (x - t[2]))),
                NonlinearCurveFamily.Logistic4 => t[0] + (t[1] - t[0]) / (1 + Math.Exp(-t[2] * (x - t[3]))),
                NonlinearCurveFamily.Gompertz => t[0] * Math.Exp(-t[1] * Math.Exp(-t[2] * x)),
                NonlinearCurveFamily.MichaelisMenten => t[0] * x / (t[1] + x),
                NonlinearCurveFamily.Hill => t[0] * Math.Pow(x, t[2]) / (Math.Pow(t[1], t[2]) + Math.Pow(x, t[2])),
                NonlinearCurveFamily.GaussianPeak => t[0] * Math.Exp(-Math.Pow(x - t[1], 2) / (2 * t[2] * t[2])),
                _ => 1 / (t[0] + t[1] * x)
            };
        }

        [Theory]
        [InlineData(NonlinearCurveFamily.Exponential, 0.5, 3.0)]
        [InlineData(NonlinearCurveFamily.AsymptoticExponential, 0.0, 6.0)]
        [InlineData(NonlinearCurveFamily.SaturatingExponential, 0.1, 6.0)]
        [InlineData(NonlinearCurveFamily.Power, 0.5, 8.0)]
        [InlineData(NonlinearCurveFamily.Logarithmic, 0.5, 9.0)]
        [InlineData(NonlinearCurveFamily.Logistic3, -6.0, 9.0)]
        [InlineData(NonlinearCurveFamily.Logistic4, -6.0, 6.0)]
        [InlineData(NonlinearCurveFamily.Gompertz, 0.0, 10.0)]
        [InlineData(NonlinearCurveFamily.MichaelisMenten, 0.1, 10.0)]
        [InlineData(NonlinearCurveFamily.Hill, 0.2, 8.0)]
        [InlineData(NonlinearCurveFamily.GaussianPeak, -3.0, 7.0)]
        [InlineData(NonlinearCurveFamily.Reciprocal, 0.0, 6.0)]
        [Trait("Covers", "FitNonlinearCurve")]
        public void EveryFamilyRecoversItsGeneratingParametersFromNoiseFreeData(NonlinearCurveFamily family,
            double from, double to)
        {
            var truth = Truth(family);
            var x = Grid(from, to, 30);
            var y = x.Select(v => Evaluate(family, truth, v)).ToArray();

            var result = tools.FitNonlinearCurve(x, y, family, null, 0.95, null, 20000, true);

            result.Family.Should().Be(family.ToString());
            result.Formula.Should().StartWith("y =");
            result.Parameters.Should().HaveCount(truth.Length);

            for (var j = 0; j < truth.Length; j++)
            {
                result.Parameters[j].Estimate.Should().BeApproximately(truth[j],
                    Math.Max(1e-4 * Math.Abs(truth[j]), 1e-5), $"parameter {result.Parameters[j].Parameter}");
            }

            result.RSquared.Should().BeGreaterThan(0.999999);
            result.Residuals!.Max(Math.Abs).Should().BeLessThan(1e-3);
            result.FittedValues.Should().HaveCount(30);
            result.Observations.Should().Be(30);
        }

        [Fact]
        [Trait("Covers", "FitNonlinearCurve")]
        public void MichaelisMentenMatchesThePublishedPuromycinFit()
        {
            var result = tools.FitNonlinearCurve(puromycinConcentration, puromycinRate,
                NonlinearCurveFamily.MichaelisMenten);

            result.Parameters[0].Parameter.Should().Be("maximumRate");
            result.Parameters[0].Estimate.Should().BeApproximately(212.7, 0.1);
            result.Parameters[1].Estimate.Should().BeApproximately(0.06412, 5e-5);
            result.Parameters[0].StandardError.Should().BeApproximately(6.947, 0.01);
            result.Parameters[1].StandardError.Should().BeApproximately(0.008281, 2e-5);
            result.Parameters[0].TStatistic.Should().BeApproximately(30.615, 0.05);
            result.Parameters[1].TStatistic.Should().BeApproximately(7.743, 0.02);
            result.ResidualStandardError.Should().BeApproximately(10.93, 0.01);
            result.ResidualDegreesOfFreedom.Should().Be(10);
            result.Converged.Should().BeTrue();
            result.Parameters[0].PValue.Should().BeLessThan(1e-9);
            result.Parameters[1].ConfidenceLower.Should().BeLessThan(0.06412);
            result.Parameters[1].ConfidenceUpper.Should().BeGreaterThan(0.06412);
            var tss = puromycinRate.Sum(v => Math.Pow(v - puromycinRate.Average(), 2));
            result.RSquared.Should().BeApproximately(1 - result.ResidualSumOfSquares / tss, 1e-12);
            result.Aic.Should().BeApproximately(-2 * result.LogLikelihood + 6, 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitNonlinearCurve")]
        public void PredictionsUseTheDeltaMethod()
        {
            var result = tools.FitNonlinearCurve(puromycinConcentration, puromycinRate,
                NonlinearCurveFamily.MichaelisMenten, null, 0.95, [0.5, 10]);

            var v = result.Parameters[0].Estimate;
            var k = result.Parameters[1].Estimate;
            result.Predictions![0].Prediction.Should().BeApproximately(v * 0.5 / (k + 0.5), 1e-9);
            result.Predictions[0].StandardErrorOfMean.Should().BeGreaterThan(0);
            result.Predictions[0].PredictionUpper.Should().BeGreaterThan(result.Predictions[0].MeanUpper);

            result.Predictions[1].StandardErrorOfMean.Should()
                .BeGreaterThan(result.Predictions[0].StandardErrorOfMean * 0.1);

            result.Predictions[0].X.Should().Be(0.5);
        }

        [Fact]
        [Trait("Covers", "FitNonlinearCurve")]
        public void FitValidatesItsInputs()
        {
            var x = puromycinConcentration;
            var y = puromycinRate;
            var mm = NonlinearCurveFamily.MichaelisMenten;

            RelationshipsFixtures.Rejects(() => tools.FitNonlinearCurve(x, [.. y.Take(3)], mm), "y");
            RelationshipsFixtures.Rejects(() => tools.FitNonlinearCurve(x, y, (NonlinearCurveFamily)99), "family");
            RelationshipsFixtures.Rejects(() => tools.FitNonlinearCurve(x, y, mm, [1.0]), "startValues");

            RelationshipsFixtures.Rejects(() => tools.FitNonlinearCurve(x, y, mm, [1.0, double.NaN]),
                "startValues");

            RelationshipsFixtures.Rejects(() => tools.FitNonlinearCurve(x, y, mm, null, 1.5), "confidenceLevel");

            RelationshipsFixtures.Rejects(() => tools.FitNonlinearCurve(x, y, mm, null, 0.95, null, 5),
                "maximumIterations");

            RelationshipsFixtures.Rejects(() => tools.FitNonlinearCurve(x, y, mm, null, 0.95, [double.NaN]),
                "predictAt");

            RelationshipsFixtures.Rejects(() => tools.FitNonlinearCurve([1.0, 2, 3], [1.0, 2, 3], mm),
                "x");

            RelationshipsFixtures.Rejects(
                () => tools.FitNonlinearCurve([2.0, 2, 2, 2, 2], [1.0, 2, 3, 4, 5], mm), "x");

            RelationshipsFixtures.Rejects(
                () => tools.FitNonlinearCurve([0.0, 1, 2, 3, 4], [1.0, 2, 3, 4, 5],
                    NonlinearCurveFamily.Power), "x");

            RelationshipsFixtures.Rejects(
                () => tools.FitNonlinearCurve([-1.0, 1, 2, 3, 4], [1.0, 2, 3, 4, 5], mm), "x");

            RelationshipsFixtures.Rejects(
                () => tools.FitNonlinearCurve([1.0, 2, double.NaN, 3, 4], [1.0, 2, 3, 4, 5], mm), "x");

            tools.FitNonlinearCurve([1.0, 2, double.NaN, 3, 4, 6, 8], [1.0, 1.8, 3, 2.5, 3, 3.3, 3.5],
                mm, null, 0.95, null, 1000, false, true).RowsDropped.Should().Be(1);

            RelationshipsFixtures.Rejects(
                () => tools.FitNonlinearCurve(x, y, NonlinearCurveFamily.Power, null, 0.95, [-1.0]),
                "predictAt");
        }

        [Fact]
        [Trait("Covers", "FitNonlinearCurve")]
        public void FitWarnsWhenParametersAreNotIdentifiable()
        {
            var x = Grid(1, 10, 12);
            var y = x.Select(v => 2 * v + 1).ToArray();

            var result = tools.FitNonlinearCurve(x, y, NonlinearCurveFamily.Logistic4, [0.0, 30.0, 0.1, 5.0]);

            result.Warnings.Should().NotBeNull();
            result.StartValuesUsed.Should().Equal(0.0, 30.0, 0.1, 5.0);
        }

        [Fact]
        [Trait("Covers", "CompareNonlinearCurveFamilies")]
        public void ComparisonRanksTheSaturatingFamiliesAheadOfUnsuitableOnes()
        {
            var result = tools.CompareNonlinearCurveFamilies(puromycinConcentration, puromycinRate,
            [
                NonlinearCurveFamily.MichaelisMenten, NonlinearCurveFamily.Exponential,
                NonlinearCurveFamily.Reciprocal, NonlinearCurveFamily.Logarithmic, NonlinearCurveFamily.Hill
            ]);

            result.Families.Should().HaveCount(5);
            var mm = result.Families.Single(f => f.Family == "MichaelisMenten");
            var exp = result.Families.Single(f => f.Family == "Exponential");
            mm.Fitted.Should().BeTrue();
            mm.Aic.Should().BeLessThan(exp.Aic);
            mm.Rank.Should().BeLessThan(exp.Rank);
            mm.Parameters[0].Should().BeApproximately(212.7, 0.1);
            mm.ParameterNames.Should().Equal("maximumRate", "halfSaturation");
            result.Families.Where(f => f.Fitted).Sum(f => f.AkaikeWeight).Should().BeApproximately(1, 1e-9);
            result.Families.Select(f => f.Rank).Where(r => r > 0).Should().BeInAscendingOrder();
            result.BestByAic.Should().BeOneOf("MichaelisMenten", "Hill", "Logarithmic");
            result.Observations.Should().Be(12);
        }

        [Fact]
        [Trait("Covers", "CompareNonlinearCurveFamilies")]
        public void ComparisonReportsFamiliesThatCannotBeFittedAndValidates()
        {
            var x = new[] { 0.0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var y = x.Select(v => 3 * Math.Exp(0.3 * v) + Math.Sin(v * 3) * 0.05).ToArray();

            var all = tools.CompareNonlinearCurveFamilies(x, y);
            all.Families.Should().HaveCount(12);
            all.Families.Single(f => f.Family == "Power").Fitted.Should().BeFalse();
            all.Families.Single(f => f.Family == "Power").Note.Should().Contain("greater than 0");
            all.Families.Single(f => f.Family == "Power").Rank.Should().Be(0);
            all.BestByAic.Should().BeOneOf("Exponential", "Gompertz", "Logistic3", "Logistic4");
            all.Families.Single(f => f.Family == "Exponential").Parameters[1].Should().BeApproximately(0.3, 0.01);

            RelationshipsFixtures.Rejects(
                () => tools.CompareNonlinearCurveFamilies(x, y, [NonlinearCurveFamily.Power]), "families");

            RelationshipsFixtures.Rejects(() => tools.CompareNonlinearCurveFamilies(x, [.. y.Take(3)]), "y");

            RelationshipsFixtures.Rejects(
                () => tools.CompareNonlinearCurveFamilies([1.0, 2, 3], [1.0, 2, 3]), "x");

            RelationshipsFixtures.Rejects(
                () => tools.CompareNonlinearCurveFamilies(x, y, [(NonlinearCurveFamily)77]), "family");
        }
    }
}