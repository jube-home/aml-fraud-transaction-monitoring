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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Accord.Math.Optimization
{
    using QFunc = Func<int, int[], int, double[], double[]>;

    /// <summary>
    ///     General Sequential Minimal Optimization algorithm for Quadratic Programming problems.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements the same optimization method found in LibSVM. It can be used
    ///         to solve quadratic programming problems where the quadratic matrix Q may be too large
    ///         to fit in memory.
    ///     </para>
    ///     <para>
    ///         The method is described in Fan et al., JMLR 6(2005), p. 1889--1918. It solves the
    ///         minimization problem:
    ///     </para>
    ///     <code>
    ///    min 0.5(\alpha^T Q \alpha) + p^T \alpha
    /// 
    ///      y^T \alpha = \eps
    ///      y_i = +1 or -1
    ///      0 &lt;= alpha_i &lt;= C_i
    /// </code>
    ///     <para>
    ///         Given Q, p, y, C, and an initial feasible point \alpha, where l is
    ///         the size of vectors and matrices and eps is the stopping tolerance.
    ///     </para>
    ///     <para>
    ///         The <c>Q</c>, <c>p</c> and <c>y</c> parameters can be given as arguments of its
    ///         <see cref="FanChenLinQuadraticOptimization(int, QFunc)">
    ///             class consstructor
    ///         </see>
    ///         . The C parameters can be set using in the <see cref="UpperBounds" />
    ///         property. The <c>eps</c> parameter can be set using the <see cref="Tolerance" /> property.
    ///     </para>
    /// </remarks>
    public partial class FanChenLinQuadraticOptimization : IOptimizationMethod
    {
        private const double TAU = 1e-12;
        private double[] C;
        private double[] G; // gradient of objective function
        private double[] G_bar; // gradient, if we treat free variables as 0
        private QFunc Q;
        private double[] QD; // diagonal
        private int[] active_set;

        private int active_size;
        private double[] alpha;

        private Status[] alpha_status;
        private int[] indices;
        private int l;
        private double[] p;

        private double[] temp;
        private bool unshrink;
        private int[] y;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FanChenLinQuadraticOptimization" /> class.
        /// </summary>
        /// <param name="numberOfVariables">The number of free parameters in the optimization problem.</param>
        /// <param name="Q">
        ///     The quadratic matrix Q. It should be specified as a lambda
        ///     function so Q doesn't need to be always kept in memory.
        /// </param>
        public FanChenLinQuadraticOptimization(int numberOfVariables, QFunc Q)
        {
            var zeros = new double[numberOfVariables];
            var ones = new int[numberOfVariables];
            for (var i = 0; i < ones.Length; i++)
            {
                ones[i] = 1;
            }

            initialize(numberOfVariables, Q, zeros, ones);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FanChenLinQuadraticOptimization" /> class.
        /// </summary>
        /// <param name="numberOfVariables">The number of free parameters in the optimization problem.</param>
        /// <param name="Q">
        ///     The quadratic matrix Q. It should be specified as a lambda
        ///     function so Q doesn't need to be always kept in memory.
        /// </param>
        /// <param name="p">The vector of linear terms p. Default is a zero vector.</param>
        /// <param name="y">The class labels y. Default is a unit vector.</param>
        public FanChenLinQuadraticOptimization(int numberOfVariables, QFunc Q, double[] p, int[] y)
        {
            initialize(numberOfVariables, Q, p, y);
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        [field: NonSerialized]
        public CancellationToken Token { get; set; }

        /// <summary>
        ///     Gets the threshold (bias) value for a SVM trained using this method.
        /// </summary>
        public double Rho { get; private set; }

        /// <summary>
        ///     Gets or sets the precision tolerance before
        ///     the method stops. Default is 0.001.
        /// </summary>
        public double Tolerance { get; set; } = 0.001;

        /// <summary>
        ///     Gets or sets a value indicating whether shrinking
        ///     heuristics should be used. Default is false.
        /// </summary>
        /// <value>
        ///     <c>true</c> to use shrinking heuristics; otherwise, <c>false</c>.
        /// </value>
        public bool Shrinking { get; set; }

        /// <summary>
        ///     Gets the upper bounds for the optimization problem. In
        ///     a SVM learning problem, this would be the capacity limit
        ///     for each Lagrange multiplier (alpha) in the machine. The
        ///     default is to use a vector filled with 1's.
        /// </summary>
        public double[] UpperBounds
        {
            get => C;
            set
            {
                if (value.Length != NumberOfVariables)
                {
                    throw new DimensionMismatchException("value");
                }

                C = value;
            }
        }

        /// <summary>
        ///     Gets the number of variables (free parameters) in the optimization
        ///     problem. In a SVM learning problem, this is the number of samples in
        ///     the learning dataset.
        /// </summary>
        /// <value>
        ///     The number of parameters for the optimization problem.
        /// </value>
        public int NumberOfVariables
        {
            get => l;
            set => setNumberOfVariables(value);
        }

        /// <summary>
        ///     Gets the current solution found, the values of
        ///     the parameters which optimizes the function.
        /// </summary>
        public double[] Solution
        {
            get => alpha;
            set
            {
                if (value.Length != NumberOfVariables)
                {
                    throw new DimensionMismatchException("value");
                }

                alpha = value;
            }
        }

        /// <summary>
        ///     Gets the output of the function at the current <see cref="Solution" />.
        /// </summary>
        public double Value { get; private set; }

        /// <summary>
        ///     Finds the minimum value of a function. The solution vector
        ///     will be made available at the <see cref="Solution" /> property.
        /// </summary>
        /// <returns>
        ///     Returns <c>true</c> if the method converged to a <see cref="Solution" />.
        ///     In this case, the found value will also be available at the <see cref="Value" />
        ///     property.
        /// </returns>
        public bool Minimize()
        {
            temp = new double[l];
            QD = new double[l];
            for (var k = 0; k < QD.Length; k++)
            {
                QD[k] = Q(k, new[] { k }, 1, temp)[0];
            }

            var Q_i = new double[l];
            var Q_j = new double[l];

            unshrink = false;

            InitializeAlphaStatusActiveSetAndIndices();

            InitializeGradient(Q_i);

            // optimization step
            var iter = 0;
            var max_iter = System.Math.Max(10000000, l > int.MaxValue / 100 ? int.MaxValue : 100 * l);
            var counter = System.Math.Min(l, 1000) + 1;

            while (iter < max_iter)
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }

                // show progress and do shrinking
                ShrinkPeriodically(ref counter);

                int i = 0, j = 0;
                if (!SelectWorkingPair(ref counter, ref i, ref j))
                {
                    break;
                }

                ++iter;

                // update alpha[i] and alpha[j], handle bounds carefully
                Q(i, indices, active_size, Q_i);
                Q(j, indices, active_size, Q_j);

                var C_i = C[i];
                var C_j = C[j];

                var old_alpha_i = alpha[i];
                var old_alpha_j = alpha[j];

                if (y[i] != y[j])
                {
                    OptimizeDifferentLabels(i, j, C_i, C_j, Q_i);
                }
                else
                {
                    OptimizeSameLabels(i, j, C_i, C_j, Q_i);
                }

                UpdateGradient(i, j, Q_i, Q_j, old_alpha_i, old_alpha_j);

                UpdateAlphaStatusAndGBar(i, j, C_i, C_j, Q_i, Q_j);
            }

            FinalizeSolution(iter, max_iter);

            return iter < max_iter;
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public bool Maximize()
        {
            throw new NotSupportedException();
        }

        private void initialize(int numberOfVariables, QFunc Q, double[] p, int[] y)
        {
            setNumberOfVariables(numberOfVariables);

            this.Q = Q;
            this.p = p;
            this.y = y;
        }

        private void setNumberOfVariables(int numberOfVariables)
        {
            l = numberOfVariables;
            indices = new int[l];
            alpha_status = new Status[l];
            active_set = new int[l];
            G = new double[l];
            G_bar = new double[l];
            alpha = new double[l];
            C = new double[l];
            for (var i = 0; i < C.Length; i++)
            {
                C[i] = 1.0;
            }
        }

        private void update_alpha_status(int i)
        {
            if (alpha[i] >= C[i])
            {
                alpha_status[i] = Status.UPPER_BOUND;
            }
            else if (alpha[i] <= 0)
            {
                alpha_status[i] = Status.LOWER_BOUND;
            }
            else
            {
                alpha_status[i] = Status.FREE;
            }
        }

        private bool is_upper_bound(int i)
        {
            return alpha_status[i] == Status.UPPER_BOUND;
        }

        private bool is_lower_bound(int i)
        {
            return alpha_status[i] == Status.LOWER_BOUND;
        }

        private bool is_free(int i)
        {
            return alpha_status[i] == Status.FREE;
        }

        private void reconstruct_gradient()
        {
            // reconstruct inactive elements of G from G_bar and free variables
            if (active_size == l)
            {
                return;
            }

            var nr_free = 0;

            for (var j = active_size; j < l; j++)
            {
                G[j] = G_bar[j] + p[j];
            }

            for (var j = 0; j < active_size; j++)
            {
                if (is_free(j))
                {
                    nr_free++;
                }
            }

            if (2 * nr_free < active_size)
            {
                Trace.WriteLine("WARNING: using -h 0 may be faster");
            }

            if (nr_free * l > 2 * active_size * (l - active_size))
            {
                for (var i = active_size; i < l; i++)
                {
                    Q(indices[i], indices, active_size, temp);
                    for (var j = 0; j < active_size; j++)
                    {
                        if (is_free(j))
                        {
                            G[i] += alpha[j] * temp[j];
                        }
                    }
                }
            }
            else
            {
                for (var i = 0; i < active_size; i++)
                {
                    if (is_free(i))
                    {
                        Q(indices[i], indices, l, temp);
                        var alpha_i = alpha[i];
                        for (var j = active_size; j < l; j++)
                        {
                            G[j] += alpha_i * temp[j];
                        }
                    }
                }
            }
        }

        // return 1 if already optimal, return 0 otherwise
        private int select_working_set(ref int out_i, ref int out_j)
        {
            // return i,j such that
            // i: maximizes -y_i * grad(f)_i, i in I_up(\alpha)
            // j: minimizes the decrease of obj value
            //    (if quadratic coefficient <= 0, replace it with tau)
            //    -y_j*grad(f)_j < -y_i*grad(f)_i, j in I_low(\alpha)

            var Gmax = double.NegativeInfinity;
            var Gmax2 = double.NegativeInfinity;
            var Gmax_idx = -1;
            var Gmin_idx = -1;
            var obj_diff_min = double.PositiveInfinity;

            for (var t = 0; t < active_size; t++)
            {
                if (y[t] == +1)
                {
                    if (!is_upper_bound(t))
                    {
                        if (-G[t] >= Gmax)
                        {
                            Gmax = -G[t];
                            Gmax_idx = t;
                        }
                    }
                }
                else
                {
                    if (!is_lower_bound(t))
                    {
                        if (G[t] >= Gmax)
                        {
                            Gmax = G[t];
                            Gmax_idx = t;
                        }
                    }
                }
            }

            var i = Gmax_idx;

            if (i != -1)
            {
                Q(i, indices, active_size, temp); // NULL Q_i not accessed: Gmax=-INF if i=-1
            }

            for (var j = 0; j < active_size; j++)
            {
                if (y[j] == +1)
                {
                    if (!is_lower_bound(j))
                    {
                        var grad_diff = Gmax + G[j];
                        if (G[j] >= Gmax2)
                        {
                            Gmax2 = G[j];
                        }

                        if (grad_diff > 0)
                        {
                            double obj_diff;
                            var quad_coef = QD[i] + QD[j] - 2.0 * y[i] * temp[j];

                            if (quad_coef > 0)
                            {
                                obj_diff = -(grad_diff * grad_diff) / quad_coef;
                            }
                            else
                            {
                                obj_diff = -(grad_diff * grad_diff) / TAU;
                            }

                            if (obj_diff <= obj_diff_min)
                            {
                                Gmin_idx = j;
                                obj_diff_min = obj_diff;
                            }
                        }
                    }
                }
                else
                {
                    if (!is_upper_bound(j))
                    {
                        var grad_diff = Gmax - G[j];
                        if (-G[j] >= Gmax2)
                        {
                            Gmax2 = -G[j];
                        }

                        if (grad_diff > 0)
                        {
                            double obj_diff;
                            var quad_coef = QD[i] + QD[j] + 2.0 * y[i] * temp[j];

                            if (quad_coef > 0)
                            {
                                obj_diff = -(grad_diff * grad_diff) / quad_coef;
                            }
                            else
                            {
                                obj_diff = -(grad_diff * grad_diff) / TAU;
                            }

                            if (obj_diff <= obj_diff_min)
                            {
                                Gmin_idx = j;
                                obj_diff_min = obj_diff;
                            }
                        }
                    }
                }
            }

            if (Gmax + Gmax2 < Tolerance)
            {
                return 1;
            }

            out_i = Gmax_idx;
            out_j = Gmin_idx;
            return 0;
        }

        private double calculate_rho()
        {
            double r;
            var nr_free = 0;
            var ub = double.PositiveInfinity;
            var lb = double.NegativeInfinity;
            double sum_free = 0;

            for (var i = 0; i < active_size; i++)
            {
                var yG = y[i] * G[i];

                if (is_upper_bound(i))
                {
                    if (y[i] == -1)
                    {
                        ub = System.Math.Min(ub, yG);
                    }
                    else
                    {
                        lb = System.Math.Max(lb, yG);
                    }
                }
                else if (is_lower_bound(i))
                {
                    if (y[i] == +1)
                    {
                        ub = System.Math.Min(ub, yG);
                    }
                    else
                    {
                        lb = System.Math.Max(lb, yG);
                    }
                }
                else
                {
                    ++nr_free;
                    sum_free += yG;
                }
            }

            if (nr_free > 0)
            {
                r = sum_free / nr_free;
            }
            else
            {
                r = (ub + lb) / 2;
            }

            return r;
        }

        private void do_shrinking()
        {
            var Gmax1 = double.NegativeInfinity; // max { -y_i * grad(f)_i | i in I_up(\alpha) }
            var Gmax2 = double.NegativeInfinity; // max { y_i * grad(f)_i | i in I_low(\alpha) }

            // find maximal violating pair first
            for (var i = 0; i < active_size; i++)
            {
                if (y[i] == +1)
                {
                    if (!is_upper_bound(i))
                    {
                        if (-G[i] >= Gmax1)
                        {
                            Gmax1 = -G[i];
                        }
                    }

                    if (!is_lower_bound(i))
                    {
                        if (G[i] >= Gmax2)
                        {
                            Gmax2 = G[i];
                        }
                    }
                }
                else
                {
                    if (!is_upper_bound(i))
                    {
                        if (-G[i] >= Gmax2)
                        {
                            Gmax2 = -G[i];
                        }
                    }

                    if (!is_lower_bound(i))
                    {
                        if (G[i] >= Gmax1)
                        {
                            Gmax1 = G[i];
                        }
                    }
                }
            }

            if (!unshrink && Gmax1 + Gmax2 <= Tolerance * 10)
            {
                unshrink = true;
                reconstruct_gradient();
                active_size = l;
                Trace.WriteLine("*");
            }

            for (var i = 0; i < active_size; i++)
            {
                if (be_shrunk(i, Gmax1, Gmax2))
                {
                    active_size--;
                    while (active_size > i)
                    {
                        if (!be_shrunk(active_size, Gmax1, Gmax2))
                        {
                            swap_index(i, active_size);
                            break;
                        }

                        active_size--;
                    }
                }
            }
        }

        private bool be_shrunk(int i, double Gmax1, double Gmax2)
        {
            if (is_upper_bound(i))
            {
                if (y[i] == +1)
                {
                    return -G[i] > Gmax1;
                }

                return -G[i] > Gmax2;
            }

            if (is_lower_bound(i))
            {
                if (y[i] == +1)
                {
                    return G[i] > Gmax2;
                }

                return G[i] > Gmax1;
            }

            return false;
        }

#if !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void swap_index(int i, int j)
        {
            swap(indices, i, j);
            swap(y, i, j);
            swap(G, i, j);
            swap(alpha_status, i, j);
            swap(alpha, i, j);
            swap(p, i, j);
            swap(active_set, i, j);
            swap(G_bar, i, j);
        }

#if !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static void swap<T>(T[] array, int i, int j)
        {
            var t = array[i];
            array[i] = array[j];
            array[j] = t;
        }

        private enum Status
        {
            LOWER_BOUND,
            UPPER_BOUND,
            FREE
        }
    }
}