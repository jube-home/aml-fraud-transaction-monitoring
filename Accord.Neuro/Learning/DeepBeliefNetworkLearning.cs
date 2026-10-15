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
using Accord.Neuro.Layers;
using Accord.Neuro.Networks;

namespace Accord.Neuro.Learning
{
    /// <summary>
    ///     Delegate used to configure and create layer-specific learning algorithms.
    /// </summary>
    /// <param name="hiddenLayer">The hidden layer being trained.</param>
    /// <param name="visibleLayer">The visible layer being trained.</param>
    /// <param name="index">The layer-pair index in the deep network.</param>
    /// <returns>
    ///     The function should return an instance of the algorithm
    ///     which should be used to train the pair of layers.
    /// </returns>
    public delegate IUnsupervisedLearning RestrictedBoltzmannNetworkLearningConfigurationFunction(
        StochasticLayer hiddenLayer, StochasticLayer visibleLayer, int index);

    /// <summary>
    ///     Deep Belief Network learning algorithm.
    /// </summary>
    public class DeepBeliefNetworkLearning : IUnsupervisedLearning
    {
        private readonly DeepBeliefNetwork network;

        private IUnsupervisedLearning[] algorithms;
        private RestrictedBoltzmannNetworkLearningConfigurationFunction configure;

        private int layerIndex;

        /// <summary>
        ///     Creates a new <see cref="DeepBeliefNetworkLearning" /> algorithm.
        /// </summary>
        /// <param name="network">The network to be trained.</param>
        public DeepBeliefNetworkLearning(DeepBeliefNetwork network)
        {
            this.network = network;
        }

        /// <summary>
        ///     Gets or sets the configuration function used
        ///     to specify and create the learning algorithms
        ///     for each of the layers of the deep network.
        /// </summary>
        public RestrictedBoltzmannNetworkLearningConfigurationFunction Algorithm
        {
            get => configure;
            set
            {
                configure = value;
                createAlgorithms();
            }
        }

        /// <summary>
        ///     Gets or sets the current layer index being
        ///     trained by the deep learning algorithm.
        /// </summary>
        public int LayerIndex
        {
            get => layerIndex;
            set
            {
                if (value < 0 || value >= network.Machines.Count)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                layerIndex = value;
            }
        }

        /// <summary>
        ///     Runs a single learning iteration.
        /// </summary>
        /// <param name="input">A single input vector.</param>
        /// <returns>
        ///     Returns the learning error after the iteration.
        /// </returns>
        public double Run(double[] input)
        {
            // Get layer learning algorithm
            var teacher = algorithms[layerIndex];

            // Learn the layer using data
            return teacher.Run(input);
        }

        /// <summary>
        ///     Runs a single batch epoch
        ///     of the learning algorithm.
        /// </summary>
        /// <param name="input">Array of input vectors.</param>
        /// <returns>
        ///     Returns sum of learning errors.
        /// </returns>
        public double RunEpoch(double[][] input)
        {
            // Get layer learning algorithm
            var teacher = algorithms[layerIndex];

            // Learn the layer using data
            return teacher.RunEpoch(input);
        }

        private void createAlgorithms()
        {
            algorithms = new IUnsupervisedLearning[network.Machines.Count];
            for (var i = 0; i < network.Machines.Count; i++)
            {
                var hidden = network.Machines[i].Hidden;
                var visible = network.Machines[i].Visible;
                algorithms[i] = configure(hidden, visible, i);
            }
        }

        /// <summary>
        ///     Gets the learning data needed to train the
        ///     <see cref="LayerIndex">
        ///         currently
        ///         selected layer
        ///     </see>
        ///     . The return of this function should then be passed to
        ///     <see cref="RunEpoch(double[][])" /> to actually run a learning epoch.
        /// </summary>
        /// <param name="input">The batch of input data.</param>
        /// <returns>The learning data for the current layer.</returns>
        public double[][] GetLayerInput(double[][] input)
        {
            return GetLayerInput(new[] { input })[0];
        }

        /// <summary>
        ///     Gets the learning data needed to train the
        ///     <see cref="LayerIndex">
        ///         currently
        ///         selected layer
        ///     </see>
        ///     . The return of this function should then be passed to
        ///     <see cref="RunEpoch(double[][])" /> to actually run a learning epoch.
        /// </summary>
        /// <param name="batches">The mini-batches of input data.</param>
        /// <returns>The learning data for the current layer.</returns>
        public double[][][] GetLayerInput(double[][][] batches)
        {
            if (layerIndex == 0)
            {
                return batches;
            }

            var outputBatches = new double[batches.Length][][];

            for (var j = 0; j < batches.Length; j++)
            {
                var batchSize = batches[j].Length;

                var inputs = batches[j];
                var outputs = new double[batchSize][];

                for (var i = 0; i < inputs.Length; i++)
                {
                    network.Compute(inputs[i]); // double[] responses = 
                    outputs[i] = network.Machines[layerIndex - 1].Hidden.Output;
                }

                outputBatches[j] = outputs;
            }

            return outputBatches;
        }

        /// <summary>
        ///     Gets the
        ///     <see cref="IUnsupervisedLearning">
        ///         unsupervised
        ///         learning algorithm
        ///     </see>
        ///     allocated for the given layer.
        /// </summary>
        /// <param name="layerIndex">The index of the layer to get the algorithm for.</param>
        public IUnsupervisedLearning GetLayerAlgorithm(int layerIndex)
        {
            return algorithms[layerIndex];
        }

        /// <summary>
        ///     Runs a single learning epoch using
        ///     multiple mini-batches to improve speed.
        /// </summary>
        /// <param name="batches">Array of input batches.</param>
        /// <returns>
        ///     Returns sum of learning errors.
        /// </returns>
        public double RunEpoch(double[][][] batches)
        {
            // Get layer learning algorithm
            var teacher = algorithms[layerIndex];

            // Learn the layer using data
            double error = 0;
            for (var i = 0; i < batches.Length; i++)
            {
                error += teacher.RunEpoch(batches[i]);
            }

            return error;
        }

        /// <summary>
        ///     Computes the reconstruction error for
        ///     a given set of input values.
        /// </summary>
        /// <param name="inputs">The input values.</param>
        /// <returns>The squared reconstruction error.</returns>
        public double ComputeError(double[][] inputs)
        {
            double error = 0;
            for (var i = 0; i < inputs.Length; i++)
            {
                var output = network.Compute(inputs[i]);
                var reconstruct = network.Reconstruct(output);

                for (var j = 0; j < inputs[i].Length; j++)
                {
                    var e = reconstruct[j] - inputs[i][j];
                    error += e * e;
                }
            }

            return error;
        }
    }
}