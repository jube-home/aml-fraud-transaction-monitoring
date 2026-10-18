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
    public abstract partial class BaseLinearCoordinateDescent<TModel, TKernel>
    {
        private sealed class L1RegularizedL2LossSolver
        {
            private readonly double[] b; // b = 1-ywTx
            private readonly double[] bias;
            private readonly int biasIndex;

            private readonly double[] c;
            private readonly double eps;
            private readonly int[] index;
            private readonly int max_iter = 1000;
            private readonly int max_num_linesearch = 20;
            private readonly Random random;

            private readonly double sigma = 0.01;
            private readonly CancellationToken token;
            private readonly double[] w;
            private readonly double[][] x;
            private readonly double[] xj_sq;
            private readonly int[] y;
            private double Gmax_new, Gnorm1_new;
            private double Gmax_old = double.PositiveInfinity;
            private double Gnorm1_init = -1.0; // Gnorm1_init is initialized at the first iteration
            private int active_size;

            private int iter;

            public L1RegularizedL2LossSolver(double[] w, double[] bias, int biasIndex, Random random,
                CancellationToken token, double eps, double[] c, double[][] x, int[] y)
            {
                this.w = w;
                this.bias = bias;
                this.biasIndex = biasIndex;
                this.random = random;
                this.token = token;
                this.eps = eps;
                this.c = c;
                this.x = x;
                this.y = y;

                active_size = w.Length;
                index = new int[w.Length];
                b = new double[x.Length];
                xj_sq = new double[w.Length];
            }

            public void Initialize()
            {
                for (var i = 0; i < bias.Length; i++)
                {
                    bias[i] = 1;
                }

                for (var j = 0; j < b.Length; j++)
                {
                    b[j] = 1;
                }

                for (var j = 0; j < w.Length; j++)
                {
                    index[j] = j;
                    xj_sq[j] = 0;

                    if (j == biasIndex)
                    {
                        for (var i = 0; i < x.Length; i++)
                        {
                            bias[i] *= y[i]; // x->value stores yi*xij
                            b[i] -= w[j];
                            xj_sq[j] += c[i];
                        }
                    }
                    else
                    {
                        for (var i = 0; i < x.Length; i++)
                        {
                            var val = x[i][j] * y[i];
                            b[i] -= w[j] * val;
                            xj_sq[j] += c[i] * val * val;
                        }
                    }
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
                        if (active_size == w.Length)
                        {
                            break;
                        }

                        active_size = w.Length;
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
                }
            }

            private void ShuffleActiveSet()
            {
                int j;

                for (j = 0; j < active_size; j++)
                {
                    var i = j + random.Next() % (active_size - j);
                    var old = index[i];
                    index[i] = index[j];
                    index[j] = old;
                }
            }

            private bool UpdateCoordinate(int s)
            {
                double d, G_loss, G, H;

                var j = index[s];

                ComputeLossDerivatives(j, out G_loss, out H);

                G = G_loss;

                var Gp = G + 1;
                var Gn = G - 1;
                double violation;

                if (!TryComputeViolation(j, s, Gp, Gn, out violation))
                {
                    return true;
                }

                Gmax_new = System.Math.Max(Gmax_new, violation);
                Gnorm1_new += violation;

                // obtain Newton direction d
                if (Gp < H * w[j])
                {
                    d = -Gp / H;
                }
                else if (Gn > H * w[j])
                {
                    d = -Gn / H;
                }
                else
                {
                    d = -w[j];
                }

                if (System.Math.Abs(d) < 1.0e-12)
                {
                    return false;
                }

                var num_linesearch = LineSearch(j, ref d, G, G_loss);

                w[j] += d;

                // recompute b[] if line search takes too many steps
                if (num_linesearch >= max_num_linesearch)
                {
                    Debug.WriteLine("#");
                    RecomputeB();
                }

                return false;
            }

            private bool TryComputeViolation(int j, int s, double Gp, double Gn, out double violation)
            {
                violation = 0;

                if (w[j] == 0)
                {
                    if (Gp < 0)
                    {
                        violation = -Gp;
                    }
                    else if (Gn > 0)
                    {
                        violation = Gn;
                    }
                    else if (Gp > Gmax_old / x.Length && Gn < -Gmax_old / x.Length)
                    {
                        active_size--;

                        var old = index[s];
                        index[s] = index[active_size];
                        index[active_size] = old;

                        return false;
                    }
                }
                else if (w[j] > 0)
                {
                    violation = System.Math.Abs(Gp);
                }
                else
                {
                    violation = System.Math.Abs(Gn);
                }

                return true;
            }

            private void ComputeLossDerivatives(int j, out double G_loss, out double H)
            {
                var b = this.b;
                var x = this.x;
                var y = this.y;
                var c = this.c;
                var bias = this.bias;

                G_loss = 0;
                H = 0;

                for (var i = 0; i < x.Length; i++)
                {
                    if (b[i] > 0)
                    {
                        if (j == biasIndex)
                        {
                            var val = bias[i];
                            var tmp = c[i] * val;
                            G_loss -= tmp * b[i];
                            H += tmp * val;
                        }
                        else
                        {
                            var val = x[i][j] * y[i];
                            var tmp = c[i] * val;
                            G_loss -= tmp * b[i];
                            H += tmp * val;
                        }
                    }
                }

                G_loss *= 2;

                H *= 2;
                H = System.Math.Max(H, 1e-12);
            }

            private int LineSearch(int j, ref double d, double G, double G_loss)
            {
                double d_old, d_diff;
                double loss_old = 0, loss_new;
                double appxcond, cond;

                var delta = System.Math.Abs(w[j] + d) - System.Math.Abs(w[j]) + G * d;
                d_old = 0;

                int num_linesearch;
                for (num_linesearch = 0; num_linesearch < max_num_linesearch; num_linesearch++)
                {
                    d_diff = d_old - d;
                    cond = System.Math.Abs(w[j] + d) - System.Math.Abs(w[j]) - sigma * delta;

                    appxcond = xj_sq[j] * d * d + G_loss * d + cond;

                    if (appxcond <= 0)
                    {
                        ApplyStepToB(j, d_diff);

                        break;
                    }

                    if (num_linesearch == 0)
                    {
                        loss_old = 0;
                        loss_new = 0;

                        UpdateBAndLossesFirst(j, d_diff, ref loss_old, ref loss_new);
                    }
                    else
                    {
                        loss_new = 0;

                        UpdateBAndLoss(j, d_diff, ref loss_new);
                    }

                    cond = cond + loss_new - loss_old;

                    if (cond <= 0)
                    {
                        break;
                    }

                    d_old = d;
                    d *= 0.5;
                    delta *= 0.5;
                }

                return num_linesearch;
            }

            private void ApplyStepToB(int j, double d_diff)
            {
                if (j == biasIndex)
                {
                    for (var i = 0; i < b.Length; i++)
                    {
                        b[i] += d_diff * bias[i];
                    }
                }
                else
                {
                    for (var i = 0; i < b.Length; i++)
                    {
                        b[i] += d_diff * x[i][j] * y[i];
                    }
                }
            }

            private void UpdateBAndLossesFirst(int j, double d_diff, ref double loss_old, ref double loss_new)
            {
                if (j == biasIndex)
                {
                    for (var i = 0; i < x.Length; i++)
                    {
                        if (b[i] > 0)
                        {
                            loss_old += c[i] * b[i] * b[i];
                        }

                        var b_new = b[i] + d_diff * bias[i];
                        b[i] = b_new;

                        if (b_new > 0)
                        {
                            loss_new += c[i] * b_new * b_new;
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < x.Length; i++)
                    {
                        if (b[i] > 0)
                        {
                            loss_old += c[i] * b[i] * b[i];
                        }

                        var b_new = b[i] + d_diff * x[i][j] * y[i];

                        b[i] = b_new;

                        if (b_new > 0)
                        {
                            loss_new += c[i] * b_new * b_new;
                        }
                    }
                }
            }

            private void UpdateBAndLoss(int j, double d_diff, ref double loss_new)
            {
                if (j == biasIndex)
                {
                    for (var i = 0; i < x.Length; i++)
                    {
                        var b_new = b[i] + d_diff * bias[i];

                        b[i] = b_new;

                        if (b_new > 0)
                        {
                            loss_new += c[i] * b_new * b_new;
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < x.Length; i++)
                    {
                        var b_new = b[i] + d_diff * x[i][j] * y[i];
                        b[i] = b_new;

                        if (b_new > 0)
                        {
                            loss_new += c[i] * b_new * b_new;
                        }
                    }
                }
            }

            private void RecomputeB()
            {
                for (var i = 0; i < b.Length; i++)
                {
                    b[i] = 1;
                }

                for (var i = 0; i < w.Length; i++)
                {
                    if (w[i] == 0)
                    {
                        continue;
                    }

                    if (i == biasIndex)
                    {
                        for (var k = 0; k < x.Length; k++)
                        {
                            b[k] -= w[i] * bias[k];
                        }
                    }
                    else
                    {
                        for (var k = 0; k < x.Length; k++)
                        {
                            b[k] -= w[i] * x[k][i];
                        }
                    }
                }
            }

            public void TraceObjective()
            {
                // calculate objective value

                double v = 0;
                var nnz = 0;
                for (var j = 0; j < w.Length; j++)
                {
                    if (j == biasIndex)
                    {
                        for (var i = 0; i < x.Length; i++)
                        {
                            bias[i] *= y[i]; // restore x->value
                        }
                    }

                    if (w[j] != 0)
                    {
                        v += System.Math.Abs(w[j]);
                        nnz++;
                    }
                }

                for (var j = 0; j < c.Length; j++)
                {
                    if (b[j] > 0)
                    {
                        v += c[j] * b[j] * b[j];
                    }
                }

                Debug.WriteLine("Objective value = " + v);
                Debug.WriteLine("#nonzeros/#features = " + nnz + "/" + w.Length);
            }
        }
    }
}