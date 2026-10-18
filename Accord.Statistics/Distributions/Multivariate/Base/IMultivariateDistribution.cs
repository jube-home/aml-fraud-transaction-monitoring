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

using Accord.Statistics.Distributions.Multivariate;

namespace Accord.Statistics.Distributions
{
    /// <summary>
    ///     Common interface for multivariate probability distributions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This interface is implemented by both multivariate
    ///         <see cref="MultivariateDiscreteDistribution">
    ///             Discrete Distributions
    ///         </see>
    ///         and
    ///         <see cref="MultivariateContinuousDistribution">
    ///             Continuous
    ///             Distributions
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         For Univariate distributions, see <see cref="IUnivariateDistribution" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="MultivariateNormalDistribution" />
    /// <seealso cref="DirichletDistribution" />
    /// <seealso cref="MultivariateEmpiricalDistribution" />
    /// <seealso cref="WishartDistribution" />
    public interface IMultivariateDistribution : IDistribution
    {
        /// <summary>
        ///     Gets the number of variables for the distribution.
        /// </summary>
        int Dimension { get; }

        /// <summary>
        ///     Gets the Mean vector for the distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the mean values for this distribution.
        /// </value>
        double[] Mean { get; }

        /// <summary>
        ///     Gets the Median vector for the distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the median values for this distribution.
        /// </value>
        double[] Median { get; }

        /// <summary>
        ///     Gets the Mode vector for the distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the mode values for this distribution.
        /// </value>
        double[] Mode { get; }

        /// <summary>
        ///     Gets the Variance vector for the distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the variance values for this distribution.
        /// </value>
        double[] Variance { get; }

        /// <summary>
        ///     Gets the Variance-Covariance matrix for the distribution.
        /// </summary>
        /// <value>
        ///     An multidimensional array of double-precision values
        ///     containing the covariance values for this distribution.
        /// </value>
        double[,] Covariance { get; }
    }

    /// <summary>
    ///     Common interface for multivariate probability distributions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This interface is implemented by both multivariate
    ///         <see cref="MultivariateDiscreteDistribution">
    ///             Discrete Distributions
    ///         </see>
    ///         and
    ///         <see cref="MultivariateContinuousDistribution">
    ///             Continuous
    ///             Distributions
    ///         </see>
    ///         .  However, unlike <see cref="IMultivariateDistribution" />, this interface
    ///         has a generic parameter that allows to define the type of the distribution values (i.e.
    ///         <see cref="T:double" />).
    ///     </para>
    ///     <para>
    ///         For Univariate distributions, see <see cref="IUnivariateDistribution" />.
    ///     </para>
    /// </remarks>
    /// <seealso cref="MultivariateNormalDistribution" />
    /// <seealso cref="DirichletDistribution" />
    /// <seealso cref="MultivariateEmpiricalDistribution" />
    /// <seealso cref="WishartDistribution" />
    public interface IMultivariateDistribution<in TObservation>
        : IDistribution<TObservation>, IDistribution
    {
        /// <summary>
        ///     Gets the number of variables for the distribution.
        /// </summary>
        int Dimension { get; }

        /// <summary>
        ///     Gets the Mean vector for the distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the mean values for this distribution.
        /// </value>
        double[] Mean { get; }

        /// <summary>
        ///     Gets the Median vector for the distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the median values for this distribution.
        /// </value>
        double[] Median { get; }

        /// <summary>
        ///     Gets the Mode vector for the distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the mode values for this distribution.
        /// </value>
        double[] Mode { get; }

        /// <summary>
        ///     Gets the Variance vector for the distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the variance values for this distribution.
        /// </value>
        double[] Variance { get; }

        /// <summary>
        ///     Gets the Variance-Covariance matrix for the distribution.
        /// </summary>
        /// <value>
        ///     An multidimensional array of double-precision values
        ///     containing the covariance values for this distribution.
        /// </value>
        double[,] Covariance { get; }
    }
}