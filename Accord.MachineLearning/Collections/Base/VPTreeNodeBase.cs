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
using System.Collections;
using System.Text;

namespace Accord.Collections
{
#if !MONO
    /// <summary>
    ///     Base class for <see cref="VPTree" /> nodes.
    /// </summary>
    /// <typeparam name="TPoint">The type for the position vector (e.g. double[]).</typeparam>
    /// <typeparam name="TNode">The class type for the nodes of the tree.</typeparam>
    [Serializable]
    public class VPTreeNodeBase<TPoint, TNode> : BinaryNode<TNode>, IEquatable<TNode> // TODO: Try to remove IEquatable
        where TNode : VPTreeNodeBase<TPoint, TNode>
    {
        private TPoint position;
        private double threshold; // radius(?)

        /// <summary>
        ///     Gets or sets the current position for this Vantage-Point Tree Node.
        /// </summary>
        public TPoint Position
        {
            get => position;
            set => position = value;
        }

        /// <summary>
        ///     Gets or sets the threshold radius for this node.
        /// </summary>
        public double Threshold
        {
            get => threshold;
            set => threshold = value;
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
            return ReferenceEquals(Position, other.Position);
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
            sb.Append("[(");
            var list = Position as IList;
            if (list != null)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    sb.Append(list[i]);
                    if (i < list.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
            }
            else
            {
                sb.Append(Position);
            }

            sb.AppendFormat("), {0}]", threshold);
            return sb.ToString();
        }
    }
#endif
}