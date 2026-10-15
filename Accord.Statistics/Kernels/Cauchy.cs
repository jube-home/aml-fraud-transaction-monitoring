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
using Accord.Math.Distances;

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Cauchy Kernel.
    /// </summary>
    /// <remarks>
    ///     The Cauchy kernel comes from the Cauchy distribution (Basak, 2008). It is a
    ///     long-tailed kernel and can be used to give long-range influence and sensitivity
    ///     over the high dimension space.
    /// </remarks>
    [Serializable]
    public sealed class Cauchy : KernelBase, IKernel,
        IRadialBasisKernel, ICloneable, IKernel<Sparse<double>>, IDistance<Sparse<double>>
    {
        private double sigma;

        /// <summary>
        ///     Constructs a new Cauchy Kernel.
        /// </summary>
        /// <param name="sigma">The value for sigma.</param>
        public Cauchy(double sigma)
        {
            this.sigma = sigma;
        }

        /// <summary>
        ///     Gets or sets the kernel's sigma value.
        /// </summary>
        public double Sigma
        {
            get => sigma;
            set => sigma = value;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return MemberwiseClone();
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
        public double Distance(Sparse<double> x, Sparse<double> y)
        {
            return Function(x, x) + Function(y, y) - 2 * Function(x, y);
        }

        /// <summary>
        ///     Cauchy Kernel Function
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            // Optimization in case x and y are
            // exactly the same object reference.

            if (x == y)
            {
                return 1.0;
            }

            var norm = 0.0;
            for (var i = 0; i < x.Length; i++)
            {
                var d = x[i] - y[i];
                norm += d * d;
            }

            return 1.0 / (1.0 + norm / sigma);
        }

        /// <summary>
        ///     Cauchy Kernel Function
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(Sparse<double> x, Sparse<double> y)
        {
            // Optimization in case x and y are
            // exactly the same object reference.

            if (x == y)
            {
                return 1.0;
            }

            var norm = Math.Distance.SquareEuclidean(x, y);

            return 1.0 / (1.0 + norm / sigma);
        }

        /// <summary>
        ///     Cauchy Kernel Function
        /// </summary>
        /// <param name="z">Distance <c>z</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double z)
        {
            return 1.0 / (1.0 + z / sigma);
        }
    }
}