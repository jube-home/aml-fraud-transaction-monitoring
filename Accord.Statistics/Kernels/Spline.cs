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
    ///     Infinite Spline Kernel function.
    /// </summary>
    /// <remarks>
    ///     The Spline kernel is given as a piece-wise cubic
    ///     polynomial, as derived in the works by Gunn (1998).
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Kernels\SplineTest.cs" region="doc_learn" />
    /// </example>
    [Serializable]
    public sealed class Spline : KernelBase, IKernel, ICloneable
    {
        /// <summary>
        ///     Constructs a new Spline Kernel.
        /// </summary>
        public Spline()
        {
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
        ///     Spline Kernel Function
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            double k = 1;
            for (var i = 0; i < x.Length; i++)
            {
                var min = System.Math.Min(x[i], y[i]);
                var xy = x[i] * y[i];

                // prod{1}^d 1 + xy + xy*min - (x+y)/2 min² + min³/3} 
                k *= 1.0 + xy + xy * min - (x[i] + y[i]) / 2.0 * min * min + min * min * min / 3.0;
            }

            return k;
        }
    }
}