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
using Accord.Statistics;

namespace Accord.MachineLearning.DecisionTrees.Pruning
{
    /// <summary>
    ///     Reduced error pruning.
    /// </summary>
    public class ReducedErrorPruning
    {
        private readonly int[] actual;

        private readonly Dictionary<DecisionNode, NodeInfo> info;

        private readonly double[][] inputs;
        private readonly int[] outputs;
        private readonly DecisionTree tree;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReducedErrorPruning" /> class.
        /// </summary>
        /// <param name="tree">The tree to be pruned.</param>
        /// <param name="inputs">The pruning set inputs.</param>
        /// <param name="outputs">The pruning set outputs.</param>
        public ReducedErrorPruning(DecisionTree tree, double[][] inputs, int[] outputs)
        {
            this.tree = tree;
            this.inputs = inputs;
            this.outputs = outputs;
            info = new Dictionary<DecisionNode, NodeInfo>();
            actual = new int[outputs.Length];

            foreach (var node in tree)
            {
                info[node] = new NodeInfo();
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                trackDecisions(tree.Root, inputs[i], i);
            }
        }

        /// <summary>
        ///     Computes one pass of the pruning algorithm.
        /// </summary>
        public double Run()
        {
            // Compute misclassifications at each node
            foreach (var node in tree)
            {
                info[node].error = computeError(node);
            }

            // Compute the gain at each node
            foreach (var node in tree)
            {
                info[node].gain = computeGain(node);
            }

            // Get maximum violating node
            var maxGain = double.NegativeInfinity;
            DecisionNode maxNode = null;
            foreach (var node in tree)
            {
                var gain = info[node].gain;

                if (gain > maxGain)
                {
                    maxGain = gain;
                    maxNode = node;
                }
            }

            if (maxGain >= 0 && maxNode != null)
            {
                var o = outputs.Get(info[maxNode].subset.ToArray());

                // prune the maximum gain node
                var common = o.Mode();

                maxNode.Branches = null;
                maxNode.Output = common;
            }

            return computeError();
        }

        private double computeError(DecisionNode node)
        {
            var indices = info[node].subset;

            var error = 0;
            foreach (var i in indices)
            {
                if (outputs[i] != actual[i])
                {
                    error++;
                }
            }

            return error / (double)indices.Count;
        }

        private double computeGain(DecisionNode node)
        {
            if (node.IsLeaf)
            {
                return double.NegativeInfinity;
            }

            // Compute the sum of misclassifications at the children
            double sum = 0;
            foreach (var child in node.Branches)
            {
                sum += info[child].error;
            }

            // Get the misclassifications at the current node
            var current = info[node].error;

            // Compute the expected gain at the current node:
            return sum - current;
        }

        private double computeError()
        {
            var error = 0;
            for (var i = 0; i < inputs.Length; i++)
            {
                var actual = tree.Decide(inputs[i]);
                var expected = outputs[i];
                if (actual != expected)
                {
                    error++;
                }
            }

            return error / (double)inputs.Length;
        }

        private void trackDecisions(DecisionNode root, double[] input, int index)
        {
            var current = root;

            while (current != null)
            {
                info[current].subset.Add(index);

                if (current.IsLeaf)
                {
                    actual[index] = current.Output.HasValue ? current.Output.Value : -1;
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

        private class NodeInfo
        {
            public readonly List<int> subset;
            public double error;
            public double gain;

            public NodeInfo()
            {
                subset = new List<int>();
            }
        }
    }
}