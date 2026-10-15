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
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class RelationshipsAndRegressionCorrelationTests
    {
        private readonly CorrelationTools tools = new();

        [Fact]
        [Trait("Covers", "PearsonCorrelation")]
        public void PearsonMatchesAnscombeQuartetOne()
        {
            var result =
                tools.PearsonCorrelation(RelationshipsFixtures.AnscombeOneX, RelationshipsFixtures.AnscombeOneY);

            result.Coefficient.Should().BeApproximately(0.8164205, 1e-6);
            result.CoefficientOfDetermination.Should().BeApproximately(0.6665, 1e-4);
            result.Statistic.Should().BeApproximately(0.8164205 * Math.Sqrt(9 / (1 - 0.8164205 * 0.8164205)), 1e-5);
            result.DegreesOfFreedom.Should().Be(9);
            result.PValue.Should().BeApproximately(0.002170, 2e-5);
            result.PairCount.Should().Be(11);
            var z = Math.Atanh(result.Coefficient);
            result.ConfidenceLower.Should().BeApproximately(Math.Tanh(z - 1.959964 / Math.Sqrt(8)), 1e-5);
            result.ConfidenceUpper.Should().BeApproximately(Math.Tanh(z + 1.959964 / Math.Sqrt(8)), 1e-5);
        }

        [Fact]
        [Trait("Covers", "PearsonCorrelation")]
        public void PearsonEdgeCases()
        {
            var perfect = tools.PearsonCorrelation([1.0, 2, 3, 4], [2.0, 4, 6, 8]);
            perfect.Coefficient.Should().BeApproximately(1, 1e-12);
            perfect.PValue.Should().Be(0);

            var constant = tools.PearsonCorrelation([1.0, 2, 3, 4], [5.0, 5, 5, 5]);
            double.IsNaN(constant.Coefficient).Should().BeTrue();
            constant.Interpretation.Should().Contain("undefined");

            var huge = tools.PearsonCorrelation([1e150, 2e150, 3e150, 4e150],
                [1e-150, 3e-150, 2e-150, 5e-150]);
            var small = tools.PearsonCorrelation([1.0, 2, 3, 4], [1.0, 3, 2, 5]);
            huge.Coefficient.Should().BeApproximately(small.Coefficient, 1e-9);

            var dropped = tools.PearsonCorrelation([1.0, 2, 3, 4, double.NaN], [2.0, 4, 6, 8, 1], 0.95,
                true);
            dropped.PairCount.Should().Be(4);
            dropped.MissingPairsDropped.Should().Be(1);

            RelationshipsFixtures.Rejects(
                () => tools.PearsonCorrelation([1.0, double.NaN, 3], [1.0, 2, 3]), "x");

            RelationshipsFixtures.Rejects(() => tools.PearsonCorrelation([1.0, 2, 3], [1.0, 2]), "y");
            RelationshipsFixtures.Rejects(() => tools.PearsonCorrelation([1.0, 2], [1.0, 2]), "x");

            RelationshipsFixtures.Rejects(() => tools.PearsonCorrelation([], []),
                "x");

            RelationshipsFixtures.Rejects(() => tools.PearsonCorrelation([1.0, 2, 3], [1.0, 2, 3], 1.5),
                "confidenceLevel");

            RelationshipsFixtures.Rejects(
                () => tools.PearsonCorrelation([1.0, double.PositiveInfinity, 3], [1.0, 2, 3]), "x");
        }

        [Fact]
        [Trait("Covers", "SpearmanRankCorrelation")]
        public void SpearmanMatchesTheClosedFormOnAnscombeQuartetOne()
        {
            const double sumSquaredRankDifferences = 40;
            var expected = 1 - 6 * sumSquaredRankDifferences / (11 * (11 * 11 - 1));

            var result =
                tools.SpearmanRankCorrelation(RelationshipsFixtures.AnscombeOneX, RelationshipsFixtures.AnscombeOneY);

            result.Coefficient.Should().BeApproximately(expected, 1e-12);
            result.Statistic.Should().BeApproximately(expected * Math.Sqrt(9 / (1 - expected * expected)), 1e-9);
            var z = Math.Atanh(expected);
            var se = Math.Sqrt((1 + expected * expected / 2) / 8);
            result.ConfidenceLower.Should().BeApproximately(Math.Tanh(z - 1.959964 * se), 1e-5);
        }

        [Fact]
        [Trait("Covers", "SpearmanRankCorrelation")]
        public void SpearmanIsInvariantToMonotoneTransformsAndHandlesTies()
        {
            var x = new[] { 1.0, 2, 3, 4, 5, 6 };
            var y = new[] { 1.0, 4, 9, 16, 25, 36 };
            tools.SpearmanRankCorrelation(x, y).Coefficient.Should().BeApproximately(1, 1e-12);

            var tied = tools.SpearmanRankCorrelation([1.0, 2, 2, 3], [1.0, 2, 3, 3]);
            tied.Coefficient.Should().BeApproximately(5d / 6d, 1e-12);

            var constant = tools.SpearmanRankCorrelation([1.0, 2, 3], [4.0, 4, 4]);
            double.IsNaN(constant.Coefficient).Should().BeTrue();
            RelationshipsFixtures.Rejects(() => tools.SpearmanRankCorrelation([1.0, 2], [1.0, 2]), "x");
            RelationshipsFixtures.Rejects(() => tools.SpearmanRankCorrelation([1.0, 2, 3], [1.0]), "y");
        }

        [Fact]
        [Trait("Covers", "KendallTauCorrelation")]
        public void KendallCountsMatchTheHandCountOnAnscombeQuartetOne()
        {
            var result =
                tools.KendallTauCorrelation(RelationshipsFixtures.AnscombeOneX, RelationshipsFixtures.AnscombeOneY);

            result.ConcordantPairs.Should().Be(45);
            result.DiscordantPairs.Should().Be(10);
            result.TauB.Should().BeApproximately(35d / 55d, 1e-12);
            result.TauA.Should().BeApproximately(35d / 55d, 1e-12);
            result.ZStatistic.Should().BeApproximately(35 / Math.Sqrt(165), 1e-9);
            result.PValue.Should().BeApproximately(0.00643, 2e-4);
        }

        [Fact]
        [Trait("Covers", "KendallTauCorrelation")]
        public void KendallHandlesTiesAndVariants()
        {
            var x = new[] { 1.0, 2, 2, 3 };
            var y = new[] { 1.0, 2, 3, 3 };

            var b = tools.KendallTauCorrelation(x, y);
            b.ConcordantPairs.Should().Be(4);
            b.DiscordantPairs.Should().Be(0);
            b.PairsTiedInXOnly.Should().Be(1);
            b.PairsTiedInYOnly.Should().Be(1);
            b.TauB.Should().BeApproximately(0.8, 1e-12);
            b.TauA.Should().BeApproximately(4d / 6d, 1e-12);
            b.TauC.Should().BeApproximately(0.75, 1e-12);
            b.Tau.Should().BeApproximately(0.8, 1e-12);
            b.ZStatistic.Should().BeApproximately(4 / Math.Sqrt(41d / 6d), 1e-9);

            tools.KendallTauCorrelation(x, y, KendallTauVariant.TauC).Tau.Should().BeApproximately(0.75, 1e-12);
            tools.KendallTauCorrelation(x, y, KendallTauVariant.TauA).Tau.Should().BeApproximately(4d / 6d, 1e-12);

            var constant = tools.KendallTauCorrelation([1.0, 2, 3], [2.0, 2, 2]);
            double.IsNaN(constant.TauB).Should().BeTrue();
            RelationshipsFixtures.Rejects(() => tools.KendallTauCorrelation([1.0, 2], [1.0, 2]), "x");
            RelationshipsFixtures.Rejects(() => tools.KendallTauCorrelation(x, y, (KendallTauVariant)9), "variant");
            RelationshipsFixtures.Rejects(() => tools.KendallTauCorrelation(new double[30001], new double[30001]), "x");
        }

        [Fact]
        [Trait("Covers", "PointBiserialCorrelation")]
        public void PointBiserialMatchesTheHandComputation()
        {
            var group = new[] { 0, 0, 0, 1, 1, 1 };
            var values = new[] { 1.0, 2, 3, 4, 5, 6 };

            var result = tools.PointBiserialCorrelation(group, values);

            result.Coefficient.Should().BeApproximately(4.5 / Math.Sqrt(1.5 * 17.5), 1e-12);
            result.Group1Mean.Should().Be(5);
            result.Group0Mean.Should().Be(2);
            result.PooledStandardDeviation.Should().BeApproximately(1, 1e-12);
            result.TStatistic.Should().BeApproximately(3 / (1 * Math.Sqrt(2d / 3d)), 1e-9);
            result.DegreesOfFreedom.Should().Be(4);
            var t = 3 / Math.Sqrt(2d / 3d);
            var x = t / Math.Sqrt(1 + t * t / 4);
            var cdf = 0.5 + 3d / 8d * x * (1 - x * x / 12);
            result.PValue.Should().BeApproximately(2 * (1 - cdf), 1e-6);
        }

        [Fact]
        [Trait("Covers", "PointBiserialCorrelation")]
        public void PointBiserialRejectsBadFlagsAndMissing()
        {
            RelationshipsFixtures.Rejects(() => tools.PointBiserialCorrelation([0, 2, 1], [1.0, 2, 3]),
                "group");
            RelationshipsFixtures.Rejects(() => tools.PointBiserialCorrelation([1, 1, 1], [1.0, 2, 3]),
                "group");

            RelationshipsFixtures.Rejects(() => tools.PointBiserialCorrelation([0, 1], [1.0, 2, 3]),
                "group");

            RelationshipsFixtures.Rejects(
                () => tools.PointBiserialCorrelation([0, 1, 1], [1.0, double.NaN, 3]), "values");

            var dropped = tools.PointBiserialCorrelation([0, 0, 1, 1, 1], [1.0, 2, 3, double.NaN, 5],
                0.95, true);
            dropped.MissingPairsDropped.Should().Be(1);
            dropped.Group1Count.Should().Be(2);

            var constant = tools.PointBiserialCorrelation([0, 0, 1, 1], [3.0, 3, 3, 3]);
            double.IsNaN(constant.Coefficient).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "BiserialCorrelation")]
        public void BiserialMatchesTheHandComputation()
        {
            var group = new[] { 0, 0, 0, 1, 1, 1 };
            var values = new[] { 1.0, 2, 3, 4, 5, 6 };

            var result = tools.BiserialCorrelation(group, values);

            var expected = 3 / Math.Sqrt(17.5 / 6) * 0.25 / 0.3989422804;
            result.Coefficient.Should().BeApproximately(expected, 1e-6);
            result.Method.Should().Be("Biserial");
            double.IsNaN(result.DegreesOfFreedom).Should().BeTrue();

            RelationshipsFixtures.Rejects(() => tools.BiserialCorrelation([0, 1, 3], [1.0, 2, 3]),
                "group");
        }

        [Fact]
        [Trait("Covers", "CorrelationMatrix")]
        public void CorrelationMatrixAgreesWithThePairwiseToolsAndRanksPairs()
        {
            var x = RelationshipsFixtures.AnscombeOneX;
            var y = RelationshipsFixtures.AnscombeOneY;
            var rows = x.Select((v, i) => new[] { v, y[i], -2 * v + 1 }).ToArray();

            var result = tools.CorrelationMatrix(rows, ["x", "y", "negx"]);

            result.Coefficients[0][0].Should().Be(1);
            result.Coefficients[0][1].Should().BeApproximately(0.8164205, 1e-6);
            result.Coefficients[1][0].Should().BeApproximately(0.8164205, 1e-6);
            result.Coefficients[0][2].Should().BeApproximately(-1, 1e-12);
            result.PValues[0][1].Should().BeApproximately(0.002170, 2e-5);
            result.PairCounts[0][1].Should().Be(11);
            result.TotalPairs.Should().Be(3);
            result.StrongestPairs[0].VariableA.Should().Be("x");
            result.StrongestPairs[0].VariableB.Should().Be("negx");
            result.StrongestPairs.Select(p => p.AdjustedPValue).Should().OnlyContain(p => p >= 0 && p <= 1);

            var spearman = tools.CorrelationMatrix(rows, ["x", "y", "negx"], CorrelationMethod.Spearman);
            spearman.Coefficients[0][1].Should().BeApproximately(0.8181818, 1e-6);
            var kendall = tools.CorrelationMatrix(rows, ["x", "y", "negx"], CorrelationMethod.Kendall);
            kendall.Coefficients[0][1].Should().BeApproximately(35d / 55d, 1e-12);
        }

        [Fact]
        [Trait("Covers", "CorrelationMatrix")]
        public void CorrelationMatrixHandlesMissingAndConstantColumns()
        {
            var rows = new[]
            {
                new[] { 1.0, 2, 5 },
                new[] { 2.0, 4, 5 },
                new[] { 3.0, double.NaN, 5 },
                new[] { 4.0, 8, 5 },
                new[] { 5.0, 11, 5 }
            };

            var pairwise = tools.CorrelationMatrix(rows);
            pairwise.PairCounts[0][1].Should().Be(4);
            pairwise.PairCounts[0][0].Should().Be(5);
            double.IsNaN(pairwise.Coefficients[0][2]).Should().BeTrue();
            double.IsNaN(pairwise.Coefficients[2][2]).Should().BeTrue();
            pairwise.Variables.Should().Equal("x1", "x2", "x3");

            var listwise = tools.CorrelationMatrix(rows, null, CorrelationMethod.Pearson, false);
            listwise.RowsUsed.Should().Be(4);
            listwise.RowsDropped.Should().Be(1);

            RelationshipsFixtures.Rejects(() => tools.CorrelationMatrix([[1.0], [2.0]]),
                "inputs");

            RelationshipsFixtures.Rejects(() => tools.CorrelationMatrix([[1.0, 2], [2.0]]),
                "inputs");

            RelationshipsFixtures.Rejects(() => tools.CorrelationMatrix(rows, ["a", "a", "b"]),
                "variableNames");

            RelationshipsFixtures.Rejects(() => tools.CorrelationMatrix(rows, ["a", "b"]), "variableNames");
            RelationshipsFixtures.Rejects(() => tools.CorrelationMatrix(rows, null, (CorrelationMethod)9), "method");

            RelationshipsFixtures.Rejects(
                () => tools.CorrelationMatrix(rows, null, CorrelationMethod.Pearson, true, 5000),
                "maximumPairsReported");

            RelationshipsFixtures.Rejects(() => tools.CorrelationMatrix([]), "inputs");
        }

        [Fact]
        [Trait("Covers", "CovarianceMatrix")]
        public void CovarianceMatrixMatchesTheClosedForm()
        {
            var rows = new[] { new[] { 1.0, 2 }, new[] { 2.0, 4 }, new[] { 3.0, 6 }, new[] { 4.0, 8 } };

            var sample = tools.CovarianceMatrix(rows, ["a", "b"]);

            sample.Covariance[0][0].Should().BeApproximately(5d / 3d, 1e-12);
            sample.Covariance[0][1].Should().BeApproximately(10d / 3d, 1e-12);
            sample.Covariance[1][1].Should().BeApproximately(20d / 3d, 1e-12);
            sample.Means.Should().Equal(2.5, 5.0);
            sample.TotalVariance.Should().BeApproximately(25d / 3d, 1e-12);
            sample.GeneralisedVariance.Should().BeApproximately(0, 1e-9);
            sample.StandardDeviations[0].Should().BeApproximately(Math.Sqrt(5d / 3d), 1e-12);

            var population = tools.CovarianceMatrix(rows, null, false);
            population.Covariance[0][0].Should().BeApproximately(1.25, 1e-12);
            population.Covariance[0][1].Should().BeApproximately(2.5, 1e-12);
        }

        [Fact]
        [Trait("Covers", "CovarianceMatrix")]
        public void CovarianceMatrixEdgeCases()
        {
            var rows = new[] { new[] { 1.0, 2 }, new[] { 2.0, double.NaN }, new[] { 3.0, 6 }, new[] { 4.0, 9 } };

            RelationshipsFixtures.Rejects(() => tools.CovarianceMatrix(rows), "inputs");
            var result = tools.CovarianceMatrix(rows, null, true, true);
            result.RowsUsed.Should().Be(3);
            result.RowsDropped.Should().Be(1);
            RelationshipsFixtures.Rejects(() => tools.CovarianceMatrix([[1.0, 2]]), "inputs");

            RelationshipsFixtures.Rejects(
                () => tools.CovarianceMatrix([[1.0, double.PositiveInfinity], [2.0, 3]]),
                "inputs");
        }

        private static (double[] X, double[] Y, double[][] Z) ControlledData()
        {
            var x = new[] { 1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var z = new[] { 2.0, 1, 4, 3, 6, 5, 8, 7, 10, 9 };
            var y = new[] { 3.0, 2, 5, 7, 8, 9, 12, 11, 14, 17 };

            return (x, y, [.. z.Select(v => new[] { v })]);
        }

        [Fact]
        [Trait("Covers", "PartialCorrelation")]
        public void PartialCorrelationMatchesTheFirstOrderFormula()
        {
            var (x, y, z) = ControlledData();
            var zs = z.Select(r => r[0]).ToArray();
            var rxy = tools.PearsonCorrelation(x, y).Coefficient;
            var rxz = tools.PearsonCorrelation(x, zs).Coefficient;
            var ryz = tools.PearsonCorrelation(y, zs).Coefficient;
            var expected = (rxy - rxz * ryz) / Math.Sqrt((1 - rxz * rxz) * (1 - ryz * ryz));

            var result = tools.PartialCorrelation(x, y, z);

            result.Coefficient.Should().BeApproximately(expected, 1e-10);
            result.ZeroOrderCorrelation.Should().BeApproximately(rxy, 1e-12);
            result.DegreesOfFreedom.Should().Be(7);
            result.ControlCount.Should().Be(1);
            result.TStatistic.Should().BeApproximately(expected * Math.Sqrt(7 / (1 - expected * expected)), 1e-9);

            result.ConfidenceLower.Should()
                .BeApproximately(Math.Tanh(Math.Atanh(expected) - 1.959964 / Math.Sqrt(6)), 1e-5);
        }

        [Fact]
        [Trait("Covers", "PartialCorrelation")]
        public void PartialCorrelationEdgeCases()
        {
            var (x, y, z) = ControlledData();

            var spearman = tools.PartialCorrelation(x, y, z, CorrelationMethod.Spearman);
            spearman.Method.Should().Be("PartialSpearman");
            RelationshipsFixtures.Rejects(() => tools.PartialCorrelation(x, y, z, CorrelationMethod.Kendall), "method");
            RelationshipsFixtures.Rejects(() => tools.PartialCorrelation(x, [.. y.Take(5)], z), "y");
            RelationshipsFixtures.Rejects(() => tools.PartialCorrelation(x, y, [.. z.Take(5)]), "controls");

            RelationshipsFixtures.Rejects(
                () => tools.PartialCorrelation([1.0, 2, 3], [1.0, 2, 3],
                    [[1.0], [2.0], [4.0]]), "x");

            var withGap = (double[])x.Clone();
            withGap[2] = double.NaN;
            RelationshipsFixtures.Rejects(() => tools.PartialCorrelation(withGap, y, z), "x");
            tools.PartialCorrelation(withGap, y, z, CorrelationMethod.Pearson, 0.95, true).RowsDropped.Should().Be(1);

            RelationshipsFixtures.Rejects(() => tools.PartialCorrelation(x, y, z, CorrelationMethod.Pearson, 2),
                "level");
        }

        [Fact]
        [Trait("Covers", "SemiPartialCorrelation")]
        public void SemiPartialCorrelationMatchesTheFirstOrderFormula()
        {
            var (x, y, z) = ControlledData();
            var zs = z.Select(r => r[0]).ToArray();
            var rxy = tools.PearsonCorrelation(x, y).Coefficient;
            var rxz = tools.PearsonCorrelation(x, zs).Coefficient;
            var ryz = tools.PearsonCorrelation(y, zs).Coefficient;
            var expected = (rxy - ryz * rxz) / Math.Sqrt(1 - rxz * rxz);

            var result = tools.SemiPartialCorrelation(x, y, z);

            result.Coefficient.Should().BeApproximately(expected, 1e-10);
            result.DegreesOfFreedom.Should().Be(7);

            var full = new LinearRegressionTools().FitMultipleLinearRegression(
                [.. x.Select((v, i) => new[] { v, zs[i] })], y, ["x", "z"]);

            result.PValue.Should().BeApproximately(full.Coefficients[1].PValue, 1e-9);

            (result.Coefficient * result.Coefficient).Should().BeApproximately(
                full.RSquared - tools.PearsonCorrelation(y, zs).CoefficientOfDetermination, 1e-9);

            RelationshipsFixtures.Rejects(() => tools.SemiPartialCorrelation(x, y, z, CorrelationMethod.Kendall),
                "method");
        }

        [Fact]
        [Trait("Covers", "PartialCorrelationMatrix")]
        public void PartialCorrelationMatrixMatchesThePartialCorrelationTool()
        {
            var (x, y, z) = ControlledData();
            var rows = x.Select((v, i) => new[] { v, y[i], z[i][0] }).ToArray();

            var matrix = tools.PartialCorrelationMatrix(rows, ["x", "y", "z"]);
            var single = tools.PartialCorrelation(x, y, z);

            matrix.Coefficients[0][1].Should().BeApproximately(single.Coefficient, 1e-9);
            matrix.Coefficients[1][0].Should().BeApproximately(single.Coefficient, 1e-9);
            matrix.PValues[0][1].Should().BeApproximately(single.PValue, 1e-9);
            matrix.Coefficients[2][2].Should().Be(1);
            matrix.DegreesOfFreedom.Should().Be(7);
        }

        [Fact]
        [Trait("Covers", "PartialCorrelationMatrix")]
        public void PartialCorrelationMatrixRejectsCollinearAndSmallInputs()
        {
            var collinear = Enumerable.Range(1, 10).Select(i => new[] { i, 2.0 * i, Math.Sin(i) }).ToArray();
            RelationshipsFixtures.Rejects(() => tools.PartialCorrelationMatrix(collinear), "inputs");

            RelationshipsFixtures.Rejects(
                () => tools.PartialCorrelationMatrix([[1.0, 2], [2.0, 3]]), "inputs");

            RelationshipsFixtures.Rejects(
                () => tools.PartialCorrelationMatrix([[1.0, 2, 3], [2.0, 3, 1], [3.0, 1, 2]]), "inputs");
        }

        [Fact]
        [Trait("Covers", "MutualInformationEstimate")]
        public void MutualInformationOfAFunctionalNonLinearRelationEqualsTheEntropyOfTheOutput()
        {
            var x = new[] { -1.0, -1, 0, 0, 1, 1 };
            var y = x.Select(v => v * v).ToArray();

            var result = tools.MutualInformationEstimate(x, y, 3);

            var expected = Math.Log(3) - 2d / 3d * Math.Log(2);
            result.MutualInformationNats.Should().BeApproximately(expected, 1e-12);
            result.MutualInformationBits.Should().BeApproximately(expected / Math.Log(2), 1e-12);
            result.EntropyYBits.Should().BeApproximately(expected / Math.Log(2), 1e-12);
            result.NormalisedMutualInformation.Should().BeApproximately(Math.Sqrt(expected / Math.Log(3)), 1e-9);
            tools.PearsonCorrelation(x, y).Coefficient.Should().BeApproximately(0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "MutualInformationEstimate")]
        public void MutualInformationEdgeCases()
        {
            var independent = tools.MutualInformationEstimate([0.0, 0, 1, 1], [0.0, 1, 0, 1], 2);
            independent.MutualInformationNats.Should().BeApproximately(0, 1e-12);
            independent.PValue.Should().BeApproximately(1, 1e-9);

            var identical = tools.MutualInformationEstimate([0.0, 0, 1, 1], [0.0, 0, 1, 1], 2);
            identical.MutualInformationBits.Should().BeApproximately(1, 1e-12);

            var constant = tools.MutualInformationEstimate([1.0, 2, 3, 4, 5], [2.0, 2, 2, 2, 2]);
            constant.MutualInformationNats.Should().BeApproximately(0, 1e-12);
            double.IsNaN(constant.NormalisedMutualInformation).Should().BeTrue();

            var corrected = tools.MutualInformationEstimate([0.0, 0, 1, 1, 0, 1, 0, 1],
                [0.0, 1, 0, 1, 1, 0, 0, 1], 2, MutualInformationBinning.EqualWidth, true);
            corrected.BiasCorrected.Should().BeTrue();
            corrected.MutualInformationNats.Should().Be(0);

            var byFrequency = tools.MutualInformationEstimate([1.0, 2, 3, 4, 5, 6, 7, 8],
                [1.0, 2, 3, 4, 5, 6, 7, 8], 4, MutualInformationBinning.EqualFrequency);
            byFrequency.MutualInformationBits.Should().BeApproximately(2, 1e-12);

            RelationshipsFixtures.Rejects(
                () => tools.MutualInformationEstimate([1.0, 2, 3], [1.0, 2, 3]), "x");

            RelationshipsFixtures.Rejects(
                () => tools.MutualInformationEstimate([1.0, 2, 3, 4], [1.0, 2, 3, 4], 1), "bins");

            RelationshipsFixtures.Rejects(
                () => tools.MutualInformationEstimate([1.0, 2, 3, 4], [1.0, 2, 3, 4], 2,
                    (MutualInformationBinning)5), "binning");

            RelationshipsFixtures.Rejects(
                () => tools.MutualInformationEstimate([1.0, 2, 3, double.NaN], [1.0, 2, 3, 4]), "x");
        }

        [Fact]
        [Trait("Covers", "RankPredictorsByMutualInformation")]
        public void MutualInformationRankingPutsTheNonLinearDriverFirst()
        {
            var x = Enumerable.Range(0, 60).Select(i => i % 5 - 2.0).ToArray();
            var target = x.Select(v => v * v).ToArray();
            var noise = Enumerable.Range(0, 60).Select(i => i * 7 % 11 * 1.0).ToArray();
            var rows = x.Select((v, i) => new[] { noise[i], v }).ToArray();

            var ranked = tools.RankPredictorsByMutualInformation(rows, target, ["noise", "driver"], 5);

            ranked[0].Variable.Should().Be("driver");
            ranked[0].Rank.Should().Be(1);
            ranked[1].Rank.Should().Be(2);
            ranked[0].MutualInformationBits.Should().BeGreaterThan(ranked[1].MutualInformationBits);
            ranked[0].AdjustedPValue.Should().BeLessThan(0.001);

            RelationshipsFixtures.Rejects(() => tools.RankPredictorsByMutualInformation(rows, [.. target.Take(3)]),
                "target");
        }

        [Fact]
        [Trait("Covers", "RankPredictorsByCorrelation")]
        public void CorrelationRankingOrdersByAbsoluteStrengthAndAdjustsPValues()
        {
            var x = RelationshipsFixtures.AnscombeOneX;
            var y = RelationshipsFixtures.AnscombeOneY;
            var rows = x.Select((v, i) => new[] { Math.Sin(i * 3.0), v, -v }).ToArray();

            var ranked = tools.RankPredictorsByCorrelation(rows, y, ["noise", "x", "negx"]);

            ranked[0].Coefficient.Should().BeApproximately(0.8164205, 1e-6);
            ranked.Select(r => r.Rank).Should().Equal(1, 2, 3);
            ranked[2].Variable.Should().Be("noise");
            ranked.Should().OnlyContain(r => r.AdjustedPValue >= r.PValue - 1e-12);

            var missing =
                tools.RankPredictorsByCorrelation([[double.NaN], [double.NaN], [1.0]],
                    [1.0, 2, 3]);

            double.IsNaN(missing[0].Coefficient).Should().BeTrue();

            RelationshipsFixtures.Rejects(() => tools.RankPredictorsByCorrelation(rows, y, null, (CorrelationMethod)7),
                "method");

            RelationshipsFixtures.Rejects(() => tools.RankPredictorsByCorrelation(rows, [.. y.Take(4)]), "target");
        }

        [Fact]
        [Trait("Covers", "CorrelationRatio")]
        public void CorrelationRatioMatchesTheOneWayAnovaClosedForm()
        {
            var groups = new[] { 0, 0, 1, 1, 2, 2 };
            var values = new[] { 1.0, 3, 5, 7, 9, 11 };

            var result = tools.CorrelationRatio(groups, values);

            result.EtaSquared.Should().BeApproximately(64d / 70d, 1e-12);
            result.Eta.Should().BeApproximately(Math.Sqrt(64d / 70d), 1e-12);
            result.FStatistic.Should().BeApproximately(16, 1e-12);
            result.DegreesOfFreedomBetween.Should().Be(2);
            result.DegreesOfFreedomWithin.Should().Be(3);
            result.PValue.Should().BeApproximately(Math.Pow(1 + 2 * 16 / 3d, -1.5), 1e-9);
            result.GroupMeans.Should().Equal(2.0, 6.0, 10.0);
            result.GroupSizes.Should().Equal(2, 2, 2);
            result.AdjustedEtaSquared.Should().BeApproximately((64 - 2 * 2.0) / 70, 1e-12);
        }

        [Fact]
        [Trait("Covers", "CorrelationRatio")]
        public void CorrelationRatioEdgeCases()
        {
            RelationshipsFixtures.Rejects(() => tools.CorrelationRatio([1, 1, 1], [1.0, 2, 3]),
                "groups");

            RelationshipsFixtures.Rejects(() => tools.CorrelationRatio([0, 1], [1.0, 2, 3]), "groups");

            RelationshipsFixtures.Rejects(() => tools.CorrelationRatio([0, 1, 2], [1.0, 2, 3]),
                "values");

            RelationshipsFixtures.Rejects(
                () => tools.CorrelationRatio([0, 0, 1, 1], [1.0, double.NaN, 3, 4]), "values");

            tools.CorrelationRatio([0, 0, 1, 1, 1], [1.0, double.NaN, 3, 4, 6], true)
                .MissingPairsDropped.Should().Be(1);

            var flat = tools.CorrelationRatio([0, 0, 1, 1], [2.0, 2, 2, 2]);
            double.IsNaN(flat.EtaSquared).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "CompareIndependentCorrelations")]
        public void CompareIndependentCorrelationsMatchesTheFisherClosedForm()
        {
            var result = tools.CompareIndependentCorrelations(0.6, 50, 0.4, 60);

            result.FisherZ1.Should().BeApproximately(Math.Log(4) / 2, 1e-12);
            result.FisherZ2.Should().BeApproximately(Math.Log(1.4 / 0.6) / 2, 1e-12);
            var se = Math.Sqrt(1d / 47 + 1d / 57);
            result.ZStatistic.Should().BeApproximately((Math.Log(4) / 2 - Math.Log(1.4 / 0.6) / 2) / se, 1e-12);
            result.PValue.Should().BeApproximately(0.1713, 1e-3);
            result.ConfidenceLower.Should().BeApproximately(result.DifferenceInFisherZ - 1.959964 * se, 1e-5);
        }

        [Fact]
        [Trait("Covers", "CompareIndependentCorrelations")]
        public void CompareIndependentCorrelationsRejectsBadInput()
        {
            RelationshipsFixtures.Rejects(() => tools.CompareIndependentCorrelations(1, 50, 0.4, 60), "correlation1");
            RelationshipsFixtures.Rejects(() => tools.CompareIndependentCorrelations(0.5, 50, -1, 60), "correlation2");
            RelationshipsFixtures.Rejects(() => tools.CompareIndependentCorrelations(0.5, 3, 0.4, 60), "sampleSize1");

            RelationshipsFixtures.Rejects(() => tools.CompareIndependentCorrelations(0.5, 30, 0.4, 60, 0),
                "confidenceLevel");

            tools.CompareIndependentCorrelations(0.5, 40, 0.5, 40).PValue.Should().BeApproximately(1, 1e-9);
        }

        [Fact]
        [Trait("Covers", "CorrelationSummaryInterval")]
        public void CorrelationSummaryIntervalMatchesTheCriticalValueTable()
        {
            var result = tools.CorrelationSummaryInterval(0.5, 28);

            result.TStatistic.Should().BeApproximately(0.5 * Math.Sqrt(26 / 0.75), 1e-12);
            result.PValue.Should().BeApproximately(0.00669, 2e-4);
            result.FisherZ.Should().BeApproximately(0.549306, 1e-6);
            result.StandardErrorOfZ.Should().BeApproximately(0.2, 1e-12);
            result.ConfidenceLower.Should().BeApproximately(Math.Tanh(0.549306 - 1.959964 * 0.2), 1e-5);
            result.ConfidenceUpper.Should().BeApproximately(Math.Tanh(0.549306 + 1.959964 * 0.2), 1e-5);
            result.SmallestSignificantCorrelation.Should().BeApproximately(0.3739, 5e-4);
        }

        [Fact]
        [Trait("Covers", "CorrelationSummaryInterval")]
        public void CorrelationSummaryIntervalRejectsBadInput()
        {
            RelationshipsFixtures.Rejects(() => tools.CorrelationSummaryInterval(1.0, 30), "correlation");
            RelationshipsFixtures.Rejects(() => tools.CorrelationSummaryInterval(0.5, 3), "sampleSize");
            RelationshipsFixtures.Rejects(() => tools.CorrelationSummaryInterval(0.5, 30, 1), "confidenceLevel");
            tools.CorrelationSummaryInterval(0, 30).PValue.Should().BeApproximately(1, 1e-9);
        }
    }
}