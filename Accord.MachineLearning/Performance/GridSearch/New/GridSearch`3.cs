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
    ///     Grid search procedure for automatic parameter tuning.
    /// </summary>
    /// <typeparam name="TModel">The type of the machine learning model whose parameters should be searched.</typeparam>
    /// <typeparam name="TInput">The type of the input data. Default is double[].</typeparam>
    /// <typeparam name="TOutput">The type of the output data. Default is int.</typeparam>
    /// <remarks>
    ///     Grid Search tries to find the best combination of parameters across a range of possible values that produces the
    ///     best fit model. If there
    ///     are two parameters, each with 10 possible values, Grid Search will try an exhaustive evaluation of the model using
    ///     every combination of points,
    ///     resulting in 100 model fits.
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The framework offers different ways to use grid search: one version is strongly-typed using generics
    ///         and the other might need some manual casting. The exapmle below shows how to perform grid-search in
    ///         a non-stringly typed way:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\GridSearchTest.cs" region="doc_learn" />
    ///     <para>
    ///         The main disadvantages of the method above is the need to keep string identifiers for each of the parameters
    ///         being searched. Furthermore, it is also necessary to keep track of their types in order to cast them
    ///         accordingly
    ///         when using them in the specification of the
    ///         <see cref="BaseGridSearch{TResult, TModel, TRange, TParam, TLearner, TInput, TOutput}.Learner" />
    ///         property.
    ///     </para>
    ///     <para>
    ///         The next example shows how to perform grid-search in a strongly typed way:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\GridSearchTest.cs" region="doc_learn_strongly_typed" />
    ///     <para>
    ///         The code above uses anonymous types and generics to create a specialized
    ///         <see cref="GridSearch{TModel, TInput, TOutput}" />
    ///         class that keeps the anonymous type given as
    ///         <see cref="BaseGridSearch{TResult, TModel, TRange, TParam, TLearner, TInput, TOutput}.ParameterRanges" />.
    ///         Its main disadvantage is the (high) increase in type complexity, making the use of the <c>var</c> keyword
    ///         almost
    ///         mandatory.
    ///     </para>
    ///     <para>
    ///         It is also possible to create grid-search objects using convenience methods from the static
    ///         <see cref="GridSearch" /> class:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\GridSearchTest.cs" region="doc_create" />
    ///     <para>
    ///         Finally, it is also possible to combine grid-search with
    ///         <see cref="CrossValidation{TModel, TInput, TOutput}" />,
    ///         as shown in the examples below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\GridSearchTest.cs" region="doc_learn_cv" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\GridSearchTest.cs" region="doc_learn_tree_cv" />
    /// </example>
    public class GridSearch<TModel, TInput, TOutput>
        : GridSearch<TModel, ISupervisedLearning<TModel, TInput, TOutput>, TInput, TOutput>
        where TModel : class, ITransform<TInput, TOutput>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GridSearch{TModel, TInput, TOutput}" /> class.
        /// </summary>
        public GridSearch()
        {
        }
    }
}