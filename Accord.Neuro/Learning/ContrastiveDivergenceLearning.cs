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
using System.Threading;
using System.Threading.Tasks;
using Accord.Diagnostics;
using Accord.Math;
using Accord.Neuro.ActivationFunctions;
using Accord.Neuro.Layers;
using Accord.Neuro.Networks;

namespace Accord.Neuro.Learning
{
    /// <summary>
    ///     Contrastive Divergence learning algorithm for Restricted Boltzmann Machines.
    /// </summary>
    public class ContrastiveDivergenceLearning : IUnsupervisedLearning, IDisposable
    {
        private readonly int steps = 1;

        private StochasticLayer hidden;
        private double[] hiddenBiasGradient;
        private double[] hiddenBiasUpdates;
        private int hiddenCount;

        private int inputsCount;

        [NonSerialized] private ParallelOptions parallelOptions;

        private ThreadLocal<ParallelStorage> storage;
        private StochasticLayer visible;
        private double[] visibleBiasGradient;
        private double[] visibleBiasUpdates;

        private double[][] weightsGradient;

        private double[][] weightsUpdates;

        /// <summary>
        ///     Creates a new <see cref="ContrastiveDivergenceLearning" /> algorithm.
        /// </summary>
        /// <param name="network">The network to be trained.</param>
        public ContrastiveDivergenceLearning(RestrictedBoltzmannMachine network)
        {
            init(network.Hidden, network.Visible);
        }

        /// <summary>
        ///     Creates a new <see cref="ContrastiveDivergenceLearning" /> algorithm.
        /// </summary>
        /// <param name="hidden">The hidden layer of the hidden-visible layer pair to be trained.</param>
        /// <param name="visible">The visible layer of the hidden-visible layer pair to be trained.</param>
        public ContrastiveDivergenceLearning(StochasticLayer hidden, StochasticLayer visible)
        {
            init(hidden, visible);
        }

        /// <summary>
        ///     Gets or sets the learning rate of the
        ///     learning algorithm. Default is 0.1.
        /// </summary>
        public double LearningRate { get; set; } = 0.1;

        /// <summary>
        ///     Gets or sets parallelization options.
        /// </summary>
        /// <summary>
        ///     Gets or sets parallelization options.
        /// </summary>
        public ParallelOptions ParallelOptions
        {
            get
            {
                if (parallelOptions == null)
                {
                    parallelOptions = new ParallelOptions();
                }

                return parallelOptions;
            }
            set => parallelOptions = value;
        }

        /// <summary>
        ///     Gets or sets the momentum term of the
        ///     learning algorithm. Default is 0.9.
        /// </summary>
        public double Momentum { get; set; } = 0.9;

        /// <summary>
        ///     Gets or sets the Weight Decay constant
        ///     of the learning algorithm. Default is 0.01.
        /// </summary>
        public double Decay { get; set; } = 0.01;

        /// <summary>
        ///     Not supported.
        /// </summary>
        public double Run(double[] input)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Runs learning epoch.
        /// </summary>
        /// <param name="input">Array of input vectors.</param>
        /// <returns>
        ///     Returns sum of learning errors.
        /// </returns>
        public double RunEpoch(double[][] input)
        {
            // Initialize iteration
            for (var i = 0; i < weightsGradient.Length; i++)
            {
                Array.Clear(weightsGradient[i], 0, weightsGradient[i].Length);
            }

            Array.Clear(hiddenBiasGradient, 0, hiddenBiasGradient.Length);
            Array.Clear(visibleBiasGradient, 0, visibleBiasGradient.Length);


            // calculate gradient and model error
            var error = ComputeGradient(input);

            // calculate weights updates
            CalculateUpdates(input);

            // update the network
            UpdateNetwork();

            return error;
        }

        private void init(StochasticLayer hidden, StochasticLayer visible)
        {
            this.hidden = hidden;
            this.visible = visible;

            inputsCount = hidden.InputsCount;
            hiddenCount = hidden.Neurons.Length;

            weightsGradient = new double[inputsCount][];
            for (var i = 0; i < weightsGradient.Length; i++)
            {
                weightsGradient[i] = new double[hiddenCount];
            }

            visibleBiasGradient = new double[inputsCount];
            hiddenBiasGradient = new double[hiddenCount];

            weightsUpdates = new double[inputsCount][];
            for (var i = 0; i < weightsUpdates.Length; i++)
            {
                weightsUpdates[i] = new double[hiddenCount];
            }

            visibleBiasUpdates = new double[inputsCount];
            hiddenBiasUpdates = new double[hiddenCount];

            storage = new ThreadLocal<ParallelStorage>(() =>
                new ParallelStorage(inputsCount, hiddenCount));

            ParallelOptions = new ParallelOptions();
        }

        private double ComputeGradient(double[][] input)
        {
            double errors = 0;


#if NET35
            var partial = storage.Value.Clear();
            for (int i = 0; i < input.Length; i++)
            {
                int observationIndex = i;
#else
            var lockObj = new object();

            // For each training instance
            Parallel.For(0, input.Length,
                ParallelOptions,

                // Initialize
                () => storage.Value.Clear(),

                // Map
                (observationIndex, loopState, partial) =>
#endif
                {
                    var observation = input[observationIndex];

                    var probability = partial.OriginalProbability;
                    var activations = partial.OriginalActivations;
                    var reconstruction = partial.ReconstructedInput;
                    var reprobability = partial.ReconstructedProbs;

                    var weightGradient = partial.WeightGradient;
                    var hiddenGradient = partial.HiddenBiasGradient;
                    var visibleGradient = partial.VisibleBiasGradient;


                    // 1. Compute a forward pass. The network is being
                    //    driven by data, so we will gather activations
                    for (var j = 0; j < hidden.Neurons.Length; j++)
                    {
                        probability[j] = hidden.Neurons[j].Compute(observation); // output probabilities
                        activations[j] = hidden.Neurons[j].Generate(probability[j]); // state activations
                    }

                    // 2. Reconstruct inputs from previous outputs
                    for (var j = 0; j < visible.Neurons.Length; j++)
                    {
                        reconstruction[j] = visible.Neurons[j].Compute(activations);
                    }


                    if (steps > 1)
                    {
                        // Perform Gibbs sampling
                        var current = probability;
                        for (var k = 0; k < steps - 1; k++)
                        {
                            for (var j = 0; j < probability.Length; j++)
                            {
                                probability[j] = hidden.Neurons[j].Compute(current);
                            }

                            for (var j = 0; j < reconstruction.Length; j++)
                            {
                                reconstruction[j] = visible.Neurons[j].Compute(probability);
                            }

                            current = reconstruction;
                        }
                    }


                    // 3. Compute outputs for the reconstruction. The network
                    //    is now being driven by reconstructions, so we should
                    //    gather the output probabilities without sampling
                    for (var j = 0; j < hidden.Neurons.Length; j++)
                    {
                        reprobability[j] = hidden.Neurons[j].Compute(reconstruction);
                    }


                    // 4.1. Compute positive associations
                    for (var k = 0; k < observation.Length; k++)
                    {
                        for (var j = 0; j < probability.Length; j++)
                        {
                            weightGradient[k][j] += observation[k] * probability[j];
                        }
                    }

                    for (var j = 0; j < hiddenGradient.Length; j++)
                    {
                        hiddenGradient[j] += probability[j];
                    }

                    for (var j = 0; j < visibleGradient.Length; j++)
                    {
                        visibleGradient[j] += observation[j];
                    }


                    // 4.2. Compute negative associations
                    for (var k = 0; k < reconstruction.Length; k++)
                    {
                        for (var j = 0; j < reprobability.Length; j++)
                        {
                            weightGradient[k][j] -= reconstruction[k] * reprobability[j];
                        }
                    }

                    for (var j = 0; j < reprobability.Length; j++)
                    {
                        hiddenGradient[j] -= reprobability[j];
                    }

                    for (var j = 0; j < reconstruction.Length; j++)
                    {
                        visibleGradient[j] -= reconstruction[j];
                    }


                    // Compute current error
                    for (var j = 0; j < observation.Length; j++)
                    {
                        var e = observation[j] - reconstruction[j];
                        partial.ErrorSumOfSquares += e * e;
                    }

#if !NET35
                    return partial; // Report partial solution
                },

                // Reduce
                partial =>
                {
                    lock (lockObj)
                    {
                        // Accumulate partial solutions
                        for (var i = 0; i < weightsGradient.Length; i++)
                        {
                            for (var j = 0; j < weightsGradient[i].Length; j++)
                            {
                                weightsGradient[i][j] += partial.WeightGradient[i][j];
                            }
                        }

                        for (var i = 0; i < hiddenBiasGradient.Length; i++)
                        {
                            hiddenBiasGradient[i] += partial.HiddenBiasGradient[i];
                        }

                        for (var i = 0; i < visibleBiasGradient.Length; i++)
                        {
                            visibleBiasGradient[i] += partial.VisibleBiasGradient[i];
                        }

                        errors += partial.ErrorSumOfSquares;
                    }
                });
#else
                }
            }

            weightsGradient = partial.WeightGradient;
            hiddenBiasGradient = partial.HiddenBiasGradient;
            visibleBiasGradient = partial.VisibleBiasGradient;
            errors = partial.ErrorSumOfSquares;
#endif

            return errors;
        }

        /// <summary>
        ///     Computes the reconstruction error of the current layer.
        /// </summary>
        /// <param name="input">Array of input vectors.</param>
        /// <returns>
        ///     Returns sum of learning errors.
        /// </returns>
        public double ComputeError(double[][] input)
        {
            double errors = 0;


#if NET35
            var partial = storage.Value.Clear();
            for (int i = 0; i < input.Length; i++)
            {
                int observationIndex = i;
#else
            var lockObj = new object();

            // For each training instance
            Parallel.For(0, input.Length,

#if DEBUG
                new ParallelOptions { MaxDegreeOfParallelism = 1 },
#endif

                // Initialize
                () => storage.Value.Clear(),

                // Map
                (observationIndex, loopState, partial) =>
#endif
                {
                    var observation = input[observationIndex];

                    var probability = partial.OriginalProbability;
                    var activations = partial.OriginalActivations;
                    var reconstruction = partial.ReconstructedInput;


                    // 1. Compute a forward pass. The network is being
                    //    driven by data, so we will gather activations
                    for (var j = 0; j < hidden.Neurons.Length; j++)
                    {
                        probability[j] = hidden.Neurons[j].Compute(observation); // output probabilities
                        activations[j] = hidden.Neurons[j].Generate(probability[j]); // state activations
                    }

                    // 2. Reconstruct inputs from previous outputs
                    for (var j = 0; j < visible.Neurons.Length; j++)
                    {
                        reconstruction[j] = visible.Neurons[j].Compute(activations);
                    }

                    if (steps > 1)
                    {
                        // Perform Gibbs sampling
                        var current = probability;
                        for (var k = 0; k < steps - 1; k++)
                        {
                            for (var j = 0; j < probability.Length; j++)
                            {
                                probability[j] = hidden.Neurons[j].Compute(current);
                            }

                            for (var j = 0; j < reconstruction.Length; j++)
                            {
                                reconstruction[j] = visible.Neurons[j].Compute(probability);
                            }

                            current = reconstruction;
                        }
                    }

                    // Compute current error
                    for (var j = 0; j < observation.Length; j++)
                    {
                        var e = observation[j] - reconstruction[j];
                        partial.ErrorSumOfSquares += e * e;
                    }

#if !NET35
                    return partial; // Report partial solution
                },

                // Reduce
                partial =>
                {
                    lock (lockObj)
                    {
                        errors += partial.ErrorSumOfSquares;
                    }
                });
#else
                }
            }

            weightsGradient = partial.WeightGradient;
            hiddenBiasGradient = partial.HiddenBiasGradient;
            visibleBiasGradient = partial.VisibleBiasGradient;
            errors = partial.ErrorSumOfSquares;
#endif

            return errors;
        }

        private void CalculateUpdates(double[][] input)
        {
            double rate;

            // Assume all neurons in the layer have the same act function
            if (visible.Neurons[0].ActivationFunction is GaussianFunction)
            {
                rate = LearningRate / (100 * input.Length);
            }
            else
            {
                rate = LearningRate / input.Length;
            }


            // 5. Compute gradient descent updates
            for (var i = 0; i < weightsGradient.Length; i++)
            {
                for (var j = 0; j < weightsGradient[i].Length; j++)
                {
                    weightsUpdates[i][j] = Momentum * weightsUpdates[i][j]
                                           + rate * weightsGradient[i][j];
                }
            }

            for (var i = 0; i < hiddenBiasUpdates.Length; i++)
            {
                hiddenBiasUpdates[i] = Momentum * hiddenBiasUpdates[i]
                                       + rate * hiddenBiasGradient[i];
            }

            for (var i = 0; i < visibleBiasUpdates.Length; i++)
            {
                visibleBiasUpdates[i] = Momentum * visibleBiasUpdates[i]
                                        + rate * visibleBiasGradient[i];
            }

            Debug.Assert(!weightsGradient.HasNaN());
            Debug.Assert(!visibleBiasUpdates.HasNaN());
            Debug.Assert(!hiddenBiasUpdates.HasNaN());
        }

        private void UpdateNetwork()
        {
            // 6.1 Update hidden layer weights
            for (var i = 0; i < hidden.Neurons.Length; i++)
            {
                var neuron = hidden.Neurons[i];
                for (var j = 0; j < neuron.Weights.Length; j++)
                {
                    neuron.Weights[j] += weightsUpdates[j][i] - LearningRate * Decay * neuron.Weights[j];
                }

                neuron.Threshold += hiddenBiasUpdates[i];
            }

            // 6.2 Update visible layer with reverse weights
            for (var i = 0; i < visible.Neurons.Length; i++)
            {
                visible.Neurons[i].Threshold += visibleBiasUpdates[i];
            }

            visible.CopyReversedWeightsFrom(hidden);
        }

        private class ParallelStorage
        {
            public ParallelStorage(int inputsCount, int hiddenCount)
            {
                WeightGradient = new double[inputsCount][];
                for (var i = 0; i < WeightGradient.Length; i++)
                {
                    WeightGradient[i] = new double[hiddenCount];
                }

                VisibleBiasGradient = new double[inputsCount];
                HiddenBiasGradient = new double[hiddenCount];

                OriginalActivations = new double[hiddenCount];
                OriginalProbability = new double[hiddenCount];
                ReconstructedInput = new double[inputsCount];
                ReconstructedProbs = new double[hiddenCount];
            }

            public double[][] WeightGradient { get; }
            public double[] VisibleBiasGradient { get; }
            public double[] HiddenBiasGradient { get; }

            public double[] OriginalActivations { get; }
            public double[] OriginalProbability { get; }

            public double[] ReconstructedInput { get; }
            public double[] ReconstructedProbs { get; }

            public double ErrorSumOfSquares { get; set; }

            public ParallelStorage Clear()
            {
                ErrorSumOfSquares = 0;
                for (var i = 0; i < WeightGradient.Length; i++)
                {
                    Array.Clear(WeightGradient[i], 0, WeightGradient[i].Length);
                }

                Array.Clear(VisibleBiasGradient, 0, VisibleBiasGradient.Length);
                Array.Clear(HiddenBiasGradient, 0, HiddenBiasGradient.Length);
                return this;
            }
        }

        #region IDisposable members

        /// <summary>
        ///     Performs application-defined tasks associated with
        ///     freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged
        ///     resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (storage != null)
                {
                    storage.Dispose();
                    storage = null;
                }
            }
        }

        /// <summary>
        ///     Releases unmanaged resources and performs other cleanup operations before the
        ///     <see cref="ContrastiveDivergenceLearning" /> is reclaimed by garbage collection.
        /// </summary>
        ~ContrastiveDivergenceLearning()
        {
            Dispose(false);
        }

        #endregion
    }
}