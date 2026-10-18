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
using System.Collections.Generic;
using System.Threading.Tasks;
using Accord.MachineLearning.Performance;
using Accord.Math;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Obsolete. Please use <see cref="CrossValidation{TModel,TInput,TOutput}" /> instead.
    /// </summary>
    public delegate CrossValidationValues<TModel>
        CrossValidationFittingFunction<TModel>(int k, int[] trainingSamples, int[] validationSamples)
        where TModel : class;

    /// <summary>
    ///     Obsolete. Please use <see cref="CrossValidation{TModel, TInput, TOutput}" /> instead.
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\CrossValidationTest.cs" region="doc_learn" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\CrossValidationTest.cs" region="doc_learn_hmm" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\DecisionTreeTest.cs"
    ///         region="doc_cross_validation" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayesTest.cs" region="doc_cross_validation" />
    /// </example>
    [Serializable]
    [Obsolete("Please use CrossValidation<TModel, TInput, TOutput> instead.")]
    public class CrossValidation<TModel> where TModel : class
    {
        [NonSerialized] private CrossValidationFittingFunction<TModel> fitting;
        private bool parallel = true;

        /// <summary>
        ///     Creates a new k-fold cross-validation algorithm.
        /// </summary>
        /// <param name="size">The total number samples in the entire dataset.</param>
        public CrossValidation(int size)
            : this(size, 10)
        {
        }

        /// <summary>
        ///     Creates a new k-fold cross-validation algorithm.
        /// </summary>
        /// <param name="size">The total number samples in the entire dataset.</param>
        /// <param name="folds">The number of folds, usually denoted as <c>k</c> (default is 10).</param>
        public CrossValidation(int size, int folds)
        {
            if (folds > size)
            {
                throw new ArgumentException("The number of folds can not exceed "
                                            + "the total number of samples in the data set", "folds");
            }

            Samples = size;
            this.Folds = new int[folds][];

            Indices = CrossValidation.Splittings(size, folds);

            // Create foldings
            for (var i = 0; i < folds; i++)
            {
                var fold = i;
                this.Folds[i] = Indices.Find(x => x == fold);
            }
        }

        /// <summary>
        ///     Creates a new k-fold cross-validation algorithm.
        /// </summary>
        /// <param name="labels">A vector containing class labels.</param>
        /// <param name="classes">The number of different classes in <paramref name="labels" />.</param>
        /// <param name="folds">The number of folds, usually denoted as <c>k</c> (default is 10).</param>
        public CrossValidation(int[] labels, int classes, int folds)
        {
            if (folds > labels.Length)
            {
                throw new ArgumentException("The number of folds can not exceed "
                                            + "the total number of samples in the data set", "folds");
            }

            Samples = labels.Length;
            this.Folds = new int[folds][];

            Indices = CrossValidation.Splittings(labels, classes, folds);

            // Create foldings
            for (var i = 0; i < folds; i++)
            {
                var fold = i;
                this.Folds[i] = Indices.Find(x => x == fold);
            }
        }

        /// <summary>
        ///     Creates a new k-fold cross-validation algorithm.
        /// </summary>
        /// <param name="indices">An already created set of fold indices for each sample in a dataset.</param>
        /// <param name="folds">The total number of folds referenced in the <paramref name="indices" /> parameter.</param>
        public CrossValidation(int[] indices, int folds)
        {
            Samples = indices.Length;
            this.Folds = new int[folds][];
            this.Indices = indices;

            // Create foldings
            for (var i = 0; i < folds; i++)
            {
                var fold = i;
                this.Folds[i] = indices.Find(x => x == fold);
            }
        }

        /// <summary>
        ///     Gets or sets the model fitting function.
        /// </summary>
        /// <remarks>
        ///     The fitting function should accept an array of integers containing the
        ///     indexes for the training samples, an array of integers containing the
        ///     indexes for the validation samples and should return information about
        ///     the model fitted using those two subsets of the available data.
        /// </remarks>
        public CrossValidationFittingFunction<TModel> Fitting
        {
            get => fitting;
            set => fitting = value;
        }

        /// <summary>
        ///     Gets the array of data set indexes contained in each fold.
        /// </summary>
        public int[][] Folds { get; }

        /// <summary>
        ///     Gets the array of fold indices for each point in the data set.
        /// </summary>
        public int[] Indices { get; }

        /// <summary>
        ///     Gets the number of folds in the k-fold cross validation.
        /// </summary>
        public int K => Folds.Length;

        /// <summary>
        ///     Gets the total number of data samples in the data set.
        /// </summary>
        public int Samples { get; }

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
        /// <param name="validationFoldIndex">The index of the validation fold.</param>
        /// <param name="trainingSet">The indices for the observations in the training set.</param>
        /// <param name="validationSet">The indices for the observations in the validation set.</param>
        public void CreatePartitions(int validationFoldIndex, out int[] trainingSet, out int[] validationSet)
        {
            if (validationFoldIndex < 0 || validationFoldIndex >= Folds.Length)
            {
                throw new ArgumentOutOfRangeException("validationFoldIndex");
            }

            // Create indices for the foldings

            // The training set is given by joining all sets
            // other than the current validation set.
            var list = new List<int[]>();
            for (var j = 0; j < Folds.Length; j++)
            {
                if (validationFoldIndex != j)
                {
                    list.Add(Folds[j]);
                }
            }

            // Select training set
            trainingSet = list.ToArray().Concatenate();

            // Select validation set
            validationSet = Folds[validationFoldIndex];
        }

        /// <summary>
        ///     Gets the number of instances in training and validation
        ///     sets for the specified validation fold index.
        /// </summary>
        /// <param name="validationFoldIndex">The index of the validation fold.</param>
        /// <param name="trainingCount">The number of instances in the training set.</param>
        /// <param name="validationCount">The number of instances in the validation set.</param>
        public void GetPartitionSize(int validationFoldIndex, out int trainingCount, out int validationCount)
        {
            if (validationFoldIndex < 0 || validationFoldIndex >= Folds.Length)
            {
                throw new ArgumentOutOfRangeException("validationFoldIndex");
            }

            trainingCount = 0;
            validationCount = 0;

            for (var j = 0; j < Folds.Length; j++)
            {
                if (validationFoldIndex != j)
                {
                    trainingCount += Folds[j].Length;
                }
            }

            validationCount = Folds[validationFoldIndex].Length;
        }

        /// <summary>
        ///     Computes the cross validation algorithm.
        /// </summary>
        public CrossValidationResult<TModel> Compute()
        {
            if (Fitting == null)
            {
                throw new InvalidOperationException("Fitting function must have been previously defined.");
            }

            var models = new CrossValidationValues<TModel>[Folds.Length];

            if (RunInParallel)
            {
                Parallel.For(0, Folds.Length, i =>
                {
                    int[] trainingSet, validationSet;

                    // Create training and validation sets
                    CreatePartitions(i, out trainingSet, out validationSet);

                    // Fit and evaluate the model
                    models[i] = fitting(i, trainingSet, validationSet);
                });
            }
            else
            {
                for (var i = 0; i < Folds.Length; i++)
                {
                    int[] trainingSet, validationSet;

                    // Create training and validation sets
                    CreatePartitions(i, out trainingSet, out validationSet);

                    // Fit and evaluate the model
                    models[i] = fitting(i, trainingSet, validationSet);
                }
            }

            // Return cross-validation statistics
            return new CrossValidationResult<TModel>(this, models);
        }
    }
}