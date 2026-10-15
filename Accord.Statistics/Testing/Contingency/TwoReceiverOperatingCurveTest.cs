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
    ///     Hypothesis test for two Receiver-Operating
    ///     Characteristic (ROC) curve areas (ROC-AUC).
    /// </summary>
    /// <seealso cref="TwoSampleZTest" />
    /// <seealso cref="ZTest" />
    [Serializable]
    public class TwoReceiverOperatingCurveTest : TwoSampleZTest
    {
        /// <summary>
        ///     Creates a new test for two ROC curves.
        /// </summary>
        /// <param name="curve1">The first ROC curve.</param>
        /// <param name="curve2">The second ROC curve.</param>
        /// <param name="hypothesizedDifference">The hypothesized difference between the two areas.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public TwoReceiverOperatingCurveTest(ReceiverOperatingCharacteristic curve1,
            ReceiverOperatingCharacteristic curve2,
            double hypothesizedDifference = 0, TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent)
        {
            Curve1 = curve1;
            Curve2 = curve2;

            var Vx1 = curve1.NegativeAccuracies;
            var Vy1 = curve1.PositiveAccuracies;

            var Vx2 = curve2.NegativeAccuracies;
            var Vy2 = curve2.PositiveAccuracies;

            var covx = Vx1.Covariance(Vx2);
            var covy = Vy1.Covariance(Vy2);
            var cov = covx / Vx1.Length + covy / Vy1.Length;

            EstimatedValue1 = curve1.Area;
            EstimatedValue2 = curve2.Area;
            ObservedDifference = EstimatedValue1 - EstimatedValue2;
            HypothesizedDifference = hypothesizedDifference;

            Variance1 = curve1.Variance;
            Variance2 = curve2.Variance;

            OverallVariance = Variance1 + Variance2 - 2 * cov;
            StandardError = System.Math.Sqrt(OverallVariance);

            // Compute Z statistic
            var z = (ObservedDifference - HypothesizedDifference) / StandardError;

            Compute(z, alternate);
        }

        /// <summary>
        ///     First Receiver-Operating Characteristic curve.
        /// </summary>
        public ReceiverOperatingCharacteristic Curve1 { get; private set; }

        /// <summary>
        ///     First Receiver-Operating Characteristic curve.
        /// </summary>
        public ReceiverOperatingCharacteristic Curve2 { get; private set; }

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