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
using Accord.Math;
using Accord.Math.Random;
using Accord.Statistics.Kernels;
using Accord.Statistics.Models.Regression.Linear;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     L1-regularized L2-loss support vector
    ///     Support Vector Machine learning (-s 5).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements a <see cref="SupportVectorMachine" /> learning algorithm
    ///         specifically crafted for linear machines only. It provides a L1-regularized,
    ///         L2-loss coordinate descent learning algorithm for optimizing the primal form of
    ///         learning. The code has been based on liblinear's method <c>solve_l1r_l2_svc</c>
    ///         method, whose original description is provided below.
    ///     </para>
    ///     <para>
    ///         Liblinear's solver <c>-s 5</c>: <c>L1R_L2LOSS_svc</c>. A coordinate descent
    ///         algorithm for L2-loss SVM problems in the primal.
    ///     </para>
    ///     <code>
    ///  min_w \sum |wj| + C \sum max(0, 1-yi w^T xi)^2,
    /// </code>
    ///     <para>
    ///         Given: x, y, Cp, Cn and eps as the stopping tolerance
    ///     </para>
    ///     <para>
    ///         See Yuan et al. (2010) and appendix of LIBLINEAR paper, Fan et al. (2008)
    ///     </para>
    /// </remarks>
    /// <examples>
    ///     <para>
    ///         The following example shows how to obtain a <see cref="MultipleLinearRegression" />
    ///         from a linear <see cref="SupportVectorMachine" />. It contains exactly the same data
    ///         used in the <see cref="OrdinaryLeastSquares" /> documentation page for
    ///         <see cref="MultipleLinearRegression" />.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\LinearCoordinateDescentTest.cs"
    ///         region="doc_linreg" />
    /// </examples>
    /// <see cref="SequentialMinimalOptimization{TKernel}" />
    /// <see cref="LinearNewtonMethod" />
    /// <see cref="LinearDualCoordinateDescent" />
    public class LinearCoordinateDescent :
        BaseLinearCoordinateDescent<SupportVectorMachine, Linear>,
        ILinearSupportVectorMachineLearning
    {
        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public LinearCoordinateDescent(SupportVectorMachine model, double[][] input, int[] output)
            : base(model, input, output)
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public LinearCoordinateDescent(KernelSupportVectorMachine model, double[][] input, int[] output)
            : base(model, input, output)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LinearCoordinateDescent" /> class.
        /// </summary>
        public LinearCoordinateDescent()
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
    ///     L1-regularized L2-loss support vector
    ///     Support Vector Machine learning (-s 5).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements a <see cref="SupportVectorMachine" /> learning algorithm
    ///         specifically crafted for linear machines only. It provides a L1-regularized,
    ///         L2-loss coordinate descent learning algorithm for optimizing the primal form of
    ///         learning. The code has been based on liblinear's method <c>solve_l1r_l2_svc</c>
    ///         method, whose original description is provided below.
    ///     </para>
    ///     <para>
    ///         Liblinear's solver <c>-s 5</c>: <c>L1R_L2LOSS_svc</c>. A coordinate descent
    ///         algorithm for L2-loss SVM problems in the primal.
    ///     </para>
    ///     <code>
    ///  min_w \sum |wj| + C \sum max(0, 1-yi w^T xi)^2,
    /// </code>
    ///     <para>
    ///         Given: x, y, Cp, Cn and eps as the stopping tolerance
    ///     </para>
    ///     <para>
    ///         See Yuan et al. (2010) and appendix of LIBLINEAR paper, Fan et al. (2008)
    ///     </para>
    /// </remarks>
    /// <see cref="SequentialMinimalOptimization{TKernel}" />
    /// <see cref="LinearNewtonMethod" />
    /// <see cref="LinearDualCoordinateDescent" />
    public class LinearCoordinateDescent<TKernel> :
        BaseLinearCoordinateDescent<SupportVectorMachine<TKernel>, TKernel>
        where TKernel : ILinear
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

    // TODO: Add support for sparse linear kernels
    //public class LinearCoordinateDescent<TKernel, TInput> :
    //    BaseLinearCoordinateDescent<SupportVectorMachine<TKernel, TInput>, TKernel, TInput>
    //    where TKernel : struct, ILinear<TInput>
    //    where TInput : ICloneable, IList
    //{
    //    /// <summary>
    //    /// Creates an instance of the model to be learned. Inheritors
    //    /// of this abstract class must define this method so new models
    //    /// can be created from the training data.
    //    /// </summary>
    //    protected override SupportVectorMachine<TKernel, TInput> Create(int inputs, TKernel kernel)
    //    {
    //        return new SupportVectorMachine<TKernel, TInput>(inputs, kernel);
    //    }
    //}

    ///// <summary>
    /////   Base class for linear coordinate descent learning algorithm.
    ///// </summary>
    ///// 
    //public abstract class BaseLinearCoordinateDescent<TModel, TKernel, TInput> :
    //    BaseSupportVectorClassification<TModel, TKernel, TInput>
    //    where TModel : SupportVectorMachine<TKernel, TInput>
    //    where TKernel : struct, ILinear<TInput>
    //    where TInput : ICloneable, IList

    /// <summary>
    ///     Base class for linear coordinate descent learning algorithm.
    /// </summary>
    public abstract partial class BaseLinearCoordinateDescent<TModel, TKernel> :
        BaseSupportVectorClassification<TModel, TKernel, double[]>
        where TModel : SupportVectorMachine<TKernel, double[]>
        where TKernel : ILinear
    {
        private double[] bias;
        private int biasIndex;
        private double eps = 0.1;
        private double[] weights;

        /// <summary>
        ///     Constructs a new coordinate descent algorithm for L1-loss and L2-loss SVM dual problems.
        /// </summary>
        public BaseLinearCoordinateDescent()
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete]
        protected BaseLinearCoordinateDescent(ISupportVectorMachine<double[]> model, double[][] input, int[] output)
            : base(model, input, output)
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        protected BaseLinearCoordinateDescent(TModel model, double[][] input, int[] output)
            : base(model, input, output)
        {
        }

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
            var samples = Inputs.Length;
            var dimension = Inputs[0].Length;

            // Lagrange multipliers
            Lagrange = new double[samples];
            weights = new double[dimension + 1];
            bias = new double[samples];
            biasIndex = dimension;

            var w = weights;


            var random = Generator.Random;

            // Lagrange multipliers
            Array.Clear(Lagrange, 0, Lagrange.Length);
            Array.Clear(w, 0, w.Length);

            var solver = new L1RegularizedL2LossSolver(w, bias, biasIndex, random, Token, eps, C, Inputs, Outputs);

            solver.Initialize();
            solver.Optimize();
            solver.TraceObjective();

            Model.SupportVectors = new[] { w.First(Model.NumberOfInputs) };
            Model.Weights = new[] { 1.0 };
            Model.Threshold = w[biasIndex];
        }
    }
}