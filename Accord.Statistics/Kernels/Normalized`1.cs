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

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Normalized Kernel.
    /// </summary>
    /// <remarks>
    ///     This kernel definition can be used to provide normalized versions
    ///     of other kernel classes, such as the <see cref="Polynomial" />. A
    ///     normalized kernel will always produce distances between -1 and 1.
    /// </remarks>
    [Serializable]
    public sealed class Normalized<TKernel> : KernelBase, IKernel, ICloneable
        where TKernel : IKernel
    {
        private TKernel kernel;
        private double power;

        /// <summary>
        ///     Constructs a new Cauchy Kernel.
        /// </summary>
        /// <param name="kernel">The kernel function to be normalized.</param>
        /// <param name="power">The normalization power to be used. Default is 0.5 (take the square root).</param>
        public Normalized(TKernel kernel, double power = 0.5)
        {
            this.kernel = kernel;
            this.power = power;
        }

        /// <summary>
        ///     Gets or sets the inner kernel function
        ///     whose results should be normalized.
        /// </summary>
        public TKernel Kernel
        {
            get => kernel;
            set => kernel = value;
        }

        /// <summary>
        ///     Gets or sets the normalization power to be
        ///     used. Default is 0.5 (take the square root).
        /// </summary>
        public double Power
        {
            get => power;
            set => power = value;
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
        ///     Normalized Kernel Function
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            return kernel.Function(x, y) / System.Math.Pow(kernel.Function(x, x) * kernel.Function(y, y), power);
        }
    }
}