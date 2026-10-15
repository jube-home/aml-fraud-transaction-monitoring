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
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Math.Distances;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     K-Nearest Neighbor (k-NN) algorithm.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <remarks>
    ///     <para>
    ///         The k-nearest neighbor algorithm (k-NN) is a method for classifying objects
    ///         based on closest training examples in the feature space. It is amongst the simplest
    ///         of all machine learning algorithms: an object is classified by a majority vote of
    ///         its neighbors, with the object being assigned to the class most common amongst its
    ///         k nearest neighbors (k is a positive integer, typically small).
    ///     </para>
    ///     <para>If k = 1, then the object is simply assigned to the class of its nearest neighbor.</para>
    ///     <note type="note">
    ///         When learning a model with instance weights, the weights will not be used when
    ///         finding the <c>k</c> nearest neighbors of a query point. Instead, it will be used
    ///         to weight the similarity between the query point and each of its <c>k</c> nearest
    ///         neighbors when deciding for the queried point's class.
    ///     </note>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia contributors. "K-nearest neighbor algorithm." Wikipedia, The
    ///                     Free Encyclopedia. Wikipedia, The Free Encyclopedia, 10 Oct. 2012. Web.
    ///                     9 Nov. 2012. http://en.wikipedia.org/wiki/K-nearest_neighbor_algorithm
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The first example shows how to create and use a k-Nearest Neighbor algorithm to classify
    ///         a set of numeric vectors in a multi-class decision problem involving 3 classes. It also shows
    ///         how to compute class decisions for a new sample and how to measure the performance of a classifier.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\KNearestNeighbors\KNearestNeighborsTest.cs"
    ///         region="doc_learn" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\KNearestNeighbors\KNearestNeighborsTest.cs"
    ///         region="doc_serialization" />
    ///     <para>
    ///         The second example show how to use a different distance metric when computing k-NN:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\KNearestNeighbors\KNearestNeighborsTest.cs"
    ///         region="doc_learn_distance" />
    ///     <para>
    ///         The k-Nearest neighbor algorithm implementation in the framework can also be used with any instance
    ///         data type. For such cases, the framework offers a generic version of the classifier. The third example
    ///         shows how to use the generic kNN classifier to perform the direct classification of actual text samples:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\KNearestNeighbors\KNearestNeighborsTest.cs"
    ///         region="doc_learn_text" />
    /// </example>
    /// <seealso cref="KNearestNeighbors" />
    [Serializable]
    public class KNearestNeighbors<TInput> :
        BaseKNearestNeighbors<KNearestNeighbors<TInput>, TInput, IDistance<TInput>>,
        IParallel
    {
        // TODO: After removing obsolete methods, mark class as sealed

        [NonSerialized] private ThreadLocal<double[]> distanceCache = new();

        [NonSerialized] private ParallelOptions parallelOptions = new();

        /// <summary>
        ///     Gets or sets the parallelization options for this algorithm.
        /// </summary>
        public ParallelOptions ParallelOptions
        {
            get => parallelOptions;
            set => parallelOptions = value;
        }

        /// <summary>
        ///     Creates a new <see cref="KNearestNeighbors" />.
        /// </summary>
        public KNearestNeighbors()
        {
        }

        /// <summary>
        ///     Creates a new <see cref="KNearestNeighbors" />.
        /// </summary>
        public KNearestNeighbors(int k, IDistance<TInput> distance)
        {
            K = k;
            Distance = distance;
        }

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and each class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the result will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        /// <returns>System.Double[].</returns>
        public override double[] Scores(TInput input, double[] result)
        {
            double[] distances;
            var idx = getNearestIndices(input, out distances);

            // Compute the scores for these points
            for (var i = 0; i < idx.Length; i++)
            {
                var j = idx[i];

                var label = Outputs[j];
                var weight = Weights[j];
                var d = distances[i]; // distances vector is already in order because
                // the bottom operation below was done in-place

                // Convert to similarity measure
                result[label] += 1.0 / (1.0 + d) * weight;
            }

            return result;
        }

#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int[] getNearestIndices(TInput input, out double[] distances)
        {
            var d = distanceCache.Value;

            if (parallelOptions.MaxDegreeOfParallelism == 1)
            {
                // Compute all distances
                for (var i = 0; i < Inputs.Length; i++)
                {
                    d[i] = Distance.Distance(input, Inputs[i]);
                }
            }
            else
            {
                // Compute all distances
                Parallel.For(0, Inputs.Length, parallelOptions, i => { d[i] = Distance.Distance(input, Inputs[i]); });
            }

            distances = d;

            // Get the K closest points
            return d.Bottom(K, true);
        }

        /// <summary>
        ///     Gets the top <see cref="BaseKNearestNeighbors{TModel, TInput, TDistance}.K" /> points
        ///     that are the closest to a given <paramref name="input"> reference point</paramref>.
        /// </summary>
        /// <param name="input">The query point whose neighbors will be found.</param>
        /// <param name="labels">The label for each neighboring point.</param>
        /// <returns>
        ///     An array containing the top <see cref="BaseKNearestNeighbors{TModel, TInput, TDistance}.K" /> points that are
        ///     at the closest possible distance to <paramref name="input" />.
        /// </returns>
        public override TInput[] GetNearestNeighbors(TInput input, out int[] labels)
        {
            var idx = getNearestIndices(input, out _);

            labels = Outputs.Get(idx);
            return Inputs.Get(idx);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        public override KNearestNeighbors<TInput> Learn(TInput[] x, int[] y, double[] weights = null)
        {
            CheckArgs(K, x, y, Distance, weights);

            Inputs = x;
            Outputs = y;
            Weights = weights ?? Vector.Ones(x.Length);

            NumberOfInputs = GetNumberOfInputs(x);
            NumberOfOutputs = y.DistinctCount();
            NumberOfClasses = NumberOfOutputs;
            distanceCache = new ThreadLocal<double[]>(() => new double[Inputs.Length]);

            return this;
        }

        [OnDeserialized]
        private void SetValuesOnDeserialized(StreamingContext context)
        {
            distanceCache = new ThreadLocal<double[]>(() => new double[Inputs.Length]);
            parallelOptions = new ParallelOptions();
            if (Weights == null && Inputs != null)
            {
                Weights = Vector.Ones(Inputs.Length);
            }
        }

        #region Obsolete

        /// <summary>
        ///     Creates a new <see cref="KNearestNeighbors" />.
        /// </summary>
        /// <param name="k">The number of nearest neighbors to be used in the decision.</param>
        /// <param name="inputs">The input data points.</param>
        /// <param name="outputs">The associated labels for the input points.</param>
        /// <param name="distance">The distance measure to use in the decision.</param>
        [Obsolete("Please use KNearestNeighbors(int k, IDistance<T> distance) constructor instead.")]
        public KNearestNeighbors(int k, TInput[] inputs, int[] outputs, IDistance<TInput> distance)
        {
            K = k;
            Distance = distance;
            Learn(inputs, outputs);
        }

        /// <summary>
        ///     Creates a new <see cref="KNearestNeighbors" />.
        /// </summary>
        /// <param name="k">The number of nearest neighbors to be used in the decision.</param>
        /// <param name="classes">The number of classes in the classification problem.</param>
        /// <param name="inputs">The input data points.</param>
        /// <param name="outputs">The associated labels for the input points.</param>
        /// <param name="distance">The distance measure to use in the decision.</param>
        [Obsolete("Please use KNearestNeighbors(int k, IDistance<T> distance) constructor instead.")]
        public KNearestNeighbors(int k, int classes, TInput[] inputs, int[] outputs, IDistance<TInput> distance)
            : this(k, inputs, outputs, distance)
        {
            if (classes != NumberOfOutputs)
            {
                throw new ArgumentException(
                    "The number of classes does not match the number of outputs learned from the data.", "classes");
            }
        }

        /// <summary>
        ///     Gets the number of class labels
        ///     handled by this classifier.
        /// </summary>
        [Obsolete("Please use NumberOfOutputs instead.")]
        public int ClassCount => NumberOfOutputs;

        /// <summary>
        ///     Computes the most likely label of a new given point.
        /// </summary>
        /// <param name="input">A point to be classified.</param>
        /// <returns>The most likely label for the given point.</returns>
        [Obsolete("Please use the Decide(input) method instead.")]
        public int Compute(TInput input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Computes the most likely label of a new given point.
        /// </summary>
        /// <param name="input">A point to be classified.</param>
        /// <param name="response">
        ///     A value between 0 and 1 giving
        ///     the strength of the classification in relation to the
        ///     other classes.
        /// </param>
        /// <returns>The most likely label for the given point.</returns>
        [Obsolete("Please use the Score(input, out decision) method instead.")]
        public int Compute(TInput input, out double response)
        {
            int decision;
            response = Score(input, out decision);
            return decision;
        }

        /// <summary>
        ///     Computes the most likely label of a new given point.
        /// </summary>
        /// <param name="input">A point to be classified.</param>
        /// <param name="scores">The distance score for each possible class.</param>
        /// <returns>The most likely label for the given point.</returns>
        [Obsolete("Please use the Scores(input, out decision) method instead.")]
        public virtual int Compute(TInput input, out double[] scores)
        {
            int decision;
            scores = Scores(input, out decision);
            return decision;
        }

        #endregion
    }
}