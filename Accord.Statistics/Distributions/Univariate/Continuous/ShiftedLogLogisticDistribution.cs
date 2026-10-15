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
using Accord.Diagnostics;
using Accord.Math;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Shift Log-Logistic distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The shifted log-logistic distribution is a probability distribution also known as
    ///         the generalized log-logistic or the three-parameter log-logistic distribution. It
    ///         has also been called the generalized logistic distribution, but this conflicts with
    ///         other uses of the term: see generalized logistic distribution.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Shifted_log-logistic_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Shifted log-logistic distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Shifted_log-logistic_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This examples shows how to create a Shifted Log-Logistic distribution,
    ///         compute some of its properties and generate a number of random samples
    ///         from it.
    ///     </para>
    ///     <code>
    /// // Create a LLD3 distribution with μ = 0.0, scale = 0.42, and shape = 0.1
    /// var log = new ShiftedLogLogisticDistribution(location: 0, scale: 0.42, shape: 0.1);
    /// 
    /// double mean = log.Mean;     // 0.069891101544818923
    /// double median = log.Median; // 0.0
    /// double mode = log.Mode;     // -0.083441677069328604
    /// double var = log.Variance;  // 0.62447259946747213
    /// 
    /// double cdf = log.DistributionFunction(x: 1.4); // 0.94668863559417671
    /// double pdf = log.ProbabilityDensityFunction(x: 1.4); // 0.090123683626808615
    /// double lpdf = log.LogProbabilityDensityFunction(x: 1.4); // -2.4065722895662613
    /// 
    /// double ccdf = log.ComplementaryDistributionFunction(x: 1.4); // 0.053311364405823292
    /// double icdf = log.InverseDistributionFunction(p: cdf); // 1.4000000037735139
    /// 
    /// double hf = log.HazardFunction(x: 1.4); // 1.6905154207038875
    /// double chf = log.CumulativeHazardFunction(x: 1.4); // 2.9316057546685061
    /// 
    /// string str = log.ToString(CultureInfo.InvariantCulture); // LLD3(x; μ = 0, σ = 0.42, ξ = 0.1)
    /// </code>
    /// </example>
    /// <seealso cref="LogisticDistribution" />
    /// <seealso cref="ParetoDistribution" />
    [Serializable]
    public class ShiftedLogLogisticDistribution : UnivariateContinuousDistribution
    {
        // Distribution parameters

        /// <summary>
        ///     Constructs a Shifted Log-Logistic distribution
        ///     with zero location, unit scale, and zero shape.
        /// </summary>
        public ShiftedLogLogisticDistribution()
        {
            initialize(0, 1, 0);
        }

        /// <summary>
        ///     Constructs a Shifted Log-Logistic distribution
        ///     with the given location, unit scale and zero shape.
        /// </summary>
        /// <param name="location">The distribution's location value μ (mu).</param>
        public ShiftedLogLogisticDistribution([Real] double location)
        {
            initialize(location, 1, 0);
        }

        /// <summary>
        ///     Constructs a Shifted Log-Logistic distribution
        ///     with the given location and scale and zero shape.
        /// </summary>
        /// <param name="location">The distribution's location value μ (mu).</param>
        /// <param name="scale">The distribution's scale value σ (sigma).</param>
        public ShiftedLogLogisticDistribution([Real] double location, [Positive] double scale)
        {
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale", "Scale must be positive.");
            }

            initialize(location, scale, 0);
        }

        /// <summary>
        ///     Constructs a Shifted Log-Logistic distribution
        ///     with the given location and scale and zero shape.
        /// </summary>
        /// <param name="location">The distribution's location value μ (mu).</param>
        /// <param name="scale">The distribution's scale value s.</param>
        /// <param name="shape">The distribution's shape value ξ (ksi).</param>
        public ShiftedLogLogisticDistribution([Real] double location, [Positive] double scale, [Real] double shape)
        {
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale", "Scale must be positive.");
            }

            initialize(location, scale, shape);
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Mean
        {
            get
            {
                if (Shape == 0)
                {
                    return Location;
                }

                var b = System.Math.PI * Shape;
                var a = Scale / Shape;
                return Location + a * b * Special.Cosec(b) - a * 1;
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the distribution's location value μ (mu).
        /// </summary>
        public double Location { get; private set; }

        /// <summary>
        ///     Gets the distribution's scale value (σ).
        /// </summary>
        public double Scale { get; private set; }

        /// <summary>
        ///     Gets the distribution's shape value (ξ).
        /// </summary>
        public double Shape { get; private set; }

        /// <summary>
        ///     Gets the median for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's median value.
        /// </value>
        public override double Median
        {
            get
            {
                Debug.Assert(Location.IsEqual(base.Median, 1e-5));
                return Location;
            }
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's variance.
        /// </value>
        public override double Variance
        {
            get
            {
                if (Shape == 0)
                {
                    // lim x->0  ((b^2)/(x^2))*(2*pi*x csc(2*pi*x) - (pi*x*csc(pi*x))^2)
                    // http://www.wolframalpha.com/input/?i=lim+x-%3E0++%28a+%2B+%28%28b%5E2%29%2F%28x%5E2%29%29*%282*pi*x+csc%282*pi*x%29+-+%28pi*x*csc%28pi*x%29%29%5E2%29%29
                    return Scale * Scale * System.Math.PI * System.Math.PI / 3.0;
                }

                var pb = System.Math.PI * Shape;
                var a = Scale * Scale / (Shape * Shape);
                var b = 2 * pb * Special.Cosec(2 * pb);
                var c = pb * Special.Cosec(pb);

                return a * (b - c * c);
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's mode value.
        /// </value>
        public override double Mode
        {
            get
            {
                if (Shape == 0)
                {
                    return Location;
                }

                return Location + Scale / Shape * (System.Math.Pow((1 - Shape) / (1 + Shape), Shape) - 1);
            }
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
            get
            {
                if (Shape > 0)
                {
                    return new DoubleRange(Location - Scale / Shape, double.PositiveInfinity);
                }

                if (Shape < 0)
                {
                    return new DoubleRange(double.NegativeInfinity, Location - Scale / Shape);
                }

                // if (ksi == 0)
                return new DoubleRange(double.NegativeInfinity, double.PositiveInfinity);
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        protected internal override double InnerDistributionFunction(double x)
        {
            if (Shape > 0 && x < Location - Scale / Shape)
            {
                return 0;
            }

            var z = (x - Location) / Scale;

            if (Shape == 0)
            {
                return 1 / (1 + System.Math.Exp(-z));
            }

            return 1 / (1 + System.Math.Pow(1 + Shape * z, -1 / Shape));
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
            var z = (x - Location) / Scale;

            double a, b;

            if (Shape == 0)
            {
                a = System.Math.Exp(-z);
                b = 1 + a;
            }
            else
            {
                a = System.Math.Pow(1 + Shape * z, -1 / Shape - 1);
                b = 1 + System.Math.Pow(1 + Shape * z, -1 / Shape);
            }

            return a / (Scale * b * b);
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new ShiftedLogLogisticDistribution(Location, Scale, Shape);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "LLD3(x; μ = {0}, σ = {1}, ξ = {2})",
                Location.ToString(format, formatProvider),
                Scale.ToString(format, formatProvider),
                Shape.ToString(format, formatProvider));
        }

        private void initialize(double mean, double scale, double shape)
        {
            Location = mean;
            Scale = scale;
            Shape = shape;
        }
    }
}