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
using System.Threading;
using System.Threading.Tasks;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Statistics.Kernels;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Delegate for grid search fitting functions.
    /// </summary>
    /// <typeparam name="TModel">The type of the model to fit.</typeparam>
    /// <param name="parameters">The collection of parameters to be used in the fitting process.</param>
    /// <param name="error">The error (or any other performance measure) returned by the model.</param>
    /// <returns>The model fitted to the data using the given parameters.</returns>
    public delegate TModel
        GridSearchFittingFunction<TModel>(GridSearchParameterCollection parameters, out double error);

    /// <summary>
    ///     Grid search procedure for automatic parameter tuning.
    /// </summary>
    /// <remarks>
    ///     Grid Search tries to find the best combination of parameters across
    ///     a range of possible values that produces the best fit model. If there
    ///     are two parameters, each with 10 possible values, Grid Search will try
    ///     an exhaustive evaluation of the model using every combination of points,
    ///     resulting in 100 model fits.
    /// </remarks>
    /// <typeparam name="TModel">The type of the model to be tuned.</typeparam>
    /// <example>
    ///     How to fit a Kernel Support Vector Machine using Grid Search.
    ///     <code>
    ///   // Example binary data
    ///   double[][] inputs =
    ///   {
    ///       new double[] { -1, -1 },
    ///       new double[] { -1,  1 },
    ///       new double[] {  1, -1 },
    ///       new double[] {  1,  1 }
    ///   };
    /// 
    ///   int[] xor = // xor labels
    ///   {
    ///       -1, 1, 1, -1
    ///   };
    /// 
    ///   // Declare the parameters and ranges to be searched
    ///   GridSearchRange[] ranges = 
    ///   {
    ///       new GridSearchRange("complexity", new double[] { 0.00000001, 5.20, 0.30, 0.50 } ),
    ///       new GridSearchRange("degree",     new double[] { 1, 10, 2, 3, 4, 5 } ),
    ///       new GridSearchRange("constant",   new double[] { 0, 1, 2 } )
    ///   };
    /// 
    /// 
    ///   // Instantiate a new Grid Search algorithm for Kernel Support Vector Machines
    ///   var gridsearch = new GridSearch&lt;KernelSupportVectorMachine>(ranges);
    /// 
    ///   // Set the fitting function for the algorithm
    ///   gridsearch.Fitting = delegate(GridSearchParameterCollection parameters, out double error)
    ///   {
    ///       // The parameters to be tried will be passed as a function parameter.
    ///       int degree = (int)parameters["degree"].Value;
    ///       double constant = parameters["constant"].Value;
    ///       double complexity = parameters["complexity"].Value;
    /// 
    ///       // Use the parameters to build the SVM model
    ///       Polynomial kernel = new Polynomial(degree, constant);
    ///       KernelSupportVectorMachine ksvm = new KernelSupportVectorMachine(kernel, 2);
    /// 
    ///       // Create a new learning algorithm for SVMs
    ///       SequentialMinimalOptimization smo = new SequentialMinimalOptimization(ksvm, inputs, xor);
    ///       smo.Complexity = complexity;
    /// 
    ///       // Measure the model performance to return as an out parameter
    ///       error = smo.Run();
    /// 
    ///       return ksvm; // Return the current model
    ///   };
    /// 
    /// 
    ///   // Declare some out variables to pass to the grid search algorithm
    ///   GridSearchParameterCollection bestParameters; double minError;
    /// 
    ///   // Compute the grid search to find the best Support Vector Machine
    ///   KernelSupportVectorMachine bestModel = gridsearch.Compute(out bestParameters, out minError);
    ///   </code>
    /// </example>
    [Obsolete("Please use GridSearch<TModel, TInput, TOutput> instead.")]
    [Serializable]
    public class GridSearch<TModel> : IParallel
    // TODO: Inherit from ISupervisedLearning<TModel>
        where TModel : class
    {
        private GridSearchFittingFunction<TModel> fitting;
        private ParallelOptions parallelOptions = new();

        /// <summary>
        ///     Constructs a new Grid search algorithm.
        /// </summary>
        /// <param name="parameterRanges">The range of parameters to search.</param>
        public GridSearch(params GridSearchRange[] parameterRanges)
        {
            ParameterRanges = new GridSearchRangeCollection(parameterRanges);
        }

        /// <summary>
        ///     A function that fits a model using the given parameters.
        /// </summary>
        public GridSearchFittingFunction<TModel> Fitting
        {
            get => fitting;
            set => fitting = value;
        }

        /// <summary>
        ///     The range of parameters to consider during search.
        /// </summary>
        public GridSearchRangeCollection ParameterRanges { get; }

        /// <summary>
        ///     Gets or sets the parallelization options for this algorithm.
        /// </summary>
        public ParallelOptions ParallelOptions
        {
            get => parallelOptions;
            set => parallelOptions = value;
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used
        ///     to cancel the algorithm while it is running.
        /// </summary>
        public CancellationToken Token
        {
            get => parallelOptions.CancellationToken;
            set => parallelOptions.CancellationToken = value;
        }

        /// <summary>
        ///     Searches for the best combination of parameters that results in the most accurate model.
        /// </summary>
        /// <param name="bestParameters">The best combination of parameters found by the grid search.</param>
        /// <param name="error">The minimum error of the best model found by the grid search.</param>
        /// <returns>The best model found during the grid search.</returns>
        public TModel Compute(out GridSearchParameterCollection bestParameters, out double error)
        {
            var result = Compute();
            bestParameters = result.Parameter;
            error = result.Error;
            return result.Model;
        }

        /// <summary>
        ///     Searches for the best combination of parameters that results in the most accurate model.
        /// </summary>
        /// <returns>The results found during the grid search.</returns>
        public GridSearchResult<TModel> Compute()
        {
            // Get the total number of different parameters
            var values = new GridSearchParameter[ParameterRanges.Count][];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = ParameterRanges[i].GetParameters();
            }


            // Generate the Cartesian product between all parameters
            var grid = Matrix.Cartesian(values);


            // Initialize the search
            var parameters = new GridSearchParameterCollection[grid.Length];
            var models = new TModel[grid.Length];
            var errors = new double[grid.Length];

            // Search the grid for the optimal parameters
            Parallel.For(0, grid.Length, parallelOptions, i =>
            {
                // Get the current parameters for the current point
                parameters[i] = new GridSearchParameterCollection(grid[i]);

                try
                {
                    // Try to fit a model using the parameters
                    models[i] = Fitting(parameters[i], out errors[i]);
                }
                catch (ConvergenceException)
                {
                    errors[i] = double.PositiveInfinity;
                }
            });


            // Select the minimum error
            int best;
            errors.Min(out best);

            // Return the best model found.
            return new GridSearchResult<TModel>(grid.Length,
                parameters, models, errors, best);
        }

        internal object Create(object ranges, Func<object, SequentialMinimalOptimization<Polynomial>> learner,
            Func<object, object, object, double> loss)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Contains results from the grid-search procedure.
    /// </summary>
    /// <typeparam name="TModel">The type of the model to be tuned.</typeparam>
    public class GridSearchResult<TModel> where TModel : class
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GridSearchResult&lt;TModel&gt;" /> class.
        /// </summary>
        public GridSearchResult(int size)
        {
            Count = size;
            Parameters = new GridSearchParameterCollection[size];
            Models = new TModel[size];
            Errors = new double[size];
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GridSearchResult&lt;TModel&gt;" /> class.
        /// </summary>
        public GridSearchResult(int size, GridSearchParameterCollection[] parameters,
            TModel[] models, double[] errors, int index)
        {
            Count = size;

            if (parameters.Length != size || models.Length != size || errors.Length != size)
            {
                throw new DimensionMismatchException("size", "All array parameters must have the same length.");
            }

            if (0 > index || index >= size)
            {
                throw new ArgumentOutOfRangeException("index", "Index must be higher than 0 and less than size.");
            }

            this.Parameters = parameters;
            this.Models = models;
            this.Errors = errors;
            Index = index;
        }

        /// <summary>
        ///     Gets all combination of parameters tried.
        /// </summary>
        public GridSearchParameterCollection[] Parameters { get; }

        /// <summary>
        ///     Gets all models created during the search.
        /// </summary>
        public TModel[] Models { get; }

        /// <summary>
        ///     Gets the error for each of the created models.
        /// </summary>
        public double[] Errors { get; }

        /// <summary>
        ///     Gets the index of the best found model
        ///     in the <see cref="Models" /> collection.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Gets the best model found.
        /// </summary>
        public TModel Model => Models[Index];

        /// <summary>
        ///     Gets the best parameter combination found.
        /// </summary>
        public GridSearchParameterCollection Parameter => Parameters[Index];

        /// <summary>
        ///     Gets the minimum error found.
        /// </summary>
        public double Error => Errors[Index];

        /// <summary>
        ///     Gets the size of the grid used in the grid-search.
        /// </summary>
        public int Count { get; }
    }
}