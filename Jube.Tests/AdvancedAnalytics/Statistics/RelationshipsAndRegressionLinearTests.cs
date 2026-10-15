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
    public class RelationshipsAndRegressionLinearTests
    {
        private static readonly double[] longleyEstimates =
        [
            -3482258.63459582, 15.0618722713733, -0.0358191792925910, -2.02022980381683, -1.03322686717359,
            -0.0511041056535807, 1829.15146461355
        ];

        private static readonly double[] longleyStandardErrors =
        [
            890420.383607373, 84.9149257747669, 0.0334910077722432, 0.488399681651699, 0.214274163161675,
            0.226073200069370, 455.478499142212
        ];

        private readonly RegularisedAndRobustRegressionTools robust = new();
        private readonly LinearRegressionTools tools = new();

        private static void Close(double actual, double expected, double relative)
        {
            actual.Should().BeApproximately(expected, Math.Abs(expected) * relative);
        }

        private static (double Slope, double Intercept, double Sxx, double Sse, double MeanX) OrdinaryClosedForm(
            double[] x, double[] y)
        {
            var mx = x.Average();
            var my = y.Average();
            var sxx = x.Sum(v => (v - mx) * (v - mx));
            var sxy = x.Zip(y, (a, b) => (a - mx) * (b - my)).Sum();
            var slope = sxy / sxx;
            var intercept = my - slope * mx;
            var sse = x.Zip(y, (a, b) => Math.Pow(b - intercept - slope * a, 2)).Sum();

            return (slope, intercept, sxx, sse, mx);
        }

        [Fact]
        [Trait("Covers", "FitSimpleLinearRegression")]
        public void SimpleRegressionMatchesTheClosedFormAndPublishedAnscombeFigures()
        {
            var x = RelationshipsFixtures.AnscombeOneX;
            var y = RelationshipsFixtures.AnscombeOneY;
            var (slope, intercept, sxx, sse, _) = OrdinaryClosedForm(x, y);
            var mse = sse / 9;

            var result = tools.FitSimpleLinearRegression(x, y);

            result.Coefficients[0].Term.Should().Be("(Intercept)");
            result.Coefficients[1].Term.Should().Be("x");
            result.Coefficients[0].Estimate.Should().BeApproximately(intercept, 1e-10);
            result.Coefficients[1].Estimate.Should().BeApproximately(slope, 1e-10);
            result.Coefficients[1].StandardError.Should().BeApproximately(Math.Sqrt(mse / sxx), 1e-10);
            result.Coefficients[0].StandardError.Should().BeApproximately(Math.Sqrt(mse * (1d / 11 + 81 / sxx)), 1e-10);
            result.Coefficients[1].StandardisedEstimate.Should().BeApproximately(0.8164205, 1e-6);
            result.Coefficients[1].PValue.Should().BeApproximately(0.00217, 1e-5);
            result.Coefficients[0].PValue.Should().BeApproximately(0.02573, 1e-4);

            result.Coefficients[1].ConfidenceLower.Should()
                .BeApproximately(slope - 2.262157 * Math.Sqrt(mse / sxx), 1e-5);

            result.ResidualStandardError.Should().BeApproximately(1.237, 1e-3);
            result.RSquared.Should().BeApproximately(0.6665, 1e-4);
            result.AdjustedRSquared.Should().BeApproximately(0.6295, 1e-4);
            result.FStatistic.Should().BeApproximately(17.99, 1e-2);
            result.FpValue.Should().BeApproximately(0.00217, 1e-5);
            result.FDegreesOfFreedomModel.Should().Be(1);
            result.FDegreesOfFreedomResidual.Should().Be(9);
            result.Observations.Should().Be(11);
            result.DurbinWatson.Should().BeInRange(0.5, 4);
            var logLikelihood = -11d / 2 * (Math.Log(2 * Math.PI) + Math.Log(sse / 11) + 1);
            result.LogLikelihood.Should().BeApproximately(logLikelihood, 1e-9);
            result.Aic.Should().BeApproximately(-2 * logLikelihood + 6, 1e-9);
            result.Bic.Should().BeApproximately(-2 * logLikelihood + 3 * Math.Log(11), 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitSimpleLinearRegression")]
        public void SimpleRegressionOptionsAndEdgeCases()
        {
            var x = RelationshipsFixtures.AnscombeOneX;
            var y = RelationshipsFixtures.AnscombeOneY;

            var withSeries = tools.FitSimpleLinearRegression(x, y, true, 0.9, [10.0, 100], true);
            withSeries.FittedValues.Should().HaveCount(11);
            withSeries.Residuals!.Sum().Should().BeApproximately(0, 1e-9);
            withSeries.Predictions.Should().HaveCount(2);
            withSeries.ConfidenceLevel.Should().Be(0.9);

            var origin = tools.FitSimpleLinearRegression(x, y, false);
            origin.Coefficients.Should().HaveCount(1);

            origin.Coefficients[0].Estimate.Should()
                .BeApproximately(x.Zip(y, (a, b) => a * b).Sum() / x.Sum(v => v * v), 1e-10);

            origin.Warnings.Should().Contain(w => w.Contains("No intercept"));

            RelationshipsFixtures.Rejects(() => tools.FitSimpleLinearRegression([1.0, 2, 3], [1.0, 2]),
                "y");

            RelationshipsFixtures.Rejects(() => tools.FitSimpleLinearRegression([1.0, 2], [1.0, 2]),
                "x");

            RelationshipsFixtures.Rejects(
                () => tools.FitSimpleLinearRegression([2.0, 2, 2, 2], [1.0, 2, 3, 4]), "inputs");

            RelationshipsFixtures.Rejects(
                () => tools.FitSimpleLinearRegression([1.0, 2, double.NaN, 4], [1.0, 2, 3, 4]), "x");

            RelationshipsFixtures.Rejects(() => tools.FitSimpleLinearRegression(x, y, true, 0), "confidenceLevel");

            RelationshipsFixtures.Rejects(() => tools.FitSimpleLinearRegression(x, y, true, 0.95, [double.NaN]),
                "predictAt");

            var dropped = tools.FitSimpleLinearRegression([1.0, 2, double.NaN, 4, 5],
                [1.0, 2.1, 3, 4.2, 4.9], true, 0.95, null, false, true);
            dropped.Observations.Should().Be(4);
            dropped.RowsDropped.Should().Be(1);

            var constantY = tools.FitSimpleLinearRegression([1.0, 2, 3, 4], [5.0, 5, 5, 5]);
            double.IsNaN(constantY.RSquared).Should().BeTrue();
            constantY.Coefficients[1].Estimate.Should().BeApproximately(0, 1e-12);

            var large = tools.FitSimpleLinearRegression([1e9, 2e9, 3e9, 4e9, 5e9],
                [2.1e9, 3.9e9, 6.2e9, 7.8e9, 10.1e9]);

            large.Coefficients[1].Estimate.Should().BeApproximately(2.0, 0.1);
        }

        [Fact]
        [Trait("Covers", "FitMultipleLinearRegression")]
        public void MultipleRegressionReproducesTheNistLongleyCertifiedValues()
        {
            var result = tools.FitMultipleLinearRegression(RelationshipsFixtures.LongleyPredictors,
                RelationshipsFixtures.LongleyEmployed, RelationshipsFixtures.LongleyNames);

            for (var i = 0; i < 7; i++)
            {
                Close(result.Coefficients[i].Estimate, longleyEstimates[i], 1e-6);
                Close(result.Coefficients[i].StandardError, longleyStandardErrors[i], 1e-6);
            }

            Close(result.RSquared, 0.995479004577296, 1e-9);
            Close(result.ResidualStandardError, 304.854073561965, 1e-7);
            Close(result.FStatistic, 330.2853392346658, 1e-6);
            result.FpValue.Should().BeLessThan(1e-8);
            result.AdjustedRSquared.Should().BeApproximately(1 - (1 - 0.995479004577296) * 15d / 9d, 1e-9);
            result.ResidualDegreesOfFreedom.Should().Be(9);
            result.Coefficients[1].Term.Should().Be("deflator");
        }

        [Fact]
        [Trait("Covers", "FitMultipleLinearRegression")]
        public void MultipleRegressionReportsVarianceInflationForCollinearPredictors()
        {
            var result = tools.FitMultipleLinearRegression(RelationshipsFixtures.LongleyPredictors,
                RelationshipsFixtures.LongleyEmployed, RelationshipsFixtures.LongleyNames);

            var vif = result.Coefficients.Skip(1).Select(c => c.VarianceInflationFactor).ToArray();
            Close(vif[0], 135.532, 1e-3);
            Close(vif[1], 1788.513, 1e-3);
            Close(vif[2], 33.619, 1e-3);
            Close(vif[3], 3.589, 2e-3);
            Close(vif[4], 399.151, 1e-3);
            Close(vif[5], 758.981, 1e-3);
            result.Warnings.Should().Contain(w => w.Contains("collinear"));
            double.IsNaN(result.Coefficients[0].VarianceInflationFactor).Should().BeTrue();
            result.Coefficients[0].StandardisedEstimate.Should().Be(double.NaN);
        }

        [Fact]
        [Trait("Covers", "FitMultipleLinearRegression")]
        public void MultipleRegressionValidatesItsInput()
        {
            var x = RelationshipsFixtures.AnscombeOneX.Select((v, i) => new[] { v, Math.Cos(i) * 3 }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;

            RelationshipsFixtures.Rejects(() => tools.FitMultipleLinearRegression(x, [.. y.Take(5)]), "outputs");

            RelationshipsFixtures.Rejects(
                () => tools.FitMultipleLinearRegression([], []), "inputs");

            RelationshipsFixtures.Rejects(() => tools.FitMultipleLinearRegression(x, y, ["a"]),
                "variableNames");

            RelationshipsFixtures.Rejects(() => tools.FitMultipleLinearRegression(x, y, ["a", "a"]),
                "variableNames");

            RelationshipsFixtures.Rejects(
                () => tools.FitMultipleLinearRegression([.. x.Take(3)], [.. y.Take(3)]), "inputs");

            var collinear = x.Select(r => new[] { r[0], r[1], 2 * r[0] + 1 }).ToArray();
            RelationshipsFixtures.Rejects(() => tools.FitMultipleLinearRegression(collinear, y), "inputs");
            var constant = x.Select(r => new[] { r[0], 7.0 }).ToArray();
            RelationshipsFixtures.Rejects(() => tools.FitMultipleLinearRegression(constant, y), "inputs");
            var zeros = x.Select(r => new[] { r[0], 0.0 }).ToArray();
            RelationshipsFixtures.Rejects(() => tools.FitMultipleLinearRegression(zeros, y, null, false), "inputs");

            var ragged = x.ToArray();
            ragged[2] = [1.0];
            RelationshipsFixtures.Rejects(() => tools.FitMultipleLinearRegression(ragged, y), "inputs");

            var infinite = x.Select(r => (double[])r.Clone()).ToArray();
            infinite[1][0] = double.PositiveInfinity;
            RelationshipsFixtures.Rejects(() => tools.FitMultipleLinearRegression(infinite, y), "inputs");

            var gap = x.Select(r => (double[])r.Clone()).ToArray();
            gap[4][1] = double.NaN;
            RelationshipsFixtures.Rejects(() => tools.FitMultipleLinearRegression(gap, y), "inputs");
            var repaired = tools.FitMultipleLinearRegression(gap, y, null, true, 0.95, false, true);
            repaired.Observations.Should().Be(10);
            repaired.RowsDropped.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "FitWeightedLinearRegression")]
        public void WeightedRegressionMatchesTheWeightedClosedForm()
        {
            var x = new[] { 1.0, 2, 3, 4, 5 };
            var y = new[] { 2.1, 3.9, 6.2, 7.8, 10.1 };
            var w = new[] { 1.0, 2, 3, 2, 1 };
            var sw = w.Sum();
            var mx = x.Zip(w, (a, b) => a * b).Sum() / sw;
            var my = y.Zip(w, (a, b) => a * b).Sum() / sw;
            var sxx = x.Select((v, i) => w[i] * (v - mx) * (v - mx)).Sum();
            var sxy = x.Select((v, i) => w[i] * (v - mx) * (y[i] - my)).Sum();
            var slope = sxy / sxx;
            var intercept = my - slope * mx;
            var wss = x.Select((v, i) => w[i] * Math.Pow(y[i] - intercept - slope * v, 2)).Sum();

            var result = tools.FitWeightedLinearRegression([.. x.Select(v => new[] { v })], y, w);

            result.Coefficients[1].Estimate.Should().BeApproximately(slope, 1e-10);
            result.Coefficients[0].Estimate.Should().BeApproximately(intercept, 1e-10);
            result.Coefficients[1].StandardError.Should().BeApproximately(Math.Sqrt(wss / 3 / sxx), 1e-10);
            result.ResidualSumOfSquares.Should().BeApproximately(wss, 1e-10);
            result.ResidualStandardError.Should().BeApproximately(Math.Sqrt(wss / 3), 1e-10);

            var equal = tools.FitWeightedLinearRegression([.. x.Select(v => new[] { v })], y,
                [4.0, 4, 4, 4, 4]);

            var plain = tools.FitMultipleLinearRegression([.. x.Select(v => new[] { v })], y);
            equal.Coefficients[1].Estimate.Should().BeApproximately(plain.Coefficients[1].Estimate, 1e-10);
            equal.Coefficients[1].StandardError.Should().BeApproximately(plain.Coefficients[1].StandardError, 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitWeightedLinearRegression")]
        public void WeightedRegressionRejectsBadWeights()
        {
            var rows = new[] { new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 }, new[] { 4.0 } };
            var y = new[] { 1.0, 2, 2, 4 };

            RelationshipsFixtures.Rejects(() => tools.FitWeightedLinearRegression(rows, y, [1.0, 1, 1]),
                "weights");

            RelationshipsFixtures.Rejects(() => tools.FitWeightedLinearRegression(rows, y, [1.0, 0, 1, 1]),
                "weights");

            RelationshipsFixtures.Rejects(() => tools.FitWeightedLinearRegression(rows, y, [1.0, -1, 1, 1]),
                "weights");

            RelationshipsFixtures.Rejects(() => tools.FitWeightedLinearRegression(rows, y, null!), "weights");

            RelationshipsFixtures.Rejects(
                () => tools.FitWeightedLinearRegression(rows, y, [1.0, double.NaN, 1, 1]), "inputs");

            tools.FitWeightedLinearRegression(rows, y, [1.0, double.NaN, 1, 1], null, true, 0.95, false, true)
                .Observations.Should().Be(3);
        }

        [Fact]
        [Trait("Covers", "FitPolynomialRegression")]
        public void PolynomialRegressionRecoversAnExactQuadraticAndIsInvariantToCentring()
        {
            var x = Enumerable.Range(0, 7).Select(i => (double)i).ToArray();
            var y = x.Select(v => 2 + 3 * v - 0.5 * v * v).ToArray();

            var raw = tools.FitPolynomialRegression(x, y, 2, false, 0.95, [10.0]);

            raw.Coefficients[0].Estimate.Should().BeApproximately(2, 1e-9);
            raw.Coefficients[1].Estimate.Should().BeApproximately(3, 1e-9);
            raw.Coefficients[2].Estimate.Should().BeApproximately(-0.5, 1e-9);
            raw.Coefficients[2].Term.Should().Be("x^2");
            raw.RSquared.Should().BeApproximately(1, 1e-12);
            raw.Predictions![0].Prediction.Should().BeApproximately(2 + 30 - 50, 1e-7);

            var centred = tools.FitPolynomialRegression(x, y, 2, true, 0.95, [10.0]);
            centred.XCentre.Should().Be(3);
            centred.Predictions![0].Prediction.Should().BeApproximately(-18, 1e-7);
            centred.Coefficients[2].Estimate.Should().BeApproximately(-0.5, 1e-9);
            centred.Coefficients[2].Term.Should().Be("(x-centre)^2");
        }

        [Fact]
        [Trait("Covers", "FitPolynomialRegression")]
        public void PolynomialDegreeOneEqualsSimpleRegressionAndValidates()
        {
            var x = RelationshipsFixtures.AnscombeOneX;
            var y = RelationshipsFixtures.AnscombeOneY;
            var poly = tools.FitPolynomialRegression(x, y, 1, false);
            var simple = tools.FitSimpleLinearRegression(x, y);

            poly.Coefficients[1].Estimate.Should().BeApproximately(simple.Coefficients[1].Estimate, 1e-10);
            poly.Coefficients[1].PValue.Should().BeApproximately(simple.Coefficients[1].PValue, 1e-10);
            poly.RSquared.Should().BeApproximately(simple.RSquared, 1e-12);

            var years = Enumerable.Range(2000, 12).Select(v => (double)v).ToArray();
            var wave = years.Select((v, i) => 5 + 0.3 * (v - 2000) + Math.Sin(i)).ToArray();
            tools.FitPolynomialRegression(years, wave, 4).RSquared.Should().BeGreaterThan(0);

            RelationshipsFixtures.Rejects(() => tools.FitPolynomialRegression(x, y, 0), "degree");
            RelationshipsFixtures.Rejects(() => tools.FitPolynomialRegression(x, y, 13), "degree");

            RelationshipsFixtures.Rejects(
                () => tools.FitPolynomialRegression([1.0, 2, 3, 4], [1.0, 2, 3, 4], 3), "x");

            RelationshipsFixtures.Rejects(() => tools.FitPolynomialRegression(x, [.. y.Take(3)]), "y");
        }

        [Fact]
        [Trait("Covers", "SelectPolynomialDegree")]
        public void DegreeSelectionFindsTheTrueQuadratic()
        {
            var x = Enumerable.Range(0, 11).Select(i => (double)i).ToArray();
            var y = x.Select((v, i) => 0.5 * v * v + v + (i % 2 == 0 ? 0.3 : -0.3)).ToArray();

            var result = tools.SelectPolynomialDegree(x, y, 5);

            result.BestByBic.Should().Be(2);
            result.BestByAic.Should().Be(2);
            result.BestByLeaveOneOut.Should().Be(2);
            result.BestBySequentialFTest.Should().Be(2);
            result.Degrees.Should().HaveCount(5);
            result.Degrees[0].IncrementalFpValue.Should().BeLessThan(0.05);
            result.Degrees[1].IncrementalFpValue.Should().BeLessThan(1e-6);
            result.Degrees[2].IncrementalFpValue.Should().BeGreaterThan(0.05);
            result.Degrees.Should().OnlyContain(d => d.Fitted);

            var quadratic = tools.FitPolynomialRegression(x, y);
            result.Degrees[1].RSquared.Should().BeApproximately(quadratic.RSquared, 1e-12);
            result.Degrees[1].Aic.Should().BeApproximately(quadratic.Aic, 1e-9);
            result.Degrees[1].LeaveOneOutRmse.Should().BeGreaterThan(result.Degrees[1].ResidualStandardError * 0.5);
        }

        [Fact]
        [Trait("Covers", "SelectPolynomialDegree")]
        public void DegreeSelectionMarksUnfittableDegreesAndValidates()
        {
            var x = new[] { 1.0, 2, 3, 4, 5, 6 };
            var y = new[] { 1.0, 3, 2, 5, 4, 6 };

            var result = tools.SelectPolynomialDegree(x, y, 8);

            result.Degrees.Where(d => !d.Fitted).Select(d => d.Degree).Should().BeEquivalentTo([5, 6, 7, 8]);
            RelationshipsFixtures.Rejects(() => tools.SelectPolynomialDegree(x, y, 1), "maxDegree");
            RelationshipsFixtures.Rejects(() => tools.SelectPolynomialDegree(x, y, 13), "maxDegree");

            RelationshipsFixtures.Rejects(() => tools.SelectPolynomialDegree([1.0, 2, 3], [1.0, 2, 3]),
                "x");

            RelationshipsFixtures.Rejects(() => tools.SelectPolynomialDegree(x, [.. y.Take(3)]), "y");
        }

        [Fact]
        [Trait("Covers", "PredictWithLinearRegression")]
        public void PredictionIntervalsMatchTheClosedForm()
        {
            var x = RelationshipsFixtures.AnscombeOneX;
            var y = RelationshipsFixtures.AnscombeOneY;
            var (slope, intercept, sxx, sse, mean) = OrdinaryClosedForm(x, y);
            var s = Math.Sqrt(sse / 9);
            const double t = 2.262157;

            var result = tools.PredictWithLinearRegression([.. x.Select(v => new[] { v })], y,
                [[10.0], [9.0], [100.0]]);

            var fitted = intercept + slope * 10;
            var q = 1d / 11 + Math.Pow(10 - mean, 2) / sxx;
            result.Predictions[0].Prediction.Should().BeApproximately(fitted, 1e-10);
            result.Predictions[0].StandardErrorOfMean.Should().BeApproximately(s * Math.Sqrt(q), 1e-10);
            result.Predictions[0].MeanLower.Should().BeApproximately(fitted - t * s * Math.Sqrt(q), 1e-5);
            result.Predictions[0].MeanUpper.Should().BeApproximately(fitted + t * s * Math.Sqrt(q), 1e-5);
            result.Predictions[0].PredictionLower.Should().BeApproximately(fitted - t * s * Math.Sqrt(1 + q), 1e-5);
            result.Predictions[0].PredictionUpper.Should().BeApproximately(fitted + t * s * Math.Sqrt(1 + q), 1e-5);
            result.Predictions[1].Leverage.Should().BeApproximately(1d / 11, 1e-12);
            result.ExtrapolatedRows.Should().Be(1);
            result.Warnings.Should().ContainSingle(w => w.Contains("extrapolations"));
            result.ResidualDegreesOfFreedom.Should().Be(9);
        }

        [Fact]
        [Trait("Covers", "PredictWithLinearRegression")]
        public void PredictionValidatesItsInputs()
        {
            var x = RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;

            RelationshipsFixtures.Rejects(() => tools.PredictWithLinearRegression(x, y, null!), "newInputs");

            RelationshipsFixtures.Rejects(() => tools.PredictWithLinearRegression(x, y, []),
                "newInputs");

            RelationshipsFixtures.Rejects(() => tools.PredictWithLinearRegression(x, y, [[1.0, 2]]),
                "newInputs");

            RelationshipsFixtures.Rejects(() => tools.PredictWithLinearRegression(x, y, [[double.NaN]]),
                "newInputs");

            RelationshipsFixtures.Rejects(
                () => tools.PredictWithLinearRegression(x, y, [[1.0]], null, true, 1.2),
                "confidenceLevel");

            var weighted = tools.PredictWithLinearRegression(x, y, [[10.0]], null, true, 0.95,
                [.. Enumerable.Repeat(2.0, 11)]);

            var unweighted = tools.PredictWithLinearRegression(x, y, [[10.0]]);
            weighted.Predictions[0].Prediction.Should().BeApproximately(unweighted.Predictions[0].Prediction, 1e-10);
        }

        [Fact]
        [Trait("Covers", "CompareNestedLinearModels")]
        public void NestedComparisonOfOneVariableEqualsTheSquaredTStatistic()
        {
            var rows = RelationshipsFixtures.AnscombeOneX.Select((v, i) => new[] { v, Math.Sin(i * 1.7) * 2 })
                .ToArray();

            var y = RelationshipsFixtures.AnscombeOneY;
            var full = tools.FitMultipleLinearRegression(rows, y, ["x", "wobble"]);

            var result = tools.CompareNestedLinearModels(rows, y, ["wobble"], ["x", "wobble"]);

            result.FStatistic.Should().BeApproximately(Math.Pow(full.Coefficients[2].TStatistic, 2), 1e-8);
            result.PValue.Should().BeApproximately(full.Coefficients[2].PValue, 1e-9);
            result.DegreesOfFreedomDifference.Should().Be(1);
            result.ReducedVariables.Should().Equal("x");
            result.RssFull.Should().BeApproximately(full.ResidualSumOfSquares, 1e-9);
            result.RSquaredFull.Should().BeApproximately(full.RSquared, 1e-12);
        }

        [Fact]
        [Trait("Covers", "CompareNestedLinearModels")]
        public void NestedComparisonOfSeveralVariablesMatchesThePartialFFormula()
        {
            var names = RelationshipsFixtures.LongleyNames;
            var y = RelationshipsFixtures.LongleyEmployed;
            var rows = RelationshipsFixtures.LongleyPredictors;
            var full = tools.FitMultipleLinearRegression(rows, y, names);
            var reducedRows = rows.Select(r => new[] { r[1], r[2], r[4], r[5] }).ToArray();

            var reduced =
                tools.FitMultipleLinearRegression(reducedRows, y, ["gnp", "unemployed", "population", "year"]);

            var result = tools.CompareNestedLinearModels(rows, y, ["deflator", "armed"], names);

            var expected = (full.RSquared - reduced.RSquared) / 2 / ((1 - full.RSquared) / 9);
            result.FStatistic.Should().BeApproximately(expected, expected * 1e-6);
            result.DegreesOfFreedomDifference.Should().Be(2);
            result.ResidualDegreesOfFreedomFull.Should().Be(9);
            result.DroppedVariables.Should().Equal("deflator", "armed");
            result.Conclusion.Should().NotBeNullOrEmpty();
            result.AicReduced.Should().BeApproximately(reduced.Aic, 1e-6);

            var interceptOnly = tools.CompareNestedLinearModels(rows, y, names, names);
            interceptOnly.FStatistic.Should().BeApproximately(full.FStatistic, full.FStatistic * 1e-6);
            interceptOnly.PValue.Should().BeApproximately(full.FpValue, 1e-10);
        }

        [Fact]
        [Trait("Covers", "CompareNestedLinearModels")]
        public void NestedComparisonRejectsBadNames()
        {
            var rows = RelationshipsFixtures.AnscombeOneX.Select((v, i) => new[] { v, Math.Cos(i) }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;

            RelationshipsFixtures.Rejects(
                () => tools.CompareNestedLinearModels(rows, y, ["nope"], ["a", "b"]), "dropVariables");

            RelationshipsFixtures.Rejects(() => tools.CompareNestedLinearModels(rows, y, []),
                "dropVariables");

            RelationshipsFixtures.Rejects(
                () => tools.CompareNestedLinearModels(rows, y, ["x1", "x2"], null, false), "dropVariables");
        }

        [Fact]
        [Trait("Covers", "FitMultivariateLinearRegression")]
        public void MultivariateRegressionMatchesSeparateFits()
        {
            var x = RelationshipsFixtures.AnscombeOneX.Select((v, i) => new[] { v, Math.Cos(i) }).ToArray();
            var y1 = RelationshipsFixtures.AnscombeOneY;
            var y2 = RelationshipsFixtures.AnscombeOneX.Select((v, i) => 3 * v - 4 + Math.Sin(i * 2)).ToArray();
            var outputs = y1.Select((v, i) => new[] { v, y2[i] }).ToArray();

            var result = tools.FitMultivariateLinearRegression(x, outputs, null, ["yield", "cost"]);

            result.Outputs.Should().Equal("yield", "cost");
            var first = tools.FitMultipleLinearRegression(x, y1);
            var second = tools.FitMultipleLinearRegression(x, y2);
            result.Models[0].Coefficients[1].Estimate.Should().BeApproximately(first.Coefficients[1].Estimate, 1e-9);
            result.Models[1].Coefficients[1].StandardError.Should()
                .BeApproximately(second.Coefficients[1].StandardError, 1e-9);

            result.Models[1].RSquared.Should().BeApproximately(second.RSquared, 1e-12);
        }

        [Fact]
        [Trait("Covers", "FitMultivariateLinearRegression")]
        public void MultivariateRegressionValidates()
        {
            var x = RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v }).ToArray();
            var outputs = x.Select((r, i) => new[] { r[0] * 2 + Math.Sin(i), r[0] - Math.Cos(i) }).ToArray();

            RelationshipsFixtures.Rejects(() => tools.FitMultivariateLinearRegression(x, [.. outputs.Take(4)]),
                "outputs");

            RelationshipsFixtures.Rejects(
                () => tools.FitMultivariateLinearRegression(x, outputs, null, ["one"]), "outputNames");

            var gap = outputs.Select(r => (double[])r.Clone()).ToArray();
            gap[3][1] = double.NaN;
            RelationshipsFixtures.Rejects(() => tools.FitMultivariateLinearRegression(x, gap), "outputs");

            tools.FitMultivariateLinearRegression(x, gap, null, null, true, 0.95, true).Models[0].Observations.Should()
                .Be(10);

            RelationshipsFixtures.Rejects(() => tools.FitMultivariateLinearRegression(x, outputs, null, null, true, 5),
                "confidenceLevel");
        }

        [Fact]
        [Trait("Covers", "FitRidgeRegression")]
        public void RidgeMatchesTheSinglePredictorClosedForm()
        {
            var x = RelationshipsFixtures.AnscombeOneX;
            var y = RelationshipsFixtures.AnscombeOneY;
            var (slope, intercept, sxx, sse, mean) = OrdinaryClosedForm(x, y);
            var rows = x.Select(v => new[] { v }).ToArray();

            var result = robust.FitRidgeRegression(rows, y, 10);

            var shrink = 10d / 20d;
            result.Coefficients[0].Estimate.Should().BeApproximately(slope * shrink, 1e-10);
            result.Coefficients[0].OrdinaryLeastSquaresEstimate.Should().BeApproximately(slope, 1e-10);
            result.EffectiveDegreesOfFreedom.Should().BeApproximately(shrink, 1e-10);
            result.Intercept.Should().BeApproximately(y.Average() - slope * shrink * mean, 1e-10);
            var rss = x.Zip(y, (a, b) => Math.Pow(b - result.Intercept - result.Coefficients[0].Estimate * a, 2)).Sum();
            result.GeneralisedCrossValidation.Should().BeApproximately(11 * rss / Math.Pow(11 - 1 - shrink, 2), 1e-9);

            var unpenalised = robust.FitRidgeRegression(rows, y, 0, null, true);
            unpenalised.Coefficients[0].Estimate.Should().BeApproximately(slope, 1e-10);
            unpenalised.Intercept.Should().BeApproximately(intercept, 1e-10);
            unpenalised.EffectiveDegreesOfFreedom.Should().BeApproximately(1, 1e-10);
            unpenalised.RSquared.Should().BeApproximately(0.6665, 1e-4);
            unpenalised.FittedValues.Should().HaveCount(11);

            unpenalised.Coefficients[0].ApproximateStandardError.Should()
                .BeApproximately(Math.Sqrt(sse / 9 / sxx), 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitRidgeRegression")]
        public void RidgeShrinksCollinearCoefficientsAndValidates()
        {
            var ols = tools.FitMultipleLinearRegression(RelationshipsFixtures.LongleyPredictors,
                RelationshipsFixtures.LongleyEmployed, RelationshipsFixtures.LongleyNames);

            var ridge = robust.FitRidgeRegression(RelationshipsFixtures.LongleyPredictors,
                RelationshipsFixtures.LongleyEmployed, 1, RelationshipsFixtures.LongleyNames);

            var unpenalised = robust.FitRidgeRegression(RelationshipsFixtures.LongleyPredictors,
                RelationshipsFixtures.LongleyEmployed, 0, RelationshipsFixtures.LongleyNames);

            for (var j = 0; j < 6; j++)
            {
                Close(unpenalised.Coefficients[j].Estimate, ols.Coefficients[j + 1].Estimate, 1e-5);
            }

            ridge.EffectiveDegreesOfFreedom.Should().BeLessThan(6);

            ridge.Coefficients.Sum(c => Math.Abs(c.StandardisedEstimate))
                .Should().BeLessThan(unpenalised.Coefficients.Sum(c => Math.Abs(c.StandardisedEstimate)));

            ridge.RSquared.Should().BeLessThan(unpenalised.RSquared);

            var rows = RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v, 2 * v }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;
            RelationshipsFixtures.Rejects(() => robust.FitRidgeRegression(rows, y, 0), "lambda");

            robust.FitRidgeRegression(rows, y).Coefficients[0].Estimate.Should().BeApproximately(
                robust.FitRidgeRegression(rows, y).Coefficients[1].Estimate * 2, 1e-9);

            RelationshipsFixtures.Rejects(() => robust.FitRidgeRegression(rows, y, -1), "lambda");
            RelationshipsFixtures.Rejects(() => robust.FitRidgeRegression(rows, y, double.NaN), "lambda");

            RelationshipsFixtures.Rejects(
                () => robust.FitRidgeRegression(
                    [.. RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v, 5.0 })], y), "inputs");
        }

        [Fact]
        [Trait("Covers", "TraceRidgeRegularisationPath")]
        public void RidgePathShrinksMonotonicallyAndPicksTheLowestGcv()
        {
            var rows = RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;

            var result = robust.TraceRidgeRegularisationPath(rows, y, [0.0, 1, 10, 100, 1000]);

            result.Path.Select(p => p.Lambda).Should().Equal(0, 1, 10, 100, 1000);
            var slope = result.Path.Select(p => p.Estimates[0]).ToArray();
            slope.Zip(slope.Skip(1), (a, b) => a > b).Should().OnlyContain(v => v);
            result.Path[2].Estimates[0].Should().BeApproximately(slope[0] * 10d / 20d, 1e-10);
            result.Path[3].EffectiveDegreesOfFreedom.Should().BeApproximately(10d / 110d, 1e-10);
            var best = result.Path.OrderBy(p => p.GeneralisedCrossValidation).First();
            result.BestLambdaByGcv.Should().Be(best.Lambda);
            result.Path[result.BestIndex].Lambda.Should().Be(best.Lambda);

            var automatic = robust.TraceRidgeRegularisationPath(rows, y, null, null, 10);
            automatic.Path.Should().HaveCount(10);
            automatic.Path[0].Lambda.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "TraceRidgeRegularisationPath")]
        public void RidgePathValidates()
        {
            var rows = RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;

            RelationshipsFixtures.Rejects(() => robust.TraceRidgeRegularisationPath(rows, y, null, null, 2),
                "pathLength");

            RelationshipsFixtures.Rejects(() => robust.TraceRidgeRegularisationPath(rows, y, [-1.0]),
                "lambdas");

            RelationshipsFixtures.Rejects(() => robust.TraceRidgeRegularisationPath(rows, [.. y.Take(3)]),
                "outputs");

            var collinear = RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v, 3 * v }).ToArray();

            RelationshipsFixtures.Rejects(() => robust.TraceRidgeRegularisationPath(collinear, y, [0.0]),
                "lambdas");

            robust.TraceRidgeRegularisationPath(collinear, y, [0.0, 1]).Path.Should().ContainSingle();
        }

        [Fact]
        [Trait("Covers", "FitNonNegativeLeastSquares")]
        public void NonNegativeLeastSquaresClampsNegativeCoefficientsToZero()
        {
            var rows = new[] { new[] { 1.0, 1 }, new[] { 1.0, 2 }, new[] { 1.0, 3 } };

            var exact = robust.FitNonNegativeLeastSquares(rows, [1.0, 2, 3]);
            exact.Coefficients[0].Should().BeApproximately(0, 1e-9);
            exact.Coefficients[1].Should().BeApproximately(1, 1e-9);
            exact.ResidualSumOfSquares.Should().BeApproximately(0, 1e-12);
            exact.ActiveVariables.Should().Equal("x2");

            var clamped =
                robust.FitNonNegativeLeastSquares(rows, [3.0, 2, 1], ["constant", "slope"], true);
            clamped.Coefficients[0].Should().BeApproximately(2, 1e-9);
            clamped.Coefficients[1].Should().BeApproximately(0, 1e-9);
            clamped.ResidualSumOfSquares.Should().BeApproximately(2, 1e-9);
            clamped.RootMeanSquaredError.Should().BeApproximately(Math.Sqrt(2d / 3), 1e-9);
            clamped.ActiveVariables.Should().Equal("constant");
            clamped.FittedValues.Should().HaveCount(3);
            clamped.UncentredRSquared.Should().BeApproximately(1 - 2d / 14, 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitNonNegativeLeastSquares")]
        public void NonNegativeLeastSquaresValidates()
        {
            RelationshipsFixtures.Rejects(
                () => robust.FitNonNegativeLeastSquares([[1.0], [2.0]], [1.0]),
                "outputs");

            RelationshipsFixtures.Rejects(
                () => robust.FitNonNegativeLeastSquares([], []), "inputs");

            RelationshipsFixtures.Rejects(
                () => robust.FitNonNegativeLeastSquares([[1.0], [double.NaN]],
                    [1.0, 2]), "inputs");

            robust.FitNonNegativeLeastSquares([[1.0], [double.NaN], [3.0]],
                [1.0, 2, 3], null, false, true).Observations.Should().Be(2);
        }

        private static (double[][] Rows, double[] Y) ContaminatedLine()
        {
            var x = Enumerable.Range(0, 24).Select(i => (double)i).ToArray();
            var y = x.Select((v, i) => 2 * v + 1 + (i % 2 == 0 ? 0.05 : -0.05)).ToArray();
            y[3] += 90;
            y[9] -= 120;
            y[15] += 80;
            y[21] += 150;

            return ([.. x.Select(v => new[] { v })], y);
        }

        [Fact]
        [Trait("Covers", "FitRansacLinearRegression")]
        public void RansacFindsTheTrueLineAndTheOutliers()
        {
            var (rows, y) = ContaminatedLine();

            var result = robust.FitRansacLinearRegression(rows, y, 1.0);

            result.InlierModel.Coefficients[1].Estimate.Should().BeApproximately(2, 0.01);
            result.InlierModel.Coefficients[0].Estimate.Should().BeApproximately(1, 0.1);
            result.OutlierRows.Should().Equal(3, 9, 15, 21);
            result.InlierRows.Should().HaveCount(20);
            result.InlierFraction.Should().BeApproximately(20d / 24, 1e-12);
            Math.Abs(result.OrdinaryModel.Coefficients[1].Estimate - 2).Should().BeGreaterThan(0.05);
            result.Threshold.Should().Be(1.0);
            result.Seed.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "FitRansacLinearRegression")]
        public void RansacIsReproduciblePerSeedAndValidates()
        {
            var (rows, y) = ContaminatedLine();

            var a = robust.FitRansacLinearRegression(rows, y, null, null, 7);
            var b = robust.FitRansacLinearRegression(rows, y, null, null, 7);
            a.InlierRows.Should().Equal(b.InlierRows);
            a.Threshold.Should().Be(b.Threshold);
            a.InlierFraction.Should().BeGreaterThan(0.5);

            RelationshipsFixtures.Rejects(() => robust.FitRansacLinearRegression(rows, y, -1), "inlierThreshold");

            RelationshipsFixtures.Rejects(() => robust.FitRansacLinearRegression(rows, y, 1, null, 1, 5),
                "maximumTrials");

            RelationshipsFixtures.Rejects(() => robust.FitRansacLinearRegression(rows, y, 1, null, 1, 100, 1.5),
                "successProbability");

            RelationshipsFixtures.Rejects(() => robust.FitRansacLinearRegression(rows, [.. y.Take(5)], 1),
                "outputs");

            RelationshipsFixtures.Rejects(
                () => robust.FitRansacLinearRegression([.. rows.Take(2)], [.. y.Take(2)], 1), "inputs");

            RelationshipsFixtures.Rejects(
                () => robust.FitRansacLinearRegression(
                    [.. RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v })],
                    RelationshipsFixtures.AnscombeOneY, 1e-9), "inlierThreshold");
        }

        [Fact]
        [Trait("Covers", "FitMultivariateRegressionByPartialLeastSquares")]
        public void PartialLeastSquaresWithAllComponentsEqualsOrdinaryLeastSquares()
        {
            var rows = RelationshipsFixtures.AnscombeOneX.Select((v, i) => new[] { v, Math.Sin(i * 1.3) * 3 })
                .ToArray();

            var y = RelationshipsFixtures.AnscombeOneY;
            var ols = tools.FitMultipleLinearRegression(rows, y);

            var pls = robust.FitMultivariateRegressionByPartialLeastSquares(rows, y);

            pls.Coefficients[0].Should().BeApproximately(ols.Coefficients[1].Estimate, 1e-6);
            pls.Coefficients[1].Should().BeApproximately(ols.Coefficients[2].Estimate, 1e-6);
            pls.Intercept.Should().BeApproximately(ols.Coefficients[0].Estimate, 1e-6);
            pls.RSquared.Should().BeApproximately(ols.RSquared, 1e-9);
            pls.ComponentSummaries.Should().HaveCount(2);
            pls.ComponentSummaries[1].CumulativePredictorVarianceExplained.Should().BeApproximately(1, 1e-9);
            pls.VariableImportance.Should().HaveCount(2);

            var single =
                robust.FitMultivariateRegressionByPartialLeastSquares([.. rows.Select(r => new[] { r[0] })], y, 1);

            var simple = tools.FitSimpleLinearRegression(RelationshipsFixtures.AnscombeOneX, y);
            single.Coefficients[0].Should().BeApproximately(simple.Coefficients[1].Estimate, 1e-6);
            single.RSquared.Should().BeApproximately(simple.RSquared, 1e-9);
        }

        [Fact]
        [Trait("Covers", "FitMultivariateRegressionByPartialLeastSquares")]
        public void PartialLeastSquaresValidates()
        {
            var rows = RelationshipsFixtures.AnscombeOneX.Select((v, i) => new[] { v, Math.Cos(i) }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;

            RelationshipsFixtures.Rejects(() => robust.FitMultivariateRegressionByPartialLeastSquares(rows, y, 0),
                "components");
            RelationshipsFixtures.Rejects(() => robust.FitMultivariateRegressionByPartialLeastSquares(rows, y, 3),
                "components");

            RelationshipsFixtures.Rejects(
                () => robust.FitMultivariateRegressionByPartialLeastSquares(
                    [.. rows.Select(r => new[] { r[0], 4.0 })], y, 1), "inputs");

            RelationshipsFixtures.Rejects(
                () => robust.FitMultivariateRegressionByPartialLeastSquares(rows, [.. y.Take(3)], 1), "outputs");

            var short2 = rows.Take(3).ToArray();

            RelationshipsFixtures.Rejects(
                () => robust.FitMultivariateRegressionByPartialLeastSquares(short2, [.. y.Take(3)], 1), "inputs");
        }

        [Fact]
        [Trait("Covers", "CrossValidateLinearRegression")]
        public void LeaveOneOutCrossValidationEqualsTheHatMatrixShortcut()
        {
            var rows = RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;

            var result = robust.CrossValidateLinearRegression(rows, y, 11);

            result.CrossValidatedRmse.Should().BeApproximately(result.LeaveOneOutRmse, 1e-9);
            result.PressStatistic.Should().BeApproximately(11 * result.LeaveOneOutRmse * result.LeaveOneOutRmse, 1e-9);
            result.Folds.Should().HaveCount(11);
            result.Folds.Should().OnlyContain(f => f.TestRows == 1 && f.TrainRows == 10);
            result.CrossValidatedRmse.Should().BeGreaterThan(result.InSampleRmse);
            result.PredictedRSquared.Should().BeLessThan(result.InSampleRSquared);
            var mean = y.Average();

            result.PredictedRSquared.Should()
                .BeApproximately(1 - result.PressStatistic / y.Sum(v => Math.Pow(v - mean, 2)), 1e-12);
        }

        [Fact]
        [Trait("Covers", "CrossValidateLinearRegression")]
        public void KFoldCrossValidationIsSeededAndValidates()
        {
            var rows = RelationshipsFixtures.AnscombeOneX.Select(v => new[] { v }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;

            var a = robust.CrossValidateLinearRegression(rows, y, 4, 3);
            var b = robust.CrossValidateLinearRegression(rows, y, 4, 3);
            a.CrossValidatedRmse.Should().Be(b.CrossValidatedRmse);
            a.Folds.Sum(f => f.TestRows).Should().Be(11);
            a.Folds.Select(f => f.TestRows).Should().BeEquivalentTo([3, 3, 3, 2]);
            a.Seed.Should().Be(3);

            RelationshipsFixtures.Rejects(() => robust.CrossValidateLinearRegression(rows, y, 1), "folds");
            RelationshipsFixtures.Rejects(() => robust.CrossValidateLinearRegression(rows, y, 12), "folds");

            RelationshipsFixtures.Rejects(() => robust.CrossValidateLinearRegression(rows, [.. y.Take(3)]),
                "outputs");

            var perfect = robust.CrossValidateLinearRegression(rows, [.. rows.Select(r => 2 * r[0] + 1)]);
            perfect.CrossValidatedRmse.Should().BeApproximately(0, 1e-9);
            perfect.CrossValidatedRSquared.Should().BeApproximately(1, 1e-12);
        }
    }
}