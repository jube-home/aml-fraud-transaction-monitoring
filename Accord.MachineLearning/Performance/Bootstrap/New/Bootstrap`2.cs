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

using System.Collections.Generic;
using Accord.Math;
using Accord.Math.Random;
using Accord.Statistics.Analysis;

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Bootstrap method for generalization performance measurements (with
    ///     support for stratification and default loss function for classification).
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\BootstrapTest.cs" region="doc_learn" />
    /// </example>
    /// <typeparam name="TModel">The type of the machine learning model.</typeparam>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <seealso cref="CrossValidation{TModel, TInput}" />
    /// <seealso cref="SplitSetValidation{TModel, TInput}" />
    public class Bootstrap<TModel, TInput> : Bootstrap<TModel, TInput, int>
        where TModel : class, IClassifier<TInput, int>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Bootstrap{TModel, TInput}" /> class.
        /// </summary>
        public Bootstrap()
        {
            Loss = (expected, actual, r) =>
            {
                return new GeneralConfusionMatrix(r.Model.NumberOfClasses, expected, actual).Error;
            };
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the prevalence of an output
        ///     label should be balanced between training and testing sets. Default is false.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is stratified; otherwise, <c>false</c>.
        /// </value>
        public bool Stratify { get; set; }

        /// <summary>
        ///     Draws the bootstrap samples from the population.
        /// </summary>
        /// <param name="x">The input data from where subsamples should be drawn.</param>
        /// <param name="y">The output data from where subsamples should be drawn.</param>
        /// <param name="resamplings">The number of samples to drawn.</param>
        /// <param name="subsampleSize">The size of the samples to be drawn.</param>
        /// <returns>The indices of the samples in the original set.</returns>
        protected override int[][] CreateSubSampleIndices(TInput[] x, int[] y, int resamplings, int subsampleSize)
        {
            if (!Stratify)
            {
                return base.CreateSubSampleIndices(x, y, resamplings, subsampleSize);
            }

            // Create samples with replacement
            var idx = new int[resamplings][];

            var size = x.Length;

            var random = Generator.Random;

            var numberOfClasses = y.DistinctCount();
            var samplesPerClass = (int)System.Math.Ceiling(subsampleSize / (double)numberOfClasses);

            var sampleIndicesPerClass = new List<int>[numberOfClasses];
            for (var i = 0; i < y.Length; i++)
            {
                sampleIndicesPerClass[y[i]].Add(i);
            }


            // For each fold to be created
            for (var i = 0; i < idx.Length; i++)
            {
                idx[i] = new int[subsampleSize];

                // Generate a fixed amount of samples from each class
                for (int j = 0, p = 0; j < sampleIndicesPerClass.Length; j++)
                {
                    for (var k = 0; k < samplesPerClass; k++, p++)
                    {
                        var m = sampleIndicesPerClass[j].Count;
                        idx[i][p] = sampleIndicesPerClass[j][random.Next(0, m)];
                    }
                }

                // Fill the rest with random examples
                for (var k = samplesPerClass * numberOfClasses; k < idx[i].Length; k++)
                {
                    idx[i][k] = random.Next(0, size);
                }
            }

            return idx;
        }
    }
}