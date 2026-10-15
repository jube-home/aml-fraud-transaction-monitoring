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

namespace Accord.Neuro.Neurons
{
    /// <summary>
    ///     Stochastic Activation Neuron.
    /// </summary>
    /// <remarks>
    ///     The Stochastic Activation Neuron is an activation neuron
    ///     which activates (returns 1) only within a given probability.
    ///     The neuron has a random component in the activation function,
    ///     and the neuron fires only if the total sum, after applied
    ///     to a logistic activation function, is greater than a randomly
    ///     sampled value.
    /// </remarks>
    [Serializable]
    public class StochasticNeuron : ActivationNeuron
    {
        private new IStochasticFunction function;

        /// <summary>
        ///     Initializes a new instance of the <see cref="StochasticNeuron" /> class.
        /// </summary>
        /// <param name="inputs">Number of inputs for the neuron.</param>
        /// <param name="function">Activation function for the neuron.</param>
        public StochasticNeuron(int inputs, IStochasticFunction function)
            : base(inputs, function)
        {
            ActivationFunction = function;
            Threshold = 0; // Ruslan Salakhutdinov and Geoff Hinton
            // also start with zero thresholds
        }

        /// <summary>
        ///     Gets the neuron sample value generated in the last
        ///     call of any of the <see cref="Generate(double[])" /> methods.
        /// </summary>
        public double Sample { get; private set; }

        /// <summary>
        ///     Gets or sets the stochastic activation
        ///     function for this stochastic neuron.
        /// </summary>
        public new IStochasticFunction ActivationFunction
        {
            get => function;
            set => base.function = function = value;
        }

        /// <summary>
        ///     Samples the neuron output considering
        ///     the stochastic activation function.
        /// </summary>
        /// <param name="input">An input vector.</param>
        /// <returns>
        ///     A possible output for the neuron drawn
        ///     from the neuron's stochastic function.
        /// </returns>
        public double Generate(double[] input)
        {
            var sum = threshold;
            for (var i = 0; i < weights.Length; i++)
            {
                sum += weights[i] * input[i];
            }

            var output = function.Function(sum);
            var sample = function.Generate2(output);

            this.output = output;
            this.Sample = sample;

            return sample;
        }

        /// <summary>
        ///     Samples the neuron output considering
        ///     the stochastic activation function.
        /// </summary>
        /// <param name="output">The (previously computed) neuron output.</param>
        /// <returns>
        ///     A possible output for the neuron drawn
        ///     from the neuron's stochastic function.
        /// </returns>
        public double Generate(double output)
        {
            var sample = function.Generate2(output);

            this.output = output;
            this.Sample = sample;

            return sample;
        }
    }
}