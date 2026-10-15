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
using System.Threading.Tasks;

namespace Accord.Math.Decompositions
{
    /// <summary>
    ///     Cholesky Decomposition of a symmetric, positive definite matrix.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For a symmetric, positive definite matrix <c>A</c>, the Cholesky decomposition is a
    ///         lower triangular matrix <c>L</c> so that <c>A = L * L'</c>.
    ///         If the matrix is not positive definite, the constructor returns a partial
    ///         decomposition and sets two internal variables that can be queried using the
    ///         <see cref="IsUndefined" /> and <see cref="IsPositiveDefinite" /> properties.
    ///     </para>
    ///     <para>
    ///         Any square matrix A with non-zero pivots can be written as the product of a
    ///         lower triangular matrix L and an upper triangular matrix U; this is called
    ///         the LU decomposition. However, if A is symmetric and positive definite, we
    ///         can choose the factors such that U is the transpose of L, and this is called
    ///         the Cholesky decomposition. Both the LU and the Cholesky decomposition are
    ///         used to solve systems of linear equations.
    ///     </para>
    ///     <para>
    ///         When it is applicable, the Cholesky decomposition is twice as efficient
    ///         as the LU decomposition.
    ///     </para>
    /// </remarks>
    [Serializable]
    public sealed class JaggedCholeskyDecompositionF : ICloneable, ISolverArrayDecomposition<float>
    {
        private float[][] L;
        private bool destroyed;
        private float? determinant;
        private float[][] diagonalMatrix;

        // cache for lazy evaluation
        private float[][] leftTriangularFactor;
        private double? lndeterminant;
        private int n;
        private bool? nonsingular;

        private bool positiveDefinite;
        private bool robust;

        /// <summary>
        ///     Constructs a new Cholesky Decomposition.
        /// </summary>
        /// <param name="value">
        ///     The symmetric matrix, given in upper triangular form, to be decomposed.
        /// </param>
        /// <param name="robust">
        ///     True to perform a square-root free LDLt decomposition, false otherwise.
        /// </param>
        /// <param name="inPlace">
        ///     True to perform the decomposition in place, storing the factorization in the
        ///     lower triangular part of the given matrix.
        /// </param>
        /// <param name="valueType">
        ///     How to interpret the matrix given to be decomposed. Using this parameter, a lower or
        ///     upper-triangular matrix can be interpreted as a symmetric matrix by assuming both lower
        ///     and upper parts contain the same elements. Use this parameter in conjunction with inPlace
        ///     to save memory by storing the original matrix and its decomposition at the same memory
        ///     location (lower part will contain the decomposition's L matrix, upper part will contains
        ///     the original matrix).
        /// </param>
        public JaggedCholeskyDecompositionF(float[][] value, bool robust = false,
            bool inPlace = false, MatrixType valueType = MatrixType.UpperTriangular)
        {
            if (value.Rows() != value.Columns())
            {
                throw new DimensionMismatchException("value", "Matrix is not square.");
            }

            if (!inPlace)
            {
                value = value.Copy();
            }

            n = value.Rows();
            L = value.ToUpperTriangular(valueType, value);
            this.robust = robust;

            if (robust)
            {
                LDLt(); // Compute square-root free decomposition
            }
            else
            {
                LLt(); // Compute standard Cholesky decomposition
            }
        }

        /// <summary>
        ///     Gets whether the decomposed matrix was positive definite.
        /// </summary>
        public bool IsPositiveDefinite => positiveDefinite && !IsUndefined;

        /// <summary>
        ///     Gets a value indicating whether the LDLt factorization
        ///     has been computed successfully or if it is undefined.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the factorization is not defined; otherwise, <c>false</c>.
        /// </value>
        public bool IsUndefined { get; private set; }

        /// <summary>
        ///     Gets the left (lower) triangular factor
        ///     <c>L</c> so that <c>A = L * D * L'</c>.
        /// </summary>
        public float[][] LeftTriangularFactor
        {
            get
            {
                if (leftTriangularFactor == null)
                {
                    if (destroyed)
                    {
                        throw new InvalidOperationException("The decomposition has been destroyed.");
                    }

                    if (IsUndefined)
                    {
                        throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
                    }

                    leftTriangularFactor = L.GetLowerTriangle();
                }

                return leftTriangularFactor;
            }
        }

        /// <summary>
        ///     Gets the block diagonal matrix of diagonal elements in a LDLt decomposition.
        /// </summary>
        public float[][] DiagonalMatrix
        {
            get
            {
                if (diagonalMatrix == null)
                {
                    if (destroyed)
                    {
                        throw new InvalidOperationException("The decomposition has been destroyed.");
                    }

                    diagonalMatrix = Jagged.Diagonal(Diagonal);
                }

                return diagonalMatrix;
            }
        }

        /// <summary>
        ///     Gets the one-dimensional array of diagonal elements in a LDLt decomposition.
        /// </summary>
        public float[] Diagonal { get; private set; }

        /// <summary>
        ///     Gets the determinant of the decomposed matrix.
        /// </summary>
        public float Determinant
        {
            get
            {
                if (!determinant.HasValue)
                {
                    if (destroyed)
                    {
                        throw new InvalidOperationException("The decomposition has been destroyed.");
                    }

                    if (IsUndefined)
                    {
                        throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
                    }

                    float detL = 1, detD = 1;
                    for (var i = 0; i < n; i++)
                    {
                        detL *= L[i][i];
                    }

                    if (Diagonal != null)
                    {
                        for (var i = 0; i < n; i++)
                        {
                            detD *= Diagonal[i];
                        }
                    }

                    determinant = detL * detL * detD;
                }

                return determinant.Value;
            }
        }

        /// <summary>
        ///     If the matrix is positive-definite, gets the
        ///     log-determinant of the decomposed matrix.
        /// </summary>
        public double LogDeterminant
        {
            get
            {
                if (!lndeterminant.HasValue)
                {
                    if (destroyed)
                    {
                        throw new InvalidOperationException("The decomposition has been destroyed.");
                    }

                    if (IsUndefined)
                    {
                        throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
                    }

                    double detL = 0, detD = 0;
                    for (var i = 0; i < n; i++)
                    {
                        detL += System.Math.Log(L[i][i]);
                    }

                    if (Diagonal != null)
                    {
                        for (var i = 0; i < Diagonal.Length; i++)
                        {
                            detD += System.Math.Log(Diagonal[i]);
                        }
                    }

                    lndeterminant = detL + detL + detD;
                }

                return lndeterminant.Value;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the decomposed
        ///     matrix is non-singular (i.e. invertible).
        /// </summary>
        public bool Nonsingular
        {
            get
            {
                if (!nonsingular.HasValue)
                {
                    if (destroyed)
                    {
                        throw new InvalidOperationException("The decomposition has been destroyed.");
                    }

                    var nonSingular = true;
                    for (var i = 0; i < n && nonSingular; i++)
                    {
                        if (L[i][i] == 0 || Diagonal[i] == 0)
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
        ///     Solves a set of equation systems of type <c>A * X = B</c>.
        /// </summary>
        /// <param name="value">Right hand side matrix with as many rows as <c>A</c> and any number of columns.</param>
        /// <returns>Matrix <c>X</c> so that <c>L * L' * X = B</c>.</returns>
        /// <exception cref="T:System.ArgumentException">Matrix dimensions do not match.</exception>
        /// <exception cref="T:Accord.NonSymmetricMatrixException">Matrix is not symmetric.</exception>
        /// <exception cref="T:Accord.NonPositiveDefiniteMatrixException">Matrix is not positive-definite.</exception>
        public float[][] Solve(float[][] value)
        {
            return Solve(value, false);
        }

        /// <summary>
        ///     Solves a set of equation systems of type <c>A * x = b</c>.
        /// </summary>
        /// <param name="value">Right hand side column vector with as many rows as <c>A</c>.</param>
        /// <returns>Vector <c>x</c> so that <c>L * L' * x = b</c>.</returns>
        /// <exception cref="T:System.ArgumentException">Matrix dimensions do not match.</exception>
        /// <exception cref="T:Accord.NonSymmetricMatrixException">Matrix is not symmetric.</exception>
        /// <exception cref="T:Accord.NonPositiveDefiniteMatrixException">Matrix is not positive-definite.</exception>
        public float[] Solve(float[] value)
        {
            return Solve(value, false);
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
        ///     Solves a set of equation systems of type <c>A * X = I</c>.
        /// </summary>
        public float[][] Inverse()
        {
            return Solve(Jagged.Identity<float>(n));
        }

        /// <summary>
        ///     Reverses the decomposition, reconstructing the original matrix <c>X</c>.
        /// </summary>
        public float[][] Reverse()
        {
            if (destroyed)
            {
                throw new InvalidOperationException("The decomposition has been destroyed.");
            }

            if (IsUndefined)
            {
                throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
            }

            if (robust)
            {
                return LeftTriangularFactor.Dot(DiagonalMatrix).DotWithTransposed(LeftTriangularFactor);
            }

            return LeftTriangularFactor.DotWithTransposed(LeftTriangularFactor);
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

        private void LLt()
        {
            Diagonal = Vector.Ones<float>(n);

            positiveDefinite = true;

            for (var j = 0; j < n; j++)
            {
                float s = 0;
                for (var k = 0; k < j; k++)
                {
                    var t = L[k][j];
                    for (var i = 0; i < k; i++)
                    {
                        t -= L[j][i] * L[k][i];
                    }

                    t = t / L[k][k];

                    L[j][k] = t;
                    s += t * t;
                }

                s = L[j][j] - s;

                // Use a tolerance for positive-definiteness
                positiveDefinite &= s > (float)1e-14 * System.Math.Abs(L[j][j]);

                L[j][j] = (float)System.Math.Sqrt(s);
            }
        }

        private void LDLt()
        {
            Diagonal = new float[n];

            positiveDefinite = true;

            var v = new float[n];
            for (var i = 0; i < L.Length; i++)
            {
                for (var j = 0; j < i; j++)
                {
                    v[j] = L[i][j] * Diagonal[j];
                }

                float d = 0;
                for (var k = 0; k < i; k++)
                {
                    d += L[i][k] * v[k];
                }

                d = Diagonal[i] = v[i] = L[i][i] - d;

                // Use a tolerance for positive-definiteness
                positiveDefinite &= v[i] > (float)1e-14 * System.Math.Abs(L[i][i]);

                // If one of the diagonal elements is zero, the 
                // decomposition (without pivoting) is undefined.
                if (v[i] == 0)
                {
                    IsUndefined = true;
                    return;
                }

                var localI = i;
                var localD = d;
                Parallel.For(i + 1, L.Length, k =>
                {
                    float s = 0;
                    for (var j = 0; j < localI; j++)
                    {
                        s += L[k][j] * v[j];
                    }

                    L[k][localI] = (L[localI][k] - s) / localD;
                });
            }

            for (var i = 0; i < L.Length; i++)
            {
                L[i][i] = 1;
            }
        }

        /// <summary>
        ///     Solves a set of equation systems of type <c>A * X = B</c>.
        /// </summary>
        /// <param name="value">Right hand side matrix with as many rows as <c>A</c> and any number of columns.</param>
        /// <returns>Matrix <c>X</c> so that <c>L * L' * X = B</c>.</returns>
        /// <exception cref="T:System.ArgumentException">Matrix dimensions do not match.</exception>
        /// <exception cref="T:Accord.NonSymmetricMatrixException">Matrix is not symmetric.</exception>
        /// <exception cref="T:Accord.NonPositiveDefiniteMatrixException">Matrix is not positive-definite.</exception>
        /// <param name="inPlace">True to compute the solving in place, false otherwise.</param>
        public float[][] Solve(float[][] value, bool inPlace)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.Length != n)
            {
                throw new ArgumentException(
                    "Argument matrix should have the same number of rows as the decomposed matrix.", "value");
            }

            if (!robust && !positiveDefinite)
            {
                throw new NonPositiveDefiniteMatrixException("Decomposed matrix is not positive definite.");
            }

            if (destroyed)
            {
                throw new InvalidOperationException("The decomposition has been destroyed.");
            }

            if (IsUndefined)
            {
                throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
            }

            var B = inPlace ? value : Matrix.MemberwiseClone(value);

            // Solve L*Y = B;
            for (var k = 0; k < n; k++)
            {
                for (var j = 0; j < B[k].Length; j++)
                {
                    for (var i = 0; i < k; i++)
                    {
                        B[k][j] -= B[i][j] * L[k][i];
                    }

                    B[k][j] /= L[k][k];
                }
            }

            if (robust)
            {
                for (var k = 0; k < Diagonal.Length; k++)
                {
                    for (var j = 0; j < B[k].Length; j++)
                    {
                        B[k][j] /= Diagonal[k];
                    }
                }
            }

            // Solve L'*X = Y;
            for (var k = n - 1; k >= 0; k--)
            {
                for (var j = 0; j < B[k].Length; j++)
                {
                    for (var i = k + 1; i < n; i++)
                    {
                        B[k][j] -= B[i][j] * L[i][k];
                    }

                    B[k][j] /= L[k][k];
                }
            }

            return B;
        }

        /// <summary>
        ///     Solves a set of equation systems of type <c>A * x = b</c>.
        /// </summary>
        /// <param name="value">Right hand side column vector with as many rows as <c>A</c>.</param>
        /// <returns>Vector <c>x</c> so that <c>L * L' * x = b</c>.</returns>
        /// <exception cref="T:System.ArgumentException">Matrix dimensions do not match.</exception>
        /// <exception cref="T:Accord.NonSymmetricMatrixException">Matrix is not symmetric.</exception>
        /// <exception cref="T:Accord.NonPositiveDefiniteMatrixException">Matrix is not positive-definite.</exception>
        /// <param name="inPlace">True to compute the solving in place, false otherwise.</param>
        public float[] Solve(float[] value, bool inPlace)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.Length != n)
            {
                throw new ArgumentException(
                    "Argument vector should have the same length as rows in the decomposed matrix.", "value");
            }

            if (!robust && !positiveDefinite)
            {
                throw new NonPositiveDefiniteMatrixException("Decomposed matrix is not positive definite.");
            }

            if (destroyed)
            {
                throw new InvalidOperationException("The decomposition has been destroyed.");
            }

            if (IsUndefined)
            {
                throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
            }

            var B = inPlace ? value : value.Copy();

            // Solve L*Y = B;
            for (var k = 0; k < n; k++)
            {
                for (var i = 0; i < k; i++)
                {
                    B[k] -= B[i] * L[k][i];
                }

                B[k] /= L[k][k];
            }

            if (robust)
            {
                for (var k = 0; k < Diagonal.Length; k++)
                {
                    B[k] /= Diagonal[k];
                }
            }

            // Solve L'*X = Y;
            for (var k = n - 1; k >= 0; k--)
            {
                for (var i = k + 1; i < n; i++)
                {
                    B[k] -= B[i] * L[i][k];
                }

                B[k] /= L[k][k];
            }

            return B;
        }

        /// <summary>
        ///     Computes the diagonal of the inverse of the decomposed matrix.
        /// </summary>
        public float[] InverseDiagonal(bool destroy = false)
        {
            return InverseDiagonal(new float[n], destroy);
        }

        /// <summary>
        ///     Computes the diagonal of the inverse of the decomposed matrix.
        /// </summary>
        /// <param name="destroy">
        ///     True to conserve memory by reusing the
        ///     same space used to hold the decomposition, thus destroying
        ///     it in the process. Pass false otherwise.
        /// </param>
        /// <param name="result">
        ///     The array to hold the result of the
        ///     computation. Should be of same length as the the diagonal
        ///     of the original matrix.
        /// </param>
        public float[] InverseDiagonal(float[] result, bool destroy = false)
        {
            if (!robust && !positiveDefinite)
            {
                throw new NonPositiveDefiniteMatrixException("Matrix is not positive definite.");
            }

            if (destroyed)
            {
                throw new InvalidOperationException("The decomposition has been destroyed.");
            }

            if (IsUndefined)
            {
                throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
            }

            float[][] S;

            if (destroy)
            {
                S = L;
                destroyed = true;
            }
            else
            {
                S = Jagged.Zeros<float>(n, n);
            }

            // References:
            // http://books.google.com/books?id=myzIPBwyBbcC&pg=PA119

            // Compute the inverse S of the lower triangular matrix L
            // and store in place of the upper triangular part of S.

            for (var j = n - 1; j >= 0; j--)
            {
                S[j][j] = 1 / L[j][j];
                for (var i = j - 1; i >= 0; i--)
                {
                    float sum = 0;
                    for (var k = i + 1; k <= j; k++)
                    {
                        sum += L[k][i] * S[k][j];
                    }

                    S[i][j] = -sum / L[i][i];
                }
            }

            // Compute the 2-norm squared of the rows
            // of the upper (right) triangular matrix S.
            if (robust)
            {
                for (var i = 0; i < S.Length; i++)
                {
                    float sum = 0;
                    for (var j = i; j < S[i].Length; j++)
                    {
                        sum += S[i][j] * S[i][j] / Diagonal[j];
                    }

                    result[i] = sum;
                }
            }
            else
            {
                for (var i = 0; i < S.Length; i++)
                {
                    float sum = 0;
                    for (var j = i; j < S[i].Length; j++)
                    {
                        sum += S[i][j] * S[i][j];
                    }

                    result[i] = sum;
                }
            }

            return result;
        }

        /// <summary>
        ///     Computes the trace of the inverse of the decomposed matrix.
        /// </summary>
        /// <param name="destroy">
        ///     True to conserve memory by reusing the
        ///     same space used to hold the decomposition, thus destroying
        ///     it in the process. Pass false otherwise.
        /// </param>
        public float InverseTrace(bool destroy = false)
        {
            if (!robust && !positiveDefinite)
            {
                throw new NonPositiveDefiniteMatrixException("Matrix is not positive definite.");
            }

            if (destroyed)
            {
                throw new InvalidOperationException("The decomposition has been destroyed.");
            }

            if (IsUndefined)
            {
                throw new InvalidOperationException("The decomposition is undefined (zero in diagonal).");
            }

            float[][] S;

            if (destroy)
            {
                S = L;
                destroyed = true;
            }
            else
            {
                S = Jagged.Zeros<float>(n, n);
            }

            // References:
            // http://books.google.com/books?id=myzIPBwyBbcC&pg=PA119

            // Compute the inverse S of the lower triangular matrix L
            // and store in place of the upper triangular part of S.

            for (var j = n - 1; j >= 0; j--)
            {
                S[j][j] = 1 / L[j][j];
                for (var i = j - 1; i >= 0; i--)
                {
                    float sum = 0;
                    for (var k = i + 1; k <= j; k++)
                    {
                        sum += L[k][i] * S[k][j];
                    }

                    S[i][j] = -sum / L[i][i];
                }
            }

            // Compute the 2-norm squared of the rows
            // of the upper (right) triangular matrix S.
            float trace = 0;

            if (robust)
            {
                for (var i = 0; i < S.Length; i++)
                {
                    for (var j = i; j < S[i].Length; j++)
                    {
                        trace += S[i][j] * S[i][j] / Diagonal[j];
                    }
                }
            }
            else
            {
                for (var i = 0; i < S.Length; i++)
                {
                    for (var j = i; j < S[i].Length; j++)
                    {
                        trace += S[i][j] * S[i][j];
                    }
                }
            }

            return trace;
        }

        /// <summary>
        ///     Creates a new Cholesky decomposition directly from
        ///     an already computed left triangular matrix <c>L</c>.
        /// </summary>
        /// <param name="leftTriangular">The left triangular matrix from a Cholesky decomposition.</param>
        public static JaggedCholeskyDecompositionF FromLeftTriangularMatrix(float[][] leftTriangular)
        {
            var chol = new JaggedCholeskyDecompositionF();
            chol.n = leftTriangular.Length;
            chol.L = leftTriangular;
            chol.positiveDefinite = true;
            chol.robust = false;
            chol.Diagonal = Vector.Ones<float>(chol.n);
            return chol;
        }

        #region ICloneable Members

        private JaggedCholeskyDecompositionF()
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
            var clone = new JaggedCholeskyDecompositionF();
            clone.L = Matrix.MemberwiseClone(L);
            clone.Diagonal = (float[])Diagonal.Clone();
            clone.destroyed = destroyed;
            clone.n = n;
            clone.IsUndefined = IsUndefined;
            clone.robust = robust;
            clone.positiveDefinite = positiveDefinite;
            return clone;
        }

        #endregion
    }
}