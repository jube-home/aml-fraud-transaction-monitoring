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
    ///     Normalized Polynomial Kernel. This class is equivalent to the
    ///     Normalized&gt;Polynomial> kernel but has more efficient
    ///     implementation.
    /// </summary>
    [Serializable]
    public sealed class NormalizedPolynomial : KernelBase, IKernel,
        IDistance, ICloneable
    {
        private double constant;
        private int degree;

        /// <summary>
        ///     Constructs a new Normalized Polynomial kernel of a given degree.
        /// </summary>
        /// <param name="degree">The polynomial degree for this kernel.</param>
        /// <param name="constant">The polynomial constant for this kernel. Default is 1.</param>
        public NormalizedPolynomial(int degree, double constant)
        {
            this.degree = degree;
            this.constant = constant;
        }

        /// <summary>
        ///     Constructs a new Normalized Polynomial kernel of a given degree.
        /// </summary>
        /// <param name="degree">The polynomial degree for this kernel.</param>
        public NormalizedPolynomial(int degree)
            : this(degree, 1.0)
        {
        }

        /// <summary>
        ///     Gets or sets the kernel's polynomial degree.
        /// </summary>
        public int Degree
        {
            get => degree;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Degree must be positive.");
                }

                degree = value;
            }
        }

        /// <summary>
        ///     Gets or sets the kernel's polynomial constant term.
        /// </summary>
        public double Constant
        {
            get => constant;
            set => constant = value;
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
        ///     Normalized polynomial kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            var sum = constant;
            var sumX = constant;
            var sumY = constant;

            for (var i = 0; i < x.Length; i++)
            {
                sum += x[i] * y[i];
                sumX += x[i] * x[i];
                sumY += y[i] * y[i];
            }

            return System.Math.Pow(sum / (sumX + sumY), degree);
        }
    }
}