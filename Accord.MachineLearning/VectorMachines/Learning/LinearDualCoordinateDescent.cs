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
using System.Collections;
using Accord.Math.Random;
using Accord.Statistics.Kernels;
using Accord.Statistics.Models.Regression.Linear;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     Different categories of loss functions that can be used to learn
    ///     <see cref="SupportVectorMachine">support vector machines</see>.
    /// </summary>
    public enum Loss
    {
        /// <summary>
        ///     Hinge-loss function.
        /// </summary>
        L1,

        /// <summary>
        ///     Squared hinge-loss function.
        /// </summary>
        L2
    }

    /// <summary>
    ///     L2-regularized, L1 or L2-loss dual formulation
    ///     Support Vector Machine learning (-s 1 and -s 3).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements a <see cref="SupportVectorMachine" /> learning algorithm
    ///         specifically crafted for linear machines only. It provides a L2-regularized, L1
    ///         or L2-loss coordinate descent learning algorithm for optimizing the dual form of
    ///         learning. The code has been based on liblinear's method <c>solve_l2r_l1l2_svc</c>
    ///         method, whose original description is provided below.
    ///     </para>
    ///     <para>
    ///         Liblinear's solver <c>-s 1</c>: <c>L2R_L2LOSS_SVC_DUAL</c> and <c>-s 3</c>:
    ///         <c>L2R_L1LOSS_SVC_DUAL</c>. A coordinate descent algorithm for L1-loss and
    ///         L2-loss SVM problems in the dual.
    ///     </para>
    ///     <code>
    ///  min_\alpha  0.5(\alpha^T (Q + D)\alpha) - e^T \alpha,
    ///    s.t.      0 &lt;= \alpha_i &lt;= upper_bound_i,
    /// </code>
    ///     <para>
    ///         where Qij = yi yj xi^T xj and
    ///         D is a diagonal matrix
    ///     </para>
    ///     <para>
    ///         In L1-SVM case:
    ///     </para>
    ///     <code>
    ///         upper_bound_i = Cp if y_i = 1
    ///         upper_bound_i = Cn if y_i = -1
    ///         D_ii = 0
    /// </code>
    ///     <para>
    ///         In L2-SVM case:
    ///     </para>
    ///     <code>
    ///         upper_bound_i = INF
    ///         D_ii = 1/(2*Cp)	if y_i = 1
    ///         D_ii = 1/(2*Cn)	if y_i = -1
    /// </code>
    ///     <para>
    ///         Given: x, y, Cp, Cn, and eps as the stopping tolerance
    ///     </para>
    ///     <para>
    ///         See Algorithm 3 of Hsieh et al., ICML 2008.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The next example shows how to solve a multi-class problem using a one-vs-one SVM
    ///         where the binary machines are learned using the Linear Dual Coordinate Descent algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_ldcd" />
    ///     <para>
    ///         The following example shows how to obtain a <see cref="MultipleLinearRegression" />
    ///         from a linear <see cref="SupportVectorMachine" />. It contains exactly the same data
    ///         used in the <see cref="OrdinaryLeastSquares" /> documentation page for
    ///         <see cref="MultipleLinearRegression" />.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\LinearDualCoordinateDescentTest.cs"
    ///         region="doc_linreg" />
    /// </example>
    /// <see cref="SequentialMinimalOptimization{TKernel}" />
    /// <see cref="LinearNewtonMethod" />
    /// <see cref="LinearCoordinateDescent" />
    public class LinearDualCoordinateDescent :
        BaseLinearDualCoordinateDescent<SupportVectorMachine, Linear, double[]>,
        ILinearSupportVectorMachineLearning
    {
        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public LinearDualCoordinateDescent(ISupportVectorMachine<double[]> model, double[][] input, int[] output)
            : base(model, input, output)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LinearDualCoordinateDescent" /> class.
        /// </summary>
        public LinearDualCoordinateDescent()
        {
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override SupportVectorMachine Create(int inputs, Linear kernel)
        {
            return new SupportVectorMachine(inputs) { Kernel = kernel };
        }
    }

    /// <summary>
    ///     L2-regularized, L1 or L2-loss dual formulation
    ///     Support Vector Machine learning (-s 1 and -s 3).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements a <see cref="SupportVectorMachine" /> learning algorithm
    ///         specifically crafted for linear machines only. It provides a L2-regularized, L1
    ///         or L2-loss coordinate descent learning algorithm for optimizing the dual form of
    ///         learning. The code has been based on liblinear's method <c>solve_l2r_l1l2_svc</c>
    ///         method, whose original description is provided below.
    ///     </para>
    ///     <para>
    ///         Liblinear's solver <c>-s 1</c>: <c>L2R_L2LOSS_SVC_DUAL</c> and <c>-s 3</c>:
    ///         <c>L2R_L1LOSS_SVC_DUAL</c>. A coordinate descent algorithm for L1-loss and
    ///         L2-loss SVM problems in the dual.
    ///     </para>
    ///     <code>
    ///  min_\alpha  0.5(\alpha^T (Q + D)\alpha) - e^T \alpha,
    ///    s.t.      0 &lt;= \alpha_i &lt;= upper_bound_i,
    /// </code>
    ///     <para>
    ///         where Qij = yi yj xi^T xj and
    ///         D is a diagonal matrix
    ///     </para>
    ///     <para>
    ///         In L1-SVM case:
    ///     </para>
    ///     <code>
    ///         upper_bound_i = Cp if y_i = 1
    ///         upper_bound_i = Cn if y_i = -1
    ///         D_ii = 0
    /// </code>
    ///     <para>
    ///         In L2-SVM case:
    ///     </para>
    ///     <code>
    ///         upper_bound_i = INF
    ///         D_ii = 1/(2*Cp)	if y_i = 1
    ///         D_ii = 1/(2*Cn)	if y_i = -1
    /// </code>
    ///     <para>
    ///         Given: x, y, Cp, Cn, and eps as the stopping tolerance
    ///     </para>
    ///     <para>
    ///         See Algorithm 3 of Hsieh et al., ICML 2008.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The next example shows how to solve a multi-class problem using a one-vs-one SVM
    ///         where the binary machines are learned using the Linear Dual Coordinate Descent algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_ldcd" />
    /// </example>
    /// <see cref="SequentialMinimalOptimization{TKernel}" />
    /// <see cref="LinearNewtonMethod" />
    /// <see cref="LinearCoordinateDescent" />
    public class LinearDualCoordinateDescent<TKernel> :
        BaseLinearDualCoordinateDescent<SupportVectorMachine<TKernel>, TKernel, double[]>
        where TKernel : struct, ILinear
    {
        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override SupportVectorMachine<TKernel> Create(int inputs, TKernel kernel)
        {
            return new SupportVectorMachine<TKernel>(inputs, kernel);
        }
    }

    /// <summary>
    ///     L2-regularized, L1 or L2-loss dual formulation
    ///     Support Vector Machine learning (-s 1 and -s 3).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements a <see cref="SupportVectorMachine" /> learning algorithm
    ///         specifically crafted for linear machines only. It provides a L2-regularized, L1
    ///         or L2-loss coordinate descent learning algorithm for optimizing the dual form of
    ///         learning. The code has been based on liblinear's method <c>solve_l2r_l1l2_svc</c>
    ///         method, whose original description is provided below.
    ///     </para>
    ///     <para>
    ///         Liblinear's solver <c>-s 1</c>: <c>L2R_L2LOSS_SVC_DUAL</c> and <c>-s 3</c>:
    ///         <c>L2R_L1LOSS_SVC_DUAL</c>. A coordinate descent algorithm for L1-loss and
    ///         L2-loss SVM problems in the dual.
    ///     </para>
    ///     <code>
    ///  min_\alpha  0.5(\alpha^T (Q + D)\alpha) - e^T \alpha,
    ///    s.t.      0 &lt;= \alpha_i &lt;= upper_bound_i,
    /// </code>
    ///     <para>
    ///         where Qij = yi yj xi^T xj and
    ///         D is a diagonal matrix
    ///     </para>
    ///     <para>
    ///         In L1-SVM case:
    ///     </para>
    ///     <code>
    ///         upper_bound_i = Cp if y_i = 1
    ///         upper_bound_i = Cn if y_i = -1
    ///         D_ii = 0
    /// </code>
    ///     <para>
    ///         In L2-SVM case:
    ///     </para>
    ///     <code>
    ///         upper_bound_i = INF
    ///         D_ii = 1/(2*Cp)	if y_i = 1
    ///         D_ii = 1/(2*Cn)	if y_i = -1
    /// </code>
    ///     <para>
    ///         Given: x, y, Cp, Cn, and eps as the stopping tolerance
    ///     </para>
    ///     <para>
    ///         See Algorithm 3 of Hsieh et al., ICML 2008.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The next example shows how to solve a multi-class problem using a one-vs-one SVM
    ///         where the binary machines are learned using the Linear Dual Coordinate Descent algorithm.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_ldcd" />
    /// </example>
    /// <see cref="SequentialMinimalOptimization{TKernel}" />
    /// <see cref="LinearNewtonMethod" />
    /// <see cref="LinearCoordinateDescent" />
    public class LinearDualCoordinateDescent<TKernel, TInput> :
        BaseLinearDualCoordinateDescent<SupportVectorMachine<TKernel, TInput>, TKernel, TInput>
        where TKernel : struct, ILinear<TInput>
        where TInput : IList
#if !NETSTANDARD1_4
        , ICloneable
#endif
    {
        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override SupportVectorMachine<TKernel, TInput> Create(int inputs, TKernel kernel)
        {
            return new SupportVectorMachine<TKernel, TInput>(inputs, kernel);
        }
    }

    /// <summary>
    ///     Base class for Linear Dual Coordinate Descent.
    /// </summary>
    public abstract partial class BaseLinearDualCoordinateDescent<TModel, TKernel, TInput> :
        BaseSupportVectorClassification<TModel, TKernel, TInput>
        where TModel : SupportVectorMachine<TKernel, TInput>
        where TKernel : struct, ILinear<TInput>
        where TInput : IList
#if !NETSTANDARD1_4
        , ICloneable
#endif
    {
        private readonly int max_iter = 1000;

        private double bias;

        private double eps = 0.1;

        private double[] weights;

        /// <summary>
        ///     Constructs a new coordinate descent algorithm for L1-loss and L2-loss SVM dual problems.
        /// </summary>
        public BaseLinearDualCoordinateDescent()
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        protected BaseLinearDualCoordinateDescent(ISupportVectorMachine<TInput> model, TInput[] input, int[] output)
            : base(model, input, output)
        {
        }

        /// <summary>
        ///     Gets or sets the <see cref="Loss" /> cost function that
        ///     should be optimized. Default is
        ///     <see cref="Loss.L2" />.
        /// </summary>
        public Loss Loss { get; set; } = Loss.L2;

        /// <summary>
        ///     Gets the value for the Lagrange multipliers
        ///     (alpha) for every observation vector.
        /// </summary>
        public double[] Lagrange { get; private set; }

        /// <summary>
        ///     Convergence tolerance. Default value is 0.1.
        /// </summary>
        /// <remarks>
        ///     The criterion for completing the model training process. The default is 0.1.
        /// </remarks>
        public double Tolerance
        {
            get => eps;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                eps = value;
            }
        }

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        protected override void InnerRun()
        {
            var x = Inputs;
            var samples = Inputs.Length;
            var kernel = Kernel;
            var dimensions = kernel.GetLength(x);
            Lagrange = new double[samples];
            weights = new double[dimensions];
            var w = weights;
            var c = C;
            var y = Outputs;

            var random = Generator.Random;

            // Lagrange multipliers
            Array.Clear(Lagrange, 0, Lagrange.Length);

            // Zero the weight vector
            bias = 0;

            double[] diag;
            var upper_bound = ComputeUpperBounds(c, out diag);

            var solver = new DualSvmSolver(kernel, x, y, w, Lagrange, diag, upper_bound, random, Token, max_iter, eps);

            solver.Initialize();
            solver.Optimize();
            bias = solver.Bias;

            Model.Weights = new[] { 1.0 };
            Model.SupportVectors = new[] { kernel.CreateVector(w) };
            Model.Threshold = bias;
        }
    }
}