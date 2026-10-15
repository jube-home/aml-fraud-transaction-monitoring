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
    ///     Birnbaum-Saunders (Fatigue Life) distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Birnbaum–Saunders distribution, also known as the fatigue life distribution,
    ///         is a probability distribution used extensively in reliability applications to model
    ///         failure times. There are several alternative formulations of this distribution in
    ///         the literature. It is named after Z. W. Birnbaum and S. C. Saunders.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Birnbaum%E2%80%93Saunders_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Birnbaum–Saunders distribution.
    ///                         Available from: http://en.wikipedia.org/wiki/Birnbaum%E2%80%93Saunders_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.itl.nist.gov/div898/handbook/eda/section3/eda366a.htm">
    ///                         NIST/SEMATECH e-Handbook of Statistical Methods, Birnbaum-Saunders (Fatigue Life) Distribution
    ///                         Available from: http://www.itl.nist.gov/div898/handbook/eda/section3/eda366a.htm
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This example shows how to create a Birnbaum-Saunders distribution
    ///         and compute some of its properties.
    ///     </para>
    ///     <code>
    /// // Creates a new Birnbaum-Saunders distribution
    /// var bs = new BirnbaumSaundersDistribution(shape: 0.42);
    /// 
    /// double mean = bs.Mean;     // 1.0882000000000001
    /// double median = bs.Median; // 1.0
    /// double var = bs.Variance;  // 0.21529619999999997
    /// 
    /// double cdf = bs.DistributionFunction(x: 1.4); // 0.78956384911580346
    /// double pdf = bs.ProbabilityDensityFunction(x: 1.4); // 1.3618433601225426
    /// double lpdf = bs.LogProbabilityDensityFunction(x: 1.4); // 0.30883919386130815
    /// 
    /// double ccdf = bs.ComplementaryDistributionFunction(x: 1.4); // 0.21043615088419654
    /// double icdf = bs.InverseDistributionFunction(p: cdf); // 2.0618330099769064
    /// 
    /// double hf = bs.HazardFunction(x: 1.4); // 6.4715276077824093
    /// double chf = bs.CumulativeHazardFunction(x: 1.4); // 1.5585729930861034
    /// 
    /// string str = bs.ToString(CultureInfo.InvariantCulture); // BirnbaumSaunders(x; μ = 0, β = 1, γ = 0.42)
    /// </code>
    /// </example>
    [Serializable]
    public class BirnbaumSaundersDistribution : UnivariateContinuousDistribution
    {
        // Distribution parameters

        /// <summary>
        ///     Constructs a Birnbaum-Saunders distribution
        ///     with location parameter 0, scale 1, and shape 1.
        /// </summary>
        public BirnbaumSaundersDistribution()
            : this(0, 1, 1)
        {
        }

        /// <summary>
        ///     Constructs a Birnbaum-Saunders distribution
        ///     with location parameter 0, scale 1, and the
        ///     given shape.
        /// </summary>
        /// <param name="shape">The shape parameter gamma (γ). Default is 1.</param>
        public BirnbaumSaundersDistribution([Positive] double shape)
            : this(0, 1, shape)
        {
        }

        /// <summary>
        ///     Constructs a Birnbaum-Saunders distribution
        ///     with given location, shape and scale parameters.
        /// </summary>
        /// <param name="location">The location parameter μ. Default is 0.</param>
        /// <param name="scale">The scale parameter beta (β). Default is 1.</param>
        /// <param name="shape">The shape parameter gamma (γ). Default is 1.</param>
        public BirnbaumSaundersDistribution([Real] double location,
            [Positive] double scale, [Positive] double shape)
        {
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale", "Scale must be greater than zero.");
            }

            if (shape <= 0)
            {
                throw new ArgumentOutOfRangeException("shape", "Shape must be greater than zero.");
            }

            init(location, scale, shape);
        }

        /// <summary>
        ///     Gets the distribution's location parameter μ.
        /// </summary>
        public double Location { get; private set; }

        /// <summary>
        ///     Gets the distribution's scale parameter β.
        /// </summary>
        public double Scale { get; private set; }

        /// <summary>
        ///     Gets the distribution's shape parameter γ.
        /// </summary>
        public double Shape { get; private set; }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="DoubleRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override DoubleRange Support
        {
            get => new(Location, double.PositiveInfinity);
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <remarks>
        ///     The Birnbaum Saunders mean is defined as
        ///     <c>1 + 0.5<see cref="Shape">γ</see>²</c>.
        /// </remarks>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Mean
        {
            get => 1 + 0.5 * Shape * Shape;
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <remarks>
        ///     The Birnbaum Saunders variance is defined as
        ///     <c><see cref="Shape">γ</see>² (1 + (5/4)<see cref="Shape">γ</see>²)</c>.
        /// </remarks>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Variance
        {
            get => Shape * Shape * (1 + 5 * Shape * Shape / 4);
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     This method is not supported.
        /// </summary>
        public override double Mode
        {
            get => throw new NotSupportedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     This method is not supported.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
            set => throw new NotImplementedException();
        }

        private void init(double location, double scale, double shape)
        {
            this.Location = location;
            this.Scale = scale;
            this.Shape = shape;
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     <para>
        ///         The Cumulative Distribution Function (CDF) describes the cumulative
        ///         probability that a given value or any value smaller than it will occur.
        ///     </para>
        /// </remarks>
        protected internal override double InnerDistributionFunction(double x)
        {
            var c = x - Location;

            var a = System.Math.Sqrt(c / Scale);
            var b = System.Math.Sqrt(Scale / c);
            var z = (a - b) / Shape;

            // Normal cumulative distribution function
            return Special.Erfc(-z / Constants.Sqrt2) * 0.5;
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
        /// <remarks>
        ///     <para>
        ///         The Probability Density Function (PDF) describes the
        ///         probability that a given value <c>x</c> will occur.
        ///     </para>
        /// </remarks>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var c = x - Location;

            var a = System.Math.Sqrt(c / Scale);
            var b = System.Math.Sqrt(Scale / c);

            var alpha = (a + b) / (2 * Shape * c);
            var z = (a - b) / Shape;


            return alpha * System.Math.Exp(-0.5 * z * z) / System.Math.Sqrt(2.0 * System.Math.PI);
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
            if (p <= 0)
            {
                return Support.Min;
            }

            if (p >= 1)
            {
                return Support.Max;
            }

            var z = Normal.Inverse(p);
            var a = z + System.Math.Sqrt(4 + Shape * (z * z));

            return 1.0 / 4.0 * a * a + Location;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new BirnbaumSaundersDistribution(Location, Scale, Shape);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "BirnbaumSaunders(x; μ = {0}, β = {1}, γ = {2})",
                Location.ToString(format, formatProvider),
                Scale.ToString(format, formatProvider),
                Shape.ToString(format, formatProvider));
        }
    }
}