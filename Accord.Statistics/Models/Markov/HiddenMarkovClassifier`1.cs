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
using Accord.Statistics.Distributions;
using Accord.Statistics.Models.Markov.Learning;
using Accord.Statistics.Models.Markov.Topology;

#pragma warning disable 612, 618

namespace Accord.Statistics.Models.Markov
{
    /// <summary>
    ///     Obsolete. Please use <see cref="HiddenMarkovClassifier{TDistribution, TObservation}" /> instead.
    /// </summary>
    /// <seealso cref="HiddenMarkovClassifierLearning{TDistribution,TObservation}" />
    /// <seealso cref="HiddenMarkovClassifier" />
    [Serializable]
    [Obsolete("Please use HiddenMarkovClassifier<TDistribution, TObservation> instead.")]
    public class HiddenMarkovClassifier<TDistribution> : BaseHiddenMarkovClassifier<HiddenMarkovModel<TDistribution>>,
        IEnumerable<HiddenMarkovModel<TDistribution>>, IHiddenMarkovClassifier where TDistribution : IDistribution
    {
        /// <summary>
        ///     Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        /// <param name="classes">The number of classes in the classifier.</param>
        /// <param name="states">
        ///     An array specifying the number of hidden states for each
        ///     of the classifiers. By default, and Ergodic topology will be used.
        /// </param>
        /// <param name="initial">
        ///     The initial probability distributions for the hidden states.
        ///     For multivariate continuous density distributions, such as Normal mixtures, the
        ///     choice of initial values is crucial for a good performance.
        /// </param>
        public HiddenMarkovClassifier(int classes, int[] states, TDistribution initial)
            : base(classes)
        {
            for (var i = 0; i < classes; i++)
            {
                Models[i] = new HiddenMarkovModel<TDistribution>(states[i], initial);
            }
        }

        /// <summary>
        ///     Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        /// <param name="classes">The number of classes in the classifier.</param>
        /// <param name="topology">
        ///     The topology of the hidden states. A forward-only topology
        ///     is indicated to sequence classification problems, such as speech recognition.
        /// </param>
        /// <param name="initial">
        ///     The initial probability distributions for the hidden states.
        ///     For multivariate continuous density distributions, such as Normal mixtures, the
        ///     choice of initial values is crucial for a good performance.
        /// </param>
        public HiddenMarkovClassifier(int classes, ITopology topology, TDistribution initial)
            : base(classes)
        {
            for (var i = 0; i < classes; i++)
            {
                Models[i] = new HiddenMarkovModel<TDistribution>(topology, initial);
            }
        }

        /// <summary>
        ///     Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        /// <param name="classes">The number of classes in the classifier.</param>
        /// <param name="topology">
        ///     The topology of the hidden states. A forward-only topology
        ///     is indicated to sequence classification problems, such as speech recognition.
        /// </param>
        /// <param name="initial">
        ///     The initial probability distributions for the hidden states.
        ///     For multivariate continuous density distributions, such as Normal mixtures, the
        ///     choice of initial values is crucial for a good performance.
        /// </param>
        public HiddenMarkovClassifier(int classes, ITopology topology, TDistribution[] initial)
            : base(classes)
        {
            for (var i = 0; i < classes; i++)
            {
                Models[i] = new HiddenMarkovModel<TDistribution>(topology, initial[i]);
            }
        }

        /// <summary>
        ///     Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        /// <param name="classes">The number of classes in the classifier.</param>
        /// <param name="topology">
        ///     The topology of the hidden states. A forward-only topology
        ///     is indicated to sequence classification problems, such as speech recognition.
        /// </param>
        /// <param name="initial">
        ///     The initial probability distributions for the hidden states.
        ///     For multivariate continuous density distributions, such as Normal mixtures, the
        ///     choice of initial values is crucial for a good performance.
        /// </param>
        public HiddenMarkovClassifier(int classes, ITopology[] topology, TDistribution[] initial)
            : base(classes)
        {
            for (var i = 0; i < classes; i++)
            {
                Models[i] = new HiddenMarkovModel<TDistribution>(topology[i], initial[i]);
            }
        }

        /// <summary>
        ///     Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        /// <param name="classes">The number of classes in the classifier.</param>
        /// <param name="topology">
        ///     The topology of the hidden states. A forward-only topology
        ///     is indicated to sequence classification problems, such as speech recognition.
        /// </param>
        /// <param name="initial">
        ///     The initial probability distributions for the hidden states.
        ///     For multivariate continuous density distributions, such as Normal mixtures, the
        ///     choice of initial values is crucial for a good performance.
        /// </param>
        public HiddenMarkovClassifier(int classes, ITopology[] topology, TDistribution initial)
            : base(classes)
        {
            for (var i = 0; i < classes; i++)
            {
                Models[i] = new HiddenMarkovModel<TDistribution>(topology[i], initial);
            }
        }

        /// <summary>
        ///     Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        /// <param name="classes">The number of classes in the classifier.</param>
        /// <param name="topology">
        ///     The topology of the hidden states. A forward-only topology
        ///     is indicated to sequence classification problems, such as speech recognition.
        /// </param>
        /// <param name="initial">
        ///     The initial probability distributions for the hidden states.
        ///     For multivariate continuous density distributions, such as Normal mixtures, the
        ///     choice of initial values is crucial for a good performance.
        /// </param>
        /// <param name="names">The class labels for each of the models.</param>
        public HiddenMarkovClassifier(int classes, ITopology[] topology, TDistribution initial, string[] names)
            : base(classes)
        {
            for (var i = 0; i < classes; i++)
            {
                Models[i] = new HiddenMarkovModel<TDistribution>(topology[i], initial) { Tag = names[i] };
            }
        }

        /// <summary>
        ///     Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        /// <param name="models">
        ///     The models specializing in each of the classes of
        ///     the classification problem.
        /// </param>
        public HiddenMarkovClassifier(HiddenMarkovModel<TDistribution>[] models)
            : base(models)
        {
        }

        /// <summary>
        ///     Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        /// <param name="classes">The number of classes in the classifier.</param>
        /// <param name="topology">
        ///     The topology of the hidden states. A forward-only topology
        ///     is indicated to sequence classification problems, such as speech recognition.
        /// </param>
        /// <param name="initial">
        ///     The initial probability distributions for the hidden states.
        ///     For multivariate continuous density distributions, such as Normal mixtures, the
        ///     choice of initial values is crucial for a good performance.
        /// </param>
        /// <param name="names">The class labels for each of the models.</param>
        public HiddenMarkovClassifier(int classes, ITopology topology, TDistribution initial, string[] names)
            : base(classes)
        {
            for (var i = 0; i < classes; i++)
            {
                Models[i] = new HiddenMarkovModel<TDistribution>(topology, initial) { Tag = names[i] };
            }
        }

        /// <summary>
        ///     Gets the number of dimensions of the
        ///     observations handled by this classifier.
        /// </summary>
        public int Dimension => Models[0].Dimension;

        /// <summary>
        ///     Computes the most likely class for a given sequence.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <param name="responsibilities">
        ///     The class responsibilities (or
        ///     the probability of the sequence to belong to each class). When
        ///     using threshold models, the sum of the probabilities will not
        ///     equal one, and the amount left was the threshold probability.
        ///     If a threshold model is not being used, the array should sum to
        ///     one.
        /// </param>
        /// <returns>
        ///     Return the label of the given sequence, or -1 if it has
        ///     been rejected by the
        ///     <see cref="BaseHiddenMarkovClassifier{T}.Threshold">
        ///         threshold model
        ///     </see>
        ///     .
        /// </returns>
        public new int Compute(Array sequence, out double[] responsibilities)
        {
            return base.Compute(sequence, out responsibilities);
        }

        /// <summary>
        ///     Computes the most likely class for a given sequence.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <returns>
        ///     Return the label of the given sequence, or -1 if it has
        ///     been rejected by the
        ///     <see cref="BaseHiddenMarkovClassifier{T}.Threshold">
        ///         threshold model
        ///     </see>
        ///     .
        /// </returns>
        public new int Compute(Array sequence)
        {
            return base.Compute(sequence);
        }

        /// <summary>
        ///     Computes the most likely class for a given sequence.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <param name="response">The probability of the assigned class.</param>
        /// <returns>
        ///     Return the label of the given sequence, or -1 if it has
        ///     been rejected by the
        ///     <see cref="BaseHiddenMarkovClassifier{T}.Threshold">
        ///         threshold model
        ///     </see>
        ///     .
        /// </returns>
        public new int Compute(Array sequence, out double response)
        {
            return base.Compute(sequence, out response);
        }

        /// <summary>
        ///     Computes the log-likelihood of a sequence
        ///     belong to a given class according to this
        ///     classifier.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <param name="output">The output class label.</param>
        /// <returns>The log-likelihood of the sequence belonging to the given class.</returns>
        public new double LogLikelihood(Array sequence, int output)
        {
            return base.LogLikelihood(sequence, output);
        }

        /// <summary>
        ///     Computes the log-likelihood that a sequence
        ///     belongs any of the classes in the classifier.
        /// </summary>
        /// <param name="sequence">The sequence of observations.</param>
        /// <returns>The log-likelihood of the sequence belonging to the classifier.</returns>
        public new double LogLikelihood(Array sequence)
        {
            return base.LogLikelihood(sequence);
        }

        /// <summary>
        ///     Computes the log-likelihood of a set of sequences
        ///     belonging to their given respective classes according
        ///     to this classifier.
        /// </summary>
        /// <param name="sequences">A set of sequences of observations.</param>
        /// <param name="outputs">The output class label for each sequence.</param>
        /// <returns>The log-likelihood of the sequences belonging to the given classes.</returns>
        public new double LogLikelihood(Array[] sequences, int[] outputs)
        {
            return base.LogLikelihood(sequences, outputs);
        }
    }
}