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

namespace Accord.Neuro.ActivationFunctions
{
    /// <summary>
    ///     Common interface for stochastic activation functions.
    /// </summary>
    /// <seealso cref="BernoulliFunction" />
    /// <seealso cref="GaussianFunction" />
    public interface IStochasticFunction : IActivationFunction
    {
        /// <summary>
        ///     Samples a value from the function given a input value.
        /// </summary>
        /// <param name="x">Function input value.</param>
        /// <returns>Draws a random value from the function.</returns>
        double Generate(double x);

        /// <summary>
        ///     Samples a value from the function given a function output value.
        /// </summary>
        /// <param name="y">
        ///     The function output value. This is the value which was obtained
        ///     with the help of the <see cref="IActivationFunction.Function(double)" /> method.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         The method calculates the same output value as the
        ///         <see cref="Generate" /> method, but it takes not the input <b>x</b> value
        ///         itself, but the function value, which was calculated previously with help
        ///         of the <see cref="IActivationFunction.Function(double)" /> method.
        ///     </para>
        /// </remarks>
        /// <returns>Draws a random value from the function.</returns>
        double Generate2(double y);
    }
}