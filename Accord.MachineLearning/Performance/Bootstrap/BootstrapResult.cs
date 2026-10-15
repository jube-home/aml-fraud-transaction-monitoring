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
using Accord.MachineLearning.Performance;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Obsolete. Please refer to <see cref="Bootstrap{TModel,TInput,TOutput}" /> instead.
    /// </summary>
    [Obsolete("Please use Bootstrap<TModel, TInput, TOutput> instead.")]
    public class BootstrapResult
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BootstrapResult" /> class.
        /// </summary>
        /// <param name="owner">The <see cref="Bootstrap" /> that is creating this result.</param>
        /// <param name="models">The models created during the cross-validation runs.</param>
        public BootstrapResult(Bootstrap owner, BootstrapValues[] models)
        {
            var trainingValues = new double[models.Length];
            var trainingVariances = new double[models.Length];
            var trainingCount = new int[models.Length];

            var validationValues = new double[models.Length];
            var validationVariances = new double[models.Length];
            var validationCount = new int[models.Length];

            for (var i = 0; i < models.Length; i++)
            {
                trainingValues[i] = models[i].TrainingValue;
                trainingVariances[i] = models[i].TrainingVariance;

                validationValues[i] = models[i].ValidationValue;
                validationVariances[i] = models[i].ValidationVariance;

                owner.GetPartitionSize(i, out trainingCount[i], out validationCount[i]);
            }

            Settings = owner;
            Training = new CrossValidationStatistics(trainingCount, trainingValues, trainingVariances);
            Validation = new CrossValidationStatistics(validationCount, validationValues, validationVariances);

            Estimate = 0.632 * Validation.Mean + 0.368 * Training.Mean;
        }

        /// <summary>
        ///     Gets the <see cref="Bootstrap" />
        ///     object used to generate this result.
        /// </summary>
        public Bootstrap Settings { get; private set; }

        /// <summary>
        ///     Gets the performance statistics for the training set.
        /// </summary>
        public CrossValidationStatistics Training { get; }

        /// <summary>
        ///     Gets the performance statistics for the validation set.
        /// </summary>
        public CrossValidationStatistics Validation { get; }

        /// <summary>
        ///     Gets the 0.632 bootstrap estimate.
        /// </summary>
        public double Estimate { get; private set; }
    }
}