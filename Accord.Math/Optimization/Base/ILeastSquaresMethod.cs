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

using System.Threading;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Least Squares function delegate.
    /// </summary>
    /// <remarks>
    ///     This delegate represents a parameterized function that, given a set of
    ///     <paramref name="parameters" /> and an <paramref name="input" /> vector,
    ///     produces an associated output value.
    /// </remarks>
    /// <param name="parameters">The function parameters, also known as weights or coefficients.</param>
    /// <param name="input">An input vector.</param>
    /// <returns>
    ///     The output value produced given the <paramref name="input" /> vector
    ///     using the given <paramref name="parameters" />.
    /// </returns>
    public delegate double LeastSquaresFunction(double[] parameters, double[] input);

    /// <summary>
    ///     Gradient function delegate.
    /// </summary>
    /// <remarks>
    ///     This delegate represents the gradient of a
    ///     <see cref="LeastSquaresFunction">
    ///         Least
    ///         Squares objective function
    ///     </see>
    ///     . This function should compute the gradient vector
    ///     in respect to the function <paramref name="parameters" />.
    /// </remarks>
    /// <param name="parameters">The function parameters, also known as weights or coefficients.</param>
    /// <param name="input">An input vector.</param>
    /// <param name="result">The resulting gradient vector (w.r.t to the parameters).</param>
    public delegate void LeastSquaresGradientFunction(double[] parameters, double[] input, double[] result);

    /// <summary>
    ///     Common interface for Least Squares algorithms, i.e. algorithms
    ///     that can be used to solve Least Squares optimization problems.
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\GaussNewtonTest.cs" region="doc_minimize" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\LevenbergMarquardtTest.cs" region="doc_minimize" />
    /// </example>
    /// <seealso cref="GaussNewton" />
    /// <seealso cref="LevenbergMarquardt" />
    public interface ILeastSquaresMethod
    {
        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        CancellationToken Token { get; set; }

        /// <summary>
        ///     Gets or sets a parameterized model function mapping input vectors
        ///     into output values, whose optimum parameters must be found.
        /// </summary>
        /// <value>The function to be optimized.</value>
        LeastSquaresFunction Function { get; set; }

        /// <summary>
        ///     Gets or sets a function that computes the gradient vector in respect
        ///     to the function parameters, given a set of input and output values.
        /// </summary>
        /// <value>The gradient function.</value>
        LeastSquaresGradientFunction Gradient { get; set; }

        /// <summary>
        ///     Gets the number of variables (free parameters) in the optimization problem.
        /// </summary>
        /// <value>The number of parameters.</value>
        int NumberOfVariables { get; set; }

        /// <summary>
        ///     Gets the solution found, the values of the parameters which
        ///     optimizes the function, in a least squares sense.
        /// </summary>
        double[] Solution { get; set; }

        /// <summary>
        ///     Gets standard error for each parameter in the solution.
        /// </summary>
        double[] StandardErrors { get; }

        /// <summary>
        ///     Gets the value at the solution found. This should be
        ///     the minimum value found for the objective function.
        /// </summary>
        double Value { get; set; }

        /// <summary>
        ///     Attempts to find the best values for the parameter vector
        ///     minimizing the discrepancy between the generated outputs
        ///     and the expected outputs for a given set of input data.
        /// </summary>
        /// <param name="inputs">A set of input data.</param>
        /// <param name="outputs">
        ///     The values associated with each
        ///     vector in the <paramref name="inputs" /> data.
        /// </param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\GaussNewtonTest.cs" region="doc_minimize" />
        ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\LevenbergMarquardtTest.cs" region="doc_minimize" />
        /// </example>
        double Minimize(double[][] inputs, double[] outputs);
    }
}