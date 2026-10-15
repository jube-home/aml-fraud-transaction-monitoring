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
using Accord.Diagnostics;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics.Analysis.Base;
using Accord.Statistics.Kernels;
using Accord.Statistics.Models.Regression;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Kernel Principal Component Analysis.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Kernel principal component analysis (kernel PCA) is an extension of principal
    ///         component analysis (PCA) using techniques of kernel methods. Using a kernel,
    ///         the originally linear operations of PCA are done in a reproducing kernel Hilbert
    ///         space with a non-linear mapping.
    ///     </para>
    ///     <para>
    ///         This class can also be bound to standard controls such as the
    ///         <a href="http://msdn.microsoft.com/en-us/library/system.windows.forms.datagridview.aspx">DataGridView</a>
    ///         by setting their DataSource property to the analysis' <see cref="BasePrincipalComponentAnalysis.Components" />
    ///         property.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.heikohoffmann.de/htmlthesis/hoffmann_diss.html">
    ///                         Heiko Hoffmann, Unsupervised Learning of Visuomotor Associations (Kernel PCA topic).
    ///                         PhD thesis. 2005. Available on: http://www.heikohoffmann.de/htmlthesis/hoffmann_diss.html
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.hpl.hp.com/conferences/icml2003/papers/345.pdf">
    ///                         James T. Kwok, Ivor W. Tsang. The Pre-Image Problem in Kernel Methods. 2003. Available on:
    ///                         http://www.hpl.hp.com/conferences/icml2003/papers/345.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The example below shows a typical usage of the analysis. We will be replicating
    ///         the exact same example which can be found on the <see cref="PrincipalComponentAnalysis" />
    ///         documentation page. However, while we will be using a <see cref="Linear" /> kernel,
    ///         any other kernel function could have been used.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\KernelPrincipalComponentAnalysisTest.cs"
    ///         region="doc_learn_1" />
    ///     <para>
    ///         It is also possible to create a KPCA from a kernel matrix that already exists. The example
    ///         below shows how this could be accomplished.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\KernelPrincipalComponentAnalysisTest.cs"
    ///         region="doc_learn_kernel_matrix" />
    /// </example>
    [Serializable]
#pragma warning disable 612, 618
    public class KernelPrincipalComponentAnalysis : BasePrincipalComponentAnalysis, ITransform<double[], double[]>,
        IUnsupervisedLearning<MultivariateKernelRegression, double[], double[]>,
        IMultivariateAnalysis, IProjectionAnalysis
#pragma warning restore 612, 618
    {
        private bool allowReversion = true;
        private bool centerFeatureSpace;
        private double featureGrandMean;
        private double[] featureMean;
        private IKernel kernel;
        private double[][] sourceCentered;
        private double threshold = 0.001;

        /// <summary>
        ///     Constructs the Kernel Principal Component Analysis.
        /// </summary>
        public KernelPrincipalComponentAnalysis()
        {
            Method = PrincipalComponentMethod.Center;
            kernel = new Linear();
            centerFeatureSpace = true;
            NumberOfOutputs = 0;
            Whiten = false;
        }

        /// <summary>
        ///     Constructs the Kernel Principal Component Analysis.
        /// </summary>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        /// <param name="method">The analysis method to perform.</param>
        /// <param name="centerInFeatureSpace">
        ///     True to center the data in feature space,
        ///     false otherwise. Default is true.
        /// </param>
        /// <param name="numberOfOutputs">The maximum number of components that the analysis will be able to project data into.</param>
        /// <param name="whiten">
        ///     Whether to whiten the results or not. If set to <c>true</c> the generatred output
        ///     will be normalized to have unit standard deviation.
        /// </param>
        public KernelPrincipalComponentAnalysis(IKernel kernel,
            PrincipalComponentMethod method = PrincipalComponentMethod.Center,
            bool centerInFeatureSpace = true, int numberOfOutputs = 0, bool whiten = false)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }

            Method = method;
            this.kernel = kernel;
            centerFeatureSpace = centerInFeatureSpace;
            NumberOfOutputs = numberOfOutputs;
            Whiten = whiten;
        }

        /// <summary>
        ///     Constructs the Kernel Principal Component Analysis.
        /// </summary>
        /// <param name="data">
        ///     The source data to perform analysis. The matrix should contain
        ///     variables as columns and observations of each variable as rows.
        /// </param>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        /// <param name="method">The analysis method to perform.</param>
        /// <param name="centerInFeatureSpace">
        ///     True to center the data in feature space,
        ///     false otherwise. Default is true.
        /// </param>
        [Obsolete("Please pass the 'data' matrix to the Learn method instead.")]
        public KernelPrincipalComponentAnalysis(double[,] data, IKernel kernel,
            AnalysisMethod method, bool centerInFeatureSpace)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }

            source = data;
            array = data.ToJagged();
            Method = (PrincipalComponentMethod)method;
            this.kernel = kernel;
            centerFeatureSpace = centerInFeatureSpace;
        }

        /// <summary>
        ///     Constructs the Kernel Principal Component Analysis.
        /// </summary>
        /// <param name="data">
        ///     The source data to perform analysis. The matrix should contain
        ///     variables as columns and observations of each variable as rows.
        /// </param>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        /// <param name="method">The analysis method to perform.</param>
        /// <param name="centerInFeatureSpace">
        ///     True to center the data in feature space,
        ///     false otherwise. Default is true.
        /// </param>
        [Obsolete("Please pass the 'data' matrix to the Learn method instead.")]
        public KernelPrincipalComponentAnalysis(double[][] data, IKernel kernel,
            AnalysisMethod method, bool centerInFeatureSpace)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }

            this.kernel = kernel;
            centerFeatureSpace = centerInFeatureSpace;
            array = data;
        }

        /// <summary>
        ///     Constructs the Kernel Principal Component Analysis.
        /// </summary>
        /// <param name="data">
        ///     The source data to perform analysis. The matrix should contain
        ///     variables as columns and observations of each variable as rows.
        /// </param>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        /// <param name="method">The analysis method to perform.</param>
        [Obsolete("Please pass the 'data' matrix to the Learn method instead.")]
        public KernelPrincipalComponentAnalysis(double[,] data, IKernel kernel, AnalysisMethod method)
            : this(data, kernel, method, true)
        {
        }

        /// <summary>
        ///     Constructs the Kernel Principal Component Analysis.
        /// </summary>
        /// <param name="data">
        ///     The source data to perform analysis. The matrix should contain
        ///     variables as columns and observations of each variable as rows.
        /// </param>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        /// <param name="method">The analysis method to perform.</param>
        [Obsolete("Please pass the 'data' matrix to the Learn method instead.")]
        public KernelPrincipalComponentAnalysis(double[][] data, IKernel kernel, AnalysisMethod method)
            : this(data, kernel, method, true)
        {
        }

        /// <summary>Constructs the Kernel Principal Component Analysis.</summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        [Obsolete("Please pass the 'data' matrix to the Learn method instead.")]
        public KernelPrincipalComponentAnalysis(double[,] data, IKernel kernel)
            : this(data, kernel, AnalysisMethod.Center, true)
        {
        }

        /// <summary>Constructs the Kernel Principal Component Analysis.</summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        [Obsolete("Please pass the 'data' matrix to the Learn method instead.")]
        public KernelPrincipalComponentAnalysis(double[][] data, IKernel kernel)
            : this(data, kernel, AnalysisMethod.Center, true)
        {
        }

        /// <summary>
        ///     Gets or sets the Kernel used in the analysis.
        /// </summary>
        public IKernel Kernel
        {
            get => kernel;
            set => kernel = value;
        }

        /// <summary>
        ///     Gets or sets whether the points should be centered in feature space.
        /// </summary>
        public bool Center
        {
            get => centerFeatureSpace;
            set => centerFeatureSpace = value;
        }

        /// <summary>
        ///     Gets or sets the minimum variance proportion needed to keep a
        ///     principal component. If set to zero, all components will be
        ///     kept. Default is 0.001 (all components which contribute less
        ///     than 0.001 to the variance in the data will be discarded).
        /// </summary>
        [Obsolete("Please set ExplainedVariance instead.")]
        public double Threshold
        {
            get => threshold;
            set => threshold = value;
        }

        /// <summary>
        ///     Gets or sets a boolean value indicating whether this analysis
        ///     should store enough information to allow the reversion of the
        ///     transformation to be computed. Set this to no in case you would
        ///     like to store the analysis object to disk and you do not need to
        ///     reverse a transformation after it has been computed.
        /// </summary>
        public bool AllowReversion
        {
            get => allowReversion;
            set => allowReversion = value;
        }

        /// <summary>
        ///     Computes the Kernel Principal Component Analysis algorithm.
        /// </summary>
        [Obsolete("Please use the Learn method instead.")]
        public void Compute()
        {
            Compute(NumberOfOutputs);
        }

        /// <summary>
        ///     Projects a given matrix into principal component space.
        /// </summary>
        /// <param name="data">The matrix to be projected.</param>
        /// <param name="result">The matrix where to store the results.</param>
        public override double[][] Transform(double[][] data, double[][] result)
        {
            double[][] newK;

            if (Method == PrincipalComponentMethod.KernelMatrix)
            {
                newK = data;

                if (centerFeatureSpace)
                {
                    newK = Kernels.Kernel.Center(newK, featureMean, featureGrandMean); // do not overwrite
                }
            }
            else
            {
                if (data.Columns() != NumberOfInputs)
                {
                    throw new DimensionMismatchException("data",
                        "The input data should have the same number of columns as the original data.");
                }

                data = data.Subtract(Means, (VectorType)0);
                if (Method == PrincipalComponentMethod.Standardize)
                {
                    data.Divide(StandardDeviations, (VectorType)0, data);
                }

                // Create the Kernel matrix
                newK = kernel.ToJagged2(data, sourceCentered);

                if (centerFeatureSpace)
                {
                    newK = Kernels.Kernel.Center(newK, featureMean, featureGrandMean, newK); // overwrite
                }
            }

            // Project into the kernel principal components
            if (NumberOfOutputs == ComponentVectors.Length)
            {
                return newK.DotWithTransposed(ComponentVectors, result);
            }

            if (NumberOfOutputs < ComponentVectors.Length)
            {
                var selectedComponents = ComponentVectors.Get(0, NumberOfOutputs);
                return newK.DotWithTransposed(selectedComponents, result);
            }

            throw new DimensionMismatchException("Number of outputs cannot exceed the number of principal components.");
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
        public MultivariateKernelRegression Learn(double[][] x, double[] weights = null)
        {
            sourceCentered = null;
            StandardDeviations = null;
            featureMean = null;
            featureGrandMean = 0;
            double[][] K;

            if (Method == PrincipalComponentMethod.KernelMatrix)
            {
                K = x;
                if (centerFeatureSpace) // Center the Gram (Kernel) Matrix if requested
                {
                    K = Kernels.Kernel.Center(K, out featureMean, out featureGrandMean); // do not overwrite
                }
            }
            else
            {
                NumberOfInputs = x.Columns();
                Means = x.Mean(0);
                sourceCentered = Overwrite ? x : Jagged.CreateAs(x);
                x.Subtract(Means, (VectorType)0, sourceCentered);

                if (Method == PrincipalComponentMethod.Standardize)
                {
                    StandardDeviations = x.StandardDeviation(Means);
                    sourceCentered.Divide(StandardDeviations, (VectorType)0, sourceCentered);
                }

                // Create the Gram (Kernel) Matrix
                K = kernel.ToJagged(sourceCentered);
                if (centerFeatureSpace) // Center the Gram (Kernel) Matrix if requested
                {
                    K = Kernels.Kernel.Center(K, out featureMean, out featureGrandMean, K); // overwrite
                }
            }

            // Perform the Eigenvalue Decomposition (EVD) of the Kernel matrix
            var evd = new JaggedEigenvalueDecomposition(K,
                assumeSymmetric: true, sort: true);

            // Gets the Eigenvalues and corresponding Eigenvectors
            var numberOfSamples = x.Length;
            var evals = evd.RealEigenvalues;
            var eigs = evd.Eigenvectors;

            var nonzero = evd.Rank;
            if (NumberOfInputs != 0)
            {
                nonzero = System.Math.Min(nonzero, NumberOfInputs);
            }

            if (NumberOfOutputs != 0)
            {
                nonzero = System.Math.Min(nonzero, NumberOfOutputs);
            }

            // Eliminate unwanted components
            eigs = eigs.Get(null, 0, nonzero);
            evals = evals.Get(0, nonzero);

            // Normalize eigenvectors
            if (centerFeatureSpace)
            {
                eigs.Divide(evals.Sqrt(), (VectorType)0, eigs);
            }

            if (Whiten)
            {
                eigs.Divide(evals.Sqrt(), (VectorType)0, eigs);
            }

            //this.Eigenvalues = evals.Divide(numberOfSamples - 1);
            Eigenvalues = evals;
            SingularValues = evals.Divide(numberOfSamples - 1).Sqrt();
            ComponentVectors = eigs.Transpose();

            if (allowReversion)
            {
                // Project the original data into principal component space
                result = K.Dot(eigs).ToMatrix();
            }

            // Computes additional information about the analysis and creates the
            //  object-oriented structure to hold the principal components found.
            CreateComponents();

            Debug.Assert(NumberOfOutputs > 0);

            return CreateRegression();
        }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        [Obsolete("Please use jagged matrices instead.")]
        public MultivariateKernelRegression Learn(double[,] x)
        {
            return Learn(x.ToJagged());
        }

        private MultivariateKernelRegression CreateRegression()
        {
            return new MultivariateKernelRegression
            {
                NumberOfInputs = NumberOfInputs,
                NumberOfOutputs = NumberOfOutputs,
                Kernel = kernel,
                Weights = ComponentVectors,
                BasisVectors = sourceCentered,
                Means = Means,
                StandardDeviations = StandardDeviations,
                FeatureGrandMean = featureGrandMean,
                FeatureMeans = featureMean
            };
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please set the desired number of components in the NumberOfOutputs property and call Learn.")]
        public void Compute(int components)
        {
            NumberOfOutputs = components;
            if (array != null)
            {
                Learn(array);
            }
            else
#pragma warning disable 612, 618
            {
                Learn(source);
            }
#pragma warning restore 612, 618
        }

        /// <summary>
        ///     Reverts a set of projected data into it's original form. Complete reverse
        ///     transformation is not always possible and is not even guaranteed to exist.
        /// </summary>
        /// <remarks>
        ///     This method works using a closed-form MDS approach as suggested by
        ///     Kwok and Tsang. It is currently a direct implementation of the algorithm
        ///     without any kind of optimization.
        ///     Reference:
        ///     - http://cmp.felk.cvut.cz/cmp/software/stprtool/manual/kernels/preimage/list/rbfpreimg3.html
        /// </remarks>
        /// <param name="data">The kpca-transformed data.</param>
        [Obsolete("Please use Jagged matrices instead.")]
        public double[,] Revert(double[,] data)
        {
            return Revert(data.ToJagged()).ToMatrix();
        }

        /// <summary>
        ///     Reverts a set of projected data into it's original form. Complete reverse
        ///     transformation is not always possible and is not even guaranteed to exist.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method works using a closed-form MDS approach as suggested by
        ///         Kwok and Tsang. It is currently a direct implementation of the algorithm
        ///         without any kind of optimization.
        ///     </para>
        ///     <para>
        ///         Reference:
        ///         - http://cmp.felk.cvut.cz/cmp/software/stprtool/manual/kernels/preimage/list/rbfpreimg3.html
        ///     </para>
        /// </remarks>
        /// <param name="data">The kpca-transformed data.</param>
        /// <param name="neighbors">The number of nearest neighbors to use while constructing the pre-image.</param>
        [Obsolete("Please use Jagged matrices instead.")]
        public double[,] Revert(double[,] data, int neighbors)
        {
            return Revert(data.ToJagged(), neighbors).ToMatrix();
        }

        /// <summary>
        ///     Reverts a set of projected data into it's original form. Complete reverse
        ///     transformation is not always possible and is not even guaranteed to exist.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method works using a closed-form MDS approach as suggested by
        ///         Kwok and Tsang. It is currently a direct implementation of the algorithm
        ///         without any kind of optimization.
        ///     </para>
        ///     <para>
        ///         Reference:
        ///         - http://cmp.felk.cvut.cz/cmp/software/stprtool/manual/kernels/preimage/list/rbfpreimg3.html
        ///     </para>
        /// </remarks>
        /// <param name="data">The kpca-transformed data.</param>
        /// <param name="neighbors">The number of nearest neighbors to use while constructing the pre-image.</param>
        public double[][] Revert(double[][] data, int neighbors = 10)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (sourceCentered == null)
            {
                throw new InvalidOperationException("The analysis must have been computed first.");
            }

            if (neighbors < 2)
            {
                throw new ArgumentOutOfRangeException("neighbors", "At least two neighbors are necessary.");
            }

            // Verify if the current kernel supports
            // distance calculation in feature space.
            var distance = kernel as IReverseDistance;

            if (distance == null)
            {
                throw new NotSupportedException(
                    "Current kernel does not support distance calculation in feature space.");
            }

            var rows = data.Rows();

            var result = this.result;

            var reversion = Jagged.Zeros(rows, sourceCentered.Columns());

            // number of neighbors cannot exceed the number of training vectors.
            var nn = System.Math.Min(neighbors, sourceCentered.Rows());


            // For each point to be reversed
            for (var p = 0; p < rows; p++)
            {
                // 1. Get the point in feature space
                var y = data.GetRow(p);

                // 2. Select nn nearest neighbors of the feature space
                var X = sourceCentered;
                var d2 = new double[result.GetLength(0)];
                var inx = new int[result.GetLength(0)];

                // 2.1 Calculate distances
                for (var i = 0; i < X.GetLength(0); i++)
                {
                    inx[i] = i;
                    d2[i] = distance.ReverseDistance(y, result.GetRow(i).First(y.Length));

                    if (double.IsNaN(d2[i]))
                    {
                        d2[i] = double.PositiveInfinity;
                    }
                }

                // 2.2 Order them
                Array.Sort(d2, inx);

                // 2.3 Select nn neighbors
                var def = 0;
                for (var i = 0; i < d2.Length && i < nn; i++, def++)
                {
                    if (double.IsInfinity(d2[i]))
                    {
                        break;
                    }
                }

                inx = inx.First(def);
                X = X.Get(inx).Transpose(); // X is in input space
                d2 = d2.First(def); // distances in input space

                // 3. Perform SVD
                //    [U,L,V] = svd(X*H);

                // TODO: If X has more columns than rows, the SV decomposition should be
                //  computed on the transpose of X and the left and right vectors should
                //  be swapped. This should be fixed after more unit tests are elaborated.
                var svd = new JaggedSingularValueDecomposition(X,
                    true,
                    true,
                    false);

                var U = svd.LeftSingularVectors;
                var L = Jagged.Diagonal(def, svd.Diagonal);
                var V = svd.RightSingularVectors;


                // 4. Compute projections
                //    Z = L*V';
                var Z = L.DotWithTransposed(V);

                // 5. Calculate distances
                //    d02 = sum(Z.^2)';
                var d02 = Z.Pow(2).Sum(0);

                // 6. Get the pre-image using
                //    z = -0.5*inv(Z')*(d2-d02)
                var inv = Z.Transpose().PseudoInverse();

                var w = (-0.5).Multiply(inv).Dot(d2.Subtract(d02));
                var z = w.First(U.Columns());

                // 8. Project the pre-image on the original basis using 
                //    x = U*z + sum(X,2)/nn;
                var x = U.Dot(z).Add(X.Transpose().Sum(0).Multiply(1.0 / nn));

                // 9. Store the computed pre-image.
                for (var i = 0; i < reversion.Columns(); i++)
                {
                    reversion[p][i] = x[i];
                }
            }

            // if the data has been standardized or centered,
            //  we need to revert those operations as well
            if (Method == PrincipalComponentMethod.Standardize)
            {
                // multiply by standard deviation and add the mean
                reversion.Multiply(StandardDeviations, (VectorType)0, reversion)
                    .Add(Means, (VectorType)0, reversion);
            }
            else if (Method == PrincipalComponentMethod.Center)
            {
                // only add the mean
                reversion.Add(Means, (VectorType)0, reversion);
            }


            return reversion;
        }
    }
}