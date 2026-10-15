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
using System.Runtime.Serialization;
using Accord.Diagnostics;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Links;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Models.Regression
{
    /// <summary>
    ///     Generalized Linear Model Regression.
    /// </summary>
    /// <remarks>
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
    [Serializable]
    public class GeneralizedLinearRegression : BinaryLikelihoodClassifierBase<double[]>, ICloneable
    {
#pragma warning disable 0649
        [Obsolete] private double[] coefficients;
#pragma warning restore 0649
        private MultipleLinearRegression linear;
        private ILinkFunction linkFunction;

        private double[] standardErrors;

        /// <summary>
        ///     Creates a new Generalized Linear Regression Model.
        /// </summary>
        /// <param name="function">The link function to use.</param>
        public GeneralizedLinearRegression(ILinkFunction function)
        {
            linear = new MultipleLinearRegression();
            linkFunction = function;
            NumberOfInputs = 1;
        }

        /// <summary>
        ///     Creates a new Generalized Linear Regression Model.
        /// </summary>
        /// <param name="function">The link function to use.</param>
        /// <param name="inputs">The number of input variables for the model.</param>
        [Obsolete("Please use the default constructor and set the NumberOfInputs property instead.")]
        public GeneralizedLinearRegression(ILinkFunction function, int inputs)
            : this(function)
        {
            NumberOfInputs = inputs;
        }

        /// <summary>
        ///     Creates a new Generalized Linear Regression Model.
        /// </summary>
        /// <param name="function">The link function to use.</param>
        /// <param name="inputs">The number of input variables for the model.</param>
        /// <param name="intercept">The starting intercept value. Default is 0.</param>
        [Obsolete("Please use the default constructor and set the NumberofInputs and Intercept properties instead.")]
        public GeneralizedLinearRegression(ILinkFunction function, int inputs, double intercept)
            : this(function)
        {
            NumberOfInputs = inputs;
            Intercept = intercept;
        }

        /// <summary>
        ///     Creates a new Generalized Linear Regression Model.
        /// </summary>
        public GeneralizedLinearRegression()
            : this(new LogitLinkFunction())
        {
        }

        /// <summary>
        ///     Creates a new Generalized Linear Regression Model.
        /// </summary>
        /// <param name="function">The link function to use.</param>
        /// <param name="coefficients">The coefficient vector.</param>
        /// <param name="standardErrors">The standard error vector.</param>
        [Obsolete("Please use the default constructor and set the Weights and StandardErrors properties instead.")]
        public GeneralizedLinearRegression(ILinkFunction function,
            double[] coefficients, double[] standardErrors)
            : this()
        {
            linkFunction = function;
            Weights = coefficients.Get(1, 0);
            StandardErrors = standardErrors;
        }

        /// <summary>
        ///     Gets the number of inputs accepted by the model.
        /// </summary>
        public override int NumberOfInputs
        {
            get => Linear.NumberOfInputs;
            set
            {
                Linear.NumberOfInputs = value;
                standardErrors = Vector.Create(value + 1, standardErrors);
            }
        }

        /// <summary>
        ///     Obsolete. For quick compatibility fixes in the short term, use
        ///     <see cref="GetCoefficient(int)" /> and <see cref="SetCoefficient(int, double)" />.
        /// </summary>
        [Obsolete("Please use Weights and Intercept instead.")]
        public double[] Coefficients => Intercept.Concatenate(Weights);

        /// <summary>
        ///     Gets the number of parameters in this model (equals the NumberOfInputs + 1).
        /// </summary>
        public int NumberOfParameters => Linear.NumberOfParameters;

        /// <summary>
        ///     Gets or sets the linear weights of the regression model. The
        ///     intercept term is not stored in this vector, but is instead
        ///     available through the <see cref="Intercept" /> property.
        /// </summary>
        public double[] Weights
        {
            get => Linear.Weights;
            set => linear.Weights = value;
        }

        /// <summary>
        ///     Gets the standard errors associated with each
        ///     coefficient during the model estimation phase.
        /// </summary>
        public double[] StandardErrors
        {
            get => standardErrors;
            set => standardErrors = value;
        }

        /// <summary>
        ///     Gets the number of inputs handled by this model.
        /// </summary>
        [Obsolete("Please use NumberOfInputs instead.")]
        public int Inputs => NumberOfInputs;

        /// <summary>
        ///     Gets the link function used by
        ///     this generalized linear model.
        /// </summary>
        public ILinkFunction Link
        {
            get => linkFunction;
            protected set => linkFunction = value;
        }

        /// <summary>
        ///     Gets the underlying linear regression.
        /// </summary>
        public MultipleLinearRegression Linear
        {
            get => linear;
            protected set => linear = value;
        }

        /// <summary>
        ///     Gets or sets the intercept term. This is always the
        ///     first value of the <see cref="Coefficients" /> array.
        /// </summary>
        public double Intercept
        {
            get => linear.Intercept;
            set => linear.Intercept = value;
        }

        /// <summary>
        ///     Creates a new GeneralizedLinearRegression that is a copy of the current instance.
        /// </summary>
        public object Clone()
        {
            return new GeneralizedLinearRegression
            {
                Link = (ILinkFunction)linkFunction.Clone(),
                Linear = (MultipleLinearRegression)Linear.Clone(),
                StandardErrors = (double[])StandardErrors.Clone()
            };
        }

        /// <summary>
        ///     Gets a coefficient value, where 0 is the intercept term
        ///     and the other coefficients are indexed starting at 1.
        /// </summary>
        public double GetCoefficient(int index)
        {
            if (index == 0)
            {
                return Intercept;
            }

            return Weights[index - 1];
        }

        /// <summary>
        ///     Sets a coefficient value, where 0 is the intercept term
        ///     and the other coefficients are indexed starting at 1.
        /// </summary>
        public void SetCoefficient(int index, double value)
        {
            if (index == 0)
            {
                Intercept = value;
            }
            else
            {
                Weights[index - 1] = value;
            }
        }

        /// <summary>
        ///     Computes the model output for the given input vector.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <returns>The output value.</returns>
        [Obsolete("Please use the Probability method instead.")]
        public double Compute(double[] input)
        {
            return Probability(input);
        }

        /// <summary>
        ///     Computes the model output for each of the given input vectors.
        /// </summary>
        /// <param name="input">The array of input vectors.</param>
        /// <returns>The array of output values.</returns>
        [Obsolete("Please use the Probability method instead.")]
        public double[] Compute(double[][] input)
        {
            return Probability(input);
        }

        /// <summary>
        ///     Gets the Wald Test for a given coefficient.
        /// </summary>
        /// <remarks>
        ///     The Wald statistical test is a test for a model parameter in which
        ///     the estimated parameter θ is compared with another proposed parameter
        ///     under the assumption that the difference between them will be approximately
        ///     normal. There are several problems with the use of the Wald test. Please
        ///     take a look on substitute tests based on the log-likelihood if possible.
        /// </remarks>
        /// <param name="index">
        ///     The coefficient's index. The first value
        ///     (at zero index) is the intercept value.
        /// </param>
        public WaldTest GetWaldTest(int index)
        {
            // TODO: Eventually change this function so index is 0-based instead of 1-based
            //       (user could select -1 for the intercept term, or no arguments at all)
            return new WaldTest(GetCoefficient(index), 0.0, standardErrors[index]);
        }

        /// <summary>
        ///     Gets the Log-Likelihood for the model.
        /// </summary>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <returns>
        ///     The Log-Likelihood (a measure of performance) of
        ///     the model calculated over the given data sets.
        /// </returns>
        public double GetLogLikelihood(double[][] input, double[] output)
        {
            double sum = 0;

            for (var i = 0; i < input.Length; i++)
            {
                var actualOutput = Probability(input[i]);
                var expectedOutput = output[i];

                if (actualOutput != 0)
                {
                    sum += expectedOutput * System.Math.Log(actualOutput);
                }

                if (actualOutput != 1)
                {
                    sum += (1 - expectedOutput) * System.Math.Log(1 - actualOutput);
                }

                Debug.Assert(!double.IsNaN(sum));
            }

            return sum;
        }

        /// <summary>
        ///     Gets the Log-Likelihood for the model.
        /// </summary>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <param name="weights">The weights associated with each input vector.</param>
        /// <returns>
        ///     The Log-Likelihood (a measure of performance) of
        ///     the model calculated over the given data sets.
        /// </returns>
        public double GetLogLikelihood(double[][] input, double[] output, double[] weights)
        {
            double sum = 0;

            for (var i = 0; i < input.Length; i++)
            {
                var w = weights[i];
                var actualOutput = Probability(input[i]);
                var expectedOutput = output[i];

                if (actualOutput != 0)
                {
                    sum += expectedOutput * System.Math.Log(actualOutput) * w;
                }

                if (actualOutput != 1)
                {
                    sum += (1 - expectedOutput) * System.Math.Log(1 - actualOutput) * w;
                }

                Debug.Assert(!double.IsNaN(sum));
            }

            return sum;
        }

        /// <summary>
        ///     Gets the Deviance for the model.
        /// </summary>
        /// <remarks>
        ///     The deviance is defined as -2*Log-Likelihood.
        /// </remarks>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <returns>
        ///     The deviance (a measure of performance) of the model
        ///     calculated over the given data sets.
        /// </returns>
        public double GetDeviance(double[][] input, double[] output)
        {
            return -2.0 * GetLogLikelihood(input, output);
        }

        /// <summary>
        ///     Gets the Deviance for the model.
        /// </summary>
        /// <remarks>
        ///     The deviance is defined as -2*Log-Likelihood.
        /// </remarks>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <param name="weights">The weights associated with each input vector.</param>
        /// <returns>
        ///     The deviance (a measure of performance) of the model
        ///     calculated over the given data sets.
        /// </returns>
        public double GetDeviance(double[][] input, double[] output, double[] weights)
        {
            return -2.0 * GetLogLikelihood(input, output, weights);
        }

        /// <summary>
        ///     Gets the Log-Likelihood Ratio between two models.
        /// </summary>
        /// <remarks>
        ///     The Log-Likelihood ratio is defined as 2*(LL - LL0).
        /// </remarks>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <param name="regression">Another Logistic Regression model.</param>
        /// <returns>
        ///     The Log-Likelihood ratio (a measure of performance
        ///     between two models) calculated over the given data sets.
        /// </returns>
        public double GetLogLikelihoodRatio(double[][] input, double[] output, GeneralizedLinearRegression regression)
        {
            return 2.0 * (GetLogLikelihood(input, output) - regression.GetLogLikelihood(input, output));
        }

        /// <summary>
        ///     Gets the Log-Likelihood Ratio between two models.
        /// </summary>
        /// <remarks>
        ///     The Log-Likelihood ratio is defined as 2*(LL - LL0).
        /// </remarks>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <param name="weights">The weights associated with each input vector.</param>
        /// <param name="regression">Another Logistic Regression model.</param>
        /// <returns>
        ///     The Log-Likelihood ratio (a measure of performance
        ///     between two models) calculated over the given data sets.
        /// </returns>
        public double GetLogLikelihoodRatio(double[][] input, double[] output, double[] weights,
            GeneralizedLinearRegression regression)
        {
            return 2.0 *
                   (GetLogLikelihood(input, output, weights) - regression.GetLogLikelihood(input, output, weights));
        }

        /// <summary>
        ///     The likelihood ratio test of the overall model, also called the model chi-square test.
        /// </summary>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <remarks>
        ///     <para>
        ///         The Chi-square test, also called the likelihood ratio test or the log-likelihood test
        ///         is based on the deviance of the model (-2*log-likelihood). The log-likelihood ratio test
        ///         indicates whether there is evidence of the need to move from a simpler model to a more
        ///         complicated one (where the simpler model is nested within the complicated one).
        ///     </para>
        ///     <para>
        ///         The difference between the log-likelihood ratios for the researcher's model and a
        ///         simpler model is often called the "model chi-square".
        ///     </para>
        /// </remarks>
        public ChiSquareTest ChiSquare(double[][] input, double[] output)
        {
            double y0 = 0;
            double y1 = 0;

            for (var i = 0; i < output.Length; i++)
            {
                y0 += 1.0 - output[i];
                y1 += output[i];
            }

            var regression = new GeneralizedLinearRegression(linkFunction)
            {
                NumberOfInputs = NumberOfInputs,
                Intercept = System.Math.Log(y1 / y0)
            };

            var ratio = GetLogLikelihoodRatio(input, output, regression);
            return new ChiSquareTest(ratio, NumberOfInputs);
        }

        /// <summary>
        ///     The likelihood ratio test of the overall model, also called the model chi-square test.
        /// </summary>
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <param name="weights">The weights associated with each input vector.</param>
        /// <remarks>
        ///     <para>
        ///         The Chi-square test, also called the likelihood ratio test or the log-likelihood test
        ///         is based on the deviance of the model (-2*log-likelihood). The log-likelihood ratio test
        ///         indicates whether there is evidence of the need to move from a simpler model to a more
        ///         complicated one (where the simpler model is nested within the complicated one).
        ///     </para>
        ///     <para>
        ///         The difference between the log-likelihood ratios for the researcher's model and a
        ///         simpler model is often called the "model chi-square".
        ///     </para>
        /// </remarks>
        public ChiSquareTest ChiSquare(double[][] input, double[] output, double[] weights)
        {
            double y0 = 0;
            double y1 = 0;

            for (var i = 0; i < output.Length; i++)
            {
                y0 += (1.0 - output[i]) * weights[i];
                y1 += output[i] * weights[i];
            }

            var regression = new GeneralizedLinearRegression(linkFunction)
            {
                NumberOfInputs = NumberOfInputs,
                Intercept = System.Math.Log(y1 / y0)
            };

            var ratio = GetLogLikelihoodRatio(input, output, weights, regression);
            return new ChiSquareTest(ratio, NumberOfInputs);
        }

        /// <summary>
        ///     Gets the degrees of freedom when fitting the regression.
        /// </summary>
        public double GetDegreesOfFreedom(int numberOfSamples)
        {
            return Linear.GetDegreesOfFreedom(numberOfSamples);
        }

        /// <summary>
        ///     Gets the standard error for each coefficient.
        /// </summary>
        /// <param name="informationMatrix">
        ///     The information matrix obtained when training the model (see
        ///     <see cref="OrdinaryLeastSquares.GetInformationMatrix()" />).
        /// </param>
        public double[] GetStandardErrors(double[][] informationMatrix)
        {
            var se = new double[informationMatrix.Length];
            for (var i = 0; i < se.Length; i++)
            {
                se[i] = System.Math.Sqrt(informationMatrix[i][i]);
            }

            return se;
        }

        /// <summary>
        ///     Gets the standard error of the fit for a particular input vector.
        /// </summary>
        /// <param name="input">The input vector where the standard error of the fit should be computed.</param>
        /// <param name="informationMatrix">
        ///     The information matrix obtained when training the model (see
        ///     <see cref="OrdinaryLeastSquares.GetInformationMatrix()" />).
        /// </param>
        /// <returns>The standard error of the fit at the given input point.</returns>
        public double GetStandardError(double[] input, double[][] informationMatrix)
        {
            var rim = predictionVariance(input, informationMatrix);
            return System.Math.Sqrt(rim);
        }

        /// <summary>
        ///     Gets the standard error of the prediction for a particular input vector.
        /// </summary>
        /// <param name="input">The input vector where the standard error of the prediction should be computed.</param>
        /// <param name="informationMatrix">
        ///     The information matrix obtained when training the model (see
        ///     <see cref="OrdinaryLeastSquares.GetInformationMatrix()" />).
        /// </param>
        /// <returns>The standard error of the prediction given for the input point.</returns>
        public double GetPredictionStandardError(double[] input, double[][] informationMatrix)
        {
            var rim = predictionVariance(input, informationMatrix);
            return System.Math.Sqrt(1 + rim);
        }

        /// <summary>
        ///     Gets the confidence interval for an input point.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="numberOfSamples">The number of training samples used to fit the model.</param>
        /// <param name="informationMatrix">
        ///     The information matrix obtained when training the model (see
        ///     <see cref="OrdinaryLeastSquares.GetInformationMatrix()" />).
        /// </param>
        /// <param name="percent">The prediction interval confidence (default is 95%).</param>
        public DoubleRange GetConfidenceInterval(double[] input, int numberOfSamples, double[][] informationMatrix,
            double percent = 0.95)
        {
            var se = GetStandardError(input, informationMatrix);
            return computeInterval(input, numberOfSamples, percent, se);
        }

        /// <summary>
        ///     Gets the prediction interval for an input point.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="numberOfSamples">The number of training samples used to fit the model.</param>
        /// <param name="informationMatrix">
        ///     The information matrix obtained when training the model (see
        ///     <see cref="OrdinaryLeastSquares.GetInformationMatrix()" />).
        /// </param>
        /// <param name="percent">The prediction interval confidence (default is 95%).</param>
        public DoubleRange GetPredictionInterval(double[] input, int numberOfSamples, double[][] informationMatrix,
            double percent = 0.95)
        {
            var se = GetPredictionStandardError(input, informationMatrix);
            return computeInterval(input, numberOfSamples, percent, se);
        }

        private static double predictionVariance(double[] input, double[][] im)
        {
            if (input.Length < im.Length)
            {
                input = 1.0.Concatenate(input);
            }

            return input.Dot(im).Dot(input);
        }

        private DoubleRange computeInterval(double[] input, int numberOfSamples, double percent, double se)
        {
            var y = linear.Transform(input);
            var df = GetDegreesOfFreedom(numberOfSamples);
            var t = new TTest(y, se, df);
            var lci = t.GetConfidenceInterval(percent);
            var nci = new DoubleRange(linkFunction.Inverse(lci.Min), linkFunction.Inverse(lci.Max));
            return nci;
        }

        /// <summary>
        ///     Creates a GeneralizedLinearRegression from a <see cref="LogisticRegression" /> object.
        /// </summary>
        /// <param name="regression">A <see cref="LogisticRegression" /> object.</param>
        /// <param name="makeCopy">
        ///     True to make a copy of the logistic regression values, false
        ///     to use the actual values. If the actual values are used, changes done on one model
        ///     will be reflected on the other model.
        /// </param>
        /// <returns>
        ///     A new <see cref="GeneralizedLinearRegression" /> which is a copy of the
        ///     given <see cref="LogisticRegression" />.
        /// </returns>
        [Obsolete(
            "Simply cast the logistic regression to a GeneralizedLinearRegression instead, using Clone() if necessary.")]
        public static GeneralizedLinearRegression FromLogisticRegression(LogisticRegression regression, bool makeCopy)
        {
#pragma warning disable 612, 618
            if (makeCopy)
            {
                var coefficients = (double[])regression.Coefficients.Clone();
                var standardErrors = (double[])regression.StandardErrors.Clone();
                return new GeneralizedLinearRegression(new LogitLinkFunction(),
                    coefficients, standardErrors);
            }

            return new GeneralizedLinearRegression(new LogitLinkFunction(),
                regression.Coefficients, regression.StandardErrors);
#pragma warning restore 612, 618
        }

        /// <summary>
        ///     Computes a numerical score measuring the association between
        ///     the given <paramref name="input" /> vector and each class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the result will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        /// <returns>System.Double[].</returns>
        public override double[] Score(double[][] input, double[] result)
        {
            linear.Transform(input, result);
            //for (int i = 0; i < input.Length; i++)
            //    result[i] = linkFunction.Inverse(result[i]);
            //return result.Subtract(0.5, result: result);
            return result;
        }

        /// <summary>
        ///     Predicts a class label vector for the given input vectors, returning the
        ///     log-likelihood that the input vector belongs to its predicted class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the log-likelihoods will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        /// <returns>System.Double[].</returns>
        public override double[] LogLikelihood(double[][] input, double[] result)
        {
            linear.Transform(input, result);
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = linkFunction.Inverse(result[i]);
            }

            return result.Log(result);
        }

        /// <summary>
        ///     Predicts a class label for the given input vector, returning the
        ///     probability that the input vector belongs to its predicted class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="result">
        ///     An array where the probabilities will be stored,
        ///     avoiding unnecessary memory allocations.
        /// </param>
        /// <returns>System.Double[].</returns>
        public override double[] Probability(double[][] input, double[] result)
        {
            linear.Transform(input, result);
            for (var i = 0; i < input.Length; i++)
            {
                result[i] = linkFunction.Inverse(result[i]);
            }

            return result;
        }

        [OnDeserialized]
        private void SetValuesOnDeserialized(StreamingContext context)
        {
            if (linear == null)
            {
                linear = new MultipleLinearRegression
                {
#pragma warning disable 612, 618
                    Weights = coefficients.Get(1, 0),
                    Intercept = coefficients[0]
#pragma warning restore 612, 618
                };
            }
        }
    }
}