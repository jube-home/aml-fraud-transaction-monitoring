// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Steven G. Johnson, 2008
// stevenj@alum.mit.edu
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
// The source code presented in this file has been adapted from the
// Nelder Mead Simplex method implementation presented in the NLopt
// Numerical Optimization Library. Original license is given below.
//
//    Copyright (c) 2007-2011 Massachusetts Institute of Technology
//
//    Permission is hereby granted, free of charge, to any person obtaining
//    a copy of this software and associated documentation files (the
//    "Software"), to deal in the Software without restriction, including
//    without limitation the rights to use, copy, modify, merge, publish,
//    distribute, sublicense, and/or sell copies of the Software, and to
//    permit persons to whom the Software is furnished to do so, subject to
//    the following conditions:
// 
//    The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.
// 
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
//

using System;
using System.Collections.Generic;
using Accord.Collections;
using Accord.Math.Convergence;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     <see cref="NelderMead" /> exit codes.
    /// </summary>
    public enum NelderMeadStatus
    {
        /// <summary>
        ///     Optimization was canceled by the user.
        /// </summary>
        ForcedStop,

        /// <summary>
        ///     Optimization ended successfully.
        /// </summary>
        Success,

        /// <summary>
        ///     The execution time exceeded the established limit.
        /// </summary>
        MaximumTimeReached,

        /// <summary>
        ///     The minimum desired value has been reached.
        /// </summary>
        MinimumAllowedValueReached,

        /// <summary>
        ///     The algorithm had stopped prematurely because
        ///     the maximum number of evaluations was reached.
        /// </summary>
        MaximumEvaluationsReached,

        /// <summary>
        ///     The algorithm failed internally.
        /// </summary>
        Failure,

        /// <summary>
        ///     The desired output tolerance (minimum change in the function
        ///     output between two consecutive iterations) has been reached.
        /// </summary>
        FunctionToleranceReached,

        /// <summary>
        ///     The desired parameter tolerance (minimum change in the
        ///     solution vector between two iterations) has been reached.
        /// </summary>
        SolutionToleranceReached
    }

    /// <summary>
    ///     Nelder-Mead simplex algorithm with support for bound
    ///     constraints for non-linear, gradient-free optimization.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Nelder–Mead method or downhill simplex method or amoeba method is a
    ///         commonly used nonlinear optimization technique, which is a well-defined
    ///         numerical method for problems for which derivatives may not be known.
    ///         However, the Nelder–Mead technique is a heuristic search method that can
    ///         converge to non-stationary points on problems that can be solved by
    ///         alternative methods.
    ///     </para>
    ///     <para>
    ///         The Nelder–Mead technique was proposed by John Nelder and Roger Mead (1965)
    ///         and is a technique for minimizing an objective function in a many-dimensional
    ///         space.
    ///     </para>
    ///     <para>
    ///         The source code presented in this file has been adapted from the
    ///         Sbplx method (based on Nelder-Mead's Simplex) given in the NLopt
    ///         Numerical Optimization Library, created by Steven G. Johnson.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://ab-initio.mit.edu/nlopt">
    ///                         Steven G. Johnson, The NLopt nonlinear-optimization package,
    ///                         http://ab-initio.mit.edu/nlopt
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Nelder%E2%80%93Mead_method">
    ///                         Wikipedia, The Free Encyclopedia. Nelder Mead method. Available on:
    ///                         http://en.wikipedia.org/wiki/Nelder%E2%80%93Mead_method
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Math\Optimization\NelderMeadTest.cs" region="doc_min" />
    /// </example>
    public partial class NelderMead : BaseOptimizationMethod, IOptimizationMethod<NelderMeadStatus>
    {
        // heuristic "strategy" constants:
        private const double alpha = 1;
        private const double beta = 0.5;
        private const double gamm = 2;
        private const double delta = 0.5;

        private double[] c; // centroid * n

        // bounds

        private double[][] pts; // simplex points

        private double[] val; // vertex values
        private double[] xcur; // current point 

        /// <summary>
        ///     Creates a new <see cref="NelderMead" /> non-linear optimization algorithm.
        /// </summary>
        /// <param name="numberOfVariables">The number of free parameters in the optimization problem.</param>
        public NelderMead(int numberOfVariables)
            : base(numberOfVariables)
        {
            init(numberOfVariables);
        }

        /// <summary>
        ///     Creates a new <see cref="NelderMead" /> non-linear optimization algorithm.
        /// </summary>
        /// <param name="numberOfVariables">The number of free parameters in the optimization problem.</param>
        /// <param name="function">The objective function whose optimum values should be found.</param>
        public NelderMead(int numberOfVariables, Func<double[], double> function)
            : base(numberOfVariables, function)
        {
            init(numberOfVariables);
        }

        /// <summary>
        ///     Creates a new <see cref="NelderMead" /> non-linear optimization algorithm.
        /// </summary>
        /// <param name="function">The objective function whose optimum values should be found.</param>
        public NelderMead(NonlinearObjectiveFunction function)
            : base(function)
        {
            init(function.NumberOfVariables);
        }

        /// <summary>
        ///     Gets the maximum
        ///     <see cref="NumberOfVariables">
        ///         number of
        ///         variables
        ///     </see>
        ///     that can be optimized by this instance.
        ///     This is the initial value that has been passed to this
        ///     class constructor at the time the algorithm was created.
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        ///     Gets or sets the maximum value that the objective
        ///     function could produce before the algorithm could
        ///     be terminated as if the solution was good enough.
        /// </summary>
        public double MaximumValue { get; set; } = double.NegativeInfinity;

        /// <summary>
        ///     Gets the step sizes to be used by the optimization
        ///     algorithm. Default is to initialize each with 1e-5.
        /// </summary>
        public double[] StepSize { get; private set; }

        /// <summary>
        ///     Gets or sets multiple convergence options to
        ///     determine when the optimization can terminate.
        /// </summary>
        public GeneralConvergence Convergence { get; set; }

        /// <summary>
        ///     Gets the lower bounds that should be respected in this
        ///     optimization problem. Default is to initialize this vector
        ///     with <see cref="Double.NegativeInfinity" />.
        /// </summary>
        public double[] LowerBounds { get; private set; }

        /// <summary>
        ///     Gets the upper bounds that should be respected in this
        ///     optimization problem. Default is to initialize this vector
        ///     with <see cref="Double.PositiveInfinity" />.
        /// </summary>
        public double[] UpperBounds { get; private set; }

        /// <summary>
        ///     Gets or sets the by how much the simplex diameter |xl - xh| must be
        ///     reduced before the algorithm can be terminated. Setting this value
        ///     to a value higher than zero causes the algorithm to replace the
        ///     standard <see cref="Convergence" /> criteria with this condition.
        ///     Default is zero.
        /// </summary>
        public double DiameterTolerance { get; set; }

        /// <summary>
        ///     The difference between the high and low function
        ///     values of the last simplex in the previous call
        ///     to the optimization function.
        /// </summary>
        public double Difference { get; private set; }

        /// <summary>
        ///     Gets or sets the number of variables (free parameters) in the
        ///     optimization problem. This number can be decreased after the
        ///     algorithm has been created so it can operate on subspaces.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" />
        public override int NumberOfVariables
        {
            get => base.NumberOfVariables;
            set
            {
                if (Capacity > 0 && (value <= 0 || value > Capacity))
                {
                    throw new ArgumentOutOfRangeException("value",
                        "The number of variables must be higher than 0 and less than or"
                        + " equal to the maximum number of variables initially passed to the"
                        + " Nelder-Mead constructor (it was passed " + Capacity + ").");
                }

                base.NumberOfVariables = value;
            }
        }

        /// <summary>
        ///     Get the exit code returned in the last call to the
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Maximize()" /> or
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Minimize()" /> methods.
        /// </summary>
        public NelderMeadStatus Status { get; private set; }

        private void init(int n)
        {
            Capacity = n;
            Convergence = new GeneralConvergence(Capacity);

            pts = new double[n + 1][];
            for (var i = 0; i < pts.Length; i++)
            {
                pts[i] = new double[n];
            }

            val = new double[n + 1];

            c = new double[n];
            xcur = new double[n];

            StepSize = new double[n];
            for (var i = 0; i < StepSize.Length; i++)
            {
                StepSize[i] = 1e-5;
            }

            LowerBounds = new double[n];
            for (var i = 0; i < LowerBounds.Length; i++)
            {
                LowerBounds[i] = double.NegativeInfinity;
            }

            UpperBounds = new double[n];
            for (var i = 0; i < UpperBounds.Length; i++)
            {
                UpperBounds[i] = double.PositiveInfinity;
            }
        }

        /// <summary>
        ///     Called when the <see cref="NumberOfVariables" /> property has changed.
        /// </summary>
        /// <param name="numberOfVariables">The number of variables.</param>
        protected override void OnNumberOfVariablesChanged(int numberOfVariables)
        {
            if (Solution == null || numberOfVariables > Solution.Length)
            {
                base.OnNumberOfVariablesChanged(numberOfVariables);
            }
        }

        /// <summary>
        ///     Finds the minimum value of a function, using the function output at
        ///     the current value, if already known. This overload can be used when
        ///     embedding Nelder-Mead in other algorithms to avoid initial checks.
        /// </summary>
        /// <param name="fmin">The function output at the current values, if already known.</param>
        public NelderMeadStatus Minimize(double fmin)
        {
            Value = fmin;
            Status = minimize();
            return Status;
        }

        /// <summary>
        ///     Implements the actual optimization algorithm. This
        ///     method should try to minimize the objective function.
        /// </summary>
        protected override bool Optimize()
        {
            Status = NelderMeadStatus.Success;

            Value = Function(Solution);
            Convergence.Evaluations++;

            if (nlopt_stop_forced(Convergence))
            {
                Status = NelderMeadStatus.ForcedStop;
            }
            else if (Value < MaximumValue)
            {
                Status = NelderMeadStatus.MinimumAllowedValueReached;
            }
            else if (nlopt_stop_evals(Convergence))
            {
                Status = NelderMeadStatus.MaximumEvaluationsReached;
            }
            else if (nlopt_stop_time(Convergence))
            {
                Status = NelderMeadStatus.MaximumTimeReached;
            }

            if (Status != NelderMeadStatus.Success)
            {
                return false;
            }

            Status = minimize();

            return Status == NelderMeadStatus.Success ||
                   Status == NelderMeadStatus.FunctionToleranceReached ||
                   Status == NelderMeadStatus.SolutionToleranceReached;
        }

        private NelderMeadStatus minimize()
        {
            /*
               Internal version of nldrmd_minimize, intended to be used as
             a subroutine for the subplex method.  Three differences compared
             to nldrmd_minimize:

             *minf should contain the value of f(x)  (so that we don't have to
             re-evaluate f at the starting x).

             if psi > 0, then it *replaces* xtol and ftol in stop with the condition
             that the simplex diameter |xl - xh| must be reduced by a factor of psi
             ... this is for when nldrmd is used within the subplex method; for
             ordinary termination tests, set psi = 0.

             scratch should contain an array of length >= (n+1)*(n+1) + 2*n,
             used as scratch workspace.

             On output, *fdiff will contain the difference between the high
             and low function values of the last simplex.                   */


            var x = Solution;
            var n = NumberOfVariables;

            var ninv = 1.0 / n;
            double init_diam = 0;

            var t = new RedBlackTree<double, double[]>(true);

            Difference = double.MaxValue;

            // initialize the simplex based on the starting xstep
            Array.Copy(x, pts[0], n);

            val[0] = Value;

            if (Value < MaximumValue)
            {
                return NelderMeadStatus.MinimumAllowedValueReached;
            }

            var initialization = InitializeSimplex(x, n);
            if (initialization != NelderMeadStatus.Success)
            {
                return initialization;
            }

            while (true)
            {
                for (var i = 0; i < n + 1; i++)
                {
                    t.Add(new KeyValuePair<double, double[]>(val[i], pts[i]));
                }

                var result = SearchUntilRestart(t, n, ninv, ref init_diam);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
        }

        /// <summary>
        ///     Performs the reflection <c>xnew = c + scale * (c - xold)</c>,
        ///     returning 0 if <c>xnew == c</c> or <c>xnew == xold</c> (coincident
        ///     points), and 1 otherwise.
        /// </summary>
        /// <remarks>
        ///     The reflected point xnew is "pinned" to the lower and upper bounds
        ///     (lb and ub), as suggested by J. A. Richardson and J. L. Kuester,
        ///     "The complex method for constrained optimization," Commun. ACM
        ///     16(8), 487-489 (1973).  This is probably a suboptimal way to handle
        ///     bound constraints, but I don't know a better way.  The main danger
        ///     with this is that the simplex might collapse into a
        ///     lower-dimensional hyperplane; this danger can be ameliorated by
        ///     restarting (as in subplex), however.
        /// </remarks>
        private static bool reflectpt(int n, double[] xnew, double[] c,
            double scale, double[] xold, double[] lb, double[] ub)
        {
            bool equalc = true, equalold = true;

            for (var i = 0; i < n; ++i)
            {
                var newx = c[i] + scale * (c[i] - xold[i]);

                if (newx < lb[i])
                {
                    newx = lb[i];
                }

                if (newx > ub[i])
                {
                    newx = ub[i];
                }

                equalc = equalc && close(newx, c[i]);
                equalold = equalold && close(newx, xold[i]);
                xnew[i] = newx;
            }

            return !(equalc || equalold);
        }

        internal NelderMeadStatus checkeval(double[] xc, double fc)
        {
            Convergence.Evaluations++;

            if (nlopt_stop_forced(Convergence))
            {
                return NelderMeadStatus.ForcedStop;
            }

            if (fc <= Value)
            {
                Value = fc;

                Array.Copy(xc, Solution, NumberOfVariables);

                if (Value < MaximumValue)
                {
                    return NelderMeadStatus.MinimumAllowedValueReached;
                }
            }

            if (nlopt_stop_evals(Convergence))
            {
                return NelderMeadStatus.MaximumEvaluationsReached;
            }

            if (nlopt_stop_time(Convergence))
            {
                return NelderMeadStatus.MaximumTimeReached;
            }

            return NelderMeadStatus.Success;
        }

        /// <summary>
        ///     Determines whether two numbers are numerically
        ///     close (within current floating-point precision).
        /// </summary>
        private static bool close(double a, double b)
        {
            return System.Math.Abs(a - b) <= 1e-13 * (System.Math.Abs(a) + System.Math.Abs(b));
        }

        internal static bool nlopt_stop_ftol(GeneralConvergence stop, double f, double oldf)
        {
            var ftol_rel = stop.RelativeFunctionTolerance;
            var ftol_abs = stop.AbsoluteFunctionTolerance;

            return relstop(oldf, f, ftol_rel, ftol_abs);
        }

        internal static bool nlopt_stop_xtol(GeneralConvergence stop, double[] x, double[] oldx, int n)
        {
            var xtol_rel = stop.RelativeParameterTolerance;
            var xtol_abs = stop.AbsoluteParameterTolerance;

            for (var i = 0; i < n; ++i)
            {
                if (!relstop(oldx[i], x[i], xtol_rel, xtol_abs[i]))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool nlopt_stop_forced(GeneralConvergence stop)
        {
            return stop.Cancel;
        }

        internal static bool nlopt_stop_evals(GeneralConvergence stop)
        {
            var maxeval = stop.MaximumEvaluations;
            var nevals = stop.Evaluations;
            return maxeval > 0 && nevals >= maxeval;
        }

        internal static bool nlopt_stop_time(GeneralConvergence stop)
        {
            var maxtime = stop.MaximumTime;
            var start = stop.StartTime;
            return maxtime > TimeSpan.Zero && DateTime.Now - start >= maxtime;
        }

        internal static bool relstop(double old, double n, double reltol, double abstol)
        {
            if (double.IsInfinity(old))
            {
                return false;
            }

            return System.Math.Abs(n - old) < abstol
                   || System.Math.Abs(n - old) < reltol * (System.Math.Abs(n) + System.Math.Abs(old)) * 0.5
                   || (reltol > 0 && n == old); /* catch new == old == 0 case */
        }
    }
}