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

using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions
{
    /// <summary>
    ///     Common interface for distributions which can be estimated from data.
    /// </summary>
    /// <typeparam name="TObservations">The type of the observations, such as <see cref="System.Double" />.</typeparam>
    /// <typeparam name="TOptions">The type of the options specifying object.</typeparam>
    public interface IFittableDistribution<in TObservations, in TOptions> :
        IFittable<TObservations, TOptions>,
        IFittableDistribution<TObservations>
        where TOptions : class, IFittingOptions
    {
    }

    /// <summary>
    ///     Common interface for distributions which can be estimated from data.
    /// </summary>
    /// <typeparam name="TObservations">The type of the observations, such as <see cref="System.Double" />.</typeparam>
    /// <typeparam name="TOptions">The type of the options specifying object.</typeparam>
    public interface IFittable<in TObservations, in TOptions> :
        IFittable<TObservations>
        where TOptions : class, IFittingOptions
    {
        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        void Fit(TObservations[] observations, double[] weights = null, TOptions options = default);
    }

    /// <summary>
    ///     Common interface for distributions which can be estimated from data.
    /// </summary>
    /// <typeparam name="TObservations">The type of the observations, such as <see cref="System.Double" />.</typeparam>
    public interface IFittableDistribution<in TObservations> :
        IFittable<TObservations>, IDistribution<TObservations>
    {
    }

    /// <summary>
    ///     Common interface for distributions which can be estimated from data.
    /// </summary>
    public interface IUnivariateFittableDistribution :
        IFittableDistribution<double>,
        IUnivariateDistribution<double>,
        IUnivariateDistribution
    {
    }

    /// <summary>
    ///     Common interface for distributions which can be estimated from data.
    /// </summary>
    /// <typeparam name="TObservations">The type of the observations, such as <see cref="System.Double" />.</typeparam>
    public interface IFittable<in TObservations>
    {
        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        void Fit(TObservations[] observations);

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        void Fit(TObservations[] observations, double[] weights);
    }
}