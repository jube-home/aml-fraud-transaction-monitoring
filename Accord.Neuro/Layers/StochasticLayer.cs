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
using Accord.Neuro.Neurons;

namespace Accord.Neuro.Layers
{
    /// <summary>
    ///     Stochastic Activation Layer.
    /// </summary>
    /// <remarks>
    ///     This class represents a layer of <see cref="StochasticNeuron">stochastic neurons</see>.
    /// </remarks>
    [Serializable]
    public class StochasticLayer : ActivationLayer
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="StochasticLayer" /> class.
        /// </summary>
        /// <param name="neuronsCount">Layer's neurons count.</param>
        /// <param name="inputsCount">Layer's inputs count.</param>
        public StochasticLayer(int neuronsCount, int inputsCount)
            : this(new BernoulliFunction(1), neuronsCount, inputsCount)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="StochasticLayer" /> class.
        /// </summary>
        /// <param name="function">The activation function for the neurons in the layer.</param>
        /// <param name="neuronsCount">The neurons count.</param>
        /// <param name="inputsCount">The inputs count.</param>
        public StochasticLayer(IStochasticFunction function, int neuronsCount, int inputsCount)
            : base(neuronsCount, inputsCount, function)
        {
            Neurons = new StochasticNeuron[neuronsCount];
            for (var i = 0; i < Neurons.Length; i++)
            {
                base.neurons[i] = Neurons[i] =
                    new StochasticNeuron(inputsCount, function);
            }
        }

        /// <summary>
        ///     Gets the layer's neurons.
        /// </summary>
        public new StochasticNeuron[] Neurons { get; }

        /// <summary>
        ///     Gets the layer's sample values generated in the last
        ///     call of any of the <see cref="Generate" /> methods.
        /// </summary>
        public double[] Sample { get; private set; }

        /// <summary>
        ///     Compute output vector of the layer.
        /// </summary>
        /// <param name="input">Input vector.</param>
        /// <returns>
        ///     Returns layer's output vector.
        /// </returns>
        public override double[] Compute(double[] input)
        {
            var output = new double[neuronsCount];

            for (var i = 0; i < Neurons.Length; i++)
            {
                output[i] = Neurons[i].Compute(input);
            }

            this.output = output;

            return output;
        }

        /// <summary>
        ///     Compute probability vector of the layer.
        /// </summary>
        /// <param name="input">Input vector.</param>
        /// <returns>
        ///     Returns layer's probability vector.
        /// </returns>
        public double[] Generate(double[] input)
        {
            var sample = new double[neuronsCount];
            var output = new double[neuronsCount];

            for (var i = 0; i < Neurons.Length; i++)
            {
                output[i] = Neurons[i].Compute(input);
                sample[i] = Neurons[i].Generate(output[i]);
            }

            this.Sample = sample;
            this.output = output;

            return sample;
        }

        /// <summary>
        ///     Copy the weights of another layer in reversed order. This
        ///     can be used to update visible layers from hidden layers and
        ///     vice-versa.
        /// </summary>
        /// <param name="layer">The layer to copy the weights from.</param>
        public void CopyReversedWeightsFrom(StochasticLayer layer)
        {
            for (var i = 0; i < Neurons.Length; i++)
            {
                for (var j = 0; j < inputsCount; j++)
                {
                    Neurons[i].Weights[j] = layer.Neurons[j].Weights[i];
                }
            }
        }
    }
}