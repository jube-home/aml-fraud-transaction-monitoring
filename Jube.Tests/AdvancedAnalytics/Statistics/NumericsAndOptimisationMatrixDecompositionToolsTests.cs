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
using Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class NumericsAndOptimisationMatrixDecompositionToolsTests
    {
        private static readonly double[][] threeByTwo = [[1.0, 2.0], [3.0, 4.0], [5.0, 6.0]];

        private static readonly double[][] wikipediaCholeskyExample =
        [
            [4.0, 12.0, -16.0], [12.0, 37.0, -43.0], [-16.0, -43.0, 98.0]
        ];

        private readonly LinearAlgebraTools algebra = new();
        private readonly MatrixDecompositionTools tools = new();

        private double[][] Multiply(double[][] a, double[][] b)
        {
            return algebra.MatrixMultiply(a, b).Matrix;
        }

        private double[][] Transpose(double[][] a)
        {
            return algebra.MatrixTranspose(a).Matrix;
        }

        [Fact]
        [Trait("Covers", "MatrixDecompositionTools.LuDecomposition")]
        public void LuDecompositionReconstructsPermutedInput()
        {
            var a = new[] { new[] { 2.0, 1.0 }, new[] { 4.0, 3.0 } };
            var result = tools.LuDecomposition(a);
            var product = Multiply(result.Lower, result.Upper);

            for (var i = 0; i < 2; i++)
            {
                NumericsTestSupport.VectorShouldMatch(product[i], a[result.PivotPermutation[i]], 1e-10);
            }

            result.IsNonsingular.Should().BeTrue();
            result.Determinant.Should().BeApproximately(2.0, 1e-10);

            tools.LuDecomposition([[1.0, 2.0], [2.0, 4.0]]).IsNonsingular.Should().BeFalse();
            NumericsTestSupport.ShouldRejectWith(() => tools.LuDecomposition(threeByTwo), "matrix");
        }

        [Fact]
        [Trait("Covers", "MatrixDecompositionTools.QrDecomposition")]
        public void QrDecompositionReconstructsAndIsOrthonormal()
        {
            var a = new[] { new[] { 1.0, 1.0 }, new[] { 1.0, 2.0 }, new[] { 1.0, 3.0 } };
            var result = tools.QrDecomposition(a);
            NumericsTestSupport.MatrixShouldMatch(Multiply(result.Q, result.R), a, 1e-10);

            NumericsTestSupport.MatrixShouldMatch(Multiply(Transpose(result.Q), result.Q),
                [[1.0, 0.0], [0.0, 1.0]], 1e-10);

            result.IsFullRank.Should().BeTrue();

            tools.QrDecomposition([[1.0, 2.0], [2.0, 4.0], [3.0, 6.0]]).IsFullRank
                .Should().BeFalse();

            NumericsTestSupport.ShouldRejectWith(() => tools.QrDecomposition([[1.0, 2.0, 3.0]]),
                "matrix");
        }

        [Fact]
        [Trait("Covers", "MatrixDecompositionTools.CholeskyDecomposition")]
        public void CholeskyDecompositionMatchesPublishedExample()
        {
            var result = tools.CholeskyDecomposition(wikipediaCholeskyExample);
            result.IsPositiveDefinite.Should().BeTrue();

            NumericsTestSupport.MatrixShouldMatch(result.Lower,
                [[2.0, 0.0, 0.0], [6.0, 1.0, 0.0], [-8.0, 5.0, 3.0]], 1e-10);

            result.Determinant.Should().BeApproximately(36.0, 1e-8);
            result.LogDeterminant.Should().BeApproximately(Math.Log(36.0), 1e-10);

            var small = tools.CholeskyDecomposition([[4.0, 2.0], [2.0, 3.0]]);

            NumericsTestSupport.MatrixShouldMatch(small.Lower,
                [[2.0, 0.0], [1.0, Math.Sqrt(2.0)]], 1e-10);

            var notPositive = tools.CholeskyDecomposition([[1.0, 2.0], [2.0, 1.0]]);
            notPositive.IsPositiveDefinite.Should().BeFalse();
            notPositive.Lower.Should().BeEmpty();

            NumericsTestSupport.ShouldRejectWith(
                () => tools.CholeskyDecomposition([[1.0, 2.0], [3.0, 1.0]]), "matrix");
        }

        [Fact]
        [Trait("Covers", "MatrixDecompositionTools.SolveSymmetricPositiveDefiniteSystem")]
        public void SolveSymmetricPositiveDefiniteSystemKnownSolution()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.SolveSymmetricPositiveDefiniteSystem([[4.0, 2.0], [2.0, 3.0]],
                    [2.0, 1.0]),
                [0.5, 0.0], 1e-10);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.SolveSymmetricPositiveDefiniteSystem([[1.0, 2.0], [2.0, 1.0]],
                    [1.0, 1.0]), "a");
        }

        [Fact]
        [Trait("Covers", "MatrixDecompositionTools.SingularValueDecomposition")]
        public void SingularValueDecompositionMatchesPublishedValuesAndReconstructs()
        {
            var result = tools.SingularValueDecomposition(threeByTwo);
            result.SingularValues[0].Should().BeApproximately(9.52551809, 1e-6);
            result.SingularValues[1].Should().BeApproximately(0.51430058, 1e-6);
            result.Rank.Should().Be(2);
            result.ConditionNumber.Should().BeApproximately(9.52551809 / 0.51430058, 1e-4);

            var diagonal = result.SingularValues.Select((s, i) =>
                Enumerable.Range(0, result.SingularValues.Length).Select(j => i == j ? s : 0.0).ToArray()).ToArray();

            NumericsTestSupport.MatrixShouldMatch(Multiply(Multiply(result.U, diagonal), Transpose(result.V)),
                threeByTwo, 1e-9);

            var wide = tools.SingularValueDecomposition(Transpose(threeByTwo));
            wide.SingularValues[0].Should().BeApproximately(9.52551809, 1e-6);
            wide.SingularValues[1].Should().BeApproximately(0.51430058, 1e-6);
        }

        [Fact]
        [Trait("Covers", "MatrixDecompositionTools.SymmetricEigenDecomposition")]
        public void SymmetricEigenDecompositionKnownEigenpairs()
        {
            var a = new[] { new[] { 2.0, 1.0 }, new[] { 1.0, 2.0 } };
            var descending = tools.SymmetricEigenDecomposition(a);
            NumericsTestSupport.VectorShouldMatch(descending.RealEigenvalues, [3.0, 1.0], 1e-10);
            descending.HasComplexEigenvalues.Should().BeFalse();

            for (var k = 0; k < 2; k++)
            {
                var vector = descending.Eigenvectors.Select(r => r[k]).ToArray();
                var image = algebra.MatrixVectorMultiply(a, vector).Values;

                NumericsTestSupport.VectorShouldMatch(image,
                    [.. vector.Select(v => v * descending.RealEigenvalues[k])], 1e-9);

                Math.Sqrt(vector.Sum(v => v * v)).Should().BeApproximately(1.0, 1e-9);
            }

            var ascending = tools.SymmetricEigenDecomposition(a, false);

            NumericsTestSupport.VectorShouldMatch([.. ascending.RealEigenvalues.OrderBy(v => v)],
                [1.0, 3.0], 1e-10);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.SymmetricEigenDecomposition([[1.0, 2.0], [3.0, 4.0]]), "matrix");
        }

        [Fact]
        [Trait("Covers", "MatrixDecompositionTools.GeneralEigenDecomposition")]
        public void GeneralEigenDecompositionRealAndComplexCases()
        {
            var real = tools.GeneralEigenDecomposition([[1.0, 2.0], [3.0, 4.0]]);
            var expected = new[] { (5.0 + Math.Sqrt(33.0)) / 2.0, (5.0 - Math.Sqrt(33.0)) / 2.0 };

            real.RealEigenvalues.OrderByDescending(v => v).ToArray().Should().BeEquivalentTo(expected,
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-9))
                    .WhenTypeIs<double>());

            real.HasComplexEigenvalues.Should().BeFalse();

            var rotation = tools.GeneralEigenDecomposition([[0.0, -1.0], [1.0, 0.0]]);
            rotation.HasComplexEigenvalues.Should().BeTrue();
            rotation.ImaginaryEigenvalues.Select(Math.Abs).Should().OnlyContain(v => Math.Abs(v - 1.0) < 1e-9);
            rotation.RealEigenvalues.Should().OnlyContain(v => Math.Abs(v) < 1e-9);
        }

        [Fact]
        [Trait("Covers", "MatrixDecompositionTools.GeneralizedEigenDecomposition")]
        public void GeneralizedEigenDecompositionScalesWithB()
        {
            var a = new[] { new[] { 2.0, 0.0 }, new[] { 0.0, 3.0 } };
            var identity = tools.GeneralizedEigenDecomposition(a, [[1.0, 0.0], [0.0, 1.0]]);

            identity.RealEigenvalues.OrderBy(v => v).Should().BeEquivalentTo([2.0, 3.0],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-9))
                    .WhenTypeIs<double>());

            var doubled = tools.GeneralizedEigenDecomposition(a, [[2.0, 0.0], [0.0, 2.0]]);

            doubled.RealEigenvalues.OrderBy(v => v).Should().BeEquivalentTo([1.0, 1.5],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-9))
                    .WhenTypeIs<double>());

            doubled.IsSingular.Should().BeFalse();

            NumericsTestSupport.ShouldRejectWith(() => tools.GeneralizedEigenDecomposition(a, [[1.0]]),
                "b");
        }

        [Fact]
        [Trait("Covers", "MatrixDecompositionTools.GramSchmidtOrthogonalisation")]
        public void GramSchmidtOrthogonalisationReconstructs()
        {
            var triangular = tools.GramSchmidtOrthogonalisation([[1.0, 1.0], [0.0, 1.0]]);

            NumericsTestSupport.MatrixShouldMatch(triangular.Q, [[1.0, 0.0], [0.0, 1.0]],
                1e-10);
            NumericsTestSupport.MatrixShouldMatch(triangular.R, [[1.0, 1.0], [0.0, 1.0]],
                1e-10);

            var a = new[] { new[] { 3.0, 1.0 }, new[] { 4.0, 2.0 } };

            foreach (var modified in new[] { true, false })
            {
                var result = tools.GramSchmidtOrthogonalisation(a, modified);
                NumericsTestSupport.MatrixShouldMatch(Multiply(result.Q, result.R), a, 1e-10);

                NumericsTestSupport.MatrixShouldMatch(Multiply(Transpose(result.Q), result.Q),
                    [[1.0, 0.0], [0.0, 1.0]], 1e-10);
            }

            NumericsTestSupport.ShouldRejectWith(
                () => tools.GramSchmidtOrthogonalisation([[1.0, 2.0, 3.0]]), "matrix");
        }

        [Fact]
        [Trait("Covers", "MatrixDecompositionTools.NonnegativeMatrixFactorisation")]
        public void NonnegativeMatrixFactorisationRecoversRankOneMatrixAndIsSeedDeterministic()
        {
            var v = new[] { new[] { 1.0, 2.0 }, new[] { 2.0, 4.0 } };
            var first = tools.NonnegativeMatrixFactorisation(v, 1, 2000, 7);
            first.ReconstructionError.Should().BeLessThan(1e-3);
            first.W.SelectMany(r => r).Concat(first.H.SelectMany(r => r)).Should().OnlyContain(x => x >= 0);
            var second = tools.NonnegativeMatrixFactorisation(v, 1, 2000, 7);
            NumericsTestSupport.MatrixShouldMatch(second.W, first.W, 0);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NonnegativeMatrixFactorisation([[1.0, -2.0], [2.0, 4.0]], 1),
                "matrix");

            NumericsTestSupport.ShouldRejectWith(() => tools.NonnegativeMatrixFactorisation(v, 3), "rank");
        }
    }
}