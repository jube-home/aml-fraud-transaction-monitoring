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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Filters;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Multinomial Logistic Regression Analysis
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In statistics, multinomial logistic regression is a classification method that
    ///         generalizes logistic regression to multiclass problems, i.e. with more than two
    ///         possible discrete outcomes.[1] That is, it is a model that is used to predict the
    ///         probabilities of the different possible outcomes of a categorically distributed
    ///         dependent variable, given a set of independent variables (which may be real-valued,
    ///         binary-valued, categorical-valued, etc.).
    ///     </para>
    ///     <para>
    ///         Multinomial logistic regression is known by a variety of other names, including
    ///         multiclass LR, multinomial regression,[2] softmax regression, multinomial logit,
    ///         maximum entropy (MaxEnt) classifier, conditional maximum entropy model.
    ///     </para>
    ///     para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia contributors. "Multinomial logistic regression." Wikipedia, The Free Encyclopedia, 1st
    ///                     April, 2015.
    ///                     Available at: https://en.wikipedia.org/wiki/Multinomial_logistic_regression
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The first example shows how to reproduce a textbook example using categorical and categorical-with-baseline
    ///         variables. Those variables can be transformed/factored to their respective representations using the
    ///         <see cref="Codification" /> class. However, please note that while this example uses features from the
    ///         <see cref="Codification" /> class, the use of this class is not required when learning a
    ///         <see cref="MultinomialLogisticRegression" />
    ///         modoel.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\MultinomialLogisticRegressionAnalysisTest.cs"
    ///         region="doc_learn_1" />
    ///     <para>
    ///         The second example shows how to learn a <see cref="MultinomialLogisticRegressionAnalysis" /> from the
    ///         famous Fisher's Iris dataset. This example should demonstrate that <see cref="Codification" /> filters
    ///         are not required to successfully learn multinomial logistic regression analyses.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\MultinomialLogisticRegressionAnalysisTest.cs"
    ///         region="doc_learn_2" />
    /// </example>
    [Serializable]
    public class MultinomialLogisticRegressionAnalysis : TransformBase<double[], int>,
        IMultivariateRegressionAnalysis,
        ISupervisedLearning<MultinomialLogisticRegression, double[], double[]>,
        ISupervisedLearning<MultinomialLogisticRegression, double[], int>
    {
        private int coefficientCount;

        internal double confidencePercent = 0.95;

        // Coefficient measures
        internal DoubleRange[][] confidences;

        private int inputCount;

        [Obsolete] private double[][] inputData;

        private string[] inputNames;

        private int iterations = 50;
        private double[][] oddsRatios;
        [Obsolete] private double[][] outputData;
        private string[] outputNames;

        internal MultinomialLogisticRegression regression;
        [Obsolete] private double[][] results;
        [NonSerialized] private CancellationToken token;
        private double tolerance = 1e-5;

        /// <summary>
        ///     Constructs a Multinomial Logistic Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        [Obsolete("Please pass the inputs and outputs to the Learn() method.")]
        public MultinomialLogisticRegressionAnalysis(double[][] inputs, int[] outputs)
        {
            // Initial argument checking
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

            init(inputs, Jagged.OneHot(outputs));
        }

        /// <summary>
        ///     Constructs a Multinomial Logistic Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        [Obsolete("Please pass the inputs and outputs to the Learn() method.")]
        public MultinomialLogisticRegressionAnalysis(double[][] inputs, double[][] outputs)
        {
            // Initial argument checking
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

            init(inputs, outputs);
        }

        /// <summary>
        ///     Constructs a Multiple Linear Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        /// <param name="inputNames">The names of the input variables.</param>
        /// <param name="outputNames">The names of the output variables.</param>
#pragma warning disable 612, 618
        [Obsolete("Please pass the inputs and outputs to the Learn() method.")]
        public MultinomialLogisticRegressionAnalysis(double[][] inputs, int[] outputs,
            string[] inputNames, string[] outputNames)
            : this(inputs, outputs)
#pragma warning restore 612, 618
        {
            names(inputNames, outputNames);
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
        ///     Gets the number of outputs in the regression problem.
        /// </summary>
        public int OutputCount { get; private set; }

        /// <summary>
        ///     Gets the Standard Error for each coefficient
        ///     found during the logistic regression.
        /// </summary>
        public double[][] StandardErrors { get; private set; }

        /// <summary>
        ///     Gets the Regression model created
        ///     and evaluated by this analysis.
        /// </summary>
        public MultinomialLogisticRegression Regression => regression;

        /// <summary>
        ///     Gets the value of each coefficient.
        /// </summary>
        public double[][] CoefficientValues { get; private set; }

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
        ///     Gets the Wald Tests for each coefficient.
        /// </summary>
        public WaldTest[][] WaldTests { get; private set; }

        /// <summary>
        ///     Obsolete. Please use <see cref="InputNames" /> instead.
        /// </summary>
        [Obsolete("Please use InputNames instead.")]
        public string[] Inputs
        {
            get => InputNames;
            set => InputNames = value;
        }

        /// <summary>
        ///     Gets or sets the name of the input variables for the model.
        /// </summary>
        public string[] InputNames
        {
            get => inputNames;
            set => inputNames = value;
        }

        /// <summary>
        ///     Gets or sets the name of the output variable for the model.
        /// </summary>
        public string[] OutputNames
        {
            get => outputNames;
            set => outputNames = value;
        }

        /// <summary>
        ///     Gets the Confidence Intervals (C.I.)
        ///     for each coefficient found in the regression.
        /// </summary>
        public DoubleRange[][] Confidences => confidences;

        /// <summary>
        ///     Gets the collection of coefficients of the model.
        /// </summary>
        public MultinomialCoefficientCollection Coefficients { get; private set; }

#pragma warning disable 612, 618
        [Obsolete("Please use the Learn method instead.")]
        void IAnalysis.Compute()
        {
            Compute();
        }
#pragma warning restore 612, 618

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
        public MultinomialLogisticRegression Learn(double[][] x, double[][] y, double[] weights = null)
        {
            init(x, y);

            var learning = new LowerBoundNewtonRaphson(regression)
            {
                Tolerance = tolerance,
                MaxIterations = iterations,
                Token = Token
            };

            learning.Learn(x, y, weights);

            computeInformation(x, y);

            return regression;
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
        public MultinomialLogisticRegression Learn(double[][] x, int[] y, double[] weights = null)
        {
            return Learn(x, Jagged.OneHot(y), weights);
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
        public MultinomialLogisticRegression Learn(int[][] x, int[][] y, double[] weights = null)
        {
            return Learn(x.ToDouble(), y.ToDouble(), weights);
        }

        /// <summary>
        ///     Computes the Multinomial Logistic Regression Analysis.
        /// </summary>
        [Obsolete("Please use the Learn method instead.")]
        public bool Compute()
        {
#pragma warning disable 612, 618
            double delta;
            var iteration = 0;

            var learning = new LowerBoundNewtonRaphson(regression);

            do // learning iterations until convergence
            {
                delta = learning.Run(inputData, outputData);
                iteration++;
            } while (delta > tolerance && iteration < iterations);

            // Check if the full model has converged
            var converged = iteration < iterations;

            computeInformation(inputData, outputData);

            return converged;
#pragma warning restore 612, 618
        }

        private void computeInformation(double[][] inputData, double[][] outputData)
        {
            // Store model information
#pragma warning disable 612, 618
            results = regression.Compute(inputData);
#pragma warning restore 612, 618

            Deviance = regression.GetDeviance(inputData, outputData);
            LogLikelihood = regression.GetLogLikelihood(inputData, outputData);
            ChiSquare = regression.ChiSquare(inputData, outputData);

            CoefficientValues = regression.Coefficients;
            StandardErrors = regression.StandardErrors;

            // Store coefficient information
            for (var i = 0; i < WaldTests.Length; i++)
            {
                WaldTests[i] = regression.GetWaldTest(i);
                confidences[i] = regression.GetConfidenceInterval(i);
                oddsRatios[i] = regression.GetOddsRatio(i);
            }
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override int Transform(double[] input)
        {
            return regression.Transform(input);
        }

#pragma warning disable 612, 618
        /// <summary>
        ///     Source data used in the analysis.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[][] Array => inputData;

        /// <summary>
        ///     Source data used in the analysis.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Source => inputData.ToMatrix();

        /// <summary>
        ///     Gets the dependent variable value
        ///     for each of the source input points.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Output => outputData.ToMatrix();

        /// <summary>
        ///     Gets the dependent variable value
        ///     for each of the source input points.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[][] Outputs => outputData;

        /// <summary>
        ///     Gets the resulting values obtained by the regression model.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[][] Results => results;
#pragma warning restore 612, 618

        /// <summary>
        ///     Constructs a Multiple Linear Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        /// <param name="inputNames">The names of the input variables.</param>
        /// <param name="outputNames">The names of the output variables.</param>
#pragma warning disable 612, 618
        [Obsolete("Please pass the inputs and outputs to the Learn() method.")]
        public MultinomialLogisticRegressionAnalysis(double[][] inputs, double[][] outputs,
            string[] inputNames, string[] outputNames)
            : this(inputs, outputs)
        {
            names(inputNames, outputNames);
        }

        /// <summary>
        ///     Constructs a Multiple Linear Regression Analysis.
        /// </summary>
        /// <param name="inputNames">The names of the input variables.</param>
        /// <param name="outputNames">The names of the output variables.</param>
        public MultinomialLogisticRegressionAnalysis(string[] inputNames, string[] outputNames)
        {
            this.inputNames = inputNames;
            this.outputNames = outputNames;
        }

        /// <summary>
        ///     Constructs a Multiple Linear Regression Analysis.
        /// </summary>
        public MultinomialLogisticRegressionAnalysis()
        {
        }

        private void names(string[] inputNames, string[] outputNames)
        {
            if (inputNames.Length != this.inputNames.Length)
            {
                throw new ArgumentException("The input names vector should have the same length"
                                            + " as the number of variables in the analysis. In this analysis, there are "
                                            + this.inputNames.Length + " variables expected.");
            }

            if (outputNames.Length != this.outputNames.Length)
            {
                throw new ArgumentException("The output names vector should have the same length"
                                            + " as the number of output classes in the analysis. In this analysis, there are "
                                            + this.outputNames.Length + " class labels expected.");
            }

            this.inputNames = inputNames;
            this.outputNames = outputNames;
        }

        private void init(double[][] inputs, double[][] outputs)
        {
            inputCount = inputs[0].Length;
            OutputCount = outputs[0].Length;

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i].Length != inputCount)
                {
                    throw new ArgumentException("All input vectors must have the same length.");
                }
            }

            for (var i = 0; i < outputs.Length; i++)
            {
                if (outputs[i].Length != OutputCount)
                {
                    throw new ArgumentException("All output vectors must have the same length.");
                }
            }

            // Store data sets
            inputData = inputs;
            outputData = outputs;

            // Create the linear regression
            regression = new MultinomialLogisticRegression(inputCount, OutputCount);

            // Create additional structures
            coefficientCount = regression.Coefficients[0].Length;
            CoefficientValues = regression.Coefficients;
            StandardErrors = regression.StandardErrors;
            confidences = new DoubleRange[OutputCount - 1][];
            oddsRatios = new double[OutputCount - 1][];
            WaldTests = new WaldTest[OutputCount - 1][];
            NumberOfInputs = inputs.Columns();
            NumberOfOutputs = outputs.Columns();

            for (var i = 0; i < confidences.Length; i++)
            {
                confidences[i] = new DoubleRange[coefficientCount];
                oddsRatios[i] = new double[coefficientCount];
                WaldTests[i] = new WaldTest[coefficientCount];
            }

            if (inputNames == null)
            {
                inputNames = new string[inputCount];
                for (var i = 0; i < inputNames.Length; i++)
                {
                    inputNames[i] = "Input " + i;
                }
            }

            if (outputNames == null)
            {
                outputNames = new string[OutputCount];
                for (var i = 0; i < outputNames.Length; i++)
                {
                    outputNames[i] = "Class " + i;
                }
            }

            // Create object-oriented structure to represent the analysis
            var coefs = new MultinomialCoefficient[(OutputCount - 1) * coefficientCount + 1];
            coefs[0] = new MultinomialCoefficient(this, 0, 0);
            for (int k = 1, j = 1; j < OutputCount; j++)
            {
                for (var i = 0; i < coefficientCount; i++, k++)
                {
                    coefs[k] = new MultinomialCoefficient(this, j, i);
                }
            }

            Coefficients = new MultinomialCoefficientCollection(coefs);
        }
#pragma warning restore 612, 618
    }

    #region Support Classes

    /// <summary>
    ///     <para>
    ///         Represents a Multinomial Logistic Regression coefficient found in the
    ///         <see cref="MultinomialLogisticRegressionAnalysis">
    ///             multinomial logistic
    ///             regression analysis
    ///         </see>
    ///         allowing it to be bound to controls like the
    ///         DataGridView.
    ///     </para>
    ///     <para>
    ///         This class cannot be instantiated.
    ///     </para>
    /// </summary>
    [Serializable]
    public class MultinomialCoefficient
    {
        private int category;

        /// <summary>
        ///     Creates a regression coefficient representation.
        /// </summary>
        /// <param name="analysis">The analysis to which this coefficient belongs.</param>
        /// <param name="index">The coefficient's index.</param>
        /// <param name="category">The coefficient's category.</param>
        internal MultinomialCoefficient(MultinomialLogisticRegressionAnalysis analysis, int category, int index)
        {
            this.category = category;
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
        public MultinomialLogisticRegressionAnalysis Analysis { get; }

        /// <summary>
        ///     Gets the name of the category that this coefficient belongs to.
        /// </summary>
        public string Class => Analysis.OutputNames[category];

        /// <summary>
        ///     Gets the name for the current coefficient.
        /// </summary>
        public string Name
        {
            get
            {
                if (category == 0)
                {
                    return "(baseline)";
                }

                if (Index == 0)
                {
                    return "Intercept";
                }

                return Analysis.InputNames[Index - 1];
            }
        }

        /// <summary>
        ///     Gets the coefficient value.
        /// </summary>
        [DisplayName("Value")]
        public double Value
        {
            get
            {
                if (category == 0)
                {
                    return 0.0;
                }

                return Analysis.regression.Coefficients[category - 1][Index];
            }
        }

        /// <summary>
        ///     Gets the Standard Error for the current coefficient.
        /// </summary>
        [DisplayName("Std. Error")]
        public double StandardError
        {
            get
            {
                if (category == 0)
                {
                    return 0.0;
                }

                return Analysis.StandardErrors[category - 1][Index];
            }
        }

        /// <summary>
        ///     Gets the confidence interval (C.I.) for the current coefficient.
        /// </summary>
        [Browsable(false)]
        public DoubleRange Confidence
        {
            get
            {
                if (category == 0)
                {
                    return new DoubleRange(0, 0);
                }

                return Analysis.Confidences[category - 1][Index];
            }
        }

        /// <summary>
        ///     Gets the upper limit for the confidence interval.
        /// </summary>
        [DisplayName("Upper confidence limit")]
        public double ConfidenceUpper
        {
            get
            {
                if (category == 0)
                {
                    return 0.0;
                }

                return Analysis.Confidences[category - 1][Index].Max;
            }
        }

        /// <summary>
        ///     Gets the lower limit for the confidence interval.
        /// </summary>
        [DisplayName("Lower confidence limit")]
        public double ConfidenceLower
        {
            get
            {
                if (category == 0)
                {
                    return 0.0;
                }

                return Analysis.Confidences[category - 1][Index].Min;
            }
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (category == 0)
            {
                return Class + " (baseline)";
            }

            return string.Format("{0}, {1}; {2} ({3}, {4})", Class, Name, Value, ConfidenceLower, ConfidenceUpper);
        }
    }

    /// <summary>
    ///     Represents a Collection of Multinomial Logistic Regression Coefficients found in the
    ///     <see cref="MultinomialLogisticRegressionAnalysis" />. This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public class MultinomialCoefficientCollection : ReadOnlyCollection<MultinomialCoefficient>
    {
        internal MultinomialCoefficientCollection(MultinomialCoefficient[] components)
            : base(components)
        {
        }
    }

    #endregion
}