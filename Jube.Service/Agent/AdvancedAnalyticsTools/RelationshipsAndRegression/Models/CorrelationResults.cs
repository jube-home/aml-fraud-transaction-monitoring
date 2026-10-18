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

    public sealed record CorrelationCoefficientResult
    {
        public string Method { get; init; } = string.Empty;
        public double Coefficient { get; init; }
        public double CoefficientOfDetermination { get; init; }
        public int PairCount { get; init; }
        public int MissingPairsDropped { get; init; }
        public string StatisticName { get; init; } = string.Empty;
        public double Statistic { get; init; }
        public double DegreesOfFreedom { get; init; }
        public double PValue { get; init; }
        public double ConfidenceLevel { get; init; }
        public double ConfidenceLower { get; init; }
        public double ConfidenceUpper { get; init; }
        public string Interpretation { get; init; } = string.Empty;
    }

    public sealed record KendallTauResult
    {
        public string Variant { get; init; } = string.Empty;
        public double Tau { get; init; }
        public double TauA { get; init; }
        public double TauB { get; init; }
        public double TauC { get; init; }
        public long ConcordantPairs { get; init; }
        public long DiscordantPairs { get; init; }
        public long PairsTiedInXOnly { get; init; }
        public long PairsTiedInYOnly { get; init; }
        public long PairsTiedInBoth { get; init; }
        public int PairCount { get; init; }
        public int MissingPairsDropped { get; init; }
        public double ZStatistic { get; init; }
        public double PValue { get; init; }
        public double ConfidenceLevel { get; init; }
        public double ConfidenceLower { get; init; }
        public double ConfidenceUpper { get; init; }
        public string Interpretation { get; init; } = string.Empty;
    }

    public sealed record PointBiserialResult
    {
        public string Method { get; init; } = string.Empty;
        public double Coefficient { get; init; }
        public double TStatistic { get; init; }
        public double DegreesOfFreedom { get; init; }
        public double PValue { get; init; }
        public double ConfidenceLevel { get; init; }
        public double ConfidenceLower { get; init; }
        public double ConfidenceUpper { get; init; }
        public int Group1Count { get; init; }
        public int Group0Count { get; init; }
        public double Group1Mean { get; init; }
        public double Group0Mean { get; init; }
        public double PooledStandardDeviation { get; init; }
        public int MissingPairsDropped { get; init; }
        public string Interpretation { get; init; } = string.Empty;
    }

    public sealed record CorrelationPair
    {
        public string VariableA { get; init; } = string.Empty;
        public string VariableB { get; init; } = string.Empty;
        public double Coefficient { get; init; }
        public double PValue { get; init; }
        public double AdjustedPValue { get; init; }
        public int PairCount { get; init; }
    }

    public sealed record CorrelationMatrixResult
    {
        public string Method { get; init; } = string.Empty;
        public string[] Variables { get; init; } = Array.Empty<string>();
        public double[][] Coefficients { get; init; } = Array.Empty<double[]>();
        public double[][] PValues { get; init; } = Array.Empty<double[]>();
        public int[][] PairCounts { get; init; } = Array.Empty<int[]>();
        public int RowsUsed { get; init; }
        public int RowsDropped { get; init; }
        public CorrelationPair[] StrongestPairs { get; init; } = Array.Empty<CorrelationPair>();
        public int TotalPairs { get; init; }
    }

    public sealed record CovarianceMatrixResult
    {
        public string[] Variables { get; init; } = Array.Empty<string>();
        public double[][] Covariance { get; init; } = Array.Empty<double[]>();
        public double[] Means { get; init; } = Array.Empty<double>();
        public double[] Variances { get; init; } = Array.Empty<double>();
        public double[] StandardDeviations { get; init; } = Array.Empty<double>();
        public double TotalVariance { get; init; }
        public double GeneralisedVariance { get; init; }
        public int RowsUsed { get; init; }
        public int RowsDropped { get; init; }
        public bool Unbiased { get; init; }
    }

    public sealed record PartialCorrelationResult
    {
        public string Method { get; init; } = string.Empty;
        public double Coefficient { get; init; }
        public double ZeroOrderCorrelation { get; init; }
        public int PairCount { get; init; }
        public int ControlCount { get; init; }
        public double DegreesOfFreedom { get; init; }
        public double TStatistic { get; init; }
        public double PValue { get; init; }
        public double ConfidenceLevel { get; init; }
        public double ConfidenceLower { get; init; }
        public double ConfidenceUpper { get; init; }
        public int RowsDropped { get; init; }
        public string Interpretation { get; init; } = string.Empty;
    }

    public sealed record PartialCorrelationMatrixResult
    {
        public string[] Variables { get; init; } = Array.Empty<string>();
        public double[][] Coefficients { get; init; } = Array.Empty<double[]>();
        public double[][] PValues { get; init; } = Array.Empty<double[]>();
        public double DegreesOfFreedom { get; init; }
        public int RowsUsed { get; init; }
        public int RowsDropped { get; init; }
    }

    public sealed record MutualInformationResult
    {
        public double MutualInformationNats { get; init; }
        public double MutualInformationBits { get; init; }
        public double NormalisedMutualInformation { get; init; }
        public double EntropyXBits { get; init; }
        public double EntropyYBits { get; init; }
        public double JointEntropyBits { get; init; }
        public int BinsX { get; init; }
        public int BinsY { get; init; }
        public int PairCount { get; init; }
        public int MissingPairsDropped { get; init; }
        public double GStatistic { get; init; }
        public double DegreesOfFreedom { get; init; }
        public double PValue { get; init; }
        public bool BiasCorrected { get; init; }
    }

    public sealed record PredictorInformationScore
    {
        public string Variable { get; init; } = string.Empty;
        public double MutualInformationBits { get; init; }
        public double NormalisedMutualInformation { get; init; }
        public double PValue { get; init; }
        public double AdjustedPValue { get; init; }
        public int PairCount { get; init; }
        public int Rank { get; init; }
    }

    public sealed record PredictorCorrelationScore
    {
        public string Variable { get; init; } = string.Empty;
        public double Coefficient { get; init; }
        public double PValue { get; init; }
        public double AdjustedPValue { get; init; }
        public int PairCount { get; init; }
        public int Rank { get; init; }
    }

    public sealed record CorrelationRatioResult
    {
        public double Eta { get; init; }
        public double EtaSquared { get; init; }
        public double AdjustedEtaSquared { get; init; }
        public double FStatistic { get; init; }
        public double DegreesOfFreedomBetween { get; init; }
        public double DegreesOfFreedomWithin { get; init; }
        public double PValue { get; init; }
        public int GroupCount { get; init; }
        public int PairCount { get; init; }
        public int MissingPairsDropped { get; init; }
        public double[] GroupMeans { get; init; } = Array.Empty<double>();
        public int[] GroupSizes { get; init; } = Array.Empty<int>();
        public int[] GroupLabels { get; init; } = Array.Empty<int>();
    }

    public sealed record CorrelationComparisonResult
    {
        public double Correlation1 { get; init; }
        public double Correlation2 { get; init; }
        public double FisherZ1 { get; init; }
        public double FisherZ2 { get; init; }
        public double ZStatistic { get; init; }
        public double PValue { get; init; }
        public double DifferenceInFisherZ { get; init; }
        public double ConfidenceLower { get; init; }
        public double ConfidenceUpper { get; init; }
        public double ConfidenceLevel { get; init; }
    }

    public sealed record CorrelationSummaryIntervalResult
    {
        public double Correlation { get; init; }
        public int SampleSize { get; init; }
        public double TStatistic { get; init; }
        public double DegreesOfFreedom { get; init; }
        public double PValue { get; init; }
        public double FisherZ { get; init; }
        public double StandardErrorOfZ { get; init; }
        public double ConfidenceLevel { get; init; }
        public double ConfidenceLower { get; init; }
        public double ConfidenceUpper { get; init; }
        public double SmallestSignificantCorrelation { get; init; }
    }
}