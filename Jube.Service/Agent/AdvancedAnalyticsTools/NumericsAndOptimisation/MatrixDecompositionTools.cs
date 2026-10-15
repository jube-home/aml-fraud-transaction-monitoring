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

using System.ComponentModel;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Math.Random;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation
{
    public sealed record NumericsLuResult
    {
        public double[][] Lower { get; init; } = Array.Empty<double[]>();
        public double[][] Upper { get; init; } = Array.Empty<double[]>();
        public int[] PivotPermutation { get; init; } = Array.Empty<int>();
        public bool IsNonsingular { get; init; }
        public double Determinant { get; init; }
    }

    public sealed record NumericsQrResult
    {
        public double[][] Q { get; init; } = Array.Empty<double[]>();
        public double[][] R { get; init; } = Array.Empty<double[]>();
        public bool IsFullRank { get; init; }
    }

    public sealed record NumericsCholeskyResult
    {
        public bool IsPositiveDefinite { get; init; }
        public double[][] Lower { get; init; } = Array.Empty<double[]>();
        public double Determinant { get; init; }
        public double LogDeterminant { get; init; }
    }

    public sealed record NumericsSvdResult
    {
        public double[][] U { get; init; } = Array.Empty<double[]>();
        public double[] SingularValues { get; init; } = Array.Empty<double>();
        public double[][] V { get; init; } = Array.Empty<double[]>();
        public int Rank { get; init; }
        public double ConditionNumber { get; init; }
    }

    public sealed record NumericsEigenResult
    {
        public double[] RealEigenvalues { get; init; } = Array.Empty<double>();
        public double[] ImaginaryEigenvalues { get; init; } = Array.Empty<double>();
        public double[][] Eigenvectors { get; init; } = Array.Empty<double[]>();
        public bool HasComplexEigenvalues { get; init; }
    }

    public sealed record NumericsGeneralizedEigenResult
    {
        public double[] RealEigenvalues { get; init; } = Array.Empty<double>();
        public double[] ImaginaryEigenvalues { get; init; } = Array.Empty<double>();
        public double[][] Eigenvectors { get; init; } = Array.Empty<double[]>();
        public bool IsSingular { get; init; }
        public bool IsDegenerate { get; init; }
    }

    public sealed record NumericsNonnegativeFactorsResult
    {
        public double[][] W { get; init; } = Array.Empty<double[]>();
        public double[][] H { get; init; } = Array.Empty<double[]>();
        public double ReconstructionError { get; init; }
    }

    [AdvancedAnalyticsToolGroup("NumericsAndOptimisation")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class MatrixDecompositionTools
    {
        [AdvancedAnalyticsTool("LuDecomposition")]
        [Description(
            "Factors a matrix as L times U with partial pivoting: Lower is unit lower triangular, Upper is upper triangular, and row i of L times U equals row PivotPermutation[i] of the input. Use it to understand how a linear system is solved or to get the determinant cheaply. " +
            "IsNonsingular false means a zero pivot was met, so the matrix has no inverse. Limited to 300 by 300.")]
        public NumericsLuResult LuDecomposition(
            [Description("Square matrix as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _, true);
            var lu = new JaggedLuDecomposition(matrix);

            return new NumericsLuResult
            {
                Lower = lu.LowerTriangularFactor,
                Upper = lu.UpperTriangularFactor,
                PivotPermutation = lu.PivotPermutationVector,
                IsNonsingular = lu.Nonsingular,
                Determinant = lu.Determinant
            };
        }

        [AdvancedAnalyticsTool("QrDecomposition")]
        [Description(
            "Factors a matrix with at least as many rows as columns as Q times R, where Q has orthonormal columns and R is upper triangular. It is the numerically stable route to least-squares problems. " +
            "IsFullRank false means the columns are linearly dependent. Limited to 300 by 300.")]
        public NumericsQrResult QrDecomposition(
            [Description("Matrix with rows greater than or equal to columns, as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out var rows, out var columns);

            if (rows < columns)
            {
                throw new ArgumentException(
                    $"QR needs at least as many rows as columns but the matrix is {rows} by {columns}; supply the transpose.",
                    nameof(matrix));
            }

            var qr = new JaggedQrDecomposition(matrix);

            return new NumericsQrResult
            {
                Q = qr.OrthogonalFactor,
                R = qr.UpperTriangularFactor,
                IsFullRank = qr.Diagonal.Length > 0 &&
                             qr.Diagonal.All(d => Math.Abs(d) > 1e-12 * qr.Diagonal.Max(Math.Abs))
            };
        }

        [AdvancedAnalyticsTool("CholeskyDecomposition")]
        [Description(
            "Factors a symmetric positive definite matrix as L times L transpose with L lower triangular, and returns the determinant and its log. Use it to test whether a covariance matrix is valid, to draw correlated random numbers or to solve symmetric systems quickly. " +
            "If the matrix is not positive definite IsPositiveDefinite is false and the factors are empty rather than an error. Limited to 300 by 300.")]
        public NumericsCholeskyResult CholeskyDecomposition(
            [Description("Symmetric square matrix as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _, symmetric: true);
            var cholesky = new JaggedCholeskyDecomposition(matrix);

            if (!cholesky.IsPositiveDefinite)
            {
                return new NumericsCholeskyResult
                {
                    IsPositiveDefinite = false,
                    Determinant = double.NaN,
                    LogDeterminant = double.NaN
                };
            }

            return new NumericsCholeskyResult
            {
                IsPositiveDefinite = true,
                Lower = cholesky.LeftTriangularFactor,
                Determinant = cholesky.Determinant,
                LogDeterminant = cholesky.LogDeterminant
            };
        }

        [AdvancedAnalyticsTool("SolveSymmetricPositiveDefiniteSystem")]
        [Description(
            "Solves A x = b for a symmetric positive definite A (such as a covariance or normal-equations matrix) using the Cholesky factorisation, which is about twice as fast as the general solver. " +
            "A matrix that is not positive definite is rejected with advice to use SolveLinearSystem. Limited to 300 by 300.")]
        public double[] SolveSymmetricPositiveDefiniteSystem(
            [Description("Symmetric positive definite matrix as an array of equal-length rows.")]
            double[][] a,
            [Description("Right hand side vector with one value per row of A.")]
            double[] b)
        {
            NumericsGuard.RequireMatrix(a, nameof(a), out var rows, out _, symmetric: true);
            NumericsGuard.RequireVector(b, nameof(b), rows, rows);
            var cholesky = new JaggedCholeskyDecomposition(a);

            if (!cholesky.IsPositiveDefinite)
            {
                throw new ArgumentException(
                    "The matrix is not positive definite; use SolveLinearSystem for a general square matrix.",
                    nameof(a));
            }

            return cholesky.Solve((double[])b.Clone());
        }

        [AdvancedAnalyticsTool("SingularValueDecomposition")]
        [Description(
            "Factors any matrix A as U times diag(SingularValues) times V transpose. Singular values come largest first; the number above tolerance is the rank, and the ratio of largest to smallest is the condition number. " +
            "V holds the right singular vectors as columns (the principal directions of the data in a mean-centred matrix) and U the left ones. Limited to 300 by 300.")]
        public NumericsSvdResult SingularValueDecomposition(
            [Description("Matrix of any shape as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _);
            var svd = new JaggedSingularValueDecomposition(matrix, true, true, true);

            return new NumericsSvdResult
            {
                U = svd.LeftSingularVectors,
                SingularValues = svd.Diagonal,
                V = svd.RightSingularVectors,
                Rank = svd.Rank,
                ConditionNumber = svd.Condition
            };
        }

        [AdvancedAnalyticsTool("SymmetricEigenDecomposition")]
        [Description(
            "Finds the eigenvalues and orthonormal eigenvectors of a symmetric matrix such as a covariance or correlation matrix. Eigenvectors are the columns of the returned matrix, aligned with the eigenvalues; with sortDescending true the largest comes first. " +
            "All eigenvalues are real for a symmetric matrix; non-symmetric input is rejected, use GeneralEigenDecomposition for that. Limited to 300 by 300.")]
        public NumericsEigenResult SymmetricEigenDecomposition(
            [Description("Symmetric square matrix as an array of equal-length rows.")]
            double[][] matrix,
            [Description("When true, eigenvalues and eigenvectors are ordered from largest eigenvalue to smallest.")]
            bool sortDescending = true)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _, symmetric: true);
            var eigen = new JaggedEigenvalueDecomposition(matrix, true, false, sortDescending);

            return new NumericsEigenResult
            {
                RealEigenvalues = eigen.RealEigenvalues,
                ImaginaryEigenvalues = eigen.ImaginaryEigenvalues,
                Eigenvectors = eigen.Eigenvectors,
                HasComplexEigenvalues = false
            };
        }

        [AdvancedAnalyticsTool("GeneralEigenDecomposition")]
        [Description(
            "Finds the eigenvalues and eigenvectors of any square matrix, symmetric or not, such as a Markov transition matrix or a dynamical system. " +
            "Eigenvalues may be complex: the real and imaginary parts are returned in parallel arrays, and HasComplexEigenvalues says whether any imaginary part is non-zero (a rotation-like or oscillating system). Eigenvectors are columns and, for complex pairs, hold the real and imaginary parts in adjacent columns. Limited to 300 by 300.")]
        public NumericsEigenResult GeneralEigenDecomposition(
            [Description("Square matrix as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _, true);
            var eigen = new JaggedEigenvalueDecomposition(matrix, false, false, false);

            return new NumericsEigenResult
            {
                RealEigenvalues = eigen.RealEigenvalues,
                ImaginaryEigenvalues = eigen.ImaginaryEigenvalues,
                Eigenvectors = eigen.Eigenvectors,
                HasComplexEigenvalues = eigen.ImaginaryEigenvalues.Any(v => Math.Abs(v) > 1e-12)
            };
        }

        [AdvancedAnalyticsTool("GeneralizedEigenDecomposition")]
        [Description(
            "Solves the generalised eigenproblem A v = lambda B v for two square matrices of the same size, as used in linear discriminant analysis and vibration problems. With B the identity it reduces to the ordinary eigenproblem. " +
            "Eigenvalues are returned as real and imaginary parts; IsSingular or IsDegenerate true warns that B is singular so some eigenvalues are infinite or undefined. Limited to 300 by 300.")]
        public NumericsGeneralizedEigenResult GeneralizedEigenDecomposition(
            [Description("Square matrix A as an array of equal-length rows.")]
            double[][] a,
            [Description("Square matrix B, same size as A.")]
            double[][] b)
        {
            NumericsGuard.RequireMatrix(a, nameof(a), out var rows, out _, true);
            NumericsGuard.RequireMatrix(b, nameof(b), out var bRows, out _, true);

            if (rows != bRows)
            {
                throw new ArgumentException(
                    $"A is {rows} by {rows} but B is {bRows} by {bRows}; they must be the same size.", nameof(b));
            }

            var eigen = new JaggedGeneralizedEigenvalueDecomposition(a, b);

            return new NumericsGeneralizedEigenResult
            {
                RealEigenvalues = eigen.RealEigenvalues,
                ImaginaryEigenvalues = eigen.ImaginaryEigenvalues,
                Eigenvectors = eigen.Eigenvectors,
                IsSingular = eigen.IsSingular,
                IsDegenerate = eigen.IsDegenerate
            };
        }

        [AdvancedAnalyticsTool("GramSchmidtOrthogonalisation")]
        [Description(
            "Orthonormalises the columns of a square matrix with the Gram-Schmidt process, returning Q (orthonormal columns) and R (upper triangular) with A = Q R. Use the modified variant for better numerical stability with nearly dependent columns. " +
            "Requires a square matrix with independent columns (dependent columns give NaN). Limited to 300 by 300.")]
        public NumericsQrResult GramSchmidtOrthogonalisation(
            [Description("Square matrix as an array of equal-length rows.")]
            double[][] matrix,
            [Description("True uses modified Gram-Schmidt (more stable); false the classical form.")]
            bool modified = true)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _, true);
            var gs = new GramSchmidtOrthogonalization(NumericsGuard.ToRectangular(matrix), modified);

            return new NumericsQrResult
            {
                Q = NumericsGuard.FromRectangular(gs.OrthogonalFactor),
                R = NumericsGuard.FromRectangular(gs.UpperTriangularFactor),
                IsFullRank = true
            };
        }

        [AdvancedAnalyticsTool("NonnegativeMatrixFactorisation")]
        [Description(
            "Approximates a non-negative matrix V (m by n) as W (m by r) times H (r by n) with all entries non-negative, which yields parts-based components such as topics or spending patterns. " +
            "The multiplicative-update algorithm starts from seeded random factors so the same seed reproduces the result; ReconstructionError is the Frobenius norm of V minus W H (0 is a perfect fit). All entries of V must be zero or positive. Limited to 300 by 300, rank up to 50 and 5000 iterations.")]
        public NumericsNonnegativeFactorsResult NonnegativeMatrixFactorisation(
            [Description("Non-negative matrix V as an array of equal-length rows.")]
            double[][] matrix,
            [Description("Number of components r, from 1 up to 50 and not more than the smaller matrix dimension.")]
            int rank,
            [Description("Maximum number of update iterations, from 1 to 5000.")]
            int maximumIterations = 500,
            [Description("Seed that makes the random starting point reproducible.")]
            int seed = 1)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out var rows, out var columns);
            NumericsGuard.RequireInRange(rank, nameof(rank), 1, Math.Min(50, Math.Min(rows, columns)));
            NumericsGuard.RequireInRange(maximumIterations, nameof(maximumIterations), 1, 5000);

            if (matrix.Any(r => r.Any(v => v < 0)))
            {
                throw new ArgumentException(
                    "All entries of the matrix must be zero or positive for a non-negative factorisation.",
                    nameof(matrix));
            }

            var previous = Generator.ThreadSeed;

            try
            {
                Generator.ThreadSeed = seed == 0 ? 1 : seed;

                var nmf = new NonnegativeMatrixFactorization(NumericsGuard.ToRectangular(matrix), rank,
                    maximumIterations);

                var w = NumericsGuard.FromRectangular(nmf.LeftNonnegativeFactors);
                var h = NumericsGuard.FromRectangular(nmf.RightNonnegativeFactors);
                var product = w.Dot(h);
                var sum = 0.0;

                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < columns; j++)
                    {
                        var difference = matrix[i][j] - product[i][j];
                        sum += difference * difference;
                    }
                }

                return new NumericsNonnegativeFactorsResult { W = w, H = h, ReconstructionError = Math.Sqrt(sum) };
            }
            finally
            {
                Generator.ThreadSeed = previous;
            }
        }
    }
}