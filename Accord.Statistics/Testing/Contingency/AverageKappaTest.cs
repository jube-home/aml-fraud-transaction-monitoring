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
    ///     Kappa Test for multiple contingency tables.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The multiple-matrix Kappa test tries to assert whether the Kappa measure
    ///         of many contingency tables, each of which created by a different rater
    ///         or classification model, differs significantly. The computations are
    ///         based on the pages 607, 608 of  (Fleiss, 2003).
    ///     </para>
    ///     <para>
    ///         This is a <see cref="ChiSquareTest">Chi-square kind of test</see>.
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
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="ChiSquareTest" />
    [Serializable]
    public class AverageKappaTest : ChiSquareTest
    {
        /// <summary>
        ///     Creates a new multiple table Kappa test.
        /// </summary>
        /// <param name="kappas">The kappa values.</param>
        /// <param name="variances">The variance for each kappa value.</param>
        public AverageKappaTest(double[] kappas, double[] variances)
        {
            if (kappas.Length != variances.Length)
            {
                throw new DimensionMismatchException("variances",
                    "The number of variance estimates and kappa estimates must match.");
            }

            Compute(kappas, variances);
        }

        /// <summary>
        ///     Creates a new multiple table Kappa test.
        /// </summary>
        /// <param name="matrices">The contingency tables.</param>
        public AverageKappaTest(GeneralConfusionMatrix[] matrices)
        {
            var kappas = new double[matrices.Length];
            var variances = new double[matrices.Length];

            for (var i = 0; i < matrices.Length; i++)
            {
                kappas[i] = matrices[i].Kappa;
                variances[i] = matrices[i].Variance;
            }

            Compute(kappas, variances);
        }

        /// <summary>
        ///     Gets the overall Kappa value
        ///     for the many contingency tables.
        /// </summary>
        public double OverallKappa { get; private set; }

        /// <summary>
        ///     Gets the overall Kappa variance
        ///     for the many contingency tables.
        /// </summary>
        public double OverallVariance { get; private set; }

        /// <summary>
        ///     Gets the variance for each kappa value.
        /// </summary>
        public double[] Variances { get; private set; }

        /// <summary>
        ///     Gets the kappa for each contingency table.
        /// </summary>
        public double[] EstimatedValues { get; private set; }

        /// <summary>
        ///     Computes the multiple matrix Kappa test.
        /// </summary>
        protected void Compute(double[] kappas, double[] variances)
        {
            EstimatedValues = kappas;
            Variances = variances;

            double num = 0;
            for (var i = 0; i < kappas.Length; i++)
            {
                num += kappas[i] / variances[i];
            }

            double den = 0;
            for (var i = 0; i < variances.Length; i++)
            {
                den += 1.0 / variances[i];
            }

            OverallKappa = num / den;
            OverallVariance = den;

            double chiSquare = 0;
            for (var i = 0; i < kappas.Length; i++)
            {
                chiSquare += System.Math.Pow(kappas[i] - OverallKappa, 2) / variances[i];
            }

            var df = kappas.Length - 1;

            Compute(chiSquare, df);
        }
    }
}