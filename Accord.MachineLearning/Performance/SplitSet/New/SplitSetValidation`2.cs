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

using Accord.Statistics;
using Accord.Statistics.Analysis;

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Split-Set Validation (with support for stratification and default loss function for classification).
    /// </summary>
    /// <typeparam name="TModel">The type of the machine learning model.</typeparam>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\SplitSetTest.cs" region="doc_learn" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\SplitSetTest.cs" region="doc_learn_multiclass" />
    /// </example>
    /// <seealso cref="Bootstrap{TModel, TInput, TOutput}" />
    /// <seealso cref="CrossValidation{TModel, TInput}" />
    /// <seealso cref="SplitSetValidation{TModel, TInput, TOutput}" />
    public class SplitSetValidation<TModel, TInput>
        : SplitSetValidation<TModel, TInput, int>
        where TModel : class, IClassifier<TInput, int>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SplitSetValidation{TModel, TInput}" /> class.
        /// </summary>
        public SplitSetValidation()
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
        ///     Creates a list of the sample indices that should serve as the validation set.
        /// </summary>
        /// <param name="x">The input data from where subsamples should be drawn.</param>
        /// <param name="y">The output data from where subsamples should be drawn.</param>
        /// <returns>The indices of the samples in the original set that should compose the validation set.</returns>
        protected override int[] CreateValidationSplits(TInput[] x, int[] y)
        {
            if (Stratify)
            {
                return Classes.Random(y, TrainingSetProportion);
            }

            return base.CreateValidationSplits(x, y);
        }
    }
}