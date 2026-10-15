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
using Accord.Statistics.Analysis;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Kappa Test for two contingency tables.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The two-matrix Kappa test tries to assert whether the Kappa measure
    ///         of two contingency tables, each of which created by a different rater
    ///         or classification model, differs significantly.
    ///     </para>
    ///     <para>
    ///         This is a <see cref="TwoSampleZTest">two sample z-test kind of test</see>.
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
    ///                     Ientilucci, Emmett (2006). "On Using and Computing the Kappa Statistic".
    ///                     Available on: http://www.cis.rit.edu/~ejipci/Reports/On_Using_and_Computing_the_Kappa_Statistic.pdf
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="TwoSampleZTest" />
    /// <seealso cref="ZTest" />
    [Serializable]
    public class TwoMatrixKappaTest : TwoSampleZTest
    {
        /// <summary>
        ///     Creates a new Two-Table Kappa test.
        /// </summary>
        /// <param name="kappa1">The kappa value for the first contingency table to test.</param>
        /// <param name="kappa2">The kappa value for the second contingency table to test.</param>
        /// <param name="var1">The variance of the kappa value for the first contingency table to test.</param>
        /// <param name="var2">The variance of the kappa value for the second contingency table to test.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        /// <param name="hypothesizedDifference">The hypothesized difference between the two Kappa values.</param>
        public TwoMatrixKappaTest(double kappa1, double var1, double kappa2, double var2,
            double hypothesizedDifference = 0,
            TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent)
        {
            EstimatedValue1 = kappa1;
            EstimatedValue2 = kappa2;

            Variance1 = var1;
            Variance2 = var2;

            OverallVariance = Variance1 + Variance2;

            var diff = System.Math.Abs(EstimatedValue1 - EstimatedValue2);
            var stdError = System.Math.Sqrt(OverallVariance);

            Compute(diff, hypothesizedDifference, stdError, alternate);
        }

        /// <summary>
        ///     Creates a new Two-Table Kappa test.
        /// </summary>
        /// <param name="matrix1">The first contingency table to test.</param>
        /// <param name="matrix2">The second contingency table to test.</param>
        /// <param name="hypothesizedDifference">The hypothesized difference between the two Kappa values.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public TwoMatrixKappaTest(GeneralConfusionMatrix matrix1, GeneralConfusionMatrix matrix2,
            double hypothesizedDifference = 0,
            TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent)
        {
            EstimatedValue1 = matrix1.Kappa;
            EstimatedValue2 = matrix2.Kappa;

            Variance1 = matrix1.Variance;
            Variance2 = matrix2.Variance;

            OverallVariance = Variance1 + Variance2;

            var diff = System.Math.Abs(EstimatedValue1 - EstimatedValue2);
            var stdError = System.Math.Sqrt(OverallVariance);

            Compute(diff, hypothesizedDifference, stdError, alternate);
        }

        /// <summary>
        ///     Gets the summed Kappa variance
        ///     for the two contingency tables.
        /// </summary>
        public double OverallVariance { get; private set; }

        /// <summary>
        ///     Gets the variance for the first Kappa value.
        /// </summary>
        public double Variance1 { get; private set; }

        /// <summary>
        ///     Gets the variance for the second Kappa value.
        /// </summary>
        public double Variance2 { get; private set; }
    }
}