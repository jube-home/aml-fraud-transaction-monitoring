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
using System.ComponentModel;
using Accord.Collections;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Analysis
{
    /// <summary>
    ///     Descriptive statistics analysis for circular data.
    /// </summary>
    /// <seealso cref="Statistics.Tools" />
    /// <seealso cref="DescriptiveAnalysis" />
    /// <seealso cref="DescriptiveMeasures" />
    [Serializable]
#pragma warning disable 612, 618
    public class CircularDescriptiveAnalysis : IMultivariateAnalysis,
        IDescriptiveLearning<CircularDescriptiveAnalysis, double[]>
#pragma warning restore 612, 618
    {
        private double[][] angles;

        private double[] angularMeans;
        private double[] angularMedians;

        private string[] columnNames;

        private double[] concentration;

        private DoubleRange[] confidences;
        private double[] cos;
        private DoubleRange[] deviances;
        private int[] distinct;
        private DoubleRange[] innerFences;
        private double[] kurtosis;
        private bool lazy = true;

        private double[] means;

        private double[] medians;
        private double[] modes;
        private DoubleRange[] outerFences;

        private QuantileMethod quantileMethod = QuantileMethod.Default;
        private DoubleRange[] quartiles;
        private DoubleRange[] ranges;
        private double[] sin;
        private double[] skewness;
        private double[][] sourceArray;

        private double[,] sourceMatrix;
        private double[] sourceRow;
        private double[] standardDeviations;
        private double[] standardErrors;

        private double[] sums;

        private bool useStrictRanges = true;
        private double[] variances;

        /// <summary>
        ///     Constructs the Circular Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="length">The length of each circular variable (i.e. 24 for hours).</param>
        /// <param name="columnName">The names for the analyzed variable.</param>
        /// <param name="inPlace">
        ///     Whether the analysis should conserve memory by doing
        ///     operations over the original <paramref name="data" /> array.
        /// </param>
        [Obsolete(
            "Please pass only the lengths and columnNames parameters and call the Learn() method passing the data to be analyzed.")]
        public CircularDescriptiveAnalysis(double[] data, double length, string columnName, bool inPlace = false)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            compute(data, null, null, new[] { length }, new[] { columnName }, inPlace);
        }

        /// <summary>
        ///     Constructs the Circular Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="length">The length of each circular variable (i.e. 24 for hours).</param>
        /// <param name="inPlace">
        ///     Whether the analysis should conserve memory by doing
        ///     operations over the original <paramref name="data" /> array.
        /// </param>
        [Obsolete(
            "Please pass only the lengths and columnNames parameters and call the Learn() method passing the data to be analyzed.")]
        public CircularDescriptiveAnalysis(double[] data, double length, bool inPlace = false)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            compute(data, null, null, new[] { length }, null, inPlace);
        }

        /// <summary>
        ///     Constructs the Circular Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="length">The length of each circular variable (i.e. 24 for hours).</param>
        [Obsolete(
            "Please pass only the lengths and columnNames parameters and call the Learn() method passing the data to be analyzed.")]
        public CircularDescriptiveAnalysis(double[,] data, double[] length)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (length == null)
            {
                throw new ArgumentNullException("length");
            }

            compute(null, data, null, length, null);
        }

        /// <summary>
        ///     Constructs the Circular Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="length">The length of each circular variable (i.e. 24 for hours).</param>
        /// <param name="columnNames">Names for the analyzed variables.</param>
        [Obsolete(
            "Please pass only the lengths and columnNames parameters and call the Learn() method passing the data to be analyzed.")]
        public CircularDescriptiveAnalysis(double[,] data, double[] length, string[] columnNames)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (length == null)
            {
                throw new ArgumentNullException("length");
            }

            if (columnNames == null)
            {
                throw new ArgumentNullException("columnNames");
            }

            compute(null, data, null, length, columnNames);
        }

        /// <summary>
        ///     Constructs the Circular Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="length">The length of each circular variable (i.e. 24 for hours).</param>
        [Obsolete(
            "Please pass only the lengths and columnNames parameters and call the Learn() method passing the data to be analyzed.")]
        public CircularDescriptiveAnalysis(double[][] data, double[] length)
        {
            // Initial argument checking
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (length == null)
            {
                throw new ArgumentNullException("length");
            }

            compute(null, null, data, length, null);
        }

        /// <summary>
        ///     Constructs the Circular Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="length">The length of each circular variable (i.e. 24 for hours).</param>
        /// <param name="columnNames">Names for the analyzed variables.</param>
        [Obsolete(
            "Please pass only the lengths and columnNames parameters and call the Learn() method passing the data to be analyzed.")]
        public CircularDescriptiveAnalysis(double[][] data, double[] length, string[] columnNames)
        {
            // Initial argument checking
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (length == null)
            {
                throw new ArgumentNullException("length");
            }

            if (columnNames == null)
            {
                throw new ArgumentNullException("columnNames");
            }

            compute(null, null, data, length, columnNames);
        }

        /// <summary>
        ///     Constructs the Circular Descriptive Analysis.
        /// </summary>
        /// <param name="length">The length of each circular variable (i.e. 24 for hours).</param>
        /// <param name="columnNames">Names for the analyzed variables.</param>
        public CircularDescriptiveAnalysis(double[] length, string[] columnNames)
        {
            compute(null, null, null, length, columnNames);
        }

        /// <summary>
        ///     Constructs the Circular Descriptive Analysis.
        /// </summary>
        /// <param name="length">The length of each circular variable (i.e. 24 for hours).</param>
        public CircularDescriptiveAnalysis(double[] length)
        {
            compute(null, null, null, length, null);
        }

        /// <summary>
        ///     Gets or sets whether all reported statistics should respect the circular
        ///     interval. For example, setting this property to <c>false</c> would allow
        ///     the <see cref="Confidence" />, <see cref="Deviance" />, <see cref="InnerFences" />
        ///     and <see cref="OuterFences" /> properties report minimum and maximum values
        ///     outside the variable's allowed circular range. Default is <c>true</c>.
        /// </summary>
        public bool UseStrictRanges
        {
            get => useStrictRanges;
            set
            {
                if (useStrictRanges != value)
                {
                    useStrictRanges = value;
                    innerFences = null;
                    outerFences = null;
                    deviances = null;
                    confidences = null;
                }
            }
        }

        /// <summary>
        ///     Gets the source matrix from which the analysis was run.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[][] Array
        {
            get
            {
                if (sourceArray == null)
                {
                    if (sourceRow == null)
                    {
                        sourceArray = sourceMatrix.ToJagged();
                    }
                    else
                    {
                        sourceArray = sourceRow.ToJagged();
                    }
                }

                return sourceArray;
            }
        }

        /// <summary>
        ///     Gets or sets the method to be used when computing quantiles (median and quartiles).
        /// </summary>
        /// <value>The quantile method.</value>
        public QuantileMethod QuantileMethod
        {
            get => quantileMethod;
            set => quantileMethod = value;
        }

        /// <summary>
        ///     Gets or sets whether the properties of this class should
        ///     be computed only when necessary. If set to true, a copy
        ///     of the input data will be maintained inside an instance
        ///     of this class, using more memory.
        /// </summary>
        public bool Lazy
        {
            get => lazy;
            set => lazy = value;
        }

        /// <summary>
        ///     Gets the source matrix from which the analysis was run.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[][] Angles => angles;

        /// <summary>
        ///     Gets the column names from the variables in the data.
        /// </summary>
        public string[] ColumnNames
        {
            get
            {
                if (columnNames == null)
                {
                    columnNames = new string[Variables];
                    for (var i = 0; i < columnNames.Length; i++)
                    {
                        columnNames[i] = "Column " + i;
                    }
                }

                return columnNames;
            }
        }

        /// <summary>
        ///     Gets a vector containing the length of
        ///     the circular domain for each data column.
        /// </summary>
        public double[] Lengths { get; private set; }

        /// <summary>
        ///     Gets a vector containing the Mean of each data column.
        /// </summary>
        public double[] Means
        {
            get
            {
                if (means == null)
                {
                    double[] c = CosineSum, s = SineSum;

                    means = new double[Variables];
                    angularMeans = new double[Variables];
                    for (var i = 0; i < means.Length; i++)
                    {
                        angularMeans[i] = Circular.Mean(Samples, c[i], s[i]);
                        means[i] = angularMeans[i].ToCircular(Lengths[i]);
                    }
                }

                return means;
            }
            private set => means = value;
        }

        /// <summary>
        ///     Gets a vector containing the Mode of each data column.
        /// </summary>
        public double[] Modes
        {
            get
            {
                if (modes == null)
                {
                    if (sourceMatrix != null)
                    {
                        modes = sourceMatrix.Mode();
                    }
                    else if (sourceArray != null)
                    {
                        modes = sourceArray.Mode();
                    }
                    else
                    {
                        modes = new[] { sourceRow.Mode() };
                    }
                }

                return modes;
            }
        }

        /// <summary>
        ///     Gets a vector containing the Standard Deviation of each data column.
        /// </summary>
        public double[] StandardDeviations
        {
            get
            {
                if (standardDeviations == null)
                {
                    double[] c = CosineSum, s = SineSum;

                    standardDeviations = new double[Variables];
                    for (var i = 0; i < standardDeviations.Length; i++)
                    {
                        standardDeviations[i] = Circular.StandardDeviation(Samples, c[i], s[i])
                            * Lengths[i] / (2 * System.Math.PI);
                    }
                }

                return standardDeviations;
            }
        }

        /// <summary>
        ///     Gets a vector containing the Standard Error of the Mean of each data column.
        /// </summary>
        public double[] StandardErrors
        {
            get
            {
                if (standardErrors == null)
                {
                    double[] c = CosineSum, s = SineSum;

                    standardErrors = new double[Variables];
                    for (var i = 0; i < standardErrors.Length; i++)
                    {
                        standardErrors[i] = Circular.StandardError(Samples, c[i], s[i], 0.05)
                            * Lengths[i] / (2 * System.Math.PI);
                    }
                }

                return standardErrors;
            }
        }

        /// <summary>
        ///     Gets the 95% confidence intervals for the <see cref="Means" />.
        /// </summary>
        public DoubleRange[] Confidence
        {
            get
            {
                if (confidences == null)
                {
                    confidences = new DoubleRange[Variables];
                    for (var i = 0; i < confidences.Length; i++)
                    {
                        confidences[i] = GetConfidenceInterval(i);
                    }
                }

                return confidences;
            }
        }

        /// <summary>
        ///     Gets the 95% deviance intervals for the <see cref="Means" />.
        /// </summary>
        /// <remarks>
        ///     A deviance interval uses the standard deviation rather
        ///     than the standard error to compute the range interval
        ///     for a variable.
        /// </remarks>
        public DoubleRange[] Deviance
        {
            get
            {
                if (deviances == null)
                {
                    deviances = new DoubleRange[Variables];
                    for (var i = 0; i < deviances.Length; i++)
                    {
                        deviances[i] = GetDevianceInterval(i);
                    }
                }

                return deviances;
            }
        }

        /// <summary>
        ///     Gets a vector containing the Median of each data column.
        /// </summary>
        public double[] Medians
        {
            get
            {
                if (medians == null)
                {
                    computeMedians();
                }

                return medians;
            }
        }

        /// <summary>
        ///     Gets a vector containing the Variance of each data column.
        /// </summary>
        public double[] Variances
        {
            get
            {
                if (variances == null)
                {
                    double[] c = CosineSum, s = SineSum;

                    variances = new double[Variables];
                    for (var i = 0; i < variances.Length; i++)
                    {
                        var scale = Lengths[i] / (2 * System.Math.PI);
                        variances[i] = Circular.Variance(Samples, c[i], s[i]) * scale * scale;
                    }
                }

                return variances;
            }
        }

        /// <summary>
        ///     Gets a vector containing the number of distinct elements for each data column.
        /// </summary>
        public int[] Distinct
        {
            get
            {
                if (distinct == null)
                {
                    if (sourceMatrix != null)
                    {
                        distinct = sourceMatrix.DistinctCount();
                    }
                    else if (sourceArray != null)
                    {
                        distinct = sourceArray.DistinctCount();
                    }
                    else
                    {
                        distinct = new[] { sourceRow.DistinctCount() };
                    }
                }

                return distinct;
            }
        }

        /// <summary>
        ///     Gets an array containing the Ranges of each data column.
        /// </summary>
        public DoubleRange[] Ranges
        {
            get
            {
                if (ranges == null)
                {
                    ranges = new DoubleRange[Variables];
                    for (var i = 0; i < ranges.Length; i++)
                    {
                        ranges[i] = new DoubleRange(0, Lengths[i]);
                    }
                }

                return ranges;
            }
        }

        /// <summary>
        ///     Gets an array containing the interquartile range of each data column.
        /// </summary>
        public DoubleRange[] Quartiles
        {
            get
            {
                if (quartiles == null)
                {
                    if (medians == null)
                    {
                        computeMedians();
                    }

                    quartiles = new DoubleRange[Variables];
                    for (var i = 0; i < variances.Length; i++)
                    {
                        Circular.Quartiles(angles[i], out quartiles[i], angularMedians[i], useStrictRanges,
                            quantileMethod);

                        quartiles[i].Min = quartiles[i].Min.ToCircular(Lengths[i], useStrictRanges);
                        quartiles[i].Max = quartiles[i].Max.ToCircular(Lengths[i], useStrictRanges);
                    }
                }

                return quartiles;
            }
        }

        /// <summary>
        ///     Gets an array containing the inner fences of each data column.
        /// </summary>
        public DoubleRange[] InnerFences
        {
            get
            {
                if (innerFences == null)
                {
                    var Q = Quartiles;

                    innerFences = new DoubleRange[Variables];
                    for (var i = 0; i < innerFences.Length; i++)
                    {
                        innerFences[i] = Q[i].InnerFence();

                        if (useStrictRanges)
                        {
                            innerFences[i].Min = Math.Tools.Mod(innerFences[i].Min, Lengths[i]);
                            innerFences[i].Max = Math.Tools.Mod(innerFences[i].Max, Lengths[i]);
                        }
                    }
                }

                return innerFences;
            }
        }

        /// <summary>
        ///     Gets an array containing the outer fences of each data column.
        /// </summary>
        public DoubleRange[] OuterFences
        {
            get
            {
                if (outerFences == null)
                {
                    var Q = Quartiles;

                    outerFences = new DoubleRange[Variables];
                    for (var i = 0; i < outerFences.Length; i++)
                    {
                        outerFences[i] = Q[i].OuterFence();

                        if (useStrictRanges)
                        {
                            outerFences[i].Min = Math.Tools.Mod(outerFences[i].Min, Lengths[i]);
                            outerFences[i].Max = Math.Tools.Mod(outerFences[i].Max, Lengths[i]);
                        }
                    }
                }

                return outerFences;
            }
        }

        /// <summary>
        ///     Gets an array containing the sum of each data column. If
        ///     the analysis has been computed in place, this will contain
        ///     the sum of the transformed angle values instead.
        /// </summary>
        public double[] Sums
        {
            get
            {
                if (sums == null)
                {
                    if (sourceMatrix != null)
                    {
                        sums = sourceMatrix.Sum(0);
                    }
                    else if (sourceArray != null)
                    {
                        sums = sourceArray.Sum(0);
                    }
                    else
                    {
                        sums = new[] { sourceRow.Sum() };
                    }
                }

                return sums;
            }
        }

        /// <summary>
        ///     Gets an array containing the sum of cosines for each data column.
        /// </summary>
        public double[] CosineSum
        {
            get
            {
                if (cos == null)
                {
                    computeSums();
                }

                return cos;
            }
        }

        /// <summary>
        ///     Gets an array containing the sum of sines for each data column.
        /// </summary>
        public double[] SineSum
        {
            get
            {
                if (sin == null)
                {
                    computeSums();
                }

                return sin;
            }
        }

        /// <summary>
        ///     Gets an array containing the circular concentration for each data column.
        /// </summary>
        public double[] Concentration
        {
            get
            {
                if (concentration == null)
                {
                    var m = Means;

                    concentration = new double[Variables];
                    for (var i = 0; i < variances.Length; i++)
                    {
                        concentration[i] = Circular.Concentration(angles[i], m[i]);
                    }
                }

                return variances;
            }
        }

        /// <summary>
        ///     Gets an array containing the skewness for of each data column.
        /// </summary>
        public double[] Skewness
        {
            get
            {
                if (skewness == null)
                {
                    skewness = new double[Variables];
                    for (var i = 0; i < skewness.Length; i++)
                    {
                        skewness[i] = Circular.Skewness(angles[i])
                            * Lengths[i] / (2 * System.Math.PI);
                    }
                }

                return skewness;
            }
        }

        /// <summary>
        ///     Gets an array containing the kurtosis for of each data column.
        /// </summary>
        public double[] Kurtosis
        {
            get
            {
                if (kurtosis == null)
                {
                    kurtosis = new double[Variables];
                    for (var i = 0; i < kurtosis.Length; i++)
                    {
                        kurtosis[i] = Circular.Kurtosis(angles[i])
                            * Lengths[i] / (2 * System.Math.PI);
                    }
                }

                return kurtosis;
            }
        }

        /// <summary>
        ///     Gets the number of samples (or observations) in the data.
        /// </summary>
        public int Samples { get; private set; }

        /// <summary>
        ///     Gets the number of variables (or features) in the data.
        /// </summary>
        public int Variables { get; private set; }

        /// <summary>
        ///     Gets a collection of DescriptiveMeasures objects that can be bound to a DataGridView.
        /// </summary>
        public CircularDescriptiveMeasureCollection Measures { get; private set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public CircularDescriptiveAnalysis Learn(double[][] x)
        {
            reset();

            compute(null, null, x, Lengths, columnNames);

            if (!lazy)
            {
                sums = Sums;
                means = Means;
                standardDeviations = StandardDeviations;
                ranges = Ranges;
                medians = Medians;
                variances = Variances;
                distinct = Distinct;
                quartiles = Quartiles;
                innerFences = InnerFences;
                outerFences = OuterFences;
                modes = Modes;
                cos = CosineSum;
                sin = SineSum;
                skewness = Skewness;
                kurtosis = Kurtosis;
                concentration = Concentration;
                deviances = Deviance;
                confidences = Confidence;
                sourceArray = null;
                sourceMatrix = null;
                sourceRow = null;
            }

            return this;
        }

        /// <summary>
        ///     Computes the analysis using given source data and parameters.
        /// </summary>
        [Obsolete("Please use Learn() instead.")]
        public void Compute()
        {
            reset();

            sums = Sums;
            means = Means;
            standardDeviations = StandardDeviations;
            ranges = Ranges;
            medians = Medians;
            variances = Variances;
            distinct = Distinct;
            quartiles = Quartiles;
            innerFences = InnerFences;
            outerFences = OuterFences;
            modes = Modes;
            cos = CosineSum;
            sin = SineSum;
            skewness = Skewness;
            kurtosis = Kurtosis;
            concentration = Concentration;
            deviances = Deviance;
            confidences = Confidence;
        }

        /// <summary>
        ///     Gets the source matrix from which the analysis was run.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[,] Source
        {
            get
            {
                if (sourceMatrix == null)
                {
                    if (sourceArray == null)
                    {
                        sourceMatrix = sourceRow.ToMatrix(true);
                    }
                    else
                    {
                        sourceMatrix = sourceArray.ToMatrix();
                    }
                }

                return sourceMatrix;
            }
        }

        private void compute(double[] row, double[,] matrix, double[][] array,
            double[] length, string[] columnNames, bool inPlace = false)
        {
            this.columnNames = columnNames;
            sourceArray = array;
            sourceMatrix = matrix;
            sourceRow = row;

            if (matrix != null)
            {
                Samples = matrix.Rows();
                Variables = matrix.Columns();
                if (Lengths == null)
                {
                    Lengths = matrix.Max(0);
                }

                if (Lengths.Length != Variables)
                {
                    throw new DimensionMismatchException("length");
                }

                Lengths = length;
                angles = new double[Variables][];
                for (var i = 0; i < angles.Length; i++)
                {
                    angles[i] = new double[Samples];
                    for (var j = 0; j < angles[i].Length; j++)
                    {
                        angles[i][j] = matrix[j, i].ToRadians(Lengths[i]);
                    }
                }
            }
            else if (array != null)
            {
                Samples = array.Length;
                Variables = array[0].Length;
                angles = new double[Variables][];
                if (Lengths == null)
                {
                    Lengths = array.Max(1);
                }

                if (Lengths.Length != Variables)
                {
                    throw new DimensionMismatchException("length");
                }

                Lengths = length;
                for (var i = 0; i < angles.Length; i++)
                {
                    angles[i] = new double[Samples];
                    for (var j = 0; j < angles[i].Length; j++)
                    {
                        angles[i][j] = array[j][i].ToRadians(length[i]);
                    }
                }
            }
            else
            {
                Samples = sourceRow.Length;
                Variables = 1;
                angles = new double[Variables][];
                if (Lengths == null)
                {
                    Lengths = new[] { sourceRow.Max() };
                }

                if (Lengths.Length != Variables)
                {
                    throw new DimensionMismatchException("length");
                }

                Lengths = length;
                angles[0] = inPlace ? sourceRow : new double[Samples];
                for (var j = 0; j < angles[0].Length; j++)
                {
                    angles[0][j] = sourceRow[j].ToRadians(length[0]);
                }
            }


            // Create object-oriented structure to access data
            var measures = new CircularDescriptiveMeasures[Variables];
            for (var i = 0; i < measures.Length; i++)
            {
                measures[i] = new CircularDescriptiveMeasures(this, i);
            }

            Measures = new CircularDescriptiveMeasureCollection(measures);
        }

        private void reset()
        {
            sums = null;
            means = null;
            standardDeviations = null;
            ranges = null;
            medians = null;
            variances = null;
            standardErrors = null;
            distinct = null;
            modes = null;
            deviances = null;
            confidences = null;
            quartiles = null;
            innerFences = null;
            outerFences = null;
            sin = null;
            cos = null;
            skewness = null;
            kurtosis = null;
            concentration = null;
            standardErrors = null;
        }

        /// <summary>
        ///     Gets a confidence interval for the <see cref="Means" />
        ///     within the given confidence level percentage.
        /// </summary>
        /// <param name="percent">The confidence level. Default is 0.95.</param>
        /// <param name="index">
        ///     The index of the data column whose confidence
        ///     interval should be calculated.
        /// </param>
        /// <returns>A confidence interval for the estimated value.</returns>
        public DoubleRange GetConfidenceInterval(int index, double percent = 0.95)
        {
            double[] c = CosineSum, s = SineSum;

            var t = Circular.StandardError(Samples, c[index], s[index], 1 - percent);

            t *= Lengths[index] / (2 * System.Math.PI);

            var min = Means[index] - t;
            var max = Means[index] + t;

            if (useStrictRanges)
            {
                min = Math.Tools.Mod(min, Lengths[index]);
                max = Math.Tools.Mod(max, Lengths[index]);
            }

            return new DoubleRange(min, max);
        }

        /// <summary>
        ///     Gets a deviance interval for the <see cref="Means" />
        ///     within the given confidence level percentage (i.e. uses
        ///     the standard deviation rather than the standard error to
        ///     compute the range interval for the variable).
        /// </summary>
        /// <param name="percent">The confidence level. Default is 0.95.</param>
        /// <param name="index">
        ///     The index of the data column whose confidence
        ///     interval should be calculated.
        /// </param>
        /// <returns>A confidence interval for the estimated value.</returns>
        public DoubleRange GetDevianceInterval(int index, double percent = 0.95)
        {
            var z = NormalDistribution.Standard
                .InverseDistributionFunction(0.5 + percent / 2.0);

            var min = Means[index] - z * StandardDeviations[index];
            var max = Means[index] + z * StandardDeviations[index];

            if (useStrictRanges)
            {
                min = Math.Tools.Mod(min, Lengths[index]);
                max = Math.Tools.Mod(max, Lengths[index]);
            }

            return new DoubleRange(min, max);
        }

        private void computeSums()
        {
            cos = new double[Variables];
            sin = new double[Variables];
            for (var i = 0; i < angles.Length; i++)
            {
                Circular.Sum(angles[i], out cos[i], out sin[i]);
            }
        }

        private void computeMedians()
        {
            medians = new double[Variables];
            angularMedians = new double[Variables];
            for (var i = 0; i < medians.Length; i++)
            {
                angularMedians[i] = Circular.Median(angles[i]);
                medians[i] = angularMedians[i].ToCircular(Lengths[i]);
            }
        }
    }

    /// <summary>
    ///     Circular descriptive measures for a variable.
    /// </summary>
    /// <seealso cref="CircularDescriptiveAnalysis" />
    [Serializable]
    public class CircularDescriptiveMeasures : IDescriptiveMeasures
    {
        internal CircularDescriptiveMeasures(CircularDescriptiveAnalysis analysis, int index)
        {
            this.Analysis = analysis;
            this.Index = index;
        }

        /// <summary>
        ///     Gets the circular analysis
        ///     that originated this measure.
        /// </summary>
        [Browsable(false)]
        public CircularDescriptiveAnalysis Analysis { get; }

        /// <summary>
        ///     Gets the sum of cosines for the variable.
        /// </summary>
        public double CosineSum => Analysis.CosineSum[Index];

        /// <summary>
        ///     Gets the sum of sines for the variable.
        /// </summary>
        public double SineSum => Analysis.SineSum[Index];

        /// <summary>
        ///     Gets the transformed variable's observations.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[] Angles => Analysis.Angles[Index];

        /// <summary>
        ///     Gets the variable's index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Gets the variable's name
        /// </summary>
        public string Name => Analysis.ColumnNames[Index];

        /// <summary>
        ///     Gets the variable's total sum.
        /// </summary>
        public double Sum => Analysis.Sums[Index];

        /// <summary>
        ///     Gets the variable's mean.
        /// </summary>
        public double Mean => Analysis.Means[Index];

        /// <summary>
        ///     Gets the variable's standard deviation.
        /// </summary>
        public double StandardDeviation => Analysis.StandardDeviations[Index];

        /// <summary>
        ///     Gets the variable's median.
        /// </summary>
        public double Median => Analysis.Medians[Index];

        /// <summary>
        ///     Gets the variable's mode.
        /// </summary>
        public double Mode => Analysis.Modes[Index];

        /// <summary>
        ///     Gets the variable's outer fences range.
        /// </summary>
        public DoubleRange OuterFence => Analysis.OuterFences[Index];

        /// <summary>
        ///     Gets the variable's inner fence range.
        /// </summary>
        public DoubleRange InnerFence => Analysis.InnerFences[Index];

        /// <summary>
        ///     Gets the variable's interquartile range.
        /// </summary>
        public DoubleRange Quartiles => Analysis.Quartiles[Index];

        /// <summary>
        ///     Gets the variable's variance.
        /// </summary>
        public double Variance => Analysis.Variances[Index];

        /// <summary>
        ///     Gets the variable's maximum value.
        /// </summary>
        public double Max => Analysis.Ranges[Index].Max;

        /// <summary>
        ///     Gets the variable's minimum value.
        /// </summary>
        public double Min => Analysis.Ranges[Index].Min;

        /// <summary>
        ///     Gets the variable's length.
        /// </summary>
        public double Length => Analysis.Ranges[Index].Length;

        /// <summary>
        ///     Gets the number of distinct values for the variable.
        /// </summary>
        public int Distinct => Analysis.Distinct[Index];

        /// <summary>
        ///     Gets the number of samples for the variable.
        /// </summary>
        public int Count => Analysis.Samples;

        /// <summary>
        ///     Gets the variable's standard error of the mean.
        /// </summary>
        public double StandardError => Analysis.StandardErrors[Index];

        /// <summary>
        ///     Gets the 95% confidence interval around the <see cref="Mean" />.
        /// </summary>
        public DoubleRange Confidence => Analysis.Confidence[Index];

        /// <summary>
        ///     Gets the 95% deviance interval around the <see cref="Mean" />.
        /// </summary>
        public DoubleRange Deviance => Analysis.Deviance[Index];

        /// <summary>
        ///     Gets the variable's observations.
        /// </summary>
        [Obsolete("This property will be removed.")]
        public double[] Samples => Analysis.Source.GetColumn(Index);

        /// <summary>
        ///     Gets the variable <see cref="Circular.Skewness">skewness</see>.
        /// </summary>
        public double Skewness => Analysis.Skewness[Index];

        /// <summary>
        ///     Gets the variable <see cref="Circular.Skewness">kurtosis</see>.
        /// </summary>
        public double Kurtosis => Analysis.Kurtosis[Index];

        /// <summary>
        ///     Gets a confidence interval for the <see cref="Mean" />
        ///     within the given confidence level percentage.
        /// </summary>
        /// <param name="percent">The confidence level. Default is 0.95.</param>
        /// <returns>A confidence interval for the estimated value.</returns>
        public DoubleRange GetConfidenceInterval(double percent = 0.95)
        {
            return Analysis.GetConfidenceInterval(Index, percent);
        }

        /// <summary>
        ///     Gets a deviance interval for the <see cref="Mean" />
        ///     within the given confidence level percentage (i.e. uses
        ///     the standard deviation rather than the standard error to
        ///     compute the range interval for the variable).
        /// </summary>
        /// <param name="percent">The confidence level. Default is 0.95.</param>
        /// <returns>A confidence interval for the estimated value.</returns>
        public DoubleRange GetDevianceInterval(double percent = 0.95)
        {
            return Analysis.GetDevianceInterval(Index, percent);
        }
    }

    /// <summary>
    ///     Collection of descriptive measures.
    /// </summary>
    /// <seealso cref="CircularDescriptiveMeasures" />
    /// <seealso cref="CircularDescriptiveAnalysis" />
    [Serializable]
    public class CircularDescriptiveMeasureCollection : ReadOnlyKeyedCollection<string, CircularDescriptiveMeasures>
    {
        internal CircularDescriptiveMeasureCollection(CircularDescriptiveMeasures[] components)
            : base(components)
        {
        }

        /// <summary>
        ///     Gets the key for item.
        /// </summary>
        protected override string GetKeyForItem(CircularDescriptiveMeasures item)
        {
            return item.Name;
        }
    }
}