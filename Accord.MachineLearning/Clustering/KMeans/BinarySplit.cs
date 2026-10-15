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
using Accord.Diagnostics;
using Accord.Math;
using Accord.Math.Distances;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Binary split clustering algorithm.
    /// </summary>
    /// <example>
    ///     How to perform clustering with Binary Split.
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Clustering\BinarySplitTest.cs" region="doc_sample1" />
    /// </example>
    /// <seealso cref="KMeans" />
    /// <seealso cref="GaussianMixtureModel" />
    [Serializable]
    public class BinarySplit : KMeans
    {
        private bool computeProportions;

        /// <summary>
        ///     Initializes a new instance of the Binary Split algorithm
        /// </summary>
        /// <param name="k">The number of clusters to divide the input data into.</param>
        /// <param name="distance">
        ///     The distance function to use. Default is to
        ///     use the <see cref="Distance.SquareEuclidean(double[], double[])" /> distance.
        /// </param>
        public BinarySplit(int k, IDistance<double[]> distance)
            : base(k, distance)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the Binary Split algorithm
        /// </summary>
        /// <param name="k">The number of clusters to divide the input data into.</param>
        public BinarySplit(int k)
            : base(k)
        {
        }

        /// <summary>
        ///     Gets or sets whether <see cref="KMeansClusterCollection.Proportions">cluster proportions</see>
        ///     should be calculated after the learning algorithm has finished computing the clusters. Default
        ///     is false.
        /// </summary>
        /// <value><c>true</c> if  to compute proportions after learning; otherwise, <c>false</c>.</value>
        public bool ComputeProportions
        {
            get => computeProportions;
            set => computeProportions = value;
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
        public override KMeansClusterCollection Learn(double[][] x, double[] weights = null)
        {
            // Initial argument checking
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (x.Length < K)
            {
                throw new ArgumentException(
                    "Not enough points. There should be more points than the number K of clusters.");
            }

            if (weights == null)
            {
                weights = Vector.Ones(x.Length);
            }

            if (x.Length != weights.Length)
            {
                throw new ArgumentException("Data weights vector must be the same length as data samples.");
            }

            var weightSum = weights.Sum();
            if (weightSum <= 0)
            {
                throw new ArgumentException(
                    "Not enough points. There should be more points than the number K of clusters.");
            }

            var cols = x.Columns();
            for (var i = 0; i < x.Length; i++)
            {
                if (x[i].Length != cols)
                {
                    throw new DimensionMismatchException("data",
                        "The points matrix should be rectangular. The vector at position {} has a different length than previous ones.");
                }
            }

            var k = Clusters.Count;

            var kmeans = new KMeans(2)
            {
                Distance = (IDistance<double[]>)Clusters.Distance,
                ComputeError = false,
                ComputeCovariances = false,
                UseSeeding = UseSeeding,
                Tolerance = Tolerance,
                MaxIterations = MaxIterations
            };

            var centroids = Clusters.Centroids;
            var clusters = new double[k][][];
            var distortions = new double[k];

            // 1. Start with all data points in one cluster
            clusters[0] = x;

            // 2. Repeat steps 3 to 6 (k-1) times to obtain K centroids
            for (var current = 1; current < k; current++)
            {
                // 3. Choose cluster with largest distortion
                int choosen;
                distortions.Max(current, out choosen);

                // 4. Split cluster into two sub-clusters
                var splits = split(clusters[choosen], kmeans);

                clusters[choosen] = splits.Item1;
                clusters[current] = splits.Item2;

                // 5. Replace chosen centroid and add a new one
                centroids[choosen] = kmeans.Clusters.Centroids[0];
                centroids[current] = kmeans.Clusters.Centroids[1];

                // Recompute distortions for the updated clusters
                distortions[choosen] = kmeans.Clusters[0].Distortion(clusters[choosen]);
                distortions[current] = kmeans.Clusters[1].Distortion(clusters[current]);

                // 6. Increment cluster count (current = current + 1)
            }

            Clusters.NumberOfInputs = cols;

            Debug.Assert(Clusters.NumberOfClasses == K);
            Debug.Assert(Clusters.NumberOfOutputs == K);
            Debug.Assert(Clusters.NumberOfInputs == x[0].Length);

            if (ComputeProportions)
            {
                var y = Clusters.Decide(x);
                var counts = y.Histogram();
                counts.Divide(y.Length, Clusters.Proportions);

                ComputeInformation(x, y);
            }
            else
            {
                ComputeInformation(x);
            }

            return Clusters;
        }

        private static Tuple<double[][], double[][]> split(double[][] cluster, KMeans kmeans)
        {
            kmeans.Randomize(cluster);

            var idx = kmeans.Learn(cluster).Decide(cluster);

            var a = new List<double[]>();
            var b = new List<double[]>();

            for (var i = 0; i < idx.Length; i++)
            {
                if (idx[i] == 0)
                {
                    a.Add(cluster[i]);
                }
                else
                {
                    b.Add(cluster[i]);
                }
            }

            return Tuple.Create(a.ToArray(), b.ToArray());
        }
    }
}