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

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Hypothesis type
    /// </summary>
    /// <remarks>
    ///     The type of the hypothesis being made expresses the way in
    ///     which a value of a parameter may deviate from that assumed
    ///     in the null hypothesis. It can either state that a value is
    ///     higher, lower or simply different than the one assumed under
    ///     the null hypothesis.
    /// </remarks>
    public enum DistributionTail
    {
        /// <summary>
        ///     The test considers the two tails from a probability distribution.
        /// </summary>
        /// <remarks>
        ///     The two-tailed test is a statistical test in which a given statistical
        ///     hypothesis, H0 (the null hypothesis), will be rejected when the value of
        ///     the test statistic is either sufficiently small or sufficiently large.
        /// </remarks>
        TwoTail = 0,

        /// <summary>
        ///     The test considers the upper (right) tail from a probability distribution.
        /// </summary>
        /// <remarks>
        ///     The one-tailed, upper tail test is a statistical test in which a given
        ///     statistical hypothesis, H0 (the null hypothesis), will be rejected when
        ///     the value of the test statistic is sufficiently large.
        /// </remarks>
        OneUpper = 1,

        /// <summary>
        ///     The test considers the lower (left) tail from a probability distribution.
        /// </summary>
        /// <remarks>
        ///     The one-tailed, lower tail test is a statistical test in which a given
        ///     statistical hypothesis, H0 (the null hypothesis), will be rejected when
        ///     the value of the test statistic is sufficiently small.
        /// </remarks>
        OneLower = 2
    }

    /// <summary>
    ///     Common test Hypothesis for one sample tests, such
    ///     as <see cref="ZTest" /> and <see cref="TTest" />.
    /// </summary>
    public enum OneSampleHypothesis
    {
        /// <summary>
        ///     Tests if the mean (or the parameter under test)
        ///     is significantly different from the hypothesized
        ///     value, without considering the direction for this
        ///     difference.
        /// </summary>
        ValueIsDifferentFromHypothesis = DistributionTail.TwoTail,

        /// <summary>
        ///     Tests if the mean (or the parameter under test)
        ///     is significantly greater (larger, bigger) than
        ///     the hypothesized value.
        /// </summary>
        ValueIsGreaterThanHypothesis = DistributionTail.OneUpper,

        /// <summary>
        ///     Tests if the mean (or the parameter under test)
        ///     is significantly smaller (lesser) than the
        ///     hypothesized value.
        /// </summary>
        ValueIsSmallerThanHypothesis = DistributionTail.OneLower
    }

    /// <summary>
    ///     Common test Hypothesis for two sample tests, such as
    ///     <see cref="TwoSampleZTest" /> and <see cref="TwoSampleTTest" />.
    /// </summary>
    public enum TwoSampleHypothesis
    {
        /// <summary>
        ///     Tests if the mean (or the parameter under test) of
        ///     the first sample is different from the mean of the
        ///     second sample, without considering any particular
        ///     direction for the difference.
        /// </summary>
        ValuesAreDifferent = DistributionTail.TwoTail,

        /// <summary>
        ///     Tests if the mean (or the parameter under test) of
        ///     the first sample is greater (larger, bigger) than
        ///     the mean of the second sample.
        /// </summary>
        FirstValueIsGreaterThanSecond = DistributionTail.OneUpper,

        /// <summary>
        ///     Tests if the mean (or the parameter under test) of
        ///     the first sample is smaller (lesser) than the mean
        ///     of the second sample.
        /// </summary>
        FirstValueIsSmallerThanSecond = DistributionTail.OneLower
    }

    /// <summary>
    ///     Hypothesis for the <see cref="KolmogorovSmirnovTest">one-sample Kolmogorov-Smirnov test</see>.
    /// </summary>
    public enum KolmogorovSmirnovTestHypothesis
    {
        /// <summary>
        ///     Tests whether the sample's distribution is
        ///     different from the reference distribution.
        /// </summary>
        SampleIsDifferent = DistributionTail.TwoTail,

        /// <summary>
        ///     Tests whether the distribution of one sample is greater
        ///     than the reference distribution, in a statistical sense.
        /// </summary>
        SampleIsGreater = DistributionTail.OneUpper,

        /// <summary>
        ///     Tests whether the distribution of one sample is smaller
        ///     than the reference distribution, in a statistical sense.
        /// </summary>
        SampleIsSmaller = DistributionTail.OneLower
    }

    /// <summary>
    ///     Test hypothesis for the <see cref="TwoSampleKolmogorovSmirnovTest">two-sample Kolmogorov-Smirnov tests</see>.
    /// </summary>
    public enum TwoSampleKolmogorovSmirnovTestHypothesis
    {
        /// <summary>
        ///     Tests whether samples have been drawn
        ///     from significantly unequal distributions.
        /// </summary>
        SamplesDistributionsAreUnequal = DistributionTail.TwoTail,

        /// <summary>
        ///     Tests whether the distribution of one sample is
        ///     greater than the other, in a statistical sense.
        /// </summary>
        FirstSampleIsLargerThanSecond = DistributionTail.OneUpper,

        /// <summary>
        ///     Tests whether the distribution of one sample is
        ///     smaller than the other, in a statistical sense.
        /// </summary>
        FirstSampleIsSmallerThanSecond = DistributionTail.OneLower
    }

    /// <summary>
    ///     Hypothesis for the one-sample <see cref="GrubbTest">Grubb's test</see>.
    /// </summary>
    public enum GrubbTestHypothesis
    {
        /// <summary>
        ///     Tests whether there is at least one outlier in the data.
        /// </summary>
        ThereAreOutliers,

        /// <summary>
        ///     Tests whether the maximum value in the data is actually an outlier.
        /// </summary>
        TheMaximumIsAnOutlier,

        /// <summary>
        ///     Tests whether the minimum value in the data is actually an outlier.
        /// </summary>
        TheMinimumIsAnOutlier
    }
}