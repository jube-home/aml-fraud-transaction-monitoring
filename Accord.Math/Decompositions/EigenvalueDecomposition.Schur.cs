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

namespace Accord.Math.Decompositions
{
    public sealed partial class EigenvalueDecomposition
    {
        private sealed class SchurReduction
        {
            private readonly double[,] H;
            private readonly double[,] V;
            private readonly double[] d;
            private readonly double[] e;
            private readonly int high;
            private readonly int low;
            private readonly int nn;
            private readonly double schurEps;
            private double exshift;
            private int iter;
            private int n;
            private double norm;
            private double p;
            private double q;
            private double r;
            private double s;
            private double t;
            private double w;
            private double x;
            private double y;
            private double z;

            public SchurReduction(EigenvalueDecomposition owner)
            {
                H = owner.H;
                V = owner.Eigenvectors;
                d = owner.RealEigenvalues;
                e = owner.ImaginaryEigenvalues;
                nn = owner.n;
                n = nn - 1;
                low = 0;
                high = nn - 1;
                schurEps = 2 * Constants.DoubleEpsilon;
                exshift = 0;
                p = 0;
                q = 0;
                r = 0;
                s = 0;
                z = 0;
            }

            public void Run()
            {
                // Nonsymmetric reduction from Hessenberg to real Schur form.   
                // This is derived from the Algol procedure hqr2, by Martin and Wilkinson, Handbook for Auto. Comp.,
                // Vol.ii-Linear Algebra, and the corresponding  Fortran subroutine in EISPACK.

                ComputeNorm();

                ReduceToRealSchurForm();

                // Backsubstitute to find vectors of upper triangular form
                if (norm == 0)
                {
                    return;
                }

                BackSubstitute();

                CopyIsolatedRoots();

                BackTransform();
            }

            private void ComputeNorm()
            {
                // Store roots isolated by balanc and compute matrix norm
                norm = 0;
                for (var i = 0; i < nn; i++)
                {
                    if ((i < low) || (i > high))
                    {
                        d[i] = H[i, i];
                        e[i] = 0;
                    }

                    for (var j = System.Math.Max(i - 1, 0); j < nn; j++)
                    {
                        norm = norm + System.Math.Abs(H[i, j]);
                    }
                }
            }

            private void ReduceToRealSchurForm()
            {
                // Outer loop over eigenvalue index
                iter = 0;
                while (n >= low)
                {
                    var l = FindSmallSubdiagonal();

                    // Check for convergence
                    if (l == n)
                    {
                        OneRootFound();
                    }
                    else if (l == n - 1)
                    {
                        TwoRootsFound();
                    }
                    else
                    {
                        ShiftAndIterate(l);
                    }
                }
            }

            private int FindSmallSubdiagonal()
            {
                // Look for single small sub-diagonal element
                var l = n;
                while (l > low)
                {
                    s = System.Math.Abs(H[l - 1, l - 1]) + System.Math.Abs(H[l, l]);

                    if (s == 0)
                    {
                        s = norm;
                    }

                    if (double.IsNaN(s))
                    {
                        break;
                    }

                    if (System.Math.Abs(H[l, l - 1]) < schurEps * s)
                    {
                        break;
                    }

                    l--;
                }

                return l;
            }

            private void OneRootFound()
            {
                // One root found
                H[n, n] = H[n, n] + exshift;
                d[n] = H[n, n];
                e[n] = 0;
                n--;
                iter = 0;
            }

            private void TwoRootsFound()
            {
                // Two roots found
                w = H[n, n - 1] * H[n - 1, n];
                p = (H[n - 1, n - 1] - H[n, n]) / 2;
                q = p * p + w;
                z = System.Math.Sqrt(System.Math.Abs(q));
                H[n, n] = H[n, n] + exshift;
                H[n - 1, n - 1] = H[n - 1, n - 1] + exshift;
                x = H[n, n];


                if (q >= 0)
                {
                    ProcessRealPair();
                }
                else
                {
                    // Complex pair
                    d[n - 1] = x + p;
                    d[n] = x + p;
                    e[n - 1] = z;
                    e[n] = -z;
                }

                n = n - 2;
                iter = 0;
            }

            private void ProcessRealPair()
            {
                // Real pair
                z = p >= 0 ? p + z : p - z;
                d[n - 1] = x + z;
                d[n] = d[n - 1];
                if (z != 0)
                {
                    d[n] = x - w / z;
                }

                e[n - 1] = 0;
                e[n] = 0;
                x = H[n, n - 1];
                s = System.Math.Abs(x) + System.Math.Abs(z);
                p = x / s;
                q = z / s;
                r = System.Math.Sqrt(p * p + q * q);
                p = p / r;
                q = q / r;


                RotateRealPair();
            }

            private void RotateRealPair()
            {
                // Row modification
                for (var j = n - 1; j < nn; j++)
                {
                    z = H[n - 1, j];
                    H[n - 1, j] = q * z + p * H[n, j];
                    H[n, j] = q * H[n, j] - p * z;
                }

                // Column modification
                for (var i = 0; i <= n; i++)
                {
                    z = H[i, n - 1];
                    H[i, n - 1] = q * z + p * H[i, n];
                    H[i, n] = q * H[i, n] - p * z;
                }

                // Accumulate transformations
                for (var i = low; i <= high; i++)
                {
                    z = V[i, n - 1];
                    V[i, n - 1] = q * z + p * V[i, n];
                    V[i, n] = q * V[i, n] - p * z;
                }
            }

            private void ShiftAndIterate(int l)
            {
                // No convergence yet     

                // Form shift
                x = H[n, n];
                y = 0;
                w = 0;
                if (l < n)
                {
                    y = H[n - 1, n - 1];
                    w = H[n, n - 1] * H[n - 1, n];
                }

                ApplyExceptionalShifts();

                iter = iter + 1;


                var m = FindTwoSmallSubdiagonals(l);

                ZeroBelowSubdiagonals(m);

                DoubleQrStep(l, m);
            }

            private void ApplyExceptionalShifts()
            {
                // Wilkinson's original ad hoc shift
                if (iter == 10)
                {
                    exshift += x;
                    for (var i = low; i <= n; i++)
                    {
                        H[i, i] -= x;
                    }

                    s = System.Math.Abs(H[n, n - 1]) + System.Math.Abs(H[n - 1, n - 2]);
                    x = y = 0.75 * s;
                    w = -0.4375 * s * s;
                }

                // MATLAB's new ad hoc shift
                if (iter == 30)
                {
                    s = (y - x) / 2;
                    s = s * s + w;
                    if (s > 0)
                    {
                        s = System.Math.Sqrt(s);
                        if (y < x)
                        {
                            s = -s;
                        }

                        s = x - w / ((y - x) / 2 + s);
                        for (var i = low; i <= n; i++)
                        {
                            H[i, i] -= s;
                        }

                        exshift += s;
                        x = y = w = 0.964;
                    }
                }
            }

            private int FindTwoSmallSubdiagonals(int l)
            {
                // Look for two consecutive small sub-diagonal elements
                var m = n - 2;
                while (m >= l)
                {
                    z = H[m, m];
                    r = x - z;
                    s = y - z;
                    p = (r * s - w) / H[m + 1, m] + H[m, m + 1];
                    q = H[m + 1, m + 1] - z - r - s;
                    r = H[m + 2, m + 1];
                    s = System.Math.Abs(p) + System.Math.Abs(q) + System.Math.Abs(r);
                    p = p / s;
                    q = q / s;
                    r = r / s;
                    if (m == l)
                    {
                        break;
                    }

                    if (System.Math.Abs(H[m, m - 1]) * (System.Math.Abs(q) + System.Math.Abs(r)) < schurEps *
                        (System.Math.Abs(p) * (System.Math.Abs(H[m - 1, m - 1]) + System.Math.Abs(z) +
                                               System.Math.Abs(H[m + 1, m + 1]))))
                    {
                        break;
                    }

                    m--;
                }

                return m;
            }

            private void ZeroBelowSubdiagonals(int m)
            {
                for (var i = m + 2; i <= n; i++)
                {
                    H[i, i - 2] = 0;
                    if (i > m + 2)
                    {
                        H[i, i - 3] = 0;
                    }
                }
            }

            private void DoubleQrStep(int l, int m)
            {
                // Double QR step involving rows l:n and columns m:n
                for (var k = m; k <= n - 1; k++)
                {
                    var notlast = k != n - 1;
                    if (k != m)
                    {
                        p = H[k, k - 1];
                        q = H[k + 1, k - 1];
                        r = notlast ? H[k + 2, k - 1] : 0;
                        x = System.Math.Abs(p) + System.Math.Abs(q) + System.Math.Abs(r);
                        if (x != 0)
                        {
                            p = p / x;
                            q = q / x;
                            r = r / x;
                        }
                    }

                    if (x == 0)
                    {
                        break;
                    }

                    s = System.Math.Sqrt(p * p + q * q + r * r);
                    if (p < 0)
                    {
                        s = -s;
                    }

                    if (s != 0)
                    {
                        if (k != m)
                        {
                            H[k, k - 1] = -s * x;
                        }
                        else if (l != m)
                        {
                            H[k, k - 1] = -H[k, k - 1];
                        }

                        p = p + s;
                        x = p / s;
                        y = q / s;
                        z = r / s;
                        q = q / p;
                        r = r / p;

                        RowModification(k, notlast);

                        ColumnModification(k, notlast);

                        AccumulateTransformations(k, notlast);
                    }
                }
            }

            private void RowModification(int k, bool notlast)
            {
                var H = this.H;
                var nn = this.nn;
                var q = this.q;
                var r = this.r;
                var x = this.x;
                var y = this.y;
                var z = this.z;
                var p = this.p;

                // Row modification
                for (var j = k; j < nn; j++)
                {
                    p = H[k, j] + q * H[k + 1, j];
                    if (notlast)
                    {
                        p = p + r * H[k + 2, j];
                        H[k + 2, j] = H[k + 2, j] - p * z;
                    }

                    H[k, j] = H[k, j] - p * x;
                    H[k + 1, j] = H[k + 1, j] - p * y;
                }

                this.p = p;
            }

            private void ColumnModification(int k, bool notlast)
            {
                var H = this.H;
                var n = this.n;
                var q = this.q;
                var r = this.r;
                var x = this.x;
                var y = this.y;
                var z = this.z;
                var p = this.p;

                // Column modification
                for (var i = 0; i <= System.Math.Min(n, k + 3); i++)
                {
                    p = x * H[i, k] + y * H[i, k + 1];
                    if (notlast)
                    {
                        p = p + z * H[i, k + 2];
                        H[i, k + 2] = H[i, k + 2] - p * r;
                    }

                    H[i, k] = H[i, k] - p;
                    H[i, k + 1] = H[i, k + 1] - p * q;
                }

                this.p = p;
            }

            private void AccumulateTransformations(int k, bool notlast)
            {
                var V = this.V;
                var low = this.low;
                var high = this.high;
                var q = this.q;
                var r = this.r;
                var x = this.x;
                var y = this.y;
                var z = this.z;
                var p = this.p;

                // Accumulate transformations
                for (var i = low; i <= high; i++)
                {
                    p = x * V[i, k] + y * V[i, k + 1];
                    if (notlast)
                    {
                        p = p + z * V[i, k + 2];
                        V[i, k + 2] = V[i, k + 2] - p * r;
                    }

                    V[i, k] = V[i, k] - p;
                    V[i, k + 1] = V[i, k + 1] - p * q;
                }

                this.p = p;
            }

            private void BackSubstitute()
            {
                // Backsubstitute to find vectors of upper triangular form
                for (n = nn - 1; n >= 0; n--)
                {
                    p = d[n];
                    q = e[n];

                    // Real vector
                    if (q == 0)
                    {
                        RealVector();
                    }
                    else if (q < 0)
                    {
                        ComplexVector();
                    }
                }
            }

            private void RealVector()
            {
                var l = n;
                H[n, n] = 1;
                for (var i = n - 1; i >= 0; i--)
                {
                    RealVectorRow(i, ref l);
                }
            }

            private void RealVectorRow(int i, ref int l)
            {
                w = H[i, i] - p;
                r = 0;
                for (var j = l; j <= n; j++)
                {
                    r = r + H[i, j] * H[j, n];
                }

                if (e[i] < 0)
                {
                    z = w;
                    s = r;
                }
                else
                {
                    l = i;
                    if (e[i] == 0)
                    {
                        H[i, n] = w != 0 ? -r / w : -r / (schurEps * norm);
                    }
                    else
                    {
                        // Solve real equations
                        x = H[i, i + 1];
                        y = H[i + 1, i];
                        q = (d[i] - p) * (d[i] - p) + e[i] * e[i];
                        t = (x * s - z * r) / q;
                        H[i, n] = t;
                        H[i + 1, n] = System.Math.Abs(x) > System.Math.Abs(z) ? (-r - w * t) / x : (-s - y * t) / z;
                    }

                    // Overflow control
                    t = System.Math.Abs(H[i, n]);
                    if (schurEps * t * t > 1)
                    {
                        for (var j = i; j <= n; j++)
                        {
                            H[j, n] = H[j, n] / t;
                        }
                    }
                }
            }

            private void ComplexVector()
            {
                // Complex vector
                var l = n - 1;

                // Last vector component imaginary so matrix is triangular
                if (System.Math.Abs(H[n, n - 1]) > System.Math.Abs(H[n - 1, n]))
                {
                    H[n - 1, n - 1] = q / H[n, n - 1];
                    H[n - 1, n] = -(H[n, n] - p) / H[n, n - 1];
                }
                else
                {
                    cdiv(0, -H[n - 1, n], H[n - 1, n - 1] - p, q, out H[n - 1, n - 1], out H[n - 1, n]);
                }

                H[n, n - 1] = 0;
                H[n, n] = 1;
                for (var i = n - 2; i >= 0; i--)
                {
                    ComplexVectorRow(i, ref l);
                }
            }

            private void ComplexVectorRow(int i, ref int l)
            {
                double ra = 0;
                double sa = 0;
                for (var j = l; j <= n; j++)
                {
                    ra = ra + H[i, j] * H[j, n - 1];
                    sa = sa + H[i, j] * H[j, n];
                }

                w = H[i, i] - p;

                if (e[i] < 0)
                {
                    z = w;
                    r = ra;
                    s = sa;
                }
                else
                {
                    l = i;
                    if (e[i] == 0)
                    {
                        cdiv(-ra, -sa, w, q, out H[i, n - 1], out H[i, n]);
                    }
                    else
                    {
                        // Solve complex equations
                        x = H[i, i + 1];
                        y = H[i + 1, i];
                        var vr = (d[i] - p) * (d[i] - p) + e[i] * e[i] - q * q;
                        var vi = (d[i] - p) * 2 * q;
                        if ((vr == 0) && (vi == 0))
                        {
                            vr = schurEps * norm *
                                 (System.Math.Abs(w) + System.Math.Abs(q) + System.Math.Abs(x) + System.Math.Abs(y) +
                                  System.Math.Abs(z));
                        }

                        cdiv(x * r - z * ra + q * sa, x * s - z * sa - q * ra, vr, vi, out H[i, n - 1], out H[i, n]);
                        if (System.Math.Abs(x) > System.Math.Abs(z) + System.Math.Abs(q))
                        {
                            H[i + 1, n - 1] = (-ra - w * H[i, n - 1] + q * H[i, n]) / x;
                            H[i + 1, n] = (-sa - w * H[i, n] - q * H[i, n - 1]) / x;
                        }
                        else
                        {
                            cdiv(-r - y * H[i, n - 1], -s - y * H[i, n], z, q, out H[i + 1, n - 1], out H[i + 1, n]);
                        }
                    }

                    // Overflow control
                    t = System.Math.Max(System.Math.Abs(H[i, n - 1]), System.Math.Abs(H[i, n]));
                    if (schurEps * t * t > 1)
                    {
                        for (var j = i; j <= n; j++)
                        {
                            H[j, n - 1] = H[j, n - 1] / t;
                            H[j, n] = H[j, n] / t;
                        }
                    }
                }
            }

            private void CopyIsolatedRoots()
            {
                // Vectors of isolated roots
                for (var i = 0; i < nn; i++)
                {
                    if ((i < low) || (i > high))
                    {
                        for (var j = i; j < nn; j++)
                        {
                            V[i, j] = H[i, j];
                        }
                    }
                }
            }

            private void BackTransform()
            {
                var H = this.H;
                var V = this.V;
                var low = this.low;
                var high = this.high;
                var z = this.z;

                // Back transformation to get eigenvectors of original matrix
                for (var j = nn - 1; j >= low; j--)
                {
                    for (var i = low; i <= high; i++)
                    {
                        z = 0;
                        for (var k = low; k <= System.Math.Min(j, high); k++)
                        {
                            z = z + V[i, k] * H[k, j];
                        }

                        V[i, j] = z;
                    }
                }

                this.z = z;
            }
        }
    }
}