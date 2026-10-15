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
    ///     Tree node with arbitrary number of children.
    /// </summary>
    /// <typeparam name="TNode">The class type for the nodes of the tree.</typeparam>
    [Serializable]
    public class TreeNode<TNode> : ITreeNode<TNode>
        where TNode : TreeNode<TNode>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TreeNode{TNode}" /> class.
        /// </summary>
        /// <param name="index">The index of this node in the children collection of its parent node.</param>
        public TreeNode(int index)
        {
            Index = index;
        }

        /// <summary>
        ///     Gets or sets the parent of this node.
        /// </summary>
        public TNode Parent { get; set; }

        /// <summary>
        ///     Gets or sets the index of this node in the
        ///     collection of children nodes of its parent.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        ///     Gets the next sibling of this node (the node
        ///     immediately next to it in its parent's collection).
        /// </summary>
        public TNode Next
        {
            get
            {
                if (Parent == null)
                {
                    return null;
                }

                if (Index + 1 >= Parent.Children.Length)
                {
                    return null;
                }

                return Parent.Children[Index + 1];
            }
        }

        /// <summary>
        ///     Gets the previous sibling of this node.
        /// </summary>
        public TNode Previous
        {
            get
            {
                if (Index == 0)
                {
                    return null;
                }

                return Parent.Children[Index - 1];
            }
        }

        /// <summary>
        ///     Gets or sets the collection of child nodes
        ///     under this node.
        /// </summary>
        public TNode[] Children { get; set; }

        /// <summary>
        ///     Gets whether this node is a leaf (has no children).
        /// </summary>
        public bool IsLeaf => Children == null || Children.Length == 0;
    }
}