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

namespace Jube.Dictionary.Extensions
{
    internal static partial class FlowPredicates
    {
        internal static bool DoubleGreater(double v, double other)
        {
            return v > other;
        }

        internal static bool DoubleGreaterOrEqual(double v, double other)
        {
            return v >= other;
        }

        internal static bool DoubleLess(double v, double other)
        {
            return v < other;
        }

        internal static bool DoubleLessOrEqual(double v, double other)
        {
            return v <= other;
        }

        internal static bool DoubleEqual(double v, double other)
        {
            return v == other;
        }

        internal static bool DoubleNotEqual(double v, double other)
        {
            return !double.IsNaN(v) && !double.IsNaN(other) && v != other;
        }

        internal static bool DoubleBetween(double v, double minimum, double maximum)
        {
            return v.Between(minimum, maximum);
        }

        internal static bool DoubleInRange(double v, double minimum, double maximum)
        {
            return v.InRange(minimum, maximum);
        }

        internal static bool DoubleOutsideRange(double v, double minimum, double maximum)
        {
            return !double.IsNaN(v) && (v < minimum || v > maximum);
        }

        internal static bool DoubleIn(double v, double[] values)
        {
            return v.In(values);
        }

        internal static bool DoubleNotIn(double v, double[] values)
        {
            return !v.In(values);
        }

        internal static bool DoubleIsZero(double v)
        {
            return v == 0;
        }

        internal static bool DoubleIsPositive(double v)
        {
            return v > 0;
        }

        internal static bool DoubleIsNegative(double v)
        {
            return v < 0;
        }

        internal static bool DoubleIsNaN(double v)
        {
            return double.IsNaN(v);
        }

        internal static bool DoubleIsFinite(double v)
        {
            return double.IsFinite(v);
        }

        internal static bool DoubleIsWholeNumber(double v)
        {
            return double.IsFinite(v) && v == Math.Floor(v);
        }

        internal static bool DoubleIsMultipleOf(double v, double divisor)
        {
            return divisor != 0 && double.IsFinite(v) && double.IsFinite(divisor) && v % divisor == 0;
        }

        internal static bool DoubleWithinPercentOf(double v, double target, double percent)
        {
            return v.IsWithinPercentOf(target, percent);
        }

        internal static bool DoubleDeviatesFromByPercent(double v, double target, double percent)
        {
            return !double.IsNaN(v) && !double.IsNaN(target) && !v.IsWithinPercentOf(target, percent);
        }

        internal static bool DoubleIncreasedByMoreThanPercent(double v, double previous, double percent)
        {
            return v.PercentageChangeFrom(previous) > percent;
        }

        internal static bool DoubleDecreasedByMoreThanPercent(double v, double previous, double percent)
        {
            return v.PercentageChangeFrom(previous) < -percent;
        }

        internal static bool DoubleZScoreAbove(double v, double mean, double standardDeviation, double threshold)
        {
            return v.ZScore(mean, standardDeviation) > threshold;
        }

        internal static bool DoubleZScoreBelow(double v, double mean, double standardDeviation, double threshold)
        {
            return v.ZScore(mean, standardDeviation) < threshold;
        }

        internal static bool DoubleZScoreOutside(double v, double mean, double standardDeviation, double threshold)
        {
            return Math.Abs(v.ZScore(mean, standardDeviation)) > threshold;
        }

        internal static bool DoubleRatioAbove(double v, double denominator, double threshold)
        {
            return denominator != 0 && v / denominator > threshold;
        }

        internal static bool DoubleRatioBelow(double v, double denominator, double threshold)
        {
            return denominator != 0 && v / denominator < threshold;
        }

        internal static bool DoubleIsJustBelowThreshold(double v, double threshold, double marginPercent)
        {
            return v.IsJustBelowThreshold(threshold, marginPercent);
        }

        internal static bool DoubleIsJustAboveThreshold(double v, double threshold, double marginPercent)
        {
            return v > threshold && v <= threshold + threshold * (marginPercent / 100.0);
        }

        internal static bool DoubleIsRoundAmount(double v, double nearest)
        {
            return v.IsRoundAmount(nearest);
        }

        internal static bool DoubleWithinRadiusKm(double v, double longitude1, double latitude2, double longitude2, double radiusKm)
        {
            return v.IsWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm);
        }

        internal static bool DoubleOutsideRadiusKm(double v, double longitude1, double latitude2, double longitude2, double radiusKm)
        {
            return v.HaversineDistanceKilometers(longitude1, latitude2, longitude2) > radiusKm;
        }

        internal static bool DoubleDistanceKmAbove(double v, double longitude1, double latitude2, double longitude2, double kilometers)
        {
            return v.HaversineDistanceKilometers(longitude1, latitude2, longitude2) > kilometers;
        }

        internal static bool DoubleDistanceKmBelow(double v, double longitude1, double latitude2, double longitude2, double kilometers)
        {
            return v.HaversineDistanceKilometers(longitude1, latitude2, longitude2) < kilometers;
        }

        internal static bool DoubleImpliedSpeedAbove(double v, double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
        {
            return v.ImpliedTravelSpeedKmh(longitude1, latitude2, longitude2, hoursElapsed) > kilometersPerHour;
        }

        internal static bool DoubleImpliedSpeedBelow(double v, double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
        {
            return v.ImpliedTravelSpeedKmh(longitude1, latitude2, longitude2, hoursElapsed) < kilometersPerHour;
        }

        internal static bool DoubleHasValue(double v)
        {
            return !double.IsNaN(v);
        }

        internal static bool DoubleHasNoValue(double v)
        {
            return double.IsNaN(v);
        }

        internal static bool DoubleRatioInRange(double v, double denominator, double minimum, double maximum)
        {
            return denominator != 0 && v / denominator >= minimum && v / denominator <= maximum;
        }

        internal static bool DoubleRatioOutsideRange(double v, double denominator, double minimum, double maximum)
        {
            return denominator != 0 && (v / denominator < minimum || v / denominator > maximum);
        }

        internal static bool DoubleIsJustBelowAnyThreshold(double v, double marginPercent, double[] thresholds)
        {
            return thresholds.Any(x => v.IsJustBelowThreshold(x, marginPercent));
        }

        internal static bool DoubleIsJustAboveAnyThreshold(double v, double marginPercent, double[] thresholds)
        {
            return thresholds.Any(x => v > x && v <= x + x * (marginPercent / 100.0));
        }

        internal static bool DoubleWithinRadiusMiles(double v, double longitude1, double latitude2, double longitude2, double radiusMiles)
        {
            return v.HaversineDistanceMiles(longitude1, latitude2, longitude2) <= radiusMiles;
        }

        internal static bool DoubleOutsideRadiusMiles(double v, double longitude1, double latitude2, double longitude2, double radiusMiles)
        {
            return v.HaversineDistanceMiles(longitude1, latitude2, longitude2) > radiusMiles;
        }

        internal static bool DoubleDistanceMilesAbove(double v, double longitude1, double latitude2, double longitude2, double miles)
        {
            return v.HaversineDistanceMiles(longitude1, latitude2, longitude2) > miles;
        }

        internal static bool DoubleDistanceMilesBelow(double v, double longitude1, double latitude2, double longitude2, double miles)
        {
            return v.HaversineDistanceMiles(longitude1, latitude2, longitude2) < miles;
        }

        internal static bool DoubleImpliedSpeedMphAbove(double v, double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
        {
            return FlowPredicates.ImpliedSpeedMph(v, longitude1, latitude2, longitude2, hoursElapsed) > milesPerHour;
        }

        internal static bool DoubleImpliedSpeedMphBelow(double v, double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
        {
            return FlowPredicates.ImpliedSpeedMph(v, longitude1, latitude2, longitude2, hoursElapsed) < milesPerHour;
        }

        internal static bool DoubleRatioOfAbove(double v, double denominator, double bound)
        {
            return CalculationSupport.Above(v.RatioOf(denominator), bound);
        }

        internal static bool DoubleRatioOfBelow(double v, double denominator, double bound)
        {
            return CalculationSupport.Below(v.RatioOf(denominator), bound);
        }

        internal static bool DoubleRatioOfInRange(double v, double denominator, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.RatioOf(denominator), lowerBound, upperBound);
        }

        internal static bool DoubleRatioOfOutsideRange(double v, double denominator, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.RatioOf(denominator), lowerBound, upperBound);
        }

        internal static bool DoublePercentOfAbove(double v, double total, double bound)
        {
            return CalculationSupport.Above(v.PercentOf(total), bound);
        }

        internal static bool DoublePercentOfBelow(double v, double total, double bound)
        {
            return CalculationSupport.Below(v.PercentOf(total), bound);
        }

        internal static bool DoublePercentOfInRange(double v, double total, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.PercentOf(total), lowerBound, upperBound);
        }

        internal static bool DoublePercentOfOutsideRange(double v, double total, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.PercentOf(total), lowerBound, upperBound);
        }

        internal static bool DoubleShareOfSumWithAbove(double v, double other, double bound)
        {
            return CalculationSupport.Above(v.ShareOfSumWith(other), bound);
        }

        internal static bool DoubleShareOfSumWithBelow(double v, double other, double bound)
        {
            return CalculationSupport.Below(v.ShareOfSumWith(other), bound);
        }

        internal static bool DoubleShareOfSumWithInRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.ShareOfSumWith(other), lowerBound, upperBound);
        }

        internal static bool DoubleShareOfSumWithOutsideRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.ShareOfSumWith(other), lowerBound, upperBound);
        }

        internal static bool DoubleComplementAbove(double v, double bound)
        {
            return CalculationSupport.Above(v.Complement(), bound);
        }

        internal static bool DoubleComplementBelow(double v, double bound)
        {
            return CalculationSupport.Below(v.Complement(), bound);
        }

        internal static bool DoubleComplementInRange(double v, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.Complement(), lowerBound, upperBound);
        }

        internal static bool DoubleComplementOutsideRange(double v, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.Complement(), lowerBound, upperBound);
        }

        internal static bool DoubleLogRatioOfAbove(double v, double denominator, double bound)
        {
            return CalculationSupport.Above(v.LogRatioOf(denominator), bound);
        }

        internal static bool DoubleLogRatioOfBelow(double v, double denominator, double bound)
        {
            return CalculationSupport.Below(v.LogRatioOf(denominator), bound);
        }

        internal static bool DoubleLogRatioOfInRange(double v, double denominator, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.LogRatioOf(denominator), lowerBound, upperBound);
        }

        internal static bool DoubleLogRatioOfOutsideRange(double v, double denominator, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.LogRatioOf(denominator), lowerBound, upperBound);
        }

        internal static bool DoubleRatioOfRatiosAbove(double v, double denominator, double otherNumerator, double otherDenominator, double bound)
        {
            return CalculationSupport.Above(v.RatioOfRatios(denominator, otherNumerator, otherDenominator), bound);
        }

        internal static bool DoubleRatioOfRatiosBelow(double v, double denominator, double otherNumerator, double otherDenominator, double bound)
        {
            return CalculationSupport.Below(v.RatioOfRatios(denominator, otherNumerator, otherDenominator), bound);
        }

        internal static bool DoubleRatioOfRatiosInRange(double v, double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.RatioOfRatios(denominator, otherNumerator, otherDenominator), lowerBound, upperBound);
        }

        internal static bool DoubleRatioOfRatiosOutsideRange(double v, double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.RatioOfRatios(denominator, otherNumerator, otherDenominator), lowerBound, upperBound);
        }

        internal static bool DoubleInverseRatioOfAbove(double v, double other, double bound)
        {
            return CalculationSupport.Above(v.InverseRatioOf(other), bound);
        }

        internal static bool DoubleInverseRatioOfBelow(double v, double other, double bound)
        {
            return CalculationSupport.Below(v.InverseRatioOf(other), bound);
        }

        internal static bool DoubleInverseRatioOfInRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.InverseRatioOf(other), lowerBound, upperBound);
        }

        internal static bool DoubleInverseRatioOfOutsideRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.InverseRatioOf(other), lowerBound, upperBound);
        }

        internal static bool DoubleDifferenceFromAbove(double v, double other, double bound)
        {
            return CalculationSupport.Above(v.DifferenceFrom(other), bound);
        }

        internal static bool DoubleDifferenceFromBelow(double v, double other, double bound)
        {
            return CalculationSupport.Below(v.DifferenceFrom(other), bound);
        }

        internal static bool DoubleDifferenceFromInRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.DifferenceFrom(other), lowerBound, upperBound);
        }

        internal static bool DoubleDifferenceFromOutsideRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.DifferenceFrom(other), lowerBound, upperBound);
        }

        internal static bool DoubleAbsoluteDifferenceFromAbove(double v, double other, double bound)
        {
            return CalculationSupport.Above(v.AbsoluteDifferenceFrom(other), bound);
        }

        internal static bool DoubleAbsoluteDifferenceFromBelow(double v, double other, double bound)
        {
            return CalculationSupport.Below(v.AbsoluteDifferenceFrom(other), bound);
        }

        internal static bool DoubleAbsoluteDifferenceFromInRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.AbsoluteDifferenceFrom(other), lowerBound, upperBound);
        }

        internal static bool DoubleAbsoluteDifferenceFromOutsideRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.AbsoluteDifferenceFrom(other), lowerBound, upperBound);
        }

        internal static bool DoublePercentDifferenceFromAbove(double v, double other, double bound)
        {
            return CalculationSupport.Above(v.PercentDifferenceFrom(other), bound);
        }

        internal static bool DoublePercentDifferenceFromBelow(double v, double other, double bound)
        {
            return CalculationSupport.Below(v.PercentDifferenceFrom(other), bound);
        }

        internal static bool DoublePercentDifferenceFromInRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.PercentDifferenceFrom(other), lowerBound, upperBound);
        }

        internal static bool DoublePercentDifferenceFromOutsideRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.PercentDifferenceFrom(other), lowerBound, upperBound);
        }

        internal static bool DoubleRelativeChangeFromAbove(double v, double previous, double bound)
        {
            return CalculationSupport.Above(v.RelativeChangeFrom(previous), bound);
        }

        internal static bool DoubleRelativeChangeFromBelow(double v, double previous, double bound)
        {
            return CalculationSupport.Below(v.RelativeChangeFrom(previous), bound);
        }

        internal static bool DoubleRelativeChangeFromInRange(double v, double previous, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.RelativeChangeFrom(previous), lowerBound, upperBound);
        }

        internal static bool DoubleRelativeChangeFromOutsideRange(double v, double previous, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.RelativeChangeFrom(previous), lowerBound, upperBound);
        }

        internal static bool DoubleGrowthFactorFromAbove(double v, double previous, double bound)
        {
            return CalculationSupport.Above(v.GrowthFactorFrom(previous), bound);
        }

        internal static bool DoubleGrowthFactorFromBelow(double v, double previous, double bound)
        {
            return CalculationSupport.Below(v.GrowthFactorFrom(previous), bound);
        }

        internal static bool DoubleGrowthFactorFromInRange(double v, double previous, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.GrowthFactorFrom(previous), lowerBound, upperBound);
        }

        internal static bool DoubleGrowthFactorFromOutsideRange(double v, double previous, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.GrowthFactorFrom(previous), lowerBound, upperBound);
        }

        internal static bool DoubleSlopeFromAbove(double v, double previous, double elapsed, double bound)
        {
            return CalculationSupport.Above(v.SlopeFrom(previous, elapsed), bound);
        }

        internal static bool DoubleSlopeFromBelow(double v, double previous, double elapsed, double bound)
        {
            return CalculationSupport.Below(v.SlopeFrom(previous, elapsed), bound);
        }

        internal static bool DoubleSlopeFromInRange(double v, double previous, double elapsed, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.SlopeFrom(previous, elapsed), lowerBound, upperBound);
        }

        internal static bool DoubleSlopeFromOutsideRange(double v, double previous, double elapsed, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.SlopeFrom(previous, elapsed), lowerBound, upperBound);
        }

        internal static bool DoubleCompoundGrowthRateAbove(double v, double previous, double periods, double bound)
        {
            return CalculationSupport.Above(v.CompoundGrowthRate(previous, periods), bound);
        }

        internal static bool DoubleCompoundGrowthRateBelow(double v, double previous, double periods, double bound)
        {
            return CalculationSupport.Below(v.CompoundGrowthRate(previous, periods), bound);
        }

        internal static bool DoubleCompoundGrowthRateInRange(double v, double previous, double periods, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.CompoundGrowthRate(previous, periods), lowerBound, upperBound);
        }

        internal static bool DoubleCompoundGrowthRateOutsideRange(double v, double previous, double periods, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.CompoundGrowthRate(previous, periods), lowerBound, upperBound);
        }

        internal static bool DoublePercentDeviationFromMeanAbove(double v, double mean, double bound)
        {
            return CalculationSupport.Above(v.PercentDeviationFromMean(mean), bound);
        }

        internal static bool DoublePercentDeviationFromMeanBelow(double v, double mean, double bound)
        {
            return CalculationSupport.Below(v.PercentDeviationFromMean(mean), bound);
        }

        internal static bool DoublePercentDeviationFromMeanInRange(double v, double mean, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.PercentDeviationFromMean(mean), lowerBound, upperBound);
        }

        internal static bool DoublePercentDeviationFromMeanOutsideRange(double v, double mean, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.PercentDeviationFromMean(mean), lowerBound, upperBound);
        }

        internal static bool DoubleModifiedZScoreAbove(double v, double median, double medianAbsoluteDeviation, double bound)
        {
            return CalculationSupport.Above(v.ModifiedZScore(median, medianAbsoluteDeviation), bound);
        }

        internal static bool DoubleModifiedZScoreBelow(double v, double median, double medianAbsoluteDeviation, double bound)
        {
            return CalculationSupport.Below(v.ModifiedZScore(median, medianAbsoluteDeviation), bound);
        }

        internal static bool DoubleModifiedZScoreInRange(double v, double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.ModifiedZScore(median, medianAbsoluteDeviation), lowerBound, upperBound);
        }

        internal static bool DoubleModifiedZScoreOutsideRange(double v, double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.ModifiedZScore(median, medianAbsoluteDeviation), lowerBound, upperBound);
        }

        internal static bool DoubleCoefficientOfVariationAbove(double v, double mean, double bound)
        {
            return CalculationSupport.Above(v.CoefficientOfVariation(mean), bound);
        }

        internal static bool DoubleCoefficientOfVariationBelow(double v, double mean, double bound)
        {
            return CalculationSupport.Below(v.CoefficientOfVariation(mean), bound);
        }

        internal static bool DoubleCoefficientOfVariationInRange(double v, double mean, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.CoefficientOfVariation(mean), lowerBound, upperBound);
        }

        internal static bool DoubleCoefficientOfVariationOutsideRange(double v, double mean, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.CoefficientOfVariation(mean), lowerBound, upperBound);
        }

        internal static bool DoubleMinMaxNormaliseAbove(double v, double minimum, double maximum, double bound)
        {
            return CalculationSupport.Above(v.MinMaxNormalise(minimum, maximum), bound);
        }

        internal static bool DoubleMinMaxNormaliseBelow(double v, double minimum, double maximum, double bound)
        {
            return CalculationSupport.Below(v.MinMaxNormalise(minimum, maximum), bound);
        }

        internal static bool DoubleMinMaxNormaliseInRange(double v, double minimum, double maximum, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.MinMaxNormalise(minimum, maximum), lowerBound, upperBound);
        }

        internal static bool DoubleMinMaxNormaliseOutsideRange(double v, double minimum, double maximum, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.MinMaxNormalise(minimum, maximum), lowerBound, upperBound);
        }

        internal static bool DoublePercentOfRangeAbove(double v, double minimum, double maximum, double bound)
        {
            return CalculationSupport.Above(v.PercentOfRange(minimum, maximum), bound);
        }

        internal static bool DoublePercentOfRangeBelow(double v, double minimum, double maximum, double bound)
        {
            return CalculationSupport.Below(v.PercentOfRange(minimum, maximum), bound);
        }

        internal static bool DoublePercentOfRangeInRange(double v, double minimum, double maximum, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.PercentOfRange(minimum, maximum), lowerBound, upperBound);
        }

        internal static bool DoublePercentOfRangeOutsideRange(double v, double minimum, double maximum, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.PercentOfRange(minimum, maximum), lowerBound, upperBound);
        }

        internal static bool DoubleSumWithAbove(double v, double bound, double[] others)
        {
            return CalculationSupport.Above(v.SumWith(others), bound);
        }

        internal static bool DoubleSumWithBelow(double v, double bound, double[] others)
        {
            return CalculationSupport.Below(v.SumWith(others), bound);
        }

        internal static bool DoubleSumWithInRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.InRange(v.SumWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleSumWithOutsideRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.OutsideRange(v.SumWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleMeanWithAbove(double v, double bound, double[] others)
        {
            return CalculationSupport.Above(v.MeanWith(others), bound);
        }

        internal static bool DoubleMeanWithBelow(double v, double bound, double[] others)
        {
            return CalculationSupport.Below(v.MeanWith(others), bound);
        }

        internal static bool DoubleMeanWithInRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.InRange(v.MeanWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleMeanWithOutsideRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.OutsideRange(v.MeanWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleMaxWithAbove(double v, double bound, double[] others)
        {
            return CalculationSupport.Above(v.MaxWith(others), bound);
        }

        internal static bool DoubleMaxWithBelow(double v, double bound, double[] others)
        {
            return CalculationSupport.Below(v.MaxWith(others), bound);
        }

        internal static bool DoubleMaxWithInRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.InRange(v.MaxWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleMaxWithOutsideRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.OutsideRange(v.MaxWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleMinWithAbove(double v, double bound, double[] others)
        {
            return CalculationSupport.Above(v.MinWith(others), bound);
        }

        internal static bool DoubleMinWithBelow(double v, double bound, double[] others)
        {
            return CalculationSupport.Below(v.MinWith(others), bound);
        }

        internal static bool DoubleMinWithInRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.InRange(v.MinWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleMinWithOutsideRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.OutsideRange(v.MinWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleSpreadWithAbove(double v, double bound, double[] others)
        {
            return CalculationSupport.Above(v.SpreadWith(others), bound);
        }

        internal static bool DoubleSpreadWithBelow(double v, double bound, double[] others)
        {
            return CalculationSupport.Below(v.SpreadWith(others), bound);
        }

        internal static bool DoubleSpreadWithInRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.InRange(v.SpreadWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleSpreadWithOutsideRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.OutsideRange(v.SpreadWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleWeightedMeanWithAbove(double v, double weight, double other, double otherWeight, double bound)
        {
            return CalculationSupport.Above(v.WeightedMeanWith(weight, other, otherWeight), bound);
        }

        internal static bool DoubleWeightedMeanWithBelow(double v, double weight, double other, double otherWeight, double bound)
        {
            return CalculationSupport.Below(v.WeightedMeanWith(weight, other, otherWeight), bound);
        }

        internal static bool DoubleWeightedMeanWithInRange(double v, double weight, double other, double otherWeight, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.WeightedMeanWith(weight, other, otherWeight), lowerBound, upperBound);
        }

        internal static bool DoubleWeightedMeanWithOutsideRange(double v, double weight, double other, double otherWeight, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.WeightedMeanWith(weight, other, otherWeight), lowerBound, upperBound);
        }

        internal static bool DoubleGeometricMeanWithAbove(double v, double other, double bound)
        {
            return CalculationSupport.Above(v.GeometricMeanWith(other), bound);
        }

        internal static bool DoubleGeometricMeanWithBelow(double v, double other, double bound)
        {
            return CalculationSupport.Below(v.GeometricMeanWith(other), bound);
        }

        internal static bool DoubleGeometricMeanWithInRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.GeometricMeanWith(other), lowerBound, upperBound);
        }

        internal static bool DoubleGeometricMeanWithOutsideRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.GeometricMeanWith(other), lowerBound, upperBound);
        }

        internal static bool DoubleHarmonicMeanWithAbove(double v, double other, double bound)
        {
            return CalculationSupport.Above(v.HarmonicMeanWith(other), bound);
        }

        internal static bool DoubleHarmonicMeanWithBelow(double v, double other, double bound)
        {
            return CalculationSupport.Below(v.HarmonicMeanWith(other), bound);
        }

        internal static bool DoubleHarmonicMeanWithInRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.HarmonicMeanWith(other), lowerBound, upperBound);
        }

        internal static bool DoubleHarmonicMeanWithOutsideRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.HarmonicMeanWith(other), lowerBound, upperBound);
        }

        internal static bool DoubleShareOfMaxAbove(double v, double bound, double[] others)
        {
            return CalculationSupport.Above(v.ShareOfMax(others), bound);
        }

        internal static bool DoubleShareOfMaxBelow(double v, double bound, double[] others)
        {
            return CalculationSupport.Below(v.ShareOfMax(others), bound);
        }

        internal static bool DoubleShareOfMaxInRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.InRange(v.ShareOfMax(others), lowerBound, upperBound);
        }

        internal static bool DoubleShareOfMaxOutsideRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.OutsideRange(v.ShareOfMax(others), lowerBound, upperBound);
        }

        internal static bool DoubleHerfindahlWithAbove(double v, double bound, double[] others)
        {
            return CalculationSupport.Above(v.HerfindahlWith(others), bound);
        }

        internal static bool DoubleHerfindahlWithBelow(double v, double bound, double[] others)
        {
            return CalculationSupport.Below(v.HerfindahlWith(others), bound);
        }

        internal static bool DoubleHerfindahlWithInRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.InRange(v.HerfindahlWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleHerfindahlWithOutsideRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.OutsideRange(v.HerfindahlWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleEntropyOfSharesWithAbove(double v, double bound, double[] others)
        {
            return CalculationSupport.Above(v.EntropyOfSharesWith(others), bound);
        }

        internal static bool DoubleEntropyOfSharesWithBelow(double v, double bound, double[] others)
        {
            return CalculationSupport.Below(v.EntropyOfSharesWith(others), bound);
        }

        internal static bool DoubleEntropyOfSharesWithInRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.InRange(v.EntropyOfSharesWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleEntropyOfSharesWithOutsideRange(double v, double lowerBound, double upperBound, double[] others)
        {
            return CalculationSupport.OutsideRange(v.EntropyOfSharesWith(others), lowerBound, upperBound);
        }

        internal static bool DoubleHeadroomToAbove(double v, double limit, double bound)
        {
            return CalculationSupport.Above(v.HeadroomTo(limit), bound);
        }

        internal static bool DoubleHeadroomToBelow(double v, double limit, double bound)
        {
            return CalculationSupport.Below(v.HeadroomTo(limit), bound);
        }

        internal static bool DoubleHeadroomToInRange(double v, double limit, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.HeadroomTo(limit), lowerBound, upperBound);
        }

        internal static bool DoubleHeadroomToOutsideRange(double v, double limit, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.HeadroomTo(limit), lowerBound, upperBound);
        }

        internal static bool DoubleExcessOverAbove(double v, double limit, double bound)
        {
            return CalculationSupport.Above(v.ExcessOver(limit), bound);
        }

        internal static bool DoubleExcessOverBelow(double v, double limit, double bound)
        {
            return CalculationSupport.Below(v.ExcessOver(limit), bound);
        }

        internal static bool DoubleExcessOverInRange(double v, double limit, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.ExcessOver(limit), lowerBound, upperBound);
        }

        internal static bool DoubleExcessOverOutsideRange(double v, double limit, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.ExcessOver(limit), lowerBound, upperBound);
        }

        internal static bool DoubleShortfallBelowAbove(double v, double minimum, double bound)
        {
            return CalculationSupport.Above(v.ShortfallBelow(minimum), bound);
        }

        internal static bool DoubleShortfallBelowBelow(double v, double minimum, double bound)
        {
            return CalculationSupport.Below(v.ShortfallBelow(minimum), bound);
        }

        internal static bool DoubleShortfallBelowInRange(double v, double minimum, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.ShortfallBelow(minimum), lowerBound, upperBound);
        }

        internal static bool DoubleShortfallBelowOutsideRange(double v, double minimum, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.ShortfallBelow(minimum), lowerBound, upperBound);
        }

        internal static bool DoubleDistanceToThresholdAbove(double v, double threshold, double bound)
        {
            return CalculationSupport.Above(v.DistanceToThreshold(threshold), bound);
        }

        internal static bool DoubleDistanceToThresholdBelow(double v, double threshold, double bound)
        {
            return CalculationSupport.Below(v.DistanceToThreshold(threshold), bound);
        }

        internal static bool DoubleDistanceToThresholdInRange(double v, double threshold, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.DistanceToThreshold(threshold), lowerBound, upperBound);
        }

        internal static bool DoubleDistanceToThresholdOutsideRange(double v, double threshold, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.DistanceToThreshold(threshold), lowerBound, upperBound);
        }

        internal static bool DoublePercentBelowThresholdAbove(double v, double threshold, double bound)
        {
            return CalculationSupport.Above(v.PercentBelowThreshold(threshold), bound);
        }

        internal static bool DoublePercentBelowThresholdBelow(double v, double threshold, double bound)
        {
            return CalculationSupport.Below(v.PercentBelowThreshold(threshold), bound);
        }

        internal static bool DoublePercentBelowThresholdInRange(double v, double threshold, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.PercentBelowThreshold(threshold), lowerBound, upperBound);
        }

        internal static bool DoublePercentBelowThresholdOutsideRange(double v, double threshold, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.PercentBelowThreshold(threshold), lowerBound, upperBound);
        }

        internal static bool DoubleImbalanceWithAbove(double v, double other, double bound)
        {
            return CalculationSupport.Above(v.ImbalanceWith(other), bound);
        }

        internal static bool DoubleImbalanceWithBelow(double v, double other, double bound)
        {
            return CalculationSupport.Below(v.ImbalanceWith(other), bound);
        }

        internal static bool DoubleImbalanceWithInRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.ImbalanceWith(other), lowerBound, upperBound);
        }

        internal static bool DoubleImbalanceWithOutsideRange(double v, double other, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.ImbalanceWith(other), lowerBound, upperBound);
        }

        internal static bool DoubleRetentionAfterAbove(double v, double outflow, double bound)
        {
            return CalculationSupport.Above(v.RetentionAfter(outflow), bound);
        }

        internal static bool DoubleRetentionAfterBelow(double v, double outflow, double bound)
        {
            return CalculationSupport.Below(v.RetentionAfter(outflow), bound);
        }

        internal static bool DoubleRetentionAfterInRange(double v, double outflow, double lowerBound, double upperBound)
        {
            return CalculationSupport.InRange(v.RetentionAfter(outflow), lowerBound, upperBound);
        }

        internal static bool DoubleRetentionAfterOutsideRange(double v, double outflow, double lowerBound, double upperBound)
        {
            return CalculationSupport.OutsideRange(v.RetentionAfter(outflow), lowerBound, upperBound);
        }
    }
}
