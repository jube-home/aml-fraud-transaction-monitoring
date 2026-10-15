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
    ///     Base class for binary trees. This class does not implement a binary search
    ///     tree, but can used to implement one. For binary search trees, please refer
    ///     to <see cref="RedBlackTree{T}" />, KDTree and VPTree.
    /// </summary>
    /// <example>
    ///     <para>
    ///         The <see cref="BinaryTree{TNode}" /> class is a base class for other tree classes
    ///         such as <see cref="RedBlackTree{T}" />, KDTree and VPTree. For examples on how to
    ///         use those classes, please see their respective documentation pages.
    ///     </para>
    ///     <para>
    ///         If you would like to implement your own binary tree that inherits from this class,
    ///         then you can do so as shown in the following example. First, make sure your custom
    ///         node class inherits from <see cref="BinaryNode{T}" /> and passes itself as the generic
    ///         argument of <see cref="BinaryNode{T}" />:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Core\BinaryTreeTest.cs" region="doc_ctor_1" />
    ///     <para>
    ///         Now, once the tree node has been implemented, we can create a new <see cref="BinaryTree{TNode}" />
    ///         and explore the tree in different ways as shown below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Core\BinaryTreeTest.cs" region="doc_ctor_2" />
    /// </example>
    /// <typeparam name="TNode">The class type for the nodes of the tree.</typeparam>
    [Serializable]
    public class BinaryTree<TNode> : IEnumerable<TNode>
        where TNode : BinaryNode<TNode>
    {
        /// <summary>
        ///     Gets the root node of this tree.
        /// </summary>
        public TNode Root { get; set; }

        /// <summary>
        ///     Returns an enumerator that iterates through the tree.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object
        ///     that can be used to iterate through the collection.
        /// </returns>
        public virtual IEnumerator<TNode> GetEnumerator()
        {
            if (Root == null)
            {
                yield break;
            }

            var stack = new Stack<TNode>(new[] { Root });

            while (stack.Count != 0)
            {
                var current = stack.Pop();

                yield return current;

                if (current.Left != null)
                {
                    stack.Push(current.Left);
                }

                if (current.Right != null)
                {
                    stack.Push(current.Right);
                }
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
        public IEnumerable<TNode> Traverse(BinaryTraversalMethod<TNode> method)
        {
            return new BinaryTreeTraversal(this, method);
        }

        private class BinaryTreeTraversal : IEnumerable<TNode>
        {
            private readonly BinaryTraversalMethod<TNode> method;
            private readonly BinaryTree<TNode> tree;

            public BinaryTreeTraversal(BinaryTree<TNode> tree, BinaryTraversalMethod<TNode> method)
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