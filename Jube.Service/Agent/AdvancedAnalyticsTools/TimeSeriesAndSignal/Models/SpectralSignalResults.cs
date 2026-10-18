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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models
{
    public sealed record FourierResult(
        double[] Real,
        double[] Imaginary,
        double[] Frequencies,
        double[] Magnitudes,
        double[] Amplitudes,
        double[] Phases,
        double SamplingIntervalSeconds,
        int Samples);

    public sealed record InverseFourierResult(
        double[] Real,
        double[] Imaginary);

    public sealed record PowerSpectrumResult(
        double[] Frequencies,
        double[] Power,
        double[] Periods,
        double PeakFrequency,
        double PeakPeriod,
        double PeakPower,
        double TotalPowerExcludingDc,
        double FrequencyResolution,
        int Samples,
        int Segments,
        string Window);

    public sealed record DominantPeriodsResult(
        double[] Periods,
        double[] PeriodsInSamples,
        double[] Frequencies,
        double[] Powers,
        double[] PowerShares,
        double FrequencyResolution,
        int Samples);

    public sealed record SpectralSummaryResult(
        double TotalPowerExcludingDc,
        double SpectralEntropyBits,
        double NormalisedSpectralEntropy,
        double SpectralFlatness,
        double CentroidFrequency,
        double RolloffFrequency,
        double DominantFrequency,
        double DominantPeriod,
        double DominantPowerShare);

    public sealed record FisherPeriodicityResult(
        double GStatistic,
        double PValue,
        int TestedFrequencies,
        int PeakBin,
        double PeakPeriodInSamples,
        bool Significant);

    public sealed record TemplateMatchResult(
        double[] Scores,
        int[] BestOffsets,
        double[] BestScores,
        int TemplateLength);

    public sealed record HilbertResult(
        double[] Envelope,
        double[] Phase,
        double[] InstantaneousFrequency,
        double[] QuadratureComponent);

    public sealed record HaarDecompositionResult(
        double[] Approximation,
        double[][] Details,
        double[] DetailEnergyShares,
        int Levels);

    public sealed record HaarDenoiseResult(
        double[] Denoised,
        double Threshold,
        double NoiseStandardDeviation,
        int ZeroedCoefficients,
        int DetailCoefficients);

    public sealed record FrequencyFilterResult(
        double[] Filtered,
        double[] Removed,
        int KeptFrequencyBins,
        int TotalFrequencyBins);

    public sealed record ZeroCrossingResult(
        int Count,
        double[] Positions,
        double RatePerSample,
        double MeanCyclePeriodInSamples);

    public sealed record PeakDetectionResult(
        int[] Indices,
        double[] Values,
        double[] Prominences,
        double[] LeftBaseValues,
        double[] RightBaseValues);
}