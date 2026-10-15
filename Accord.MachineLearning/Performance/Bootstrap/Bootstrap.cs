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
using Accord.MachineLearning.Performance;
using Accord.Math;
using Accord.Math.Random;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Obsolete. Please refer to <see cref="Bootstrap{TModel,TInput,TOutput}" /> instead.
    /// </summary>
    [Obsolete("Please refer to Bootstrap<TModel, TInput, TOutput> instead.")]
    public delegate BootstrapValues BootstrapFittingFunction(int[] trainingSamples, int[] validationSamples);

    /// <summary>
    ///     Obsolete. Please use <see cref="Bootstrap{TModel, TInput, TOutput}" /> instead.
    /// </summary>
    [Obsolete("Please use Bootstrap<TModel, TInput, TOutput> instead.")]
    [Serializable]
    public class Bootstrap
    {
        [NonSerialized] private BootstrapFittingFunction fitting;
        private bool parallel = true;

        /// <summary>
        ///     Creates a new Bootstrap estimation algorithm.
        /// </summary>
        /// <param name="size">The size of the complete dataset.</param>
        /// <param name="subsamples">The number B of bootstrap resamplings to perform.</param>
        public Bootstrap(int size, int subsamples)
            : this(size, subsamples, size)
        {
        }

        /// <summary>
        ///     Creates a new Bootstrap estimation algorithm.
        /// </summary>
        /// <param name="size">The size of the complete dataset.</param>
        /// <param name="subsamples">The number B of bootstrap resamplings to perform.</param>
        /// <param name="subsampleSize">
        ///     The number of samples in each subsample. Default
        ///     is to use the total number of samples in the population dataset.
        /// </param>
        /// .
        public Bootstrap(int size, int subsamples, int subsampleSize)
        {
            Samples = size;
            Subsamples = new int[subsamples][];

            Subsamples = Samplings(size, subsamples, subsampleSize);
        }

        /// <summary>
        ///     Creates a new Bootstrap estimation algorithm.
        /// </summary>
        /// <param name="size">The size of the complete dataset.</param>
        /// <param name="resamplings">The indices of the bootstrap samplings.</param>
        public Bootstrap(int size, int[][] resamplings)
        {
            Samples = size;
            Subsamples = resamplings;
        }

        /// <summary>
        ///     Gets the number B of bootstrap samplings
        ///     to be drawn from the population dataset.
        /// </summary>
        public int B => Subsamples.Length;

        /// <summary>
        ///     Gets the total number of samples in the population dataset.
        /// </summary>
        public int Samples { get; }

        /// <summary>
        ///     Gets the bootstrap samples drawn from
        ///     the population dataset as indices.
        /// </summary>
        public int[][] Subsamples { get; }

        /// <summary>
        ///     Gets or sets the model fitting function.
        /// </summary>
        /// <remarks>
        ///     The fitting function should accept an array of integers containing the
        ///     indexes for the training samples, an array of integers containing the
        ///     indexes for the validation samples and should return information about
        ///     the model fitted using those two subsets of the available data.
        /// </remarks>
        public BootstrapFittingFunction Fitting
        {
            get => fitting;
            set => fitting = value;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether to use parallel
        ///     processing through the use of multiple threads or not.
        ///     Default is true.
        /// </summary>
        /// <value><c>true</c> to use multiple threads; otherwise, <c>false</c>.</value>
        public bool RunInParallel
        {
            get => parallel;
            set => parallel = value;
        }

        /// <summary>
        ///     Gets the indices for the training and validation
        ///     sets for the specified validation fold index.
        /// </summary>
        /// <param name="index">The index of the validation fold.</param>
        /// <param name="trainingSet">The indices for the observations in the training set.</param>
        /// <param name="validationSet">The indices for the observations in the validation set.</param>
        public void CreatePartitions(int index, out int[] trainingSet, out int[] validationSet)
        {
            if (index < 0 || index >= Subsamples.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            // Create indices for the foldings

            // The training set is already computed
            trainingSet = Subsamples[index];

            // The validation set is the complement of the training set
            validationSet = Vector.Range(0, Samples).Except(trainingSet).ToArray();
        }

        /// <summary>
        ///     Computes the cross validation algorithm.
        /// </summary>
        public BootstrapResult Compute()
        {
            if (Fitting == null)
            {
                throw new InvalidOperationException("Fitting function must have been previously defined.");
            }

            var models = new BootstrapValues[Subsamples.Length];

            if (RunInParallel)
            {
                Parallel.For(0, Subsamples.Length, i =>
                {
                    int[] trainingSet, validationSet;

                    // Create training and validation sets
                    CreatePartitions(i, out trainingSet, out validationSet);

                    // Fit and evaluate the model
                    models[i] = fitting(trainingSet, validationSet);
                });
            }
            else
            {
                for (var i = 0; i < Subsamples.Length; i++)
                {
                    int[] trainingSet, validationSet;

                    // Create training and validation sets
                    CreatePartitions(i, out trainingSet, out validationSet);

                    // Fit and evaluate the model
                    models[i] = fitting(trainingSet, validationSet);
                }
            }

            // Return cross-validation statistics
            return new BootstrapResult(this, models);
        }

        /// <summary>
        ///     Gets the number of instances in training and validation
        ///     sets for the specified validation fold index.
        /// </summary>
        /// <param name="index">The index of the bootstrap sample.</param>
        /// <param name="trainingCount">The number of instances in the training set.</param>
        /// <param name="validationCount">The number of instances in the validation set.</param>
        public void GetPartitionSize(int index, out int trainingCount, out int validationCount)
        {
            if (index < 0 || index >= Subsamples.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            trainingCount = Subsamples[index].Length;

            validationCount = Samples - trainingCount;
        }

        /// <summary>
        ///     Draws the bootstrap samples from the population.
        /// </summary>
        /// <param name="size">The size of the samples to be drawn.</param>
        /// <param name="resamplings">The number of samples to drawn.</param>
        /// <param name="subsampleSize">The size of the samples to be drawn.</param>
        /// <returns>The indices of the samples in the original set.</returns>
        public static int[][] Samplings(int size, int resamplings, int subsampleSize)
        {
            // Create samples with replacement
            var idx = new int[resamplings][];

            // For each fold to be created
            for (var i = 0; i < idx.Length; i++)
            {
                idx[i] = new int[subsampleSize];

                // Generate a random sample (with replacement)
                for (var j = 0; j < idx[i].Length; j++)
                {
                    idx[i][j] = Generator.Random.Next(0, size);
                }
            }

            return idx;
        }
    }
}