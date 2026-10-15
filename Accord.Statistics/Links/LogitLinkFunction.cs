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
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Links
{
    /// <summary>
    ///     Logit link function.
    /// </summary>
    /// <remarks>
    ///     The Logit link function is associated with the
    ///     <see cref="BinomialDistribution">Binomial</see> and
    ///     <see cref="MultinomialDistribution">Multinomial</see> distributions.
    /// </remarks>
    [Serializable]
    public class LogitLinkFunction : ILinkFunction
    {
        /// <summary>
        ///     Creates a new Logit link function.
        /// </summary>
        /// <param name="beta">The beta value. Default is 1.</param>
        /// <param name="constant">The constant value. Default is 0.</param>
        public LogitLinkFunction(double beta, double constant)
        {
            B = beta;
            A = constant;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LogitLinkFunction" /> class.
        /// </summary>
        public LogitLinkFunction()
            : this(1, 0)
        {
        }

        /// <summary>
        ///     Linear scaling coefficient a (intercept).
        /// </summary>
        public double A { get; private set; }

        /// <summary>
        ///     Linear scaling coefficient b (slope).
        /// </summary>
        public double B { get; private set; }

        /// <summary>
        ///     The Logit link function.
        /// </summary>
        /// <param name="x">An input value.</param>
        /// <returns>The transformed input value.</returns>
        /// <remarks>
        ///     The inverse Logit link function is given by
        ///     <c>f(x) = (Math.Log(x / (1.0 - x)) - A) / B</c>.
        /// </remarks>
        public double Function(double x)
        {
            return (System.Math.Log(x / (1.0 - x)) - A) / B;
        }

        /// <summary>
        ///     The Logit mean (activation) function.
        /// </summary>
        /// <param name="x">A transformed value.</param>
        /// <returns>The reverse transformed value.</returns>
        /// <remarks>
        ///     The inverse Logit link function is given by
        ///     <c>g(x) = 1.0 / (1.0 + Math.Exp(-z)</c> in
        ///     which <c>z =  B * x + A</c>.
        /// </remarks>
        public double Inverse(double x)
        {
            var z = B * x + A;
            return 1.0 / (1.0 + System.Math.Exp(-z));
        }

        /// <summary>The logarithm of the inverse of the link function.</summary>
        /// <param name="x">A transformed value.</param>
        /// <returns>The log of the reverse transformed value.</returns>
        public double Log(double x)
        {
            //double z = B * x + A;
            //return -Math.Log(1.0 + Math.Exp(-z)); // TODO: Log 1+z
            return x;
        }

        /// <summary>
        ///     First derivative of the <see cref="Inverse" /> function.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The first derivative of the input value.</returns>
        /// <remarks>
        ///     The first derivative of the identity link
        ///     function is given by <c>f'(x) = y * (1.0 - y)</c>
        ///     where <c>y = f(x)</c> is the
        ///     <see cref="Function">
        ///         Logit function
        ///     </see>
        ///     .
        /// </remarks>
        public double Derivative(double x)
        {
            var z = B * x + A;
            var y = 1.0 / (1.0 + System.Math.Exp(-z));

            return y * (1.0 - y);
        }

        /// <summary>
        ///     First derivative of the mean function
        ///     expressed in terms of it's output.
        /// </summary>
        /// <param name="y">The reverse transformed value.</param>
        /// <returns>The first derivative of the input value.</returns>
        /// <remarks>
        ///     The first derivative of the Logit link function
        ///     in terms of y = f(x) is given by <c>y * (1.0 - y)</c>.
        /// </remarks>
        public double Derivative2(double y)
        {
            return y * (1.0 - y);
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
    }
}