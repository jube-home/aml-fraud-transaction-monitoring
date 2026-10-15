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
    ///     Training and validation errors of a model.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    [Serializable]
    public class SetResult<TModel>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SetResult{TModel}" /> class.
        /// </summary>
        /// <param name="model">The model computed in this subset.</param>
        /// <param name="indices">The indices of the samples in this subset.</param>
        /// <param name="name">The name of this set.</param>
        /// <param name="proportion">The proportion of samples in this subset, compared to the full dataset.</param>
        public SetResult(TModel model, int[] indices, string name, double proportion)
        {
            Name = name;
            Model = model;
            Indices = indices;
            NumberOfSamples = indices.Length;
            Proportion = proportion;
        }

        /// <summary>
        ///     Gets or sets the set name (e.g. "Training" or "Testing").
        /// </summary>
        /// <value>The name of this set.</value>
        public string Name { get; set; }

        /// <summary>
        ///     Gets the model.
        /// </summary>
        public TModel Model { get; private set; }

        /// <summary>
        ///     Gets the indices of the samples in this subset in the original complete dataset.
        /// </summary>
        public int[] Indices { get; private set; }

        /// <summary>
        ///     Gets the number of samples in this subset.
        /// </summary>
        public int NumberOfSamples { get; private set; }

        /// <summary>
        ///     Gets how much this subset represents, in proportion, of the original dataset.
        /// </summary>
        public double Proportion { get; private set; }

        /// <summary>
        ///     Gets the metric value for the model in the current <see cref="Name">set</see>.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        ///     Gets the variance of the validation value for the model, if available.
        /// </summary>
        public double Variance { get; set; }

        /// <summary>
        ///     Gets the standard deviation of the validation value for the model, if available.
        /// </summary>
        public double StandardDeviation => System.Math.Sqrt(Variance);

        /// <summary>
        ///     Gets or sets a tag for user-defined information.
        /// </summary>
        public object Tag { get; set; }
    }
}