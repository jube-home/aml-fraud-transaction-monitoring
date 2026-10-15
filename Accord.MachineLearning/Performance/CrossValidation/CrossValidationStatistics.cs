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
using Accord.Statistics;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Summary statistics for a cross-validation trial.
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\CrossValidationTest.cs" region="doc_learn" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\CrossValidationTest.cs" region="doc_learn_hmm" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\DecisionTreeTest.cs"
    ///         region="doc_cross_validation" />
    /// </example>
    [Serializable]
    public class CrossValidationStatistics
    {
        /// <summary>
        ///     Create a new cross-validation statistics class.
        /// </summary>
        /// <param name="sizes">The number of samples used to compute the statistics.</param>
        /// <param name="statistics">The performance statistics gathered during the run.</param>
        public CrossValidationStatistics(int[] sizes, double[] statistics)
            : this(sizes, statistics, null)
        {
        }

        /// <summary>
        ///     Create a new cross-validation statistics class.
        /// </summary>
        /// <param name="sizes">The number of samples used to compute the statistics.</param>
        /// <param name="statistics">The performance statistics gathered during the run.</param>
        /// <param name="variances">The variance of the statistics gathered during the run, if available.</param>
        public CrossValidationStatistics(int[] sizes, double[] statistics, double[] variances)
        {
            Sizes = sizes;
            Values = statistics;
            Variances = variances;

            Mean = statistics.Mean();
            Variance = statistics.Variance(Mean);

            // Compute the pooled variance if the individual variances
            //  for each cross-validation run are available

            if (variances != null)
            {
                PooledVariance = Measures.PooledVariance(sizes, variances);
            }
        }

        /// <summary>
        ///     Gets the values acquired during the cross-validation.
        ///     Most often those will be the errors for each folding.
        /// </summary>
        public double[] Values { get; private set; }

        /// <summary>
        ///     Gets the variance for each value acquired during the cross-validation.
        ///     Most often those will be the error variance for each folding.
        /// </summary>
        public double[] Variances { get; private set; }

        /// <summary>
        ///     Gets the number of samples used to compute the variance
        ///     of the values acquired during the cross-validation.
        /// </summary>
        public int[] Sizes { get; private set; }

        /// <summary>
        ///     Gets the mean of the performance statistics.
        /// </summary>
        public double Mean { get; private set; }

        /// <summary>
        ///     Gets the variance of the performance statistics.
        /// </summary>
        public double Variance { get; private set; }

        /// <summary>
        ///     Gets the standard deviation of the performance statistics.
        /// </summary>
        public double StandardDeviation => System.Math.Sqrt(Variance);

        /// <summary>
        ///     Gets the pooled variance of the performance statistics.
        /// </summary>
        public double PooledVariance { get; private set; }

        /// <summary>
        ///     Gets the pooled standard deviation of the performance statistics.
        /// </summary>
        public double PooledStandardDeviation => System.Math.Sqrt(PooledVariance);

        /// <summary>
        ///     Gets or sets a tag for user-defined information.
        /// </summary>
        public object Tag { get; set; }
    }
}