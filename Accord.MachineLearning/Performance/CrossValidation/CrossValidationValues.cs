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
    ///     Obsolete. Please refer to <see cref="CrossValidation{TModel,TInput}" /> instead.
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\CrossValidationTest.cs" region="doc_learn" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\CrossValidationTest.cs" region="doc_learn_hmm" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\DecisionTreeTest.cs"
    ///         region="doc_cross_validation" />
    /// </example>
    [Obsolete("Please refer to CrossValidation<TModel, TInput> instead.")]
    [Serializable]
    public class CrossValidationValues : CrossValidationValues<object>
    {
        /// <summary>
        ///     Creates a new Cross-Validation Values class.
        /// </summary>
        /// <param name="trainingValue">The training value for the model.</param>
        /// <param name="validationValue">The validation value for the model.</param>
        public CrossValidationValues(
            double trainingValue, double validationValue)
            : base(trainingValue, validationValue)
        {
        }

        /// <summary>
        ///     Creates a new Cross-Validation Values class.
        /// </summary>
        /// <param name="trainingValue">The training value for the model.</param>
        /// <param name="validationValue">The validation value for the model.</param>
        /// <param name="trainingVariance">The variance of the training values.</param>
        /// <param name="validationVariance">The variance of the validation values.</param>
        public CrossValidationValues(
            double trainingValue, double trainingVariance,
            double validationValue, double validationVariance)
            : base(trainingValue, trainingVariance, validationValue, validationVariance)
        {
        }

        /// <summary>
        ///     Creates a new Cross-Validation Values class.
        /// </summary>
        /// <param name="model">The fitted model.</param>
        /// <param name="trainingValue">The training value for the model.</param>
        /// <param name="validationValue">The validation value for the model.</param>
        public CrossValidationValues(object model,
            double trainingValue, double validationValue)
            : base(model, trainingValue, validationValue)
        {
        }

        /// <summary>
        ///     Creates a new Cross-Validation Values class.
        /// </summary>
        /// <param name="model">The fitted model.</param>
        /// <param name="trainingValue">The training value for the model.</param>
        /// <param name="validationValue">The validation value for the model.</param>
        /// <param name="trainingVariance">The variance of the training values.</param>
        /// <param name="validationVariance">The variance of the validation values.</param>
        public CrossValidationValues(object model,
            double trainingValue, double trainingVariance,
            double validationValue, double validationVariance)
            : base(model, trainingValue, trainingVariance, validationValue, validationVariance)
        {
        }

        /// <summary>
        ///     Creates a new Cross-Validation Values class.
        /// </summary>
        /// <param name="model">The fitted model.</param>
        /// <param name="trainingValue">The training value for the model.</param>
        /// <param name="validationValue">The validation value for the model.</param>
        /// <param name="trainingVariance">The variance of the training values.</param>
        /// <param name="validationVariance">The variance of the validation values.</param>
        public static CrossValidationValues<TModel> Create<TModel>(TModel model,
            double trainingValue, double trainingVariance,
            double validationValue, double validationVariance) where TModel : class
        {
            return new CrossValidationValues<TModel>(model, trainingValue, trainingVariance, validationValue,
                validationVariance);
        }

        /// <summary>
        ///     Creates a new Cross-Validation Values class.
        /// </summary>
        /// <param name="model">The fitted model.</param>
        /// <param name="trainingValue">The training value for the model.</param>
        /// <param name="trainingVariance">The variance of the training values.</param>
        public static CrossValidationValues<TModel> Create<TModel>(TModel model,
            double trainingValue, double trainingVariance) where TModel : class
        {
            return new CrossValidationValues<TModel>(model, trainingValue, trainingVariance);
        }
    }
}