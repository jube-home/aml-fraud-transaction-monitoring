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
using System.Linq;

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Class for representing results acquired through a
    ///     <see cref="CrossValidation{TModel,TInput,TOutput}">k-fold cross-validation analysis</see>.
    /// </summary>
    /// <typeparam name="TModel">The type of the machine learning model.</typeparam>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <typeparam name="TOutput">The type of the output data or labels.</typeparam>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\CrossValidationTest.cs" region="doc_learn" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\CrossValidationTest.cs" region="doc_learn_hmm" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\DecisionTreeTest.cs"
    ///         region="doc_cross_validation" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayesTest.cs" region="doc_cross_validation" />
    /// </example>
    [Serializable]
    public class CrossValidationResult<TModel, TInput, TOutput> : TrainValSplit<CrossValidationStatistics>,
        ITransform<TInput, TOutput>
        where TModel : class, ITransform<TInput, TOutput>
    {
        private Func<TOutput[], TOutput> combineMethod;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CrossValidationResult{TModel}" /> class.
        /// </summary>
        /// <param name="models">The models created during the cross-validation runs.</param>
        public CrossValidationResult(SplitResult<TModel, TInput, TOutput>[] models)
        {
            var trainingValues = models.Select(x => x.Training.Value).ToArray();
            var trainingVariances = models.Select(x => x.Training.Variance).ToArray();
            var trainingCount = models.Select(x => x.Training.NumberOfSamples).ToArray();

            var validationValues = models.Select(x => x.Validation.Value).ToArray();
            var validationVariances = models.Select(x => x.Validation.Variance).ToArray();
            var validationCount = models.Select(x => x.Validation.NumberOfSamples).ToArray();

            Models = models;
            Training = new CrossValidationStatistics(trainingCount, trainingValues, trainingVariances);
            Validation = new CrossValidationStatistics(validationCount, validationValues, validationVariances);
        }

        /// <summary>
        ///     Gets the total number of data samples in the entire data set.
        /// </summary>
        public int NumberOfSamples
        {
            get { return Models.Select(x => x.Validation.NumberOfSamples).Sum(); }
        }

        /// <summary>
        ///     Gets the average number of data samples in
        ///     each cross-validation fold of the data set.
        /// </summary>
        public double AverageNumberOfSamples
        {
            get { return Models.Select(x => x.Validation.NumberOfSamples).Average(); }
        }

        /// <summary>
        ///     Gets the models created for each fold of the cross validation.
        /// </summary>
        public SplitResult<TModel, TInput, TOutput>[] Models { get; private set; }

        /// <summary>
        ///     Gets or sets a tag for user-defined information.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        ///     Gets or sets the method used to combine the scores of different classifiers.
        /// </summary>
        public Func<TOutput[], TOutput> CombineMethod
        {
            get => combineMethod;
            set => combineMethod = value;
        }

        /// <summary>
        ///     Gets the number of inputs accepted by the model.
        /// </summary>
        /// <value>The number of inputs.</value>
        /// <exception cref="ArgumentException">This property is read only.</exception>
        public int NumberOfInputs
        {
            get => Models[0].NumberOfInputs;
            set => throw new ArgumentException("This property is read only.");
        }

        /// <summary>
        ///     Gets the number of outputs generated by the model.
        /// </summary>
        /// <value>The number of outputs.</value>
        /// <exception cref="ArgumentException">This property is read only.</exception>
        public int NumberOfOutputs
        {
            get => Models[0].NumberOfOutputs;
            set => throw new ArgumentException("This property is read only.");
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>The output generated by applying this transformation to the given input.</returns>
        /// <exception cref="Exception">
        ///     Please specify how the results of the different models should be combined by setting the
        ///     CombineMethod property.
        /// </exception>
        public TOutput Transform(TInput input)
        {
            if (CombineMethod == null)
            {
                throw new Exception(
                    "Please specify how the results of the different models should be combined by setting the CombineMethod property.");
            }

            var output = new TOutput[Models.Length];
            for (var i = 0; i < Models.Length; i++)
            {
                output[i] = Models[i].Model.Transform(input);
            }

            return CombineMethod(output);
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
            return Transform(input, new TOutput[input.Length]);
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
        /// <exception cref="Exception">
        ///     Please specify how the results of the different models should be combined by setting the
        ///     CombineMethod property.
        /// </exception>
        public TOutput[] Transform(TInput[] input, TOutput[] result)
        {
            if (CombineMethod == null)
            {
                throw new Exception(
                    "Please specify how the results of the different models should be combined by setting the CombineMethod property.");
            }

            var output = new TOutput[Models.Length];
            for (var j = 0; j < input.Length; j++)
            {
                for (var i = 0; i < Models.Length; i++)
                {
                    output[i] = Models[i].Model.Transform(input[i]);
                }

                result[j] = CombineMethod(output);
            }

            return result;
        }
    }
}