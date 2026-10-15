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

namespace Accord.Neuro
{
    /// <summary>
    ///     Rectified linear activation function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements a rectified linear activation
    ///         function as given by the piecewise formula:
    ///     </para>
    ///     <code lang="none">
    ///   f(x) = x, if x > 0
    ///   f(x) = 0, otherwise
    ///   </code>
    ///     <para>
    ///         This function is non-differentiable at zero.
    ///     </para>
    /// </remarks>
    [Serializable]
    public class RectifiedLinearFunction : IActivationFunction
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RectifiedLinearFunction" /> class.
        /// </summary>
        public RectifiedLinearFunction()
        {
        }

        /// <summary>
        ///     Calculates function value.
        /// </summary>
        /// <param name="x">Function input value.</param>
        /// <returns>Function output value, <i>f(x)</i>.</returns>
        /// <remarks>The method calculates function value at point <paramref name="x" />.</remarks>
        public double Function(double x)
        {
            if (x > 0)
            {
                return x;
            }

            return 0;
        }

        /// <summary>
        ///     Calculates function derivative.
        /// </summary>
        /// <param name="x">Function input value.</param>
        /// <returns>Function derivative, <i>f'(x)</i>.</returns>
        /// <remarks>The method calculates function derivative at point <paramref name="x" />.</remarks>
        public double Derivative(double x)
        {
            if (x > 0)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        ///     Calculates function derivative.
        /// </summary>
        /// <param name="y">
        ///     Function output value - the value, which was obtained
        ///     with the help of <see cref="Function" /> method.
        /// </param>
        /// <returns>Function derivative, <i>f'(x)</i>.</returns>
        /// <remarks>
        ///     <para>
        ///         The method calculates the same derivative value as the
        ///         <see cref="Derivative" /> method, but it takes not the input <b>x</b> value
        ///         itself, but the function value, which was calculated previously with
        ///         the help of <see cref="Function" /> method.
        ///     </para>
        ///     <para>
        ///         <note>
        ///             Some applications require as function value, as derivative value,
        ///             so they can save the amount of calculations using this method to calculate derivative.
        ///         </note>
        ///     </para>
        /// </remarks>
        public double Derivative2(double y)
        {
            if (y > 0)
            {
                return 1;
            }

            return 0;
        }
    }
}