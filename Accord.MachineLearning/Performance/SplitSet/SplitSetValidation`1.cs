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
using System.Linq;
using Accord.MachineLearning.Performance;
using Accord.Math;
using Accord.Statistics;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Obsolete. Please use <see cref="SplitSetValidation{TModel,TInput,TOutput}" /> instead.
    /// </summary>
    [Obsolete("Please refer to SplitSetValidation<TModel, TInput> instead.")]
    public delegate SplitSetStatistics<TModel> SplitValidationFittingFunction<TModel>(int[] trainingSamples)
        where TModel : class;

    /// <summary>
    ///     Obsolete. Please use <see cref="SplitSetValidation{TModel, TInput, TOutput}" /> instead.
    /// </summary>
    [Obsolete("Please refer to SplitSetValidation<TModel, TInput> instead.")]
    public delegate SplitSetStatistics<TModel> SplitValidationEvaluateFunction<TModel>(int[] validationSamples,
        TModel model)
        where TModel : class;

    /// <summary>
    ///     Obsolete. Please use <see cref="SplitSetValidation{TModel, TInput, TOutput}" /> instead.
    /// </summary>
    [Serializable]
    [Obsolete("Please use SplitSetValidation<TModel, TInput> instead.")]
    public class SplitSetValidation<TModel> where TModel : class
    {
        [NonSerialized] private SplitValidationEvaluateFunction<TModel> estimation;

        [NonSerialized] private SplitValidationFittingFunction<TModel> fitting;

        /// <summary>
        ///     Creates a new split-set validation algorithm.
        /// </summary>
        /// <param name="size">The total number of available samples.</param>
        /// <param name="proportion">
        ///     The desired proportion of samples in the training
        ///     set in comparison with the testing set.
        /// </param>
        public SplitSetValidation(int size, double proportion)
        {
            Proportion = proportion;
            IsStratified = false;
            Indices = Classes.Random(size, proportion);

            ValidationSet = Indices.Find(x => x == 0);
            TrainingSet = Indices.Find(x => x == 1);
        }

        /// <summary>
        ///     Creates a new split-set validation algorithm.
        /// </summary>
        /// <param name="size">The total number of available samples.</param>
        /// <param name="proportion">
        ///     The desired proportion of samples in the training
        ///     set in comparison with the testing set.
        /// </param>
        /// <param name="outputs">The output labels to be balanced between the sets.</param>
        public SplitSetValidation(int size, double proportion, int[] outputs)
        {
            if (outputs.Length != size)
            {
                throw new DimensionMismatchException("outputs");
            }

            IsStratified = true;
            Proportion = proportion;

            var negative = outputs.Min();
            var positive = outputs.Max();

            var negativeIndices = outputs.Find(x => x == negative).ToList();
            var positiveIndices = outputs.Find(x => x == positive).ToList();

            var positiveCount = positiveIndices.Count;
            var negativeCount = negativeIndices.Count;

            var firstGroupPositives = (int)(positiveCount / 2.0 * proportion);
            var firstGroupNegatives = (int)(negativeCount / 2.0 * proportion);

            var training = new List<int>();
            var testing = new List<int>();

            // Put positives and negatives into training
            for (var j = 0; j < firstGroupNegatives; j++)
            {
                training.Add(negativeIndices[0]);
                negativeIndices.RemoveAt(0);
            }

            for (var j = 0; j < firstGroupPositives; j++)
            {
                training.Add(positiveIndices[0]);
                positiveIndices.RemoveAt(0);
            }

            testing.AddRange(negativeIndices);
            testing.AddRange(positiveIndices);

            TrainingSet = training.ToArray();
            ValidationSet = testing.ToArray();
        }

        /// <summary>
        ///     Gets the group labels assigned to each of the data samples.
        /// </summary>
        public int[] Indices { get; private set; }

        /// <summary>
        ///     Gets the desired proportion of cases in
        ///     the training set in comparison to the
        ///     testing set.
        /// </summary>
        public double Proportion { get; private set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the prevalence of
        ///     an output label should be balanced between training and
        ///     testing sets.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is stratified; otherwise, <c>false</c>.
        /// </value>
        public bool IsStratified { get; private set; }

        /// <summary>
        ///     Gets the indices of elements in the validation set.
        /// </summary>
        public int[] ValidationSet { get; private set; }

        /// <summary>
        ///     Gets the indices of elements in the training set.
        /// </summary>
        public int[] TrainingSet { get; private set; }

        /// <summary>
        ///     Get or sets the model fitting function.
        /// </summary>
        public SplitValidationFittingFunction<TModel> Fitting
        {
            get => fitting;
            set => fitting = value;
        }

        /// <summary>
        ///     Gets or sets the performance estimation function.
        /// </summary>
        public SplitValidationEvaluateFunction<TModel> Evaluation
        {
            get => estimation;
            set => estimation = value;
        }

        /// <summary>
        ///     Computes the split-set validation algorithm.
        /// </summary>
        public SplitSetResult<TModel> Compute()
        {
            if (Fitting == null)
            {
                throw new InvalidOperationException("Fitting function must have been previously defined.");
            }

            // Fit and evaluate the model
            var training = fitting(TrainingSet);
            var testing = estimation(ValidationSet, training.Model);

            // Return validation statistics
            return new SplitSetResult<TModel>(this, training, testing);
        }
    }
}