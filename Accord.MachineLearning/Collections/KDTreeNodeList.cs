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

namespace Accord.Collections
{
    /// <summary>
    ///     List of k-dimensional tree nodes.
    /// </summary>
    /// <typeparam name="T">The type of the value being stored.</typeparam>
    /// <remarks>
    ///     This class is used to store neighbor nodes when running one of the
    ///     search algorithms for <see cref="KDTree{T}">k-dimensional trees</see>.
    /// </remarks>
    /// <seealso cref="KDTree{T}" />
    /// <seealso cref="NodeDistance{T}" />
    [Serializable]
    public class KDTreeNodeList<T> : List<NodeDistance<T>>
        where T : IComparable<T>, IEquatable<T>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KDTreeNodeList&lt;T&gt;" />
        ///     class that is empty.
        /// </summary>
        public KDTreeNodeList()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KDTreeNodeList&lt;T&gt;" />
        ///     class that is empty and has the specified capacity.
        /// </summary>
        public KDTreeNodeList(int capacity)
            : base(capacity)
        {
        }
    }
}