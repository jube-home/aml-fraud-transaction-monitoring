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

namespace Accord.Statistics.Models.Markov
{
    /// <summary>
    ///     Common interface for sequence classifiers using
    ///     <see cref="IHiddenMarkovModel">
    ///         hidden Markov models
    ///     </see>
    ///     .
    /// </summary>
    [Obsolete("Please use IMulticlassClassifier from Accord.MachineLearning namespace.")]
    public interface IHiddenMarkovClassifier
    {
        /// <summary>
        ///     Gets the number of classes which can be recognized by this classifier.
        /// </summary>
        int Classes { get; }

        /// <summary>
        ///     Computes the most likely class for a given sequence.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <param name="responsibilities">
        ///     The class responsibilities (or
        ///     the probability of the sequence to belong to each class). When
        ///     using threshold models, the sum of the probabilities will not
        ///     equal one, and the amount left was the threshold probability.
        ///     If a threshold model is not being used, the array should sum to
        ///     one.
        /// </param>
        /// <returns>
        ///     Return the label of the given sequence, or -1 if it has
        ///     been rejected.
        /// </returns>
        int Compute(Array sequence, out double[] responsibilities);
    }
}