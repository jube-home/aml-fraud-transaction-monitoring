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

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Common interface for function optimization methods.
    /// </summary>
    /// <seealso cref="BoundedBroydenFletcherGoldfarbShanno" />
    /// <seealso cref="BroydenFletcherGoldfarbShanno" />
    /// <seealso cref="ConjugateGradient" />
    /// <seealso cref="ResilientBackpropagation" />
    /// <seealso cref="GoldfarbIdnani" />
    public interface IOptimizationMethod : IOptimizationMethod<double[], double>
    {
        // For backward compatibility
    }

    /// <summary>
    ///     Common interface for function optimization methods.
    /// </summary>
    /// <seealso cref="BoundedBroydenFletcherGoldfarbShanno" />
    /// <seealso cref="BroydenFletcherGoldfarbShanno" />
    /// <seealso cref="ConjugateGradient" />
    /// <seealso cref="ResilientBackpropagation" />
    /// <seealso cref="GoldfarbIdnani" />
    public interface IOptimizationMethod<TCode> : IOptimizationMethod, IOptimizationMethod<double[], double, TCode>
        where TCode : struct
    {
        // For backward compatibility
    }

    /// <summary>
    ///     Common interface for function optimization methods.
    /// </summary>
    /// <seealso cref="BoundedBroydenFletcherGoldfarbShanno" />
    /// <seealso cref="BroydenFletcherGoldfarbShanno" />
    /// <seealso cref="ConjugateGradient" />
    /// <seealso cref="ResilientBackpropagation" />
    /// <seealso cref="GoldfarbIdnani" />
    public interface IOptimizationMethod<TInput, TOutput>
    {
        /// <summary>
        ///     Gets the number of variables (free parameters)
        ///     in the optimization problem.
        /// </summary>
        /// <value>The number of parameters.</value>
        int NumberOfVariables { get; set; }

        /// <summary>
        ///     Gets the current solution found, the values of
        ///     the parameters which optimizes the function.
        /// </summary>
        TInput Solution { get; set; }

        /// <summary>
        ///     Gets the output of the function at the current <see cref="Solution" />.
        /// </summary>
        TOutput Value { get; }

        /// <summary>
        ///     Finds the minimum value of a function. The solution vector
        ///     will be made available at the <see cref="Solution" /> property.
        /// </summary>
        /// <returns>
        ///     Returns <c>true</c> if the method converged to a <see cref="Solution" />.
        ///     In this case, the found value will also be available at the <see cref="Value" />
        ///     property.
        /// </returns>
        bool Minimize();

        /// <summary>
        ///     Finds the maximum value of a function. The solution vector
        ///     will be made available at the <see cref="Solution" /> property.
        /// </summary>
        /// <returns>
        ///     Returns <c>true</c> if the method converged to a <see cref="Solution" />.
        ///     In this case, the found value will also be available at the <see cref="Value" />
        ///     property.
        /// </returns>
        bool Maximize();
    }

    /// <summary>
    ///     Common interface for function optimization methods.
    /// </summary>
    /// <seealso cref="BoundedBroydenFletcherGoldfarbShanno" />
    /// <seealso cref="BroydenFletcherGoldfarbShanno" />
    /// <seealso cref="ConjugateGradient" />
    /// <seealso cref="ResilientBackpropagation" />
    /// <seealso cref="GoldfarbIdnani" />
    public interface IOptimizationMethod<TInput, TOutput, TCode> : IOptimizationMethod<TInput, TOutput>
        where TCode : struct
    {
        /// <summary>
        ///     Get the exit code returned in the last call to the
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Maximize()" /> or
        ///     <see cref="IOptimizationMethod{TInput, TOutput}.Minimize()" /> methods.
        /// </summary>
        TCode Status { get; }
    }
}