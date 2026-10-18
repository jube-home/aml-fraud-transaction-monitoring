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
using System.Linq;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Math.Random;

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Bootstrap method for generalization performance measurements.
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\BootstrapTest.cs" region="doc_learn" />
    /// </example>
    /// <typeparam name="TModel">The type of the machine learning model.</typeparam>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <typeparam name="TOutput">The type of the output data or labels.</typeparam>
    /// <seealso cref="CrossValidation{TModel, TInput}" />
    /// <seealso cref="SplitSetValidation{TModel, TInput}" />
    public class Bootstrap<TModel, TInput, TOutput> : BaseSplitSetValidation<BootstrapResult<TModel, TInput, TOutput>,
        TModel, TInput, TOutput>
        where TModel : class, ITransform<TInput, TOutput>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Bootstrap" /> class.
        /// </summary>
        public Bootstrap()
        {
        }

        /// <summary>
        ///     Gets or sets the number B of bootstrap samplings
        ///     to be drawn from the population dataset.
        /// </summary>
        public int B { get; set; }

        /// <summary>
        ///     Gets or sets the number of samples to be drawn in each subsample. If
        ///     set to zero, all samples in the entire dataset will be selected.
        /// </summary>
        public int NumberOfSubsamples { get; set; }

        /// <summary>
        ///     Gets the bootstrap samples drawn from the population dataset as indices.
        /// </summary>
        public int[][] SubSampleIndices { get; private set; }

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
        public override BootstrapResult<TModel, TInput, TOutput> Learn(TInput[] x, TOutput[] y, double[] weights = null)
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

            var sampleSize = NumberOfSubsamples;
            if (sampleSize == 0)
            {
                sampleSize = x.Length;
            }

            SubSampleIndices = CreateSubSampleIndices(x, y, B, sampleSize);


            var foldResults = new SplitResult<TModel, TInput, TOutput>[B];

            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                for (var i = 0; i < B; i++)
                {
                    foldResults[i] = LearnSubset(GetSubsample(i, x, y, weights), i);
                }
            }
            else
            {
                Parallel.For(0, B, i => { foldResults[i] = LearnSubset(GetSubsample(i, x, y, weights), i); });
            }

            return new BootstrapResult<TModel, TInput, TOutput>(foldResults);
        }

        /// <summary>
        ///     Draws the bootstrap samples from the population.
        /// </summary>
        /// <param name="x">The input data from where subsamples should be drawn.</param>
        /// <param name="y">The output data from where subsamples should be drawn.</param>
        /// <param name="resamplings">The number of samples to drawn.</param>
        /// <param name="subsampleSize">The size of the samples to be drawn.</param>
        /// <returns>The indices of the samples in the original set.</returns>
        protected virtual int[][] CreateSubSampleIndices(TInput[] x, TOutput[] y, int resamplings, int subsampleSize)
        {
            // Create samples with replacement
            var idx = new int[resamplings][];

            var size = x.Length;

            var random = Generator.Random;

            // For each fold to be created
            for (var i = 0; i < idx.Length; i++)
            {
                idx[i] = new int[subsampleSize];

                // Generate a random sample (with replacement)
                for (var j = 0; j < idx[i].Length; j++)
                {
                    idx[i][j] = random.Next(0, size);
                }
            }

            return idx;
        }

        /// <summary>
        ///     Gets a subset of the training and testing sets.
        /// </summary>
        /// <param name="subsampleIndex">Index of the subsample.</param>
        /// <param name="x">The input data x.</param>
        /// <param name="y">The output data y.</param>
        /// <param name="weights">The weights of each sample.</param>
        /// <returns>
        ///     A <see cref="TrainValDataSplit{TInput, TOutput}" /> that defines a
        ///     data split of a subsample of the dataset.
        /// </returns>
        public TrainValDataSplit<TInput, TOutput> GetSubsample(int subsampleIndex, TInput[] x, TOutput[] y,
            double[] weights)
        {
            // The training set is already computed
            var trainingSet = SubSampleIndices[subsampleIndex];

            // The validation set is the complement of the training set
            var validationSet = Vector.Range(0, x.Length).Except(trainingSet).ToArray();

            return new TrainValDataSplit<TInput, TOutput>(subsampleIndex, x, y, weights, trainingSet, validationSet);
        }
    }
}