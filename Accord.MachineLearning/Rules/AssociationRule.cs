// TODO: Implement the Apriori algorithm.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.Math;

namespace Accord.MachineLearning.Rules
{
    /// <summary>
    ///     Association rule.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    [Serializable]
    public class AssociationRule<T>
    {
        /// <summary>
        ///     Gets or sets the set of items that triggers the
        ///     activation of this association rule.
        /// </summary>
        public SortedSet<T> X { get; set; }

        /// <summary>
        ///     Gets or sets the set of items that are also likely to
        ///     be included in the original list given that
        ///     <see cref="X">
        ///         the input items
        ///     </see>
        ///     are present.
        /// </summary>
        public SortedSet<T> Y { get; set; }

        /// <summary>
        ///     Gets or sets the number of cases that support this rule
        ///     (the number of times this association has been seen in the
        ///     training set).
        /// </summary>
        public double Support { get; set; }

        /// <summary>
        ///     Gets or sets the confidence of this rule (as a percentage).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        ///     Determines whether this rule can be applied to a given input.
        /// </summary>
        /// <param name="input">The set of elements (being bought, or processed).</param>
        /// <returns>True, if this rule can be applied to the given set of inputs; false othersie.</returns>
        public bool Matches(SortedSet<T> input)
        {
            return X.IsSubsetOf(input);
        }

        /// <summary>
        ///     Determines whether this rule can be applied to a given input.
        /// </summary>
        /// <param name="input">The set of elements (being bought, or processed).</param>
        /// <returns>True, if this rule can be applied to the given set of inputs; false othersie.</returns>
        public bool Matches(T[] input)
        {
            return X.IsSubsetOf(input);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(X.ToArray().ToString(OctaveArrayFormatProvider.InvariantCulture));
            sb.Append(" -> ");
            sb.Append(Y.ToArray().ToString(OctaveArrayFormatProvider.InvariantCulture));
            sb.AppendFormat("; support: {0}, confidence: {1}", Support, Confidence);
            return sb.ToString();
        }
    }
}