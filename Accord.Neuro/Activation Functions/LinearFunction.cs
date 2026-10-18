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
    ///     Linear activation function.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements a linear activation function bounded
    ///         in the interval (a,b), as given by the piecewise formula:
    ///     </para>
    ///     <code lang="none">
    ///   f(x) = alpha*x, if a > x*alpha > b
    ///   f(x) = a,       if a > x*alpha;
    ///   f(x) = b,       if     x*alpha > b;
    ///   </code>
    ///     <para>
    ///         In which, by default, a = -1 and b = +1.
    ///     </para>
    ///     <para>
    ///         This function is continuous only in the interval (a/alpha, b/alpha). This is similar
    ///         to the threshold function but with a linear growth component. If alpha is set to a
    ///         very high value (such as infinity), the function behaves as a threshold function.
    ///     </para>
    ///     <para>
    ///         The output range of the function can be set to an arbitrary
    ///         value. The default output range is <b>[-1, +1]</b>.
    ///     </para>
    /// </remarks>
    [Serializable]
    public class LinearFunction : IActivationFunction
    {
        // linear slope value
        private double alpha = 1;

        // function output range
        private DoubleRange range = new(-1, +1);

        /// <summary>
        ///     Initializes a new instance of the <see cref="LinearFunction" /> class.
        /// </summary>
        public LinearFunction(double alpha)
        {
            Alpha = alpha;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LinearFunction" /> class.
        /// </summary>
        public LinearFunction(DoubleRange range)
        {
            Range = range;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LinearFunction" /> class.
        /// </summary>
        public LinearFunction(double alpha, DoubleRange range)
        {
            Alpha = alpha;
            Range = range;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LinearFunction" /> class.
        /// </summary>
        public LinearFunction()
        {
        }

        /// <summary>
        ///     Linear slope value.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is set to <b>1</b>.</para>
        /// </remarks>
        public double Alpha
        {
            get => alpha;
            set => alpha = value;
        }

        /// <summary>
        ///     Function output range.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is set to [-1;+1]</para>
        /// </remarks>
        public DoubleRange Range
        {
            get => range;
            set => range = value;
        }

        /// <summary>
        ///     Calculates function value.
        /// </summary>
        /// <param name="x">Function input value.</param>
        /// <returns>Function output value, <i>f(x)</i>.</returns>
        /// <remarks>The method calculates function value at point <paramref name="x" />.</remarks>
        public double Function(double x)
        {
            var y = alpha * x;

            if (y > range.Max)
            {
                return range.Max;
            }

            if (y < range.Min)
            {
                return range.Min;
            }

            return y;
        }

        /// <summary>
        ///     Calculates function derivative.
        /// </summary>
        /// <param name="x">Function input value.</param>
        /// <returns>Function derivative, <i>f'(x)</i>.</returns>
        /// <remarks>The method calculates function derivative at point <paramref name="x" />.</remarks>
        public double Derivative(double x)
        {
            var y = alpha * x;

            if (y <= range.Min || y >= range.Max)
            {
                return 0;
            }

            return alpha;
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
            if (y <= range.Min || y >= range.Max)
            {
                return 0;
            }

            return alpha;
        }
    }
}