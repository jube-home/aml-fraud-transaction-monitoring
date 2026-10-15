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

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Contains results from the grid-search procedure.
    /// </summary>
    /// <typeparam name="TModel">The type of the machine learning model whose parameters should be searched.</typeparam>
    /// <typeparam name="TInput">The type of the input data. Default is double[].</typeparam>
    /// <typeparam name="TOutput">The type of the output data. Default is int.</typeparam>
    /// <seealso cref="GridSearch" />
    public class
        GridSearchResult<TModel, TInput, TOutput> : GridSearchResult<TModel, GridSearchParameterCollection, TInput,
        TOutput>
        where TModel : class, ITransform<TInput, TOutput>
    {
    }

    /// <summary>
    ///     Contains results from the grid-search procedure.
    /// </summary>
    /// <typeparam name="TModel">The type of the machine learning model whose parameters should be searched.</typeparam>
    /// <typeparam name="TParam">The type that specifies how the value for a single parameter is represented.</typeparam>
    /// <typeparam name="TInput">The type of the input data. Default is double[].</typeparam>
    /// <typeparam name="TOutput">The type of the output data. Default is int.</typeparam>
    /// <seealso cref="GridSearch" />
    [Serializable]
    public class GridSearchResult<TModel, TParam, TInput, TOutput> : ITransform<TInput, TOutput>
        where TModel : class, ITransform<TInput, TOutput>
    {
        private int bestIndex;
        private double[] errors;
        private Exception[] exceptions;
        private TModel[] models;
        private TParam[] parameters;

        /// <summary>
        ///     Gets all combination of parameters tried.
        /// </summary>
        public TParam[] Parameters
        {
            get => parameters;
            set => parameters = value;
        }

        /// <summary>
        ///     Gets all models created during the search.
        /// </summary>
        public TModel[] Models
        {
            get => models;
            set => models = value;
        }

        /// <summary>
        ///     Gets the error for each of the created models.
        /// </summary>
        public double[] Errors
        {
            get => errors;
            set => errors = value;
        }

        /// <summary>
        ///     Gets exceptions found during the learning of each of the created models, if any.
        /// </summary>
        public Exception[] Exceptions
        {
            get => exceptions;
            set => exceptions = value;
        }

        /// <summary>
        ///     Gets the index of the best found model
        ///     in the <see cref="Models" /> collection.
        /// </summary>
        public int BestModelIndex
        {
            get => bestIndex;
            set => bestIndex = value;
        }

        /// <summary>
        ///     Gets the best model found.
        /// </summary>
        public TModel BestModel => models[bestIndex];

        /// <summary>
        ///     Gets the best parameter combination found.
        /// </summary>
        public TParam BestParameters => parameters[bestIndex];

        /// <summary>
        ///     Gets the minimum validation error found. If this
        ///     result has been retrieved through Grid-Search Cross-Validation,
        ///     this will correspond to the minimum average validation error
        ///     for the different data splits (validation folds).
        /// </summary>
        public double BestModelError => errors[bestIndex];

        /// <summary>
        ///     Gets the size of the grid used in the grid-search.
        /// </summary>
        public int Count => models.Length;

        /// <summary>
        ///     Gets the number of inputs accepted by the model.
        /// </summary>
        public int NumberOfInputs
        {
            get => BestModel.NumberOfInputs;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        /// <summary>
        ///     Gets the number of outputs generated by the model.
        /// </summary>
        public int NumberOfOutputs
        {
            get => BestModel.NumberOfOutputs;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>The output generated by applying this transformation to the given input.</returns>
        public TOutput Transform(TInput input)
        {
            return BestModel.Transform(input);
        }

        /// <summary>
        ///     Applies the transformation to a set of input vectors,
        ///     producing an associated set of output vectors.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public TOutput[] Transform(TInput[] input)
        {
            return BestModel.Transform(input);
        }

        /// <summary>
        ///     Applies the transformation to a set of input vectors,
        ///     producing an associated set of output vectors.
        /// </summary>
        /// <param name="input">
        ///     The input data to which
        ///     the transformation should be applied.
        /// </param>
        /// <param name="result">
        ///     The location to where to store the
        ///     result of this transformation.
        /// </param>
        /// <returns>
        ///     The output generated by applying this
        ///     transformation to the given input.
        /// </returns>
        public TOutput[] Transform(TInput[] input, TOutput[] result)
        {
            return BestModel.Transform(input, result);
        }
    }
}