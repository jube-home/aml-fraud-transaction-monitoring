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

using Accord.Statistics.Distributions.Univariate;

namespace Accord.Neuro
{
    /// <summary>
    ///     Nguyen-Widrow weight initialization.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Nguyen-Widrow initialization algorithm chooses values in
    ///         order to distribute the active region of each neuron in the layer
    ///         approximately evenly across the layers' input space.
    ///     </para>
    ///     <para>
    ///         The values contain a degree of randomness, so they are not the
    ///         same each time this function is called.
    ///     </para>
    /// </remarks>
    public class NguyenWidrow
    {
        private readonly double beta;
        private readonly ActivationNetwork network;
        private readonly Range randRange;

        /// <summary>
        ///     Constructs a new Nguyen-Widrow Weight initialization.
        /// </summary>
        /// <param name="network">The activation network whose weights will be initialized.</param>
        public NguyenWidrow(ActivationNetwork network)
        {
            this.network = network;

            var hiddenNodes = network.Layers[0].Neurons.Length;
            var inputNodes = network.Layers[0].InputsCount;

            randRange = new Range(-0.5f, 0.5f);
            beta = 0.7 * System.Math.Pow(hiddenNodes, 1.0 / inputNodes);
        }

        /// <summary>
        ///     Randomizes (initializes) the weights of
        ///     the network using Nguyen-Widrow method's.
        /// </summary>
        public void Randomize(int layerIndex)
        {
            for (var j = 0; j < network.Layers[layerIndex].Neurons.Length; j++)
            {
                var neuron = network.Layers[layerIndex].Neurons[j] as ActivationNeuron;
                neuron.RandGenerator = new UniformContinuousDistribution(randRange);
                neuron.Randomize();

                var norm = 0.0;

                // Calculate the Euclidean Norm for the weights
                for (var k = 0; k < neuron.Weights.Length; k++)
                {
                    norm += neuron.Weights[k] * neuron.Weights[k];
                }

                norm += neuron.Threshold * neuron.Threshold;

                norm = System.Math.Sqrt(norm);

                // Rescale the weights using beta and the norm
                for (var k = 0; k < neuron.InputsCount; k++)
                {
                    neuron.Weights[k] = beta * neuron.Weights[k] / norm;
                }

                neuron.Threshold = beta * neuron.Threshold / norm;
            }
        }

        /// <summary>
        ///     Randomizes (initializes) the weights of
        ///     the network using a Gaussian distribution.
        /// </summary>
        public void Randomize()
        {
            for (var i = 0; i < network.Layers.Length; i++)
            {
                Randomize(i);
            }
        }
    }
}