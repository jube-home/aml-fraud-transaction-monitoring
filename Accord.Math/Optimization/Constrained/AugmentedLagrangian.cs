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
//
// The source code presented in this file has been adapted from the
// Augmented Lagrangian method implementation presented in the NLopt
// Numerical Optimization Library. This file is thus licensed under
// the same MIT license as the original. Details are given below.
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

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Status codes for the <see cref="AugmentedLagrangian" />
    ///     optimization algorithm.
    /// </summary>
    public enum AugmentedLagrangianStatus
    {
        /// <summary>
        ///     The algorithm has found a feasible solution.
        /// </summary>
        Converged = 1,

        /// <summary>
        ///     The optimization could not make progress towards finding a feasible
        ///     solution. Try increasing the <see cref="IConstraint.Tolerance" />
        ///     of the constraints.
        /// </summary>
        NoProgress = -1,

        /// <summary>
        ///     The optimization has reached the
        ///     <see cref="AugmentedLagrangian.MaxEvaluations">
        ///         maximum number of function evaluations
        ///     </see>
        ///     .
        /// </summary>
        MaxEvaluations = -2,

        /// <summary>
        ///     The optimization has been cancelled by the user (such as for
        ///     example by using <see cref="BaseOptimizationMethod.Token" />).
        /// </summary>
        Cancelled = -3
    }

    /// <summary>
    ///     Augmented Lagrangian method for constrained non-linear optimization.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://ab-initio.mit.edu/nlopt">
    ///                         Steven G. Johnson, The NLopt nonlinear-optimization package, http://ab-initio.mit.edu/nlopt
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.72.6121">
    ///                         E. G. Birgin and J. M. Martinez, "Improving ultimate convergence of an augmented Lagrangian
    ///                         method," Optimization Methods and Software vol. 23, no. 2, p. 177-195 (2008).
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         In this framework, it is possible to state a non-linear programming problem
    ///         using either symbolic processing or vector-valued functions. The following
    ///         example demonstrates the symbolic processing case:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Math\Optimization\AugmentedLagrangianTest.cs" region="doc_lambda" />
    ///     <para>
    ///         And this is the same example as before, but using standard vectors instead.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Math\Optimization\AugmentedLagrangianTest.cs" region="doc_vector" />
    /// </example>
    /// <seealso cref="GoldfarbIdnani" />
    public partial class AugmentedLagrangian : BaseGradientOptimizationMethod, IGradientOptimizationMethod,
        IOptimizationMethod<AugmentedLagrangianStatus>
    {
        // Stopping criteria
        private readonly double ftol_abs = 0;
        private readonly double ftol_rel = 1e-10;
        private readonly double rhoMax = 1e+1;
        private readonly double rhoMin = 1e-6;
        private readonly double xtol_rel = 1e-10;
        private IConstraint[] equalityConstraints;

        private double[] g;
        private IConstraint[] greaterThanConstraints;

        private double[] lambda; // equality multipliers

        private IConstraint[] lesserThanConstraints;
        private int maxEvaluations;
        private double[] mu; // "lesser than"  inequality multipliers
        private double[] nu; // "greater than" inequality multipliers

        private double rho;

        /// <summary>
        ///     Creates a new instance of the Augmented Lagrangian algorithm.
        /// </summary>
        /// <param name="numberOfVariables">The number of free parameters in the optimization problem.</param>
        /// <param name="constraints">
        ///     The <see cref="IConstraint" />s to which the solution must be subjected.
        /// </param>
        public AugmentedLagrangian(int numberOfVariables, IEnumerable<IConstraint> constraints)
            : base(numberOfVariables)
        {
            init(null, constraints, null);
        }

        /// <summary>
        ///     Creates a new instance of the Augmented Lagrangian algorithm.
        /// </summary>
        /// <param name="function">The objective function to be optimized.</param>
        /// <param name="constraints">
        ///     The <see cref="IConstraint" />s to which the solution must be subjected.
        /// </param>
        public AugmentedLagrangian(NonlinearObjectiveFunction function, IEnumerable<IConstraint> constraints)
            : base(function.NumberOfVariables)
        {
            init(function, constraints, null);
        }

        /// <summary>
        ///     Creates a new instance of the Augmented Lagrangian algorithm.
        /// </summary>
        /// <param name="innerSolver">
        ///     The
        ///     <see cref="IGradientOptimizationMethod">
        ///         unconstrained
        ///         optimization method
        ///     </see>
        ///     used internally to solve the dual of this optimization
        ///     problem.
        /// </param>
        /// <param name="function">The objective function to be optimized.</param>
        /// <param name="constraints">
        ///     The <see cref="IConstraint" />s to which the solution must be subjected.
        /// </param>
        public AugmentedLagrangian(IGradientOptimizationMethod innerSolver,
            NonlinearObjectiveFunction function, IEnumerable<IConstraint> constraints)
            : base(innerSolver.NumberOfVariables)
        {
            if (innerSolver.NumberOfVariables != function.NumberOfVariables)
            {
                throw new ArgumentException("The inner unconstrained optimization algorithm and the "
                                            + "objective function should have the same number of variables.",
                    "function");
            }

            init(function, constraints, innerSolver);
        }

        /// <summary>
        ///     Creates a new instance of the Augmented Lagrangian algorithm.
        /// </summary>
        /// <param name="innerSolver">
        ///     The
        ///     <see cref="IGradientOptimizationMethod">
        ///         unconstrained
        ///         optimization method
        ///     </see>
        ///     used internally to solve the dual of this optimization
        ///     problem.
        /// </param>
        /// <param name="constraints">
        ///     The <see cref="IConstraint" />s to which the solution must be subjected.
        /// </param>
        public AugmentedLagrangian(IGradientOptimizationMethod innerSolver, IEnumerable<IConstraint> constraints)
            : base(innerSolver.NumberOfVariables)
        {
            init(null, constraints, innerSolver);
        }

        /// <summary>
        ///     Gets the number of iterations performed in the
        ///     last call to the <see cref="IOptimizationMethod{TInput, TOutput}.Minimize()" /> or
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Maximize" /> methods.
        /// </summary>
        /// <value>
        ///     The number of iterations performed
        ///     in the previous optimization.
        /// </value>
        public int Iterations { get; private set; }

        /// <summary>
        ///     Gets the number of function evaluations performed
        ///     in the last call to the <see cref="IOptimizationMethod{TInput, TOutput}.Minimize()" /> or
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Maximize" /> methods.
        /// </summary>
        /// <value>
        ///     The number of evaluations performed
        ///     in the previous optimization.
        /// </value>
        public int Evaluations { get; private set; }

        /// <summary>
        ///     Gets or sets the maximum number of evaluations
        ///     to be performed during optimization. Default
        ///     is 0 (evaluate until convergence).
        /// </summary>
        public int MaxEvaluations
        {
            get => maxEvaluations;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                maxEvaluations = value;
            }
        }

        /// <summary>
        ///     Gets the inner dual problem optimization algorithm.
        /// </summary>
        public IGradientOptimizationMethod Optimizer { get; private set; }

        /// <summary>
        ///     Get the exit code returned in the last call to the
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Maximize()" /> or
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Minimize()" /> methods.
        /// </summary>
        public AugmentedLagrangianStatus Status { get; private set; }

        private void init(NonlinearObjectiveFunction function,
            IEnumerable<IConstraint> constraints, IGradientOptimizationMethod innerSolver)
        {
            if (function != null)
            {
                if (function.NumberOfVariables != NumberOfVariables)
                {
                    throw new ArgumentOutOfRangeException("function",
                        "Incorrect number of variables in the objective function. " +
                        "The number of variables must match the number of variables set in the solver.");
                }

                Function = function.Function;
                Gradient = function.Gradient;
            }

            if (innerSolver == null)
            {
                innerSolver = new BroydenFletcherGoldfarbShanno(NumberOfVariables)
                {
                    LineSearch = LineSearch.BacktrackingArmijo,
                    Corrections = 10,
                    Epsilon = 1e-10,
                    MaxIterations = 100000
                };
            }

            var equality = new List<IConstraint>();
            var lesserThan = new List<IConstraint>();
            var greaterThan = new List<IConstraint>();

            foreach (var c in constraints)
            {
                switch (c.ShouldBe)
                {
                    case ConstraintType.EqualTo:
                        equality.Add(c); break;

                    case ConstraintType.GreaterThanOrEqualTo:
                        greaterThan.Add(c); break;

                    case ConstraintType.LesserThanOrEqualTo:
                        lesserThan.Add(c); break;

                    default:
                        throw new ArgumentException("Unknown constraint type.", "constraints");
                }
            }

            lesserThanConstraints = lesserThan.ToArray();
            greaterThanConstraints = greaterThan.ToArray();
            equalityConstraints = equality.ToArray();

            lambda = new double[equalityConstraints.Length];
            mu = new double[lesserThanConstraints.Length];
            nu = new double[greaterThanConstraints.Length];

            g = new double[NumberOfVariables];

            Optimizer = innerSolver;
            Optimizer.Function = objectiveFunction;
            Optimizer.Gradient = objectiveGradient;
        }

        // Augmented Lagrangian objective
        private double objectiveFunction(double[] x)
        {
            // Compute
            //
            //   Phi(x) = f(x) + rho/2 sum(c_i(x)²) - sum(lambda_i * c_i(x))
            //

            var phi = Function(x);
            var rho2 = rho / 2.0;

            // For each equality constraint
            for (var i = 0; i < equalityConstraints.Length; i++)
            {
                var actual = equalityConstraints[i].Function(x);
                var c = actual - equalityConstraints[i].Value;

                phi += rho2 * c * c; // - lambda[i] * c;
            }

            // For each "lesser than" inequality constraint
            for (var i = 0; i < lesserThanConstraints.Length; i++)
            {
                var actual = lesserThanConstraints[i].Function(x);
                var c = actual - lesserThanConstraints[i].Value;

                if (c > 0)
                {
                    phi += rho2 * c * c; // - mu[i] * c;
                }
            }

            // For each "greater than" inequality constraint
            for (var i = 0; i < greaterThanConstraints.Length; i++)
            {
                var actual = greaterThanConstraints[i].Function(x);
                var c = greaterThanConstraints[i].Value - actual;

                if (c > 0)
                {
                    phi += rho2 * c * c; // - nu[i] * c;
                }
            }

            return phi;
        }

        // Augmented Lagrangian gradient 
        private double[] objectiveGradient(double[] x)
        {
            // Compute
            //
            //   Phi'(x) = f'(x) + rho sum(c_i(x)*c_i'(x)) - sum(lambda_i * c_i'(x))
            //

            var orig = Gradient(x);
            for (var i = 0; i < g.Length; i++)
            {
                g[i] = orig[i];
            }

            // For each equality constraint
            for (var i = 0; i < equalityConstraints.Length; i++)
            {
                var actual = equalityConstraints[i].Function(x);
                var c = actual - equalityConstraints[i].Value;
                var cg = equalityConstraints[i].Gradient(x);

                for (var j = 0; j < cg.Length; j++)
                {
                    g[j] += rho * c * cg[j]; // - lambda[i] * cg[j];
                }
            }

            // For each "lesser than" inequality constraint
            for (var i = 0; i < lesserThanConstraints.Length; i++)
            {
                var actual = lesserThanConstraints[i].Function(x);
                var c = actual - lesserThanConstraints[i].Value;
                var cg = lesserThanConstraints[i].Gradient(x);

                if (c > 0)
                {
                    // Constraint is being violated
                    for (var j = 0; j < cg.Length; j++)
                    {
                        g[j] += rho * c * cg[j]; //- mu[i] * cg[j];
                    }
                }
            }

            // For each "greater-than" inequality constraint
            for (var i = 0; i < greaterThanConstraints.Length; i++)
            {
                var actual = greaterThanConstraints[i].Function(x);
                var c = greaterThanConstraints[i].Value - actual;
                var cg = greaterThanConstraints[i].Gradient(x);

                if (c > 0)
                {
                    // Constraint is being violated
                    for (var j = 0; j < cg.Length; j++)
                    {
                        g[j] += rho * c * -cg[j]; //- nu[i] * -cg[j];
                    }
                }
            }

            return g;
        }

        /// <summary>
        ///     Implements the actual optimization algorithm. This
        ///     method should try to minimize the objective function.
        /// </summary>
        protected override bool Optimize()
        {
            Status = optimize();

            return Status == AugmentedLagrangianStatus.Converged;
        }

        private AugmentedLagrangianStatus optimize()
        {
            var ICM = double.PositiveInfinity;

            var minPenalty = double.PositiveInfinity;
            var minValue = double.PositiveInfinity;

            double penalty;

            var minFeasible = false;
            var noProgressCounter = 0;
            var maxCount = 100;
            Iterations = 0;
            Evaluations = 0;

            var currentSolution = Solution.Copy();

            Array.Clear(lambda, 0, lambda.Length);
            Array.Clear(mu, 0, mu.Length);
            Array.Clear(nu, 0, nu.Length);
            rho = 1;


            // Starting rho suggested by B & M 
            if (lambda.Length > 0 || mu.Length > 0 || nu.Length > 0)
            {
                InitializeRho(currentSolution, out minValue, out minPenalty, out minFeasible);
            }


            while (true)
            {
                if (Token.IsCancellationRequested)
                {
                    return AugmentedLagrangianStatus.Cancelled;
                }

                var prevICM = ICM;

                // Minimize the dual problem using current solution
                MinimizeDual(currentSolution);

                // Evaluate function
                Evaluations++;
                var currentValue = Optimizer.Function(currentSolution);
                bool feasible;

                UpdateMultipliers(currentSolution, out ICM, out penalty, out feasible);

                // Update rho
                if (ICM > tau * prevICM)
                {
                    rho *= gam;
                }

                var progress = CheckProgress(currentSolution, feasible, penalty, currentValue, ICM,
                    ref minValue, ref minPenalty, ref minFeasible, ref noProgressCounter, maxCount);

                if (progress.HasValue)
                {
                    return progress.Value;
                }

                // Go to next iteration
                Iterations++;

                if (maxEvaluations > 0 && Evaluations >= maxEvaluations)
                {
                    return AugmentedLagrangianStatus.MaxEvaluations;
                }
            }
        }

        private bool xtolreach(double[] currentSolution, double reltol, double abstol)
        {
            for (var i = 0; i < currentSolution.Length; i++)
            {
                if (!relstop(Solution[i], currentSolution[i], reltol, abstol))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool relstop(double vold, double vnew, double reltol, double abstol)
        {
            if (double.IsInfinity(vold))
            {
                return false;
            }

            if (double.IsNaN(vold) || double.IsNaN(vnew))
            {
                return true;
            }

            return System.Math.Abs(vnew - vold) < abstol
                   || System.Math.Abs(vnew - vold) < reltol * (System.Math.Abs(vnew) + System.Math.Abs(vold)) * 0.5
                   || (reltol > 0 && vnew == vold); // catch vnew == vold == 0 
        }
    }
}