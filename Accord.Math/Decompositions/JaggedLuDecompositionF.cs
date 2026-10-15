// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Original work copyright © Lutz Roeder, 2000
//  Adapted from Mapack for .NET, September 2000
//  Adapted from Mapack for COM and Jama routines
//  http://www.aisto.com/roeder/dotnet
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;

namespace Accord.Math.Decompositions
{
    /// <summary>
    ///     LU decomposition of a jagged rectangular matrix.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For an m-by-n matrix <c>A</c> with <c>m >= n</c>, the LU decomposition is an m-by-n
    ///         unit lower triangular matrix <c>L</c>, an n-by-n upper triangular matrix <c>U</c>,
    ///         and a permutation vector <c>piv</c> of length m so that <c>A(piv) = L*U</c>.
    ///         If m &lt; n, then <c>L</c> is m-by-m and <c>U</c> is m-by-n.
    ///     </para>
    ///     <para>
    ///         The LU decomposition with pivoting always exists, even if the matrix is
    ///         singular, so the constructor will never fail.  The primary use of the
    ///         LU decomposition is in the solution of square systems of simultaneous
    ///         linear equations. This will fail if <see cref="Nonsingular" /> returns
    ///         <see langword="false" />.
    ///     </para>
    ///     <para>
    ///         If you need to compute a LU decomposition for matrices with data types other than
    ///         double, see <see cref="JaggedLuDecompositionF" />, <see cref="JaggedLuDecompositionD" />. If you
    ///         need to compute a LU decomposition for a multidimensional matrix, see <see cref="LuDecomposition" />,
    ///         <see cref="LuDecompositionF" />, and <see cref="LuDecompositionD" />.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Math\Decompositions\JaggedLuDecompositionFTest.cs" region="doc_ctor" />
    /// </example>
    /// <seealso cref="CholeskyDecomposition" />
    /// <seealso cref="EigenvalueDecomposition" />
    /// <seealso cref="SingularValueDecomposition" />
    /// <seealso cref="JaggedEigenvalueDecomposition" />
    /// <seealso cref="JaggedSingularValueDecomposition" />
    public sealed class JaggedLuDecompositionF : ICloneable, ISolverArrayDecomposition<float>
    {
        private int cols;

        // cache for lazy evaluation
        private float? determinant;
        private double? lndeterminant;
        private float[][] lowerTriangularFactor;
        private float[][] lu;
        private bool? nonsingular;

        private int pivotSign;
        private int rows;
        private float[][] upperTriangularFactor;

        /// <summary>
        ///     Constructs a new LU decomposition.
        /// </summary>
        /// <param name="value">The matrix A to be decomposed.</param>
        /// <param name="inPlace">
        ///     True if the decomposition should be performed over the
        ///     <paramref name="value" /> matrix rather than on a copy of it. If true, the
        ///     matrix will be destroyed during the decomposition. Default is false.
        /// </param>
        /// <param name="transpose">
        ///     True if the decomposition should be performed on
        ///     the transpose of A rather than A itself, false otherwise. Default is false.
        /// </param>
        public JaggedLuDecompositionF(float[][] value, bool transpose = false, bool inPlace = false)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "Matrix cannot be null.");
            }

            if (transpose)
            {
                lu = value.Transpose(inPlace);
            }
            else
            {
                lu = inPlace ? value : Matrix.MemberwiseClone(value);
            }

            rows = lu.Length;
            cols = lu[0].Length;
            pivotSign = 1;

            PivotPermutationVector = new int[rows];
            for (var i = 0; i < rows; i++)
            {
                PivotPermutationVector[i] = i;
            }

            var LUcolj = new float[rows];


            // Outer loop.
            for (var j = 0; j < cols; j++)
            {
                // Make a copy of the j-th column to localize references.
                for (var i = 0; i < rows; i++)
                {
                    LUcolj[i] = lu[i][j];
                }

                // Apply previous transformations.
                for (var i = 0; i < rows; i++)
                {
                    float s = 0;

                    // Most of the time is spent in
                    // the following dot product:
                    var kmax = System.Math.Min(i, j);
                    for (var k = 0; k < kmax; k++)
                    {
                        s += lu[i][k] * LUcolj[k];
                    }

                    lu[i][j] = LUcolj[i] -= s;
                }

                // Find pivot and exchange if necessary.
                var p = j;
                for (var i = j + 1; i < rows; i++)
                {
                    if (System.Math.Abs(LUcolj[i]) > System.Math.Abs(LUcolj[p]))
                    {
                        p = i;
                    }
                }

                if (p != j)
                {
                    for (var k = 0; k < cols; k++)
                    {
                        var t = lu[p][k];
                        lu[p][k] = lu[j][k];
                        lu[j][k] = t;
                    }

                    var v = PivotPermutationVector[p];
                    PivotPermutationVector[p] = PivotPermutationVector[j];
                    PivotPermutationVector[j] = v;

                    pivotSign = -pivotSign;
                }

                // Compute multipliers.
                if (j < rows && lu[j][j] != 0)
                {
                    for (var i = j + 1; i < rows; i++)
                    {
                        lu[i][j] /= lu[j][j];
                    }
                }
            }
        }

        /// <summary>
        ///     Returns if the matrix is non-singular (i.e. invertible).
        /// </summary>
        public bool Nonsingular
        {
            get
            {
                if (!nonsingular.HasValue)
                {
                    if (rows != cols)
                    {
                        throw new InvalidOperationException("Matrix must be square.");
                    }

                    var nonSingular = true;
                    for (var i = 0; i < rows && nonSingular; i++)
                    {
                        if (lu[i][i] == 0)
                        {
                            nonSingular = false;
                        }
                    }

                    nonsingular = nonSingular;
                }

                return nonsingular.Value;
            }
        }

        /// <summary>
        ///     Returns the determinant of the matrix.
        /// </summary>
        public float Determinant
        {
            get
            {
                if (!determinant.HasValue)
                {
                    if (rows != cols)
                    {
                        throw new InvalidOperationException("Matrix must be square.");
                    }

                    float det = pivotSign;
                    for (var i = 0; i < rows; i++)
                    {
                        det *= lu[i][i];
                    }

                    determinant = det;
                }

                return determinant.Value;
            }
        }

        /// <summary>
        ///     Returns the log-determinant of the matrix.
        /// </summary>
        public double LogDeterminant
        {
            get
            {
                if (!lndeterminant.HasValue)
                {
                    if (rows != cols)
                    {
                        throw new InvalidOperationException("Matrix must be square.");
                    }

                    double lndet = 0;
                    for (var i = 0; i < rows; i++)
                    {
                        lndet += System.Math.Log(System.Math.Abs(lu[i][i]));
                    }

                    lndeterminant = lndet;
                }

                return lndeterminant.Value;
            }
        }

        /// <summary>
        ///     Returns the lower triangular factor <c>L</c> with <c>A=LU</c>.
        /// </summary>
        public float[][] LowerTriangularFactor
        {
            get
            {
                if (lowerTriangularFactor == null)
                {
                    var L = new float[rows][];

                    for (var i = 0; i < rows; i++)
                    {
                        L[i] = new float[rows];

                        for (var j = 0; j < rows; j++)
                        {
                            if (i > j)
                            {
                                L[i][j] = lu[i][j];
                            }
                            else if (i == j)
                            {
                                L[i][j] = 1;
                            }
                            else
                            {
                                L[i][j] = 0;
                            }
                        }
                    }

                    lowerTriangularFactor = L;
                }

                return lowerTriangularFactor;
            }
        }

        /// <summary>
        ///     Returns the lower triangular factor <c>L</c> with <c>A=LU</c>.
        /// </summary>
        public float[][] UpperTriangularFactor
        {
            get
            {
                if (upperTriangularFactor == null)
                {
                    var U = new float[rows][];
                    for (var i = 0; i < rows; i++)
                    {
                        U[i] = new float[cols];

                        for (var j = 0; j < cols; j++)
                        {
                            if (i <= j)
                            {
                                U[i][j] = lu[i][j];
                            }
                            else
                            {
                                U[i][j] = 0;
                            }
                        }
                    }

                    upperTriangularFactor = U;
                }

                return upperTriangularFactor;
            }
        }

        /// <summary>
        ///     Returns the pivot permutation vector.
        /// </summary>
        public int[] PivotPermutationVector { get; private set; }

        /// <summary>
        ///     Solves a set of equation systems of type <c>A * X = I</c>.
        /// </summary>
        public float[][] Inverse()
        {
            if (!Nonsingular)
            {
                throw new SingularMatrixException("Matrix is singular.");
            }

            // Copy right hand side with pivoting
            var X = new float[rows][];
            for (var i = 0; i < rows; i++)
            {
                X[i] = new float[rows];
                var k = PivotPermutationVector[i];
                X[i][k] = 1;
            }

            // Solve L*Y = B(piv,:)
            for (var k = 0; k < rows; k++)
            {
                for (var i = k + 1; i < rows; i++)
                {
                    for (var j = 0; j < rows; j++)
                    {
                        X[i][j] -= X[k][j] * lu[i][k];
                    }
                }
            }

            // Solve U*X = I;
            for (var k = rows - 1; k >= 0; k--)
            {
                for (var j = 0; j < rows; j++)
                {
                    X[k][j] /= lu[k][k];
                }

                for (var i = 0; i < k; i++)
                {
                    for (var j = 0; j < rows; j++)
                    {
                        X[i][j] -= X[k][j] * lu[i][k];
                    }
                }
            }

            return X;
        }

        /// <summary>
        ///     Reverses the decomposition, reconstructing the original matrix <c>X</c>.
        /// </summary>
        public float[][] Reverse()
        {
            return LowerTriangularFactor.Dot(UpperTriangularFactor)
                .Get(PivotPermutationVector.ArgSort(), null);
        }

        /// <summary>
        ///     Computes <c>(Xt * X)^1</c> (the inverse of the covariance matrix). This
        ///     matrix can be used to determine standard errors for the coefficients when
        ///     solving a linear set of equations through any of the <see cref="Solve(Single[][])" />
        ///     methods.
        /// </summary>
        public float[][] GetInformationMatrix()
        {
            var X = Reverse();
            return X.TransposeAndDot(X).Inverse();
        }

        /// <summary>
        ///     Solves a set of equation systems of type <c>A * X = B</c>.
        /// </summary>
        /// <param name="value">Right hand side matrix with as many rows as <c>A</c> and any number of columns.</param>
        /// <returns>Matrix <c>X</c> so that <c>L * U * X = B</c>.</returns>
        public float[][] Solve(float[][] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.Length != rows)
            {
                throw new DimensionMismatchException("value",
                    "The matrix should have the same number of rows as the decomposition.");
            }

            if (!Nonsingular)
            {
                throw new InvalidOperationException("Matrix is singular.");
            }


            // Copy right hand side with pivoting
            var count = value[0].Length;
            var X = value.Get(PivotPermutationVector, null);


            // Solve L*Y = B(piv,:)
            for (var k = 0; k < cols; k++)
            {
                for (var i = k + 1; i < cols; i++)
                {
                    for (var j = 0; j < count; j++)
                    {
                        X[i][j] -= X[k][j] * lu[i][k];
                    }
                }
            }

            // Solve U*X = Y;
            for (var k = cols - 1; k >= 0; k--)
            {
                for (var j = 0; j < count; j++)
                {
                    X[k][j] /= lu[k][k];
                }

                for (var i = 0; i < k; i++)
                {
                    for (var j = 0; j < count; j++)
                    {
                        X[i][j] -= X[k][j] * lu[i][k];
                    }
                }
            }

            return X;
        }

        /// <summary>
        ///     Solves a set of equation systems of type <c>A * X = B</c> where B is a diagonal matrix.
        /// </summary>
        /// <param name="diagonal">Diagonal fo the right hand side matrix with as many rows as <c>A</c>.</param>
        /// <returns>Matrix <c>X</c> so that <c>L * U * X = B</c>.</returns>
        public float[][] SolveForDiagonal(float[] diagonal)
        {
            if (diagonal == null)
            {
                throw new ArgumentNullException("diagonal");
            }

            return Solve(Jagged.Diagonal(diagonal));
        }

        /// <summary>
        ///     Solves a set of equation systems of type <c>A * X = B</c>.
        /// </summary>
        /// <param name="value">Right hand side column vector with as many rows as <c>A</c>.</param>
        /// <returns>Matrix <c>X</c> so that <c>L * U * X = B</c>.</returns>
        public float[] Solve(float[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.Length != rows)
            {
                throw new DimensionMismatchException("value",
                    "The vector should have the same length as rows in the decomposition.");
            }

            if (!Nonsingular)
            {
                throw new InvalidOperationException("Matrix is singular.");
            }


            // Copy right hand side with pivoting
            var count = value.Length;
            var b = new float[count];
            for (var i = 0; i < b.Length; i++)
            {
                b[i] = value[PivotPermutationVector[i]];
            }


            // Solve L*Y = B
            var X = new float[count];
            for (var i = 0; i < rows; i++)
            {
                X[i] = b[i];
                for (var j = 0; j < i; j++)
                {
                    X[i] -= lu[i][j] * X[j];
                }
            }

            // Solve U*X = Y;
            for (var i = rows - 1; i >= 0; i--)
            {
                for (var j = rows - 1; j > i; j--)
                {
                    X[i] -= lu[i][j] * X[j];
                }

                X[i] /= lu[i][i];
            }

            return X;
        }

        /// <summary>
        ///     Solves a set of equation systems of type <c>X * A = B</c>.
        /// </summary>
        /// <param name="value">Right hand side matrix with as many columns as <c>A</c> and any number of rows.</param>
        /// <returns>Matrix <c>X</c> so that <c>X * L * U = A</c>.</returns>
        public float[][] SolveTranspose(float[][] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.Length != rows)
            {
                throw new DimensionMismatchException("value",
                    "The matrix should have the same number of rows as the decomposition.");
            }

            if (!Nonsingular)
            {
                throw new SingularMatrixException("Matrix is singular.");
            }


            // Copy right hand side with pivoting
            var X = value.Get(null, PivotPermutationVector);

            var count = X[0].Length;

            // Solve L*Y = B(piv,:)
            for (var k = 0; k < rows; k++)
            {
                for (var i = k + 1; i < rows; i++)
                {
                    for (var j = 0; j < count; j++)
                    {
                        X[j][i] -= X[j][k] * lu[i][k];
                    }
                }
            }

            // Solve U*X = Y;
            for (var k = rows - 1; k >= 0; k--)
            {
                for (var j = 0; j < count; j++)
                {
                    X[j][k] /= lu[k][k];
                }

                for (var i = 0; i < k; i++)
                {
                    for (var j = 0; j < count; j++)
                    {
                        X[j][i] -= X[j][k] * lu[i][k];
                    }
                }
            }

            return X;
        }

        #region ICloneable Members

        private JaggedLuDecompositionF()
        {
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var lud = new JaggedLuDecompositionF();
            lud.rows = rows;
            lud.cols = cols;
            lud.lu = Matrix.MemberwiseClone(lu);
            lud.pivotSign = pivotSign;
            lud.PivotPermutationVector = PivotPermutationVector;
            return lud;
        }

        #endregion
    }
}