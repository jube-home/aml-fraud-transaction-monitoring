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
using System.Threading;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Math.Optimization;
using Accord.Properties;

namespace Accord.Statistics.Models.Regression.Fitting
{
    /// <summary>
    ///     Non-linear Least Squares for <see cref="NonlinearRegression" /> optimization.
    /// </summary>
    /// <example>
    ///     <para>
    ///         The first example shows how to fit a non-linear least squares problem with <see cref="LevenbergMarquardt" />.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\NonlinearLeastSquaresTest.cs"
    ///         region="doc_learn_lm" lang="cs" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics.VB\Models\Regression\NonlinearLeastSquaresTest.vb"
    ///         region="doc_learn_lm" lang="vb" />
    ///     <para>
    ///         The second example shows how to fit a non-linear least squares problem with <see cref="GaussNewton" />.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\NonlinearLeastSquaresTest.cs"
    ///         region="doc_learn_gn" lang="cs" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics.VB\Models\Regression\NonlinearLeastSquaresTest.vb"
    ///         region="doc_learn_gn" lang="vb" />
    /// </example>
#pragma warning disable 612, 618
    public class NonlinearLeastSquares :
        ISupervisedLearning<NonlinearRegression, double[], double>,
        IRegressionFitting
#pragma warning restore 612, 618
    {
        private NonlinearRegression regression;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NonlinearLeastSquares" /> class.
        /// </summary>
        public NonlinearLeastSquares()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="NonlinearLeastSquares" /> class.
        /// </summary>
        /// <param name="regression">The regression model.</param>
        public NonlinearLeastSquares(NonlinearRegression regression)
            : this(regression, new LevenbergMarquardt(regression.Coefficients.Length))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="NonlinearLeastSquares" /> class.
        /// </summary>
        /// <param name="regression">The regression model.</param>
        /// <param name="algorithm">
        ///     The <see cref="ILeastSquaresMethod">least squares</see>
        ///     algorithm to be used to estimate the regression parameters. Default is to
        ///     use a <see cref="LevenbergMarquardt">Levenberg-Marquardt</see> algorithm.
        /// </param>
        public NonlinearLeastSquares(NonlinearRegression regression, ILeastSquaresMethod algorithm)
        {
            if (regression == null)
            {
                throw new ArgumentNullException("regression");
            }

            if (algorithm == null)
            {
                throw new ArgumentNullException("algorithm");
            }

            if (regression.Gradient == null)
            {
                throw new ArgumentException("The regression must have a gradient function defined.", "regression");
            }

            this.regression = regression;
            NumberOfParameters = regression.Coefficients.Length;

            Algorithm = algorithm;
            Algorithm.Solution = regression.Coefficients;
            Algorithm.Function = new LeastSquaresFunction(regression.Function);
            Algorithm.Gradient = new LeastSquaresGradientFunction(regression.Gradient);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether standard
        ///     errors should be computed in the next iteration.
        /// </summary>
        /// <value>
        ///     <c>true</c> to compute standard errors; otherwise, <c>false</c>.
        /// </value>
        public bool ComputeStandardErrors { get; set; } = true;

        /// <summary>
        ///     Gets the <see cref="ILeastSquaresMethod">Least-Squares</see>
        ///     optimization algorithm used to perform the actual learning.
        /// </summary>
        public ILeastSquaresMethod Algorithm { get; set; }

        /// <summary>
        ///     Gets the number of variables (free parameters) in the non-linear model specified in <see cref="Function" />.
        /// </summary>
        /// <value>
        ///     The number of parameters of <see cref="Function" />.
        /// </value>
        public int NumberOfParameters { get; set; }

        /// <summary>
        ///     Gets or sets the model function, mapping inputs to
        ///     outputs given a suitable parameter vector.
        /// </summary>
        public RegressionFunction Function { get; set; }

        /// <summary>
        ///     Gets or sets a function that computes the gradient of the
        ///     <see cref="Function" /> in respect to the current parameters.
        /// </summary>
        public RegressionGradientFunction Gradient { get; set; }

        /// <summary>
        ///     Gets or sets the vector of initial values to be used at the beginning
        ///     of the optimization. Setting a suitable set of initial values can be
        ///     important to achieve good convergence or avoid poor local minimas.
        /// </summary>
        public double[] StartValues { get; set; }

        /// <summary>
        ///     Runs the fitting algorithm.
        /// </summary>
        /// <param name="inputs">The input training data.</param>
        /// <param name="outputs">The output associated with each of the outputs.</param>
        /// <returns>
        ///     The sum of squared errors after the learning.
        /// </returns>
        [Obsolete("Please use the Learn() method instead.")]
        public double Run(double[][] inputs, double[] outputs)
        {
            var c = Algorithm as IConvergenceLearning;

            if (c != null)
            {
                c.MaxIterations = 1;
                c.Tolerance = 0;
            }

            Learn(inputs, outputs);
            return Algorithm.Value;
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        [field: NonSerialized]
        public CancellationToken Token { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public NonlinearRegression Learn(double[][] x, double[] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            if (NumberOfParameters == 0)
            {
                if (regression == null)
                {
                    if (StartValues == null)
                    {
                        throw new InvalidOperationException(
                            "Please set the number of parameters, starting values, or the initial regression model.");
                    }

                    NumberOfParameters = StartValues.Length;
                }
            }

            if (regression == null)
            {
                regression = new NonlinearRegression(NumberOfParameters, Function, Gradient);
                if (StartValues != null)
                {
                    regression.Coefficients.SetTo(StartValues);
                }
            }

            if (Algorithm == null)
            {
                Algorithm = new LevenbergMarquardt(NumberOfParameters);
            }

            Algorithm.NumberOfVariables = NumberOfParameters;
            Algorithm.Solution = regression.Coefficients;
            Algorithm.Function = new LeastSquaresFunction(regression.Function);
            Algorithm.Gradient = new LeastSquaresGradientFunction(regression.Gradient);
            Algorithm.Token = Token;

            var error = Algorithm.Minimize(x, y);

            if (double.IsNaN(error) || double.IsInfinity(error))
            {
                throw new Exception();
            }

            if (ComputeStandardErrors)
            {
                var errors = Algorithm.StandardErrors;
                for (var i = 0; i < errors.Length; i++)
                {
                    regression.StandardErrors[i] = Algorithm.StandardErrors[i];
                }
            }


            return regression;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs by using (gradient free) optimization algorithm
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <typeparam name="T">The desired algorithm which shall be used</typeparam>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public NonlinearRegression Learn<T>(double[][] x, double[] y) where T : BaseOptimizationMethod
        {
            var error = ErrorFunction;
            var errorForInputAndOutput = error.Curry()(x)(y);

            var gradientFreeSolver = (BaseOptimizationMethod)Activator.CreateInstance(typeof(T), NumberOfParameters);
            gradientFreeSolver.Function = errorForInputAndOutput;
            gradientFreeSolver.NumberOfVariables = NumberOfParameters;

            gradientFreeSolver.Minimize();

            var problemFormulation =
                new NonlinearRegression(gradientFreeSolver.NumberOfVariables, Function);

            for (var idx = 0; idx < gradientFreeSolver.Solution.Length; idx++)
            {
                problemFormulation.Coefficients.SetValue(gradientFreeSolver.Solution[idx], idx);
            }

            return problemFormulation;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs by using (gradient free) optimization algorithm
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="gradientFreeSolver">The optimization solver<paramref name="gradientFreeSolver">inputs</paramref>.</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public NonlinearRegression Learn(double[][] x, double[] y, BaseOptimizationMethod gradientFreeSolver)
        {
            var error = ErrorFunction;
            var errorForInputAndOutput = error.Curry()(x)(y);

            gradientFreeSolver.Function = errorForInputAndOutput;
            gradientFreeSolver.NumberOfVariables = NumberOfParameters;

            gradientFreeSolver.Minimize();

            var problemFormulation =
                new NonlinearRegression(gradientFreeSolver.NumberOfVariables, Function);

            for (var idx = 0; idx < gradientFreeSolver.Solution.Length; idx++)
            {
                problemFormulation.Coefficients.SetValue(gradientFreeSolver.Solution[idx], idx);
            }

            return problemFormulation;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs by using (gradient free) optimization algorithm
        /// </summary>
        /// <param name="input">The model inputs.</param>
        /// <param name="output">The desired outputs associated with each inputs.</param>
        /// <param name="parameter">The parameter for model .</param>
        /// <returns>
        ///     The error between model output with parameter and true output.
        /// </returns>
        protected double ErrorFunction(double[][] input, double[] output, double[] parameter)
        {
            var points = input.Length;
            double error = 0;
            // invoke the function to compute the regression functions output
            // build the difference between computed output and true output, i.e. error
            // error with power of 2 and sum them up
            for (var idx = 0; idx < points; idx++)
            {
                error = error + System.Math.Pow(Function.Invoke(parameter, input[idx]) - output[idx], 2);
            }

            return error;
        }
    }

    // magic transformation to map a 4 parameter function to partial application
    // like in F#
    internal static class CurryExtension
    {
        internal static Func<A, Func<B, Func<C, D>>> Curry<A, B, C, D>(this Func<A, B, C, D> func)
        {
            return a => b => c => func(a, b, c);
        }

        internal static Func<A, Func<B, C>> Curry<A, B, C>(this Func<A, B, C> func)
        {
            return a => b => func(a, b);
        }
    }
}