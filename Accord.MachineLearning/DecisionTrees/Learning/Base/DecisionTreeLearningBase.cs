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
using System.Linq;
using System.Threading.Tasks;
using Accord.Diagnostics;

namespace Accord.MachineLearning.DecisionTrees.Learning
{
    /// <summary>
    ///     Base class for tree inducing (learning) algorithms.
    /// </summary>
    [Serializable]
    public class DecisionTreeLearningBase : ParallelLearningBase, IEnumerable<DecisionVariable>
    {
        private int[] attributeUsageCount;

        private IList<DecisionVariable> attributes;

        private int join = 1;

        private int maxHeight;
        private int maxVariables;
        private DecisionTree tree;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DecisionTreeLearningBase" /> class.
        /// </summary>
        public DecisionTreeLearningBase()
        {
            ParallelOptions = new ParallelOptions();
            attributes = new List<DecisionVariable>();
        }

        /// <param name="attributes">The attributes to be processed by the induced tree.</param>
        //
        public DecisionTreeLearningBase(DecisionVariable[] attributes)
        {
            this.attributes = new List<DecisionVariable>(attributes);
        }

        /// <summary>
        ///     Gets or sets the maximum allowed height when learning a tree. If
        ///     set to zero, the tree can have an arbitrary length. Default is 0.
        /// </summary>
        public int MaxHeight
        {
            get => maxHeight;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value",
                        "The height must be greater than or equal to zero.");
                }

                maxHeight = value;
            }
        }

        /// <summary>
        ///     Gets or sets the maximum number of variables that
        ///     can enter the tree. A value of zero indicates there
        ///     is no limit. Default is 0 (there is no limit on the
        ///     number of variables).
        /// </summary>
        public int MaxVariables
        {
            get => maxVariables;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value",
                        "The height must be greater than or equal to zero.");
                }

                maxVariables = value;
            }
        }

        /// <summary>
        ///     Gets or sets the collection of attributes to
        ///     be processed by the induced decision tree.
        /// </summary>
        public IList<DecisionVariable> Attributes
        {
            get => attributes;
            set => attributes = value;
        }

        /// <summary>
        ///     Gets or sets how many times one single variable can be integrated into the decision process. In the original
        ///     ID3 algorithm, a variable can join only one time per decision path (path from the root to a leaf). If set to
        ///     zero, a single variable can participate as many times as needed. Default is 1.
        /// </summary>
        public int Join
        {
            get => join;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value",
                        "The number of times must be greater than or equal to zero.");
                }

                join = value;
            }
        }

        /// <summary>
        ///     Gets or sets the decision trees being learned.
        /// </summary>
        public DecisionTree Model
        {
            get => tree;
            set => tree = value;
        }

        /// <summary>
        ///     Gets how many times each attribute has already been used in the current path.
        ///     In the original C4.5 and ID3 algorithms, attributes could be re-used only once,
        ///     but in the framework implementation this behaviour can be adjusted by setting
        ///     the <see cref="Join" /> property.
        /// </summary>
        protected int[] AttributeUsageCount
        {
            get => attributeUsageCount;
            set => attributeUsageCount = value;
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<DecisionVariable> GetEnumerator()
        {
            return attributes.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return attributes.GetEnumerator();
        }

        /// <summary>
        ///     Adds the specified variable to the list of <see cref="Attribute" />s.
        /// </summary>
        public void Add(DecisionVariable variable)
        {
            attributes.Add(variable);
        }

        /// <summary>
        ///     Computes the split information measure.
        /// </summary>
        /// <param name="samples">The total number of samples.</param>
        /// <param name="partitions">The partitioning.</param>
        /// <param name="missing">An extra partition containing only missing values.</param>
        /// <returns>The split information for the given partitions.</returns>
        public static double SplitInformation(int samples, IList<int>[] partitions, List<int> missing = null)
        {
            double info = 0;

            for (var i = 0; i < partitions.Length; i++)
            {
                if (partitions[i] == null)
                {
                    continue;
                }

                var p = partitions[i].Count / (double)samples;

                if (p != 0)
                {
                    info -= p * System.Math.Log(p, 2);
                }
            }

            if (missing != null)
            {
                var p = missing.Count / (double)samples;

                if (p != 0)
                {
                    info -= p * System.Math.Log(p, 2);
                }
            }

#if DEBUG
            var totalSum = partitions.Sum(x => x == null ? 0 : x.Count);
            var missingSum = missing == null ? 0 : missing.Count;
            Debug.Assert(samples == totalSum + missingSum);
#endif

            return info;
        }
    }
}