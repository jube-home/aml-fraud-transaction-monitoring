// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Copyright © Jorge Nocedal, 1990
// http://users.eecs.northwestern.edu/~nocedal/
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
using Accord.MachineLearning;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Conjugate gradient direction update formula.
    /// </summary>
    public enum ConjugateGradientMethod
    {
        /// <summary>
        ///     Fletcher-Reeves formula.
        /// </summary>
        FletcherReeves = 1,

        /// <summary>
        ///     Polak-Ribière formula.
        /// </summary>
        /// <remarks>
        ///     The Polak-Ribière is known to perform better for non-quadratic functions.
        /// </remarks>
        PolakRibiere = 2,

        /// <summary>
        ///     Polak-Ribière formula.
        /// </summary>
        /// <remarks>
        ///     The Polak-Ribière is known to perform better for non-quadratic functions.
        ///     The positive version B=max(0,Bpr) provides a direction reset automatically.
        /// </remarks>
        PositivePolakRibiere = 3
    }

    /// <summary>
    ///     Conjugate Gradient exit codes.
    /// </summary>
    public enum ConjugateGradientCode
    {
        /// <summary>
        ///     Success.
        /// </summary>
        Success,

        /// <summary>
        ///     Invalid step size.
        /// </summary>
        StepSize = 1,

        /// <summary>
        ///     Descent direction was not obtained.
        /// </summary>
        DescentNotObtained = -2,

        /// <summary>
        ///     Rounding errors prevent further progress. There may not be a step
        ///     which satisfies the sufficient decrease and curvature conditions.
        ///     Tolerances may be too small.
        /// </summary>
        RoundingErrors = 6,

        /// <summary>
        ///     The step size has reached the upper bound.
        /// </summary>
        StepHigh = 5,

        /// <summary>
        ///     The step size has reached the lower bound.
        /// </summary>
        StepLow = 4,

        /// <summary>
        ///     Maximum number of function evaluations has been reached.
        /// </summary>
        MaximumEvaluations = 3,

        /// <summary>
        ///     Relative width of the interval of uncertainty is at machine precision.
        /// </summary>
        Precision = 2
    }

    /// <summary>
    ///     Conjugate Gradient (CG) optimization method.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In mathematics, the conjugate gradient method is an algorithm for the numerical solution of
    ///         particular systems of linear equations, namely those whose matrix is symmetric and positive-
    ///         definite. The conjugate gradient method is an iterative method, so it can be applied to sparse
    ///         systems that are too large to be handled by direct methods. Such systems often arise when
    ///         numerically solving partial differential equations. The nonlinear conjugate gradient method
    ///         generalizes the conjugate gradient method to nonlinear optimization (Wikipedia, 2011).
    ///     </para>
    ///     <para>
    ///         T
    ///     </para>
    ///     <para>
    ///         The framework implementation of this method is based on the original FORTRAN source code
    ///         by Jorge Nocedal (see references below). The original FORTRAN source code of CG+ (for large
    ///         scale unconstrained problems) is available at http://users.eecs.northwestern.edu/~nocedal/CG+.html
    ///         and had been made freely available for educational or commercial use. The original authors
    ///         expect that all publications describing work using this software quote the (Gilbert and Nocedal, 1992)
    ///         reference given below.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://users.eecs.northwestern.edu/~nocedal/CG+.html">
    ///                         J. C. Gilbert and J. Nocedal. Global Convergence Properties of Conjugate Gradient
    ///                         Methods for Optimization, (1992) SIAM J. on Optimization, 2, 1.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Wikipedia contributors, "Nonlinear conjugate gradient method," Wikipedia, The Free
    ///                     Encyclopedia, http://en.wikipedia.org/w/index.php?title=Nonlinear_conjugate_gradient_method
    ///                     (accessed December 22, 2011).
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Wikipedia contributors, "Conjugate gradient method," Wikipedia, The Free Encyclopedia,
    ///                     http://en.wikipedia.org/w/index.php?title=Conjugate_gradient_method
    ///                     (accessed December 22, 2011).
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="BroydenFletcherGoldfarbShanno" />
    /// <seealso cref="ResilientBackpropagation" />
    /// <seealso cref="BoundedBroydenFletcherGoldfarbShanno" />
    /// <seealso cref="TrustRegionNewtonMethod" />
    public partial class ConjugateGradient : BaseGradientOptimizationMethod,
        IGradientOptimizationMethod, IOptimizationMethod<ConjugateGradientCode>,
        ISupportsCancellation
    {
        private readonly double epsilon = 1e-5;

        private readonly double ftol = 1e-4;
        private readonly double gtol = 0.1;
        private readonly int maxfev = 40;
        private readonly double stpmax = 1e20;
        private readonly double stpmin = 1e-20;
        private readonly double xtol = 1e-17;

        // TODO: Move to separate classes

        private bool brackt;

        private double[] d;
        private double dg2;
        private double dgtest;
        private double dgx;
        private double dgy;
        private double finit;
        private double ftest1;
        private double fx;
        private double fy;
        private double[] g; // gradient at current solution
        private double[] gold;
        private int infoc;
        private bool stage1;
        private double stmax;

        private double stmin;
        private double stx;
        private double sty;
        private double[] w;
        private double width;
        private double width1;

        /// <summary>
        ///     Creates a new instance of the CG optimization algorithm.
        /// </summary>
        public ConjugateGradient()
        {
        }

        /// <summary>
        ///     Creates a new instance of the CG optimization algorithm.
        /// </summary>
        /// <param name="numberOfVariables">The number of free parameters in the optimization problem.</param>
        public ConjugateGradient(int numberOfVariables)
            : base(numberOfVariables)
        {
        }

        /// <summary>
        ///     Creates a new instance of the CG optimization algorithm.
        /// </summary>
        /// <param name="numberOfVariables">The number of free parameters in the function to be optimized.</param>
        /// <param name="function">The function to be optimized.</param>
        /// <param name="gradient">The gradient of the function.</param>
        public ConjugateGradient(int numberOfVariables,
            Func<double[], double> function, Func<double[], double[]> gradient)
            : base(numberOfVariables, function, gradient)
        {
        }

        /// <summary>
        ///     Gets or sets the relative difference threshold
        ///     to be used as stopping criteria between two
        ///     iterations. Default is 0 (iterate until convergence).
        /// </summary>
        public double Tolerance { get; set; } = 1e-5;

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     to be performed during optimization. Default
        ///     is 0 (iterate until convergence).
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        ///     Gets or sets the conjugate gradient update
        ///     method to be used during optimization.
        /// </summary>
        public ConjugateGradientMethod Method { get; set; }

        /// <summary>
        ///     Gets the number of iterations performed
        ///     in the last call to <see cref="IOptimizationMethod{TInput, TOutput}.Minimize()" />.
        /// </summary>
        /// <value>
        ///     The number of iterations performed
        ///     in the previous optimization.
        /// </value>
        public int Iterations { get; private set; }

        /// <summary>
        ///     Gets the number of function evaluations performed
        ///     in the last call to <see cref="IOptimizationMethod{TInput, TOutput}.Minimize()" />.
        /// </summary>
        /// <value>
        ///     The number of evaluations performed
        ///     in the previous optimization.
        /// </value>
        public int Evaluations { get; private set; }

        /// <summary>
        ///     Gets the number of linear searches performed
        ///     in the last call to <see cref="IOptimizationMethod{TInput, TOutput}.Minimize()" />.
        /// </summary>
        public int Searches { get; private set; }

        /// <summary>
        ///     Get the exit code returned in the last call to the
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Maximize()" /> or
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Minimize()" /> methods.
        /// </summary>
        public ConjugateGradientCode Status { get; private set; }

        /// <summary>
        ///     Occurs when progress is made during the optimization.
        /// </summary>
        public event EventHandler<OptimizationProgressEventArgs> Progress;

        /// <summary>
        ///     Called when the <see cref="IOptimizationMethod{TInput, TOutput}.NumberOfVariables" /> property has changed.
        /// </summary>
        /// <param name="numberOfVariables">The number of variables.</param>
        protected override void OnNumberOfVariablesChanged(int numberOfVariables)
        {
            base.OnNumberOfVariablesChanged(numberOfVariables);

            d = new double[numberOfVariables];
            gold = new double[numberOfVariables];
            w = new double[numberOfVariables];
        }

        /// <summary>
        ///     Implements the actual optimization algorithm. This
        ///     method should try to minimize the objective function.
        /// </summary>
        protected override bool Optimize()
        {
            return new Session(this).Run();
        }

        private ConjugateGradientCode cvsmod(ref double f, double[] s, ref double stp, ref int info,
            ref int nfev, double[] wa, ref double dginit, ref double dgout)
        {
            var x = Solution;

            if (info == 1)
            {
                goto L321;
            }

            infoc = 1;

            if (stp <= 0) // Check the input parameters for errors
            {
                return ConjugateGradientCode.StepSize;
            }

            // Compute the initial gradient in the search direction
            // and check that S is a descent direction.

            if (dginit >= 0)
            {
                throw new LineSearchFailedException(0, "The search direction is not a descent direction.");
            }

            // Initialize local variables
            brackt = false;
            stage1 = true;
            nfev = 0;
            finit = f;
            dgtest = ftol * dginit;
            width = stpmax - stpmin;
            width1 = width / 0.5;

            for (var j = 0; j < x.Length; ++j)
            {
                wa[j] = x[j];
            }

            // The variables STX, FX, DGX contain the values of the step,
            //   function, and directional derivative at the best step.
            // The variables STY, FY, DGY contain the value of the step,
            //   function, and derivative at the other endpoint of the interval
            //   of uncertainty.
            // The variables STP, F, DG contain the values of the step,
            //   function, and derivative at the current step.

            stx = 0;
            fx = finit;
            dgx = dginit;
            sty = 0;
            fy = finit;
            dgy = dginit;


            L30: // Start of iteration.

            // Set the minimum and maximum steps to correspond
            // to the present interval of uncertainty.

            if (brackt)
            {
                stmin = System.Math.Min(stx, sty);
                stmax = System.Math.Max(stx, sty);
            }
            else
            {
                stmin = stx;
                stmax = stp + 4 * (stp - stx);
            }

            // Force the step to be within
            // the bounds STPMAX and STPMIN.

            stp = System.Math.Max(stp, stpmin);
            stp = System.Math.Min(stp, stpmax);

            // If an unusual termination is to occur then 
            // let STP be the lowest point obtained so far.

            if ((brackt && (stp <= stmin || stp >= stmax)) || nfev >= maxfev - 1 ||
                infoc == 0 || (brackt && stmax - stmin <= xtol * stmax))
            {
                stp = stx;
            }

            // Evaluate the function and gradient at STP
            // and compute the directional derivative.

            for (var j = 0; j < s.Length; ++j)
            {
                x[j] = wa[j] + stp * s[j];
            }

            // Fetch function and gradient
            f = Function(x);
            g = Gradient(x);

            info = 0;
            nfev++;
            dg2 = 0;

            for (var j = 0; j < g.Length; ++j)
            {
                dg2 += g[j] * s[j];
            }

            ftest1 = finit + stp * dgtest;

            if ((brackt && (stp <= stmin || stp >= stmax)) || infoc == 0)
            {
                return ConjugateGradientCode.RoundingErrors;
            }

            if (stp == stpmax && f <= ftest1 && dg2 <= dgtest)
            {
                return ConjugateGradientCode.StepHigh;
            }

            if (stp == stpmin && (f > ftest1 || dg2 >= dgtest))
            {
                return ConjugateGradientCode.StepLow;
            }

            if (nfev >= maxfev)
            {
                return ConjugateGradientCode.MaximumEvaluations;
            }

            if (brackt && stmax - stmin <= xtol * stmax)
            {
                return ConjugateGradientCode.Precision;
            }


            // More's code has been modified so that at least one new 
            //  function value is computed during the line search (enforcing 
            //  at least one interpolation is not easy, since the code may 
            //  override an interpolation) 

            if (f <= ftest1 && System.Math.Abs(dg2) <= gtol * -dginit && nfev > 1)
            {
                info = 1;
                dgout = dg2;
                return ConjugateGradientCode.Success;
            }


            L321:

            // In the first stage we seek a step for which the modified
            // function has a nonpositive value and nonnegative derivative.
            if (stage1 && f <= ftest1 && dg2 >= System.Math.Min(ftol, gtol) * dginit)
            {
                stage1 = false;
            }

            // A modified function is used to predict the step only if
            // we have not obtained a step for which the modified function
            // has a nonpositive function value and nonnegative derivative,
            // and if a lower function value has been obtained but the 
            // decrease is not sufficient.

            if (stage1 && f <= fx && f > ftest1)
            {
                // Define the modified function and derivative values
                var fm = f - stp * dgtest;
                var fxm = fx - stx * dgtest;
                var fym = fy - sty * dgtest;
                var dgm = dg2 - dgtest;
                var dgxm = dgx - dgtest;
                var dgym = dgy - dgtest;

                // Call CSTEPM to update the interval of
                // uncertainty and to compute the new step.

                BoundedBroydenFletcherGoldfarbShanno.dcstep(ref stx, ref fxm, ref dgxm,
                    ref sty, ref fym, ref dgym, ref stp, fm, dgm, ref brackt, stpmin, stpmax);

                // Reset the function and gradient values for f.
                fx = fxm + stx * dgtest;
                fy = fym + sty * dgtest;
                dgx = dgxm + dgtest;
                dgy = dgym + dgtest;
            }
            else
            {
                // Call CSTEPM to update the interval of
                // uncertainty and to compute the new step.
                BoundedBroydenFletcherGoldfarbShanno.dcstep(ref stx, ref fx, ref dgx,
                    ref sty, ref fy, ref dgy, ref stp, f, dg2, ref brackt, stpmin, stpmax);
            }

            // Force a sufficient decrease in the 
            // size of the interval of uncertainty.

            if (brackt)
            {
                if (System.Math.Abs(sty - stx) >= 0.66 * width1)
                {
                    stp = stx + 0.5 * (sty - stx);
                }

                width1 = width;
                width = System.Math.Abs(sty - stx);
            }

            goto L30;
        }
    }
}