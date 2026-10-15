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
using System.Text;
using System.Threading;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Properties;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Backward Stepwise Logistic Regression Analysis.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Backward Stepwise regression is an exploratory analysis procedure,
    ///         where the analysis begins with a full (saturated) model and at each step
    ///         variables are eliminated from the model in a iterative fashion.
    ///     </para>
    ///     <para>
    ///         Significance tests are performed after each removal to track which of
    ///         the variables can be discarded safely without implying in degradation.
    ///         When no more variables can be removed from the model without causing
    ///         a significant loss in the model likelihood, the method can stop.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Suppose we have the following data about some patients.
    /// // The first variable is continuous and represent patient
    /// // age. The second variable is dichotomic and give whether
    /// // they smoke or not (this is completely fictional data).
    /// 
    /// double[][] inputs =
    /// {
    ///     //            Age  Smoking
    ///     new double[] { 55,    0   },  // 1
    ///     new double[] { 28,    0   },  // 2
    ///     new double[] { 65,    1   },  // 3
    ///     new double[] { 46,    0   },  // 4
    ///     new double[] { 86,    1   },  // 5
    ///     new double[] { 56,    1   },  // 6
    ///     new double[] { 85,    0   },  // 7
    ///     new double[] { 33,    0   },  // 8
    ///     new double[] { 21,    1   },  // 9
    ///     new double[] { 42,    1   },  // 10
    ///     new double[] { 33,    0   },  // 11
    ///     new double[] { 20,    1   },  // 12
    ///     new double[] { 43,    1   },  // 13
    ///     new double[] { 31,    1   },  // 14
    ///     new double[] { 22,    1   },  // 15
    ///     new double[] { 43,    1   },  // 16
    ///     new double[] { 46,    0   },  // 17
    ///     new double[] { 86,    1   },  // 18
    ///     new double[] { 56,    1   },  // 19
    ///     new double[] { 55,    0   },  // 20
    /// };
    /// 
    /// // Additionally, we also have information about whether
    /// // or not they those patients had lung cancer. The array
    /// // below gives 0 for those who did not, and 1 for those
    /// // who did.
    /// 
    /// double[] output =
    /// {
    ///     0, 0, 0, 1, 1, 1, 0, 0, 0, 1,
    ///     0, 1, 1, 1, 1, 1, 0, 1, 1, 0
    /// };
    /// 
    /// 
    /// // Create a Stepwise Logistic Regression analysis
    /// var regression = new StepwiseLogisticRegressionAnalysis(inputs, output,
    ///     new[] { "Age", "Smoking" }, "Cancer");
    /// 
    /// regression.Compute(); // compute the analysis.
    /// 
    /// // The full model will be stored in the complete property:
    /// StepwiseLogisticRegressionModel full = regression.Complete;
    /// 
    /// // The best model will be stored in the current property:
    /// StepwiseLogisticRegressionModel best = regression.Current;
    /// 
    /// // Let's check the full model results
    /// DataGridBox.Show(full.Coefficients); 
    /// 
    /// // We can see only the Smoking variable is statistically significant.
    /// // This is an indication the Age variable could be discarded from
    /// // the model.
    /// 
    /// // And check the best inner model result
    /// DataGridBox.Show(best.Coefficients);
    /// 
    /// // This is the best nested model found. This model only has the 
    /// // Smoking variable, which is still significant. Since no other
    /// // variables can be dropped, this is the best final model.
    /// 
    /// // The variables used in the current best model are
    /// string[] inputVariableNames = best.Inputs; // Smoking
    /// 
    /// // The best model likelihood ratio p-value is
    /// ChiSquareTest test = best.ChiSquare; // {0.816990081334823}
    /// 
    /// // so the model is distinguishable from a null model. We can also
    /// // query the other nested models by checking the Nested property:
    /// 
    /// DataGridBox.Show(regression.Nested);
    /// 
    /// // Finally, we can also use the analysis to classify a new patient
    /// double y = regression.Current.Regression.Compute(new double[] { 1 });
    /// 
    /// // For a smoking person, the answer probability is approximately 83%.
    /// </code>
    /// </example>
    /// <seealso cref="LogisticRegressionAnalysis" />
    [Serializable]
    public class StepwiseLogisticRegressionAnalysis : IRegressionAnalysis,
        ISupervisedLearning<LogisticRegression, double[], double>
    {
        private double fullLikelihood;

        private double[][] inputData;

        private string[] inputNames;

        // Fitting parameters
        private int iterations = 100;
        private string outputName;

        private double threshold = 0.15;
        private double tolerance = 10e-4;

        /// <summary>
        ///     Constructs a Stepwise Logistic Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        public StepwiseLogisticRegressionAnalysis(double[][] inputs, double[] outputs)
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


            inputData = inputs;
            Outputs = outputs;

            inputNames = new string[inputs[0].Length];
            for (var i = 0; i < inputNames.Length; i++)
            {
                inputNames[i] = "Input " + i;
            }

            outputName = "Output";

            Source = inputs.ToMatrix();
        }

        /// <summary>
        ///     Constructs a Stepwise Logistic Regression Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        /// <param name="inputNames">The names for the input variables.</param>
        /// <param name="outputName">The name for the output variable.</param>
        public StepwiseLogisticRegressionAnalysis(double[][] inputs, double[] outputs, string[] inputNames,
            string outputName)
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


            inputData = inputs;
            Outputs = outputs;

            this.inputNames = inputNames;
            this.outputName = outputName;

            Source = inputs.ToMatrix();
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
        ///     Gets the resulting probabilities obtained
        ///     by the most likely logistic regression model.
        /// </summary>
        public double[] Result { get; private set; }

        /// <summary>
        ///     Gets the current best nested model.
        /// </summary>
        public StepwiseLogisticRegressionModel Current { get; private set; }

        /// <summary>
        ///     Gets the full model.
        /// </summary>
        public StepwiseLogisticRegressionModel Complete { get; private set; }

        /// <summary>
        ///     Gets the collection of nested models obtained after
        ///     a step of the backward stepwise procedure.
        /// </summary>
        public StepwiseLogisticRegressionModelCollection Nested { get; private set; }

        /// <summary>
        ///     Gets or sets the name of the input variables.
        /// </summary>
        public string[] Inputs
        {
            get => inputNames;
            set => inputNames = value;
        }

        /// <summary>
        ///     Gets or sets the name of the output variables.
        /// </summary>
        public string Output
        {
            get => outputName;
            set => outputName = value;
        }

        /// <summary>
        ///     Gets or sets the significance threshold used to
        ///     determine if a nested model is significant or not.
        /// </summary>
        public double Threshold
        {
            get => threshold;
            set => threshold = value;
        }

        /// <summary>
        ///     Gets the final set of input variables indices
        ///     as selected by the stepwise procedure.
        /// </summary>
        public int[] Variables { get; private set; }

        /// <summary>
        ///     Source data used in the analysis.
        /// </summary>
        public double[,] Source { get; }

        /// <summary>
        ///     Gets the dependent variable value
        ///     for each of the source input points.
        /// </summary>
        public double[] Outputs { get; private set; }

        /// <summary>
        ///     Computes the Stepwise Logistic Regression.
        /// </summary>
        [Obsolete("Please use Learn(x, y) instead.")]
        public void Compute()
        {
            int changed;
            do
            {
                changed = DoStep();
            } while (changed != -1);


            // Get the final variable selection
            Variables = Current.Variables;

            var resultInput = inputData
                .Submatrix(null, Variables);

            // Compute the final model output probabilities
            Result = Current.Regression.Probability(resultInput);
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        public CancellationToken Token { get; set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        /// <exception cref="ArgumentException"></exception>
        public LogisticRegression Learn(double[][] x, double[] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            inputData = x;
            Outputs = y;

            int changed;
            do
            {
                changed = DoStep();
                if (Token.IsCancellationRequested)
                {
                    break;
                }
            } while (changed != -1);


            // Get the final variable selection
            Variables = Current.Variables;

            var resultInput = inputData.Submatrix(null, Variables);

            // Compute the final model output probabilities
            Result = Current.Regression.Probability(resultInput);

            return Current.Regression;
        }

        /// <summary>
        ///     Computes one step of the Stepwise Logistic Regression Analysis.
        /// </summary>
        /// <returns>
        ///     Returns the index of the variable discarded in the step or -1
        ///     in case no variable could be discarded.
        /// </returns>
        public int DoStep()
        {
            ChiSquareTest[] tests = null;

            // Check if we are performing the first step
            if (Current == null)
            {
                // This is the first step. We should create the full model.
                var inputCount = inputData[0].Length;
                var variables = Vector.Range(0, inputCount);

                var regression = new LogisticRegression
                {
                    NumberOfInputs = inputCount
                };

                fit(regression, inputData, Outputs);

                var test = regression.ChiSquare(inputData, Outputs);
                fullLikelihood = regression.GetLogLikelihood(inputData, Outputs);

                if (double.IsNaN(fullLikelihood))
                {
                    throw new ConvergenceException(
                        "Perfect separation detected. Please rethink the use of logistic regression.");
                }

                tests = new ChiSquareTest[regression.NumberOfInputs + 1];
                Current = new StepwiseLogisticRegressionModel(this, regression, variables, test, tests);
                Complete = Current;
            }


            // Verify first if a variable reduction is possible
            if (Current.Regression.NumberOfInputs == 1)
            {
                return -1; // cannot reduce further
            }


            // Now go and create the diminished nested models
            var nestedModels = new StepwiseLogisticRegressionModel[Current.Regression.NumberOfInputs];

            for (var i = 0; i < nestedModels.Length; i++)
            {
                // Create a diminished nested model without the current variable
                var regression = new LogisticRegression
                {
                    NumberOfInputs = Current.Regression.NumberOfInputs - 1
                };

                var variables = Current.Variables.RemoveAt(i);
                var subset = inputData.Get(null, variables);

                fit(regression, subset, Outputs);

                // Check the significance of the nested model
                var logLikelihood = regression.GetLogLikelihood(subset, Outputs);
                var ratio = 2.0 * (fullLikelihood - logLikelihood);

                var test = new ChiSquareTest(ratio, inputNames.Length - variables.Length)
                    { Size = threshold };

                if (tests != null)
                {
                    tests[i + 1] = test;
                }

                // Store the nested model
                nestedModels[i] = new StepwiseLogisticRegressionModel(this, regression, variables, test, null);
            }

            // Select the model with the highest p-value
            double pmax = 0;
            var imax = -1;
            for (var i = 0; i < nestedModels.Length; i++)
            {
                if (nestedModels[i].ChiSquare.PValue >= pmax)
                {
                    imax = i;
                    pmax = nestedModels[i].ChiSquare.PValue;
                }
            }

            // Create the read-only nested model collection
            Nested = new StepwiseLogisticRegressionModelCollection(nestedModels);


            // If the model with highest p-value is not significant,
            if (imax >= 0 && pmax > threshold)
            {
                // Then this means the variable can be safely discarded from the full model
                var removed = Current.Variables[imax];

                // Our diminished nested model will become our next full model.
                Current = nestedModels[imax];

                // Finally, return the index of the removed variable
                return removed;
            }

            // Else we can not safely remove any variable from the model.
            return -1;
        }

        /// <summary>
        ///     Fits a logistic regression model to data until convergence.
        /// </summary>
        private bool fit(LogisticRegression regression, double[][] input, double[] output)
        {
            var irls = new IterativeReweightedLeastSquares(regression)
            {
                Tolerance = tolerance,
                MaxIterations = iterations,
                Token = Token
            };

            irls.Learn(input, output);

            // Check if the full model has converged
            return irls.MaxIterations <= iterations;
        }
    }

    /// <summary>
    ///     Stepwise Logistic Regression Nested Model.
    /// </summary>
    [Serializable]
    public class StepwiseLogisticRegressionModel
    {
        /// <summary>
        ///     Constructs a new Logistic regression model.
        /// </summary>
        internal StepwiseLogisticRegressionModel(StepwiseLogisticRegressionAnalysis analysis,
            LogisticRegression regression,
            int[] variables, ChiSquareTest chiSquare, ChiSquareTest[] tests)
        {
            Analysis = analysis;
            Regression = regression;

            var coefficientCount = regression.NumberOfInputs + 1;

            Inputs = analysis.Inputs.Get(variables);
            ChiSquare = chiSquare;
            LikelihoodRatioTests = tests;
            Variables = variables;
            StandardErrors = new double[coefficientCount];
            WaldTests = new WaldTest[coefficientCount];
            CoefficientValues = new double[coefficientCount];
            Confidences = new DoubleRange[coefficientCount];
            OddsRatios = new double[coefficientCount];

            // Store coefficient information
            for (var i = 0; i < regression.NumberOfInputs + 1; i++)
            {
                StandardErrors[i] = regression.StandardErrors[i];
                WaldTests[i] = regression.GetWaldTest(i);
                CoefficientValues[i] = regression.GetCoefficient(i);
                Confidences[i] = regression.GetConfidenceInterval(i);
                OddsRatios[i] = regression.GetOddsRatio(i);
            }

            var sb = new StringBuilder();
            for (var i = 0; i < Inputs.Length; i++)
            {
                sb.Append(Inputs[i]);
                if (i < Inputs.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            Names = sb.ToString();


            var logCoefs = new List<NestedLogisticCoefficient>(coefficientCount);
            for (var i = 0; i < coefficientCount; i++)
            {
                logCoefs.Add(new NestedLogisticCoefficient(this, i));
            }

            Coefficients = new NestedLogisticCoefficientCollection(logCoefs);
        }

        /// <summary>
        ///     Gets information about the regression model
        ///     coefficients in a object-oriented structure.
        /// </summary>
        [Browsable(false)]
        public NestedLogisticCoefficientCollection Coefficients { get; private set; }

        /// <summary>
        ///     Gets the Stepwise Logistic Regression Analysis
        ///     from which this model belongs to.
        /// </summary>
        [Browsable(false)]
        public StepwiseLogisticRegressionAnalysis Analysis { get; private set; }

        /// <summary>
        ///     Gets the regression model.
        /// </summary>
        [Browsable(false)]
        public LogisticRegression Regression { get; private set; }

        /// <summary>
        ///     Gets the subset of the original variables used by the model.
        /// </summary>
        [Browsable(false)]
        public int[] Variables { get; private set; }

        /// <summary>
        ///     Gets the name of the variables used in
        ///     this model combined as a single string.
        /// </summary>
        [DisplayName("Inputs")]
        public string Names { get; private set; }

        /// <summary>
        ///     Gets the Chi-Square Likelihood Ratio test for the model.
        /// </summary>
        [DisplayName("Likelihood-ratio")]
        public ChiSquareTest ChiSquare { get; private set; }

        /// <summary>
        ///     Gets the subset of the original variables used by the model.
        /// </summary>
        [Browsable(false)]
        public string[] Inputs { get; private set; }

        /// <summary>
        ///     Gets the Odds Ratio for each coefficient
        ///     found during the logistic regression.
        /// </summary>
        [Browsable(false)]
        public double[] OddsRatios { get; private set; }

        /// <summary>
        ///     Gets the Standard Error for each coefficient
        ///     found during the logistic regression.
        /// </summary>
        [Browsable(false)]
        public double[] StandardErrors { get; internal set; }

        /// <summary>
        ///     Gets the Wald Tests for each coefficient.
        /// </summary>
        [Browsable(false)]
        public WaldTest[] WaldTests { get; internal set; }

        /// <summary>
        ///     Gets the value of each coefficient.
        /// </summary>
        [Browsable(false)]
        public double[] CoefficientValues { get; private set; }

        /// <summary>
        ///     Gets the 95% Confidence Intervals (C.I.)
        ///     for each coefficient found in the regression.
        /// </summary>
        [Browsable(false)]
        public DoubleRange[] Confidences { get; private set; }

        /// <summary>
        ///     Gets the Likelihood-Ratio Tests for each coefficient.
        /// </summary>
        [Browsable(false)]
        public ChiSquareTest[] LikelihoodRatioTests { get; private set; }
    }

    /// <summary>
    ///     Stepwise Logistic Regression Nested Model collection.
    ///     This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public class StepwiseLogisticRegressionModelCollection :
        ReadOnlyCollection<StepwiseLogisticRegressionModel>
    {
        internal StepwiseLogisticRegressionModelCollection(StepwiseLogisticRegressionModel[] models)
            : base(models)
        {
        }
    }

    /// <summary>
    ///     Represents a Logistic Regression Coefficient found in the Logistic Regression,
    ///     allowing it to be bound to controls like the DataGridView. This class cannot
    ///     be instantiated outside the <see cref="LogisticRegressionAnalysis" />.
    /// </summary>
    [Serializable]
    public class NestedLogisticCoefficient
    {
        private StepwiseLogisticRegressionModel analysis;
        private int index;

        internal NestedLogisticCoefficient(StepwiseLogisticRegressionModel analysis, int index)
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
        [DisplayName("Likelihood-Ratio p-value")]
        public ChiSquareTest LikelihoodRatio
        {
            get
            {
                if (analysis.LikelihoodRatioTests == null)
                {
                    return null;
                }

                return analysis.LikelihoodRatioTests[index];
            }
        }
    }

    /// <summary>
    ///     Represents a collection of Logistic Coefficients found in the
    ///     <see cref="LogisticRegressionAnalysis" />. This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public class NestedLogisticCoefficientCollection : ReadOnlyCollection<NestedLogisticCoefficient>
    {
        internal NestedLogisticCoefficientCollection(IList<NestedLogisticCoefficient> coefficients)
            : base(coefficients)
        {
        }
    }
}