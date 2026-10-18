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
    public sealed record CusumResult(
        double[] UpperSums,
        double[] LowerSums,
        int[] AlarmIndices,
        string[] AlarmDirections,
        int[] EstimatedChangeStarts,
        double Target,
        double StandardDeviation,
        double ReferenceValue,
        double DecisionInterval);

    public sealed record PageHinkleyResult(
        double[] IncreaseStatistic,
        double[] DecreaseStatistic,
        int[] AlarmIndices,
        string[] AlarmDirections,
        double Delta,
        double Lambda);

    public sealed record EwmaChartResult(
        double[] Ewma,
        double[] UpperLimits,
        double[] LowerLimits,
        int[] AlarmIndices,
        double Target,
        double StandardDeviation,
        double Lambda,
        double LimitWidth);

    public sealed record ShewhartChartResult(
        double[] ZScores,
        int[] BeyondThreeSigma,
        int[] TwoOfThreeBeyondTwoSigma,
        int[] FourOfFiveBeyondOneSigma,
        int[] EightInARowOneSide,
        int[] AlarmIndices,
        double Target,
        double StandardDeviation);

    public sealed record SpikeDetectionResult(
        double[] Scores,
        int[] SpikeIndices,
        double[] SpikeValues,
        string Method,
        double Threshold);

    public sealed record LevelShiftResult(
        double[] Scores,
        int[] ShiftIndices,
        double[] ShiftSizes,
        double[] ShiftScores,
        int Window);

    public sealed record MeanShiftSegment(
        int StartIndex,
        int EndIndex,
        int Count,
        double Mean);

    public sealed record SeriesMeanShiftResult(
        int[] ChangePointIndices,
        double[] Gains,
        MeanShiftSegment[] Segments,
        double NoiseStandardDeviation,
        double PenaltyPerChangePoint);

    public sealed record DrawdownResult(
        double[] Drawdowns,
        double MaximumDrawdown,
        double MaximumDrawdownFraction,
        int PeakIndex,
        int TroughIndex,
        int RecoveryIndex,
        int LongestUnderwaterSteps,
        int CurrentUnderwaterSteps);
}