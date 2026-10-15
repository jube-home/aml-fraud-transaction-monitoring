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
    ///     Tree node for binary trees.
    /// </summary>
    /// <typeparam name="TNode">The class type for the nodes of the tree.</typeparam>
    [Serializable]
    public class BinaryNode<TNode> : IEquatable<TNode>, ITreeNode<TNode> // TODO: Try to remove IEquatable
        where TNode : BinaryNode<TNode>
    {
        /// <summary>
        ///     Gets or sets the left subtree of this node.
        /// </summary>
        public TNode Left { get; set; }

        /// <summary>
        ///     Gets or sets the right subtree of this node.
        /// </summary>
        public TNode Right { get; set; }

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///     true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(TNode other)
        {
            return this == other;
        }

        /// <summary>
        ///     Gets whether this node is a leaf (has no children).
        /// </summary>
        public bool IsLeaf => Left == default(TNode) && Right == default(TNode);

        /// <summary>
        ///     Gets or sets the collection of child nodes
        ///     under this node.
        /// </summary>
        public TNode[] Children
        {
            get { return new[] { Left, Right }; }
            set
            {
                if (value.Length != 2)
                {
                    throw new ArgumentException("The array must have length 2.", "value");
                }

                Left = value[0];
                Right = value[1];
            }
        }
    }
}