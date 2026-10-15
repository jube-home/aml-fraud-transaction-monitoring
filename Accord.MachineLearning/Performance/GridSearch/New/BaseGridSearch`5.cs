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
using System.Threading.Tasks;
using Accord.Math;

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Function signature for a function that creates a machine learning model
    ///     given a set of parameter values. This function should use the parameters to create and configure
    ///     a <see cref="ISupervisedLearning{TModel, TInput, TOutput}" /> learning algorithm that can in turn
    ///     be used to create a new machine learning model with those parameters.
    /// </summary>
    /// <param name="parameters">The training parameters.</param>
    /// <returns>
    ///     A <see cref="ISupervisedLearning{TModel, TInput, TOutput}" />learning algorithm that can be used
    ///     to create and train machine learning models.
    /// </returns>
    public delegate TLearner CreateLearnerFromParameter<TLearner, TParam>(TParam parameters);

    /// <summary>
    ///     Base class for <see cref="GridSearch{TModel, TInput, TOutput}" /> methods.
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the object that should hold the results of the grid serach (e.g.
    ///     <see cref="GridSearchResult{TModel, TInput, TOutput}" />).
    /// </typeparam>
    /// <typeparam name="TModel">The type of the machine learning model whose parameters should be searched.</typeparam>
    /// <typeparam name="TRange">The type that specifies how ranges of the parameter values are represented.</typeparam>
    /// <typeparam name="TParam">The type that specifies how the value for a single parameter is represented.</typeparam>
    /// <typeparam name="TLearner">The type of the learning algorithm used to learn <typeparamref name="TModel" />.</typeparam>
    /// <typeparam name="TInput">The type of the input data. Default is double[].</typeparam>
    /// <typeparam name="TOutput">The type of the output data. Default is int.</typeparam>
    /// <seealso cref="GridSearch" />
    /// <seealso cref="GridSearch{TInput, TOutput}" />
    /// <seealso cref="GridSearch{TModel, TInput, TOutput}" />
    /// <seealso cref="GridSearch{TModel, TLearner, TInput, TOutput}" />
    /// <seealso cref="GridSearch{TModel, TRange, TLearner, TInput, TOutput}" />
    /// <seealso cref="ParallelLearningBase" />
    /// <seealso cref="ISupervisedLearning{TResult, TInput, TOutput}" />
    public abstract class BaseGridSearch<TResult, TModel, TRange, TParam, TLearner, TInput, TOutput> :
        ParallelLearningBase,
        ISupervisedLearning<TResult, TInput, TOutput>
        where TModel : class, ITransform<TInput, TOutput>
        where TResult : GridSearchResult<TModel, TParam, TInput, TOutput>, new()
    {
        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="BaseGridSearch{TResult, TModel, TRange, TParam, TLearner, TInput, TOutput}" /> class.
        /// </summary>
        protected BaseGridSearch()
        {
        }

        /// <summary>
        ///     The range of parameters to consider during search.
        /// </summary>
        public TRange ParameterRanges { get; set; }

        /// <summary>
        ///     Gets or sets a <see cref="CreateLearnerFromParameter{TLearner, TParam}" /> function
        ///     that can be used to create a <typeparamref name="TModel" /> given training parameters.
        /// </summary>
        public CreateLearnerFromParameter<TLearner, TParam> Learner { get; set; }

        /// <summary>
        ///     Gets or sets a <see cref="LearnNewModel{TLearner, TInput, TOutput, TModel}" /> function that can be used to create
        ///     new
        ///     <typeparam ref="TModel">machine learning models</typeparam>
        ///     using the current
        ///     <typeparam ref="TLearner">learning algorithm</typeparam>
        ///     .
        /// </summary>
        public LearnNewModel<TLearner, TInput, TOutput, TModel> Fit { get; set; }

        /// <summary>
        ///     Gets or sets a <see cref="ComputeLoss{TOutput, TInfo}" /> function that can
        ///     be used to measure how far the actual model predictions were from the expected ground-truth.
        /// </summary>
        public ComputeLoss<TOutput, TModel> Loss { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public TResult Learn(TInput[] x, TOutput[] y, double[] weights = null)
        {
            if (Learner == null)
            {
                throw new InvalidOperationException();
            }

            if (ParameterRanges == null)
            {
                throw new InvalidOperationException();
            }

            if (Loss == null)
            {
                throw new InvalidOperationException();
            }

            if (Fit == null)
            {
                throw new InvalidOperationException();
            }


            // Use reflection to get properties of the anonymous 
            // type that return GridSearchRange<T>.

            var lengths = GetLengths();

            // Get the total number of different parameters
            var values = new int[lengths.Length][];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Vector.Range(0, lengths[i]);
            }

            // Generate the Cartesian product between all parameters
            var grid = Matrix.Cartesian(values);

            // Initialize the search
            var parameters = new TParam[grid.Length];
            var models = new TModel[grid.Length];
            var errors = new double[grid.Length];
            var exceptions = new Exception[grid.Length];

            for (var i = 0; i < grid.Length; i++)
            {
                parameters[i] = GetParameters(grid[i]);
            }

            // Search the grid for the optimal parameters
            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                for (var i = 0; i < grid.Length; i++)
                {
                    InnerLearn(x, y, weights, parameters[i], out models[i], out errors[i], out exceptions[i]);
                }
            }
            else
            {
                Parallel.For(0, grid.Length, ParallelOptions, i =>
                    InnerLearn(x, y, weights, parameters[i], out models[i], out errors[i], out exceptions[i]));
            }


            // Select the model with minimum error
            var minimumErrorIndex = errors.ArgMin();


            // Return the best model found.
            var result = new TResult();
            result.BestModelIndex = minimumErrorIndex;
            result.Models = models;
            result.Errors = errors;
            result.Parameters = parameters;
            result.Exceptions = exceptions;
            return result;
        }

        private void InnerLearn(TInput[] x, TOutput[] y, double[] w, TParam parameters, out TModel model,
            out double error, out Exception exception)
        {
            exception = null;
            error = double.PositiveInfinity;
            model = null;


            // Try to fit a model using the parameters
            var teacher = Learner(parameters);

            try
            {
                model = Fit(teacher, x, y, w);

                TOutput[] predicted = null;

                try
                {
                    predicted = model.Transform(x);
                }
                catch
                {
                    // 
                }

                error = Loss(y, predicted, model);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }

        /// <summary>
        ///     Inheritors of this class should specify how to get actual values for the parameters
        ///     given a index vector in the grid-search space. Those indices indicate which values
        ///     should be given, e.g. if there are two parameters in the problem, the ranges of the
        ///     first parameter are {10, 20, 30}, and the ranges of the second parameter are {0.1, 0.01, 0.001 },
        ///     if the index vector is { 1, 2 } this method should return { 20, 0.001 }.
        /// </summary>
        /// <param name="indices">The indices in grid-search space.</param>
        /// <returns>The parameters at the location indicated by <paramref name="indices" />.</returns>
        protected abstract TParam GetParameters(int[] indices);

        /// <summary>
        ///     Inheritors of this class should return the number of possible parameter values for
        ///     each parameter in the grid-search range. For example, if a problem should search
        ///     parameters in the range {0, 1, ... 9} (10 values) and {-1, -2, -3 } (3 values), this
        ///     method should return { 10, 3 }.
        /// </summary>
        /// <returns>The number of possibilities for each parameter.</returns>
        protected abstract int[] GetLengths();
    }
}