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
using Accord.Diagnostics;
using Accord.Math;
using Accord.Math.Distances;
using Accord.Math.Optimization;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Balanced K-Means algorithm. Note: The balanced clusters will be
    ///     available in the <see cref="Labels" /> property of this instance!
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Balanced k-Means algorithm attempts to find a clustering where each cluster
    ///         has approximately the same number of data points. The Balanced k-Means implementation
    ///         used in the framework uses the <see cref="Munkres" /> algorithm to solve the assignment
    ///         problem thus enforcing balance between the clusters.
    ///     </para>
    ///     <para>
    ///         Note: the <see cref="Learn(double[][], double[])" /> method of this class will
    ///         return the centroids of balanced clusters, but please note that these centroids
    ///         cannot be used to obtain balanced clusterings for another (or even the same) data
    ///         set. Instead, in order to inspect the balanced clustering that has been obtained
    ///         after calling <see cref="Learn(double[][], double[])" />, please take a look at the
    ///         contents of the <see cref="Labels" /> property.
    ///     </para>
    /// </remarks>
    /// <para>
    ///     References:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 M. I. Malinen and P.Fränti, "Balanced K-means for Clustering", Joint Int.Workshop on Structural,
    ///                 Syntactic,
    ///                 and Statistical Pattern Recognition (S+SSPR 2014), LNCS 8621, 32-41, Joensuu, Finland, August 2014.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 M. I. Malinen, "New alternatives for k-Means clustering." PhD thesis. Available in:
    ///                 http://cs.uef.fi/sipu/pub/PhD_Thesis_Mikko_Malinen.pdf
    ///             </description>
    ///         </item>
    ///     </list>
    /// </para>
    /// <example>
    ///     How to perform clustering with Balanced K-Means.
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Clustering\BalancedKMeansTest.cs" region="doc_learn" />
    /// </example>
    /// <seealso cref="KMeans" />
    /// <seealso cref="BinarySplit" />
    /// <seealso cref="GaussianMixtureModel" />
    [Serializable]
    public class BalancedKMeans : KMeans
    {
        internal Munkres munkres;

        /// <summary>
        ///     Initializes a new instance of the Balanced K-Means algorithm.
        /// </summary>
        /// <param name="k">The number of clusters to divide the input data into.</param>
        /// <param name="distance">
        ///     The distance function to use. Default is to
        ///     use the <see cref="Distance.SquareEuclidean(double[], double[])" /> distance.
        /// </param>
        public BalancedKMeans(int k, IDistance<double[]> distance)
            : base(k, distance)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the Balanced K-Means algorithm.
        /// </summary>
        /// <param name="k">The number of clusters to divide the input data into.</param>
        public BalancedKMeans(int k)
            : base(k)
        {
        }

        /// <summary>
        ///     Gets the labels assigned for each data point in the last
        ///     call to <see cref="Learn(double[][], double[])" />.
        /// </summary>
        /// <value>The labels.</value>
        public int[] Labels { get; private set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs. Note:
        ///     the model created by this function will not be able to produce balanced
        ///     clusterings. To retrieve the balanced labels, check the <see cref="Labels" />
        ///     property of this class after calling this function.
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
            if (MaxIterations == 0)
            {
                throw new InvalidOperationException(
                    "MaxIterations must be higher than zero. The Balanced K-Means algorithm has a high chance of oscillating between possible answers without converging.");
            }

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
            else
            {
                if (x.Length != weights.Length)
                {
                    throw new ArgumentException("Data weights vector must be the same length as data samples.");
                }
            }

            var weightSum = weights.Sum();
            if (weightSum <= 0)
            {
                throw new ArgumentException(
                    "Not enough points. There should be more points than the number K of clusters.");
            }

            if (!x.IsRectangular())
            {
                throw new DimensionMismatchException("data",
                    "The points matrix should be rectangular. The vector at position {} has a different length than previous ones.");
            }

            var k = K;
            var rows = x.Length;
            var cols = x[0].Length;

            // Perform some iterations of the original k-Means 
            // algorithm if the model has not been initialized
            //
            if (Clusters.Centroids[0] == null)
            {
                base.Learn(x);
            }

            // Initial variables
            var labels = new int[rows];
            var count = new double[k];
            var centroids = Clusters.Centroids;
            var newCentroids = Jagged.Zeros(k, cols);

            Iterations = 0;

            var shouldStop = false;

            // We will solve the problem of assigning N data points 
            // to K clusters where the cost will be their distance.
            munkres = new Munkres(x.Length, x.Length)
            {
                Tolerance = Tolerance
            };

            while (!shouldStop) // Main loop
            {
                Array.Clear(count, 0, count.Length);
                for (var i = 0; i < newCentroids.Length; i++)
                {
                    Array.Clear(newCentroids[i], 0, newCentroids[i].Length);
                }

                // Set the cost matrix for Munkres algorithm
                GetDistances(Distance, x, centroids, k, munkres.CostMatrix);

                munkres.Minimize(); // solve the assignment problem

                // Get the clustering from the assignment
                GetLabels(x, k, munkres.Solution, labels);

                // For each point in the data set,
                for (var i = 0; i < x.Length; i++)
                {
                    // Get the point
                    var point = x[i];
                    var weight = weights[i];

                    // Get the nearest cluster centroid
                    var c = labels[i];

                    if (c >= 0)
                    {
                        // Get the closest cluster centroid
                        var centroid = newCentroids[c];

                        // Increase the cluster's sample counter
                        count[c] += weight;

                        // Accumulate in the cluster centroid
                        for (var j = 0; j < point.Length; j++)
                        {
                            centroid[j] += point[j] * weight;
                        }
                    }
                }

                // Next we will compute each cluster's new centroid
                //  by dividing the accumulated sums by the number of
                //  samples in each cluster, thus averaging its members.
                Parallel.For(0, newCentroids.Length, ParallelOptions, i =>
                {
                    var sum = count[i];

                    if (sum > 0)
                    {
                        for (var j = 0; j < newCentroids[i].Length; j++)
                        {
                            newCentroids[i][j] /= sum;
                        }
                    }
                });

                // The algorithm stops when there is no further change in the
                //  centroids (relative difference is less than the threshold).
                shouldStop = converged(centroids, newCentroids);

                // go to next generation
                for (var i = 0; i < centroids.Length; i++)
                {
                    for (var j = 0; j < centroids[i].Length; j++)
                    {
                        centroids[i][j] = newCentroids[i][j];
                    }
                }
            }

            for (var i = 0; i < Clusters.Centroids.Length; i++)
            {
                // Compute the proportion of samples in the cluster
                Clusters.Proportions[i] = count[i] / weightSum;
            }

            Labels = labels;

            ComputeInformation(x, labels);

            Debug.Assert(Clusters.NumberOfClasses == K);
            Debug.Assert(Clusters.NumberOfOutputs == K);
            Debug.Assert(Clusters.NumberOfInputs == x[0].Length);

            return Clusters;
        }

        internal static void GetLabels(double[][] points, int clusters, double[] solution, int[] labels)
        {
            for (var i = 0; i < points.Length; i++)
            {
                var j = (int)solution[i];
                if (j >= 0)
                {
                    labels[j] = GetIndex(clusters, i);
                }
                else
                {
                    labels[j] = -1;
                }
            }
        }

        internal static double[][] GetDistances(IDistance<double[], double[]> distance, double[][] points,
            double[][] centroids, int k, double[][] result)
        {
            for (var i = 0; i < result.Length; i++)
            {
                for (var j = 0; j < result[i].Length; j++)
                {
                    result[i][j] = distance.Distance(points[j], centroids[GetIndex(k, i)]);
                }
            }

            return result;
        }

        private static int GetIndex(int clusters, int index)
        {
            // Equation 6.6 uses ((a mod k) + 1) instead of (a mod k):
            // http://cs.uef.fi/sipu/pub/PhD_Thesis_Mikko_Malinen.pdf

            return (index + 1) % clusters;
        }
    }
}