// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Alex Risman, 2016
// https://github.com/mthmn20
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Accord.Math;

namespace Accord.MachineLearning.DecisionTrees
{
    /// <summary>
    ///     Random Forest.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Represents a random forest of <see cref="DecisionTree" />s. For
    ///         sample usage and example of learning, please see the documentation
    ///         page for <see cref="RandomForestLearning" />.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This example shows the simplest way to induce a decision tree with continuous variables.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\RandomForestTest.cs" region="doc_iris" />
    ///     <para>
    ///         The next example shows how to induce a decision tree with continuous variables using a
    ///         <see cref="Statistics.Filters.Codification">codebook</see> to manage how input
    ///         variables should be encoded.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\RandomForestTest.cs" region="doc_nursery" />
    /// </example>
    /// <seealso cref="DecisionTree" />
    /// <seealso cref="RandomForestLearning" />
    [Serializable]
    public class RandomForest : MulticlassClassifierBase, IParallel
    {
        [NonSerialized] private ParallelOptions parallelOptions;

        private RandomForest()
        {
            ParallelOptions = new ParallelOptions();
        }

        /// <summary>
        ///     Creates a new random forest.
        /// </summary>
        /// <param name="trees">The trees to be added to the forest.</param>
        public RandomForest(DecisionTree[] trees)
            : this()
        {
            init(trees);
        }

        /// <summary>
        ///     Creates a new random forest.
        /// </summary>
        /// <param name="trees">The number of trees to be added to the forest.</param>
        /// <param name="inputs">An array specifying the attributes to be processed by the trees.</param>
        /// <param name="classes">The number of classes in the classification problem.</param>
        public RandomForest(int trees, IList<DecisionVariable> inputs, int classes)
            : this()
        {
            var t = new DecisionTree[trees];
            for (var i = 0; i < t.Length; i++)
            {
                t[i] = new DecisionTree(inputs, classes);
            }

            init(t);
        }

        /// <summary>
        ///     Creates a new random forest.
        /// </summary>
        /// <param name="trees">The number of trees in the forest.</param>
        /// <param name="classes">The number of classes in the classification problem.</param>
        public RandomForest(int trees, int classes)
            : this()
        {
            this.Trees = new DecisionTree[trees];
            NumberOfOutputs = classes;
            NumberOfClasses = classes;
        }

        /// <summary>
        ///     Gets the trees in the random forest.
        /// </summary>
        public DecisionTree[] Trees { get; private set; }

        /// <summary>
        ///     Gets the number of classes that can be recognized
        ///     by this random forest.
        /// </summary>
        [Obsolete("Please use NumberOfOutputs instead.")]
        public int Classes => NumberOfOutputs;

        /// <summary>
        ///     Gets or sets the parallelization options for this algorithm.
        /// </summary>
        public ParallelOptions ParallelOptions
        {
            get => parallelOptions;
            set => parallelOptions = value;
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used
        ///     to cancel the algorithm while it is running.
        /// </summary>
        public CancellationToken Token
        {
            get => ParallelOptions.CancellationToken;
            set => ParallelOptions.CancellationToken = value;
        }

        private void init(DecisionTree[] trees)
        {
            this.Trees = trees;
            NumberOfInputs = trees[0].NumberOfInputs;
            NumberOfOutputs = trees[0].NumberOfOutputs;
            NumberOfClasses = trees[0].NumberOfClasses;

            for (var i = 0; i < trees.Length; i++)
            {
                if (trees[i].NumberOfInputs != NumberOfInputs)
                {
                    throw new Exception("The decision tree accepts less inputs than {0}".Format(NumberOfInputs));
                }

                if (trees[i].NumberOfClasses != NumberOfClasses)
                {
                    throw new Exception("The decision tree recognizes less classes than {0}".Format(NumberOfClasses));
                }

                if (trees[i].NumberOfOutputs != NumberOfOutputs)
                {
                    throw new Exception("The decision tree produces less outputs than {0}".Format(NumberOfOutputs));
                }
            }
        }

        /// <summary>
        ///     Computes the decision output for a given input vector.
        /// </summary>
        /// <param name="data">The input vector.</param>
        /// <returns>The forest decision for the given vector.</returns>
        [Obsolete("Please use Decide() instead.")]
        public int Compute(double[] data)
        {
            return Decide(data);
        }

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public override int Decide(double[] input)
        {
            var responses = new int[NumberOfOutputs];
            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                for (var i = 0; i < Trees.Length; i++)
                {
                    var j = Trees[i].Decide(input);
                    if (j >= 0)
                    {
                        Interlocked.Increment(ref responses[j]);
                    }
                }
            }
            else
            {
                Parallel.For(0, Trees.Length, ParallelOptions, i =>
                {
                    var j = Trees[i].Decide(input);
                    if (j >= 0)
                    {
                        Interlocked.Increment(ref responses[j]);
                    }
                });
            }

            return responses.ArgMax();
        }

        /// <summary>
        ///     Called when the object is being deserialized.
        /// </summary>
        [OnDeserializing]
        protected void OnDeserializingMethod(StreamingContext context)
        {
            parallelOptions = new ParallelOptions();
        }
    }
}