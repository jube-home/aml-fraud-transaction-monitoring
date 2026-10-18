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
    ///     Arbitrary-density Hidden Markov Model Set for Sequence Classification.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class uses a set of
    ///         <see cref="HiddenMarkovModel{TDistribution}">
    ///             density hidden
    ///             Markov models
    ///         </see>
    ///         to classify sequences of real (double-precision floating point)
    ///         numbers or arrays of those numbers. Each model will try to learn and recognize each
    ///         of the different output classes. For examples and details on how to learn such models,
    ///         please take a look on the documentation for
    ///         <see cref="HiddenMarkovClassifierLearning{TDistribution}" />.
    ///     </para>
    ///     <para>
    ///         For the discrete version of this classifier, please see its non-generic counterpart
    ///         <see cref="HiddenMarkovClassifier" />.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Examples are available at the respective learning algorithm pages. For
    ///         example, see <see cref="HiddenMarkovClassifierLearning{TDistribution, TObservation}" />.
    ///     </para>
    /// </example>
    /// <seealso cref="HiddenMarkovClassifierLearning{TDistribution}" />
    /// <seealso cref="HiddenMarkovClassifier" />
    [Serializable]
    public class HiddenMarkovClassifier<TDistribution, TObservation> :
        BaseHiddenMarkovClassifier<HiddenMarkovModel<TDistribution, TObservation>, TDistribution, TObservation>,
        IEnumerable<HiddenMarkovModel<TDistribution, TObservation>>
        where TDistribution : IDistribution<TObservation>
    {
        /// <summary>
        ///     Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        /// <param name="classes">The number of classes in the classifier.</param>
        public HiddenMarkovClassifier(int classes)
            : base(classes)
        {
        }

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
        public HiddenMarkovClassifier(int classes, int[] states, Func<int, int, TDistribution> initial)
            : base(classes)
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
        public HiddenMarkovClassifier(int classes, ITopology topology, Func<int, int, TDistribution> initial)
            : base(classes)
        {
            for (var i = 0; i < classes; i++)
            {
                var classIndex = i;
                Models[i] = new HiddenMarkovModel<TDistribution, TObservation>(topology,
                    stateIndex => initial(classIndex, stateIndex));
            }
        }

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
                Models[i] = new HiddenMarkovModel<TDistribution, TObservation>(states[i], initial);
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
                Models[i] = new HiddenMarkovModel<TDistribution, TObservation>(topology, initial);
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
                Models[i] = new HiddenMarkovModel<TDistribution, TObservation>(topology, initial[i]);
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
                Models[i] = new HiddenMarkovModel<TDistribution, TObservation>(topology[i], initial[i]);
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
                Models[i] = new HiddenMarkovModel<TDistribution, TObservation>(topology[i], initial);
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
                Models[i] = new HiddenMarkovModel<TDistribution, TObservation>(topology[i], initial) { Tag = names[i] };
            }
        }

        /// <summary>
        ///     Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        /// <param name="models">
        ///     The models specializing in each of the classes of
        ///     the classification problem.
        /// </param>
        public HiddenMarkovClassifier(HiddenMarkovModel<TDistribution, TObservation>[] models)
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
                Models[i] = new HiddenMarkovModel<TDistribution, TObservation>(topology, initial) { Tag = names[i] };
            }
        }
    }
}