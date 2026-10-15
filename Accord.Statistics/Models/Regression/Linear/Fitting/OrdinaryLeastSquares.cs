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
using Accord.Math.Decompositions;

namespace Accord.Statistics.Models.Regression.Linear
{
    /// <summary>
    ///     Least Squares learning algorithm for linear regression models.
    /// </summary>
    /// <example>
    ///     <para>
    ///         Let's say we have some univariate, continuous sets of input data,
    ///         and a corresponding univariate, continuous set of output data, such
    ///         as a set of points in R². A simple linear regression is able to fit
    ///         a line relating the input variables to the output variables in which
    ///         the minimum-squared-error of the line and the actual output points
    ///         is minimum.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\SimpleLinearRegressionTest.cs"
    ///         region="doc_learn" />
    ///     <para>
    ///         The following example shows how to fit a multiple linear regression model
    ///         to model a plane as an equation in the form ax + by + c = z.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\MultipleLinearRegressionTest.cs"
    ///         region="doc_learn" />
    ///     <para>
    ///         The following example shows how to fit a multivariate linear regression model,
    ///         producing multidimensional outputs for each input.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\MultivariateLinearRegressionTest.cs"
    ///         region="doc_learn" />
    /// </example>
    /// <seealso cref="SimpleLinearRegression" />
    /// <seealso cref="MultipleLinearRegression" />
    /// <seealso cref="MultivariateLinearRegression" />
    public class OrdinaryLeastSquares :
        ISupervisedLearning<MultivariateLinearRegression, double[], double[]>,
        ISupervisedLearning<MultipleLinearRegression, double[], double>,
        ISupervisedLearning<SimpleLinearRegression, double, double>
    {
        private ISolverArrayDecomposition<double> decomposition;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OrdinaryLeastSquares" /> class.
        /// </summary>
        public OrdinaryLeastSquares()
        {
            UseIntercept = true;
        }

        /// <summary>
        ///     Gets or sets whether to include an intercept
        ///     term in the learned models. Default is true.
        /// </summary>
        public bool UseIntercept { get; set; }

        /// <summary>
        ///     Gets or sets whether to always use a robust Least-Squares
        ///     estimate using the <see cref="SingularValueDecomposition" />.
        ///     Default is false.
        /// </summary>
        public bool IsRobust { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public MultipleLinearRegression Learn(double[][] x, double[] y, double[] weights = null)
        {
            //var Xt = x.Transpose();
            if (UseIntercept)
            {
                x = x.InsertColumn(1.0);
            }

            if (weights != null)
            {
                var sqrtW = weights.Sqrt();
                x = x.Multiply(sqrtW, (VectorType)1);
                y = y.Multiply(sqrtW);
            }

            decomposition = x.Decompose(IsRobust);
            var coefficients = decomposition.Solve(y);

            if (UseIntercept)
            {
                return new MultipleLinearRegression
                {
                    Weights = coefficients.Get(0, -1),
                    Intercept = coefficients.Get(-1)
                };
            }

            return new MultipleLinearRegression
            {
                Weights = coefficients
            };
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
        public MultivariateLinearRegression Learn(double[][] x, double[][] y, double[] weights = null)
        {
            if (UseIntercept)
            {
                x = x.InsertColumn(1.0);
            }

            if (weights != null)
            {
                var sqrtW = weights.Sqrt();
                x = x.Multiply(sqrtW, (VectorType)1);
                y = y.Multiply(sqrtW, (VectorType)1);
            }

            decomposition = x.Decompose(IsRobust);
            var coefficients = decomposition.Solve(y);

            if (UseIntercept)
            {
                return new MultivariateLinearRegression
                {
                    Weights = coefficients.Get(0, -1, null),
                    Intercepts = coefficients.GetRow(-1)
                };
            }

            return new MultivariateLinearRegression
            {
                Weights = coefficients
            };
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public SimpleLinearRegression Learn(double[] x, double[] y, double[] weights = null)
        {
            var X = Jagged.ColumnVector(x);

            if (UseIntercept)
            {
                X = X.InsertColumn(1);
            }

            if (weights != null)
            {
                var sqrtW = weights.Sqrt();
                X = X.Multiply(sqrtW, (VectorType)1, X);
                y = y.Multiply(sqrtW);
            }

            decomposition = X.Decompose(IsRobust);
            var coefficients = decomposition.Solve(y);

            if (UseIntercept)
            {
                return new SimpleLinearRegression
                {
                    Slope = coefficients[0],
                    Intercept = coefficients[1]
                };
            }

            return new SimpleLinearRegression
            {
                Slope = coefficients[0]
            };
        }

        /// <summary>
        ///     Gets the information matrix used to update the regression
        ///     weights in the last call to <see cref="Learn(double[], double[], double[])" />
        /// </summary>
        public double[][] GetInformationMatrix()
        {
            return decomposition.GetInformationMatrix();
        }
    }
}