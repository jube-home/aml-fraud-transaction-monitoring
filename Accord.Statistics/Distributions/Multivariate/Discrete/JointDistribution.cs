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
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Multivariate
{
    /// <summary>
    ///     Joint distribution of multiple discrete univariate distributions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class builds a (potentially huge) lookup table for discrete
    ///         symbol distributions. For example, given a discrete variable A
    ///         which may take symbols a, b, c; and a discrete variable B which
    ///         may assume values x, y, z, this class will build the probability
    ///         table:
    ///     </para>
    ///     <code>
    ///          x      y      z
    ///   a   p(a,x) p(a,y) p(a,z)
    ///   b   p(b,x) p(b,y) p(b,z)
    ///   c   p(c,x) p(c,y) p(c,z)
    /// </code>
    ///     <para>
    ///         Thus comprising the probabilities for all possible simple combination. This
    ///         distribution is a generalization of the
    ///         <see cref="Univariate.GeneralDiscreteDistribution" />
    ///         for multivariate discrete observations.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example should demonstrate how to estimate a joint
    ///         distribution of two discrete variables. The first variable can
    ///         take up to three distinct values, whereas the second can assume
    ///         up to five.
    ///     </para>
    ///     <code>
    ///   // Lets create a joint distribution for two discrete variables:
    ///   // the first of which can assume 3 distinct symbol values: 0, 1, 2
    ///   // the second which can assume 5 distinct symbol values: 0, 1, 2, 3, 4
    ///   
    ///   int[] symbols = { 3, 5 }; // specify the symbol counts
    ///   
    ///   // Create the joint distribution for the above variables
    ///   JointDistribution joint = new JointDistribution(symbols);
    /// 
    ///   // Now, suppose we would like to fit the distribution (estimate
    ///   // its parameters) from the following multivariate observations:
    ///   //
    ///   double[][] observations = 
    ///   {
    ///       new double[] { 0, 0 },
    ///       new double[] { 1, 1 },
    ///       new double[] { 2, 1 },
    ///       new double[] { 0, 0 },
    ///   };
    /// 
    ///   
    ///   // Estimate parameters
    ///   joint.Fit(observations);
    /// 
    ///   // At this point, we can query the distribution for observations:
    ///   double p1 = joint.ProbabilityMassFunction(new[] { 0, 0 }); // should be 0.50
    ///   double p2 = joint.ProbabilityMassFunction(new[] { 1, 1 }); // should be 0.25
    ///   double p3 = joint.ProbabilityMassFunction(new[] { 2, 1 }); // should be 0.25
    /// 
    ///   // As it can be seem, indeed {0,0} appeared twice at the data, 
    ///   // and {1,1} and {2,1 appeared one fourth of the data each.
    /// </code>
    /// </example>
    /// <see cref="Univariate.GeneralDiscreteDistribution" />
    /// <see cref="Independent{TDistribution}" />
    [Serializable]
    public class JointDistribution : MultivariateDiscreteDistribution,
        IFittableDistribution<int[], GeneralDiscreteOptions>,
        IFittableDistribution<double[]>
    {
        // distribution measures
        private double[] mean;

        private int[] positions;

        // distribution parameters

        /// <summary>
        ///     Constructs a new joint discrete distribution.
        /// </summary>
        public JointDistribution(int[] symbols)
            : base(symbols.Length)
        {
            init(new int[symbols.Length], symbols);
        }

        /// <summary>
        ///     Constructs a new joint discrete distribution.
        /// </summary>
        public JointDistribution(int[] starts, int[] symbols)
            : base(symbols.Length)
        {
            init(starts, symbols);
        }

        /// <summary>
        ///     Constructs a new joint discrete distribution.
        /// </summary>
        public JointDistribution(int[] starts, Array probabilities)
            : base(starts.Length)
        {
            init(starts, probabilities);
        }

        /// <summary>
        ///     Constructs a new joint discrete distribution.
        /// </summary>
        public JointDistribution(Array probabilities)
            : base(probabilities.Rank)
        {
            init(new int[probabilities.Rank], probabilities);
        }

        /// <summary>
        ///     Constructs a new joint discrete distribution.
        /// </summary>
        private JointDistribution(int dimension)
            : base(dimension)
        {
        }

        /// <summary>
        ///     Gets the frequency of observation of each discrete variable.
        /// </summary>
        public double[] Frequencies { get; private set; }

        /// <summary>
        ///     Gets the integer value where the
        ///     discrete distribution starts.
        /// </summary>
        public int[] Minimum { get; private set; }

        /// <summary>
        ///     Gets the integer value where the
        ///     discrete distribution ends.
        /// </summary>
        public int[] Maximum => Minimum.Add(Lengths);

        /// <summary>
        ///     Gets the number of symbols in the distribution.
        /// </summary>
        public int[] Lengths { get; private set; }

        /// <summary>
        ///     Gets the number of symbols for each discrete variable.
        /// </summary>
        [Obsolete("Please use Lengths instead.")]
        public int[] Symbols => Lengths;

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="IntRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override IntRange[] Support
        {
            get
            {
                var range = new IntRange[Dimension];
                for (var i = 0; i < range.Length; i++)
                {
                    range[i] = new IntRange(Minimum[i], Maximum[i]);
                }

                return range;
            }
        }

        /// <summary>
        ///     Gets or sets the probability value attached to the given index.
        /// </summary>
        public double this[int[] indices]
        {
            get
            {
                var index = ravel(indices);
                return Frequencies[index];
            }
            set
            {
                var index = ravel(indices);
                Frequencies[index] = value;
            }
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the mean values for this distribution.
        /// </value>
        public override double[] Mean
        {
            get
            {
                if (mean == null)
                {
                    mean = Minimum.ToDouble();

                    foreach (var idx in Lengths.Sequences())
                    {
                        var p = this[idx.Add(Minimum)];
                        for (var i = 0; i < idx.Length; i++)
                        {
                            mean[i] += idx[i] * p;
                        }
                    }
                }

                return mean;
            }
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the variance values for this distribution.
        /// </value>
        public override double[] Variance => throw new NotSupportedException();

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>
        ///     An multidimensional array of double-precision values
        ///     containing the covariance values for this distribution.
        /// </value>
        public override double[,] Covariance => throw new NotSupportedException();

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        public void Fit(int[][] observations)
        {
            Fit(observations, null, null);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights"></param>
        public void Fit(int[][] observations, double[] weights)
        {
            Fit(observations, weights, null);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        /// <exception cref="ArgumentException">This method does not accept fitting options.</exception>
        /// <exception cref="DimensionMismatchException">weights;The weight vector should have the same size as the observations</exception>
        public void Fit(int[][] observations, double[] weights, GeneralDiscreteOptions options)
        {
            if (options != null)
            {
                throw new ArgumentException("This method does not accept fitting options.");
            }

            for (var i = 0; i < Frequencies.Length; i++)
            {
                Frequencies[i] = 0;
            }

            if (weights != null)
            {
                if (observations.Length != weights.Length)
                {
                    throw new DimensionMismatchException("weights",
                        "The weight vector should have the same size as the observations");
                }

                for (var i = 0; i < observations.Length; i++)
                {
                    Frequencies[ravel(observations[i])] += weights[i];
                }
            }
            else
            {
                for (var i = 0; i < observations.Length; i++)
                {
                    Frequencies[ravel(observations[i])]++;
                }
            }

            double sum = 0;
            for (var i = 0; i < Frequencies.Length; i++)
            {
                sum += Frequencies[i];
            }

            if (sum != 0 && sum != 1)
            {
                for (var i = 0; i < Frequencies.Length; i++)
                {
                    Frequencies[i] /= sum;
                }
            }
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            var d = new JointDistribution(Dimension);
            d.positions = positions.Copy();
            d.Frequencies = Frequencies.Copy();
            d.Lengths = Lengths.Copy();
            d.Minimum = Minimum.Copy();
            return d;
        }

        private void init(int[] starts, int[] symbols)
        {
            this.Lengths = symbols;
            Minimum = starts;
            positions = new int[symbols.Length];

            positions[positions.Length - 1] = 1;
            for (var i = positions.Length - 2; i >= 0; i--)
            {
                positions[i] = positions[i + 1] * symbols[i + 1];
            }

            var total = 1;
            for (var i = 0; i < symbols.Length; i++)
            {
                total *= symbols[i];
            }

            Frequencies = new double[total];
            for (var i = 0; i < Frequencies.Length; i++)
            {
                Frequencies[i] = 1.0 / total;
            }
        }

        private void init(int[] starts, Array array)
        {
            Lengths = array.GetLength();
            Minimum = starts;
            positions = new int[Lengths.Length];
            Frequencies = new double[array.Length];

            positions[positions.Length - 1] = 1;
            for (var i = positions.Length - 2; i >= 0; i--)
            {
                positions[i] = positions[i + 1] * Lengths[i + 1];
            }

            foreach (var idx in array.GetIndices(true))
            {
                var index = 0;
                for (var j = 0; j < idx.Length; j++)
                {
                    index += idx[j] * positions[j];
                }

                Frequencies[index] = (double)Convert.ChangeType(array.GetValue(true, idx), typeof(double));
            }
        }

        private int ravel(int[] indices)
        {
            var index = 0;
            for (var j = 0; j < indices.Length; j++)
            {
                index += (indices[j] - Minimum[j]) * positions[j];
            }

            return index;
        }

        private int ravel(double[] indices)
        {
            var index = 0;
            for (var j = 0; j < indices.Length; j++)
            {
                index += ((int)indices[j] - Minimum[j]) * positions[j];
            }

            return index;
        }

        /// <summary>
        ///     Constructs a new multidimensional uniform discrete distribution.
        /// </summary>
        /// <param name="dimensions">The number of dimensions in the joint distribution.</param>
        /// <param name="a">
        ///     The integer value where the distribution starts, also
        ///     known as <c>a</c>. Default value is 0.
        /// </param>
        /// <param name="b">
        ///     The integer value where the distribution ends, also
        ///     known as <c>b</c>.
        /// </param>
        public static JointDistribution Uniform(int dimensions, int a, int b)
        {
            if (a > b)
            {
                throw new ArgumentOutOfRangeException("b",
                    "The starting number a must be lower than b.");
            }

            return new JointDistribution(
                Vector.Create(dimensions, a),
                Vector.Create(dimensions, b - a + 1));
        }

        /// <summary>
        ///     Gets the probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        protected internal override double InnerProbabilityMassFunction(int[] x)
        {
            var index = ravel(x);
            if (index < 0 || index >= Frequencies.Length)
            {
                return 0;
            }

            return Frequencies[index];
        }

        /// <summary>
        ///     Gets the log-probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerLogProbabilityMassFunction(int[] x)
        {
            var index = ravel(x);
            if (index < 0 || index >= Frequencies.Length)
            {
                return System.Math.Log(0);
            }

            return System.Math.Log(Frequencies[index]);
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
        protected internal override double InnerDistributionFunction(int[] x)
        {
            var value = x.Subtract(Minimum);

            var sum = 0.0;

            foreach (var idx in Lengths.Sequences())
            {
                if (lessThanOrEqual(value, idx))
                {
                    sum += this[idx];
                }
            }

            return sum;
        }

        private static bool lessThanOrEqual(int[] x, int[] idx)
        {
            for (var i = 0; i < x.Length; i++)
            {
                if (idx[i] > x[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        public override void Fit(double[][] observations, double[] weights, IFittingOptions options)
        {
            if (options != null)
            {
                throw new ArgumentException("This method does not accept fitting options.");
            }

            for (var i = 0; i < Frequencies.Length; i++)
            {
                Frequencies[i] = 0;
            }

            if (weights != null)
            {
                if (observations.Length != weights.Length)
                {
                    throw new DimensionMismatchException("weights",
                        "The weight vector should have the same size as the observations");
                }

                for (var i = 0; i < observations.Length; i++)
                {
                    Frequencies[ravel(observations[i])] += weights[i];
                }
            }
            else
            {
                for (var i = 0; i < observations.Length; i++)
                {
                    Frequencies[ravel(observations[i])]++;
                }
            }

            double sum = 0;
            for (var i = 0; i < Frequencies.Length; i++)
            {
                sum += Frequencies[i];
            }

            if (sum != 0 && sum != 1)
            {
                // TODO: add the following in a JointOption class:
                // avoid locking a parameter in zero.
                // if (num == 0) num = 1e-10;

                // assert that probabilities sum up to 1.
                for (var i = 0; i < Frequencies.Length; i++)
                {
                    Frequencies[i] /= sum;
                }
            }
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Joint(X)");
        }

        /// <summary>
        ///     Estimates a new <see cref="JointDistribution" /> from a given set of observations.
        /// </summary>
        /// <example>
        ///     Please see <see cref="JointDistribution" />.
        /// </example>
        public static JointDistribution Estimate(int[][] values)
        {
            var joint = new JointDistribution(values.DistinctCount());
            joint.Fit(values);
            return joint;
        }
    }
}