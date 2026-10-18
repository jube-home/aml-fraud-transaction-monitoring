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

using System.Threading.Tasks;
using Accord.Math.Decompositions;
using Accord.Math.Differentiation;
using Accord.Statistics.Models;

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Gauss-Newton algorithm for solving Least-Squares problems.
    /// </summary>
    /// <remarks>
    ///     This class isn't suitable for most real-world problems. Instead, this class
    ///     is intended to be use as a baseline comparison to help debug and check other
    ///     optimization methods, such as <see cref="LevenbergMarquardt" />.
    /// </remarks>
    /// <example>
    ///     <para>
    ///         While it is possible to use the <see cref="GaussNewton" /> class as a standalone
    ///         method for solving least squares problems, this class is intended to be used as
    ///         a strategy for NonlinearLeastSquares, as shown in the example below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\NonlinearLeastSquaresTest.cs"
    ///         region="doc_learn_gn" lang="cs" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics.VB\Models\Regression\NonlinearLeastSquaresTest.vb"
    ///         region="doc_learn_gn" lang="vb" />
    ///     <para>
    ///         However, as mentioned above it is also possible to use <see cref="GaussNewton" /> as
    ///         a standalone class, as shown in the example below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\GaussNewtonTest.cs" region="doc_minimize" />
    /// </example>
    /// <seealso cref="LevenbergMarquardt" />
    /// <seealso cref="FiniteDifferences" />
    public class GaussNewton : BaseLeastSquaresMethod, ILeastSquaresMethod, IConvergenceLearning
    {
        private JaggedSingularValueDecomposition decomposition;
        private double[] gradient; // this is just a cached vector to avoid memory allocations

        /// <summary>
        ///     Initializes a new instance of the <see cref="GaussNewton" /> class.
        /// </summary>
        public GaussNewton()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GaussNewton" /> class.
        /// </summary>
        /// <param name="parameters">
        ///     The number of variables (free parameters)
        ///     in the objective function.
        /// </param>
        public GaussNewton(int parameters)
            : this()
        {
            NumberOfParameters = parameters;
        }

        /// <summary>
        ///     Gets the approximate Hessian matrix of second derivatives
        ///     created during the last algorithm iteration.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Please note that this value is actually just an approximation to the
        ///         actual Hessian matrix using the outer Jacobian approximation (H ~ J'J).
        ///     </para>
        /// </remarks>
        public double[][] Hessian { get; private set; }

        /// <summary>
        ///     Gets the vector of residuals computed in the last iteration.
        ///     The residuals are computed as <c>(y - f(w, x))</c>, in which
        ///     <c>y</c> are the expected output values, and <c>f</c> is the
        ///     parameterized model function.
        /// </summary>
        public double[] Residuals { get; private set; }

        /// <summary>
        ///     Gets the Jacobian matrix of first derivatives computed in the
        ///     last iteration.
        /// </summary>
        public double[][] Jacobian { get; private set; }

        /// <summary>
        ///     Gets the vector of coefficient updates computed in the last iteration.
        /// </summary>
        public double[] Deltas { get; private set; }

        /// <summary>
        ///     Gets standard error for each parameter in the solution.
        /// </summary>
        public double[] StandardErrors => decomposition.Inverse().Diagonal().Sqrt();

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
            Convergence.CurrentIteration = 0;

            do
            {
                Convergence.NewValue = iterate(inputs, outputs);
            } while (!Convergence.HasConverged);

            return Value = Convergence.NewValue;
        }

        /// <summary>
        ///     This method should be implemented by child classes to initialize
        ///     their fields once the <see cref="BaseLeastSquaresMethod.NumberOfParameters" /> is known.
        /// </summary>
        protected override void Initialize()
        {
            Hessian = Jagged.Zeros(NumberOfParameters, NumberOfParameters);
            gradient = new double[NumberOfParameters];
            Jacobian = new double[NumberOfParameters][];
        }

        private double iterate(double[][] inputs, double[] outputs)
        {
            if (Residuals == null || inputs.Length != Residuals.Length)
            {
                Residuals = new double[inputs.Length];
                for (var i = 0; i < Jacobian.Length; i++)
                {
                    Jacobian[i] = new double[inputs.Length];
                }
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                Residuals[i] = outputs[i] - Function(Solution, inputs[i]);
            }

            if (ParallelOptions.MaxDegreeOfParallelism == 1)
            {
                for (var i = 0; i < inputs.Length; i++)
                {
                    Gradient(Solution, inputs[i], gradient);

                    for (var j = 0; j < gradient.Length; j++)
                    {
                        Jacobian[j][i] = -gradient[j];
                    }

                    if (Token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            else
            {
                Parallel.For(0, inputs.Length, ParallelOptions,
                    () => new double[NumberOfParameters],
                    (i, state, grad) =>
                    {
                        Gradient(Solution, inputs[i], grad);

                        for (var j = 0; j < grad.Length; j++)
                        {
                            Jacobian[j][i] = -grad[j];
                        }

                        return grad;
                    },
                    grad => { }
                );
            }


            // Compute error gradient using Jacobian
            Jacobian.Dot(Residuals, gradient);

            // Compute Quasi-Hessian Matrix approximation
            Jacobian.DotWithTransposed(Jacobian, Hessian);

            decomposition = new JaggedSingularValueDecomposition(Hessian,
                true, true, true);

            Deltas = decomposition.Solve(gradient);

            for (var i = 0; i < Deltas.Length; i++)
            {
                Solution[i] -= Deltas[i];
            }

            return ComputeError(inputs, outputs);
        }
    }
}