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
    ///     Polynomial Least-Squares.
    /// </summary>
    /// <remarks>
    ///     In linear regression, the model specification is that the dependent
    ///     variable, y is a linear combination of the parameters (but need not
    ///     be linear in the independent variables). As the linear regression
    ///     has a closed form solution, the regression coefficients can be
    ///     efficiently computed using the Regress method of this class.
    /// </remarks>
    /// <seealso cref="PolynomialRegression" />
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\PolynomialRegressionTest.cs" region="doc_learn" />
    /// </example>
    [Serializable]
    public class PolynomialLeastSquares :
        ISupervisedLearning<PolynomialRegression, double, double>
    {
        private int degree = 1;

        [NonSerialized] private CancellationToken token;

        /// <summary>
        ///     Gets or sets whether to always use a robust Least-Squares
        ///     estimate using the <see cref="SingularValueDecomposition" />.
        ///     Default is false.
        /// </summary>
        public bool IsRobust { get; set; }

        /// <summary>
        ///     Gets or sets the polynomial degree to use
        ///     in the polynomial regression.
        /// </summary>
        public int Degree
        {
            get => degree;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Degree should be higher than zero.");
                }

                degree = value;
            }
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        public CancellationToken Token
        {
            get => token;
            set => token = value;
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
        public PolynomialRegression Learn(double[] x, double[] y, double[] weights = null)
        {
            var z = Jagged.Zeros(x.Length, Degree);
            for (var i = 0; i < x.Length; i++)
            {
                for (var j = 0; j < z[i].Length; j++)
                {
                    z[i][j] = System.Math.Pow(x[i], Degree - j);
                }
            }

            var lls = new OrdinaryLeastSquares
            {
                UseIntercept = true,
                IsRobust = IsRobust
            };

            var linear = lls.Learn(z, y, weights);

            return new PolynomialRegression(linear);
        }
    }
}