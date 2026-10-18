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
using System.Diagnostics;

namespace Accord.Math.Decompositions
{
    /// <summary>
    ///     Singular Value Decomposition for a rectangular matrix.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For an m-by-n matrix <c>A</c> with <c>m >= n</c>, the singular value decomposition
    ///         is an m-by-n orthogonal matrix <c>U</c>, an n-by-n diagonal matrix <c>S</c>, and
    ///         an n-by-n orthogonal matrix <c>V</c> so that <c>A = U * S * V'</c>.
    ///         The singular values, <c>sigma[k] = S[k,k]</c>, are ordered so that
    ///         <c>sigma[0] >= sigma[1] >= ... >= sigma[n-1]</c>.
    ///     </para>
    ///     <para>
    ///         The singular value decomposition always exists, so the constructor will
    ///         never fail. The matrix condition number and the effective numerical
    ///         rank can be computed from this decomposition.
    ///     </para>
    ///     <para>
    ///         WARNING! Please be aware that if A has less rows than columns, it is better
    ///         to compute the decomposition on the transpose of A and then swap the left
    ///         and right eigenvectors. If the routine is computed on A directly, the diagonal
    ///         of singular values may contain one or more zeros. The identity A = U * S * V'
    ///         may still hold, however. To overcome this problem, pass true to the
    ///         <see cref="SingularValueDecompositionF(float[,], bool, bool, bool)">autoTranspose</see>
    ///         argument of the class constructor.
    ///     </para>
    ///     <para>
    ///         This routine computes the economy decomposition of A.
    ///     </para>
    /// </remarks>
    [Serializable]
    public sealed class SingularValueDecompositionF : ICloneable, ISolverMatrixDecomposition<float>
    {
        private const float eps = 2 * Constants.SingleEpsilon;
        private const float tiny = Constants.SingleSmall;
        private float? determinant;

        private float[,] diagonalMatrix;
        private float? lndeterminant;
        private float? lnpseudoDeterminant;
        private int m;
        private int n;
        private float? pseudoDeterminant;

        private int? rank;

        private bool swapped;

        /// <summary>
        ///     Constructs a new singular value decomposition.
        /// </summary>
        /// <param name="value">
        ///     The matrix to be decomposed.
        /// </param>
        public SingularValueDecompositionF(float[,] value)
            : this(value, true, true)
        {
        }

        /// <summary>
        ///     Constructs a new singular value decomposition.
        /// </summary>
        /// <param name="value">
        ///     The matrix to be decomposed.
        /// </param>
        /// <param name="computeLeftSingularVectors">
        ///     Pass <see langword="true" /> if the left singular vector matrix U
        ///     should be computed. Pass <see langword="false" /> otherwise. Default
        ///     is <see langword="true" />.
        /// </param>
        /// <param name="computeRightSingularVectors">
        ///     Pass <see langword="true" /> if the right singular vector matrix V
        ///     should be computed. Pass <see langword="false" /> otherwise. Default
        ///     is <see langword="true" />.
        /// </param>
        public SingularValueDecompositionF(float[,] value,
            bool computeLeftSingularVectors, bool computeRightSingularVectors)
            : this(value, computeLeftSingularVectors, computeRightSingularVectors, false)
        {
        }

        /// <summary>
        ///     Constructs a new singular value decomposition.
        /// </summary>
        /// <param name="value">
        ///     The matrix to be decomposed.
        /// </param>
        /// <param name="computeLeftSingularVectors">
        ///     Pass <see langword="true" /> if the left singular vector matrix U
        ///     should be computed. Pass <see langword="false" /> otherwise. Default
        ///     is <see langword="true" />.
        /// </param>
        /// <param name="computeRightSingularVectors">
        ///     Pass <see langword="true" /> if the right singular vector matrix V
        ///     should be computed. Pass <see langword="false" /> otherwise. Default
        ///     is <see langword="true" />.
        /// </param>
        /// <param name="autoTranspose">
        ///     Pass <see langword="true" /> to automatically transpose the value matrix in
        ///     case JAMA's assumptions about the dimensionality of the matrix are violated.
        ///     Pass <see langword="false" /> otherwise. Default is <see langword="false" />.
        /// </param>
        public SingularValueDecompositionF(float[,] value,
            bool computeLeftSingularVectors, bool computeRightSingularVectors, bool autoTranspose)
            : this(value, computeLeftSingularVectors, computeRightSingularVectors, autoTranspose, false)
        {
        }

        /// <summary>
        ///     Constructs a new singular value decomposition.
        /// </summary>
        /// <param name="value">
        ///     The matrix to be decomposed.
        /// </param>
        /// <param name="computeLeftSingularVectors">
        ///     Pass <see langword="true" /> if the left singular vector matrix U
        ///     should be computed. Pass <see langword="false" /> otherwise. Default
        ///     is <see langword="true" />.
        /// </param>
        /// <param name="computeRightSingularVectors">
        ///     Pass <see langword="true" /> if the right singular vector matrix V
        ///     should be computed. Pass <see langword="false" /> otherwise. Default
        ///     is <see langword="true" />.
        /// </param>
        /// <param name="autoTranspose">
        ///     Pass <see langword="true" /> to automatically transpose the value matrix in
        ///     case JAMA's assumptions about the dimensionality of the matrix are violated.
        ///     Pass <see langword="false" /> otherwise. Default is <see langword="false" />.
        /// </param>
        /// <param name="inPlace">
        ///     Pass <see langword="true" /> to perform the decomposition in place. The matrix
        ///     <paramref name="value" /> will be destroyed in the process, resulting in less
        ///     memory comsumption.
        /// </param>
        public SingularValueDecompositionF(float[,] value,
            bool computeLeftSingularVectors, bool computeRightSingularVectors, bool autoTranspose, bool inPlace)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "Matrix cannot be null.");
            }

            float[,] a;
            m = value.Rows(); // rows

            if (m == 0)
            {
                throw new ArgumentException("Matrix does not have any rows.", "value");
            }

            n = value.Columns(); // cols

            if (n == 0)
            {
                throw new ArgumentException("Matrix does not have any columns.", "value");
            }

            if (m < n) // Check if we are violating JAMA's assumption
            {
                if (!autoTranspose) // Yes, check if we should correct it
                {
                    // Warning! This routine is not guaranteed to work when A has less rows
                    //  than columns. If this is the case, you should compute SVD on the
                    //  transpose of A and then swap the left and right eigenvectors.

                    // However, as the solution found can still be useful, the exception below
                    // will not be thrown, and only a warning will be output in the trace.

                    // throw new ArgumentException("Matrix should have more rows than columns.");

                    Trace.WriteLine("WARNING: Computing SVD on a matrix with more columns than rows.");

                    // Proceed anyway
                    a = inPlace ? value : value.Copy();
                }
                else
                {
                    // Transposing and swapping
                    a = value.Transpose(inPlace && m == n);
                    n = value.Rows(); // rows
                    m = value.Columns(); // cols
                    swapped = true;

                    var aux = computeLeftSingularVectors;
                    computeLeftSingularVectors = computeRightSingularVectors;
                    computeRightSingularVectors = aux;
                }
            }
            else
            {
                // Input matrix is ok
                a = inPlace ? value : value.Copy();
            }


            var nu = System.Math.Min(m, n);
            var ni = System.Math.Min(m + 1, n);
            Diagonal = new float[ni];
            LeftSingularVectors = new float[m, nu];
            RightSingularVectors = new float[n, n];

            var e = new float[n];
            var work = new float[m];
            var wantu = computeLeftSingularVectors;
            var wantv = computeRightSingularVectors;

            // Will store ordered sequence of indices after sorting.
            Ordering = new int[ni];
            for (var i = 0; i < ni; i++)
            {
                Ordering[i] = i;
            }


            // Reduce A to bidiagonal form, storing the diagonal elements in s and the super-diagonal elements in e.
            var nct = System.Math.Min(m - 1, n);
            var nrt = System.Math.Max(0, System.Math.Min(n - 2, m));
            var mrc = System.Math.Max(nct, nrt);

            for (var k = 0; k < mrc; k++)
            {
                if (k < nct)
                {
                    // Compute the transformation for the k-th column and place the k-th diagonal in s[k].
                    // Compute 2-norm of k-th column without under/overflow.
                    Diagonal[k] = 0;
                    for (var i = k; i < a.Rows(); i++)
                    {
                        Diagonal[k] = Tools.Hypotenuse(Diagonal[k], a[i, k]);
                    }

                    if (Diagonal[k] != 0)
                    {
                        if (a[k, k] < 0)
                        {
                            Diagonal[k] = -Diagonal[k];
                        }

                        for (var i = k; i < a.Rows(); i++)
                        {
                            a[i, k] /= Diagonal[k];
                        }

                        a[k, k] += 1;
                    }

                    Diagonal[k] = -Diagonal[k];
                }

                for (var j = k + 1; j < n; j++)
                {
                    if ((k < nct) && (Diagonal[k] != 0))
                    {
                        // Apply the transformation.
                        float t = 0;
                        for (var i = k; i < a.Rows(); i++)
                        {
                            t += a[i, k] * a[i, j];
                        }

                        t = -t / a[k, k];

                        for (var i = k; i < a.Rows(); i++)
                        {
                            a[i, j] += t * a[i, k];
                        }
                    }

                    // Place the k-th row of A into e for the
                    // subsequent calculation of the row transformation.

                    e[j] = a[k, j];
                }

                if (wantu & (k < nct))
                {
                    // Place the transformation in U for subsequent back
                    // multiplication.

                    for (var i = k; i < a.Rows(); i++)
                    {
                        LeftSingularVectors[i, k] = a[i, k];
                    }
                }

                if (k < nrt)
                {
                    // Compute the k-th row transformation and place the
                    // k-th super-diagonal in e[k].
                    // Compute 2-norm without under/overflow.
                    e[k] = 0;
                    for (var i = k + 1; i < e.Rows(); i++)
                    {
                        e[k] = Tools.Hypotenuse(e[k], e[i]);
                    }

                    if (e[k] != 0)
                    {
                        if (e[k + 1] < 0)
                        {
                            e[k] = -e[k];
                        }

                        for (var i = k + 1; i < e.Rows(); i++)
                        {
                            e[i] /= e[k];
                        }

                        e[k + 1] += 1;
                    }

                    e[k] = -e[k];
                    if ((k + 1 < m) && (e[k] != 0))
                    {
                        // Apply the transformation.
                        for (var i = k + 1; i < work.Rows(); i++)
                        {
                            work[i] = 0;
                        }

                        for (var i = k + 1; i < a.Rows(); i++)
                        {
                            for (var j = k + 1; j < a.Columns(); j++)
                            {
                                work[i] += e[j] * a[i, j];
                            }
                        }

                        for (var j = k + 1; j < n; j++)
                        {
                            var t = -e[j] / e[k + 1];
                            for (var i = k + 1; i < work.Rows(); i++)
                            {
                                a[i, j] += t * work[i];
                            }
                        }
                    }

                    if (wantv)
                    {
                        // Place the transformation in V for subsequent
                        // back multiplication.

                        for (var i = k + 1; i < RightSingularVectors.Rows(); i++)
                        {
                            RightSingularVectors[i, k] = e[i];
                        }
                    }
                }
            }

            // Set up the final bidiagonal matrix or order p.
            var p = System.Math.Min(n, m + 1);
            if (nct < n)
            {
                Diagonal[nct] = a[nct, nct];
            }

            if (m < p)
            {
                Diagonal[p - 1] = 0;
            }

            if (nrt + 1 < p)
            {
                e[nrt] = a[nrt, p - 1];
            }

            e[p - 1] = 0;

            // If required, generate U.
            if (wantu)
            {
                for (var j = nct; j < nu; j++)
                {
                    for (var i = 0; i < LeftSingularVectors.Rows(); i++)
                    {
                        LeftSingularVectors[i, j] = 0;
                    }

                    LeftSingularVectors[j, j] = 1;
                }

                for (var k = nct - 1; k >= 0; k--)
                {
                    if (Diagonal[k] != 0)
                    {
                        for (var j = k + 1; j < nu; j++)
                        {
                            float t = 0;
                            for (var i = k; i < LeftSingularVectors.Rows(); i++)
                            {
                                t += LeftSingularVectors[i, k] * LeftSingularVectors[i, j];
                            }

                            t = -t / LeftSingularVectors[k, k];

                            for (var i = k; i < LeftSingularVectors.Rows(); i++)
                            {
                                LeftSingularVectors[i, j] += t * LeftSingularVectors[i, k];
                            }
                        }

                        for (var i = k; i < LeftSingularVectors.Rows(); i++)
                        {
                            LeftSingularVectors[i, k] = -LeftSingularVectors[i, k];
                        }

                        LeftSingularVectors[k, k] = 1 + LeftSingularVectors[k, k];
                        for (var i = 0; i < k - 1; i++)
                        {
                            LeftSingularVectors[i, k] = 0;
                        }
                    }
                    else
                    {
                        for (var i = 0; i < LeftSingularVectors.Rows(); i++)
                        {
                            LeftSingularVectors[i, k] = 0;
                        }

                        LeftSingularVectors[k, k] = 1;
                    }
                }
            }


            // If required, generate V.
            if (wantv)
            {
                for (var k = n - 1; k >= 0; k--)
                {
                    if ((k < nrt) && (e[k] != 0))
                    {
                        // TODO: The following is a pseudo correction to make SVD
                        //  work on matrices with n > m (less rows than columns).

                        // For the proper correction, compute the decomposition of the
                        //  transpose of A and swap the left and right eigenvectors

                        // Original line:
                        //   for (int j = k + 1; j < nu; j++)
                        // Pseudo correction:
                        //   for (int j = k + 1; j < n; j++)

                        for (var j = k + 1; j < n; j++) // pseudo-correction
                        {
                            float t = 0;
                            for (var i = k + 1; i < RightSingularVectors.Rows(); i++)
                            {
                                t += RightSingularVectors[i, k] * RightSingularVectors[i, j];
                            }

                            t = -t / RightSingularVectors[k + 1, k];
                            for (var i = k + 1; i < RightSingularVectors.Rows(); i++)
                            {
                                RightSingularVectors[i, j] += t * RightSingularVectors[i, k];
                            }
                        }
                    }

                    for (var i = 0; i < RightSingularVectors.Rows(); i++)
                    {
                        RightSingularVectors[i, k] = 0;
                    }

                    RightSingularVectors[k, k] = 1;
                }
            }

            // Main iteration loop for the singular values.

            var pp = p - 1;
            var iter = 0;
            var eps = Constants.SingleEpsilon;
            while (p > 0)
            {
                int k, kase;

                // Here is where a test for too many iterations would go.

                // This section of the program inspects for
                // negligible elements in the s and e arrays.  On
                // completion the variables kase and k are set as follows.

                // kase = 1     if s(p) and e[k-1] are negligible and k<p
                // kase = 2     if s(k) is negligible and k<p
                // kase = 3     if e[k-1] is negligible, k<p, and
                //              s(k), ..., s(p) are not negligible (qr step).
                // kase = 4     if e(p-1) is negligible (convergence).

                for (k = p - 2; k >= -1; k--)
                {
                    if (k == -1)
                    {
                        break;
                    }

                    var alpha = tiny + eps * (System.Math.Abs(Diagonal[k]) + System.Math.Abs(Diagonal[k + 1]));
                    if (System.Math.Abs(e[k]) <= alpha || float.IsNaN(e[k]))
                    {
                        e[k] = 0;
                        break;
                    }
                }

                if (k == p - 2)
                {
                    kase = 4;
                }

                else
                {
                    int ks;
                    for (ks = p - 1; ks >= k; ks--)
                    {
                        if (ks == k)
                        {
                            break;
                        }

                        var t = (ks != p ? System.Math.Abs(e[ks]) : 0) +
                                (ks != k + 1 ? System.Math.Abs(e[ks - 1]) : 0);

                        if (System.Math.Abs(Diagonal[ks]) <= eps * t)
                        {
                            Diagonal[ks] = 0;
                            break;
                        }
                    }

                    if (ks == k)
                    {
                        kase = 3;
                    }

                    else if (ks == p - 1)
                    {
                        kase = 1;
                    }

                    else
                    {
                        kase = 2;
                        k = ks;
                    }
                }

                k++;

                // Perform the task indicated by kase.
                switch (kase)
                {
                    // Deflate negligible s(p).
                    case 1:
                    {
                        var f = e[p - 2];
                        e[p - 2] = 0;
                        for (var j = p - 2; j >= k; j--)
                        {
                            var t = Tools.Hypotenuse(Diagonal[j], f);
                            var cs = Diagonal[j] / t;
                            var sn = f / t;
                            Diagonal[j] = t;
                            if (j != k)
                            {
                                f = -sn * e[j - 1];
                                e[j - 1] = cs * e[j - 1];
                            }

                            if (wantv)
                            {
                                for (var i = 0; i < RightSingularVectors.Rows(); i++)
                                {
                                    t = cs * RightSingularVectors[i, j] + sn * RightSingularVectors[i, p - 1];
                                    RightSingularVectors[i, p - 1] = -sn * RightSingularVectors[i, j] +
                                                                     cs * RightSingularVectors[i, p - 1];
                                    RightSingularVectors[i, j] = t;
                                }
                            }
                        }
                    }
                        break;

                    // Split at negligible s(k).

                    case 2:
                    {
                        var f = e[k - 1];
                        e[k - 1] = 0;
                        for (var j = k; j < p; j++)
                        {
                            var t = Tools.Hypotenuse(Diagonal[j], f);
                            var cs = Diagonal[j] / t;
                            var sn = f / t;
                            Diagonal[j] = t;
                            f = -sn * e[j];
                            e[j] = cs * e[j];
                            if (wantu)
                            {
                                for (var i = 0; i < LeftSingularVectors.Rows(); i++)
                                {
                                    t = cs * LeftSingularVectors[i, j] + sn * LeftSingularVectors[i, k - 1];
                                    LeftSingularVectors[i, k - 1] = -sn * LeftSingularVectors[i, j] +
                                                                    cs * LeftSingularVectors[i, k - 1];
                                    LeftSingularVectors[i, j] = t;
                                }
                            }
                        }
                    }
                        break;

                    // Perform one qr step.
                    case 3:
                    {
                        // Calculate the shift.
                        var scale = System.Math.Max(System.Math.Max(System.Math.Max(System.Math.Max(
                                    System.Math.Abs(Diagonal[p - 1]), System.Math.Abs(Diagonal[p - 2])),
                                System.Math.Abs(e[p - 2])),
                            System.Math.Abs(Diagonal[k])), System.Math.Abs(e[k]));
                        var sp = Diagonal[p - 1] / scale;
                        var spm1 = Diagonal[p - 2] / scale;
                        var epm1 = e[p - 2] / scale;
                        var sk = Diagonal[k] / scale;
                        var ek = e[k] / scale;
                        var b = ((spm1 + sp) * (spm1 - sp) + epm1 * epm1) / 2;
                        var c = sp * epm1 * (sp * epm1);
                        float shift = 0;
                        if (b != 0 || c != 0)
                        {
                            if (b < 0)
                            {
                                shift = -(float)System.Math.Sqrt(b * b + c);
                            }
                            else
                            {
                                shift = (float)System.Math.Sqrt(b * b + c);
                            }

                            shift = c / (b + shift);
                        }

                        var f = (sk + sp) * (sk - sp) + shift;
                        var g = sk * ek;

                        // Chase zeros.
                        for (var j = k; j < p - 1; j++)
                        {
                            var t = Tools.Hypotenuse(f, g);
                            var cs = f / t;
                            var sn = g / t;

                            if (j != k)
                            {
                                e[j - 1] = t;
                            }

                            f = cs * Diagonal[j] + sn * e[j];
                            e[j] = cs * e[j] - sn * Diagonal[j];
                            g = sn * Diagonal[j + 1];
                            Diagonal[j + 1] = cs * Diagonal[j + 1];

                            if (wantv)
                            {
                                for (var i = 0; i < RightSingularVectors.Rows(); i++)
                                {
                                    t = cs * RightSingularVectors[i, j] + sn * RightSingularVectors[i, j + 1];
                                    RightSingularVectors[i, j + 1] = -sn * RightSingularVectors[i, j] +
                                                                     cs * RightSingularVectors[i, j + 1];
                                    RightSingularVectors[i, j] = t;
                                }
                            }

                            t = Tools.Hypotenuse(f, g);
                            cs = f / t;
                            sn = g / t;
                            Diagonal[j] = t;
                            f = cs * e[j] + sn * Diagonal[j + 1];
                            Diagonal[j + 1] = -sn * e[j] + cs * Diagonal[j + 1];
                            g = sn * e[j + 1];
                            e[j + 1] = cs * e[j + 1];

                            if (wantu && j < m - 1)
                            {
                                for (var i = 0; i < LeftSingularVectors.Rows(); i++)
                                {
                                    t = cs * LeftSingularVectors[i, j] + sn * LeftSingularVectors[i, j + 1];
                                    LeftSingularVectors[i, j + 1] = -sn * LeftSingularVectors[i, j] +
                                                                    cs * LeftSingularVectors[i, j + 1];
                                    LeftSingularVectors[i, j] = t;
                                }
                            }
                        }

                        e[p - 2] = f;
                        iter = iter + 1;
                    }
                        break;

                    // Convergence.
                    case 4:
                    {
                        // Make the singular values positive.
                        if (Diagonal[k] <= 0)
                        {
                            Diagonal[k] = Diagonal[k] < 0 ? -Diagonal[k] : 0;

                            if (wantv)
                            {
                                for (var i = 0; i <= pp; i++)
                                {
                                    RightSingularVectors[i, k] = -RightSingularVectors[i, k];
                                }
                            }
                        }

                        // Order the singular values.
                        while (k < pp)
                        {
                            if (Diagonal[k] >= Diagonal[k + 1])
                            {
                                break;
                            }

                            var t = Diagonal[k];
                            Diagonal[k] = Diagonal[k + 1];
                            Diagonal[k + 1] = t;
                            if (wantv && k < n - 1)
                            {
                                for (var i = 0; i < n; i++)
                                {
                                    t = RightSingularVectors[i, k + 1];
                                    RightSingularVectors[i, k + 1] = RightSingularVectors[i, k];
                                    RightSingularVectors[i, k] = t;
                                }
                            }

                            if (wantu && k < m - 1)
                            {
                                for (var i = 0; i < LeftSingularVectors.Rows(); i++)
                                {
                                    t = LeftSingularVectors[i, k + 1];
                                    LeftSingularVectors[i, k + 1] = LeftSingularVectors[i, k];
                                    LeftSingularVectors[i, k] = t;
                                }
                            }

                            k++;
                        }

                        iter = 0;
                        p--;
                    }
                        break;
                }
            }


            // If we are violating JAMA's assumption about 
            // the input dimension, we need to swap u and v.
            if (swapped)
            {
                var temp = LeftSingularVectors;
                LeftSingularVectors = RightSingularVectors;
                RightSingularVectors = temp;
            }
        }

        /// <summary>
        ///     Returns the condition number <c>max(S) / min(S)</c>.
        /// </summary>
        public float Condition => Diagonal[0] / Diagonal[System.Math.Min(m, n) - 1];

        /// <summary>
        ///     Returns the singularity threshold.
        /// </summary>
        public float Threshold => Constants.SingleEpsilon * System.Math.Max(m, n) * Diagonal[0];

        /// <summary>
        ///     Returns the Two norm.
        /// </summary>
        public float TwoNorm => Diagonal[0];

        /// <summary>
        ///     Returns the effective numerical matrix rank.
        /// </summary>
        /// <value>Number of non-negligible singular values.</value>
        public int Rank
        {
            get
            {
                if (rank.HasValue)
                {
                    return rank.Value;
                }

                var tol = System.Math.Max(m, n) * Diagonal[0] * eps;

                var r = 0;
                for (var i = 0; i < Diagonal.Rows(); i++)
                {
                    if (Diagonal[i] > tol)
                    {
                        r++;
                    }
                }

                return (int)(rank = r);
            }
        }

        /// <summary>
        ///     Gets whether the decomposed matrix is singular.
        /// </summary>
        public bool IsSingular => Rank < System.Math.Max(m, n);

        /// <summary>
        ///     Gets the one-dimensional array of singular values.
        /// </summary>
        public float[] Diagonal { get; private set; }

        /// <summary>
        ///     Returns the block diagonal matrix of singular values.
        /// </summary>
        public float[,] DiagonalMatrix
        {
            get
            {
                if (diagonalMatrix != null)
                {
                    return diagonalMatrix;
                }

                return diagonalMatrix = Matrix.Diagonal(LeftSingularVectors.Columns(), RightSingularVectors.Columns(),
                    Diagonal);
            }
        }

        /// <summary>
        ///     Returns the V matrix of Singular Vectors.
        /// </summary>
        public float[,] RightSingularVectors { get; private set; }

        /// <summary>
        ///     Returns the U matrix of Singular Vectors.
        /// </summary>
        public float[,] LeftSingularVectors { get; private set; }

        /// <summary>
        ///     Returns the ordering in which the singular values have been sorted.
        /// </summary>
        public int[] Ordering { get; private set; }

        /// <summary>
        ///     Returns the absolute value of the matrix determinant.
        /// </summary>
        public float AbsoluteDeterminant
        {
            get
            {
                if (!determinant.HasValue)
                {
                    float det = 1;
                    for (var i = 0; i < Diagonal.Rows(); i++)
                    {
                        det *= Diagonal[i];
                    }

                    determinant = det;
                }

                return determinant.Value;
            }
        }

        /// <summary>
        ///     Returns the log of the absolute value for the matrix determinant.
        /// </summary>
        public float LogDeterminant
        {
            get
            {
                if (!lndeterminant.HasValue)
                {
                    double det = 0;
                    for (var i = 0; i < Diagonal.Rows(); i++)
                    {
                        det += System.Math.Log(Diagonal[i]);
                    }

                    lndeterminant = (float)det;
                }

                return lndeterminant.Value;
            }
        }

        /// <summary>
        ///     Returns the pseudo-determinant for the matrix.
        /// </summary>
        public float PseudoDeterminant
        {
            get
            {
                if (!pseudoDeterminant.HasValue)
                {
                    float det = 1;
                    for (var i = 0; i < Diagonal.Rows(); i++)
                    {
                        if (Diagonal[i] != 0)
                        {
                            det *= Diagonal[i];
                        }
                    }

                    pseudoDeterminant = det;
                }

                return pseudoDeterminant.Value;
            }
        }

        /// <summary>
        ///     Returns the log of the pseudo-determinant for the matrix.
        /// </summary>
        public float LogPseudoDeterminant
        {
            get
            {
                if (!lnpseudoDeterminant.HasValue)
                {
                    double det = 0;
                    for (var i = 0; i < Diagonal.Rows(); i++)
                    {
                        if (Diagonal[i] != 0)
                        {
                            det += System.Math.Log(Diagonal[i]);
                        }
                    }

                    lnpseudoDeterminant = (float)det;
                }

                return lnpseudoDeterminant.Value;
            }
        }

        /// <summary>
        ///     Solves a linear equation system of the form AX = B.
        /// </summary>
        /// <param name="value">Parameter B from the equation AX = B.</param>
        /// <returns>The solution X from equation AX = B.</returns>
        public float[,] Solve(float[,] value)
        {
            // Additionally an important property is that if there does not exists a solution
            // when the matrix A is singular but replacing 1/Li with 0 will provide a solution
            // that minimizes the residue |AX -Y|. SVD finds the least squares best compromise
            // solution of the linear equation system. Interestingly SVD can be also used in an
            // over-determined system where the number of equations exceeds that of the parameters.

            // L is a diagonal matrix with non-negative matrix elements having the same
            // dimension as A, Wi ? 0. The diagonal elements of L are the singular values of matrix A.

            var Y = value;

            // Create L*, which is a diagonal matrix with elements
            //    L*[i] = 1/L[i]  if L[i] < e, else 0, 
            // where e is the so-called singularity threshold.

            // In other words, if L[i] is zero or close to zero (smaller than e),
            // one must replace 1/L[i] with 0. The value of e depends on the precision
            // of the hardware. This method can be used to solve linear equations
            // systems even if the matrices are singular or close to singular.

            //singularity threshold
            var e = Threshold;


            var scols = Diagonal.Rows();
            var Ls = new float[scols, scols];
            for (var i = 0; i < Diagonal.Rows(); i++)
            {
                if (System.Math.Abs(Diagonal[i]) <= e)
                {
                    Ls[i, i] = 0;
                }
                else
                {
                    Ls[i, i] = 1 / Diagonal[i];
                }
            }

            //(V x L*) x Ut x Y
            var VL = RightSingularVectors.Dot(Ls);

            //(V x L* x Ut) x Y
            var vrows = RightSingularVectors.Rows();
            var urows = LeftSingularVectors.Rows();
            var ucols = LeftSingularVectors.Columns();
            var VLU = new float[vrows, urows];
            for (var i = 0; i < vrows; i++)
            {
                for (var j = 0; j < urows; j++)
                {
                    float sum = 0;
                    for (var k = 0; k < ucols; k++)
                    {
                        sum += VL[i, k] * LeftSingularVectors[j, k];
                    }

                    VLU[i, j] = sum;
                }
            }

            //(V x L* x Ut x Y)
            return VLU.Dot(Y);
        }

        /// <summary>
        ///     Solves a linear equation system of the form Ax = b.
        /// </summary>
        /// <param name="value">The b from the equation Ax = b.</param>
        /// <returns>The x from equation Ax = b.</returns>
        public float[] Solve(float[] value)
        {
            // Additionally an important property is that if there does not exists a solution
            // when the matrix A is singular but replacing 1/Li with 0 will provide a solution
            // that minimizes the residue |AX -Y|. SVD finds the least squares best compromise
            // solution of the linear equation system. Interestingly SVD can be also used in an
            // over-determined system where the number of equations exceeds that of the parameters.

            // L is a diagonal matrix with non-negative matrix elements having the same
            // dimension as A, Wi ? 0. The diagonal elements of L are the singular values of matrix A.

            //singularity threshold
            var e = Threshold;

            var Y = value;

            // Create L*, which is a diagonal matrix with elements
            //    L*i = 1/Li  if Li = e, else 0, 
            // where e is the so-called singularity threshold.

            // In other words, if Li is zero or close to zero (smaller than e),
            // one must replace 1/Li with 0. The value of e depends on the precision
            // of the hardware. This method can be used to solve linear equations
            // systems even if the matrices are singular or close to singular.


            var scols = Diagonal.Rows();

            var Ls = new float[scols, scols];
            for (var i = 0; i < Diagonal.Rows(); i++)
            {
                if (System.Math.Abs(Diagonal[i]) <= e)
                {
                    Ls[i, i] = 0;
                }
                else
                {
                    Ls[i, i] = 1 / Diagonal[i];
                }
            }

            //(V x L*) x Ut x Y
            var VL = RightSingularVectors.Dot(Ls);

            //(V x L* x Ut) x Y
            var urows = LeftSingularVectors.Rows();
            var vrows = RightSingularVectors.Rows();
            var VLU = new float[vrows, urows];
            for (var i = 0; i < vrows; i++)
            {
                for (var j = 0; j < urows; j++)
                {
                    float sum = 0;
                    for (var k = 0; k < scols; k++)
                    {
                        sum += VL[i, k] * LeftSingularVectors[j, k];
                    }

                    VLU[i, j] = sum;
                }
            }

            //(V x L* x Ut x Y)
            return VLU.Dot(Y);
        }

        /// <summary>
        ///     Computes the (pseudo-)inverse of the matrix given to the Singular value decomposition.
        /// </summary>
        public float[,] Inverse()
        {
            var e = Threshold;

            // X = V*S^-1
            var vrows = RightSingularVectors.Rows();
            var vcols = RightSingularVectors.Columns();
            var X = new float[vrows, Diagonal.Rows()];
            for (var i = 0; i < vrows; i++)
            {
                for (var j = 0; j < vcols; j++)
                {
                    if (System.Math.Abs(Diagonal[j]) > e)
                    {
                        X[i, j] = RightSingularVectors[i, j] / Diagonal[j];
                    }
                }
            }

            // Y = X*U'
            var urows = LeftSingularVectors.Rows();
            var ucols = LeftSingularVectors.Columns();
            var Y = new float[vrows, urows];
            for (var i = 0; i < vrows; i++)
            {
                for (var j = 0; j < urows; j++)
                {
                    float sum = 0;
                    for (var k = 0; k < ucols; k++)
                    {
                        sum += X[i, k] * LeftSingularVectors[j, k];
                    }

                    Y[i, j] = sum;
                }
            }

            return Y;
        }

        /// <summary>
        ///     Reverses the decomposition, reconstructing the original matrix <c>X</c>.
        /// </summary>
        public float[,] Reverse()
        {
            return LeftSingularVectors.Dot(DiagonalMatrix).DotWithTransposed(RightSingularVectors);
        }

        /// <summary>
        ///     Computes <c>(Xt * X)^1</c> (the inverse of the covariance matrix). This
        ///     matrix can be used to determine standard errors for the coefficients when
        ///     solving a linear set of equations through any of the <see cref="Solve(Single[,])" />
        ///     methods.
        /// </summary>
        public float[,] GetInformationMatrix()
        {
            var e = Threshold;

            // X = V*S^-1
            var vrows = RightSingularVectors.Rows();
            var vcols = RightSingularVectors.Columns();
            var X = new float[vrows, Diagonal.Rows()];
            for (var i = 0; i < vrows; i++)
            {
                for (var j = 0; j < vcols; j++)
                {
                    if (System.Math.Abs(Diagonal[j]) > e)
                    {
                        X[i, j] = RightSingularVectors[i, j] / Diagonal[j];
                    }
                }
            }

            // Y = X*V'
            var Y = new float[vrows, vrows];
            for (var i = 0; i < vrows; i++)
            {
                for (var j = 0; j < vrows; j++)
                {
                    float sum = 0;
                    for (var k = 0; k < vrows; k++)
                    {
                        sum += X[i, k] * RightSingularVectors[j, k];
                    }

                    Y[i, j] = sum;
                }
            }

            return Y;
        }

        /// <summary>
        ///     Solves a linear equation system of the form AX = B.
        /// </summary>
        /// <param name="value">Parameter B from the equation AX = B.</param>
        /// <returns>The solution X from equation AX = B.</returns>
        public float[,] SolveTranspose(float[,] value)
        {
            // Additionally an important property is that if there does not exists a solution
            // when the matrix A is singular but replacing 1/Li with 0 will provide a solution
            // that minimizes the residue |AX -Y|. SVD finds the least squares best compromise
            // solution of the linear equation system. Interestingly SVD can be also used in an
            // over-determined system where the number of equations exceeds that of the parameters.

            // L is a diagonal matrix with non-negative matrix elements having the same
            // dimension as A, Wi ? 0. The diagonal elements of L are the singular values of matrix A.

            var Y = value;

            // Create L*, which is a diagonal matrix with elements
            //    L*[i] = 1/L[i]  if L[i] < e, else 0, 
            // where e is the so-called singularity threshold.

            // In other words, if L[i] is zero or close to zero (smaller than e),
            // one must replace 1/L[i] with 0. The value of e depends on the precision
            // of the hardware. This method can be used to solve linear equations
            // systems even if the matrices are singular or close to singular.

            //singularity threshold
            var e = Threshold;


            var scols = Diagonal.Rows();
            var Ls = new float[scols, scols];
            for (var i = 0; i < Diagonal.Rows(); i++)
            {
                if (System.Math.Abs(Diagonal[i]) <= e)
                {
                    Ls[i, i] = 0;
                }
                else
                {
                    Ls[i, i] = 1 / Diagonal[i];
                }
            }

            //(V x L*) x Ut x Y
            var VL = RightSingularVectors.Dot(Ls);

            //(V x L* x Ut) x Y
            var vrows = RightSingularVectors.Rows();
            var urows = LeftSingularVectors.Rows();
            var VLU = new float[vrows, scols];
            for (var i = 0; i < vrows; i++)
            {
                for (var j = 0; j < urows; j++)
                {
                    float sum = 0;
                    for (var k = 0; k < urows; k++)
                    {
                        sum += VL[i, k] * LeftSingularVectors[j, k];
                    }

                    VLU[i, j] = sum;
                }
            }

            return Y.Dot(VLU);
        }

        /// <summary>
        ///     Solves a linear equation system of the form AX = B.
        /// </summary>
        /// <param name="value">Parameter B from the equation AX = B.</param>
        /// <returns>The solution X from equation AX = B.</returns>
        public float[,] SolveForDiagonal(float[] value)
        {
            // Additionally an important property is that if there does not exists a solution
            // when the matrix A is singular but replacing 1/Li with 0 will provide a solution
            // that minimizes the residue |AX -Y|. SVD finds the least squares best compromise
            // solution of the linear equation system. Interestingly SVD can be also used in an
            // over-determined system where the number of equations exceeds that of the parameters.

            // L is a diagonal matrix with non-negative matrix elements having the same
            // dimension as A, Wi ? 0. The diagonal elements of L are the singular values of matrix A.

            var Y = value;

            // Create L*, which is a diagonal matrix with elements
            //    L*[i] = 1/L[i]  if L[i] < e, else 0, 
            // where e is the so-called singularity threshold.

            // In other words, if L[i] is zero or close to zero (smaller than e),
            // one must replace 1/L[i] with 0. The value of e depends on the precision
            // of the hardware. This method can be used to solve linear equations
            // systems even if the matrices are singular or close to singular.

            //singularity threshold
            var e = Threshold;


            var scols = Diagonal.Rows();
            var Ls = new float[scols, scols];
            for (var i = 0; i < Diagonal.Rows(); i++)
            {
                if (System.Math.Abs(Diagonal[i]) <= e)
                {
                    Ls[i, i] = 0;
                }
                else
                {
                    Ls[i, i] = 1 / Diagonal[i];
                }
            }

            //(V x L*) x Ut x Y
            var VL = RightSingularVectors.Dot(Ls);

            //(V x L* x Ut) x Y
            var vrows = RightSingularVectors.Rows();
            var urows = LeftSingularVectors.Rows();
            var VLU = new float[vrows, scols];
            for (var i = 0; i < vrows; i++)
            {
                for (var j = 0; j < urows; j++)
                {
                    float sum = 0;
                    for (var k = 0; k < urows; k++)
                    {
                        sum += VL[i, k] * LeftSingularVectors[j, k];
                    }

                    VLU[i, j] = sum;
                }
            }

            //(V x L* x Ut x Y)
            return VLU.DotWithDiagonal(Y);
        }

        /// <summary>
        ///     Solves a linear equation system of the form xA = b.
        /// </summary>
        /// <param name="value">The b from the equation xA = b.</param>
        /// <returns>The x from equation Ax = b.</returns>
        public float[] SolveTranspose(float[] value)
        {
            // Additionally an important property is that if there does not exists a solution
            // when the matrix A is singular but replacing 1/Li with 0 will provide a solution
            // that minimizes the residue |AX -Y|. SVD finds the least squares best compromise
            // solution of the linear equation system. Interestingly SVD can be also used in an
            // over-determined system where the number of equations exceeds that of the parameters.

            // L is a diagonal matrix with non-negative matrix elements having the same
            // dimension as A, Wi ? 0. The diagonal elements of L are the singular values of matrix A.

            var Y = value;

            // Create L*, which is a diagonal matrix with elements
            //    L*[i] = 1/L[i]  if L[i] < e, else 0, 
            // where e is the so-called singularity threshold.

            // In other words, if L[i] is zero or close to zero (smaller than e),
            // one must replace 1/L[i] with 0. The value of e depends on the precision
            // of the hardware. This method can be used to solve linear equations
            // systems even if the matrices are singular or close to singular.

            //singularity threshold
            var e = Threshold;


            var scols = Diagonal.Rows();
            var Ls = new float[scols, scols];
            for (var i = 0; i < Diagonal.Rows(); i++)
            {
                if (System.Math.Abs(Diagonal[i]) <= e)
                {
                    Ls[i, i] = 0;
                }
                else
                {
                    Ls[i, i] = 1 / Diagonal[i];
                }
            }

            //(V x L*) x Ut x Y
            var VL = RightSingularVectors.Dot(Ls);

            //(V x L* x Ut) x Y
            var vrows = RightSingularVectors.Rows();
            var urows = LeftSingularVectors.Rows();
            var VLU = new float[vrows, scols];
            for (var i = 0; i < vrows; i++)
            {
                for (var j = 0; j < urows; j++)
                {
                    float sum = 0;
                    for (var k = 0; k < urows; k++)
                    {
                        sum += VL[i, k] * LeftSingularVectors[j, k];
                    }

                    VLU[i, j] = sum;
                }
            }

            return Y.Dot(VLU);
        }

        #region ICloneable Members

        private SingularValueDecompositionF()
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
            var svd = new SingularValueDecompositionF();
            svd.m = m;
            svd.n = n;
            svd.Diagonal = (float[])Diagonal.Clone();
            svd.Ordering = (int[])Ordering.Clone();
            svd.swapped = swapped;
            if (LeftSingularVectors != null)
            {
                svd.LeftSingularVectors = Matrix.MemberwiseClone(LeftSingularVectors);
            }

            if (RightSingularVectors != null)
            {
                svd.RightSingularVectors = Matrix.MemberwiseClone(RightSingularVectors);
            }

            return svd;
        }

        #endregion
    }
}