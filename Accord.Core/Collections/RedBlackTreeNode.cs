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
    ///     Possible node colors for <see cref="RedBlackTree{T}" />s.
    /// </summary>
    public enum RedBlackTreeNodeType
    {
        /// <summary>
        ///     Red node.
        /// </summary>
        Red,

        /// <summary>
        ///     Black node.
        /// </summary>
        Black
    }

    /// <summary>
    ///     <see cref="RedBlackTree{T}" /> node.
    /// </summary>
    /// <typeparam name="T">The type of the value to be stored.</typeparam>
    [Serializable]
    public class RedBlackTreeNode<T> : BinaryNode<RedBlackTreeNode<T>>
    {
        private RedBlackTreeNodeType color;
        private RedBlackTreeNode<T> parent;

        private T value;

        /// <summary>
        ///     Constructs a new empty node.
        /// </summary>
        public RedBlackTreeNode()
        {
        }

        /// <summary>
        ///     Constructs a node containing the given
        ///     <param name="value" />
        ///     .
        /// </summary>
        public RedBlackTreeNode(T value)
        {
            this.value = value;
        }

        /// <summary>
        ///     Gets or sets a reference to this node's parent node.
        /// </summary>
        public RedBlackTreeNode<T> Parent
        {
            get => parent;
            set => parent = value;
        }

        /// <summary>
        ///     Gets or sets this node's color.
        /// </summary>
        public RedBlackTreeNodeType Color
        {
            get => color;
            set => color = value;
        }

        /// <summary>
        ///     Gets or sets the value associated with this node.
        /// </summary>
        public T Value
        {
            get => value;
            set => this.value = value;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (color == RedBlackTreeNodeType.Black)
            {
                return "Black: {0)".Format(Value);
            }

            return "Red: {0)".Format(Value);
        }
    }

    /// <summary>
    ///     <see cref="RedBlackTree{T}" /> node.
    /// </summary>
    /// <typeparam name="TKey">The type of the key that identifies the value.</typeparam>
    /// <typeparam name="TValue">The type of the values stored in this node.</typeparam>
    [Serializable]
    public class RedBlackTreeNode<TKey, TValue> : RedBlackTreeNode<KeyValuePair<TKey, TValue>>
    {
        /// <summary>
        ///     Constructs a new empty node.
        /// </summary>
        public RedBlackTreeNode()
        {
        }

        /// <summary>
        ///     Constructs a new node containing the given
        ///     <param name="key">
        ///         key
        ///     </param>
        ///     and
        ///     <param name="value">value</param>
        ///     pair.
        /// </summary>
        public RedBlackTreeNode(TKey key, TValue value)
            : base(new KeyValuePair<TKey, TValue>(key, value))
        {
        }

        /// <summary>
        ///     Constructs a new node containing the given
        ///     <param name="item">key and value pair</param>
        ///     .
        /// </summary>
        public RedBlackTreeNode(KeyValuePair<TKey, TValue> item)
            : base(item)
        {
        }
    }
}