// Accord.NET Framework (originally)
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Relicensed to MIT per the author's own explicit grant made when the
// upstream project was archived (2020-11-19):
// https://github.com/accord-net/framework
// "I am also willing to change the license of any file where I am the
// single author... to MIT so people can reuse individual pieces of code
// more easily." -- Cesar De Souza
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using Accord.Math.Optimization;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     Support vector regression using <see cref="FanChenLinQuadraticOptimization" /> (LibSVM) algorithm.
    /// </summary>
    public class FanChenLinSupportVectorRegression
        : BaseFanChenLinSupportVectorRegression<SupportVectorMachine, Linear, double[]>
    {
        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override SupportVectorMachine Create(int inputs, Linear kernel)
        {
            return new SupportVectorMachine(inputs);
        }
    }

    /// <summary>
    ///     Support vector regression using <see cref="FanChenLinQuadraticOptimization" /> (LibSVM)  algorithm.
    /// </summary>
    public class FanChenLinSupportVectorRegression<TKernel>
        : BaseFanChenLinSupportVectorRegression<SupportVectorMachine<TKernel>, TKernel, double[]>
        where TKernel : IKernel<double[]>
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
    ///     Support vector regression using <see cref="FanChenLinQuadraticOptimization" />  (LibSVM) algorithm.
    /// </summary>
    public class FanChenLinSupportVectorRegression<TKernel, TInput>
        : BaseFanChenLinSupportVectorRegression<SupportVectorMachine<TKernel, TInput>, TKernel, TInput>
        where TKernel : IKernel<TInput>
        where TInput : ICloneable
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
    ///     Base class for Fan-Chen-Lin (LibSVM) regression algorithms.
    /// </summary>
    public abstract class BaseFanChenLinSupportVectorRegression<TModel, TKernel, TInput>
        : BaseSupportVectorRegression<TModel, TKernel, TInput>
        where TKernel : IKernel<TInput>
        where TModel : SupportVectorMachine<TKernel, TInput>
#if !NETSTANDARD1_4
        where TInput : ICloneable
#endif
    {
        /// <summary>
        ///     Constructs a new one-class support vector learning algorithm.
        /// </summary>
        /// <param name="machine">A support vector machine.</param>
        public BaseFanChenLinSupportVectorRegression(TModel machine)
        {
            Model = machine;
        }

        /// <summary>
        ///     Constructs a new one-class support vector learning algorithm.
        /// </summary>
        public BaseFanChenLinSupportVectorRegression()
        {
        }

        /// <summary>
        ///     Gets the value for the Lagrange multipliers
        ///     (alpha) for every observation vector.
        /// </summary>
        public double[] Lagrange { get; private set; }

        /// <summary>
        ///     Convergence tolerance. Default value is 1e-3.
        /// </summary>
        /// <remarks>
        ///     The criterion for completing the model training process. The default is 0.001.
        /// </remarks>
        public double Tolerance { get; set; } = 1e-3;

        /// <summary>
        ///     Gets or sets a value indicating whether to use
        ///     shrinking heuristics during learning. Default is true.
        /// </summary>
        /// <value>
        ///     <c>true</c> to use shrinking; otherwise, <c>false</c>.
        /// </value>
        public bool Shrinking { get; set; } = true;

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        protected override void InnerRun()
        {
            var x = Inputs;
            var y = Outputs;

            var l = Inputs.Length;
            var alpha2 = new double[2 * l];
            var linear_term = new double[2 * l];
            var signs = new int[2 * l];
            var c = new double[2 * l];
            //int[] sign = new int[2 * l];
            var index = new int[2 * l];

            for (var i = 0; i < l; i++)
            {
                linear_term[i] = Epsilon - y[i];
                signs[i] = +1;
                c[i] = C[i];
                index[i] = i;

                linear_term[i + l] = Epsilon + y[i];
                signs[i + l] = -1;
                c[i + l] = C[i];
                index[i + l] = i;
            }

            Func<int, int[], int, double[], double[]> Q = (i, indices, length, row) =>
            {
                for (var j = 0; j < length; j++)
                {
                    var k = indices[j];
                    row[j] = signs[i] * signs[k] * Kernel.Function(x[index[i]], x[index[k]]);
                }

                return row;
            };

            var s = new FanChenLinQuadraticOptimization(2 * l, Q, linear_term, signs)
            {
                Tolerance = Tolerance,
                Shrinking = Shrinking,
                Solution = alpha2,
                Token = Token,
                UpperBounds = c
            };

            var success = s.Minimize();

            Lagrange = new double[Inputs.Length];
            for (var i = 0; i < Lagrange.Length; i++)
            {
                Lagrange[i] = alpha2[i] - alpha2[i + l];
            }

            var sv = 0;
            for (var i = 0; i < Lagrange.Length; i++)
            {
                if (Lagrange[i] != 0)
                {
                    sv++;
                }
            }

            Model.SupportVectors = new TInput[sv];
            Model.Weights = new double[sv];

            for (int i = 0, j = 0; i < Lagrange.Length; i++)
            {
                if (Lagrange[i] != 0)
                {
                    Model.SupportVectors[j] = Inputs[i];
                    Model.Weights[j] = Lagrange[i];
                    j++;
                }
            }

            Model.Threshold = -s.Rho;

            if (!success)
            {
                throw new ConvergenceException("Convergence could not be attained. " +
                                               "Please reduce the cost of misclassification errors by reducing " +
                                               "the complexity parameter C or try a different kernel function.");
            }
        }
    }
}