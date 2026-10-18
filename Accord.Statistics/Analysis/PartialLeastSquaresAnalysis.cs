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
using System.Collections.ObjectModel;
using System.Threading;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Properties;
using Accord.Statistics.Models.Regression.Linear;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     The PLS algorithm to use in the Partial Least Squares Analysis.
    /// </summary>
    public enum PartialLeastSquaresAlgorithm
    {
        /// <summary>
        ///     Sijmen de Jong's SIMPLS algorithm.
        /// </summary>
        /// <remarks>
        ///     The SIMPLS algorithm is considerably faster than NIPALS, especially when the number of
        ///     input variables increases; but gives slightly different results in the case of multiple
        ///     outputs.
        /// </remarks>
        SIMPLS,

        /// <summary>
        ///     Traditional NIPALS algorithm.
        /// </summary>
        NIPALS
    }

    /// <summary>
    ///     Partial Least Squares Regression/Analysis (a.k.a Projection To Latent Structures)
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Partial least squares regression (PLS-regression) is a statistical method that bears
    ///         some relation to principal components regression; instead of finding hyperplanes of
    ///         maximum variance between the response and independent variables, it finds a linear
    ///         regression model by projecting the predicted variables and the observable variables
    ///         to a new space. Because both the X and Y data are projected to new spaces, the PLS
    ///         family of methods are known as bilinear factor models.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Abdi, H. (2010). Partial least square regression, projection on latent structure regression,
    ///                     PLS-Regression. Wiley Interdisciplinary Reviews: Computational Statistics, 2, 97-106.
    ///                     Available in: http://www.utdallas.edu/~herve/abdi-wireCS-PLS2010.pdf
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Abdi, H. (2007). Partial least square regression (PLS regression). In N.J. Salkind (Ed.):
    ///                     Encyclopedia of Measurement and Statistics. Thousand Oaks (CA): Sage. pp. 740-744.
    ///                     Resource available online in: http://www.utdallas.edu/~herve/Abdi-PLS-pretty.pdf
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Martin Anderson, "A comparison of nine PLS1 algorithms". Available on:
    ///                     http://onlinelibrary.wiley.com/doi/10.1002/cem.1248/pdf
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Mevik, B-H. Wehrens, R. (2007). The pls Package: Principal Component and Partial Least Squares
    ///                     Regression in R. Journal of Statistical Software, Volume 18, Issue 2.
    ///                     Resource available online in: http://www.jstatsoft.org/v18/i02/paper
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Garson, D. Partial Least Squares Regression (PLS).
    ///                     http://faculty.chass.ncsu.edu/garson/PA765/pls.htm
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     De Jong, S. (1993). SIMPLS: an alternative approach to partial least squares regression.
    ///                     Chemometrics and Intelligent Laboratory Systems, 18: 251–263.
    ///                     http://dx.doi.org/10.1016/0169-7439(93)85002-X
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Rosipal, Roman and Nicole Kramer. (2006). Overview and Recent Advances in Partial Least
    ///                     Squares, in Subspace, Latent Structure and Feature Selection Techniques, pp 34–51.
    ///                     http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.85.7735
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Yi Cao. (2008). Partial Least-Squares and Discriminant Analysis: A tutorial and tool
    ///                     using PLS for discriminant analysis.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Wikipedia contributors. Partial least squares regression. Wikipedia, The Free Encyclopedia;
    ///                     2009. Available from: http://en.wikipedia.org/wiki/Partial_least_squares_regression.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\PartialLeastSquaresAnalysisTest.cs" region="doc_learn" />
    /// </example>
    [Serializable]
#pragma warning disable 612, 618
    public partial class PartialLeastSquaresAnalysis : MultipleTransformBase<double[], double>,
        IMultivariateRegressionAnalysis, IProjectionAnalysis,
        ISupervisedLearning<MultivariateLinearRegression, double[], double[]>
#pragma warning restore 612, 618
    {
        private PartialLeastSquaresAlgorithm algorithm;

        private AnalysisMethod analysisMethod;
        private double[][] coeffbase;

        internal double[] componentProportionX;
        internal double[] componentProportionY;
        internal double[] cumulativeProportionX;
        internal double[] cumulativeProportionY;

        internal double[][] loadingsX;
        internal double[][] loadingsY;

        internal double[] meanX;
        internal double[] meanY;
        private int numberOfFactors;

        private bool overwriteSourceMatrix;
        internal double[][] scoresX;
        internal double[][] scoresY;

        [Obsolete] internal double[,] sourceX;
        [Obsolete] internal double[,] sourceY;
        internal double[] stdDevX;
        internal double[] stdDevY;
        [NonSerialized] private CancellationToken token;

        /// <summary>
        ///     Constructs a new Partial Least Squares Analysis.
        /// </summary>
        /// <param name="inputs">The input source data to perform analysis.</param>
        /// <param name="outputs">The output source data to perform analysis.</param>
        [Obsolete("Please pass the 'inputs' and 'outputs' matrices to the Learn method instead.")]
        public PartialLeastSquaresAnalysis(double[,] inputs, double[,] outputs)
            : this(inputs, outputs, AnalysisMethod.Center, PartialLeastSquaresAlgorithm.NIPALS)
        {
        }

        /// <summary>
        ///     Constructs a new Partial Least Squares Analysis.
        /// </summary>
        /// <param name="inputs">The input source data to perform analysis.</param>
        /// <param name="outputs">The output source data to perform analysis.</param>
        /// <param name="algorithm">
        ///     The PLS algorithm to use in the analysis. Default is
        ///     <see cref="PartialLeastSquaresAlgorithm.NIPALS" />.
        /// </param>
        [Obsolete("Please pass the 'inputs' and 'outputs' matrices to the Learn method instead.")]
        public PartialLeastSquaresAnalysis(double[,] inputs, double[,] outputs, PartialLeastSquaresAlgorithm algorithm)
            : this(inputs, outputs, AnalysisMethod.Center, algorithm)
        {
        }

        /// <summary>
        ///     Constructs a new Partial Least Squares Analysis.
        /// </summary>
        /// <param name="inputs">The input source data to perform analysis.</param>
        /// <param name="outputs">The output source data to perform analysis.</param>
        /// <param name="method">The analysis method to perform. Default is <see cref="AnalysisMethod.Center" />.</param>
        /// <param name="algorithm">
        ///     The PLS algorithm to use in the analysis. Default is
        ///     <see cref="PartialLeastSquaresAlgorithm.NIPALS" />.
        /// </param>
        [Obsolete("Please pass the 'inputs' and 'outputs' matrices to the Learn method instead.")]
        public PartialLeastSquaresAnalysis(double[,] inputs, double[,] outputs, AnalysisMethod method,
            PartialLeastSquaresAlgorithm algorithm)
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

            if (inputs.GetLength(0) != outputs.GetLength(0))
            {
                throw new ArgumentException(
                    "The number of rows in the inputs array must match the number of rows in the outputs array.");
            }


            analysisMethod = method;
            this.algorithm = algorithm;

#pragma warning disable 612, 618
            sourceX = inputs;
            sourceY = outputs;
#pragma warning restore 612, 618

            // Calculate common measures to speedup other calculations
            meanX = inputs.Mean(0);
            meanY = outputs.Mean(0);
            stdDevX = inputs.StandardDeviation(meanX);
            stdDevY = outputs.StandardDeviation(meanY);

            base.NumberOfInputs = sourceX.Columns();
            base.NumberOfOutputs = NumberOfInputs;

            Predictors = new PartialLeastSquaresVariables(this, true);
            Dependents = new PartialLeastSquaresVariables(this, false);
        }

        /// <summary>
        ///     Constructs a new Partial Least Squares Analysis.
        /// </summary>
        /// <param name="method">The analysis method to perform. Default is <see cref="AnalysisMethod.Center" />.</param>
        /// <param name="algorithm">
        ///     The PLS algorithm to use in the analysis. Default is
        ///     <see cref="PartialLeastSquaresAlgorithm.NIPALS" />.
        /// </param>
        public PartialLeastSquaresAnalysis(AnalysisMethod method = AnalysisMethod.Center,
            PartialLeastSquaresAlgorithm algorithm = PartialLeastSquaresAlgorithm.NIPALS)
        {
            analysisMethod = method;
            this.algorithm = algorithm;
        }

        /// <summary>
        ///     Gets information about independent (input) variables.
        /// </summary>
        public PartialLeastSquaresVariables Predictors { get; private set; }

        /// <summary>
        ///     Gets information about dependent (output) variables.
        /// </summary>
        public PartialLeastSquaresVariables Dependents { get; private set; }

        /// <summary>
        ///     Gets the Weight matrix obtained during the analysis. For the NIPALS algorithm
        ///     this is the W matrix. For the SIMPLS algorithm this is the R matrix.
        /// </summary>
        public double[][] Weights { get; private set; }

        /// <summary>
        ///     Gets information about the factors discovered during the analysis in a
        ///     object-oriented structure which can be data-bound directly to many controls.
        /// </summary>
        public PartialLeastSquaresFactorCollection Factors { get; private set; }

        /// <summary>
        ///     Gets or sets the PLS algorithm used by the analysis.
        /// </summary>
        public PartialLeastSquaresAlgorithm Algorithm
        {
            get => algorithm;
            set => algorithm = value;
        }

        /// <summary>
        ///     Gets or sets the method used by this analysis.
        /// </summary>
        public AnalysisMethod Method
        {
            get => analysisMethod;
            set => analysisMethod = value;
        }

        /// <summary>
        ///     Gets the Variable Importance in Projection (VIP).
        /// </summary>
        /// <remarks>
        ///     This method has been implemented considering only PLS
        ///     models fitted using the NIPALS algorithm containing a
        ///     single response (output) variable.
        /// </remarks>
        public double[][] Importance { get; private set; }

        /// <summary>
        ///     Gets or sets whether calculations will be performed overwriting
        ///     data in the original source matrix, using less memory.
        /// </summary>
        public bool Overwrite
        {
            get => overwriteSourceMatrix;
            set => overwriteSourceMatrix = value;
        }

        /// <summary>
        ///     Gets or sets the number of latent factors that can be considered in this model.
        /// </summary>
        public int NumberOfLatentFactors
        {
            get => numberOfFactors;
            set
            {
                if (value < 0 || (MaximumNumberOfFactors > 0 && value > MaximumNumberOfFactors))
                {
                    throw new ArgumentOutOfRangeException("value",
                        "Number of latent factors should be less than or equal the number of columns in the input data {0}."
                            .Format(MaximumNumberOfFactors));
                }

                numberOfFactors = value;
            }
        }

        /// <summary>
        ///     Gets the number of inputs accepted by the model.
        /// </summary>
        /// <value>The number of inputs.</value>
        /// <exception cref="InvalidOperationException">This property is read-only.</exception>
        public override int NumberOfInputs
        {
            get => base.NumberOfInputs;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        /// <summary>
        ///     Gets the number of outputs generated by the model.
        /// </summary>
        /// <value>The number of outputs.</value>
        /// <exception cref="InvalidOperationException">This property is read-only.</exception>
        public override int NumberOfOutputs
        {
            get => base.NumberOfOutputs;
            set => throw new InvalidOperationException("This property is read-only.");
        }

        /// <summary>
        ///     Gets the maximum number of latent factors that can be considered in this model.
        /// </summary>
        public int MaximumNumberOfFactors { get; private set; }

        /// <summary>
        ///     Computes the Partial Least Squares Analysis.
        /// </summary>
        [Obsolete("Please use the Learn method instead.")]
        public void Compute()
        {
            // maxFactors = min(rows-1,cols)
            MaximumNumberOfFactors = System.Math.Min(sourceX.GetLength(0) - 1, sourceX.GetLength(1));

            Compute(MaximumNumberOfFactors);
        }

        /// <summary>
        ///     Projects a given set of inputs into latent space.
        /// </summary>
        [Obsolete("Please use jagged matrices instead.")]
        public double[,] Transform(double[,] data)
        {
            return Transform(data, loadingsX.GetLength(1));
        }

        /// <summary>
        ///     Projects a given set of inputs into latent space.
        /// </summary>
        [Obsolete("Please use jagged matrices instead.")]
        public double[,] Transform(double[,] data, int dimensions)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var rows = data.GetLength(0);
            var cols = data.GetLength(1);

            if (cols > loadingsX.GetLength(0))
            {
                throw new DimensionMismatchException("data",
                    "The data matrix should have a number of columns less than or equal to"
                    + " the number of rows in the loadings matrix for the input variables.");
            }

            var result = new double[rows, dimensions];
            var source = Adjust(data, meanX, stdDevX, false);

            // multiply the data matrix by the selected factors
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < dimensions; j++)
                {
                    for (var k = 0; k < cols; k++)
                    {
                        result[i, j] += source[i, k] * loadingsX[k][j];
                    }
                }
            }

            return result;
        }

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
        public MultivariateLinearRegression Learn(double[][] x, double[][] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            // maxFactors = min(rows-1, cols)
            MaximumNumberOfFactors = System.Math.Min(x.Rows() - 1, x.Columns());

            Predictors = new PartialLeastSquaresVariables(this, true);
            Dependents = new PartialLeastSquaresVariables(this, false);

            base.NumberOfInputs = x.Columns();
            base.NumberOfOutputs = y.Columns();
            if (NumberOfLatentFactors == 0 || NumberOfLatentFactors > MaximumNumberOfFactors)
            {
                NumberOfLatentFactors = MaximumNumberOfFactors;
            }

            meanX = x.Mean(0);
            meanY = y.Mean(0);
            stdDevX = x.StandardDeviation(meanX);
            stdDevY = y.StandardDeviation(meanY);

            // Initialize and prepare the data
            var inputs = Adjust(x, meanX, stdDevX, Overwrite);
            var outputs = Adjust(y, meanY, null, Overwrite);


            // Run selected algorithm
            if (algorithm == PartialLeastSquaresAlgorithm.SIMPLS)
            {
                simpls(inputs, outputs, MaximumNumberOfFactors);
            }
            else
            {
                nipals(inputs, outputs, MaximumNumberOfFactors, 0);
            }


            // Calculate cumulative proportions
            cumulativeProportionX = new double[MaximumNumberOfFactors];
            cumulativeProportionY = new double[MaximumNumberOfFactors];
            cumulativeProportionX[0] = componentProportionX[0];
            cumulativeProportionY[0] = componentProportionY[0];
            for (var i = 1; i < MaximumNumberOfFactors; i++)
            {
                cumulativeProportionX[i] = cumulativeProportionX[i - 1] + componentProportionX[i];
                cumulativeProportionY[i] = cumulativeProportionY[i - 1] + componentProportionY[i];
            }


            // Compute Variable Importance in Projection (VIP)
            Importance = ComputeVariableImportanceInProjection(MaximumNumberOfFactors);


            // Create the object-oriented structure to hold the partial least squares factors
            var array = new PartialLeastSquaresFactor[MaximumNumberOfFactors];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = new PartialLeastSquaresFactor(this, i);
            }

            Factors = new PartialLeastSquaresFactorCollection(array);

            return CreateRegression();
        }

        /// <summary>
        ///     Computes the Partial Least Squares Analysis.
        /// </summary>
        /// <param name="factors">
        ///     The number of factors to compute. The number of factors
        ///     should be a value between 1 and min(rows-1,cols) where
        ///     rows and columns are the number of observations and
        ///     variables in the input source data matrix.
        /// </param>
        [Obsolete("Please set the NumberOfOutputs property and use the Learn method instead.")]
        public void Compute(int factors)
        {
#pragma warning disable 612, 618
            // maxFactors = min(rows-1,cols)
            MaximumNumberOfFactors = System.Math.Min(sourceX.GetLength(0) - 1, sourceX.GetLength(1));

            if (factors > MaximumNumberOfFactors)
            {
                throw new ArgumentOutOfRangeException("factors");
            }

            // Initialize and prepare the data
            var inputs = Adjust(sourceX, meanX, stdDevX, Overwrite);
            var outputs = Adjust(sourceY, meanY, null, Overwrite);

            NumberOfLatentFactors = factors;
            base.NumberOfInputs = sourceX.Columns();
            base.NumberOfOutputs = sourceY.Columns();

            // Run selected algorithm
            if (algorithm == PartialLeastSquaresAlgorithm.SIMPLS)
            {
                simpls(inputs.ToJagged(), outputs.ToJagged(), factors);
            }
            else
            {
                nipals(inputs.ToJagged(), outputs.ToJagged(), factors, 0);
            }


            // Calculate cumulative proportions
            cumulativeProportionX = new double[factors];
            cumulativeProportionY = new double[factors];
            cumulativeProportionX[0] = componentProportionX[0];
            cumulativeProportionY[0] = componentProportionY[0];
            for (var i = 1; i < factors; i++)
            {
                cumulativeProportionX[i] = cumulativeProportionX[i - 1] + componentProportionX[i];
                cumulativeProportionY[i] = cumulativeProportionY[i - 1] + componentProportionY[i];
            }


            // Compute Variable Importance in Projection (VIP)
            Importance = ComputeVariableImportanceInProjection(factors);

            // Create the object-oriented structure to hold the partial least squares factors
            var array = new PartialLeastSquaresFactor[factors];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = new PartialLeastSquaresFactor(this, i);
            }

            Factors = new PartialLeastSquaresFactorCollection(array);
#pragma warning restore 612, 618
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
            var source = Adjust(input, meanX, stdDevX, false);

            // multiply the data matrix by the selected factors
            for (var i = 0; i < source.Length; i++)
            {
                for (var j = 0; j < NumberOfOutputs; j++)
                {
                    for (var k = 0; k < source[i].Length; k++)
                    {
                        result[i][j] += source[i][k] * loadingsX[k][j];
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Projects a given set of outputs into latent space.
        /// </summary>
        public double[][] TransformOutput(double[][] outputs)
        {
            return TransformOutput(outputs, loadingsY.Columns());
        }

        /// <summary>
        ///     Projects a given set of outputs into latent space.
        /// </summary>
        [Obsolete("Please use jagged matrices instead.")]
        public double[,] TransformOutput(double[,] outputs)
        {
            return TransformOutput(outputs.ToJagged(), loadingsY.Columns()).ToMatrix();
        }

        /// <summary>
        ///     Projects a given set of outputs into latent space.
        /// </summary>
        [Obsolete("Please use jagged matrices instead.")]
        public double[,] TransformOutput(double[,] outputs, int dimensions)
        {
            return TransformOutput(outputs.ToJagged(), dimensions).ToMatrix();
        }

        /// <summary>
        ///     Projects a given set of outputs into latent space.
        /// </summary>
        public double[][] TransformOutput(double[][] outputs, int dimensions)
        {
            var rows = outputs.Rows();
            var cols = outputs.Columns();

            if (cols > loadingsY.Rows())
            {
                throw new DimensionMismatchException("outputs",
                    "The data matrix should have a number of columns less than or equal to"
                    + " the number of rows in the loadings matrix for the input variables.");
            }

            var result = Jagged.Zeros(rows, dimensions);
            var source = Adjust(outputs, meanY, stdDevY, false);

            // multiply the data matrix by the selected factors
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < dimensions; j++)
                {
                    for (var k = 0; k < cols; k++)
                    {
                        result[i][j] += source[i][k] * loadingsY[k][j];
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Creates a Multivariate Linear Regression model using
        ///     coefficients obtained by the Partial Least Squares.
        /// </summary>
        public MultivariateLinearRegression CreateRegression()
        {
            return CreateRegression(NumberOfLatentFactors);
        }

        /// <summary>
        ///     Creates a Multivariate Linear Regression model using
        ///     coefficients obtained by the Partial Least Squares.
        /// </summary>
        public MultivariateLinearRegression CreateRegression(int factors)
        {
            if (factors > Factors.Count)
            {
                throw new ArgumentOutOfRangeException("factors",
                    "The number of factors should be equal to or less than the number of factors computed in the analysis.");
            }

            //  Compute regression coefficients B of Y on X as B = RQ'
            var B = Jagged.Zeros(coeffbase.Length, loadingsY.Length);
            for (var i = 0; i < coeffbase.Length; i++)
            {
                for (var j = 0; j < loadingsY.Length; j++)
                {
                    for (var k = 0; k < factors; k++)
                    {
                        B[i][j] += coeffbase[i][k] * loadingsY[j][k];
                    }
                }
            }

            // Divide by standard deviation if X has been normalized
            if (analysisMethod == AnalysisMethod.Standardize)
            {
                for (var i = 0; i < B.Length; i++)
                {
                    for (var j = 0; j < B[i].Length; j++)
                    {
                        B[i][j] = B[i][j] / stdDevX[i];
                    }
                }
            }

            // Compute regression intercepts A as A = meanY - meanX'*B
            var A = new double[loadingsY.Length];
            for (var i = 0; i < meanY.Length; i++)
            {
                var sum = 0.0;
                for (var j = 0; j < B.Length; j++)
                {
                    sum += meanX[j] * B[j][i];
                }

                A[i] = meanY[i] - sum;
            }

            return new MultivariateLinearRegression
            {
                Weights = B,
                Intercepts = A
            };
        }

        /// <summary>
        ///     Adjusts a data matrix, centering and standardizing its values
        ///     using the already computed column's means and standard deviations.
        /// </summary>
        protected double[,] Adjust(double[,] matrix, double[] columnMeans, double[] columnStdDev, bool inPlace)
        {
            // Center the data around the mean. Will have no effect if
            //  the data is already centered (the mean will be zero).
            var result = matrix.Center(columnMeans, inPlace);

            // Check if we also have to standardize our data (convert to Z Scores).
            if (columnStdDev != null && analysisMethod == AnalysisMethod.Standardize)
            {
                for (var j = 0; j < columnStdDev.Length; j++)
                {
                    if (columnStdDev[j] == 0)
                    {
                        throw new ArithmeticException("Standard deviation cannot be" +
                                                      " zero (cannot standardize the constant variable at column index " +
                                                      j + ").");
                    }
                }

                // Yes. Divide by standard deviation
                result.Standardize(columnStdDev, true);
            }

            return result;
        }

        /// <summary>
        ///     Adjusts a data matrix, centering and standardizing its values
        ///     using the already computed column's means and standard deviations.
        /// </summary>
        protected double[][] Adjust(double[][] matrix, double[] columnMeans, double[] columnStdDev, bool inPlace)
        {
            // Center the data around the mean. Will have no effect if
            //  the data is already centered (the mean will be zero).
            var result = matrix.Center(columnMeans, inPlace);

            // Check if we also have to standardize our data (convert to Z Scores).
            if (columnStdDev != null && analysisMethod == AnalysisMethod.Standardize)
            {
                for (var j = 0; j < columnStdDev.Length; j++)
                {
                    if (columnStdDev[j] == 0)
                    {
                        throw new ArithmeticException("Standard deviation cannot be" +
                                                      " zero (cannot standardize the constant variable at column index " +
                                                      j + ").");
                    }
                }

                // Yes. Divide by standard deviation
                result.Standardize(columnStdDev, true);
            }

            return result;
        }

        /// <summary>
        ///     Returns the index for the column with largest squared sum.
        /// </summary>
        private static int largest(double[][] matrix)
        {
            var rows = matrix.Rows();
            var cols = matrix.Columns();

            var index = 0;
            double max = 0;
            for (var i = 0; i < cols; i++)
            {
                var squareSum = 0.0;

                for (var j = 0; j < rows; j++)
                {
                    squareSum += matrix[j][i] * matrix[j][i];
                }

                if (squareSum > max)
                {
                    max = squareSum;
                    index = i;
                }
            }

            return index;
        }

        /// <summary>
        ///     Computes the variable importance in projection (VIP).
        /// </summary>
        /// <returns>
        ///     A predictor factors matrix in which each row represents
        ///     the importance of the variable in a projection considering
        ///     the number of factors indicated by the column's index.
        /// </returns>
        /// <remarks>
        ///     References:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 Il-Gyo Chong, Chi-Hyuck Jun, Performance of some variable selection methods
        ///                 when multicollinearity is present, Chemometrics and Intelligent Laboratory
        ///                 Systems, Volume 78, Issues 1-2, 28 July 2005, Pages 103-112, ISSN 0169-7439,
        ///                 DOI: 10.1016/j.chemolab.2004.12.011.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        protected double[][] ComputeVariableImportanceInProjection(int factors)
        {
            // Tested against VIP.R code from Bjørn-Helge Mevik.
            // Available on http://mevik.net/work/software/VIP.R

            var xcols = NumberOfInputs;

            var importance = Jagged.Zeros(xcols, factors);

            // For each input variable
            for (var j = 0; j < xcols; j++)
            {
                var SS1 = new double[factors];
                var SS2 = new double[factors];

                // For each latent factor
                for (var k = 0; k < factors; k++)
                {
                    // Assume single response variable
                    var b = loadingsY.GetColumn(k)[0];
                    var t = scoresX.GetColumn(k);
                    var w = loadingsX.GetColumn(k);

                    var ss = b * b * t.Dot(t);
                    var wn = w[j] * w[j] / w.SquareEuclidean();

                    SS1[k] = ss * wn;
                    SS2[k] = ss;
                }

                var sum1 = SS1.CumulativeSum();
                var sum2 = SS2.CumulativeSum();

                for (var k = 0; k < factors; k++)
                {
                    importance[j][k] = System.Math.Sqrt(xcols * sum1[k] / sum2[k]);
                }
            }

            return importance;
        }

#pragma warning disable 612, 618
        /// <summary>
        ///     Source data used in the analysis.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Source => sourceX;

        /// <summary>
        ///     Gets the dependent variables' values
        ///     for each of the source input points.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Output => sourceY;
#pragma warning restore 612, 618

        #region Partial Least Squares Algorithms

        /// <summary>
        ///     Computes PLS parameters using NIPALS algorithm.
        /// </summary>
        /// <param name="factors">The number of factors to compute.</param>
        /// <param name="inputsX">The mean-centered input values X.</param>
        /// <param name="outputsY">The mean-centered output values Y.</param>
        /// <param name="tolerance">The tolerance for convergence.</param>
        /// <remarks>
        ///     <para>
        ///         The algorithm implementation follows the original paper by Hervé
        ///         Abdi, with overall structure as suggested in Yi Cao's tutorial.
        ///     </para>
        ///     <para>
        ///         References:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     Abdi, H. (2010). Partial least square regression, projection on latent structure regression,
        ///                     PLS-Regression. Wiley Interdisciplinary Reviews: Computational Statistics, 2, 97-106.
        ///                     Available in: http://www.utdallas.edu/~herve/abdi-wireCS-PLS2010.pdf
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     Yi Cao. (2008). Partial Least-Squares and Discriminant Analysis: A tutorial and tool
        ///                     using PLS for discriminant analysis.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        private void nipals(double[][] inputsX, double[][] outputsY, int factors, double tolerance)
        {
            var run = new NipalsRun(this, inputsX, outputsY, factors);
            run.Extract(tolerance);

            // Solve the linear system R = inv(P')*B
            //var svd = new JaggedSingularValueDecomposition(P.Transpose());
            //this.coeffbase = svd.SolveForDiagonal(B);
            coeffbase = run.P.Transpose().SolveForDiagonal(run.B, true);

            // Set class variables
            scoresX = run.T; // factor score matrix T
            scoresY = run.U; // factor score matrix U
            loadingsX = run.P; // loading matrix P, the loadings for X such that X = TP + F
            loadingsY = run.C; // loading matrix C, the loadings for Y such that Y = TC + E
            Weights = run.W; // the columns of W are weight vectors

            calculateVarianceProportions(inputsX, outputsY, factors, run.varX, run.varY);
        }

        /// <summary>
        ///     Computes PLS parameters using SIMPLS algorithm.
        /// </summary>
        /// <param name="factors">The number of factors to compute.</param>
        /// <param name="inputsX">The mean-centered input values X.</param>
        /// <param name="outputsY">The mean-centered output values Y.</param>
        /// <remarks>
        ///     <para>
        ///         The algorithm implementation is based on the appendix code by Martin Anderson,
        ///         with modifications for multiple output variables as given in the sources listed
        ///         below.
        ///     </para>
        ///     <para>
        ///         References:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     Martin Anderson, "A comparison of nine PLS1 algorithms". Available on:
        ///                     http://onlinelibrary.wiley.com/doi/10.1002/cem.1248/pdf
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     Abdi, H. (2010). Partial least square regression, projection on latent structure regression,
        ///                     PLS-Regression. Wiley Interdisciplinary Reviews: Computational Statistics, 2, 97-106.
        ///                     Available from: http://www.utdallas.edu/~herve/abdi-wireCS-PLS2010.pdf
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     StatSoft, Inc. (2012). Electronic Statistics Textbook: Partial Least Squares (PLS).
        ///                     Tulsa, OK: StatSoft. Available from: http://www.statsoft.com/textbook/partial-least-squares/#SIMPLS
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     De Jong, S. (1993). SIMPLS: an alternative approach to partial least squares regression.
        ///                     Chemometrics and Intelligent Laboratory Systems, 18: 251–263.
        ///                     http://dx.doi.org/10.1016/0169-7439(93)85002-X
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        private void simpls(double[][] inputsX, double[][] outputsY, int factors)
        {
            var run = new SimplsRun(this, inputsX, outputsY, factors);
            run.Extract();

            // Set class variables
            scoresX = run.T; // factor score matrix T
            scoresY = run.U; // factor score matrix U
            loadingsX = run.P; // loading matrix P, the loadings for X such that X = TP + F
            loadingsY = run.C; // loading matrix C, the loadings for Y such that Y = TC + E
            Weights = run.W; // the columns of W are weight vectors
            coeffbase = run.W;

            calculateVarianceProportions(inputsX, outputsY, factors, run.varX, run.varY);
        }

        private void calculateVarianceProportions(double[][] inputsX, double[][] outputsY, int factors, double[] varX,
            double[] varY)
        {
            var rows = inputsX.Rows();
            var xcols = inputsX.Columns();
            var ycols = outputsY.Columns();

            // Calculate variance explained proportions
            componentProportionX = new double[factors];
            componentProportionY = new double[factors];

            double sumX = 0.0, sumY = 0.0;
            for (var i = 0; i < rows; i++)
            {
                // Sum of squares for matrix X
                for (var j = 0; j < xcols; j++)
                {
                    sumX += inputsX[i][j] * inputsX[i][j];
                }

                // Sum of squares for matrix Y
                for (var j = 0; j < ycols; j++)
                {
                    sumY += outputsY[i][j] * outputsY[i][j];
                }
            }

            // Calculate variance proportions
            for (var i = 0; i < factors; i++)
            {
                componentProportionY[i] = varY[i] / sumY;
                componentProportionX[i] = varX[i] / sumX;
            }
        }

        #endregion
    }

    #region Support Classes

    /// <summary>
    ///     Represents a Partial Least Squares Factor found in the Partial Least Squares
    ///     Analysis, allowing it to be directly bound to controls like the DataGridView.
    /// </summary>
    [Serializable]
    public sealed class PartialLeastSquaresFactor : IAnalysisComponent
    {
        /// <summary>
        ///     Creates a partial least squares factor representation.
        /// </summary>
        /// <param name="analysis">The analysis to which this component belongs.</param>
        /// <param name="index">The component index.</param>
        internal PartialLeastSquaresFactor(PartialLeastSquaresAnalysis analysis, int index)
        {
            this.Index = index;
            this.Analysis = analysis;
        }

        /// <summary>
        ///     Returns a reference to the parent analysis object.
        /// </summary>
        public PartialLeastSquaresAnalysis Analysis { get; }

        /// <summary>
        ///     Gets the proportion of prediction variables
        ///     variance explained by this factor.
        /// </summary>
        public double PredictorProportion => Analysis.componentProportionX[Index];

        /// <summary>
        ///     Gets the cumulative proportion of dependent variables
        ///     variance explained by this factor.
        /// </summary>
        public double PredictorCumulativeProportion => Analysis.cumulativeProportionX[Index];

        /// <summary>
        ///     Gets the proportion of dependent variable
        ///     variance explained by this factor.
        /// </summary>
        public double DependentProportion => Analysis.componentProportionY[Index];

        /// <summary>
        ///     Gets the cumulative proportion of dependent variable
        ///     variance explained by this factor.
        /// </summary>
        public double DependentCumulativeProportion => Analysis.cumulativeProportionY[Index];

        /// <summary>
        ///     Gets the input variable's latent vectors for this factor.
        /// </summary>
        public double[] IndependentLatentVectors => Analysis.loadingsX.GetColumn(Index);

        /// <summary>
        ///     Gets the output variable's latent vectors for this factor.
        /// </summary>
        public double[] DependentLatentVector => Analysis.loadingsY.GetColumn(Index);

        /// <summary>
        ///     Gets the importance of each variable for the given component.
        /// </summary>
        public double[] VariableImportance => Analysis.Importance.GetColumn(Index);

        /// <summary>
        ///     Gets the Index of this component on the original factor collection.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Gets the proportion, or amount of information explained by this component.
        /// </summary>
        double IAnalysisComponent.Proportion => DependentProportion;

        /// <summary>
        ///     Gets the cumulative proportion of all discriminants until this component.
        /// </summary>
        double IAnalysisComponent.CumulativeProportion => DependentCumulativeProportion;
    }

    /// <summary>
    ///     Represents a Collection of Partial Least Squares Factors found in
    ///     the Partial Least Squares Analysis. This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public class PartialLeastSquaresFactorCollection : ReadOnlyCollection<PartialLeastSquaresFactor>
    {
        internal PartialLeastSquaresFactorCollection(PartialLeastSquaresFactor[] components)
            : base(components)
        {
        }
    }

    /// <summary>
    ///     Represents source variables used in Partial Least Squares Analysis. Can represent either
    ///     input variables (predictor variables) or output variables (independent variables or regressors).
    /// </summary>
    [Serializable]
    public class PartialLeastSquaresVariables
    {
        private PartialLeastSquaresAnalysis analysis;
        private bool inputs;

        internal PartialLeastSquaresVariables(PartialLeastSquaresAnalysis analysis, bool inputs)
        {
            this.analysis = analysis;
            this.inputs = inputs;
        }

        /// <summary>
        ///     Source data used in the analysis. Can be either input data
        ///     or output data depending if the variables chosen are predictor
        ///     variables or dependent variables, respectively.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Source
        {
#pragma warning disable 612, 618
            get { return inputs ? analysis.sourceX : analysis.sourceY; }
#pragma warning restore 612, 618
        }

        /// <summary>
        ///     Gets the resulting projection (scores) of the source data
        ///     into latent space. Can be either from input data or output
        ///     data depending if the variables chosen are predictor variables
        ///     or dependent variables, respectively.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Result => inputs ? analysis.scoresX.ToMatrix() : analysis.scoresY.ToMatrix();

        /// <summary>
        ///     Gets the column means of the source data. Can be either from
        ///     input data or output data, depending if the variables chosen
        ///     are predictor variables or dependent variables, respectively.
        /// </summary>
        public double[] Means => inputs ? analysis.meanX : analysis.meanY;

        /// <summary>
        ///     Gets the column standard deviations of the source data. Can be either
        ///     from input data or output data, depending if the variables chosen are
        ///     predictor variables or dependent variables, respectively.
        /// </summary>
        public double[] StandardDeviations => inputs ? analysis.stdDevX : analysis.stdDevY;

        /// <summary>
        ///     Gets the loadings (a.k.a factors or components) for the
        ///     variables obtained during the analysis. Can be either from
        ///     input data or output data, depending if the variables chosen
        ///     are predictor variables or dependent variables, respectively.
        /// </summary>
        public double[][] FactorMatrix => inputs ? analysis.loadingsX : analysis.loadingsY;

        /// <summary>
        ///     Gets the amount of variance explained by each latent factor.
        ///     Can be either by input variables' latent factors or output
        ///     variables' latent factors, depending if the variables chosen
        ///     are predictor variables or dependent variables, respectively.
        /// </summary>
        public double[] FactorProportions => inputs ? analysis.componentProportionX : analysis.componentProportionY;

        /// <summary>
        ///     Gets the cumulative variance explained by each latent factor.
        ///     Can be either by input variables' latent factors or output
        ///     variables' latent factors, depending if the variables chosen
        ///     are predictor variables or dependent variables, respectively.
        /// </summary>
        public double[] CumulativeProportions =>
            inputs ? analysis.cumulativeProportionX : analysis.cumulativeProportionY;

        /// <summary>
        ///     Projects a given dataset into latent space. Can be either input variable's
        ///     latent space or output variable's latent space, depending if the variables
        ///     chosen are predictor variables or dependent variables, respectively.
        /// </summary>
        public double[][] Transform(double[][] data)
        {
            return inputs ? analysis.Transform(data) : analysis.TransformOutput(data);
        }

        /// <summary>
        ///     Projects a given dataset into latent space. Can be either input variable's
        ///     latent space or output variable's latent space, depending if the variables
        ///     chosen are predictor variables or dependent variables, respectively.
        /// </summary>
        [Obsolete("Please set the analysis NumberOfOutputs to the desired number of factors.")]
        public double[][] Transform(double[][] data, int factors)
        {
            var previous = analysis.NumberOfOutputs;
            analysis.NumberOfOutputs = factors;
            var result = inputs ? analysis.Transform(data) : analysis.TransformOutput(data);
            analysis.NumberOfOutputs = previous;
            return result;
        }
    }

    #endregion
}