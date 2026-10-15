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

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Wald's Test using the Normal distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Wald test is a parametric statistical test named after Abraham Wald
    ///         with a great variety of uses. Whenever a relationship within or between
    ///         data items can be expressed as a statistical model with parameters to be
    ///         estimated from a sample, the Wald test can be used to test the true value
    ///         of the parameter based on the sample estimate.
    ///     </para>
    ///     <para>
    ///         Under the Wald statistical test, the maximum likelihood estimate of the
    ///         parameter(s) of interest θ is compared with the proposed value θ', with
    ///         the assumption that the difference between the two will be approximately
    ///         normal.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Wald_test">
    ///                         Wikipedia, The Free Encyclopedia. Wald Test. Available on:
    ///                         http://en.wikipedia.org/wiki/Wald_test
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="ZTest" />
    /// <seealso cref="Distributions.Univariate.NormalDistribution" />
    [Serializable]
    public class WaldTest : ZTest
    {
        /// <summary>
        ///     Constructs a Wald's test.
        /// </summary>
        /// <param name="statistic">The test statistic, as given by (θ-θ')/SE.</param>
        public WaldTest(double statistic)
            : base(statistic, OneSampleHypothesis.ValueIsDifferentFromHypothesis)
        {
        }

        /// <summary>
        ///     Constructs a Wald's test.
        /// </summary>
        /// <param name="estimatedValue">The estimated value (θ).</param>
        /// <param name="hypothesizedValue">The hypothesized value (θ').</param>
        /// <param name="standardError">The standard error of the estimation (SE).</param>
        public WaldTest(double estimatedValue, double hypothesizedValue, double standardError)
            : base(estimatedValue, hypothesizedMean: hypothesizedValue, standardError: standardError,
                alternate: OneSampleHypothesis.ValueIsDifferentFromHypothesis)
        {
        }
    }
}