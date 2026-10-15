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
using Accord.Neuro.ActivationFunctions;
using Accord.Neuro.Layers;

namespace Accord.Neuro.Networks
{
    /// <summary>
    ///     Deep Belief Network.
    /// </summary>
    /// <remarks>
    ///     The Deep Belief Network can be seen as a collection of stacked
    ///     <see cref="RestrictedBoltzmannMachine">
    ///         Restricted Boltzmann
    ///         Machines
    ///     </see>
    ///     disposed as layers of a network. In turn, the
    ///     whole network can be seen as an stochastic activation network
    ///     in which the neurons activate within some given probability.
    /// </remarks>
    [Serializable]
    public class DeepBeliefNetwork : ActivationNetwork
    {
        private List<RestrictedBoltzmannMachine> machines;

        /// <summary>
        ///     Creates a new <see cref="DeepBeliefNetwork" />.
        /// </summary>
        /// <param name="inputsCount">The number of inputs for the network.</param>
        /// <param name="hiddenNeurons">The number of hidden neurons in each layer.</param>
        public DeepBeliefNetwork(int inputsCount, params int[] hiddenNeurons)
            : this(new BernoulliFunction(1), inputsCount, hiddenNeurons)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="DeepBeliefNetwork" />.
        /// </summary>
        /// <param name="function">The activation function to be used in the network neurons.</param>
        /// <param name="inputsCount">The number of inputs for the network.</param>
        /// <param name="hiddenNeurons">The number of hidden neurons in each layer.</param>
        public DeepBeliefNetwork(IStochasticFunction function, int inputsCount, params int[] hiddenNeurons)
            : base(function, inputsCount, null, hiddenNeurons)
        {
            machines = new List<RestrictedBoltzmannMachine>();

            // Create first layer
            machines.Add(new RestrictedBoltzmannMachine(
                new StochasticLayer(function, hiddenNeurons[0], inputsCount),
                new StochasticLayer(function, inputsCount, hiddenNeurons[0]))
            );

            // Create other layers
            for (var i = 1; i < hiddenNeurons.Length; i++)
            {
                machines.Add(new RestrictedBoltzmannMachine(
                    new StochasticLayer(function, hiddenNeurons[i], hiddenNeurons[i - 1]),
                    new StochasticLayer(function, hiddenNeurons[i - 1], hiddenNeurons[i])));
            }

            // Override AForge layers
            layers = new Layer[machines.Count];
            for (var i = 0; i < layers.Length; i++)
            {
                layers[i] = machines[i].Hidden;
            }
        }

        /// <summary>
        ///     Creates a new <see cref="DeepBeliefNetwork" />.
        /// </summary>
        /// <param name="inputsCount">The number of inputs for the network.</param>
        /// <param name="layers">The layers to add to the deep network.</param>
        public DeepBeliefNetwork(int inputsCount, params RestrictedBoltzmannMachine[] layers)
            : base(null, inputsCount, null, new int[layers.Length])
        {
            machines = new List<RestrictedBoltzmannMachine>(layers);

            // Override AForge layers
            this.layers = new Layer[machines.Count];
            for (var i = 0; i < layers.Length; i++)
            {
                this.layers[i] = machines[i].Hidden;
            }
        }

        /// <summary>
        ///     Gets the number of output neurons in the network
        ///     (the size of the computed output vectors).
        /// </summary>
        public int OutputCount
        {
            get
            {
                if (machines.Count == 0)
                {
                    return 0;
                }

                return machines[machines.Count - 1].Hidden.Neurons.Length;
            }
        }

        /// <summary>
        ///     Gets the Restricted Boltzmann Machines
        ///     on each layer of this deep network.
        /// </summary>
        public IList<RestrictedBoltzmannMachine> Machines => machines;

        /// <summary>
        ///     Computes the network's outputs for a given input.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <returns>
        ///     Returns the network's output for the given input.
        /// </returns>
        public override double[] Compute(double[] input)
        {
            var output = input;

            foreach (var layer in machines)
            {
                output = layer.Hidden.Compute(output);
            }

            return output;
        }

        /// <summary>
        ///     Computes the network's outputs for a given input.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="layerIndex">The index of the layer.</param>
        /// <returns>
        ///     Returns the network's output for the given input.
        /// </returns>
        public double[] Compute(double[] input, int layerIndex)
        {
            var output = input;

            for (var i = 0; i <= layerIndex; i++)
            {
                output = machines[i].Hidden.Compute(output);
            }

            return output;
        }

        /// <summary>
        ///     Reconstructs a input vector for a given output.
        /// </summary>
        /// <param name="output">The output vector.</param>
        /// <returns>
        ///     Returns a probable input vector which may
        ///     have originated the given output.
        /// </returns>
        public double[] Reconstruct(double[] output)
        {
            var input = output;

            for (var i = machines.Count - 1; i >= 0; i--)
            {
                input = machines[i].Visible.Compute(input);
            }

            return input;
        }

        /// <summary>
        ///     Reconstructs a input vector using the output
        ///     vector of a given layer.
        /// </summary>
        /// <param name="output">The output vector.</param>
        /// <param name="layerIndex">The index of the layer.</param>
        /// <returns>
        ///     Returns a probable input vector which may
        ///     have originated the given output in the
        ///     indicated layer.
        /// </returns>
        public double[] Reconstruct(double[] output, int layerIndex)
        {
            var input = output;

            for (var i = layerIndex; i >= 0; i--)
            {
                input = machines[i].Visible.Compute(input);
            }

            return input;
        }

        /// <summary>
        ///     Samples an output vector from the network
        ///     given an input vector.
        /// </summary>
        /// <param name="input">An input vector.</param>
        /// <returns>
        ///     A possible output considering the
        ///     stochastic activations of the network.
        /// </returns>
        public double[] GenerateOutput(double[] input)
        {
            var output = input;

            foreach (var layer in machines)
            {
                output = layer.Hidden.Generate(output);
            }

            return output;
        }

        /// <summary>
        ///     Samples an output vector from the network
        ///     given an input vector.
        /// </summary>
        /// <param name="input">An input vector.</param>
        /// <param name="layerIndex">The index of the layer.</param>
        /// <returns>
        ///     A possible output considering the
        ///     stochastic activations of the network.
        /// </returns>
        public double[] GenerateOutput(double[] input, int layerIndex)
        {
            var output = input;

            for (var i = 0; i <= layerIndex; i++)
            {
                output = machines[i].Hidden.Generate(output);
            }

            return output;
        }

        /// <summary>
        ///     Samples an input vector from the network
        ///     given an output vector.
        /// </summary>
        /// <param name="output">An output vector.</param>
        /// <returns>
        ///     A possible reconstruction considering the
        ///     stochastic activations of the network.
        /// </returns>
        public double[] GenerateInput(double[] output)
        {
            var input = output;

            for (var i = layers.Length - 1; i >= 0; i--)
            {
                input = machines[i].Visible.Generate(input);
            }

            return input;
        }

        /// <summary>
        ///     Inserts a new layer at the end of this network.
        /// </summary>
        /// <param name="neurons">The number of neurons in the new layer.</param>
        public void Push(int neurons)
        {
            Push(neurons, new BernoulliFunction(1));
        }

        /// <summary>
        ///     Inserts a new layer at the end of this network.
        /// </summary>
        /// <param name="neurons">The number of neurons in the new layer.</param>
        /// <param name="function">The activation function which should be used by the neurons.</param>
        public void Push(int neurons, IStochasticFunction function)
        {
            Push(neurons, function, function);
        }

        /// <summary>
        ///     Inserts a new layer at the end of this network.
        /// </summary>
        /// <param name="neurons">The number of neurons in the layer.</param>
        /// <param name="visibleFunction">The activation function which should be used by the visible neurons.</param>
        /// <param name="hiddenFunction">The activation function which should be used by the hidden neurons.</param>
        public void Push(int neurons, IStochasticFunction visibleFunction, IStochasticFunction hiddenFunction)
        {
            int lastLayerNeurons;

            if (machines.Count > 0)
            {
                lastLayerNeurons = machines[machines.Count - 1].Hidden.Neurons.Length;
            }
            else
            {
                lastLayerNeurons = inputsCount;
            }

            machines.Add(new RestrictedBoltzmannMachine(
                new StochasticLayer(hiddenFunction, neurons, lastLayerNeurons),
                new StochasticLayer(visibleFunction, lastLayerNeurons, neurons)));

            // Override AForge layers
            layers = new Layer[machines.Count];
            for (var i = 0; i < layers.Length; i++)
            {
                layers[i] = machines[i].Hidden;
            }
        }

        /// <summary>
        ///     Stacks a new Boltzmann Machine at the end of this network.
        /// </summary>
        /// <param name="network">The machine to be added to the network.</param>
        public void Push(RestrictedBoltzmannMachine network)
        {
            machines.Add(network);

            // Override AForge layers
            layers = new Layer[machines.Count];
            for (var i = 0; i < layers.Length; i++)
            {
                layers[i] = machines[i].Hidden;
            }
        }

        /// <summary>
        ///     Removes the last layer from the network.
        /// </summary>
        public void Pop()
        {
            if (machines.Count == 0)
            {
                return;
            }

            machines.RemoveAt(machines.Count - 1);

            // Override AForge layers
            layers = new Layer[machines.Count];
            for (var i = 0; i < layers.Length; i++)
            {
                layers[i] = machines[i].Hidden;
            }
        }

        /// <summary>
        ///     Updates the weights of the visible layers by copying
        ///     the reverse of the weights in the hidden layers.
        /// </summary>
        public void UpdateVisibleWeights()
        {
            foreach (var machine in machines)
            {
                machine.UpdateVisibleWeights();
            }
        }

        /// <summary>
        ///     Creates a Gaussian-Bernoulli network.
        /// </summary>
        /// <param name="inputsCount">The number of inputs for the network.</param>
        /// <param name="hiddenNeurons">The number of hidden neurons in each layer.</param>
        public static DeepBeliefNetwork CreateGaussianBernoulli(int inputsCount, params int[] hiddenNeurons)
        {
            var network = new DeepBeliefNetwork(inputsCount, hiddenNeurons);

            var gaussian = new GaussianFunction();
            foreach (var neuron in network.machines[0].Visible.Neurons)
            {
                neuron.ActivationFunction = gaussian;
            }

            return network;
        }

        /// <summary>
        ///     Creates a Mixed-Bernoulli network.
        /// </summary>
        /// <param name="visible">The <see cref="IStochasticFunction" /> to be used in the first visible layer.</param>
        /// <param name="hidden">The <see cref="IStochasticFunction" /> to be used in all other layers.</param>
        /// <param name="inputsCount">The number of inputs for the network.</param>
        /// <param name="hiddenNeurons">The number of hidden neurons in each layer.</param>
        public static DeepBeliefNetwork CreateMixedNetwork(IStochasticFunction visible,
            IStochasticFunction hidden, int inputsCount, params int[] hiddenNeurons)
        {
            var network = new DeepBeliefNetwork(hidden, inputsCount, hiddenNeurons);

            foreach (var neuron in network.machines[0].Visible.Neurons)
            {
                neuron.ActivationFunction = visible;
            }

            return network;
        }
    }
}