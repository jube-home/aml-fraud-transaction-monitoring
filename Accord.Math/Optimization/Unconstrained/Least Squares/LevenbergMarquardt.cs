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
using System.Threading.Tasks;
using Accord.Math.Decompositions;
using Accord.Math.Differentiation;
using Accord.Statistics.Models;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Levenberg-Marquardt algorithm for solving Least-Squares problems.
    /// </summary>
    /// <example>
    ///     <para>
    ///         While it is possible to use the <see cref="LevenbergMarquardt" /> class as a standalone
    ///         method for solving least squares problems, this class is intended to be used as
    ///         a strategy for NonlinearLestSquares, as shown in the example below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\NonlinearLeastSquaresTest.cs"
    ///         region="doc_learn_lm" lang="cs" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics.VB\Models\Regression\NonlinearLeastSquaresTest.vb"
    ///         region="doc_learn_lm" lang="vb" />
    ///     <para>
    ///         However, as mentioned above it is also possible to use <see cref="LevenbergMarquardt" />
    ///         as a standalone class, as shown in the example below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\LevenbergMarquardtTest.cs" region="doc_minimize" />
    /// </example>
    /// <seealso cref="GaussNewton" />
    /// <seealso cref="FiniteDifferences" />
    public class LevenbergMarquardt : BaseLeastSquaresMethod, ILeastSquaresMethod, IConvergenceLearning
    {
        private const double lambdaMax = 1e25;
        private readonly int outputCount = 1;

        private JaggedCholeskyDecomposition decomposition;
        private double[] deltas;

        private double[] diagonal;
        private double[] errors;
        private double[] gradient;

        // Levenberg-Marquardt variables
        private double[][] jacobian;

        // Levenberg damping factor

        // The amount the damping factor is adjusted
        // when searching the minimum error surface
        private double[] weights;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LevenbergMarquardt" /> class.
        /// </summary>
        public LevenbergMarquardt()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LevenbergMarquardt" /> class.
        /// </summary>
        /// <param name="parameters">The number of free parameters in the optimization problem.</param>
        public LevenbergMarquardt(int parameters)
            : this()
        {
            NumberOfParameters = parameters;
        }

        /// <summary>
        ///     Levenberg's damping factor, also known as lambda.
        /// </summary>
        /// <remarks>
        ///     <para>The value determines speed of learning.</para>
        ///     <para>Default value is <b>0.1</b>.</para>
        /// </remarks>
        public double LearningRate { get; set; } = 0.1;

        /// <summary>
        ///     Learning rate adjustment.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The value by which the learning rate
        ///         is adjusted when searching for the minimum cost surface.
        ///     </para>
        ///     <para>Default value is <b>10</b>.</para>
        /// </remarks>
        public double Adjustment { get; set; } = 10.0;

        /// <summary>
        ///     Gets or sets the number of blocks to divide the
        ///     Jacobian matrix in the Hessian calculation to
        ///     preserve memory. Default is 1.
        /// </summary>
        public int Blocks { get; set; } = 1;

        /// <summary>
        ///     Gets or sets a small epsilon value to be added to the
        ///     diagonal of the Hessian matrix. Default is 1e-12.
        /// </summary>
        public double Epsilon { get; set; } = 1e-12;

        /// <summary>
        ///     Gets the approximate Hessian matrix of second derivatives
        ///     generated in the last algorithm iteration. The Hessian is
        ///     stored in the upper triangular part of this matrix. See
        ///     remarks for details.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The Hessian needs only be upper-triangular, since
        ///         it is symmetric. The Cholesky decomposition will
        ///         make use of this fact and use the lower-triangular
        ///         portion to hold the decomposition, conserving memory
        ///     </para>
        ///     <para>
        ///         Thus said, this property will hold the Hessian matrix
        ///         in the upper-triangular part of this matrix, and store
        ///         its Cholesky decomposition on its lower triangular part.
        ///     </para>
        ///     <para>
        ///         Please note that this value is actually just an approximation to the
        ///         actual Hessian matrix using the outer Jacobian approximation (H ~ J'J).
        ///     </para>
        /// </remarks>
        public double[][] Hessian { get; private set; }

        /// <summary>
        ///     Gets standard error for each parameter in the solution.
        /// </summary>
        public double[] StandardErrors => decomposition.InverseDiagonal().Sqrt();

        /// <summary>
        ///     Attempts to find the best values for the parameter vector
        ///     minimizing the discrepancy between the generated outputs
        ///     and the expected outputs for a given set of input data.
        /// </summary>
        /// <param name="inputs">A set of input data.</param>
        /// <param name="outputs">
        ///     The values associated with each
        ///     vector in the <paramref name="inputs" /> data.
        /// </param>
        public double Minimize(double[][] inputs, double[] outputs)
        {
            if (NumberOfParameters == 0)
            {
                throw new InvalidOperationException("Please set the NumberOfVariables property first.");
            }

            // Divide the problem into blocks. Instead of computing
            // a single Jacobian and a single error vector, we will
            // be computing multiple Jacobians for smaller problems
            // and then sum all blocks into the final Hessian matrix
            // and gradient vector.

            var blockSize = inputs.Length / Blocks;
            var finalBlock = inputs.Length % Blocks;
            var jacobianSize = blockSize * outputCount;

            // Re-allocate the partial Jacobian matrix only if needed
            if (jacobian[0] == null || jacobian[0].Length < jacobianSize)
            {
                for (var i = 0; i < jacobian.Length; i++)
                {
                    jacobian[i] = new double[jacobianSize];
                }
            }

            // Re-allocate error vector only if needed
            if (errors == null || errors.Length < jacobianSize)
            {
                errors = new double[jacobianSize];
            }

            Convergence.CurrentIteration = 0;

            do
            {
                Convergence.NewValue = iterate(inputs, outputs, blockSize, finalBlock, jacobianSize);
            } while (!Convergence.HasConverged);


            return Value = Convergence.NewValue;
        }

        /// <summary>
        ///     This method should be implemented by child classes to initialize
        ///     their fields once the <see cref="BaseLeastSquaresMethod.NumberOfParameters" /> is known.
        /// </summary>
        protected override void Initialize()
        {
            weights = new double[NumberOfParameters];
            diagonal = new double[NumberOfParameters];
            gradient = new double[NumberOfParameters];

            jacobian = new double[NumberOfParameters][];
            Hessian = Jagged.Zeros(NumberOfParameters, NumberOfParameters);
            for (var i = 0; i < Hessian.Length; i++)
            {
                Hessian[i] = new double[NumberOfParameters];
            }
        }

        private double iterate(double[][] inputs, double[] outputs, int blockSize, int finalBlock, int jacobianSize)
        {
            double sumOfSquaredErrors = 0;

            // Set upper triangular Hessian to zero
            for (var i = 0; i < Hessian.Length; i++)
            {
                Array.Clear(Hessian[i], i, Hessian.Length - i);
            }

            // Set Gradient vector to zero
            Array.Clear(gradient, 0, gradient.Length);

            // For each block
            for (var s = 0; s <= Blocks; s++)
            {
                if (s == Blocks && finalBlock == 0)
                {
                    continue;
                }

                var B = s == Blocks ? finalBlock : blockSize;
                var block = Vector.Range(s * blockSize, s * blockSize + B);

                // Compute the partial residuals vector
                sumOfSquaredErrors += computeErrors(inputs, outputs, block);

                // Compute the partial Jacobian
                computeJacobian(inputs, block);

                if (double.IsNaN(sumOfSquaredErrors))
                {
                    throw new ArithmeticException("Error calculation has produced a non-finite number."
                                                  + " Please make sure that there are no constant columns in the input data.");
                }


                // Compute error gradient using Jacobian
                for (var i = 0; i < jacobian.Length; i++)
                {
                    double sum = 0;
                    for (var j = 0; j < jacobianSize; j++)
                    {
                        sum += jacobian[i][j] * errors[j];
                    }

                    gradient[i] += sum;
                }


                // Compute Quasi-Hessian Matrix approximation
                //  using the outer product Jacobian (H ~ J'J)
                //
                Parallel.For(0, jacobian.Length, ParallelOptions, i =>
                {
                    var ji = jacobian[i];
                    var hi = Hessian[i];

                    for (var j = i; j < hi.Length; j++)
                    {
                        var jj = jacobian[j];

                        double sum = 0;
                        for (var k = 0; k < jj.Length; k++)
                        {
                            sum += ji[k] * jj[k];
                        }

                        // The Hessian need only be upper-triangular, since
                        // it is symmetric. The Cholesky decomposition will
                        // make use of this fact and use the lower-triangular
                        // portion to hold the decomposition, conserving memory.

                        hi[j] += 2 * sum;
                    }
                });
            }


            // Store the Hessian's diagonal for future computations. The
            // diagonal will be destroyed in the decomposition, so it can
            // still be updated on every iteration by restoring this copy.
            //
            for (var i = 0; i < Hessian.Length; i++)
            {
                diagonal[i] = Hessian[i][i] + Epsilon;
            }

            // Create the initial weights vector
            for (var i = 0; i < Solution.Length; i++)
            {
                weights[i] = Solution[i];
            }


            // Define the objective function:
            var objective = sumOfSquaredErrors;
            var current = objective + 1.0;


            // Begin of the main Levenberg-Marquardt method
            LearningRate /= Adjustment;

            // We'll try to find a direction with less error
            //  (or where the objective function is smaller)
            while (current >= objective && LearningRate < lambdaMax)
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }

                LearningRate *= Adjustment;

                // Update diagonal (Levenberg-Marquardt)
                for (var i = 0; i < diagonal.Length; i++)
                {
                    Hessian[i][i] = diagonal[i] * (1 + LearningRate);
                }


                // Decompose to solve the linear system. The Cholesky decomposition
                // is done in place, occupying the Hessian's lower-triangular part.
                decomposition = new JaggedCholeskyDecomposition(Hessian, true, true);


                // Check if the decomposition exists
                if (decomposition.IsUndefined)
                {
                    // The Hessian is singular. Continue to the next
                    // iteration until the diagonal update transforms
                    // it back to non-singular.
                    continue;
                }


                // Solve using Cholesky decomposition
                deltas = decomposition.Solve(gradient);


                // Update weights using the calculated deltas
                for (var i = 0; i < Solution.Length; i++)
                {
                    Solution[i] = weights[i] + deltas[i];
                }


                // Calculate the new error
                sumOfSquaredErrors = ComputeError(inputs, outputs);

                // Update the objective function
                current = sumOfSquaredErrors;

                // If the object function is bigger than before, the method
                // is tried again using a greater damping factor.
            }

            // If this iteration caused a error drop, then next iteration
            //  will use a smaller damping factor.
            LearningRate /= Adjustment;

            return sumOfSquaredErrors;
        }

        private double computeErrors(double[][] input, double[] output, int[] block)
        {
            var sumOfSquaredErrors = 0.0;

            // for each input sample
            foreach (var i in block)
            {
                var actual = Function(Solution, input[i]);
                var expected = output[i];

                var e = expected - actual;
                sumOfSquaredErrors += e * e;

                errors[i] = e;
            }

            return sumOfSquaredErrors / 2.0;
        }

        private void computeJacobian(double[][] input, int[] block)
        {
            var derivatives = new double[NumberOfParameters];

            // for each input sample
            foreach (var i in block)
            {
                Gradient(Solution, input[i], derivatives);

                // copy the gradient vector into the Jacobian
                for (var j = 0; j < derivatives.Length; j++)
                {
                    jacobian[j][i] = derivatives[j];
                }
            }
        }
    }
}