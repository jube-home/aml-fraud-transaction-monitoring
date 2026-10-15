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
using System.Collections.Generic;

namespace Accord.Collections
{
    /// <summary>
    ///     Base class for tree structures.
    /// </summary>
    /// <typeparam name="TNode">The class type for the nodes of the tree.</typeparam>
    [Serializable]
    public class Tree<TNode> : IEnumerable<TNode>
        where TNode : TreeNode<TNode>
    {
        /// <summary>
        ///     Gets the root node of this tree.
        /// </summary>
        public TNode Root { get; protected set; }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<TNode> GetEnumerator()
        {
            foreach (var node in Traverse(TreeTraversal.DepthFirst))
            {
                yield return node;
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the tree.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Traverse the tree using a
        ///     <see cref="TreeTraversal">
        ///         tree traversal
        ///         method
        ///     </see>
        ///     . Can be iterated with a foreach loop.
        /// </summary>
        /// <param name="method">
        ///     The tree traversal method. Common methods are
        ///     available in the <see cref="TreeTraversal" />static class.
        /// </param>
        /// <returns>
        ///     An <see cref="IEnumerable{T}" /> object which can be used to
        ///     traverse the tree using the chosen traversal method.
        /// </returns>
        public IEnumerable<TNode> Traverse(TraversalMethod<TNode> method)
        {
            return new InnerTreeTraversal(this, method);
        }

        private class InnerTreeTraversal : IEnumerable<TNode>
        {
            private readonly TraversalMethod<TNode> method;
            private readonly Tree<TNode> tree;

            public InnerTreeTraversal(Tree<TNode> tree, TraversalMethod<TNode> method)
            {
                this.tree = tree;
                this.method = method;
            }

            public IEnumerator<TNode> GetEnumerator()
            {
                return method(tree);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return method(tree);
            }
        }
    }
}