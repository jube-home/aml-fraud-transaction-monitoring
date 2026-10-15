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

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Common interface for descriptive measures, such as
    ///     <see cref="DescriptiveMeasures" /> and
    ///     <see cref="CircularDescriptiveMeasures" />.
    /// </summary>
    /// <seealso cref="DescriptiveAnalysis" />
    /// <seealso cref="CircularDescriptiveAnalysis" />
    public interface IDescriptiveMeasures
    {
        /// <summary>
        ///     Gets the variable's index.
        /// </summary>
        int Index { get; }

        /// <summary>
        ///     Gets the variable's name
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Gets the variable's total sum.
        /// </summary>
        double Sum { get; }

        /// <summary>
        ///     Gets the variable's mean.
        /// </summary>
        double Mean { get; }

        /// <summary>
        ///     Gets the variable's standard deviation.
        /// </summary>
        double StandardDeviation { get; }

        /// <summary>
        ///     Gets the variable's median.
        /// </summary>
        double Median { get; }

        /// <summary>
        ///     Gets the variable's outer fences range.
        /// </summary>
        DoubleRange OuterFence { get; }

        /// <summary>
        ///     Gets the variable's inner fence range.
        /// </summary>
        DoubleRange InnerFence { get; }

        /// <summary>
        ///     Gets the variable's interquartile range.
        /// </summary>
        DoubleRange Quartiles { get; }

        /// <summary>
        ///     Gets the variable's mode.
        /// </summary>
        double Mode { get; }

        /// <summary>
        ///     Gets the variable's variance.
        /// </summary>
        double Variance { get; }

        /// <summary>
        ///     Gets the variable's skewness.
        /// </summary>
        double Skewness { get; }

        /// <summary>
        ///     Gets the variable's kurtosis.
        /// </summary>
        double Kurtosis { get; }

        /// <summary>
        ///     Gets the variable's standard error of the mean.
        /// </summary>
        double StandardError { get; }

        /// <summary>
        ///     Gets the variable's maximum value.
        /// </summary>
        double Max { get; }

        /// <summary>
        ///     Gets the variable's minimum value.
        /// </summary>
        double Min { get; }

        /// <summary>
        ///     Gets the variable's length.
        /// </summary>
        double Length { get; }

        /// <summary>
        ///     Gets the number of distinct values for the variable.
        /// </summary>
        int Distinct { get; }

        /// <summary>
        ///     Gets the number of samples for the variable.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///     Gets the 95% confidence interval around the <see cref="Mean" />.
        /// </summary>
        DoubleRange Confidence { get; }

        /// <summary>
        ///     Gets the 95% deviance interval around the <see cref="Mean" />.
        /// </summary>
        DoubleRange Deviance { get; }

        /// <summary>
        ///     Gets the variable's observations.
        /// </summary>
        double[] Samples { get; }

        /// <summary>
        ///     Gets a confidence interval for the <see cref="Mean" />
        ///     within the given confidence level percentage.
        /// </summary>
        /// <param name="percent">The confidence level. Default is 0.95.</param>
        /// <returns>A confidence interval for the estimated value.</returns>
        DoubleRange GetConfidenceInterval(double percent = 0.95);

        /// <summary>
        ///     Gets a deviance interval for the <see cref="Mean" />
        ///     within the given confidence level percentage (i.e. uses
        ///     the standard deviation rather than the standard error to
        ///     compute the range interval for the variable).
        /// </summary>
        /// <param name="percent">The confidence level. Default is 0.95.</param>
        /// <returns>A confidence interval for the estimated value.</returns>
        DoubleRange GetDevianceInterval(double percent = 0.95);
    }
}