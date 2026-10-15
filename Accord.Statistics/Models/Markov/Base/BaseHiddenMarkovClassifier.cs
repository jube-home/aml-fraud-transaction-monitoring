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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Accord.Math;

namespace Accord.Statistics.Models.Markov
{
    /// <summary>
    ///     Base class for (HMM) Sequence Classifiers.
    ///     This class cannot be instantiated.
    /// </summary>
    [Obsolete("This class will be removed.")]
    [Serializable]
    public abstract class BaseHiddenMarkovClassifier<TModel> : IEnumerable<TModel>
        where TModel : IHiddenMarkovModel
    {
        // Threshold (rejection) model
        private TModel threshold;
        private double weight = 1;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseHiddenMarkovClassifier&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="classes">The number of classes in the classification problem.</param>
        protected BaseHiddenMarkovClassifier(int classes)
        {
            Models = new TModel[classes];

            Priors = new double[classes];
            for (var i = 0; i < Priors.Length; i++)
            {
                Priors[i] = 1.0 / Priors.Length;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseHiddenMarkovClassifier&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="models">The models specializing in each of the classes of the classification problem.</param>
        protected BaseHiddenMarkovClassifier(TModel[] models)
        {
            this.Models = models;

            Priors = new double[models.Length];
            for (var i = 0; i < Priors.Length; i++)
            {
                Priors[i] = 1.0 / Priors.Length;
            }
        }

        /// <summary>
        ///     Gets or sets the threshold model.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For gesture spotting, Lee and Kim introduced a threshold model which is
        ///         composed of parts of the models in a hidden Markov sequence classifier.
        ///     </para>
        ///     <para>
        ///         The threshold model acts as a baseline for decision rejection. If none of
        ///         the classifiers is able to produce a higher likelihood than the threshold
        ///         model, the decision is rejected.
        ///     </para>
        ///     <para>
        ///         In the original Lee and Kim publication, the threshold model is constructed
        ///         by creating a fully connected ergodic model by removing all outgoing transitions
        ///         of states in all gesture models and fully connecting those states.
        ///     </para>
        ///     <para>
        ///         References:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     H. Lee, J. Kim, An HMM-based threshold model approach for gesture
        ///                     recognition, IEEE Trans. Pattern Anal. Mach. Intell. 21 (10) (1999)
        ///                     961–973.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        public TModel Threshold
        {
            get => threshold;
            set => threshold = value;
        }

        /// <summary>
        ///     Gets or sets a value governing the rejection given by
        ///     a threshold model (if present). Increasing this value
        ///     will result in higher rejection rates. Default is 1.
        /// </summary>
        public double Sensitivity
        {
            get => weight;
            set => weight = value;
        }

        /// <summary>
        ///     Gets the collection of models specialized in each
        ///     class of the sequence classification problem.
        /// </summary>
        public TModel[] Models { get; }

        /// <summary>
        ///     Gets the
        ///     <see cref="IHiddenMarkovModel">
        ///         Hidden Markov
        ///         Model
        ///     </see>
        ///     implementation responsible for recognizing
        ///     each of the classes given the desired class label.
        /// </summary>
        /// <param name="label">The class label of the model to get.</param>
        public TModel this[int label] => Models[label];

        /// <summary>
        ///     Gets the number of classes which can be recognized by this classifier.
        /// </summary>
        public int Classes => Models.Length;

        /// <summary>
        ///     Gets the prior distribution assumed for the classes.
        /// </summary>
        public double[] Priors { get; }

        /// <summary>
        ///     Returns an enumerator that iterates through the models in the classifier.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that
        ///     can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<TModel> GetEnumerator()
        {
            foreach (var model in Models)
            {
                yield return model;
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the models in the classifier.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that
        ///     can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var model in Models)
            {
                yield return model;
            }
        }

        /// <summary>
        ///     Computes the most likely class for a given sequence.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <returns>
        ///     Return the label of the given sequence, or -1 if it has
        ///     been rejected by the <see cref="Threshold">threshold model</see>.
        /// </returns>
        protected int Compute(Array sequence)
        {
            return Compute(sequence, out double[] _);
        }

        /// <summary>
        ///     Computes the most likely class for a given sequence.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <param name="response">The probability of the assigned class.</param>
        /// <returns>
        ///     Return the label of the given sequence, or -1 if it has
        ///     been rejected by the <see cref="Threshold">threshold model</see>.
        /// </returns>
        protected int Compute(Array sequence, out double response)
        {
            double[] likelihoods;
            var label = Compute(sequence, out likelihoods);

            if (label == -1)
            {
                // The sequence has been rejected
                response = 1.0 - likelihoods.Sum();
            }
            else
            {
                // Successful recognition
                response = likelihoods[label];
            }

            return label;
        }

        /// <summary>
        ///     Computes the most likely class for a given sequence.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <param name="responsibilities">The probabilities for each class.</param>
        /// <returns>
        ///     Return the label of the given sequence, or -1 if it has
        ///     been rejected by the <see cref="Threshold">threshold model</see>.
        /// </returns>
        protected int Compute(Array sequence, out double[] responsibilities)
        {
            double maxValue, thresholdValue;
            var imax = compute(sequence, out responsibilities, out thresholdValue, out maxValue);

            // Convert to probabilities
            for (var i = 0; i < responsibilities.Length; i++)
            {
                responsibilities[i] = System.Math.Exp(responsibilities[i]);
            }

            // return the index of the most likely model
            // or -1 if the sequence has been rejected.
            //
            return thresholdValue > maxValue ? -1 : imax;
        }

        private int compute(Array sequence, out double[] logLikelihoods,
            out double rejectionValue, out double maxValue)
        {
            logLikelihoods = new double[Models.Length];

            var responses = logLikelihoods;
            var rejection = double.NegativeInfinity;

            // For every model in the set (including the threshold model)
#if SERIAL
            for (int i = 0; i < Models.Length+1; i++)
#else
            Parallel.For(0, Models.Length + 1, i =>
#endif
                {
                    if (i < Models.Length)
                    {
                        // Evaluate the probability of the sequence
                        responses[i] = Models[i].Evaluate(sequence) + System.Math.Log(Priors[i]);
                    }
                    else if (threshold != null)
                    {
                        // Evaluate the current rejection threshold 
                        rejection = threshold.Evaluate(sequence) + System.Math.Log(weight);
                    }
                }
#if !SERIAL
            );
#endif

            logLikelihoods = responses;
            rejectionValue = rejection;

            // Compute posterior likelihoods
            var lnsum = double.NegativeInfinity;
            for (var i = 0; i < Priors.Length; i++)
            {
                lnsum = Special.LogSum(lnsum, logLikelihoods[i]);
            }

            // Compute threshold model posterior likelihood
            if (threshold != null)
            {
                lnsum = Special.LogSum(lnsum, rejection);
            }

            int imax;

            // Get the index of the most likely model
            maxValue = logLikelihoods.Max(out imax);

            // Normalize if different from zero
            if (lnsum != double.NegativeInfinity)
            {
                for (var i = 0; i < logLikelihoods.Length; i++)
                {
                    logLikelihoods[i] -= lnsum;
                }
            }

            return imax;
        }

        /// <summary>
        ///     Computes the log-likelihood that a sequence
        ///     belongs to a given class according to this
        ///     classifier.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <param name="output">The output class label.</param>
        /// <returns>The log-likelihood of the sequence belonging to the given class.</returns>
        protected double LogLikelihood(Array sequence, int output)
        {
            compute(sequence, out var logLikelihoods, out _, out _);

            return logLikelihoods[output];
        }

        /// <summary>
        ///     Computes the log-likelihood that a sequence
        ///     belongs any of the classes in the classifier.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <returns>The log-likelihood of the sequence belonging to the classifier.</returns>
        protected double LogLikelihood(Array sequence)
        {
            var sum = double.NegativeInfinity;

            for (var i = 0; i < Models.Length; i++)
            {
                var prior = System.Math.Log(Priors[i]);
                var model = Models[i].Evaluate(sequence);
                var result = Special.LogSum(prior, model);

                sum = Special.LogSum(sum, result);
            }

            return sum;
        }

        /// <summary>
        ///     Computes the log-likelihood of a set of sequences
        ///     belonging to their given respective classes according
        ///     to this classifier.
        /// </summary>
        /// <param name="sequences">A set of sequences of observations.</param>
        /// <param name="outputs">The output class label for each sequence.</param>
        /// <returns>The log-likelihood of the sequences belonging to the given classes.</returns>
        protected double LogLikelihood(Array[] sequences, int[] outputs)
        {
            double sum = 0;
            for (var i = 0; i < sequences.Length; i++)
            {
                compute(sequences[i], out var logLikelihoods, out _, out _);

                sum += logLikelihoods[outputs[i]];
            }

            return sum;
        }

        [OnDeserializing]
        private void setSerializationDefaults(StreamingContext sc)
        {
            // In case this was an older model which didn't include a
            // configurable rejection threshold, initialize it with 1.

            weight = 1;
        }
    }
}