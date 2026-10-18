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
using Accord.Math;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Power Normal distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.itl.nist.gov/div898/handbook/eda/section3/eda366d.htm">
    ///                         NIST/SEMATECH e-Handbook of Statistical Methods. Power Normal distribution. Available on:
    ///                         http://www.itl.nist.gov/div898/handbook/eda/section3/eda366d.htm
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This example shows how to create a Power Normal distribution
    ///         and compute some of its properties.
    ///     </para>
    ///     <code>
    /// // Create a new Power-Normal distribution with p = 4.2
    /// var pnormal = new PowerNormalDistribution(power: 4.2);
    /// 
    /// double cdf = pnormal.DistributionFunction(x: 1.4); // 0.99997428721920678
    /// double pdf = pnormal.ProbabilityDensityFunction(x: 1.4); // 0.00020022645890003279
    /// double lpdf = pnormal.LogProbabilityDensityFunction(x: 1.4); // -0.20543269836728234
    /// 
    /// double ccdf = pnormal.ComplementaryDistributionFunction(x: 1.4); // 0.000025712780793218926
    /// double icdf = pnormal.InverseDistributionFunction(p: cdf); // 1.3999999999998953
    /// 
    /// double hf = pnormal.HazardFunction(x: 1.4); // 7.7870402470368854
    /// double chf = pnormal.CumulativeHazardFunction(x: 1.4); // 10.568522382550167
    /// 
    /// string str = pnormal.ToString(); // PND(x; p = 4.2)
    /// </code>
    /// </example>
    [Serializable]
    public class PowerNormalDistribution : UnivariateContinuousDistribution
    {
        // Distribution parameters

        /// <summary>
        ///     Constructs a Power Normal distribution
        ///     with given power (shape) parameter.
        /// </summary>
        /// <param name="power">The distribution's power p.</param>
        public PowerNormalDistribution([Positive] double power)
        {
            if (power <= 0)
            {
                throw new ArgumentOutOfRangeException("power", "Power must be positive.");
            }

            initialize(power);
        }

        /// <summary>
        ///     Gets the distribution shape (power) parameter.
        /// </summary>
        public double Power { get; private set; } = 1;

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
        public override double Mode
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
        public override double StandardDeviation => throw new NotSupportedException();

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
            get => new(double.NegativeInfinity, double.PositiveInfinity);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        protected internal override double InnerDistributionFunction(double x)
        {
            var phi = Normal.Function(-x);
            return 1.0 - System.Math.Pow(phi, Power);
        }

        /// <summary>
        ///     Gets the inverse of the cumulative distribution function (icdf) for
        ///     this distribution evaluated at probability <c>p</c>. This function
        ///     is also known as the Quantile function.
        /// </summary>
        /// <remarks>
        ///     The Inverse Cumulative Distribution Function (ICDF) specifies, for
        ///     a given probability, the value which the random variable will be at,
        ///     or below, with that probability.
        /// </remarks>
        /// <param name="p">A probability value between 0 and 1.</param>
        /// <returns>
        ///     A sample which could original the given probability
        ///     value when applied in the <see cref="UnivariateContinuousDistribution.DistributionFunction(double)" />.
        /// </returns>
        protected internal override double InnerInverseDistributionFunction(double p)
        {
            return Normal.Inverse(1.0 - System.Math.Pow(1.0 - p, 1.0 / Power));
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var pdf = Normal.Derivative(x);
            var cdf = Normal.Function(-x);
            return Power * pdf * System.Math.Pow(cdf, Power - 1);
        }

        /// <summary>
        ///     Gets the log-probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            return System.Math.Log(Power) + Normal.LogDerivative(x) + (Power - 1) * Normal.Function(-x);
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new PowerNormalDistribution(Power);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "PND(x; p = {0})",
                Power.ToString(format, formatProvider));
        }

        private void initialize(double power)
        {
            this.Power = power;
        }
    }
}