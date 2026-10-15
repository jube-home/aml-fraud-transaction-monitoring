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
using Accord.MachineLearning;
using Accord.Math.Optimization;
using Accord.Statistics.Models.Regression.Fitting;

namespace Accord.Statistics.Models.Regression
{
    /// <summary>
    ///     Regression function delegate.
    /// </summary>
    /// <remarks>
    ///     This delegate represents a parameterized function that, given a set of
    ///     <paramref name="coefficients">model coefficients</paramref> and an input
    ///     vector, produces an associated output value.
    /// </remarks>
    /// <param name="coefficients">The model coefficients, also known as parameters or coefficients.</param>
    /// <param name="input">An input vector.</param>
    /// <returns>
    ///     The output value produced given the
    ///     <paramref name="coefficients" /> and <paramref name="input" /> vector.
    /// </returns>
    public delegate double RegressionFunction(double[] coefficients, double[] input);

    /// <summary>
    ///     Gradient function delegate.
    /// </summary>
    /// <remarks>
    ///     This delegate represents the gradient of
    ///     <see cref="RegressionFunction">
    ///         regression
    ///         function
    ///     </see>
    ///     . A regression function is a parameterized function that, given a set
    ///     of <paramref name="coefficients">model coefficients</paramref> and an input vector,
    ///     produces an associated output value. This function should compute the gradient vector
    ///     in respect to the function <paramref name="coefficients" />.
    /// </remarks>
    /// <param name="coefficients">The model coefficients, also known as parameters or coefficients.</param>
    /// <param name="input">An input vector.</param>
    /// <param name="result">The resulting gradient vector (w.r.t to the coefficients).</param>
    public delegate void RegressionGradientFunction(double[] coefficients, double[] input, double[] result);

    /// <summary>
    ///     Nonlinear Regression.
    /// </summary>
    /// <example>
    ///     <para>
    ///         The first example shows how to fit a non-linear regression with <see cref="LevenbergMarquardt" />.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\NonlinearLeastSquaresTest.cs"
    ///         region="doc_learn_lm" lang="cs" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics.VB\Models\Regression\NonlinearLeastSquaresTest.vb"
    ///         region="doc_learn_lm" lang="vb" />
    ///     <para>
    ///         The second example shows how to fit a non-linear regression with <see cref="GaussNewton" />.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\NonlinearLeastSquaresTest.cs"
    ///         region="doc_learn_gn" lang="cs" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics.VB\Models\Regression\NonlinearLeastSquaresTest.vb"
    ///         region="doc_learn_gn" lang="vb" />
    /// </example>
    /// <seealso cref="NonlinearLeastSquares" />
    [Serializable]
    public class NonlinearRegression : TransformBase<double[], double>, ICloneable
    {
        // TODO: implement a way to serialize the function and gradient
        // functions, most likely using serializable expression trees.

        private RegressionGradientFunction gradient;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NonlinearRegression" /> class.
        /// </summary>
        /// <param name="parameters">The number of variables (free parameters) in the model.</param>
        /// <param name="function">The regression function implementing the regression model.</param>
        public NonlinearRegression(int parameters, RegressionFunction function)
        {
            Coefficients = new double[parameters];
            StandardErrors = new double[parameters];
            this.Function = function;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="NonlinearRegression" /> class.
        /// </summary>
        /// <param name="parameters">The number of variables (free parameters) in the model.</param>
        /// <param name="function">The regression function implementing the regression model.</param>
        /// <param name="gradient">The function that computes the gradient for <paramref name="function" /> (optional).</param>
        public NonlinearRegression(int parameters, RegressionFunction function, RegressionGradientFunction gradient)
            : this(parameters, function)
        {
            this.gradient = gradient;
        }

        /// <summary>
        ///     Gets the regression coefficients.
        /// </summary>
        public double[] Coefficients { get; private set; }

        /// <summary>
        ///     Gets the standard errors for the regression coefficients.
        /// </summary>
        public double[] StandardErrors { get; private set; }

        /// <summary>
        ///     Gets the model function, mapping inputs to
        ///     outputs given a suitable parameter vector.
        /// </summary>
        public RegressionFunction Function { get; }

        /// <summary>
        ///     Gets or sets a function that computes the gradient of the
        ///     <see cref="Function" /> in respect to the <see cref="Coefficients" />.
        /// </summary>
        public RegressionGradientFunction Gradient
        {
            get => gradient;
            set => gradient = value;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new NonlinearRegression(Coefficients.Length, Function, gradient);
            clone.Coefficients = (double[])Coefficients.Clone();
            clone.StandardErrors = (double[])StandardErrors.Clone();
            return clone;
        }

        /// <summary>
        ///     Computes the model output for the given input vector.
        /// </summary>
        /// <param name="inputs">The input vector.</param>
        /// <returns>The output value.</returns>
        [Obsolete("Please use Transform instead.")]
        public double Compute(double[] inputs)
        {
            return Function(Coefficients, inputs);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override double Transform(double[] input)
        {
            return Function(Coefficients, input);
        }
    }
}