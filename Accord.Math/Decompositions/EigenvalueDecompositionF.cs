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
    ///     Determines the eigenvalues and eigenvectors of a real square matrix.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In the mathematical discipline of linear algebra, eigendecomposition
    ///         or sometimes spectral decomposition is the factorization of a matrix
    ///         into a canonical form, whereby the matrix is represented in terms of
    ///         its eigenvalues and eigenvectors.
    ///     </para>
    ///     <para>
    ///         If <c>A</c> is symmetric, then <c>A = V * D * V'</c> and <c>A = V * V'</c>
    ///         where the eigenvalue matrix <c>D</c> is diagonal and the eigenvector matrix <c>V</c> is orthogonal.
    ///         If <c>A</c> is not symmetric, the eigenvalue matrix <c>D</c> is block diagonal
    ///         with the real eigenvalues in 1-by-1 blocks and any complex eigenvalues,
    ///         <c>lambda + i*mu</c>, in 2-by-2 blocks, <c>[lambda, mu; -mu, lambda]</c>.
    ///         The columns of <c>V</c> represent the eigenvectors in the sense that <c>A * V = V * D</c>.
    ///         The matrix V may be badly conditioned, or even singular, so the validity of the equation
    ///         <c>A = V * D * inverse(V)</c> depends upon the condition of <c>V</c>.
    ///     </para>
    /// </remarks>
    public sealed partial class EigenvalueDecompositionF : ICloneable
    {
        private const float eps = 2 * Constants.SingleEpsilon;
        private float[,] H; // storage of nonsymmetric Hessenberg form.
        private float[,] diagonalMatrix;
        private int n; // matrix dimension
        private float[] ort; // storage for nonsymmetric algorithm.

        private int? rank;
        private bool symmetric;

        /// <summary>
        ///     Construct an eigenvalue decomposition.
        /// </summary>
        /// <param name="value">
        ///     The matrix to be decomposed.
        /// </param>
        /// <param name="inPlace">
        ///     Pass <see langword="true" /> to perform the decomposition in place. The matrix
        ///     <paramref name="value" /> will be destroyed in the process, resulting in less
        ///     memory comsumption.
        /// </param>
        /// <param name="sort">
        ///     Pass <see langword="true" /> to sort the eigenvalues and eigenvectors at the end
        ///     of the decomposition.
        /// </param>
        public EigenvalueDecompositionF(float[,] value, bool inPlace = false, bool sort = false)
            : this(value, value.IsSymmetric(), inPlace, sort)
        {
        }

        /// <summary>
        ///     Construct an eigenvalue decomposition.
        /// </summary>
        /// <param name="value">
        ///     The matrix to be decomposed.
        /// </param>
        /// <param name="assumeSymmetric">
        ///     Defines if the matrix should be assumed as being symmetric
        ///     regardless if it is or not. Default is <see langword="false" />.
        /// </param>
        /// <param name="inPlace">
        ///     Pass <see langword="true" /> to perform the decomposition in place. The matrix
        ///     <paramref name="value" /> will be destroyed in the process, resulting in less
        ///     memory comsumption.
        /// </param>
        /// <param name="sort">
        ///     Pass <see langword="true" /> to sort the eigenvalues and eigenvectors at the end
        ///     of the decomposition.
        /// </param>
        public EigenvalueDecompositionF(float[,] value, bool assumeSymmetric,
            bool inPlace = false, bool sort = false)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "Matrix cannot be null.");
            }

            if (value.GetLength(0) != value.GetLength(1))
            {
                throw new ArgumentException("Matrix is not a square matrix.", "value");
            }

            n = value.GetLength(1);
            Eigenvectors = new float[n, n];
            RealEigenvalues = new float[n];
            ImaginaryEigenvalues = new float[n];


            symmetric = assumeSymmetric;

            if (symmetric)
            {
                Eigenvectors = inPlace ? value : (float[,])value.Clone();

                // Tridiagonalize.
                tred2();

                // Diagonalize.
                tql2();
            }
            else
            {
                H = inPlace ? value : (float[,])value.Clone();

                ort = new float[n];

                // Reduce to Hessenberg form.
                orthes();

                // Reduce Hessenberg to real Schur form.
                hqr2();
            }

            if (sort)
            {
                // Sort eigenvalues and vectors in descending order
                var idx = Vector.Range(n);
                Array.Sort(idx, (i, j) =>
                {
                    if (System.Math.Abs(RealEigenvalues[i]) == System.Math.Abs(RealEigenvalues[j]))
                    {
                        return -System.Math.Abs(ImaginaryEigenvalues[i])
                            .CompareTo(System.Math.Abs(ImaginaryEigenvalues[j]));
                    }

                    return -System.Math.Abs(RealEigenvalues[i]).CompareTo(System.Math.Abs(RealEigenvalues[j]));
                });

                RealEigenvalues = RealEigenvalues.Get(idx);
                ImaginaryEigenvalues = ImaginaryEigenvalues.Get(idx);
                Eigenvectors = Eigenvectors.Get(null, idx);
            }
        }

        /// <summary>
        ///     Returns the effective numerical matrix rank.
        /// </summary>
        /// <value>Number of non-negligible eigen values.</value>
        public int Rank
        {
            get
            {
                if (rank.HasValue)
                {
                    return rank.Value;
                }

                var tol = n * RealEigenvalues[0] * eps;

                var r = 0;
                for (var i = 0; i < RealEigenvalues.Length; i++)
                {
                    if (RealEigenvalues[i] > tol)
                    {
                        r++;
                    }
                }

                return (int)(rank = r);
            }
        }

        /// <summary>Returns the real parts of the eigenvalues.</summary>
        public float[] RealEigenvalues { get; private set; }

        /// <summary>Returns the imaginary parts of the eigenvalues.</summary>
        public float[] ImaginaryEigenvalues { get; private set; }

        /// <summary>Returns the eigenvector matrix.</summary>
        public float[,] Eigenvectors { get; private set; }

        /// <summary>Returns the block diagonal eigenvalue matrix.</summary>
        public float[,] DiagonalMatrix
        {
            get
            {
                if (diagonalMatrix != null)
                {
                    return diagonalMatrix;
                }

                var x = new float[n, n];

                for (var i = 0; i < n; i++)
                {
                    for (var j = 0; j < n; j++)
                    {
                        x[i, j] = 0;
                    }

                    x[i, i] = RealEigenvalues[i];
                    if (ImaginaryEigenvalues[i] > 0)
                    {
                        x[i, i + 1] = ImaginaryEigenvalues[i];
                    }
                    else if (ImaginaryEigenvalues[i] < 0)
                    {
                        x[i, i - 1] = ImaginaryEigenvalues[i];
                    }
                }

                return diagonalMatrix = x;
            }
        }

        /// <summary>
        ///     Reverses the decomposition, reconstructing the original matrix <c>X</c>.
        /// </summary>
        public float[,] Reverse()
        {
            return Eigenvectors.DotWithDiagonal(RealEigenvalues).Divide(Eigenvectors);
        }

        #region Private methods

        private void tred2()
        {
            // Symmetric Householder reduction to tridiagonal form.
            // This is derived from the Algol procedures tred2 by Bowdler, Martin, Reinsch, and Wilkinson, 
            // Handbook for Auto. Comp., Vol.ii-Linear Algebra, and the corresponding Fortran subroutine in EISPACK.
            for (var j = 0; j < n; j++)
            {
                RealEigenvalues[j] = Eigenvectors[n - 1, j];
            }

            // Householder reduction to tridiagonal form.
            for (var i = n - 1; i > 0; i--)
            {
                // Scale to avoid under/overflow.
                float scale = 0;
                float h = 0;
                for (var k = 0; k < i; k++)
                {
                    scale = scale + System.Math.Abs(RealEigenvalues[k]);
                }

                if (scale == 0)
                {
                    ImaginaryEigenvalues[i] = RealEigenvalues[i - 1];
                    for (var j = 0; j < i; j++)
                    {
                        RealEigenvalues[j] = Eigenvectors[i - 1, j];
                        Eigenvectors[i, j] = 0;
                        Eigenvectors[j, i] = 0;
                    }
                }
                else
                {
                    // Generate Householder vector.
                    for (var k = 0; k < i; k++)
                    {
                        RealEigenvalues[k] /= scale;
                        h += RealEigenvalues[k] * RealEigenvalues[k];
                    }

                    var f = RealEigenvalues[i - 1];
                    var g = (float)System.Math.Sqrt(h);
                    if (f > 0)
                    {
                        g = -g;
                    }

                    ImaginaryEigenvalues[i] = scale * g;
                    h = h - f * g;
                    RealEigenvalues[i - 1] = f - g;
                    for (var j = 0; j < i; j++)
                    {
                        ImaginaryEigenvalues[j] = 0;
                    }

                    // Apply similarity transformation to remaining columns.
                    for (var j = 0; j < i; j++)
                    {
                        f = RealEigenvalues[j];
                        Eigenvectors[j, i] = f;
                        g = ImaginaryEigenvalues[j] + Eigenvectors[j, j] * f;
                        for (var k = j + 1; k <= i - 1; k++)
                        {
                            g += Eigenvectors[k, j] * RealEigenvalues[k];
                            ImaginaryEigenvalues[k] += Eigenvectors[k, j] * f;
                        }

                        ImaginaryEigenvalues[j] = g;
                    }

                    f = 0;
                    for (var j = 0; j < i; j++)
                    {
                        ImaginaryEigenvalues[j] /= h;
                        f += ImaginaryEigenvalues[j] * RealEigenvalues[j];
                    }

                    var hh = f / (h + h);
                    for (var j = 0; j < i; j++)
                    {
                        ImaginaryEigenvalues[j] -= hh * RealEigenvalues[j];
                    }

                    for (var j = 0; j < i; j++)
                    {
                        f = RealEigenvalues[j];
                        g = ImaginaryEigenvalues[j];
                        for (var k = j; k <= i - 1; k++)
                        {
                            Eigenvectors[k, j] -= f * ImaginaryEigenvalues[k] + g * RealEigenvalues[k];
                        }

                        RealEigenvalues[j] = Eigenvectors[i - 1, j];
                        Eigenvectors[i, j] = 0;
                    }
                }

                RealEigenvalues[i] = h;
            }

            // Accumulate transformations.
            for (var i = 0; i < n - 1; i++)
            {
                Eigenvectors[n - 1, i] = Eigenvectors[i, i];
                Eigenvectors[i, i] = 1;
                var h = RealEigenvalues[i + 1];
                if (h != 0)
                {
                    for (var k = 0; k <= i; k++)
                    {
                        RealEigenvalues[k] = Eigenvectors[k, i + 1] / h;
                    }

                    for (var j = 0; j <= i; j++)
                    {
                        float g = 0;
                        for (var k = 0; k <= i; k++)
                        {
                            g += Eigenvectors[k, i + 1] * Eigenvectors[k, j];
                        }

                        for (var k = 0; k <= i; k++)
                        {
                            Eigenvectors[k, j] -= g * RealEigenvalues[k];
                        }
                    }
                }

                for (var k = 0; k <= i; k++)
                {
                    Eigenvectors[k, i + 1] = 0;
                }
            }

            for (var j = 0; j < n; j++)
            {
                RealEigenvalues[j] = Eigenvectors[n - 1, j];
                Eigenvectors[n - 1, j] = 0;
            }

            Eigenvectors[n - 1, n - 1] = 1;
            ImaginaryEigenvalues[0] = 0;
        }

        private void tql2()
        {
            // Symmetric tridiagonal QL algorithm.
            // This is derived from the Algol procedures tql2, by Bowdler, Martin, Reinsch, and Wilkinson, 
            // Handbook for Auto. Comp., Vol.ii-Linear Algebra, and the corresponding Fortran subroutine in EISPACK.
            for (var i = 1; i < n; i++)
            {
                ImaginaryEigenvalues[i - 1] = ImaginaryEigenvalues[i];
            }

            ImaginaryEigenvalues[n - 1] = 0;

            float f = 0;
            float tst1 = 0;
            var eps = 2 * Constants.SingleEpsilon;

            for (var l = 0; l < n; l++)
            {
                // Find small subdiagonal element.
                tst1 = System.Math.Max(tst1,
                    System.Math.Abs(RealEigenvalues[l]) + System.Math.Abs(ImaginaryEigenvalues[l]));
                var m = l;
                while (m < n)
                {
                    if (System.Math.Abs(ImaginaryEigenvalues[m]) <= eps * tst1)
                    {
                        break;
                    }

                    m++;
                }

                // If m == l, d[l] is an eigenvalue, otherwise, iterate.
                if (m > l)
                {
                    var iter = 0;
                    do
                    {
                        iter = iter + 1; // (Could check iteration count here.)

                        // Compute implicit shift
                        var g = RealEigenvalues[l];
                        var p = (RealEigenvalues[l + 1] - g) / (2 * ImaginaryEigenvalues[l]);
                        var r = Tools.Hypotenuse(p, 1);
                        if (p < 0)
                        {
                            r = -r;
                        }

                        RealEigenvalues[l] = ImaginaryEigenvalues[l] / (p + r);
                        RealEigenvalues[l + 1] = ImaginaryEigenvalues[l] * (p + r);
                        var dl1 = RealEigenvalues[l + 1];
                        var h = g - RealEigenvalues[l];
                        for (var i = l + 2; i < n; i++)
                        {
                            RealEigenvalues[i] -= h;
                        }

                        f = f + h;

                        // Implicit QL transformation.
                        p = RealEigenvalues[m];
                        float c = 1;
                        var c2 = c;
                        var c3 = c;
                        var el1 = ImaginaryEigenvalues[l + 1];
                        float s = 0;
                        float s2 = 0;
                        for (var i = m - 1; i >= l; i--)
                        {
                            c3 = c2;
                            c2 = c;
                            s2 = s;
                            g = c * ImaginaryEigenvalues[i];
                            h = c * p;
                            r = Tools.Hypotenuse(p, ImaginaryEigenvalues[i]);
                            ImaginaryEigenvalues[i + 1] = s * r;
                            s = ImaginaryEigenvalues[i] / r;
                            c = p / r;
                            p = c * RealEigenvalues[i] - s * g;
                            RealEigenvalues[i + 1] = h + s * (c * g + s * RealEigenvalues[i]);

                            // Accumulate transformation.
                            for (var k = 0; k < n; k++)
                            {
                                h = Eigenvectors[k, i + 1];
                                Eigenvectors[k, i + 1] = s * Eigenvectors[k, i] + c * h;
                                Eigenvectors[k, i] = c * Eigenvectors[k, i] - s * h;
                            }
                        }

                        p = -s * s2 * c3 * el1 * ImaginaryEigenvalues[l] / dl1;
                        ImaginaryEigenvalues[l] = s * p;
                        RealEigenvalues[l] = c * p;

                        // Check for convergence.
                    } while (System.Math.Abs(ImaginaryEigenvalues[l]) > eps * tst1);
                }

                RealEigenvalues[l] = RealEigenvalues[l] + f;
                ImaginaryEigenvalues[l] = 0;
            }

            // Sort eigenvalues and corresponding vectors.
            for (var i = 0; i < n - 1; i++)
            {
                var k = i;
                var p = RealEigenvalues[i];
                for (var j = i + 1; j < n; j++)
                {
                    if (RealEigenvalues[j] < p)
                    {
                        k = j;
                        p = RealEigenvalues[j];
                    }
                }

                if (k != i)
                {
                    RealEigenvalues[k] = RealEigenvalues[i];
                    RealEigenvalues[i] = p;
                    for (var j = 0; j < n; j++)
                    {
                        p = Eigenvectors[j, i];
                        Eigenvectors[j, i] = Eigenvectors[j, k];
                        Eigenvectors[j, k] = p;
                    }
                }
            }
        }

        private void orthes()
        {
            // Nonsymmetric reduction to Hessenberg form.
            // This is derived from the Algol procedures orthes and ortran, by Martin and Wilkinson, 
            // Handbook for Auto. Comp., Vol.ii-Linear Algebra, and the corresponding Fortran subroutines in EISPACK.
            var low = 0;
            var high = n - 1;

            for (var m = low + 1; m <= high - 1; m++)
            {
                // Scale column.

                float scale = 0;
                for (var i = m; i <= high; i++)
                {
                    scale = scale + System.Math.Abs(H[i, m - 1]);
                }

                if (scale != 0)
                {
                    // Compute Householder transformation.
                    float h = 0;
                    for (var i = high; i >= m; i--)
                    {
                        ort[i] = H[i, m - 1] / scale;
                        h += ort[i] * ort[i];
                    }

                    var g = (float)System.Math.Sqrt(h);
                    if (ort[m] > 0)
                    {
                        g = -g;
                    }

                    h = h - ort[m] * g;
                    ort[m] = ort[m] - g;

                    // Apply Householder similarity transformation
                    // H = (I - u * u' / h) * H * (I - u * u') / h)
                    for (var j = m; j < n; j++)
                    {
                        float f = 0;
                        for (var i = high; i >= m; i--)
                        {
                            f += ort[i] * H[i, j];
                        }

                        f = f / h;
                        for (var i = m; i <= high; i++)
                        {
                            H[i, j] -= f * ort[i];
                        }
                    }

                    for (var i = 0; i <= high; i++)
                    {
                        float f = 0;
                        for (var j = high; j >= m; j--)
                        {
                            f += ort[j] * H[i, j];
                        }

                        f = f / h;
                        for (var j = m; j <= high; j++)
                        {
                            H[i, j] -= f * ort[j];
                        }
                    }

                    ort[m] = scale * ort[m];
                    H[m, m - 1] = scale * g;
                }
            }

            // Accumulate transformations (Algol's ortran).
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    Eigenvectors[i, j] = i == j ? 1 : 0;
                }
            }

            for (var m = high - 1; m >= low + 1; m--)
            {
                if (H[m, m - 1] != 0)
                {
                    for (var i = m + 1; i <= high; i++)
                    {
                        ort[i] = H[i, m - 1];
                    }

                    for (var j = m; j <= high; j++)
                    {
                        float g = 0;
                        for (var i = m; i <= high; i++)
                        {
                            g += ort[i] * Eigenvectors[i, j];
                        }

                        // Double division avoids possible underflow.
                        g = g / ort[m] / H[m, m - 1];
                        for (var i = m; i <= high; i++)
                        {
                            Eigenvectors[i, j] += g * ort[i];
                        }
                    }
                }
            }
        }

        private static void cdiv(float xr, float xi, float yr, float yi,
            out float cdivr, out float cdivi)
        {
            // Complex scalar division.
            float r;
            float d;
            if (System.Math.Abs(yr) > System.Math.Abs(yi))
            {
                r = yi / yr;
                d = yr + r * yi;
                cdivr = (xr + r * xi) / d;
                cdivi = (xi - r * xr) / d;
            }
            else
            {
                r = yr / yi;
                d = yi + r * yr;
                cdivr = (r * xr + xi) / d;
                cdivi = (r * xi - xr) / d;
            }
        }

        private void hqr2()
        {
            new SchurReduction(this).Run();
        }

        #endregion

        #region ICloneable Members

        private EigenvalueDecompositionF()
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
            var clone = new EigenvalueDecompositionF();
            clone.RealEigenvalues = (float[])RealEigenvalues.Clone();
            clone.ImaginaryEigenvalues = (float[])ImaginaryEigenvalues.Clone();
            clone.H = (float[,])H.Clone();
            clone.n = n;
            clone.ort = ort;
            clone.symmetric = symmetric;
            clone.Eigenvectors = (float[,])Eigenvectors.Clone();
            return clone;
        }

        #endregion
    }
}