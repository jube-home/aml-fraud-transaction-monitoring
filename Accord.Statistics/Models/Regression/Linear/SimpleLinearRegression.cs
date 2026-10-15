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
using System.Globalization;
using Accord.MachineLearning;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Models.Regression.Linear
{
    /// <summary>
    ///     Simple Linear Regression of the form y = Ax + B.
    /// </summary>
    /// <remarks>
    ///     In linear regression, the model specification is that the dependent
    ///     variable, y is a linear combination of the parameters (but need not
    ///     be linear in the independent variables). As the linear regression
    ///     has a closed form solution, the regression coefficients can be
    ///     efficiently computed using the Regress method of this class.
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Let's say we have some univariate, continuous sets of input data,
    ///         and a corresponding univariate, continuous set of output data, such
    ///         as a set of points in R². A simple linear regression is able to fit
    ///         a line relating the input variables to the output variables in which
    ///         the minimum-squared-error of the line and the actual output points
    ///         is minimum.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\SimpleLinearRegressionTest.cs"
    ///         region="doc_learn" />
    ///     <para>
    ///         Now, let's say we would like to perform a regression using an
    ///         intermediary transformation, such as for example logarithmic
    ///         regression. In this case, all we have to do is to first transform
    ///         the input variables into the desired domain, then apply the
    ///         regression as normal:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\LogarithmRegressionTest.cs" region="doc_learn" />
    /// </example>
    /// <seealso cref="OrdinaryLeastSquares" />
    /// <seealso cref="MultipleLinearRegression" />
    /// <seealso cref="MultivariateLinearRegression" />
    [Serializable]
#pragma warning disable 612, 618
    public class SimpleLinearRegression : TransformBase<double, double>, ILinearRegression
#pragma warning restore 612, 618
    {
        private double intercept;
        [Obsolete] private MultipleLinearRegression regression;

        private double slope;

        /// <summary>
        ///     Creates a new Simple Linear Regression of the form y = Ax + B.
        /// </summary>
        public SimpleLinearRegression()
        {
            NumberOfInputs = 1;
            NumberOfOutputs = 1;
        }

        /// <summary>
        ///     Angular coefficient (Slope).
        /// </summary>
        public double Slope
        {
            get => slope;
            set => slope = value;
        }

        /// <summary>
        ///     Linear coefficient (Intercept).
        /// </summary>
        public double Intercept
        {
            get => intercept;
            set => intercept = value;
        }

        /// <summary>
        ///     Gets the number of parameters in the model (returns 2).
        /// </summary>
        public int NumberOfParameters => 2;

#pragma warning disable 612, 618
        [Obsolete("Please use Transform instead.")]
        double[] ILinearRegression.Compute(double[] inputs)
        {
            if (inputs.Length > 1)
            {
                throw new ArgumentException("Simple regression supports only one-length input vectors", "inputs");
            }

            return new[] { Compute(inputs[0]) };
        }
#pragma warning restore 612, 618

        /// <summary>
        ///     Performs the regression using the input and output
        ///     data, returning the sum of squared errors of the fit.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The output data.</param>
        /// <returns>The regression Sum-of-Squares error.</returns>
        [Obsolete("Please use the OrdinaryLeastSquares class.")]
        public double Regress(double[] inputs, double[] outputs)
        {
            if (inputs.Length != outputs.Length)
            {
                throw new ArgumentException("Number of input and output samples does not match", "outputs");
            }

            var X = new double[inputs.Length][];
            for (var i = 0; i < inputs.Length; i++)
            {
                X[i] = new[] { 1.0, inputs[i] };
            }

#pragma warning disable 612, 618
            regression = new MultipleLinearRegression(2, false);
            var err = regression.Regress(X, outputs);
            slope = regression.Coefficients[1];
            intercept = regression.Coefficients[0];
#pragma warning restore 612, 618
            return err;
        }

        /// <summary>
        ///     Computes the regression output for a given input.
        /// </summary>
        /// <param name="input">An array of input values.</param>
        /// <returns>The array of calculated output values.</returns>
        [Obsolete("Please use Transform instead.")]
        public double[] Compute(double[] input)
        {
            var output = new double[input.Length];
            for (var i = 0; i < input.Length; i++)
            {
                output[i] = Compute(input[i]);
            }

            return output;
        }

        /// <summary>
        ///     Computes the regression for a single input.
        /// </summary>
        /// <param name="input">The input value.</param>
        /// <returns>The calculated output.</returns>
        [Obsolete("Please use Transform instead.")]
        public double Compute(double input)
        {
            return Slope * input + Intercept;
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
        public double CoefficientOfDetermination(double[] inputs, double[] outputs, bool adjust,
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
        ///     Gets the coefficient of determination, or R² (r-squared).
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
        public double CoefficientOfDetermination(double[] inputs, double[] outputs, double[] weights = null)
        {
            return CoefficientOfDetermination(inputs, outputs, false, weights);
        }

        /// <summary>
        ///     Returns a System.String representing the regression.
        /// </summary>
        public override string ToString()
        {
            return ToString(null as string);
        }

        /// <summary>
        ///     Returns a System.String representing the regression.
        /// </summary>
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     Returns a System.String representing the regression.
        /// </summary>
        public string ToString(IFormatProvider formatProvider)
        {
            return ToString(null, formatProvider);
        }

        /// <summary>
        ///     Returns a System.String representing the regression.
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            var a = format != null ? Slope.ToString(format, formatProvider) : Slope.ToString(formatProvider);

            var b = format != null ? Intercept.ToString(format, formatProvider) : Intercept.ToString(formatProvider);

            return string.Format("y(x) = {0}x + {1}", a, b);
        }

        /// <summary>
        ///     Creates a new linear regression directly from data points.
        /// </summary>
        /// <param name="x">The input vectors <c>x</c>.</param>
        /// <param name="y">The output vectors <c>y</c>.</param>
        /// <returns>A linear regression f(x) that most approximates y.</returns>
        public static SimpleLinearRegression FromData(double[] x, double[] y)
        {
            return new OrdinaryLeastSquares().Learn(x, y);
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override double Transform(double input)
        {
            return Slope * input + Intercept;
        }

        /// <summary>
        ///     Gets the degrees of freedom when fitting the regression.
        /// </summary>
        public double GetDegreesOfFreedom(int numberOfSamples)
        {
            return numberOfSamples - NumberOfParameters;
        }

        /// <summary>
        ///     Gets the overall regression standard error.
        /// </summary>
        /// <param name="inputs">The inputs used to train the model.</param>
        /// <param name="outputs">The outputs used to train the model.</param>
        public double GetStandardError(double[] inputs, double[] outputs)
        {
            var meanX = inputs.Mean();
            var meanY = outputs.Mean();

            double xx = 0;
            double yy = 0;
            double xy = 0;
            for (var i = 0; i < inputs.Length; i++)
            {
                var x = inputs[i] - meanX;
                var y = outputs[i] - meanY;
                xx += x * x;
                xy += x * y;
                yy += y * y;
            }

            var ss = yy - xy * xy / xx;

            return System.Math.Sqrt(ss / GetDegreesOfFreedom(inputs.Length));
        }

        /// <summary>
        ///     Gets the standard error of the fit for a particular input point.
        /// </summary>
        /// <param name="input">The input vector where the standard error of the fit should be computed.</param>
        /// <param name="inputs">The inputs used to train the model.</param>
        /// <param name="outputs">The outputs used to train the model.</param>
        /// <returns>The standard error of the fit at the given input point.</returns>
        public double GetStandardError(double input, double[] inputs, double[] outputs)
        {
            var se = GetStandardError(inputs, outputs);
            var a = predictionVariance(input, inputs, outputs);
            return se * System.Math.Sqrt(a);
        }

        /// <summary>
        ///     Gets the standard error of the prediction for a particular input point.
        /// </summary>
        /// <param name="input">The input vector where the standard error of the prediction should be computed.</param>
        /// <param name="inputs">The inputs used to train the model.</param>
        /// <param name="outputs">The outputs used to train the model.</param>
        /// <returns>The standard error of the prediction given for the input point.</returns>
        public double GetPredictionStandardError(double input, double[] inputs, double[] outputs)
        {
            // http://www2.stat.duke.edu/~tjl13/s101/slides/unit6lec3H.pdf
            var se = GetStandardError(inputs, outputs);
            var a = predictionVariance(input, inputs, outputs);
            return se * System.Math.Sqrt(1 + a);
        }

        /// <summary>
        ///     Gets the confidence interval for an input point.
        /// </summary>
        /// <param name="input">The input point.</param>
        /// <param name="inputs">The inputs used to train the model.</param>
        /// <param name="outputs">The outputs used to train the model.</param>
        /// <param name="percent">The prediction interval confidence (default is 95%).</param>
        public DoubleRange GetConfidenceInterval(double input, double[] inputs, double[] outputs, double percent = 0.95)
        {
            var se = GetStandardError(input, inputs, outputs);
            return createInterval(input, inputs, percent, se);
        }

        /// <summary>
        ///     Gets the prediction interval for an input point.
        /// </summary>
        /// <param name="input">The input point.</param>
        /// <param name="inputs">The inputs used to train the model.</param>
        /// <param name="outputs">The outputs used to train the model.</param>
        /// <param name="percent">The prediction interval confidence (default is 95%).</param>
        public DoubleRange GetPredictionInterval(double input, double[] inputs, double[] outputs, double percent = 0.95)
        {
            var se = GetPredictionStandardError(input, inputs, outputs);
            return createInterval(input, inputs, percent, se);
        }

        private DoubleRange createInterval(double input, double[] inputs, double percent, double se)
        {
            var y = Transform(input);
            var df = GetDegreesOfFreedom(inputs.Length);
            var t = new TTest(y, se, df);
            return t.GetConfidenceInterval(percent);
        }

        private static double predictionVariance(double input, double[] inputs, double[] outputs)
        {
            double n = outputs.Length;
            var meanX = inputs.Mean();

            double sx = 0;
            for (var i = 0; i < inputs.Length; i++)
            {
                var d = inputs[i] - meanX;
                sx += d * d;
            }

            var z = input - meanX;
            return 1 / n + z * z / sx;
        }
    }
}