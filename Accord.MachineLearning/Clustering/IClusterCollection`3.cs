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
using Accord.Math.Distances;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Common interface for collections of clusters (i.e. <see cref="KMeansClusterCollection" />,
    ///     <see cref="GaussianClusterCollection" />, <see cref="MeanShiftClusterCollection" />).
    /// </summary>
    public interface IClusterCollectionEx<TData, TCluster> : IEnumerable<TCluster>
    {
        /// <summary>
        ///     Gets the number of clusters in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///     Gets the collection of clusters currently modeled by the clustering algorithm.
        /// </summary>
        TCluster[] Clusters { get; }

        /// <summary>
        ///     Gets the proportion of samples in each cluster.
        /// </summary>
        double[] Proportions { get; }

        /// <summary>
        ///     Gets the cluster at the given index.
        /// </summary>
        /// <param name="index">The index of the cluster. This should also be the class label of the cluster.</param>
        /// <returns>An object holding information about the selected cluster.</returns>
        TCluster this[int index] { get; }
    }

    /// <summary>
    ///     Common interface for clusters that contains centroids, where the centroid data type might be different
    ///     from the data type of the data bring clustered (i.e. <see cref="GaussianClusterCollection.GaussianCluster" />).
    /// </summary>
    public interface ICentroidClusterCollection<TData, TCentroids, TCluster> : IClusterCollectionEx<TData, TCluster>
    {
        /// <summary>
        ///     Gets or sets the clusters' centroids.
        /// </summary>
        /// <value>The clusters' centroids.</value>
        TCentroids[] Centroids { get; set; }

        /// <summary>
        ///     Gets or sets the distance function used to measure the distance
        ///     between a point and the cluster centroid in this clustering definition.
        /// </summary>
        IDistance<TData, TCentroids> Distance { get; set; }

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
        double Distortion(TData[] data, int[] labels = null, double[] weights = null);

        /// <summary>
        ///     Transform data points into feature vectors containing the
        ///     distance between each point and each of the clusters.
        /// </summary>
        /// <param name="points">The input points.</param>
        /// <param name="weights">The weight associated with each point.</param>
        /// <param name="result">An optional matrix to store the computed transformation.</param>
        /// <returns>A vector containing the distance between the input points and the clusters.</returns>
        double[][] Transform(TData[] points, double[] weights = null, double[][] result = null);

        /// <summary>
        ///     Transform data points into feature vectors containing the
        ///     distance between each point and each of the clusters.
        /// </summary>
        /// <param name="points">The input points.</param>
        /// <param name="labels">The label of each input point.</param>
        /// <param name="weights">The weight associated with each point.</param>
        /// <param name="result">An optional matrix to store the computed transformation.</param>
        /// <returns>A vector containing the distance between the input points and the clusters.</returns>
        double[] Transform(TData[] points, int[] labels, double[] weights = null, double[] result = null);
    }

    /// <summary>
    ///     Common interface for clusters that contains centroids which are of the same data type
    ///     as the clustered data types (i.e. <see cref="KMeansClusterCollection.KMeansCluster" />).
    /// </summary>
    public interface ICentroidClusterCollection<TData, TCluster> : ICentroidClusterCollection<TData, TData, TCluster>
    {
    }
}