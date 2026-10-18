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
using Accord.MachineLearning;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics.Analysis.Base;
using Accord.Statistics.Models.Regression.Linear;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Principal component analysis (PCA) is a technique used to reduce
    ///     multidimensional data sets to lower dimensions for analysis.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Principal Components Analysis or the Karhunen-Loève expansion is a
    ///         classical method for dimensionality reduction or exploratory data
    ///         analysis.
    ///     </para>
    ///     <para>
    ///         Mathematically, PCA is a process that decomposes the covariance matrix of a matrix
    ///         into two parts: Eigenvalues and column eigenvectors, whereas Singular Value Decomposition
    ///         (SVD) decomposes a matrix per se into three parts: singular values, column eigenvectors,
    ///         and row eigenvectors. The relationships between PCA and SVD lie in that the eigenvalues
    ///         are the square of the singular values and the column vectors are the same for both.
    ///     </para>
    ///     <para>
    ///         This class uses SVD on the data set which generally gives better numerical accuracy.
    ///     </para>
    ///     <para>
    ///         This class can also be bound to standard controls such as the
    ///         <a href="http://msdn.microsoft.com/en-us/library/system.windows.forms.datagridview.aspx">DataGridView</a>
    ///         by setting their DataSource property to the analysis' <see cref="BasePrincipalComponentAnalysis.Components" />
    ///         property.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The example below shows a typical usage of the analysis. However, users
    ///         often ask why the framework produces different values than other packages
    ///         such as STATA or MATLAB. After the simple introductory example below, we
    ///         will be exploring why those results are often different.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\PrincipalComponentAnalysisTest.cs" region="doc_learn_1" />
    ///     <para>
    ///         A question often asked by users is "why my matrices have inverted
    ///         signs" or "why my results differ from [another software]". In short,
    ///         despite any differences, the results are most likely correct (unless
    ///         you firmly believe you have found a bug; in this case, please fill
    ///         in a bug report).
    ///     </para>
    ///     <para>
    ///         The example below explores, in the same steps given in Lindsay's
    ///         tutorial, anything that would cause any discrepancies between the
    ///         results given by Accord.NET and results given by other softwares.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\PrincipalComponentAnalysisTest.cs" region="doc_learn_2" />
    ///     <para>
    ///         Some users would like to analyze huge amounts of data. In this case,
    ///         computing the SVD directly on the data could result in memory exceptions
    ///         or excessive computing times. If your data's number of dimensions is much
    ///         less than the number of observations (i.e. your matrix have less columns
    ///         than rows) then it would be a better idea to summarize your data in the
    ///         form of a covariance or correlation matrix and compute PCA using the EVD.
    ///     </para>
    ///     <para>
    ///         The example below shows how to compute the analysis with covariance
    ///         matrices only.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\PrincipalComponentAnalysisTest.cs" region="doc_learn_3" />
    /// </example>
    [Serializable]
#pragma warning disable 612, 618
    public class PrincipalComponentAnalysis : BasePrincipalComponentAnalysis, ITransform<double[], double[]>,
        IUnsupervisedLearning<MultivariateLinearRegression, double[], double[]>,
        IMultivariateAnalysis, IProjectionAnalysis
#pragma warning restore 612, 618
    {
        /// <summary>
        ///     Constructs a new Principal Component Analysis.
        /// </summary>
        /// <param name="data">
        ///     The source data to perform analysis. The matrix should contain
        ///     variables as columns and observations of each variable as rows.
        /// </param>
        /// <param name="method">The analysis method to perform. Default is <see cref="AnalysisMethod.Center" />.</param>
        [Obsolete("Please pass the 'data' matrix to the Learn method instead.")]
        public PrincipalComponentAnalysis(double[][] data, AnalysisMethod method = AnalysisMethod.Center)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Length == 0)
            {
                throw new ArgumentException("Data matrix cannot be empty.", "data");
            }

            var cols = data[0].Length;
            for (var i = 0; i < data.Length; i++)
            {
                if (data[i].Length != cols)
                {
                    throw new DimensionMismatchException("data",
                        "Matrix must be rectangular. The vector at position " + i +
                        " has a different length than other vectors");
                }
            }

            array = data;
            Method = (PrincipalComponentMethod)method;
            NumberOfInputs = cols;
            NumberOfOutputs = data.Columns();
        }

        /// <summary>
        ///     Constructs a new Principal Component Analysis.
        /// </summary>
        /// <param name="data">
        ///     The source data to perform analysis. The matrix should contain
        ///     variables as columns and observations of each variable as rows.
        /// </param>
        /// <param name="method">The analysis method to perform. Default is <see cref="AnalysisMethod.Center" />.</param>
        [Obsolete("Please pass the 'data' matrix to the Learn method instead.")]
        public PrincipalComponentAnalysis(double[,] data, AnalysisMethod method = AnalysisMethod.Center)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            source = data;
            Method = (PrincipalComponentMethod)method;
            NumberOfInputs = data.Rows();
            NumberOfOutputs = data.Columns();
        }

        /// <summary>
        ///     Constructs a new Principal Component Analysis.
        /// </summary>
        /// <param name="method">The analysis method to perform. Default is <see cref="AnalysisMethod.Center" />.</param>
        /// <param name="whiten">
        ///     Whether to whiten the results or not. If set to <c>true</c> the generatred output
        ///     will be normalized to have unit standard deviation.
        /// </param>
        /// <param name="numberOfOutputs">The maximum number of components that the analysis will be able to project data into.</param>
        public PrincipalComponentAnalysis(PrincipalComponentMethod method = PrincipalComponentMethod.Center,
            bool whiten = false, int numberOfOutputs = 0)
        {
            Method = method;
            Whiten = whiten;
            NumberOfOutputs = numberOfOutputs;
        }

        /// <summary>
        ///     Computes the Principal Component Analysis algorithm.
        /// </summary>
        [Obsolete("Please use the Learn method instead.")]
        public virtual void Compute()
        {
            if (!onlyCovarianceMatrixAvailable)
            {
                int rows;

                if (array != null)
                {
                    rows = array.Length;

                    // Center and standardize the source matrix
                    var matrix = Adjust(array, Overwrite);

                    // Perform the Singular Value Decomposition (SVD) of the matrix
                    var svd = new JaggedSingularValueDecomposition(matrix,
                        true,
                        true,
                        true,
                        true);

                    SingularValues = svd.Diagonal;

                    //  The principal components of 'Source' are the eigenvectors of Cov(Source). Thus if we
                    //  calculate the SVD of 'matrix' (which is Source standardized), the columns of matrix V
                    //  (right side of SVD) will be the principal components of Source.                        

                    // The right singular vectors contains the principal components of the data matrix
                    ComponentVectors = svd.RightSingularVectors.Transpose();
                }
                else
                {
                    rows = source.GetLength(0);

                    // Center and standardize the source matrix
#pragma warning disable 612, 618
                    var matrix = Adjust(source, Overwrite);
#pragma warning restore 612, 618

                    // Perform the Singular Value Decomposition (SVD) of the matrix
                    var svd = new SingularValueDecomposition(matrix,
                        true,
                        true,
                        true,
                        true);

                    SingularValues = svd.Diagonal;

                    //  The principal components of 'Source' are the eigenvectors of Cov(Source). Thus if we
                    //  calculate the SVD of 'matrix' (which is Source standardized), the columns of matrix V
                    //  (right side of SVD) will be the principal components of Source.                        

                    // The right singular vectors contains the principal components of the data matrix
                    ComponentVectors = svd.RightSingularVectors.ToJagged(true);

                    // The left singular vectors contains the factor scores for the principal components
                }

                // Eigenvalues are the square of the singular values
                Eigenvalues = new double[SingularValues.Length];
                for (var i = 0; i < SingularValues.Length; i++)
                {
                    Eigenvalues[i] = SingularValues[i] * SingularValues[i] / (rows - 1);
                }
            }
            else
            {
                // We only have the covariance matrix. Compute the Eigenvalue decomposition
                var evd = new EigenvalueDecomposition(covarianceMatrix,
                    assumeSymmetric: true,
                    sort: true);

                // Gets the Eigenvalues and corresponding Eigenvectors
                Eigenvalues = evd.RealEigenvalues;
                var eigenvectors = evd.Eigenvectors.ToJagged();
                SingularValues = Eigenvalues.Sqrt();
                ComponentVectors = eigenvectors.Transpose();
            }

            if (Whiten)
            {
                ComponentVectors = ComponentVectors.Transpose().Divide(Eigenvalues, 0).Transpose();
            }

            CreateComponents();

            if (!onlyCovarianceMatrixAvailable)
            {
                if (array != null)
                {
                    result = Transform(array).ToMatrix();
                }
                else if (source != null)
                {
                    result = Transform(source.ToJagged()).ToMatrix();
                }
            }
        }

        /// <summary>
        ///     Projects a given matrix into principal component space.
        /// </summary>
        /// <param name="data">The matrix to be projected.</param>
        /// <param name="result">The array where to store the results.</param>
        public override double[][] Transform(double[][] data, double[][] result)
        {
            if (ComponentVectors == null)
            {
                throw new InvalidOperationException("The analysis must have been computed first.");
            }

            var rows = data.Rows();
            //int cols = data.Columns();

            // Center the data
#pragma warning disable 612, 618
            data = Adjust(data, false);
#pragma warning restore 612, 618

            // multiply the data matrix by the selected eigenvectors
            // TODO: Use cache-friendly multiplication
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < NumberOfOutputs; j++)
                {
                    for (var k = 0; k < ComponentVectors[j].Length; k++)
                    {
                        result[i][j] += data[i][k] * ComponentVectors[j][k];
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <param name="weights">The weight of importance for each input sample.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public MultivariateLinearRegression Learn(double[][] x, double[] weights = null)
        {
            NumberOfInputs = x.Columns();

            if (Method == PrincipalComponentMethod.Center || Method == PrincipalComponentMethod.Standardize)
            {
                if (weights == null)
                {
                    Means = x.Mean(0);

                    var matrix = Overwrite ? x : Jagged.CreateAs(x);
                    x.Subtract(Means, (VectorType)0, matrix);

                    if (Method == PrincipalComponentMethod.Standardize)
                    {
                        StandardDeviations = x.StandardDeviation(Means);
                        matrix.Divide(StandardDeviations, (VectorType)0, matrix);
                    }

                    //  The principal components of 'Source' are the eigenvectors of Cov(Source). Thus if we
                    //  calculate the SVD of 'matrix' (which is Source standardized), the columns of matrix V
                    //  (right side of SVD) will be the principal components of Source.                        

                    // Perform the Singular Value Decomposition (SVD) of the matrix
                    var svd = new JaggedSingularValueDecomposition(matrix,
                        false,
                        true,
                        true, true);

                    SingularValues = svd.Diagonal;
                    Eigenvalues = SingularValues.Pow(2);
                    Eigenvalues.Divide(x.Rows() - 1, Eigenvalues);
                    ComponentVectors = svd.RightSingularVectors.Transpose();
                }
                else
                {
                    Means = x.WeightedMean(weights);

                    var matrix = Overwrite ? x : Jagged.CreateAs(x);
                    x.Subtract(Means, (VectorType)0, matrix);

                    if (Method == PrincipalComponentMethod.Standardize)
                    {
                        StandardDeviations = x.WeightedStandardDeviation(weights, Means);
                        matrix.Divide(StandardDeviations, (VectorType)0, matrix);
                    }

                    var cov = x.WeightedCovariance(weights, Means);

                    // Perform the Eigenvalue Decomposition of the covariance
                    // We only have the covariance matrix. Compute the Eigenvalue decomposition
                    var evd = new EigenvalueDecomposition(cov,
                        assumeSymmetric: true, sort: true);

                    // Gets the Eigenvalues and corresponding Eigenvectors
                    Eigenvalues = evd.RealEigenvalues;
                    SingularValues = Eigenvalues.Sqrt();
                    ComponentVectors = Jagged.Transpose(evd.Eigenvectors);
                }
            }
            else if (Method == PrincipalComponentMethod.CovarianceMatrix
                     || Method == PrincipalComponentMethod.CorrelationMatrix)
            {
                if (weights != null)
                {
                    throw new Exception();
                }

                // We only have the covariance matrix. Compute the Eigenvalue decomposition
                var evd = new JaggedEigenvalueDecomposition(x,
                    assumeSymmetric: true, sort: true);

                // Gets the Eigenvalues and corresponding Eigenvectors
                Eigenvalues = evd.RealEigenvalues;
                SingularValues = Eigenvalues.Sqrt();
                ComponentVectors = evd.Eigenvectors.Transpose();
            }
            else
            {
                // The method type should have been validated before we even entered this section
                throw new InvalidOperationException("Invalid method, this should never happen: {0}".Format(Method));
            }

            if (Whiten)
            {
                ComponentVectors.Divide(SingularValues, (VectorType)1, ComponentVectors);
            }

            // Computes additional information about the analysis and creates the
            //  object-oriented structure to hold the principal components found.
            CreateComponents();

            return CreateRegression();
        }

        private MultivariateLinearRegression CreateRegression()
        {
            var weights = ComponentVectors;
            if (Method == PrincipalComponentMethod.Standardize || Method == PrincipalComponentMethod.CorrelationMatrix)
            {
                weights = weights.Divide(StandardDeviations, (VectorType)0);
            }

            var bias = weights.Dot(Means).Multiply(-1);

            return new MultivariateLinearRegression
            {
                Weights = weights.Transpose(),
                Intercepts = bias
            };
        }

        /// <summary>
        ///     Reverts a set of projected data into it's original form. Complete reverse
        ///     transformation is only possible if all components are present, and, if the
        ///     data has been standardized, the original standard deviation and means of
        ///     the original matrix are known.
        /// </summary>
        /// <param name="data">The pca transformed data.</param>
        [Obsolete("Please use Jagged matrices instead.")]
        public virtual double[,] Revert(double[,] data)
        {
            return Revert(data.ToJagged()).ToMatrix();
        }

        /// <summary>
        ///     Reverts a set of projected data into it's original form. Complete reverse
        ///     transformation is only possible if all components are present, and, if the
        ///     data has been standardized, the original standard deviation and means of
        ///     the original matrix are known.
        /// </summary>
        /// <param name="data">The pca transformed data.</param>
        public virtual double[][] Revert(double[][] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var rows = data.Rows();
            var cols = data.Columns();
            var components = NumberOfOutputs;
            var reversion = Jagged.Zeros(rows, components);

            // Revert the data (reversion = data * eigenVectors.Transpose())
            for (var i = 0; i < components; i++)
            {
                for (var j = 0; j < rows; j++)
                {
                    for (var k = 0; k < cols; k++)
                    {
                        reversion[j][i] += data[j][k] * ComponentVectors[k][i];
                    }
                }
            }


            // if the data has been standardized or centered,
            //  we need to revert those operations as well
            if (Method == PrincipalComponentMethod.Standardize || Method == PrincipalComponentMethod.CorrelationMatrix)
            {
                reversion.Multiply(StandardDeviations, (VectorType)0, reversion);
            }

            reversion.Add(Means, (VectorType)0, reversion);
            return reversion;
        }

        /// <summary>
        ///     Adjusts a data matrix, centering and standardizing its values
        ///     using the already computed column's means and standard deviations.
        /// </summary>
        [Obsolete("This method is obsolete.")]
        protected internal double[,] Adjust(double[,] matrix, bool inPlace)
        {
            if (Means == null || Means.Length == 0)
            {
                Means = matrix.Mean(0);
                StandardDeviations = matrix.StandardDeviation(Means);
            }

            // Center the data around the mean. Will have no effect if
            //  the data is already centered (the mean will be zero).
            var result = matrix.Center(Means, inPlace);

            // Check if we also have to standardize our data (convert to Z Scores).
            if (Method == PrincipalComponentMethod.Standardize || Method == PrincipalComponentMethod.CorrelationMatrix)
            {
                // Yes. Divide by standard deviation
                result.Standardize(StandardDeviations, true);
            }

            return result;
        }

        /// <summary>
        ///     Adjusts a data matrix, centering and standardizing its values
        ///     using the already computed column's means and standard deviations.
        /// </summary>
        [Obsolete("This method is obsolete.")]
        protected internal double[][] Adjust(double[][] matrix, bool inPlace)
        {
            if (Means == null || Means.Length == 0)
            {
                Means = matrix.Mean(0);
                StandardDeviations = matrix.StandardDeviation(Means);
            }

            // Center the data around the mean. Will have no effect if
            //  the data is already centered (the mean will be zero).
            var result = matrix.Center(Means, inPlace);

            // Check if we also have to standardize our data (convert to Z Scores).
            if (Method == PrincipalComponentMethod.Standardize || Method == PrincipalComponentMethod.CorrelationMatrix)
            {
                // Yes. Divide by standard deviation
                result.Standardize(StandardDeviations, true);
            }

            return result;
        }

        #region Named Constructors

        /// <summary>
        ///     Constructs a new Principal Component Analysis from a Covariance matrix.
        /// </summary>
        /// <remarks>
        ///     This method may be more suitable to high dimensional problems in which
        ///     the original data matrix may not fit in memory but the covariance matrix
        ///     will.
        /// </remarks>
        /// <param name="mean">The mean vector for the source data.</param>
        /// <param name="covariance">The covariance matrix of the data.</param>
        public static PrincipalComponentAnalysis FromCovarianceMatrix(double[] mean, double[,] covariance)
        {
            if (mean == null)
            {
                throw new ArgumentNullException("mean");
            }

            if (covariance == null)
            {
                throw new ArgumentNullException("covariance");
            }

            if (covariance.GetLength(0) != covariance.GetLength(1))
            {
                throw new NonSymmetricMatrixException("Covariance matrix must be symmetric");
            }

            var pca = new PrincipalComponentAnalysis(PrincipalComponentMethod.CovarianceMatrix);
            pca.Means = mean;
            pca.covarianceMatrix = covariance;
            pca.onlyCovarianceMatrixAvailable = true;
            pca.NumberOfInputs = covariance.GetLength(0);
            pca.NumberOfOutputs = covariance.GetLength(0);
            return pca;
        }

        /// <summary>
        ///     Constructs a new Principal Component Analysis from a Correlation matrix.
        /// </summary>
        /// <remarks>
        ///     This method may be more suitable to high dimensional problems in which
        ///     the original data matrix may not fit in memory but the covariance matrix
        ///     will.
        /// </remarks>
        /// <param name="mean">The mean vector for the source data.</param>
        /// <param name="stdDev">The standard deviation vectors for the source data.</param>
        /// <param name="correlation">The correlation matrix of the data.</param>
        public static PrincipalComponentAnalysis FromCorrelationMatrix(double[] mean, double[] stdDev,
            double[,] correlation)
        {
            if (!correlation.IsSquare())
            {
                throw new NonSymmetricMatrixException("Correlation matrix must be symmetric");
            }

            var pca = new PrincipalComponentAnalysis(PrincipalComponentMethod.CorrelationMatrix);
            pca.Means = mean;
            pca.StandardDeviations = stdDev;
            pca.covarianceMatrix = correlation;
            pca.onlyCovarianceMatrixAvailable = true;
            pca.NumberOfInputs = correlation.GetLength(0);
            pca.NumberOfOutputs = correlation.GetLength(0);
            return pca;
        }

        /// <summary>
        ///     Constructs a new Principal Component Analysis from a Kernel (Gram) matrix.
        /// </summary>
        /// <remarks>
        ///     This method may be more suitable to high dimensional problems in which
        ///     the original data matrix may not fit in memory but the covariance matrix
        ///     will.
        /// </remarks>
        /// <param name="mean">The mean vector for the source data.</param>
        /// <param name="stdDev">The standard deviation vectors for the source data.</param>
        /// <param name="kernelMatrix">The kernel matrix for the data.</param>
        public static PrincipalComponentAnalysis FromGramMatrix(double[] mean, double[] stdDev, double[,] kernelMatrix)
        {
            if (!kernelMatrix.IsSquare())
            {
                throw new NonSymmetricMatrixException("Correlation matrix must be symmetric");
            }

            var pca = new PrincipalComponentAnalysis(PrincipalComponentMethod.KernelMatrix);
            pca.Means = mean;
            pca.StandardDeviations = stdDev;
            pca.covarianceMatrix = kernelMatrix;
            pca.onlyCovarianceMatrixAvailable = true;
            pca.NumberOfInputs = mean.GetLength(0);
            return pca;
        }

        /// <summary>
        ///     Reduces the dimensionality of a given matrix <paramref name="x" />
        ///     to the given number of <paramref name="dimensions" />.
        /// </summary>
        /// <param name="x">The matrix that should have its dimensionality reduced.</param>
        /// <param name="dimensions">The number of dimensions for the reduced matrix.</param>
        public static double[][] Reduce(double[][] x, int dimensions)
        {
            return new PrincipalComponentAnalysis
            {
                NumberOfOutputs = dimensions
            }.Learn(x).Transform(x);
        }

        #endregion
    }

    /// <summary>
    ///     <para>
    ///         Represents a Principal Component found in the Principal Component Analysis,
    ///         allowing it to be bound to controls like the DataGridView.
    ///     </para>
    ///     <para>
    ///         This class cannot be instantiated.
    ///     </para>
    /// </summary>
    [Serializable]
    public class PrincipalComponent : IAnalysisComponent
    {
        /// <summary>
        ///     Creates a principal component representation.
        /// </summary>
        /// <param name="analysis">The analysis to which this component belongs.</param>
        /// <param name="index">The component index.</param>
        internal PrincipalComponent(BasePrincipalComponentAnalysis analysis, int index)
        {
            this.Index = index;
            Analysis = analysis;
        }

        /// <summary>
        ///     Returns a reference to the parent analysis object.
        /// </summary>
        public BasePrincipalComponentAnalysis Analysis { get; }

        /// <summary>
        ///     If available, gets the Singular Value of this component found during the Analysis.
        /// </summary>
        public double SingularValue => Analysis.SingularValues[Index];

        /// <summary>
        ///     Gets the Eigenvalue of this component found during the analysis.
        /// </summary>
        public double Eigenvalue => Analysis.Eigenvalues[Index];

        /// <summary>
        ///     Gets the Eigenvector of this component.
        /// </summary>
        public double[] Eigenvector => Analysis.ComponentVectors[Index];

        /// <summary>
        ///     Gets the Index of this component on the original analysis principal component collection.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Gets the proportion of data this component represents.
        /// </summary>
        public double Proportion => Analysis.ComponentProportions[Index];

        /// <summary>
        ///     Gets the cumulative proportion of data this component represents.
        /// </summary>
        public double CumulativeProportion => Analysis.CumulativeProportions[Index];
    }

    /// <summary>
    ///     Represents a Collection of Principal Components found in the
    ///     <see cref="PrincipalComponentAnalysis" />. This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public class PrincipalComponentCollection : ReadOnlyCollection<PrincipalComponent>
    {
        internal PrincipalComponentCollection(PrincipalComponent[] components)
            : base(components)
        {
        }
    }
}