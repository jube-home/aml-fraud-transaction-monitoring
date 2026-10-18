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
    ///     Power Lognormal distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.itl.nist.gov/div898/handbook/eda/section3/eda366e.htm">
    ///                         NIST/SEMATECH e-Handbook of Statistical Methods. Power Lognormal distribution. Available on:
    ///                         http://www.itl.nist.gov/div898/handbook/eda/section3/eda366e.htm
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This example shows how to create a Power Lognormal
    ///         distribution and compute some of its properties.
    ///     </para>
    ///     <code>
    /// // Create a Power-Lognormal distribution with p = 4.2 and s = 1.2
    /// var plog = new PowerLognormalDistribution(power: 4.2, shape: 1.2);
    /// 
    /// double cdf = plog.DistributionFunction(x: 1.4); // 0.98092157745191766
    /// double pdf = plog.ProbabilityDensityFunction(x: 1.4); // 0.046958580233533977
    /// double lpdf = plog.LogProbabilityDensityFunction(x: 1.4); // -3.0584893374471496
    /// 
    /// double ccdf = plog.ComplementaryDistributionFunction(x: 1.4); // 0.019078422548082351
    /// double icdf = plog.InverseDistributionFunction(p: cdf); // 1.4
    /// 
    /// double hf = plog.HazardFunction(x: 1.4); // 10.337649063164642
    /// double chf = plog.CumulativeHazardFunction(x: 1.4); // 3.9591972920568446
    /// 
    /// string str = plog.ToString(CultureInfo.InvariantCulture); // PLD(x; p = 4.2, σ = 1.2)
    /// </code>
    /// </example>
    [Serializable]
    public class PowerLognormalDistribution : UnivariateContinuousDistribution
    {
        // Distribution parameters

        /// <summary>
        ///     Constructs a Power Lognormal distribution
        ///     with the given power and shape parameters.
        /// </summary>
        /// <param name="power">The distribution's power p.</param>
        /// <param name="shape">The distribution's shape σ.</param>
        public PowerLognormalDistribution([Positive] double power, [Positive] double shape)
        {
            if (power <= 0)
            {
                throw new ArgumentOutOfRangeException("power", "Power must be positive.");
            }

            if (shape <= 0)
            {
                throw new ArgumentOutOfRangeException("shape", "Shape must be positive.");
            }

            initialize(power, shape);
        }

        /// <summary>
        ///     Gets the distribution's power parameter (p).
        /// </summary>
        public double Power { get; private set; } = 1;

        /// <summary>
        ///     Gets the distribution's shape parameter sigma (σ).
        /// </summary>
        public double Shape { get; private set; } = 1;

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
            get => new(0, double.PositiveInfinity);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        protected internal override double InnerDistributionFunction(double x)
        {
            var z = System.Math.Log(x) / Shape;
            return 1.0 - System.Math.Pow(Normal.Function(-z), Power);
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
        protected internal override double InnerInverseDistributionFunction(double p)
        {
            return System.Math.Exp(Normal.Inverse(1.0 - System.Math.Pow(1.0 - p, 1.0 / Power)) * Shape);
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var a = Power / (x * Shape);
            var z = System.Math.Log(x) / Shape;

            return a * Normal.Derivative(z) * System.Math.Pow(Normal.Function(-z), Power - 1);
        }

        /// <summary>
        ///     Gets the hazard function, also known as the failure rate or
        ///     the conditional failure density function for this distribution
        ///     evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The conditional failure density function <c>h(x)</c>
        ///     evaluated at <c>x</c> in the current distribution.
        /// </returns>
        public override double HazardFunction(double x)
        {
            var a = Power / (x * Shape);
            var z = System.Math.Log(x) / Shape;

            var num = Power * a * Normal.Derivative(z);
            var den = Normal.Function(-z);
            return num / den;
        }

        /// <summary>
        ///     Gets the cumulative hazard function for this
        ///     distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The cumulative hazard function <c>H(x)</c>
        ///     evaluated at <c>x</c> in the current distribution.
        /// </returns>
        public override double CumulativeHazardFunction(double x)
        {
            var z = System.Math.Log(x) / Shape;
            return -System.Math.Log(System.Math.Pow(Normal.Function(-z), Power));
        }

        /// <summary>
        ///     Gets the complementary cumulative distribution function
        ///     (ccdf) for this distribution evaluated at point <c>x</c>.
        ///     This function is also known as the Survival function.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        protected internal override double InnerComplementaryDistributionFunction(double x)
        {
            var z = System.Math.Log(x) / Shape;
            return System.Math.Pow(Normal.Function(-z), Power);
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new PowerLognormalDistribution(Power, Shape);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "PLD(x; p = {0}, σ = {1})",
                Power.ToString(format, formatProvider),
                Shape.ToString(format, formatProvider));
        }

        private void initialize(double power, double sigma)
        {
            this.Power = power;
            this.Shape = sigma;
        }
    }
}