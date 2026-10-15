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
    ///     Tree enumeration method delegate.
    /// </summary>
    /// <typeparam name="TNode">The class type for the nodes of the tree.</typeparam>
    /// <param name="tree">The binary tree to be traversed.</param>
    /// <returns>An enumerator traversing the tree.</returns>
    public delegate IEnumerator<TNode> BinaryTraversalMethod<TNode>(BinaryTree<TNode> tree)
        where TNode : BinaryNode<TNode>;

    /// <summary>
    ///     Tree enumeration method delegate.
    /// </summary>
    /// <typeparam name="TNode">The class type for the nodes of the tree.</typeparam>
    /// <returns>An enumerator traversing the tree.</returns>
    public delegate IEnumerator<TNode> TraversalMethod<TNode>(Tree<TNode> tree)
        where TNode : TreeNode<TNode>;

    /// <summary>
    ///     Static class with tree traversal methods.
    /// </summary>
    public static class TreeTraversal
    {
        /// <summary>
        ///     Breadth-first tree traversal method.
        /// </summary>
        public static IEnumerator<TNode> BreadthFirst<TNode>(BinaryTree<TNode> tree)
            where TNode : BinaryNode<TNode>, new()
        {
            if (tree.Root == null)
            {
                yield break;
            }

            var queue = new Queue<TNode>(new[] { tree.Root });

            while (queue.Count != 0)
            {
                var current = queue.Dequeue();

                if (current != null)
                {
                    yield return current;
                }

                if (current.Left != null)
                {
                    queue.Enqueue(current.Left);
                }

                if (current.Right != null)
                {
                    queue.Enqueue(current.Right);
                }
            }
        }

        /// <summary>
        ///     Pre-order tree traversal method.
        /// </summary>
        public static IEnumerator<TNode> PreOrder<TNode>(BinaryTree<TNode> tree)
            where TNode : BinaryNode<TNode>, new()
        {
            if (tree.Root == null)
            {
                yield break;
            }

            var stack = new Stack<TNode>();

            var current = tree.Root;

            while (stack.Count != 0 || current != null)
            {
                if (current != null)
                {
                    stack.Push(current);
                    yield return current;
                    current = current.Left;
                }
                else
                {
                    current = stack.Pop();
                    current = current.Right;
                }
            }
        }

        /// <summary>
        ///     In-order tree traversal method.
        /// </summary>
        public static IEnumerator<TNode> InOrder<TNode>(BinaryTree<TNode> tree)
            where TNode : BinaryNode<TNode>, new()
        {
            if (tree.Root == null)
            {
                yield break;
            }

            var stack = new Stack<TNode>();

            var current = tree.Root;

            while (stack.Count != 0 || current != null)
            {
                if (current != null)
                {
                    stack.Push(current);
                    current = current.Left;
                }
                else
                {
                    current = stack.Pop();
                    yield return current;
                    current = current.Right;
                }
            }
        }

        /// <summary>
        ///     Post-order tree traversal method.
        /// </summary>
        public static IEnumerator<TNode> PostOrder<TNode>(BinaryTree<TNode> tree)
            where TNode : BinaryNode<TNode>, new()
        {
            if (tree.Root == null)
            {
                yield break;
            }

            var stack = new Stack<TNode>(new[] { tree.Root });

            var previous = tree.Root;

            while (stack.Count != 0)
            {
                var current = stack.Peek();

                if (previous == current || previous.Left == current || previous.Right == current)
                {
                    if (current.Left != null)
                    {
                        stack.Push(current.Left);
                    }
                    else if (current.Right != null)
                    {
                        stack.Push(current.Right);
                    }
                    else
                    {
                        yield return stack.Pop();
                    }
                }
                else if (current.Left == previous)
                {
                    if (current.Right != null)
                    {
                        stack.Push(current.Right);
                    }
                    else
                    {
                        yield return stack.Pop();
                    }
                }
                else if (current.Right == previous)
                {
                    yield return stack.Pop();
                }
                else
                {
                    throw new InvalidOperationException();
                }

                previous = current;
            }
        }

        /// <summary>
        ///     Depth-first tree traversal method.
        /// </summary>
        public static IEnumerator<TNode> DepthFirst<TNode>(BinaryTree<TNode> tree)
            where TNode : BinaryNode<TNode>
        {
            if (tree.Root == null)
            {
                yield break;
            }

            var stack = new Stack<TNode>();
            stack.Push(tree.Root);

            while (stack.Count != 0)
            {
                var node = stack.Pop();

                if (node != null)
                {
                    yield return node;
                }

                if (node.Left != null)
                {
                    stack.Push(node.Left);
                }

                if (node.Right != null)
                {
                    stack.Push(node.Right);
                }
            }
        }

        /// <summary>
        ///     Depth-first tree traversal method.
        /// </summary>
        public static IEnumerator<TNode> DepthFirst<TNode>(Tree<TNode> tree)
            where TNode : TreeNode<TNode>
        {
            if (tree.Root == null)
            {
                yield break;
            }

            var node = tree.Root;

            while (node != null)
            {
                yield return node;

                if (node.IsLeaf)
                {
                    while (node.Next == null && node.Parent != null)
                    {
                        node = node.Parent;
                    }

                    node = node.Next;
                }
                else
                {
                    node = node.Children[0];
                }
            }
        }
    }
}