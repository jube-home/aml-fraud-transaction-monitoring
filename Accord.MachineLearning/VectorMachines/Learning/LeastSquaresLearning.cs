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
using Accord.Math;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning.VectorMachines.Learning
{
    /// <summary>
    ///     Least Squares SVM (LS-SVM) learning algorithm.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.43.6438">
    ///                         Suykens, J. A. K., et al. "Least squares support vector machine classifiers: a large scale
    ///                         algorithm." European Conference on Circuit Theory and Design, ECCTD. Vol. 99. 1999. Available
    ///                         on:
    ///                         http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.43.6438
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="SequentialMinimalOptimization{TKernel}" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    /// <seealso cref="SupportVectorMachine" />
    public class LeastSquaresLearning :
        LeastSquaresLearningBase<SupportVectorMachine<IKernel<double[]>, double[]>, IKernel<double[]>, double[]>
    {
        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete(
            "Please do not pass parameters in the constructor. Use the default constructor and the Learn method instead.")]
        public LeastSquaresLearning(ISupportVectorMachine<double[]> model, double[][] input, int[] output)
            : base(model, input, output)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LeastSquaresLearning" /> class.
        /// </summary>
        public LeastSquaresLearning()
        {
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors
        ///     of this abstract class must define this method so new models
        ///     can be created from the training data.
        /// </summary>
        protected override SupportVectorMachine<IKernel<double[]>, double[]> Create(int inputs,
            IKernel<double[]> kernel)
        {
            return new SupportVectorMachine<IKernel<double[]>, double[]>(inputs, kernel);
        }
    }

    /// <summary>
    ///     Least Squares SVM (LS-SVM) learning algorithm.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.43.6438">
    ///                         Suykens, J. A. K., et al. "Least squares support vector machine classifiers: a large scale
    ///                         algorithm." European Conference on Circuit Theory and Design, ECCTD. Vol. 99. 1999. Available
    ///                         on:
    ///                         http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.43.6438
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="SequentialMinimalOptimization{TKernel}" />
    /// <seealso cref="MulticlassSupportVectorLearning{TKernel}" />
    /// <seealso cref="MultilabelSupportVectorLearning{TKernel}" />
    /// <seealso cref="SupportVectorMachine" />
    public class LeastSquaresLearning<TKernel, TInput> :
        LeastSquaresLearningBase<SupportVectorMachine<TKernel, TInput>, TKernel, TInput>
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

    // TODO: Move to base namespace
    /// <summary>
    ///     Base class for Least Squares SVM (LS-SVM) learning algorithm.
    /// </summary>
    public abstract class LeastSquaresLearningBase<TModel, TKernel, TInput> :
        BaseSupportVectorClassification<TModel, TKernel, TInput>
        where TKernel : IKernel<TInput>
        where TModel : SupportVectorMachine<TKernel, TInput>
#if !NETSTANDARD1_4
        where TInput : ICloneable
#endif
    {
        private KernelFunctionCache<TKernel, TInput> cache;
        private int cacheSize;
        private double[] diagonal;
        private int[] ones;
        private double tolerance = 1e-6;

        /// <summary>
        ///     Constructs a new Least Squares SVM (LS-SVM) learning algorithm.
        /// </summary>
        public LeastSquaresLearningBase()
        {
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        protected LeastSquaresLearningBase(ISupportVectorMachine<TInput> model, TInput[] input, int[] output)
            : base(model, input, output)
        {
        }

        /// <summary>
        ///     Convergence tolerance. Default value is 1e-6.
        /// </summary>
        /// <remarks>
        ///     The criterion for completing the model training process. The default is 1e-6.
        /// </remarks>
        public double Tolerance
        {
            get => tolerance;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                tolerance = value;
            }
        }

        /// <summary>
        ///     Gets or sets the cache size to partially
        ///     stored the kernel matrix. Default is the
        ///     same number of input vectors.
        /// </summary>
        public int CacheSize
        {
            get => cacheSize;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                cacheSize = value;
            }
        }

        /// <summary>
        ///     Runs the main body of the learning algorithm.
        /// </summary>
        protected override void InnerRun()
        {
            var inputs = Inputs;
            var outputs = Outputs;
            ones = Vector.Create(outputs.Length, 1);

            // Create kernel function cache
            diagonal = new double[inputs.Length];
            cache = new KernelFunctionCache<TKernel, TInput>(Kernel, inputs);
            for (var i = 0; i < diagonal.Length; i++)
            {
                diagonal[i] = Kernel.Function(inputs[i], inputs[i]) + 1.0 / C[i];
            }

            // 1. Solve to find nu and eta
            var eta = conjugateGradient(outputs);
            var nu = conjugateGradient(ones);

            // 2. Compute  s = Y' eta
            double s = 0;
            for (var i = 0; i < outputs.Length; i++)
            {
                s += outputs[i] * eta[i];
            }

            // 3. Find solution
            double b = 0;
            for (var i = 0; i < eta.Length; i++)
            {
                b += eta[i];
            }

            b /= s;

            var alpha = new double[nu.Length];
            for (var i = 0; i < alpha.Length; i++)
            {
                alpha[i] = (nu[i] - eta[i] * b) * outputs[i];
            }

            Model.SupportVectors = inputs;
            Model.Weights = alpha;
            Model.Threshold = b;
        }

        private double[] conjugateGradient(int[] B)
        {
            var y = Outputs;
            var x = new double[B.Length];
            var r = new double[B.Length];
            var p = new double[B.Length];
            var H = new double[p.Length];
            var col = new double[Inputs.Length];

            // Initialization
            for (var i = 0; i < B.Length; i++)
            {
                p[i] = r[i] = B[i];
            }

            double norm = 0;
            for (var i = 0; i < r.Length; i++)
            {
                norm += r[i] * r[i];
            }

            double beta = 1;
            var iteration = 0;

            // Repeat to convergence
            while (norm > Tolerance)
            {
                iteration++;

                if (Token.IsCancellationRequested)
                {
                    break;
                }

                if (iteration > B.Length)
                {
                    break;
                }

                for (var i = 0; i < p.Length; i++)
                {
                    p[i] = r[i] + beta * p[i];
                }


                for (var i = 0; i < p.Length; i++)
                {
                    for (var j = 0; j < col.Length; j++)
                    {
                        col[j] = cache.GetOrCompute(i, j) * y[j] * p[j];
                    }

                    double s = 0;
                    for (var j = 0; j < i; j++)
                    {
                        s += col[j];
                    }

                    for (var j = i + 1; j < col.Length; j++)
                    {
                        s += col[j];
                    }

                    H[i] = y[i] < 0 ? -s : s;
                }

                for (var i = 0; i < p.Length; i++)
                {
                    H[i] += diagonal[i] * p[i];
                }


                double sum = 0;
                for (var i = 0; i < p.Length; i++)
                {
                    sum += p[i] * H[i];
                }

                // Update the solution
                var lambda = norm / sum;
                for (var i = 0; i < p.Length; i++)
                {
                    x[i] += lambda * p[i];
                }

                for (var i = 0; i < p.Length; i++)
                {
                    r[i] -= lambda * H[i];
                }

                // Prepare for a next iteration
                double newNorm = 0;
                for (var i = 0; i < r.Length; i++)
                {
                    newNorm += r[i] * r[i];
                }

                beta = newNorm / norm;
                norm = newNorm;
            }

            return x;
        }
    }
}