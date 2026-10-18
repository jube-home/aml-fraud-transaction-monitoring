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
using Accord.Statistics.Testing;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Grubb's statistic distribution.
    /// </summary>
    /// <seealso cref="GrubbTest" />
    [Serializable]
    public class GrubbDistribution : UnivariateContinuousDistribution
    {
        private TDistribution tDistribution;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GrubbDistribution" /> class.
        /// </summary>
        /// <param name="samples">The number of samples.</param>
        public GrubbDistribution([PositiveInteger(3)] int samples)
        {
            if (samples <= 2)
            {
                throw new ArgumentOutOfRangeException("samples", "Number of samples must be greater than two.");
            }

            NumberOfSamples = samples;
            tDistribution = new TDistribution(samples - 2);
        }

        /// <summary>
        ///     Gets the number of samples for the distribution.
        /// </summary>
        public int NumberOfSamples { get; private set; }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Mean
        {
            get => throw new NotSupportedException();
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Variance
        {
            get => throw new NotSupportedException();
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="DoubleRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override DoubleRange Support
        {
            get =>
                new(InnerInverseDistributionFunction(0),
                    (NumberOfSamples - 1.0) / System.Math.Sqrt(NumberOfSamples));
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>System.Double.</returns>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        protected internal override double InnerDistributionFunction(double x)
        {
            // http://graphpad.com/support/faqid/1598/
            double N = NumberOfSamples;
            var num = N * (N - 2) * x * x;
            var den = (N - 1) * (N - 1) - N * x * x;
            var t = System.Math.Sqrt(num / den);
            var p = tDistribution.ComplementaryDistributionFunction(t);
            var r = 1.0 - N * p;
            if (r > 1)
            {
                return 1;
            }

            if (r < 0)
            {
                return 0;
            }

            return r;
        }

        /// <summary>
        ///     Gets the inverse of the cumulative distribution function (icdf) for
        ///     this distribution evaluated at probability <c>p</c>. This function
        ///     is also known as the Quantile function.
        /// </summary>
        /// <param name="p">A probability value between 0 and 1.</param>
        /// <returns>
        ///     A sample which could original the given probability
        ///     value when applied in the <see cref="UnivariateContinuousDistribution.DistributionFunction(double)" />.
        /// </returns>
        /// <remarks>
        ///     The Inverse Cumulative Distribution Function (ICDF) specifies, for
        ///     a given probability, the value which the random variable will be at,
        ///     or below, with that probability.
        /// </remarks>
        protected internal override double InnerInverseDistributionFunction(double p)
        {
            // https://www.wolframalpha.com/input/?i=sqrt((N+*(+N-+2)*x%C2%B2)+%2F+((N-1)%C2%B2+-+N*x%C2%B2))+%3D+t+solve+for+x
            double N = NumberOfSamples;
            var t = tDistribution.InverseDistributionFunction((1 - p) / N);
            var a = (N - 1.0) / System.Math.Sqrt(N);
            var b = System.Math.Sqrt(t * t / (N + t * t - 2));
            var r = a * b;
            return r;
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public override object Clone()
        {
            return new GrubbDistribution(NumberOfSamples);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("Grubb(x; n = {0})", NumberOfSamples);
        }
    }
}