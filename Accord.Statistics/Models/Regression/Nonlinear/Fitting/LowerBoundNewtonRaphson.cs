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
using Accord.MachineLearning;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Properties;

namespace Accord.Statistics.Models.Regression.Fitting
{
    /// <summary>
    ///     Lower-Bound Newton-Raphson for Multinomial logistic regression fitting.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Lower Bound principle consists of replacing the second derivative
    ///         matrix by a global lower bound in the Leowner ordering [Böhning, 92].
    ///         In the case of multinomial logistic regression estimation, the Hessian
    ///         of the negative log-likelihood function can be replaced by one of those
    ///         lower bounds, leading to a monotonically converging sequence of iterates.
    ///         Furthermore, [Krishnapuram, Carin, Figueiredo and Hartemink, 2005] also
    ///         have shown that a lower bound can be achieved which does not depend on
    ///         the coefficients for the current iteration.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     B. Krishnapuram, L. Carin, M.A.T. Figueiredo, A. Hartemink. Sparse Multinomial
    ///                     Logistic Regression: Fast Algorithms and Generalization Bounds. 2005. Available on:
    ///                     http://www.lx.it.pt/~mtf/Krishnapuram_Carin_Figueiredo_Hartemink_2005.pdf
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     D. Böhning. Multinomial logistic regression algorithm. Annals of the Institute
    ///                     of Statistical Mathematics, 44(9):197 ˝U200, 1992. 2. M. Corney.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Bishop, Christopher M.; Pattern Recognition and Machine Learning.
    ///                     Springer; 1st ed. 2006.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\MultinomialLogisticRegressionTest.cs"
    ///         region="doc_learn" />
    /// </example>
#pragma warning disable 612, 618
    public partial class LowerBoundNewtonRaphson :
        ISupervisedLearning<MultinomialLogisticRegression, double[], int>,
        ISupervisedLearning<MultinomialLogisticRegression, double[], int[]>,
        ISupervisedLearning<MultinomialLogisticRegression, double[], double[]>,
        IMultipleRegressionFitting, IConvergenceLearning
#pragma warning restore 612, 618
    {
        private readonly IConvergence<double> convergence;

        private int K;
        private int M;

        private ISolverMatrixDecomposition<double> decomposition;

        private double[] errors;

        private double[] output;

        private MultinomialLogisticRegression regression;

        private double[,] weights;
        private double[,] xxt;

        /// <summary>
        ///     Creates a new <see cref="LowerBoundNewtonRaphson" />.
        /// </summary>
        public LowerBoundNewtonRaphson()
        {
            convergence = new AbsoluteConvergence();
        }

        /// <summary>
        ///     Creates a new <see cref="LowerBoundNewtonRaphson" />.
        /// </summary>
        /// <param name="regression">The regression to estimate.</param>
        public LowerBoundNewtonRaphson(MultinomialLogisticRegression regression)
            : this()
        {
            init(regression);
        }

        /// <summary>
        ///     Gets the previous values for the coefficients which were
        ///     in place before the last learning iteration was performed.
        /// </summary>
        public double[] Previous { get; private set; }

        /// <summary>
        ///     Gets the current values for the coefficients.
        /// </summary>
        public double[] Solution { get; private set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the lower bound
        ///     should be updated using new data. Default is <c>true</c>.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the lower bound should be
        ///     updated; otherwise, <c>false</c>.
        /// </value>
        public bool UpdateLowerBound { get; set; } = true;

        /// <summary>
        ///     Gets the Lower-Bound matrix being used in place of
        ///     the Hessian matrix in the Newton-Raphson iterations.
        /// </summary>
        public double[,] HessianLowerBound { get; private set; }

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
        ///     Gets or sets a value indicating whether standard
        ///     errors should be computed in the next iteration.
        /// </summary>
        /// <value>
        ///     <c>true</c> to compute standard errors; otherwise, <c>false</c>.
        /// </value>
        public bool ComputeStandardErrors { get; set; } = true;

        /// <summary>
        ///     Gets the vector of parameter updates in the last iteration.
        /// </summary>
        /// <value>How much each parameter changed after the last update.</value>
        public double[] ParameterChange { get; private set; }

        /// <summary>
        ///     Gets the maximum <see cref="ParameterChange">parameter change</see> in the last
        ///     iteration. If this value is less than <see cref="Tolerance" />, the algorithm
        ///     has converged.
        /// </summary>
        public double MaximumChange => convergence.NewValue;

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
        ///     Gets or sets the tolerance value used to determine
        ///     whether the algorithm has converged.
        /// </summary>
        public double Tolerance
        {
            get => convergence.Tolerance;
            set => convergence.Tolerance = value;
        }

        /// <summary>
        ///     Gets or sets the number of performed iterations.
        /// </summary>
        public int CurrentIteration => convergence.CurrentIteration;

        /// <summary>
        ///     Gets or sets whether the algorithm has converged.
        /// </summary>
        /// <value><c>true</c> if this instance has converged; otherwise, <c>false</c>.</value>
        public bool HasConverged => convergence.HasConverged;

        /// <summary>
        ///     Runs one iteration of the Lower-Bound Newton-Raphson iteration.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The outputs associated with each input vector.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use the Learn() method instead.")]
        public double Run(double[][] inputs, double[][] outputs)
        {
            var old = Iterations;
            Iterations = 1;
            Learn(inputs, outputs);
            Iterations = old;
            return ParameterChange.Max();
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
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            // Regress using Lower-Bound Newton-Raphson estimation
            //
            // The main idea is to replace the Hessian matrix with a 
            //   suitable lower bound. Indeed, the Hessian is lower
            //   bounded by a negative definite matrix that does not
            //   even depend on w [Krishnapuram et al].
            //
            //   - http://www.lx.it.pt/~mtf/Krishnapuram_Carin_Figueiredo_Hartemink_2005.pdf
            //

            if (regression == null)
            {
                init(new MultinomialLogisticRegression(x.Columns(), y.Columns()));
            }

            // Initial definitions and memory allocations
            var design = x.InsertColumn(1, 0);
            var coefficients = regression.Coefficients;

            // In the multinomial logistic regression, the objective
            // function is the log-likelihood function l(w). As given
            // by Krishnapuram et al and Böhning, this is a concave 
            // function with Hessian given by:
            //
            //       H(w) = -sum(P(w) - p(w)p(w)')  (x)  xx'
            //      (see referenced paper for proper indices)
            //       
            // In which (x) denotes the Kronecker product. By using
            // the lower bound principle, Krishnapuram has shown that
            // we can replace H(w) with a lower bound approximation B
            // which does not depend on w (eq. 8 on aforementioned paper):
            // 
            //      B = -(1/2) [I - 11/M]  (x)  sum(xx')
            //
            // Thus we can compute and invert this matrix only once.
            //

            do
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }

                accumulateGradientAndLowerBound(x, y, design);

                solveForDeltas();

                updateCoefficients(coefficients);

                if (Token.IsCancellationRequested)
                {
                    break;
                }
            } while (!convergence.HasConverged);

            if (ComputeStandardErrors)
            {
                computeStandardErrorsFromInverse();
            }

            return regression;
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
        public MultinomialLogisticRegression Learn(double[][] x, int[][] y, double[] weights = null)
        {
            return Learn(x, y.ToDouble(), weights);
        }

        private void init(MultinomialLogisticRegression regression)
        {
            this.regression = regression;

            K = regression.NumberOfOutputs - 1;
            M = regression.NumberOfInputs + 1;
            Parameters = K * M;

            Solution = regression.Coefficients.Reshape();

            xxt = new double[M, M];
            errors = new double[K];
            output = new double[K];

            HessianLowerBound = new double[Parameters, Parameters];
            Gradient = new double[Parameters];

            // Differently from the IRLS iteration, the weight matrix can be fixed
            // as it does not depend on the current coefficients anymore [I - 11/m]

            // TODO: Avoid the multiple allocations in the line below
            weights = (-0.5).Multiply(Matrix.Identity(K).Subtract(Matrix.Create(K, K, 1.0 / M)));
        }

        /// <summary>
        ///     Runs one iteration of the Lower-Bound Newton-Raphson iteration.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="classes">The outputs associated with each input vector.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use the Learn() method instead.")]
        public double Run(double[][] inputs, int[] classes)
        {
            var old = Iterations;
            Iterations = 1;
            Learn(inputs, classes);
            Iterations = old;
            return ParameterChange.Max();
        }

        private void compute(double[] x, double[] responses)
        {
            var coefficients = regression.Coefficients;

            double sum = 1;

            // For each category (except the first)
            for (var j = 0; j < coefficients.Length; j++)
            {
                // Get category coefficients
                var c = coefficients[j];

                var logit = c[0]; // intercept

                for (var i = 0; i < x.Length; i++)
                {
                    logit += c[i + 1] * x[i];
                }

                sum += responses[j] = System.Math.Exp(logit);
            }

            // Normalize the probabilities
            for (var j = 0; j < responses.Length; j++)
            {
                responses[j] /= sum;
            }
        }
    }
}