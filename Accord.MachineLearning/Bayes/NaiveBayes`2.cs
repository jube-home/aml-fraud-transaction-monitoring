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
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Multivariate;

namespace Accord.MachineLearning.Bayes
{
#if !MONO
    /// <summary>
    ///     Naïve Bayes Classifier for arbitrary distributions of arbitrary elements.
    /// </summary>
    /// <seealso cref="NaiveBayes" />
    /// <seealso cref="NaiveBayes{TDistribution}" />
    /// <seealso cref="NaiveBayesLearning{TDistribution, TInput}" />
    [Serializable]
    public class NaiveBayes<TDistribution, TInput> :
        Bayes<Independent<TDistribution, TInput>, TInput[]>
        where TDistribution : IFittableDistribution<TInput>,
        IUnivariateDistribution<TInput>,
        IUnivariateDistribution
    {
        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initializer">
        ///     An initialization function used to create the distribution functions for
        ///     each class. Those will be available in the <see cref="Bayes{TDistribution, TInput}.Distributions" /> property.
        /// </param>
        public NaiveBayes(int classes, int inputs, Func<int, TDistribution> initializer)
            : base(classes, inputs, j => new Independent<TDistribution, TInput>(inputs, initializer))
        {
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initial">
        ///     An initialization function used to create the distribution functions for
        ///     each class. Those will be available in the <see cref="Bayes{TDistribution, TInput}.Distributions" /> property.
        /// </param>
        public NaiveBayes(int classes, int inputs, Func<int, int, TDistribution> initial)
            : base(classes, inputs,
                classIndex => new Independent<TDistribution, TInput>(inputs,
                    variableIndex => (TDistribution)initial(classIndex, variableIndex).Clone()))
        {
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initial">
        ///     An initial distribution to be used to initialized all independent
        ///     distribution components of this Naive Bayes. Those distributions
        ///     will made available in the <see cref="Bayes{TDistribution, TInput}.Distributions" /> property.
        /// </param>
        public NaiveBayes(int classes, int inputs, TDistribution initial)
            : base(classes, inputs,
                () => new Independent<TDistribution, TInput>(inputs, (TDistribution)initial.Clone()))
        {
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initial">
        ///     An initial distribution to be used to initialized all independent
        ///     distribution components of this Naive Bayes. Those distributions
        ///     will made available in the <see cref="Bayes{TDistribution, TInput}.Distributions" /> property.
        /// </param>
        public NaiveBayes(int classes, int inputs, TDistribution[] initial)
            : base(classes, inputs,
                () => new Independent<TDistribution, TInput>(inputs, j => (TDistribution)initial[j].Clone()))
        {
        }

        /// <summary>
        ///     Constructs a new Naïve Bayes Classifier.
        /// </summary>
        /// <param name="classes">The number of output classes.</param>
        /// <param name="inputs">The number of input variables.</param>
        /// <param name="initial">
        ///     An initial distribution to be used to initialized all independent
        ///     distribution components of this Naive Bayes. Those distributions
        ///     will made available in the <see cref="Bayes{TDistribution, TInput}.Distributions" /> property.
        /// </param>
        public NaiveBayes(int classes, int inputs, TDistribution[][] initial)
            : base(classes, inputs,
                i => new Independent<TDistribution, TInput>(inputs, j => (TDistribution)initial[i][j].Clone()))
        {
        }
    }
#else
    /// <summary>
    ///   This class is currently not supported in Mono due to
    ///   a bug in the Mono compiler.
    /// </summary>
    /// 
    [Obsolete("This class is not supported in Mono.")]
    public class NaiveBayes<T1, T2>
    {

    }
#endif
}