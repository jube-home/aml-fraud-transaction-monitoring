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

using Accord.Math.Random;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Neuro
{
    /// <summary>
    ///     Gaussian weight initialization.
    /// </summary>
    public class GaussianWeights
    {
        private readonly ActivationNetwork network;
        private readonly IRandomNumberGenerator<double> random;

        /// <summary>
        ///     Constructs a new Gaussian Weight initialization.
        /// </summary>
        /// <param name="network">The activation network whose weights will be initialized.</param>
        /// <param name="stdDev">
        ///     The standard deviation to be used. Common values lie in the 0.001-
        ///     0.1 range. Default is 0.1.
        /// </param>
        public GaussianWeights(ActivationNetwork network, double stdDev = 0.1)
        {
            this.network = network;

            random = new NormalDistribution(0, stdDev);

            UpdateThresholds = false;
        }

        /// <summary>
        ///     Gets ors sets whether the initialization
        ///     should update neurons thresholds (biases)
        /// </summary>
        public bool UpdateThresholds { get; set; }

        /// <summary>
        ///     Randomizes (initializes) the weights of
        ///     the network using a Gaussian distribution.
        /// </summary>
        public void Randomize()
        {
            foreach (ActivationLayer layer in network.Layers)
            {
                foreach (ActivationNeuron neuron in layer.Neurons)
                {
                    for (var i = 0; i < neuron.Weights.Length; i++)
                    {
                        neuron.Weights[i] = random.Generate();
                    }

                    if (UpdateThresholds)
                    {
                        neuron.Threshold = random.Generate();
                    }
                }
            }
        }

        /// <summary>
        ///     Randomizes (initializes) the weights of
        ///     the network using a Gaussian distribution.
        /// </summary>
        public void Randomize(int layerIndex)
        {
            var layer = network.Layers[layerIndex] as ActivationLayer;

            foreach (ActivationNeuron neuron in layer.Neurons)
            {
                for (var i = 0; i < neuron.Weights.Length; i++)
                {
                    neuron.Weights[i] = random.Generate();
                }

                if (UpdateThresholds)
                {
                    neuron.Threshold = random.Generate();
                }
            }
        }
    }
}