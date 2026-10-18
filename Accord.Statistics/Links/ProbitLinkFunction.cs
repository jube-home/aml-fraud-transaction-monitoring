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
using Accord.Math;

namespace Accord.Statistics.Links
{
    /// <summary>
    ///     Probit link function.
    /// </summary>
    [Serializable]
    public class ProbitLinkFunction : ILinkFunction
    {
        private const double lnconstant = -Constants.LogSqrt2PI;

        /// <summary>
        ///     Creates a new Probit link function.
        /// </summary>
        public ProbitLinkFunction()
        {
        }

        /// <summary>
        ///     The Probit link function.
        /// </summary>
        /// <param name="x">An input value.</param>
        /// <returns>The transformed input value.</returns>
        /// <remarks>
        ///     The Probit link function is given by <c>f(x) = Phi^-1(x)</c>,
        ///     in which <see cref="Normal.Inverse">Phi^-1</see> is the
        ///     <see cref="Normal.Inverse">
        ///         inverse Normal (Gaussian) cumulative
        ///         distribution function
        ///     </see>
        ///     .
        /// </remarks>
        public double Function(double x)
        {
            return Normal.Inverse(x);
        }

        /// <summary>
        ///     The Probit mean (activation) function.
        /// </summary>
        /// <param name="x">A transformed value.</param>
        /// <returns>The reverse transformed value.</returns>
        /// <remarks>
        ///     The Probit link function is given by <c>g(x) = Phi(x)</c>,
        ///     in which <see cref="Normal.Function">Phi</see> is the
        ///     <see cref="Normal.Function">
        ///         Normal (Gaussian) cumulative
        ///         distribution function
        ///     </see>
        ///     .
        /// </remarks>
        public double Inverse(double x)
        {
            return Normal.Function(x);
        }

        /// <summary>
        ///     The logarithm of the inverse of the link function.
        /// </summary>
        /// <param name="x">A transformed value.</param>
        /// <returns>
        ///     The log of the reverse transformed value.
        /// </returns>
        public double Log(double x)
        {
            return Normal.Log(x);
        }

        /// <summary>
        ///     First derivative of the <see cref="Inverse" /> function.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The first derivative of the input value.</returns>
        /// <remarks>
        ///     The first derivative of the identity link function is
        ///     given by <c>f'(x) = exp(c - (Phi^-1(x))² * 0.5)</c> in
        ///     which <c>c = -<see cref="Constants.LogSqrt2PI">log(sqrt(2*π)</see></c>
        ///     and <see cref="Normal.Inverse">Phi^-1</see> is the
        ///     <see cref="Normal.Inverse">
        ///         inverse Normal (Gaussian) cumulative distribution function
        ///     </see>
        ///     .
        /// </remarks>
        public double Derivative(double x)
        {
            return System.Math.Exp(lnconstant - x * x * 0.5);
        }

        /// <summary>
        ///     First derivative of the <see cref="Inverse" />
        ///     function expressed in terms of it's output.
        /// </summary>
        /// <param name="y">The reverse transformed value.</param>
        /// <returns>The first derivative of the input value.</returns>
        /// <remarks>
        ///     The first derivative of the identity link function in terms
        ///     of y = f(x) is given by <c>f'(y) = exp(c - x * x * 0.5)</c>
        ///     in which <c>c = -<see cref="Constants.LogSqrt2PI">log(sqrt(2*π)</see></c>
        ///     and <c>x = </c>
        /// </remarks>
        public double Derivative2(double y)
        {
            var x = Normal.Inverse(y);
            return System.Math.Exp(lnconstant - x * x * 0.5);
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