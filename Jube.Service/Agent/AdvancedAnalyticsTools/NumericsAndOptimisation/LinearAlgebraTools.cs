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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation
{
    public sealed record NumericsDeterminantResult
    {
        public double Determinant { get; init; }
        public double LogAbsoluteDeterminant { get; init; }
        public int Sign { get; init; }
        public bool IsSingular { get; init; }
    }

    public sealed record NumericsMatrixNormsResult
    {
        public double Norm1 { get; init; }
        public double NormInfinity { get; init; }
        public double Frobenius { get; init; }
        public double Spectral { get; init; }
    }

    public sealed record NumericsMatrixPropertiesResult
    {
        public int Rows { get; init; }
        public int Columns { get; init; }
        public bool IsSquare { get; init; }
        public bool IsSymmetric { get; init; }
        public bool IsPositiveDefinite { get; init; }
        public bool IsDiagonal { get; init; }
        public bool IsSingular { get; init; }
        public bool HasNaN { get; init; }
        public bool HasInfinity { get; init; }
    }

    public sealed record NumericsRankResult
    {
        public int Rank { get; init; }
        public double ConditionNumber { get; init; }
        public double LargestSingularValue { get; init; }
        public double SmallestSingularValue { get; init; }
        public bool IsRankDeficient { get; init; }
    }

    public sealed record NumericsLinearSolveResult
    {
        public double[] Solution { get; init; } = Array.Empty<double>();
        public double ResidualNorm { get; init; }
        public double ConditionNumber { get; init; }
    }

    public enum NumericsStructuredMatrix
    {
        Identity,
        Zeros,
        Ones,
        Magic,
        Hilbert,
        Vandermonde
    }

    public enum NumericsElementwiseOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power
    }

    [AdvancedAnalyticsToolGroup("NumericsAndOptimisation")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class LinearAlgebraTools
    {
        [AdvancedAnalyticsTool("MatrixMultiply")]
        [Description(
            "Multiplies two matrices, A (m by k) times B (k by n), giving an m by n matrix. Use it to compose linear maps, project data or check an identity such as A times its inverse. " +
            "The number of columns of A must equal the number of rows of B; every matrix is limited to 300 rows and 300 columns and must contain only finite numbers.")]
        public NumericsMatrixResult MatrixMultiply(
            [Description("Left matrix A as an array of equal-length rows.")]
            double[][] a,
            [Description(
                "Right matrix B as an array of equal-length rows; its row count must equal the column count of A.")]
            double[][] b)
        {
            NumericsGuard.RequireMatrix(a, nameof(a), out _, out var aColumns);
            NumericsGuard.RequireMatrix(b, nameof(b), out var bRows, out _);

            if (aColumns != bRows)
            {
                throw new ArgumentException(
                    $"A has {aColumns} columns but B has {bRows} rows; they must be equal to multiply A by B.",
                    nameof(b));
            }

            return NumericsGuard.ToMatrixResult(a.Dot(b));
        }

        [AdvancedAnalyticsTool("MatrixVectorMultiply")]
        [Description(
            "Multiplies a matrix by a column vector, A (m by n) times v (length n), giving a vector of length m. Use it to apply a linear transformation or compute fitted values from a design matrix and coefficients. " +
            "Limited to 300 rows and 300 columns; NaN or infinite inputs are rejected.")]
        public NumericsVectorResult MatrixVectorMultiply(
            [Description("Matrix A as an array of equal-length rows.")]
            double[][] a,
            [Description("Vector v whose length must equal the number of columns of A.")]
            double[] v)
        {
            NumericsGuard.RequireMatrix(a, nameof(a), out _, out var columns);
            NumericsGuard.RequireVector(v, nameof(v), columns, columns);

            return NumericsGuard.ToVectorResult(a.Dot(v));
        }

        [AdvancedAnalyticsTool("MatrixTranspose")]
        [Description(
            "Returns the transpose of a matrix: rows become columns, so an m by n matrix gives an n by m matrix. Use it before multiplying when shapes do not line up. " +
            "Limited to 300 rows and 300 columns.")]
        public NumericsMatrixResult MatrixTranspose(
            [Description("Matrix as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _);

            return NumericsGuard.ToMatrixResult(matrix.Transpose());
        }

        [AdvancedAnalyticsTool("MatrixElementwiseOperation")]
        [Description(
            "Applies an element by element operation to two same-shaped matrices: Add, Subtract, Multiply (the Hadamard product), Divide or Power (each element of A raised to the matching element of B). " +
            "This is not matrix multiplication; use MatrixMultiply for that. Division by zero or a negative base with a fractional power yields NaN or infinity in that cell, which the NonFiniteCount-style caller should inspect. Limited to 300 by 300.")]
        public NumericsMatrixResult MatrixElementwiseOperation(
            [Description("First matrix A as an array of equal-length rows.")]
            double[][] a,
            [Description("Second matrix B with exactly the same shape as A.")]
            double[][] b,
            [Description("Operation to apply between matching elements of A and B.")]
            NumericsElementwiseOperation operation = NumericsElementwiseOperation.Add)
        {
            NumericsGuard.RequireMatrix(a, nameof(a), out var rows, out var columns);
            NumericsGuard.RequireMatrix(b, nameof(b), out var bRows, out var bColumns);

            if (rows != bRows || columns != bColumns)
            {
                throw new ArgumentException(
                    $"A is {rows} by {columns} but B is {bRows} by {bColumns}; element by element operations need identical shapes.",
                    nameof(b));
            }

            var result = new double[rows][];

            for (var i = 0; i < rows; i++)
            {
                result[i] = operation switch
                {
                    NumericsElementwiseOperation.Add => a[i].Add(b[i]),
                    NumericsElementwiseOperation.Subtract => a[i].Subtract(b[i]),
                    NumericsElementwiseOperation.Multiply => a[i].Multiply(b[i]),
                    NumericsElementwiseOperation.Divide => a[i].Divide(b[i]),
                    _ => a[i].Select((v, j) => Math.Pow(v, b[i][j])).ToArray()
                };
            }

            return NumericsGuard.ToMatrixResult(result);
        }

        [AdvancedAnalyticsTool("MatrixInverse")]
        [Description(
            "Computes the inverse of a square, non-singular matrix by LU decomposition, so that A times the inverse is the identity. Use it for small exact algebra; prefer SolveLinearSystem when you only need A inverse times a vector because it is more accurate. " +
            "A singular matrix (determinant zero, dependent rows) is rejected with a message; use MatrixPseudoInverse instead. Limited to 300 by 300.")]
        public NumericsMatrixResult MatrixInverse(
            [Description("Square matrix as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _, true);

            if (!new JaggedLuDecomposition(matrix).Nonsingular)
            {
                throw new ArgumentException(
                    "The matrix is singular so it has no inverse; call MatrixPseudoInverse for a generalised inverse.",
                    nameof(matrix));
            }

            return NumericsGuard.ToMatrixResult(matrix.Inverse());
        }

        [AdvancedAnalyticsTool("MatrixPseudoInverse")]
        [Description(
            "Computes the Moore-Penrose pseudo-inverse of any matrix (square or rectangular, full rank or not) using the singular value decomposition. Use it to get least-squares solutions for over-determined systems or minimum-norm solutions for under-determined ones, and when the matrix may be singular. " +
            "For a full column rank matrix A the result equals (A transpose A) inverse times A transpose. Limited to 300 by 300.")]
        public NumericsMatrixResult MatrixPseudoInverse(
            [Description("Matrix of any shape as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _);

            return NumericsGuard.ToMatrixResult(matrix.PseudoInverse());
        }

        [AdvancedAnalyticsTool("MatrixDeterminant")]
        [Description(
            "Returns the determinant of a square matrix via LU decomposition together with the log of its absolute value and its sign. Use the log form for large matrices where the determinant itself overflows or underflows. " +
            "A determinant of zero (IsSingular true) means the rows are linearly dependent and the matrix cannot be inverted. Limited to 300 by 300.")]
        public NumericsDeterminantResult MatrixDeterminant(
            [Description("Square matrix as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _, true);
            var lu = new JaggedLuDecomposition(matrix);
            var determinant = lu.Determinant;

            return new NumericsDeterminantResult
            {
                Determinant = determinant,
                LogAbsoluteDeterminant = lu.Nonsingular ? lu.LogDeterminant : double.NegativeInfinity,
                Sign = Math.Sign(determinant),
                IsSingular = !lu.Nonsingular
            };
        }

        [AdvancedAnalyticsTool("MatrixPseudoDeterminant")]
        [Description(
            "Returns the pseudo-determinant of a matrix: the product of its non-zero singular values, plus the log of that product. Use it for singular or rectangular matrices where the ordinary determinant is zero or undefined, for example in degenerate Gaussian densities. " +
            "Limited to 300 by 300.")]
        public NumericsVectorResult MatrixPseudoDeterminant(
            [Description("Matrix of any shape as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _);

            return NumericsGuard.ToVectorResult(new[]
            {
                matrix.PseudoDeterminant(),
                matrix.LogPseudoDeterminant()
            });
        }

        [AdvancedAnalyticsTool("MatrixTrace")]
        [Description(
            "Returns the trace of a square matrix, the sum of its diagonal elements, which also equals the sum of its eigenvalues. Limited to 300 by 300.")]
        public double MatrixTrace(
            [Description("Square matrix as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out var rows, out _, true);
            var sum = 0.0;

            for (var i = 0; i < rows; i++)
            {
                sum += matrix[i][i];
            }

            return sum;
        }

        [AdvancedAnalyticsTool("MatrixRankAndCondition")]
        [Description(
            "Reports the numerical rank, condition number and extreme singular values of any matrix from its singular value decomposition. Rank below the smaller dimension means dependent rows or columns; a condition number above about 1e12 means the matrix is numerically singular and solutions will be unreliable. " +
            "ConditionNumber is largest over smallest singular value (infinity when the smallest is zero). IsRankDeficient is true when the rank is below the smaller dimension. Limited to 300 by 300.")]
        public NumericsRankResult MatrixRankAndCondition(
            [Description("Matrix of any shape as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _);
            var svd = new JaggedSingularValueDecomposition(matrix, true, true, true);
            var values = svd.Diagonal;

            return new NumericsRankResult
            {
                Rank = svd.Rank,
                ConditionNumber = svd.Condition,
                LargestSingularValue = values.Max(),
                SmallestSingularValue = values.Min(),
                IsRankDeficient = svd.Rank < Math.Min(matrix.Length, matrix[0].Length)
            };
        }

        [AdvancedAnalyticsTool("MatrixNorms")]
        [Description(
            "Computes the standard matrix norms: Norm1 is the largest absolute column sum, NormInfinity the largest absolute row sum, Frobenius the square root of the sum of squared elements, and Spectral the largest singular value. " +
            "Use them to size errors and to judge how large a linear map can stretch a vector. Limited to 300 by 300.")]
        public NumericsMatrixNormsResult MatrixNorms(
            [Description("Matrix of any shape as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _);

            return new NumericsMatrixNormsResult
            {
                Norm1 = matrix.Norm1(),
                NormInfinity = matrix.Transpose().Norm1(),
                Frobenius = matrix.Frobenius(),
                Spectral = matrix.Norm2()
            };
        }

        [AdvancedAnalyticsTool("VectorNorms")]
        [Description(
            "Computes the Euclidean (L2) norm, squared L2 norm, and normalised unit-length copy of a vector. Use it to measure magnitude or to scale a direction vector to length one. " +
            "A zero vector has norm 0 and its normalised copy stays all zeros because it has no direction. Maximum 200000 values.")]
        public NumericsVectorResult VectorNorms(
            [Description("Vector of finite numbers.")]
            double[] vector,
            [Description(
                "When true the Values field holds the unit-length copy of the vector; when false it holds the two norms [L2, squared L2].")]
            bool normalise = false)
        {
            NumericsGuard.RequireVector(vector, nameof(vector));

            return normalise
                ? NumericsGuard.ToVectorResult(vector.Normalize())
                : NumericsGuard.ToVectorResult(new[] { vector.Euclidean(), vector.SquareEuclidean() });
        }

        [AdvancedAnalyticsTool("SolveLinearSystem")]
        [Description(
            "Solves the square linear system A x = b exactly by LU decomposition and returns x, the residual norm of A x - b and the condition number of A. Use it in preference to inverting A. " +
            "A singular A is rejected with a message; a large condition number (above 1e12) warns that x is numerically unreliable. Limited to 300 by 300.")]
        public NumericsLinearSolveResult SolveLinearSystem(
            [Description("Square coefficient matrix A as an array of equal-length rows.")]
            double[][] a,
            [Description("Right hand side vector b whose length equals the number of rows of A.")]
            double[] b)
        {
            NumericsGuard.RequireMatrix(a, nameof(a), out var rows, out _, true);
            NumericsGuard.RequireVector(b, nameof(b), rows, rows);

            if (!new JaggedLuDecomposition(a).Nonsingular)
            {
                throw new ArgumentException(
                    "The matrix is singular so the system has no unique solution; use SolveLeastSquares for a best-fit or minimum-norm answer.",
                    nameof(a));
            }

            var x = a.Solve(b);

            return new NumericsLinearSolveResult
            {
                Solution = x,
                ResidualNorm = a.Dot(x).Subtract(b).Euclidean(),
                ConditionNumber = new JaggedSingularValueDecomposition(a, true, true, true).Condition
            };
        }

        [AdvancedAnalyticsTool("SolveLinearSystemMultipleRightHandSides")]
        [Description(
            "Solves A X = B for a whole matrix of right hand sides at once, where A is square and non-singular; column j of X solves A x = column j of B. Use it instead of repeated SolveLinearSystem calls. " +
            "A singular A is rejected. Limited to 300 by 300.")]
        public NumericsMatrixResult SolveLinearSystemMultipleRightHandSides(
            [Description("Square coefficient matrix A as an array of equal-length rows.")]
            double[][] a,
            [Description(
                "Matrix B whose row count equals the number of rows of A; each column is one right hand side.")]
            double[][] b)
        {
            NumericsGuard.RequireMatrix(a, nameof(a), out var rows, out _, true);
            NumericsGuard.RequireMatrix(b, nameof(b), out var bRows, out _);

            if (bRows != rows)
            {
                throw new ArgumentException($"B has {bRows} rows but A has {rows}; they must be equal.", nameof(b));
            }

            if (!new JaggedLuDecomposition(a).Nonsingular)
            {
                throw new ArgumentException("The matrix is singular so the system has no unique solution.", nameof(a));
            }

            return NumericsGuard.ToMatrixResult(a.Solve(b));
        }

        [AdvancedAnalyticsTool("SolveLeastSquares")]
        [Description(
            "Finds x minimising the squared error of A x - b for any shaped A, using QR for tall matrices and the singular value decomposition for wide or rank deficient ones. Use it for over-determined systems such as fitting a line through many points; the design matrix carries one row per observation. " +
            "ResidualNorm is the Euclidean length of A x - b (zero for an exactly solvable system); ConditionNumber warns of collinear columns when very large. Limited to 300 by 300.")]
        public NumericsLinearSolveResult SolveLeastSquares(
            [Description("Coefficient (design) matrix A with one row per equation.")]
            double[][] a,
            [Description("Right hand side vector b whose length equals the number of rows of A.")]
            double[] b)
        {
            NumericsGuard.RequireMatrix(a, nameof(a), out var rows, out _);
            NumericsGuard.RequireVector(b, nameof(b), rows, rows);

            var x = a.Solve(b, true);

            return new NumericsLinearSolveResult
            {
                Solution = x,
                ResidualNorm = a.Dot(x).Subtract(b).Euclidean(),
                ConditionNumber = new JaggedSingularValueDecomposition(a, true, true, true).Condition
            };
        }

        [AdvancedAnalyticsTool("MatrixNullSpace")]
        [Description(
            "Returns an orthonormal basis of the null space of a matrix: each returned row is a vector x (with one value per column of the input) satisfying A x = 0. An empty result (zero rows) means the columns are independent and only x = 0 solves it. " +
            "Use it to find hidden linear dependencies among variables. Computed from the singular value decomposition. Limited to 300 by 300.")]
        public NumericsMatrixResult MatrixNullSpace(
            [Description("Matrix of any shape as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out var rows, out var columns);
            var padded = new double[Math.Max(rows, columns)][];

            for (var i = 0; i < padded.Length; i++)
            {
                padded[i] = i < rows ? (double[])matrix[i].Clone() : new double[columns];
            }

            var svd = new JaggedSingularValueDecomposition(padded, true, true, false);
            var rank = svd.Rank;
            var vectors = new List<double[]>();

            for (var k = rank; k < columns; k++)
            {
                vectors.Add(svd.RightSingularVectors.Select(r => r[k]).ToArray());
            }

            return NumericsGuard.ToMatrixResult(vectors.ToArray());
        }

        [AdvancedAnalyticsTool("MatrixReducedRowEchelonForm")]
        [Description(
            "Reduces a matrix to reduced row echelon form by Gauss-Jordan elimination and returns it with the pivot columns, so the pivots reveal which columns are independent. Use it to solve small exact systems by hand-style elimination. " +
            "Values near zero are ordinary floating point, so treat tiny entries as zero. Limited to 300 by 300.")]
        public NumericsMatrixResult MatrixReducedRowEchelonForm(
            [Description("Matrix of any shape as an array of equal-length rows.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out _, out _);
            var rref = new JaggedReducedRowEchelonForm(NumericsGuard.Copy(matrix));

            return NumericsGuard.ToMatrixResult(rref.Result);
        }

        [AdvancedAnalyticsTool("MatrixKroneckerProduct")]
        [Description(
            "Returns the Kronecker product of two matrices: each element of A scales a full copy of B, giving an (mA*mB) by (nA*nB) block matrix. Use it to build structured covariance or design matrices. " +
            "The result must not exceed 300 by 300, so limit both inputs to fit.")]
        public NumericsMatrixResult MatrixKroneckerProduct(
            [Description("Left matrix A as an array of equal-length rows.")]
            double[][] a,
            [Description("Right matrix B as an array of equal-length rows.")]
            double[][] b)
        {
            NumericsGuard.RequireMatrix(a, nameof(a), out var aRows, out var aColumns);
            NumericsGuard.RequireMatrix(b, nameof(b), out var bRows, out var bColumns);

            if (aRows * bRows > NumericsGuard.MaxMatrixDimension ||
                aColumns * bColumns > NumericsGuard.MaxMatrixDimension)
            {
                throw new ArgumentException(
                    $"The product would be {aRows * bRows} by {aColumns * bColumns} but at most {NumericsGuard.MaxMatrixDimension} by {NumericsGuard.MaxMatrixDimension} is allowed; use smaller matrices.",
                    nameof(b));
            }

            return NumericsGuard.ToMatrixResult(a.Kronecker(b));
        }

        [AdvancedAnalyticsTool("VectorOuterProduct")]
        [Description(
            "Returns the outer product of two vectors, a matrix whose element (i, j) is a[i] times b[j]. Use it to build rank-one matrices. Each vector is limited to 300 values.")]
        public NumericsMatrixResult VectorOuterProduct(
            [Description("Vector a; becomes the rows of the result.")]
            double[] a,
            [Description("Vector b; becomes the columns of the result.")]
            double[] b)
        {
            NumericsGuard.RequireVector(a, nameof(a), 1, NumericsGuard.MaxMatrixDimension);
            NumericsGuard.RequireVector(b, nameof(b), 1, NumericsGuard.MaxMatrixDimension);
            var result = Jagged.Zeros<double>(a.Length, b.Length);

            for (var i = 0; i < a.Length; i++)
            {
                for (var j = 0; j < b.Length; j++)
                {
                    result[i][j] = a[i] * b[j];
                }
            }

            return NumericsGuard.ToMatrixResult(result);
        }

        [AdvancedAnalyticsTool("VectorDotProduct")]
        [Description(
            "Returns the dot (inner) product of two equal-length vectors, the sum of products of matching elements; zero means the vectors are orthogonal. Maximum 200000 values each.")]
        public double VectorDotProduct(
            [Description("First vector of finite numbers.")]
            double[] a,
            [Description("Second vector of the same length as the first.")]
            double[] b)
        {
            NumericsGuard.RequireVector(a, nameof(a));
            NumericsGuard.RequireVector(b, nameof(b), a.Length, a.Length);

            return a.Dot(b);
        }

        [AdvancedAnalyticsTool("VectorCrossProduct")]
        [Description(
            "Returns the cross product of two 3-dimensional vectors: a vector perpendicular to both, with length equal to the area of the parallelogram they span. Both inputs must have exactly 3 values.")]
        public double[] VectorCrossProduct(
            [Description("First 3-dimensional vector.")]
            double[] a,
            [Description("Second 3-dimensional vector.")]
            double[] b)
        {
            NumericsGuard.RequireVector(a, nameof(a), 3, 3);
            NumericsGuard.RequireVector(b, nameof(b), 3, 3);

            return a.Cross(b);
        }

        [AdvancedAnalyticsTool("MatrixStructureProperties")]
        [Description(
            "Tests structural properties of a matrix: squareness, symmetry, positive definiteness (needed for Cholesky and valid covariance matrices), diagonality, singularity, and whether it contains NaN or infinite values. " +
            "Unlike most matrix tools this accepts non-finite entries so it can report them. Symmetry uses a small relative tolerance. Limited to 300 by 300.")]
        public NumericsMatrixPropertiesResult MatrixStructureProperties(
            [Description("Matrix of any shape as an array of equal-length rows; NaN allowed here.")]
            double[][] matrix)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out var rows, out var columns, finite: false);
            var hasNaN = matrix.Any(r => r.Any(double.IsNaN));
            var hasInfinity = matrix.Any(r => r.Any(double.IsInfinity));
            var square = rows == columns;
            var clean = !hasNaN && !hasInfinity;
            var symmetric = square && clean && matrix.IsSymmetric(1e-9, 1e-9);
            var diagonal = true;

            for (var i = 0; i < rows && diagonal; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    if (i != j && matrix[i][j] != 0)
                    {
                        diagonal = false;
                        break;
                    }
                }
            }

            return new NumericsMatrixPropertiesResult
            {
                Rows = rows,
                Columns = columns,
                IsSquare = square,
                IsSymmetric = symmetric,
                IsPositiveDefinite = symmetric && matrix.IsPositiveDefinite(),
                IsDiagonal = diagonal,
                IsSingular = square && clean && !new JaggedLuDecomposition(matrix).Nonsingular,
                HasNaN = hasNaN,
                HasInfinity = hasInfinity
            };
        }

        [AdvancedAnalyticsTool("GenerateStructuredMatrix")]
        [Description(
            "Builds a standard matrix of the requested size: Identity, Zeros, Ones, Magic (a magic square, the sizes 3 and above are supported), Hilbert (element 1/(i+j+1), famously ill-conditioned) or Vandermonde (powers of the supplied nodes, needs 'nodes'). " +
            "Use it for test inputs or to construct design matrices. Size is limited to 300; a Vandermonde matrix has one row per node and 'size' columns.")]
        public NumericsMatrixResult GenerateStructuredMatrix(
            [Description("Kind of matrix to build.")]
            NumericsStructuredMatrix kind,
            [Description("Number of rows and columns (columns for Vandermonde), from 1 to 300.")]
            int size,
            [Description("Vandermonde only: the node values x_i; row i is 1, x_i, x_i squared and so on.")]
            double[]? nodes = null)
        {
            NumericsGuard.RequireInRange(size, nameof(size), 1, NumericsGuard.MaxMatrixDimension);
            double[][] result;

            switch (kind)
            {
                case NumericsStructuredMatrix.Identity:
                    result = Jagged.Identity(size);
                    break;
                case NumericsStructuredMatrix.Zeros:
                    result = Jagged.Zeros<double>(size, size);
                    break;
                case NumericsStructuredMatrix.Ones:
                    result = Jagged.Ones<double>(size, size);
                    break;
                case NumericsStructuredMatrix.Magic:
                    if (size < 3)
                    {
                        throw new ArgumentException("A magic square needs size 3 or more.", nameof(size));
                    }

                    result = Jagged.Magic(size);
                    break;
                case NumericsStructuredMatrix.Hilbert:
                    result = Jagged.Zeros<double>(size, size);

                    for (var i = 0; i < size; i++)
                    {
                        for (var j = 0; j < size; j++)
                        {
                            result[i][j] = 1.0 / (i + j + 1);
                        }
                    }

                    break;
                default:
                    NumericsGuard.RequireVector(nodes, nameof(nodes), 1, NumericsGuard.MaxMatrixDimension);
                    result = Jagged.Zeros<double>(nodes!.Length, size);

                    for (var i = 0; i < nodes.Length; i++)
                    {
                        var power = 1.0;

                        for (var j = 0; j < size; j++)
                        {
                            result[i][j] = power;
                            power *= nodes[i];
                        }
                    }

                    break;
            }

            return NumericsGuard.ToMatrixResult(result);
        }

        [AdvancedAnalyticsTool("MatrixPower")]
        [Description(
            "Raises a square matrix to a non-negative integer power by repeated squaring; power 0 gives the identity. Use it for Markov chain transition matrices over several steps or graph path counting. " +
            "Power is limited to 1000000 and the matrix to 300 by 300; large powers of an unstable matrix overflow to infinity.")]
        public NumericsMatrixResult MatrixPower(
            [Description("Square matrix as an array of equal-length rows.")]
            double[][] matrix,
            [Description("Non-negative integer exponent, at most 1000000.")]
            int power)
        {
            NumericsGuard.RequireMatrix(matrix, nameof(matrix), out var size, out _, true);
            NumericsGuard.RequireInRange(power, nameof(power), 0, 1000000);
            var result = Jagged.Identity(size);
            var baseMatrix = NumericsGuard.Copy(matrix);
            var remaining = power;

            while (remaining > 0)
            {
                if ((remaining & 1) == 1)
                {
                    result = result.Dot(baseMatrix);
                }

                remaining >>= 1;

                if (remaining > 0)
                {
                    baseMatrix = baseMatrix.Dot(baseMatrix);
                }
            }

            return NumericsGuard.ToMatrixResult(result);
        }

        [AdvancedAnalyticsTool("GenerateMeshGrid")]
        [Description(
            "Builds a two-dimensional grid of every (x, y) pair from two evenly spaced axes, one output row per point as [x, y]. Use it to evaluate a function over a plane. " +
            "Each axis needs at least 2 points and the product of the two point counts is limited to 40000.")]
        public NumericsMatrixResult GenerateMeshGrid(
            [Description("Lowest x value.")] double xMinimum,
            [Description("Highest x value, greater than xMinimum.")]
            double xMaximum,
            [Description("Number of x points, 2 or more.")]
            int xPoints,
            [Description("Lowest y value.")] double yMinimum,
            [Description("Highest y value, greater than yMinimum.")]
            double yMaximum,
            [Description("Number of y points, 2 or more.")]
            int yPoints)
        {
            NumericsGuard.RequireFinite(xMinimum, nameof(xMinimum));
            NumericsGuard.RequireFinite(xMaximum, nameof(xMaximum));
            NumericsGuard.RequireFinite(yMinimum, nameof(yMinimum));
            NumericsGuard.RequireFinite(yMaximum, nameof(yMaximum));
            NumericsGuard.RequireInRange(xPoints, nameof(xPoints), 2, 20000);
            NumericsGuard.RequireInRange(yPoints, nameof(yPoints), 2, 20000);

            if (xMaximum <= xMinimum || yMaximum <= yMinimum)
            {
                throw new ArgumentException("Each maximum must be greater than its minimum.", nameof(xMaximum));
            }

            if ((long)xPoints * yPoints > 40000)
            {
                throw new ArgumentException(
                    $"{xPoints} by {yPoints} points is more than the limit of 40000 grid points.",
                    nameof(xPoints));
            }

            var xs = Vector.Interval(xMinimum, xMaximum, xPoints);
            var ys = Vector.Interval(yMinimum, yMaximum, yPoints);

            return NumericsGuard.ToMatrixResult(xs.Mesh(ys));
        }

        [AdvancedAnalyticsTool("GenerateEvenlySpacedValues")]
        [Description(
            "Returns 'count' evenly spaced values from 'start' to 'stop' inclusive, like a linear space. Use it to build an x axis for evaluating or plotting a function. Count is limited to 200000.")]
        public NumericsVectorResult GenerateEvenlySpacedValues(
            [Description("First value.")] double start,
            [Description("Last value, included in the output.")]
            double stop,
            [Description("Number of values, from 2 to 200000.")]
            int count)
        {
            NumericsGuard.RequireFinite(start, nameof(start));
            NumericsGuard.RequireFinite(stop, nameof(stop));
            NumericsGuard.RequireInRange(count, nameof(count), 2, NumericsGuard.MaxVectorLength);
            var step = (stop - start) / (count - 1);
            var values = new double[count];

            for (var i = 0; i < count; i++)
            {
                values[i] = start + i * step;
            }

            values[count - 1] = stop;

            return NumericsGuard.ToVectorResult(values);
        }

        [AdvancedAnalyticsTool("NumericsConvolveVectors")]
        [Description(
            "Convolves a signal with a kernel: each output value is a weighted sum of neighbouring signal values, as in moving-average smoothing or polynomial multiplication. " +
            "With trim true the output has the same length as the signal; otherwise it has signal length plus kernel length minus one. Both inputs are limited to 200000 values.")]
        public NumericsVectorResult NumericsConvolveVectors(
            [Description("Signal values.")] double[] signal,
            [Description("Kernel (filter weights), no longer than the signal.")]
            double[] kernel,
            [Description("When true the result is trimmed to the length of the signal.")]
            bool trim = false)
        {
            NumericsGuard.RequireVector(signal, nameof(signal));
            NumericsGuard.RequireVector(kernel, nameof(kernel), 1, signal.Length);

            return NumericsGuard.ToVectorResult(signal.Convolve(kernel, trim));
        }

        [AdvancedAnalyticsTool("RescaleValuesToRange")]
        [Description(
            "Linearly maps values from a source interval onto a target interval, so the source minimum maps to the target minimum and the source maximum to the target maximum. Use it to normalise to 0 to 1 or to convert units between scales. " +
            "Values outside the source interval extrapolate beyond the target interval; a zero-width source interval is rejected. Maximum 200000 values.")]
        public NumericsVectorResult RescaleValuesToRange(
            [Description("Values to rescale.")] double[] values,
            [Description("Source interval lower end.")]
            double fromMinimum,
            [Description("Source interval upper end, different from fromMinimum.")]
            double fromMaximum,
            [Description("Target interval lower end.")]
            double toMinimum,
            [Description("Target interval upper end.")]
            double toMaximum)
        {
            NumericsGuard.RequireVector(values, nameof(values));
            NumericsGuard.RequireFinite(fromMinimum, nameof(fromMinimum));
            NumericsGuard.RequireFinite(fromMaximum, nameof(fromMaximum));
            NumericsGuard.RequireFinite(toMinimum, nameof(toMinimum));
            NumericsGuard.RequireFinite(toMaximum, nameof(toMaximum));

            if (fromMinimum == fromMaximum)
            {
                throw new ArgumentException(
                    "The source interval has zero width so it cannot be mapped; give different fromMinimum and fromMaximum.",
                    nameof(fromMaximum));
            }

            return NumericsGuard.ToVectorResult(values.Scale(fromMinimum, fromMaximum, toMinimum, toMaximum));
        }
    }
}