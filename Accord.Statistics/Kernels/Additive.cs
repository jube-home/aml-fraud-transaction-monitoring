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
    ///     Additive combination of kernels.
    /// </summary>
    [Serializable]
    public sealed class Additive : KernelBase, IKernel
    {
        /// <summary>
        ///     Constructs a new additive kernel.
        /// </summary>
        /// <param name="kernels">Kernels to combine.</param>
        public Additive(params IKernel[] kernels)
        {
            this.Kernels = kernels;
            Weights = new double[kernels.Length];

            for (var i = 0; i < kernels.Length; i++)
            {
                Weights[i] = 1;
            }
        }

        /// <summary>
        ///     Constructs a new additive kernel.
        /// </summary>
        /// <param name="kernels">Kernels to combine.</param>
        /// <param name="weights">
        ///     Weight values for each of the kernels.
        ///     Default is to assign equal weights.
        /// </param>
        public Additive(IKernel[] kernels, params double[] weights)
        {
            if (kernels.Length != weights.Length)
            {
                throw new ArgumentException("The number of weights should equals the number of kernels.",
                    "weights");
            }

            this.Kernels = kernels;
            this.Weights = weights;
        }

        /// <summary>
        ///     Gets the combination of kernels to use.
        /// </summary>
        public IKernel[] Kernels { get; }

        /// <summary>
        ///     Gets the weight array to use in the weighted kernel sum.
        /// </summary>
        public double[] Weights { get; }

        /// <summary>
        ///     Additive Kernel Combination function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            var sum = 0.0;
            for (var i = 0; i < Kernels.Length; i++)
            {
                sum += Weights[i] * Kernels[i].Function(x, y);
            }

            return sum;
        }
    }
}