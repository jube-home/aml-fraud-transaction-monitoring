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
using System.Threading;
using Accord.MachineLearning;
using Accord.Math;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Base class for Discriminant Analysis (LDA, QDA or KDA).
    /// </summary>
    [Serializable]
#pragma warning disable 612, 618
    public abstract class BaseDiscriminantAnalysis : TransformBase<double[], double[]>
#pragma warning restore 612, 618
    {
        private double[][] Sw, Sb, St; // Scatter matrices
        private DiscriminantAnalysisClassCollection classCollection;

        internal int[] classCount;
        internal double[][] classMeans;
        internal double[][][] classScatter;
        internal double[][] classStdDevs;

        private DiscriminantCollection discriminantCollection;

        private double[] eigenvalues;

        // TODO: Use Mahalanobis distance instead of Euclidean
        // for classification, considering projection covariances.
        // internal double[][] projectedPrecision;

        private double[][] eigenvectors;
        private int numClasses;

        private int numSamples;
        private int[] outputs;

        internal double[][] projectedMeans;

        private double[,] result;
        private double[,] source;
        private double threshold;
        [NonSerialized] private CancellationToken token;
        private double[] totalMeans;
        private double[] totalStdDevs;

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
        ///     Returns the original supplied data to be analyzed.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Source
        {
            get => source;
            protected set => source = value;
        }

        /// <summary>
        ///     Gets or sets the minimum variance proportion needed to keep a
        ///     discriminant component. If set to zero, all components will be
        ///     kept. Default is 0.001 (all components which contribute less
        ///     than 0.001 to the variance in the data will be discarded).
        /// </summary>
        public double Threshold
        {
            get => threshold;
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException("value", "Value must be between 0 and 1.");
                }

                threshold = value;
            }
        }

        /// <summary>
        ///     Gets the resulting projection of the source data given on
        ///     the creation of the analysis into discriminant space.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Result
        {
            get => result;
            protected set => result = value;
        }

        /// <summary>
        ///     Gets the original classifications (labels) of the source data
        ///     given on the moment of creation of this analysis object.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public int[] Classifications
        {
            get => outputs;
            protected set => outputs = value;
        }

        /// <summary>
        ///     Gets the number of samples used to create the analysis.
        /// </summary>
        public int NumberOfSamples
        {
            get => numSamples;
            protected set => numSamples = value;
        }

        /// <summary>
        ///     Gets the number of classes in the analysis.
        /// </summary>
        public int NumberOfClasses
        {
            get => numClasses;
            protected set => numClasses = value;
        }

        /// <summary>
        ///     Gets the mean of the original data given at method construction.
        /// </summary>
        public double[] Means
        {
            get => totalMeans;
            protected set => totalMeans = value;
        }

        /// <summary>
        ///     Gets the standard mean of the original data given at method construction.
        /// </summary>
        public double[] StandardDeviations
        {
            get => totalStdDevs;
            protected set => totalStdDevs = value;
        }

        /// <summary>
        ///     Gets the Within-Class Scatter Matrix for the data.
        /// </summary>
        public double[][] ScatterWithinClass
        {
            get => Sw;
            protected set => Sw = value;
        }

        /// <summary>
        ///     Gets the Between-Class Scatter Matrix for the data.
        /// </summary>
        public double[][] ScatterBetweenClass
        {
            get => Sb;
            protected set => Sb = value;
        }

        /// <summary>
        ///     Gets the Total Scatter Matrix for the data.
        /// </summary>
        public double[][] ScatterMatrix
        {
            get => St;
            protected set => St = value;
        }

        /// <summary>
        ///     Gets the Eigenvectors obtained during the analysis,
        ///     composing a basis for the discriminant factor space.
        /// </summary>
        [Obsolete("Please use DiscriminantVectors.Transpose() instead.")]
        public double[,] DiscriminantMatrix
        {
            get
            {
                if (eigenvectors == null)
                {
                    return null;
                }

                return eigenvectors.Transpose().ToMatrix();
            }
        }

        /// <summary>
        ///     Gets the Eigenvectors obtained during the analysis,
        ///     composing a basis for the discriminant factor space.
        /// </summary>
        public double[][] DiscriminantVectors
        {
            get => eigenvectors;
            protected set => eigenvectors = value;
        }

        /// <summary>
        ///     Gets the Eigenvalues found by the analysis associated
        ///     with each vector of the ComponentMatrix matrix.
        /// </summary>
        public double[] Eigenvalues
        {
            get => eigenvalues;
            protected set => eigenvalues = value;
        }

        /// <summary>
        ///     Gets the level of importance each discriminant factor has in
        ///     discriminant space. Also known as amount of variance explained.
        /// </summary>
        public double[] DiscriminantProportions { get; private set; }

        /// <summary>
        ///     The cumulative distribution of the discriminants factors proportions.
        ///     Also known as the cumulative energy of the first dimensions of the discriminant
        ///     space or as the amount of variance explained by those dimensions.
        /// </summary>
        public double[] CumulativeProportions { get; private set; }

        /// <summary>
        ///     Gets the discriminant factors in a object-oriented fashion.
        /// </summary>
        public DiscriminantCollection Discriminants
        {
            get => discriminantCollection;
            protected set => discriminantCollection = value;
        }

        /// <summary>
        ///     Gets information about the distinct classes in the analyzed data.
        /// </summary>
        public DiscriminantAnalysisClassCollection Classes
        {
            get => classCollection;
            protected set => classCollection = value;
        }

        /// <summary>
        ///     Gets the Scatter matrix for each class.
        /// </summary>
        protected double[][][] ClassScatter => classScatter;

        /// <summary>
        ///     Gets the Mean vector for each class.
        /// </summary>
        protected double[][] ClassMeans => classMeans;

        /// <summary>
        ///     Gets the feature space mean of the projected data.
        /// </summary>
        protected double[][] ProjectionMeans => projectedMeans;

        /// <summary>
        ///     Gets the Standard Deviation vector for each class.
        /// </summary>
        protected double[][] ClassStandardDeviations => classStdDevs;

        /// <summary>
        ///     Gets the observation count for each class.
        /// </summary>
        protected int[] ClassCount => classCount;

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete]
        protected void init(double[,] inputs, int[] outputs)
        {
            // Gets the number of classes
            var startingClass = outputs.Min();
            NumberOfClasses = outputs.Max() - startingClass + 1;
            NumberOfSamples = inputs.Rows();
            NumberOfInputs = inputs.Columns();
            NumberOfOutputs = inputs.Columns();

            // Store the original data
            Source = inputs;
            Classifications = outputs;

            // Creates simple structures to hold information later
            classCount = new int[NumberOfClasses];
            classMeans = new double[NumberOfClasses][];
            classStdDevs = new double[NumberOfClasses][];
            classScatter = new double[NumberOfClasses][][];
            projectedMeans = new double[NumberOfClasses][];

            // Creates the object-oriented structure to hold information about the classes
            var collection = new DiscriminantAnalysisClass[NumberOfClasses];
            for (var i = 0; i < collection.Length; i++)
            {
                collection[i] = new DiscriminantAnalysisClass(this, i, startingClass + i);
            }

            Classes = new DiscriminantAnalysisClassCollection(collection);
        }

        /// <summary>
        ///     Initializes common properties.
        /// </summary>
        protected void Init(double[][] inputs, int[] outputs)
        {
#pragma warning disable 612, 618
            if (Classifications != null)
            {
                return; // TODO: remove this
            }
#pragma warning restore 612, 618

            // Gets the number of classes
            NumberOfClasses = outputs.Max() + 1;
            NumberOfSamples = inputs.Rows();
            NumberOfInputs = inputs.Columns();
            //if (this.NumberOfOutputs == 0)
            //    this.NumberOfOutputs = inputs.Columns();

            // Creates simple structures to hold information later
            classCount = new int[NumberOfClasses];
            classMeans = new double[NumberOfClasses][];
            classStdDevs = new double[NumberOfClasses][];
            classScatter = new double[NumberOfClasses][][];
            projectedMeans = new double[NumberOfClasses][];

            // Creates the object-oriented structure to hold information about the classes
            var collection = new DiscriminantAnalysisClass[NumberOfClasses];
            for (var i = 0; i < collection.Length; i++)
            {
                collection[i] = new DiscriminantAnalysisClass(this, i, i);
            }

            Classes = new DiscriminantAnalysisClassCollection(collection);
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please set NumberOfOutputs to the desired number of dimensions and call Transform()")]
        public virtual double[,] Transform(double[,] data, int dimensions)
        {
            var previous = NumberOfOutputs;
            NumberOfOutputs = dimensions;
            var result = Transform(data.ToJagged()).ToMatrix();
            NumberOfOutputs = previous;
            return result;
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please set NumberOfOutputs to the desired number of dimensions and call Transform()")]
        public virtual double[][] Transform(double[][] data, int dimensions)
        {
            var previous = NumberOfOutputs;
            NumberOfOutputs = dimensions;
            var result = Transform(data);
            NumberOfOutputs = previous;
            return result;
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please set NumberOfOutputs to the desired number of dimensions and call Transform()")]
        public double[] Transform(double[] data, int discriminants)
        {
            return Transform(data.ToJagged(), discriminants).GetRow(0);
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use jagged matrices instead.")]
        public double[,] Transform(double[,] data)
        {
            return Transform(data.ToJagged(), NumberOfOutputs).ToMatrix();
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override double[] Transform(double[] input)
        {
            return Transform(new[] { input }, new[] { new double[NumberOfOutputs] })[0];
        }

        /// <summary>
        ///     Applies the transformation to an input, producing an associated output.
        /// </summary>
        /// <param name="input">The input data to which the transformation should be applied.</param>
        /// <returns>
        ///     The output generated by applying this transformation to the given input.
        /// </returns>
        public override double[][] Transform(double[][] input)
        {
            return Transform(input, Jagged.Zeros(input.Length, NumberOfOutputs));
        }

        /// <summary>
        ///     Returns the minimum number of discriminant space dimensions (discriminant
        ///     factors) required to represent a given percentile of the data.
        /// </summary>
        /// <param name="threshold">The percentile of the data requiring representation.</param>
        /// <returns>The minimal number of dimensions required.</returns>
        public int GetNumberOfDimensions(double threshold)
        {
            if (threshold < 0 || threshold > 1.0)
            {
                throw new ArgumentException("Threshold should be a value between 0 and 1", "threshold");
            }

            for (var i = 0; i < CumulativeProportions.Length; i++)
            {
                if (CumulativeProportions[i] >= threshold)
                {
                    return i + 1;
                }
            }

            return CumulativeProportions.Length;
        }

        /// <summary>
        ///     Returns the number of discriminant space dimensions (discriminant
        ///     factors) whose variance is greater than a given threshold.
        /// </summary>
        protected static int GetNonzeroEigenvalues(double[] evals, double threshold)
        {
            // Calculate proportions
            var sum = 0.0;
            for (var i = 0; i < evals.Length; i++)
            {
                sum += System.Math.Abs(evals[i]);
            }

            sum = sum == 0 ? 0 : 1.0 / sum;

            var proportions = new double[evals.Length];
            for (var i = 0; i < evals.Length; i++)
            {
                proportions[i] = System.Math.Abs(evals[i]) * sum;
            }

            for (var i = 0; i < proportions.Length; i++)
            {
                if (proportions[i] < threshold)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        ///     Classifies a new instance into one of the available classes.
        /// </summary>
        [Obsolete("Please use Classifier.Decide() instead.")]
        public abstract int Classify(double[] input);

        /// <summary>
        ///     Classifies a new instance into one of the available classes.
        /// </summary>
        [Obsolete("Please use Classifier.Decide() or Classifier.Scores() instead.")]
        public abstract int Classify(double[] input, out double[] responses);

        /// <summary>
        ///     Classifies new instances into one of the available classes.
        /// </summary>
        [Obsolete("Please use Classifier.Decide() instead.")]
        public abstract int[] Classify(double[][] inputs);

        /// <summary>
        ///     Gets the output of the discriminant function for a given class.
        /// </summary>
        public abstract double DiscriminantFunction(double[] input, int classIndex);

        /// <summary>
        ///     Creates additional information about principal components.
        /// </summary>
        protected void CreateDiscriminants()
        {
            if (classCollection == null)
            {
                // Creates simple structures to hold information later
                classCount = new int[NumberOfOutputs];
                classMeans = new double[NumberOfOutputs][];
                classStdDevs = new double[NumberOfOutputs][];
                classScatter = new double[NumberOfOutputs][][];
                projectedMeans = new double[NumberOfOutputs][];


                // Creates the object-oriented structure to hold information about the classes
                var collection = new DiscriminantAnalysisClass[NumberOfOutputs];
                for (var i = 0; i < collection.Length; i++)
                {
                    collection[i] = new DiscriminantAnalysisClass(this, i, i);
                }

                classCollection = new DiscriminantAnalysisClassCollection(collection);
            }

            var numDiscriminants = eigenvalues.Length;
            DiscriminantProportions = new double[numDiscriminants];
            CumulativeProportions = new double[numDiscriminants];


            // Calculate total scatter matrix
            St = Sw.Add(Sb);

            // Calculate proportions
            var sum = 0.0;
            for (var i = 0; i < numDiscriminants; i++)
            {
                sum += System.Math.Abs(eigenvalues[i]);
            }

            sum = sum == 0 ? 0 : 1.0 / sum;

            for (var i = 0; i < numDiscriminants; i++)
            {
                DiscriminantProportions[i] = System.Math.Abs(eigenvalues[i]) * sum;
            }


            // Calculate cumulative proportions
            if (numDiscriminants > 0)
            {
                CumulativeProportions[0] = DiscriminantProportions[0];
                for (var i = 1; i < CumulativeProportions.Length; i++)
                {
                    CumulativeProportions[i] = CumulativeProportions[i - 1] + DiscriminantProportions[i];
                }
            }


            // Creates the object-oriented structure to hold the linear discriminants
            var discriminants = new Discriminant[numDiscriminants];
            for (var i = 0; i < discriminants.Length; i++)
            {
                discriminants[i] = new Discriminant(this, i);
            }

            discriminantCollection = new DiscriminantCollection(discriminants);
        }
    }
}