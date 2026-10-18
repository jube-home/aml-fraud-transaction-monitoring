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
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Distribution types supported by the Anderson-Darling distribution.
    /// </summary>
    public enum AndersonDarlingDistributionType
    {
        /// <summary>
        ///     The statistic should reflect p-values for
        ///     a Anderson-Darling comparison against an
        ///     Uniform distribution.
        /// </summary>
        Uniform,

        /// <summary>
        ///     The statistic should reflect p-values for
        ///     a Anderson-Darling comparison against a
        ///     Normal distribution.
        /// </summary>
        Normal
    }

    /// <summary>
    ///     Anderson-Darling (A²) distribution.
    /// </summary>
    /// <example>
    ///     <code>
    /// // Create a new Anderson Darling distribution (A²) for comparing against a Gaussian
    /// var a2 = new AndersonDarlingDistribution(AndersonDarlingDistributionType.Normal, 30);
    /// 
    /// double median = a2.Median; // 0.33089957635450062
    /// 
    /// double chf = a2.CumulativeHazardFunction(x: 0.27);           // 0.42618068373640966
    /// double cdf = a2.DistributionFunction(x: 0.27);               // 0.34700165471995292
    /// double ccdf = a2.ComplementaryDistributionFunction(x: 0.27); // 0.65299834528004708
    /// double icdf = a2.InverseDistributionFunction(p: cdf);        // 0.27000000012207787
    /// 
    /// string str = a2.ToString(CultureInfo.InvariantCulture); // "A²(x; n = 30)"
    /// </code>
    /// </example>
    /// <seealso cref="Testing.AndersonDarlingTest" />
    [Serializable]
    public class AndersonDarlingDistribution : UnivariateContinuousDistribution, IFormattable
    {
        /// <summary>
        ///     Creates a new Anderson-Darling distribution.
        /// </summary>
        /// <param name="type">The type of the compared distribution.</param>
        /// <param name="samples">The number of samples.</param>
        public AndersonDarlingDistribution(AndersonDarlingDistributionType type, [Positive] double samples)
        {
            if (samples <= 0)
            {
                throw new ArgumentOutOfRangeException("samples");
            }

            DistributionType = type;
            NumberOfSamples = samples;
        }

        /// <summary>
        ///     Gets the type of the distribution that the
        ///     Anderson-Darling is being performed against.
        /// </summary>
        public AndersonDarlingDistributionType DistributionType { get; private set; }

        /// <summary>
        ///     Gets the number of samples distribution parameter.
        /// </summary>
        public double NumberOfSamples { get; private set; }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="DoubleRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override DoubleRange Support
        {
            get => new(0, double.PositiveInfinity);
            set { } //Derived. Not Implemented.
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Mean
        {
            get => throw new NotSupportedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Mode
        {
            get => throw new NotSupportedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Variance
        {
            get => throw new NotSupportedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "A²(x; n = {0})",
                NumberOfSamples.ToString(format, formatProvider));
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        /// <example>
        ///     See <see cref="KolmogorovSmirnovDistribution" />.
        /// </example>
        protected internal override double InnerDistributionFunction(double x)
        {
            double result;
            switch (DistributionType)
            {
                case AndersonDarlingDistributionType.Uniform:
                    result = ad_uniform(x, NumberOfSamples);
                    break;

                case AndersonDarlingDistributionType.Normal:
                    result = 1.0 - ad_normal(x, NumberOfSamples);
                    break;

                default:
                    throw new InvalidOperationException("Unexpected distribution type.");
            }

            return result;
        }

        /// <summary>
        ///     Gets the complementary cumulative distribution function
        ///     (ccdf) for this distribution evaluated at point <c>x</c>.
        ///     This function is also known as the Survival function.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     The Complementary Cumulative Distribution Function (CCDF) is
        ///     the complement of the Cumulative Distribution Function, or 1
        ///     minus the CDF.
        /// </remarks>
        protected internal override double InnerComplementaryDistributionFunction(double x)
        {
            switch (DistributionType)
            {
                case AndersonDarlingDistributionType.Uniform:
                    return 1.0 - ad_uniform(x, NumberOfSamples);

                case AndersonDarlingDistributionType.Normal:
                    return ad_normal(x, NumberOfSamples);
            }

            throw new InvalidOperationException("Unexpected distribution type.");
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new AndersonDarlingDistribution(DistributionType, NumberOfSamples);
        }

        #region Static methods

        private static double ad_normal(double x, double n)
        {
            x = (1 + 0.75 / n + 2.25 / (n * n)) * x;

            if (x >= 0.00 && x < 0.200)
            {
                return 1 - System.Math.Exp(-13.436 + 101.14 * x - 223.73 * x * x);
            }

            if (x >= 0.200 && x < 0.340)
            {
                return 1 - System.Math.Exp(-8.318 + 42.796 * x - 59.938 * x * x);
            }

            if (x >= 0.340 && x < 0.600)
            {
                return System.Math.Exp(0.9177 - 4.279 * x - 1.38 * x * x);
            }

            if (x >= 0.600 && x <= 13)
            {
                return System.Math.Exp(1.2937 - 5.709 * x + 0.0186 * x * x);
            }

            return double.NaN;
        }

        private static double ad_uniform(double z, double n)
        {
            var a = adinf(z);
            return a + errfix(n, a);
        }

        private static double adinf(double z)
        {
            // From: http://www.jstatsoft.org/v09/i02/paper, page 3

            if (z == 0)
            {
                return 0;
            }

            if (z < 2)
            {
                // |error| < 0.000002
                var e = System.Math.Exp(-1.2337141 / z) / System.Math.Sqrt(z);
                return e * (2.00012 + (0.247105 - (0.0649821 - (0.0347962 - (0.011672 - 0.00168691 * z) * z) * z) * z) *
                    z);
            }
            else
            {
                // |error| < 0.0000008
                var e = System.Math.Exp(1.0776 -
                                        (2.30695 - (0.43424 - (0.082433 - (0.008056 - 0.0003146 * z) * z) * z) * z) *
                                        z);
                return System.Math.Exp(-e);
            }
        }

        private static double errfix(double n, double x)
        {
            // From: http://www.jstatsoft.org/v09/i02/paper, page 4

            var cn = 0.01265 + 0.1757 / n;

            if (x < cn)
            {
                return (0.0037 / (n * n * n) + .00078 / (n * n) + 0.00006) / n * g1(x / cn);
            }

            if (cn <= x && x < 0.8)
            {
                return (0.04213 / n + 0.01365 / (n * n)) * g2((x - cn) / (0.8 - cn));
            }

            // if (x > -0.8)
            return g3(x) / n;
        }

        private static double g1(double x)
        {
            return System.Math.Sqrt(x) * (1 - x) * (49 * x - 102);
        }

        private static double g2(double x)
        {
            return -0.00022633 + (6.54034 - (14.6538 - (14.458 - (8.259 - 1.91864 * x) * x) * x) * x) * x;
        }

        private static double g3(double x)
        {
            return -130.2137 + (745.2337 - (1705.091 - (1950.646 - (1116.360 - 255.7844 * x) * x) * x) * x) * x;
        }

        #endregion
    }
}