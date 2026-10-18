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
using Accord.Properties;
using Accord.Statistics.Kernels;
using Accord.Statistics.Models.Regression;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Kernel (Fisher) Discriminant Analysis.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Kernel (Fisher) discriminant analysis (kernel FDA) is a non-linear generalization
    ///         of linear discriminant analysis (LDA) using techniques of kernel methods. Using a
    ///         kernel, the originally linear operations of LDA are done in a reproducing kernel
    ///         Hilbert space with a non-linear mapping.
    ///     </para>
    ///     <para>
    ///         The algorithm used is a multi-class generalization of the original algorithm by
    ///         Mika et al. in Fisher discriminant analysis with kernels (1999).
    ///     </para>
    ///     <para>
    ///         This class can also be bound to standard controls such as the
    ///         <a href="http://msdn.microsoft.com/en-us/library/system.windows.forms.datagridview.aspx">DataGridView</a>
    ///         by setting their DataSource property to the analysis' <see cref="BaseDiscriminantAnalysis.Discriminants" />
    ///         property.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Mika et al, Fisher discriminant analysis with kernels (1999). Available on
    ///                     <a href="http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.35.9904">
    ///                         http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.35.9904
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\KernelDiscriminantAnalysisTest.cs" region="doc_learn" />
    /// </example>
    [Serializable]
    public partial class KernelDiscriminantAnalysis : BaseDiscriminantAnalysis,
        ISupervisedLearning<KernelDiscriminantAnalysis.Pipeline, double[], int>
    {
        private double[][] input;
        private IKernel kernel;
        private double regularization = 1e-4;

        /// <summary>
        ///     Constructs a new Kernel Discriminant Analysis object.
        /// </summary>
        /// <param name="inputs">
        ///     The source data to perform analysis. The matrix should contain
        ///     variables as columns and observations of each variable as rows.
        /// </param>
        /// <param name="output">The labels for each observation row in the input matrix.</param>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        [Obsolete("Please pass the 'inputs' and 'outputs' parameters to the Learn method instead.")]
        public KernelDiscriminantAnalysis(double[,] inputs, int[] output, IKernel kernel)
            : this(kernel)
        {
            init(inputs, output);
        }

        /// <summary>
        ///     Constructs a new Kernel Discriminant Analysis object.
        /// </summary>
        /// <param name="inputs">
        ///     The source data to perform analysis. The matrix should contain
        ///     variables as columns and observations of each variable as rows.
        /// </param>
        /// <param name="output">The labels for each observation row in the input matrix.</param>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        [Obsolete("Please pass the 'inputs' and 'outputs' parameters to the Learn method instead.")]
        public KernelDiscriminantAnalysis(double[][] inputs, int[] output, IKernel kernel)
            : this(kernel)
        {
            init(inputs.ToMatrix(), output);
        }

        /// <summary>
        ///     Constructs a new Kernel Discriminant Analysis object.
        /// </summary>
        public KernelDiscriminantAnalysis(IKernel kernel)
        {
            this.kernel = kernel;
            Threshold = 0;
        }

        /// <summary>
        ///     Constructs a new Kernel Discriminant Analysis object.
        /// </summary>
        public KernelDiscriminantAnalysis()
            : this(new Linear())
        {
        }

        /// <summary>
        ///     Gets a classification pipeline that can be used to classify
        ///     new samples into one of the <see cref="BaseDiscriminantAnalysis.NumberOfClasses" />
        ///     learned in this discriminant analysis. This pipeline is
        ///     only available after a call to the <see cref="Learn" /> method.
        /// </summary>
        public Pipeline Classifier { get; private set; }

        /// <summary>
        ///     Gets or sets the matrix of original values used to create
        ///     this analysis. Those values are required to build kernel
        ///     (Gram) matrices when classifying new samples.
        /// </summary>
        public double[][] Input
        {
            get => input;
            set => input = value;
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
        ///     Gets or sets the regularization parameter to
        ///     avoid non-singularities at the solution.
        /// </summary>
        public double Regularization
        {
            get => regularization;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Value must be positive.");
                }

                regularization = value;
            }
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
        public Pipeline Learn(double[][] x, int[] y, double[] weights = null)
        {
            if (weights != null)
            {
                throw new ArgumentException(Resources.NotSupportedWeights, "weights");
            }

            Init(x, y);

            // Create the Gram (Kernel) Matrix
            var K = kernel.ToJagged(x);

            // Compute entire data set measures
            Means = K.Mean(0);
            StandardDeviations = K.StandardDeviation(Means);

            // Initialize the kernel analogous scatter matrices
            //int dimension = x.Columns();
            var Sb = Jagged.Zeros(NumberOfSamples, NumberOfSamples);
            var Sw = Jagged.Zeros(NumberOfSamples, NumberOfSamples);

            // For each class
            for (var c = 0; c < Classes.Count; c++)
            {
                var classIndex = c;
                var idx = y.Find(y_i => y_i == classIndex);

                // Get the Kernel matrix class subset
                var Kc = K.Get(idx);
                var count = Kc.Rows();

                // Get the Kernel matrix class mean
                var mean = Kc.Mean(0);

                // Construct the Kernel equivalent of the Within-Class Scatter matrix
                var Swi = Kc.Scatter(dimension: 0, means: mean);
                Swi.Divide((double)count, Swi);
                Sw.Add(Swi, Sw); // Sw = Sw + Swi

                // Construct the Kernel equivalent of the Between-Class Scatter matrix
                var d = mean.Subtract(Means);
                var Sbi = Jagged.Outer(d, d);
                Sbi.Multiply((double)NumberOfSamples, Sbi);

                Sb.Add(Sbi, Sb); // Sb = Sb + Sbi

                // Store additional information
                ClassScatter[c] = Swi;
                ClassCount[c] = count;
                ClassMeans[c] = mean;
                ClassStandardDeviations[c] = Kc.StandardDeviation(mean);
            }

            // Add regularization to avoid singularity
            Sw.AddToDiagonal(regularization, Sw);

            // Compute the generalized eigenvalue decomposition
            var gevd = new JaggedGeneralizedEigenvalueDecomposition(Sb, Sw, true);

            if (gevd.IsSingular) // check validity of the results
            {
                throw new SingularMatrixException("One of the matrices is singular. Please retry " +
                                                  "the method with a higher regularization constant.");
            }

            // Get the eigenvalues and corresponding eigenvectors
            var evals = gevd.RealEigenvalues;
            var eigs = gevd.Eigenvectors;

            // Eliminate unwanted components
            var nonzero = x.Columns();
            if (Threshold > 0)
            {
                nonzero = System.Math.Min(gevd.Rank, GetNonzeroEigenvalues(evals, Threshold));
            }

            if (NumberOfInputs != 0)
            {
                nonzero = System.Math.Min(nonzero, NumberOfInputs);
            }

            if (NumberOfOutputs != 0)
            {
                nonzero = System.Math.Min(nonzero, NumberOfOutputs);
            }

            eigs = eigs.Get(null, 0, nonzero);
            evals = evals.Get(0, nonzero);

            // Store information
            input = x;
            Eigenvalues = evals;
            DiscriminantVectors = eigs.Transpose();
            ScatterBetweenClass = Sb;
            ScatterWithinClass = Sw;
            base.NumberOfOutputs = evals.Length;

            // Compute feature space means for later classification
            for (var c = 0; c < Classes.Count; c++)
            {
                ProjectionMeans[c] = ClassMeans[c].Dot(eigs);
            }

            // Computes additional information about the analysis and creates the
            //  object-oriented structure to hold the discriminants found.
            CreateDiscriminants();

            Classifier = CreateClassifier();

            return Classifier;
        }

        /// <summary>
        ///     Computes the Multi-Class Kernel Discriminant Analysis algorithm.
        /// </summary>
        [Obsolete("Please use Learn(x, y) instead.")]
        public void Compute()
        {
            input = Source.ToJagged();

            // Get some initial information
            var dimension = Source.GetLength(0);
            double total = dimension;

            var K = createGramMatrix(dimension);


            // Compute entire data set measures
            Means = K.Mean(0);
            StandardDeviations = K.StandardDeviation(Means);


            // Initialize the kernel analogous scatter matrices
            var Sb = new double[dimension, dimension];
            var Sw = new double[dimension, dimension];


            computeScatterMatrices(K, dimension, total, Sb, Sw);


            double[] evals;
            double[,] eigs;
            decomposeScatterMatrices(Sb, Sw, dimension, out evals, out eigs);


            if (Threshold > 0)
            {
                discardMinorComponents(dimension, ref evals, ref eigs);
            }

            // Store information
            Eigenvalues = evals;
            DiscriminantVectors = eigs.ToJagged().Transpose();
            ScatterBetweenClass = Sb.ToJagged();
            ScatterWithinClass = Sw.ToJagged();
            NumberOfOutputs = eigs.Columns();
            NumberOfClasses = Classes.Count;

            // Project into the kernel discriminant space
            Result = K.Dot(eigs);

            // Compute feature space means for later classification
            for (var c = 0; c < Classes.Count; c++)
            {
                ProjectionMeans[c] = ClassMeans[c].Dot(eigs);
            }

            // Computes additional information about the analysis and creates the
            //  object-oriented structure to hold the discriminants found.
            CreateDiscriminants();

            Classifier = CreateClassifier();
        }

        private Pipeline CreateClassifier()
        {
            if (NumberOfOutputs == 0)
            {
                return null;
            }

            var eig = DiscriminantVectors;

            return new Pipeline
            {
                NumberOfInputs = NumberOfInputs,
                NumberOfOutputs = NumberOfClasses,
                NumberOfClasses = NumberOfClasses,

                First = new MultivariateKernelRegression
                {
                    Weights = eig,
                    BasisVectors = input,
                    Kernel = Kernel,
                    NumberOfInputs = NumberOfInputs,
                    NumberOfOutputs = NumberOfOutputs
                },
                Second = new MinimumMeanDistanceClassifier
                {
                    Means = projectedMeans,
                    NumberOfInputs = NumberOfOutputs,
                    NumberOfOutputs = NumberOfClasses,
                    NumberOfClasses = NumberOfClasses
                }
            };
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <param name="result">A location to store the output, avoiding unnecessary memory allocations.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override double[][] Transform(double[][] input, double[][] result)
        {
            // TODO: Do without forming the kernel matrix
            var K = kernel.ToJagged2(input, this.input);
            // return K.DotWithTransposed(DiscriminantVectors);

            for (var i = 0; i < input.Length; i++)
            {
                for (var j = 0; j < result[i].Length; j++)
                {
                    for (var k = 0; k < K[i].Length; k++)
                    {
                        result[i][j] += K[i][k] * DiscriminantVectors[j][k];
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Classifies a new instance into one of the available classes.
        /// </summary>
        [Obsolete("Please use Classifier.Decide() instead.")]
        public override int Classify(double[] input)
        {
            return Classes[Classifier.Decide(input)].Number;
        }

        /// <summary>
        ///     Classifies a new instance into one of the available classes.
        /// </summary>
        [Obsolete("Please use Classifier.Decide() or Classifier.Scores() instead.")]
        public override int Classify(double[] input, out double[] responses)
        {
            int decision;
            responses = Classifier.Scores(input, out decision);
            return Classes[decision].Number;
        }

        /// <summary>
        ///     Classifies new instances into one of the available classes.
        /// </summary>
        [Obsolete("Please use Classifier.Decide() instead.")]
        public override int[] Classify(double[][] inputs)
        {
            var result = Classifier.Decide(inputs);
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = Classes[result[i]].Number;
            }

            return result;
        }

        /// <summary>
        ///     Gets the output of the discriminant function for a given class.
        /// </summary>
        public override double DiscriminantFunction(double[] input, int classIndex)
        {
            return Classifier.Score(input, classIndex);
        }

        /// <summary>
        ///     Standard regression and classification pipeline for <see cref="LinearDiscriminantAnalysis" />.
        /// </summary>
        [Serializable]
        public sealed class Pipeline : MulticlassScoreClassifierBase<double[]>
        {
            /// <summary>
            ///     Gets or sets the first step in the pipeline.
            /// </summary>
            public MultivariateKernelRegression First { get; set; }

            /// <summary>
            ///     Gets or sets the second step in the pipeline.
            /// </summary>
            public MinimumMeanDistanceClassifier Second { get; set; }

            /// <summary>
            ///     Computes a numerical score measuring the association between
            ///     the given <paramref name="input" /> vector and each class.
            /// </summary>
            /// <param name="input">The input vector.</param>
            /// <param name="result">
            ///     An array where the result will be stored,
            ///     avoiding unnecessary memory allocations.
            /// </param>
            /// <returns></returns>
            public override double[][] Scores(double[][] input, double[][] result)
            {
                return Second.Scores(First.Transform(input), result);
            }

            /// <summary>
            ///     Computes a numerical score measuring the association between
            ///     the given <paramref name="input" /> vector and a given
            ///     <paramref name="classIndex" />.
            /// </summary>
            /// <param name="input">The input vector.</param>
            /// <param name="classIndex">The index of the class whose score will be computed.</param>
            /// <returns>System.Double.</returns>
            public override double Score(double[] input, int classIndex)
            {
                return Second.Score(First.Transform(input), classIndex);
            }
        }
    }
}