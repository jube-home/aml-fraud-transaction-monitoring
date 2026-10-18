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
using Accord.Statistics.Distributions;

namespace Accord.Statistics.Models.Markov.Learning
{
#pragma warning disable 612, 618
    /// <summary>
    ///     Obsolete. Please use <see cref="HiddenMarkovClassifierLearning{TDistribution, TObservation}" /> instead.
    /// </summary>
    [Obsolete("Please use HiddenMarkovClassifierLearning<TDistribution, TObservation> instead.")]
    public class HiddenMarkovClassifierLearning<TDistribution> :
        BaseHiddenMarkovClassifierLearning<HiddenMarkovClassifier<TDistribution>,
            HiddenMarkovModel<TDistribution>>
        where TDistribution : IDistribution
    {
        /// <summary>
        ///     Creates a new instance of the learning algorithm for a given
        ///     Markov sequence classifier using the specified configuration
        ///     function.
        /// </summary>
        public HiddenMarkovClassifierLearning(HiddenMarkovClassifier<TDistribution> classifier,
            ClassifierLearningAlgorithmConfiguration algorithm)
            : base(classifier, algorithm)
        {
        }

        /// <summary>
        ///     Trains each model to recognize each of the output labels.
        /// </summary>
        /// <returns>The sum log-likelihood for all models after training.</returns>
        public double Run(Array[] inputs, int[] outputs)
        {
            return base.Run(inputs, outputs);
        }

        /// <summary>
        ///     Compute model error for a given data set.
        /// </summary>
        /// <param name="inputs">The input points.</param>
        /// <param name="outputs">The output points.</param>
        /// <returns>The percent of misclassification errors for the data.</returns>
        public double ComputeError(Array[] inputs, int[] outputs)
        {
            var errors = 0;
            Parallel.For(0, inputs.Length, i =>
            {
                var expectedOutput = outputs[i];
                var actualOutput = Classifier.Compute(inputs[i]);

                if (expectedOutput != actualOutput)
                {
                    Interlocked.Increment(ref errors);
                }
            });

            return errors / (double)inputs.Length;
        }

        /// <summary>
        ///     Creates a new <see cref="Threshold">threshold model</see>
        ///     for the current set of Markov models in this sequence classifier.
        /// </summary>
        /// <returns>
        ///     A <see cref="Threshold">threshold Markov model</see>.
        /// </returns>
        public override HiddenMarkovModel<TDistribution> Threshold()
        {
            var models = Classifier.Models;
            var topology = CreateThresholdTopology();

            var states = topology.States;

            // Create the threshold emission matrix
            var emissions = new TDistribution[states];


            // Then, for each hidden Markov model in the classifier
            for (int i = 0, modelStartIndex = 0; i < models.Length; i++)
            {
                // Retrieve the model definition matrices
                var B = models[i].Emissions;

                // Now, for each state in the model
                for (var j = 0; j < models[i].States; j++)
                {
                    // Copy emissions from the model
                    emissions[j + modelStartIndex] = B[j];
                }

                // Next model starts where this ends
                modelStartIndex += models[i].States;
            }


            // Create and return the threshold hidden Markov model
            return new HiddenMarkovModel<TDistribution>(topology, emissions)
            {
                Tag = "Non-gesture"
            };
        }
    }
}