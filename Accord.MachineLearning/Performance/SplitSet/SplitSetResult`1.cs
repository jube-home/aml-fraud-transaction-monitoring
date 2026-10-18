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
    ///     Obsolete. Please refer to <see cref="SplitSetValidation{TModel,TInput}" /> instead.
    /// </summary>
    [Obsolete("Please use SplitResult{TModel} instead.")]
    public class SplitSetResult<TModel>
        where TModel : class
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SplitSetValidation&lt;TModel&gt;" /> class.
        /// </summary>
        /// <param name="owner">The <see cref="SplitSetValidation{TModel}" /> that is creating this result.</param>
        /// <param name="training">The training set statistics.</param>
        /// <param name="testing">The testing set statistics.</param>
        public SplitSetResult(SplitSetValidation<TModel> owner,
            SplitSetStatistics<TModel> training, SplitSetStatistics<TModel> testing)
        {
            Settings = owner;
            Training = training;
            Validation = testing;
        }

        /// <summary>
        ///     Gets the <see cref="CrossValidation{T}" />
        ///     object used to generate this result.
        /// </summary>
        public SplitSetValidation<TModel> Settings { get; private set; }

        /// <summary>
        ///     Gets the performance statistics for the training set.
        /// </summary>
        public SplitSetStatistics<TModel> Training { get; private set; }

        /// <summary>
        ///     Gets the performance statistics for the validation set.
        /// </summary>
        public SplitSetStatistics<TModel> Validation { get; private set; }

        /// <summary>
        ///     Gets or sets a tag for user-defined information.
        /// </summary>
        public object Tag { get; set; }
    }
}