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

namespace Jube.Charting.Performance.Models
{
    public sealed record GainsResult(
        double[] Population,
        double[] Captured,
        double[] Thresholds,
        double Positives,
        double Negatives);

    public sealed record DecileRow(
        int Decile,
        double Population,
        double Positives,
        double DecileLift,
        double CumulativeLift,
        double CumulativeCaptured);

    public sealed record KsResult(
        double[] Scores,
        double[] PositiveCdf,
        double[] NegativeCdf,
        double Statistic,
        double AtScore,
        double PositiveAtMax,
        double NegativeAtMax);

    public sealed record SweepPoint(
        double Threshold,
        double Precision,
        double Recall,
        double F1,
        double Mcc,
        double Specificity);

    public sealed record SweepResult(SweepPoint[] Points, int BestF1Index);

    public sealed record DistributionResult(
        double[] Edges,
        double[] PositiveCounts,
        double[] NegativeCounts,
        double Positives,
        double Negatives);
}