// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
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
//
// This code has been based on the LIBLINEAR project;s implementation for the
// L2-regularized L2-loss support vector classification (dual) machines. The
// original LIBLINEAR license is given below:
//
//
//   Copyright (c) 2007-2011 The LIBLINEAR Project.
//   All rights reserved.
//
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions
//   are met:
//
//      1. Redistributions of source code must retain the above copyright
//      notice, this list of conditions and the following disclaimer.
//
//      2. Redistributions in binary form must reproduce the above copyright
//      notice, this list of conditions and the following disclaimer in the
//      documentation and/or other materials provided with the distribution.
//
//      3. Neither name of copyright holders nor the names of its contributors
//      may be used to endorse or promote products derived from this software
//      without specific prior written permission.
//
//
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//   ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//   A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR
//   CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//   LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//   NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

using System;
using System.Diagnostics;
using System.Threading;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    public abstract partial class BaseLinearDualCoordinateDescent<TModel, TKernel, TInput>
    {
        private double[] ComputeUpperBounds(double[] c, out double[] diag)
        {
            // default solver_type: L2R_L2LOSS_SVC_DUAL
            // double[] diag = { 0.5 / Cn, 0, 0.5 / Cp };
            // double[] upper_bound = { Double.PositiveInfinity, 0, Double.PositiveInfinity };

            diag = new double[c.Length];
            var upper_bound = new double[c.Length];

            if (Loss == Loss.L2)
            {
                // Default
                for (var i = 0; i < diag.Length; i++)
                {
                    diag[i] = 0.5 / c[i];
                }

                for (var i = 0; i < upper_bound.Length; i++)
                {
                    upper_bound[i] = double.PositiveInfinity;
                }
            }
            else
            {
                // diag remains zero, and
                upper_bound = c;
            }

            return upper_bound;
        }

        private sealed class DualSvmSolver
        {
            private readonly double[] QD;
            private readonly double[] alpha;
            private readonly double[] diag;
            private readonly double eps;
            private readonly int[] index;
            private readonly int max_iter;
            private readonly Random random;
            private readonly CancellationToken token;
            private readonly double[] upper_bound;
            private readonly double[] w;
            private readonly TInput[] x;
            private readonly int[] y;
            private double PGmax_new, PGmin_new;

            // PG: projected gradient, for shrinking and stopping
            private double PGmax_old = double.PositiveInfinity;
            private double PGmin_old = double.NegativeInfinity;
            private int active_size;

            private int iter;
            private TKernel kernel;

            public DualSvmSolver(TKernel kernel, TInput[] x, int[] y, double[] w, double[] alpha, double[] diag,
                double[] upper_bound, Random random, CancellationToken token, int max_iter, double eps)
            {
                this.kernel = kernel;
                this.x = x;
                this.y = y;
                this.w = w;
                this.alpha = alpha;
                this.diag = diag;
                this.upper_bound = upper_bound;
                this.random = random;
                this.token = token;
                this.max_iter = max_iter;
                this.eps = eps;

                QD = new double[x.Length];
                index = new int[x.Length];
                active_size = x.Length;
            }

            public double Bias { get; private set; }

            public void Initialize()
            {
                for (var i = 0; i < x.Length; i++)
                {
                    QD[i] = 1 + diag[i] + kernel.Function(x[i], x[i]);
                    kernel.Product(y[i] * alpha[i], x[i], w);
                    Bias += y[i] * alpha[i];

                    index[i] = i;
                }
            }

            public void Optimize()
            {
                while (iter < max_iter)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    PGmax_new = double.NegativeInfinity;
                    PGmin_new = double.PositiveInfinity;

                    ShuffleActiveSet();

                    for (var s = 0; s < active_size; s++)
                    {
                        if (UpdateCoordinate(s))
                        {
                            s--;
                        }
                    }

                    iter++;

                    if (iter % 10 == 0)
                    {
                        Debug.WriteLine(".");
                    }

                    if (PGmax_new - PGmin_new <= eps)
                    {
                        if (active_size == x.Length)
                        {
                            break;
                        }

                        active_size = x.Length;
                        Debug.WriteLine("*");
                        PGmax_old = double.PositiveInfinity;
                        PGmin_old = double.NegativeInfinity;
                        continue;
                    }

                    PGmax_old = PGmax_new;
                    PGmin_old = PGmin_new;

                    if (PGmax_old <= 0)
                    {
                        PGmax_old = double.PositiveInfinity;
                    }

                    if (PGmin_old >= 0)
                    {
                        PGmin_old = double.NegativeInfinity;
                    }
                }

                Debug.WriteLine("optimization finished, #iter = " + iter);

                if (iter >= max_iter)
                {
                    Debug.WriteLine("WARNING: reaching max number of iterations");
                    Debug.WriteLine("Using -s 2 may be faster (also see FAQ)");
                }
            }

            private void ShuffleActiveSet()
            {
                for (var i = 0; i < active_size; i++)
                {
                    var j = i + random.Next() % (active_size - i);

                    var old = index[i];
                    index[i] = index[j];
                    index[j] = old;
                }
            }

            private bool UpdateCoordinate(int s)
            {
                double PG;

                var i = index[s];
                var yi = y[i];

                var G = Bias + kernel.Function(w, x[i]);

                G = G * yi - 1;

                var C = upper_bound[i];
                G += alpha[i] * diag[i];

                if (!TryComputeProjectedGradient(i, s, G, C, out PG))
                {
                    return true;
                }

                PGmax_new = System.Math.Max(PGmax_new, PG);
                PGmin_new = System.Math.Min(PGmin_new, PG);

                if (System.Math.Abs(PG) > 1.0e-12)
                {
                    var alpha_old = alpha[i];

                    alpha[i] = System.Math.Min(System.Math.Max(alpha[i] - G / QD[i], 0.0), C);

                    var d = (alpha[i] - alpha_old) * yi;

                    kernel.Product(d, x[i], w);
                    Bias += d;
                }

                return false;
            }

            private bool TryComputeProjectedGradient(int i, int s, double G, double C, out double PG)
            {
                PG = 0;
                if (alpha[i] == 0)
                {
                    if (G > PGmax_old)
                    {
                        Shrink(s);

                        return false;
                    }

                    if (G < 0)
                    {
                        PG = G;
                    }
                }
                else if (alpha[i] == C)
                {
                    if (G < PGmin_old)
                    {
                        Shrink(s);

                        return false;
                    }

                    if (G > 0)
                    {
                        PG = G;
                    }
                }
                else
                {
                    PG = G;
                }

                return true;
            }

            private void Shrink(int s)
            {
                active_size--;

                var old = index[s];
                index[s] = index[active_size];
                index[active_size] = old;
            }
        }
    }
}