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
using Accord.Math;
using Accord.Statistics;

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Split-Set Validation.
    /// </summary>
    /// <typeparam name="TModel">The type of the machine learning model.</typeparam>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <typeparam name="TOutput">The type of the output data or labels.</typeparam>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\SplitSetTest.cs" region="doc_learn" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\SplitSetTest.cs" region="doc_learn_multiclass" />
    /// </example>
    /// <seealso cref="Bootstrap{TModel, TInput, TOutput}" />
    /// <seealso cref="CrossValidation{TModel, TInput, TOutput}" />
    public class SplitSetValidation<TModel, TInput, TOutput>
        : BaseSplitSetValidation<SplitResult<TModel, TInput, TOutput>, TModel, TInput, TOutput>
        where TModel : class, ITransform<TInput, TOutput>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SplitSetValidation{TModel, TInput, TOutput}" /> class.
        /// </summary>
        public SplitSetValidation()
        {
            ValidationSetProportion = 0.2;
        }

        /// <summary>
        ///     Gets the group labels assigned to each of the data samples.
        /// </summary>
        public int[] Indices { get; set; }

        /// <summary>
        ///     Gets or sets the proportion of samples that should be
        ///     reserved in the validation set. Default is 20%.
        /// </summary>
        public double ValidationSetProportion
        {
            get => 1.0 - TrainingSetProportion;
            set => TrainingSetProportion = 1.0 - value;
        }

        /// <summary>
        ///     Gets or sets the proportion of samples that should be
        ///     reserved in the training set. Default is 80%.
        /// </summary>
        public double TrainingSetProportion { get; set; } = 0.8;

        /// <summary>
        ///     Gets the indices of elements in the validation set.
        /// </summary>
        public int[] IndicesValidationSet { get; set; }

        /// <summary>
        ///     Gets the indices of elements in the training set.
        /// </summary>
        public int[] IndicesTrainingSet { get; set; }

        /// <summary>
        ///     Creates a list of the sample indices that should serve as the validation set.
        /// </summary>
        /// <param name="x">The input data from where subsamples should be drawn.</param>
        /// <param name="y">The output data from where subsamples should be drawn.</param>
        /// <returns>The indices of the samples in the original set that should compose the validation set.</returns>
        protected virtual int[] CreateValidationSplits(TInput[] x, TOutput[] y)
        {
            return Classes.Random(x.Length, TrainingSetProportion);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Please set the Learner property before calling the Learn(x, y) method.
        ///     or
        ///     Please set the Learner property before calling the Learn(x, y) method.
        /// </exception>
        public override SplitResult<TModel, TInput, TOutput> Learn(TInput[] x, TOutput[] y, double[] weights = null)
        {
            if (Learner == null)
            {
                throw new InvalidOperationException(
                    "Please set the Learner property before calling the Learn(x, y) method.");
            }

            if (Loss == null)
            {
                throw new InvalidOperationException(
                    "Please set the Learner property before calling the Learn(x, y) method.");
            }

            if (Indices == null || IndicesValidationSet == null || IndicesTrainingSet == null)
            {
                Indices = CreateValidationSplits(x, y);
                IndicesValidationSet = Indices.Find(i => i == 1);
                IndicesTrainingSet = Indices.Find(i => i == 0);
            }
            else
            {
                ValidationSetProportion = IndicesValidationSet.Length / (double)Indices.Length;
            }

            var split = new TrainValDataSplit<TInput, TOutput>(0, x, y, weights, IndicesTrainingSet,
                IndicesValidationSet);

            var result = LearnSubset(split);

            return result;
        }
    }
}