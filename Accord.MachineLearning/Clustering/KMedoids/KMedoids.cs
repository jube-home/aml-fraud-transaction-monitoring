// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza <cesarsouza at gmail.com>
// and other contributors, 2009-2017.
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
using System.Threading;
using System.Threading.Tasks;
using Accord.Diagnostics;
using Accord.Math;
using Accord.Math.Distances;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     k-Medoids clustering using PAM (Partition Around Medoids) algorithm.
    /// </summary>
    /// <remarks>
    ///     <para> From Wikipedia:</para>
    ///     <para>
    ///         The k-medoids algorithm is a clustering algorithm related to the k-means algorithm and the medoidshift
    ///         algorithm. Both the k-means and k-medoids algorithms are partitional (breaking the dataset up into groups)
    ///         and both attempt to minimize the distance between points labeled to be in a cluster and a point designated
    ///         as the center of that cluster. In contrast to the k-means algorithm, k-medoids chooses datapoints as centers
    ///         (medoids or exemplars) and works with a generalization of the Manhattan Norm to define distance between
    ///         datapoints instead of L2. This method was proposed in 1987[1] for the work with L1 norm and other distances.
    ///     </para>
    ///     <para>
    ///         The most common realisation of k-medoid clustering is the Partitioning Around Medoids (PAM) algorithm.
    ///         PAM uses a greedy search which may not find the optimum solution, but it is faster than exhaustive search.
    ///     </para>
    ///     <para>
    ///         [1] Kaufman, L. and Rousseeuw, P.J. (1987), Clustering by means of Medoids, in Statistical Data Analysis
    ///         Based on the L1–Norm and Related Methods, edited by Y. Dodge, North-Holland, 405–416.
    ///     </para>
    /// </remarks>
    /// <seealso cref="KMedoids" />
    /// <seealso cref="KMeans" />
    /// <seealso cref="MeanShift" />
    /// <example>
    ///     How to perform K-Medoids clustering with PAM algorithm.
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Clustering\KMedoidsPAMTest.cs" region="doc_learn" />
    /// </example>
    [Serializable]
    public partial class KMedoids<T> : ParallelLearningBase,
        IUnsupervisedLearning<KMedoidsClusterCollection<T>, T[], int>
    {
        /// <summary>
        ///     Initializes a new instance of PartitioningAroundMedoids algorithm
        /// </summary>
        /// <param name="k">The number of clusters to divide input data.</param>
        /// <param name="distance">
        ///     The distance function to use. Default is to
        ///     use the <see cref="Distance.Euclidean(double[], double[])" /> distance.
        /// </param>
        public KMedoids(int k, IDistance<T[]> distance)
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
            //  information about the k-Medoids' clusters.
            Clusters = new KMedoidsClusterCollection<T>(k, distance);

            Initialization = Seeding.PamBuild;
            Tolerance = 1e-5;
            MaxIterations = 100;
        }

        /// <summary>
        ///     Gets the clusters found by k-Medoids.
        /// </summary>
        public KMedoidsClusterCollection<T> Clusters { get; }

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
        public int Iterations { get; protected set; }

        /// <summary>
        ///     Gets the cluster distortion error (the average distance
        ///     between data points and the cluster centroids) after the
        ///     last call to this class' Compute methods.
        /// </summary>
        public double Error { get; private set; }

        /// <summary>
        ///     Gets or sets the strategy used to initialize the
        ///     centroids of the clustering algorithm. Default is
        ///     <see cref="Seeding.PamBuild" />.
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
        public KMedoidsClusterCollection<T> Learn(T[][] x, double[] weights = null)
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

            // Perform initialization of the clusters
            var currentMedoidIndicesArray = Clusters.Randomize(x, Initialization, ParallelOptions);

            // Detect initial medoid indices
            if (currentMedoidIndicesArray == null)
            {
                currentMedoidIndicesArray = Vector.Create(value: -1, size: K);
                Parallel.For(0, x.Length, ParallelOptions, i =>
                {
                    for (var j = 0; j < Clusters.Centroids.Length; j++)
                    {
                        if (Distance.Distance(Clusters.Centroids[j], x[i]) == 0)
                        {
                            var prev = Interlocked.CompareExchange(ref currentMedoidIndicesArray[j], i, -1);
                            if (prev != -1)
                            {
                                throw new Exception("Duplicate medoid #{0} detected: {1} and {2}".Format(j, prev, i));
                            }

                            break;
                        }
                    }
                });
            }

            for (var i = 0; i < currentMedoidIndicesArray.Length; ++i)
            {
                if (currentMedoidIndicesArray[i] == -1)
                {
                    throw new Exception("Medoid #{0} not found.".Format(i));
                }
            }


            Iterations = 0;

            var labels = new int[x.Length];

            // Special case - one medoid.
            if (K == 1)
            {
                // Arrange point with minimal total cost as medoid.
                var imin = -1;
                var min = double.PositiveInfinity;
                for (var i = 0; i < x.Length; i++)
                {
                    var cost = 0.0;
                    for (var j = 0; j < x.Length; j++)
                    {
                        cost += Distance.Distance(x[i], x[j]);
                    }

                    if (cost < min)
                    {
                        imin = i;
                    }
                }

                Clusters.Centroids[0] = x[imin];
            }
            else
            {
                Compute(x, labels, currentMedoidIndicesArray);
            }

            // Miscellaneous final computations
            if (ComputeError)
            {
                // Compute the average error
#if DEBUG
                var expected = Clusters.Decide(x);
                if (!expected.IsEqual(labels))
                {
                    throw new Exception();
                }
#endif

                Error = Clusters.Distortion(x, labels);
            }

            Debug.Assert(Clusters.NumberOfClasses == K);
            Debug.Assert(Clusters.NumberOfOutputs == K);
            Debug.Assert(Clusters.NumberOfInputs == x[0].Length);

            // Return the classification result
            return Clusters;
        }

        /// <summary>
        ///     Implementation of the PAM algorithm.
        /// </summary>
        protected virtual int[] Compute(T[][] x, int[] labels, int[] currentMedoidIndicesArray)
        {
            var currentMedoidIndices = new HashSet<int>(currentMedoidIndicesArray);
            if (currentMedoidIndices.Count < currentMedoidIndicesArray.Length)
            {
                throw new Exception("Some medoids are not unique.");
            }

            var secondClusterDistance = new double[x.Length];

            for (; Iterations < MaxIterations; Iterations++)
            {
                assignPointsToClusters(x, labels, currentMedoidIndicesArray, secondClusterDistance);

                int minI;
                int minH;
                double minTih;

                findBestSwap(x, labels, currentMedoidIndices, currentMedoidIndicesArray, secondClusterDistance,
                    out minI, out minH, out minTih);

                // Check exit criteria
                if (minTih >= 0.0)
                {
                    break;
                }

                // Swap points
                currentMedoidIndices.Remove(currentMedoidIndicesArray[minI]);
                currentMedoidIndices.Add(minH);
                currentMedoidIndicesArray[minI] = minH;
            }

            // Assign medoids
            for (var i = 0; i < Clusters.Centroids.Length; i++)
            {
                Clusters.Centroids[i] = x[currentMedoidIndicesArray[i]];
            }

            return labels;
        }
    }

    /// <summary>
    ///     k-Medoids clustering using PAM (Partition Around Medoids) algorithm.
    /// </summary>
    /// <remarks>
    ///     <para> From Wikipedia:</para>
    ///     <para>
    ///         The k-medoids algorithm is a clustering algorithm related to the k-means algorithm and the medoidshift
    ///         algorithm. Both the k-means and k-medoids algorithms are partitional (breaking the dataset up into groups)
    ///         and both attempt to minimize the distance between points labeled to be in a cluster and a point designated
    ///         as the center of that cluster. In contrast to the k-means algorithm, k-medoids chooses datapoints as centers
    ///         (medoids or exemplars) and works with a generalization of the Manhattan Norm to define distance between
    ///         datapoints instead of L2. This method was proposed in 1987[1] for the work with L1 norm and other distances.
    ///     </para>
    ///     <para>
    ///         The most common realisation of k-medoid clustering is the Partitioning Around Medoids (PAM) algorithm.
    ///         PAM uses a greedy search which may not find the optimum solution, but it is faster than exhaustive search.
    ///     </para>
    ///     <para>
    ///         [1] Kaufman, L. and Rousseeuw, P.J. (1987), Clustering by means of Medoids, in Statistical Data Analysis
    ///         Based on the L1–Norm and Related Methods, edited by Y. Dodge, North-Holland, 405–416.
    ///     </para>
    ///     <para>
    ///         This is the specialized, non-generic version of the k-Medoids algorithm
    ///         that is set to work on <c>double[]</c> arrays.
    ///     </para>
    /// </remarks>
    /// <seealso cref="KMedoids{T}" />
    [Serializable]
    public class KMedoids : KMedoids<double>
    {
        /// <summary>
        ///     Initializes a new instance of k-Medoids algorithm
        /// </summary>
        /// <param name="k">The number of clusters to divide input data.</param>
        public KMedoids(int k)
            : base(k, new Euclidean())
        {
        }
    }
}