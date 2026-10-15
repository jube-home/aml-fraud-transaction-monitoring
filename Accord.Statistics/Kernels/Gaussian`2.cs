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
using Accord.Math.Distances;

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Composite Gaussian Kernel.
    /// </summary>
    /// <para>
    ///     A composite Gaussian kernel can be used to transform any <see cref="Distance">distance function</see>
    ///     (implementing the <see cref="IDistance{T}" /> interface) into a kernel function. Therefore, it can be
    ///     used to easily define kernels over structures (e.g. matrices, trees, graphs) as long as a suitable
    ///     distance function can be defined over them.
    /// </para>
    /// <example>
    ///     <para>
    ///         Composite Gaussian Kernels can be created using a another kernel or a distance function
    ///         as a base. The first two examples below show how to create a composite Gaussian kernel
    ///         from another kernel function:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Kernels\GaussianLinearTest.cs" region="doc_kernel" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Kernels\GaussianLinearTest.cs" region="doc_kernel_generic" />
    ///     <para>
    ///         The next two examples how how to create a composite Gaussian kernel function from a distance function:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Kernels\GaussianEuclideanTest.cs" region="doc_distance" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Kernels\GaussianEuclideanTest.cs" region="doc_distance_generic" />
    /// </example>
    /// <seealso cref="Gaussian" />
    /// <seealso cref="IKernel" />
    /// <seealso cref="IDistance{T}" />
    [Serializable]
    public struct Gaussian<TDistance, TInput> :
        IKernel<TInput>, IDistance<TInput>, IEstimable<TInput>, ICloneable
        where TInput : class
        where TDistance : IDistance<TInput>, ICloneable
    {
        private double sigma;
        private double gamma;

        private TDistance distance;

        /// <summary>
        ///     Constructs a new Composite Gaussian Kernel
        /// </summary>
        /// <param name="innerKernel">The inner kernel function of the composite kernel.</param>
        public Gaussian(TDistance innerKernel)
            : this(innerKernel, 1)
        {
        }

        /// <summary>
        ///     Constructs a new Composite Gaussian Kernel
        /// </summary>
        /// <param name="innerKernel">The inner kernel function of the composite kernel.</param>
        /// <param name="sigma">The kernel's sigma parameter.</param>
        public Gaussian(TDistance innerKernel, double sigma)
        {
            distance = innerKernel;
            this.sigma = sigma;
            gamma = 1.0 / (2.0 * sigma * sigma);
        }

        /// <summary>
        ///     Gets or sets the sigma value for the kernel. When setting
        ///     sigma, gamma gets updated accordingly (gamma = 0.5/sigma^2).
        /// </summary>
        public double Sigma
        {
            get => sigma;
            set
            {
                sigma = value;
                gamma = 1.0 / (2.0 * sigma * sigma);
            }
        }

        /// <summary>
        ///     Gets or sets the sigma² value for the kernel. When setting
        ///     sigma², gamma gets updated accordingly (gamma = 0.5/sigma²).
        /// </summary>
        public double SigmaSquared
        {
            get => sigma * sigma;
            set
            {
                sigma = System.Math.Sqrt(value);
                gamma = 1.0 / (2.0 * value);
            }
        }

        /// <summary>
        ///     Gets or sets the gamma value for the kernel. When setting
        ///     gamma, sigma gets updated accordingly (gamma = 0.5/sigma^2).
        /// </summary>
        public double Gamma
        {
            get => gamma;
            set
            {
                gamma = value;
                sigma = System.Math.Sqrt(1.0 / (gamma * 2.0));
            }
        }

        /// <summary>
        ///     Gaussian Kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(TInput x, TInput y)
        {
            if (x == y)
            {
                return 1.0;
            }

            var distance = this.distance.Distance(x, y);

            return System.Math.Exp(-gamma * distance);
        }

        /// <summary>
        ///     Computes the squared distance in feature space
        ///     between two points given in input space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>
        ///     Squared distance between <c>x</c> and <c>y</c> in feature (kernel) space.
        /// </returns>
        public double Distance(TInput x, TInput y)
        {
            return Function(x, x) + Function(y, y) - 2 * Function(x, y);
        }

        void IEstimable<TInput>.Estimate(TInput[] inputs)
        {
            Gamma = Gaussian.Estimate(inputs, distance).Gamma;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = (Gaussian<TDistance, TInput>)MemberwiseClone();
            clone.distance = (TDistance)distance.Clone();
            return clone;
        }
    }
}