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

using System.Collections.Generic;
using System.Linq;

namespace Accord.MachineLearning.DecisionTrees
{
    /// <summary>
    ///     Tree enumeration method delegate.
    /// </summary>
    /// <returns>An enumerator traversing the tree.</returns>
    public delegate IEnumerator<DecisionNode> DecisionTreeTraversalMethod(DecisionNode node);

    /// <summary>
    ///     Common traversal methods for n-ary trees.
    /// </summary>
    public static class DecisionTreeTraversal
    {
        /// <summary>
        ///     Breadth-first traversal method.
        /// </summary>
        public static IEnumerator<DecisionNode> BreadthFirst(DecisionNode tree)
        {
            if (tree == null)
            {
                yield break;
            }

            var queue = new Queue<DecisionNode>(new[] { tree });

            while (queue.Count != 0)
            {
                var current = queue.Dequeue();

                yield return current;

                if (current.Branches != null)
                {
                    foreach (var child in current.Branches)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }

        /// <summary>
        ///     Depth-first traversal method.
        /// </summary>
        public static IEnumerator<DecisionNode> DepthFirst(DecisionNode tree)
        {
            if (tree == null)
            {
                yield break;
            }

            var stack = new Stack<DecisionNode>(new[] { tree });

            while (stack.Count != 0)
            {
                var current = stack.Pop();

                yield return current;

                if (current.Branches != null)
                {
                    for (var i = current.Branches.Count - 1; i >= 0; i--)
                    {
                        stack.Push(current.Branches[i]);
                    }
                }
            }
        }

        internal static DecisionNode GetNextSibling(Dictionary<DecisionNode, int> cursors, DecisionNode node)
        {
            var parent = node.Parent;

            if (parent == null)
            {
                return null;
            }

            // Get current node index
            int index;
            if (!cursors.TryGetValue(node, out index))
            {
                cursors[node] = index = 0;
            }

            var nextIndex = index + 1;
            if (nextIndex < parent.Branches.Count)
            {
                var sibling = parent.Branches[nextIndex];
                cursors[sibling] = nextIndex;
                return sibling;
            }

            return null;
        }

        /// <summary>
        ///     Post-order tree traversal method.
        /// </summary>
        /// <remarks>
        ///     Adapted from John Cowan (1998) recommendation.
        /// </remarks>
        public static IEnumerator<DecisionNode> PostOrder(DecisionNode tree)
        {
            var cursor = new Dictionary<DecisionNode, int>();

            var currentNode = tree;

            while (currentNode != null)
            {
                // Move down to first child
                DecisionNode nextNode = null;
                if (currentNode.Branches != null)
                {
                    nextNode = currentNode.Branches.FirstOrDefault();
                }

                if (nextNode != null)
                {
                    currentNode = nextNode;
                    continue;
                }

                // No child nodes, so walk tree
                while (currentNode != null)
                {
                    yield return currentNode; // post order

                    // Move to sibling if possible.
                    nextNode = GetNextSibling(cursor, currentNode);

                    if (nextNode != null)
                    {
                        currentNode = nextNode;
                        break;
                    }

                    // Move up
                    if (currentNode == nextNode)
                    {
                        currentNode = null;
                    }
                    else
                    {
                        currentNode = currentNode.Parent;
                    }
                }
            }
        }
    }
}