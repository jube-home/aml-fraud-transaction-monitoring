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
using Accord.Neuro.ActivationFunctions;
using Accord.Neuro.Layers;

namespace Accord.Neuro.Networks
{
    /// <summary>
    ///     Restricted Boltzmann Machine.
    /// </summary>
    /// <example>
    ///     <code>
    ///   // Create some sample inputs and outputs. Note that the
    ///   // first four vectors belong to one class, and the other
    ///   // four belong to another (you should see that the 1s
    ///   // accumulate on the beginning for the first four vectors
    ///   // and on the end for the second four).
    ///   
    ///   double[][] inputs =
    ///   {
    ///       new double[] { 1,1,1, 0,0,0 }, // class a
    ///       new double[] { 1,0,1, 0,0,0 }, // class a
    ///       new double[] { 1,1,1, 0,0,0 }, // class a
    ///       new double[] { 0,0,1, 1,1,0 }, // class b
    ///       new double[] { 0,0,1, 1,0,0 }, // class b
    ///       new double[] { 0,0,1, 1,1,0 }, // class b
    ///   };
    ///   
    ///   double[][] outputs =
    ///   {
    ///       new double[] { 1, 0 }, // indicates the inputs at this
    ///       new double[] { 1, 0 }, // position belongs to class a
    ///       new double[] { 1, 0 },
    ///       new double[] { 0, 1 }, // indicates the inputs at this
    ///       new double[] { 0, 1 }, // position belongs to class b
    ///       new double[] { 0, 1 },
    ///   };
    ///   
    ///   // Create a Bernoulli activation function
    ///   var function = new BernoulliFunction(alpha: 0.5);
    ///   
    ///   // Create a Restricted Boltzmann Machine for 6 inputs and with 1 hidden neuron
    ///   var rbm = new RestrictedBoltzmannMachine(function, inputsCount: 6, hiddenNeurons: 2);
    ///   
    ///   // Create the learning algorithm for RBMs
    ///   var teacher = new ContrastiveDivergenceLearning(rbm)
    ///   {
    ///       Momentum = 0,
    ///       LearningRate = 0.1,
    ///       Decay = 0
    ///   };
    ///   
    ///   // learn 5000 iterations
    ///   for (int i = 0; i &lt; 5000; i++)
    ///       teacher.RunEpoch(inputs);
    ///   
    ///   // Compute the machine answers for the given inputs:
    ///   double[] a = rbm.Compute(new double[] { 1, 1, 1, 0, 0, 0 }); // { 0.99, 0.00 }
    ///   double[] b = rbm.Compute(new double[] { 0, 0, 0, 1, 1, 1 }); // { 0.00, 0.99 }
    ///   
    ///   // As we can see, the first neuron responds to vectors belonging
    ///   // to the first class, firing 0.99 when we feed vectors which 
    ///   // have 1s at the beginning. Likewise, the second neuron fires 
    ///   // when the vector belongs to the second class.
    ///   
    ///   // We can also generate input vectors given the classes:
    ///   double[] xa = rbm.GenerateInput(new double[] { 1, 0 }); // { 1, 1, 1, 0, 0, 0 }
    ///   double[] xb = rbm.GenerateInput(new double[] { 0, 1 }); // { 0, 0, 1, 1, 1, 0 }
    ///   
    ///   // As we can see, if we feed an output pattern where the first neuron
    ///   // is firing and the second isn't, the network generates an example of
    ///   // a vector belonging to the first class. The same goes for the second
    ///   // neuron and the second class.
    /// </code>
    /// </example>
    /// <seealso cref="Learning.ContrastiveDivergenceLearning" />
    /// <seealso cref="BernoulliFunction" />
    [Serializable]
    public class RestrictedBoltzmannMachine : ActivationNetwork
    {
        /// <summary>
        ///     Creates a new <see cref="RestrictedBoltzmannMachine" />.
        /// </summary>
        /// <param name="inputsCount">The number of inputs for the machine.</param>
        /// <param name="hiddenNeurons">The number of hidden neurons in the machine.</param>
        public RestrictedBoltzmannMachine(int inputsCount, int hiddenNeurons)
            : this(new BernoulliFunction(1), inputsCount, hiddenNeurons)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="RestrictedBoltzmannMachine" />.
        /// </summary>
        /// <param name="hidden">The hidden layer to be added in the machine.</param>
        /// <param name="visible">The visible layer to be added in the machine.</param>
        public RestrictedBoltzmannMachine(StochasticLayer hidden, StochasticLayer visible)
            : base(null, hidden.InputsCount, null, 0)
        {
            this.Hidden = hidden;
            this.Visible = visible;

            layers[0] = hidden;
        }

        /// <summary>
        ///     Creates a new <see cref="RestrictedBoltzmannMachine" />.
        /// </summary>
        /// <param name="function">The activation function to use in the network neurons.</param>
        /// <param name="inputsCount">The number of inputs for the machine.</param>
        /// <param name="hiddenNeurons">The number of hidden neurons in the machine.</param>
        public RestrictedBoltzmannMachine(IStochasticFunction function, int inputsCount, int hiddenNeurons)
            : base(function, inputsCount, null, 1)
        {
            Visible = new StochasticLayer(function, inputsCount, hiddenNeurons);
            Hidden = new StochasticLayer(function, hiddenNeurons, inputsCount);

            layers[0] = Hidden;
        }

        /// <summary>
        ///     Gets the visible layer of the machine.
        /// </summary>
        public StochasticLayer Visible { get; }

        /// <summary>
        ///     Gets the hidden layer of the machine.
        /// </summary>
        public StochasticLayer Hidden { get; }

        /// <summary>
        ///     Compute output vector of the network.
        /// </summary>
        /// <param name="input">Input vector.</param>
        /// <returns>
        ///     Returns network's output vector.
        /// </returns>
        public override double[] Compute(double[] input)
        {
            return Hidden.Compute(input);
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
            return Visible.Compute(output);
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
            return Hidden.Generate(input);
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
            return Visible.Generate(output);
        }

        /// <summary>
        ///     Constructs a Gaussian-Bernoulli network with
        ///     visible Gaussian units and hidden Bernoulli units.
        /// </summary>
        /// <param name="inputsCount">The number of inputs for the machine.</param>
        /// <param name="hiddenNeurons">The number of hidden neurons in the machine.</param>
        /// <returns>A Gaussian-Bernoulli Restricted Boltzmann Machine</returns>
        public static RestrictedBoltzmannMachine CreateGaussianBernoulli(int inputsCount, int hiddenNeurons)
        {
            var network = new RestrictedBoltzmannMachine(inputsCount, hiddenNeurons);

            foreach (var neuron in network.Visible.Neurons)
            {
                neuron.ActivationFunction = new GaussianFunction();
            }

            return network;
        }

        /// <summary>
        ///     Creates a new <see cref="ActivationNetwork" /> from this instance.
        /// </summary>
        /// <param name="outputs">The number of output neurons in the last layer.</param>
        /// <returns>An <see cref="ActivationNetwork" /> containing this network.</returns>
        public ActivationNetwork ToActivationNetwork(int outputs)
        {
            return ToActivationNetwork(new SigmoidFunction(1), outputs);
        }

        /// <summary>
        ///     Creates a new <see cref="ActivationNetwork" /> from this instance.
        /// </summary>
        /// <param name="outputs">The number of output neurons in the last layer.</param>
        /// <param name="function">The activation function to use in the last layer.</param>
        /// <returns>An <see cref="ActivationNetwork" /> containing this network.</returns>
        public ActivationNetwork ToActivationNetwork(IActivationFunction function, int outputs)
        {
            var ann = new ActivationNetwork(function,
                inputsCount, null, Hidden.Neurons.Length, outputs);

            // For each neuron
            for (var i = 0; i < Hidden.Neurons.Length; i++)
            {
                var aneuron = ann.Layers[0].Neurons[i] as ActivationNeuron;
                var sneuron = Hidden.Neurons[i];

                // For each weight
                for (var j = 0; j < sneuron.Weights.Length; j++)
                {
                    aneuron.Weights[j] = sneuron.Weights[j];
                }

                aneuron.Threshold = sneuron.Threshold;
                aneuron.ActivationFunction = sneuron.ActivationFunction;
            }

            return ann;
        }

        /// <summary>
        ///     Updates the weights of the visible layer by copying
        ///     the reverse of the weights in the hidden layer.
        /// </summary>
        public void UpdateVisibleWeights()
        {
            Visible.CopyReversedWeightsFrom(Hidden);
        }
    }
}