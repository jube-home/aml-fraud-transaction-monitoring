// Accord Math Library
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
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//
//
// The source code presented in this file has been adapted from LibSVM - 
// A Library for Support Vector Machines, created by Chih-Chung Chang and
// Chih-Jen Lin. Original license is given below.
//
//    Copyright (c) 2000-2014 Chih-Chung Chang and Chih-Jen Lin
//    All rights reserved.
//    
//    Redistribution and use in source and binary forms, with or without
//    modification, are permitted provided that the following conditions
//    are met:
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
//    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//    ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//    LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//    A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR
//    CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//    EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//    PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//    PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//    LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//    SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

using System;
using System.Diagnostics;

namespace Accord.Math.Optimization
{
    public partial class FanChenLinQuadraticOptimization
    {
        private void InitializeAlphaStatusActiveSetAndIndices()
        {
            // initialize alpha_status
            {
                for (var i = 0; i < l; i++)
                {
                    update_alpha_status(i);
                }
            }

            // initialize active set (for shrinking)
            {
                for (var i = 0; i < l; i++)
                {
                    active_set[i] = i;
                }

                active_size = l;
            }

            // initialize index lookup vector
            {
                for (var i = 0; i < indices.Length; i++)
                {
                    indices[i] = i;
                }
            }
        }

        private void InitializeGradient(double[] Q_i)
        {
            // initialize gradient
            {
                for (var i = 0; i < l; i++)
                {
                    G[i] = p[i];
                    G_bar[i] = 0;
                }

                for (var i = 0; i < l; i++)
                {
                    if (!is_lower_bound(i))
                    {
                        Q(i, indices, l, Q_i);

                        var alpha_i = alpha[i];
                        for (var j = 0; j < l; j++)
                        {
                            G[j] += alpha_i * Q_i[j];
                        }

                        if (is_upper_bound(i))
                        {
                            for (var j = 0; j < l; j++)
                            {
                                G_bar[j] += C[i] * Q_i[j];
                            }
                        }
                    }
                }
            }
        }

        private void ShrinkPeriodically(ref int counter)
        {
            // show progress and do shrinking

            if (--counter == 0)
            {
                counter = System.Math.Min(l, 1000);
                if (Shrinking)
                {
                    do_shrinking();
                }

                Trace.WriteLine(".");
            }
        }

        private bool SelectWorkingPair(ref int counter, ref int i, ref int j)
        {
            if (select_working_set(ref i, ref j) != 0)
            {
                // reconstruct the whole gradient
                reconstruct_gradient();

                // reset active set size and check
                active_size = l;
                Trace.WriteLine("*");
                if (select_working_set(ref i, ref j) != 0)
                {
                    return false;
                }

                counter = 1; // do shrinking next iteration
            }

            return true;
        }

        private void OptimizeDifferentLabels(int i, int j, double C_i, double C_j, double[] Q_i)
        {
            var quad_coef = QD[i] + QD[j] + 2 * Q_i[j];
            if (quad_coef <= 0)
            {
                quad_coef = TAU;
            }

            var delta = (-G[i] - G[j]) / quad_coef;
            var diff = alpha[i] - alpha[j];
            alpha[i] += delta;
            alpha[j] += delta;

            if (diff > 0)
            {
                if (alpha[j] < 0)
                {
                    alpha[j] = 0;
                    alpha[i] = diff;
                }
            }
            else
            {
                if (alpha[i] < 0)
                {
                    alpha[i] = 0;
                    alpha[j] = -diff;
                }
            }

            if (diff > C_i - C_j)
            {
                if (alpha[i] > C_i)
                {
                    alpha[i] = C_i;
                    alpha[j] = C_i - diff;
                }
            }
            else
            {
                if (alpha[j] > C_j)
                {
                    alpha[j] = C_j;
                    alpha[i] = C_j + diff;
                }
            }
        }

        private void OptimizeSameLabels(int i, int j, double C_i, double C_j, double[] Q_i)
        {
            var quad_coef = QD[i] + QD[j] - 2 * Q_i[j];
            if (quad_coef <= 0)
            {
                quad_coef = TAU;
            }

            var delta = (G[i] - G[j]) / quad_coef;
            var sum = alpha[i] + alpha[j];
            alpha[i] -= delta;
            alpha[j] += delta;

            if (sum > C_i)
            {
                if (alpha[i] > C_i)
                {
                    alpha[i] = C_i;
                    alpha[j] = sum - C_i;
                }
            }
            else
            {
                if (alpha[j] < 0)
                {
                    alpha[j] = 0;
                    alpha[i] = sum;
                }
            }

            if (sum > C_j)
            {
                if (alpha[j] > C_j)
                {
                    alpha[j] = C_j;
                    alpha[i] = sum - C_j;
                }
            }
            else
            {
                if (alpha[i] < 0)
                {
                    alpha[i] = 0;
                    alpha[j] = sum;
                }
            }
        }

        private void UpdateGradient(int i, int j, double[] Q_i, double[] Q_j, double old_alpha_i, double old_alpha_j)
        {
            // update G
            var delta_alpha_i = alpha[i] - old_alpha_i;
            var delta_alpha_j = alpha[j] - old_alpha_j;

            for (var k = 0; k < active_size; k++)
            {
                G[k] += Q_i[k] * delta_alpha_i + Q_j[k] * delta_alpha_j;
            }
        }

        private void UpdateAlphaStatusAndGBar(int i, int j, double C_i, double C_j, double[] Q_i, double[] Q_j)
        {
            // update alpha_status and G_bar
            var ui = is_upper_bound(i);
            var uj = is_upper_bound(j);
            update_alpha_status(i);
            update_alpha_status(j);

            if (ui != is_upper_bound(i))
            {
                Q(i, indices, l, Q_i);

                if (ui)
                {
                    for (var k = 0; k < l; k++)
                    {
                        G_bar[k] -= C_i * Q_i[k];
                    }
                }
                else
                {
                    for (var k = 0; k < l; k++)
                    {
                        G_bar[k] += C_i * Q_i[k];
                    }
                }
            }

            if (uj != is_upper_bound(j))
            {
                Q(j, indices, l, Q_j);

                if (uj)
                {
                    for (var k = 0; k < l; k++)
                    {
                        G_bar[k] -= C_j * Q_j[k];
                    }
                }
                else
                {
                    for (var k = 0; k < l; k++)
                    {
                        G_bar[k] += C_j * Q_j[k];
                    }
                }
            }
        }

        private void FinalizeSolution(int iter, int max_iter)
        {
            if (iter >= max_iter)
            {
                if (active_size < l)
                {
                    // reconstruct the whole gradient to calculate objective value
                    reconstruct_gradient();
                    active_size = l;
                    Trace.WriteLine("*");
                }

                Trace.WriteLine("WARNING: reaching max number of iterations");
            }

            // calculate rho
            Rho = calculate_rho();

            // calculate objective value
            {
                double v = 0;
                for (var i = 0; i < l; i++)
                {
                    v += alpha[i] * (G[i] + p[i]);
                }

                Value = v / 2;
            }

            // put back the solution
            {
                var solution = (double[])alpha.Clone();
                Array.Clear(alpha, 0, alpha.Length);
                for (var i = 0; i < l; i++)
                {
                    alpha[active_set[i]] = solution[i];
                }
            }

            // juggle everything back
            /*{
                for(int i=0;i<l;i++)
                    while(active_set[i] != i)
                        swap_index(i,active_set[i]);
                        // or Q.swap_index(i,active_set[i]);
            }*/

            Trace.WriteLine("optimization finished, #iter = " + iter);
        }
    }
}