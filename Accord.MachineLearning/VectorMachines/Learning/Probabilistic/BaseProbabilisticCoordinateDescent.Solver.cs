// Accord Statistics Library
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
// Copyright (c) 2007-2011 The LIBLINEAR Project.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
//   1. Redistributions of source code must retain the above copyright
//   notice, this list of conditions and the following disclaimer.
//
//   2. Redistributions in binary form must reproduce the above copyright
//   notice, this list of conditions and the following disclaimer in the
//   documentation and/or other materials provided with the distribution.
//
//   3. Neither name of copyright holders nor the names of its contributors
//   may be used to endorse or promote products derived from this software
//   without specific prior written permission.
//
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

using System;
using System.Diagnostics;
using System.Threading;
using Accord.Math.Random;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    public abstract partial class BaseProbabilisticCoordinateDescent<TModel, TKernel, TInput>
    {
        private sealed class L1LogisticSolver
        {
            private readonly double[] D;
            private readonly double[] Grad;
            private readonly double[] Hdiag;
            private readonly double[] c;
            private readonly double eps;
            private readonly double[] exp_wTx;
            private readonly double[] exp_wTx_new;

            private readonly int[] index;
            private readonly double[][] inputs;
            private readonly int max_iter;
            private readonly int max_newton_iter;
            private readonly int max_num_linesearch;

            private readonly double nu = 1e-12;
            private readonly double sigma = 0.01;
            private readonly double[] tau;
            private readonly CancellationToken token;
            private readonly double[] w;
            private readonly double[] wpd;

            private readonly double[] xTd;
            private readonly double[] xjneg_sum;
            private readonly int[] y;
            private double Gmax_new, Gnorm1_new;
            private double Gmax_old = double.PositiveInfinity;
            private double Gnorm1_init;
            private double QP_Gmax_old = double.PositiveInfinity;
            private int QP_active_size;
            private int active_size;
            private double delta, negsum_xTd;
            private double inner_eps = 1;
            private int iter;

            private int newton_iter;
            private double w_norm, w_norm_new;

            public L1LogisticSolver(int samples, int parameters, int[] y, double[] w, double[][] inputs, double[] c,
                CancellationToken token, double eps, int max_iter, int max_newton_iter, int max_num_linesearch)
            {
                this.y = y;
                this.w = w;
                this.inputs = inputs;
                this.c = c;
                this.token = token;
                this.eps = eps;
                this.max_iter = max_iter;
                this.max_newton_iter = max_newton_iter;
                this.max_num_linesearch = max_num_linesearch;

                index = new int[parameters];
                Hdiag = new double[parameters];
                Grad = new double[parameters];
                wpd = new double[parameters];
                xjneg_sum = new double[parameters];

                xTd = new double[samples];
                exp_wTx = new double[samples];
                exp_wTx_new = new double[samples];
                tau = new double[samples];
                D = new double[samples];
            }

            public void Initialize()
            {
                for (var i = 0; i < exp_wTx.Length; i++)
                {
                    exp_wTx[i] = 0;
                }

                w_norm = 0;
                for (var j = 0; j < w.Length; j++)
                {
                    w_norm += System.Math.Abs(w[j]);
                    wpd[j] = w[j];
                    index[j] = j;
                    xjneg_sum[j] = 0;

                    var x = inputs[j];

                    for (var k = 0; k < x.Length; k++)
                    {
                        exp_wTx[k] += w[j] * x[k];

                        if (y[k] == -1)
                        {
                            xjneg_sum[j] += c[k] * x[k];
                        }
                    }
                }


                for (var j = 0; j < exp_wTx.Length; j++)
                {
                    exp_wTx[j] = System.Math.Exp(exp_wTx[j]);

                    var tau_tmp = 1 / (1 + exp_wTx[j]);
                    tau[j] = c[j] * tau_tmp;
                    D[j] = c[j] * exp_wTx[j] * tau_tmp * tau_tmp;
                }
            }

            public void Optimize()
            {
                while (newton_iter < max_newton_iter)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    ComputeOuterGradient();

                    if (newton_iter == 0)
                    {
                        Gnorm1_init = Gnorm1_new;
                    }

                    if (Gnorm1_new <= eps * Gnorm1_init)
                    {
                        break;
                    }

                    iter = 0;
                    QP_Gmax_old = double.PositiveInfinity;
                    QP_active_size = active_size;

                    for (var i = 0; i < xTd.Length; i++)
                    {
                        xTd[i] = 0;
                    }

                    SolveQuadraticSubproblem();

                    if (iter >= max_iter)
                    {
                        Trace.WriteLine("WARNING: reaching max number of inner iterations");
                    }

                    ComputeDelta();

                    var num_linesearch = LineSearch();

                    // Recompute some info due to too many line search steps
                    if (num_linesearch >= max_num_linesearch)
                    {
                        RecomputeExpWTx();
                    }

                    if (iter == 1)
                    {
                        inner_eps *= 0.25;
                    }

                    newton_iter++;
                    Gmax_old = Gmax_new;

                    Trace.WriteLine("iter = " + newton_iter + "_#CD cycles " + iter);
                }

                Trace.WriteLine("=========================");
                Trace.WriteLine("optimization finished, #iter = " + newton_iter);

                if (newton_iter >= max_newton_iter)
                {
                    Trace.WriteLine("WARNING: reaching max number of iterations");
                }
            }

            private void ComputeOuterGradient()
            {
                Gmax_new = 0;
                Gnorm1_new = 0;
                active_size = w.Length;

                for (var s = 0; s < active_size; s++)
                {
                    var j = index[s];
                    Hdiag[j] = nu;
                    Grad[j] = 0;

                    double tmp = 0;
                    var x = inputs[j];

                    for (var k = 0; k < x.Length; k++)
                    {
                        Hdiag[j] += x[k] * x[k] * D[k];
                        tmp += x[k] * tau[k];
                    }


                    Grad[j] = -tmp + xjneg_sum[j];

                    var Gp = Grad[j] + 1;
                    var Gn = Grad[j] - 1;
                    double violation = 0;

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

                        //outer-level shrinking
                        else if (Gp > Gmax_old / x.Length && Gn < -Gmax_old / x.Length)
                        {
                            active_size--;
                            var t = index[s];
                            index[s] = index[active_size];
                            index[active_size] = t;
                            s--;
                            continue;
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

                    Gmax_new = System.Math.Max(Gmax_new, violation);
                    Gnorm1_new += violation;
                }
            }

            private void SolveQuadraticSubproblem()
            {
                double QP_Gmax_new, QP_Gnorm1_new;

                var rand = Generator.Random;

                // optimize QP over wpd
                while (iter < max_iter)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    QP_Gmax_new = 0;
                    QP_Gnorm1_new = 0;

                    ShuffleActiveSet(rand);

                    for (var s = 0; s < QP_active_size; s++)
                    {
                        if (UpdateQuadraticCoordinate(s, ref QP_Gmax_new, ref QP_Gnorm1_new))
                        {
                            s--;
                        }
                    }

                    iter++;

                    if (QP_Gnorm1_new <= inner_eps * Gnorm1_init)
                    {
                        //inner stopping
                        if (QP_active_size == active_size)
                        {
                            break;
                        }

                        //active set reactivation
                        QP_active_size = active_size;
                        QP_Gmax_old = double.PositiveInfinity;
                        continue;
                    }

                    QP_Gmax_old = QP_Gmax_new;
                }
            }

            private void ShuffleActiveSet(Random rand)
            {
                for (var j = 0; j < QP_active_size; j++)
                {
                    var i = j + rand.Next() % (QP_active_size - j);
                    var t = index[i];
                    index[i] = index[j];
                    index[j] = t;
                }
            }

            private bool UpdateQuadraticCoordinate(int s, ref double QP_Gmax_new, ref double QP_Gnorm1_new)
            {
                double z, G, H;

                var j = index[s];
                H = Hdiag[j];

                var x = inputs[j];
                G = Grad[j] + (wpd[j] - w[j]) * nu;

                for (var k = 0; k < x.Length; k++)
                {
                    G += x[k] * D[k] * xTd[k];
                }

                var Gp = G + 1;
                var Gn = G - 1;
                double violation = 0;

                if (wpd[j] == 0)
                {
                    if (Gp < 0)
                    {
                        violation = -Gp;
                    }

                    else if (Gn > 0)
                    {
                        violation = Gn;
                    }

                    //inner-level shrinking
                    else if (Gp > QP_Gmax_old / inputs.Length && Gn < -QP_Gmax_old / inputs.Length)
                    {
                        QP_active_size--;
                        var t = index[s];
                        index[s] = index[QP_active_size];
                        index[QP_active_size] = t;
                        return true;
                    }
                }
                else if (wpd[j] > 0)
                {
                    violation = System.Math.Abs(Gp);
                }
                else
                {
                    violation = System.Math.Abs(Gn);
                }

                QP_Gmax_new = System.Math.Max(QP_Gmax_new, violation);
                QP_Gnorm1_new += violation;

                // obtain solution of one-variable problem
                if (Gp < H * wpd[j])
                {
                    z = -Gp / H;
                }
                else if (Gn > H * wpd[j])
                {
                    z = -Gn / H;
                }
                else
                {
                    z = -wpd[j];
                }

                if (System.Math.Abs(z) < 1.0e-12)
                {
                    return false;
                }

                z = System.Math.Min(System.Math.Max(z, -10.0), 10.0);

                wpd[j] += z;

                x = inputs[j];

                for (var k = 0; k < x.Length; k++)
                {
                    xTd[k] += x[k] * z;
                }

                return false;
            }

            private void ComputeDelta()
            {
                delta = 0;
                w_norm_new = 0;
                for (var j = 0; j < w.Length; j++)
                {
                    delta += Grad[j] * (wpd[j] - w[j]);

                    if (wpd[j] != 0)
                    {
                        w_norm_new += System.Math.Abs(wpd[j]);
                    }
                }

                delta += w_norm_new - w_norm;

                negsum_xTd = 0;
                for (var i = 0; i < y.Length; i++)
                {
                    if (y[i] == -1)
                    {
                        negsum_xTd += c[i] * xTd[i];
                    }
                }
            }

            private int LineSearch()
            {
                double cond;
                int num_linesearch;
                for (num_linesearch = 0; num_linesearch < max_num_linesearch; num_linesearch++)
                {
                    cond = w_norm_new - w_norm + negsum_xTd - sigma * delta;

                    for (var i = 0; i < xTd.Length; i++)
                    {
                        var exp_xTd = System.Math.Exp(xTd[i]);
                        exp_wTx_new[i] = exp_wTx[i] * exp_xTd;
                        cond += c[i] * System.Math.Log((1 + exp_wTx_new[i]) / (exp_xTd + exp_wTx_new[i]));
                    }

                    if (cond <= 0)
                    {
                        w_norm = w_norm_new;
                        for (var j = 0; j < w.Length; j++)
                        {
                            w[j] = wpd[j];
                        }

                        for (var i = 0; i < y.Length; i++)
                        {
                            exp_wTx[i] = exp_wTx_new[i];

                            var tau_tmp = 1 / (1 + exp_wTx[i]);
                            tau[i] = c[i] * tau_tmp;
                            D[i] = c[i] * exp_wTx[i] * tau_tmp * tau_tmp;
                        }

                        break;
                    }

                    w_norm_new = 0;
                    for (var j = 0; j < w.Length; j++)
                    {
                        wpd[j] = (w[j] + wpd[j]) * 0.5;

                        if (wpd[j] != 0)
                        {
                            w_norm_new += System.Math.Abs(wpd[j]);
                        }
                    }

                    delta *= 0.5;
                    negsum_xTd *= 0.5;
                    for (var i = 0; i < xTd.Length; i++)
                    {
                        xTd[i] *= 0.5;
                    }
                }

                return num_linesearch;
            }

            private void RecomputeExpWTx()
            {
                for (var i = 0; i < exp_wTx.Length; i++)
                {
                    exp_wTx[i] = 0;
                }

                for (var i = 0; i < w.Length; i++)
                {
                    if (w[i] == 0)
                    {
                        continue;
                    }

                    var x = inputs[i];
                    for (var k = 0; k < x.Length; k++)
                    {
                        exp_wTx[k] += w[i] * x[k];
                    }
                }

                for (var i = 0; i < exp_wTx.Length; i++)
                {
                    exp_wTx[i] = System.Math.Exp(exp_wTx[i]);
                }
            }

            public void TraceObjective()
            {
                // calculate objective value

                double v = 0;
                var nnz = 0;
                for (var j = 0; j < w.Length; j++)
                {
                    if (w[j] != 0)
                    {
                        v += System.Math.Abs(w[j]);
                        nnz++;
                    }
                }

                for (var j = 0; j < y.Length; j++)
                {
                    if (y[j] == 1)
                    {
                        v += c[j] * System.Math.Log(1 + 1 / exp_wTx[j]);
                    }
                    else
                    {
                        v += c[j] * System.Math.Log(1 + exp_wTx[j]);
                    }
                }

                Trace.WriteLine("Objective value = " + v);
                Trace.WriteLine("#nonzeros/#features = " + nnz + "/" + w.Length);
            }
        }
    }
}