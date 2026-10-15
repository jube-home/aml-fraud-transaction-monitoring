/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics.Models
{
    public sealed record DescriptiveScalar
    {
        public double Value { get; init; }
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveLocatedValue
    {
        public double Value { get; init; }
        public int Index { get; init; }
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveRangeSummary
    {
        public double Minimum { get; init; }
        public double Maximum { get; init; }
        public double Range { get; init; }
        public double Midrange { get; init; }
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveCountProfile
    {
        public int Total { get; init; }
        public int Valid { get; init; }
        public int Missing { get; init; }
        public int Infinite { get; init; }
        public int Distinct { get; init; }
        public int Zeros { get; init; }
        public int Positives { get; init; }
        public int Negatives { get; init; }
        public double MissingFraction { get; init; }
    }

    public sealed record DescriptiveNumericModeResult
    {
        public double[] Modes { get; init; } = Array.Empty<double>();
        public int Frequency { get; init; }
        public bool IsUnique { get; init; }
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveCategoryModeResult
    {
        public string[] Modes { get; init; } = Array.Empty<string>();
        public int Frequency { get; init; }
        public bool IsUnique { get; init; }
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveSeriesResult
    {
        public double[] Values { get; init; } = Array.Empty<double>();
        public int Count { get; init; }
        public int MissingCount { get; init; }
    }

    public sealed record DescriptiveQuantileTable
    {
        public double[] Probabilities { get; init; } = Array.Empty<double>();
        public double[] Values { get; init; } = Array.Empty<double>();
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveFiveNumberSummaryResult
    {
        public double Minimum { get; init; }
        public double LowerQuartile { get; init; }
        public double Median { get; init; }
        public double UpperQuartile { get; init; }
        public double Maximum { get; init; }
        public double InterquartileRange { get; init; }
        public double LowerInnerFence { get; init; }
        public double UpperInnerFence { get; init; }
        public double LowerOuterFence { get; init; }
        public double UpperOuterFence { get; init; }
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveRankResult
    {
        public double[] Ranks { get; init; } = Array.Empty<double>();
        public bool HasTies { get; init; }
        public int TiedGroups { get; init; }
        public int Count { get; init; }
        public int MissingCount { get; init; }
    }

    public sealed record DescriptiveFrequencyRow
    {
        public string Value { get; init; } = string.Empty;
        public int Count { get; init; }
        public double Proportion { get; init; }
        public double Percent { get; init; }
        public int CumulativeCount { get; init; }
        public double CumulativeProportion { get; init; }
    }

    public sealed record DescriptiveFrequencyTableResult
    {
        public DescriptiveFrequencyRow[] Rows { get; init; } = Array.Empty<DescriptiveFrequencyRow>();
        public int Total { get; init; }
        public int Distinct { get; init; }
        public int MissingCount { get; init; }
        public int RowsOmitted { get; init; }
    }

    public sealed record DescriptiveShapeSummary
    {
        public double Skewness { get; init; }
        public double ExcessKurtosis { get; init; }
        public double Kurtosis { get; init; }
        public bool UnbiasedEstimators { get; init; }
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveCircularSummary
    {
        public double MeanDirection { get; init; }
        public double Median { get; init; }
        public double ResultantLength { get; init; }
        public double CircularVariance { get; init; }
        public double CircularStandardDeviation { get; init; }
        public double AngularDeviation { get; init; }
        public double Concentration { get; init; }
        public double Skewness { get; init; }
        public double Kurtosis { get; init; }
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveLorenzPoint
    {
        public double PopulationShare { get; init; }
        public double ValueShare { get; init; }
    }

    public sealed record DescriptiveInequalityResult
    {
        public double Gini { get; init; }
        public double Theil { get; init; }
        public double HerfindahlIndex { get; init; }
        public double CoefficientOfVariation { get; init; }
        public DescriptiveLorenzPoint[] Lorenz { get; init; } = Array.Empty<DescriptiveLorenzPoint>();
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveOutlierResult
    {
        public string Method { get; init; } = string.Empty;
        public bool[] IsOutlier { get; init; } = Array.Empty<bool>();
        public int[] OutlierIndices { get; init; } = Array.Empty<int>();
        public double[] OutlierValues { get; init; } = Array.Empty<double>();
        public double[] Scores { get; init; } = Array.Empty<double>();
        public double LowerBound { get; init; }
        public double UpperBound { get; init; }
        public double Centre { get; init; }
        public double Scale { get; init; }
        public int OutlierCount { get; init; }
        public int Count { get; init; }
        public int MissingCount { get; init; }
    }

    public sealed record DescriptiveColumnSummary
    {
        public string Name { get; init; } = string.Empty;
        public int Count { get; init; }
        public int Missing { get; init; }
        public int Distinct { get; init; }
        public double Sum { get; init; }
        public double Mean { get; init; }
        public double Median { get; init; }
        public double StandardDeviation { get; init; }
        public double Variance { get; init; }
        public double StandardError { get; init; }
        public double CoefficientOfVariation { get; init; }
        public double Minimum { get; init; }
        public double Maximum { get; init; }
        public double Range { get; init; }
        public double LowerQuartile { get; init; }
        public double UpperQuartile { get; init; }
        public double InterquartileRange { get; init; }
        public double MedianAbsoluteDeviation { get; init; }
        public double Skewness { get; init; }
        public double ExcessKurtosis { get; init; }
        public double GeometricMean { get; init; }
        public double Mode { get; init; }
        public int Zeros { get; init; }
    }

    public sealed record DescriptiveTableSummaryResult
    {
        public DescriptiveColumnSummary[] Columns { get; init; } = Array.Empty<DescriptiveColumnSummary>();
        public int RowCount { get; init; }
        public int ColumnCount { get; init; }
    }

    public sealed record DescriptiveColumnDescription
    {
        public DescriptiveColumnSummary Summary { get; init; } = new();
        public double[] Percentiles { get; init; } = Array.Empty<double>();
        public double[] PercentileLevels { get; init; } = Array.Empty<double>();
        public double HarmonicMean { get; init; }
        public double TrimmedMean { get; init; }
        public double MeanAbsoluteDeviation { get; init; }
        public double ExcessKurtosis { get; init; }
        public double Kurtosis { get; init; }
        public double Trimean { get; init; }
        public int ZScoreOutlierCount { get; init; }
        public int IqrOutlierCount { get; init; }
        public int MadOutlierCount { get; init; }
        public double LowerInnerFence { get; init; }
        public double UpperInnerFence { get; init; }
        public double Gini { get; init; }
        public DescriptiveFrequencyRow[] TopValues { get; init; } = Array.Empty<DescriptiveFrequencyRow>();
        public string ShapeReading { get; init; } = string.Empty;
    }

    public sealed record DescriptiveMatrixResult
    {
        public string[] Names { get; init; } = Array.Empty<string>();
        public double[][] Values { get; init; } = Array.Empty<double[]>();
        public int RowsUsed { get; init; }
        public int RowsDropped { get; init; }
    }

    public sealed record DescriptiveCorrelationPair
    {
        public string First { get; init; } = string.Empty;
        public string Second { get; init; } = string.Empty;
        public double Correlation { get; init; }
        public double Covariance { get; init; }
    }

    public sealed record DescriptiveCorrelationPairsResult
    {
        public DescriptiveCorrelationPair[] Pairs { get; init; } = Array.Empty<DescriptiveCorrelationPair>();
        public int RowsUsed { get; init; }
        public int RowsDropped { get; init; }
    }

    public sealed record DescriptiveColumnMissingness
    {
        public string Name { get; init; } = string.Empty;
        public int Present { get; init; }
        public int Missing { get; init; }
        public double MissingFraction { get; init; }
        public int Infinite { get; init; }
    }

    public sealed record DescriptiveMissingProfileResult
    {
        public DescriptiveColumnMissingness[] Columns { get; init; } = Array.Empty<DescriptiveColumnMissingness>();
        public int RowCount { get; init; }
        public int CompleteRows { get; init; }
        public int RowsWithAnyMissing { get; init; }
        public int[] MissingCountPerRowDistribution { get; init; } = Array.Empty<int>();
        public int[] FirstIncompleteRowIndices { get; init; } = Array.Empty<int>();
    }

    public sealed record DescriptiveTableResult
    {
        public string[] Names { get; init; } = Array.Empty<string>();
        public double[][] Rows { get; init; } = Array.Empty<double[]>();
    }

    public sealed record DescriptiveGroupedSummaryRow
    {
        public string Group { get; init; } = string.Empty;
        public int Count { get; init; }
        public double Mean { get; init; }
        public double Median { get; init; }
        public double StandardDeviation { get; init; }
        public double Minimum { get; init; }
        public double Maximum { get; init; }
        public double Sum { get; init; }
    }

    public sealed record DescriptiveGroupedSummaryResult
    {
        public DescriptiveGroupedSummaryRow[] Groups { get; init; } = Array.Empty<DescriptiveGroupedSummaryRow>();
        public double PooledStandardDeviation { get; init; }
        public double GrandMean { get; init; }
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveCategoricalSummaryResult
    {
        public int Total { get; init; }
        public int Missing { get; init; }
        public int Distinct { get; init; }
        public string[] Modes { get; init; } = Array.Empty<string>();
        public int ModeFrequency { get; init; }
        public double ShannonEntropyBits { get; init; }
        public double NormalisedEntropy { get; init; }
        public double GiniImpurity { get; init; }
        public double HerfindahlIndex { get; init; }
        public int SingletonCount { get; init; }
        public DescriptiveFrequencyRow[] TopValues { get; init; } = Array.Empty<DescriptiveFrequencyRow>();
    }

    public sealed record DescriptiveEntropyResult
    {
        public double Entropy { get; init; }
        public string Unit { get; init; } = string.Empty;
        public double MaximumEntropy { get; init; }
        public double NormalisedEntropy { get; init; }
        public double Perplexity { get; init; }
        public int Categories { get; init; }
        public double Total { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveThresholdShare
    {
        public double Threshold { get; init; }
        public int Above { get; init; }
        public int Below { get; init; }
        public int Equal { get; init; }
        public int Count { get; init; }
        public double ProportionAbove { get; init; }
        public double ProportionBelow { get; init; }
        public double ProportionEqual { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveTransformResult
    {
        public double[] Values { get; init; } = Array.Empty<double>();
        public double Centre { get; init; }
        public double Scale { get; init; }
        public int Count { get; init; }
        public int MissingCount { get; init; }
    }

    public sealed record DescriptiveConcentrationResult
    {
        public double[] Fractions { get; init; } = Array.Empty<double>();
        public double[] Shares { get; init; } = Array.Empty<double>();
        public int[] HoldersIncluded { get; init; } = Array.Empty<int>();
        public double[] ShareTargets { get; init; } = Array.Empty<double>();
        public int[] HoldersNeeded { get; init; } = Array.Empty<int>();
        public double[] HolderFractionNeeded { get; init; } = Array.Empty<double>();
        public double Total { get; init; }
        public int Count { get; init; }
        public int MissingIgnored { get; init; }
    }

    public sealed record DescriptiveTableDescription
    {
        public DescriptiveTableSummaryResult Summary { get; init; } = new();
        public DescriptiveMissingProfileResult Missing { get; init; } = new();

        public DescriptiveCorrelationPair[] StrongestCorrelations { get; init; } =
            Array.Empty<DescriptiveCorrelationPair>();

        public string[] ConstantColumns { get; init; } = Array.Empty<string>();
        public string[] SkewedColumns { get; init; } = Array.Empty<string>();
        public string[] ColumnsWithOutliers { get; init; } = Array.Empty<string>();
        public string[] ColumnsWithMissingValues { get; init; } = Array.Empty<string>();
    }

    public sealed record DescriptiveColumnQuantilesResult
    {
        public string[] ColumnNames { get; init; } = Array.Empty<string>();
        public double[] Probabilities { get; init; } = Array.Empty<double>();
        public double[][] Values { get; init; } = Array.Empty<double[]>();
        public int[] Counts { get; init; } = Array.Empty<int>();
    }
}