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
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics;

namespace Accord.MachineLearning.DecisionTrees.Pruning
{
    /// <summary>
    ///     Error-based pruning.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Lior Rokach, Oded Maimon. The Data Mining and Knowledge Discovery Handbook,
    ///                     Chapter 9, Decision Trees. Springer, 2nd ed. 2010, XX, 1285 p. 40 illus.
    ///                     Available at: http://www.ise.bgu.ac.il/faculty/liorr/hbchap9.pdf .
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Suppose you have the following input and output data
    /// // and would like to learn the relationship between the
    /// // inputs and outputs by using a Decision Tree:
    /// 
    /// double[][] inputs = ...
    /// int[] output = ...
    /// 
    /// // To prune a decision tree, we need to split your data into
    /// // training and pruning groups. Let's say we have 100 samples,
    /// // and would like to reserve 50 samples for training, and 50
    /// // for pruning:
    /// 
    /// // Gather the first half for the training set
    /// var trainingInputs = inputs.Submatrix(0, 49);
    /// var trainingOutput = output.Submatrix(0, 49);
    /// 
    /// // Gather the second hand data for pruning
    /// var pruningInputs = inputs.Submatrix(50, 99);
    /// var pruningOutput = output.Submatrix(50, 99);
    /// 
    /// 
    /// // Create the decision tree
    /// DecisionTree tree = new DecisionTree( ... );
    /// 
    /// // Learn our tree using the training data
    /// C45Learning c45 = new C45Learning(tree);
    /// double error = c45.Run(trainingInputs, trainingOutput);
    /// 
    ///             
    /// // Now we can attempt to prune the tree using the pruning groups
    /// ErrorBasedPruning prune = new ErrorBasedPruning(tree, pruningInputs, pruningOutput);
    /// 
    /// // Gain threshold
    /// prune.Threshold = 0.1;
    /// 
    /// double lastError;
    /// double error = Double.PositiveInfinity;
    /// 
    /// do
    /// {
    ///     // Now we can start pruning the tree as 
    ///     // long as the error doesn't increase
    /// 
    ///     lastError = error;
    ///     error = prune.Run();
    /// 
    /// } while (error &lt; lastError);
    /// </code>
    /// </example>
    public class ErrorBasedPruning
    {
        private readonly double[][] inputs;
        private readonly int[] outputs;

        // stores the index of the samples that 
        // are covered by each node's subtree.
        //
        private readonly Dictionary<DecisionNode, List<int>> subsets;
        private readonly DecisionTree tree;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorBasedPruning" /> class.
        /// </summary>
        /// <param name="tree">The tree to be pruned.</param>
        /// <param name="inputs">The pruning set inputs.</param>
        /// <param name="outputs">The pruning set outputs.</param>
        public ErrorBasedPruning(DecisionTree tree, double[][] inputs, int[] outputs)
        {
            this.tree = tree;
            this.inputs = inputs;
            this.outputs = outputs;
            subsets = new Dictionary<DecisionNode, List<int>>();

            // Create the cache to store how many times a sample
            // passes through each decision node of the tree.
            //
            foreach (var node in tree)
            {
                subsets[node] = new List<int>();
            }

            // Compute the entire pruning set and track the path
            // taken by each observation during the reasoning.
            //
            for (var i = 0; i < inputs.Length; i++)
            {
                trackDecisions(tree.Root, inputs[i], i);
            }
        }

        /// <summary>
        ///     Gets or sets the minimum allowed gain threshold
        ///     to prune the tree. Default is 0.01.
        /// </summary>
        public double Threshold { get; set; } = 0.01;

        /// <summary>
        ///     Computes one pass of the pruning algorithm.
        /// </summary>
        public double Run()
        {
            // Traverse the tree in bottom-up (post-order) order
            foreach (var node in tree.Traverse(DecisionTreeTraversal.PostOrder))
            {
                if (node.IsLeaf)
                {
                    continue;
                }

                if (compute(node))
                {
                    break;
                }
            }

            return computeError();
        }

        private double computeError()
        {
            return new ZeroOneLoss(outputs) { Mean = true }.Loss(tree.Decide(inputs));
        }

        /// <summary>
        ///     Attempts to prune a node's subtrees.
        /// </summary>
        /// <returns>Whether the current node was changed or not.</returns>
        private bool compute(DecisionNode node)
        {
            var indices = subsets[node].ToArray();
            var outputSubset = outputs.Get(indices);

            if (indices.Length == 0)
            {
                // The rule employed by this node doesn't cover
                // any input points. This node could be removed.

                node.Branches = null;
                node.Output = null;

                return true;
            }

            var size = indices.Length;

            var baselineError = computeError();
            baselineError = upperBound(baselineError, size);

            var mostCommon = outputSubset.Mode();
            var pruneError = computeErrorWithoutSubtree(node, mostCommon);
            pruneError = upperBound(pruneError, size);

            var maxChild = getMaxChild(node);
            var replaceError = computeErrorReplacingSubtrees(node, maxChild);
            replaceError = upperBound(replaceError, size);

            var changed = false;
            if (System.Math.Abs(pruneError - baselineError) < Threshold ||
                System.Math.Abs(replaceError - baselineError) < Threshold)
            {
                if (replaceError < pruneError)
                {
                    // We should replace the subtree with its maximum child
                    node.Branches = maxChild.Branches;
                    node.Output = maxChild.Output;
                    foreach (var child in node.Branches)
                    {
                        child.Parent = node;
                    }
                }
                else
                {
                    // We should prune the subtree
                    node.Branches = null;
                    node.Output = mostCommon;
                }

                changed = true;

                foreach (var child in node)
                {
                    subsets[child].Clear();
                }

                var inputSubset = inputs.Get(indices);
                for (var i = 0; i < inputSubset.Length; i++)
                {
                    trackDecisions(node, inputSubset[i], i);
                }
            }

            return changed;
        }

        private static double upperBound(double error, int size)
        {
            return error + 1.96 * System.Math.Sqrt(error * (1 - error) / size);
        }

        private double computeErrorWithoutSubtree(DecisionNode tree, int mostCommon)
        {
            var branches = tree.Branches;
            var output = tree.Output;

            tree.Branches = null;
            tree.Output = mostCommon;

            var error = computeError();

            tree.Branches = branches;
            tree.Output = output;

            return error;
        }

        private double computeErrorReplacingSubtrees(DecisionNode tree, DecisionNode child)
        {
            var branches = tree.Branches;
            var output = tree.Output;

            tree.Branches = child.Branches;
            tree.Output = child.Output;

            var error = computeError();

            tree.Branches = branches;
            tree.Output = output;

            return error;
        }

        private DecisionNode getMaxChild(DecisionNode tree)
        {
            DecisionNode max = null;
            var maxCount = 0;

            foreach (var child in tree.Branches)
            {
                var list = subsets[child];
                if (list.Count > maxCount)
                {
                    max = child;
                    maxCount = list.Count;
                }
            }

            return max;
        }

        private void trackDecisions(DecisionNode root, double[] input, int index)
        {
            var current = root;

            while (current != null)
            {
                subsets[current].Add(index);

                if (current.IsLeaf)
                {
                    return;
                }

                var attribute = current.Branches.AttributeIndex;

                DecisionNode nextNode = null;

                foreach (var branch in current.Branches)
                {
                    if (branch.Compute(input[attribute]))
                    {
                        nextNode = branch;
                        break;
                    }
                }

                current = nextNode;
            }

            // Normal execution should not reach here.
            throw new InvalidOperationException("The tree is degenerated.");
        }
    }
}