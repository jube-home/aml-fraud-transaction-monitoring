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
using System.Threading;
using Accord.Math;

namespace Accord.MachineLearning.Rules
{
    /// <summary>
    ///     A-priori algorithm for association rule mining.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Anita Wasilewska, Lecture Notes. Available on
    ///                     http://www3.cs.stonybrook.edu/~cse634/lecture_notes/07apriori.pdf
    ///                 </description>
    ///             </item>
    ///         </list>
    ///         >
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\AprioriTest.cs" region="doc_learn_1" lang="cs" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\AprioriTest.cs" region="doc_learn_2" lang="cs" />
    /// </example>
    /// <seealso cref="AssociationRule{T}" />
    /// <seealso cref="AssociationRuleMatcher{T}" />
    public class Apriori : Apriori<int>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Apriori" /> class.
        /// </summary>
        /// <param name="threshold">
        ///     The minimum number of times a rule should be detected (also known as its support)
        ///     before it can be registered as a permanent in the learned classifier.
        /// </param>
        /// <param name="confidence">
        ///     The minimum confidence in an association rule beore it is
        ///     registered.
        /// </param>
        public Apriori(int threshold, double confidence)
            : base(threshold, confidence)
        {
        }
    }

    /// <summary>
    ///     A-priori algorithm for association rule mining.
    /// </summary>
    /// <typeparam name="T">The dataset item type. Default is int.</typeparam>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Anita Wasilewska, Lecture Notes. Available on
    ///                     http://www3.cs.stonybrook.edu/~cse634/lecture_notes/07apriori.pdf
    ///                 </description>
    ///             </item>
    ///         </list>
    ///         >
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\AprioriTest.cs" region="doc_learn_1" lang="cs" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\AprioriTest.cs" region="doc_learn_2" lang="cs" />
    /// </example>
    /// <seealso cref="AssociationRule{T}" />
    /// <seealso cref="AssociationRuleMatcher{T}" />
    public class Apriori<T> :
        IUnsupervisedLearning<AssociationRuleMatcher<T>, SortedSet<T>, SortedSet<T>[]>,
        IUnsupervisedLearning<AssociationRuleMatcher<T>, T[], T[][]>
    {
        private readonly double confidence;

        private readonly int supportMin;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Apriori{T}" /> class.
        /// </summary>
        /// <param name="threshold">
        ///     The minimum number of times a rule should be detected (also known as its support)
        ///     before it can be registered as a permanent in the learned classifier.
        /// </param>
        /// <param name="confidence">
        ///     The minimum confidence in an association rule beore it is
        ///     registered.
        /// </param>
        public Apriori(int threshold, double confidence)
        {
            supportMin = threshold;
            this.confidence = confidence;
            Frequent = new Dictionary<SortedSet<T>, int>(new SetComparer());
        }

        /// <summary>
        ///     Gets the set of most frequent items and the respective
        ///     number of times their appear in in the training dataset.
        /// </summary>
        public Dictionary<SortedSet<T>, int> Frequent { get; }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        /// <value>The token.</value>
        [field: NonSerialized]
        public CancellationToken Token { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public AssociationRuleMatcher<T> Learn(SortedSet<T>[] x, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException("Not Supported Weights", "weights");
            }

            Frequent.Clear();
            var L = new HashSet<SortedSet<T>>(new SetComparer());
            var counts = new Dictionary<SortedSet<T>, int>(new SetComparer());

            foreach (var t in x)
            {
                foreach (var s in t)
                {
                    L.Add(new SortedSet<T> { s });
                }
            }

            var items = L.Count;

            for (var k = 1; L.Count != 0; k++)
            {
                var C = fold(L, k);
                counts.Clear();
                foreach (var transaction in x)
                {
                    foreach (var candidate in C)
                    {
                        if (candidate.IsSubsetOf(transaction))
                        {
                            int count;
                            if (counts.TryGetValue(candidate, out count))
                            {
                                counts[candidate] = count + 1;
                            }
                            else
                            {
                                counts[candidate] = 1;
                            }
                        }
                    }
                }

                L.Clear();
                foreach (var pair in counts)
                {
                    if (pair.Value >= supportMin)
                    {
                        L.Add(pair.Key);
                        Frequent.Add(pair.Key, pair.Value);
                    }
                }
            }

            var rules = new List<AssociationRule<T>>();

            // Generate association rules from the most frequent itemsets
            foreach (var itemset in Frequent.Keys)
            {
                double total = support(itemset, x);

                // generate all non-empty subsets of I
                foreach (var s in itemset.Subsets())
                {
                    double subtotal = support(s, x);
                    var sc = total / subtotal;
                    if (sc >= confidence && subtotal >= supportMin)
                    {
                        var y = new SortedSet<T>(itemset);
                        y.ExceptWith(s);
                        if (y.Count > 0)
                        {
                            rules.Add(new AssociationRule<T>
                            {
                                Confidence = sc,
                                Support = total,
                                X = s,
                                Y = y
                            });
                        }
                    }
                }
            }

            return new AssociationRuleMatcher<T>(items, rules.ToArray());
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public AssociationRuleMatcher<T> Learn(T[][] x, double[] weights = null)
        {
            return Learn(x.Apply(xi => new SortedSet<T>(xi)), weights);
        }

        private int support(ISet<T> set, IList<SortedSet<T>> dataset)
        {
            var count = 0;
            foreach (var itemset in dataset)
            {
                if (set.IsSubsetOf(itemset))
                {
                    count++;
                }
            }

            return count;
        }

        private HashSet<SortedSet<T>> fold(HashSet<SortedSet<T>> L, int k)
        {
            var r = new HashSet<SortedSet<T>>(new SetComparer());

            foreach (var i in L)
            {
                foreach (var j in L)
                {
                    var t = new SortedSet<T>(i);
                    t.UnionWith(j);
                    if (t.Count == k)
                    {
                        r.Add(t);
                    }
                }
            }

            return r;
        }

        // TODO: Move to a public class
        internal class SetComparer : IEqualityComparer<SortedSet<T>>
        {
            public bool Equals(SortedSet<T> x, SortedSet<T> y)
            {
                return x.SetEquals(y);
            }

            public int GetHashCode(SortedSet<T> obj)
            {
                // TODO: Change to something more efficient
                var sum = 0;
                foreach (var i in obj)
                {
                    sum ^= i.GetHashCode();
                }

                return sum;
            }
        }
    }
}