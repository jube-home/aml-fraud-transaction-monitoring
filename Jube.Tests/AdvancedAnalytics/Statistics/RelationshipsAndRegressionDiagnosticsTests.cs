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
using Accord.Math;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class RelationshipsAndRegressionDiagnosticsTests
    {
        private static readonly double[] anscombeThreeY =
            [7.46, 6.77, 12.74, 7.11, 7.81, 8.84, 6.08, 5.39, 8.15, 6.42, 5.73];

        private readonly LinearRegressionTools linear = new();
        private readonly RegressionDiagnosticsTools tools = new();

        private static double[][] Column(double[] values)
        {
            return [.. values.Select(v => new[] { v })];
        }

        private static (double[][] Rows, double[] Y) Trend(bool heteroscedastic)
        {
            var x = Enumerable.Range(1, 80).Select(i => i * 0.25).ToArray();
            var rows = x.Select((v, i) => new[] { v, Math.Sin(i * 0.9) * 2 }).ToArray();

            var y = x.Select((v, i) => 3 + 1.5 * v + (heteroscedastic ? v : 1.0) * 0.6 * Math.Sin(i * 7.31 + 0.4))
                .ToArray();

            return (rows, y);
        }

        [Fact]
        [Trait("Covers", "RegressionInfluenceDiagnostics")]
        public void InfluenceMeasuresMatchTheSimpleRegressionClosedForms()
        {
            var x = RelationshipsFixtures.AnscombeOneX;
            var y = RelationshipsFixtures.AnscombeOneY;
            var mx = x.Average();
            var my = y.Average();
            var sxx = x.Sum(v => (v - mx) * (v - mx));
            var slope = x.Zip(y, (a, b) => (a - mx) * (b - my)).Sum() / sxx;
            var intercept = my - slope * mx;
            var e = x.Zip(y, (a, b) => b - intercept - slope * a).ToArray();
            var sse = e.Sum(v => v * v);
            var s2 = sse / 9;

            var result = tools.RegressionInfluenceDiagnostics(Column(x), y, null, true, true);

            result.Rows.Should().HaveCount(11);

            foreach (var row in result.Rows)
            {
                var i = row.Row;
                var h = 1d / 11 + Math.Pow(x[i] - mx, 2) / sxx;
                var standardised = e[i] / Math.Sqrt(s2 * (1 - h));
                var s2I = (sse - e[i] * e[i] / (1 - h)) / 8;
                var student = e[i] / Math.Sqrt(s2I * (1 - h));
                row.Leverage.Should().BeApproximately(h, 1e-10);
                row.Residual.Should().BeApproximately(e[i], 1e-10);
                row.StandardisedResidual.Should().BeApproximately(standardised, 1e-9);
                row.StudentisedResidual.Should().BeApproximately(student, 1e-9);
                row.CooksDistance.Should().BeApproximately(standardised * standardised * h / (2 * (1 - h)), 1e-9);
                row.Dffits.Should().BeApproximately(student * Math.Sqrt(h / (1 - h)), 1e-9);
                row.CovarianceRatio.Should().BeApproximately(Math.Pow(s2I / s2, 2) / (1 - h), 1e-9);
                row.Dfbetas.Should().HaveCount(2);
            }

            result.Terms.Should().Equal("(Intercept)", "x1");
            result.LeverageThreshold.Should().BeApproximately(4d / 11, 1e-12);
            result.CooksDistanceThreshold.Should().BeApproximately(4d / 11, 1e-12);
            result.DfbetasThreshold.Should().BeApproximately(2 / Math.Sqrt(11), 1e-12);
        }

        [Fact]
        [Trait("Covers", "RegressionInfluenceDiagnostics")]
        public void InfluenceDfbetasAndPressMatchBruteForceLeaveOneOutRefits()
        {
            var x = RelationshipsFixtures.AnscombeOneX;
            var y = RelationshipsFixtures.AnscombeOneY;
            var full = linear.FitSimpleLinearRegression(x, y);
            var press = 0d;
            var result = tools.RegressionInfluenceDiagnostics(Column(x), y, null, true, true);

            for (var i = 0; i < 11; i++)
            {
                var xr = x.Where((_, j) => j != i).ToArray();
                var yr = y.Where((_, j) => j != i).ToArray();
                var refit = linear.FitSimpleLinearRegression(xr, yr);
                var predicted = refit.Coefficients[0].Estimate + refit.Coefficients[1].Estimate * x[i];
                press += Math.Pow(y[i] - predicted, 2);
                var row = result.Rows.Single(r => r.Row == i);

                var se0 = refit.ResidualStandardError *
                          Math.Sqrt(1d / 10 + Math.Pow(xr.Average(), 2) / xr.Sum(v => Math.Pow(v - xr.Average(), 2)));

                var seSlope = refit.ResidualStandardError / Math.Sqrt(xr.Sum(v => Math.Pow(v - xr.Average(), 2)));

                row.Dfbetas[0].Should().BeApproximately(
                    (full.Coefficients[0].Estimate - refit.Coefficients[0].Estimate) / (refit.ResidualStandardError *
                        Math.Sqrt(1d / 11 + Math.Pow(x.Average(), 2) / x.Sum(v => Math.Pow(v - x.Average(), 2)))),
                    1e-8);

                row.Dfbetas[1].Should()
                    .BeApproximately(
                        (full.Coefficients[1].Estimate - refit.Coefficients[1].Estimate) /
                        (refit.ResidualStandardError / Math.Sqrt(x.Sum(v => Math.Pow(v - x.Average(), 2)))), 1e-8);

                seSlope.Should().BeGreaterThan(0);
                se0.Should().BeGreaterThan(0);
            }

            result.PressStatistic.Should().BeApproximately(press, 1e-8);
            result.LeaveOneOutRmse.Should().BeApproximately(Math.Sqrt(press / 11), 1e-9);
        }

        [Fact]
        [Trait("Covers", "RegressionInfluenceDiagnostics")]
        public void InfluenceFlagsTheAnscombeOutlierAndValidates()
        {
            var x = RelationshipsFixtures.AnscombeOneX;

            var result = tools.RegressionInfluenceDiagnostics(Column(x), anscombeThreeY);

            result.Rows[0].Row.Should().Be(2);
            result.Rows[0].Flags.Should().Contain(["Outlier", "Influential"]);
            result.OutlierCount.Should().Be(1);
            result.MostInfluential[0].Row.Should().Be(2);
            result.Rows.Should().OnlyContain(r => r.Flags.Length > 0);

            RelationshipsFixtures.Rejects(
                () => tools.RegressionInfluenceDiagnostics(Column(x), anscombeThreeY, null, true, false, 0),
                "maximumRows");

            RelationshipsFixtures.Rejects(
                () => tools.RegressionInfluenceDiagnostics(Column(x), anscombeThreeY, null, true, false, 5, 0),
                "topCount");

            RelationshipsFixtures.Rejects(
                () => tools.RegressionInfluenceDiagnostics(Column(x), [.. anscombeThreeY.Take(4)]), "outputs");

            RelationshipsFixtures.Rejects(
                () => tools.RegressionInfluenceDiagnostics(Column([.. x.Take(3)]),
                    [.. anscombeThreeY.Take(3)]), "inputs");

            var gap = (double[])x.Clone();
            gap[4] = double.NaN;

            var kept = tools.RegressionInfluenceDiagnostics(Column(gap), anscombeThreeY, null, true, true, 50, 10,
                true);
            kept.RowsDropped.Should().Be(1);
            kept.KeptRowIndices.Should().NotContain(4);
            kept.Rows.Select(r => r.Row).Should().NotContain(4);
        }

        [Fact]
        [Trait("Covers", "DurbinWatsonRegressionTest")]
        public void DurbinWatsonMatchesTheHandComputedResiduals()
        {
            var rows = Column([2.0, -1, -2, -1, 2]);
            var y = new[] { -2.0, -1, 0, 1, 2 };

            var result = tools.DurbinWatsonRegressionTest(rows, y);

            result.DurbinWatson.Should().BeApproximately(0.4, 1e-9);
            result.FirstOrderAutocorrelation.Should().BeApproximately(0.4, 1e-9);
            result.ApproximateZ.Should().BeApproximately(0.4 * Math.Sqrt(5), 1e-9);
            result.Interpretation.Should().Contain("positively");
            result.Observations.Should().Be(5);
        }

        [Fact]
        [Trait("Covers", "DurbinWatsonRegressionTest")]
        public void DurbinWatsonSeparatesAlternatingFromSmoothResiduals()
        {
            var x = Enumerable.Range(0, 40).Select(i => (double)i).ToArray();
            var alternating = x.Select((v, i) => v + (i % 2 == 0 ? 1.0 : -1.0)).ToArray();
            var smooth = x.Select((v, i) => v + Math.Sin(i * 0.2) * 3).ToArray();

            tools.DurbinWatsonRegressionTest(Column(x), alternating).DurbinWatson.Should().BeGreaterThan(3);
            tools.DurbinWatsonRegressionTest(Column(x), alternating).Interpretation.Should().Contain("negatively");
            var positive = tools.DurbinWatsonRegressionTest(Column(x), smooth);
            positive.DurbinWatson.Should().BeLessThan(0.5);
            positive.ApproximatePValue.Should().BeLessThan(0.001);
            var fit = linear.FitSimpleLinearRegression(x, smooth, true, 0.95, null, true);
            positive.DurbinWatson.Should().BeApproximately(fit.DurbinWatson, 1e-12);

            RelationshipsFixtures.Rejects(() => tools.DurbinWatsonRegressionTest(Column(x), [.. smooth.Take(5)]),
                "outputs");

            RelationshipsFixtures.Rejects(
                () => tools.DurbinWatsonRegressionTest(Column([1.0, 2, 3]), [1.0, 2, 3]), "inputs");
        }

        [Fact]
        [Trait("Covers", "BreuschGodfreySerialCorrelationTest")]
        public void BreuschGodfreyMatchesTheAuxiliaryRegressionRecomputedByAnotherTool()
        {
            var x = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
            var y = x.Select((v, i) => 2 + 0.3 * v + Math.Sin(i * 0.35) * 2 + Math.Cos(i * 4.1) * 0.3).ToArray();
            var basic = linear.FitSimpleLinearRegression(x, y, true, 0.95, null, true);
            var residuals = basic.Residuals!;
            var lag1 = residuals.Select((_, i) => i == 0 ? 0 : residuals[i - 1]).ToArray();
            var lag2 = residuals.Select((_, i) => i < 2 ? 0 : residuals[i - 2]).ToArray();

            var one = tools.BreuschGodfreySerialCorrelationTest(Column(x), y);

            var auxOne =
                linear.FitMultipleLinearRegression([.. x.Select((v, i) => new[] { v, lag1[i] })], residuals);

            one.Statistic.Should().BeApproximately(50 * auxOne.RSquared, 1e-9);
            one.DegreesOfFreedom.Should().Be(1);
            one.PValue.Should().BeLessThan(1e-6);
            one.AuxiliaryRSquared.Should().BeApproximately(auxOne.RSquared, 1e-12);

            var two = tools.BreuschGodfreySerialCorrelationTest(Column(x), y, 2);

            var auxTwo = linear.FitMultipleLinearRegression([.. x.Select((v, i) => new[] { v, lag1[i], lag2[i] })],
                residuals);

            two.Statistic.Should().BeApproximately(50 * auxTwo.RSquared, 1e-9);
            two.DegreesOfFreedom.Should().Be(2);
        }

        [Fact]
        [Trait("Covers", "BreuschGodfreySerialCorrelationTest")]
        public void BreuschGodfreyAcceptsIndependentResidualsAndValidates()
        {
            var x = Enumerable.Range(0, 60).Select(i => (double)i).ToArray();
            var random = new Random(11);
            var noisy = x.Select(v => 1 + v + random.NextDouble() - 0.5).ToArray();

            tools.BreuschGodfreySerialCorrelationTest(Column(x), noisy).PValue.Should().BeGreaterThan(0.05);

            RelationshipsFixtures.Rejects(() => tools.BreuschGodfreySerialCorrelationTest(Column(x), noisy, 0),
                "order");
            RelationshipsFixtures.Rejects(() => tools.BreuschGodfreySerialCorrelationTest(Column(x), noisy, 21),
                "order");

            RelationshipsFixtures.Rejects(
                () => tools.BreuschGodfreySerialCorrelationTest(Column([.. x.Take(8)]), [.. noisy.Take(8)],
                    6), "order");

            RelationshipsFixtures.Rejects(
                () => tools.BreuschGodfreySerialCorrelationTest(Column(x), [.. noisy.Take(3)]), "outputs");
        }

        [Fact]
        [Trait("Covers", "BreuschPaganHeteroscedasticityTest")]
        public void BreuschPaganMatchesTheAuxiliaryRegressionForBothVariants()
        {
            var (rows, y) = Trend(true);
            var fit = linear.FitMultipleLinearRegression(rows, y, null, true, 0.95, true);
            var squared = fit.Residuals!.Select(e => e * e).ToArray();
            var aux = linear.FitMultipleLinearRegression(rows, squared);

            var koenker = tools.BreuschPaganHeteroscedasticityTest(rows, y);
            koenker.Statistic.Should().BeApproximately(80 * aux.RSquared, 1e-8);
            koenker.DegreesOfFreedom.Should().Be(2);
            koenker.AuxiliaryRSquared.Should().BeApproximately(aux.RSquared, 1e-12);
            koenker.PValue.Should().BeLessThan(0.001);

            var sigma2 = fit.ResidualSumOfSquares / 80;
            var explained = aux.TotalSumOfSquares - aux.ResidualSumOfSquares;

            var classical =
                tools.BreuschPaganHeteroscedasticityTest(rows, y, null, HeteroscedasticityVariant.Classical);

            classical.Statistic.Should().BeApproximately(explained / (2 * sigma2 * sigma2), 1e-8);
            classical.Test.Should().Be("Breusch-Pagan");
        }

        [Fact]
        [Trait("Covers", "BreuschPaganHeteroscedasticityTest")]
        public void BreuschPaganAcceptsConstantVarianceAndValidates()
        {
            var (rows, y) = Trend(false);

            tools.BreuschPaganHeteroscedasticityTest(rows, y).PValue.Should().BeGreaterThan(0.05);

            RelationshipsFixtures.Rejects(
                () => tools.BreuschPaganHeteroscedasticityTest(rows, y, null, (HeteroscedasticityVariant)4), "variant");

            RelationshipsFixtures.Rejects(() => tools.BreuschPaganHeteroscedasticityTest(rows, [.. y.Take(3)]),
                "outputs");

            RelationshipsFixtures.Rejects(
                () => tools.BreuschPaganHeteroscedasticityTest([.. rows.Take(4)], [.. y.Take(4)]), "inputs");
        }

        [Fact]
        [Trait("Covers", "WhiteHeteroscedasticityTest")]
        public void WhiteMatchesTheAuxiliaryRegressionWithSquaresAndCrossProducts()
        {
            var (rows, y) = Trend(true);
            var fit = linear.FitMultipleLinearRegression(rows, y, null, true, 0.95, true);
            var squared = fit.Residuals!.Select(e => e * e).ToArray();
            var auxRows = rows.Select(r => new[] { r[0], r[1], r[0] * r[0], r[0] * r[1], r[1] * r[1] }).ToArray();
            var aux = linear.FitMultipleLinearRegression(auxRows, squared);

            var result = tools.WhiteHeteroscedasticityTest(rows, y);

            result.Statistic.Should().BeApproximately(80 * aux.RSquared, 1e-7);
            result.DegreesOfFreedom.Should().Be(5);
            result.PValue.Should().BeLessThan(0.01);
            result.Test.Should().Be("White");
        }

        [Fact]
        [Trait("Covers", "WhiteHeteroscedasticityTest")]
        public void WhiteRejectsUnusableInput()
        {
            var (rows, y) = Trend(false);

            tools.WhiteHeteroscedasticityTest(rows, y).PValue.Should().BeGreaterThan(0.05);
            var dummies = rows.Select((r, i) => new[] { r[0], i % 2 == 0 ? 1.0 : 0.0 }).ToArray();
            RelationshipsFixtures.Rejects(() => tools.WhiteHeteroscedasticityTest(dummies, y), "inputs");

            RelationshipsFixtures.Rejects(
                () => tools.WhiteHeteroscedasticityTest([.. rows.Take(6)], [.. y.Take(6)]), "inputs");

            var wide = rows.Select(r => Enumerable.Range(0, 11).Select(j => r[0] * (j + 1) + r[1] * j * j).ToArray())
                .ToArray();

            RelationshipsFixtures.Rejects(() => tools.WhiteHeteroscedasticityTest(wide, y), "inputs");
        }

        [Fact]
        [Trait("Covers", "GoldfeldQuandtTest")]
        public void GoldfeldQuandtMatchesTheHandComputedVarianceRatio()
        {
            var rows = Column([1.0, 2, 3, 4, 5, 6, 7, 8]);
            var y = new[] { 0.0, 1, 0, 1, 0, 4, 0, 4 };

            var result = tools.GoldfeldQuandtTest(rows, y, "x1", null, 0);

            result.Estimate1.Should().BeApproximately(0.4, 1e-10);
            result.Estimate2.Should().BeApproximately(6.4, 1e-10);
            result.Statistic.Should().BeApproximately(16, 1e-9);
            result.DegreesOfFreedom.Should().Be(2);
            result.DegreesOfFreedom2.Should().Be(2);
            result.PValue.Should().BeApproximately(1d / 17, 1e-9);

            var two = tools.GoldfeldQuandtTest(rows, y, "x1", null, 0, true);
            two.PValue.Should().BeApproximately(2d / 17, 1e-9);
        }

        [Fact]
        [Trait("Covers", "GoldfeldQuandtTest")]
        public void GoldfeldQuandtSortsByTheChosenVariableAndValidates()
        {
            var (rows, y) = Trend(true);
            var shuffled = Enumerable.Range(0, 80).OrderBy(i => i * 37 % 80).ToArray();
            var mixedRows = shuffled.Select(i => rows[i]).ToArray();
            var mixedY = shuffled.Select(i => y[i]).ToArray();

            var sorted = tools.GoldfeldQuandtTest(rows, y, "x1");
            var mixed = tools.GoldfeldQuandtTest(mixedRows, mixedY, "x1");
            mixed.Statistic.Should().BeApproximately(sorted.Statistic, 1e-9);
            sorted.PValue.Should().BeLessThan(0.01);
            sorted.Conclusion.Should().Contain("larger in the upper");

            var named = tools.GoldfeldQuandtTest(rows, y, "time", ["time", "other"]);
            named.Statistic.Should().BeApproximately(sorted.Statistic, 1e-9);
            RelationshipsFixtures.Rejects(() => tools.GoldfeldQuandtTest(rows, y, "nope"), "sortVariable");
            RelationshipsFixtures.Rejects(() => tools.GoldfeldQuandtTest(rows, y, "x1", null, 0.9), "fractionOmitted");

            RelationshipsFixtures.Rejects(
                () => tools.GoldfeldQuandtTest([.. rows.Take(8)], [.. y.Take(8)], "x1", null, 0.5),
                "fractionOmitted");

            RelationshipsFixtures.Rejects(() => tools.GoldfeldQuandtTest(rows, [.. y.Take(7)], "x1"), "outputs");
        }

        [Fact]
        [Trait("Covers", "ResidualNormalityTests")]
        public void ResidualNormalityMatchesHandComputedMoments()
        {
            var rows = Column([2.0, -1, -2, -1, 2]);
            var y = new[] { -2.0, -1, 0, 1, 2 };

            var result = tools.ResidualNormalityTests(rows, y);

            result.Mean.Should().BeApproximately(0, 1e-12);
            result.Skewness.Should().BeApproximately(0, 1e-12);
            result.ExcessKurtosis.Should().BeApproximately(6.8 / 4 - 3, 1e-9);
            var jb = 5d / 6 * (1.3 * 1.3 / 4);
            result.JarqueBeraStatistic.Should().BeApproximately(jb, 1e-9);
            result.JarqueBeraPValue.Should().BeApproximately(Math.Exp(-jb / 2), 1e-9);
            result.MinimumResidual.Should().BeApproximately(-2, 1e-12);
            result.MaximumResidual.Should().BeApproximately(2, 1e-12);
            result.StandardDeviation.Should().BeApproximately(Math.Sqrt(2.5), 1e-12);
            result.FractionBeyondTwoSigma.Should().Be(0);
            result.ShapiroWilkStatistic.Should().BeInRange(0.8, 1);
        }

        [Fact]
        [Trait("Covers", "ResidualNormalityTests")]
        public void ResidualNormalityDistinguishesNormalFromSkewedResiduals()
        {
            var x = Enumerable.Range(1, 200).Select(i => (double)i).ToArray();

            var normalNoise = Enumerable.Range(1, 200)
                .Select(i => Special.Ierf(2.0 * (i - 0.5) / 200 - 1) * Math.Sqrt(2)).ToArray();

            var shuffled = Enumerable.Range(0, 200).OrderBy(i => i * 73 % 200).Select(i => normalNoise[i]).ToArray();
            var skewed = Enumerable.Range(1, 200).Select(i => -Math.Log(1 - (i - 0.5) / 200)).ToArray();
            var skewShuffled = Enumerable.Range(0, 200).OrderBy(i => i * 73 % 200).Select(i => skewed[i]).ToArray();

            var normal = tools.ResidualNormalityTests(Column(x), [.. x.Select((v, i) => 2 * v + shuffled[i])]);
            normal.JarqueBeraPValue.Should().BeGreaterThan(0.05);
            normal.ShapiroWilkPValue.Should().BeGreaterThan(0.05);
            normal.Conclusion.Should().Contain("No significant");

            var bad = tools.ResidualNormalityTests(Column(x), [.. x.Select((v, i) => 2 * v + skewShuffled[i])]);
            bad.JarqueBeraPValue.Should().BeLessThan(0.001);
            bad.ShapiroWilkPValue.Should().BeLessThan(0.001);
            bad.Skewness.Should().BeGreaterThan(1);

            RelationshipsFixtures.Rejects(() => tools.ResidualNormalityTests(Column(x), [.. x.Take(3)]),
                "outputs");

            RelationshipsFixtures.Rejects(
                () => tools.ResidualNormalityTests(Column([1.0, 2, 3]), [1.0, 2, 3]), "inputs");

            var exact = tools.ResidualNormalityTests(Column(x), [.. x.Select(v => 3 * v + 1)]);
            exact.StandardDeviation.Should().BeLessThan(1e-6);
        }

        [Fact]
        [Trait("Covers", "RamseyResetTest")]
        public void ResetMatchesTheNestedFTestOnFittedPowers()
        {
            var x = Enumerable.Range(1, 40).Select(i => i / 4.0).ToArray();
            var rows = x.Select((v, i) => new[] { v, Math.Cos(i) }).ToArray();
            var y = x.Select((v, i) => 1 + v + 0.35 * v * v + 0.4 * Math.Sin(i * 3.3)).ToArray();
            var fit = linear.FitMultipleLinearRegression(rows, y, null, true, 0.95, true);

            var augmented = rows.Select((r, i) => new[]
                { r[0], r[1], Math.Pow(fit.FittedValues![i], 2), Math.Pow(fit.FittedValues[i], 3) }).ToArray();

            var nested = linear.CompareNestedLinearModels(augmented, y, ["x3", "x4"]);

            var result = tools.RamseyResetTest(rows, y);

            result.Statistic.Should().BeApproximately(nested.FStatistic, nested.FStatistic * 1e-7);
            result.PValue.Should().BeApproximately(nested.PValue, 1e-9);
            result.PValue.Should().BeLessThan(1e-6);
            result.DegreesOfFreedom.Should().Be(2);
            result.DegreesOfFreedom2.Should().Be(40 - 5);
            result.Estimate2.Should().BeGreaterThan(result.Estimate1);
        }

        [Fact]
        [Trait("Covers", "RamseyResetTest")]
        public void ResetAcceptsALinearModelAndValidates()
        {
            var x = Enumerable.Range(1, 40).Select(i => i / 4.0).ToArray();
            var rows = x.Select((v, i) => new[] { v, Math.Cos(i) }).ToArray();
            var y = x.Select((v, i) => 1 + v + 0.4 * Math.Sin(i * 3.3)).ToArray();

            tools.RamseyResetTest(rows, y, 2).PValue.Should().BeGreaterThan(0.05);
            RelationshipsFixtures.Rejects(() => tools.RamseyResetTest(rows, y, 1), "power");
            RelationshipsFixtures.Rejects(() => tools.RamseyResetTest(rows, y, 6), "power");

            RelationshipsFixtures.Rejects(() => tools.RamseyResetTest([.. rows.Take(6)], [.. y.Take(6)]),
                "inputs");

            RelationshipsFixtures.Rejects(() => tools.RamseyResetTest(rows, [.. y.Take(9)]), "outputs");
            var exact = rows.Select(r => 2 * r[0] + 1).ToArray();
            RelationshipsFixtures.Rejects(() => tools.RamseyResetTest(rows, exact), "inputs");
        }

        [Fact]
        [Trait("Covers", "ChowStructuralBreakTest")]
        public void ChowMatchesTheHandComputedSegmentResiduals()
        {
            var rows = Column([1.0, 2, 3, 4, 5, 6, 7, 8]);
            var y = new[] { 0.0, 1, 0, 1, 0, 4, 0, 4 };
            var pooled = linear.FitSimpleLinearRegression([1.0, 2, 3, 4, 5, 6, 7, 8], y);

            var result = tools.ChowStructuralBreakTest(rows, y, 4);

            result.Estimate1.Should().BeApproximately(0.8, 1e-10);
            result.Estimate2.Should().BeApproximately(12.8, 1e-10);
            var expected = (pooled.ResidualSumOfSquares - 13.6) / 2 / (13.6 / 4);
            result.Statistic.Should().BeApproximately(expected, 1e-9);
            result.DegreesOfFreedom.Should().Be(2);
            result.DegreesOfFreedom2.Should().Be(4);
            result.Observations.Should().Be(8);
        }

        [Fact]
        [Trait("Covers", "ChowStructuralBreakTest")]
        public void ChowDetectsABreakAndValidates()
        {
            var x = Enumerable.Range(0, 40).Select(i => (double)i).ToArray();
            var noise = x.Select((_, i) => Math.Sin(i * 5.3) * 0.3).ToArray();
            var broken = x.Select((v, i) => v < 20 ? 1 + v + noise[i] : 30 - 0.5 * v + noise[i]).ToArray();
            var stable = x.Select((v, i) => 1 + v + noise[i]).ToArray();

            tools.ChowStructuralBreakTest(Column(x), broken, 20).PValue.Should().BeLessThan(1e-6);
            tools.ChowStructuralBreakTest(Column(x), stable, 20).PValue.Should().BeGreaterThan(0.05);
            RelationshipsFixtures.Rejects(() => tools.ChowStructuralBreakTest(Column(x), broken, 2), "breakIndex");
            RelationshipsFixtures.Rejects(() => tools.ChowStructuralBreakTest(Column(x), broken, 39), "breakIndex");

            RelationshipsFixtures.Rejects(() => tools.ChowStructuralBreakTest(Column(x), [.. broken.Take(5)], 2),
                "outputs");
        }

        [Fact]
        [Trait("Covers", "VarianceInflationFactors")]
        public void VarianceInflationMatchesTheTwoPredictorClosedFormAndLongley()
        {
            var x = RelationshipsFixtures.AnscombeOneX;
            var y = RelationshipsFixtures.AnscombeOneY;
            var r = 0.8164205;

            var pair = tools.VarianceInflationFactors([.. x.Select((v, i) => new[] { v, y[i] })],
                ["a", "b"]);

            pair.Variables[0].VarianceInflationFactor.Should().BeApproximately(1 / (1 - r * r), 1e-5);
            pair.Variables[1].VarianceInflationFactor.Should().BeApproximately(1 / (1 - r * r), 1e-5);
            pair.Variables[0].Tolerance.Should().BeApproximately(1 - r * r, 1e-5);
            pair.Variables[0].StandardErrorInflation.Should().BeApproximately(Math.Sqrt(1 / (1 - r * r)), 1e-5);
            pair.Variables[0].Severity.Should().Be("Low");

            var longley = tools.VarianceInflationFactors(RelationshipsFixtures.LongleyPredictors,
                RelationshipsFixtures.LongleyNames);

            longley.Variables[1].VarianceInflationFactor.Should().BeApproximately(1788.513, 1.8);
            longley.Variables[1].Severity.Should().Be("High");
            longley.Variables[3].VarianceInflationFactor.Should().BeApproximately(3.589, 0.01);
            longley.Variables[3].Severity.Should().Be("Low");
            longley.MaximumVif.Should().BeApproximately(1788.513, 1.8);
            longley.Summary.Should().Contain("above 10");
        }

        [Fact]
        [Trait("Covers", "VarianceInflationFactors")]
        public void VarianceInflationHandlesSingleExactlyCollinearAndConstantColumns()
        {
            tools.VarianceInflationFactors(Column([1.0, 2, 3, 4, 5])).Variables[0].VarianceInflationFactor
                .Should().Be(1);

            var collinear = Enumerable.Range(1, 10).Select(i => new[] { i, 2.0 * i + 1, Math.Sin(i * 1.7) })
                .ToArray();

            var result = tools.VarianceInflationFactors(collinear);
            double.IsInfinity(result.Variables[0].VarianceInflationFactor).Should().BeTrue();
            double.IsInfinity(result.Variables[1].VarianceInflationFactor).Should().BeTrue();
            result.Variables[2].VarianceInflationFactor.Should().BeLessThan(2);

            var constant = Enumerable.Range(1, 10).Select(i => new[] { i, 5.0, Math.Sin(i) }).ToArray();

            double.IsNaN(tools.VarianceInflationFactors(constant).Variables[1].VarianceInflationFactor).Should()
                .BeTrue();

            RelationshipsFixtures.Rejects(() => tools.VarianceInflationFactors([]), "inputs");
            RelationshipsFixtures.Rejects(() => tools.VarianceInflationFactors([.. collinear.Take(3)]), "inputs");

            RelationshipsFixtures.Rejects(() => tools.VarianceInflationFactors(collinear, ["a", "b"]),
                "variableNames");
        }

        [Fact]
        [Trait("Covers", "MulticollinearityConditionDiagnostics")]
        public void ConditionIndicesMatchTheTwoColumnClosedForm()
        {
            var x1 = new[] { 1.0, 2, 3 };
            var x2 = new[] { 1.0, 2, 2.5 };
            var cosine = (1 + 4 + 7.5) / Math.Sqrt(14 * 11.25);

            var result =
                tools.MulticollinearityConditionDiagnostics([.. x1.Select((v, i) => new[] { v, x2[i] })], null,
                    false);

            result.Rows.Should().HaveCount(2);
            result.Rows[0].ConditionIndex.Should().BeApproximately(1, 1e-9);
            result.Rows[1].ConditionIndex.Should().BeApproximately(Math.Sqrt((1 + cosine) / (1 - cosine)), 1e-6);
            result.ConditionNumber.Should().BeApproximately(result.Rows[1].ConditionIndex, 1e-9);
            result.Rows[1].VarianceProportions[0].Should().BeApproximately((1 + cosine) / 2, 1e-6);
            result.Rows[1].VarianceProportions[1].Should().BeApproximately((1 + cosine) / 2, 1e-6);
            result.Rows[1].Severity.Should().Be("Moderate");
            result.NumericalRank.Should().Be(2);
            result.Summary.Should().Contain("moderate");
        }

        [Fact]
        [Trait("Covers", "MulticollinearityConditionDiagnostics")]
        public void ConditionIndicesForOrthogonalAndLongleyDesigns()
        {
            var orthogonal = new[] { new[] { 1.0, 1 }, new[] { 1.0, -1 }, new[] { -1.0, 1 }, new[] { -1.0, -1 } };
            var flat = tools.MulticollinearityConditionDiagnostics(orthogonal, null, false);
            flat.Rows.Should().OnlyContain(r => Math.Abs(r.ConditionIndex - 1) < 1e-9);
            flat.Rows.All(r => Math.Abs(r.VarianceProportions.Length - 2) == 0).Should().BeTrue();
            flat.Determinant.Should().BeApproximately(1, 1e-9);
            flat.Summary.Should().Contain("no meaningful");

            var longley = tools.MulticollinearityConditionDiagnostics(RelationshipsFixtures.LongleyPredictors,
                RelationshipsFixtures.LongleyNames);
            longley.Terms.Should().HaveCount(7);
            longley.ConditionNumber.Should().BeGreaterThan(1000);
            longley.Rows.Last().Severity.Should().Be("Severe");
            longley.Rows.Last().VarianceProportions.Count(p => p > 0.5).Should().BeGreaterOrEqualTo(2);

            for (var j = 0; j < 7; j++)
            {
                longley.Rows.Sum(r => r.VarianceProportions[j]).Should().BeApproximately(1, 1e-9);
            }

            longley.CorrelationMatrixConditionNumber.Should().BeGreaterThan(100);
            longley.Determinant.Should().BeLessThan(1e-6);
        }

        [Fact]
        [Trait("Covers", "MulticollinearityConditionDiagnostics")]
        public void ConditionDiagnosticsValidates()
        {
            RelationshipsFixtures.Rejects(() => tools.MulticollinearityConditionDiagnostics([]),
                "inputs");

            RelationshipsFixtures.Rejects(
                () => tools.MulticollinearityConditionDiagnostics([
                    [1.0, 0], [2.0, 0], [3.0, 0], [4.0, 0]
                ]), "inputs");

            RelationshipsFixtures.Rejects(
                () => tools.MulticollinearityConditionDiagnostics([
                    [1.0, 2], [2.0, double.NaN], [3.0, 1], [4.0, 5]
                ]), "inputs");

            var exact = Enumerable.Range(1, 8).Select(i => new[] { i, 2.0 * i }).ToArray();
            var singular = tools.MulticollinearityConditionDiagnostics(exact, null, false);
            singular.NumericalRank.Should().Be(1);
            singular.ConditionNumber.Should().BeGreaterThan(1e8);
        }

        [Fact]
        [Trait("Covers", "AddedVariablePlotData")]
        public void AddedVariablePlotSlopeEqualsTheMultipleRegressionCoefficient()
        {
            var rows = RelationshipsFixtures.AnscombeOneX
                .Select((v, i) => new[] { v, Math.Sin(i * 1.9) * 3, Math.Cos(i * 0.6) }).ToArray();

            var y = RelationshipsFixtures.AnscombeOneY;
            var full = linear.FitMultipleLinearRegression(rows, y, ["a", "b", "c"]);

            var plot = tools.AddedVariablePlotData(rows, y, "b", ["a", "b", "c"]);

            plot.Slope.Should().BeApproximately(full.Coefficients[2].Estimate, 1e-9);
            plot.XResiduals.Should().HaveCount(11);
            plot.XResiduals.Sum().Should().BeApproximately(0, 1e-9);
            plot.YResiduals.Sum().Should().BeApproximately(0, 1e-9);

            var partial = new CorrelationTools().PartialCorrelation([.. rows.Select(r => r[1])], y,
                [.. rows.Select(r => new[] { r[0], r[2] })]);

            plot.PartialCorrelation.Should().BeApproximately(partial.Coefficient, 1e-9);
            plot.KeptRowIndices.Should().Equal(Enumerable.Range(0, 11));
        }

        [Fact]
        [Trait("Covers", "AddedVariablePlotData")]
        public void AddedVariablePlotValidates()
        {
            var rows = RelationshipsFixtures.AnscombeOneX.Select((v, i) => new[] { v, Math.Cos(i * 0.6) }).ToArray();
            var y = RelationshipsFixtures.AnscombeOneY;

            RelationshipsFixtures.Rejects(() => tools.AddedVariablePlotData(rows, y, "zzz"), "variable");

            RelationshipsFixtures.Rejects(
                () => tools.AddedVariablePlotData(Column(RelationshipsFixtures.AnscombeOneX), y, "x1"), "inputs");

            RelationshipsFixtures.Rejects(() => tools.AddedVariablePlotData(rows, [.. y.Take(3)], "x1"),
                "outputs");

            tools.AddedVariablePlotData(rows, y, "x2").Observations.Should().Be(11);
        }

        [Fact]
        [Trait("Covers", "CheckLinearRegressionAssumptions")]
        public void AssumptionCheckPassesAWellBehavedModelAndFlagsAProblemModel()
        {
            var (rows, good) = Trend(false);
            var independentRows = rows.Select((r, i) => new[] { r[0], Math.Sin(i * 2.13) }).ToArray();

            var goodY = independentRows.Select((r, i) => 3 + 1.5 * r[0] + 0.5 * r[1] + Math.Sin(i * 12.9898 + 1) * 0.6)
                .ToArray();

            var healthy = tools.CheckLinearRegressionAssumptions(independentRows, goodY);

            healthy.Checks.Select(c => c.Assumption).Should().Contain([
                "Linearity", "Independence", "Constant variance", "Normality of residuals", "Multicollinearity",
                "Influence"
            ]);

            healthy.Checks.Single(c => c.Assumption == "Multicollinearity").Passed.Should().BeTrue();
            healthy.Checks.Single(c => c.Assumption == "Constant variance").Passed.Should().BeTrue();
            healthy.Observations.Should().Be(80);

            var (hetRows, hetY) = Trend(true);
            var curved = hetRows.Select((r, i) => hetY[i] + 0.08 * r[0] * r[0]).ToArray();
            var problem = tools.CheckLinearRegressionAssumptions(hetRows, curved);
            problem.Checks.Single(c => c.Assumption == "Constant variance").Passed.Should().BeFalse();
            problem.Checks.Single(c => c.Assumption == "Linearity").Passed.Should().BeFalse();
            problem.FailedCount.Should().BeGreaterOrEqualTo(2);
            problem.Summary.Should().Contain("failed");
            good.Should().HaveCount(80);
        }

        [Fact]
        [Trait("Covers", "CheckLinearRegressionAssumptions")]
        public void AssumptionCheckReportsUncomputableChecksAndValidates()
        {
            var x = Enumerable.Range(1, 12).Select(i => (double)i).ToArray();
            var y = x.Select((v, i) => 2 * v + Math.Sin(i * 3.1)).ToArray();

            var small = tools.CheckLinearRegressionAssumptions(Column(x), y);
            small.Checks.Should().HaveCount(7);

            var dummy = Enumerable.Range(0, 20).Select(i => new[] { i % 2 == 0 ? 1.0 : 0.0 }).ToArray();
            var dummyY = Enumerable.Range(0, 20).Select(i => (i % 2 == 0 ? 3.0 : 1.0) + Math.Sin(i * 2.7)).ToArray();
            var uncomputable = tools.CheckLinearRegressionAssumptions(dummy, dummyY);
            var linearity = uncomputable.Checks.Single(c => c.Assumption == "Linearity");
            linearity.Passed.Should().BeFalse();
            linearity.Comment.Should().StartWith("Could not be computed");
            double.IsNaN(linearity.Statistic).Should().BeTrue();

            var flat = tools.CheckLinearRegressionAssumptions(Column(x), [.. x.Select(v => 5.0 + 0 * v)]);
            flat.Checks.Should().NotBeEmpty();

            RelationshipsFixtures.Rejects(() => tools.CheckLinearRegressionAssumptions(Column(x), [.. y.Take(3)]),
                "outputs");

            RelationshipsFixtures.Rejects(
                () => tools.CheckLinearRegressionAssumptions(Column([.. x.Take(4)]), [.. y.Take(4)]),
                "inputs");
        }
    }
}