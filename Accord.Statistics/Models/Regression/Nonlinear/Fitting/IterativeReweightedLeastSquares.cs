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

namespace Accord.Statistics.Models.Regression.Fitting
{
    /// <summary>
    ///     Iterative Reweighted Least Squares for Logistic Regression fitting.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Iterative Reweighted Least Squares is an iterative technique based
    ///         on the Newton-Raphson iterative optimization scheme. The IRLS method uses
    ///         a local quadratic approximation to the log-likelihood function.
    ///     </para>
    ///     <para>
    ///         By applying the Newton-Raphson optimization scheme to the cross-entropy
    ///         error function (defined as the negative logarithm of the likelihood), one
    ///         arises at a weighted formulation for the Hessian matrix.
    ///     </para>
    ///     <para>
    ///         The Iterative Reweighted Least Squares algorithm can also be used to learn
    ///         arbitrary generalized linear models. However, the use of this class to learn
    ///         such models is currently experimental.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Bishop, Christopher M.; Pattern Recognition and Machine Learning.
    ///                     Springer; 1st ed. 2006.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Amos Storkey. (2005). Learning from Data: Learning Logistic Regressors. School of Informatics.
    ///                     Available on: http://www.inf.ed.ac.uk/teaching/courses/lfd/lectures/logisticlearn-print.pdf
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Cosma Shalizi. (2009). Logistic Regression and Newton's Method. Available on:
    ///                     http://www.stat.cmu.edu/~cshalizi/350/lectures/26/lecture-26.pdf
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Edward F. Conor. Logistic Regression. Website. Available on:
    ///                     http://userwww.sfsu.edu/~efc/classes/biol710/logistic/logisticreg.htm
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\LogisticRegressionTest.cs"
    ///         region="doc_log_reg_1" />
    /// </example>
#pragma warning disable 612, 618
    public class IterativeReweightedLeastSquares : IterativeReweightedLeastSquares<GeneralizedLinearRegression>,
        IRegressionFitting
#pragma warning restore 612, 618
    {
        /// <summary>
        ///     Constructs a new Iterative Reweighted Least Squares.
        /// </summary>
        public IterativeReweightedLeastSquares()
        {
        }

        /// <summary>
        ///     Constructs a new Iterative Reweighted Least Squares.
        /// </summary>
        /// <param name="regression">The regression to estimate.</param>
        public IterativeReweightedLeastSquares(LogisticRegression regression)
        {
            // TODO: Remove this method
            Initialize(regression);
        }

        /// <summary>
        ///     Constructs a new Iterative Reweighted Least Squares.
        /// </summary>
        /// <param name="regression">The regression to estimate.</param>
        public IterativeReweightedLeastSquares(GeneralizedLinearRegression regression)
        {
            Initialize(regression);
        }

        /// <summary>
        ///     Runs one iteration of the Reweighted Least Squares algorithm.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The outputs associated with each input vector.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[][] inputs, double[] outputs)
        {
            return Run(inputs, outputs, null);
        }

        /// <summary>
        ///     Runs one iteration of the Reweighted Least Squares algorithm.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The outputs associated with each input vector.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[][] inputs, int[] outputs)
        {
            return Run(inputs, outputs.Apply(x => x > 0 ? 1.0 : 0.0));
        }

        /// <summary>
        ///     Runs one iteration of the Reweighted Least Squares algorithm.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The outputs associated with each input vector.</param>
        /// <param name="weights">The weights associated with each sample.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[][] inputs, int[] outputs, double[] weights)
        {
            return Run(inputs, outputs.Apply(x => x > 0 ? 1.0 : 0.0), weights);
        }

        /// <summary>
        ///     Runs one iteration of the Reweighted Least Squares algorithm.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The outputs associated with each input vector.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[][] inputs, int[][] outputs)
        {
            if (outputs[0].Length != 1)
            {
                throw new ArgumentException("Function must have a single output.", "outputs");
            }

            var output = new double[outputs.Length];
            for (var i = 0; i < outputs.Length; i++)
            {
                output[i] = outputs[i][0] > 0 ? 1.0 : 0.0;
            }

            return Run(inputs, output);
        }

        /// <summary>
        ///     Runs one iteration of the Reweighted Least Squares algorithm.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The outputs associated with each input vector.</param>
        /// <param name="sampleWeight">The weight associated with each sample.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[][] inputs, int[][] outputs, double[] sampleWeight)
        {
            if (outputs[0].Length != 1)
            {
                throw new ArgumentException("Function must have a single output.", "outputs");
            }

            var output = new double[outputs.Length];
            for (var i = 0; i < outputs.Length; i++)
            {
                output[i] = outputs[i][0] > 0 ? 1.0 : 0.0;
            }

            return Run(inputs, output, sampleWeight);
        }

        /// <summary>
        ///     Runs one iteration of the Reweighted Least Squares algorithm.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The outputs associated with each input vector.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[][] inputs, double[][] outputs)
        {
            if (outputs[0].Length != 1)
            {
                throw new ArgumentException("Function must have a single output.", "outputs");
            }

            var output = new double[outputs.Length];
            for (var i = 0; i < outputs.Length; i++)
            {
                output[i] = outputs[i][0];
            }

            return Run(inputs, output);
        }

        /// <summary>
        ///     Runs one iteration of the Reweighted Least Squares algorithm.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The outputs associated with each input vector.</param>
        /// <param name="sampleWeights">An weight associated with each sample.</param>
        /// <returns>The maximum relative change in the parameters after the iteration.</returns>
        [Obsolete("Please use Learn(x, y) instead.")]
        public double Run(double[][] inputs, double[] outputs, double[] sampleWeights)
        {
            var old = Iterations;
            Iterations = 1;
            Learn(inputs, outputs, sampleWeights);
            Iterations = old;
            return Updates.Abs().Max();
        }

        /// <summary>
        ///     Computes the sum-of-squared error between the
        ///     model outputs and the expected outputs.
        /// </summary>
        /// <param name="inputs">The input data set.</param>
        /// <param name="outputs">The output values.</param>
        /// <returns>The sum-of-squared errors.</returns>
        [Obsolete("Please use the LogLikelihoodLoss class instead.")]
        public double ComputeError(double[][] inputs, double[] outputs)
        {
            double sum = 0;

            for (var i = 0; i < inputs.Length; i++)
            {
                var actual = Model.Probability(inputs[i]);
                var expected = outputs[i];
                var delta = actual - expected;
                sum += delta * delta;
            }

            return sum;
        }
    }

    /// <summary>
    ///     Iterative Reweighted Least Squares for fitting Generalized Linear Models.
    /// </summary>
    public class IterativeReweightedLeastSquares<TModel> :
        ISupervisedLearning<TModel, double[], double>,
        ISupervisedLearning<TModel, double[], int>,
        ISupervisedLearning<TModel, double[], bool>,
        IConvergenceLearning
        where TModel : GeneralizedLinearRegression, new()
    {
        private readonly RelativeConvergence convergence;

        private ISolverArrayDecomposition<double> decomposition;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IterativeReweightedLeastSquares{TModel}" /> class.
        /// </summary>
        public IterativeReweightedLeastSquares()
        {
            convergence = new RelativeConvergence
            {
                MaxIterations = 0,
                Tolerance = 1e-5
            };
        }

        /// <summary>
        ///     Gets or sets the regression model being learned.
        /// </summary>
        public TModel Model { get; set; }

        /// <summary>
        ///     Gets the previous values for the coefficients which were
        ///     in place before the last learning iteration was performed.
        /// </summary>
        public double[] Previous { get; private set; }

        /// <summary>
        ///     Gets the last parameter updates in the last iteration.
        /// </summary>
        public double[] Updates { get; private set; }

        /// <summary>
        ///     Gets the current values for the coefficients.
        /// </summary>
        public double[] Solution => Model.Intercept.Concatenate(Model.Weights);

        /// <summary>
        ///     Gets the Hessian matrix computed in
        ///     the last Newton-Raphson iteration.
        /// </summary>
        public double[][] Hessian { get; private set; }

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
        ///     Gets or sets the regularization value to be
        ///     added in the objective function. Default is
        ///     1e-10.
        /// </summary>
        public double Regularization { get; set; } = 1e-10;

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
        ///     Gets or sets the tolerance value used to determine
        ///     whether the algorithm has converged.
        /// </summary>
        public double Tolerance
        {
            get => convergence.Tolerance;
            set => convergence.Tolerance = value;
        }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     performed by the learning algorithm.
        /// </summary>
        /// <value>The maximum iterations.</value>
        public int MaxIterations
        {
            get => convergence.MaxIterations;
            set => convergence.MaxIterations = value;
        }

        /// <summary>
        ///     Gets the current iteration number.
        /// </summary>
        /// <value>The current iteration.</value>
        public int CurrentIteration => convergence.CurrentIteration;

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
        public TModel Learn(double[][] x, bool[] y, double[] weights = null)
        {
            return Learn(x, y.ToZeroOne(), weights);
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
        /// <exception cref="DimensionMismatchException">
        ///     outputs;The number of input vectors and their associated output values
        ///     must have the same size.
        /// </exception>
        public TModel Learn(double[][] x, double[] y, double[] weights = null)
        {
            // Regress using Iteratively Reweighted Least Squares estimation.

            // References:
            //  - Bishop, Christopher M.; Pattern Recognition 
            //    and Machine Learning. Springer; 1st ed. 2006.

            if (x.Length != y.Length)
            {
                throw new DimensionMismatchException("y",
                    "The number of input vectors and their associated output values must have the same size.");
            }

            if (Model == null)
            {
                Initialize(new TModel
                {
                    NumberOfInputs = x.Columns()
                });
            }

            // Initial definitions and memory allocations
            var N = x.Length;

            var errors = new double[N];
            var w = new double[N];
            convergence.Clear();

            var design = x.InsertColumn(1, 0);

            do
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }

                // Compute errors and weighting matrix
                for (var i = 0; i < x.Length; i++)
                {
                    var z = Model.Linear.Transform(x[i]);
                    var actual = Model.Link.Inverse(z);

                    // Calculate error vector
                    errors[i] = actual - y[i];

                    // Calculate weighting matrix
                    w[i] = Model.Link.Derivative2(actual);
                }

                if (weights != null)
                {
                    for (var i = 0; i < weights.Length; i++)
                    {
                        errors[i] *= weights[i];
                        w[i] *= weights[i];
                    }
                }

                // Reset Hessian matrix and gradient
                Gradient.Clear();
                Hessian.Clear();

                // (Re-) Compute error gradient
                for (var j = 0; j < design.Length; j++)
                {
                    for (var i = 0; i < Gradient.Length; i++)
                    {
                        Gradient[i] += design[j][i] * errors[j];
                    }
                }

                // (Re-) Compute weighted "Hessian" matrix 
                for (var k = 0; k < w.Length; k++)
                {
                    var row = design[k];
                    for (var j = 0; j < row.Length; j++)
                    {
                        for (var i = 0; i < row.Length; i++)
                        {
                            Hessian[j][i] += row[i] * row[j] * w[k];
                        }
                    }
                }

                // Apply L2 regularization
                if (Regularization > 0)
                {
                    // https://www.cs.ubc.ca/~murphyk/Teaching/CS540-Fall08/L6.pdf
                    for (var i = 0; i < Gradient.Length; i++)
                    {
                        Gradient[i] += Regularization * Model.GetCoefficient(i);
                        Hessian[i][i] += Regularization;
                    }
                }

                decomposition = new JaggedSingularValueDecomposition(Hessian);
                Updates = decomposition.Solve(Gradient);

                Previous = (double[])Solution.Clone();

                // Update coefficients using the calculated deltas
                for (var i = 0; i < Model.Weights.Length; i++)
                {
                    Model.Weights[i] -= Updates[i + 1];
                }

                Model.Intercept -= Updates[0];

                // Return the relative maximum parameter change
                convergence.NewValue = Updates.Abs().Max();

                if (Token.IsCancellationRequested)
                {
                    break;
                }
            } while (!convergence.HasConverged);

            if (ComputeStandardErrors)
            {
                // Grab the regression information matrix
                var inverse = decomposition.Inverse();

                // Calculate coefficients' standard errors
                var standardErrors = Model.StandardErrors;
                for (var i = 0; i < standardErrors.Length; i++)
                {
                    standardErrors[i] = System.Math.Sqrt(inverse[i][i]);
                }
            }

            return Model;
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
        public TModel Learn(double[][] x, int[] y, double[] weights = null)
        {
            return Learn(x, y.ToDouble(), weights);
        }

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected void Initialize(TModel regression)
        {
            if (regression == null)
            {
                throw new ArgumentNullException("regression");
            }

            this.Model = regression;
            Parameters = regression.NumberOfParameters;
            Hessian = Jagged.Zeros(Parameters, Parameters);
            Gradient = new double[Parameters];
            Previous = new double[Parameters];
        }

        /// <summary>
        ///     Gets the information matrix used to update the regression
        ///     weights in the last call to <see cref="Learn(double[][], double[], double[])" />
        /// </summary>
        public double[][] GetInformationMatrix()
        {
            return decomposition.Inverse();
        }
    }
}