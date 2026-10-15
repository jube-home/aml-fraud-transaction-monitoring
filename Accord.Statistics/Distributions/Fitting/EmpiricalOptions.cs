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
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Distributions.Fitting
{
    /// <summary>
    ///     Smoothing rule function definition for
    ///     <see cref="EmpiricalDistribution">
    ///         Empirical distributions
    ///     </see>
    ///     .
    /// </summary>
    /// <param name="observations">The observations for the empirical distribution.</param>
    /// <param name="weights">The fractional importance for each sample. Those values must sum up to one.</param>
    /// <param name="repeats">The number of times each sample should be repeated.</param>
    /// <returns>An estimative of the smoothing parameter.</returns>
    public delegate double SmoothingRule(double[] observations, double[] weights = null, int[] repeats = null);

    /// <summary>
    ///     Estimation options for
    ///     <see cref="EmpiricalDistribution">
    ///         Empirical distributions
    ///     </see>
    ///     .
    /// </summary>
    [Serializable]
    public class EmpiricalOptions : IFittingOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NormalOptions" /> class.
        /// </summary>
        public EmpiricalOptions()
        {
            SmoothingRule = EmpiricalDistribution.SmoothingRule;
            InPlace = false;
        }

        /// <summary>
        ///     Gets or sets the smoothing rule used to compute the smoothing
        ///     parameter in the <see cref="EmpiricalDistribution" />. Default
        ///     is to use the
        ///     <see cref="EmpiricalDistribution.SmoothingRule(double[], double[], int[])">
        ///         normal distribution bandwidth approximation
        ///     </see>
        ///     .
        /// </summary>
        public SmoothingRule SmoothingRule { get; set; }

        /// <summary>
        ///     Gets or sets whether the empirical distribution should be take the
        ///     observation and weight vectors directly instead of making a copy
        ///     beforehand.
        /// </summary>
        public bool InPlace { get; set; }

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