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

using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.MachineLearning.Bayes
{
#if !MONO
    /// <summary>
    ///     Base class for Naive Bayes learning algorithms.
    /// </summary>
    /// <typeparam name="TModel">The type for the Naive Bayes model to be learned.</typeparam>
    /// <typeparam name="TDistribution">The univariate distribution to be used as components in the Naive Bayes distribution.</typeparam>
    /// <typeparam name="TInput">The type for the samples modeled by the distribution.</typeparam>
    /// <typeparam name="TOptions">The fitting options for the independent distribution.</typeparam>
    /// <typeparam name="TInnerOptions">The individual fitting options for the component distributions.</typeparam>
    public abstract class NaiveBayesLearningBase<TModel, TDistribution, TInput, TOptions, TInnerOptions> :
        NaiveBayesLearningBase<TModel, TDistribution, TInput, TOptions>
        where TDistribution : IFittableDistribution<TInput, TInnerOptions>,
        IUnivariateDistribution<TInput>,
        IUnivariateDistribution
        where TOptions : IndependentOptions<TInnerOptions>, new()
        where TInnerOptions : class, IFittingOptions, new()
        where TModel : NaiveBayes<TDistribution, TInput>
    {
        /// <summary>
        ///     Fits one of the distributions in the naive bayes model.
        /// </summary>
        protected override void Fit(int i, TInput[][] values, double[] weights, bool transposed)
        {
            Options.Transposed = transposed;
            Model.Distributions[i].Fit(values, weights, Options);
            optimized = true;
        }
    }
#endif
}