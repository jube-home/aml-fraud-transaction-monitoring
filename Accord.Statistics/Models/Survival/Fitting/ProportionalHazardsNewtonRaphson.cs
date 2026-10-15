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
using System.Threading;
using Accord.Diagnostics;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Properties;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Models.Regression.Fitting
{
    /// <summary>
    ///     Newton-Raphson learning updates for Cox's Proportional Hazards models.
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\CoxProportionalHazardsTest.cs"
    ///         region="doc_learn" />
    /// </example>
    /// <seealso cref="ProportionalHazards" />
    /// <seealso cref="Analysis.ProportionalHazardsAnalysis" />
#pragma warning disable 612, 618
    public partial class ProportionalHazardsNewtonRaphson :
        ISupervisedLearning<ProportionalHazards, Tuple<double[], double>, int>,
        ISupervisedLearning<ProportionalHazards, Tuple<double[], double>, bool>,
        ISurvivalFitting, IConvergenceLearning
#pragma warning disable 612, 618
    {
        private readonly RelativeParameterConvergence convergence;

        private ISolverMatrixDecomposition<double> decomposition;

        private double[] partialGradient;
        private double[,] partialHessian;

        private ProportionalHazards regression;

        /// <summary>
        ///     Constructs a new Newton-Raphson learning algorithm
        ///     for Cox's Proportional Hazards models.
        /// </summary>
        public ProportionalHazardsNewtonRaphson()
        {
            convergence = new RelativeParameterConvergence
            {
                Iterations = 0,
                Tolerance = 1e-5
            };

            Estimator = HazardEstimator.BreslowNelsonAalen;
            Ties = HazardTiesMethod.Efron;
            Lambda = 0.1;
            Token = new CancellationToken();
        }

        /// <summary>
        ///     Constructs a new Newton-Raphson learning algorithm
        ///     for Cox's Proportional Hazards models.
        /// </summary>
        /// <param name="hazards">The model to estimate.</param>
        public ProportionalHazardsNewtonRaphson(ProportionalHazards hazards)
            : this()
        {
            init(hazards);
        }

        /// <summary>
        ///     Gets or sets the hazard estimator that should be used by the
        ///     proportional hazards learning algorithm. Default is to use
        ///     <see cref="HazardEstimator.BreslowNelsonAalen" />.
        /// </summary>
        public HazardEstimator Estimator { get; set; }

        /// <summary>
        ///     Gets or sets the ties handling method to be used by the
        ///     proportional hazards learning algorithm. Default is to use
        ///     <see cref="HazardTiesMethod.Efron" />'s method.
        /// </summary>
        public HazardTiesMethod Ties { get; set; }

        /// <summary>
        ///     Gets the previous values for the coefficients which were
        ///     in place before the last learning iteration was performed.
        /// </summary>
        public double[] Previous => convergence.OldValues;

        /// <summary>
        ///     Gets the current values for the coefficients.
        /// </summary>
        public double[] Solution => regression.Coefficients;

        /// <summary>
        ///     Gets the Hessian matrix computed in
        ///     the last Newton-Raphson iteration.
        /// </summary>
        public double[,] Hessian { get; private set; }

        /// <summary>
        ///     Gets the Gradient vector computed in
        ///     the last Newton-Raphson iteration.
        /// </summary>
        public double[] Gradient { get; private set; }

        /// <summary>
        ///     Gets the total number of parameters in the model.
        /// </summary>
        public int Parameters { get; private set; }

        /// <summary>
        ///     Gets or sets the regression model being learned.
        /// </summary>
        public ProportionalHazards Model
        {
            get => regression;
            set
            {
                regression = value;
                if (regression != null)
                {
                    init(regression);
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether standard
        ///     errors should be computed at the end of the next
        ///     iterations.
        /// </summary>
        /// <value>
        ///     <c>true</c> to compute standard errors; otherwise, <c>false</c>.
        /// </value>
        public bool ComputeStandardErrors { get; set; } = true;

        /// <summary>
        ///     Gets or sets a value indicating whether an estimate
        ///     of the baseline hazard function should be computed
        ///     at the end of the next iterations.
        /// </summary>
        /// <value>
        ///     <c>true</c> to compute the baseline function; otherwise, <c>false</c>.
        /// </value>
        public bool ComputeBaselineFunction { get; set; } = true;

        /// <summary>
        ///     Gets or sets a value indicating whether the Cox model should
        ///     be computed using the mean-centered version of the covariates.
        ///     Default is true.
        /// </summary>
        public bool Normalize { get; set; } = true;

        /// <summary>
        ///     Gets or sets the smoothing factor used to avoid numerical
        ///     problems in the beginning of the training. Default is 0.1.
        /// </summary>
        public double Lambda { get; set; }

        /// <summary>
        ///     Gets or sets the maximum absolute parameter change detectable
        ///     after an iteration of the algorithm used to detect convergence.
        ///     Default is 1e-5.
        /// </summary>
        public double Tolerance
        {
            get => convergence.Tolerance;
            set => convergence.Tolerance = value;
        }

        /// <summary>
        ///     Please use MaxIterations instead.
        /// </summary>
        [Obsolete("Please use MaxIterations instead.")]
        public int Iterations
        {
            get => convergence.MaxIterations;
            set => convergence.MaxIterations = value;
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     performed by the learning algorithm.
        /// </summary>
        public int MaxIterations
        {
            get => convergence.MaxIterations;
            set => convergence.MaxIterations = value;
        }

        /// <summary>
        ///     Gets or sets the number of performed iterations.
        /// </summary>
        public int CurrentIteration
        {
            get => convergence.CurrentIteration;
            set => convergence.CurrentIteration = value;
        }

        /// <summary>
        ///     Gets or sets whether the algorithm has converged.
        /// </summary>
        /// <value><c>true</c> if this instance has converged; otherwise, <c>false</c>.</value>
        public bool HasConverged => convergence.HasConverged;

        /// <summary>
        ///     Learns a model that can map the given inputs to the given outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="y">The desired outputs associated with each <paramref name="x">inputs</paramref>.</param>
        /// <param name="weights">The weight of importance for each input-output pair (if supported by the learning algorithm).</param>
        /// <returns>
        ///     A model that has learned how to produce <paramref name="y" /> given <paramref name="x" />.
        /// </returns>
        public ProportionalHazards Learn(Tuple<double[], double>[] x, bool[] y, double[] weights = null)
        {
            var inputs = x.Apply(x_i => x_i.Item1);
            var time = x.Apply(x_i => x_i.Item2);
            return Learn(inputs, time, Matrix.To<SurvivalOutcome[]>(y), weights);
        }

        /// <summary>
        ///     Gets or sets a cancellation token that can be used to
        ///     stop the learning algorithm while it is running.
        /// </summary>
        [field: NonSerialized]
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
        public ProportionalHazards Learn(Tuple<double[], double>[] x, int[] y, double[] weights = null)
        {
            var inputs = x.Apply(x_i => x_i.Item1);
            var time = x.Apply(x_i => x_i.Item2);
            return Learn(inputs, time, Matrix.To<SurvivalOutcome[]>(y), weights);
        }

        /// <summary>
        ///     Runs the Newton-Raphson update for Cox's hazards learning until convergence.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="time">The time-to-event for the training samples.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[][] inputs, double[] time)
        {
            var censor = new SurvivalOutcome[time.Length];

            Debug.Assert(censor[0] == SurvivalOutcome.Failed);

            return Run(inputs, time, censor);
        }

        /// <summary>
        ///     Runs the Newton-Raphson update for Cox's hazards learning until convergence.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="censor">The output (event) associated with each input vector.</param>
        /// <param name="time">The time-to-event for the non-censored training samples.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[][] inputs, double[] time, int[] censor)
        {
            return Run(inputs, time, Matrix.To<SurvivalOutcome[]>(censor));
        }

        /// <summary>
        ///     Runs the Newton-Raphson update for Cox's hazards learning until convergence.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="censor">The output (event) associated with each input vector.</param>
        /// <param name="time">The time-to-event for the non-censored training samples.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[][] inputs, double[] time, SurvivalOutcome[] censor)
        {
            innerLearn(inputs, time, censor, null);
            return regression.GetPartialLogLikelihood(inputs, time, censor);
        }

        private void init(ProportionalHazards hazards)
        {
            regression = hazards;
            Parameters = hazards.Coefficients.Length;

            Hessian = new double[Parameters, Parameters];
            Gradient = new double[Parameters];

            partialHessian = new double[Parameters, Parameters];
            partialGradient = new double[Parameters];
        }

        /// <summary>
        ///     Runs the Newton-Raphson update for Cox's hazards learning until convergence.
        /// </summary>
        /// <param name="censor">The output (event) associated with each input vector.</param>
        /// <param name="time">The time-to-event for the non-censored training samples.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[] time, int[] censor)
        {
            return Run(time, Matrix.To<SurvivalOutcome[]>(censor));
        }

        /// <summary>
        ///     Runs the Newton-Raphson update for Cox's hazards learning until convergence.
        /// </summary>
        /// <param name="censor">The output (event) associated with each input vector.</param>
        /// <param name="time">The time-to-event for the non-censored training samples.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[] time, SurvivalOutcome[] censor)
        {
            if (time.Length != censor.Length)
            {
                throw new DimensionMismatchException("time",
                    "The time and output vector must have the same length.");
            }

            // Sort data by time to accelerate performance
            EmpiricalHazardDistribution.Sort(ref time, ref censor);

            createBaseline(time, censor);

            return regression.GetPartialLogLikelihood(time, censor);
        }

        private void createBaseline(double[] time, SurvivalOutcome[] censor, double[] output = null)
        {
            if (regression.BaselineHazard == null)
            {
                return;
            }

            var hazard = regression.BaselineHazard as IFittableDistribution<double, EmpiricalHazardOptions>;
            if (hazard != null)
            {
                // Compute an estimate of the cumulative Hazard
                //   function using the Nelson-Aalen estimator
                hazard.Fit(time, output, new EmpiricalHazardOptions
                {
                    Outcome = censor,
                    Estimator = Estimator,
                    Ties = Ties
                });
                return;
            }

            var survival = regression.BaselineHazard as IFittableDistribution<double, SurvivalOptions>;
            if (survival != null)
            {
                // Compute an estimate of the cumulative Hazard
                //   function using the Kaplan-Meier estimator
                survival.Fit(time, new SurvivalOptions
                {
                    Outcome = censor
                });
            }
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
            var inputs = x.Apply(x_i => x_i.Item1);
            var time = x.Apply(x_i => x_i.Item2);
            var censor = y;
            return Learn(inputs, time, censor, weights);
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
            return Learn(inputs, time, Matrix.To<SurvivalOutcome[]>(censor), weights);
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
            var regression = innerLearn(inputs, time, censor, weights);

            // Disable deprecated mechanism
            regression.Intercept = -regression.Coefficients.Dot(regression.Offsets);
            regression.Offsets.Clear();
            return regression;
        }

        private ProportionalHazards innerLearn(double[][] inputs, double[] time, SurvivalOutcome[] censor,
            double[] weights)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            if (inputs.Length != time.Length || time.Length != censor.Length)
            {
                throw new DimensionMismatchException("time",
                    "The inputs, time and output vector must have the same length.");
            }

            if (regression == null)
            {
                init(new ProportionalHazards(inputs.Columns()));
            }

            // Sort data by time to accelerate performance
            EmpiricalHazardDistribution.Sort(ref time, ref censor, ref inputs);


            var sdev = new double[Parameters];
            for (var i = 0; i < sdev.Length; i++)
            {
                sdev[i] = 1;
            }

            if (Normalize)
            {
                standardizeInputs(ref inputs, out sdev);
            }

            // Compute actual outputs
            var output = new double[inputs.Length];
            computeOutputs(inputs, output);

            if (Parameters == 0)
            {
                createBaseline(time, censor, output);
                return regression;
            }

            if (!iterate(inputs, time, censor, output))
            {
                return regression;
            }


            for (var i = 0; i < regression.Coefficients.Length; i++)
            {
                regression.Coefficients[i] /= sdev[i];
            }

            if (ComputeStandardErrors)
            {
                computeStandardErrorsFromHessian(sdev);
            }

            if (ComputeBaselineFunction)
            {
                createBaseline(time, censor, output);
            }

            return regression;
        }
    }
}