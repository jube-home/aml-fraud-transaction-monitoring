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
using Accord.Math;
using Accord.Math.Random;
using Accord.Statistics.Kernels;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     L2-regularized logistic regression (probabilistic support
    ///     vector machine) learning algorithm in the dual form (-s 7).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements a <see cref="SupportVectorMachine" /> learning algorithm
    ///         specifically crafted for probabilistic linear machines only. It provides a L2-
    ///         regularized coordinate descent learning algorithm for optimizing the dual form
    ///         of the learning problem. The code has been based on liblinear's method
    ///         <c>solve_l2r_lr_dual</c> method, whose original description is provided below.
    ///     </para>
    ///     <para>
    ///         Liblinear's solver <c>-s 7</c>: <c>L2R_LR_DUAL</c>. A coordinate descent
    ///         algorithm for the dual of L2-regularized logistic regression problems.
    ///     </para>
    ///     <code>
    ///   min_\alpha  0.5(\alpha^T Q \alpha) + \sum \alpha_i log (\alpha_i) 
    ///     + (upper_bound_i - \alpha_i) log (upper_bound_i - \alpha_i),
    ///     
    ///    s.t.      0 &lt;= \alpha_i &lt;= upper_bound_i,
    /// </code>
    ///     <para>
    ///         where <c>Qij = yi yj xi^T xj</c> and
    ///     </para>
    ///     <code>
    ///  upper_bound_i = Cp if y_i = 1
    ///  upper_bound_i = Cn if y_i = -1
    /// </code>
    ///     <para>
    ///         Given: x, y, Cp, Cn, and eps as the stopping tolerance
    ///     </para>
    ///     <para>
    ///         See Algorithm 5 of Yu et al., MLJ 2010.
    ///     </para>
    /// </remarks>
    /// <examples>
    ///     <para>
    ///         Probabilistic SVMs are exactly the same as logistic regression models
    ///         trained using a large-margin decision criteria. As such, any linear SVM
    ///         learning algorithm can be used to obtain <see cref="LogisticRegression" />
    ///         objects as well.
    ///     </para>
    ///     <para>
    ///         The following example shows how to obtain a <see cref="LogisticRegression" />
    ///         from a probabilistic linear <see cref="SupportVectorMachine" />. It contains
    ///         exactly the same data used in the <see cref="IterativeReweightedLeastSquares" />
    ///         documentation page for <see cref="LogisticRegression" />.
    ///     </para>
    ///     <code
    ///         source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\Probabilistic\ProbabilisticDualCoordinateDescentTest.cs"
    ///         region="doc_logreg" />
    /// </examples>
    /// <see cref="SequentialMinimalOptimization{TKernel}" />
    /// <see cref="ProbabilisticNewtonMethod" />
    public class ProbabilisticDualCoordinateDescent :
        BaseProbabilisticDualCoordinateDescent<SupportVectorMachine, Linear, double[]>,
        ILinearSupportVectorMachineLearning
    {
        /// <summary>
        ///     Constructs a new Newton method algorithm for L2-regularized
        ///     logistic regression (probabilistic linear SVMs) dual problems.
        /// </summary>
        public ProbabilisticDualCoordinateDescent()
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public ProbabilisticDualCoordinateDescent(SupportVectorMachine model, double[][] input, int[] output)
            : base(model, input, output)
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
    ///     L2-regularized logistic regression (probabilistic support
    ///     vector machine) learning algorithm in the dual form (-s 7).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements a <see cref="SupportVectorMachine" /> learning algorithm
    ///         specifically crafted for probabilistic linear machines only. It provides a L2-
    ///         regularized coordinate descent learning algorithm for optimizing the dual form
    ///         of the learning problem. The code has been based on liblinear's method
    ///         <c>solve_l2r_lr_dual</c> method, whose original description is provided below.
    ///     </para>
    ///     <para>
    ///         Liblinear's solver <c>-s 7</c>: <c>L2R_LR_DUAL</c>. A coordinate descent
    ///         algorithm for the dual of L2-regularized logistic regression problems.
    ///     </para>
    ///     <code>
    ///   min_\alpha  0.5(\alpha^T Q \alpha) + \sum \alpha_i log (\alpha_i) 
    ///     + (upper_bound_i - \alpha_i) log (upper_bound_i - \alpha_i),
    ///     
    ///    s.t.      0 &lt;= \alpha_i &lt;= upper_bound_i,
    /// </code>
    ///     <para>
    ///         where <c>Qij = yi yj xi^T xj</c> and
    ///     </para>
    ///     <code>
    ///  upper_bound_i = Cp if y_i = 1
    ///  upper_bound_i = Cn if y_i = -1
    /// </code>
    ///     <para>
    ///         Given: x, y, Cp, Cn, and eps as the stopping tolerance
    ///     </para>
    ///     <para>
    ///         See Algorithm 5 of Yu et al., MLJ 2010.
    ///     </para>
    /// </remarks>
    /// <examples>
    ///     <para>
    ///         Probabilistic SVMs are exactly the same as logistic regression models
    ///         trained using a large-margin decision criteria. As such, any linear SVM
    ///         learning algorithm can be used to obtain <see cref="LogisticRegression" />
    ///         objects as well.
    ///     </para>
    ///     <para>
    ///         The following example shows how to obtain a <see cref="LogisticRegression" />
    ///         from a probabilistic linear <see cref="SupportVectorMachine" />. It contains
    ///         exactly the same data used in the <see cref="IterativeReweightedLeastSquares" />
    ///         documentation page for <see cref="LogisticRegression" />.
    ///     </para>
    ///     <code
    ///         source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\Probabilistic\ProbabilisticDualCoordinateDescentTest.cs"
    ///         region="doc_logreg" />
    /// </examples>
    /// <see cref="SequentialMinimalOptimization{TKernel}" />
    /// <see cref="ProbabilisticNewtonMethod" />
    public class ProbabilisticDualCoordinateDescent<TKernel, TInput> :
        BaseProbabilisticDualCoordinateDescent<SupportVectorMachine<TKernel, TInput>, TKernel, TInput>
        where TKernel : ILinear<TInput>
    {
        /// <summary>
        ///     Constructs a new Newton method algorithm for L2-regularized
        ///     logistic regression (probabilistic linear SVMs) dual problems.
        /// </summary>
        public ProbabilisticDualCoordinateDescent()
        {
        }

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
    ///     Base class for L2-regularized logistic regression (probabilistic support
    ///     vector machine) learning algorithm in the dual form (-s 7).
    /// </summary>
    public abstract partial class BaseProbabilisticDualCoordinateDescent<TModel, TKernel, TInput> :
        BaseSupportVectorClassification<TModel, TKernel, TInput>
        where TModel : SupportVectorMachine<TKernel, TInput>
        where TKernel : ILinear<TInput>
    {
        private int biasIndex;

        private double[] weights;

        /// <summary>
        ///     Constructs a new Newton method algorithm for L2-regularized
        ///     logistic regression (probabilistic linear SVMs) dual problems.
        /// </summary>
        protected BaseProbabilisticDualCoordinateDescent()
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public BaseProbabilisticDualCoordinateDescent(SupportVectorMachine<TKernel, TInput> model, TInput[] input,
            int[] output)
            : base(model, input, output)
        {
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations that should
        ///     be performed until the algorithm stops. Default is 1000.
        /// </summary>
        public int MaximumIterations { get; set; } = 1000;

        /// <summary>
        ///     Gets or sets the maximum number of inner iterations that can
        ///     be performed by the inner solver algorithm. Default is 100.
        /// </summary>
        public int MaximumNewtonIterations { get; set; } = 100;

        /// <summary>
        ///     Convergence tolerance. Default value is 0.1.
        /// </summary>
        /// <remarks>
        ///     The criterion for completing the model training process. The default is 0.1.
        /// </remarks>
        public double Tolerance { get; set; } = 0.1;

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        protected override void InnerRun()
        {
            var parameters = Kernel.GetLength(Inputs);
            weights = new double[parameters + 1];
            biasIndex = parameters;

            var inputs = Inputs;
            var y = Outputs;
            var w = weights;

            var upper_bound = C; // = { Cn, 0, Cp };

            var rand = Generator.Random;

            var solver = new LogisticDualSolver(Kernel, inputs, y, w, biasIndex, upper_bound, rand, Token,
                Tolerance, MaximumIterations, MaximumNewtonIterations);

            solver.Initialize();
            solver.Optimize();
            solver.TraceObjective();

            Model.Weights = new[] { 1.0 };
            Model.SupportVectors = new[] { Kernel.CreateVector(w.First(w.Length - 1)) };
            Model.Threshold = weights[biasIndex];
            Model.IsProbabilistic = true;
        }
    }
}