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
using System.Threading.Tasks;
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics;
using Accord.Statistics.Filters;

namespace Accord.MachineLearning.DecisionTrees.Learning
{
    /// <summary>
    ///     ID3 (Iterative Dichotomizer 3) learning algorithm
    ///     for <see cref="DecisionTree">Decision Trees</see>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Quinlan, J. R 1986. Induction of Decision Trees.
    ///                     Mach. Learn. 1, 1 (Mar. 1986), 81-106.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Mitchell, T. M. Machine Learning. McGraw-Hill, 1997. pp. 55-58.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/ID3_algorithm">
    ///                         Wikipedia, the free encyclopedia. ID3 algorithm. Available on
    ///                         http://en.wikipedia.org/wiki/ID3_algorithm
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This example shows the simplest way to induce a decision tree with discrete variables.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\ID3LearningTest.cs" region="doc_learn_simplest" />
    ///     <para>
    ///         This example shows a common textbook example, and how to induce a decision tree using a
    ///         <see cref="Codification">codebook</see> to convert string (text) variables into discrete symbols.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\ID3LearningTest.cs" region="doc_learn_mitchell" />
    /// </example>
    /// <see cref="DecisionTree" />
    /// <see cref="C45Learning" />
    /// <see cref="RandomForestLearning" />
    [Serializable]
    public class ID3Learning : DecisionTreeLearningBase, ISupervisedLearning<DecisionTree, int[], int>
    {
        private IntRange[] inputRanges;

        private bool rejection = true;

        /// <summary>
        ///     Creates a new ID3 learning algorithm.
        /// </summary>
        public ID3Learning()
        {
        }

        /// <summary>
        ///     Creates a new ID3 learning algorithm.
        /// </summary>
        /// <param name="tree">The decision tree to be generated.</param>
        public ID3Learning(DecisionTree tree)
        {
            init(tree);
        }

        /// <summary>
        ///     Creates a new ID3 learning algorithm.
        /// </summary>
        /// <param name="attributes">The attributes to be processed by the induced tree.</param>
        public ID3Learning(DecisionVariable[] attributes)
            : base(attributes)
        {
        }

        /// <summary>
        ///     Gets or sets whether all nodes are obligated to provide
        ///     a true decision value. If set to false, some leaf nodes
        ///     may contain <c>null</c>. Default is false.
        /// </summary>
        public bool Rejection
        {
            get => rejection;
            set => rejection = value;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        public DecisionTree Learn(int[][] x, int[] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException("Not Supported Weights", "weights");
            }

            if (Model == null)
            {
                init(DecisionTreeHelper.Create(x, y, Attributes));
            }

            run(x, y);

            return Model;
        }

        private void init(DecisionTree tree)
        {
            if (tree == null)
            {
                throw new ArgumentNullException("tree");
            }

            Model = tree;
            inputRanges = new IntRange[tree.NumberOfInputs];
            AttributeUsageCount = new int[tree.NumberOfInputs];
            Attributes = tree.Attributes;

            for (var i = 0; i < tree.Attributes.Count; i++)
            {
                if (tree.Attributes[i].Nature != DecisionVariableKind.Discrete)
                {
                    throw new ArgumentException("The ID3 learning algorithm can only handle discrete inputs.");
                }
            }

            for (var i = 0; i < inputRanges.Length; i++)
            {
                inputRanges[i] = tree.Attributes[i].Range.ToIntRange(false);
            }
        }

        /// <summary>
        ///     Runs the learning algorithm, creating a decision
        ///     tree modeling the given inputs and outputs.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="outputs">The corresponding outputs.</param>
        /// <returns>The error of the generated tree.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(int[][] inputs, int[] outputs)
        {
            run(inputs, outputs);

            // Return the classification error
            return new ZeroOneLoss(outputs)
            {
                Mean = true
            }.Loss(Model.Decide(inputs));
        }

        private void run(int[][] inputs, int[] outputs)
        {
            // Initial argument check
            DecisionTreeHelper.CheckArgs(Model, inputs, outputs);

            // Reset the usage of all attributes
            for (var i = 0; i < AttributeUsageCount.Length; i++)
            {
                // a[i] has never been used
                AttributeUsageCount[i] = 0;
            }

            // 1. Create a root node for the tree
            Model.Root = new DecisionNode(Model);

            // Recursively split the tree nodes
            split(Model.Root, inputs, outputs, 0);
        }

        /// <summary>
        ///     Computes the prediction error for the tree
        ///     over a given set of input and outputs.
        /// </summary>
        /// <param name="inputs">The input points.</param>
        /// <param name="outputs">The corresponding output labels.</param>
        /// <returns>The percentage error of the prediction.</returns>
        [Obsolete("Please use the ZeroOneLoss class instead.")]
        public double ComputeError(int[][] inputs, int[] outputs)
        {
            return new ZeroOneLoss(outputs)
            {
                Mean = true
            }.Loss(Model.Decide(inputs));
        }

        private void split(DecisionNode root, int[][] input, int[] output, int height)
        {
            // 2. If all examples are for the same class, return the single-node
            //    tree with the output label corresponding to this common class.
            var entropy = Measures.Entropy(output, Model.NumberOfClasses);

            if (entropy == 0)
            {
                if (output.Length > 0)
                {
                    root.Output = output[0];
                }

                return;
            }

            // 3. If number of predicting attributes is empty, then return the single-node
            //    tree with the output label corresponding to the most common value of
            //    the target attributes in the examples.
            //

            // how many variables have been used less than the limit (if there is a limit)
            var candidates = AttributeUsageCount.Find(x => Join == 0 ? true : x < Join);

            if (candidates.Length == 0 || (MaxHeight > 0 && height == MaxHeight))
            {
                root.Output = output.Mode();
                return;
            }

            // 4. Otherwise, try to select the attribute which
            //    best explains the data sample subset. If the tree
            //    is part of a random forest, only consider a percentage
            //    of the candidate attributes at each split point

            if (MaxVariables > 0 && candidates.Length > MaxVariables)
            {
                candidates = candidates.Sample(MaxVariables);
            }

            var scores = new double[candidates.Length];
            var partitions = new int[candidates.Length][][];
            var outputSubs = new int[candidates.Length][][];

            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                for (var i = 0; i < scores.Length; i++)
                {
                    scores[i] = computeGainRatio(input, output, candidates[i],
                        entropy, out partitions[i], out outputSubs[i]);
                }
            }
            else
            {
                // For each attribute in the data set
                Parallel.For(0, scores.Length, ParallelOptions, i =>
                {
                    scores[i] = computeGainRatio(input, output, candidates[i],
                        entropy, out partitions[i], out outputSubs[i]);
                });
            }

            // Select the attribute with maximum gain ratio
            int maxGainIndex;
            scores.Max(out maxGainIndex);
            var maxGainPartition = partitions[maxGainIndex];
            var maxGainOutputs = outputSubs[maxGainIndex];
            var maxGainAttribute = candidates[maxGainIndex];
            var maxGainRange = inputRanges[maxGainAttribute];

            AttributeUsageCount[maxGainAttribute]++;

            // Now, create next nodes and pass those partitions as their responsibilities.
            var children = new DecisionNode[maxGainPartition.Length];

            for (var i = 0; i < children.Length; i++)
            {
                children[i] = new DecisionNode(Model)
                {
                    Parent = root,
                    Comparison = ComparisonKind.Equal,
                    Value = i + maxGainRange.Min
                };


                var inputSubset = input.Get(maxGainPartition[i]);
                var outputSubset = maxGainOutputs[i];

                split(children[i], inputSubset, outputSubset, height + 1); // recursion

                if (children[i].IsLeaf)
                {
                    // If the resulting node is a leaf, and it has not
                    // been assigned a value because there were no available
                    // output samples in this category, we will be assigning
                    // the most common label for the current node to it.
                    if (!Rejection && !children[i].Output.HasValue)
                    {
                        children[i].Output = output.Mode();
                    }
                }
            }


            AttributeUsageCount[maxGainAttribute]--;

            root.Branches.AttributeIndex = maxGainAttribute;
            root.Branches.AddRange(children);
        }

        private double computeInfo(int[][] input, int[] output, int attributeIndex,
            out int[][] partitions, out int[][] outputSubset)
        {
            // Compute the information gain obtained by using
            // this current attribute as the next decision node.
            double info = 0;

            var valueRange = inputRanges[attributeIndex];

            partitions = new int[valueRange.Length + 1][];
            outputSubset = new int[valueRange.Length + 1][];

            // For each possible value of the attribute
            for (var i = 0; i < partitions.Length; i++)
            {
                var value = valueRange.Min + i;

                // Partition the remaining data set
                // according to the attribute values
                partitions[i] = input.Find(x => x[attributeIndex] == value);

                // For each of the instances under responsibility
                // of this node, check which have the same value
                outputSubset[i] = output.Get(partitions[i]);

                // Check the entropy gain originating from this partitioning
                var e = Measures.Entropy(outputSubset[i], Model.NumberOfClasses);

                info += outputSubset[i].Length / (double)output.Length * e;
            }

            return info;
        }

        private double computeInfoGain(int[][] input, int[] output, int attributeIndex,
            double entropy, out int[][] partitions, out int[][] outputSubset)
        {
            return entropy - computeInfo(input, output, attributeIndex, out partitions, out outputSubset);
        }

        private double computeGainRatio(int[][] input, int[] output, int attributeIndex,
            double entropy, out int[][] partitions, out int[][] outputSubset)
        {
            var infoGain = computeInfoGain(input, output, attributeIndex, entropy, out partitions, out outputSubset);
            var splitInfo = SplitInformation(output.Length, partitions);

            return infoGain == 0 ? 0 : infoGain / splitInfo;
        }
    }
}