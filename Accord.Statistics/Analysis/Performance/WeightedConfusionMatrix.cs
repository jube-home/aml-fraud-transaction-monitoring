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
using System.ComponentModel;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Weighted confusion matrix for multi-class decision problems.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://uwf.edu/zhu/evr6930/2.pdf">
    ///                         R.  G.  Congalton. A Review  of Assessing  the Accuracy  of Classifications
    ///                         of Remotely  Sensed  Data. Available on: http://uwf.edu/zhu/evr6930/2.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.iiasa.ac.at/Admin/PUB/Documents/IR-98-081.pdf">
    ///                         G. Banko. A Review of Assessing the Accuracy of Classiﬁcations of Remotely Sensed Data and
    ///                         of Methods Including Remote Sensing Data in Forest Inventory. Interim report. Available on:
    ///                         http://www.iiasa.ac.at/Admin/PUB/Documents/IR-98-081.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [Serializable]
    public class WeightedConfusionMatrix : GeneralConfusionMatrix
    {
        private double? kappa;
        private double? kappaStdError;

        private double? kappaStdError0;
        private double? kappaVariance;
        private double? kappaVariance0;
        private double[] weightedColProportion;

        private double[] weightedRowProportion;

        /// <summary>
        ///     Creates a new Confusion Matrix.
        /// </summary>
        public WeightedConfusionMatrix(double[,] matrix, double[,] weights, int samples)
            : base(matrix, samples)
        {
            this.Weights = weights;
        }

        /// <summary>
        ///     Creates a new Confusion Matrix.
        /// </summary>
        public WeightedConfusionMatrix(int[,] matrix, double[,] weights)
            : base(matrix)
        {
            this.Weights = weights;
        }

        /// <summary>
        ///     Creates a new Confusion Matrix.
        /// </summary>
        public WeightedConfusionMatrix(double[,] weights, int[] expected, int[] predicted)
            : base(weights.GetLength(0), expected, predicted)
        {
            this.Weights = weights;
        }

        /// <summary>
        ///     Gets the Weights matrix.
        /// </summary>
        public double[,] Weights { get; }

        /// <summary>
        ///     Gets the row marginals (proportions).
        /// </summary>
        [DisplayName("Weighted Row Proportions")]
        public double[] WeightedRowProportions
        {
            get
            {
                if (weightedRowProportion == null)
                {
                    var rowProportion = RowProportions;
                    weightedRowProportion = new double[NumberOfClasses];
                    for (var i = 0; i < rowProportion.Length; i++)
                    {
                        for (var j = 0; j < rowProportion.Length; j++)
                        {
                            weightedRowProportion[i] += rowProportion[j] * Weights[i, j];
                        }
                    }
                }

                return weightedRowProportion;
            }
        }

        /// <summary>
        ///     Gets the column marginals (proportions).
        /// </summary>
        [DisplayName("Weighted Column Proportions")]
        public double[] WeightedColumnProportions
        {
            get
            {
                if (weightedColProportion == null)
                {
                    var colProportion = ColumnProportions;
                    weightedColProportion = new double[NumberOfClasses];
                    for (var i = 0; i < colProportion.Length; i++)
                    {
                        for (var j = 0; j < colProportion.Length; j++)
                        {
                            weightedColProportion[i] += colProportion[j] * Weights[i, j];
                        }
                    }
                }

                return weightedColProportion;
            }
        }

        /// <summary>
        ///     Gets the Kappa coefficient of performance.
        /// </summary>
        [DisplayName("Kappa Coefficient (κ)")]
        public double WeightedKappa
        {
            get
            {
                if (kappa == null)
                {
                    var Po = WeightedOverallAgreement;
                    var Pc = WeightedChanceAgreement;

                    kappa = (Po - Pc) / (1.0 - Pc);
                }

                return kappa.Value;
            }
        }

        /// <summary>
        ///     Gets the standard error of the <see cref="WeightedKappa" />
        ///     coefficient of performance.
        /// </summary>
        [DisplayName("Kappa (κ) Std. Error")]
        public double WeightedStandardError
        {
            get
            {
                if (kappaStdError == null)
                {
                    double se;
                    kappaVariance = KappaTest.AsymptoticKappaVariance(this, out se);
                    kappaStdError = se;
                }

                return kappaStdError.Value;
            }
        }

        /// <summary>
        ///     Gets the variance of the <see cref="WeightedKappa" />
        ///     coefficient of performance.
        /// </summary>
        [DisplayName("Kappa (κ) Variance")]
        public double WeightedVariance
        {
            get
            {
                if (kappaVariance == null)
                {
                    double se;
                    kappaVariance = KappaTest.AsymptoticKappaVariance(this, out se);
                    kappaStdError = se;
                }

                return kappaVariance.Value;
            }
        }

        /// <summary>
        ///     Gets the variance of the <see cref="WeightedKappa" />
        ///     under the null hypothesis that the underlying
        ///     Kappa value is 0.
        /// </summary>
        [DisplayName("Kappa (κ) H₀ Variance")]
        public double WeightedVarianceUnderNull
        {
            get
            {
                if (kappaVariance0 == null)
                {
                    double se;
                    kappaVariance0 = KappaTest.AsymptoticKappaVariance(this,
                        out se, true);
                    kappaStdError0 = se;
                }

                return kappaVariance0.Value;
            }
        }

        /// <summary>
        ///     Gets the standard error of the <see cref="WeightedKappa" />
        ///     under the null hypothesis that the underlying Kappa
        ///     value is 0.
        /// </summary>
        [DisplayName("Kappa (κ) H₀ Std. Error")]
        public double WeightedStandardErrorUnderNull
        {
            get
            {
                if (kappaStdError0 == null)
                {
                    double se;
                    kappaVariance0 = KappaTest.AsymptoticKappaVariance(this,
                        out se, true);
                    kappaStdError0 = se;
                }

                return kappaStdError0.Value;
            }
        }

        /// <summary>
        ///     Overall agreement.
        /// </summary>
        [DisplayName("Overall Agreement")]
        public double WeightedOverallAgreement
        {
            get
            {
                double sum = 0;
                for (var i = 0; i < NumberOfClasses; i++)
                {
                    for (var j = 0; j < NumberOfClasses; j++)
                    {
                        sum += Weights[i, j] * Matrix[i, j];
                    }
                }

                return sum / NumberOfSamples;
            }
        }

        /// <summary>
        ///     Chance agreement.
        /// </summary>
        /// <remarks>
        ///     The chance agreement tells how many samples
        ///     were correctly classified by chance alone.
        /// </remarks>
        [DisplayName("Chance Agreement")]
        public double WeightedChanceAgreement
        {
            get
            {
                double chance = 0;
                for (var i = 0; i < NumberOfClasses; i++)
                {
                    for (var j = 0; j < NumberOfClasses; j++)
                    {
                        chance += Weights[i, j]
                                  * RowTotals[i]
                                  * ColumnTotals[j];
                    }
                }

                return chance / (NumberOfSamples * NumberOfSamples);
            }
        }

        /// <summary>
        ///     Creates a new Weighted Confusion Matrix with linear weighting.
        /// </summary>
        public static WeightedConfusionMatrix LinearWeighting(int[,] matrix)
        {
            // Create matrix of weights with linear weighting.
            var classes = matrix.GetLength(0);

            var weights = new double[classes, classes];
            for (var i = 0; i < classes; i++)
            {
                for (var j = 0; j < classes; j++)
                {
                    double num = System.Math.Abs(i - j);
                    double den = classes - 1;
                    weights[i, j] = 1.0 - num / den;
                }
            }

            return new WeightedConfusionMatrix(matrix, weights);
        }

        /// <summary>
        ///     Creates a new Weighted Confusion Matrix with linear weighting.
        /// </summary>
        public static WeightedConfusionMatrix QuadraticWeighting(int[,] matrix)
        {
            // Create matrix of weights with quadratic weighting.
            var classes = matrix.GetLength(0);

            var weights = new double[classes, classes];
            for (var i = 0; i < classes; i++)
            {
                for (var j = 0; j < classes; j++)
                {
                    double num = System.Math.Abs(i - j);
                    double den = classes - 1;
                    weights[i, j] = 1.0 - num / den * (num / den);
                }
            }

            return new WeightedConfusionMatrix(matrix, weights);
        }
    }
}