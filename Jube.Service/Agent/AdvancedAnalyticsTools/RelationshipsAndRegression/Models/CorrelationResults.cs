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

using MessagePack;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models
{
    public enum CorrelationMethod
    {
        Pearson = 0,
        Spearman = 1,
        Kendall = 2
    }

    public enum KendallTauVariant
    {
        TauB = 0,
        TauA = 1,
        TauC = 2
    }

    public enum MutualInformationBinning
    {
        EqualWidth = 0,
        EqualFrequency = 1
    }

    [MessagePackObject]
    public sealed record CorrelationCoefficientResult
    {
        [Key(0)] public string Method { get; init; } = string.Empty;
        [Key(1)] public double Coefficient { get; init; }
        [Key(2)] public double CoefficientOfDetermination { get; init; }
        [Key(3)] public int PairCount { get; init; }
        [Key(4)] public int MissingPairsDropped { get; init; }
        [Key(5)] public string StatisticName { get; init; } = string.Empty;
        [Key(6)] public double Statistic { get; init; }
        [Key(7)] public double DegreesOfFreedom { get; init; }
        [Key(8)] public double PValue { get; init; }
        [Key(9)] public double ConfidenceLevel { get; init; }
        [Key(10)] public double ConfidenceLower { get; init; }
        [Key(11)] public double ConfidenceUpper { get; init; }
        [Key(12)] public string Interpretation { get; init; } = string.Empty;
    }

    [MessagePackObject]
    public sealed record KendallTauResult
    {
        [Key(0)] public string Variant { get; init; } = string.Empty;
        [Key(1)] public double Tau { get; init; }
        [Key(2)] public double TauA { get; init; }
        [Key(3)] public double TauB { get; init; }
        [Key(4)] public double TauC { get; init; }
        [Key(5)] public long ConcordantPairs { get; init; }
        [Key(6)] public long DiscordantPairs { get; init; }
        [Key(7)] public long PairsTiedInXOnly { get; init; }
        [Key(8)] public long PairsTiedInYOnly { get; init; }
        [Key(9)] public long PairsTiedInBoth { get; init; }
        [Key(10)] public int PairCount { get; init; }
        [Key(11)] public int MissingPairsDropped { get; init; }
        [Key(12)] public double ZStatistic { get; init; }
        [Key(13)] public double PValue { get; init; }
        [Key(14)] public double ConfidenceLevel { get; init; }
        [Key(15)] public double ConfidenceLower { get; init; }
        [Key(16)] public double ConfidenceUpper { get; init; }
        [Key(17)] public string Interpretation { get; init; } = string.Empty;
    }

    [MessagePackObject]
    public sealed record PointBiserialResult
    {
        [Key(0)] public string Method { get; init; } = string.Empty;
        [Key(1)] public double Coefficient { get; init; }
        [Key(2)] public double TStatistic { get; init; }
        [Key(3)] public double DegreesOfFreedom { get; init; }
        [Key(4)] public double PValue { get; init; }
        [Key(5)] public double ConfidenceLevel { get; init; }
        [Key(6)] public double ConfidenceLower { get; init; }
        [Key(7)] public double ConfidenceUpper { get; init; }
        [Key(8)] public int Group1Count { get; init; }
        [Key(9)] public int Group0Count { get; init; }
        [Key(10)] public double Group1Mean { get; init; }
        [Key(11)] public double Group0Mean { get; init; }
        [Key(12)] public double PooledStandardDeviation { get; init; }
        [Key(13)] public int MissingPairsDropped { get; init; }
        [Key(14)] public string Interpretation { get; init; } = string.Empty;
    }

    [MessagePackObject]
    public sealed record CorrelationPair
    {
        [Key(0)] public string VariableA { get; init; } = string.Empty;
        [Key(1)] public string VariableB { get; init; } = string.Empty;
        [Key(2)] public double Coefficient { get; init; }
        [Key(3)] public double PValue { get; init; }
        [Key(4)] public double AdjustedPValue { get; init; }
        [Key(5)] public int PairCount { get; init; }
    }

    [MessagePackObject]
    public sealed record CorrelationMatrixResult
    {
        [Key(0)] public string Method { get; init; } = string.Empty;
        [Key(1)] public string[] Variables { get; init; } = Array.Empty<string>();
        [Key(2)] public double[][] Coefficients { get; init; } = Array.Empty<double[]>();
        [Key(3)] public double[][] PValues { get; init; } = Array.Empty<double[]>();
        [Key(4)] public int[][] PairCounts { get; init; } = Array.Empty<int[]>();
        [Key(5)] public int RowsUsed { get; init; }
        [Key(6)] public int RowsDropped { get; init; }
        [Key(7)] public CorrelationPair[] StrongestPairs { get; init; } = Array.Empty<CorrelationPair>();
        [Key(8)] public int TotalPairs { get; init; }
    }

    [MessagePackObject]
    public sealed record CovarianceMatrixResult
    {
        [Key(0)] public string[] Variables { get; init; } = Array.Empty<string>();
        [Key(1)] public double[][] Covariance { get; init; } = Array.Empty<double[]>();
        [Key(2)] public double[] Means { get; init; } = Array.Empty<double>();
        [Key(3)] public double[] Variances { get; init; } = Array.Empty<double>();
        [Key(4)] public double[] StandardDeviations { get; init; } = Array.Empty<double>();
        [Key(5)] public double TotalVariance { get; init; }
        [Key(6)] public double GeneralisedVariance { get; init; }
        [Key(7)] public int RowsUsed { get; init; }
        [Key(8)] public int RowsDropped { get; init; }
        [Key(9)] public bool Unbiased { get; init; }
    }

    [MessagePackObject]
    public sealed record PartialCorrelationResult
    {
        [Key(0)] public string Method { get; init; } = string.Empty;
        [Key(1)] public double Coefficient { get; init; }
        [Key(2)] public double ZeroOrderCorrelation { get; init; }
        [Key(3)] public int PairCount { get; init; }
        [Key(4)] public int ControlCount { get; init; }
        [Key(5)] public double DegreesOfFreedom { get; init; }
        [Key(6)] public double TStatistic { get; init; }
        [Key(7)] public double PValue { get; init; }
        [Key(8)] public double ConfidenceLevel { get; init; }
        [Key(9)] public double ConfidenceLower { get; init; }
        [Key(10)] public double ConfidenceUpper { get; init; }
        [Key(11)] public int RowsDropped { get; init; }
        [Key(12)] public string Interpretation { get; init; } = string.Empty;
    }

    [MessagePackObject]
    public sealed record PartialCorrelationMatrixResult
    {
        [Key(0)] public string[] Variables { get; init; } = Array.Empty<string>();
        [Key(1)] public double[][] Coefficients { get; init; } = Array.Empty<double[]>();
        [Key(2)] public double[][] PValues { get; init; } = Array.Empty<double[]>();
        [Key(3)] public double DegreesOfFreedom { get; init; }
        [Key(4)] public int RowsUsed { get; init; }
        [Key(5)] public int RowsDropped { get; init; }
    }

    [MessagePackObject]
    public sealed record MutualInformationResult
    {
        [Key(0)] public double MutualInformationNats { get; init; }
        [Key(1)] public double MutualInformationBits { get; init; }
        [Key(2)] public double NormalisedMutualInformation { get; init; }
        [Key(3)] public double EntropyXBits { get; init; }
        [Key(4)] public double EntropyYBits { get; init; }
        [Key(5)] public double JointEntropyBits { get; init; }
        [Key(6)] public int BinsX { get; init; }
        [Key(7)] public int BinsY { get; init; }
        [Key(8)] public int PairCount { get; init; }
        [Key(9)] public int MissingPairsDropped { get; init; }
        [Key(10)] public double GStatistic { get; init; }
        [Key(11)] public double DegreesOfFreedom { get; init; }
        [Key(12)] public double PValue { get; init; }
        [Key(13)] public bool BiasCorrected { get; init; }
    }

    [MessagePackObject]
    public sealed record PredictorInformationScore
    {
        [Key(0)] public string Variable { get; init; } = string.Empty;
        [Key(1)] public double MutualInformationBits { get; init; }
        [Key(2)] public double NormalisedMutualInformation { get; init; }
        [Key(3)] public double PValue { get; init; }
        [Key(4)] public double AdjustedPValue { get; init; }
        [Key(5)] public int PairCount { get; init; }
        [Key(6)] public int Rank { get; init; }
    }

    [MessagePackObject]
    public sealed record PredictorCorrelationScore
    {
        [Key(0)] public string Variable { get; init; } = string.Empty;
        [Key(1)] public double Coefficient { get; init; }
        [Key(2)] public double PValue { get; init; }
        [Key(3)] public double AdjustedPValue { get; init; }
        [Key(4)] public int PairCount { get; init; }
        [Key(5)] public int Rank { get; init; }
    }

    [MessagePackObject]
    public sealed record CorrelationRatioResult
    {
        [Key(0)] public double Eta { get; init; }
        [Key(1)] public double EtaSquared { get; init; }
        [Key(2)] public double AdjustedEtaSquared { get; init; }
        [Key(3)] public double FStatistic { get; init; }
        [Key(4)] public double DegreesOfFreedomBetween { get; init; }
        [Key(5)] public double DegreesOfFreedomWithin { get; init; }
        [Key(6)] public double PValue { get; init; }
        [Key(7)] public int GroupCount { get; init; }
        [Key(8)] public int PairCount { get; init; }
        [Key(9)] public int MissingPairsDropped { get; init; }
        [Key(10)] public double[] GroupMeans { get; init; } = Array.Empty<double>();
        [Key(11)] public int[] GroupSizes { get; init; } = Array.Empty<int>();
        [Key(12)] public int[] GroupLabels { get; init; } = Array.Empty<int>();
    }

    [MessagePackObject]
    public sealed record CorrelationComparisonResult
    {
        [Key(0)] public double Correlation1 { get; init; }
        [Key(1)] public double Correlation2 { get; init; }
        [Key(2)] public double FisherZ1 { get; init; }
        [Key(3)] public double FisherZ2 { get; init; }
        [Key(4)] public double ZStatistic { get; init; }
        [Key(5)] public double PValue { get; init; }
        [Key(6)] public double DifferenceInFisherZ { get; init; }
        [Key(7)] public double ConfidenceLower { get; init; }
        [Key(8)] public double ConfidenceUpper { get; init; }
        [Key(9)] public double ConfidenceLevel { get; init; }
    }

    [MessagePackObject]
    public sealed record CorrelationSummaryIntervalResult
    {
        [Key(0)] public double Correlation { get; init; }
        [Key(1)] public int SampleSize { get; init; }
        [Key(2)] public double TStatistic { get; init; }
        [Key(3)] public double DegreesOfFreedom { get; init; }
        [Key(4)] public double PValue { get; init; }
        [Key(5)] public double FisherZ { get; init; }
        [Key(6)] public double StandardErrorOfZ { get; init; }
        [Key(7)] public double ConfidenceLevel { get; init; }
        [Key(8)] public double ConfidenceLower { get; init; }
        [Key(9)] public double ConfidenceUpper { get; init; }
        [Key(10)] public double SmallestSignificantCorrelation { get; init; }
    }
}