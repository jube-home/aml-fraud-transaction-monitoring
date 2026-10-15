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
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.Math.Optimization;

#pragma warning disable 612, 618

namespace Accord.Statistics.Models.Fields.Learning
{
    /// <summary>
    ///     Base class for Hidden Conditional Random Fields learning algorithms based
    ///     on <see cref="IGradientOptimizationMethod">gradient optimization algorithms</see>.
    /// </summary>
    public abstract class BaseHiddenGradientOptimizationLearning<TData, TOptimizer> :
        BaseHiddenConditionalRandomFieldLearning<TData>,
        ISupervisedLearning<HiddenConditionalRandomField<TData>, TData[], int>, IParallel,
        IHiddenConditionalRandomFieldLearning<TData>, IDisposable
        where TOptimizer : IGradientOptimizationMethod, ISupportsCancellation
    {
        private ForwardBackwardGradient<TData> calculator = new();
        private TOptimizer optimizer;

        /// <summary>
        ///     Constructs a new L-BFGS learning algorithm.
        /// </summary>
        public BaseHiddenGradientOptimizationLearning()
        {
        }

        /// <summary>
        ///     Gets the optimization algorithm being used.
        /// </summary>
        public TOptimizer Optimizer => optimizer;

        /// <summary>
        ///     Gets or sets the amount of the parameter weights
        ///     which should be included in the objective function.
        ///     Default is 0 (do not include regularization).
        /// </summary>
        public double Regularization
        {
            get => calculator.Regularization;
            set => calculator.Regularization = value;
        }

        /// <summary>
        ///     Gets or sets the tolerance value used to determine
        ///     whether the algorithm has converged.
        /// </summary>
        /// <value>The tolerance.</value>
        public double Tolerance { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     performed by the learning algorithm.
        /// </summary>
        /// <value>The maximum iterations.</value>
        public int MaxIterations { get; set; }

        /// <summary>
        ///     Gets or sets whether the algorithm has converged.
        /// </summary>
        /// <value><c>true</c> if this instance has converged; otherwise, <c>false</c>.</value>
        public bool HasConverged { get; private set; }

        /// <summary>
        ///     Online learning is not supported.
        /// </summary>
        public double Run(TData[] observations, int output)
        {
            throw new NotSupportedException("Online learning is not supported.");
        }

        /// <summary>
        ///     Runs the learning algorithm with the specified input
        ///     training observations and corresponding output labels.
        /// </summary>
        /// <param name="observations">The training observations.</param>
        /// <param name="outputs">The observation's labels.</param>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(TData[][] observations, int[] outputs)
        {
            return InnerRun(observations, outputs);
        }

        /// <summary>
        ///     Online learning is not supported.
        /// </summary>
        [Obsolete("Use Run(T[][], int[]) instead.")]
        public double RunEpoch(TData[][] observations, int[] output)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Gets or sets the parallelization options for this algorithm.
        /// </summary>
        /// <value>The parallel options.</value>
        public ParallelOptions ParallelOptions
        {
            get => ((IParallel)calculator).ParallelOptions;
            set => ((IParallel)calculator).ParallelOptions = value;
        }

        /// <summary>
        ///     Inheritors of this class should create the optimization algorithm in this
        ///     method, using the current <see cref="MaxIterations" /> and <see cref="Tolerance" />
        ///     settings.
        /// </summary>
        protected abstract TOptimizer CreateOptimizer();

        /// <summary>
        ///     Runs the learning algorithm.
        /// </summary>
        protected override double InnerRun(TData[][] observations, int[] outputs)
        {
            calculator.Model = Model;
            optimizer = CreateOptimizer();
            optimizer.Function = calculator.Objective;
            optimizer.Gradient = calculator.Gradient;
            optimizer.Token = Token;

            calculator.Inputs = observations;
            calculator.Outputs = outputs;

            optimizer.Solution = Model.Function.Weights;
            HasConverged = optimizer.Minimize();
            Model.Function.Weights = optimizer.Solution;

            // Return negative log-likelihood as error function
            return -Model.LogLikelihood(observations, outputs);
        }

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing,
        ///     releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Releases unmanaged resources and performs other cleanup operations before
        ///     the <see cref="HiddenQuasiNewtonLearning{T}" /> is reclaimed by garbage
        ///     collection.
        /// </summary>
        ~BaseHiddenGradientOptimizationLearning()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed
        ///     and unmanaged resources; <c>false</c> to release only unmanaged
        ///     resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (calculator != null)
                {
                    calculator.Dispose();
                    calculator = null;
                }
            }
        }

        #endregion
    }
}