// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © 1995, 1996, 1997, 1998
// Berwin A. Turlach <bturlach@stats.adelaide.edu.au>
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
// This work is based on the original Fortran implementation by Berwin Turlach,
// also shared under the LGPL license.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Status codes for the <see cref="GoldfarbIdnani" />
    ///     constrained quadratic programming solver.
    /// </summary>
    public enum GoldfarbIdnaniStatus
    {
        /// <summary>
        ///     Convergence was attained.
        /// </summary>
        Success,

        /// <summary>
        ///     The quadratic problem matrix is not positive definite.
        /// </summary>
        NonPositiveDefinite,

        /// <summary>
        ///     The posed constraints cannot be fulfilled.
        /// </summary>
        NoPossibleSolution
    }

    /// <summary>
    ///     Goldfarb-Idnani Quadratic Programming Solver.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.javaquant.net/papers/GoldfarbIdnani.pdf">
    ///                         Goldfarb D., Idnani A. (1982) Dual and Primal-Dual Methods for Solving Strictly Convex
    ///                         Quadratic Programs.
    ///                         Available on: http://www.javaquant.net/papers/GoldfarbIdnani.pdf .
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.javaquant.net/papers/GoldfarbIdnani.pdf">
    ///                         Berwin A Turlach. QuadProg, Quadratic Programming Solver (implementation in Fortran).
    ///                         Available on:  http://school.maths.uwa.edu.au/~berwin/software/quadprog.html .
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         There are three ways to state a quadratic programming problem in this framework.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 The first is to state the problem in its canonical form, explicitly stating the
    ///                 matrix Q and vector d specifying the quadratic function and the matrices A and
    ///                 vector b specifying the problem constraints.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The second is to state the problem with lambda expressions using symbolic variables.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 The third is to state the problem using text strings.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         In the following section we will provide examples for those ways.
    ///     </para>
    ///     <para>
    ///         This is an example stating the problem using lambdas:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Math\Optimization\GoldfarbIdnaniTest.cs" region="doc_lambdas" />
    ///     <para>
    ///         This is an example stating the problem using strings:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Math\Optimization\GoldfarbIdnaniTest.cs" region="doc_string" />
    ///     <para>
    ///         And finally, an example stating the problem using matrices:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Math\Optimization\GoldfarbIdnaniTest.cs" region="doc_matrix" />
    /// </example>
    /// <seealso cref="AugmentedLagrangian" />
    public partial class GoldfarbIdnani : BaseGradientOptimizationMethod,
        IOptimizationMethod, IOptimizationMethod<GoldfarbIdnaniStatus>
    {
        private readonly double[] constraintTolerances;

        private double[] constraintValues;
        private double[] iwnbv;
        private double[] iwrm;
        private double[] iwrv;
        private double[] iwsv;
        private double[] iwuv;
        private double[] iwzv;

        //private double[] work;
        private int r;
        private double[] work;

        /// <summary>
        ///     Constructs a new <see cref="GoldfarbIdnani" /> class.
        /// </summary>
        /// <param name="function">The objective function to be optimized.</param>
        /// <param name="constraints">The problem's constraints.</param>
        public GoldfarbIdnani(QuadraticObjectiveFunction function, IEnumerable<LinearConstraint> constraints)
            : this(function, new LinearConstraintCollection(constraints))
        {
        }

        /// <summary>
        ///     Constructs a new <see cref="GoldfarbIdnani" /> class.
        /// </summary>
        /// <param name="function">The objective function to be optimized.</param>
        /// <param name="constraints">The problem's constraints.</param>
        public GoldfarbIdnani(QuadraticObjectiveFunction function, LinearConstraintCollection constraints)
            : base(function.NumberOfVariables, function.Function, function.Gradient)
        {
            int equalities;

            // Create the constraint matrix A from the specified constraint list
            var A = constraints.CreateMatrix(function.NumberOfVariables,
                out constraintValues, out constraintTolerances, out equalities);

            Debug.Assert(A.GetLength(1) == function.NumberOfVariables);

            initialize(function.NumberOfVariables,
                function.QuadraticTerms, function.LinearTerms,
                A, constraintValues, equalities);
        }

        /// <summary>
        ///     Constructs a new instance of the <see cref="GoldfarbIdnani" /> class.
        /// </summary>
        /// <param name="function">The objective function to be optimized.</param>
        /// <param name="constraintMatrix">The constraints matrix <c>A</c>.</param>
        /// <param name="constraintValues">The constraints values <c>b</c>.</param>
        /// <param name="numberOfEqualities">The number of equalities in the constraints.</param>
        public GoldfarbIdnani(QuadraticObjectiveFunction function, double[,] constraintMatrix,
            double[] constraintValues, int numberOfEqualities = 0)
            : base(function.NumberOfVariables, function.Function, function.Gradient)
        {
            if (function.NumberOfVariables != constraintMatrix.GetLength(1))
            {
                throw new ArgumentException("The number of columns in the constraint matrix A "
                                            + "should equal the number of variables in the problem.",
                    "constraintMatrix");
            }

            if (constraintValues.Length != constraintMatrix.GetLength(0))
            {
                throw new DimensionMismatchException("constraintValues");
            }

            if (numberOfEqualities < 0 || numberOfEqualities > constraintValues.Length)
            {
                throw new ArgumentOutOfRangeException("numberOfEqualities");
            }

            constraintTolerances = Vector.Create(constraintValues.Length, LinearConstraint.DefaultTolerance);

            initialize(function.NumberOfVariables, function.QuadraticTerms,
                function.LinearTerms, constraintMatrix, constraintValues, numberOfEqualities);
        }

        /// <summary>
        ///     Constructs a new instance of the <see cref="GoldfarbIdnani" /> class.
        /// </summary>
        /// <param name="quadratic">The symmetric matrix of quadratic terms defining the objective function.</param>
        /// <param name="linear">The vector of linear terms defining the objective function.</param>
        /// <param name="constraintMatrix">The constraints matrix <c>A</c>.</param>
        /// <param name="constraintValues">The constraints values <c>b</c>.</param>
        /// <param name="numberOfEqualities">The number of equalities in the constraints.</param>
        public GoldfarbIdnani(double[,] quadratic, double[] linear,
            double[,] constraintMatrix, double[] constraintValues, int numberOfEqualities = 0)
            : this(new QuadraticObjectiveFunction(quadratic, linear), constraintMatrix, constraintValues,
                numberOfEqualities)
        {
        }

        /// <summary>
        ///     Gets the total number of constraints in the problem.
        /// </summary>
        public int NumberOfConstraints { get; private set; }

        /// <summary>
        ///     Gets how many constraints are inequality constraints.
        /// </summary>
        public int NumberOfEqualities { get; private set; }

        /// <summary>
        ///     Gets the total number of iterations performed on the
        ///     last call to the <see cref="Minimize" /> or <see cref="Maximize" /> methods.
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of iterations that should be
        ///     performed before the method terminates. If set to zero, the
        ///     method will run to completion. Default is 0.
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        ///     Gets the total number of constraint removals performed
        ///     on the last call to the <see cref="Minimize" /> or <see cref="Maximize" /> methods.
        /// </summary>
        public int Deletions { get; set; }

        /// <summary>
        ///     Gets the Lagrangian multipliers for the
        ///     last solution found.
        /// </summary>
        public double[] Lagrangian { get; private set; }

        /// <summary>
        ///     Gets the indices of the active constraints
        ///     found during the last call of the
        ///     <see cref="Minimize" /> or <see cref="Maximize" />
        ///     methods.
        /// </summary>
        public int[] ActiveConstraints { get; private set; }

        /// <summary>
        ///     Gets the constraint matrix <c>A</c> for the problem.
        /// </summary>
        public double[,] ConstraintMatrix { get; private set; }

        /// <summary>
        ///     Gets the constraint values <c>b</c> for the problem.
        /// </summary>
        public double[] ConstraintValues => constraintValues;

        /// <summary>
        ///     Gets the constraint tolerances <c>b</c> for the problem.
        /// </summary>
        public double[] ConstraintTolerances => constraintTolerances;

        /// <summary>
        ///     Gets the matrix of quadratic terms of
        ///     the quadratic optimization problem.
        /// </summary>
        public double[,] QuadraticTerms { get; private set; }

        /// <summary>
        ///     Gets the vector of linear terms of the
        ///     quadratic optimization problem.
        /// </summary>
        public double[] LinearTerms { get; private set; }

        /// <summary>
        ///     Finds the minimum value of a function. The solution vector
        ///     will be made available at the <see cref="IOptimizationMethod{TInput, TOutput}.Solution" /> property.
        /// </summary>
        /// <returns>
        ///     Returns <c>true</c> if the method converged to a <see cref="IOptimizationMethod{TInput, TOutput}.Solution" />.
        ///     In this case, the found value will also be available at the
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Value" />
        ///     property.
        /// </returns>
        public override bool Minimize()
        {
            var h = new double[NumberOfVariables, NumberOfVariables];
            var d = new double[NumberOfVariables];

            // Prepare a minimization problem
            for (var i = 0; i < NumberOfVariables; i++)
            {
                for (var j = 0; j < NumberOfVariables; j++)
                {
                    h[i, j] = QuadraticTerms[i, j];
                }
            }

            for (var i = 0; i < LinearTerms.Length; i++)
            {
                d[i] = -LinearTerms[i];
            }

            Status = minimize(h, d);

            Value = Function(Solution);

            return Status == GoldfarbIdnaniStatus.Success;
        }

        /// <summary>
        ///     Finds the maximum value of a function. The solution vector
        ///     will be made available at the <see cref="IOptimizationMethod{TInput, TOutput}.Solution" /> property.
        /// </summary>
        /// <returns>
        ///     Returns <c>true</c> if the method converged to a <see cref="IOptimizationMethod{TInput, TOutput}.Solution" />.
        ///     In this case, the found value will also be available at the
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Value" />
        ///     property.
        /// </returns>
        public override bool Maximize()
        {
            var h = new double[NumberOfVariables, NumberOfVariables];
            var d = new double[NumberOfVariables];

            // Prepare a maximization problem
            for (var i = 0; i < NumberOfVariables; i++)
            {
                for (var j = 0; j < NumberOfVariables; j++)
                {
                    h[i, j] = -QuadraticTerms[i, j];
                }
            }

            for (var i = 0; i < d.Length; i++)
            {
                d[i] = LinearTerms[i];
            }

            Status = minimize(h, d);

            Value = Function(Solution);

            return Status == GoldfarbIdnaniStatus.Success;
        }

        /// <summary>
        ///     Get the exit code returned in the last call to the
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Maximize()" /> or
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Minimize()" /> methods.
        /// </summary>
        public GoldfarbIdnaniStatus Status { get; private set; }

        private void initialize(int numberOfVariables, double[,] hessian, double[] linearTerms,
            double[,] constraintMatrix, double[] b, int numberOfEqualities)
        {
            NumberOfVariables = numberOfVariables;
            this.LinearTerms = linearTerms;
            this.QuadraticTerms = hessian;

            this.ConstraintMatrix = constraintMatrix;
            constraintValues = b;

            NumberOfEqualities = numberOfEqualities;
            NumberOfConstraints = constraintMatrix.GetLength(0);
            r = System.Math.Min(NumberOfVariables, NumberOfConstraints);

            ActiveConstraints = new int[NumberOfConstraints];
            Lagrangian = new double[NumberOfConstraints];
            Solution = new double[NumberOfVariables];

            // initialize work vector
            work = new double[NumberOfVariables];
            iwzv = new double[NumberOfVariables];
            iwrv = new double[r];
            iwuv = new double[r + 1];
            iwrm = new double[r * (r + 1) / 2];
            iwsv = new double[NumberOfConstraints];
            iwnbv = new double[NumberOfConstraints];
        }

        /// <summary>
        ///     Not available.
        /// </summary>
        protected override bool Optimize()
        {
            throw new NotImplementedException();
        }

        private GoldfarbIdnaniStatus minimize(double[,] D, double[] d)
        {
            int numberOfActiveConstraints;
            var activeConstraints = new int[NumberOfConstraints];

            var ierr = 0;

            // Call qpgen2 routine from Turlach's Fortran code
            qpgen2(D, d, activeConstraints, out numberOfActiveConstraints, ref ierr);

            if (ierr != 0)
            {
                if (ierr == 1)
                {
                    return GoldfarbIdnaniStatus.NoPossibleSolution;
                }

                if (ierr == 2)
                {
                    return GoldfarbIdnaniStatus.NonPositiveDefinite;
                }

                throw new InvalidOperationException("Unexpected error.");
            }

            // Extract Lagrange multipliers from the work vector
            ActiveConstraints = activeConstraints.First(numberOfActiveConstraints);

            for (var i = 0; i < ActiveConstraints.Length; i++)
            {
                Lagrangian[ActiveConstraints[i]] = iwuv[i];
            }

            return GoldfarbIdnaniStatus.Success;
        }

        //
        // This routine uses the Goldfarb/Idnani algorithm to solve the
        // following minimization problem:
        //
        //       minimize 1/2 * x^T D x + d^T x
        //       where   A1 x  = b1
        //               A2 x >= b2
        //
        // the matrix D is assumed to be positive definite.  Especially,
        // w.l.o.g. D is assumed to be symmetric. This is slightly different
        // from the original implementation by Berwin A. Turlach.
        // 
        // Input parameter:
        // dmat   nxn matrix, the matrix D from above (dp)
        //        *** WILL BE DESTROYED ON EXIT ***
        //        The user has two possibilities:
        //        a) Give D (ierr=0), in this case we use routines from LINPACK
        //           to decompose D.
        //        b) To get the algorithm started we need R^-1, where D=R^TR.
        //           So if it is cheaper to calculate R^-1 in another way (D may
        //           be a band matrix) then with the general routine, the user
        //           may pass R^{-1}.  Indicated by ierr not equal to zero.
        // dvec   nx1 vector, the vector d from above (dp)
        //        *** WILL BE DESTROYED ON EXIT ***
        //        contains on exit the solution to the initial, i.e.,
        //        unconstrained problem
        // fddmat scalar, the leading dimension of the matrix dmat
        // n      the dimension of dmat and dvec (int)
        // amat   nxq matrix, the matrix A from above (dp) [ A=(A1 A2) ]
        //        *** ENTRIES CORRESPONDING TO EQUALITY CONSTRAINTS MAY HAVE
        //            CHANGED SIGNES ON EXIT ***
        // bvec   qx1 vector, the vector of constants b in the constraints (dp)
        //        [ b = (b1^T b2^T)^T ]
        //        *** ENTRIES CORRESPONDING TO EQUALITY CONSTRAINTS MAY HAVE
        //            CHANGED SIGNES ON EXIT ***
        // fdamat the first dimension of amat as declared in the calling program. 
        //        fdamat >= n !!
        // q      int, the number of constraints.
        // meq    int, the number of equality constraints, 0 <= meq <= q.
        // ierr   int, code for the status of the matrix D:
        //           ierr =  0, we have to decompose D
        //           ierr != 0, D is already decomposed into D=R^TR and we were
        //                      given R^{-1}.
        //
        // Output parameter:
        // sol   nx1 the final solution (x in the notation above)
        // crval scalar, the value of the criterion at the minimum      
        // iact  qx1 vector, the constraints which are active in the final
        //       fit (int)
        // nact  scalar, the number of constraints active in the final fit (int)
        // iter  2x1 vector, first component gives the number of "main" 
        //       iterations, the second one says how many constraints were
        //        deleted after they became active
        //  ierr  int, error code on exit, if
        //          ierr = 0, no problems
        //          ierr = 1, the minimization problem has no solution
        //          ierr = 2, problems with decomposing D, in this case sol
        //                     contains garbage!!
        //
        //  Working space:
        //  work  vector with length at least 2n+r*(r+5)/2 + 2q +1
        //        where r=min(n,q)
        //
        private void qpgen2(double[,] dmat, double[] dvec, int[] iact, out int nact, ref int ierr)
        {
            var solver = new QuadraticProgramSolver(this, dmat, dvec, iact, ierr);

            solver.Run();

            nact = solver.nact;
            ierr = solver.ierr;
        }

        private void dpori(double[,] a)
        {
            var n = NumberOfVariables;

            for (var k = 0; k < n; k++)
            {
                a[k, k] = 1.0 / a[k, k];
                var t = -a[k, k];

                for (var j = 0; j < k; j++)
                {
                    a[k, j] = a[k, j] * t;
                }

                for (var j = k + 1; j < n; j++)
                {
                    t = a[j, k];
                    a[j, k] = 0.0;

                    for (var i = 0; i <= k; i++)
                    {
                        a[j, i] = t * a[k, i] + a[j, i];
                    }
                }
            }
        }

        private void dposl(double[,] a, double[] b)
        {
            var n = NumberOfVariables;

            // solve trans(r)*y = b
            for (var k = 0; k < b.Length; k++)
            {
                for (var i = 0; i < k; i++)
                {
                    b[k] -= b[i] * a[k, i];
                }

                b[k] = b[k] / a[k, k];
            }

            // solve r*x = y
            for (var k = n - 1; k >= 0; k--)
            {
                for (var i = k + 1; i < n; i++)
                {
                    b[k] -= b[i] * a[i, k];
                }

                b[k] /= a[k, k];
            }
        }

        private bool dpofa(double[,] a)
        {
            var n = NumberOfVariables;

            for (var j = 0; j < n; j++)
            {
                double s = 0;
                for (var k = 0; k < j; k++)
                {
                    var t = a[j, k];
                    for (var i = 0; i < k; i++)
                    {
                        t -= a[j, i] * a[k, i];
                    }

                    t = t / a[k, k];

                    a[j, k] = t;
                    s += t * t;
                }

                s = a[j, j] - s;

                // Use a tolerance for positive-definiteness
                if (s <= 1e-14 * System.Math.Abs(a[j, j]))
                {
                    return false;
                }

                a[j, j] = System.Math.Sqrt(s);
            }

            return true;
        }
    }
}