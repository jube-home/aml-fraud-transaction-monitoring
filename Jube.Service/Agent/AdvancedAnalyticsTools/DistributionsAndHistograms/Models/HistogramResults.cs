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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models
{
    public sealed record HistogramResult(
        int SampleSize,
        int BinCount,
        string BinningMethod,
        string Scale,
        bool Cumulative,
        double[] Edges,
        double[] Centers,
        double[] Widths,
        int[] Counts,
        double[] RelativeFrequencies,
        double[] Densities,
        double[] Values,
        int OutsideCount,
        double Minimum,
        double Maximum,
        string[] Notes);

    public sealed record BinSuggestion(string Rule, int BinCount, double BinWidth);

    public sealed record BinSuggestionResult(
        int SampleSize,
        double Minimum,
        double Maximum,
        BinSuggestion[] Suggestions,
        string[] Notes);

    public sealed record Histogram2DResult(
        int SampleSize,
        double[] XEdges,
        double[] YEdges,
        int[][] Counts,
        double[][] RelativeFrequencies,
        int[] XMarginalCounts,
        int[] YMarginalCounts,
        int OutsideCount);

    public sealed record EmpiricalCdfResult(
        int SampleSize,
        bool UpperTail,
        double[] Points,
        double[] Probabilities);

    public sealed record EmpiricalCdfStepsResult(
        int SampleSize,
        double[] DistinctValues,
        int[] Counts,
        int[] CumulativeCounts,
        double[] CumulativeProbabilities,
        double[] SurvivalProbabilities);

    public sealed record EmpiricalQuantileResult(
        int SampleSize,
        string Definition,
        double[] Probabilities,
        double[] Quantiles);

    public sealed record KernelDensityResult(
        int SampleSize,
        string Kernel,
        string BandwidthRule,
        double Bandwidth,
        double[] Points,
        double[] Density);

    public sealed record KernelDensityCurveResult(
        int SampleSize,
        string Kernel,
        string BandwidthRule,
        double Bandwidth,
        double[] X,
        double[] Density,
        double[] PeakLocations,
        double[] PeakDensities,
        int ModeCount,
        string[] Notes);

    public sealed record BandwidthEstimate(string Rule, double Bandwidth);

    public sealed record KernelBandwidthResult(
        int SampleSize,
        double StandardDeviation,
        double InterquartileRange,
        BandwidthEstimate[] Estimates,
        string[] Notes);
}