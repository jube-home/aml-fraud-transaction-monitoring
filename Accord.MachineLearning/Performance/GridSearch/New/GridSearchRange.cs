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

namespace Accord.MachineLearning.Performance
{
    /// <summary>
    ///     Range of parameters to be tested in a grid search.
    /// </summary>
    public class GridSearchRange<T> : IGridSearchRange
    {
        /// <summary>
        ///     Gets or sets the range of values that should be tested for this parameter.
        /// </summary>
        public T[] Values { get; set; }

        /// <summary>
        ///     Gets the current value being considered during the grid-search.
        /// </summary>
        public T Value => Values[Index];

        /// <summary>
        ///     Gets or sets the index of the current value in the search,
        ///     whose value will be shown in the <see cref="Value" /> property.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        ///     Gets the number of values that this parameter can assume (the length of the parameter range).
        /// </summary>
        public int Length => Values.Length;

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="GridSearchRange{T}" /> to
        ///     <typeparam ref="T" />
        ///     .
        /// </summary>
        /// <param name="range">The range to be converted.</param>
        /// <returns>The value of the parameter's <see cref="GridSearchRange{T}.Value" />.</returns>
        public static implicit operator T(GridSearchRange<T> range)
        {
            return range.Value;
        }
    }
}