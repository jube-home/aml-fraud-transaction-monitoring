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

namespace Accord.Statistics.Distributions.Fitting
{
    /// <summary>
    ///     BetaPERT's distribution estimation method.
    /// </summary>
    public enum BetaPertMethod
    {
        /// <summary>
        ///     Estimates the mode using the classic method.
        /// </summary>
        Classic,

        /// <summary>
        ///     Estimates the mode using the Vose method.
        /// </summary>
        Vose
    }

    /// <summary>
    ///     Estimation options for
    ///     <see cref="Univariate.GeneralizedBetaDistribution">
    ///         Beta PERT distributions
    ///     </see>
    ///     .
    /// </summary>
    [Serializable]
    public class GeneralizedBetaOptions : IFittingOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GeneralizedBetaOptions" /> class.
        /// </summary>
        public GeneralizedBetaOptions()
        {
            MinIndex = -1;
            MaxIndex = -1;
            IsSorted = false;
            FixMin = true;
            FixMax = true;
            Method = BetaEstimationMethod.Moments;
        }

        /// <summary>
        ///     Gets or sets the index of the minimum observed
        ///     value, if already known. Default is -1.
        /// </summary>
        public int MinIndex { get; set; }

        /// <summary>
        ///     Gets or sets the index of the maximum observed
        ///     value, if already known. Default is -1.
        /// </summary>
        public int MaxIndex { get; set; }

        /// <summary>
        ///     Gets or sets which estimation method should be used by the fitting
        ///     algorithm. Default is <see cref="BetaEstimationMethod.Moments" />.
        /// </summary>
        public BetaEstimationMethod Method { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the observations are already sorted.
        /// </summary>
        /// <value>
        ///     Set to <c>true</c> if the observations are sorted; otherwise, <c>false</c>.
        /// </value>
        public bool IsSorted { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the maximum
        ///     value should be treated as fixed and not be estimated.
        ///     Default is true.
        /// </summary>
        public bool FixMax { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the minimum
        ///     value should be treated as fixed and not be estimated.
        ///     Default is true.
        /// </summary>
        public bool FixMin { get; set; }

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