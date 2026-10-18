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
using Accord.MachineLearning;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Models.Regression.Linear
{
    /// <summary>
    ///     Multivariate Linear Regression.
    /// </summary>
    /// <remarks>
    ///     Multivariate Linear Regression is a generalization of
    ///     Multiple Linear Regression to allow for multiple outputs.
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\MultivariateLinearRegressionTest.cs"
    ///         region="doc_learn" />
    /// </example>
    [Serializable]
#pragma warning disable 612, 618
    public class MultivariateLinearRegression : MultipleTransformBase<double[], double>, ILinearRegression
#pragma warning restore 612, 618
    {
        private double[,] coefficients; // obsolete
        private bool insertConstant; // obsolete
        private double[] intercepts;
        private double[][] weights;

        /// <summary>
        ///     Creates a new Multivariate Linear Regression.
        /// </summary>
        /// <param name="inputs">The number of inputs for the regression.</param>
        /// <param name="outputs">The number of outputs for the regression.</param>
        [Obsolete("Please use the parameterless constructor instead.")]
        public MultivariateLinearRegression(int inputs, int outputs)
            : this(inputs, outputs, false)
        {
        }

        /// <summary>
        ///     Creates a new Multivariate Linear Regression.
        /// </summary>
        /// <param name="inputs">The number of inputs for the regression.</param>
        /// <param name="outputs">The number of outputs for the regression.</param>
        /// <param name="intercept">True to use an intercept term, false otherwise. Default is false.</param>
        [Obsolete("Please use the parameterless constructor instead.")]
        public MultivariateLinearRegression(int inputs, int outputs, bool intercept)
        {
            coefficients = new double[inputs, outputs];
            intercepts = new double[outputs];
            insertConstant = intercept;
        }

        /// <summary>
        ///     Creates a new Multivariate Linear Regression.
        /// </summary>
        [Obsolete("Please use the parameterless constructor instead.")]
        public MultivariateLinearRegression(double[,] coefficients, double[] intercepts, bool insertConstant)
        {
            this.coefficients = coefficients;
            this.intercepts = intercepts;
            this.insertConstant = insertConstant;
        }

        /// <summary>
        ///     Creates a new Multivariate Linear Regression.
        /// </summary>
        [Obsolete("Please use the parameterless constructor instead.")]
        public MultivariateLinearRegression(double[][] coefficients, double[] intercepts, bool insertConstant)
        {
            this.coefficients = coefficients.ToMatrix();
            this.intercepts = intercepts;
            this.insertConstant = insertConstant;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultivariateLinearRegression" /> class.
        /// </summary>
        public MultivariateLinearRegression()
        {
        }

        /// <summary>
        ///     Gets the coefficient matrix used by the regression model. Each
        ///     column corresponds to the coefficient vector for each of the outputs.
        /// </summary>
        [Obsolete("Please use Weights instead.")]
        public double[,] Coefficients => coefficients;

        /// <summary>
        ///     Gets the linear weights matrix.
        /// </summary>
        public double[][] Weights
        {
            get => weights;
            set
            {
                weights = value;
                NumberOfInputs = value.Rows();
                NumberOfOutputs = value.Columns();
                coefficients = value.ToMatrix();
            }
        }

        /// <summary>
        ///     Gets the intercept vector (bias).
        /// </summary>
        public double[] Intercepts
        {
            get => intercepts;
            set => intercepts = value;
        }

        /// <summary>
        ///     Gets the number of parameters in the model (returns
        ///     NumberOfInputs * NumberOfOutputs + NumberOfInputs).
        /// </summary>
        public int NumberOfParameters => NumberOfInputs * NumberOfOutputs + NumberOfOutputs;

        /// <summary>
        ///     Gets the number of inputs in the model.
        /// </summary>
        [Obsolete("Please use NumberOfInputs instead.")]
        public int Inputs => coefficients.GetLength(0);

        /// <summary>
        ///     Gets the number of outputs in the model.
        /// </summary>
        [Obsolete("Please use NumberOfOutputs instead.")]
        public int Outputs => coefficients.GetLength(1);

#pragma warning disable 612, 618
        [Obsolete("Please use Transform instead.")]
        double[] ILinearRegression.Compute(double[] inputs)
        {
            return Compute(inputs);
        }
#pragma warning restore 612, 618

        /// <summary>
        ///     Performs the regression using the input vectors and output
        ///     vectors, returning the sum of squared errors of the fit.
        /// </summary>
        /// <param name="inputs">The input vectors to be used in the regression.</param>
        /// <param name="outputs">The output values for each input vector.</param>
        /// <returns>The Sum-Of-Squares error of the regression.</returns>
        [Obsolete("Please use the LeastSquares class instead.")]
        public virtual double Regress(double[][] inputs, double[][] outputs)
        {
            if (inputs.Length != outputs.Length)
            {
                throw new ArgumentException("Number of input and output samples does not match", "outputs");
            }

            var cols = inputs[0].Length; // inputs
            var rows = inputs.Length; // points

            if (insertConstant)
            {
                cols++;
            }

            for (var c = 0; c < coefficients.GetLength(1); c++)
            {
                var B = new double[cols];
                var V = new double[cols, cols];


                // Compute V and B matrices
                for (var i = 0; i < cols; i++)
                {
                    // Least Squares Matrix
                    for (var j = 0; j < cols; j++)
                    {
                        for (var k = 0; k < rows; k++)
                        {
                            if (insertConstant)
                            {
                                var a = i == cols - 1 ? 1 : inputs[k][i];
                                var b = j == cols - 1 ? 1 : inputs[k][j];

                                V[i, j] += a * b;
                            }
                            else
                            {
                                V[i, j] += inputs[k][i] * inputs[k][j];
                            }
                        }
                    }

                    // Function to minimize
                    for (var k = 0; k < rows; k++)
                    {
                        if (insertConstant && i == cols - 1)
                        {
                            B[i] += outputs[k][c];
                        }
                        else
                        {
                            B[i] += inputs[k][i] * outputs[k][c];
                        }
                    }
                }


                // Solve V*C = B to find C (the coefficients)
                var coef = new SingularValueDecomposition(V).Solve(B);

                if (insertConstant)
                {
                    intercepts[c] = coef[coef.Length - 1];
                    for (var i = 0; i < cols - 1; i++)
                    {
                        coefficients[i, c] = coef[i];
                    }
                }
                else
                {
                    for (var i = 0; i < cols; i++)
                    {
                        coefficients[i, c] = coef[i];
                    }
                }
            }

            Weights = coefficients.ToJagged();

            // Calculate Sum-Of-Squares error
            double error = 0.0, e;
            for (var i = 0; i < outputs.Length; i++)
            {
                var y = Transform(inputs[i]);

                for (var c = 0; c < y.Length; c++)
                {
                    e = outputs[i][c] - y[c];
                    error += e * e;
                }
            }

            return error;
        }

        /// <summary>
        ///     Gets the coefficient of determination, as known as R² (r-squared).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The coefficient of determination is used in the context of statistical models
        ///         whose main purpose is the prediction of future outcomes on the basis of other
        ///         related information. It is the proportion of variability in a data set that
        ///         is accounted for by the statistical model. It provides a measure of how well
        ///         future outcomes are likely to be predicted by the model.
        ///     </para>
        ///     <para>
        ///         The R² coefficient of determination is a statistical measure of how well the
        ///         regression line approximates the real data points. An R² of 1.0 indicates
        ///         that the regression line perfectly fits the data.
        ///     </para>
        ///     <para>
        ///         This method uses the <see cref="RSquaredLoss" /> class to compute the R²
        ///         coefficient. Please see the documentation for <see cref="RSquaredLoss" />
        ///         for more details, including usage examples.
        ///     </para>
        /// </remarks>
        /// <returns>The R² (r-squared) coefficient for the given data.</returns>
        /// <seealso cref="RSquaredLoss" />
        public double[] CoefficientOfDetermination(double[][] inputs, double[][] outputs, double[] weights = null)
        {
            return CoefficientOfDetermination(inputs, outputs, false, weights);
        }

        /// <summary>
        ///     Gets the coefficient of determination, as known as R² (r-squared).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The coefficient of determination is used in the context of statistical models
        ///         whose main purpose is the prediction of future outcomes on the basis of other
        ///         related information. It is the proportion of variability in a data set that
        ///         is accounted for by the statistical model. It provides a measure of how well
        ///         future outcomes are likely to be predicted by the model.
        ///     </para>
        ///     <para>
        ///         The R² coefficient of determination is a statistical measure of how well the
        ///         regression line approximates the real data points. An R² of 1.0 indicates
        ///         that the regression line perfectly fits the data.
        ///     </para>
        ///     <para>
        ///         This method uses the <see cref="RSquaredLoss" /> class to compute the R²
        ///         coefficient. Please see the documentation for <see cref="RSquaredLoss" />
        ///         for more details, including usage examples.
        ///     </para>
        /// </remarks>
        /// <returns>The R² (r-squared) coefficient for the given data.</returns>
        /// <seealso cref="RSquaredLoss" />
        public double[] CoefficientOfDetermination(double[][] inputs, double[][] outputs, bool adjust,
            double[] weights = null)
        {
            var rsquared = new RSquaredLoss(NumberOfInputs, outputs);

            rsquared.Adjust = adjust;

            if (weights != null)
            {
                rsquared.Weights = weights;
            }

            return rsquared.Loss(Transform(inputs));
        }

        /// <summary>
        ///     Computes the Multiple Linear Regression output for a given input.
        /// </summary>
        /// <param name="input">A input vector.</param>
        /// <returns>The computed output.</returns>
        [Obsolete("Please use Transform() instead.")]
        public double[] Compute(double[] input)
        {
            var N = input.Length;
            var M = coefficients.GetLength(1);

            var result = new double[M];
            for (var i = 0; i < M; i++)
            {
                result[i] = intercepts[i];
                for (var j = 0; j < N; j++)
                {
                    result[i] += input[j] * coefficients[j, i];
                }
            }

            return result;
        }

        /// <summary>
        ///     Computes the Multiple Linear Regression output for a given input.
        /// </summary>
        /// <param name="input">An array of input vectors.</param>
        /// <returns>The computed outputs.</returns>
        [Obsolete("Please use Transform() instead.")]
        public double[][] Compute(double[][] input)
        {
            var output = new double[input.Length][];
            for (var j = 0; j < input.Length; j++)
            {
                output[j] = Compute(input[j]);
            }

            return output;
        }

        /// <summary>
        ///     Creates a new linear regression directly from data points.
        /// </summary>
        /// <param name="x">The input vectors <c>x</c>.</param>
        /// <param name="y">The output vectors <c>y</c>.</param>
        /// <returns>A linear regression f(x) that most approximates y.</returns>
        public static MultivariateLinearRegression FromData(double[][] x, double[][] y)
        {
            return new OrdinaryLeastSquares().Learn(x, y);
        }

        /// <summary>
        ///     Creates a new linear regression from the regression coefficients.
        /// </summary>
        /// <param name="coefficients">The linear coefficients.</param>
        /// <param name="intercept">The intercept (bias) values.</param>
        /// <returns>A linear regression with the given coefficients.</returns>
        [Obsolete("Please use the parameterless constructor and set Weights and Intercept directly.")]
        public static MultivariateLinearRegression FromCoefficients(double[][] coefficients, double[] intercept)
        {
            var regression = new MultivariateLinearRegression(coefficients.Length, coefficients[0].Length);
            regression.Weights = coefficients;
            regression.intercepts = intercept;
            return regression;
        }

        /// <summary>
        ///     Creates the inverse regression, a regression that can recover
        ///     the input data given the outputs of this current regression.
        /// </summary>
        public MultivariateLinearRegression Inverse()
        {
            var inv = Weights.PseudoInverse();
            return new MultivariateLinearRegression
            {
                Weights = inv,
                Intercepts = intercepts != null ? intercepts.Dot(inv).Multiply(-1) : null
            };
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result"></param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override double[][] Transform(double[][] input, double[][] result)
        {
            input.Dot(Weights, result);
            if (intercepts != null)
            {
                result.Add(intercepts, (VectorType)0, result);
            }

            return result;
        }

        /// <summary>
        ///     Gets the overall regression standard error.
        /// </summary>
        /// <param name="inputs">The inputs used to train the model.</param>
        /// <param name="outputs">The outputs used to train the model.</param>
        public double[] GetStandardError(double[][] inputs, double[][] outputs)
        {
            // Calculate actual outputs (results)
            var results = Transform(inputs);

            var SSe = new double[NumberOfOutputs];
            for (var i = 0; i < inputs.Length; i++)
            {
                for (var j = 0; j < SSe.Length; j++)
                {
                    var d = outputs[i][j] - results[i][j];
                    SSe[j] += d * d;
                }
            }

            var DFe = GetDegreesOfFreedom(inputs.Length);
            return SSe.Divide(DFe).Sqrt();
        }

        /// <summary>
        ///     Gets the degrees of freedom when fitting the regression.
        /// </summary>
        public double GetDegreesOfFreedom(int numberOfSamples)
        {
            return numberOfSamples - NumberOfParameters;
        }

        /// <summary>
        ///     Gets the standard error for each coefficient.
        /// </summary>
        /// <param name="mse">
        ///     The overall regression standard error (can be computed from
        ///     <see cref="GetStandardError(double[][], double[][])" />.
        /// </param>
        /// <param name="informationMatrix">
        ///     The information matrix obtained when training the model (see
        ///     <see cref="OrdinaryLeastSquares.GetInformationMatrix()" />).
        /// </param>
        public double[][] GetStandardErrors(double[] mse, double[][] informationMatrix)
        {
            var se = Jagged.Zeros(NumberOfOutputs, informationMatrix.Length);
            for (var j = 0; j < se.Length; j++)
            {
                for (var i = 0; i < informationMatrix.Length; i++)
                {
                    se[j][i] = mse[j] * System.Math.Sqrt(informationMatrix[i][i]);
                }
            }

            return se;
        }

        /// <summary>
        ///     Gets the standard error of the fit for a particular input vector.
        /// </summary>
        /// <param name="input">The input vector where the standard error of the fit should be computed.</param>
        /// <param name="mse">
        ///     The overall regression standard error (can be computed from
        ///     <see cref="GetStandardError(double[][], double[][])" />.
        /// </param>
        /// <param name="informationMatrix">
        ///     The information matrix obtained when training the model (see
        ///     <see cref="OrdinaryLeastSquares.GetInformationMatrix()" />).
        /// </param>
        /// <returns>The standard error of the fit at the given input point.</returns>
        public double[] GetStandardError(double[] input, double[] mse, double[][] informationMatrix)
        {
            var rim = predictionVariance(input, informationMatrix);
            return mse.Multiply(System.Math.Sqrt(rim));
        }

        /// <summary>
        ///     Gets the standard error of the prediction for a particular input vector.
        /// </summary>
        /// <param name="input">The input vector where the standard error of the prediction should be computed.</param>
        /// <param name="mse">
        ///     The overall regression standard error (can be computed from
        ///     <see cref="GetStandardError(double[][], double[][])" />.
        /// </param>
        /// <param name="informationMatrix">
        ///     The information matrix obtained when training the model (see
        ///     <see cref="OrdinaryLeastSquares.GetInformationMatrix()" />).
        /// </param>
        /// <returns>The standard error of the prediction given for the input point.</returns>
        public double[] GetPredictionStandardError(double[] input, double[] mse, double[][] informationMatrix)
        {
            var rim = predictionVariance(input, informationMatrix);
            return mse.Multiply(System.Math.Sqrt(1 + rim));
        }

        /// <summary>
        ///     Gets the confidence interval for an input point.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="mse">
        ///     The overall regression standard error (can be computed from
        ///     <see cref="GetStandardError(double[][], double[][])" />.
        /// </param>
        /// <param name="numberOfSamples">The number of training samples used to fit the model.</param>
        /// <param name="informationMatrix">
        ///     The information matrix obtained when training the model (see
        ///     <see cref="OrdinaryLeastSquares.GetInformationMatrix()" />).
        /// </param>
        /// <param name="percent">The prediction interval confidence (default is 95%).</param>
        public DoubleRange[] GetConfidenceInterval(double[] input, double[] mse, int numberOfSamples,
            double[][] informationMatrix, double percent = 0.95)
        {
            var se = GetStandardError(input, mse, informationMatrix);
            return createInterval(input, numberOfSamples, percent, se);
        }

        /// <summary>
        ///     Gets the prediction interval for an input point.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="mse">
        ///     The overall regression standard error (can be computed from
        ///     <see cref="GetStandardError(double[][], double[][])" />.
        /// </param>
        /// <param name="numberOfSamples">The number of training samples used to fit the model.</param>
        /// <param name="informationMatrix">
        ///     The information matrix obtained when training the model (see
        ///     <see cref="OrdinaryLeastSquares.GetInformationMatrix()" />).
        /// </param>
        /// <param name="percent">The prediction interval confidence (default is 95%).</param>
        public DoubleRange[] GetPredictionInterval(double[] input, double[] mse, int numberOfSamples,
            double[][] informationMatrix, double percent = 0.95)
        {
            var se = GetPredictionStandardError(input, mse, informationMatrix);
            return createInterval(input, numberOfSamples, percent, se);
        }

        private static double predictionVariance(double[] input, double[][] im)
        {
            if (input.Length < im.Length)
            {
                input = input.Concatenate(1);
            }

            return input.Dot(im).Dot(input);
        }

        private DoubleRange[] createInterval(double[] input, int numberOfSamples, double percent, double[] se)
        {
            var y = Transform(input);
            var df = GetDegreesOfFreedom(numberOfSamples);
            return se.Apply((x, i) =>
                new TTest(y[i], x, df)
                    .GetConfidenceInterval(percent));
        }
    }
}