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

namespace Accord.Collections
{
    /// <summary>
    ///     Node-distance pair.
    /// </summary>
    /// <typeparam name="TNode">The class type for the nodes of the tree.</typeparam>
    [Serializable]
    public struct NodeDistance<TNode> : IComparable<NodeDistance<TNode>>, IEquatable<NodeDistance<TNode>>
        where TNode : IEquatable<TNode>
    {
        private TNode node;

        /// <summary>
        ///     Gets the node in this pair.
        /// </summary>
        public TNode Node => node;

        /// <summary>
        ///     Gets the distance of the node from the query point.
        /// </summary>
        public double Distance { get; }

        /// <summary>
        ///     Creates a new <see cref="NodeDistance&lt;T&gt;" />.
        /// </summary>
        /// <param name="node">The node value.</param>
        /// <param name="distance">The distance value.</param>
        public NodeDistance(TNode node, double distance)
        {
            this.node = node;
            this.Distance = distance;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="System.Object" />
        ///     is equal to this instance.
        /// </summary>
        /// <param name="obj">
        ///     The <see cref="System.Object" /> to compare
        ///     with this instance.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="System.Object" /> is
        ///     equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is NodeDistance<TNode>)
            {
                var b = (NodeDistance<TNode>)obj;
                return node.Equals(b.node) && Distance == b.Distance;
            }

            return false;
        }

        /// <summary>
        ///     Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///     A hash code for this instance, suitable for use in hashing
        ///     algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 23 + node.GetHashCode();
            hash = hash * 23 + Distance.GetHashCode();
            return hash;
        }

        /// <summary>
        ///     Implements the equality operator.
        /// </summary>
        public static bool operator ==(NodeDistance<TNode> a, NodeDistance<TNode> b)
        {
            return a.node.Equals(b.node) && a.Distance == b.Distance;
        }

        /// <summary>
        ///     Implements the inequality operator.
        /// </summary>
        public static bool operator !=(NodeDistance<TNode> a, NodeDistance<TNode> b)
        {
            return !a.node.Equals(b.node) || a.Distance != b.Distance;
        }

        /// <summary>
        ///     Implements the lesser than operator.
        /// </summary>
        public static bool operator <(NodeDistance<TNode> a, NodeDistance<TNode> b)
        {
            return a.Distance < b.Distance;
        }

        /// <summary>
        ///     Implements the greater than operator.
        /// </summary>
        public static bool operator >(NodeDistance<TNode> a, NodeDistance<TNode> b)
        {
            return a.Distance > b.Distance;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="NodeDistance{T}" />
        ///     is equal to this instance.
        /// </summary>
        /// <param name="other">
        ///     The <see cref="NodeDistance{T}" /> to compare
        ///     with this instance.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="NodeDistance{T}" /> is
        ///     equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(NodeDistance<TNode> other)
        {
            return Distance == other.Distance && node.Equals(other.node);
        }

        /// <summary>
        ///     Compares this instance to another node, returning an integer
        ///     indicating whether this instance has a distance that is less
        ///     than, equal to, or greater than the other node's distance.
        /// </summary>
        public int CompareTo(NodeDistance<TNode> other)
        {
            return Distance.CompareTo(other.Distance);
        }

        /// <summary>
        ///     Compares this instance to another node, returning an integer
        ///     indicating whether this instance has a distance that is less
        ///     than, equal to, or greater than the other node's distance.
        /// </summary>
        public int CompareTo(object obj)
        {
            return CompareTo((NodeDistance<TNode>)obj);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("<{0}, {1}>", node, Distance);
        }
    }
}