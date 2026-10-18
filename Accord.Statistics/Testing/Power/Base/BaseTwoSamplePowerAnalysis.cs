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
using System.Globalization;
using Accord.Math.Optimization;

namespace Accord.Statistics.Testing.Power
{
    /// <summary>
    ///     Base class for two sample power analysis methods.
    ///     This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public abstract class BaseTwoSamplePowerAnalysis : ITwoSamplePowerAnalysis
    {
        /// <summary>
        ///     Constructs a new power analysis for a two-sample test.
        /// </summary>
        protected BaseTwoSamplePowerAnalysis(DistributionTail tail)
        {
            Tail = tail;
            Size = 0.05;
            Power = 0.8;
            Samples1 = 2;
            Samples2 = 2;
        }

        /// <summary>
        ///     Gets the total number of observations
        ///     in both samples considered in the test.
        /// </summary>
        public double TotalSamples => Samples1 + Samples2;

        /// <summary>
        ///     Gets the test type.
        /// </summary>
        public DistributionTail Tail { get; set; }

        /// <summary>
        ///     Gets or sets the power of the test, also
        ///     known as the (1-Beta error rate).
        /// </summary>
        public double Power { get; set; }

        /// <summary>
        ///     Gets or sets the significance level
        ///     for the test. Also known as alpha.
        /// </summary>
        public double Size { get; set; }

        /// <summary>
        ///     Gets or sets the number of observations
        ///     in the first sample considered in the test.
        /// </summary>
        public double Samples1 { get; set; }

        /// <summary>
        ///     Gets or sets the number of observations
        ///     in the second sample considered in the test.
        /// </summary>
        public double Samples2 { get; set; }

        /// <summary>
        ///     Gets the total number of observations
        ///     in both samples considered in the test.
        /// </summary>
        double IPowerAnalysis.Samples => TotalSamples;

        /// <summary>
        ///     Gets or sets the effect size of the test.
        /// </summary>
        public double Effect { get; set; }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        ///     Converts the numeric power of this test to its equivalent string representation.
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return Power.ToString(format, formatProvider);
        }

        /// <summary>
        ///     Computes the power for a test with givens values of
        ///     <see cref="Effect">effect size</see> and
        ///     <see cref="TotalSamples">
        ///         number of samples
        ///     </see>
        ///     under <see cref="Size" />.
        /// </summary>
        /// <returns>The power for the test under the given conditions.</returns>
        public abstract void ComputePower();

        /// <summary>
        ///     Computes the minimum detectable effect size for the test
        ///     considering the power given in <see cref="Power" />, the
        ///     number of samples in <see cref="TotalSamples" /> and the
        ///     significance level <see cref="Size" />.
        /// </summary>
        /// <returns>
        ///     The minimum detectable
        ///     <see cref="Effect">
        ///         effect
        ///         size
        ///     </see>
        ///     for the test under the given conditions.
        /// </returns>
        public virtual void ComputeEffect()
        {
            var requiredPower = Power;

            // Attempt to locate the optimal sample size
            // to attain the required power by locating
            // a zero in the difference function:

            var sol = BrentSearch.FindRoot(eff =>
                {
                    Effect = eff;
                    ComputePower();

                    return requiredPower - Power;
                },
                1e-5,
                1e+5);


            // Check it
            Effect = sol;
            ComputePower();

            var newPower = Power;
            Power = requiredPower;

            if (System.Math.Abs(requiredPower - newPower) > 1e-5)
            {
                Effect = double.NaN;
            }
        }

        /// <summary>
        ///     Computes the minimum significance level for the test
        ///     considering the power given in <see cref="Power" />, the
        ///     number of samples in <see cref="TotalSamples" /> and the
        ///     effect size <see cref="Effect" />.
        /// </summary>
        /// <returns>
        ///     The minimum detectable
        ///     <see cref="Effect">
        ///         effect
        ///         size
        ///     </see>
        ///     for the test under the given conditions.
        /// </returns>
        public virtual void ComputeSize()
        {
            var requiredPower = Power;

            // Attempt to locate the optimal sample size
            // to attain the required power by locating
            // a zero in the difference function:

            var sol = BrentSearch.FindRoot(alpha =>
                {
                    Size = alpha;
                    ComputePower();

                    return requiredPower - Power;
                },
                0,
                1);


            // Check it
            Size = sol;
            ComputePower();

            var newPower = Power;
            Power = requiredPower;

            if (System.Math.Abs(requiredPower - newPower) > 1e-5)
            {
                Effect = double.NaN;
            }
        }

        /// <summary>
        ///     Computes the recommended sample size for the test to attain
        ///     the power indicated in <see cref="Power" /> considering
        ///     values of <see cref="Effect" /> and <see cref="Size" />.
        /// </summary>
        /// <returns>
        ///     Recommended sample size for attaining the given
        ///     <see cref="Power" /> for size effect <see cref="Effect" />
        ///     under the given <see cref="Size" />.
        /// </returns>
        public virtual void ComputeSamples(double proportion = 1)
        {
            var requiredPower = Power;

            // Attempt to locate the optimal sample size
            // to attain the required power by locating
            // a zero in the difference function:

            var sol = BrentSearch.FindRoot(n =>
                {
                    Samples1 = n;
                    Samples2 = n * proportion;

                    ComputePower();

                    return requiredPower - Power;
                },
                2,
                1e+4);


            // Check it
            Samples1 = sol;
            Samples2 = sol * proportion;
            ComputePower();

            var newPower = Power;
            Power = requiredPower;

            if (System.Math.Abs(requiredPower - newPower) > 1e-5)
            {
                Samples1 = Samples2 = double.NaN;
            }
        }

        /// <summary>
        ///     Converts the numeric power of this test to its equivalent string representation.
        /// </summary>
        public override string ToString()
        {
            return Power.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     Gets the minimum difference in the experiment units
        ///     to which it is possible to detect a difference.
        /// </summary>
        /// <param name="standardDeviation">The common standard deviation for the samples.</param>
        /// <returns>The minimum difference in means which can be detected by the test.</returns>
        public double GetDiferentiableUnits(double standardDeviation)
        {
            return Effect * standardDeviation;
        }

        /// <summary>
        ///     Gets the minimum difference in the experiment units
        ///     to which it is possible to detect a difference.
        /// </summary>
        /// <param name="var1">The variance for the first sample.</param>
        /// <param name="var2">The variance for the second sample.</param>
        /// <returns>The minimum difference in means which can be detected by the test.</returns>
        public double GetDiferentiableUnits(double var1, double var2)
        {
            return GetDiferentiableUnits(System.Math.Sqrt((var1 + var2) / 2.0));
        }
    }
}