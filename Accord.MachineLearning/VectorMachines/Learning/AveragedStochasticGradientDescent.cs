// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Copyright © Leon Bottou, 2010-2017 
// http://leon.bottou.org/projects/sgd
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
// This file is based on the original C++ implementation by Leon Bottou, licensed
// under the LGPL 2.1 license and available at http://leon.bottou.org/projects/sgd
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     Averaged Stochastic Gradient Descent (ASGD) for training linear support vector machines.
    /// </summary>
    /// <see cref="StochasticGradientDescent" />
    /// <see cref="SequentialMinimalOptimization{TKernel}" />
    /// <see cref="LinearNewtonMethod" />
    /// <see cref="LinearDualCoordinateDescent" />
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
    ///         region="doc_learn_multiclass" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
    ///         region="doc_learn_multilabel" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
    ///         region="doc_learn_nonlinear" />
    /// </example>
    public sealed class AveragedStochasticGradientDescent :
        BaseAveragedStochasticGradientDescent<
            SupportVectorMachine, Linear, double[], HingeLoss>
    {
        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override SupportVectorMachine Create(int inputs, Linear kernel)
        {
            return new SupportVectorMachine(inputs) { Kernel = kernel };
        }

        /// <summary>
        ///     Inheritors should implement this function to produce a new instance
        ///     with the same characteristics of the current object.
        /// </summary>
        protected override BaseAveragedStochasticGradientDescent<SupportVectorMachine, Linear, double[], HingeLoss>
            InnerClone()
        {
            return new AveragedStochasticGradientDescent();
        }
    }

    /// <summary>
    ///     Averaged Stochastic Gradient Descent (ASGD) for training linear support vector machines.
    /// </summary>
    /// <see cref="StochasticGradientDescent" />
    /// <see cref="SequentialMinimalOptimization{TKernel}" />
    /// <see cref="LinearNewtonMethod" />
    /// <see cref="LinearDualCoordinateDescent" />
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
    ///         region="doc_learn_multiclass" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
    ///         region="doc_learn_multilabel" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
    ///         region="doc_learn_nonlinear" />
    /// </example>
    public sealed class AveragedStochasticGradientDescent<TKernel> :
        BaseAveragedStochasticGradientDescent<
            SupportVectorMachine<TKernel>, TKernel, double[], HingeLoss>
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

        /// <summary>
        ///     Inheritors should implement this function to produce a new instance
        ///     with the same characteristics of the current object.
        /// </summary>
        /// <returns>BaseAveragedStochasticGradientDescent&lt;SupportVectorMachine&lt;TKernel&gt;, TKernel, System.Double[]&gt;.</returns>
        protected override BaseAveragedStochasticGradientDescent<
            SupportVectorMachine<TKernel>, TKernel, double[], HingeLoss> InnerClone()
        {
            return new AveragedStochasticGradientDescent<TKernel>();
        }
    }

    /// <summary>
    ///     Averaged Stochastic Gradient Descent (ASGD) for training linear support vector machines.
    /// </summary>
    /// <see cref="StochasticGradientDescent" />
    /// <see cref="LinearNewtonMethod" />
    /// <see cref="LinearDualCoordinateDescent" />
    public sealed class AveragedStochasticGradientDescent<TKernel, TInput> :
        BaseAveragedStochasticGradientDescent<
            SupportVectorMachine<TKernel, TInput>, TKernel, TInput, HingeLoss>
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
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
        ///         region="doc_learn_multiclass" />
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
        ///         region="doc_learn_multilabel" />
        ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
        ///         region="doc_learn_nonlinear" />
        /// </example>
        protected override SupportVectorMachine<TKernel, TInput> Create(int inputs, TKernel kernel)
        {
            return new SupportVectorMachine<TKernel, TInput>(inputs, kernel);
        }

        /// <summary>
        ///     Inheritors should implement this function to produce a new instance
        ///     with the same characteristics of the current object.
        /// </summary>
        /// <returns>BaseAveragedStochasticGradientDescent&lt;SupportVectorMachine&lt;TKernel, TInput&gt;, TKernel, TInput&gt;.</returns>
        protected override BaseAveragedStochasticGradientDescent<
            SupportVectorMachine<TKernel, TInput>, TKernel, TInput, HingeLoss> InnerClone()
        {
            return new AveragedStochasticGradientDescent<TKernel, TInput>();
        }
    }

    /// <summary>
    ///     Averaged Stochastic Gradient Descent (ASGD) for training linear support vector machines.
    /// </summary>
    /// <see cref="StochasticGradientDescent" />
    /// <see cref="LinearNewtonMethod" />
    /// <see cref="LinearDualCoordinateDescent" />
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
    ///         region="doc_learn_multiclass" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
    ///         region="doc_learn_multilabel" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\AveragedStochasticGradientDescentTest.cs"
    ///         region="doc_learn_nonlinear" />
    /// </example>
    public sealed class AveragedStochasticGradientDescent<TKernel, TInput, TLoss> :
        BaseAveragedStochasticGradientDescent<
            SupportVectorMachine<TKernel, TInput>, TKernel, TInput, TLoss>
        where TKernel : struct, ILinear<TInput>
        where TInput : IList
#if !NETSTANDARD1_4
        , ICloneable
#endif
        where TLoss : struct, IDifferentiableLoss<bool, double, double>
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

        /// <summary>
        ///     Inheritors should implement this function to produce a new instance
        ///     with the same characteristics of the current object.
        /// </summary>
        /// <returns>BaseAveragedStochasticGradientDescent&lt;SupportVectorMachine&lt;TKernel, TInput&gt;, TKernel, TInput&gt;.</returns>
        protected override BaseAveragedStochasticGradientDescent<
            SupportVectorMachine<TKernel, TInput>, TKernel, TInput, TLoss> InnerClone()
        {
            return new AveragedStochasticGradientDescent<TKernel, TInput, TLoss>();
        }
    }

    /// <summary>
    ///     Base class for Averaged Stochastic Gradient Descent algorithm implementations.
    /// </summary>
    /// <remarks>
    ///     The <see cref="IKernel" /> and <see cref="IDifferentiableLoss{TInput, TScore, TLoss}" />
    ///     are passed as generic parameters (constrained to be structs) because this is the only
    ///     way to force the compiler to emit a separate native code for this class whose performance
    ///     critical sections can be inlined.
    /// </remarks>
    /// <typeparam name="TModel">The type of the model being learned.</typeparam>
    /// <typeparam name="TKernel">The type of the kernel function to use.</typeparam>
    /// <typeparam name="TInput">The type of the input to consider.</typeparam>
    /// <typeparam name="TLoss">The type of the loss function to use.</typeparam>
    /// <seealso cref="StochasticGradientDescent" />
    /// <seealso cref="AveragedStochasticGradientDescent" />
    /// <seealso cref="BinaryLearningBase{TModel, TInput}" />
    public abstract class BaseAveragedStochasticGradientDescent<TModel, TKernel, TInput, TLoss> :
        BinaryLearningBase<TModel, TInput>, ICloneable
        where TModel : SupportVectorMachine<TKernel, TInput>
        where TKernel : struct, ILinear<TInput>
        where TInput : IList
#if !NETSTANDARD1_4
        , ICloneable
#endif
        where TLoss : struct, IDifferentiableLoss<bool, double, double>
    {
        private TKernel kernel;

        private double mu0 = 1;
        private double tstart;
        private double[] w;
        private double wDivisor = 1;
        private double wBias;
        private double[] a;
        private double aDivisor = 1;
        private double wFraction;
        private double aBias;
        private double t;

        private int maxEtaEstimationSamples = 1000;

        private bool bias = true;
        private bool regularizedBias;

        private readonly RelativeConvergence convergence = new();
        private TLoss loss;

        private readonly object syncObject = new();

        /// <summary>
        ///     Gets or sets the kernel function use to create a
        ///     kernel Support Vector Machine.
        /// </summary>
        public TKernel Kernel
        {
            get => kernel;
            set => kernel = value;
        }

        /// <summary>
        ///     Gets or sets the loss function to be used.
        ///     Default is to use the <see cref="LogisticLoss" />.
        /// </summary>
        public TLoss Loss
        {
            get => loss;
            set => loss = value;
        }

        /// <summary>
        ///     Gets or sets the learning rate for the SGD algorithm.
        /// </summary>
        public double LearningRate { get; set; }

        /// <summary>
        ///     Gets or sets the number of iterations that should be
        ///     performed by the algorithm when calling <see cref="Learn" />.
        ///     Default is 0 (iterate until convergence).
        /// </summary>
        public int MaxIterations
        {
            get => convergence.MaxIterations;
            set => convergence.MaxIterations = value;
        }

        /// <summary>
        ///     Please use MaxIterations instead.
        /// </summary>
        [Obsolete("Please use MaxIterations instead.")]
        public int Iterations
        {
            get => MaxIterations;
            set => MaxIterations = value;
        }

        /// <summary>
        ///     Gets or sets the current epoch counter.
        /// </summary>
        public int CurrentEpoch => convergence.CurrentIteration;

        /// <summary>
        ///     Gets or sets the parallelization options for this algorithm.
        /// </summary>
        [field: NonSerialized]
        public ParallelOptions ParallelOptions { get; set; } = new();

        /// <summary>
        ///     Gets or sets a cancellation token that can be used
        ///     to cancel the algorithm while it is running.
        /// </summary>
        public override CancellationToken Token
        {
            get => ParallelOptions.CancellationToken;
            set => ParallelOptions.CancellationToken = value;
        }

        /// <summary>
        ///     Gets or sets the maximum relative change in the watched value
        ///     after an iteration of the algorithm used to detect convergence.
        ///     Default is 1e-3. If set to 0, the loss will not be computed
        ///     during learning and execution will be faster.
        /// </summary>
        public double Tolerance
        {
            get => convergence.Tolerance;
            set => convergence.Tolerance = value;
        }

        /// <summary>
        ///     Gets or sets the lambda regularization term. Default is 0.5.
        /// </summary>
        public double Lambda { get; set; } = 1e-3;

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="BaseAveragedStochasticGradientDescent{TModel, TKernel, TInput, TLoss}" /> class.
        /// </summary>
        protected BaseAveragedStochasticGradientDescent()
        {
            MaxIterations = 0;
            Tolerance = 1e-5;
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected abstract TModel Create(int inputs, TKernel kernel);

        /// <summary>
        ///     Renormalize the weights.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void renorm()
        {
            if (wDivisor != 1.0 || aDivisor != 1.0 || wFraction != 0)
            {
                for (var i = 0; i < w.Length; i++)
                {
                    a[i] = a[i] / aDivisor + w[i] * wFraction / aDivisor;
                    w[i] /= wDivisor;
                }

                wDivisor = 1;
                aDivisor = 1;
                wFraction = 0;
            }
        }

        /// <summary>
        ///     Compute the norm of the weights.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private double wnorm()
        {
            var norm = w.Dot(w) / wDivisor / wDivisor;
            if (regularizedBias)
            {
                norm += wBias * wBias;
            }

            return norm;
        }

        /// <summary>
        ///     Compute the norm of the averaged weights.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private double anorm()
        {
            renorm(); // this is simpler!
            var norm = a.Dot(a);
            if (regularizedBias)
            {
                norm += aBias * aBias;
            }

            return norm;
        }

        /// <summary>
        ///     Perform one iteration of the SGD algorithm with specified gains
        /// </summary>
        private void trainOne(TInput x, bool y, double eta, double mu)
        {
            // Renormalize if needed
            if (aDivisor > 1e5 || wDivisor > 1e5)
            {
                renorm();
            }

            // Forward
            var s = Kernel.Function(w, x) / wDivisor + wBias;

            // SGD update for regularization term
            wDivisor = wDivisor / (1 - eta * Lambda);

            // SGD update for loss term
            var d = -loss.Derivative(y, s);

            var etd = eta * d * wDivisor;
            if (etd != 0)
            {
                Kernel.Product(etd, x, w);
            }

            // Averaging
            if (mu >= 1)
            {
                a.Clear();
                aDivisor = wDivisor;
                wFraction = 1;
            }
            else if (mu > 0)
            {
                if (etd != 0)
                {
                    Kernel.Product(-wFraction * etd, x, a);
                }

                aDivisor = aDivisor / (1 - mu);
                wFraction = wFraction + mu * aDivisor / wDivisor;
            }

            // same for the bias
            if (bias)
            {
                var etab = eta * 0.01;
                if (regularizedBias)
                {
                    wBias *= 1 - etab * Lambda;
                }

                wBias += etab * d;
                aBias += mu * (wBias - aBias);
            }
        }

#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private double score(TInput x)
        {
            var s = Kernel.Function(a, x);
            if (wFraction != 0)
            {
                s += Kernel.Function(w, x) * wFraction;
            }

            return s / aDivisor + aBias;
        }

        private double evaluateEta(TInput[] x, bool[] y, double eta)
        {
            // take a copy of the current state
            var clone = (BaseAveragedStochasticGradientDescent<TModel, TKernel, TInput, TLoss>)Clone();

            var nSamples = System.Math.Min(maxEtaEstimationSamples, x.Length);

            for (var i = 0; i < nSamples; i++)
            {
                clone.trainOne(x[i], y[i], eta, 1.0);
            }

            var loss = clone.evaluateLoss(x, y, nSamples);
            Trace.WriteLine(" - eta " + eta + " gives loss " + loss);

            return loss;
        }

        private double evaluateLoss(TInput[] x, bool[] y, int nSamples)
        {
            const int parallelization_threshold = 10000;

            var lossFunction = Loss;
            double loss = 0;
            double cost;

            if (nSamples < parallelization_threshold)
            {
                for (var i = 0; i < nSamples; i++)
                {
                    loss += lossFunction.Loss(y[i], score(x[i]));
                }
            }
            else
            {
                Parallel.For(0, nSamples, ParallelOptions, () => 0.0,
                    (i, o, s) => s + lossFunction.Loss(y[i], score(x[i])),
                    s =>
                    {
                        lock (syncObject)
                        {
                            loss += s;
                        }
                    });
            }

            loss = -loss / x.Length;
            cost = loss + 0.5 * Lambda * wnorm();
            Trace.WriteLine(" -- loss = " + loss);
            Trace.WriteLine(" -- cost = " + cost);
            return cost;
        }

        private void determineEta0(TInput[] x, bool[] y)
        {
            const double factor = 2.0;

            double loEta = 1;
            var loCost = evaluateEta(x, y, loEta);

            var hiEta = loEta * factor;
            var hiCost = evaluateEta(x, y, hiEta);

            if (loCost < hiCost)
            {
                while (loCost < hiCost)
                {
                    hiEta = loEta;
                    hiCost = loCost;
                    loEta = hiEta / factor;
                    loCost = evaluateEta(x, y, loEta);
                }
            }
            else if (hiCost < loCost)
            {
                while (hiCost < loCost)
                {
                    loEta = hiEta;
                    loCost = hiCost;
                    hiEta = loEta * factor;
                    hiCost = evaluateEta(x, y, hiEta);
                }
            }

            LearningRate = loEta;

            Trace.WriteLine(" - using eta0 = " + LearningRate);
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
        public override TModel Learn(TInput[] x, bool[] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException("Not Supported Weights", "weights");
            }

            if (Model == null)
            {
                Model = Create(SupportVectorLearningHelper.GetNumberOfInputs(Kernel, x), Kernel);
            }

            if (w == null)
            {
                w = new double[Model.NumberOfInputs];
                a = new double[Model.NumberOfInputs];
            }

            if (LearningRate == 0)
            {
                determineEta0(x, y);
            }

            if (tstart == 0)
            {
                tstart = Model.NumberOfInputs;
            }

            do
            {
                Trace.WriteLine("Epoch " + (convergence.CurrentIteration + 1));

                Trace.WriteLine(" - Learning");
                var t1 = Stopwatch.StartNew();

                for (var i = 0; i < x.Length; i++)
                {
                    var eta = LearningRate / System.Math.Pow(1 + Lambda * LearningRate * t, 0.75);
                    var mu = t <= tstart ? 1.0 : mu0 / (1 + mu0 * (t - tstart));
                    trainOne(x[i], y[i], eta, mu);
                    t++;
                }

                t1.Stop();

                Trace.WriteLine(" -- wNorm: " + wnorm());
                Trace.WriteLine(" -- wBias: " + wBias);
                Trace.WriteLine(" -- aNorm: " + anorm());
                Trace.WriteLine(" -- aBias: " + aBias);
                Trace.WriteLine(" -- epoch done in " + t1.Elapsed);

                double loss = 0;
                if (convergence.Tolerance > 0)
                {
                    Trace.WriteLine("- Computing loss");
                    t1 = Stopwatch.StartNew();
                    loss = evaluateLoss(x, y, x.Length);
                    Trace.WriteLine(" -- loss done in " + t1.Elapsed);
                }

                // Check if it has converged
                convergence.NewValue = loss;
            } while (!convergence.HasConverged);

            Model.Weights = new[] { 1.0 };
            Model.SupportVectors = new[] { kernel.CreateVector(w) };
            Model.Threshold = wBias;

            return Model;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            var clone = InnerClone();

            clone.kernel = kernel;

            clone.Lambda = Lambda;
            clone.LearningRate = LearningRate;
            clone.mu0 = mu0;
            clone.tstart = tstart;
            clone.w = w.Copy();
            clone.wDivisor = wDivisor;
            clone.wBias = wBias;
            clone.a = a.Copy();
            clone.aDivisor = aDivisor;
            clone.wFraction = wFraction;
            clone.aBias = aBias;
            clone.t = t;

            clone.loss = loss;
            clone.maxEtaEstimationSamples = maxEtaEstimationSamples;
            clone.bias = bias;
            clone.regularizedBias = regularizedBias;

            clone.Tolerance = Tolerance;
            clone.MaxIterations = MaxIterations;

            return clone;
        }

        /// <summary>
        ///     Inheritors should implement this function to produce a new instance
        ///     with the same characteristics of the current object.
        /// </summary>
        protected abstract BaseAveragedStochasticGradientDescent<TModel, TKernel, TInput, TLoss> InnerClone();
    }
}