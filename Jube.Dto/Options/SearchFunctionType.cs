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

namespace Jube.Dto.Options
{
    using System.ComponentModel;

    public enum SearchFunctionType
    {
        [Description("Count")] Count = 1,
        [Description("Distinct count")] DistinctCount = 2,
        [Description("Sum")] Sum = 3,
        [Description("Average")] Average = 4,
        [Description("Median")] Median = 5,
        [Description("Kurtosis")] Kurtosis = 6,
        [Description("Skew")] Skew = 7,
        [Description("Standard deviation")] StandardDeviation = 8,
        [Description("Mode")] Mode = 11,
        [Description("Same count")] SameCount = 12,
        [Description("Actual value")] ActualValue = 13,
        [Description("Max")] Max = 14,
        [Description("Min")] Min = 15,
        [Description("Since")] Since = 16,
        [Description("Geometric mean")] GeometricMean = 17,
        [Description("Harmonic mean")] HarmonicMean = 18,
        [Description("Trimmed mean")] TrimmedMean = 19,
        [Description("Winsorised mean")] WinsorisedMean = 20,
        [Description("Exponentially weighted mean")] ExponentialWeightedMean = 21,
        [Description("Root mean square")] RootMeanSquare = 22,
        [Description("Variance")] Variance = 23,
        [Description("Range")] Range = 24,
        [Description("Interquartile range")] InterquartileRange = 25,
        [Description("Percentile")] Percentile = 26,
        [Description("Median absolute deviation")] MedianAbsoluteDeviation = 27,
        [Description("Mean absolute deviation")] MeanAbsoluteDeviation = 28,
        [Description("Coefficient of variation")] CoefficientOfVariation = 29,
        [Description("Entropy")] Entropy = 30,
        [Description("Gini coefficient")] GiniCoefficient = 31,
        [Description("Nth value")] NthValue = 32,
        [Description("First to last change")] FirstToLastChange = 33,
        [Description("Largest step")] LargestStep = 34,
        [Description("Count above")] CountAbove = 35,
        [Description("Count below")] CountBelow = 36,
        [Description("Share above")] ShareAbove = 37,
        [Description("Sum above")] SumAbove = 38,
        [Description("Longest run above")] LongestRunAbove = 39,
        [Description("Last value z-score")] LastValueZScore = 40,
        [Description("Last value modified z-score")] LastValueModifiedZScore = 41,
        [Description("Last value percentile rank")] LastValuePercentileRank = 42,
        [Description("Last value to mean ratio")] LastValueToMeanRatio = 43,
        [Description("Trend slope")] TrendSlope = 44,
        [Description("Autocorrelation")] Autocorrelation = 45,
        [Description("Cursor result")] CursorResult = 46
    }
}
