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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Properties;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Multiple Linear Regression Analysis
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Linear regression is an approach to model the relationship between
    ///         a single scalar dependent variable <c>y</c> and one or more explanatory
    ///         variables <c>x</c>. This class uses a <see cref="MultipleLinearRegression" />
    ///         to extract information about a given problem, such as confidence intervals,
    ///         hypothesis tests and performance measures.
    ///     </para>
    ///     <para>
    ///         This class can also be bound to standard controls such as the
    ///         <a href="http://msdn.microsoft.com/en-us/library/system.windows.forms.datagridview.aspx">DataGridView</a>
    ///         by setting their DataSource property to the analysis' <see cref="Coefficients" /> property.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia contributors. "Linear regression." Wikipedia, The Free Encyclopedia, 4 Nov. 2012.
    ///                     Available at: http://en.wikipedia.org/wiki/Linear_regression
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The first example shows how to learn a multiple linear regression analysis
    ///         from a dataset already given in matricial form (using jagged double[][] arrays).
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\MultipleLinearRegressionAnalysisTest.cs"
    ///         region="doc_learn_part2" />
    ///     <code>
    /// // Now we can show a summary of analysis
    /// // Accord.Controls.DataGridBox.Show(regression.Coefficients);
    /// </code>
    ///     <img src="..\images\linear-regression.png" />
    ///     <code>
    /// // We can also show a summary ANOVA
    /// DataGridBox.Show(regression.Table);
    /// </code>
    ///     <img src="..\images\linear-anova.png" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\MultipleLinearRegressionAnalysisTest.cs"
    ///         region="doc_learn_part2" />
    ///     <para>
    ///         The second example shows how to learn a multiple linear regression analysis using data
    ///         given in the form of a System.Data.DataTable. This data is also heterogeneous, mixing
    ///         both discrete (symbol) variables and continuous variables. This example is also available
    ///         for <see cref="LogisticRegressionAnalysis" />.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\MultipleLinearRegressionAnalysisTest.cs"
    ///         region="doc_learn_database" />
    /// </example>
    /// <seealso cref="LogisticRegressionAnalysis" />
    [Serializable]
    public class MultipleLinearRegressionAnalysis : TransformBase<double[], double>,
        IRegressionAnalysis, IAnova,
        ISupervisedLearning<MultipleLinearRegression, double[], double>
    {
        private int DFe; // Error degrees of freedom
        private int DFr; // Regression degrees of freedom
        private int DFt; // Total degrees of freedom

        private double MSe; // Error mean sum of squares
        private double MSr; // Regression sum of squares
        private double MSt; // Total sum of squares

        private double SSe; // Error sum of squares
        private double SSr; // Regression sum of squares
        private double SSt; // Total sum of squares

        internal double confidencePercent = 0.95;
        internal DoubleRange[] confidences;
        internal FTest[] ftests;

        [Obsolete] private double[][] inputData;

        private string[] inputNames;
        [Obsolete] private double[] outputData;

        // Result related measures
        private double outputMean;
        private string outputName;

        internal MultipleLinearRegression regression;
        [Obsolete] private double[] results;

        // Coefficient measures
        internal double[] standardErrors;
        [NonSerialized] private CancellationToken token;
        private TTest ttest;
        internal TTest[] ttests;

        /// <summary>
        ///     Constructs a Multiple Linear Regression Analysis.
        /// </summary>
        /// <param name="intercept">True to use an intercept term, false otherwise. Default is false.</param>
        public MultipleLinearRegressionAnalysis(bool intercept = false)
        {
            OrdinaryLeastSquares = new OrdinaryLeastSquares { UseIntercept = intercept };
        }

        /// <summary>
        ///     Constructs a Multiple Linear Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        /// <param name="intercept">True to use an intercept term, false otherwise. Default is false.</param>
        [Obsolete("Please pass the 'inputs' and 'outputs' parameters to the Learn method instead.")]
        public MultipleLinearRegressionAnalysis(double[][] inputs, double[] outputs, bool intercept = false)
            : this(intercept)
        {
            // Initial argument checking
            init(inputs, outputs);

            // Store data sets
            Source = inputs.ToMatrix();
            inputData = inputs;
            outputData = outputs;
        }

        /// <summary>
        ///     Constructs a Multiple Linear Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        /// <param name="intercept">True to use an intercept term, false otherwise. Default is false.</param>
        /// <param name="inputNames">The names of the input variables.</param>
        /// <param name="outputName">The name of the output variable.</param>
        [Obsolete("Please pass the 'inputs' and 'outputs' parameters to the Learn method instead.")]
        public MultipleLinearRegressionAnalysis(double[][] inputs, double[] outputs,
            string[] inputNames, string outputName, bool intercept = false)
            : this(inputs, outputs, intercept)
        {
            if (inputNames.Length != this.inputNames.Length)
            {
                throw new ArgumentException("The input names vector should have the same length"
                                            + " as the number of variables in the analysis. In this analysis, there are "
                                            + this.inputNames.Length + " variables expected.");
            }

            this.inputNames = inputNames;
            this.outputName = outputName;
        }

        /// <summary>
        ///     Source data used in the analysis.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[][] Array => inputData;

        /// <summary>
        ///     Gets the resulting values obtained
        ///     by the linear regression model.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[] Results => results;

        /// <summary>
        ///     Gets or sets the learning algorithm used to learn the <see cref="MultipleLinearRegression" />.
        /// </summary>
        public OrdinaryLeastSquares OrdinaryLeastSquares { get; set; }

        /// <summary>
        ///     Gets the standard deviation of the errors.
        /// </summary>
        public double StandardError { get; private set; }

        /// <summary>
        ///     Gets the information matrix obtained during learning.
        /// </summary>
        public double[][] InformationMatrix { get; private set; }

        /// <summary>
        ///     Gets the coefficient of determination, as known as R²
        /// </summary>
        public double RSquared { get; private set; }

        /// <summary>
        ///     Gets the number of samples used to compute the analysis.
        /// </summary>
        public int NumberOfSamples { get; private set; }

        /// <summary>
        ///     Gets the adjusted coefficient of determination, as known as R² adjusted
        /// </summary>
        public double RSquareAdjusted { get; private set; }

        /// <summary>
        ///     Gets a F-Test between the expected outputs and results.
        /// </summary>
        public FTest FTest { get; private set; }

        /// <summary>
        ///     Gets a Z-Test between the expected outputs and the results.
        /// </summary>
        public ZTest ZTest { get; private set; }

        /// <summary>
        ///     Gets a Chi-Square Test between the expected outputs and the results.
        /// </summary>
        public ChiSquareTest ChiSquareTest { get; private set; }

        /// <summary>
        ///     Gets the Standard Error for each coefficient
        ///     found during the logistic regression.
        /// </summary>
        public double[] StandardErrors => standardErrors;

        /// <summary>
        ///     Gets the Regression model created
        ///     and evaluated by this analysis.
        /// </summary>
        public MultipleLinearRegression Regression => regression;

        /// <summary>
        ///     Gets the value of each coefficient.
        /// </summary>
        public double[] CoefficientValues => regression.Weights.Concatenate(regression.Intercept);

        /// <summary>
        ///     Gets or sets the name of the input variables for the model.
        /// </summary>
        public string[] Inputs
        {
            get => inputNames;
            set => inputNames = value;
        }

        /// <summary>
        ///     Gets or sets the name of the output variable for the model.
        /// </summary>
        public string Output
        {
            get => outputName;
            set => outputName = value;
        }

        /// <summary>
        ///     Gets the Confidence Intervals (C.I.)
        ///     for each coefficient found in the regression.
        /// </summary>
        public DoubleRange[] Confidences => confidences;

        /// <summary>
        ///     Gets the collection of coefficients of the model.
        /// </summary>
        public LinearRegressionCoefficientCollection Coefficients { get; private set; }

        /// <summary>
        ///     Gets the ANOVA table for the analysis.
        /// </summary>
        public AnovaSourceCollection Table { get; private set; }

        /// <summary>
        ///     Source data used in the analysis.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Source { get; private set; }

        /// <summary>
        ///     Gets the dependent variable value
        ///     for each of the source input points.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[] Outputs => outputData;

        /// <summary>
        ///     Computes the Multiple Linear Regression Analysis.
        /// </summary>
        [Obsolete("Please use the Learn method instead.")]
        public void Compute()
        {
            compute(Source.ToJagged(), Outputs);
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        public CancellationToken Token
        {
            get => token;
            set => token = value;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public MultipleLinearRegression Learn(double[][] x, double[] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            init(x, y);
            compute(x, y);
            return regression;
        }

        private void init(double[][] inputs, double[] outputs)
        {
            if (inputs == null)
            {
                throw new ArgumentNullException("inputs");
            }

            if (outputs == null)
            {
                throw new ArgumentNullException("outputs");
            }

            if (inputs.Length != outputs.Length)
            {
                throw new ArgumentException(
                    "The number of rows in the input array must match the number of given outputs.");
            }

            NumberOfInputs = inputs[0].Length;
            NumberOfOutputs = 1;

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i].Length != NumberOfInputs)
                {
                    throw new ArgumentException("All input vectors must have the same length.");
                }
            }

            // Create the linear regression
            regression = new MultipleLinearRegression
            {
                NumberOfInputs = NumberOfInputs
            };

            // Create additional structures
            var coefficientCount = NumberOfInputs + 1;
            standardErrors = new double[coefficientCount];
            confidences = new DoubleRange[coefficientCount];
            ftests = new FTest[coefficientCount];
            ttests = new TTest[coefficientCount];

            if (outputName == null)
            {
                outputName = "Output";
            }

            if (inputNames == null)
            {
                inputNames = new string[NumberOfInputs];
                for (var i = 0; i < inputNames.Length; i++)
                {
                    inputNames[i] = "Input " + i;
                }
            }

            // Create object-oriented structure to represent the analysis
            var coefs = new LinearRegressionCoefficient[coefficientCount];
            for (var i = 0; i < coefs.Length; i++)
            {
                coefs[i] = new LinearRegressionCoefficient(this, i);
            }

            Coefficients = new LinearRegressionCoefficientCollection(this, coefs);
        }

        private void compute(double[][] x, double[] y)
        {
            var n = x.Length;
            var p = NumberOfInputs;

            SSt = 0;
            SSe = 0;
            outputMean = 0.0;
            NumberOfSamples = x.Length;

            // Compute the regression
            OrdinaryLeastSquares.Token = Token;
            regression = OrdinaryLeastSquares.Learn(x, y);
            InformationMatrix = OrdinaryLeastSquares.GetInformationMatrix();

            // Calculate mean of the expected outputs
            outputMean = y.Mean();

            // Calculate actual outputs (results)
#pragma warning disable 612, 618
            results = regression.Transform(x);

            // Calculate SSe and SSt
            for (var i = 0; i < x.Length; i++)
            {
                double d;
                d = y[i] - results[i];
                SSe += d * d;

                d = y[i] - outputMean;
                SSt += d * d;
            }


            // Calculate SSr
            SSr = SSt - SSe;

            // Calculate R-Squared
            RSquared = SSt != 0 ? 1.0 - SSe / SSt : 1.0;

            // Calculated Adjusted R-Squared
            if (RSquared == 1)
            {
                RSquareAdjusted = 1;
            }
            else
            {
                if (n - p == 1)
                {
                    RSquareAdjusted = double.NaN;
                }
                else
                {
                    RSquareAdjusted = 1.0 - (1.0 - RSquared) * ((n - 1.0) / (n - p - 1.0));
                }
            }

            // Calculate Degrees of Freedom
            DFr = p;
            DFe = n - (p + 1);
            DFt = DFr + DFe;

            // Calculate Sum of Squares Mean
            MSe = SSe / DFe;
            MSr = SSr / DFr;
            MSt = SSt / DFt;

            // Calculate the F statistic
            FTest = new FTest(MSr / MSe, DFr, DFe);
            StandardError = System.Math.Sqrt(MSe);

            // Create the ANOVA table
            var table = new List<AnovaVariationSource>();
            table.Add(new AnovaVariationSource(this, "Regression", SSr, DFr, MSr, FTest));
            table.Add(new AnovaVariationSource(this, "Error", SSe, DFe, MSe, null));
            table.Add(new AnovaVariationSource(this, "Total", SSt, DFt, MSt, null));
            Table = new AnovaSourceCollection(table);

            // Compute coefficient standard errors;
            standardErrors = new double[NumberOfInputs + 1];
            for (var i = 0; i < InformationMatrix.Length; i++)
            {
                standardErrors[i] = System.Math.Sqrt(MSe * InformationMatrix[i][i]);
            }

            // Compute coefficient tests
            for (var i = 0; i < CoefficientValues.Length; i++)
            {
                var tStatistic = CoefficientValues[i] / standardErrors[i];

                ttests[i] = new TTest(CoefficientValues[i],
                    standardErrors[i], degreesOfFreedom: DFe);

                ftests[i] = new FTest(tStatistic * tStatistic, 1, DFe);

                confidences[i] = ttests[i].GetConfidenceInterval(confidencePercent);
            }


            // Compute model performance tests
            ttest = new TTest(results, outputMean);
            ZTest = new ZTest(results, outputMean);
            ChiSquareTest = new ChiSquareTest(y, results, n - p - 1);
#pragma warning restore 612, 618
        }

        internal void setConfidenceIntervals(double percent)
        {
            confidencePercent = percent;
            for (var i = 0; i < ttests.Length; i++)
            {
                confidences[i] = ttest.GetConfidenceInterval(percent);
            }
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override double Transform(double[] input)
        {
            return regression.Transform(input);
        }

        /// <summary>
        ///     Gets the prediction interval for a given input.
        /// </summary>
        public DoubleRange GetPredictionInterval(double[] input)
        {
            return regression.GetPredictionInterval(input, System.Math.Sqrt(MSe), NumberOfSamples, InformationMatrix,
                confidencePercent);
        }

        /// <summary>
        ///     Gets the confidence interval for a given input.
        /// </summary>
        public DoubleRange GetConfidenceInterval(double[] input)
        {
            return regression.GetConfidenceInterval(input, System.Math.Sqrt(MSe), NumberOfSamples, InformationMatrix,
                confidencePercent);
        }
    }

    /// <summary>
    ///     <para>
    ///         Represents a Linear Regression coefficient found in the Multiple
    ///         Linear Regression Analysis allowing it to be bound to controls like
    ///         the DataGridView.
    ///     </para>
    ///     <para>
    ///         This class cannot be instantiated.
    ///     </para>
    /// </summary>
    [Serializable]
    public class LinearRegressionCoefficient
    {
        /// <summary>
        ///     Creates a regression coefficient representation.
        /// </summary>
        /// <param name="analysis">The analysis to which this coefficient belongs.</param>
        /// <param name="index">The coefficient index.</param>
        internal LinearRegressionCoefficient(MultipleLinearRegressionAnalysis analysis, int index)
        {
            this.Index = index;
            this.Analysis = analysis;
        }

        /// <summary>
        ///     Gets the Index of this coefficient on the original analysis coefficient collection.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Returns a reference to the parent analysis object.
        /// </summary>
        [Browsable(false)]
        public MultipleLinearRegressionAnalysis Analysis { get; }

        /// <summary>
        ///     Gets the name for the current coefficient.
        /// </summary>
        public string Name
        {
            get
            {
                if (IsIntercept)
                {
                    return "Intercept";
                }

                return Analysis.Inputs[Index];
            }
        }

        /// <summary>
        ///     Gets a value indicating whether this coefficient is an intercept term.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this coefficient is the intercept; otherwise, <c>false</c>.
        /// </value>
        [DisplayName("Intercept?")]
        public bool IsIntercept => Index == Analysis.NumberOfInputs;

        /// <summary>
        ///     Gets the coefficient value.
        /// </summary>
        [DisplayName("Value")]
        public double Value => Analysis.CoefficientValues[Index];

        /// <summary>
        ///     Gets the Standard Error for the current coefficient.
        /// </summary>
        [DisplayName("Std. Error")]
        public double StandardError => Analysis.StandardErrors[Index];

        /// <summary>
        ///     Gets the T-test performed for this coefficient.
        /// </summary>
        public TTest TTest => Analysis.ttests[Index];

        /// <summary>
        ///     Gets the F-test performed for this coefficient.
        /// </summary>
        public FTest FTest => Analysis.ftests[Index];

        /// <summary>
        ///     Gets the confidence interval (C.I.) for the current coefficient.
        /// </summary>
        [Browsable(false)]
        public DoubleRange Confidence => Analysis.Confidences[Index];

        /// <summary>
        ///     Gets the upper limit for the confidence interval.
        /// </summary>
        [DisplayName("Upper confidence limit")]
        public double ConfidenceUpper => Analysis.Confidences[Index].Max;

        /// <summary>
        ///     Gets the lower limit for the confidence interval.
        /// </summary>
        [DisplayName("Lower confidence limit")]
        public double ConfidenceLower => Analysis.Confidences[Index].Min;
    }

    /// <summary>
    ///     Represents a Collection of Linear Regression Coefficients found in the
    ///     <see cref="MultipleLinearRegressionAnalysis" />. This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public class LinearRegressionCoefficientCollection : ReadOnlyCollection<LinearRegressionCoefficient>
    {
        private MultipleLinearRegressionAnalysis analysis;

        internal LinearRegressionCoefficientCollection(MultipleLinearRegressionAnalysis analysis,
            LinearRegressionCoefficient[] components)
            : base(components)
        {
            this.analysis = analysis;
        }

        /// <summary>
        ///     Gets or sets the size of the confidence
        ///     intervals reported for the coefficients.
        ///     Default is 0.95.
        /// </summary>
        public double ConfidencePercent
        {
            get => analysis.confidencePercent;
            set => analysis.setConfidenceIntervals(value);
        }
    }
}