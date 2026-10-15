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
using Accord.MachineLearning.DecisionTrees;
using Accord.Math;
using Accord.Math.Comparers;
using Accord.Statistics;

namespace Accord.MachineLearning.Boosting.Learners
{
    /// <summary>
    ///     Learning algorithm for <see cref="DecisionStump" />s.
    /// </summary>
    /// <seealso cref="AdaBoost{TModel}" />
    /// <seealso cref="DecisionStump" />
    /// <example>
    ///     <para>
    ///         The <see cref="ThresholdLearning" /> algorithm is mostly intended to be used to create
    ///         <see cref="DecisionStump" /> weak classifiers in the context of an <see cref="AdaBoost{TModel}" />
    ///         learning algorithm. Please refer to the <see cref="AdaBoost{TModel}" /> class for more examples
    ///         on using the classifier in this scenario. A simple example is shown below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Boosting\AdaBoostTest.cs" region="doc_learn" />
    ///     <para>
    ///         It is also possible to use the <see cref="ThresholdLearning" /> as a standalone learning algorithm.
    ///         An example is given below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Boosting\DecisionStumpTest.cs" region="doc_learn" />
    /// </example>
    public class ThresholdLearning : ParallelLearningBase,
        ISupervisedBinaryLearning<DecisionStump, double[]>
    {
        /// <summary>
        ///     Gets or sets the model being trained.
        /// </summary>
        public DecisionStump Model { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        public DecisionStump Learn(double[][] x, int[][] y, double[] weights = null)
        {
            return Learn(x, y.ArgMax(0), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        public DecisionStump Learn(double[][] x, bool[][] y, double[] weights = null)
        {
            return Learn(x, y.ArgMax(0), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        public DecisionStump Learn(double[][] x, int[] y, double[] weights = null)
        {
            return Learn(x, Classes.Decide(y), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        public DecisionStump Learn(double[][] x, bool[] y, double[] weights = null)
        {
            if (weights == null)
            {
                weights = Vector.Create(x.Length, 1.0 / x.Length);
            }

            if (Model == null)
            {
                Model = new DecisionStump();
            }

            var comparer = new ElementComparer();
            var errorMinimum = double.MaxValue;
            var numberOfVariables = x.Columns();

            for (var i = 0; i < numberOfVariables; i++)
            {
                comparer.Index = i;
                var indices = Vector.Range(0, x.Length);
                Array.Sort(indices, (a, b) => x[a][i].CompareTo(x[b][i]));

                var error = 0.0;
                for (var j = 0; j < y.Length; j++)
                {
                    var idx = indices[j];
                    if (y[idx])
                    {
                        error += weights[idx];
                    }
                }

                for (var j = 0; j < y.Length - 1; j++)
                {
                    var idx = indices[j];
                    var nidx = indices[j + 1];

                    if (y[idx])
                    {
                        error -= weights[idx];
                    }
                    else
                    {
                        error += weights[idx];
                    }

                    var midpoint = (x[idx][i] + x[nidx][i]) / 2.0;

                    // Compare to current best
                    if (error < errorMinimum)
                    {
                        errorMinimum = error;
                        Model.Index = i;
                        Model.Threshold = midpoint;
                        Model.Comparison = ComparisonKind.LessThan;
                    }

                    if (1.0 - error < errorMinimum)
                    {
                        errorMinimum = 1.0 - error;
                        Model.Index = i;
                        Model.Threshold = midpoint;
                        Model.Comparison = ComparisonKind.GreaterThan;
                    }
                }
            }

            return Model;
        }
    }
}