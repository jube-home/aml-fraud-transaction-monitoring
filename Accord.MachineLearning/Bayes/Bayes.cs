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
using Accord.Math;
using Accord.Statistics.Distributions;

namespace Accord.MachineLearning.Bayes
{
    /// <summary>
    ///     Bayes decision algorithm (not naive).
    /// </summary>
    /// <typeparam name="TDistribution">The type for the distributions used to model each class.</typeparam>
    /// <typeparam name="TInput">The type for the samples modeled by the distributions.</typeparam>
    /// <seealso cref="NaiveBayes" />
    /// <seealso cref="NaiveBayes{TDistribution}" />
    /// <seealso cref="NaiveBayes{TDistribution, TInput}" />
    [Serializable]
    public class Bayes<TDistribution, TInput> : MulticlassLikelihoodClassifierBase<TInput>
        where TDistribution : IFittableDistribution<TInput>,
        IMultivariateDistribution<TInput>
    {
        private TDistribution[] distributions;
        private double[] priors;

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initializer">
        ///     An initialization function used to create the distribution functions for
        ///     each class. Those will be available in the <see cref="Bayes{TDistribution, TInput}.Distributions" /> property.
        /// </param>
        public Bayes(int classes, int inputs, Func<TDistribution> initializer)
        {
            init(classes, inputs);
            for (var i = 0; i < distributions.Length; i++)
            {
                distributions[i] = initializer();
            }
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initializer">
        ///     An initialization function used to create the distribution functions for
        ///     each class. Those will be available in the <see cref="Bayes{TDistribution, TInput}.Distributions" /> property.
        /// </param>
        public Bayes(int classes, int inputs, Func<int, TDistribution> initializer)
        {
            init(classes, inputs);
            for (var i = 0; i < distributions.Length; i++)
            {
                distributions[i] = initializer(i);
            }
        }

        /// <summary>
        ///     Gets the probability distributions for each class and input.
        /// </summary>
        /// <value>
        ///     A TDistribution[,] array in with each row corresponds to a
        ///     class, each column corresponds to an input variable. Each element
        ///     of this double[,] array is a probability distribution modeling
        ///     the occurrence of the input variable in the corresponding class.
        /// </value>
        public TDistribution[] Distributions
        {
            get => distributions;
            set
            {
                if (!value.DimensionEquals(distributions))
                {
                    throw new DimensionMismatchException("value");
                }

                distributions = value;
            }
        }

        /// <summary>
        ///     Gets the prior beliefs for each class.
        /// </summary>
        public virtual double[] Priors
        {
            get => priors;
            set
            {
                if (!value.DimensionEquals(priors))
                {
                    throw new DimensionMismatchException("value");
                }

                priors = value;
            }
        }

        private void init(int classes, int inputs)
        {
            if (classes < 2)
            {
                throw new ArgumentOutOfRangeException("classes",
                    "The number of classses should be higher than 1 (currently detected: {0}).".Format(classes));
            }

            if (inputs <= 0)
            {
                throw new ArgumentOutOfRangeException("inputs",
                    "The number of inputs should be higher than 0 (currently detected: {0}).".Format(inputs));
            }

            NumberOfOutputs = classes;
            NumberOfClasses = classes;
            NumberOfInputs = inputs;

            distributions = new TDistribution[classes];

            priors = new double[NumberOfOutputs];
            for (var i = 0; i < priors.Length; i++)
            {
                priors[i] = 1.0 / priors.Length;
            }
        }

        /// <summary>
        ///     Computes the log-likelihood that the given input vector
        ///     belongs to the specified <paramref name="classIndex" />.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="classIndex">The index of the class whose score will be computed.</param>
        public override double LogLikelihood(TInput input, int classIndex)
        {
            var log = distributions[classIndex].LogProbabilityFunction(input);
            return System.Math.Log(priors[classIndex]) + log;
        }
    }
}