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
using Accord.Math.Decompositions;
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Distributions.Fitting
{
    /// <summary>
    ///     Estimation options for
    ///     <see cref="NormalDistribution">
    ///         Normal distributions
    ///     </see>
    ///     .
    /// </summary>
    [Serializable]
    public class NormalOptions : IFittingOptions, IComponentOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NormalOptions" /> class.
        /// </summary>
        public NormalOptions()
        {
            Regularization = 0;
            Diagonal = false;
        }

        /// <summary>
        ///     Gets or sets the regularization step to avoid singular or non-positive definite
        ///     covariance matrices. Default is 0. Setting this property to a small constant like
        ///     1e-6 is more efficient than setting <see cref="Robust" /> to true.
        /// </summary>
        /// <value>The regularization step.</value>
        public double Regularization { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the covariance
        ///     matrix to be estimated should be assumed to be diagonal.
        /// </summary>
        /// <value><c>true</c> to estimate a diagonal covariance matrix; otherwise, <c>false</c>.</value>
        public bool Diagonal { get; set; }

        /// <summary>
        ///     Gets or sets whether the estimation function should allow non-positive definite covariance
        ///     matrices by using the <see cref="SingularValueDecomposition">Singular Value Decomposition</see>
        ///     instead of the default <see cref="CholeskyDecomposition">Cholesky</see>. Enabling this property
        ///     can significantly increase learning times.
        /// </summary>
        public bool Robust { get; set; }

        /// <summary>
        ///     Gets or sets whether the normal distributions should have only a single, shared
        ///     covariance matrix among all components in a mixture. Setting this property only
        ///     has effect if the distributions are part of a <see cref="Mixture{T}" /> or
        ///     <see cref="MultivariateMixture{T}" />
        /// </summary>
        public bool Shared { get; set; }

        /// <summary>
        ///     Gets or sets a post processing step can be called after all component
        ///     distributions have been fitted (or their .Fit() method has been called).
        /// </summary>
        public Action<IDistribution[], double[]> Postprocessing { get; set; }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}