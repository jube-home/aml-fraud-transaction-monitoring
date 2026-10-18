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
    ///     Kurtosis contrast function.
    /// </summary>
    /// <remarks>
    ///     According to Hyvärinen, the kurtosis contrast function is
    ///     justified on statistical grounds only for estimating sub-Gaussian
    ///     independent components when there are no outliers.
    /// </remarks>
    /// <seealso cref="IndependentComponentAnalysis" />
    [Serializable]
    public class Kurtosis : IContrastFunction
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Kurtosis" /> class.
        /// </summary>
        public Kurtosis()
        {
        }

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
            for (var j = 0; j < x.Length; j++)
            {
                // Kurtosis contrast function and its derivative, as given
                //  in original Hyvärinen's paper. See main references for the
                //  Independent Component Analysis class for details.

                var v = x[j];

                // g(w*x)
                output[j] = v * v * v;

                // g'(w*x)
                derivative[j] = 3.0 * v * v;
            }
        }
    }
}