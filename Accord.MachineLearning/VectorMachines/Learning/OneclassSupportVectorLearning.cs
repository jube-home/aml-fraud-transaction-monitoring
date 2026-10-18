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
using Accord.Math;
using Accord.Math.Optimization;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     One-class Support Vector Machine learning algorithm.
    /// </summary>
    /// <example>
    ///     <para>The following example shows how to use an one-class SVM.</para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\OneclassSupportVectorLearningTest.cs"
    ///         region="doc_learn" />
    /// </example>
    /// <seealso cref="SupportVectorMachine" />
    /// <seealso cref="ProbabilisticOutputCalibration" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
#pragma warning disable 0618
    [Obsolete("Please use OneclassSupportVectorLearning<TKernel> instead.")]
    public class OneclassSupportVectorLearning
        : BaseOneclassSupportVectorLearning<ISupportVectorMachine<double[]>, IKernel<double[]>, double[]>
    {
        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public OneclassSupportVectorLearning(KernelSupportVectorMachine model, double[][] input)
            : base(model, input)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OneclassSupportVectorLearning" /> class.
        /// </summary>
        public OneclassSupportVectorLearning()
        {
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override ISupportVectorMachine<double[]> Create(int inputs, IKernel<double[]> kernel)
        {
            if (kernel is Linear)
            {
                return new SupportVectorMachine(inputs) { Kernel = (Linear)kernel };
            }

            return new SupportVectorMachine<IKernel<double[]>>(inputs, kernel);
        }
    }
#pragma warning restore 0618

    /// <summary>
    ///     One-class Support Vector Machine learning algorithm.
    /// </summary>
    /// <example>
    ///     <para>The following example shows how to use an one-class SVM.</para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\OneclassSupportVectorLearningTest.cs"
    ///         region="doc_learn" />
    /// </example>
    /// <seealso cref="SupportVectorMachine" />
    /// <seealso cref="ProbabilisticOutputCalibration" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    public class OneclassSupportVectorLearning<TKernel>
        : BaseOneclassSupportVectorLearning<SupportVectorMachine<TKernel>, TKernel, double[]>
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
    ///     One-class Support Vector Machine learning algorithm.
    /// </summary>
    /// <example>
    ///     <para>The following example shows how to use an one-class SVM.</para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\OneclassSupportVectorLearningTest.cs"
    ///         region="doc_learn" />
    /// </example>
    /// <seealso cref="SupportVectorMachine" />
    /// <seealso cref="ProbabilisticOutputCalibration" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    public class OneclassSupportVectorLearning<TKernel, TInput>
        : BaseOneclassSupportVectorLearning<SupportVectorMachine<TKernel, TInput>, TKernel, TInput>
        where TKernel : IKernel<TInput>
#if !NETSTANDARD1_4
        where TInput : ICloneable
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
    ///     One-class Support Vector Machine Learning Algorithm.
    /// </summary>
    public abstract class BaseOneclassSupportVectorLearning<TModel, TKernel, TInput>
        : IUnsupervisedLearning<TModel, TInput, bool>
        where TKernel : IKernel<TInput>
        where TModel : ISupportVectorMachine<TInput>
    // TODO: after a few releases, the TModel constraint should be changed to:
    // where TModel : SupportVectorMachine<TKernel, TInput>, ISupportVectorMachine<TInput>
    {
        // TODO: Remove this field after a few releases. It exists
        // only to provide compatibility with previous releases of
        // the framework.
        private readonly ISupportVectorMachine<TInput> machine;

        private bool hasKernelBeenSet;
        private TInput[] inputs;

        private TKernel kernel;

        /// <summary>
        ///     Constructs a new one-class support vector learning algorithm.
        /// </summary>
        /// <param name="machine">A support vector machine.</param>
        public BaseOneclassSupportVectorLearning(TModel machine)
        {
            Model = machine;
        }

        /// <summary>
        ///     Constructs a new one-class support vector learning algorithm.
        /// </summary>
        public BaseOneclassSupportVectorLearning()
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        protected BaseOneclassSupportVectorLearning(ISupportVectorMachine<TInput> model, TInput[] input)
        {
            machine = model;
            inputs = input;
            Kernel = (TKernel)model.Kernel;
        }

        /// <summary>
        ///     Gets or sets the classifier being learned.
        /// </summary>
        public TModel Model { get; set; }

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
        ///     Gets or sets whether initial values for some kernel parameters
        ///     should be estimated from the data, if possible. Default is true.
        /// </summary>
        public bool UseKernelEstimation { get; set; }

        /// <summary>
        ///     Gets the value for the Lagrange multipliers
        ///     (alpha) for every observation vector.
        /// </summary>
        public double[] Lagrange { get; private set; }

        /// <summary>
        ///     Convergence tolerance. Default value is 1e-2.
        /// </summary>
        /// <remarks>
        ///     The criterion for completing the model training process. The default is 0.01.
        /// </remarks>
        public double Tolerance { get; set; } = 0.001;

        /// <summary>
        ///     Gets or sets a value indicating whether to use
        ///     shrinking heuristics during learning. Default is true.
        /// </summary>
        /// <value>
        ///     <c>true</c> to use shrinking; otherwise, <c>false</c>.
        /// </value>
        public bool Shrinking { get; set; } = true;

        /// <summary>
        ///     Controls the number of outliers accepted by the algorithm. This
        ///     value provides an upper bound on the fraction of training errors
        ///     and a lower bound of the fraction of support vectors. Default is 0.5
        /// </summary>
        /// <remarks>
        ///     The summary description is given in Chang and Lin,
        ///     "LIBSVM: A Library for Support Vector Machines", 2013.
        /// </remarks>
        public double Nu { get; set; } = 0.5;

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        [field: NonSerialized]
        public CancellationToken Token { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public TModel Learn(TInput[] x, double[] weights = null)
        {
            Tools.CheckArgs(x, weights, () =>
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

            try
            {
                inputs = x;
                var zeros = new double[inputs.Length];
                var ones = Vector.Ones<int>(inputs.Length);
                Lagrange = Vector.Ones<double>(inputs.Length);

                var l = inputs.Length;
                var n = (int)(Nu * l); // # of alpha's at upper bound

                for (var i = 0; i < n; i++)
                {
                    Lagrange[i] = 1;
                }

                if (n < inputs.Length)
                {
                    Lagrange[n] = Nu * l - n;
                }

                for (var i = n + 1; i < l; i++)
                {
                    Lagrange[i] = 0;
                }

                Func<int, int[], int, double[], double[]> Q = (i, indices, length, row) =>
                {
                    for (var j = 0; j < length; j++)
                    {
                        row[j] = Kernel.Function(x[i], x[indices[j]]);
                    }

                    return row;
                };

                var s = new FanChenLinQuadraticOptimization(Lagrange.Length, Q, zeros, ones)
                {
                    Tolerance = Tolerance,
                    Shrinking = Shrinking,
                    Solution = Lagrange,
                    Token = Token
                };

                var success = s.Minimize();

                var sv = 0;
                for (var i = 0; i < Lagrange.Length; i++)
                {
                    if (Lagrange[i] > 0)
                    {
                        sv++;
                    }
                }

                Model.SupportVectors = new TInput[sv];
                Model.Weights = new double[sv];

                for (int i = 0, j = 0; i < Lagrange.Length; i++)
                {
                    if (Lagrange[i] > 0)
                    {
                        Model.SupportVectors[j] = inputs[i];
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

            return Model;
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected abstract TModel Create(int inputs, TKernel kernel);

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete]
        public double Run()
        {
            Learn(inputs);
            return new LogLikelihoodLoss().Loss(Model.Score(inputs));
        }
    }
}