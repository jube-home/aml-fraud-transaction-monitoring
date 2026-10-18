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
using System.Diagnostics;
using System.Threading;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     Base class for <see cref="SupportVectorMachine" /> regression learning algorithms.
    /// </summary>
    public abstract class BaseSupportVectorRegression<TModel, TKernel, TInput> :
        ISupervisedLearning<TModel, TInput, double>
        where TKernel : IKernel<TInput>
        where TModel : SupportVectorMachine<TKernel, TInput>, ISupportVectorMachine<TInput>
#if !NETSTANDARD1_4
        where TInput : ICloneable
#endif
    {
        // TODO: Remove this field after a few releases. It exists
        // only to provide compatibility with previous releases of
        // the framework.
        private readonly ISupportVectorMachine<TInput> machine;

        private double complexity = 1;

        private bool hasKernelBeenSet;

        // Training data

        // Support Vector Machine parameters
        private TKernel kernel;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseSupportVectorRegression{TModel, TKernel, TInput}" /> class.
        /// </summary>
        public BaseSupportVectorRegression()
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        protected BaseSupportVectorRegression(ISupportVectorMachine<TInput> model, TInput[] input, double[] output)
        {
            machine = model;
            Inputs = input;
            Outputs = output;
            Kernel = (TKernel)model.Kernel;
        }

        /// <summary>
        ///     Gets or sets the cost values associated with each input vector.
        /// </summary>
        protected double[] C { get; set; }

        /// <summary>
        ///     Complexity (cost) parameter C. Increasing the value of C forces the creation
        ///     of a more accurate model that may not generalize well. If this value is not
        ///     set and <see cref="UseComplexityHeuristic" /> is set to <c>true</c>, the framework
        ///     will automatically guess a value for <c>C</c>. If this value is manually set to
        ///     something else, then <see cref="UseComplexityHeuristic" /> will be automatically
        ///     disabled and the given value will be used instead.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The cost parameter C controls the trade off between allowing training
        ///         errors and forcing rigid margins. It creates a soft margin that permits
        ///         some misclassifications. Increasing the value of C increases the cost of
        ///         misclassifying points and forces the creation of a more accurate model
        ///         that may not generalize well.
        ///     </para>
        ///     <para>
        ///         If this value is not set and <see cref="UseComplexityHeuristic" /> is set to
        ///         <c>true</c>, the framework will automatically guess a suitable value for C by
        ///         calling <see cref="Kernel.EstimateComplexity{TKernel, TInput}(TKernel, TInput[])" />.  If this value
        ///         is manually set to something else, then the class will respect the new value
        ///         and automatically disable <see cref="UseComplexityHeuristic" />.
        ///     </para>
        /// </remarks>
        public double Complexity
        {
            get => complexity;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                complexity = value;
                UseComplexityHeuristic = false;
            }
        }

        /// <summary>
        ///     Insensitivity zone ε. Increasing the value of ε can result in fewer
        ///     support vectors in the created model. Default value is 1e-3.
        /// </summary>
        /// <remarks>
        ///     Parameter ε controls the width of the ε-insensitive zone, used to fit the training
        ///     data. The value of ε can affect the number of support vectors used to construct the
        ///     regression function. The bigger ε, the fewer support vectors are selected. On the
        ///     other hand, bigger ε-values results in more flat estimates.
        /// </remarks>
        public double Epsilon { get; set; } = 1e-3;

        /// <summary>
        ///     Gets or sets the individual weight of each sample in the training set. If set
        ///     to <c>null</c>, all samples will be assumed equal weight. Default is <c>null</c>.
        /// </summary>
        public double[] Weights { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the Complexity parameter C
        ///     should be computed automatically by employing an heuristic rule.
        ///     Default is false.
        /// </summary>
        /// <value>
        ///     <c>true</c> if complexity should be computed automatically; otherwise, <c>false</c>.
        /// </value>
        public bool UseComplexityHeuristic { get; set; } = true;

        /// <summary>
        ///     Gets or sets whether initial values for some kernel parameters
        ///     should be estimated from the data, if possible. Default is true.
        /// </summary>
        public bool UseKernelEstimation { get; set; }

        /// <summary>
        ///     Gets whether the machine to be learned
        ///     has a <see cref="Linear" /> kernel.
        /// </summary>
        protected bool IsLinear { get; set; }

        /// <summary>
        ///     Gets or sets the kernel function use to create a
        ///     kernel Support Vector Machine. If this property
        ///     is set, <see cref="UseKernelEstimation" /> will be
        ///     set to false.
        /// </summary>
        public TKernel Kernel
        {
            get => kernel;
            set
            {
                kernel = value;
                UseKernelEstimation = false;
                hasKernelBeenSet = true;
            }
        }

        /// <summary>
        ///     Gets or sets the input vectors for training.
        /// </summary>
        protected TInput[] Inputs { get; private set; }

        /// <summary>
        ///     Gets or sets the output values for each calibration vector.
        /// </summary>
        protected double[] Outputs { get; private set; }

        /// <summary>
        ///     Gets the machine to be taught.
        /// </summary>
        public TModel Model { get; set; }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        [field: NonSerialized]
        public CancellationToken Token { get; set; }

        //protected virtual TModel Create(int inputs, TKernel kernel)
        //{
        //    return SupportVectorLearningHelper.Create<TModel, TInput, TKernel>(inputs, kernel);
        //}

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public TModel Learn(TInput[] x, double[] y, double[] weights = null)
        {
            Tools.CheckArgs(x, y, weights, () =>
            {
                var initialized = false;

                if (kernel == null)
                {
                    kernel = SupportVectorLearningHelper.CreateKernel<TKernel, TInput>(x);
                    initialized = true;
                }

                if (!initialized)
                {
                    if (UseKernelEstimation)
                    {
                        kernel = SupportVectorLearningHelper.EstimateKernel(kernel, x);
                    }
                    else
                    {
                        if (!hasKernelBeenSet)
                        {
                            Trace.TraceWarning(
                                "The Kernel property has not been set and the UseKernelEstimation property is set to false. Please" +
                                " make sure that the default parameters of the kernel are suitable for your application, otherwise the learning" +
                                " will result in a model with very poor performance.");
                        }
                    }
                }

                if (Model == null)
                {
                    Model = Create(SupportVectorLearningHelper.GetNumberOfInputs(kernel, x), kernel);
                }

                Model.Kernel = kernel;
                return Model;
            });

            // Learning data
            Inputs = x;
            Outputs = y;

            try
            {
                // Initialization heuristics
                if (UseComplexityHeuristic)
                {
                    complexity = kernel.EstimateComplexity(Inputs);
                }

                C = new double[Inputs.Length];
                for (var i = 0; i < Outputs.Length; i++)
                {
                    C[i] = complexity;
                }

                if (Weights != null)
                {
                    for (var i = 0; i < C.Length; i++)
                    {
                        C[i] *= Weights[i];
                    }
                }


                InnerRun();

                SupportVectorLearningHelper.CheckOutput(Model);

                return Model;
            }
            finally
            {
                if (machine != null)
                {
                    // TODO: This block is only necessary to offer compatibility
                    // to code written using previous versions of the framework,
                    // and should be removed after a few releases.
                    machine.SupportVectors = Model.SupportVectors;
                    machine.Weights = Model.Weights;
                    machine.Threshold = Model.Threshold;
                    machine.Kernel = Model.Kernel;
                    machine.IsProbabilistic = Model.IsProbabilistic;
                }
            }
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected abstract TModel Create(int inputs, TKernel kernel);

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        protected abstract void InnerRun();

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please use Accord.Math.Optimization.SquareLoss or any other losses of your choice from the Accord.Math.Optimization namespace.")]
        public double ComputeError(TInput[] inputs, double[] expectedOutputs)
        {
            return new SquareLoss(expectedOutputs).Loss(machine.Score(inputs));
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Learn() instead.")]
        public double Run()
        {
            Learn(Inputs, Outputs);
            return new SquareLoss(Outputs).Loss(machine.Score(Inputs));
        }
    }
}