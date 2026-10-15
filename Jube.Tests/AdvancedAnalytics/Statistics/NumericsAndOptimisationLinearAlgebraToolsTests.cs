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
    public class NumericsAndOptimisationLinearAlgebraToolsTests
    {
        private const double Tight = 1e-10;
        private static readonly double[][] twoByTwo = [[1.0, 2.0], [3.0, 4.0]];
        private readonly LinearAlgebraTools tools = new();

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixMultiply")]
        public void MatrixMultiplyKnownProductAndShapeChecks()
        {
            var result = tools.MatrixMultiply(twoByTwo, [[5.0, 6.0], [7.0, 8.0]]);

            NumericsTestSupport.MatrixShouldMatch(result.Matrix, [[19.0, 22.0], [43.0, 50.0]],
                Tight);

            result.Rows.Should().Be(2);
            result.Columns.Should().Be(2);

            var rectangular = tools.MatrixMultiply([[1.0, 2.0, 3.0]],
                [[1.0], [1.0], [1.0]]);
            rectangular.Matrix[0][0].Should().Be(6.0);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MatrixMultiply(twoByTwo, [[1.0, 2.0, 3.0]]), "b");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MatrixMultiply([[1.0, 2.0], [3.0]], twoByTwo), "a");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MatrixMultiply([[1.0, double.NaN]], twoByTwo), "a");

            NumericsTestSupport.ShouldRejectWith(() => tools.MatrixMultiply([], twoByTwo), "a");
            NumericsTestSupport.ShouldRejectWith(() => tools.MatrixMultiply(null!, twoByTwo), "a");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixVectorMultiply")]
        public void MatrixVectorMultiplyKnownProduct()
        {
            NumericsTestSupport.VectorShouldMatch(tools.MatrixVectorMultiply(twoByTwo, [5.0, 6.0]).Values,
                [17.0, 39.0], Tight);

            NumericsTestSupport.ShouldRejectWith(() => tools.MatrixVectorMultiply(twoByTwo, [1.0]), "v");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixTranspose")]
        public void MatrixTransposeSwapsRowsAndColumns()
        {
            var result = tools.MatrixTranspose([[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]]);
            result.Rows.Should().Be(3);
            result.Columns.Should().Be(2);

            NumericsTestSupport.MatrixShouldMatch(result.Matrix,
                [[1.0, 4.0], [2.0, 5.0], [3.0, 6.0]], 0);
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixElementwiseOperation")]
        public void MatrixElementwiseOperationAllOperations()
        {
            var b = new[] { new[] { 2.0, 2.0 }, new[] { 2.0, 2.0 } };

            NumericsTestSupport.MatrixShouldMatch(tools.MatrixElementwiseOperation(twoByTwo, b).Matrix,
                [[3.0, 4.0], [5.0, 6.0]], Tight);

            NumericsTestSupport.MatrixShouldMatch(
                tools.MatrixElementwiseOperation(twoByTwo, b, NumericsElementwiseOperation.Subtract).Matrix,
                [[-1.0, 0.0], [1.0, 2.0]], Tight);

            NumericsTestSupport.MatrixShouldMatch(
                tools.MatrixElementwiseOperation(twoByTwo, b, NumericsElementwiseOperation.Multiply).Matrix,
                [[2.0, 4.0], [6.0, 8.0]], Tight);
            NumericsTestSupport.MatrixShouldMatch(
                tools.MatrixElementwiseOperation(twoByTwo, b, NumericsElementwiseOperation.Divide).Matrix,
                [[0.5, 1.0], [1.5, 2.0]], Tight);
            NumericsTestSupport.MatrixShouldMatch(
                tools.MatrixElementwiseOperation(twoByTwo, b, NumericsElementwiseOperation.Power).Matrix,
                [[1.0, 4.0], [9.0, 16.0]], Tight);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MatrixElementwiseOperation(twoByTwo, [[1.0, 2.0]]), "b");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixInverse")]
        public void MatrixInverseKnownAnswerAndSingularRejected()
        {
            var inverse = tools.MatrixInverse([[4.0, 7.0], [2.0, 6.0]]);

            NumericsTestSupport.MatrixShouldMatch(inverse.Matrix, [[0.6, -0.7], [-0.2, 0.4]],
                Tight);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MatrixInverse([[1.0, 2.0], [2.0, 4.0]]), "matrix");

            NumericsTestSupport.ShouldRejectWith(() => tools.MatrixInverse([[1.0, 2.0, 3.0]]),
                "matrix");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixPseudoInverse")]
        public void MatrixPseudoInverseMatchesPublishedMoorePenroseExample()
        {
            var result =
                tools.MatrixPseudoInverse([[1.0, 2.0], [3.0, 4.0], [5.0, 6.0]]);

            NumericsTestSupport.MatrixShouldMatch(result.Matrix, [
                [-4.0 / 3.0, -1.0 / 3.0, 2.0 / 3.0],
                [13.0 / 12.0, 1.0 / 3.0, -5.0 / 12.0]
            ], 1e-9);

            var singular = tools.MatrixPseudoInverse([[1.0, 2.0], [2.0, 4.0]]);
            singular.Matrix[0][0].Should().BeApproximately(0.04, 1e-9);
            singular.Matrix[1][1].Should().BeApproximately(0.16, 1e-9);
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixDeterminant")]
        public void MatrixDeterminantKnownAnswers()
        {
            var ten = tools.MatrixDeterminant([[4.0, 7.0], [2.0, 6.0]]);
            ten.Determinant.Should().BeApproximately(10.0, Tight);
            ten.LogAbsoluteDeterminant.Should().BeApproximately(Math.Log(10.0), Tight);
            ten.Sign.Should().Be(1);
            ten.IsSingular.Should().BeFalse();

            var negative = tools.MatrixDeterminant(twoByTwo);
            negative.Determinant.Should().BeApproximately(-2.0, Tight);
            negative.Sign.Should().Be(-1);

            var singular = tools.MatrixDeterminant([[1.0, 2.0], [2.0, 4.0]]);
            singular.IsSingular.Should().BeTrue();
            singular.Determinant.Should().Be(0.0);
            singular.LogAbsoluteDeterminant.Should().Be(double.NegativeInfinity);

            tools.MatrixDeterminant([[7.0]]).Determinant.Should().Be(7.0);
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixPseudoDeterminant")]
        public void MatrixPseudoDeterminantIsProductOfNonZeroSingularValues()
        {
            var result = tools.MatrixPseudoDeterminant([
                [2.0, 0.0, 0.0], [0.0, 3.0, 0.0], [0.0, 0.0, 0.0]
            ]);
            result.Values[0].Should().BeApproximately(6.0, 1e-9);
            result.Values[1].Should().BeApproximately(Math.Log(6.0), 1e-9);
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixTrace")]
        public void MatrixTraceSumsDiagonal()
        {
            tools.MatrixTrace(twoByTwo).Should().Be(5.0);
            NumericsTestSupport.ShouldRejectWith(() => tools.MatrixTrace([[1.0, 2.0, 3.0]]), "matrix");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixRankAndCondition")]
        public void MatrixRankAndConditionKnownAnswers()
        {
            var diagonal = tools.MatrixRankAndCondition([[3.0, 0.0], [0.0, 4.0]]);
            diagonal.Rank.Should().Be(2);
            diagonal.ConditionNumber.Should().BeApproximately(4.0 / 3.0, 1e-9);
            diagonal.LargestSingularValue.Should().BeApproximately(4.0, 1e-9);
            diagonal.SmallestSingularValue.Should().BeApproximately(3.0, 1e-9);
            diagonal.IsRankDeficient.Should().BeFalse();

            var dependent = tools.MatrixRankAndCondition([[1.0, 2.0], [2.0, 4.0]]);
            dependent.Rank.Should().Be(1);
            dependent.IsRankDeficient.Should().BeTrue();

            tools.MatrixRankAndCondition([[1.0, 2.0], [3.0, 4.0], [5.0, 6.0]])
                .ConditionNumber.Should().BeApproximately(9.52551809 / 0.51430058, 1e-4);

            tools.MatrixRankAndCondition([[1.0, 3.0, 5.0], [2.0, 4.0, 6.0]]).Rank.Should()
                .Be(2);
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixNorms")]
        public void MatrixNormsMatchDefinitions()
        {
            var result = tools.MatrixNorms([[1.0, -2.0], [3.0, 4.0]]);
            result.Norm1.Should().BeApproximately(6.0, Tight);
            result.NormInfinity.Should().BeApproximately(7.0, Tight);
            result.Frobenius.Should().BeApproximately(Math.Sqrt(30.0), Tight);
            result.Spectral.Should().BeApproximately(Math.Sqrt(15.0 + Math.Sqrt(125.0)), 1e-9);
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.VectorNorms")]
        public void VectorNormsThreeFourFive()
        {
            NumericsTestSupport.VectorShouldMatch(tools.VectorNorms([3.0, 4.0]).Values, [5.0, 25.0],
                Tight);

            NumericsTestSupport.VectorShouldMatch(tools.VectorNorms([3.0, 4.0], true).Values,
                [0.6, 0.8], Tight);

            tools.VectorNorms([0.0, 0.0], true).Values.Should().OnlyContain(v => v == 0.0);
            NumericsTestSupport.ShouldRejectWith(() => tools.VectorNorms([]), "vector");

            NumericsTestSupport.ShouldRejectWith(() => tools.VectorNorms([1.0, double.PositiveInfinity]),
                "vector");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.SolveLinearSystem")]
        public void SolveLinearSystemKnownSolution()
        {
            var result =
                tools.SolveLinearSystem([[2.0, 1.0], [1.0, 3.0]], [5.0, 10.0]);

            NumericsTestSupport.VectorShouldMatch(result.Solution, [1.0, 3.0], Tight);
            result.ResidualNorm.Should().BeLessThan(1e-12);
            result.ConditionNumber.Should().BeGreaterThan(1.0);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.SolveLinearSystem([[1.0, 2.0], [2.0, 4.0]], [1.0, 2.0]),
                "a");

            NumericsTestSupport.ShouldRejectWith(() => tools.SolveLinearSystem(twoByTwo, [1.0]), "b");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.SolveLinearSystemMultipleRightHandSides")]
        public void SolveLinearSystemMultipleRightHandSidesKnownSolution()
        {
            var result = tools.SolveLinearSystemMultipleRightHandSides(
                [[2.0, 1.0], [1.0, 3.0]],
                [[5.0, 0.0], [10.0, 1.0]]);

            NumericsTestSupport.MatrixShouldMatch(result.Matrix, [[1.0, -0.2], [3.0, 0.4]],
                Tight);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.SolveLinearSystemMultipleRightHandSides(twoByTwo, [[1.0]]), "b");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.SolveLeastSquares")]
        public void SolveLeastSquaresFitsLineExactAndApproximate()
        {
            var design = new[] { new[] { 1.0, 0.0 }, new[] { 1.0, 1.0 }, new[] { 1.0, 2.0 } };
            var exact = tools.SolveLeastSquares(design, [1.0, 3.0, 5.0]);
            NumericsTestSupport.VectorShouldMatch(exact.Solution, [1.0, 2.0], 1e-9);
            exact.ResidualNorm.Should().BeLessThan(1e-9);

            var noisy = tools.SolveLeastSquares(design, [0.0, 1.0, 1.0]);
            NumericsTestSupport.VectorShouldMatch(noisy.Solution, [1.0 / 6.0, 0.5], 1e-9);
            noisy.ResidualNorm.Should().BeApproximately(Math.Sqrt(6.0) / 6.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixNullSpace")]
        public void MatrixNullSpaceVectorsAreOrthonormalAndAnnihilateTheMatrix()
        {
            var matrix = new[] { new[] { 1.0, 2.0 }, new[] { 1.0, 2.0 } };
            var result = tools.MatrixNullSpace(matrix);
            result.Rows.Should().Be(1);
            tools.MatrixVectorMultiply(matrix, result.Matrix[0]).Values.Should().OnlyContain(v => Math.Abs(v) < 1e-9);
            Math.Sqrt(result.Matrix[0].Sum(v => v * v)).Should().BeApproximately(1.0, 1e-9);
            Math.Abs(result.Matrix[0][0] / result.Matrix[0][1]).Should().BeApproximately(2.0, 1e-9);

            var wide = new[] { new[] { 1.0, 1.0, 1.0 } };
            var wideResult = tools.MatrixNullSpace(wide);
            wideResult.Rows.Should().Be(2);

            foreach (var vector in wideResult.Matrix)
            {
                tools.MatrixVectorMultiply(wide, vector).Values[0].Should().BeApproximately(0.0, 1e-9);
            }

            tools.MatrixNullSpace([[2.0, 0.0], [0.0, 3.0]]).Rows.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixReducedRowEchelonForm")]
        public void MatrixReducedRowEchelonFormKnownAnswers()
        {
            var full = tools.MatrixReducedRowEchelonForm([
                [1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 10.0]
            ]);

            NumericsTestSupport.MatrixShouldMatch(full.Matrix,
                [[1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0]], 1e-9);

            var dependent =
                tools.MatrixReducedRowEchelonForm([[1.0, 2.0, 3.0], [2.0, 4.0, 6.0]]);

            NumericsTestSupport.MatrixShouldMatch(dependent.Matrix,
                [[1.0, 2.0, 3.0], [0.0, 0.0, 0.0]], 1e-9);
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixKroneckerProduct")]
        public void MatrixKroneckerProductMatchesReferenceExample()
        {
            var result = tools.MatrixKroneckerProduct(twoByTwo, [[0.0, 5.0], [6.0, 7.0]]);

            NumericsTestSupport.MatrixShouldMatch(result.Matrix, [
                [0.0, 5.0, 0.0, 10.0],
                [6.0, 7.0, 12.0, 14.0],
                [0.0, 15.0, 0.0, 20.0],
                [18.0, 21.0, 24.0, 28.0]
            ], Tight);

            var large = Enumerable.Range(0, 20).Select(_ => new double[20]).ToArray();
            NumericsTestSupport.ShouldRejectWith(() => tools.MatrixKroneckerProduct(large, large), "b");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.VectorOuterProduct")]
        public void VectorOuterProductKnownAnswer()
        {
            var result = tools.VectorOuterProduct([1.0, 2.0, 3.0], [4.0, 5.0]);

            NumericsTestSupport.MatrixShouldMatch(result.Matrix,
                [[4.0, 5.0], [8.0, 10.0], [12.0, 15.0]], 0);

            NumericsTestSupport.ShouldRejectWith(() => tools.VectorOuterProduct(new double[301], [1.0]), "a");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.VectorDotProduct")]
        public void VectorDotProductKnownAnswerAndLengthMismatch()
        {
            tools.VectorDotProduct([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Should().Be(32.0);
            NumericsTestSupport.ShouldRejectWith(() => tools.VectorDotProduct([1.0], [1.0, 2.0]), "b");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.VectorCrossProduct")]
        public void VectorCrossProductKnownAnswers()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.VectorCrossProduct([1.0, 0.0, 0.0], [0.0, 1.0, 0.0]), [0.0, 0.0, 1.0],
                Tight);

            NumericsTestSupport.VectorShouldMatch(
                tools.VectorCrossProduct([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]), [-3.0, 6.0, -3.0],
                Tight);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.VectorCrossProduct([1.0, 2.0], [1.0, 2.0]), "a");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixStructureProperties")]
        public void MatrixStructurePropertiesClassifiesMatrices()
        {
            var spd = tools.MatrixStructureProperties([[2.0, -1.0], [-1.0, 2.0]]);
            spd.IsSquare.Should().BeTrue();
            spd.IsSymmetric.Should().BeTrue();
            spd.IsPositiveDefinite.Should().BeTrue();
            spd.IsDiagonal.Should().BeFalse();
            spd.IsSingular.Should().BeFalse();

            var indefinite = tools.MatrixStructureProperties([[1.0, 2.0], [2.0, 1.0]]);
            indefinite.IsSymmetric.Should().BeTrue();
            indefinite.IsPositiveDefinite.Should().BeFalse();

            var diagonal = tools.MatrixStructureProperties([[5.0, 0.0], [0.0, 0.0]]);
            diagonal.IsDiagonal.Should().BeTrue();
            diagonal.IsSingular.Should().BeTrue();

            var withNaN = tools.MatrixStructureProperties([
                [1.0, double.NaN], [0.0, double.PositiveInfinity]
            ]);
            withNaN.HasNaN.Should().BeTrue();
            withNaN.HasInfinity.Should().BeTrue();
            withNaN.IsSymmetric.Should().BeFalse();

            var rectangular = tools.MatrixStructureProperties([[1.0, 2.0, 3.0]]);
            rectangular.IsSquare.Should().BeFalse();
            rectangular.Columns.Should().Be(3);
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.GenerateStructuredMatrix")]
        public void GenerateStructuredMatrixKinds()
        {
            NumericsTestSupport.MatrixShouldMatch(
                tools.GenerateStructuredMatrix(NumericsStructuredMatrix.Identity, 2).Matrix,
                [[1.0, 0.0], [0.0, 1.0]], 0);

            tools.GenerateStructuredMatrix(NumericsStructuredMatrix.Zeros, 3).Matrix.SelectMany(r => r).Should()
                .OnlyContain(v => v == 0.0);
            tools.GenerateStructuredMatrix(NumericsStructuredMatrix.Ones, 3).Matrix.SelectMany(r => r).Should()
                .OnlyContain(v => v == 1.0);

            var magic = tools.GenerateStructuredMatrix(NumericsStructuredMatrix.Magic, 3).Matrix;
            magic.Select(r => r.Sum()).Should().OnlyContain(s => Math.Abs(s - 15.0) < 1e-9);

            Enumerable.Range(0, 3).Select(j => magic.Sum(r => r[j])).Should()
                .OnlyContain(s => Math.Abs(s - 15.0) < 1e-9);

            tools.GenerateStructuredMatrix(NumericsStructuredMatrix.Hilbert, 3).Matrix[1][2].Should()
                .BeApproximately(0.25, Tight);

            NumericsTestSupport.MatrixShouldMatch(
                tools.GenerateStructuredMatrix(NumericsStructuredMatrix.Vandermonde, 3, [1.0, 2.0, 3.0])
                    .Matrix,
                [[1.0, 1.0, 1.0], [1.0, 2.0, 4.0], [1.0, 3.0, 9.0]], Tight);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.GenerateStructuredMatrix(NumericsStructuredMatrix.Identity, 301), "size");
            NumericsTestSupport.ShouldRejectWith(
                () => tools.GenerateStructuredMatrix(NumericsStructuredMatrix.Magic, 2), "size");
            NumericsTestSupport.ShouldRejectWith(
                () => tools.GenerateStructuredMatrix(NumericsStructuredMatrix.Vandermonde, 3), "nodes");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.MatrixPower")]
        public void MatrixPowerGivesFibonacciNumbers()
        {
            var fibonacci = new[] { new[] { 1.0, 1.0 }, new[] { 1.0, 0.0 } };

            NumericsTestSupport.MatrixShouldMatch(tools.MatrixPower(fibonacci, 10).Matrix,
                [[89.0, 55.0], [55.0, 34.0]], Tight);

            NumericsTestSupport.MatrixShouldMatch(tools.MatrixPower(fibonacci, 0).Matrix,
                [[1.0, 0.0], [0.0, 1.0]], 0);

            NumericsTestSupport.MatrixShouldMatch(tools.MatrixPower(fibonacci, 1).Matrix, fibonacci, 0);
            NumericsTestSupport.ShouldRejectWith(() => tools.MatrixPower(fibonacci, -1), "power");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.GenerateMeshGrid")]
        public void GenerateMeshGridEnumeratesPoints()
        {
            var result = tools.GenerateMeshGrid(0, 1, 2, 0, 2, 3);
            result.Rows.Should().Be(6);
            result.Columns.Should().Be(2);
            result.Matrix.Select(r => r[0]).Distinct().OrderBy(v => v).Should().Equal(0.0, 1.0);
            result.Matrix.Select(r => r[1]).Distinct().OrderBy(v => v).Should().Equal(0.0, 1.0, 2.0);
            NumericsTestSupport.ShouldRejectWith(() => tools.GenerateMeshGrid(1, 0, 2, 0, 1, 2), "xMaximum");
            NumericsTestSupport.ShouldRejectWith(() => tools.GenerateMeshGrid(0, 1, 500, 0, 1, 500), "xPoints");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.GenerateEvenlySpacedValues")]
        public void GenerateEvenlySpacedValuesIncludesBothEnds()
        {
            NumericsTestSupport.VectorShouldMatch(tools.GenerateEvenlySpacedValues(0, 1, 5).Values,
                [0.0, 0.25, 0.5, 0.75, 1.0], Tight);

            NumericsTestSupport.ShouldRejectWith(() => tools.GenerateEvenlySpacedValues(0, 1, 1), "count");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.NumericsConvolveVectors")]
        public void NumericsConvolveVectorsKnownConvolution()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.NumericsConvolveVectors([1.0, 2.0, 3.0], [0.0, 1.0, 0.5]).Values,
                [0.0, 1.0, 2.5, 4.0, 1.5], Tight);

            tools.NumericsConvolveVectors([1.0, 2.0, 3.0], [0.0, 1.0, 0.5], true).Values.Length
                .Should().Be(3);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericsConvolveVectors([1.0], [1.0, 2.0]), "kernel");
        }

        [Fact]
        [Trait("Covers", "LinearAlgebraTools.RescaleValuesToRange")]
        public void RescaleValuesToRangeMapsLinearly()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.RescaleValuesToRange([0.0, 5.0, 10.0, 20.0], 0, 10, 0, 1).Values,
                [0.0, 0.5, 1.0, 2.0], Tight);

            NumericsTestSupport.VectorShouldMatch(
                tools.RescaleValuesToRange([0.0, 100.0], 0, 100, 32, 212).Values, [32.0, 212.0],
                Tight);

            NumericsTestSupport.ShouldRejectWith(() => tools.RescaleValuesToRange([1.0], 3, 3, 0, 1),
                "fromMaximum");
        }
    }
}