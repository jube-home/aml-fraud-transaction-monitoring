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

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Base class for a data cluster.
    /// </summary>
    [Serializable]
    public abstract class Cluster<TCollection, TData, TCluster>
        where TCollection : class, IClusterCollectionEx<TData, TCluster>
        where TCluster : Cluster<TCollection, TData, TCluster>, new()
    {
        /// <summary>
        ///     Gets the collection to which this cluster belongs to.
        /// </summary>
        public TCollection Owner { get; private set; }

        /// <summary>
        ///     Gets the label for this cluster.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        ///     Gets the proportion of samples contained in this cluster.
        /// </summary>
        public double Proportion => Owner.Proportions[Index];

        [Serializable]
        internal class ClusterCollection : IClusterCollectionEx<TData, TCluster>
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="KMeansClusterCollection" /> class.
            /// </summary>
            /// <param name="collection">The collection that contains this instance as a field.</param>
            /// <param name="k">The number of clusters K.</param>
            public ClusterCollection(TCollection collection, int k)
            {
                // To store centroids of the clusters
                Proportions = new double[k];
                Clusters = new TCluster[k];
                this.Owner = collection;
                for (var i = 0; i < Clusters.Length; i++)
                {
                    Clusters[i] = new TCluster();
                    Clusters[i].Owner = collection;
                    Clusters[i].Index = i;
                }
            }

            protected TCollection Owner { get; }

            public double[] Proportions { get; }

            /// <summary>
            ///     Gets the cluster definitions.
            /// </summary>
            public TCluster[] Clusters { get; }

            /// <summary>
            ///     Gets the number of clusters in the collection.
            /// </summary>
            public int Count => Clusters.Length;

            /// <summary>
            ///     Gets the cluster at the given index.
            /// </summary>
            /// <param name="index">The index of the cluster. This should also be the class label of the cluster.</param>
            /// <returns>An object holding information about the selected cluster.</returns>
            public TCluster this[int index] => Clusters[index];

            /// <summary>
            ///     Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<TCluster> GetEnumerator()
            {
                foreach (var cluster in Clusters)
                {
                    yield return cluster;
                }
            }

            /// <summary>
            ///     Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return Clusters.GetEnumerator();
            }
        }
    }
}