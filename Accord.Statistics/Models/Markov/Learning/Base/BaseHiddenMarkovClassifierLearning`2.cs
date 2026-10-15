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
using System.Threading.Tasks;
using Accord.Math;
using Accord.Statistics.Models.Markov.Topology;

namespace Accord.Statistics.Models.Markov.Learning
{
#pragma warning disable 612, 618

    /// <summary>
    ///     Configuration function delegate for Sequence Classifier Learning algorithms.
    /// </summary>
    public delegate IUnsupervisedLearning ClassifierLearningAlgorithmConfiguration(int modelIndex);

    /// <summary>
    ///     Submodel learning event arguments.
    /// </summary>
    public class GenerativeLearningEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GenerativeLearningEventArgs" /> class.
        /// </summary>
        /// <param name="classLabel">The class label.</param>
        /// <param name="classes">The total number of classes.</param>
        public GenerativeLearningEventArgs(int classLabel, int classes)
        {
            Class = classLabel;
            Total = classes;
        }

        /// <summary>
        ///     Gets the generative class model to
        ///     which this event refers to.
        /// </summary>
        public int Class { get; set; }

        /// <summary>
        ///     Gets the total number of models
        ///     to be learned.
        /// </summary>
        public int Total { get; set; }
    }

    /// <summary>
    ///     Abstract base class for Sequence Classifier learning algorithms.
    /// </summary>
    public abstract class BaseHiddenMarkovClassifierLearning<TClassifier, TModel>
        where TClassifier : BaseHiddenMarkovClassifier<TModel>
        where TModel : IHiddenMarkovModel
    {
        /// <summary>
        ///     Creates a new instance of the learning algorithm for a given
        ///     Markov sequence classifier using the specified configuration
        ///     function.
        /// </summary>
        [Obsolete("Please set the learning algorithm using the Learner property.")]
        protected BaseHiddenMarkovClassifierLearning(TClassifier classifier,
            ClassifierLearningAlgorithmConfiguration algorithm)
        {
            Classifier = classifier;
            Algorithm = algorithm;
        }

        /// <summary>
        ///     Creates a new instance of the learning algorithm for a given
        ///     Markov sequence classifier.
        /// </summary>
        protected BaseHiddenMarkovClassifierLearning(TClassifier classifier)
        {
            Classifier = classifier;
        }

        /// <summary>
        ///     Gets the classifier being trained by this instance.
        /// </summary>
        /// <value>The classifier being trained by this instance.</value>
        public TClassifier Classifier { get; }

        /// <summary>
        ///     Gets or sets the configuration function specifying which
        ///     training algorithm should be used for each of the models
        ///     in the hidden Markov model set.
        /// </summary>
        public ClassifierLearningAlgorithmConfiguration Algorithm { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether a threshold model
        ///     should be created or updated after training to support rejection.
        /// </summary>
        /// <value>
        ///     <c>true</c> to update the threshold model after training;
        ///     otherwise, <c>false</c>.
        /// </value>
        public bool Rejection { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the class priors
        ///     should be estimated from the data, as in an empirical Bayes method.
        /// </summary>
        public bool Empirical { get; set; }

        /// <summary>
        ///     Occurs when the learning of a class model has started.
        /// </summary>
        public event EventHandler<GenerativeLearningEventArgs> ClassModelLearningStarted;

        /// <summary>
        ///     Occurs when the learning of a class model has finished.
        /// </summary>
        public event EventHandler<GenerativeLearningEventArgs> ClassModelLearningFinished;

        /// <summary>
        ///     Trains each model to recognize each of the output labels.
        /// </summary>
        /// <returns>The sum log-likelihood for all models after training.</returns>
        [Obsolete("Please use the Learn method.")]
        protected double Run<T>(T[] inputs, int[] outputs)
        {
            if (inputs == null)
            {
                throw new ArgumentNullException("inputs");
            }

            if (outputs == null)
            {
                throw new ArgumentNullException("outputs");
            }

            if (inputs.Length != outputs.Length)
            {
                throw new DimensionMismatchException("outputs",
                    "The number of inputs and outputs does not match.");
            }

            for (var i = 0; i < outputs.Length; i++)
            {
                if (outputs[i] < 0 || outputs[i] >= Classifier.Classes)
                {
                    throw new ArgumentOutOfRangeException("outputs");
                }
            }


            var classes = Classifier.Classes;
            var logLikelihood = new double[classes];
            var classCounts = new int[classes];


            // For each model,
            Parallel.For(0, classes, i =>
            {
                // We will start the class model learning problem
                var args = new GenerativeLearningEventArgs(i, classes);
                OnGenerativeClassModelLearningStarted(args);

                // Select the input/output set corresponding
                //  to the model's specialization class
                var inx = outputs.Find(y => y == i);
                var observations = inputs.Get(inx);

                classCounts[i] = observations.Length;

                if (observations.Length > 0)
                {
                    // Create and configure the learning algorithm
                    var teacher = Algorithm(i);

                    // Train the current model in the input/output subset
                    logLikelihood[i] = teacher.Run(observations as Array[]);
                }

                // Update and report progress
                OnGenerativeClassModelLearningFinished(args);
            });

            if (Empirical)
            {
                for (var i = 0; i < classes; i++)
                {
                    Classifier.Priors[i] = (double)classCounts[i] / inputs.Length;
                }
            }

            if (Rejection)
            {
                Classifier.Threshold = Threshold();
            }

            // Returns the sum log-likelihood for all models.
            return logLikelihood.Sum();
        }

        /// <summary>
        ///     Creates a new <see cref="Threshold">threshold model</see>
        ///     for the current set of Markov models in this sequence classifier.
        /// </summary>
        /// <returns>A <see cref="Threshold">threshold Markov model</see>.</returns>
        public abstract TModel Threshold();

        /// <summary>
        ///     Creates the state transition topology for the threshold model. This
        ///     method can be used to help in the implementation of the <see cref="Threshold" />
        ///     abstract method which has to be defined for implementers of this class.
        /// </summary>
        protected ITopology CreateThresholdTopology()
        {
            var models = Classifier.Models;

            var states = 0;

            // Get the total number of states
            for (var i = 0; i < models.Length; i++)
            {
                states += models[i].States;
            }

            // Create the threshold model transition matrix
            var transitions = new double[states, states];

            // Set the initial probabilities
            var initial = new double[states];
            for (var i = 0; i < initial.Length; i++)
            {
                initial[i] = 1.0 / states;
            }


            // Then, for each hidden Markov model in the classifier
            for (int i = 0, modelStartIndex = 0; i < models.Length; i++)
            {
                // Now, for each state 'j' in the model
                for (var j = 0; j < models[i].States; j++)
                {
                    // Retrieve the state self-transition probability
                    var self = System.Math.Exp(models[i].Transitions[j, j]);

                    // Make sure the exp-log conversion was within limits
                    if (self < 0)
                    {
                        self = 0;
                    }
                    else if (self > 1)
                    {
                        self = 1;
                    }

                    // Check where we should write it
                    var stateIndex = modelStartIndex + j;

                    // Copy the self-transition probability
                    transitions[stateIndex, stateIndex] = self;

                    // And normalize all others to sum up to one
                    var pinv = (1.0 - self) / (models[i].States - 1);

                    for (var k = 0; k < models[i].States; k++)
                    {
                        if (j != k)
                        {
                            transitions[stateIndex, modelStartIndex + k] = pinv;
                        }
                    }

#if DEBUG
                    // Rows should sum up to one.
                    check(transitions, stateIndex);
#endif
                }

                // Next model starts where this ends
                modelStartIndex += models[i].States;
            }


            // Create and return the custom threshold topology
            return new Custom(transitions, initial, false);
        }

        private static void check(double[,] transitions, int index)
        {
            // Check if they indeed sum up to one
            var modelRow = transitions.GetRow(index);
            var modelRowSum = modelRow.Sum();
            if (System.Math.Abs(modelRowSum - 1.0) >= 1e-4)
            {
                throw new InvalidOperationException("Rows do not sum to one.");
            }
        }

        /// <summary>
        ///     Raises the <see cref="E:ClassModelLearningFinished" /> event.
        /// </summary>
        /// <param name="args">The <see cref="GenerativeLearningEventArgs" /> instance containing the event data.</param>
        protected void OnGenerativeClassModelLearningFinished(GenerativeLearningEventArgs args)
        {
            if (ClassModelLearningFinished != null)
            {
                ClassModelLearningFinished(this, args);
            }
        }

        /// <summary>
        ///     Raises the <see cref="E:ClassModelLearningStarted" /> event.
        /// </summary>
        /// <param name="args">The <see cref="GenerativeLearningEventArgs" /> instance containing the event data.</param>
        protected void OnGenerativeClassModelLearningStarted(GenerativeLearningEventArgs args)
        {
            if (ClassModelLearningStarted != null)
            {
                ClassModelLearningStarted(this, args);
            }
        }
    }
}