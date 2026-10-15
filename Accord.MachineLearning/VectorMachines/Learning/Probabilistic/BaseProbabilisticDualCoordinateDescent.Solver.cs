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

namespace Accord.MachineLearning.VectorMachines.Learning
{
    public abstract partial class BaseProbabilisticDualCoordinateDescent<TModel, TKernel, TInput>
    {
        private sealed class LogisticDualSolver
        {
            private readonly double[] alpha; // store alpha and C - alpha
            private readonly int biasIndex;
            private readonly double eps;
            private readonly int[] index;
            private readonly double innereps_min;
            private readonly TInput[] inputs;
            private readonly int max_inner_iter;
            private readonly int max_iter;
            private readonly Random rand;
            private readonly CancellationToken token;
            private readonly double[] upper_bound;
            private readonly double[] w;
            private readonly double[] xTx;
            private readonly int[] y;

            private double innereps = 1e-2;

            private int iter;
            private TKernel kernel;

            public LogisticDualSolver(TKernel kernel, TInput[] inputs, int[] y, double[] w, int biasIndex,
                double[] upper_bound, Random rand, CancellationToken token, double eps, int max_iter,
                int max_inner_iter)
            {
                this.kernel = kernel;
                this.inputs = inputs;
                this.y = y;
                this.w = w;
                this.biasIndex = biasIndex;
                this.upper_bound = upper_bound;
                this.rand = rand;
                this.token = token;
                this.eps = eps;
                this.max_iter = max_iter;
                this.max_inner_iter = max_inner_iter;

                xTx = new double[inputs.Length];
                index = new int[inputs.Length];
                alpha = new double[2 * inputs.Length];
                innereps_min = System.Math.Min(1e-8, eps);
            }

            public void Initialize()
            {
                // Initial alpha can be set here. Note that
                // 0 < alpha[i] < upper_bound[GETI(i)]
                // alpha[2*i] + alpha[2*i+1] = upper_bound[GETI(i)]

                for (var i = 0; i < upper_bound.Length; i++)
                {
                    alpha[2 * i] = System.Math.Min(0.001 * upper_bound[i], 1e-8);
                    alpha[2 * i + 1] = upper_bound[i] - alpha[2 * i];
                }

                for (var i = 0; i < inputs.Length; i++)
                {
                    index[i] = i;
                    xTx[i] = 1;

                    var xi = inputs[i];

                    xTx[i] += kernel.Function(xi, xi);
                    kernel.Product(y[i] * alpha[2 * i], xi, w);
                    w[biasIndex] += y[i] * alpha[2 * i];
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

                    ShuffleIndex();

                    var newton_iter = 0;
                    double Gmax = 0;

                    for (var s = 0; s < index.Length; s++)
                    {
                        UpdateCoordinate(index[s], ref Gmax, ref newton_iter);
                    }

                    iter++;
                    if (iter % 10 == 0)
                    {
                        Trace.Write(".");
                    }

                    if (Gmax < eps)
                    {
                        break;
                    }

                    if (newton_iter <= inputs.Length / 10)
                    {
                        innereps = System.Math.Max(innereps_min, 0.1 * innereps);
                    }
                }

                Trace.WriteLine("optimization finished, #iter = " + iter);
                if (iter >= max_iter)
                {
                    Trace.WriteLine("WARNING: reaching max number of iterations");
                    Trace.WriteLine("Using -s 0 may be faster (also see FAQ)");
                }
            }

            private void ShuffleIndex()
            {
                for (var i = 0; i < inputs.Length; i++)
                {
                    var j = i + rand.Next() % (inputs.Length - i);

                    var t = index[i];
                    index[i] = index[j];
                    index[j] = t;
                }
            }

            private void UpdateCoordinate(int i, ref double Gmax, ref int newton_iter)
            {
                var yi = y[i];
                var C = upper_bound[i];

                var xisq = xTx[i];
                var ywTx = y[i] * (kernel.Function(w, inputs[i]) + w[biasIndex]);

                var a = xisq;
                var b = ywTx;

                // Decide to minimize g_1(z) or g_2(z)
                var ind1 = 2 * i;
                var ind2 = 2 * i + 1;
                var sign = 1;

                if (0.5 * a * (alpha[ind2] - alpha[ind1]) + b < 0)
                {
                    ind1 = 2 * i + 1;
                    ind2 = 2 * i;
                    sign = -1;
                }

                //  g_t(z) = z*log(z) + (C-z)*log(C-z) + 0.5a(z-alpha_old)^2 + sign*b(z-alpha_old)
                var alpha_old = alpha[ind1];
                var z = alpha_old;

                if (C - z < 0.5 * C)
                {
                    z = 0.1 * z;
                }

                var gp = a * (z - alpha_old) + sign * b + System.Math.Log(z / (C - z));
                Gmax = System.Math.Max(Gmax, System.Math.Abs(gp));

                var inner_iter = SolveSubproblem(a, b, C, sign, alpha_old, ref z, gp, ref newton_iter);

                if (inner_iter > 0) // update w
                {
                    alpha[ind1] = z;
                    alpha[ind2] = C - z;

                    kernel.Product(sign * (z - alpha_old) * yi, inputs[i], w);
                    w[biasIndex] += sign * (z - alpha_old) * yi;
                }
            }

            private int SolveSubproblem(double a, double b, double C, int sign, double alpha_old, ref double z,
                double gp, ref int newton_iter)
            {
                // Newton method on the sub-problem
                const double eta = 0.1; // xi in the paper

                var inner_iter = 0;
                while (inner_iter <= max_inner_iter)
                {
                    if (System.Math.Abs(gp) < innereps)
                    {
                        break;
                    }

                    var gpp = a + C / (C - z) / z;
                    var tmpz = z - gp / gpp;

                    if (tmpz <= 0)
                    {
                        z *= eta;
                    }
                    else // tmpz in (0, C)
                    {
                        z = tmpz;
                    }

                    gp = a * (z - alpha_old) + sign * b + System.Math.Log(z / (C - z));
                    newton_iter++;
                    inner_iter++;
                }

                return inner_iter;
            }

            public void TraceObjective()
            {
                // calculate objective value

                double v = 0;
                for (var i = 0; i < w.Length; i++)
                {
                    v += w[i] * w[i];
                }

                v *= 0.5;

                for (var i = 0; i < upper_bound.Length; i++)
                {
                    v += alpha[2 * i] * System.Math.Log(alpha[2 * i])
                         + alpha[2 * i + 1] * System.Math.Log(alpha[2 * i + 1])
                         - upper_bound[i] * System.Math.Log(upper_bound[i]);
                }

                Trace.WriteLine("Objective value = " + v);
            }
        }
    }
}