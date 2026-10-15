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
using System.Threading.Tasks;
using Accord.Collections;
using Accord.Math;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Mean shift cluster collection.
    /// </summary>
    /// <seealso cref="MeanShift" />
    [Serializable]
    public class MeanShiftClusterCollection : MulticlassClassifierBase<double[]>,
        IClusterCollectionEx<double[], MeanShiftClusterCollection.MeanShiftCluster>
    {
        private Cluster<MeanShiftClusterCollection, double[], MeanShiftCluster>.ClusterCollection collection;

        //private MeanShift algorithm;
        private KDTree<int> tree;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MeanShiftClusterCollection" /> class.
        /// </summary>
        public MeanShiftClusterCollection(int k, KDTree<int> tree, double[][] modes)
        {
            collection = new Cluster<MeanShiftClusterCollection, double[], MeanShiftCluster>.ClusterCollection(this, k);
            //this.algorithm = algorithm;
            this.tree = tree;
            this.Modes = modes;
        }

        /// <summary>
        ///     Gets the cluster modes.
        /// </summary>
        public double[][] Modes { get; }

        // Using composition over inheritance to achieve the closest as possible effect to a Mixin
        // in C# - unfortunately needs a lot a boilerplate code to rewrire the interface methods to
        // where their actual implementation is

        /// <summary>
        ///     Gets the number of clusters in the collection.
        /// </summary>
        /// <value>The count.</value>
        public int Count => ((IClusterCollectionEx<double[], MeanShiftCluster>)collection).Count;

        /// <summary>
        ///     Gets the collection of clusters currently modeled by the clustering algorithm.
        /// </summary>
        /// <value>The clusters.</value>
        public MeanShiftCluster[] Clusters => ((IClusterCollectionEx<double[], MeanShiftCluster>)collection).Clusters;

        /// <summary>
        ///     Gets the proportion of samples in each cluster.
        /// </summary>
        public double[] Proportions => ((IClusterCollectionEx<double[], MeanShiftCluster>)collection).Proportions;

        /// <summary>
        ///     Gets the <see cref="MeanShiftCluster" /> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>GaussianCluster.</returns>
        public MeanShiftCluster this[int index] =>
            ((IClusterCollectionEx<double[], MeanShiftCluster>)collection)[index];

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<MeanShiftCluster> GetEnumerator()
        {
            return ((IClusterCollectionEx<double[], MeanShiftCluster>)collection).GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IClusterCollectionEx<double[], MeanShiftCluster>)collection).GetEnumerator();
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
            // the tree contains the class label as the value for the seed point.
            return tree.Nearest(input).Value;
        }

        /// <summary>
        ///     Calculates the average square distance from the data points
        ///     to the nearest clusters' centroids.
        /// </summary>
        /// <remarks>
        ///     The average distance from centroids can be used as a measure
        ///     of the "goodness" of the clustering. The more the data are
        ///     aggregated around the centroids, the less the average distance.
        /// </remarks>
        /// <returns>
        ///     The average square distance from the data points to the nearest
        ///     clusters' centroids.
        /// </returns>
        public double Distortion(double[][] data, int[] labels = null, double[] weights = null)
        {
            if (labels != null)
            {
                throw new NotSupportedException();
            }

            if (weights == null)
            {
                weights = Vector.Ones(data.Length);
            }

            var error = 0.0;

            // TODO: Use ParallelOptions in the loop below
            Parallel.For(0, data.Length,
                () => 0.0,
                (i, state, acc) =>
                {
                    double distance;
                    tree.Nearest(data[i], out distance);
                    return acc + weights[i] * distance;
                },
                acc => { error += acc; });

            return error / weights.Sum();
        }

        /// <summary>
        ///     Transform data points into feature vectors containing the
        ///     distance between each point and each of the clusters.
        /// </summary>
        /// <param name="points">The input points.</param>
        /// <param name="labels">The label of each input point.</param>
        /// <param name="weights">The weight associated with each point.</param>
        /// <param name="result">An optional matrix to store the computed transformation.</param>
        /// <returns>
        ///     A vector containing the distance between the input points and the clusters.
        /// </returns>
        /// <exception cref="NotSupportedException"></exception>
        public double[] Transform(double[][] points, int[] labels, double[] weights = null, double[] result = null)
        {
            if (result == null)
            {
                result = new double[points.Length];
            }

            if (labels != null)
            {
                throw new NotSupportedException();
            }

            if (weights == null)
            {
                weights = Vector.Ones(points.Length);
            }

            Parallel.For(0, result.Length, i =>
            {
                double distance;
                tree.Nearest(points[i], out distance);
                result[i] = weights[i] * distance;
            });

            return result;
        }

        /// <summary>
        ///     Mean shift cluster.
        /// </summary>
        /// <seealso cref="MeanShift" />
        /// <seealso cref="MeanShiftClusterCollection" />
        [Serializable]
        public class MeanShiftCluster : Cluster<MeanShiftClusterCollection, double[], MeanShiftCluster>
        {
        }
    }
}