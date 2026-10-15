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

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Base class for performance measurement methods based on splitting the data into multiple sets,
    ///     such as <see cref="SplitSetValidation{TModel, TInput, TOutput}" />,
    ///     <see cref="CrossValidation{TModel, TInput, TOutput}" />
    ///     and <see cref="Bootstrap{TModel, TInput, TOutput}" />.
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result learned by the validation method (e.g.
    ///     <see cref="CrossValidationResult{TModel, TInput, TOutput}" />).
    /// </typeparam>
    /// <typeparam name="TModel">The type of the machine learning model.</typeparam>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <typeparam name="TOutput">The type of the output data or labels.</typeparam>
    /// <seealso cref="GridSearch" />
    /// <seealso cref="SplitSetValidation{TModel, TInput, TOutput}" />
    /// <seealso cref="CrossValidation{TModel, TInput, TOutput}" />
    /// <seealso cref="Bootstrap{TModel, TInput, TOutput}" />
    public abstract class BaseSplitSetValidation<TResult, TModel, TInput, TOutput> : BaseSplitSetValidation<
            TResult,
            TModel,
            ISupervisedLearning<TModel, TInput, TOutput>,
            TInput, TOutput>,
        ISupervisedLearning<TResult, TInput, TOutput>
        where TModel : class, ITransform<TInput, TOutput>
        where TResult : ITransform<TInput, TOutput>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseSplitSetValidation{TResult, TModel, TInput, TOutput}" /> class.
        /// </summary>
        public BaseSplitSetValidation()
        {
            Fit = (teacher, inputs, outputs, weight) => teacher.Learn(inputs, outputs, weight);
        }
    }
}