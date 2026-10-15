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

namespace Accord.Statistics.Analysis.ContrastFunctions
{
    /// <summary>
    ///     Exponential contrast function.
    /// </summary>
    /// <remarks>
    ///     According to Hyvärinen, the Exponential contrast function may be
    ///     used when the independent components are highly super-Gaussian or
    ///     when robustness is very important.
    /// </remarks>
    /// <seealso cref="IndependentComponentAnalysis" />
    [Serializable]
    public class Exponential : IContrastFunction
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Exponential" /> class.
        /// </summary>
        /// <param name="alpha">The exponential alpha constant. Default is 1.</param>
        public Exponential(double alpha)
        {
            this.Alpha = alpha;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Exponential" /> class.
        /// </summary>
        public Exponential()
        {
        }

        /// <summary>
        ///     Gets the exponential alpha constant.
        /// </summary>
        public double Alpha { get; } = 1;

        /// <summary>
        ///     Contrast function.
        /// </summary>
        /// <param name="x">The vector of observations.</param>
        /// <param name="output">
        ///     At method's return, this parameter
        ///     should contain the evaluation of function over the vector
        ///     of observations <paramref name="x" />.
        /// </param>
        /// <param name="derivative">
        ///     At method's return, this parameter
        ///     should contain the evaluation of function derivative over
        ///     the vector of observations <paramref name="x" />.
        /// </param>
        public void Evaluate(double[] x, double[] output, double[] derivative)
        {
            // Exponential contrast function and its derivative, as given
            //  in original Hyvärinen's paper. See main references for the
            //  Independent Component Analysis class for details.

            for (var j = 0; j < x.Length; j++)
            {
                var w = x[j];
                var e = System.Math.Exp(-Alpha * (w * w) / 2.0);

                // g(w*x) = wx * exp(-(wx^2)/2)
                output[j] = w * e;

                // g'(w*x) = (1 - wx^2) * exp(-(wx^2)/2)
                derivative[j] = (1.0 - Alpha * w * w) * e;
            }
        }
    }
}