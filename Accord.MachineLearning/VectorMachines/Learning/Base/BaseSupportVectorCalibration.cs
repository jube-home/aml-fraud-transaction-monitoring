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
using System.Threading;
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     Base class for <see cref="SupportVectorMachine" /> calibration algorithms.
    /// </summary>
    public abstract class BaseSupportVectorCalibration<TModel, TKernel, TInput> :
        ISupervisedBinaryLearning<TModel, TInput>
        where TKernel : IKernel<TInput>
        where TModel : SupportVectorMachine<TKernel, TInput>
#if !NETSTANDARD1_4
        where TInput : ICloneable
#endif
    {
        // Support Vector Machine parameters

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseSupportVectorCalibration{TModel, TKernel, TInput}" /> class.
        /// </summary>
        /// <param name="machine">The machine to be calibrated.</param>
        protected BaseSupportVectorCalibration(TModel machine)
        {
            this.Model = machine;
            Kernel = machine.Kernel;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseSupportVectorCalibration{TModel, TKernel, TInput}" /> class.
        /// </summary>
        /// <param name="machine">The machine to be calibrated.</param>
        protected BaseSupportVectorCalibration(ISupportVectorMachine<TInput> machine)
        {
            this.Model = (TModel)machine;
            Kernel = (TKernel)machine.Kernel;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseSupportVectorCalibration{TModel, TKernel, TInput}" /> class.
        /// </summary>
        public BaseSupportVectorCalibration()
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        protected BaseSupportVectorCalibration(ISupportVectorMachine<double[]> model, TInput[] input, int[] output)
        {
            Model = model as TModel;
            Input = input;
            Output = Classes.Decide(output);
        }

        /// <summary>
        ///     Gets or sets the input vectors for training.
        /// </summary>
        protected TInput[] Input { get; set; }

        /// <summary>
        ///     Gets or sets the output labels for each training vector.
        /// </summary>
        protected bool[] Output { get; set; }

        /// <summary>
        ///     Gets whether the machine being learned is linear.
        /// </summary>
        public bool IsLinear => Kernel is ILinear;

        /// <summary>
        ///     Gets the machine's <see cref="IKernel" /> function.
        /// </summary>
        protected TKernel Kernel { get; }

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

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public TModel Learn(TInput[] x, int[] y, double[] weights = null)
        {
            return Learn(x, Classes.Decide(y), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public TModel Learn(TInput[] x, int[][] y, double[] weights = null)
        {
            return Learn(x, Classes.Decide(y.GetColumn(0)), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public TModel Learn(TInput[] x, bool[][] y, double[] weights = null)
        {
            return Learn(x, y.GetColumn(0), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public TModel Learn(TInput[] x, bool[] y, double[] weights = null)
        {
            Tools.CheckArgs(x, y, weights, () =>
                {
                    if (Model == null)
                    {
                        var numberOfInputs = SupportVectorLearningHelper.GetNumberOfInputs(Kernel, x);
                        Model = Create(numberOfInputs, Kernel);
                    }

                    return Model;
                },
                true);

            InnerRun();

            return Model;
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected virtual TModel Create(int inputs, TKernel kernel)
        {
            TModel result = null;
            var type = typeof(TModel);
            if (type == typeof(SupportVectorMachine))
            {
                result = new SupportVectorMachine(inputs) as TModel;
            }
#if !NETSTANDARD1_4
#pragma warning disable 0618
            else if (type == typeof(KernelSupportVectorMachine))
            {
                result = new KernelSupportVectorMachine(kernel as IKernel, inputs) as TModel;
            }
#pragma warning restore 0618
#endif
            else if (type == typeof(SupportVectorMachine<TKernel, TInput>))
            {
                result = new SupportVectorMachine<TKernel, TInput>(inputs, kernel) as TModel;
            }

            if (result == null)
            {
                throw new NotSupportedException(
                    "If you are implementing your own support vector machine type, please override the Create method in your learning algorithm to instruct the framework how to instantiate a type of your new class.");
            }

            return result;
        }

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
            return Learn(x, Classes.Decide(y), weights);
        }

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        protected abstract void InnerRun();

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Learn() instead.")]
        public virtual double Run()
        {
            Learn(Input, Output);
            var classifier = (IClassifier<TInput, bool>)Model;
            return new ZeroOneLoss(Output).Loss(classifier.Decide(Input));
        }
    }
}