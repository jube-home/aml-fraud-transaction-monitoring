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
    ///     Power Kernel, also known as the (Unrectified) Triangular Kernel.
    /// </summary>
    /// <remarks>
    ///     The Power kernel is also known as the (unrectified) triangular kernel.
    ///     It is an example of scale-invariant kernel (Sahbi and Fleuret, 2004)
    ///     and is also only conditionally positive definite.
    /// </remarks>
    [Serializable]
    public sealed class Power : KernelBase, IKernel, IRadialBasisKernel, ICloneable
    {
        private double degree;

        /// <summary>
        ///     Constructs a new Power Kernel.
        /// </summary>
        /// <param name="degree">The kernel's degree.</param>
        public Power(int degree)
        {
            this.degree = degree;
        }

        /// <summary>
        ///     Constructs a new Power Kernel.
        /// </summary>
        /// <param name="degree">The kernel's degree.</param>
        public Power(double degree)
        {
            this.degree = degree;
        }

        /// <summary>
        ///     Gets or sets the kernel's degree.
        /// </summary>
        public double Degree
        {
            get => degree;
            set => degree = value;
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
        ///     Power Kernel Function
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
                return 0.0;
            }

            var norm = 0.0;
            for (var i = 0; i < x.Length; i++)
            {
                var d = x[i] - y[i];
                norm += d * d;
            }

            return -System.Math.Pow(norm, degree / 2.0);
        }

        /// <summary>
        ///     Power Kernel Function
        /// </summary>
        /// <param name="z">Distance <c>z</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double z)
        {
            return -System.Math.Pow(z, degree);
        }
    }
}