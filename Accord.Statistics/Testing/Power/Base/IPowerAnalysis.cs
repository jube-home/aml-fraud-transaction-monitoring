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

namespace Accord.Statistics.Testing.Power
{
    /// <summary>
    ///     Common interface for power analysis objects.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The power of a statistical test is the probability that it correctly rejects
    ///         the null hypothesis when the null hypothesis is false. That is,
    ///     </para>
    ///     <para>
    ///         <c>power = P(reject null hypothesis | null hypothesis is false)</c>
    ///     </para>
    ///     <para>
    ///         It can be equivalently thought of as the probability of correctly accepting the
    ///         alternative hypothesis when the alternative hypothesis is true - that is, the ability
    ///         of a test to detect an effect, if the effect actually exists. The power is in general
    ///         a function of the possible distributions, often determined by a parameter, under the
    ///         alternative hypothesis. As the power increases, the chances of a Type II error occurring
    ///         decrease. The probability of a Type II error occurring is referred to as the false
    ///         negative rate (β) and the power is equal to 1−β. The power is also known as the sensitivity.
    ///     </para>
    ///     <para>
    ///         Power analysis can be used to calculate the minimum sample size required so that
    ///         one can be reasonably likely to detect an effect of a given size. Power analysis
    ///         can also be used to calculate the minimum effect size that is likely to be detected
    ///         in a study using a given sample size. In addition, the concept of power is used to
    ///         make comparisons between different statistical testing procedures: for example,
    ///         between a parametric and a nonparametric test of the same hypothesis. There is also
    ///         the concept of a power function of a test, which is the probability of rejecting the
    ///         null when the null is true.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Statistical power. Available on:
    ///                     http://en.wikipedia.org/wiki/Statistical_power
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="TTestPowerAnalysis" />
    /// <seealso cref="ZTestPowerAnalysis" />
    public interface IPowerAnalysis : ICloneable, IFormattable
    {
        /// <summary>
        ///     Gets the test type.
        /// </summary>
        DistributionTail Tail { get; }

        /// <summary>
        ///     Gets the power of the test, also known as the
        ///     (1-Beta error rate) or the test's sensitivity.
        /// </summary>
        double Power { get; }

        /// <summary>
        ///     Gets the significance level
        ///     for the test. Also known as alpha.
        /// </summary>
        double Size { get; }

        /// <summary>
        ///     Gets the number of samples
        ///     considered in the test.
        /// </summary>
        double Samples { get; }

        /// <summary>
        ///     Gets the effect size of the test.
        /// </summary>
        double Effect { get; }
    }
}