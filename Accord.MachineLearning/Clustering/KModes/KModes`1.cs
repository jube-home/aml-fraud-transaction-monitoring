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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Accord.Diagnostics;
using Accord.Math;
using Accord.Math.Distances;
using Accord.Statistics;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     k-Modes algorithm.
    /// </summary>
    /// <remarks>
    ///     The k-Modes algorithm is a variant of the k-Means which instead of locating means attempts to locate
    ///     the modes of a set of points. As the algorithm does not require explicit numeric manipulation of the
    ///     input points (such as addition and division to compute the means), the algorithm can be used with
    ///     arbitrary (generic) data structures.
    /// </remarks>
    /// <seealso cref="KModes" />
    /// <seealso cref="KMeans" />
    /// <seealso cref="MeanShift" />
    /// <example>
    ///     How to perform clustering with K-Modes.
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Clustering\KModesTest.cs" region="doc_learn" />
    /// </example>
    [Serializable]
    public class KModes<T> : ParallelLearningBase,
        IUnsupervisedLearning<KModesClusterCollection<T>, T[], int>
    {
        /// <summary>
        ///     Initializes a new instance of KModes algorithm
        /// </summary>
        /// <param name="k">The number of clusters to divide input data.</param>
        /// <param name="distance">
        ///     The distance function to use. Default is to
        ///     use the <see cref="Distance.SquareEuclidean(double[], double[])" /> distance.
        /// </param>
        [Obsolete("Please specify the distance function using classes instead of lambda functions.")]
        public KModes(int k, Func<T[], T[], double> distance)
            : this(k, Math.Distance.GetDistance(distance))
        {
        }

        /// <summary>
        ///     Initializes a new instance of KModes algorithm
        /// </summary>
        /// <param name="k">The number of clusters to divide input data.</param>
        /// <param name="distance">
        ///     The distance function to use. Default is to
        ///     use the <see cref="Distance.SquareEuclidean(double[], double[])" /> distance.
        /// </param>
        public KModes(int k, IDistance<T[]> distance)
        {
            if (k <= 0)
            {
                throw new ArgumentOutOfRangeException("k");
            }

            if (distance == null)
            {
                throw new ArgumentNullException("distance");
            }

            // Create the object-oriented structure to hold
            //  information about the k-modes' clusters.
            Clusters = new KModesClusterCollection<T>(k, distance);

            Initialization = Seeding.KMeansPlusPlus;
            Tolerance = 1e-5;
            MaxIterations = 100;
        }

        /// <summary>
        ///     Gets the clusters found by K-modes.
        /// </summary>
        public KModesClusterCollection<T> Clusters { get; }

        /// <summary>
        ///     Gets the number of clusters.
        /// </summary>
        public int K => Clusters.Count;

        /// <summary>
        ///     Gets the dimensionality of the data space.
        /// </summary>
        public int Dimension => Clusters.NumberOfInputs;

        /// <summary>
        ///     Gets or sets whether the clustering distortion error (the
        ///     average distance between all data points and the cluster
        ///     centroids) should be computed at the end of the algorithm.
        ///     The result will be stored in <see cref="Error" />. Default is true.
        /// </summary>
        public bool ComputeError { get; set; }

        /// <summary>
        ///     Gets or sets the distance function used
        ///     as a distance metric between data points.
        /// </summary>
        public IDistance<T[], T[]> Distance
        {
            get => Clusters.Distance;
            set => Clusters.Distance = value;
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations to
        ///     be performed by the method. If set to zero, no
        ///     iteration limit will be imposed. Default is 0.
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        ///     Gets or sets the relative convergence threshold
        ///     for stopping the algorithm. Default is 1e-5.
        /// </summary>
        public double Tolerance { get; set; }

        /// <summary>
        ///     Gets the number of iterations performed in the
        ///     last call to this class' Compute methods.
        /// </summary>
        public int Iterations { get; private set; }

        /// <summary>
        ///     Gets the cluster distortion error (the average distance
        ///     between data points and the cluster centroids) after the
        ///     last call to this class' Compute methods.
        /// </summary>
        public double Error { get; private set; }

        /// <summary>
        ///     Gets or sets the strategy used to initialize the
        ///     centroids of the clustering algorithm. Default is
        ///     <see cref="Seeding.KMeansPlusPlus" />.
        /// </summary>
        public Seeding Initialization { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">x</exception>
        /// <exception cref="ArgumentException">Not enough points. There should be more points than the number K of clusters.</exception>
        public KModesClusterCollection<T> Learn(T[][] x, double[] weights = null)
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

            var k = K;
            var rows = x.Length;
            var cols = x[0].Length;

            // Perform a random initialization of the clusters
            // if the algorithm has not been initialized before.
            //
            if (Clusters.Centroids[0] == null)
            {
                Clusters.Randomize(x);
            }

            // Initial variables
            var labels = new int[rows];
            var proportions = Clusters.Proportions;
            var centroids = Clusters.Centroids;
            var newCentroids = new T[k][];
            for (var i = 0; i < newCentroids.Length; i++)
            {
                newCentroids[i] = new T[cols];
            }

            var clusters = new ConcurrentBag<T[]>[k];

            Iterations = 0;

            do // Main loop
            {
                // Reset the centroids and the
                //  cluster member counters'
                for (var i = 0; i < k; i++)
                {
                    clusters[i] = new ConcurrentBag<T[]>();
                }

                // First we will accumulate the data points
                // into their nearest clusters, storing this
                // information into the newClusters variable.

                // For each point in the data set,
                Parallel.For(0, x.Length, ParallelOptions, i =>
                {
                    // Get the point
                    var point = x[i];

                    // Compute the nearest cluster centroid
                    var c = labels[i] = Clusters.Decide(x[i]);

                    // Accumulate in the corresponding centroid
                    clusters[c].Add(point);
                });

                // Next we will compute each cluster's new centroid
                //  value by computing the mode in each cluster.

                Parallel.For(0, k, ParallelOptions, i =>
                {
                    if (clusters[i].Count == 0)
                    {
                        newCentroids[i] = centroids[i];
                    }
                    else
                    {
                        var p = clusters[i].Transpose<ConcurrentBag<T[]>, T>();

                        // For each dimension
                        for (var d = 0; d < Dimension; d++)
                        {
                            newCentroids[i][d] = p[d].Mode(alreadySorted: false, inPlace: true);
                        }
                    }
                });


                // The algorithm stops when there is no further change in the
                //  centroids (relative difference is less than the threshold).
                if (converged(centroids, newCentroids))
                {
                    break;
                }


                // go to next generation
                for (var i = 0; i < centroids.Length; i++)
                {
                    centroids[i] = newCentroids[i];
                }
            } while (true);


            // Compute cluster information (optional)
            for (var i = 0; i < k; i++)
            {
                // Compute the proportion of samples in the cluster
                proportions[i] = clusters[i].Count / (double)x.Length;
            }

            if (ComputeError)
                // Compute the average error
            {
                Error = Clusters.Distortion(x, labels);
            }

            Debug.Assert(Clusters.NumberOfClasses == K);
            Debug.Assert(Clusters.NumberOfOutputs == K);
            Debug.Assert(Clusters.NumberOfInputs == x[0].Length);

            // Return the classification result
            return Clusters;
        }

        /// <summary>
        ///     Divides the input data into K clusters.
        /// </summary>
        /// <param name="points">The data where to compute the algorithm.</param>
        [Obsolete("Please use Learn(x) instead.")]
        public int[] Compute(T[][] points)
        {
            return Learn(points).Decide(points);
        }

        /// <summary>
        ///     Determines if the algorithm has converged by comparing the
        ///     centroids between two consecutive iterations.
        /// </summary>
        /// <param name="centroids">The previous centroids.</param>
        /// <param name="newCentroids">The new centroids.</param>
        /// <returns>
        ///     Returns <see langword="true" /> if all centroids had a percentage change
        ///     less than <see param="threshold" />. Returns <see langword="false" /> otherwise.
        /// </returns>
        private bool converged(T[][] centroids, T[][] newCentroids)
        {
            Iterations++;

            if (MaxIterations > 0 && Iterations > MaxIterations)
            {
                return true;
            }

            if (Token.IsCancellationRequested)
            {
                return true;
            }

            for (var i = 0; i < centroids.Length; i++)
            {
                var centroid = centroids[i];
                var newCentroid = newCentroids[i];

                if (System.Math.Abs(Distance.Distance(centroid, newCentroid)) >= Tolerance)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    ///     k-Modes algorithm.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The k-Modes algorithm is a variant of the k-Means which instead of
    ///         locating means attempts to locate the modes of a set of points. As
    ///         the algorithm does not require explicit numeric manipulation of the
    ///         input points (such as addition and division to compute the means),
    ///         the algorithm can be used with arbitrary (generic) data structures.
    ///     </para>
    ///     <para>
    ///         This is the specialized, non-generic version of the K-Modes algorithm
    ///         that is set to work on <see cref="T:System.Int32" /> arrays.
    ///     </para>
    /// </remarks>
    /// <seealso cref="KModes{T}" />
    [Serializable]
    public class KModes : KModes<int>
    {
        /// <summary>
        ///     Initializes a new instance of K-Modes algorithm
        /// </summary>
        /// <param name="k">The number of clusters to divide input data.</param>
        public KModes(int k)
            : base(k, new Manhattan())
        {
        }
    }
}