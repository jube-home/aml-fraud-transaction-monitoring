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
using System.Threading;
using Accord.Math;
using Accord.Math.Distances;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Base class for <see cref="KNearestNeighbors{TInput}">K-Nearest Neighbor (k-NN) algorithms</see>.
    /// </summary>
    /// <typeparam name="TModel">The type of the model being learned.</typeparam>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <typeparam name="TDistance">The type for distance functions that can be used with this algorithm.</typeparam>
    /// <seealso cref="KNearestNeighbors" />
    /// <seealso cref="KNearestNeighbors{T}" />
    [Serializable]
    public abstract class BaseKNearestNeighbors<TModel, TInput, TDistance> : MulticlassScoreClassifierBase<TInput>,
        ISupervisedMulticlassLearning<TModel, TInput>
        where TModel : BaseKNearestNeighbors<TModel, TInput, TDistance>
        where TDistance : IDistance<TInput>
    {
        private TDistance distance;

        private TInput[] inputs;
        private int k = 5;
        private int[] outputs;

        [NonSerialized] private CancellationToken token;
        private double[] weights;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseKNearestNeighbors{TModel, TInput, TDistance}" /> class.
        /// </summary>
        public BaseKNearestNeighbors()
        {
        }

        /// <summary>
        ///     Gets or sets the distance function used
        ///     as a distance metric between data points.
        /// </summary>
        public TDistance Distance
        {
            get => distance;
            set => distance = value;
        }

        /// <summary>
        ///     Gets or sets the number of nearest neighbors to be used
        ///     in the decision. Default is 5.
        /// </summary>
        /// <value>The number of neighbors.</value>
        public int K
        {
            get => k;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value",
                        "The value for k should be greater than zero and less than total number of input points.");
                }

                k = value;
            }
        }

        /// <summary>
        ///     Gets the set of points given as input of the algorithm.
        /// </summary>
        /// <value>The input points.</value>
        public TInput[] Inputs
        {
            get => inputs;
            protected set => inputs = value;
        }

        /// <summary>
        ///     Gets the set of labels associated with each <see cref="Inputs" /> point.
        /// </summary>
        public int[] Outputs
        {
            get => outputs;
            protected set => outputs = value;
        }

        /// <summary>
        ///     Gets the weights associated with each <see cref="Inputs">input</see>-<see cref="Outputs">output</see> pair.
        /// </summary>
        public double[] Weights
        {
            get => weights;
            protected set => weights = value;
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        public CancellationToken Token
        {
            get => token;
            set => token = value;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        public TModel Learn(TInput[] x, int[][] y, double[] weights = null)
        {
            return Learn(x, y.ArgMax(0), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        public TModel Learn(TInput[] x, bool[][] y, double[] weights = null)
        {
            return Learn(x, y.ArgMax(0), weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.</returns>
        public abstract TModel Learn(TInput[] x, int[] y, double[] weights = null);

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and a given
        ///     <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <returns>System.Double.</returns>
        public override double Score(TInput input, int classIndex)
        {
            return Scores(input)[classIndex];
        }

        /// <summary>
        ///     Gets the top <see cref="BaseKNearestNeighbors{TModel, TInput, TDistance}.K" /> points that are the closest
        ///     to a given <paramref name="input"> reference point</paramref>.
        /// </summary>
        /// <param name="input">The query point whose neighbors will be found.</param>
        /// <param name="labels">The label for each neighboring point.</param>
        /// <returns>
        ///     An array containing the top <see cref="BaseKNearestNeighbors{TModel, TInput, TDistance}.K" /> points that are
        ///     at the closest possible distance to <paramref name="input" />.
        /// </returns>
        public abstract TInput[] GetNearestNeighbors(TInput input, out int[] labels);

        internal static void CheckArgs(int k, TInput[] inputs, int[] outputs, IDistance<TInput> distance,
            double[] weights)
        {
            if (inputs == null)
            {
                throw new ArgumentNullException("inputs");
            }

            if (outputs == null)
            {
                throw new ArgumentNullException("inputs");
            }

            if (inputs.Length != outputs.Length)
            {
                throw new DimensionMismatchException("outputs",
                    "The number of input vectors should match the number of corresponding output labels");
            }

            if (k <= 0 || k > inputs.Length)
            {
                throw new ArgumentOutOfRangeException("k",
                    "The value for k should be greater than zero and less than total number of input points.");
            }

            if (distance == null)
            {
                throw new ArgumentNullException("distance");
            }
        }

        internal int GetNumberOfInputs(TInput[] x)
        {
            var first = x[0] as IList;
            if (first == null)
            {
                return 0;
            }

            var length = first.Count;

            for (var i = 0; i < x.Length; i++)
            {
                var l = x[i] as IList;
                if (l == null || l.Count != length)
                {
                    return 0;
                }
            }

            return length;
        }
    }
}