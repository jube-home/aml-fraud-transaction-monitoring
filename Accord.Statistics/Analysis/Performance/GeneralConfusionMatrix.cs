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
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Analysis
{
    /*
    // TODO: Implement arbitrary orientation for confusion matrices
    public enum ConfusionMatrixOrientation
    {
        TitleAboveColumnsShouldBeCalledGroundTruth,
        TitleAboveColumnsShouldBeCalledPrediction,

        TitleOnTheLeftOfRowsShouldBeCalledGroundTruth = TitleAboveColumnsShouldBeCalledPrediction,
        TitleOnTheLeftOfRowsShouldBeCalledPrediction = TitleAboveColumnsShouldBeCalledGroundTruth
    }
    */

    /// <summary>
    ///     General confusion matrix for multi-class decision problems. For
    ///     binary problems, please see <see cref="ConfusionMatrix" />.
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
    /// <example>
    ///     <para>
    ///         The first example shows how a confusion matrix can be constructed from a vector of expected (ground-truth)
    ///         values and their associated predictions (as done by a test, procedure or machine learning classifier):
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\GeneralConfusionMatrixTest.cs" region="doc_ctor_values" />
    ///     <para>
    ///         The second example shows how to construct a general confusion matrix directly from a integer matrix with
    ///         the class assignments:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\GeneralConfusionMatrixTest.cs" region="doc_ctor_matrix" />
    ///     <para>
    ///         The third example shows how to construct a general confusion matrix directly from a classifier and a dataset:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_iris_confusion_matrix" />
    ///     <para>
    ///         The next examples reproduce the results shown in various papers, with special attention to the multiple
    ///         ways of computing the <see cref="Variance" /> for the <see cref="Kappa" /> statistic:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\GeneralConfusionMatrixTest.cs" region="doc_congalton" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\GeneralConfusionMatrixTest.cs" region="doc_ientilucci" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\GeneralConfusionMatrixTest.cs" region="doc_fleiss" />
    /// </example>
    /// <see cref="ConfusionMatrix" />
    [Serializable]
    public class GeneralConfusionMatrix
    {
        private double? chiSquare;
        private int[] colErrors;
        private double[] colProportion;
        private int[] colSum;
        private double? diagMax;
        private double? diagMin;

        private int[] diagonal;

        // Association measures
        private double? kappa;
        private double? kappaStdError;
        private double? kappaStdError0;
        private double? kappaStdErrorDeltaMethod;

        private double? kappaVariance;

        private double? kappaVariance0;

        private double? kappaVarianceDeltaMethod;

        private ConfusionMatrix[] matrices;

        private double[] precision;

        private double[,] proportions;
        private double[] recall;

        private int[] rowErrors;

        private double[] rowProportion;

        private int[] rowSum;

        private double? tau;

        /// <summary>
        ///     Creates a new Confusion Matrix.
        /// </summary>
        public GeneralConfusionMatrix(double[,] matrix, int samples)
        {
            this.Matrix = matrix.Multiply(samples).ToInt32();
            NumberOfClasses = matrix.Rows();
            this.NumberOfSamples = samples;
            proportions = matrix;
        }

        /// <summary>
        ///     Creates a new Confusion Matrix.
        /// </summary>
        public GeneralConfusionMatrix(int[,] matrix)
        {
            this.Matrix = matrix;
            NumberOfClasses = matrix.Rows();
            NumberOfSamples = matrix.Sum();
        }

        /// <summary>
        ///     Creates a new Confusion Matrix.
        /// </summary>
        public GeneralConfusionMatrix(int[] expected, int[] predicted)
        {
            var classes = System.Math.Max(expected.Max(), predicted.Max()) + 1;
            compute(classes, expected, predicted);
        }

        /// <summary>
        ///     Creates a new Confusion Matrix.
        /// </summary>
        public GeneralConfusionMatrix(int classes, int[] expected, int[] predicted)
        {
            compute(classes, expected, predicted);
        }

        /// <summary>
        ///     Gets or sets the title that ought be displayed on top of the columns of
        ///     this <see cref="GeneralConfusionMatrix" />. Default is "Expected (Ground-truth)".
        /// </summary>
        public string TitleAboveColumns { get; set; }

        /// <summary>
        ///     Gets or sets the title that ought be displayed on left side of this
        ///     <see cref="GeneralConfusionMatrix" />. Default is "Actual (Prediction)".
        /// </summary>
        public string TitleOnTheLeftOfRows { get; set; }

        /// <summary>
        ///     Gets the confusion matrix, in which each element e_ij
        ///     represents the number of elements from class i classified
        ///     as belonging to class j.
        /// </summary>
        public int[,] Matrix { get; private set; }

        /// <summary>
        ///     Gets the number of samples.
        /// </summary>
        [DisplayName("Number of samples")]
        public int NumberOfSamples { get; private set; }

        /// <summary>
        ///     Gets the number of classes.
        /// </summary>
        [DisplayName("Number of classes")]
        public int NumberOfClasses { get; private set; }

        /// <summary>
        ///     Obsolete. Please use <see cref="NumberOfSamples" /> instead.
        /// </summary>
        [DisplayName("Number of samples")]
        [Obsolete("Please use NumberOfSamples instead.")]
        public int Samples => NumberOfSamples;

        /// <summary>
        ///     Obsolete. Please use <see cref="NumberOfClasses" /> instead.
        /// </summary>
        [DisplayName("Number of classes")]
        [Obsolete("Please use NumberOfClasses instead.")]
        public int Classes => NumberOfClasses;

        /// <summary>
        ///     Gets the row totals.
        /// </summary>
        [DisplayName("Row Totals")]
        public int[] RowTotals
        {
            get
            {
                if (rowSum == null)
                {
                    rowSum = Matrix.Sum(1);
                }

                return rowSum;
            }
        }

        /// <summary>
        ///     Gets the column totals.
        /// </summary>
        [DisplayName("Column Totals")]
        public int[] ColumnTotals
        {
            get
            {
                if (colSum == null)
                {
                    colSum = Matrix.Sum(0);
                }

                return colSum;
            }
        }

        /// <summary>
        ///     Gets the row errors.
        /// </summary>
        [DisplayName("Row Errors")]
        public int[] RowErrors
        {
            get
            {
                if (rowErrors == null)
                {
                    rowErrors = RowTotals.Subtract(Diagonal);
                }

                return rowErrors;
            }
        }

        /// <summary>
        ///     Gets the col errors.
        /// </summary>
        [DisplayName("Column Errors")]
        public int[] ColumnErrors
        {
            get
            {
                if (colErrors == null)
                {
                    colErrors = ColumnTotals.Subtract(Diagonal);
                }

                return colErrors;
            }
        }

        /// <summary>
        ///     Gets the row marginals (proportions).
        /// </summary>
        [DisplayName("Row Proportions")]
        public double[] RowProportions
        {
            get
            {
                if (rowProportion == null)
                {
                    rowProportion = ProportionMatrix.Sum(1);
                }

                return rowProportion;
            }
        }

        /// <summary>
        ///     Gets the column marginals (proportions).
        /// </summary>
        [DisplayName("Column Proportions")]
        public double[] ColumnProportions
        {
            get
            {
                if (colProportion == null)
                {
                    colProportion = ProportionMatrix.Sum(0);
                }

                return colProportion;
            }
        }

        /// <summary>
        ///     Gets the row precision.
        /// </summary>
        [DisplayName("Precision")]
        public double[] Precision
        {
            get
            {
                if (precision == null)
                {
                    var diagonal = Diagonal;
                    var totals = ColumnTotals;
                    precision = new double[NumberOfClasses];
                    for (var i = 0; i < precision.Length; i++)
                    {
                        precision[i] = diagonal[i] == 0 ? 0 : diagonal[i] / (double)totals[i];
                    }
                }

                return precision;
            }
        }

        /// <summary>
        ///     Gets the column recall.
        /// </summary>
        [DisplayName("Recall")]
        public double[] Recall
        {
            get
            {
                if (recall == null)
                {
                    var diagonal = Diagonal;
                    var totals = RowTotals;
                    recall = new double[NumberOfClasses];
                    for (var i = 0; i < recall.Length; i++)
                    {
                        recall[i] = diagonal[i] == 0 ? 0 : diagonal[i] / (double)totals[i];
                    }
                }

                return recall;
            }
        }

        /// <summary>
        ///     Gets the diagonal of the confusion matrix.
        /// </summary>
        public int[] Diagonal
        {
            get
            {
                if (diagonal == null)
                {
                    diagonal = Matrix.Diagonal();
                }

                return diagonal;
            }
        }

        /// <summary>
        ///     Gets the maximum number of correct
        ///     matches (the maximum over the diagonal)
        /// </summary>
        [DisplayName("Maximum Hits")]
        public double Max
        {
            get
            {
                if (diagMax == null)
                {
                    diagMax = Diagonal.Max();
                }

                return diagMax.Value;
            }
        }

        /// <summary>
        ///     Gets the minimum number of correct
        ///     matches (the minimum over the diagonal)
        /// </summary>
        [DisplayName("Minimum Hits")]
        public double Min
        {
            get
            {
                if (diagMin == null)
                {
                    diagMin = Diagonal.Min();
                }

                return diagMin.Value;
            }
        }

        /// <summary>
        ///     Gets the confusion matrix in
        ///     terms of cell percentages.
        /// </summary>
        [DisplayName("Proportion Matrix")]
        public double[,] ProportionMatrix
        {
            get
            {
                if (proportions == null)
                {
                    proportions = Matrix.ToDouble().Divide(NumberOfSamples);
                }

                return proportions;
            }
        }

        /// <summary>
        ///     Gets the Kappa coefficient of performance.
        /// </summary>
        [DisplayName("Kappa Coefficient (κ)")]
        public double Kappa
        {
            get
            {
                if (kappa == null)
                {
                    var Po = OverallAgreement;
                    var Pc = ChanceAgreement;

                    kappa = (Po - Pc) / (1.0 - Pc);
                }

                return kappa.Value;
            }
        }

        /// <summary>
        ///     Gets the standard error of the <see cref="Kappa" />
        ///     coefficient of performance.
        /// </summary>
        [DisplayName("Kappa (κ) Std. Error")]
        public double StandardError
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
        ///     Gets the variance of the <see cref="Kappa" />
        ///     coefficient of performance.
        /// </summary>
        [DisplayName("Kappa (κ) Variance")]
        public double Variance
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
        ///     Gets the variance of the <see cref="Kappa" />
        ///     coefficient of performance using Congalton's delta method.
        /// </summary>
        [DisplayName("Kappa (κ) Variance (delta method)")]
        public double VarianceDeltaMethod
        {
            get
            {
                if (kappaVarianceDeltaMethod == null)
                {
                    double se;
                    kappaVarianceDeltaMethod = KappaTest.DeltaMethodKappaVariance(this, out se);
                    kappaStdErrorDeltaMethod = se;
                }

                return kappaVarianceDeltaMethod.Value;
            }
        }

        /// <summary>
        ///     Gets the standard error of the <see cref="Kappa" />
        ///     coefficient of performance using Congalton's delta method.
        /// </summary>
        [DisplayName("Kappa (κ) Std. Error (delta method)")]
        public double StandardErrorDeltaMethod
        {
            get
            {
                if (kappaStdErrorDeltaMethod == null)
                {
                    double se;
                    kappaStdErrorDeltaMethod = KappaTest.DeltaMethodKappaVariance(this, out se);
                    kappaStdErrorDeltaMethod = se;
                }

                return kappaStdErrorDeltaMethod.Value;
            }
        }

        /// <summary>
        ///     Gets the variance of the <see cref="Kappa" />
        ///     under the null hypothesis that the underlying
        ///     Kappa value is 0.
        /// </summary>
        [DisplayName("Kappa (κ) H₀ Variance")]
        public double VarianceUnderNull
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
        ///     Gets the standard error of the <see cref="Kappa" />
        ///     under the null hypothesis that the underlying Kappa
        ///     value is 0.
        /// </summary>
        [DisplayName("Kappa (κ) H₀ Std. Error")]
        public double StandardErrorUnderNull
        {
            get
            {
                if (kappaStdError0 == null)
                {
                    double se;
                    kappaVariance0 = KappaTest.AsymptoticKappaVariance(this, out se, true);
                    kappaStdError0 = se;
                }

                return kappaStdError0.Value;
            }
        }

        /// <summary>
        ///     Gets the Tau coefficient of performance.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Tau-b statistic, unlike tau-a, makes adjustments for ties and
        ///         is suitable for square tables. Values of tau-b range from −1
        ///         (100% negative association, or perfect inversion) to +1 (100%
        ///         positive association, or perfect agreement). A value of zero
        ///         indicates the absence of association.
        ///     </para>
        ///     <para>
        ///         References:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     http://en.wikipedia.org/wiki/Kendall_tau_rank_correlation_coefficient
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     LEVADA, Alexandre Luis Magalhães. Combinação de modelos de campos aleatórios markovianos
        ///                     para classificação contextual de imagens multiespectrais [online]. São Carlos : Instituto
        ///                     de Física de São Carlos, Universidade de São Paulo, 2010. Tese de Doutorado em Física Aplicada.
        ///                     Disponível em: http://www.teses.usp.br/teses/disponiveis/76/76132/tde-11052010-165642/.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     MA, Z.; REDMOND, R. L. Tau coefficients for accuracy assessment of
        ///                     classification of remote sensing data.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        [DisplayName("Tau Coefficient (τ)")]
        public double Tau
        {
            get
            {
                if (tau == null)
                {
                    var diagonal = Diagonal;
                    var rowTotals = RowTotals;
                    var directionSum = 0;
                    for (var i = 0; i < rowTotals.Length; i++)
                    {
                        directionSum += diagonal[i] * rowTotals[i];
                    }

                    var Po = OverallAgreement;
                    var Pr = directionSum / (double)(NumberOfSamples * NumberOfSamples);

                    tau = (Po - Pr) / (1.0 - Pr);
                }

                return tau.Value;
            }
        }

        /// <summary>
        ///     Phi coefficient.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The Pearson correlation coefficient (phi) ranges from −1 to +1, where
        ///         a value of +1 indicates perfect agreement, a value of -1 indicates perfect
        ///         disagreement and a value 0 indicates no agreement or relationship.
        ///     </para>
        ///     <para>
        ///         References:
        ///         http://en.wikipedia.org/wiki/Phi_coefficient,
        ///         http://www.psychstat.missouristate.edu/introbook/sbk28m.htm
        ///     </para>
        /// </remarks>
        [DisplayName("Pearson Correlation (φ)")]
        public double Phi => System.Math.Sqrt(ChiSquare / NumberOfSamples);

        /// <summary>
        ///     Gets the Chi-Square statistic for the contingency table.
        /// </summary>
        [DisplayName("Chi-Square (χ²)")]
        public double ChiSquare
        {
            get
            {
                if (chiSquare == null)
                {
                    var rowTotals = RowTotals;
                    var colTotals = ColumnTotals;

                    double x = 0;
                    for (var i = 0; i < NumberOfClasses; i++)
                    {
                        for (var j = 0; j < NumberOfClasses; j++)
                        {
                            var e = rowTotals[i] * colTotals[j] / (double)NumberOfSamples;
                            double o = Matrix[i, j];

                            var num = (o - e) * (o - e);
                            if (num != 0)
                            {
                                x += num / e;
                            }
                        }
                    }

                    chiSquare = x;
                }

                return chiSquare.Value;
            }
        }

        /// <summary>
        ///     Tschuprow's T association measure.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Tschuprow's T is a measure of association between two nominal variables, giving
        ///         a value between 0 and 1 (inclusive). It is closely related to
        ///         <see cref="Cramer">
        ///             Cramér's V
        ///         </see>
        ///         , coinciding with it for square contingency tables.
        ///     </para>
        ///     <para>
        ///         References:
        ///         http://en.wikipedia.org/wiki/Tschuprow's_T
        ///     </para>
        /// </remarks>
        [DisplayName("Tschuprow's T")]
        public double Tschuprow => System.Math.Sqrt(ChiSquare / (NumberOfSamples * (NumberOfClasses - 1)));

        /// <summary>
        ///     Pearson's contingency coefficient C.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Pearson's C measures the degree of association between the two variables. However,
        ///         C suffers from the disadvantage that it does not reach a maximum of 1 or the minimum
        ///         of -1; the highest it can reach in a 2 x 2 table is .707; the maximum it can reach in
        ///         a 4 × 4 table is 0.870. It can reach values closer to 1 in contingency tables with more
        ///         categories. It should, therefore, not be used to compare associations among tables with
        ///         different numbers of categories. For a improved version of C, see <see cref="Sakoda" />.
        ///     </para>
        ///     <para>
        ///         References:
        ///         http://en.wikipedia.org/wiki/Contingency_table
        ///     </para>
        /// </remarks>
        [DisplayName("Pearson's C")]
        public double Pearson => System.Math.Sqrt(ChiSquare / (ChiSquare + NumberOfSamples));

        /// <summary>
        ///     Sakoda's contingency coefficient V.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Sakoda's V is an adjusted version of <see cref="Pearson">Pearson's C</see>
        ///         so it reaches a maximum of 1 when there is complete association in a table
        ///         of any number of rows and columns.
        ///     </para>
        ///     <para>
        ///         References:
        ///         http://en.wikipedia.org/wiki/Contingency_table
        ///     </para>
        /// </remarks>
        [DisplayName("Sakoda's V")]
        public double Sakoda => Pearson / System.Math.Sqrt((NumberOfClasses - 1) / (double)NumberOfClasses);

        /// <summary>
        ///     Cramer's V association measure.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Cramér's V varies from 0 (corresponding to no association between the variables)
        ///         to 1 (complete association) and can reach 1 only when the two variables are equal
        ///         to each other. In practice, a value of 0.1 already provides a good indication that
        ///         there is substantive relationship between the two variables.
        ///     </para>
        ///     <para>
        ///         References:
        ///         http://en.wikipedia.org/wiki/Cram%C3%A9r%27s_V,
        ///         http://www.acastat.com/Statbook/chisqassoc.htm
        ///     </para>
        /// </remarks>
        [DisplayName("Cramér's V")]
        public double Cramer => System.Math.Sqrt(ChiSquare / (NumberOfSamples * (NumberOfClasses - 1)));

        /// <summary>
        ///     Overall agreement.
        /// </summary>
        /// <remarks>
        ///     The overall agreement is the sum of the diagonal elements
        ///     of the contingency table divided by the number of samples.
        /// </remarks>
        [DisplayName("Overall Agreement")]
        public double OverallAgreement => Matrix.Trace() / (double)NumberOfSamples;

        /// <summary>
        ///     Accuracy. This is the same value as <see cref="OverallAgreement" />.
        /// </summary>
        /// <value>The accuracy, or <see cref="OverallAgreement" />.</value>
        [DisplayName("Accuracy")]
        public double Accuracy => OverallAgreement;

        /// <summary>
        ///     Error. This is the same value as 1.0 - <see cref="OverallAgreement" />.
        /// </summary>
        /// <value>The average error, or 1.0 - <see cref="OverallAgreement" />.</value>
        [DisplayName("Error")]
        public double Error => 1.0 - OverallAgreement;

        /// <summary>
        ///     Geometric agreement.
        /// </summary>
        /// <remarks>
        ///     The geometric agreement is the geometric mean of the
        ///     diagonal elements of the confusion matrix.
        /// </remarks>
        [DisplayName("Geometric Agreement")]
        public double GeometricAgreement => System.Math.Exp(Diagonal.LogGeometricMean());

        /// <summary>
        ///     Chance agreement.
        /// </summary>
        /// <remarks>
        ///     The chance agreement tells how many samples
        ///     were correctly classified by chance alone.
        /// </remarks>
        [DisplayName("Chance Agreement")]
        public double ChanceAgreement
        {
            get
            {
                var rowTotals = RowTotals;
                var colTotals = ColumnTotals;

                double chance = 0;
                for (var i = 0; i < NumberOfClasses; i++)
                {
                    chance += rowTotals[i] * colTotals[i];
                }

                return chance / (NumberOfSamples * NumberOfSamples);
            }
        }

        /// <summary>
        ///     Expected values, or values that could
        ///     have been generated just by chance.
        /// </summary>
        [DisplayName("Expected values")]
        public double[,] ExpectedValues
        {
            get
            {
                var row = RowTotals;
                var col = ColumnTotals;

                var expected = new double[NumberOfClasses, NumberOfClasses];

                for (var i = 0; i < row.Length; i++)
                {
                    for (var j = 0; j < col.Length; j++)
                    {
                        expected[i, j] = row[i] * col[j] / (double)NumberOfSamples;
                    }
                }

                return expected;
            }
        }

        /// <summary>
        ///     Gets <see cref="ConfusionMatrix">binary confusion matrices</see> for each class in the multi-class
        ///     classification problem. You can use this property to obtain <see cref="ConfusionMatrix.Recall">recall</see>,
        ///     <see cref="ConfusionMatrix.Precision">precision</see> and other metrics for each of the classes.
        /// </summary>
        public ConfusionMatrix[] PerClassMatrices
        {
            get
            {
                if (matrices == null)
                {
                    var diagonal = Diagonal;
                    var colSum = ColumnTotals;
                    var rowSum = RowTotals;

                    matrices = new ConfusionMatrix[NumberOfClasses];
                    for (var i = 0; i < NumberOfClasses; i++)
                    {
                        var tp = diagonal[i];
                        var fp = rowSum[i] - diagonal[i];
                        var fn = colSum[i] - diagonal[i];
                        var tn = Matrix.Sum() - tp - fp - fn;

                        matrices[i] = new ConfusionMatrix(
                            tp, trueNegatives: tn,
                            falsePositives: fp, falseNegatives: fn);
                    }
                }

                return matrices;
            }
        }

        private void compute(int classes, int[] expected, int[] predicted)
        {
            if (expected.Length != predicted.Length)
            {
                throw new DimensionMismatchException("predicted",
                    "The number of expected and predicted observations must match.");
            }

            if (classes == 2)
            {
                expected = expected.ToZeroOne();
                predicted = predicted.ToZeroOne();
            }

            for (var i = 0; i < expected.Length; i++)
            {
                if (expected[i] < 0)
                {
                    throw new ArgumentOutOfRangeException("expected",
                        "Negative class labels are not supported for the moment. If you need" +
                        " this functionality in your application, please open an issue report in the project issue tracker.");
                }

                if (predicted[i] < 0)
                {
                    throw new ArgumentOutOfRangeException("predicted",
                        "Negative class labels are not supported for the moment. If you need" +
                        " this functionality in your application, please open an issue report in the project issue tracker.");
                }
            }

            TitleAboveColumns = "Expected (Ground-Truth)";
            TitleOnTheLeftOfRows = "Actual (Prediction)";

            NumberOfSamples = expected.Length;
            this.NumberOfClasses = classes;
            Matrix = new int[classes, classes];

            // Each element ij represents the number of elements
            // from class i classified as belonging to class j.

            // For each classification,
            for (var k = 0; k < expected.Length; k++)
            {
                // Make sure the expected and predicted
                // values are from valid classes.

                var p = predicted[k]; // cols contain expected values
                var e = expected[k]; // rows contain predicted values

                if (p < 0 || p >= classes)
                {
                    throw new ArgumentOutOfRangeException("predicted");
                }

                if (e < 0 || e >= classes)
                {
                    throw new ArgumentOutOfRangeException("expected");
                }

                Matrix[p, e]++;
            }
        }

        /// <summary>
        ///     Combines several confusion matrices into one single matrix.
        /// </summary>
        /// <param name="matrices">The matrices to combine.</param>
        public static GeneralConfusionMatrix Combine(params GeneralConfusionMatrix[] matrices)
        {
            if (matrices == null)
            {
                throw new ArgumentNullException("matrices");
            }

            if (matrices.Length == 0)
            {
                throw new ArgumentException("At least one confusion matrix is required.", "matrices");
            }

            var classes = matrices[0].NumberOfClasses;
            var total = new int[classes, classes];

            foreach (var matrix in matrices)
            {
                if (matrix.NumberOfClasses != classes)
                {
                    throw new ArgumentException("The number of classes in one of the matrices differs.");
                }

                for (var j = 0; j < classes; j++)
                {
                    for (var k = 0; k < classes; k++)
                    {
                        total[j, k] += matrix.Matrix[j, k];
                    }
                }
            }

            return new GeneralConfusionMatrix(total);
        }

        /// <summary>
        ///     Estimates a <see cref="GeneralConfusionMatrix" /> directly from a classifier, a set of inputs and its expected
        ///     outputs.
        /// </summary>
        /// <typeparam name="TInput">The type of the inputs accepted by the classifier.</typeparam>
        /// <param name="classifier">The classifier.</param>
        /// <param name="inputs">The input vectors.</param>
        /// <param name="expected">The expected outputs associated with each input vector.</param>
        /// <returns>
        ///     A <see cref="GeneralConfusionMatrix" /> capturing the performance of the classifier when
        ///     trying to predict the outputs <paramref name="expected" /> from the <paramref name="inputs" />.
        /// </returns>
        public static GeneralConfusionMatrix Estimate<TInput>(IClassifier<TInput, int> classifier, TInput[] inputs,
            int[] expected)
        {
            return new GeneralConfusionMatrix(expected, classifier.Decide(inputs));
        }

        /// <summary>
        ///     Estimates a <see cref="GeneralConfusionMatrix" /> directly from a classifier, a set of inputs and its expected
        ///     outputs.
        /// </summary>
        /// <typeparam name="TInput">The type of the inputs accepted by the classifier.</typeparam>
        /// <param name="classifier">The classifier.</param>
        /// <param name="inputs">The input vectors.</param>
        /// <param name="expected">The expected outputs associated with each input vector.</param>
        /// <returns>
        ///     A <see cref="GeneralConfusionMatrix" /> capturing the performance of the classifier when
        ///     trying to predict the outputs <paramref name="expected" /> from the <paramref name="inputs" />.
        /// </returns>
        public static GeneralConfusionMatrix Estimate<TInput>(IClassifier<TInput, bool> classifier, TInput[] inputs,
            bool[] expected)
        {
            return new GeneralConfusionMatrix(expected.ToZeroOne(),
                classifier.Decide(inputs).ToZeroOne());
        }
    }
}