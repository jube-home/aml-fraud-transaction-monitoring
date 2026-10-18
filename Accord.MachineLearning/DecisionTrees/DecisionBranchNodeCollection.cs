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
using System.Collections.ObjectModel;
using System.Linq;

namespace Accord.MachineLearning.DecisionTrees
{
    /// <summary>
    ///     Collection of decision nodes. A decision branch specifies the index of
    ///     an attribute whose current value should be compared against its children
    ///     nodes. The type of the comparison is specified in each child node.
    /// </summary>
    [Serializable]
    public class DecisionBranchNodeCollection : Collection<DecisionNode>
    {
        [NonSerialized] private DecisionNode owner;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DecisionBranchNodeCollection" /> class.
        /// </summary>
        [Obsolete("Please specify an owner instead.")]
        public DecisionBranchNodeCollection()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DecisionBranchNodeCollection" /> class.
        /// </summary>
        /// <param name="owner">
        ///     The <see cref="DecisionNode" /> to whom
        ///     this <see cref="DecisionBranchNodeCollection" /> belongs.
        /// </param>
        public DecisionBranchNodeCollection(DecisionNode owner)
        {
            this.owner = owner;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DecisionBranchNodeCollection" /> class.
        /// </summary>
        /// <param name="attributeIndex">Index of the attribute to be processed.</param>
        /// <param name="nodes">
        ///     The children nodes. Each child node should be
        ///     responsible for a possible value of a discrete attribute, or for
        ///     a region of a continuous-valued attribute.
        /// </param>
        [Obsolete("Please specify an owner instead.")]
        public DecisionBranchNodeCollection(int attributeIndex, DecisionNode[] nodes)
            : base(nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException("nodes");
            }

            if (nodes.Length == 0)
            {
                throw new ArgumentException("Node collection is empty.", "nodes");
            }

            AttributeIndex = attributeIndex;
            owner = nodes[0].Parent;
        }

        /// <summary>
        ///     Gets or sets the index of the attribute to be
        ///     used in this stage of the decision process.
        /// </summary>
        public int AttributeIndex { get; set; }

        /// <summary>
        ///     Gets the attribute that is being used in
        ///     this stage of the decision process, given
        ///     by the current <see cref="AttributeIndex" />
        /// </summary>
        public DecisionVariable Attribute
        {
            get
            {
                // TODO: Remove the obsolete attribute and make owner mandatory.
                if (owner == null)
                {
                    return null;
                }

                return owner.Owner.Attributes[AttributeIndex];
            }
        }

        /// <summary>
        ///     Gets or sets the decision node that contains this collection.
        /// </summary>
        public DecisionNode Owner
        {
            get => owner;
            set => owner = value;
        }

        /// <summary>
        ///     Adds the elements of the specified collection to the end of the collection.
        /// </summary>
        /// <param name="children">The child nodes to be added.</param>
        public void AddRange(IEnumerable<DecisionNode> children)
        {
            foreach (var node in children)
            {
                Add(node);
            }
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            var values = string.Join(", ", this.Select(x => x.ToString()).ToArray());
            return string.Format("{0} ({1}) => {2}", Attribute, AttributeIndex, values);
        }
    }
}