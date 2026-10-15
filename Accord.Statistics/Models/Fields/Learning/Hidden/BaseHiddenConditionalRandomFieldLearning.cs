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
using Accord.Properties;
using Accord.Statistics.Models.Fields.Functions;

namespace Accord.Statistics.Models.Fields.Learning
{
    /// <summary>
    ///     Abstract base class for hidden Conditional Random Fields algorithms.
    /// </summary>
    public abstract class BaseHiddenConditionalRandomFieldLearning<T>
    {
        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        [field: NonSerialized]
        public CancellationToken Token { get; set; }

        /// <summary>
        ///     Gets or sets the potential function to be used if this learning algorithm
        ///     needs to create a new <see cref="HiddenConditionalRandomField{T}" />.
        /// </summary>
        public IPotentialFunction<T> Function { get; set; }

        /// <summary>
        ///     Gets or sets the model being trained.
        /// </summary>
        public HiddenConditionalRandomField<T> Model { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public HiddenConditionalRandomField<T> Learn(T[][] x, int[] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            if (Model == null)
            {
                Model = Create(x, y);
            }

            InnerRun(x, y);

            return Model;
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors of this abstract
        ///     class must define this method so new models can be created from the training data.
        /// </summary>
        protected virtual HiddenConditionalRandomField<T> Create(T[][] x, int[] y)
        {
            if (Function == null)
            {
                throw new InvalidOperationException(
                    "Please set the Function property with the PotentialFunction would like to use.");
            }

            return new HiddenConditionalRandomField<T>(Function);
        }

        /// <summary>
        ///     Inheritors should implement the actual learning algorithm in this method.
        /// </summary>
        protected abstract double InnerRun(T[][] x, int[] y);
    }
}