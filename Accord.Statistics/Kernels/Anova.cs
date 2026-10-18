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
    ///     ANOVA (ANalysis Of VAriance) Kernel.
    /// </summary>
    /// <remarks>
    ///     The ANOVA kernel is a graph kernel, which can be
    ///     computed using dynamic programming tables.
    ///     References:
    ///     - http://www.cse.ohio-state.edu/mlss09/mlss09_talks/1.june-MON/jst_tutorial.pdf
    /// </remarks>
    [Serializable]
    public sealed class Anova : KernelBase, IKernel
    {
        private double[,,] K; // value cache
        private int n; // input vector length
        private int p; // length of subsequence

        /// <summary>
        ///     Constructs a new ANOVA Kernel.
        /// </summary>
        /// <param name="vectorLength">Length of the input vector.</param>
        /// <param name="subsequenceLength">Length of the subsequences for the ANOVA decomposition.</param>
        public Anova(int vectorLength, int subsequenceLength)
        {
            n = vectorLength;
            p = subsequenceLength;
            K = new double[vectorLength, vectorLength, subsequenceLength];
        }

        /// <summary>
        ///     ANOVA Kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public override double Function(double[] x, double[] y)
        {
            // Evaluate the kernel by dynamic programming
            for (var k = 0; k < p; k++)
            {
                for (var i = 0; i < n; i++)
                {
                    for (var j = 0; j < n; j++)
                    {
                        K[i, j, k] = kernel(x, i, y, j, k);
                    }
                }
            }

            // Get the final result
            return K[n - 1, n - 1, p - 1];
        }

        private double kernel(double[] x, int ni, double[] y, int mi, int pi)
        {
            double a;

            if (ni == 0 || mi == 0)
            {
                a = 0;
            }
            else
            {
                // Retrieve the value from the cache
                a = K[ni - 1, mi - 1, pi];
            }


            // Compute a linear kernel
            var k = x[ni] * y[mi];


            if (pi == 0)
            {
                return a + k;
            }

            if (ni == 0 || mi == 0)
            {
                return a;
            }

            // Retrieve the value from the cache
            return a + k * K[ni - 1, mi - 1, pi - 1];
        }
    }
}