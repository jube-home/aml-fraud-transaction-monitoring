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
    [Obsolete("Please refer to Bootstrap<TModel, TInput, TOutput> instead.")]
    public class BootstrapValues
    {
        /// <param name="trainingValue">The training value for the model.</param>
        /// <param name="validationValue">The validation value for the model.</param>
        public BootstrapValues(double trainingValue, double validationValue)
        {
            TrainingValue = trainingValue;
            ValidationValue = validationValue;
        }

        /// <param name="trainingValue">The training value for the model.</param>
        /// <param name="validationValue">The validation value for the model.</param>
        /// <param name="trainingVariance">The variance of the training values.</param>
        /// <param name="validationVariance">The variance of the validation values.</param>
        public BootstrapValues(double trainingValue, double trainingVariance,
            double validationValue, double validationVariance)
        {
            TrainingValue = trainingValue;
            ValidationValue = validationValue;

            TrainingVariance = trainingVariance;
            ValidationVariance = validationVariance;
        }

        /// <summary>
        ///     Gets the validation value for the model.
        /// </summary>
        public double ValidationValue { get; private set; }

        /// <summary>
        ///     Gets the variance of the validation
        ///     value for the model, if available.
        /// </summary>
        public double ValidationVariance { get; private set; }

        /// <summary>
        ///     Gets the training value for the model.
        /// </summary>
        public double TrainingValue { get; private set; }

        /// <summary>
        ///     Gets the variance of the training
        ///     value for the model, if available.
        /// </summary>
        public double TrainingVariance { get; private set; }

        /// <summary>
        ///     Gets or sets a tag for user-defined information.
        /// </summary>
        public object Tag { get; set; }
    }
}