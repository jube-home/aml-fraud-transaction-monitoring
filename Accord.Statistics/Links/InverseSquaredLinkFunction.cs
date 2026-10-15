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
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Links
{
    /// <summary>
    ///     Inverse squared link function.
    /// </summary>
    /// <remarks>
    ///     The inverse squared link function is associated with the
    ///     <see cref="InverseGaussianDistribution">Inverse Gaussian distribution</see>.
    /// </remarks>
    [Serializable]
    public class InverseSquaredLinkFunction : ILinkFunction
    {
        /// <summary>
        ///     Creates a new Inverse squared Link function.
        /// </summary>
        /// <param name="beta">The beta value.</param>
        /// <param name="constant">The constant value.</param>
        public InverseSquaredLinkFunction(double beta, double constant)
        {
            B = beta;
            A = constant;
        }

        /// <summary>
        ///     Creates a new Inverse squared Link function.
        /// </summary>
        public InverseSquaredLinkFunction() : this(1, 0)
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
        ///     The Inverse Squared link function.
        /// </summary>
        /// <param name="x">An input value.</param>
        /// <returns>The transformed input value.</returns>
        public double Function(double x)
        {
            return (1.0 / (x * x) - A) / B;
        }

        /// <summary>
        ///     The Inverse Squared mean (activation) function.
        /// </summary>
        /// <param name="x">A transformed value.</param>
        /// <returns>The reverse transformed value.</returns>
        public double Inverse(double x)
        {
            return 1.0 / System.Math.Sqrt(B * x + A);
        }

        /// <summary>The logarithm of the inverse of the link function.</summary>
        /// <param name="x">A transformed value.</param>
        /// <returns>The log of the reverse transformed value.</returns>
        public double Log(double x)
        {
            return -System.Math.Log(System.Math.Sqrt(B * x + A));
        }

        /// <summary>
        ///     First derivative of the <see cref="Inverse" /> function.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The first derivative of the input value.</returns>
        public double Derivative(double x)
        {
            return -0.5 * B * System.Math.Pow(B * x + A, -1.5);
        }

        /// <summary>
        ///     First derivative of the <see cref="Inverse" />
        ///     function expressed in terms of it's output.
        /// </summary>
        /// <param name="y">The reverse transformed value.</param>
        /// <returns>The first derivative of the input value.</returns>
        public double Derivative2(double y)
        {
            return -0.5 * B * y * y * y;
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