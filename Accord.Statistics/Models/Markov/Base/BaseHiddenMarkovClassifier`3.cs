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
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions;

namespace Accord.Statistics.Models.Markov
{
    /// <summary>
    ///     Base class for (HMM) Sequence Classifiers.
    ///     This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public abstract class BaseHiddenMarkovClassifier<TModel, TDistribution, TObservation> :
        MulticlassLikelihoodClassifierBase<TObservation[]>, IEnumerable<TModel> //, IParallel // TODO: Uncomment
        where TModel : HiddenMarkovModel<TDistribution, TObservation>
        where TDistribution : IDistribution<TObservation>
    {
        // TODO: Uncomment
        // ParallelLearningBase parallel;

        private TModel[] models;

        // Threshold (rejection) model
        private TModel threshold;
        private double weight = 1;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseHiddenMarkovClassifier&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="classes">The number of classes in the classification problem.</param>
        protected BaseHiddenMarkovClassifier(int classes)
            : this(new TModel[classes])
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseHiddenMarkovClassifier&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="models">The models specializing in each of the classes of the classification problem.</param>
        protected BaseHiddenMarkovClassifier(TModel[] models)
        {
            Models = models;
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
        public TModel[] Models
        {
            get => models;
            set
            {
                var classes = value.Length;
                models = value;
                NumberOfOutputs = classes;
                NumberOfClasses = classes;
                Priors = new double[classes];
                for (var i = 0; i < Priors.Length; i++)
                {
                    Priors[i] = 1.0 / Priors.Length;
                }
            }
        }

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
        public TModel this[int label] => models[label];

        /// <summary>
        ///     Gets the number of classes which can be recognized by this classifier.
        /// </summary>
        [Obsolete("Please use NumberOfClasses instead.")]
        public int Classes => models.Length;

        /// <summary>
        ///     Gets the prior distribution assumed for the classes.
        /// </summary>
        public double[] Priors { get; private set; }

        /// <summary>
        ///     Returns an enumerator that iterates through the models in the classifier.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that
        ///     can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<TModel> GetEnumerator()
        {
            foreach (var model in models)
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
            foreach (var model in models)
            {
                yield return model;
            }
        }

        // TODO: Uncomment
        ///// <summary>
        ///// Gets or sets the parallelization options for this algorithm.
        ///// </summary>
        ///// 
        ///// <value>The parallel options.</value>
        ///// 
        //public ParallelOptions ParallelOptions
        //{
        //    get { return parallel.ParallelOptions; }
        //    set { parallel.ParallelOptions = value; }
        //}

        ///// <summary>
        /////   Gets or sets a cancellation token that can be used
        /////   to cancel the algorithm while it is running.
        ///// </summary>
        ///// 
        ///// <value>The token.</value>
        ///// 
        //public CancellationToken Token
        //{
        //    get { return parallel.Token; }
        //    set { parallel.Token = value; }
        //}

        /// <summary>
        ///     Computes the log-likelihood that the given input vector
        ///     belongs to its decided class.
        /// </summary>
        public override double LogLikelihood(TObservation[] input)
        {
            return LogLikelihood(input, out int _);
        }

        /// <summary>
        ///     Computes the likelihood that the given input vector
        ///     belongs to its decided class.
        /// </summary>
        public override double Probability(TObservation[] input)
        {
            return Probability(input, out int _);
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input vector
        ///     belongs to its decided class.
        /// </summary>
        public override double LogLikelihood(TObservation[] input, out int decision)
        {
            var result = LogLikelihoods(input, out decision);
            if (decision == -1)
            {
                return Special.Log1m(1.0 - System.Math.Exp(result.LogSumExp()));
            }

            return result[decision];
        }

        /// <summary>
        ///     Computes the probability that the given input vector
        ///     belongs to its decided class.
        /// </summary>
        public override double Probability(TObservation[] input, out int decision)
        {
            var result = Probabilities(input, out decision);
            if (decision == -1)
            {
                return 1.0 - result.Sum();
            }

            return result[decision];
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        /// <returns></returns>
        public override double LogLikelihood(TObservation[] input, int classIndex)
        {
            return LogLikelihoods(input, out int _)[classIndex];
        }

        /// <summary>
        ///     Computes the probabilities that the given input
        ///     vector belongs to each of the possible classes.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The decided class for the input.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        /// <returns></returns>
        public override double[] Probabilities(TObservation[] input, out int decision, double[] result)
        {
            LogLikelihoods(input, out decision, result);
            return result.Exp(result);
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vector, returning the
        ///     log-likelihoods of the input vector belonging to each possible class.
        /// </summary>
        /// <param name="input">A set of input vectors.</param>
        /// <param name="decision">The decided class for the input.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        /// <returns></returns>
        //public override double[][] LogLikelihoods(TObservation[][] input, int[] decision, double[][] result)
        public override double[] LogLikelihoods(TObservation[] input, out int decision, double[] result)
        {
            // TODO: Uncomment this code
            //Parallel.For(0, input.Length, ParallelOptions, k =>
            //{
            //    double[] r = result[k];
            //    TObservation[] o = input[k];

            var r = result;
            var o = input;

            // Evaluate the probability of the sequence for every model in the set
            for (var i = 0; i < models.Length; i++)
            {
                r[i] = models[i].LogLikelihood(o) + System.Math.Log(Priors[i]);
            }

            // Get the index of the most likely model
            var maxValue = r.Max(out decision);

            // Compute posterior likelihoods
            var lnsum = r.LogSumExp();

            // Compute threshold model posterior likelihood
            if (threshold != null)
            {
                // Evaluate the current rejection threshold 
                var rejection = threshold.LogLikelihood(o) + System.Math.Log(weight);

                if (rejection > maxValue)
                {
                    decision = -1; // input should be rejected (does not belong to any of the classes)
                }

                lnsum = Special.LogSum(lnsum, rejection);
            }

            // Normalize if different from zero
            if (lnsum != double.NegativeInfinity)
            {
                r.Subtract(lnsum, r);
            }
            //});

            return result;
        }

        /// <summary>
        ///     Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">
        ///     The input vector that should be classified into
        ///     one of the <see cref="ITransform.NumberOfOutputs" /> possible classes.
        /// </param>
        /// <returns>
        ///     A class-label that best described <paramref name="input" /> according
        ///     to this classifier.
        /// </returns>
        public override int Decide(TObservation[] input)
        {
            int decision;
            LogLikelihoods(input, out decision);
            return decision;
        }
    }
}