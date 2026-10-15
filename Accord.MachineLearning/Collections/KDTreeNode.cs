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
using System.Text;

namespace Accord.Collections
{
    /// <summary>
    ///     K-dimensional tree node (for <see cref="KDTree" />).
    /// </summary>
    /// <seealso cref="SPTreeNode" />
    /// <seealso cref="VPTreeNode{TPoint}" />
    [Serializable]
    public class KDTreeNode : KDTreeNodeBase<KDTreeNode>
    {
    }

    /// <summary>
    ///     K-dimensional tree node (for <see cref="KDTree{T}" />).
    /// </summary>
    /// <seealso cref="SPTreeNode" />
    /// <seealso cref="VPTreeNode{TPoint}" />
    [Serializable]
    public class KDTreeNode<T> : KDTreeNodeBase<KDTreeNode<T>>
    {
        /// <summary>
        ///     Gets or sets the value being stored at this node.
        /// </summary>
        public T Value { get; set; }
    }

    /// <summary>
    ///     Base class for K-dimensional tree nodes.
    /// </summary>
    /// <typeparam name="TNode">The class type for the nodes of the tree.</typeparam>
    /// <seealso cref="KDTreeNode" />
    /// <seealso cref="KDTreeNode{T}" />
    /// <seealso cref="BinaryNode{TNode}" />
    [Serializable]
    public class KDTreeNodeBase<TNode> : BinaryNode<TNode>,
        IComparable<TNode>, IEquatable<TNode> // TODO: Try to remove IEquatable
        where TNode : KDTreeNodeBase<TNode>
    {
        /// <summary>
        ///     Gets or sets the position of
        ///     the node in spatial coordinates.
        /// </summary>
        public double[] Position { get; set; }

        /// <summary>
        ///     Gets or sets the dimension index of the split. This value is a
        ///     index of the <see cref="Position" /> vector and as such should
        ///     be higher than zero and less than the number of elements in <see cref="Position" />.
        /// </summary>
        public int Axis { get; set; }

        /// <summary>
        ///     Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///     A value that indicates the relative order of the objects being compared. The return value has the following
        ///     meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This
        ///     object is equal to <paramref name="other" />. Greater than zero This object is greater than
        ///     <paramref name="other" />.
        /// </returns>
        public int CompareTo(TNode other)
        {
            return Position[Axis].CompareTo(other.Position[other.Axis]);
        }

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///     true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public new bool Equals(TNode other) // TODO: Try to remove IEquatable
        {
            return this == other;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (Position == null)
            {
                return "(null)";
            }

            var sb = new StringBuilder();
            sb.Append("(");
            for (var i = 0; i < Position.Length; i++)
            {
                sb.Append(Position[i]);
                if (i < Position.Length - 1)
                {
                    sb.Append(",");
                }
            }

            sb.Append(")");

            return sb.ToString();
        }
    }
}