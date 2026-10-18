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
using System.Threading.Tasks;
using Accord.Math;
using Accord.Statistics;

namespace Accord.MachineLearning.DecisionTrees.Learning
{
    public partial class C45Learning
    {
        private void computeGainRatios(double[][] inputs, int[] outputs, double[] weights, double entropy,
            int[] candidates, double[] scores, double[] thresholds, List<int>[][] partitions)
        {
            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                // For each attribute in the data set
                for (var i = 0; i < scores.Length; i++)
                {
                    scores[i] = computeGainRatio(inputs, outputs, weights, candidates[i],
                        entropy, out partitions[i], out thresholds[i]);
                }
            }
            else
            {
                // For each attribute in the data set
                Parallel.For(0, scores.Length, ParallelOptions, i =>
                {
                    scores[i] = computeGainRatio(inputs, outputs, weights, candidates[i],
                        entropy, out partitions[i], out thresholds[i]);
                });
            }
        }

        private void splitDiscrete(DecisionNode root, double[][] inputs, int[] outputs, double[] weights, int height,
            int maxGainAttribute, List<int>[] maxGainPartition, IntRange maxGainRange)
        {
            double[][] inputSubset;
            int[] outputSubset;
            double[] weightSubset;

            // This is a discrete nature attribute. We will branch at each
            // possible value for the discrete variable and call recursion.
            var children = new DecisionNode[maxGainPartition.Length];

            // Create a branch for each possible value
            for (var i = 0; i < children.Length; i++)
            {
                children[i] = new DecisionNode(Model)
                {
                    Parent = root,
                    Value = i + maxGainRange.Min,
                    Comparison = ComparisonKind.Equal
                };

                inputSubset = inputs.Get(maxGainPartition[i]);
                outputSubset = outputs.Get(maxGainPartition[i]);
                weightSubset = weights.Get(maxGainPartition[i]);

                if (outputSubset.Length == 0)
                {
                    //in this case the we have no samples for this category
                    //but we still want to be able to make a decision, so we will give the best of the current node as output
                    outputSubset = new[] { outputs.WeightedMode(weights) };
                    weightSubset = new double[] { 1 }; //does not matter
                }

                split(children[i], inputSubset, outputSubset, weightSubset, height + 1); // recursion
            }

            root.Branches.AttributeIndex = maxGainAttribute;
            root.Branches.AddRange(children);
        }

        private void splitContinuous(DecisionNode root, double[][] inputs, int[] outputs, double[] weights, int height,
            int maxGainAttribute, List<int>[] maxGainPartition, double maxGainThreshold)
        {
            double[][] inputSubset;
            int[] outputSubset;
            double[] weightSubset;

            var partitionBelowThreshold = maxGainPartition[0];
            var partitionAboveThreshold = maxGainPartition[1];


            if (partitionBelowThreshold != null && partitionAboveThreshold != null)
            {
                //Before we branch we test whether each node is big enough, we stop here and set it as a leaf node
                if (partitionAboveThreshold.Count < minimumLeafSize || partitionBelowThreshold.Count < minimumLeafSize)
                {
                    root.Output = outputs.WeightedMode(weights);
                }
                else
                {
                    // This is a continuous nature attribute, and we achieved two partitions
                    // using the partitioning scheme. We will branch on two possible settings:
                    // either the value is greater than a currently detected optimal threshold 
                    // or it is less.

                    DecisionNode[] children =
                    {
                        new(Model)
                        {
                            Parent = root, Value = maxGainThreshold,
                            Comparison = ComparisonKind.LessThanOrEqual
                        },

                        new(Model)
                        {
                            Parent = root, Value = maxGainThreshold,
                            Comparison = ComparisonKind.GreaterThan
                        }
                    };

                    // Create a branch for lower values
                    inputSubset = inputs.Get(partitionBelowThreshold);
                    outputSubset = outputs.Get(partitionBelowThreshold);
                    weightSubset = weights.Get(partitionBelowThreshold);
                    split(children[0], inputSubset, outputSubset, weightSubset, height + 1);

                    // Create a branch for higher values
                    inputSubset = inputs.Get(partitionAboveThreshold);
                    outputSubset = outputs.Get(partitionAboveThreshold);
                    weightSubset = weights.Get(partitionAboveThreshold);
                    split(children[1], inputSubset, outputSubset, weightSubset, height + 1);

                    root.Branches.AttributeIndex = maxGainAttribute;
                    root.Branches.AddRange(children);
                }
            }
            else
            {
                // This is a continuous nature attribute, but all variables are equal
                // to a constant. If there is only a constant value as the predictor 
                // and there are multiple output labels associated with this constant
                // value, there isn't much we can do. This node will be a leaf.

                // We will set the class label for this node as the
                // majority of the currently selected output classes.

                var outputIndices = partitionBelowThreshold ?? partitionAboveThreshold;
                outputSubset = outputs.Get(outputIndices);
                root.Output = outputSubset.Mode();
            }
        }
    }
}