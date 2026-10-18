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
    public abstract partial class BaseLinearRegressionCoordinateDescent<TModel, TKernel, TInput>
    {
        private sealed class DualRegressionSolver
        {
            private readonly double[] QD;
            private readonly double[] beta;
            private readonly double eps;
            private readonly int[] index;
            private readonly double lambda;
            private readonly int max_iter;
            private readonly double p;
            private readonly Random random;
            private readonly CancellationToken token;
            private readonly double upper_bound;
            private readonly double[] w;
            private readonly TInput[] x;
            private readonly double[] y;
            private double Gmax_new, Gnorm1_new;

            private double Gmax_old = double.PositiveInfinity;
            private double Gnorm1_init = -1.0; // Gnorm1_init is initialized at the first iteration
            private int active_size;

            private int iter;
            private TKernel kernel;

            public DualRegressionSolver(TKernel kernel, double[] w, TInput[] x, double[] y, double[] beta,
                Random random, CancellationToken token, int max_iter, double p, double eps,
                double lambda, double upper_bound)
            {
                this.kernel = kernel;
                this.w = w;
                this.x = x;
                this.y = y;
                this.beta = beta;
                this.random = random;
                this.token = token;
                this.max_iter = max_iter;
                this.p = p;
                this.eps = eps;
                this.lambda = lambda;
                this.upper_bound = upper_bound;

                active_size = x.Length;
                index = new int[x.Length];
                QD = new double[x.Length];
            }

            public void Initialize()
            {
                for (var i = 0; i < x.Length; i++)
                {
                    QD[i] = 0;

                    var xi = x[i];

                    QD[i] = kernel.Function(xi, xi);
                    kernel.Product(beta[i], xi, w);
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

                    Gmax_new = 0;
                    Gnorm1_new = 0;

                    ShuffleActiveSet();

                    for (var s = 0; s < active_size; s++)
                    {
                        if (UpdateCoordinate(s))
                        {
                            s--;
                        }
                    }

                    if (iter == 0)
                    {
                        Gnorm1_init = Gnorm1_new;
                    }

                    iter++;

                    if (iter % 10 == 0)
                    {
                        Debug.WriteLine(".");
                    }

                    if (Gnorm1_new <= eps * Gnorm1_init)
                    {
                        if (active_size == x.Length)
                        {
                            break;
                        }

                        active_size = x.Length;
                        Debug.WriteLine("*");
                        Gmax_old = double.PositiveInfinity;
                        continue;
                    }

                    Gmax_old = Gmax_new;
                }

                Debug.WriteLine("optimization finished, #iter = " + iter);

                if (iter >= max_iter)
                {
                    Debug.WriteLine("WARNING: reaching max number of iterations");
                    Debug.WriteLine("Using -s 11 may be faster (also see FAQ)");
                }
            }

            private void ShuffleActiveSet()
            {
                int i;

                for (i = 0; i < active_size; i++)
                {
                    var j = i + random.Next() % (active_size - i);

                    var old = index[i];
                    index[i] = index[j];
                    index[j] = old;
                }
            }

            private void Shrink(int s)
            {
                active_size--;

                var old = index[s];
                index[s] = index[active_size];
                index[active_size] = old;
            }

            private bool UpdateCoordinate(int s)
            {
                double d, G, H;

                var i = index[s];
                G = -y[i] + lambda * beta[i];
                H = QD[i] + lambda;

                G += kernel.Function(w, x[i]);

                var Gp = G + p;
                var Gn = G - p;
                double violation;

                if (!TryComputeViolation(i, s, Gp, Gn, out violation))
                {
                    return true;
                }

                Gmax_new = System.Math.Max(Gmax_new, violation);
                Gnorm1_new += violation;

                // obtain Newton direction d
                if (Gp < H * beta[i])
                {
                    d = -Gp / H;
                }
                else if (Gn > H * beta[i])
                {
                    d = -Gn / H;
                }
                else
                {
                    d = -beta[i];
                }

                if (System.Math.Abs(d) < 1.0e-12)
                {
                    return false;
                }

                var beta_old = beta[i];
                beta[i] = System.Math.Min(System.Math.Max(beta[i] + d, -upper_bound), upper_bound);
                d = beta[i] - beta_old;

                if (d != 0)
                {
                    kernel.Product(d, x[i], w);
                }

                return false;
            }

            private bool TryComputeViolation(int i, int s, double Gp, double Gn, out double violation)
            {
                violation = 0;

                if (beta[i] == 0)
                {
                    if (Gp < 0)
                    {
                        violation = -Gp;
                    }
                    else if (Gn > 0)
                    {
                        violation = Gn;
                    }
                    else if (Gp > Gmax_old && Gn < -Gmax_old)
                    {
                        Shrink(s);

                        return false;
                    }
                }
                else if (beta[i] >= upper_bound)
                {
                    if (Gp > 0)
                    {
                        violation = Gp;
                    }
                    else if (Gp < -Gmax_old)
                    {
                        Shrink(s);

                        return false;
                    }
                }
                else if (beta[i] <= -upper_bound)
                {
                    if (Gn < 0)
                    {
                        violation = -Gn;
                    }
                    else if (Gn > Gmax_old)
                    {
                        Shrink(s);

                        return false;
                    }
                }
                else if (beta[i] > 0)
                {
                    violation = System.Math.Abs(Gp);
                }
                else
                {
                    violation = System.Math.Abs(Gn);
                }

                return true;
            }

            public void TraceObjective()
            {
                // calculate objective value
                double v = 0;
                var nSV = 0;

                for (var i = 0; i < w.Length; i++)
                {
                    v += w[i] * w[i];
                }

                v = 0.5 * v;

                for (var i = 0; i < x.Length; i++)
                {
                    v += p * System.Math.Abs(beta[i]) - y[i] * beta[i] + 0.5 * lambda * beta[i] * beta[i];

                    if (beta[i] != 0)
                    {
                        nSV++;
                    }
                }

                Debug.WriteLine("Objective value = " + v);
                Debug.WriteLine("nSV = " + nSV);
            }
        }
    }
}