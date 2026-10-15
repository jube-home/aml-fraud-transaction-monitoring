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
using Accord.Statistics.Analysis;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Kappa Test for agreement in contingency tables.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Kappa test tries to assert whether the Kappa measure of a
    ///         a contingency table, is significantly different from another
    ///         hypothesized value.
    ///     </para>
    ///     <para>
    ///         The computations used by the test are the same found in the 1969 paper by
    ///         J. L. Fleiss, J. Cohen, B. S. Everitt, in which they presented the finally
    ///         corrected version of the Kappa's variance formulae. This is contrast to the
    ///         computations traditionally found in the remote sensing literature. For those
    ///         variance computations, see the <see cref="DeltaMethodKappaVariance(GeneralConfusionMatrix)" /> method.
    ///     </para>
    ///     <para>
    ///         This is a <see cref="ZTest">z-test kind of test</see>.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     J. L. Fleiss. Statistical methods for rates and proportions.
    ///                     Wiley-Interscience; 3rd edition (September 5, 2003)
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     J. L. Fleiss, J. Cohen, B. S. Everitt. Large sample standard errors of
    ///                     kappa and weighted kappa. Psychological Bulletin, Volume: 72, Issue: 5. Washington,
    ///                     DC: American Psychological Association, Pages: 323-327, 1969.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="ZTest" />
    [Serializable]
    public class KappaTest : ZTest
    {
        /// <summary>
        ///     Creates a new Kappa test.
        /// </summary>
        /// <param name="sampleKappa">The estimated Kappa statistic.</param>
        /// <param name="standardError">
        ///     The standard error of the kappa statistic. If the test is
        ///     being used to assert independency between two raters (i.e. testing the null hypothesis
        ///     that the underlying Kappa is zero), then the
        ///     <see cref="AsymptoticKappaVariance(GeneralConfusionMatrix)">
        ///         standard error should be computed with the null hypothesis parameter set to true
        ///     </see>
        ///     .
        /// </param>
        /// <param name="alternate">
        ///     The alternative hypothesis (research hypothesis) to test. If the
        ///     hypothesized kappa is left unspecified, a one-tailed test will be used. Otherwise, the
        ///     default is to use a two-sided test.
        /// </param>
        public KappaTest(double sampleKappa, double standardError,
            OneSampleHypothesis alternate = OneSampleHypothesis.ValueIsGreaterThanHypothesis)
            : this(sampleKappa, standardError, 0, alternate)
        {
        }

        /// <summary>
        ///     Creates a new Kappa test.
        /// </summary>
        /// <param name="sampleKappa">The estimated Kappa statistic.</param>
        /// <param name="standardError">
        ///     The standard error of the kappa statistic. If the test is
        ///     being used to assert independency between two raters (i.e. testing the null hypothesis
        ///     that the underlying Kappa is zero), then the
        ///     <see cref="AsymptoticKappaVariance(GeneralConfusionMatrix)">
        ///         standard error should be computed with the null hypothesis parameter set to true
        ///     </see>
        ///     .
        /// </param>
        /// <param name="hypothesizedKappa">The hypothesized value for the Kappa statistic.</param>
        /// <param name="alternate">
        ///     The alternative hypothesis (research hypothesis) to test. If the
        ///     hypothesized kappa is left unspecified, a one-tailed test will be used. Otherwise, the
        ///     default is to use a two-sided test.
        /// </param>
        public KappaTest(double sampleKappa, double standardError, double hypothesizedKappa,
            OneSampleHypothesis alternate = OneSampleHypothesis.ValueIsDifferentFromHypothesis)
        {
            Variance = standardError * standardError;

            Compute(sampleKappa, hypothesizedKappa, standardError, alternate);
        }

        /// <summary>
        ///     Creates a new Kappa test.
        /// </summary>
        /// <param name="matrix">The contingency table to test.</param>
        /// <param name="alternate">
        ///     The alternative hypothesis (research hypothesis) to test. If the
        ///     hypothesized kappa is left unspecified, a one-tailed test will be used. Otherwise, the
        ///     default is to use a two-sided test.
        /// </param>
        public KappaTest(GeneralConfusionMatrix matrix,
            OneSampleHypothesis alternate = OneSampleHypothesis.ValueIsGreaterThanHypothesis)
            : this(matrix, 0, alternate)
        {
        }

        /// <summary>
        ///     Creates a new Kappa test.
        /// </summary>
        /// <param name="matrix">The contingency table to test.</param>
        /// <param name="alternate">
        ///     The alternative hypothesis (research hypothesis) to test. If the
        ///     hypothesized kappa is left unspecified, a one-tailed test will be used. Otherwise, the
        ///     default is to use a two-sided test.
        /// </param>
        /// <param name="hypothesizedKappa">
        ///     The hypothesized value for the Kappa statistic. If the test
        ///     is being used to assert independency between two raters (i.e. testing the null hypothesis
        ///     that the underlying Kappa is zero), then the
        ///     <see cref="AsymptoticKappaVariance(GeneralConfusionMatrix)">
        ///         standard error will be computed with the null hypothesis parameter set to true
        ///     </see>
        ///     .
        /// </param>
        public KappaTest(GeneralConfusionMatrix matrix, double hypothesizedKappa,
            OneSampleHypothesis alternate = OneSampleHypothesis.ValueIsDifferentFromHypothesis)
        {
            if (hypothesizedKappa == 0)
            {
                // Use the null hypothesis variance
                Compute(matrix.Kappa, hypothesizedKappa, matrix.StandardErrorUnderNull, alternate);
                Variance = matrix.VarianceUnderNull;
            }
            else
            {
                // Use the default variance
                Compute(matrix.Kappa, hypothesizedKappa, matrix.StandardError, alternate);
                Variance = matrix.Variance;
            }
        }

        /// <summary>
        ///     Creates a new Kappa test.
        /// </summary>
        /// <param name="matrix">The contingency table to test.</param>
        /// <param name="alternate">
        ///     The alternative hypothesis (research hypothesis) to test. If the
        ///     hypothesized kappa is left unspecified, a one-tailed test will be used. Otherwise, the
        ///     default is to use a two-sided test.
        /// </param>
        /// <param name="hypothesizedWeightedKappa">
        ///     The hypothesized value for the Kappa statistic. If the test
        ///     is being used to assert independency between two raters (i.e. testing the null hypothesis
        ///     that the underlying Kappa is zero), then the
        ///     <see cref="AsymptoticKappaVariance(GeneralConfusionMatrix)">
        ///         standard error will be computed with the null hypothesis parameter set to true
        ///     </see>
        ///     .
        /// </param>
        public KappaTest(WeightedConfusionMatrix matrix, double hypothesizedWeightedKappa,
            OneSampleHypothesis alternate = OneSampleHypothesis.ValueIsDifferentFromHypothesis)
        {
            if (hypothesizedWeightedKappa == 0)
            {
                // Use the null hypothesis variance
                Compute(matrix.WeightedKappa, hypothesizedWeightedKappa, matrix.WeightedStandardErrorUnderNull,
                    alternate);
                Variance = matrix.WeightedVarianceUnderNull;
            }
            else
            {
                // Use the default variance
                Compute(matrix.WeightedKappa, hypothesizedWeightedKappa, matrix.WeightedStandardError, alternate);
                Variance = matrix.WeightedVariance;
            }
        }

        /// <summary>
        ///     Gets the variance of the Kappa statistic.
        /// </summary>
        public double Variance { get; private set; }

        /// <summary>
        ///     Compute Cohen's Kappa variance using the large sample approximation
        ///     given by Congalton, which is common in the remote sensing literature.
        /// </summary>
        /// <param name="matrix">A <see cref="GeneralConfusionMatrix" /> representing the ratings.</param>
        /// <returns>Kappa's variance.</returns>
        public static double DeltaMethodKappaVariance(GeneralConfusionMatrix matrix)
        {
            return DeltaMethodKappaVariance(matrix, out _);
        }

        /// <summary>
        ///     Compute Cohen's Kappa variance using the large sample approximation
        ///     given by Congalton, which is common in the remote sensing literature.
        /// </summary>
        /// <param name="matrix">A <see cref="GeneralConfusionMatrix" /> representing the ratings.</param>
        /// <param name="stdDev">Kappa's standard deviation.</param>
        /// <returns>Kappa's variance.</returns>
        public static double DeltaMethodKappaVariance(GeneralConfusionMatrix matrix, out double stdDev)
        {
            var n = matrix.NumberOfSamples;
            double sum;

            var θ1 = 1.0 / n * matrix.Diagonal.Sum(); // observed agreement, po

            sum = 0;
            for (var i = 0; i < matrix.RowTotals.Length; i++)
            {
                sum += matrix.RowTotals[i] * matrix.ColumnTotals[i];
            }

            var θ2 = 1.0 / (n * n) * sum; // expected agreement, pe


            sum = 0;
            for (var i = 0; i < matrix.RowTotals.Length; i++)
            {
                sum += matrix.Diagonal[i] * (matrix.RowTotals[i] + matrix.ColumnTotals[i]);
            }

            var θ3 = 1.0 / (n * n) * sum;


            sum = 0;
            for (var i = 0; i < matrix.RowTotals.Length; i++)
            {
                for (var j = 0; j < matrix.ColumnTotals.Length; j++)
                {
                    sum += matrix.Matrix[i, j] * System.Math.Pow(matrix.RowTotals[i] + matrix.ColumnTotals[j], 2);
                }
            }

            var θ4 = 1.0 / (n * n * n) * sum;

            var A = θ1 * (1 - θ1) / ((1 - θ2) * (1 - θ2));
            var B = 2 * (1 - θ1) * (2 * θ1 * θ2 - θ3) / ((1 - θ2) * (1 - θ2) * (1 - θ2));
            var C = (1 - θ1) * (1 - θ1) * (θ4 - 4 * θ2 * θ2) / ((1 - θ2) * (1 - θ2) * (1 - θ2) * (1 - θ2));

            var var = 1.0 / n * (A + B + C);
            stdDev = System.Math.Sqrt(var);

            return var;
        }

        /// <summary>
        ///     Computes the asymptotic variance for Fleiss's Kappa variance using the formulae
        ///     by (Fleiss et al, 1969) when the underlying Kappa is assumed different from zero.
        /// </summary>
        /// <param name="matrix">A <see cref="GeneralConfusionMatrix" /> representing the ratings.</param>
        /// <returns>Kappa's variance.</returns>
        public static double AsymptoticKappaVariance(GeneralConfusionMatrix matrix)
        {
            return AsymptoticKappaVariance(matrix, out _);
        }

        /// <summary>
        ///     Computes the asymptotic variance for Fleiss's Kappa variance using the formulae
        ///     by (Fleiss et al, 1969). If <paramref name="nullHypothesis" /> is set to true, the
        ///     method will return the variance under the null hypothesis.
        /// </summary>
        /// <param name="matrix">A <see cref="GeneralConfusionMatrix" /> representing the ratings.</param>
        /// <param name="stdDev">Kappa's standard deviation.</param>
        /// <param name="nullHypothesis">
        ///     True to compute Kappa's variance when the null hypothesis
        ///     is true (i.e. that the underlying kappa is zer). False otherwise. Default is false.
        /// </param>
        /// <returns>Kappa's variance.</returns>
        public static double AsymptoticKappaVariance(GeneralConfusionMatrix matrix, out double stdDev,
            bool nullHypothesis = false)
        {
            double n = matrix.NumberOfSamples;
            var k = matrix.Kappa;

            var p = matrix.ProportionMatrix;
            var colMarginal = matrix.ColumnProportions;
            var rowMarginal = matrix.RowProportions;

            double Pe = 0;
            for (var i = 0; i < rowMarginal.Length; i++)
            {
                Pe += rowMarginal[i] * colMarginal[i];
            }

            double variance;


            if (!nullHypothesis)
            {
                // References: Statistical Methods for Rates and Proportions, pg 606.
                // Fleiss calculations on page 607 are done with a rounded Kappa of:
                //
                //   k = 0.68


                // Compute A (eq. 18.16)
                double A = 0;
                for (var i = 0; i < rowMarginal.Length; i++)
                {
                    var pii = p[i, i];
                    var pid = rowMarginal[i];
                    var pdi = colMarginal[i];
                    A += pii * square(1.0 - (pid + pdi) * (1 - k));
                }

                // Compute B (eq. 18.17)
                double sum = 0;
                for (var i = 0; i < colMarginal.Length; i++)
                {
                    for (var j = 0; j < rowMarginal.Length; j++)
                    {
                        if (i != j)
                        {
                            var pij = p[i, j];
                            var pdi = colMarginal[i];
                            var pjd = rowMarginal[j];
                            sum += pij * square(pdi + pjd);
                        }
                    }
                }

                var B = square(1.0 - k) * sum;


                // Compute C
                var C = square(k - Pe * (1 - k));

                // Compute variance and standard error using A, B and C
                variance = (A + B - C) / (square(1.0 - Pe) * n);
                stdDev = System.Math.Sqrt(A + B - C) / ((1.0 - Pe) * System.Math.Sqrt(n));
            }
            else
            {
                double sum = 0;
                for (var i = 0; i < rowMarginal.Length; i++)
                {
                    sum += colMarginal[i] * rowMarginal[i] * (colMarginal[i] + rowMarginal[i]);
                }

                variance = 1.0 / (square(1.0 - Pe) * n) * (Pe + Pe * Pe - sum);
                stdDev = 1.0 / ((1.0 - Pe) * System.Math.Sqrt(n)) * System.Math.Sqrt(Pe + Pe * Pe - sum);
            }

            Debug.Assert(!(System.Math.Abs(variance - stdDev * stdDev) > 1e-10 * variance));

            return variance;
        }

        /// <summary>
        ///     Computes the asymptotic variance for Fleiss's Kappa variance using the formulae
        ///     by (Fleiss et al, 1969). If <paramref name="nullHypothesis" /> is set to true, the
        ///     method will return the variance under the null hypothesis.
        /// </summary>
        /// <param name="matrix">A <see cref="GeneralConfusionMatrix" /> representing the ratings.</param>
        /// <param name="stdDev">Kappa's standard deviation.</param>
        /// <param name="nullHypothesis">
        ///     True to compute Kappa's variance when the null hypothesis
        ///     is true (i.e. that the underlying kappa is zer). False otherwise. Default is false.
        /// </param>
        /// <returns>Kappa's variance.</returns>
        public static double AsymptoticKappaVariance(WeightedConfusionMatrix matrix, out double stdDev,
            bool nullHypothesis = false)
        {
            double n = matrix.NumberOfSamples;

            var p = matrix.ProportionMatrix;
            var w = matrix.Weights;

            var pj = matrix.ColumnProportions;
            var pi = matrix.RowProportions;
            var wi = matrix.WeightedColumnProportions;
            var wj = matrix.WeightedRowProportions;

            var Po = matrix.WeightedOverallAgreement;
            var Pc = matrix.WeightedChanceAgreement;

            double variance;


            if (!nullHypothesis)
            {
                // References: Statistical Methods for Rates and Proportions, pg 610.
                double a = 0;
                for (var i = 0; i < pi.Length; i++)
                {
                    for (var j = 0; j < pj.Length; j++)
                    {
                        var t = w[i, j] * (1.0 - Pc) - (wi[i] + wj[j]) * (1.0 - Po);
                        a += p[i, j] * (t * t);
                    }
                }

                var b = (Po * Pc - 2 * Pc + Po) * (Po * Pc - 2 * Pc + Po);
                var c = (1.0 - Pc) * (1.0 - Pc);

                stdDev = System.Math.Sqrt(a - b) / (c * System.Math.Sqrt(n));
                variance = (a - b) / (c * c * n);
            }
            else
            {
                double a = 0;
                for (var i = 0; i < pi.Length; i++)
                {
                    for (var j = 0; j < pj.Length; j++)
                    {
                        var t = w[i, j] - (wj[i] + wi[j]);
                        a += pj[i] * pi[j] * t * t;
                    }
                }

                var b = Pc * Pc;
                var c = 1.0 - Pc;

                stdDev = System.Math.Sqrt(a - b) / (c * System.Math.Sqrt(n));
                variance = (a - b) / (c * c * n);
            }


            return variance;
        }

        private static double square(double x)
        {
            return x * x;
        }
    }
}