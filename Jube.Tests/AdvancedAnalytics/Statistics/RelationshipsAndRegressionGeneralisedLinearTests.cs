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
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class RelationshipsAndRegressionGeneralisedLinearTests
    {
        private static readonly double[] dobsonCounts = [18, 17, 15, 20, 10, 20, 25, 13, 12];
        private static readonly double[] clottingU = [5, 10, 15, 20, 30, 40, 60, 80, 100];
        private static readonly double[] clottingTime = [118, 58, 42, 35, 27, 25, 21, 19, 18];

        private static readonly double[] beetleDose =
            [1.6907, 1.7242, 1.7552, 1.7842, 1.8113, 1.8369, 1.8610, 1.8839];

        private static readonly int[] beetleTotal = [59, 60, 62, 56, 63, 59, 62, 60];
        private static readonly int[] beetleKilled = [6, 13, 18, 28, 52, 53, 61, 60];
        private static readonly string[] dobsonNames = ["outcome2", "outcome3", "treatment2", "treatment3"];
        private readonly GeneralisedLinearModelTools tools = new();

        private static double[][] DobsonRows()
        {
            return
            [
                .. Enumerable.Range(0, 9).Select(i => new[]
                {
                    i % 3 == 1 ? 1.0 : 0.0, i % 3 == 2 ? 1.0 : 0.0, i / 3 == 1 ? 1.0 : 0.0, i / 3 == 2 ? 1.0 : 0.0
                })
            ];
        }

        private static (double[][] Rows, double[] Outcome) Beetles()
        {
            var rows = new List<double[]>();
            var outcome = new List<double>();

            for (var g = 0; g < 8; g++)
            {
                for (var i = 0; i < beetleTotal[g]; i++)
                {
                    rows.Add([beetleDose[g]]);
                    outcome.Add(i < beetleKilled[g] ? 1 : 0);
                }
            }

            return ([.. rows], [.. outcome]);
        }

        [Fact]
        [Trait("Covers", "FitGeneralisedLinearModel")]
        public void PoissonMatchesTheDobsonRandomisedTrialFromTheRGlmDocumentation()
        {
            var result =
                tools.FitGeneralisedLinearModel(DobsonRows(), dobsonCounts, GlmFamily.Poisson, null, dobsonNames);

            result.Family.Should().Be("Poisson");
            result.Link.Should().Be("Log");
            result.Converged.Should().BeTrue();
            var expected = new[] { 3.044522, -0.4542553, -0.2929871, 0, 0 };
            var expectedSe = new[] { 0.1708987, 0.2021708, 0.1927423, 0.2, 0.2 };

            for (var j = 0; j < 5; j++)
            {
                result.Coefficients[j].Estimate.Should().BeApproximately(expected[j], 1e-5);
                result.Coefficients[j].StandardError.Should().BeApproximately(expectedSe[j], 1e-5);
            }

            result.Coefficients[1].ExponentiatedEstimate.Should().BeApproximately(Math.Exp(-0.4542553), 1e-5);
            result.Coefficients[0].Term.Should().Be("(Intercept)");
            result.Deviance.Should().BeApproximately(5.129141, 1e-5);
            result.NullDeviance.Should().BeApproximately(10.58145, 1e-4);
            result.ResidualDegreesOfFreedom.Should().Be(4);
            result.NullDegreesOfFreedom.Should().Be(8);
            result.Aic.Should().BeApproximately(56.76132, 1e-4);
            result.Dispersion.Should().Be(1);
            result.PseudoRSquared.Should().BeApproximately(1 - 5.129141 / 10.58145, 1e-5);
            result.LikelihoodRatioStatistic.Should().BeApproximately(10.58145 - 5.129141, 1e-4);

            result.LikelihoodRatioPValue.Should()
                .BeApproximately(Math.Exp(-(10.58145 - 5.129141) / 2) * (1 + (10.58145 - 5.129141) / 2), 1e-4);
        }

        [Fact]
        [Trait("Covers", "FitGeneralisedLinearModel")]
        public void GammaWithInverseLinkMatchesTheMcCullaghNelderClottingData()
        {
            var rows = clottingU.Select(u => new[] { Math.Log(u) }).ToArray();

            var result = tools.FitGeneralisedLinearModel(rows, clottingTime, GlmFamily.Gamma, GlmLink.Inverse,
                ["logU"]);

            result.Coefficients[0].Estimate.Should().BeApproximately(-0.01655438, 1e-7);
            result.Coefficients[1].Estimate.Should().BeApproximately(0.01534311, 1e-7);
            result.Coefficients[0].StandardError.Should().BeApproximately(0.0009275, 1e-6);
            result.Coefficients[1].StandardError.Should().BeApproximately(0.0004150, 1e-6);
            result.Dispersion.Should().BeApproximately(0.002446059, 1e-7);
            result.Deviance.Should().BeApproximately(0.01673, 1e-5);
            result.NullDeviance.Should().BeApproximately(3.51283, 1e-4);
            result.Aic.Should().BeApproximately(37.99, 0.01);
            result.ResidualDegreesOfFreedom.Should().Be(7);
            result.Coefficients[1].PValue.Should().BeLessThan(1e-5);
        }

        [Fact]
        [Trait("Covers", "FitGeneralisedLinearModel")]
        public void GaussianIdentityEqualsOrdinaryLeastSquares()
        {
            var rows = RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;
            var ols = new LinearRegressionTools().FitMultipleLinearRegression(rows, y);

            var glm = tools.FitGeneralisedLinearModel(rows, y, GlmFamily.Gaussian);

            glm.Link.Should().Be("Identity");
            glm.Coefficients[1].Estimate.Should().BeApproximately(ols.Coefficients[1].Estimate, 1e-9);
            glm.Coefficients[1].StandardError.Should().BeApproximately(ols.Coefficients[1].StandardError, 1e-9);
            glm.Coefficients[1].PValue.Should().BeApproximately(ols.Coefficients[1].PValue, 1e-9);
            glm.Deviance.Should().BeApproximately(ols.ResidualSumOfSquares, 1e-9);
            glm.Dispersion.Should().BeApproximately(ols.ResidualStandardError * ols.ResidualStandardError, 1e-9);
            glm.LogLikelihood.Should().BeApproximately(ols.LogLikelihood, 1e-9);
            glm.Aic.Should().BeApproximately(ols.Aic, 1e-9);
            glm.Bic.Should().BeApproximately(ols.Bic, 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitGeneralisedLinearModel")]
        public void BinomialLogitMatchesTheBlissBeetleCoefficients()
        {
            var (rows, outcome) = Beetles();

            var result = tools.FitGeneralisedLinearModel(rows, outcome, GlmFamily.Binomial, null, ["dose"]);

            result.Link.Should().Be("Logit");
            result.Coefficients[0].Estimate.Should().BeApproximately(-60.72, 0.01);
            result.Coefficients[1].Estimate.Should().BeApproximately(34.27, 0.01);
            result.Coefficients[0].StandardError.Should().BeApproximately(5.181, 0.005);
            result.Coefficients[1].StandardError.Should().BeApproximately(2.912, 0.005);
            result.Observations.Should().Be(481);
            result.Coefficients[1].ExponentiatedEstimate.Should().BeGreaterThan(1e10);
        }

        [Fact]
        [Trait("Covers", "FitGeneralisedLinearModel")]
        public void GlmOptionsOffsetsWeightsDispersionAndValidation()
        {
            var rows = DobsonRows();

            var plain = tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Poisson, null, dobsonNames);

            var offset = tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Poisson, null, dobsonNames, null,
                [.. Enumerable.Repeat(Math.Log(2), 9)]);

            offset.Coefficients[0].Estimate.Should()
                .BeApproximately(plain.Coefficients[0].Estimate - Math.Log(2), 1e-8);

            offset.Coefficients[1].Estimate.Should().BeApproximately(plain.Coefficients[1].Estimate, 1e-8);
            offset.Deviance.Should().BeApproximately(plain.Deviance, 1e-8);

            var quasi = tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Poisson, null, dobsonNames, null,
                null, true);

            var dispersion = quasi.PearsonChiSquare / 4;
            quasi.Dispersion.Should().BeApproximately(dispersion, 1e-9);

            quasi.Coefficients[1].StandardError.Should()
                .BeApproximately(plain.Coefficients[1].StandardError * Math.Sqrt(dispersion), 1e-9);

            quasi.Coefficients[1].Estimate.Should().BeApproximately(plain.Coefficients[1].Estimate, 1e-9);

            var doubled = tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Poisson, null, dobsonNames,
                [.. Enumerable.Repeat(1.0, 9)]);

            doubled.Deviance.Should().BeApproximately(plain.Deviance, 1e-9);

            var series = tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Poisson, null, dobsonNames, null,
                null, false, 0.9, 100, true);
            series.FittedMeans.Should().HaveCount(9);
            series.FittedMeans![0].Should().BeApproximately(Math.Exp(3.044522), 1e-4);
            series.DevianceResiduals!.Select(r => r * r).Sum().Should().BeApproximately(5.129141, 1e-5);

            RelationshipsFixtures.Rejects(
                () => tools.FitGeneralisedLinearModel(rows, [.. dobsonCounts.Take(4)], GlmFamily.Poisson),
                "outputs");

            RelationshipsFixtures.Rejects(() => tools.FitGeneralisedLinearModel(rows, dobsonCounts, (GlmFamily)9),
                "family");

            RelationshipsFixtures.Rejects(
                () => tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Poisson, (GlmLink)9), "link");

            RelationshipsFixtures.Rejects(
                () => tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Poisson, null, null, null, null,
                    false, 2), "confidenceLevel");

            RelationshipsFixtures.Rejects(
                () => tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Poisson, null, null, null, null,
                    false, 0.95, 2), "maximumIterations");

            RelationshipsFixtures.Rejects(
                () => tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Poisson, null, null, [1.0]),
                "weights");

            RelationshipsFixtures.Rejects(
                () => tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Poisson, null, null, null,
                    [1.0]), "offset");

            RelationshipsFixtures.Rejects(
                () => tools.FitGeneralisedLinearModel(rows, [.. dobsonCounts.Select(v => -v)], GlmFamily.Poisson),
                "outputs");

            RelationshipsFixtures.Rejects(() => tools.FitGeneralisedLinearModel(rows, dobsonCounts, GlmFamily.Binomial),
                "outputs");

            RelationshipsFixtures.Rejects(
                () => tools.FitGeneralisedLinearModel(rows, [.. dobsonCounts.Select(v => v - 10)],
                    GlmFamily.Gamma), "outputs");

            RelationshipsFixtures.Rejects(
                () => tools.FitGeneralisedLinearModel([.. rows.Take(4)], [.. dobsonCounts.Take(4)],
                    GlmFamily.Poisson), "inputs");

            var gap = dobsonCounts.ToArray();
            gap[2] = double.NaN;

            RelationshipsFixtures.Rejects(() => tools.FitGeneralisedLinearModel(rows, gap, GlmFamily.Poisson),
                "inputs");

            tools.FitGeneralisedLinearModel(rows, gap, GlmFamily.Poisson, null, null, null, null, false, 0.95, 100,
                false, true).RowsDropped.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "FitGeneralisedLinearModel")]
        public void PerfectSeparationIsReportedNotHidden()
        {
            var rows = Enumerable.Range(0, 20).Select(i => new[] { (double)i }).ToArray();
            var outcome = Enumerable.Range(0, 20).Select(i => i < 10 ? 0.0 : 1.0).ToArray();

            var result = tools.FitGeneralisedLinearModel(rows, outcome, GlmFamily.Binomial);

            result.Warnings.Should().Contain(w => w.Contains("perfectly") || w.Contains("converge"));
            result.Deviance.Should().BeLessThan(1);
        }

        [Fact]
        [Trait("Covers", "FitPoissonRegression")]
        public void PoissonRegressionMatchesTheDobsonFitAndHandlesExposure()
        {
            var rows = DobsonRows();

            var result = tools.FitPoissonRegression(rows, dobsonCounts, dobsonNames);

            result.Coefficients[0].Estimate.Should().BeApproximately(3.044522, 1e-5);
            result.Coefficients[2].ExponentiatedEstimate.Should().BeApproximately(Math.Exp(-0.2929871), 1e-5);
            result.Deviance.Should().BeApproximately(5.129141, 1e-5);
            result.Family.Should().Be("Poisson");

            var exposure =
                tools.FitPoissonRegression(rows, dobsonCounts, dobsonNames, [.. Enumerable.Repeat(4.0, 9)]);

            exposure.Coefficients[0].Estimate.Should().BeApproximately(3.044522 - Math.Log(4), 1e-5);
            exposure.Coefficients[1].Estimate.Should().BeApproximately(-0.4542553, 1e-5);

            RelationshipsFixtures.Rejects(
                () => tools.FitPoissonRegression(rows, dobsonCounts, dobsonNames, [.. Enumerable.Repeat(0.0, 9)]),
                "exposure");

            RelationshipsFixtures.Rejects(
                () => tools.FitPoissonRegression(rows, dobsonCounts, dobsonNames, [1.0, 2]), "exposure");

            RelationshipsFixtures.Rejects(
                () => tools.FitPoissonRegression(rows, [.. dobsonCounts.Select(_ => -1.0)]), "counts");

            RelationshipsFixtures.Rejects(() => tools.FitPoissonRegression(rows, [.. dobsonCounts.Take(3)]),
                "counts");

            var over = Enumerable.Range(0, 40).Select(i => new[] { i / 10.0 }).ToArray();

            var counts = Enumerable.Range(0, 40)
                .Select(i => Math.Round(Math.Exp(0.5 + 0.2 * i / 10.0) * (i % 2 == 0 ? 0.2 : 1.8))).ToArray();

            var flagged = tools.FitPoissonRegression(over, counts);
            flagged.Dispersion.Should().Be(1);
            flagged.PearsonChiSquare.Should().BeGreaterThan(38 * 1.5);
            flagged.Warnings.Should().Contain(w => w.Contains("overdispersed"));
        }

        [Fact]
        [Trait("Covers", "FitGammaRegression")]
        public void GammaRegressionMatchesTheClottingDataAndLogLinkIsPositive()
        {
            var rows = clottingU.Select(u => new[] { Math.Log(u) }).ToArray();

            var inverse = tools.FitGammaRegression(rows, clottingTime, GlmLink.Inverse);
            inverse.Coefficients[1].Estimate.Should().BeApproximately(0.01534311, 1e-7);
            inverse.Dispersion.Should().BeApproximately(0.002446059, 1e-7);

            var log = tools.FitGammaRegression(rows, clottingTime);
            log.Link.Should().Be("Log");
            log.Coefficients[1].Estimate.Should().BeLessThan(0);
            log.Coefficients[1].ExponentiatedEstimate.Should().BeLessThan(1);
            log.Deviance.Should().BeGreaterThan(0);

            RelationshipsFixtures.Rejects(
                () => tools.FitGammaRegression(rows, [.. clottingTime.Select(v => v - 18)]), "outputs");

            RelationshipsFixtures.Rejects(() => tools.FitGammaRegression(rows, [.. clottingTime.Take(4)]),
                "outputs");

            RelationshipsFixtures.Rejects(() => tools.FitGammaRegression(rows, clottingTime, (GlmLink)12), "link");
        }

        [Fact]
        [Trait("Covers", "FitProbitRegression")]
        public void ProbitMatchesTheBlissBeetleCoefficientsAndTheGenericFit()
        {
            var (rows, outcome) = Beetles();

            var result = tools.FitProbitRegression(rows, outcome, ["dose"]);

            result.Link.Should().Be("Probit");
            result.Coefficients[0].Estimate.Should().BeApproximately(-34.94, 0.01);
            result.Coefficients[1].Estimate.Should().BeApproximately(19.73, 0.01);
            result.Coefficients[0].StandardError.Should().BeApproximately(2.648, 0.005);
            result.Coefficients[1].StandardError.Should().BeApproximately(1.487, 0.005);

            var generic =
                tools.FitGeneralisedLinearModel(rows, outcome, GlmFamily.Binomial, GlmLink.Probit, ["dose"]);

            result.Deviance.Should().BeApproximately(generic.Deviance, 1e-9);
            result.Aic.Should().BeApproximately(generic.Aic, 1e-9);

            RelationshipsFixtures.Rejects(() => tools.FitProbitRegression(rows, [.. outcome.Select(v => v * 2)]),
                "outcomes");

            RelationshipsFixtures.Rejects(() => tools.FitProbitRegression(rows, [.. outcome.Take(10)]),
                "outcomes");
        }

        [Fact]
        [Trait("Covers", "FitComplementaryLogLogRegression")]
        public void ComplementaryLogLogMatchesTheBlissBeetleCoefficients()
        {
            var (rows, outcome) = Beetles();

            var result = tools.FitComplementaryLogLogRegression(rows, outcome, ["dose"]);

            result.Link.Should().Be("ComplementaryLogLog");
            result.Coefficients[0].Estimate.Should().BeApproximately(-39.57, 0.01);
            result.Coefficients[1].Estimate.Should().BeApproximately(22.04, 0.01);
            result.Coefficients[0].StandardError.Should().BeApproximately(3.24, 0.02);
            result.Coefficients[1].StandardError.Should().BeApproximately(1.79, 0.02);

            var information = new double[2, 2];

            for (var g = 0; g < 8; g++)
            {
                var eta = result.Coefficients[0].Estimate + result.Coefficients[1].Estimate * beetleDose[g];
                var mu = 1 - Math.Exp(-Math.Exp(eta));
                var slope = Math.Exp(eta - Math.Exp(eta));
                var weight = beetleTotal[g] * slope * slope / (mu * (1 - mu));
                information[0, 0] += weight;
                information[0, 1] += weight * beetleDose[g];
                information[1, 0] += weight * beetleDose[g];
                information[1, 1] += weight * beetleDose[g] * beetleDose[g];
            }

            var determinant = information[0, 0] * information[1, 1] - information[0, 1] * information[1, 0];

            result.Coefficients[0].StandardError.Should()
                .BeApproximately(Math.Sqrt(information[1, 1] / determinant), 1e-6);
            result.Coefficients[1].StandardError.Should()
                .BeApproximately(Math.Sqrt(information[0, 0] / determinant), 1e-6);

            var probit = tools.FitProbitRegression(rows, outcome, ["dose"]);
            result.Deviance.Should().BeLessThan(probit.Deviance);

            RelationshipsFixtures.Rejects(
                () => tools.FitComplementaryLogLogRegression(rows, [.. outcome.Select(v => v + 0.5)]), "outcomes");

            RelationshipsFixtures.Rejects(
                () => tools.FitComplementaryLogLogRegression([], []), "inputs");
        }

        [Fact]
        [Trait("Covers", "PredictWithGeneralisedLinearModel")]
        public void PredictionUsesTheInverseLinkAndTheLinkScaleInterval()
        {
            var rows = DobsonRows();

            var result = tools.PredictWithGeneralisedLinearModel(rows, dobsonCounts,
                [[0.0, 0, 0, 0], [1.0, 0, 1, 0]], GlmFamily.Poisson, null, dobsonNames);

            result.Predictions[0].LinearPredictor.Should().BeApproximately(3.044522, 1e-5);
            result.Predictions[0].Mean.Should().BeApproximately(Math.Exp(3.044522), 1e-4);
            result.Predictions[0].LinearPredictorStandardError.Should().BeApproximately(0.1708987, 1e-5);
            result.Predictions[0].MeanLower.Should().BeApproximately(Math.Exp(3.044522 - 1.959964 * 0.1708987), 1e-3);
            result.Predictions[0].MeanUpper.Should().BeApproximately(Math.Exp(3.044522 + 1.959964 * 0.1708987), 1e-3);
            result.Predictions[1].LinearPredictor.Should().BeApproximately(3.044522 - 0.4542553 + 1.3e-15, 1e-5);
            result.Family.Should().Be("Poisson");
            result.Link.Should().Be("Log");
            result.Converged.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "PredictWithGeneralisedLinearModel")]
        public void PredictionKeepsProbabilitiesInRangeAndValidatesOffsets()
        {
            var (rows, outcome) = Beetles();

            var result = tools.PredictWithGeneralisedLinearModel(rows, outcome,
                [[1.60], [1.78], [1.95]], GlmFamily.Binomial, GlmLink.Probit);

            result.Predictions.Select(p => p.Mean).Should().BeInAscendingOrder();

            result.Predictions.Should().OnlyContain(p =>
                p.MeanLower >= 0 && p.MeanUpper <= 1 && p.MeanLower <= p.Mean && p.Mean <= p.MeanUpper);

            result.Predictions[0].Mean.Should().BeLessThan(0.01);
            result.Predictions[2].Mean.Should().BeGreaterThan(0.99);

            var counts = DobsonRows();

            RelationshipsFixtures.Rejects(
                () => tools.PredictWithGeneralisedLinearModel(counts, dobsonCounts, null!, GlmFamily.Poisson),
                "newInputs");

            RelationshipsFixtures.Rejects(
                () => tools.PredictWithGeneralisedLinearModel(counts, dobsonCounts, [[1.0]],
                    GlmFamily.Poisson), "newInputs");

            RelationshipsFixtures.Rejects(
                () => tools.PredictWithGeneralisedLinearModel(counts, dobsonCounts,
                    [[1.0, 0, 0, double.NaN]], GlmFamily.Poisson), "newInputs");

            RelationshipsFixtures.Rejects(
                () => tools.PredictWithGeneralisedLinearModel(counts, dobsonCounts, [[1.0, 0, 0, 0]],
                    GlmFamily.Poisson, null, null, null, [.. Enumerable.Repeat(0.0, 9)]), "newOffset");

            RelationshipsFixtures.Rejects(
                () => tools.PredictWithGeneralisedLinearModel(counts, dobsonCounts, [[1.0, 0, 0, 0]],
                    GlmFamily.Poisson, null, null, null, null, [0.0, 1]), "newOffset");

            RelationshipsFixtures.Rejects(
                () => tools.PredictWithGeneralisedLinearModel(counts, dobsonCounts, [[1.0, 0, 0, 0]],
                    GlmFamily.Poisson, null, null, null, null, null, false, 4), "confidenceLevel");

            var withOffset = tools.PredictWithGeneralisedLinearModel(counts, dobsonCounts,
                [[0.0, 0, 0, 0]], GlmFamily.Poisson, null, null, null,
                [.. Enumerable.Repeat(Math.Log(2), 9)], [Math.Log(2)]);

            withOffset.Predictions[0].Mean.Should().BeApproximately(Math.Exp(3.044522), 1e-4);
        }

        [Fact]
        [Trait("Covers", "ApplyLinkFunction")]
        public void LinkFunctionsMatchTheirClosedForms()
        {
            var logit = tools.ApplyLinkFunction([0.5, 0.8807971], GlmLink.Logit);
            logit.Values[0].Should().BeApproximately(0, 1e-12);
            logit.Values[1].Should().BeApproximately(2, 1e-6);
            logit.Direction.Should().Contain("Link");

            var back = tools.ApplyLinkFunction([0.0, 2], GlmLink.Logit, true);
            back.Values[0].Should().BeApproximately(0.5, 1e-12);
            back.Values[1].Should().BeApproximately(1 / (1 + Math.Exp(-2)), 1e-12);

            tools.ApplyLinkFunction([0.975], GlmLink.Probit).Values[0].Should().BeApproximately(1.959964, 1e-5);

            tools.ApplyLinkFunction([1.959964], GlmLink.Probit, true).Values[0].Should()
                .BeApproximately(0.975, 1e-6);

            tools.ApplyLinkFunction([1 - Math.Exp(-1)], GlmLink.ComplementaryLogLog).Values[0].Should()
                .BeApproximately(0, 1e-12);

            tools.ApplyLinkFunction([0.0], GlmLink.ComplementaryLogLog, true).Values[0].Should()
                .BeApproximately(1 - Math.Exp(-1), 1e-12);

            tools.ApplyLinkFunction([Math.E], GlmLink.Log).Values[0].Should().BeApproximately(1, 1e-12);
            tools.ApplyLinkFunction([1.0], GlmLink.Log, true).Values[0].Should().BeApproximately(Math.E, 1e-12);
            tools.ApplyLinkFunction([4.0], GlmLink.Inverse).Values[0].Should().BeApproximately(0.25, 1e-12);
            tools.ApplyLinkFunction([7.5], GlmLink.Identity).Values[0].Should().Be(7.5);

            tools.ApplyLinkFunction([0.75], GlmLink.Cauchit).Values[0].Should()
                .BeApproximately(Math.Tan(Math.PI * 0.25), 1e-9);

            tools.ApplyLinkFunction([1.0], GlmLink.Cauchit, true).Values[0].Should()
                .BeApproximately(Math.Atan(1) / Math.PI + 0.5, 1e-12);
        }

        [Fact]
        [Trait("Covers", "ApplyLinkFunction")]
        public void LinkFunctionsFlagValuesOutsideTheirDomain()
        {
            var result = tools.ApplyLinkFunction([0.5, 1.5, 0, -1, double.NaN, double.PositiveInfinity],
                GlmLink.Logit);

            result.Values[0].Should().BeApproximately(0, 1e-12);
            result.Values.Skip(1).Should().OnlyContain(v => double.IsNaN(v));
            result.InvalidCount.Should().Be(5);

            tools.ApplyLinkFunction([-1.0, 0], GlmLink.Log).InvalidCount.Should().Be(2);
            tools.ApplyLinkFunction([0.0], GlmLink.Inverse).InvalidCount.Should().Be(1);

            tools.ApplyLinkFunction([1000.0], GlmLink.ComplementaryLogLog, true).Values[0].Should()
                .BeApproximately(1, 1e-12);

            tools.ApplyLinkFunction([double.NaN], GlmLink.Identity, true).InvalidCount.Should().Be(1);
            RelationshipsFixtures.Rejects(() => tools.ApplyLinkFunction([], GlmLink.Log), "values");
            RelationshipsFixtures.Rejects(() => tools.ApplyLinkFunction([1.0], (GlmLink)8), "link");
        }
    }
}