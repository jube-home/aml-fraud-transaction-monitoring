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
    ///     Summary statistics for a Split-set validation trial.
    /// </summary>
    [Serializable]
    public class SplitSetStatistics<TModel>
        where TModel : class
    {
        /// <summary>
        ///     Create a new split-set statistics class.
        /// </summary>
        /// <param name="model">The generated model.</param>
        /// <param name="size">The number of samples used to compute the statistic.</param>
        /// <param name="value">The performance statistic gathered during the run.</param>
        /// <param name="variance">The variance of the performance statistic during the run.</param>
        public SplitSetStatistics(TModel model, int size, double value, double variance)
        {
            Size = size;
            Model = model;
            Value = value;
            Variance = variance;
        }

        /// <summary>
        ///     Gets the model created with the
        /// </summary>
        public TModel Model { get; private set; }

        /// <summary>
        ///     Gets the values acquired during the cross-validation.
        ///     Most often those will be the errors for each folding.
        /// </summary>
        public double Value { get; private set; }

        /// <summary>
        ///     Gets the variance for each value acquired during the cross-validation.
        ///     Most often those will be the error variance for each folding.
        /// </summary>
        public double Variance { get; private set; }

        /// <summary>
        ///     Gets the number of samples used to compute the variance
        ///     of the values acquired during the validation.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        ///     Gets the standard deviation of the performance statistic.
        /// </summary>
        public double StandardDeviation => System.Math.Sqrt(Variance);

        /// <summary>
        ///     Gets or sets a tag for user-defined information.
        /// </summary>
        public object Tag { get; set; }
    }

    /// <summary>
    ///     Obsolete. Please refer to <see cref="SplitSetValidation{TModel,TInput,TOutput}" /> instead.
    /// </summary>
    [Obsolete("Please refer to SplitSetValidation<TModel, TInput> instead.")]
    public class SplitSetStatistics : SplitSetStatistics<object>
    {
        /// <summary>
        ///     Create a new split-set statistics class.
        /// </summary>
        /// <param name="model">The generated model.</param>
        /// <param name="size">The number of samples used to compute the statistic.</param>
        /// <param name="value">The performance statistic gathered during the run.</param>
        /// <param name="variance">The variance of the performance statistic during the run.</param>
        public SplitSetStatistics(object model, int size, double value, double variance)
            : base(model, size, value, variance)
        {
        }

        /// <summary>
        ///     Create a new split-set statistics class.
        /// </summary>
        /// <param name="model">The generated model.</param>
        /// <param name="size">The number of samples used to compute the statistic.</param>
        /// <param name="value">The performance statistic gathered during the run.</param>
        /// <param name="variance">The variance of the performance statistic during the run.</param>
        public static SplitSetStatistics<TModel> Create<TModel>(TModel model, int size,
            double value, double variance) where TModel : class
        {
            return new SplitSetStatistics<TModel>(model, size, value, variance);
        }
    }
}