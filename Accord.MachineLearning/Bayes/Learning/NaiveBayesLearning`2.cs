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
using Accord.Statistics.Distributions.Fitting;

namespace Accord.MachineLearning.Bayes
{
#if !MONO
    /// <summary>
    ///     Naïve Bayes learning algorithm.
    /// </summary>
    /// <example>
    ///     <para>
    ///         For basic examples on how to learn a Naive Bayes algorithm, please see
    ///         <see cref="NaiveBayes" /> page. The following examples show how to set
    ///         more specialized learning settings for Normal (Gaussian) models.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayes`1Test.cs" region="doc_learn" />
    /// </example>
    [Serializable]
    public class NaiveBayesLearning<TDistribution> :
        NaiveBayesLearningBase<NaiveBayes<TDistribution>,
            TDistribution, double, IndependentOptions>
        where TDistribution : IFittableDistribution<double>,
        IUnivariateDistribution<double>,
        IUnivariateDistribution
    {
        /// <summary>
        ///     Creates an instance of the model to be learned.
        /// </summary>
        protected override NaiveBayes<TDistribution> Create(double[][] x, int y)
        {
            var inputs = x[0].Length;
            return new NaiveBayes<TDistribution>(inputs: inputs, classes: y, initial: Distribution);
        }
    }

    /// <summary>
    ///     Naïve Bayes learning algorithm.
    /// </summary>
    /// <example>
    ///     <para>
    ///         For basic examples on how to learn a Naive Bayes algorithm, please see
    ///         <see cref="NaiveBayes" /> page. The following examples show how to set
    ///         more specialized learning settings for Normal (Gaussian) models.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayes`1Test.cs" region="doc_learn_options" />
    /// </example>
    [Serializable]
    public class NaiveBayesLearning<TDistribution, TOptions> :
        NaiveBayesLearningBase<NaiveBayes<TDistribution>,
            TDistribution, double, IndependentOptions<TOptions>>
        where TDistribution : IFittableDistribution<double>,
        IUnivariateDistribution<double>,
        IUnivariateDistribution
        where TOptions : class, IFittingOptions, new()
    {
        /// <summary>
        ///     Creates an instance of the model to be learned.
        /// </summary>
        protected override NaiveBayes<TDistribution> Create(double[][] x, int y)
        {
            var inputs = x[0].Length;
            return new NaiveBayes<TDistribution>(inputs: inputs, classes: y, initial: Distribution);
        }
    }

    /// <summary>
    ///     Naïve Bayes learning algorithm.
    /// </summary>
    /// <example>
    ///     <para>
    ///         For basic examples on how to learn a Naive Bayes algorithm, please see
    ///         <see cref="NaiveBayes" /> page. The following examples show how to set
    ///         more specialized learning settings for Normal (Gaussian) models.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Bayes\NaiveBayes`1Test.cs" region="doc_learn_options" />
    /// </example>
    [Serializable]
    public class NaiveBayesLearning<TDistribution, TOptions, TInput> :
        NaiveBayesLearningBase<NaiveBayes<TDistribution, TInput>,
            TDistribution, TInput, IndependentOptions<TOptions>>
        where TDistribution : IFittableDistribution<TInput>,
        IUnivariateDistribution<TInput>,
        IUnivariateDistribution
        where TOptions : class, IFittingOptions, new()
    {
        /// <summary>
        ///     Creates an instance of the model to be learned.
        /// </summary>
        protected override NaiveBayes<TDistribution, TInput> Create(TInput[][] x, int y)
        {
            var inputs = x[0].Length;
            return new NaiveBayes<TDistribution, TInput>(inputs: inputs, classes: y, initial: Distribution);
        }
    }
#endif
}