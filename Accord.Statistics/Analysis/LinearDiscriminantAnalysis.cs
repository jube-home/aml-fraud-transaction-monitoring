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
using System.ComponentModel;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Properties;
using Accord.Statistics.Models.Regression.Linear;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Linear Discriminant Analysis (LDA).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Linear Discriminant Analysis (LDA) is a method of finding such a linear
    ///         combination of variables which best separates two or more classes.
    ///     </para>
    ///     <para>
    ///         In itself LDA is not a classification algorithm, although it makes use of class
    ///         labels. However, the LDA result is mostly used as part of a linear classifier.
    ///         The other alternative use is making a dimension reduction before using nonlinear
    ///         classification algorithms.
    ///     </para>
    ///     <para>
    ///         It should be noted that several similar techniques (differing in requirements to the sample)
    ///         go together under the general name of Linear Discriminant Analysis. Described below is one of
    ///         these techniques with only two requirements:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>The sample size shall exceed the number of variables, and </description>
    ///         </item>
    ///         <item>
    ///             <description>Classes may overlap, but their centers shall be distant from each other. </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Moreover, LDA requires the following assumptions to be true:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Independent subjects.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Normality: the variance-covariance matrix of the
    ///                 predictors is the same in all groups.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         If the latter assumption is violated, it is common to use quadratic discriminant analysis in
    ///         the same manner as linear discriminant analysis instead.
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
    ///                     R. Gutierrez-Osuna, Linear Discriminant Analysis. Available on:
    ///                     http://research.cs.tamu.edu/prism/lectures/pr/pr_l10.pdf
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\LinearDiscriminantAnalysisTest.cs" region="doc_learn" />
    /// </example>
    [Serializable]
#pragma warning disable 612, 618
    public class LinearDiscriminantAnalysis : BaseDiscriminantAnalysis,
        IDiscriminantAnalysis, IProjectionAnalysis,
        ISupervisedLearning<LinearDiscriminantAnalysis.Pipeline, double[], int>
#pragma warning restore 612, 618
    {
        /// <summary>
        ///     Gets a classification pipeline that can be used to classify
        ///     new samples into one of the <see cref="BaseDiscriminantAnalysis.NumberOfClasses" />
        ///     learned in this discriminant analysis. This pipeline is
        ///     only available after a call to the <see cref="Learn" /> method.
        /// </summary>
        public Pipeline Classifier { get; private set; }
#pragma warning disable 612, 618
        /// <summary>
        ///     Constructs a new Linear Discriminant Analysis object.
        /// </summary>
        /// <param name="inputs">
        ///     The source data to perform analysis. The matrix should contain
        ///     variables as columns and observations of each variable as rows.
        /// </param>
        /// <param name="outputs">The labels for each observation row in the input matrix.</param>
        [Obsolete("Please pass the 'inputs' and 'outputs' parameters to the Learn method instead.")]
        public LinearDiscriminantAnalysis(double[,] inputs, int[] outputs)
        {
            init(inputs, outputs);
            Threshold = 0;
        }

        /// <summary>
        ///     Constructs a new Linear Discriminant Analysis object.
        /// </summary>
        /// <param name="inputs">
        ///     The source data to perform analysis. The matrix should contain
        ///     variables as columns and observations of each variable as rows.
        /// </param>
        /// <param name="outputs">The labels for each observation row in the input matrix.</param>
        [Obsolete("Please pass the 'inputs' and 'outputs' parameters to the Learn method instead.")]
        public LinearDiscriminantAnalysis(double[][] inputs, int[] outputs)
        {
            init(inputs.ToMatrix(), outputs);
            Threshold = 0;
        }
#pragma warning disable 612, 618

        /// <summary>
        ///     Constructs a new Linear Discriminant Analysis object.
        /// </summary>
        public LinearDiscriminantAnalysis()
        {
            Threshold = 0;
        }

        /// <summary>
        ///     Computes the Multi-Class Linear Discriminant Analysis algorithm.
        /// </summary>
        [Obsolete("Please use the Learn method instead.")]
        public virtual void Compute()
        {
            Learn(Source.ToJagged(), Classifications);

            Result = Source.DotWithTransposed(DiscriminantVectors).ToMatrix();
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
            for (var i = 0; i < input.Length; i++)
            {
                for (var j = 0; j < result[i].Length; j++)
                {
                    for (var k = 0; k < input[i].Length; k++)
                    {
                        result[i][j] += input[i][k] * DiscriminantVectors[j][k];
                    }
                }
            }

            return result;
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

            // Compute entire data set measures
            Means = x.Mean(0);
            StandardDeviations = x.StandardDeviation(Means);

            // Initialize the scatter matrices
            var Sw = Jagged.Zeros(NumberOfInputs, NumberOfInputs);
            var Sb = Jagged.Zeros(NumberOfInputs, NumberOfInputs);

            // For each class
            for (var c = 0; c < Classes.Count; c++)
            {
                var classNumber = Classes[c].Number;
                var idx = y.Find(y_i => y_i == classNumber);

                // Get the class subset
                var subset = x.Get(idx);
                var count = subset.GetLength(0);

                // Get the class mean
                var mean = subset.Mean(0);

                // Continue constructing the Within-Class Scatter Matrix
                var Swi = subset.Scatter(mean, (double)count);

                Sw.Add(Swi, Sw); // Sw = Sw + Swi

                // Continue constructing the Between-Class Scatter Matrix
                var d = mean.Subtract(Means);
                var Sbi = Jagged.Outer(d, d);
                Sbi.Multiply((double)NumberOfInputs, Sbi);

                Sb.Add(Sbi, Sb); // Sb = Sb + Sbi

                // Store some additional information
                classScatter[c] = Swi;
                classCount[c] = count;
                classMeans[c] = mean;
                classStdDevs[c] = subset.StandardDeviation(mean);
            }


            // Compute the generalized eigenvalue decomposition
            var gevd = new JaggedGeneralizedEigenvalueDecomposition(Sb, Sw, true);

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
            Eigenvalues = evals;
            DiscriminantVectors = eigs.Transpose();
            ScatterBetweenClass = Sb;
            ScatterWithinClass = Sw;
            base.NumberOfInputs = x.Columns();
            base.NumberOfOutputs = evals.Length;

            // Compute feature space means for later classification
            for (var c = 0; c < projectedMeans.Length; c++)
            {
                projectedMeans[c] = classMeans[c].Dot(eigs);
            }

            // Computes additional information about the analysis and creates the
            //  object-oriented structure to hold the discriminants found.
            CreateDiscriminants();

            Classifier = CreateClassifier();

            return Classifier;
        }

        private Pipeline CreateClassifier()
        {
            var eig = DiscriminantVectors.Transpose().Get(null, 0, NumberOfOutputs);

            return new Pipeline
            {
                NumberOfInputs = NumberOfInputs,
                NumberOfOutputs = NumberOfClasses,
                NumberOfClasses = NumberOfClasses,
                First = new MultivariateLinearRegression
                {
                    Weights = eig,
                    NumberOfInputs = NumberOfInputs,
                    NumberOfOutputs = NumberOfOutputs
                },
                Second = new MinimumMeanDistanceClassifier
                {
                    Means = projectedMeans.Get(null, 0, NumberOfOutputs),
                    NumberOfInputs = NumberOfOutputs,
                    NumberOfOutputs = NumberOfClasses,
                    NumberOfClasses = NumberOfClasses
                }
            };
        }

        /// <summary>
        ///     Transform
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
            public MultivariateLinearRegression First { get; set; }

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

    #region Support Classes

    /// <summary>
    ///     Represents a class found during Discriminant Analysis, allowing it to
    ///     be bound to controls like the DataGridView.
    ///     This class cannot be instantiated.
    /// </summary>
    [Serializable]
    public class DiscriminantAnalysisClass
    {
        private BaseDiscriminantAnalysis analysis;

        /// <summary>
        ///     Creates a new Class representation
        /// </summary>
        internal DiscriminantAnalysisClass(BaseDiscriminantAnalysis analysis, int index, int classNumber)
        {
            this.analysis = analysis;
            this.Index = index;
            this.Number = classNumber;
        }

        /// <summary>
        ///     Gets the Index of this class on the original analysis collection.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Gets the number labeling this class.
        /// </summary>
        public int Number { get; }

        /// <summary>
        ///     Gets the prevalence of the class on the original data set.
        /// </summary>
        public double Prevalence => (double)Count / analysis.NumberOfSamples;

        /// <summary>
        ///     Gets the class' mean vector.
        /// </summary>
        public double[] Mean => analysis.classMeans[Index];

        /// <summary>
        ///     Gets the feature-space means of the projected data.
        /// </summary>
        public double[] ProjectionMean => analysis.projectedMeans[Index];

        /// <summary>
        ///     Gets the class' standard deviation vector.
        /// </summary>
        public double[] StandardDeviation => analysis.classStdDevs[Index];

        /// <summary>
        ///     Gets the Scatter matrix for this class.
        /// </summary>
        public double[][] Scatter => analysis.classScatter[Index];

        /// <summary>
        ///     Gets the number of observations inside this class.
        /// </summary>
        public int Count => analysis.classCount[Index];

        /// <summary>
        ///     Discriminant function for the class.
        /// </summary>
        public double DiscriminantFunction(double[] projection)
        {
            return analysis.DiscriminantFunction(projection, Index);
        }

#pragma warning disable 612, 618
        /// <summary>
        ///     Gets the indices of the rows in the original data which belong to this class.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public int[] Indices
        {
            get { return analysis.Classifications.Find(y => y == Number); }
        }

        /// <summary>
        ///     Gets the subset of the original data spawned by this class.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Subset => analysis.Source.Submatrix(Indices);
#pragma warning restore 612, 618
    }

    /// <summary>
    ///     <para>
    ///         Represents a discriminant factor found during Discriminant Analysis,
    ///         allowing it to be bound to controls like the DataGridView.
    ///     </para>
    ///     <para>
    ///         This class cannot be instantiated.
    ///     </para>
    /// </summary>
    [Serializable]
    public class Discriminant : IAnalysisComponent
    {
        private BaseDiscriminantAnalysis analysis;

        /// <summary>
        ///     Creates a new discriminant factor representation.
        /// </summary>
        internal Discriminant(BaseDiscriminantAnalysis analysis, int index)
        {
            this.analysis = analysis;
            this.Index = index;
        }

        /// <summary>
        ///     Gets the Eigenvector for this discriminant factor.
        /// </summary>
        public double[] Eigenvector => analysis.DiscriminantVectors[Index];

        /// <summary>
        ///     Gets the Eigenvalue for this discriminant factor.
        /// </summary>
        public double Eigenvalue => analysis.Eigenvalues[Index];

        /// <summary>
        ///     Gets the index of this discriminant factor
        ///     on the original analysis collection.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Gets the proportion, or amount of information explained by this discriminant factor.
        /// </summary>
        public double Proportion => analysis.DiscriminantProportions[Index];

        /// <summary>
        ///     Gets the cumulative proportion of all discriminant factors until this factor.
        /// </summary>
        [DisplayName("Cumulative")]
        public double CumulativeProportion => analysis.CumulativeProportions[Index];
    }

    /// <summary>
    ///     <para>
    ///         Represents a collection of Discriminants factors found in the Discriminant Analysis.
    ///     </para>
    ///     <para>
    ///         This class cannot be instantiated.
    ///     </para>
    /// </summary>
    [Serializable]
    public class DiscriminantCollection : ReadOnlyCollection<Discriminant>
    {
        internal DiscriminantCollection(Discriminant[] components)
            : base(components)
        {
        }
    }

    /// <summary>
    ///     <para>
    ///         Represents a collection of classes found in the Discriminant Analysis.
    ///     </para>
    ///     <para>
    ///         This class cannot be instantiated.
    ///     </para>
    /// </summary>
    [Serializable]
    public class DiscriminantAnalysisClassCollection : ReadOnlyCollection<DiscriminantAnalysisClass>
    {
        internal DiscriminantAnalysisClassCollection(DiscriminantAnalysisClass[] components)
            : base(components)
        {
        }
    }

    #endregion
}