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
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Univariate;
using AForge.Math;

#pragma warning disable 612, 618

namespace Accord.Statistics.Models.Markov.Learning
{
    /// <summary>
    ///     Learning algorithm for discrete-density
    ///     <see cref="HiddenMarkovClassifier">
    ///         generative hidden Markov sequence classifiers
    ///     </see>
    ///     .
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class acts as a teacher for
    ///         <see cref="HiddenMarkovClassifier">
    ///             classifiers based on discrete hidden Markov models
    ///         </see>
    ///         . The learning
    ///         algorithm uses a generative approach. It works by training each model in
    ///         the generative classifier separately.
    ///     </para>
    ///     <para>
    ///         This class implements discrete classifiers only. Discrete classifiers can
    ///         be used whenever the sequence of observations is discrete or can be represented
    ///         by discrete symbols, such as class labels, integers, and so on. If you need
    ///         to classify sequences of other entities, such as real numbers, vectors (i.e.
    ///         multivariate observations), then you can use
    ///         <see cref="HiddenMarkovClassifierLearning{TDistribution}">
    ///             generic-density
    ///             hidden Markov models
    ///         </see>
    ///         . Those models can be modeled after any kind of
    ///         <see cref="IDistribution">probability distribution</see> implementing
    ///         the <see cref="IDistribution" /> interface.
    ///     </para>
    ///     <para>
    ///         For a more thorough explanation on <see cref="HiddenMarkovModel">hidden Markov models</see>
    ///         with practical examples on gesture recognition, please see
    ///         <a href="http://www.codeproject.com/Articles/541428/Sequence-Classifiers-in-Csharp-Part-I-Hidden-Marko">
    ///             Sequence Classifiers in C#, Part I: Hidden Markov Models
    ///         </a>
    ///         [1].
    ///     </para>
    ///     <para>
    ///         [1]:
    ///         <a href="http://www.codeproject.com/Articles/541428/Sequence-Classifiers-in-Csharp-Part-I-Hidden-Marko">
    ///             http://www.codeproject.com/Articles/541428/Sequence-Classifiers-in-Csharp-Part-I-Hidden-Marko
    ///         </a>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to create a hidden Markov model sequence classifier
    ///         to classify discrete sequences into two disjoint labels: labels for class 0 and
    ///         labels for class 1. The training data is separated in inputs and outputs. The
    ///         inputs are the sequences we are trying to learn, and the outputs are the labels
    ///         associated with each input sequence.
    ///     </para>
    ///     <para>
    ///         In this example we will be using the <see cref="BaumWelchLearning">Baum-Welch</see>
    ///         algorithm to learn each model in our generative classifier; however, any other
    ///         <see cref="IUnsupervisedLearning">unsupervised learning algorithm</see> could be used.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\HiddenMarkovClassifierTest.cs" region="doc_learn" />
    ///     <para>
    ///         It is also possible to learn a hidden Markov classifier with support for rejection.
    ///         When a classifier is configured to use rejection, it will be able to detect when a
    ///         sample does not belong to any of the classes that it has previously seen.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Markov\HiddenMarkovClassifierTest.cs"
    ///         region="doc_rejection" />
    /// </example>
    /// <seealso cref="HiddenMarkovClassifier" />
    /// <seealso cref="HiddenMarkovClassifier{TDistribution, TObservation}" />
    /// <seealso cref="HiddenMarkovClassifierLearning{TDistribution, TObservation}" />
    public class HiddenMarkovClassifierLearning :
        BaseHiddenMarkovClassifierLearning<HiddenMarkovClassifier, HiddenMarkovModel, GeneralDiscreteDistribution, int>
    {
        private readonly int smoothingKernelSize = 3;
        private double[] gaussianKernel;
        private double smoothingSigma = 1.0;

        /// <summary>
        ///     Creates a new instance of the learning algorithm for a given
        ///     Markov sequence classifier using the specified configuration
        ///     function.
        /// </summary>
        public HiddenMarkovClassifierLearning()
        {
            createSmoothingKernel();
        }

        /// <summary>
        ///     Creates a new instance of the learning algorithm for a given
        ///     Markov sequence classifier using the specified configuration
        ///     function.
        /// </summary>
        public HiddenMarkovClassifierLearning(HiddenMarkovClassifier classifier,
            Func<int, IUnsupervisedLearning<HiddenMarkovModel, int[], int[]>> learner)
            : base(classifier, learner)
        {
            createSmoothingKernel();
        }

        /// <summary>
        ///     Gets or sets the smoothing kernel's sigma
        ///     for the threshold model.
        /// </summary>
        /// <value>The smoothing kernel's sigma.</value>
        public double Smoothing
        {
            get => smoothingSigma;
            set
            {
                smoothingSigma = value;
                createSmoothingKernel();
            }
        }

        /// <summary>
        ///     Creates an instance of the model to be learned. Inheritors of this abstract
        ///     class must define this method so new models can be created from the training data.
        /// </summary>
        protected override HiddenMarkovClassifier Create(int[][] x, int[] y, int numberOfClasses)
        {
            return new HiddenMarkovClassifier(numberOfClasses);
        }

        /// <summary>
        ///     Trains each model to recognize each of the output labels.
        /// </summary>
        /// <returns>The sum log-likelihood for all models after training.</returns>
        [Obsolete("Please use the Learn(x, y) method instead.")]
        public double Run(int[][] inputs, int[] outputs)
        {
            if (inputs == null)
            {
                throw new ArgumentNullException("inputs");
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                for (var j = 0; j < inputs[i].Length; j++)
                {
                    var symbol = inputs[i][j];

                    if (symbol < 0 || symbol >= Classifier.Symbols)
                    {
                        var message = "Observation sequences should only contain symbols that are " +
                                      "greater than or equal to 0, and lesser than the number of symbols passed to " +
                                      "the HiddenMarkovClassifier. This classifier is expecting at most {0} symbols.";

                        throw new ArgumentOutOfRangeException("inputs", string.Format(message, Classifier.Symbols));
                    }
                }
            }

            Learn(inputs, outputs);
            return LogLikelihood;
        }

        /// <summary>
        ///     Compute model error for a given data set.
        /// </summary>
        /// <param name="inputs">The input points.</param>
        /// <param name="outputs">The output points.</param>
        /// <returns>The percent of misclassification errors for the data.</returns>
        public double ComputeError(int[][] inputs, int[] outputs)
        {
            // TODO: Mark as obsolete?

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
        public override HiddenMarkovModel Threshold()
        {
            var models = Classifier.Models;
            var topology = CreateThresholdTopology();

            var states = topology.States;
            var symbols = models[0].Symbols;

            // Create the threshold emission matrix
            var emissions = new double[states, symbols];


            // Then, for each hidden Markov model in the classifier
            for (int i = 0, modelStartIndex = 0; i < models.Length; i++)
            {
                // Retrieve the model definition matrices
                var B = models[i].Emissions.Exp();

                // Now, for each state 'j' in the model
                for (var j = 0; j < models[i].States; j++)
                {
                    var stateIndex = j + modelStartIndex;

                    // Copy state emissions from the model
                    emissions.SetRow(stateIndex, B.GetRow(j));
                }

                // Next model starts where this ends
                modelStartIndex += models[i].States;
            }


            // Apply smoothing
            if (smoothingSigma > 0)
            {
                for (var i = 0; i < states; i++)
                {
                    var e = emissions.GetRow(i);
                    var g = e.Convolve(gaussianKernel, true);
                    g = g.Divide(g.Sum()); // Make probabilities
                    emissions.SetRow(i, g);
                }
            }


            Debug.Assert(!emissions.HasNaN());


            // Create and return the threshold hidden Markov model
            return new HiddenMarkovModel(topology, emissions)
            {
                Tag = "Non-gesture"
            };
        }

        private void createSmoothingKernel()
        {
            var g = new Gaussian(smoothingSigma);
            gaussianKernel = g.Kernel(smoothingKernelSize);

            // Normalize
            var norm = gaussianKernel.Euclidean();
            gaussianKernel = gaussianKernel.Divide(norm);
        }
    }
}