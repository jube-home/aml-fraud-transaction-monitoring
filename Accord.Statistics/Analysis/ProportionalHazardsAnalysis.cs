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
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Cox's Proportional Hazards Survival Analysis.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Proportional hazards models are a class of survival models in statistics. Survival models
    ///         relate the time that passes before some event occurs to one or more covariates that may be
    ///         associated with that quantity. In a proportional hazards model, the unique effect of a unit
    ///         increase in a covariate is multiplicative with respect to the hazard rate.
    ///     </para>
    ///     <para>
    ///         For example, taking a drug may halve one's hazard rate for a stroke occurring, or, changing
    ///         the material from which a manufactured component is constructed may double its hazard rate
    ///         for failure. Other types of survival models such as accelerated failure time models do not
    ///         exhibit proportional hazards. These models could describe a situation such as a drug that
    ///         reduces a subject's immediate risk of having a stroke, but where there is no reduction in
    ///         the hazard rate after one year for subjects who do not have a stroke in the first year of
    ///         analysis.
    ///     </para>
    ///     <para>
    ///         This class uses the <see cref="ProportionalHazards" /> to extract more detailed
    ///         information about a given problem, such as confidence intervals, hypothesis tests
    ///         and performance measures.
    ///     </para>
    ///     <para>
    ///         This class can also be bound to standard controls such as the
    ///         <a href="http://msdn.microsoft.com/en-us/library/system.windows.forms.datagridview.aspx">DataGridView</a>
    ///         by setting their DataSource property to the analysis' <see cref="Coefficients" /> property.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\ProportionalHazardsAnalysisTest.cs"
    ///         region="doc_learn_part1" lang="cs" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics.VB\Analysis\ProportionalHazardsAnalysisTest.vb"
    ///         region="doc_learn_part1" lang="vb" />
    ///     <para>
    ///         The resulting table is shown below.
    ///     </para>
    ///     <img src="..\images\cox-hazards.png" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\ProportionalHazardsAnalysisTest.cs"
    ///         region="doc_learn_part2" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics.VB\Analysis\ProportionalHazardsAnalysisTest.vb"
    ///         region="doc_learn_part2" lang="vb" />
    /// </example>
    /// <seealso cref="ProportionalHazards" />
    /// <seealso cref="ProportionalHazardsNewtonRaphson" />
    [Serializable]
    public class ProportionalHazardsAnalysis : IRegressionAnalysis,
        ISupervisedLearning<ProportionalHazards, Tuple<double[], double>, int>
    {
        private string censorName;

        private HazardCoefficientCollection coefficientCollection;

        private bool innerComputed;

        private int inputCount;

        private double[][] inputData;

        private string[] inputNames;

        private int iterations = 50;
        private ChiSquareTest[] ratioTests;

        private double[] result;

        private double[,] source;
        private string timeName;
        [NonSerialized] private CancellationToken token;
        private double tolerance = 1e-5;

        /// <summary>
        ///     Constructs a new Cox's Proportional Hazards Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="times">The output data for the analysis.</param>
        /// <param name="censor">The right-censoring indicative values.</param>
        [Obsolete("Please do not pass data to this constructor, and use the Learn method instead.")]
        public ProportionalHazardsAnalysis(double[,] inputs, double[] times, int[] censor)
            : this(inputs, times, Matrix.To<SurvivalOutcome[]>(censor))
        {
        }

        /// <summary>
        ///     Constructs a new Cox's Proportional Hazards Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="times">The output data for the analysis.</param>
        /// <param name="censor">The right-censoring indicative values.</param>
        [Obsolete("Please do not pass data to this constructor, and use the Learn method instead.")]
        public ProportionalHazardsAnalysis(double[][] inputs, double[] times, int[] censor)
            : this(inputs, times, Matrix.To<SurvivalOutcome[]>(censor))
        {
        }

        /// <summary>
        ///     Constructs a new Cox's Proportional Hazards Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="times">The output, binary data for the analysis.</param>
        /// <param name="censor">The right-censoring indicative values.</param>
        /// <param name="inputNames">The names of the input variables.</param>
        /// <param name="timeName">The name of the time variable.</param>
        /// <param name="censorName">The name of the event indication variable.</param>
        [Obsolete("Please do not pass data to this constructor, and use the Learn method instead.")]
        public ProportionalHazardsAnalysis(double[][] inputs, double[] times, int[] censor,
            string[] inputNames, string timeName, string censorName)
            : this(inputs, times, Matrix.To<SurvivalOutcome[]>(censor), inputNames, timeName, censorName)
        {
        }

        /// <summary>
        ///     Constructs a new Cox's Proportional Hazards Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="times">The output data for the analysis.</param>
        /// <param name="censor">The right-censoring indicative values.</param>
        [Obsolete("Please do not pass data to this constructor, and use the Learn method instead.")]
        public ProportionalHazardsAnalysis(double[,] inputs, double[] times, SurvivalOutcome[] censor)
        {
            // Initial argument checking
            if (inputs == null)
            {
                throw new ArgumentNullException("inputs");
            }

            if (times == null)
            {
                throw new ArgumentNullException("times");
            }

            if (inputs.GetLength(0) != times.Length)
            {
                throw new ArgumentException(
                    "The number of rows in the input array must match the number of given outputs.");
            }

            initialize(inputs.ToJagged(), times, censor);

            // Start regression using the Null Model
            Regression = new ProportionalHazards(inputCount);
        }

        /// <summary>
        ///     Constructs a new Cox's Proportional Hazards Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="times">The output data for the analysis.</param>
        /// <param name="censor">The right-censoring indicative values.</param>
        [Obsolete("Please do not pass data to this constructor, and use the Learn method instead.")]
        public ProportionalHazardsAnalysis(double[][] inputs, double[] times, SurvivalOutcome[] censor)
        {
            // Initial argument checking
            if (inputs == null)
            {
                throw new ArgumentNullException("inputs");
            }

            if (times == null)
            {
                throw new ArgumentNullException("times");
            }

            if (inputs.Length != times.Length)
            {
                throw new ArgumentException(
                    "The number of rows in the input array must match the number of given outputs.");
            }

            initialize(inputs, times, censor);

            // Start regression using the Null Model
            Regression = new ProportionalHazards(inputCount);
        }

        /// <summary>
        ///     Constructs a new Cox's Proportional Hazards Analysis.
        /// </summary>
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="times">The output, binary data for the analysis.</param>
        /// <param name="censor">The right-censoring indicative values.</param>
        /// <param name="inputNames">The names of the input variables.</param>
        /// <param name="timeName">The name of the time variable.</param>
        /// <param name="censorName">The name of the event indication variable.</param>
        [Obsolete("Please do not pass data to this constructor, and use the Learn method instead.")]
        public ProportionalHazardsAnalysis(double[][] inputs, double[] times, SurvivalOutcome[] censor,
            string[] inputNames, string timeName, string censorName)
            : this(inputs, times, censor)
        {
            this.inputNames = inputNames;
            this.timeName = timeName;
            this.censorName = censorName;
        }

        /// <summary>
        ///     Constructs a new Cox's Proportional Hazards Analysis.
        /// </summary>
        /// <param name="inputNames">The names of the input variables.</param>
        /// <param name="timeName">The name of the time variable.</param>
        /// <param name="censorName">The name of the event indication variable.</param>
        public ProportionalHazardsAnalysis(string[] inputNames, string timeName, string censorName)
            : this()
        {
            this.inputNames = inputNames;
            this.timeName = timeName;
            this.censorName = censorName;
        }

        /// <summary>
        ///     Constructs a new Cox's Proportional Hazards Analysis.
        /// </summary>
        public ProportionalHazardsAnalysis()
        {
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
        ///     Gets the time passed until the event
        ///     occurred or until the observation was
        ///     censored.
        /// </summary>
        public double[] TimeToEvent { get; private set; }

        /// <summary>
        ///     Gets whether the event of
        ///     interest happened or not.
        /// </summary>
        public SurvivalOutcome[] Events { get; private set; }

        /// <summary>
        ///     Gets the resulting probabilities obtained
        ///     by the logistic regression model.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[] Result => result;

        /// <summary>
        ///     Gets the Proportional Hazards model created
        ///     and evaluated by this analysis.
        /// </summary>
        public ProportionalHazards Regression { get; private set; }

        /// <summary>
        ///     Gets the collection of coefficients of the model.
        /// </summary>
        public ReadOnlyCollection<HazardCoefficient> Coefficients => coefficientCollection;

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
        public string[] InputNames
        {
            get => inputNames;
            set => inputNames = value;
        }

        /// <summary>
        ///     Gets or sets the name of the output variable for the model.
        /// </summary>
        public string TimeName
        {
            get => timeName;
            set => timeName = value;
        }

        /// <summary>
        ///     Gets or sets the name of event occurrence variable in the model.
        /// </summary>
        public string EventName
        {
            get => censorName;
            set => censorName = value;
        }

        /// <summary>
        ///     Gets the Hazard Ratio for each coefficient
        ///     found during the proportional hazards.
        /// </summary>
        public double[] HazardRatios { get; private set; }

        /// <summary>
        ///     Gets the Standard Error for each coefficient
        ///     found during the proportional hazards.
        /// </summary>
        public double[] StandardErrors { get; private set; }

        /// <summary>
        ///     Gets the Wald Tests for each coefficient.
        /// </summary>
        public WaldTest[] WaldTests { get; private set; }

        /// <summary>
        ///     Gets the Likelihood-Ratio Tests for each coefficient.
        /// </summary>
        public ChiSquareTest[] LikelihoodRatioTests
        {
            get
            {
                if (!innerComputed)
                {
                    computeInner();
                }

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
        ///     Source data used in the analysis.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Source => source;

        /// <summary>
        ///     Gets the dependent variable value
        ///     for each of the source input points.
        /// </summary>
        public double[] Outputs => Matrix.To<double[]>(Events);

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
        public ProportionalHazards Learn(Tuple<double[], double>[] x, int[] y, double[] weights = null)
        {
            var learning = createLearner(Regression);
            Regression = learning.Learn(x, y, weights);
            initialize(x.Apply(a => a.Item1), x.Apply(a => a.Item2), Matrix.To<SurvivalOutcome[]>(y));
            return store();
        }

        private void initialize(double[][] inputs, double[] outputs, SurvivalOutcome[] censor)
        {
            inputCount = inputs[0].Length;
            var coefficientCount = inputCount;

            // Store data sets
            inputData = inputs;
            TimeToEvent = outputs;
            Events = censor;

            // Create additional structures
            CoefficientValues = new double[coefficientCount];
            WaldTests = new WaldTest[coefficientCount];
            StandardErrors = new double[coefficientCount];
            HazardRatios = new double[coefficientCount];
            Confidences = new DoubleRange[coefficientCount];
            ratioTests = new ChiSquareTest[coefficientCount];

            if (timeName == null)
            {
                timeName = "Time";
            }

            if (censorName == null)
            {
                censorName = "Outcome";
            }

            if (inputNames == null)
            {
                inputNames = new string[inputCount];
                for (var i = 0; i < inputNames.Length; i++)
                {
                    inputNames[i] = "Input " + i;
                }
            }

            // Create object-oriented structure to represent the analysis
            var logCoefs = new List<HazardCoefficient>(coefficientCount);
            for (var i = 0; i < coefficientCount; i++)
            {
                logCoefs.Add(new HazardCoefficient(this, i));
            }

            coefficientCollection = new HazardCoefficientCollection(logCoefs);

            source = inputs.ToMatrix();
        }

        /// <summary>
        ///     Gets the Log-Likelihood Ratio between this model and another model.
        /// </summary>
        /// <param name="model">Another proportional hazards model.</param>
        /// <returns>The Likelihood-Ratio between the two models.</returns>
        public double GetLikelihoodRatio(ProportionalHazards model)
        {
            return Regression.GetLogLikelihoodRatio(inputData, TimeToEvent, Events, model);
        }

        /// <summary>
        ///     Computes the Proportional Hazards Analysis for an already computed regression.
        /// </summary>
        [Obsolete("Please use Learn(x, y) instead.")]
        public void Compute(ProportionalHazards regression)
        {
            this.Regression = regression;

            computeInformation();

            innerComputed = false;
        }

        /// <summary>
        ///     Computes the Proportional Hazards Analysis.
        /// </summary>
        /// <returns>
        ///     True if the model converged, false otherwise.
        /// </returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public bool Compute()
        {
            return compute();
        }

        /// <summary>
        ///     Computes the Proportional Hazards Analysis.
        /// </summary>
        /// <param name="limit">
        ///     The difference between two iterations of the regression algorithm
        ///     when the algorithm should stop. If not specified, the value of
        ///     1e-4 will be used. The difference is calculated based on the largest
        ///     absolute parameter change of the regression.
        /// </param>
        /// <param name="maxIterations">
        ///     The maximum number of iterations to be performed by the regression
        ///     algorithm.
        /// </param>
        /// <returns>
        ///     True if the model converged, false otherwise.
        /// </returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public bool Compute(double limit = 1e-4, int maxIterations = 50)
        {
            iterations = maxIterations;
            tolerance = limit;

            return compute();
        }

        private bool compute()
        {
            var learning = new ProportionalHazardsNewtonRaphson(Regression);

            Array.Clear(Regression.Coefficients, 0, Regression.Coefficients.Length);


            learning.MaxIterations = Iterations;
            learning.Tolerance = Tolerance;

            learning.Learn(inputData, TimeToEvent, Events);

            // Check if the full model has converged
            var converged = learning.CurrentIteration < Iterations;


            computeInformation();

            innerComputed = false;

            // Returns true if the full model has converged, false otherwise.
            return converged;
        }

        private void computeInner()
        {
            if (inputCount <= 2)
            {
                return;
            }

            // Perform likelihood-ratio tests against diminished nested models
            var innerModel = new ProportionalHazards(inputCount - 1);
            var learning = createLearner(innerModel);

            for (var i = 0; i < inputCount; i++)
            {
                // Create a diminished inner model without the current variable
                var data = inputData.RemoveColumn(i);

#if DEBUG
                if (data[0].Length == 0)
                {
                    throw new Exception();
                }
#endif

                Array.Clear(innerModel.Coefficients, 0, inputCount - 1);

                learning.MaxIterations = Iterations;
                learning.Tolerance = Tolerance;

                learning.Learn(data, TimeToEvent, Events);


                var ratio = 2.0 * (LogLikelihood - innerModel.GetPartialLogLikelihood(data, TimeToEvent, Events));
                ratioTests[i] = new ChiSquareTest(ratio, 1);
            }

            innerComputed = true;
        }

        private void computeInformation()
        {
            // Store model information
#pragma warning disable 612, 618
            result = Regression.Compute(inputData, TimeToEvent);
#pragma warning restore 612, 618
            Deviance = Regression.GetDeviance(inputData, TimeToEvent, Events);
            LogLikelihood = Regression.GetPartialLogLikelihood(inputData, TimeToEvent, Events);
            ChiSquare = Regression.ChiSquare(inputData, TimeToEvent, Events);

            // Store coefficient information
            for (var i = 0; i < Regression.Coefficients.Length; i++)
            {
                StandardErrors[i] = Regression.StandardErrors[i];

                WaldTests[i] = Regression.GetWaldTest(i);
                CoefficientValues[i] = Regression.Coefficients[i];
                Confidences[i] = Regression.GetConfidenceInterval(i);
                HazardRatios[i] = Regression.GetHazardRatio(i);
            }
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="inputs">The model inputs.</param>
        /// <param name="censor">The output (event) associated with each input vector.</param>
        /// <param name="time">The time-to-event for the non-censored training samples.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="censor" /> given <paramref name="inputs" /> and
        ///     <paramref name="time" />.
        /// </returns>
        public ProportionalHazards Learn(double[][] inputs, double[] time, SurvivalOutcome[] censor,
            double[] weights = null)
        {
            var learning = createLearner(Regression);
            Regression = learning.Learn(inputs, time, censor, weights);
            initialize(inputs, time, censor);
            return store();
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="inputs">The model inputs.</param>
        /// <param name="censor">The output (event) associated with each input vector.</param>
        /// <param name="time">The time-to-event for the non-censored training samples.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="censor" /> given <paramref name="inputs" /> and
        ///     <paramref name="time" />.
        /// </returns>
        public ProportionalHazards Learn(double[][] inputs, double[] time, int[] censor, double[] weights = null)
        {
            var learning = createLearner(Regression);
            Regression = learning.Learn(inputs, time, censor, weights);
            initialize(inputs, time, Matrix.To<SurvivalOutcome[]>(censor));
            return store();
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
        public ProportionalHazards Learn(Tuple<double[], double>[] x, SurvivalOutcome[] y, double[] weights = null)
        {
            var learning = createLearner(Regression);
            Regression = learning.Learn(x, y, weights);
            initialize(x.Apply(a => a.Item1), x.Apply(a => a.Item2), y);
            return store();
        }

        private ProportionalHazardsNewtonRaphson createLearner(ProportionalHazards model)
        {
            return new ProportionalHazardsNewtonRaphson
            {
                Model = model,
                MaxIterations = Iterations,
                Tolerance = Tolerance,
                Token = Token
            };
        }

        private ProportionalHazards store()
        {
            computeInformation();
            innerComputed = false;
            return Regression;
        }
    }

    #region Support Classes

    /// <summary>
    ///     Represents a Proportional Hazards Coefficient found in the Cox's Hazards model,
    ///     allowing it to be bound to controls like the DataGridView. This class cannot
    ///     be instantiated outside the <see cref="LogisticRegressionAnalysis" />.
    /// </summary>
    [Serializable]
    public class HazardCoefficient
    {
        private ProportionalHazardsAnalysis analysis;
        private int index;

        internal HazardCoefficient(ProportionalHazardsAnalysis analysis, int index)
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
                if (analysis.InputNames.Length == 0)
                {
                    return string.Empty;
                }

                return analysis.InputNames[index];
            }
        }

        /// <summary>
        ///     Gets the Odds ratio for the current coefficient.
        /// </summary>
        [DisplayName("Hazard ratio")]
        public double HazardRatio => analysis.HazardRatios[index];

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
        public ChiSquareTest LikelihoodRatio => analysis.LikelihoodRatioTests[index];
    }

    /// <summary>
    ///     Represents a collection of Hazard Coefficients found in the
    ///     <see cref="ProportionalHazardsAnalysis" />. This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public class HazardCoefficientCollection : ReadOnlyCollection<HazardCoefficient>
    {
        internal HazardCoefficientCollection(IList<HazardCoefficient> coefficients)
            : base(coefficients)
        {
        }
    }

    #endregion
}