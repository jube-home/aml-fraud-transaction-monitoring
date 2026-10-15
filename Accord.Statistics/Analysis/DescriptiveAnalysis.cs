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
    ///     Descriptive statistics analysis.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Descriptive statistics are used to describe the basic features of the data
    ///         in a study. They provide simple summaries about the sample and the measures.
    ///         Together with simple graphics analysis, they form the basis of virtually
    ///         every quantitative analysis of data.
    ///     </para>
    ///     <para>
    ///         This class can also be bound to standard controls such as the
    ///         <a href="http://msdn.microsoft.com/en-us/library/system.windows.forms.datagridview.aspx">DataGridView</a>
    ///         by setting their DataSource property to the analysis' <see cref="Measures" /> property.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Descriptive Statistics. Available on:
    ///                     http://en.wikipedia.org/wiki/Descriptive_statistics
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\DescriptiveAnalysisTest.cs" region="doc_learn" />
    /// </example>
    /// <seealso cref="Statistics.Tools" />
    /// <seealso cref="DescriptiveMeasures" />
    [Serializable]
#pragma warning disable 612, 618
    public class DescriptiveAnalysis : IMultivariateAnalysis,
        IDescriptiveLearning<DescriptiveAnalysis, double[]>
#pragma warning restore 612, 618
    {
        private string[] columnNames;
        private DoubleRange[] confidence;
        private double[,] correlationMatrix;

        private double[,] covarianceMatrix;
        private double[,] dScores;
        private DoubleRange[] deviance;
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
        private double[] skewness;
        private double[][] sourceArray;

        private double[,] sourceMatrix;
        private double[] standardDeviations;
        private double[] standardErrors;

        private double[] sums;
        private double[] variances;

        private double[,] zScores;

        /// <summary>
        ///     Constructs the Descriptive Analysis.
        /// </summary>
        public DescriptiveAnalysis()
        {
        }

        /// <summary>
        ///     Constructs the Descriptive Analysis.
        /// </summary>
        /// <param name="columnNames">Names for the analyzed variables.</param>
        public DescriptiveAnalysis(string[] columnNames)
        {
            if (columnNames == null)
            {
                throw new ArgumentNullException("columnNames");
            }

            init(null, null, columnNames);
        }

        /// <summary>
        ///     Constructs the Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        [Obsolete("Please call the Learn() method passing the data to be analyzed.")]
        public DescriptiveAnalysis(double[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var matrix = new double[data.Length, 1];

            Buffer.BlockCopy(data, 0, matrix, 0, data.Length * sizeof(double));

            init(matrix, null, null);
        }

        /// <summary>
        ///     Constructs the Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        [Obsolete("Please call the Learn() method passing the data to be analyzed.")]
        public DescriptiveAnalysis(double[,] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            init(data, null, null);
        }

        /// <summary>
        ///     Constructs the Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="columnNames">Names for the analyzed variables.</param>
        [Obsolete("Please pass only columnNames and call the Learn() method passing the data to be analyzed.")]
        public DescriptiveAnalysis(double[,] data, string[] columnNames)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (columnNames == null)
            {
                throw new ArgumentNullException("columnNames");
            }

            init(data, null, columnNames);
        }

        /// <summary>
        ///     Constructs the Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        [Obsolete("Please call the Learn() method passing the data to be analyzed.")]
        public DescriptiveAnalysis(double[][] data)
        {
            // Initial argument checking
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            init(null, data, null);
        }

        /// <summary>
        ///     Constructs the Descriptive Analysis.
        /// </summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="columnNames">Names for the analyzed variables.</param>
        [Obsolete("Please pass only columnNames and call the Learn() method passing the data to be analyzed.")]
        public DescriptiveAnalysis(double[][] data, string[] columnNames)
        {
            // Initial argument checking
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (columnNames == null)
            {
                throw new ArgumentNullException("columnNames");
            }

            init(null, data, columnNames);
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
        public double[][] Array
        {
            get
            {
                if (sourceArray == null)
                {
                    sourceArray = sourceMatrix.ToJagged();
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
            set => columnNames = value;
        }

        /// <summary>
        ///     Gets the mean subtracted data.
        /// </summary>
        public double[,] DeviationScores
        {
            get
            {
                if (dScores == null)
                {
                    if (sourceMatrix != null)
                    {
                        dScores = sourceMatrix.Center(Means);
                    }
                    else
                    {
                        dScores = sourceArray.Center(Means).ToMatrix();
                    }
                }

                return dScores;
            }
        }

        /// <summary>
        ///     Gets the mean subtracted and deviation divided data. Also known as Z-Scores.
        /// </summary>
        public double[,] StandardScores
        {
            get
            {
                if (zScores == null)
                {
                    zScores = DeviationScores.Standardize(StandardDeviations);
                }

                return zScores;
            }
        }

        /// <summary>
        ///     Gets the Covariance Matrix
        /// </summary>
        public double[,] CovarianceMatrix
        {
            get
            {
                if (covarianceMatrix == null)
                {
                    if (sourceMatrix != null)
                    {
                        covarianceMatrix = sourceMatrix.Covariance(Means);
                    }
                    else
                    {
                        covarianceMatrix = sourceArray.Covariance(Means).ToMatrix();
                    }
                }

                return covarianceMatrix;
            }
        }

        /// <summary>
        ///     Gets the Correlation Matrix
        /// </summary>
        public double[,] CorrelationMatrix
        {
            get
            {
                if (correlationMatrix == null)
                {
                    if (sourceMatrix != null)
                    {
                        correlationMatrix = sourceMatrix.Correlation(Means, StandardDeviations);
                    }
                    else
                    {
                        correlationMatrix = sourceArray.Correlation(Means, StandardDeviations).ToMatrix();
                    }
                }

                return correlationMatrix;
            }
        }

        /// <summary>
        ///     Gets a vector containing the Mean of each data column.
        /// </summary>
        public double[] Means
        {
            get
            {
                if (means == null)
                {
                    if (sourceMatrix != null)
                    {
                        means = sourceMatrix.Mean(Sums);
                    }
                    else
                    {
                        means = sourceArray.Mean(Sums);
                    }
                }

                return means;
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
                    if (sourceMatrix != null)
                    {
                        standardDeviations = sourceMatrix.StandardDeviation(Means);
                    }
                    else
                    {
                        standardDeviations = sourceArray.StandardDeviation(Means);
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
                    standardErrors = Statistics.Measures.StandardError(Samples, StandardDeviations);
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
                if (confidence == null)
                {
                    confidence = new DoubleRange[Variables];
                    for (var i = 0; i < confidence.Length; i++)
                    {
                        confidence[i] = GetConfidenceInterval(i);
                    }
                }

                return confidence;
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
                if (deviance == null)
                {
                    deviance = new DoubleRange[Variables];
                    for (var i = 0; i < deviance.Length; i++)
                    {
                        deviance[i] = GetDevianceInterval(i);
                    }
                }

                return deviance;
            }
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
                    else
                    {
                        modes = sourceArray.Mode();
                    }
                }

                return modes;
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
                    if (sourceMatrix != null)
                    {
                        medians = sourceMatrix.Median(quantileMethod);
                    }
                    else
                    {
                        medians = sourceArray.Median(quantileMethod);
                    }
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
                    if (sourceMatrix != null)
                    {
                        variances = sourceMatrix.Variance(Means);
                    }
                    else
                    {
                        variances = sourceArray.Variance(Means);
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
                    else
                    {
                        distinct = sourceArray.DistinctCount();
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
                    if (sourceMatrix != null)
                    {
                        ranges = sourceMatrix.GetRange(0);
                    }
                    else
                    {
                        ranges = sourceArray.GetRange(0);
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
                    if (sourceMatrix != null)
                    {
                        medians = sourceMatrix.Quartiles(out quartiles, quantileMethod);
                    }
                    else
                    {
                        medians = sourceArray.Quartiles(out quartiles, quantileMethod);
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
                    }
                }

                return outerFences;
            }
        }

        /// <summary>
        ///     Gets an array containing the sum of each data column.
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
                    else
                    {
                        sums = sourceArray.Sum(0);
                    }
                }

                return sums;
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
                    if (sourceMatrix != null)
                    {
                        skewness = sourceMatrix.Skewness();
                    }
                    else
                    {
                        skewness = sourceArray.Skewness();
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
                    if (sourceMatrix != null)
                    {
                        kurtosis = sourceMatrix.Kurtosis();
                    }
                    else
                    {
                        kurtosis = sourceArray.Kurtosis();
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
        public DescriptiveMeasureCollection Measures { get; private set; }

        /// <summary>
        ///     Learns a model that can map the given inputs to the desired outputs.
        /// </summary>
        /// <param name="x">The model inputs.</param>
        /// <returns>
        ///     A model that has learned how to produce suitable outputs
        ///     given the input data <paramref name="x" />.
        /// </returns>
        public DescriptiveAnalysis Learn(double[][] x)
        {
            reset();

            init(null, x, columnNames);

            if (!lazy)
            {
                sums = Sums;
                means = Means;
                standardDeviations = StandardDeviations;
                ranges = Ranges;
                kurtosis = Kurtosis;
                skewness = Skewness;
                medians = Medians;
                modes = Modes;
                variances = Variances;
                standardErrors = StandardErrors;
                distinct = Distinct;
                quartiles = Quartiles;
                innerFences = InnerFences;
                outerFences = OuterFences;

                // Mean centered and standardized data
                dScores = DeviationScores;
                zScores = StandardScores;

                // Covariance and correlation
                covarianceMatrix = CovarianceMatrix;
                correlationMatrix = CorrelationMatrix;

                confidence = Confidence;
                deviance = Deviance;

                sourceArray = null;
                sourceMatrix = null;
            }

            return this;
        }

        /// <summary>
        ///     Computes the analysis using given source data and parameters.
        /// </summary>
        [Obsolete("Please use Learn() instead.")]
        public void Compute()
        {
            // Clear analysis
            reset();

            sums = Sums;
            means = Means;
            standardDeviations = StandardDeviations;
            ranges = Ranges;
            kurtosis = Kurtosis;
            skewness = Skewness;
            medians = Medians;
            modes = Modes;
            variances = Variances;
            standardErrors = StandardErrors;
            distinct = Distinct;
            quartiles = Quartiles;
            innerFences = InnerFences;
            outerFences = OuterFences;

            // Mean centered and standardized data
            dScores = DeviationScores;
            zScores = StandardScores;

            // Covariance and correlation
            covarianceMatrix = CovarianceMatrix;
            correlationMatrix = CorrelationMatrix;

            confidence = Confidence;
            deviance = Deviance;
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
                    sourceMatrix = sourceArray.ToMatrix();
                }

                return sourceMatrix;
            }
        }

        private void init(double[,] matrix, double[][] array, string[] columnNames)
        {
            sourceMatrix = matrix;
            sourceArray = array;
            this.columnNames = columnNames;

            if (matrix != null)
            {
                Samples = matrix.GetLength(0);
                Variables = matrix.GetLength(1);
            }
            else if (array != null)
            {
                Samples = array.Length;
                Variables = array[0].Length;
            }
            else
            {
                return;
            }

            // Create object-oriented structure to access data
            var measures = new DescriptiveMeasures[Variables];
            for (var i = 0; i < measures.Length; i++)
            {
                measures[i] = new DescriptiveMeasures(this, i);
            }

            Measures = new DescriptiveMeasureCollection(measures);
        }

        private void reset()
        {
            sums = null;
            means = null;
            standardDeviations = null;
            ranges = null;
            kurtosis = null;
            skewness = null;
            medians = null;
            modes = null;
            variances = null;
            standardErrors = null;
            distinct = null;
            dScores = null;
            zScores = null;
            covarianceMatrix = null;
            correlationMatrix = null;
            deviance = null;
            quartiles = null;
            innerFences = null;
            outerFences = null;
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
            var z = NormalDistribution.Standard
                .InverseDistributionFunction(0.5 + percent / 2.0);

            return new DoubleRange(
                Means[index] - z * StandardErrors[index],
                Means[index] + z * StandardErrors[index]);
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

            return new DoubleRange(
                Means[index] - z * StandardDeviations[index],
                Means[index] + z * StandardDeviations[index]);
        }
    }

    /// <summary>
    ///     Descriptive measures for a variable.
    /// </summary>
    /// <seealso cref="DescriptiveAnalysis" />
    [Serializable]
    public class DescriptiveMeasures : IDescriptiveMeasures
    {
        internal DescriptiveMeasures(DescriptiveAnalysis analysis, int index)
        {
            this.Analysis = analysis;
            this.Index = index;
        }

        /// <summary>
        ///     Gets the descriptive analysis
        ///     that originated this measure.
        /// </summary>
        [Browsable(false)]
        public DescriptiveAnalysis Analysis { get; }

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
        ///     Gets the variable's mode.
        /// </summary>
        public double Mode => Analysis.Modes[Index];

        /// <summary>
        ///     Gets the variable's variance.
        /// </summary>
        public double Variance => Analysis.Variances[Index];

        /// <summary>
        ///     Gets the variable's skewness.
        /// </summary>
        public double Skewness => Analysis.Skewness[Index];

        /// <summary>
        ///     Gets the variable's kurtosis.
        /// </summary>
        public double Kurtosis => Analysis.Kurtosis[Index];

        /// <summary>
        ///     Gets the variable's standard error of the mean.
        /// </summary>
        public double StandardError => Analysis.StandardErrors[Index];

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
    /// <seealso cref="DescriptiveMeasures" />
    /// <seealso cref="DescriptiveAnalysis" />
    [Serializable]
    public class DescriptiveMeasureCollection : ReadOnlyKeyedCollection<string, DescriptiveMeasures>
    {
        internal DescriptiveMeasureCollection(DescriptiveMeasures[] components)
            : base(components)
        {
        }

        /// <summary>
        ///     Gets the key for item.
        /// </summary>
        protected override string GetKeyForItem(DescriptiveMeasures item)
        {
            return item.Name;
        }
    }
}