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

namespace Accord.MachineLearning.DecisionTrees
{
    /// <summary>
    ///     Numeric comparison category.
    /// </summary>
    public enum ComparisonKind
    {
        /// <summary>
        ///     The node does no comparison.
        /// </summary>
        None,

        /// <summary>
        ///     The node compares for equality.
        /// </summary>
        Equal,

        /// <summary>
        ///     The node compares for non-equality.
        /// </summary>
        NotEqual,

        /// <summary>
        ///     The node compares for greater-than or equality.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        ///     The node compares for greater-than.
        /// </summary>
        GreaterThan,

        /// <summary>
        ///     The node compares for less-than.
        /// </summary>
        LessThan,

        /// <summary>
        ///     The node compares for less-than or equality.
        /// </summary>
        LessThanOrEqual
    }

    /// <summary>
    ///     Extension methods for <see cref="ComparisonKind" /> enumeration values.
    /// </summary>
    public static class ComparisonExtensions
    {
        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="comparison">The comparison type.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public static string ToString(this ComparisonKind comparison)
        {
            switch (comparison)
            {
                case ComparisonKind.Equal:
                    return "==";

                case ComparisonKind.GreaterThan:
                    return ">";

                case ComparisonKind.GreaterThanOrEqual:
                    return ">=";

                case ComparisonKind.LessThan:
                    return "<";

                case ComparisonKind.LessThanOrEqual:
                    return "<=";

                case ComparisonKind.NotEqual:
                    return "!=";

                default:
                    throw new InvalidOperationException("Unexpected node comparison type.");
            }
        }
    }
}