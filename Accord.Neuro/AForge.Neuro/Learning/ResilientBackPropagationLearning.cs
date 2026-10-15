// AForge Neural Net Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © César Souza, 2009-2012
// cesarsouza at gmail.com
//
// Copyright © AForge.NET, 2005-2012
// contacts@aforgenet.com
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

namespace Accord.Neuro.Learning
{
    /// <summary>
    ///     Resilient Backpropagation learning algorithm.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class implements the resilient backpropagation (RProp)
    ///         learning algorithm. The RProp learning algorithm is one of the fastest learning
    ///         algorithms for feed-forward learning networks which use only first-order
    ///         information.
    ///     </para>
    ///     <para>Sample usage (training network to calculate XOR function):</para>
    ///     <code>
    /// // initialize input and output values
    /// double[][] input = new double[4][] {
    ///     new double[] {0, 0}, new double[] {0, 1},
    ///     new double[] {1, 0}, new double[] {1, 1}
    /// };
    /// double[][] output = new double[4][] {
    ///     new double[] {0}, new double[] {1},
    ///     new double[] {1}, new double[] {0}
    /// };
    /// // create neural network
    /// ActivationNetwork   network = new ActivationNetwork(
    ///     SigmoidFunction( 2 ),
    ///     2, // two inputs in the network
    ///     2, // two neurons in the first layer
    ///     1 ); // one neuron in the second layer
    /// // create teacher
    /// ResilientBackpropagationLearning teacher = new ResilientBackpropagationLearning( network );
    /// // loop
    /// while ( !needToStop )
    /// {
    ///     // run epoch of learning procedure
    ///     double error = teacher.RunEpoch( input, output );
    ///     // check error value to see if we need to stop
    ///     // ...
    /// }
    /// </code>
    /// </remarks>
    public class ResilientBackpropagationLearning : ISupervisedLearning
    {
        private const double etaMinus = 0.5;
        private readonly double deltaMax = 50.0;
        private readonly double deltaMin = 1e-6;
        private readonly double etaPlus = 1.2;
        private readonly ActivationNetwork network;

        private readonly double[][] neuronErrors;
        private readonly double[][] thresholdsDerivatives;
        private readonly double[][] thresholdsPreviousDerivatives;
        private readonly double[][] thresholdsUpdates;

        // current and previous gradient values
        private readonly double[][][] weightsDerivatives;

        private readonly double[][][] weightsPreviousDerivatives;

        // update values, also known as deltas
        private readonly double[][][] weightsUpdates;

        private double learningRate = 0.0125;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ResilientBackpropagationLearning" /> class.
        /// </summary>
        /// <param name="network">Network to teach.</param>
        public ResilientBackpropagationLearning(ActivationNetwork network)
        {
            this.network = network;

            var layersCount = network.Layers.Length;

            neuronErrors = new double[layersCount][];

            weightsDerivatives = new double[layersCount][][];
            thresholdsDerivatives = new double[layersCount][];

            weightsPreviousDerivatives = new double[layersCount][][];
            thresholdsPreviousDerivatives = new double[layersCount][];

            weightsUpdates = new double[layersCount][][];
            thresholdsUpdates = new double[layersCount][];

            // initialize errors, derivatives and steps
            for (var i = 0; i < network.Layers.Length; i++)
            {
                var layer = network.Layers[i];
                var neuronsCount = layer.Neurons.Length;

                neuronErrors[i] = new double[neuronsCount];

                weightsDerivatives[i] = new double[neuronsCount][];
                weightsPreviousDerivatives[i] = new double[neuronsCount][];
                weightsUpdates[i] = new double[neuronsCount][];

                thresholdsDerivatives[i] = new double[neuronsCount];
                thresholdsPreviousDerivatives[i] = new double[neuronsCount];
                thresholdsUpdates[i] = new double[neuronsCount];

                // for each neuron
                for (var j = 0; j < layer.Neurons.Length; j++)
                {
                    weightsDerivatives[i][j] = new double[layer.InputsCount];
                    weightsPreviousDerivatives[i][j] = new double[layer.InputsCount];
                    weightsUpdates[i][j] = new double[layer.InputsCount];
                }
            }

            // intialize steps
            ResetUpdates(learningRate);
        }

        /// <summary>
        ///     Learning rate.
        /// </summary>
        /// <remarks>
        ///     <para>The value determines speed of learning.</para>
        ///     <para>Default value equals to <b>0.0125</b>.</para>
        /// </remarks>
        public double LearningRate
        {
            get => learningRate;
            set
            {
                learningRate = value;
                ResetUpdates(learningRate);
            }
        }

        /// <summary>
        ///     Runs learning iteration.
        /// </summary>
        /// <param name="input">Input vector.</param>
        /// <param name="output">Desired output vector.</param>
        /// <returns>
        ///     Returns squared error (difference between current network's output and
        ///     desired output) divided by 2.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Runs one learning iteration and updates neuron's
        ///         weights.
        ///     </para>
        /// </remarks>
        public double Run(double[] input, double[] output)
        {
            // zero gradient
            ResetGradient();

            // compute the network's output
            network.Compute(input);

            // calculate network error
            var error = CalculateError(output);

            // calculate weights updates
            CalculateGradient(input);

            // update the network
            UpdateNetwork();

            // return summary error
            return error;
        }

        /// <summary>
        ///     Runs learning epoch.
        /// </summary>
        /// <param name="input">Array of input vectors.</param>
        /// <param name="output">Array of output vectors.</param>
        /// <returns>
        ///     Returns summary learning error for the epoch. See <see cref="Run" />
        ///     method for details about learning error calculation.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The method runs one learning epoch, by calling <see cref="Run" /> method
        ///         for each vector provided in the <paramref name="input" /> array.
        ///     </para>
        /// </remarks>
        public double RunEpoch(double[][] input, double[][] output)
        {
            // zero gradient
            ResetGradient();

            var error = 0.0;

            // run learning procedure for all samples
            for (var i = 0; i < input.Length; i++)
            {
                // compute the network's output
                network.Compute(input[i]);

                // calculate network error
                error += CalculateError(output[i]);

                // calculate weights updates
                CalculateGradient(input[i]);
            }

            // update the network
            UpdateNetwork();

            // return summary error
            return error;
        }

        /// <summary>
        ///     Resets current weight and threshold derivatives.
        /// </summary>
        private void ResetGradient()
        {
            for (var i = 0; i < weightsDerivatives.Length; i++)
            {
                for (var j = 0; j < weightsDerivatives[i].Length; j++)
                {
                    Array.Clear(weightsDerivatives[i][j], 0, weightsDerivatives[i][j].Length);
                }
            }

            for (var i = 0; i < thresholdsDerivatives.Length; i++)
            {
                Array.Clear(thresholdsDerivatives[i], 0, thresholdsDerivatives[i].Length);
            }
        }

        /// <summary>
        ///     Resets the current update steps using the given learning rate.
        /// </summary>
        private void ResetUpdates(double rate)
        {
            for (var i = 0; i < weightsUpdates.Length; i++)
            {
                for (var j = 0; j < weightsUpdates[i].Length; j++)
                {
                    for (var k = 0; k < weightsUpdates[i][j].Length; k++)
                    {
                        weightsUpdates[i][j][k] = rate;
                    }
                }
            }

            for (var i = 0; i < thresholdsUpdates.Length; i++)
            {
                for (var j = 0; j < thresholdsUpdates[i].Length; j++)
                {
                    thresholdsUpdates[i][j] = rate;
                }
            }
        }

        /// <summary>
        ///     Update network's weights.
        /// </summary>
        private void UpdateNetwork()
        {
            double[][] layerWeightsUpdates;
            double[] layerThresholdUpdates;
            double[] neuronWeightUpdates;

            double[][] layerWeightsDerivatives;
            double[] layerThresholdDerivatives;
            double[] neuronWeightDerivatives;

            double[][] layerPreviousWeightsDerivatives;
            double[] layerPreviousThresholdDerivatives;
            double[] neuronPreviousWeightDerivatives;

            // for each layer of the network
            for (var i = 0; i < network.Layers.Length; i++)
            {
                var layer = network.Layers[i] as ActivationLayer;

                layerWeightsUpdates = weightsUpdates[i];
                layerThresholdUpdates = thresholdsUpdates[i];

                layerWeightsDerivatives = weightsDerivatives[i];
                layerThresholdDerivatives = thresholdsDerivatives[i];

                layerPreviousWeightsDerivatives = weightsPreviousDerivatives[i];
                layerPreviousThresholdDerivatives = thresholdsPreviousDerivatives[i];

                // for each neuron of the layer
                for (var j = 0; j < layer.Neurons.Length; j++)
                {
                    var neuron = layer.Neurons[j] as ActivationNeuron;

                    neuronWeightUpdates = layerWeightsUpdates[j];
                    neuronWeightDerivatives = layerWeightsDerivatives[j];
                    neuronPreviousWeightDerivatives = layerPreviousWeightsDerivatives[j];

                    double S;

                    // for each weight of the neuron
                    for (var k = 0; k < neuron.InputsCount; k++)
                    {
                        S = neuronPreviousWeightDerivatives[k] * neuronWeightDerivatives[k];

                        if (S > 0)
                        {
                            neuronWeightUpdates[k] = System.Math.Min(neuronWeightUpdates[k] * etaPlus, deltaMax);
                            neuron.Weights[k] -= System.Math.Sign(neuronWeightDerivatives[k]) * neuronWeightUpdates[k];
                            neuronPreviousWeightDerivatives[k] = neuronWeightDerivatives[k];
                        }
                        else if (S < 0)
                        {
                            neuronWeightUpdates[k] = System.Math.Max(neuronWeightUpdates[k] * etaMinus, deltaMin);
                            neuronPreviousWeightDerivatives[k] = 0;
                        }
                        else
                        {
                            neuron.Weights[k] -= System.Math.Sign(neuronWeightDerivatives[k]) * neuronWeightUpdates[k];
                            neuronPreviousWeightDerivatives[k] = neuronWeightDerivatives[k];
                        }
                    }

                    // update treshold
                    S = layerPreviousThresholdDerivatives[j] * layerThresholdDerivatives[j];

                    if (S > 0)
                    {
                        layerThresholdUpdates[j] = System.Math.Min(layerThresholdUpdates[j] * etaPlus, deltaMax);
                        neuron.Threshold -= System.Math.Sign(layerThresholdDerivatives[j]) * layerThresholdUpdates[j];
                        layerPreviousThresholdDerivatives[j] = layerThresholdDerivatives[j];
                    }
                    else if (S < 0)
                    {
                        layerThresholdUpdates[j] = System.Math.Max(layerThresholdUpdates[j] * etaMinus, deltaMin);
                        layerThresholdDerivatives[j] = 0;
                    }
                    else
                    {
                        neuron.Threshold -= System.Math.Sign(layerThresholdDerivatives[j]) * layerThresholdUpdates[j];
                        layerPreviousThresholdDerivatives[j] = layerThresholdDerivatives[j];
                    }
                }
            }
        }

        /// <summary>
        ///     Calculates error values for all neurons of the network.
        /// </summary>
        /// <param name="desiredOutput">Desired output vector.</param>
        /// <returns>Returns summary squared error of the last layer divided by 2.</returns>
        private double CalculateError(double[] desiredOutput)
        {
            double error = 0;
            var layersCount = network.Layers.Length;

            // assume, that all neurons of the network have the same activation function
            var function = (network.Layers[0].Neurons[0] as ActivationNeuron).ActivationFunction;

            // calculate error values for the last layer first
            var layer = network.Layers[layersCount - 1] as ActivationLayer;
            var layerDerivatives = neuronErrors[layersCount - 1];

            for (var i = 0; i < layer.Neurons.Length; i++)
            {
                var output = layer.Neurons[i].Output;

                var e = output - desiredOutput[i];
                layerDerivatives[i] = e * function.Derivative2(output);
                error += e * e;
            }

            // calculate error values for other layers
            for (var j = layersCount - 2; j >= 0; j--)
            {
                layer = network.Layers[j] as ActivationLayer;
                layerDerivatives = neuronErrors[j];

                var layerNext = network.Layers[j + 1] as ActivationLayer;
                var nextDerivatives = neuronErrors[j + 1];

                // for all neurons of the layer
                for (int i = 0, n = layer.Neurons.Length; i < n; i++)
                {
                    var sum = 0.0;

                    for (var k = 0; k < layerNext.Neurons.Length; k++)
                    {
                        sum += nextDerivatives[k] * layerNext.Neurons[k].Weights[i];
                    }

                    layerDerivatives[i] = sum * function.Derivative2(layer.Neurons[i].Output);
                }
            }

            // return squared error of the last layer divided by 2
            return error / 2.0;
        }

        /// <summary>
        ///     Calculate weights updates
        /// </summary>
        /// <param name="input">Network's input vector.</param>
        private void CalculateGradient(double[] input)
        {
            // 1. calculate updates for the first layer
            var layer = network.Layers[0] as ActivationLayer;
            var weightErrors = neuronErrors[0];
            var layerWeightsDerivatives = weightsDerivatives[0];
            var layerThresholdDerivatives = thresholdsDerivatives[0];

            // So, for each neuron of the first layer:
            for (var i = 0; i < layer.Neurons.Length; i++)
            {
                var neuron = layer.Neurons[i] as ActivationNeuron;
                var neuronWeightDerivatives = layerWeightsDerivatives[i];

                // for each weight of the neuron:
                for (var j = 0; j < neuron.InputsCount; j++)
                {
                    neuronWeightDerivatives[j] += weightErrors[i] * input[j];
                }

                layerThresholdDerivatives[i] += weightErrors[i];
            }

            // 2. for all other layers
            for (var k = 1; k < network.Layers.Length; k++)
            {
                layer = network.Layers[k] as ActivationLayer;
                weightErrors = neuronErrors[k];
                layerWeightsDerivatives = weightsDerivatives[k];
                layerThresholdDerivatives = thresholdsDerivatives[k];

                var layerPrev = network.Layers[k - 1] as ActivationLayer;

                // for each neuron of the layer
                for (var i = 0; i < layer.Neurons.Length; i++)
                {
                    var neuronWeightDerivatives = layerWeightsDerivatives[i];

                    // for each weight of the neuron
                    for (var j = 0; j < layerPrev.Neurons.Length; j++)
                    {
                        neuronWeightDerivatives[j] += weightErrors[i] * layerPrev.Neurons[j].Output;
                    }

                    layerThresholdDerivatives[i] += weightErrors[i];
                }
            }
        }
    }
}