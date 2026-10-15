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
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Logistic Regression Analysis.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Logistic Regression Analysis tries to extract useful
    ///         information about a logistic regression model.
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
    ///                     E. F. Connor. Logistic Regression. Available on:
    ///                     http://userwww.sfsu.edu/~efc/classes/biol710/logistic/logisticreg.htm
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     C. Shalizi. Logistic Regression and Newton's Method. Lecture notes. Available on:
    ///                     http://www.stat.cmu.edu/~cshalizi/350/lectures/26/lecture-26.pdf
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     A. Storkey. Learning from Data: Learning Logistic Regressors. Available on:
    ///                     http://www.inf.ed.ac.uk/teaching/courses/lfd/lectures/logisticlearn-print.pdf
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows to create a Logistic regresion analysis using a full
    ///         dataset composed of input vectors and a binary output vector. Each input vector
    ///         has an associated label (1 or 0) in the output vector, where 1 represents a positive
    ///         label (yes, or true) and 0 represents a negative label (no, or false).
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\LogisticRegressionAnalysisTest.cs"
    ///         region="doc_learn_part2" />
    ///     <para>
    ///         The resulting table is shown below.
    ///     </para>
    ///     <img src="..\images\logistic-regression.png" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\LogisticRegressionAnalysisTest.cs"
    ///         region="doc_learn_part1" />
    ///     <para>
    ///         The analysis can also be created from data given in a summary form. Instead of having
    ///         one input vector associated with one positive or negative label, each input vector is
    ///         associated with the proportion of positive to negative labels in the original dataset.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\LogisticRegressionAnalysisTest.cs"
    ///         region="doc_learn_summary" />
    ///     <para>
    ///         The last example shows how to learn a logistic regression analysis using data
    ///         given in the form of a System.Data.DataTable. This data is also heterogeneous, mixing
    ///         both discrete (symbol) variables and continuous variables. This example is also available
    ///         for <see cref="MultipleLinearRegressionAnalysis" />.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\LogisticRegressionAnalysisTest.cs"
    ///         region="doc_learn_database" />
    /// </example>
    /// <seealso cref="MultipleLinearRegressionAnalysis" />
    [Serializable]
    public class LogisticRegressionAnalysis :
        TransformBase<double[], double>, // TODO: Rename to BinaryLogLikelihoodClassifierBase
        IRegressionAnalysis,
        ISupervisedLearning<LogisticRegression, double[], int>,
        ISupervisedLearning<LogisticRegression, double[], double>
    {
        private bool innerComputed;

        [Obsolete] private double[][] inputData;

        private string[] inputNames;

        private int iterations = 50;
        [Obsolete] private double[] outputData;
        private string outputName;
        private ChiSquareTest[] ratioTests;

        private double regularization = 1e-10;
        [Obsolete] private double[] result;

        [Obsolete] private double[,] sourceMatrix;
        [NonSerialized] private CancellationToken token;
        private double tolerance = 1e-5;

        [Obsolete] private double[] weights;

        /// <summary>
        ///     Constructs a Logistic Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        [Obsolete("Please pass the inputs and outputs to the Learn method instead.")]
        public LogisticRegressionAnalysis(double[][] inputs, double[] outputs)
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
                throw new ArgumentException("The number of rows in the input array"
                                            + " must match the number of given outputs.", "outputs");
            }


            initialize(inputs, outputs);

            // Start regression using the Null Model
            Regression = new LogisticRegression(NumberOfInputs);
        }

        /// <summary>
        ///     Constructs a Logistic Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output, binary data for the analysis.</param>
        /// <param name="inputNames">The names of the input variables.</param>
        /// <param name="outputName">The name of the output variable.</param>
        /// <param name="weights">The weights associated with each input vector.</param>
        [Obsolete("Please pass the inputs and outputs to the Learn method instead.")]
        public LogisticRegressionAnalysis(double[][] inputs, double[] outputs, double[] weights,
            string[] inputNames, string outputName)
            : this(inputs, outputs, weights)
        {
            this.inputNames = inputNames;
            this.outputName = outputName;
        }

        /// <summary>
        ///     Constructs a Logistic Regression Analysis.
        /// </summary>
        public LogisticRegressionAnalysis()
        {
            NumberOfOutputs = 1;
            // this.NumberOfClasses = 2; // TODO: Uncomment this line after changing to LogLikelihoodClassifierBase
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations to be
        ///     performed by the regression algorithm. Default is 50.
        /// </summary>
        public int Iterations
        {
            get => iterations;
            set => iterations = value;
        }

        /// <summary>
        ///     Gets or sets the difference between two iterations of the regression
        ///     algorithm when the algorithm should stop. The difference is calculated
        ///     based on the largest absolute parameter change of the regression. Default
        ///     is 1e-5.
        /// </summary>
        public double Tolerance
        {
            get => tolerance;
            set => tolerance = value;
        }

        /// <summary>
        ///     Gets or sets the regularization value to be
        ///     added in the objective function. Default is
        ///     1e-10.
        /// </summary>
        public double Regularization
        {
            get => regularization;
            set => regularization = value;
        }

        /// <summary>
        ///     Gets or sets whether nested models should be computed in
        ///     order to calculate the likelihood-ratio test of each of
        ///     the coefficients. Default is false.
        /// </summary>
        public bool ComputeInnerModels { get; set; }

        /// <summary>
        ///     Gets the Logistic Regression model created
        ///     and evaluated by this analysis.
        /// </summary>
        public LogisticRegression Regression { get; private set; }

        /// <summary>
        ///     Gets the collection of coefficients of the model.
        /// </summary>
        public LogisticCoefficientCollection Coefficients { get; private set; }

        /// <summary>
        ///     Gets the Log-Likelihood for the model.
        /// </summary>
        public double LogLikelihood { get; private set; }

        /// <summary>
        ///     Gets the Chi-Square (Likelihood Ratio) Test for the model.
        /// </summary>
        public ChiSquareTest ChiSquare { get; private set; }

        /// <summary>
        ///     Gets the Deviance of the model.
        /// </summary>
        public double Deviance { get; private set; }

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
        ///     Gets the Odds Ratio for each coefficient
        ///     found during the logistic regression.
        /// </summary>
        public double[] OddsRatios { get; private set; }

        /// <summary>
        ///     Gets the Standard Error for each coefficient
        ///     found during the logistic regression.
        /// </summary>
        public double[] StandardErrors { get; private set; }

        /// <summary>
        ///     Gets the Wald Tests for each coefficient.
        /// </summary>
        public WaldTest[] WaldTests { get; private set; }

        /// <summary>
        ///     Gets the Likelihood-Ratio Tests for each coefficient.
        /// </summary>
        /// <remarks>
        ///     Since this operation might be potentially time-consuming, the likelihood-ratio
        ///     tests will be computed on the first time this property is acessed.
        /// </remarks>
        public ChiSquareTest[] LikelihoodRatioTests
        {
            get
            {
#pragma warning disable 612, 618
                if (!innerComputed)
                {
                    computeInner(Source.ToJagged(), Outputs, Weights);
                }
#pragma warning restore 612, 618

                return ratioTests;
            }
        }

        /// <summary>
        ///     Gets the value of each coefficient.
        /// </summary>
        public double[] CoefficientValues { get; private set; }

        /// <summary>
        ///     Gets the 95% Confidence Intervals (C.I.)
        ///     for each coefficient found in the regression.
        /// </summary>
        public DoubleRange[] Confidences { get; private set; }

        /// <summary>
        ///     Gets the number of samples used to compute the analysis.
        /// </summary>
        public int NumberOfSamples { get; private set; }

        /// <summary>
        ///     Gets the information matrix obtained during learning.
        /// </summary>
        public double[][] InformationMatrix { get; private set; }

#pragma warning disable 612, 618
        [Obsolete("Please use the Learn method instead.")]
        void IAnalysis.Compute()
        {
            Compute();
        }
#pragma warning restore 612, 618

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public LogisticRegression Learn(double[][] x, double[] y, double[] weights = null)
        {
            return compute(x, y, weights);
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
        public LogisticRegression Learn(double[][] x, int[] y, double[] weights = null)
        {
            return compute(x, y.ToDouble(), weights);
        }

        private void initialize(double[][] inputs, double[] outputs)
        {
            NumberOfInputs = inputs[0].Length;
            var coefficientCount = NumberOfInputs + 1;

#pragma warning disable 612, 618
            // Store data sets
            inputData = inputs;
            outputData = outputs;
#pragma warning restore 612, 618

            // Create additional structures
            CoefficientValues = new double[coefficientCount];
            WaldTests = new WaldTest[coefficientCount];
            StandardErrors = new double[coefficientCount];
            OddsRatios = new double[coefficientCount];
            Confidences = new DoubleRange[coefficientCount];
            ratioTests = new ChiSquareTest[coefficientCount];

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
            var logCoefs = new List<LogisticCoefficient>(coefficientCount);
            for (var i = 0; i < coefficientCount; i++)
            {
                logCoefs.Add(new LogisticCoefficient(this, i));
            }

            Coefficients = new LogisticCoefficientCollection(logCoefs);
        }

        /// <summary>
        ///     Gets the Log-Likelihood Ratio between this model and another model.
        /// </summary>
        /// <param name="model">Another logistic regression model.</param>
        /// <returns>The Likelihood-Ratio between the two models.</returns>
        [Obsolete("This method will be removed.")]
        public double GetLikelihoodRatio(GeneralizedLinearRegression model)
        {
#pragma warning disable 612, 618
            return Regression.GetLogLikelihoodRatio(inputData, outputData, model);
#pragma warning restore 612, 618
        }

        private LogisticRegression compute(double[][] input, double[] output, double[] weights)
        {
            if (Regression == null)
            {
                initialize(input, output);
                Regression = new LogisticRegression
                {
                    NumberOfInputs = input.Columns()
                };
            }

            var learning = new IterativeReweightedLeastSquares(Regression)
            {
                Regularization = regularization,
                MaxIterations = iterations,
                Tolerance = tolerance,
                Token = Token
            };

            learning.Learn(input, output, weights);

            InformationMatrix = learning.GetInformationMatrix();

            computeInformation(input, output, weights);

            innerComputed = false;
            if (ComputeInnerModels)
            {
                computeInner(input, output, weights);
            }

            return Regression;
        }

        /// <summary>
        ///     Computes the Logistic Regression Analysis.
        /// </summary>
        /// <remarks>
        ///     The likelihood surface for the logistic regression learning
        ///     is convex, so there will be only one peak. Any local maxima
        ///     will be also a global maxima.
        /// </remarks>
        /// <returns>
        ///     True if the model converged, false otherwise.
        /// </returns>
        [Obsolete("Please use the Learn() method instead.")]
        public bool Compute()
        {
            return compute();
        }

        /// <summary>
        ///     Computes the Logistic Regression Analysis for an already computed regression.
        /// </summary>
        [Obsolete("Please use the Learn() method instead.")]
        public void Compute(LogisticRegression regression)
        {
            this.Regression = regression;
#pragma warning disable 612, 618
            computeInformation(Source.ToJagged(), Outputs, Weights);
#pragma warning restore 612, 618

            innerComputed = false;
        }

        /// <summary>
        ///     Computes the Logistic Regression Analysis.
        /// </summary>
        /// <remarks>
        ///     The likelihood surface for the
        ///     logistic regression learning is convex, so there will be only one
        ///     peak. Any local maxima will be also a global maxima.
        /// </remarks>
        /// <param name="limit">
        ///     The difference between two iterations of the regression algorithm
        ///     when the algorithm should stop. If not specified, the value of
        ///     1e-5 will be used. The difference is calculated based on the largest
        ///     absolute parameter change of the regression.
        /// </param>
        /// <param name="maxIterations">
        ///     The maximum number of iterations to be performed by the regression
        ///     algorithm.
        /// </param>
        /// <returns>
        ///     True if the model converged, false otherwise.
        /// </returns>
        [Obsolete("Please set up the Iterations and Tolerance properties and call Learn() instead.")]
        public bool Compute(double limit = 1e-5, int maxIterations = 50)
        {
            iterations = maxIterations;
            tolerance = limit;

            return compute();
        }

        /// <summary>
        ///     Creates a new <see cref="LogisticRegressionAnalysis" /> from summarized data.
        ///     In summary data, instead of having a set of inputs and their associated outputs,
        ///     we have the number of times an input vector had a positive label in the data set
        ///     and how many times it had a negative label.
        /// </summary>
        /// <param name="data">The input data.</param>
        /// <param name="positives">The number of positives labels for each input vector.</param>
        /// <param name="negatives">The number of negative labels for each input vector.</param>
        /// <returns>
        ///     A <see cref="LogisticRegressionAnalysis" /> created from the given summary data.
        /// </returns>
        [Obsolete("Please use the Learn(x, positives, negatives) method instead.")]
        public static LogisticRegressionAnalysis FromSummary(double[][] data, int[] positives, int[] negatives)
        {
            var rate = new double[data.Length];
            var weights = new double[data.Length];

            for (var i = 0; i < rate.Length; i++)
            {
                rate[i] = positives[i] / (double)(positives[i] + negatives[i]);
                weights[i] = positives[i] + negatives[i];
            }

            return new LogisticRegressionAnalysis(data, rate, weights);
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="positives">The number of positives labels for each input vector.</param>
        /// <param name="negatives">The number of negative labels for each input vector.</param>
        /// <returns>
        ///     A <see cref="LogisticRegressionAnalysis" /> created from the given summary data.
        /// </returns>
        public LogisticRegression Learn(double[][] x, int[] positives, int[] negatives)
        {
            var rate = new double[x.Length];
            var weights = new double[x.Length];

            for (var i = 0; i < rate.Length; i++)
            {
                rate[i] = positives[i] / (double)(positives[i] + negatives[i]);
                weights[i] = positives[i] + negatives[i];
            }

            return Learn(x, rate, weights);
        }

#pragma warning disable 612, 618
        [Obsolete]
        private bool compute()
        {
            double delta;
            var iteration = 0;

            var learning = new IterativeReweightedLeastSquares(Regression);

            learning.Regularization = regularization;

            do // learning iterations until convergence
            {
                delta = learning.Run(inputData, outputData, weights);
                iteration++;
            } while (delta > tolerance && iteration < iterations);

            // Check if the full model has converged
            var converged = iteration < iterations;

            computeInformation(inputData, outputData, weights);
            innerComputed = false;

            return converged;
        }
#pragma warning restore 612, 618

        private void computeInner(double[][] inputData, double[] outputData, double[] weights)
        {
            for (var i = 0; i < NumberOfInputs; i++)
            {
                // Create a diminished inner model without the current variable
                var data = inputData.RemoveColumn(i);

                // Perform likelihood-ratio tests against diminished nested models
                var innerModel = new LogisticRegression
                {
                    NumberOfInputs = NumberOfInputs - 1
                };

                var learning = new IterativeReweightedLeastSquares(innerModel)
                {
                    MaxIterations = iterations,
                    Tolerance = tolerance,
                    Regularization = regularization
                };

                learning.Learn(data, outputData, weights);

                var ratio = 2.0 * (LogLikelihood - innerModel.GetLogLikelihood(data, outputData));
                ratioTests[i + 1] = new ChiSquareTest(ratio, 1);
            }

            innerComputed = true;
        }

        private void computeInformation(double[][] inputData, double[] outputData, double[] weights)
        {
            // Store model information
#pragma warning disable 612, 618
            result = Regression.Compute(inputData);
#pragma warning restore 612, 618

            NumberOfSamples = inputData.Length;

            if (weights == null)
            {
                Deviance = Regression.GetDeviance(inputData, outputData);
                LogLikelihood = Regression.GetLogLikelihood(inputData, outputData);
                ChiSquare = Regression.ChiSquare(inputData, outputData);
            }
            else
            {
                Deviance = Regression.GetDeviance(inputData, outputData, weights);
                LogLikelihood = Regression.GetLogLikelihood(inputData, outputData, weights);
                ChiSquare = Regression.ChiSquare(inputData, outputData, weights);
            }

            // Store coefficient information
            for (var i = 0; i < Regression.NumberOfParameters; i++)
            {
                StandardErrors[i] = Regression.StandardErrors[i];

                WaldTests[i] = Regression.GetWaldTest(i);
                CoefficientValues[i] = Regression.GetCoefficient(i);
                Confidences[i] = Regression.GetConfidenceInterval(i);
                OddsRatios[i] = Regression.GetOddsRatio(i);
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
            return (Regression as ITransform<double[], double>).Transform(input);
        }

        /// <summary>
        ///     Gets the confidence interval for a given input.
        /// </summary>
        public DoubleRange GetConfidenceInterval(double[] input)
        {
            return Regression.GetConfidenceInterval(input, NumberOfSamples, InformationMatrix);
        }

        /// <summary>
        ///     Gets the prediction interval for a given input.
        /// </summary>
        public DoubleRange GetPredictionInterval(double[] input)
        {
            return Regression.GetPredictionInterval(input, NumberOfSamples, InformationMatrix);
        }

#pragma warning disable 612, 618

        /// <summary>
        ///     Constructs a Logistic Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        /// <param name="weights">The weights associated with each input vector.</param>
        [Obsolete("Please pass the inputs and outputs to the Learn method instead.")]
        public LogisticRegressionAnalysis(double[][] inputs, double[] outputs, double[] weights)
            : this(inputs, outputs)
        {
            if (weights == null)
            {
                throw new ArgumentNullException("weights");
            }

            if (weights.Length != outputs.Length)
            {
                throw new ArgumentException("The number weights"
                                            + " must match the number of given input rows.", "outputs");
            }

            this.weights = weights;
        }

        /// <summary>
        ///     Constructs a Logistic Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output, binary data for the analysis.</param>
        /// <param name="inputNames">The names of the input variables.</param>
        /// <param name="outputName">The name of the output variable.</param>
        [Obsolete("Please pass the inputs and outputs to the Learn method instead.")]
        public LogisticRegressionAnalysis(double[][] inputs, double[] outputs,
            string[] inputNames, string outputName)
            : this(inputs, outputs)
        {
            this.inputNames = inputNames;
            this.outputName = outputName;
        }
#pragma warning restore 612, 618

#pragma warning disable 612, 618
        /// <summary>
        ///     Gets the source matrix from which the analysis was run.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Source
        {
            get
            {
                if (sourceMatrix == null)
                {
                    sourceMatrix = inputData.ToMatrix();
                }

                return sourceMatrix;
            }
        }

        /// <summary>
        ///     Gets the source matrix from which the analysis was run.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[][] Array => inputData;

        /// <summary>
        ///     Gets the dependent variable value
        ///     for each of the source input points.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[] Outputs => outputData;

        /// <summary>
        ///     Gets the resulting probabilities obtained
        ///     by the logistic regression model.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[] Result => result;

        /// <summary>
        ///     Gets the sample weight associated with each input vector.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[] Weights => weights;
#pragma warning restore 612, 618
    }

    #region Support Classes

    /// <summary>
    ///     Represents a Logistic Regression Coefficient found in the Logistic Regression,
    ///     allowing it to be bound to controls like the DataGridView. This class cannot
    ///     be instantiated outside the <see cref="LogisticRegressionAnalysis" />.
    /// </summary>
    [Serializable]
    public class LogisticCoefficient
    {
        private LogisticRegressionAnalysis analysis;

        private int index;

        internal LogisticCoefficient(LogisticRegressionAnalysis analysis, int index)
        {
            this.analysis = analysis;
            this.index = index;
        }

        /// <summary>
        ///     Gets the name for the current coefficient.
        /// </summary>
        public string Name
        {
            get
            {
                if (index == 0)
                {
                    return "Intercept";
                }

                return analysis.Inputs[index - 1];
            }
        }

        /// <summary>
        ///     Gets the Odds ratio for the current coefficient.
        /// </summary>
        [DisplayName("Odds ratio")]
        public double OddsRatio => analysis.OddsRatios[index];

        /// <summary>
        ///     Gets the Standard Error for the current coefficient.
        /// </summary>
        [DisplayName("Std. Error")]
        public double StandardError => analysis.StandardErrors[index];

        /// <summary>
        ///     Gets the 95% confidence interval (C.I.) for the current coefficient.
        /// </summary>
        [Browsable(false)]
        public DoubleRange Confidence => analysis.Confidences[index];

        /// <summary>
        ///     Gets the upper limit for the 95% confidence interval.
        /// </summary>
        [DisplayName("Upper confidence limit")]
        public double ConfidenceUpper => Confidence.Max;

        /// <summary>
        ///     Gets the lower limit for the 95% confidence interval.
        /// </summary>
        [DisplayName("Lower confidence limit")]
        public double ConfidenceLower => Confidence.Min;

        /// <summary>
        ///     Gets the coefficient value.
        /// </summary>
        [DisplayName("Value")]
        public double Value => analysis.CoefficientValues[index];

        /// <summary>
        ///     Gets the Wald's test performed for this coefficient.
        /// </summary>
        [DisplayName("Wald p-value")]
        public WaldTest Wald => analysis.WaldTests[index];

        /// <summary>
        ///     Gets the Likelihood-Ratio test performed for this coefficient.
        /// </summary>
        /// <remarks>
        ///     Since this operation might be potentially time-consuming, the likelihood-ratio
        ///     tests will be computed on the first time this property is acessed.
        /// </remarks>
        [DisplayName("Likelihood-Ratio p-value")]
        public ChiSquareTest LikelihoodRatio => analysis.LikelihoodRatioTests[index];

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}; {1} ({2}, {3})", Name, Value, ConfidenceLower, ConfidenceUpper);
        }
    }

    /// <summary>
    ///     Represents a collection of Logistic Coefficients found in the
    ///     <see cref="LogisticRegressionAnalysis" />. This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public class LogisticCoefficientCollection : ReadOnlyCollection<LogisticCoefficient>
    {
        internal LogisticCoefficientCollection(IList<LogisticCoefficient> coefficients)
            : base(coefficients)
        {
        }
    }

    #endregion
}