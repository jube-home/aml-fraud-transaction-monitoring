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

using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowDoubleTests
    {
        [Theory]
        [InlineData(150, 100, true)]
        [InlineData(100, 100, false)]
        [InlineData(50, 100, false)]
        [InlineData(double.NaN, 1, false)]
        public void GreaterAppliesTheOutcomeOfEveryStepKind(double value, double other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchGreater(other),
                start.RejectGreater(other),
                start.BreakGreater(other),
                start.RequireGreater(other),
                start.EnsureGreater(other));

            FlowAssert.Kinds(expected,
                value.MatchGreater(other),
                value.RejectGreater(other),
                value.BreakGreater(other),
                value.RequireGreater(other),
                value.EnsureGreater(other));
        }

        [Theory]
        [InlineData(150, 100, true)]
        [InlineData(100, 100, true)]
        [InlineData(50, 100, false)]
        [InlineData(double.NaN, 1, false)]
        public void GreaterOrEqualAppliesTheOutcomeOfEveryStepKind(double value, double other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchGreaterOrEqual(other),
                start.RejectGreaterOrEqual(other),
                start.BreakGreaterOrEqual(other),
                start.RequireGreaterOrEqual(other),
                start.EnsureGreaterOrEqual(other));

            FlowAssert.Kinds(expected,
                value.MatchGreaterOrEqual(other),
                value.RejectGreaterOrEqual(other),
                value.BreakGreaterOrEqual(other),
                value.RequireGreaterOrEqual(other),
                value.EnsureGreaterOrEqual(other));
        }

        [Theory]
        [InlineData(50, 100, true)]
        [InlineData(100, 100, false)]
        [InlineData(150, 100, false)]
        [InlineData(double.NaN, 1, false)]
        public void LessAppliesTheOutcomeOfEveryStepKind(double value, double other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLess(other),
                start.RejectLess(other),
                start.BreakLess(other),
                start.RequireLess(other),
                start.EnsureLess(other));

            FlowAssert.Kinds(expected,
                value.MatchLess(other),
                value.RejectLess(other),
                value.BreakLess(other),
                value.RequireLess(other),
                value.EnsureLess(other));
        }

        [Theory]
        [InlineData(50, 100, true)]
        [InlineData(100, 100, true)]
        [InlineData(150, 100, false)]
        [InlineData(double.NaN, 1, false)]
        public void LessOrEqualAppliesTheOutcomeOfEveryStepKind(double value, double other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLessOrEqual(other),
                start.RejectLessOrEqual(other),
                start.BreakLessOrEqual(other),
                start.RequireLessOrEqual(other),
                start.EnsureLessOrEqual(other));

            FlowAssert.Kinds(expected,
                value.MatchLessOrEqual(other),
                value.RejectLessOrEqual(other),
                value.BreakLessOrEqual(other),
                value.RequireLessOrEqual(other),
                value.EnsureLessOrEqual(other));
        }

        [Theory]
        [InlineData(1.5, 1.5, true)]
        [InlineData(1.5, 2.5, false)]
        [InlineData(double.NaN, double.NaN, false)]
        public void EqualAppliesTheOutcomeOfEveryStepKind(double value, double other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEqual(other),
                start.RejectEqual(other),
                start.BreakEqual(other),
                start.RequireEqual(other),
                start.EnsureEqual(other));

            FlowAssert.Kinds(expected,
                value.MatchEqual(other),
                value.RejectEqual(other),
                value.BreakEqual(other),
                value.RequireEqual(other),
                value.EnsureEqual(other));
        }

        [Theory]
        [InlineData(1, 2, true)]
        [InlineData(1, 1, false)]
        [InlineData(double.NaN, 1, false)]
        [InlineData(1, double.NaN, false)]
        public void NotEqualAppliesTheOutcomeOfEveryStepKind(double value, double other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchNotEqual(other),
                start.RejectNotEqual(other),
                start.BreakNotEqual(other),
                start.RequireNotEqual(other),
                start.EnsureNotEqual(other));

            FlowAssert.Kinds(expected,
                value.MatchNotEqual(other),
                value.RejectNotEqual(other),
                value.BreakNotEqual(other),
                value.RequireNotEqual(other),
                value.EnsureNotEqual(other));
        }

        [Theory]
        [InlineData(5, 1, 10, true)]
        [InlineData(1, 1, 10, false)]
        [InlineData(10, 1, 10, false)]
        [InlineData(11, 1, 10, false)]
        public void BetweenAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchBetween(minimum, maximum),
                start.RejectBetween(minimum, maximum),
                start.BreakBetween(minimum, maximum),
                start.RequireBetween(minimum, maximum),
                start.EnsureBetween(minimum, maximum));

            FlowAssert.Kinds(expected,
                value.MatchBetween(minimum, maximum),
                value.RejectBetween(minimum, maximum),
                value.BreakBetween(minimum, maximum),
                value.RequireBetween(minimum, maximum),
                value.EnsureBetween(minimum, maximum));
        }

        [Theory]
        [InlineData(5, 1, 10, true)]
        [InlineData(1, 1, 10, true)]
        [InlineData(10, 1, 10, true)]
        [InlineData(11, 1, 10, false)]
        [InlineData(0, 1, 10, false)]
        public void InRangeAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchInRange(minimum, maximum),
                start.RejectInRange(minimum, maximum),
                start.BreakInRange(minimum, maximum),
                start.RequireInRange(minimum, maximum),
                start.EnsureInRange(minimum, maximum));

            FlowAssert.Kinds(expected,
                value.MatchInRange(minimum, maximum),
                value.RejectInRange(minimum, maximum),
                value.BreakInRange(minimum, maximum),
                value.RequireInRange(minimum, maximum),
                value.EnsureInRange(minimum, maximum));
        }

        [Theory]
        [InlineData(0, 1, 10, true)]
        [InlineData(11, 1, 10, true)]
        [InlineData(1, 1, 10, false)]
        [InlineData(10, 1, 10, false)]
        [InlineData(double.NaN, 1, 10, false)]
        public void OutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchOutsideRange(minimum, maximum),
                start.RejectOutsideRange(minimum, maximum),
                start.BreakOutsideRange(minimum, maximum),
                start.RequireOutsideRange(minimum, maximum),
                start.EnsureOutsideRange(minimum, maximum));

            FlowAssert.Kinds(expected,
                value.MatchOutsideRange(minimum, maximum),
                value.RejectOutsideRange(minimum, maximum),
                value.BreakOutsideRange(minimum, maximum),
                value.RequireOutsideRange(minimum, maximum),
                value.EnsureOutsideRange(minimum, maximum));
        }

        [Theory]
        [InlineData(5, new[] { 1d, 5d }, true)]
        [InlineData(2, new[] { 1d, 5d }, false)]
        [InlineData(2, new double[0], false)]
        public void InAppliesTheOutcomeOfEveryStepKind(double value, double[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIn(values),
                start.RejectIn(values),
                start.BreakIn(values),
                start.RequireIn(values),
                start.EnsureIn(values));

            FlowAssert.Kinds(expected,
                value.MatchIn(values),
                value.RejectIn(values),
                value.BreakIn(values),
                value.RequireIn(values),
                value.EnsureIn(values));
        }

        [Theory]
        [InlineData(2, new[] { 1d, 5d }, true)]
        [InlineData(5, new[] { 1d, 5d }, false)]
        [InlineData(2, new double[0], true)]
        public void NotInAppliesTheOutcomeOfEveryStepKind(double value, double[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchNotIn(values),
                start.RejectNotIn(values),
                start.BreakNotIn(values),
                start.RequireNotIn(values),
                start.EnsureNotIn(values));

            FlowAssert.Kinds(expected,
                value.MatchNotIn(values),
                value.RejectNotIn(values),
                value.BreakNotIn(values),
                value.RequireNotIn(values),
                value.EnsureNotIn(values));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(0.1, false)]
        [InlineData(double.NaN, false)]
        [InlineData(-0.0, true)]
        [InlineData(double.Epsilon, false)]
        [InlineData(-1, false)]
        public void IsZeroAppliesTheOutcomeOfEveryStepKind(double value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsZero(),
                start.RejectIsZero(),
                start.BreakIsZero(),
                start.RequireIsZero(),
                start.EnsureIsZero());

            FlowAssert.Kinds(expected,
                value.MatchIsZero(),
                value.RejectIsZero(),
                value.BreakIsZero(),
                value.RequireIsZero(),
                value.EnsureIsZero());
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        public void IsPositiveAppliesTheOutcomeOfEveryStepKind(double value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsPositive(),
                start.RejectIsPositive(),
                start.BreakIsPositive(),
                start.RequireIsPositive(),
                start.EnsureIsPositive());

            FlowAssert.Kinds(expected,
                value.MatchIsPositive(),
                value.RejectIsPositive(),
                value.BreakIsPositive(),
                value.RequireIsPositive(),
                value.EnsureIsPositive());
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        public void IsNegativeAppliesTheOutcomeOfEveryStepKind(double value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNegative(),
                start.RejectIsNegative(),
                start.BreakIsNegative(),
                start.RequireIsNegative(),
                start.EnsureIsNegative());

            FlowAssert.Kinds(expected,
                value.MatchIsNegative(),
                value.RejectIsNegative(),
                value.BreakIsNegative(),
                value.RequireIsNegative(),
                value.EnsureIsNegative());
        }

        [Theory]
        [InlineData(double.NaN, true)]
        [InlineData(1, false)]
        [InlineData(double.NegativeInfinity, false)]
        [InlineData(0, false)]
        public void IsNaNAppliesTheOutcomeOfEveryStepKind(double value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNaN(),
                start.RejectIsNaN(),
                start.BreakIsNaN(),
                start.RequireIsNaN(),
                start.EnsureIsNaN());

            FlowAssert.Kinds(expected,
                value.MatchIsNaN(),
                value.RejectIsNaN(),
                value.BreakIsNaN(),
                value.RequireIsNaN(),
                value.EnsureIsNaN());
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(double.PositiveInfinity, false)]
        [InlineData(double.NaN, false)]
        public void IsFiniteAppliesTheOutcomeOfEveryStepKind(double value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsFinite(),
                start.RejectIsFinite(),
                start.BreakIsFinite(),
                start.RequireIsFinite(),
                start.EnsureIsFinite());

            FlowAssert.Kinds(expected,
                value.MatchIsFinite(),
                value.RejectIsFinite(),
                value.BreakIsFinite(),
                value.RequireIsFinite(),
                value.EnsureIsFinite());
        }

        [Theory]
        [InlineData(3, true)]
        [InlineData(3.5, false)]
        [InlineData(double.PositiveInfinity, false)]
        [InlineData(double.NaN, false)]
        public void IsWholeNumberAppliesTheOutcomeOfEveryStepKind(double value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsWholeNumber(),
                start.RejectIsWholeNumber(),
                start.BreakIsWholeNumber(),
                start.RequireIsWholeNumber(),
                start.EnsureIsWholeNumber());

            FlowAssert.Kinds(expected,
                value.MatchIsWholeNumber(),
                value.RejectIsWholeNumber(),
                value.BreakIsWholeNumber(),
                value.RequireIsWholeNumber(),
                value.EnsureIsWholeNumber());
        }

        [Theory]
        [InlineData(10, 5, true)]
        [InlineData(10, 3, false)]
        [InlineData(10, 0, false)]
        [InlineData(0, 5, true)]
        public void IsMultipleOfAppliesTheOutcomeOfEveryStepKind(double value, double divisor, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsMultipleOf(divisor),
                start.RejectIsMultipleOf(divisor),
                start.BreakIsMultipleOf(divisor),
                start.RequireIsMultipleOf(divisor),
                start.EnsureIsMultipleOf(divisor));

            FlowAssert.Kinds(expected,
                value.MatchIsMultipleOf(divisor),
                value.RejectIsMultipleOf(divisor),
                value.BreakIsMultipleOf(divisor),
                value.RequireIsMultipleOf(divisor),
                value.EnsureIsMultipleOf(divisor));
        }

        [Theory]
        [InlineData(105, 100, 10, true)]
        [InlineData(120, 100, 10, false)]
        [InlineData(0, 0, 10, true)]
        [InlineData(1, 0, 10, false)]
        public void WithinPercentOfAppliesTheOutcomeOfEveryStepKind(double value, double target, double percent, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWithinPercentOf(target, percent),
                start.RejectWithinPercentOf(target, percent),
                start.BreakWithinPercentOf(target, percent),
                start.RequireWithinPercentOf(target, percent),
                start.EnsureWithinPercentOf(target, percent));

            FlowAssert.Kinds(expected,
                value.MatchWithinPercentOf(target, percent),
                value.RejectWithinPercentOf(target, percent),
                value.BreakWithinPercentOf(target, percent),
                value.RequireWithinPercentOf(target, percent),
                value.EnsureWithinPercentOf(target, percent));
        }

        [Theory]
        [InlineData(120, 100, 10, true)]
        [InlineData(105, 100, 10, false)]
        [InlineData(1, 0, 10, true)]
        [InlineData(double.NaN, 100, 10, false)]
        public void DeviatesFromByPercentAppliesTheOutcomeOfEveryStepKind(double value, double target, double percent, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDeviatesFromByPercent(target, percent),
                start.RejectDeviatesFromByPercent(target, percent),
                start.BreakDeviatesFromByPercent(target, percent),
                start.RequireDeviatesFromByPercent(target, percent),
                start.EnsureDeviatesFromByPercent(target, percent));

            FlowAssert.Kinds(expected,
                value.MatchDeviatesFromByPercent(target, percent),
                value.RejectDeviatesFromByPercent(target, percent),
                value.BreakDeviatesFromByPercent(target, percent),
                value.RequireDeviatesFromByPercent(target, percent),
                value.EnsureDeviatesFromByPercent(target, percent));
        }

        [Theory]
        [InlineData(500, 100, 300, true)]
        [InlineData(400, 100, 300, false)]
        [InlineData(50, 100, 300, false)]
        [InlineData(0, 0, 10, false)]
        public void IncreasedByMoreThanPercentAppliesTheOutcomeOfEveryStepKind(double value, double previous, double percent, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIncreasedByMoreThanPercent(previous, percent),
                start.RejectIncreasedByMoreThanPercent(previous, percent),
                start.BreakIncreasedByMoreThanPercent(previous, percent),
                start.RequireIncreasedByMoreThanPercent(previous, percent),
                start.EnsureIncreasedByMoreThanPercent(previous, percent));

            FlowAssert.Kinds(expected,
                value.MatchIncreasedByMoreThanPercent(previous, percent),
                value.RejectIncreasedByMoreThanPercent(previous, percent),
                value.BreakIncreasedByMoreThanPercent(previous, percent),
                value.RequireIncreasedByMoreThanPercent(previous, percent),
                value.EnsureIncreasedByMoreThanPercent(previous, percent));
        }

        [Theory]
        [InlineData(40, 100, 50, true)]
        [InlineData(60, 100, 50, false)]
        [InlineData(500, 100, 50, false)]
        [InlineData(0, 0, 10, false)]
        public void DecreasedByMoreThanPercentAppliesTheOutcomeOfEveryStepKind(double value, double previous, double percent, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDecreasedByMoreThanPercent(previous, percent),
                start.RejectDecreasedByMoreThanPercent(previous, percent),
                start.BreakDecreasedByMoreThanPercent(previous, percent),
                start.RequireDecreasedByMoreThanPercent(previous, percent),
                start.EnsureDecreasedByMoreThanPercent(previous, percent));

            FlowAssert.Kinds(expected,
                value.MatchDecreasedByMoreThanPercent(previous, percent),
                value.RejectDecreasedByMoreThanPercent(previous, percent),
                value.BreakDecreasedByMoreThanPercent(previous, percent),
                value.RequireDecreasedByMoreThanPercent(previous, percent),
                value.EnsureDecreasedByMoreThanPercent(previous, percent));
        }

        [Theory]
        [InlineData(16, 10, 2, 2, true)]
        [InlineData(12, 10, 2, 2, false)]
        [InlineData(5, 5, 0, 1, false)]
        public void ZScoreAboveAppliesTheOutcomeOfEveryStepKind(double value, double mean, double standardDeviation, double threshold, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchZScoreAbove(mean, standardDeviation, threshold),
                start.RejectZScoreAbove(mean, standardDeviation, threshold),
                start.BreakZScoreAbove(mean, standardDeviation, threshold),
                start.RequireZScoreAbove(mean, standardDeviation, threshold),
                start.EnsureZScoreAbove(mean, standardDeviation, threshold));

            FlowAssert.Kinds(expected,
                value.MatchZScoreAbove(mean, standardDeviation, threshold),
                value.RejectZScoreAbove(mean, standardDeviation, threshold),
                value.BreakZScoreAbove(mean, standardDeviation, threshold),
                value.RequireZScoreAbove(mean, standardDeviation, threshold),
                value.EnsureZScoreAbove(mean, standardDeviation, threshold));
        }

        [Theory]
        [InlineData(4, 10, 2, -2, true)]
        [InlineData(9, 10, 2, -2, false)]
        [InlineData(5, 5, 0, 1, false)]
        public void ZScoreBelowAppliesTheOutcomeOfEveryStepKind(double value, double mean, double standardDeviation, double threshold, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchZScoreBelow(mean, standardDeviation, threshold),
                start.RejectZScoreBelow(mean, standardDeviation, threshold),
                start.BreakZScoreBelow(mean, standardDeviation, threshold),
                start.RequireZScoreBelow(mean, standardDeviation, threshold),
                start.EnsureZScoreBelow(mean, standardDeviation, threshold));

            FlowAssert.Kinds(expected,
                value.MatchZScoreBelow(mean, standardDeviation, threshold),
                value.RejectZScoreBelow(mean, standardDeviation, threshold),
                value.BreakZScoreBelow(mean, standardDeviation, threshold),
                value.RequireZScoreBelow(mean, standardDeviation, threshold),
                value.EnsureZScoreBelow(mean, standardDeviation, threshold));
        }

        [Theory]
        [InlineData(16, 10, 2, 2, true)]
        [InlineData(4, 10, 2, 2, true)]
        [InlineData(11, 10, 2, 2, false)]
        [InlineData(5, 5, 0, 1, false)]
        public void ZScoreOutsideAppliesTheOutcomeOfEveryStepKind(double value, double mean, double standardDeviation, double threshold, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchZScoreOutside(mean, standardDeviation, threshold),
                start.RejectZScoreOutside(mean, standardDeviation, threshold),
                start.BreakZScoreOutside(mean, standardDeviation, threshold),
                start.RequireZScoreOutside(mean, standardDeviation, threshold),
                start.EnsureZScoreOutside(mean, standardDeviation, threshold));

            FlowAssert.Kinds(expected,
                value.MatchZScoreOutside(mean, standardDeviation, threshold),
                value.RejectZScoreOutside(mean, standardDeviation, threshold),
                value.BreakZScoreOutside(mean, standardDeviation, threshold),
                value.RequireZScoreOutside(mean, standardDeviation, threshold),
                value.EnsureZScoreOutside(mean, standardDeviation, threshold));
        }

        [Theory]
        [InlineData(10, 2, 4, true)]
        [InlineData(10, 5, 4, false)]
        [InlineData(10, 0, 4, false)]
        public void RatioAboveAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double threshold, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioAbove(denominator, threshold),
                start.RejectRatioAbove(denominator, threshold),
                start.BreakRatioAbove(denominator, threshold),
                start.RequireRatioAbove(denominator, threshold),
                start.EnsureRatioAbove(denominator, threshold));

            FlowAssert.Kinds(expected,
                value.MatchRatioAbove(denominator, threshold),
                value.RejectRatioAbove(denominator, threshold),
                value.BreakRatioAbove(denominator, threshold),
                value.RequireRatioAbove(denominator, threshold),
                value.EnsureRatioAbove(denominator, threshold));
        }

        [Theory]
        [InlineData(10, 5, 4, true)]
        [InlineData(10, 2, 4, false)]
        [InlineData(10, 0, 4, false)]
        public void RatioBelowAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double threshold, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioBelow(denominator, threshold),
                start.RejectRatioBelow(denominator, threshold),
                start.BreakRatioBelow(denominator, threshold),
                start.RequireRatioBelow(denominator, threshold),
                start.EnsureRatioBelow(denominator, threshold));

            FlowAssert.Kinds(expected,
                value.MatchRatioBelow(denominator, threshold),
                value.RejectRatioBelow(denominator, threshold),
                value.BreakRatioBelow(denominator, threshold),
                value.RequireRatioBelow(denominator, threshold),
                value.EnsureRatioBelow(denominator, threshold));
        }

        [Theory]
        [InlineData(9500, 10000, 10, true)]
        [InlineData(9000, 10000, 10, true)]
        [InlineData(8999, 10000, 10, false)]
        [InlineData(10000, 10000, 10, false)]
        [InlineData(10500, 10000, 10, false)]
        public void IsJustBelowThresholdAppliesTheOutcomeOfEveryStepKind(double value, double threshold, double marginPercent, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsJustBelowThreshold(threshold, marginPercent),
                start.RejectIsJustBelowThreshold(threshold, marginPercent),
                start.BreakIsJustBelowThreshold(threshold, marginPercent),
                start.RequireIsJustBelowThreshold(threshold, marginPercent),
                start.EnsureIsJustBelowThreshold(threshold, marginPercent));

            FlowAssert.Kinds(expected,
                value.MatchIsJustBelowThreshold(threshold, marginPercent),
                value.RejectIsJustBelowThreshold(threshold, marginPercent),
                value.BreakIsJustBelowThreshold(threshold, marginPercent),
                value.RequireIsJustBelowThreshold(threshold, marginPercent),
                value.EnsureIsJustBelowThreshold(threshold, marginPercent));
        }

        [Theory]
        [InlineData(10500, 10000, 10, true)]
        [InlineData(11000, 10000, 10, true)]
        [InlineData(11001, 10000, 10, false)]
        [InlineData(10000, 10000, 10, false)]
        [InlineData(9000, 10000, 10, false)]
        public void IsJustAboveThresholdAppliesTheOutcomeOfEveryStepKind(double value, double threshold, double marginPercent, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsJustAboveThreshold(threshold, marginPercent),
                start.RejectIsJustAboveThreshold(threshold, marginPercent),
                start.BreakIsJustAboveThreshold(threshold, marginPercent),
                start.RequireIsJustAboveThreshold(threshold, marginPercent),
                start.EnsureIsJustAboveThreshold(threshold, marginPercent));

            FlowAssert.Kinds(expected,
                value.MatchIsJustAboveThreshold(threshold, marginPercent),
                value.RejectIsJustAboveThreshold(threshold, marginPercent),
                value.BreakIsJustAboveThreshold(threshold, marginPercent),
                value.RequireIsJustAboveThreshold(threshold, marginPercent),
                value.EnsureIsJustAboveThreshold(threshold, marginPercent));
        }

        [Theory]
        [InlineData(5000, 1000, true)]
        [InlineData(5100, 1000, false)]
        [InlineData(5000, 0, false)]
        [InlineData(-5000, 1000, true)]
        public void IsRoundAmountAppliesTheOutcomeOfEveryStepKind(double value, double nearest, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsRoundAmount(nearest),
                start.RejectIsRoundAmount(nearest),
                start.BreakIsRoundAmount(nearest),
                start.RequireIsRoundAmount(nearest),
                start.EnsureIsRoundAmount(nearest));

            FlowAssert.Kinds(expected,
                value.MatchIsRoundAmount(nearest),
                value.RejectIsRoundAmount(nearest),
                value.BreakIsRoundAmount(nearest),
                value.RequireIsRoundAmount(nearest),
                value.EnsureIsRoundAmount(nearest));
        }

        [Theory]
        [InlineData(51.5, -0.1, 51.5, -0.1, 1, true)]
        [InlineData(0, 0, 0, 1, 50, false)]
        [InlineData(0, 0, 0, 1, 200, true)]
        public void WithinRadiusKmAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double radiusKm, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                start.RejectWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                start.BreakWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                start.RequireWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                start.EnsureWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm));

            FlowAssert.Kinds(expected,
                value.MatchWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                value.RejectWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                value.BreakWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                value.RequireWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                value.EnsureWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 50, true)]
        [InlineData(51.5, -0.1, 51.5, -0.1, 1, false)]
        [InlineData(0, 0, 0, 1, 200, false)]
        public void OutsideRadiusKmAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double radiusKm, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                start.RejectOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                start.BreakOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                start.RequireOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                start.EnsureOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm));

            FlowAssert.Kinds(expected,
                value.MatchOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                value.RejectOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                value.BreakOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                value.RequireOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm),
                value.EnsureOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 50, true)]
        [InlineData(0, 0, 0, 1, 500, false)]
        [InlineData(0, 0, 0, 1, 111.19, true)]
        [InlineData(0, 0, 0, 0, 1, false)]
        public void DistanceKmAboveAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double kilometers, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDistanceKmAbove(longitude1, latitude2, longitude2, kilometers),
                start.RejectDistanceKmAbove(longitude1, latitude2, longitude2, kilometers),
                start.BreakDistanceKmAbove(longitude1, latitude2, longitude2, kilometers),
                start.RequireDistanceKmAbove(longitude1, latitude2, longitude2, kilometers),
                start.EnsureDistanceKmAbove(longitude1, latitude2, longitude2, kilometers));

            FlowAssert.Kinds(expected,
                value.MatchDistanceKmAbove(longitude1, latitude2, longitude2, kilometers),
                value.RejectDistanceKmAbove(longitude1, latitude2, longitude2, kilometers),
                value.BreakDistanceKmAbove(longitude1, latitude2, longitude2, kilometers),
                value.RequireDistanceKmAbove(longitude1, latitude2, longitude2, kilometers),
                value.EnsureDistanceKmAbove(longitude1, latitude2, longitude2, kilometers));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 500, true)]
        [InlineData(0, 0, 0, 1, 50, false)]
        [InlineData(0, 0, 0, 1, 111.2, true)]
        [InlineData(0, 0, 0, 0, 1, true)]
        public void DistanceKmBelowAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double kilometers, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDistanceKmBelow(longitude1, latitude2, longitude2, kilometers),
                start.RejectDistanceKmBelow(longitude1, latitude2, longitude2, kilometers),
                start.BreakDistanceKmBelow(longitude1, latitude2, longitude2, kilometers),
                start.RequireDistanceKmBelow(longitude1, latitude2, longitude2, kilometers),
                start.EnsureDistanceKmBelow(longitude1, latitude2, longitude2, kilometers));

            FlowAssert.Kinds(expected,
                value.MatchDistanceKmBelow(longitude1, latitude2, longitude2, kilometers),
                value.RejectDistanceKmBelow(longitude1, latitude2, longitude2, kilometers),
                value.BreakDistanceKmBelow(longitude1, latitude2, longitude2, kilometers),
                value.RequireDistanceKmBelow(longitude1, latitude2, longitude2, kilometers),
                value.EnsureDistanceKmBelow(longitude1, latitude2, longitude2, kilometers));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 1, 100, true)]
        [InlineData(0, 0, 0, 1, 2, 100, false)]
        [InlineData(0, 0, 0, 1, 0, 100, true)]
        public void ImpliedSpeedAboveAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                start.RejectImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                start.BreakImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                start.RequireImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                start.EnsureImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));

            FlowAssert.Kinds(expected,
                value.MatchImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                value.RejectImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                value.BreakImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                value.RequireImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                value.EnsureImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 2, 100, true)]
        [InlineData(0, 0, 0, 1, 1, 100, false)]
        [InlineData(0, 0, 0, 1, 0, 100, false)]
        public void ImpliedSpeedBelowAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                start.RejectImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                start.BreakImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                start.RequireImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                start.EnsureImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));

            FlowAssert.Kinds(expected,
                value.MatchImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                value.RejectImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                value.BreakImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                value.RequireImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour),
                value.EnsureImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, true)]
        [InlineData(double.NaN, false)]
        public void HasValueAppliesTheOutcomeOfEveryStepKind(double value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHasValue(),
                start.RejectHasValue(),
                start.BreakHasValue(),
                start.RequireHasValue(),
                start.EnsureHasValue());

            FlowAssert.Kinds(expected,
                value.MatchHasValue(),
                value.RejectHasValue(),
                value.BreakHasValue(),
                value.RequireHasValue(),
                value.EnsureHasValue());
        }

        [Theory]
        [InlineData(double.NaN, true)]
        [InlineData(1, false)]
        [InlineData(double.PositiveInfinity, false)]
        public void HasNoValueAppliesTheOutcomeOfEveryStepKind(double value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHasNoValue(),
                start.RejectHasNoValue(),
                start.BreakHasNoValue(),
                start.RequireHasNoValue(),
                start.EnsureHasNoValue());

            FlowAssert.Kinds(expected,
                value.MatchHasNoValue(),
                value.RejectHasNoValue(),
                value.BreakHasNoValue(),
                value.RequireHasNoValue(),
                value.EnsureHasNoValue());
        }

        [Theory]
        [InlineData(10, 5, 1, 3, true)]
        [InlineData(15, 5, 1, 3, true)]
        [InlineData(5, 5, 1, 3, true)]
        [InlineData(16, 5, 1, 3, false)]
        [InlineData(4, 5, 1, 3, false)]
        [InlineData(10, 0, 1, 3, false)]
        public void RatioInRangeAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double minimum, double maximum, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioInRange(denominator, minimum, maximum),
                start.RejectRatioInRange(denominator, minimum, maximum),
                start.BreakRatioInRange(denominator, minimum, maximum),
                start.RequireRatioInRange(denominator, minimum, maximum),
                start.EnsureRatioInRange(denominator, minimum, maximum));

            FlowAssert.Kinds(expected,
                value.MatchRatioInRange(denominator, minimum, maximum),
                value.RejectRatioInRange(denominator, minimum, maximum),
                value.BreakRatioInRange(denominator, minimum, maximum),
                value.RequireRatioInRange(denominator, minimum, maximum),
                value.EnsureRatioInRange(denominator, minimum, maximum));
        }

        [Theory]
        [InlineData(16, 5, 1, 3, true)]
        [InlineData(4, 5, 1, 3, true)]
        [InlineData(10, 5, 1, 3, false)]
        [InlineData(10, 0, 1, 3, false)]
        public void RatioOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double minimum, double maximum, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioOutsideRange(denominator, minimum, maximum),
                start.RejectRatioOutsideRange(denominator, minimum, maximum),
                start.BreakRatioOutsideRange(denominator, minimum, maximum),
                start.RequireRatioOutsideRange(denominator, minimum, maximum),
                start.EnsureRatioOutsideRange(denominator, minimum, maximum));

            FlowAssert.Kinds(expected,
                value.MatchRatioOutsideRange(denominator, minimum, maximum),
                value.RejectRatioOutsideRange(denominator, minimum, maximum),
                value.BreakRatioOutsideRange(denominator, minimum, maximum),
                value.RequireRatioOutsideRange(denominator, minimum, maximum),
                value.EnsureRatioOutsideRange(denominator, minimum, maximum));
        }

        [Theory]
        [InlineData(2900, 10, new[] { 3000d, 10000d, 15000d }, true)]
        [InlineData(9500, 10, new[] { 3000d, 10000d, 15000d }, true)]
        [InlineData(14000, 10, new[] { 3000d, 10000d, 15000d }, true)]
        [InlineData(5000, 10, new[] { 3000d, 10000d, 15000d }, false)]
        [InlineData(10000, 10, new[] { 3000d, 10000d, 15000d }, false)]
        [InlineData(2900, 10, new double[0], false)]
        public void IsJustBelowAnyThresholdAppliesTheOutcomeOfEveryStepKind(double value, double marginPercent, double[] thresholds, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsJustBelowAnyThreshold(marginPercent, thresholds),
                start.RejectIsJustBelowAnyThreshold(marginPercent, thresholds),
                start.BreakIsJustBelowAnyThreshold(marginPercent, thresholds),
                start.RequireIsJustBelowAnyThreshold(marginPercent, thresholds),
                start.EnsureIsJustBelowAnyThreshold(marginPercent, thresholds));

            FlowAssert.Kinds(expected,
                value.MatchIsJustBelowAnyThreshold(marginPercent, thresholds),
                value.RejectIsJustBelowAnyThreshold(marginPercent, thresholds),
                value.BreakIsJustBelowAnyThreshold(marginPercent, thresholds),
                value.RequireIsJustBelowAnyThreshold(marginPercent, thresholds),
                value.EnsureIsJustBelowAnyThreshold(marginPercent, thresholds));
        }

        [Theory]
        [InlineData(3100, 10, new[] { 3000d, 10000d }, true)]
        [InlineData(10900, 10, new[] { 3000d, 10000d }, true)]
        [InlineData(5000, 10, new[] { 3000d, 10000d }, false)]
        [InlineData(3000, 10, new[] { 3000d, 10000d }, false)]
        [InlineData(3100, 10, new double[0], false)]
        public void IsJustAboveAnyThresholdAppliesTheOutcomeOfEveryStepKind(double value, double marginPercent, double[] thresholds, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsJustAboveAnyThreshold(marginPercent, thresholds),
                start.RejectIsJustAboveAnyThreshold(marginPercent, thresholds),
                start.BreakIsJustAboveAnyThreshold(marginPercent, thresholds),
                start.RequireIsJustAboveAnyThreshold(marginPercent, thresholds),
                start.EnsureIsJustAboveAnyThreshold(marginPercent, thresholds));

            FlowAssert.Kinds(expected,
                value.MatchIsJustAboveAnyThreshold(marginPercent, thresholds),
                value.RejectIsJustAboveAnyThreshold(marginPercent, thresholds),
                value.BreakIsJustAboveAnyThreshold(marginPercent, thresholds),
                value.RequireIsJustAboveAnyThreshold(marginPercent, thresholds),
                value.EnsureIsJustAboveAnyThreshold(marginPercent, thresholds));
        }

        [Theory]
        [InlineData(51.5, -0.1, 51.5, -0.1, 1, true)]
        [InlineData(0, 0, 0, 1, 50, false)]
        [InlineData(0, 0, 0, 1, 100, true)]
        public void WithinRadiusMilesAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double radiusMiles, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                start.RejectWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                start.BreakWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                start.RequireWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                start.EnsureWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles));

            FlowAssert.Kinds(expected,
                value.MatchWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                value.RejectWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                value.BreakWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                value.RequireWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                value.EnsureWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 50, true)]
        [InlineData(51.5, -0.1, 51.5, -0.1, 1, false)]
        [InlineData(0, 0, 0, 1, 100, false)]
        public void OutsideRadiusMilesAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double radiusMiles, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchOutsideRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                start.RejectOutsideRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                start.BreakOutsideRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                start.RequireOutsideRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                start.EnsureOutsideRadiusMiles(longitude1, latitude2, longitude2, radiusMiles));

            FlowAssert.Kinds(expected,
                value.MatchOutsideRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                value.RejectOutsideRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                value.BreakOutsideRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                value.RequireOutsideRadiusMiles(longitude1, latitude2, longitude2, radiusMiles),
                value.EnsureOutsideRadiusMiles(longitude1, latitude2, longitude2, radiusMiles));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 50, true)]
        [InlineData(0, 0, 0, 1, 100, false)]
        [InlineData(0, 0, 0, 1, 69, true)]
        [InlineData(0, 0, 0, 0, 1, false)]
        public void DistanceMilesAboveAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double miles, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDistanceMilesAbove(longitude1, latitude2, longitude2, miles),
                start.RejectDistanceMilesAbove(longitude1, latitude2, longitude2, miles),
                start.BreakDistanceMilesAbove(longitude1, latitude2, longitude2, miles),
                start.RequireDistanceMilesAbove(longitude1, latitude2, longitude2, miles),
                start.EnsureDistanceMilesAbove(longitude1, latitude2, longitude2, miles));

            FlowAssert.Kinds(expected,
                value.MatchDistanceMilesAbove(longitude1, latitude2, longitude2, miles),
                value.RejectDistanceMilesAbove(longitude1, latitude2, longitude2, miles),
                value.BreakDistanceMilesAbove(longitude1, latitude2, longitude2, miles),
                value.RequireDistanceMilesAbove(longitude1, latitude2, longitude2, miles),
                value.EnsureDistanceMilesAbove(longitude1, latitude2, longitude2, miles));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 100, true)]
        [InlineData(0, 0, 0, 1, 50, false)]
        [InlineData(0, 0, 0, 1, 69.2, true)]
        [InlineData(0, 0, 0, 0, 1, true)]
        public void DistanceMilesBelowAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double miles, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDistanceMilesBelow(longitude1, latitude2, longitude2, miles),
                start.RejectDistanceMilesBelow(longitude1, latitude2, longitude2, miles),
                start.BreakDistanceMilesBelow(longitude1, latitude2, longitude2, miles),
                start.RequireDistanceMilesBelow(longitude1, latitude2, longitude2, miles),
                start.EnsureDistanceMilesBelow(longitude1, latitude2, longitude2, miles));

            FlowAssert.Kinds(expected,
                value.MatchDistanceMilesBelow(longitude1, latitude2, longitude2, miles),
                value.RejectDistanceMilesBelow(longitude1, latitude2, longitude2, miles),
                value.BreakDistanceMilesBelow(longitude1, latitude2, longitude2, miles),
                value.RequireDistanceMilesBelow(longitude1, latitude2, longitude2, miles),
                value.EnsureDistanceMilesBelow(longitude1, latitude2, longitude2, miles));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 1, 60, true)]
        [InlineData(0, 0, 0, 1, 2, 60, false)]
        [InlineData(0, 0, 0, 1, 0, 60, true)]
        public void ImpliedSpeedMphAboveAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                start.RejectImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                start.BreakImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                start.RequireImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                start.EnsureImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));

            FlowAssert.Kinds(expected,
                value.MatchImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                value.RejectImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                value.BreakImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                value.RequireImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                value.EnsureImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
        }

        [Theory]
        [InlineData(0, 0, 0, 1, 2, 60, true)]
        [InlineData(0, 0, 0, 1, 1, 60, false)]
        [InlineData(0, 0, 0, 1, 0, 60, false)]
        public void ImpliedSpeedMphBelowAppliesTheOutcomeOfEveryStepKind(double value, double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                start.RejectImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                start.BreakImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                start.RequireImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                start.EnsureImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));

            FlowAssert.Kinds(expected,
                value.MatchImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                value.RejectImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                value.BreakImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                value.RequireImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour),
                value.EnsureImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
        }

        [Theory]
        [InlineData(6, 3, 1.0, true)]
        [InlineData(6, 3, 3.0, false)]
        [InlineData(6, 3, 2.0, false)]
        [InlineData(1, 4, -0.75, true)]
        [InlineData(1, 4, 1.25, false)]
        [InlineData(1, 4, 0.25, false)]
        [InlineData(0, 5, -1.0, true)]
        [InlineData(0, 5, 1.0, false)]
        [InlineData(0, 5, 0.0, false)]
        [InlineData(5, 0, 0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        [InlineData(-6, 3, -3.0, true)]
        [InlineData(-6, 3, -1.0, false)]
        [InlineData(-6, 3, -2.0, false)]
        [InlineData(double.PositiveInfinity, 2, 0, false)]
        public void RatioOfAboveAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioOfAbove(denominator, bound),
                start.RejectRatioOfAbove(denominator, bound),
                start.BreakRatioOfAbove(denominator, bound),
                start.RequireRatioOfAbove(denominator, bound),
                start.EnsureRatioOfAbove(denominator, bound));

            FlowAssert.Kinds(expected,
                value.MatchRatioOfAbove(denominator, bound),
                value.RejectRatioOfAbove(denominator, bound),
                value.BreakRatioOfAbove(denominator, bound),
                value.RequireRatioOfAbove(denominator, bound),
                value.EnsureRatioOfAbove(denominator, bound));
        }

        [Theory]
        [InlineData(6, 3, 3.0, true)]
        [InlineData(6, 3, 1.0, false)]
        [InlineData(6, 3, 2.0, false)]
        [InlineData(1, 4, 1.25, true)]
        [InlineData(1, 4, -0.75, false)]
        [InlineData(1, 4, 0.25, false)]
        [InlineData(0, 5, 1.0, true)]
        [InlineData(0, 5, -1.0, false)]
        [InlineData(0, 5, 0.0, false)]
        [InlineData(5, 0, 0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        [InlineData(-6, 3, -1.0, true)]
        [InlineData(-6, 3, -3.0, false)]
        [InlineData(-6, 3, -2.0, false)]
        [InlineData(double.PositiveInfinity, 2, 0, false)]
        public void RatioOfBelowAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioOfBelow(denominator, bound),
                start.RejectRatioOfBelow(denominator, bound),
                start.BreakRatioOfBelow(denominator, bound),
                start.RequireRatioOfBelow(denominator, bound),
                start.EnsureRatioOfBelow(denominator, bound));

            FlowAssert.Kinds(expected,
                value.MatchRatioOfBelow(denominator, bound),
                value.RejectRatioOfBelow(denominator, bound),
                value.BreakRatioOfBelow(denominator, bound),
                value.RequireRatioOfBelow(denominator, bound),
                value.EnsureRatioOfBelow(denominator, bound));
        }

        [Theory]
        [InlineData(6, 3, 1.0, 3.0, true)]
        [InlineData(6, 3, 3.0, 4.0, false)]
        [InlineData(6, 3, 2.0, 2.0, true)]
        [InlineData(1, 4, -0.75, 1.25, true)]
        [InlineData(1, 4, 1.25, 2.25, false)]
        [InlineData(1, 4, 0.25, 0.25, true)]
        [InlineData(0, 5, -1.0, 1.0, true)]
        [InlineData(0, 5, 1.0, 2.0, false)]
        [InlineData(0, 5, 0.0, 0.0, true)]
        [InlineData(5, 0, -1000, 1000, false)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        [InlineData(-6, 3, -3.0, -1.0, true)]
        [InlineData(-6, 3, -1.0, 0.0, false)]
        [InlineData(-6, 3, -2.0, -2.0, true)]
        [InlineData(double.PositiveInfinity, 2, -1000, 1000, false)]
        public void RatioOfInRangeAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioOfInRange(denominator, lowerBound, upperBound),
                start.RejectRatioOfInRange(denominator, lowerBound, upperBound),
                start.BreakRatioOfInRange(denominator, lowerBound, upperBound),
                start.RequireRatioOfInRange(denominator, lowerBound, upperBound),
                start.EnsureRatioOfInRange(denominator, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchRatioOfInRange(denominator, lowerBound, upperBound),
                value.RejectRatioOfInRange(denominator, lowerBound, upperBound),
                value.BreakRatioOfInRange(denominator, lowerBound, upperBound),
                value.RequireRatioOfInRange(denominator, lowerBound, upperBound),
                value.EnsureRatioOfInRange(denominator, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(6, 3, 3.0, 4.0, true)]
        [InlineData(6, 3, 1.0, 3.0, false)]
        [InlineData(6, 3, 2.0, 2.0, false)]
        [InlineData(1, 4, 1.25, 2.25, true)]
        [InlineData(1, 4, -0.75, 1.25, false)]
        [InlineData(1, 4, 0.25, 0.25, false)]
        [InlineData(0, 5, 1.0, 2.0, true)]
        [InlineData(0, 5, -1.0, 1.0, false)]
        [InlineData(0, 5, 0.0, 0.0, false)]
        [InlineData(5, 0, -1000, 1000, false)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        [InlineData(-6, 3, -1.0, 0.0, true)]
        [InlineData(-6, 3, -3.0, -1.0, false)]
        [InlineData(-6, 3, -2.0, -2.0, false)]
        [InlineData(double.PositiveInfinity, 2, -1000, 1000, false)]
        public void RatioOfOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioOfOutsideRange(denominator, lowerBound, upperBound),
                start.RejectRatioOfOutsideRange(denominator, lowerBound, upperBound),
                start.BreakRatioOfOutsideRange(denominator, lowerBound, upperBound),
                start.RequireRatioOfOutsideRange(denominator, lowerBound, upperBound),
                start.EnsureRatioOfOutsideRange(denominator, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchRatioOfOutsideRange(denominator, lowerBound, upperBound),
                value.RejectRatioOfOutsideRange(denominator, lowerBound, upperBound),
                value.BreakRatioOfOutsideRange(denominator, lowerBound, upperBound),
                value.RequireRatioOfOutsideRange(denominator, lowerBound, upperBound),
                value.EnsureRatioOfOutsideRange(denominator, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(25, 200, 11.5, true)]
        [InlineData(25, 200, 13.5, false)]
        [InlineData(25, 200, 12.5, false)]
        [InlineData(50, 0, 0, false)]
        [InlineData(0, 10, -1.0, true)]
        [InlineData(0, 10, 1.0, false)]
        [InlineData(0, 10, 0.0, false)]
        [InlineData(300, 200, 149.0, true)]
        [InlineData(300, 200, 151.0, false)]
        [InlineData(300, 200, 150.0, false)]
        [InlineData(double.NaN, 10, 0, false)]
        public void PercentOfAboveAppliesTheOutcomeOfEveryStepKind(double value, double total, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentOfAbove(total, bound),
                start.RejectPercentOfAbove(total, bound),
                start.BreakPercentOfAbove(total, bound),
                start.RequirePercentOfAbove(total, bound),
                start.EnsurePercentOfAbove(total, bound));

            FlowAssert.Kinds(expected,
                value.MatchPercentOfAbove(total, bound),
                value.RejectPercentOfAbove(total, bound),
                value.BreakPercentOfAbove(total, bound),
                value.RequirePercentOfAbove(total, bound),
                value.EnsurePercentOfAbove(total, bound));
        }

        [Theory]
        [InlineData(25, 200, 13.5, true)]
        [InlineData(25, 200, 11.5, false)]
        [InlineData(25, 200, 12.5, false)]
        [InlineData(50, 0, 0, false)]
        [InlineData(0, 10, 1.0, true)]
        [InlineData(0, 10, -1.0, false)]
        [InlineData(0, 10, 0.0, false)]
        [InlineData(300, 200, 151.0, true)]
        [InlineData(300, 200, 149.0, false)]
        [InlineData(300, 200, 150.0, false)]
        [InlineData(double.NaN, 10, 0, false)]
        public void PercentOfBelowAppliesTheOutcomeOfEveryStepKind(double value, double total, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentOfBelow(total, bound),
                start.RejectPercentOfBelow(total, bound),
                start.BreakPercentOfBelow(total, bound),
                start.RequirePercentOfBelow(total, bound),
                start.EnsurePercentOfBelow(total, bound));

            FlowAssert.Kinds(expected,
                value.MatchPercentOfBelow(total, bound),
                value.RejectPercentOfBelow(total, bound),
                value.BreakPercentOfBelow(total, bound),
                value.RequirePercentOfBelow(total, bound),
                value.EnsurePercentOfBelow(total, bound));
        }

        [Theory]
        [InlineData(25, 200, 11.5, 13.5, true)]
        [InlineData(25, 200, 13.5, 14.5, false)]
        [InlineData(25, 200, 12.5, 12.5, true)]
        [InlineData(50, 0, -1000, 1000, false)]
        [InlineData(0, 10, -1.0, 1.0, true)]
        [InlineData(0, 10, 1.0, 2.0, false)]
        [InlineData(0, 10, 0.0, 0.0, true)]
        [InlineData(300, 200, 149.0, 151.0, true)]
        [InlineData(300, 200, 151.0, 152.0, false)]
        [InlineData(300, 200, 150.0, 150.0, true)]
        [InlineData(double.NaN, 10, -1000, 1000, false)]
        public void PercentOfInRangeAppliesTheOutcomeOfEveryStepKind(double value, double total, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentOfInRange(total, lowerBound, upperBound),
                start.RejectPercentOfInRange(total, lowerBound, upperBound),
                start.BreakPercentOfInRange(total, lowerBound, upperBound),
                start.RequirePercentOfInRange(total, lowerBound, upperBound),
                start.EnsurePercentOfInRange(total, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchPercentOfInRange(total, lowerBound, upperBound),
                value.RejectPercentOfInRange(total, lowerBound, upperBound),
                value.BreakPercentOfInRange(total, lowerBound, upperBound),
                value.RequirePercentOfInRange(total, lowerBound, upperBound),
                value.EnsurePercentOfInRange(total, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(25, 200, 13.5, 14.5, true)]
        [InlineData(25, 200, 11.5, 13.5, false)]
        [InlineData(25, 200, 12.5, 12.5, false)]
        [InlineData(50, 0, -1000, 1000, false)]
        [InlineData(0, 10, 1.0, 2.0, true)]
        [InlineData(0, 10, -1.0, 1.0, false)]
        [InlineData(0, 10, 0.0, 0.0, false)]
        [InlineData(300, 200, 151.0, 152.0, true)]
        [InlineData(300, 200, 149.0, 151.0, false)]
        [InlineData(300, 200, 150.0, 150.0, false)]
        [InlineData(double.NaN, 10, -1000, 1000, false)]
        public void PercentOfOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double total, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentOfOutsideRange(total, lowerBound, upperBound),
                start.RejectPercentOfOutsideRange(total, lowerBound, upperBound),
                start.BreakPercentOfOutsideRange(total, lowerBound, upperBound),
                start.RequirePercentOfOutsideRange(total, lowerBound, upperBound),
                start.EnsurePercentOfOutsideRange(total, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchPercentOfOutsideRange(total, lowerBound, upperBound),
                value.RejectPercentOfOutsideRange(total, lowerBound, upperBound),
                value.BreakPercentOfOutsideRange(total, lowerBound, upperBound),
                value.RequirePercentOfOutsideRange(total, lowerBound, upperBound),
                value.EnsurePercentOfOutsideRange(total, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(30, 70, -0.7, true)]
        [InlineData(30, 70, 1.3, false)]
        [InlineData(30, 70, 0.3, false)]
        [InlineData(1, 1, -0.5, true)]
        [InlineData(1, 1, 1.5, false)]
        [InlineData(1, 1, 0.5, false)]
        [InlineData(0, 0, 0, false)]
        [InlineData(5, 0, 0.0, true)]
        [InlineData(5, 0, 2.0, false)]
        [InlineData(5, 0, 1.0, false)]
        [InlineData(0, 5, -1.0, true)]
        [InlineData(0, 5, 1.0, false)]
        [InlineData(0, 5, 0.0, false)]
        [InlineData(-1, 1, 0, false)]
        public void ShareOfSumWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShareOfSumWithAbove(other, bound),
                start.RejectShareOfSumWithAbove(other, bound),
                start.BreakShareOfSumWithAbove(other, bound),
                start.RequireShareOfSumWithAbove(other, bound),
                start.EnsureShareOfSumWithAbove(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchShareOfSumWithAbove(other, bound),
                value.RejectShareOfSumWithAbove(other, bound),
                value.BreakShareOfSumWithAbove(other, bound),
                value.RequireShareOfSumWithAbove(other, bound),
                value.EnsureShareOfSumWithAbove(other, bound));
        }

        [Theory]
        [InlineData(30, 70, 1.3, true)]
        [InlineData(30, 70, -0.7, false)]
        [InlineData(30, 70, 0.3, false)]
        [InlineData(1, 1, 1.5, true)]
        [InlineData(1, 1, -0.5, false)]
        [InlineData(1, 1, 0.5, false)]
        [InlineData(0, 0, 0, false)]
        [InlineData(5, 0, 2.0, true)]
        [InlineData(5, 0, 0.0, false)]
        [InlineData(5, 0, 1.0, false)]
        [InlineData(0, 5, 1.0, true)]
        [InlineData(0, 5, -1.0, false)]
        [InlineData(0, 5, 0.0, false)]
        [InlineData(-1, 1, 0, false)]
        public void ShareOfSumWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShareOfSumWithBelow(other, bound),
                start.RejectShareOfSumWithBelow(other, bound),
                start.BreakShareOfSumWithBelow(other, bound),
                start.RequireShareOfSumWithBelow(other, bound),
                start.EnsureShareOfSumWithBelow(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchShareOfSumWithBelow(other, bound),
                value.RejectShareOfSumWithBelow(other, bound),
                value.BreakShareOfSumWithBelow(other, bound),
                value.RequireShareOfSumWithBelow(other, bound),
                value.EnsureShareOfSumWithBelow(other, bound));
        }

        [Theory]
        [InlineData(30, 70, -0.7, 1.3, true)]
        [InlineData(30, 70, 1.3, 2.3, false)]
        [InlineData(30, 70, 0.3, 0.3, true)]
        [InlineData(1, 1, -0.5, 1.5, true)]
        [InlineData(1, 1, 1.5, 2.5, false)]
        [InlineData(1, 1, 0.5, 0.5, true)]
        [InlineData(0, 0, -1000, 1000, false)]
        [InlineData(5, 0, 0.0, 2.0, true)]
        [InlineData(5, 0, 2.0, 3.0, false)]
        [InlineData(5, 0, 1.0, 1.0, true)]
        [InlineData(0, 5, -1.0, 1.0, true)]
        [InlineData(0, 5, 1.0, 2.0, false)]
        [InlineData(0, 5, 0.0, 0.0, true)]
        [InlineData(-1, 1, -1000, 1000, false)]
        public void ShareOfSumWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShareOfSumWithInRange(other, lowerBound, upperBound),
                start.RejectShareOfSumWithInRange(other, lowerBound, upperBound),
                start.BreakShareOfSumWithInRange(other, lowerBound, upperBound),
                start.RequireShareOfSumWithInRange(other, lowerBound, upperBound),
                start.EnsureShareOfSumWithInRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchShareOfSumWithInRange(other, lowerBound, upperBound),
                value.RejectShareOfSumWithInRange(other, lowerBound, upperBound),
                value.BreakShareOfSumWithInRange(other, lowerBound, upperBound),
                value.RequireShareOfSumWithInRange(other, lowerBound, upperBound),
                value.EnsureShareOfSumWithInRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(30, 70, 1.3, 2.3, true)]
        [InlineData(30, 70, -0.7, 1.3, false)]
        [InlineData(30, 70, 0.3, 0.3, false)]
        [InlineData(1, 1, 1.5, 2.5, true)]
        [InlineData(1, 1, -0.5, 1.5, false)]
        [InlineData(1, 1, 0.5, 0.5, false)]
        [InlineData(0, 0, -1000, 1000, false)]
        [InlineData(5, 0, 2.0, 3.0, true)]
        [InlineData(5, 0, 0.0, 2.0, false)]
        [InlineData(5, 0, 1.0, 1.0, false)]
        [InlineData(0, 5, 1.0, 2.0, true)]
        [InlineData(0, 5, -1.0, 1.0, false)]
        [InlineData(0, 5, 0.0, 0.0, false)]
        [InlineData(-1, 1, -1000, 1000, false)]
        public void ShareOfSumWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShareOfSumWithOutsideRange(other, lowerBound, upperBound),
                start.RejectShareOfSumWithOutsideRange(other, lowerBound, upperBound),
                start.BreakShareOfSumWithOutsideRange(other, lowerBound, upperBound),
                start.RequireShareOfSumWithOutsideRange(other, lowerBound, upperBound),
                start.EnsureShareOfSumWithOutsideRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchShareOfSumWithOutsideRange(other, lowerBound, upperBound),
                value.RejectShareOfSumWithOutsideRange(other, lowerBound, upperBound),
                value.BreakShareOfSumWithOutsideRange(other, lowerBound, upperBound),
                value.RequireShareOfSumWithOutsideRange(other, lowerBound, upperBound),
                value.EnsureShareOfSumWithOutsideRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(0.25, -0.25, true)]
        [InlineData(0.25, 1.75, false)]
        [InlineData(0.25, 0.75, false)]
        [InlineData(1, -1.0, true)]
        [InlineData(1, 1.0, false)]
        [InlineData(1, 0.0, false)]
        [InlineData(double.NaN, 0, false)]
        [InlineData(2, -2.0, true)]
        [InlineData(2, 0.0, false)]
        [InlineData(2, -1.0, false)]
        public void ComplementAboveAppliesTheOutcomeOfEveryStepKind(double value, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchComplementAbove(bound),
                start.RejectComplementAbove(bound),
                start.BreakComplementAbove(bound),
                start.RequireComplementAbove(bound),
                start.EnsureComplementAbove(bound));

            FlowAssert.Kinds(expected,
                value.MatchComplementAbove(bound),
                value.RejectComplementAbove(bound),
                value.BreakComplementAbove(bound),
                value.RequireComplementAbove(bound),
                value.EnsureComplementAbove(bound));
        }

        [Theory]
        [InlineData(0.25, 1.75, true)]
        [InlineData(0.25, -0.25, false)]
        [InlineData(0.25, 0.75, false)]
        [InlineData(1, 1.0, true)]
        [InlineData(1, -1.0, false)]
        [InlineData(1, 0.0, false)]
        [InlineData(double.NaN, 0, false)]
        [InlineData(2, 0.0, true)]
        [InlineData(2, -2.0, false)]
        [InlineData(2, -1.0, false)]
        public void ComplementBelowAppliesTheOutcomeOfEveryStepKind(double value, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchComplementBelow(bound),
                start.RejectComplementBelow(bound),
                start.BreakComplementBelow(bound),
                start.RequireComplementBelow(bound),
                start.EnsureComplementBelow(bound));

            FlowAssert.Kinds(expected,
                value.MatchComplementBelow(bound),
                value.RejectComplementBelow(bound),
                value.BreakComplementBelow(bound),
                value.RequireComplementBelow(bound),
                value.EnsureComplementBelow(bound));
        }

        [Theory]
        [InlineData(0.25, -0.25, 1.75, true)]
        [InlineData(0.25, 1.75, 2.75, false)]
        [InlineData(0.25, 0.75, 0.75, true)]
        [InlineData(1, -1.0, 1.0, true)]
        [InlineData(1, 1.0, 2.0, false)]
        [InlineData(1, 0.0, 0.0, true)]
        [InlineData(double.NaN, -1000, 1000, false)]
        [InlineData(2, -2.0, 0.0, true)]
        [InlineData(2, 0.0, 1.0, false)]
        [InlineData(2, -1.0, -1.0, true)]
        public void ComplementInRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchComplementInRange(lowerBound, upperBound),
                start.RejectComplementInRange(lowerBound, upperBound),
                start.BreakComplementInRange(lowerBound, upperBound),
                start.RequireComplementInRange(lowerBound, upperBound),
                start.EnsureComplementInRange(lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchComplementInRange(lowerBound, upperBound),
                value.RejectComplementInRange(lowerBound, upperBound),
                value.BreakComplementInRange(lowerBound, upperBound),
                value.RequireComplementInRange(lowerBound, upperBound),
                value.EnsureComplementInRange(lowerBound, upperBound));
        }

        [Theory]
        [InlineData(0.25, 1.75, 2.75, true)]
        [InlineData(0.25, -0.25, 1.75, false)]
        [InlineData(0.25, 0.75, 0.75, false)]
        [InlineData(1, 1.0, 2.0, true)]
        [InlineData(1, -1.0, 1.0, false)]
        [InlineData(1, 0.0, 0.0, false)]
        [InlineData(double.NaN, -1000, 1000, false)]
        [InlineData(2, 0.0, 1.0, true)]
        [InlineData(2, -2.0, 0.0, false)]
        [InlineData(2, -1.0, -1.0, false)]
        public void ComplementOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchComplementOutsideRange(lowerBound, upperBound),
                start.RejectComplementOutsideRange(lowerBound, upperBound),
                start.BreakComplementOutsideRange(lowerBound, upperBound),
                start.RequireComplementOutsideRange(lowerBound, upperBound),
                start.EnsureComplementOutsideRange(lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchComplementOutsideRange(lowerBound, upperBound),
                value.RejectComplementOutsideRange(lowerBound, upperBound),
                value.BreakComplementOutsideRange(lowerBound, upperBound),
                value.RequireComplementOutsideRange(lowerBound, upperBound),
                value.EnsureComplementOutsideRange(lowerBound, upperBound));
        }

        [Theory]
        [InlineData(100, 100, -1.0, true)]
        [InlineData(100, 100, 1.0, false)]
        [InlineData(100, 10, 1.302585092994046, true)]
        [InlineData(100, 10, 3.302585092994046, false)]
        [InlineData(10, 100, -3.302585092994046, true)]
        [InlineData(10, 100, -1.302585092994046, false)]
        [InlineData(0, 1, 0, false)]
        [InlineData(1, 0, 0, false)]
        [InlineData(-1, 1, 0, false)]
        public void LogRatioOfAboveAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLogRatioOfAbove(denominator, bound),
                start.RejectLogRatioOfAbove(denominator, bound),
                start.BreakLogRatioOfAbove(denominator, bound),
                start.RequireLogRatioOfAbove(denominator, bound),
                start.EnsureLogRatioOfAbove(denominator, bound));

            FlowAssert.Kinds(expected,
                value.MatchLogRatioOfAbove(denominator, bound),
                value.RejectLogRatioOfAbove(denominator, bound),
                value.BreakLogRatioOfAbove(denominator, bound),
                value.RequireLogRatioOfAbove(denominator, bound),
                value.EnsureLogRatioOfAbove(denominator, bound));
        }

        [Theory]
        [InlineData(100, 100, 1.0, true)]
        [InlineData(100, 100, -1.0, false)]
        [InlineData(100, 10, 3.302585092994046, true)]
        [InlineData(100, 10, 1.302585092994046, false)]
        [InlineData(10, 100, -1.302585092994046, true)]
        [InlineData(10, 100, -3.302585092994046, false)]
        [InlineData(0, 1, 0, false)]
        [InlineData(1, 0, 0, false)]
        [InlineData(-1, 1, 0, false)]
        public void LogRatioOfBelowAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLogRatioOfBelow(denominator, bound),
                start.RejectLogRatioOfBelow(denominator, bound),
                start.BreakLogRatioOfBelow(denominator, bound),
                start.RequireLogRatioOfBelow(denominator, bound),
                start.EnsureLogRatioOfBelow(denominator, bound));

            FlowAssert.Kinds(expected,
                value.MatchLogRatioOfBelow(denominator, bound),
                value.RejectLogRatioOfBelow(denominator, bound),
                value.BreakLogRatioOfBelow(denominator, bound),
                value.RequireLogRatioOfBelow(denominator, bound),
                value.EnsureLogRatioOfBelow(denominator, bound));
        }

        [Theory]
        [InlineData(100, 100, -1.0, 1.0, true)]
        [InlineData(100, 100, 1.0, 2.0, false)]
        [InlineData(100, 10, 1.302585092994046, 3.302585092994046, true)]
        [InlineData(100, 10, 3.302585092994046, 4.302585092994046, false)]
        [InlineData(10, 100, -3.302585092994046, -1.302585092994046, true)]
        [InlineData(10, 100, -1.302585092994046, -0.3025850929940459, false)]
        [InlineData(0, 1, -1000, 1000, false)]
        [InlineData(1, 0, -1000, 1000, false)]
        [InlineData(-1, 1, -1000, 1000, false)]
        public void LogRatioOfInRangeAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLogRatioOfInRange(denominator, lowerBound, upperBound),
                start.RejectLogRatioOfInRange(denominator, lowerBound, upperBound),
                start.BreakLogRatioOfInRange(denominator, lowerBound, upperBound),
                start.RequireLogRatioOfInRange(denominator, lowerBound, upperBound),
                start.EnsureLogRatioOfInRange(denominator, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchLogRatioOfInRange(denominator, lowerBound, upperBound),
                value.RejectLogRatioOfInRange(denominator, lowerBound, upperBound),
                value.BreakLogRatioOfInRange(denominator, lowerBound, upperBound),
                value.RequireLogRatioOfInRange(denominator, lowerBound, upperBound),
                value.EnsureLogRatioOfInRange(denominator, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(100, 100, 1.0, 2.0, true)]
        [InlineData(100, 100, -1.0, 1.0, false)]
        [InlineData(100, 10, 3.302585092994046, 4.302585092994046, true)]
        [InlineData(100, 10, 1.302585092994046, 3.302585092994046, false)]
        [InlineData(10, 100, -1.302585092994046, -0.3025850929940459, true)]
        [InlineData(10, 100, -3.302585092994046, -1.302585092994046, false)]
        [InlineData(0, 1, -1000, 1000, false)]
        [InlineData(1, 0, -1000, 1000, false)]
        [InlineData(-1, 1, -1000, 1000, false)]
        public void LogRatioOfOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLogRatioOfOutsideRange(denominator, lowerBound, upperBound),
                start.RejectLogRatioOfOutsideRange(denominator, lowerBound, upperBound),
                start.BreakLogRatioOfOutsideRange(denominator, lowerBound, upperBound),
                start.RequireLogRatioOfOutsideRange(denominator, lowerBound, upperBound),
                start.EnsureLogRatioOfOutsideRange(denominator, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchLogRatioOfOutsideRange(denominator, lowerBound, upperBound),
                value.RejectLogRatioOfOutsideRange(denominator, lowerBound, upperBound),
                value.BreakLogRatioOfOutsideRange(denominator, lowerBound, upperBound),
                value.RequireLogRatioOfOutsideRange(denominator, lowerBound, upperBound),
                value.EnsureLogRatioOfOutsideRange(denominator, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(10, 5, 3, 6, 3.0, true)]
        [InlineData(10, 5, 3, 6, 5.0, false)]
        [InlineData(10, 5, 3, 6, 4.0, false)]
        [InlineData(10, 0, 3, 6, 0, false)]
        [InlineData(10, 5, 0, 1, 0, false)]
        [InlineData(10, 5, 1, 0, 0, false)]
        public void RatioOfRatiosAboveAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double otherNumerator, double otherDenominator, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioOfRatiosAbove(denominator, otherNumerator, otherDenominator, bound),
                start.RejectRatioOfRatiosAbove(denominator, otherNumerator, otherDenominator, bound),
                start.BreakRatioOfRatiosAbove(denominator, otherNumerator, otherDenominator, bound),
                start.RequireRatioOfRatiosAbove(denominator, otherNumerator, otherDenominator, bound),
                start.EnsureRatioOfRatiosAbove(denominator, otherNumerator, otherDenominator, bound));

            FlowAssert.Kinds(expected,
                value.MatchRatioOfRatiosAbove(denominator, otherNumerator, otherDenominator, bound),
                value.RejectRatioOfRatiosAbove(denominator, otherNumerator, otherDenominator, bound),
                value.BreakRatioOfRatiosAbove(denominator, otherNumerator, otherDenominator, bound),
                value.RequireRatioOfRatiosAbove(denominator, otherNumerator, otherDenominator, bound),
                value.EnsureRatioOfRatiosAbove(denominator, otherNumerator, otherDenominator, bound));
        }

        [Theory]
        [InlineData(10, 5, 3, 6, 5.0, true)]
        [InlineData(10, 5, 3, 6, 3.0, false)]
        [InlineData(10, 5, 3, 6, 4.0, false)]
        [InlineData(10, 0, 3, 6, 0, false)]
        [InlineData(10, 5, 0, 1, 0, false)]
        [InlineData(10, 5, 1, 0, 0, false)]
        public void RatioOfRatiosBelowAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double otherNumerator, double otherDenominator, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound),
                start.RejectRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound),
                start.BreakRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound),
                start.RequireRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound),
                start.EnsureRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound));

            FlowAssert.Kinds(expected,
                value.MatchRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound),
                value.RejectRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound),
                value.BreakRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound),
                value.RequireRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound),
                value.EnsureRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound));
        }

        [Theory]
        [InlineData(10, 5, 3, 6, 3.0, 5.0, true)]
        [InlineData(10, 5, 3, 6, 5.0, 6.0, false)]
        [InlineData(10, 5, 3, 6, 4.0, 4.0, true)]
        [InlineData(10, 0, 3, 6, -1000, 1000, false)]
        [InlineData(10, 5, 0, 1, -1000, 1000, false)]
        [InlineData(10, 5, 1, 0, -1000, 1000, false)]
        public void RatioOfRatiosInRangeAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                start.RejectRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                start.BreakRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                start.RequireRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                start.EnsureRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                value.RejectRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                value.BreakRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                value.RequireRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                value.EnsureRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(10, 5, 3, 6, 5.0, 6.0, true)]
        [InlineData(10, 5, 3, 6, 3.0, 5.0, false)]
        [InlineData(10, 5, 3, 6, 4.0, 4.0, false)]
        [InlineData(10, 0, 3, 6, -1000, 1000, false)]
        [InlineData(10, 5, 0, 1, -1000, 1000, false)]
        [InlineData(10, 5, 1, 0, -1000, 1000, false)]
        public void RatioOfRatiosOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                start.RejectRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                start.BreakRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                start.RequireRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                start.EnsureRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                value.RejectRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                value.BreakRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                value.RequireRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound),
                value.EnsureRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(4, 8, 1.0, true)]
        [InlineData(4, 8, 3.0, false)]
        [InlineData(4, 8, 2.0, false)]
        [InlineData(0, 5, 0, false)]
        [InlineData(8, 4, -0.5, true)]
        [InlineData(8, 4, 1.5, false)]
        [InlineData(8, 4, 0.5, false)]
        public void InverseRatioOfAboveAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchInverseRatioOfAbove(other, bound),
                start.RejectInverseRatioOfAbove(other, bound),
                start.BreakInverseRatioOfAbove(other, bound),
                start.RequireInverseRatioOfAbove(other, bound),
                start.EnsureInverseRatioOfAbove(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchInverseRatioOfAbove(other, bound),
                value.RejectInverseRatioOfAbove(other, bound),
                value.BreakInverseRatioOfAbove(other, bound),
                value.RequireInverseRatioOfAbove(other, bound),
                value.EnsureInverseRatioOfAbove(other, bound));
        }

        [Theory]
        [InlineData(4, 8, 3.0, true)]
        [InlineData(4, 8, 1.0, false)]
        [InlineData(4, 8, 2.0, false)]
        [InlineData(0, 5, 0, false)]
        [InlineData(8, 4, 1.5, true)]
        [InlineData(8, 4, -0.5, false)]
        [InlineData(8, 4, 0.5, false)]
        public void InverseRatioOfBelowAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchInverseRatioOfBelow(other, bound),
                start.RejectInverseRatioOfBelow(other, bound),
                start.BreakInverseRatioOfBelow(other, bound),
                start.RequireInverseRatioOfBelow(other, bound),
                start.EnsureInverseRatioOfBelow(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchInverseRatioOfBelow(other, bound),
                value.RejectInverseRatioOfBelow(other, bound),
                value.BreakInverseRatioOfBelow(other, bound),
                value.RequireInverseRatioOfBelow(other, bound),
                value.EnsureInverseRatioOfBelow(other, bound));
        }

        [Theory]
        [InlineData(4, 8, 1.0, 3.0, true)]
        [InlineData(4, 8, 3.0, 4.0, false)]
        [InlineData(4, 8, 2.0, 2.0, true)]
        [InlineData(0, 5, -1000, 1000, false)]
        [InlineData(8, 4, -0.5, 1.5, true)]
        [InlineData(8, 4, 1.5, 2.5, false)]
        [InlineData(8, 4, 0.5, 0.5, true)]
        public void InverseRatioOfInRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchInverseRatioOfInRange(other, lowerBound, upperBound),
                start.RejectInverseRatioOfInRange(other, lowerBound, upperBound),
                start.BreakInverseRatioOfInRange(other, lowerBound, upperBound),
                start.RequireInverseRatioOfInRange(other, lowerBound, upperBound),
                start.EnsureInverseRatioOfInRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchInverseRatioOfInRange(other, lowerBound, upperBound),
                value.RejectInverseRatioOfInRange(other, lowerBound, upperBound),
                value.BreakInverseRatioOfInRange(other, lowerBound, upperBound),
                value.RequireInverseRatioOfInRange(other, lowerBound, upperBound),
                value.EnsureInverseRatioOfInRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(4, 8, 3.0, 4.0, true)]
        [InlineData(4, 8, 1.0, 3.0, false)]
        [InlineData(4, 8, 2.0, 2.0, false)]
        [InlineData(0, 5, -1000, 1000, false)]
        [InlineData(8, 4, 1.5, 2.5, true)]
        [InlineData(8, 4, -0.5, 1.5, false)]
        [InlineData(8, 4, 0.5, 0.5, false)]
        public void InverseRatioOfOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchInverseRatioOfOutsideRange(other, lowerBound, upperBound),
                start.RejectInverseRatioOfOutsideRange(other, lowerBound, upperBound),
                start.BreakInverseRatioOfOutsideRange(other, lowerBound, upperBound),
                start.RequireInverseRatioOfOutsideRange(other, lowerBound, upperBound),
                start.EnsureInverseRatioOfOutsideRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchInverseRatioOfOutsideRange(other, lowerBound, upperBound),
                value.RejectInverseRatioOfOutsideRange(other, lowerBound, upperBound),
                value.BreakInverseRatioOfOutsideRange(other, lowerBound, upperBound),
                value.RequireInverseRatioOfOutsideRange(other, lowerBound, upperBound),
                value.EnsureInverseRatioOfOutsideRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(10, 4, 5.0, true)]
        [InlineData(10, 4, 7.0, false)]
        [InlineData(10, 4, 6.0, false)]
        [InlineData(4, 10, -7.0, true)]
        [InlineData(4, 10, -5.0, false)]
        [InlineData(4, 10, -6.0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        public void DifferenceFromAboveAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDifferenceFromAbove(other, bound),
                start.RejectDifferenceFromAbove(other, bound),
                start.BreakDifferenceFromAbove(other, bound),
                start.RequireDifferenceFromAbove(other, bound),
                start.EnsureDifferenceFromAbove(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchDifferenceFromAbove(other, bound),
                value.RejectDifferenceFromAbove(other, bound),
                value.BreakDifferenceFromAbove(other, bound),
                value.RequireDifferenceFromAbove(other, bound),
                value.EnsureDifferenceFromAbove(other, bound));
        }

        [Theory]
        [InlineData(10, 4, 7.0, true)]
        [InlineData(10, 4, 5.0, false)]
        [InlineData(10, 4, 6.0, false)]
        [InlineData(4, 10, -5.0, true)]
        [InlineData(4, 10, -7.0, false)]
        [InlineData(4, 10, -6.0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        public void DifferenceFromBelowAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDifferenceFromBelow(other, bound),
                start.RejectDifferenceFromBelow(other, bound),
                start.BreakDifferenceFromBelow(other, bound),
                start.RequireDifferenceFromBelow(other, bound),
                start.EnsureDifferenceFromBelow(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchDifferenceFromBelow(other, bound),
                value.RejectDifferenceFromBelow(other, bound),
                value.BreakDifferenceFromBelow(other, bound),
                value.RequireDifferenceFromBelow(other, bound),
                value.EnsureDifferenceFromBelow(other, bound));
        }

        [Theory]
        [InlineData(10, 4, 5.0, 7.0, true)]
        [InlineData(10, 4, 7.0, 8.0, false)]
        [InlineData(10, 4, 6.0, 6.0, true)]
        [InlineData(4, 10, -7.0, -5.0, true)]
        [InlineData(4, 10, -5.0, -4.0, false)]
        [InlineData(4, 10, -6.0, -6.0, true)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        public void DifferenceFromInRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDifferenceFromInRange(other, lowerBound, upperBound),
                start.RejectDifferenceFromInRange(other, lowerBound, upperBound),
                start.BreakDifferenceFromInRange(other, lowerBound, upperBound),
                start.RequireDifferenceFromInRange(other, lowerBound, upperBound),
                start.EnsureDifferenceFromInRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchDifferenceFromInRange(other, lowerBound, upperBound),
                value.RejectDifferenceFromInRange(other, lowerBound, upperBound),
                value.BreakDifferenceFromInRange(other, lowerBound, upperBound),
                value.RequireDifferenceFromInRange(other, lowerBound, upperBound),
                value.EnsureDifferenceFromInRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(10, 4, 7.0, 8.0, true)]
        [InlineData(10, 4, 5.0, 7.0, false)]
        [InlineData(10, 4, 6.0, 6.0, false)]
        [InlineData(4, 10, -5.0, -4.0, true)]
        [InlineData(4, 10, -7.0, -5.0, false)]
        [InlineData(4, 10, -6.0, -6.0, false)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        public void DifferenceFromOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.RejectDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.BreakDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.RequireDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.EnsureDifferenceFromOutsideRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.RejectDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.BreakDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.RequireDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.EnsureDifferenceFromOutsideRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(4, 10, 5.0, true)]
        [InlineData(4, 10, 7.0, false)]
        [InlineData(4, 10, 6.0, false)]
        [InlineData(10, 4, 5.0, true)]
        [InlineData(10, 4, 7.0, false)]
        [InlineData(10, 4, 6.0, false)]
        [InlineData(5, 5, -1.0, true)]
        [InlineData(5, 5, 1.0, false)]
        [InlineData(5, 5, 0.0, false)]
        public void AbsoluteDifferenceFromAboveAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchAbsoluteDifferenceFromAbove(other, bound),
                start.RejectAbsoluteDifferenceFromAbove(other, bound),
                start.BreakAbsoluteDifferenceFromAbove(other, bound),
                start.RequireAbsoluteDifferenceFromAbove(other, bound),
                start.EnsureAbsoluteDifferenceFromAbove(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchAbsoluteDifferenceFromAbove(other, bound),
                value.RejectAbsoluteDifferenceFromAbove(other, bound),
                value.BreakAbsoluteDifferenceFromAbove(other, bound),
                value.RequireAbsoluteDifferenceFromAbove(other, bound),
                value.EnsureAbsoluteDifferenceFromAbove(other, bound));
        }

        [Theory]
        [InlineData(4, 10, 7.0, true)]
        [InlineData(4, 10, 5.0, false)]
        [InlineData(4, 10, 6.0, false)]
        [InlineData(10, 4, 7.0, true)]
        [InlineData(10, 4, 5.0, false)]
        [InlineData(10, 4, 6.0, false)]
        [InlineData(5, 5, 1.0, true)]
        [InlineData(5, 5, -1.0, false)]
        [InlineData(5, 5, 0.0, false)]
        public void AbsoluteDifferenceFromBelowAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchAbsoluteDifferenceFromBelow(other, bound),
                start.RejectAbsoluteDifferenceFromBelow(other, bound),
                start.BreakAbsoluteDifferenceFromBelow(other, bound),
                start.RequireAbsoluteDifferenceFromBelow(other, bound),
                start.EnsureAbsoluteDifferenceFromBelow(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchAbsoluteDifferenceFromBelow(other, bound),
                value.RejectAbsoluteDifferenceFromBelow(other, bound),
                value.BreakAbsoluteDifferenceFromBelow(other, bound),
                value.RequireAbsoluteDifferenceFromBelow(other, bound),
                value.EnsureAbsoluteDifferenceFromBelow(other, bound));
        }

        [Theory]
        [InlineData(4, 10, 5.0, 7.0, true)]
        [InlineData(4, 10, 7.0, 8.0, false)]
        [InlineData(4, 10, 6.0, 6.0, true)]
        [InlineData(10, 4, 5.0, 7.0, true)]
        [InlineData(10, 4, 7.0, 8.0, false)]
        [InlineData(10, 4, 6.0, 6.0, true)]
        [InlineData(5, 5, -1.0, 1.0, true)]
        [InlineData(5, 5, 1.0, 2.0, false)]
        [InlineData(5, 5, 0.0, 0.0, true)]
        public void AbsoluteDifferenceFromInRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchAbsoluteDifferenceFromInRange(other, lowerBound, upperBound),
                start.RejectAbsoluteDifferenceFromInRange(other, lowerBound, upperBound),
                start.BreakAbsoluteDifferenceFromInRange(other, lowerBound, upperBound),
                start.RequireAbsoluteDifferenceFromInRange(other, lowerBound, upperBound),
                start.EnsureAbsoluteDifferenceFromInRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchAbsoluteDifferenceFromInRange(other, lowerBound, upperBound),
                value.RejectAbsoluteDifferenceFromInRange(other, lowerBound, upperBound),
                value.BreakAbsoluteDifferenceFromInRange(other, lowerBound, upperBound),
                value.RequireAbsoluteDifferenceFromInRange(other, lowerBound, upperBound),
                value.EnsureAbsoluteDifferenceFromInRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(4, 10, 7.0, 8.0, true)]
        [InlineData(4, 10, 5.0, 7.0, false)]
        [InlineData(4, 10, 6.0, 6.0, false)]
        [InlineData(10, 4, 7.0, 8.0, true)]
        [InlineData(10, 4, 5.0, 7.0, false)]
        [InlineData(10, 4, 6.0, 6.0, false)]
        [InlineData(5, 5, 1.0, 2.0, true)]
        [InlineData(5, 5, -1.0, 1.0, false)]
        [InlineData(5, 5, 0.0, 0.0, false)]
        public void AbsoluteDifferenceFromOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.RejectAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.BreakAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.RequireAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.EnsureAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.RejectAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.BreakAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.RequireAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.EnsureAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(150, 50, 99.0, true)]
        [InlineData(150, 50, 101.0, false)]
        [InlineData(150, 50, 100.0, false)]
        [InlineData(100, 100, -1.0, true)]
        [InlineData(100, 100, 1.0, false)]
        [InlineData(100, 100, 0.0, false)]
        [InlineData(0, 0, -1.0, true)]
        [InlineData(0, 0, 1.0, false)]
        [InlineData(0, 0, 0.0, false)]
        [InlineData(0, 10, 199.0, true)]
        [InlineData(0, 10, 201.0, false)]
        [InlineData(0, 10, 200.0, false)]
        [InlineData(-50, 50, 199.0, true)]
        [InlineData(-50, 50, 201.0, false)]
        [InlineData(-50, 50, 200.0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        public void PercentDifferenceFromAboveAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentDifferenceFromAbove(other, bound),
                start.RejectPercentDifferenceFromAbove(other, bound),
                start.BreakPercentDifferenceFromAbove(other, bound),
                start.RequirePercentDifferenceFromAbove(other, bound),
                start.EnsurePercentDifferenceFromAbove(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchPercentDifferenceFromAbove(other, bound),
                value.RejectPercentDifferenceFromAbove(other, bound),
                value.BreakPercentDifferenceFromAbove(other, bound),
                value.RequirePercentDifferenceFromAbove(other, bound),
                value.EnsurePercentDifferenceFromAbove(other, bound));
        }

        [Theory]
        [InlineData(150, 50, 101.0, true)]
        [InlineData(150, 50, 99.0, false)]
        [InlineData(150, 50, 100.0, false)]
        [InlineData(100, 100, 1.0, true)]
        [InlineData(100, 100, -1.0, false)]
        [InlineData(100, 100, 0.0, false)]
        [InlineData(0, 0, 1.0, true)]
        [InlineData(0, 0, -1.0, false)]
        [InlineData(0, 0, 0.0, false)]
        [InlineData(0, 10, 201.0, true)]
        [InlineData(0, 10, 199.0, false)]
        [InlineData(0, 10, 200.0, false)]
        [InlineData(-50, 50, 201.0, true)]
        [InlineData(-50, 50, 199.0, false)]
        [InlineData(-50, 50, 200.0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        public void PercentDifferenceFromBelowAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentDifferenceFromBelow(other, bound),
                start.RejectPercentDifferenceFromBelow(other, bound),
                start.BreakPercentDifferenceFromBelow(other, bound),
                start.RequirePercentDifferenceFromBelow(other, bound),
                start.EnsurePercentDifferenceFromBelow(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchPercentDifferenceFromBelow(other, bound),
                value.RejectPercentDifferenceFromBelow(other, bound),
                value.BreakPercentDifferenceFromBelow(other, bound),
                value.RequirePercentDifferenceFromBelow(other, bound),
                value.EnsurePercentDifferenceFromBelow(other, bound));
        }

        [Theory]
        [InlineData(150, 50, 99.0, 101.0, true)]
        [InlineData(150, 50, 101.0, 102.0, false)]
        [InlineData(150, 50, 100.0, 100.0, true)]
        [InlineData(100, 100, -1.0, 1.0, true)]
        [InlineData(100, 100, 1.0, 2.0, false)]
        [InlineData(100, 100, 0.0, 0.0, true)]
        [InlineData(0, 0, -1.0, 1.0, true)]
        [InlineData(0, 0, 1.0, 2.0, false)]
        [InlineData(0, 0, 0.0, 0.0, true)]
        [InlineData(0, 10, 199.0, 201.0, true)]
        [InlineData(0, 10, 201.0, 202.0, false)]
        [InlineData(0, 10, 200.0, 200.0, true)]
        [InlineData(-50, 50, 199.0, 201.0, true)]
        [InlineData(-50, 50, 201.0, 202.0, false)]
        [InlineData(-50, 50, 200.0, 200.0, true)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        public void PercentDifferenceFromInRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentDifferenceFromInRange(other, lowerBound, upperBound),
                start.RejectPercentDifferenceFromInRange(other, lowerBound, upperBound),
                start.BreakPercentDifferenceFromInRange(other, lowerBound, upperBound),
                start.RequirePercentDifferenceFromInRange(other, lowerBound, upperBound),
                start.EnsurePercentDifferenceFromInRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchPercentDifferenceFromInRange(other, lowerBound, upperBound),
                value.RejectPercentDifferenceFromInRange(other, lowerBound, upperBound),
                value.BreakPercentDifferenceFromInRange(other, lowerBound, upperBound),
                value.RequirePercentDifferenceFromInRange(other, lowerBound, upperBound),
                value.EnsurePercentDifferenceFromInRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(150, 50, 101.0, 102.0, true)]
        [InlineData(150, 50, 99.0, 101.0, false)]
        [InlineData(150, 50, 100.0, 100.0, false)]
        [InlineData(100, 100, 1.0, 2.0, true)]
        [InlineData(100, 100, -1.0, 1.0, false)]
        [InlineData(100, 100, 0.0, 0.0, false)]
        [InlineData(0, 0, 1.0, 2.0, true)]
        [InlineData(0, 0, -1.0, 1.0, false)]
        [InlineData(0, 0, 0.0, 0.0, false)]
        [InlineData(0, 10, 201.0, 202.0, true)]
        [InlineData(0, 10, 199.0, 201.0, false)]
        [InlineData(0, 10, 200.0, 200.0, false)]
        [InlineData(-50, 50, 201.0, 202.0, true)]
        [InlineData(-50, 50, 199.0, 201.0, false)]
        [InlineData(-50, 50, 200.0, 200.0, false)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        public void PercentDifferenceFromOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.RejectPercentDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.BreakPercentDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.RequirePercentDifferenceFromOutsideRange(other, lowerBound, upperBound),
                start.EnsurePercentDifferenceFromOutsideRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchPercentDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.RejectPercentDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.BreakPercentDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.RequirePercentDifferenceFromOutsideRange(other, lowerBound, upperBound),
                value.EnsurePercentDifferenceFromOutsideRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(150, 100, -0.5, true)]
        [InlineData(150, 100, 1.5, false)]
        [InlineData(150, 100, 0.5, false)]
        [InlineData(50, 100, -1.5, true)]
        [InlineData(50, 100, 0.5, false)]
        [InlineData(50, 100, -0.5, false)]
        [InlineData(5, 0, 0, false)]
        [InlineData(0, 0, 0, false)]
        public void RelativeChangeFromAboveAppliesTheOutcomeOfEveryStepKind(double value, double previous, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRelativeChangeFromAbove(previous, bound),
                start.RejectRelativeChangeFromAbove(previous, bound),
                start.BreakRelativeChangeFromAbove(previous, bound),
                start.RequireRelativeChangeFromAbove(previous, bound),
                start.EnsureRelativeChangeFromAbove(previous, bound));

            FlowAssert.Kinds(expected,
                value.MatchRelativeChangeFromAbove(previous, bound),
                value.RejectRelativeChangeFromAbove(previous, bound),
                value.BreakRelativeChangeFromAbove(previous, bound),
                value.RequireRelativeChangeFromAbove(previous, bound),
                value.EnsureRelativeChangeFromAbove(previous, bound));
        }

        [Theory]
        [InlineData(150, 100, 1.5, true)]
        [InlineData(150, 100, -0.5, false)]
        [InlineData(150, 100, 0.5, false)]
        [InlineData(50, 100, 0.5, true)]
        [InlineData(50, 100, -1.5, false)]
        [InlineData(50, 100, -0.5, false)]
        [InlineData(5, 0, 0, false)]
        [InlineData(0, 0, 0, false)]
        public void RelativeChangeFromBelowAppliesTheOutcomeOfEveryStepKind(double value, double previous, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRelativeChangeFromBelow(previous, bound),
                start.RejectRelativeChangeFromBelow(previous, bound),
                start.BreakRelativeChangeFromBelow(previous, bound),
                start.RequireRelativeChangeFromBelow(previous, bound),
                start.EnsureRelativeChangeFromBelow(previous, bound));

            FlowAssert.Kinds(expected,
                value.MatchRelativeChangeFromBelow(previous, bound),
                value.RejectRelativeChangeFromBelow(previous, bound),
                value.BreakRelativeChangeFromBelow(previous, bound),
                value.RequireRelativeChangeFromBelow(previous, bound),
                value.EnsureRelativeChangeFromBelow(previous, bound));
        }

        [Theory]
        [InlineData(150, 100, -0.5, 1.5, true)]
        [InlineData(150, 100, 1.5, 2.5, false)]
        [InlineData(150, 100, 0.5, 0.5, true)]
        [InlineData(50, 100, -1.5, 0.5, true)]
        [InlineData(50, 100, 0.5, 1.5, false)]
        [InlineData(50, 100, -0.5, -0.5, true)]
        [InlineData(5, 0, -1000, 1000, false)]
        [InlineData(0, 0, -1000, 1000, false)]
        public void RelativeChangeFromInRangeAppliesTheOutcomeOfEveryStepKind(double value, double previous, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRelativeChangeFromInRange(previous, lowerBound, upperBound),
                start.RejectRelativeChangeFromInRange(previous, lowerBound, upperBound),
                start.BreakRelativeChangeFromInRange(previous, lowerBound, upperBound),
                start.RequireRelativeChangeFromInRange(previous, lowerBound, upperBound),
                start.EnsureRelativeChangeFromInRange(previous, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchRelativeChangeFromInRange(previous, lowerBound, upperBound),
                value.RejectRelativeChangeFromInRange(previous, lowerBound, upperBound),
                value.BreakRelativeChangeFromInRange(previous, lowerBound, upperBound),
                value.RequireRelativeChangeFromInRange(previous, lowerBound, upperBound),
                value.EnsureRelativeChangeFromInRange(previous, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(150, 100, 1.5, 2.5, true)]
        [InlineData(150, 100, -0.5, 1.5, false)]
        [InlineData(150, 100, 0.5, 0.5, false)]
        [InlineData(50, 100, 0.5, 1.5, true)]
        [InlineData(50, 100, -1.5, 0.5, false)]
        [InlineData(50, 100, -0.5, -0.5, false)]
        [InlineData(5, 0, -1000, 1000, false)]
        [InlineData(0, 0, -1000, 1000, false)]
        public void RelativeChangeFromOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double previous, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRelativeChangeFromOutsideRange(previous, lowerBound, upperBound),
                start.RejectRelativeChangeFromOutsideRange(previous, lowerBound, upperBound),
                start.BreakRelativeChangeFromOutsideRange(previous, lowerBound, upperBound),
                start.RequireRelativeChangeFromOutsideRange(previous, lowerBound, upperBound),
                start.EnsureRelativeChangeFromOutsideRange(previous, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchRelativeChangeFromOutsideRange(previous, lowerBound, upperBound),
                value.RejectRelativeChangeFromOutsideRange(previous, lowerBound, upperBound),
                value.BreakRelativeChangeFromOutsideRange(previous, lowerBound, upperBound),
                value.RequireRelativeChangeFromOutsideRange(previous, lowerBound, upperBound),
                value.EnsureRelativeChangeFromOutsideRange(previous, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(150, 100, 0.5, true)]
        [InlineData(150, 100, 2.5, false)]
        [InlineData(150, 100, 1.5, false)]
        [InlineData(50, 100, -0.5, true)]
        [InlineData(50, 100, 1.5, false)]
        [InlineData(50, 100, 0.5, false)]
        [InlineData(5, 0, 0, false)]
        public void GrowthFactorFromAboveAppliesTheOutcomeOfEveryStepKind(double value, double previous, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchGrowthFactorFromAbove(previous, bound),
                start.RejectGrowthFactorFromAbove(previous, bound),
                start.BreakGrowthFactorFromAbove(previous, bound),
                start.RequireGrowthFactorFromAbove(previous, bound),
                start.EnsureGrowthFactorFromAbove(previous, bound));

            FlowAssert.Kinds(expected,
                value.MatchGrowthFactorFromAbove(previous, bound),
                value.RejectGrowthFactorFromAbove(previous, bound),
                value.BreakGrowthFactorFromAbove(previous, bound),
                value.RequireGrowthFactorFromAbove(previous, bound),
                value.EnsureGrowthFactorFromAbove(previous, bound));
        }

        [Theory]
        [InlineData(150, 100, 2.5, true)]
        [InlineData(150, 100, 0.5, false)]
        [InlineData(150, 100, 1.5, false)]
        [InlineData(50, 100, 1.5, true)]
        [InlineData(50, 100, -0.5, false)]
        [InlineData(50, 100, 0.5, false)]
        [InlineData(5, 0, 0, false)]
        public void GrowthFactorFromBelowAppliesTheOutcomeOfEveryStepKind(double value, double previous, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchGrowthFactorFromBelow(previous, bound),
                start.RejectGrowthFactorFromBelow(previous, bound),
                start.BreakGrowthFactorFromBelow(previous, bound),
                start.RequireGrowthFactorFromBelow(previous, bound),
                start.EnsureGrowthFactorFromBelow(previous, bound));

            FlowAssert.Kinds(expected,
                value.MatchGrowthFactorFromBelow(previous, bound),
                value.RejectGrowthFactorFromBelow(previous, bound),
                value.BreakGrowthFactorFromBelow(previous, bound),
                value.RequireGrowthFactorFromBelow(previous, bound),
                value.EnsureGrowthFactorFromBelow(previous, bound));
        }

        [Theory]
        [InlineData(150, 100, 0.5, 2.5, true)]
        [InlineData(150, 100, 2.5, 3.5, false)]
        [InlineData(150, 100, 1.5, 1.5, true)]
        [InlineData(50, 100, -0.5, 1.5, true)]
        [InlineData(50, 100, 1.5, 2.5, false)]
        [InlineData(50, 100, 0.5, 0.5, true)]
        [InlineData(5, 0, -1000, 1000, false)]
        public void GrowthFactorFromInRangeAppliesTheOutcomeOfEveryStepKind(double value, double previous, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchGrowthFactorFromInRange(previous, lowerBound, upperBound),
                start.RejectGrowthFactorFromInRange(previous, lowerBound, upperBound),
                start.BreakGrowthFactorFromInRange(previous, lowerBound, upperBound),
                start.RequireGrowthFactorFromInRange(previous, lowerBound, upperBound),
                start.EnsureGrowthFactorFromInRange(previous, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchGrowthFactorFromInRange(previous, lowerBound, upperBound),
                value.RejectGrowthFactorFromInRange(previous, lowerBound, upperBound),
                value.BreakGrowthFactorFromInRange(previous, lowerBound, upperBound),
                value.RequireGrowthFactorFromInRange(previous, lowerBound, upperBound),
                value.EnsureGrowthFactorFromInRange(previous, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(150, 100, 2.5, 3.5, true)]
        [InlineData(150, 100, 0.5, 2.5, false)]
        [InlineData(150, 100, 1.5, 1.5, false)]
        [InlineData(50, 100, 1.5, 2.5, true)]
        [InlineData(50, 100, -0.5, 1.5, false)]
        [InlineData(50, 100, 0.5, 0.5, false)]
        [InlineData(5, 0, -1000, 1000, false)]
        public void GrowthFactorFromOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double previous, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchGrowthFactorFromOutsideRange(previous, lowerBound, upperBound),
                start.RejectGrowthFactorFromOutsideRange(previous, lowerBound, upperBound),
                start.BreakGrowthFactorFromOutsideRange(previous, lowerBound, upperBound),
                start.RequireGrowthFactorFromOutsideRange(previous, lowerBound, upperBound),
                start.EnsureGrowthFactorFromOutsideRange(previous, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchGrowthFactorFromOutsideRange(previous, lowerBound, upperBound),
                value.RejectGrowthFactorFromOutsideRange(previous, lowerBound, upperBound),
                value.BreakGrowthFactorFromOutsideRange(previous, lowerBound, upperBound),
                value.RequireGrowthFactorFromOutsideRange(previous, lowerBound, upperBound),
                value.EnsureGrowthFactorFromOutsideRange(previous, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(10, 4, 3, 1.0, true)]
        [InlineData(10, 4, 3, 3.0, false)]
        [InlineData(10, 4, 3, 2.0, false)]
        [InlineData(4, 10, 3, -3.0, true)]
        [InlineData(4, 10, 3, -1.0, false)]
        [InlineData(4, 10, 3, -2.0, false)]
        [InlineData(1, 1, 0, 0, false)]
        public void SlopeFromAboveAppliesTheOutcomeOfEveryStepKind(double value, double previous, double elapsed, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSlopeFromAbove(previous, elapsed, bound),
                start.RejectSlopeFromAbove(previous, elapsed, bound),
                start.BreakSlopeFromAbove(previous, elapsed, bound),
                start.RequireSlopeFromAbove(previous, elapsed, bound),
                start.EnsureSlopeFromAbove(previous, elapsed, bound));

            FlowAssert.Kinds(expected,
                value.MatchSlopeFromAbove(previous, elapsed, bound),
                value.RejectSlopeFromAbove(previous, elapsed, bound),
                value.BreakSlopeFromAbove(previous, elapsed, bound),
                value.RequireSlopeFromAbove(previous, elapsed, bound),
                value.EnsureSlopeFromAbove(previous, elapsed, bound));
        }

        [Theory]
        [InlineData(10, 4, 3, 3.0, true)]
        [InlineData(10, 4, 3, 1.0, false)]
        [InlineData(10, 4, 3, 2.0, false)]
        [InlineData(4, 10, 3, -1.0, true)]
        [InlineData(4, 10, 3, -3.0, false)]
        [InlineData(4, 10, 3, -2.0, false)]
        [InlineData(1, 1, 0, 0, false)]
        public void SlopeFromBelowAppliesTheOutcomeOfEveryStepKind(double value, double previous, double elapsed, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSlopeFromBelow(previous, elapsed, bound),
                start.RejectSlopeFromBelow(previous, elapsed, bound),
                start.BreakSlopeFromBelow(previous, elapsed, bound),
                start.RequireSlopeFromBelow(previous, elapsed, bound),
                start.EnsureSlopeFromBelow(previous, elapsed, bound));

            FlowAssert.Kinds(expected,
                value.MatchSlopeFromBelow(previous, elapsed, bound),
                value.RejectSlopeFromBelow(previous, elapsed, bound),
                value.BreakSlopeFromBelow(previous, elapsed, bound),
                value.RequireSlopeFromBelow(previous, elapsed, bound),
                value.EnsureSlopeFromBelow(previous, elapsed, bound));
        }

        [Theory]
        [InlineData(10, 4, 3, 1.0, 3.0, true)]
        [InlineData(10, 4, 3, 3.0, 4.0, false)]
        [InlineData(10, 4, 3, 2.0, 2.0, true)]
        [InlineData(4, 10, 3, -3.0, -1.0, true)]
        [InlineData(4, 10, 3, -1.0, 0.0, false)]
        [InlineData(4, 10, 3, -2.0, -2.0, true)]
        [InlineData(1, 1, 0, -1000, 1000, false)]
        public void SlopeFromInRangeAppliesTheOutcomeOfEveryStepKind(double value, double previous, double elapsed, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSlopeFromInRange(previous, elapsed, lowerBound, upperBound),
                start.RejectSlopeFromInRange(previous, elapsed, lowerBound, upperBound),
                start.BreakSlopeFromInRange(previous, elapsed, lowerBound, upperBound),
                start.RequireSlopeFromInRange(previous, elapsed, lowerBound, upperBound),
                start.EnsureSlopeFromInRange(previous, elapsed, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchSlopeFromInRange(previous, elapsed, lowerBound, upperBound),
                value.RejectSlopeFromInRange(previous, elapsed, lowerBound, upperBound),
                value.BreakSlopeFromInRange(previous, elapsed, lowerBound, upperBound),
                value.RequireSlopeFromInRange(previous, elapsed, lowerBound, upperBound),
                value.EnsureSlopeFromInRange(previous, elapsed, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(10, 4, 3, 3.0, 4.0, true)]
        [InlineData(10, 4, 3, 1.0, 3.0, false)]
        [InlineData(10, 4, 3, 2.0, 2.0, false)]
        [InlineData(4, 10, 3, -1.0, 0.0, true)]
        [InlineData(4, 10, 3, -3.0, -1.0, false)]
        [InlineData(4, 10, 3, -2.0, -2.0, false)]
        [InlineData(1, 1, 0, -1000, 1000, false)]
        public void SlopeFromOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double previous, double elapsed, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSlopeFromOutsideRange(previous, elapsed, lowerBound, upperBound),
                start.RejectSlopeFromOutsideRange(previous, elapsed, lowerBound, upperBound),
                start.BreakSlopeFromOutsideRange(previous, elapsed, lowerBound, upperBound),
                start.RequireSlopeFromOutsideRange(previous, elapsed, lowerBound, upperBound),
                start.EnsureSlopeFromOutsideRange(previous, elapsed, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchSlopeFromOutsideRange(previous, elapsed, lowerBound, upperBound),
                value.RejectSlopeFromOutsideRange(previous, elapsed, lowerBound, upperBound),
                value.BreakSlopeFromOutsideRange(previous, elapsed, lowerBound, upperBound),
                value.RequireSlopeFromOutsideRange(previous, elapsed, lowerBound, upperBound),
                value.EnsureSlopeFromOutsideRange(previous, elapsed, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(121, 100, 2, -0.9, true)]
        [InlineData(121, 100, 2, 1.1, false)]
        [InlineData(100, 100, 5, -1.0, true)]
        [InlineData(100, 100, 5, 1.0, false)]
        [InlineData(100, 0, 2, 0, false)]
        [InlineData(100, 100, 0, 0, false)]
        [InlineData(-1, 100, 2, 0, false)]
        public void CompoundGrowthRateAboveAppliesTheOutcomeOfEveryStepKind(double value, double previous, double periods, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchCompoundGrowthRateAbove(previous, periods, bound),
                start.RejectCompoundGrowthRateAbove(previous, periods, bound),
                start.BreakCompoundGrowthRateAbove(previous, periods, bound),
                start.RequireCompoundGrowthRateAbove(previous, periods, bound),
                start.EnsureCompoundGrowthRateAbove(previous, periods, bound));

            FlowAssert.Kinds(expected,
                value.MatchCompoundGrowthRateAbove(previous, periods, bound),
                value.RejectCompoundGrowthRateAbove(previous, periods, bound),
                value.BreakCompoundGrowthRateAbove(previous, periods, bound),
                value.RequireCompoundGrowthRateAbove(previous, periods, bound),
                value.EnsureCompoundGrowthRateAbove(previous, periods, bound));
        }

        [Theory]
        [InlineData(121, 100, 2, 1.1, true)]
        [InlineData(121, 100, 2, -0.9, false)]
        [InlineData(100, 100, 5, 1.0, true)]
        [InlineData(100, 100, 5, -1.0, false)]
        [InlineData(100, 0, 2, 0, false)]
        [InlineData(100, 100, 0, 0, false)]
        [InlineData(-1, 100, 2, 0, false)]
        public void CompoundGrowthRateBelowAppliesTheOutcomeOfEveryStepKind(double value, double previous, double periods, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchCompoundGrowthRateBelow(previous, periods, bound),
                start.RejectCompoundGrowthRateBelow(previous, periods, bound),
                start.BreakCompoundGrowthRateBelow(previous, periods, bound),
                start.RequireCompoundGrowthRateBelow(previous, periods, bound),
                start.EnsureCompoundGrowthRateBelow(previous, periods, bound));

            FlowAssert.Kinds(expected,
                value.MatchCompoundGrowthRateBelow(previous, periods, bound),
                value.RejectCompoundGrowthRateBelow(previous, periods, bound),
                value.BreakCompoundGrowthRateBelow(previous, periods, bound),
                value.RequireCompoundGrowthRateBelow(previous, periods, bound),
                value.EnsureCompoundGrowthRateBelow(previous, periods, bound));
        }

        [Theory]
        [InlineData(121, 100, 2, -0.9, 1.1, true)]
        [InlineData(121, 100, 2, 1.1, 2.1, false)]
        [InlineData(100, 100, 5, -1.0, 1.0, true)]
        [InlineData(100, 100, 5, 1.0, 2.0, false)]
        [InlineData(100, 0, 2, -1000, 1000, false)]
        [InlineData(100, 100, 0, -1000, 1000, false)]
        [InlineData(-1, 100, 2, -1000, 1000, false)]
        public void CompoundGrowthRateInRangeAppliesTheOutcomeOfEveryStepKind(double value, double previous, double periods, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound),
                start.RejectCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound),
                start.BreakCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound),
                start.RequireCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound),
                start.EnsureCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound),
                value.RejectCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound),
                value.BreakCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound),
                value.RequireCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound),
                value.EnsureCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(121, 100, 2, 1.1, 2.1, true)]
        [InlineData(121, 100, 2, -0.9, 1.1, false)]
        [InlineData(100, 100, 5, 1.0, 2.0, true)]
        [InlineData(100, 100, 5, -1.0, 1.0, false)]
        [InlineData(100, 0, 2, -1000, 1000, false)]
        [InlineData(100, 100, 0, -1000, 1000, false)]
        [InlineData(-1, 100, 2, -1000, 1000, false)]
        public void CompoundGrowthRateOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double previous, double periods, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound),
                start.RejectCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound),
                start.BreakCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound),
                start.RequireCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound),
                start.EnsureCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound),
                value.RejectCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound),
                value.BreakCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound),
                value.RequireCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound),
                value.EnsureCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(110, 100, 9.0, true)]
        [InlineData(110, 100, 11.0, false)]
        [InlineData(110, 100, 10.0, false)]
        [InlineData(90, 100, -11.0, true)]
        [InlineData(90, 100, -9.0, false)]
        [InlineData(90, 100, -10.0, false)]
        [InlineData(5, 0, 0, false)]
        [InlineData(-90, -100, 9.0, true)]
        [InlineData(-90, -100, 11.0, false)]
        [InlineData(-90, -100, 10.0, false)]
        public void PercentDeviationFromMeanAboveAppliesTheOutcomeOfEveryStepKind(double value, double mean, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentDeviationFromMeanAbove(mean, bound),
                start.RejectPercentDeviationFromMeanAbove(mean, bound),
                start.BreakPercentDeviationFromMeanAbove(mean, bound),
                start.RequirePercentDeviationFromMeanAbove(mean, bound),
                start.EnsurePercentDeviationFromMeanAbove(mean, bound));

            FlowAssert.Kinds(expected,
                value.MatchPercentDeviationFromMeanAbove(mean, bound),
                value.RejectPercentDeviationFromMeanAbove(mean, bound),
                value.BreakPercentDeviationFromMeanAbove(mean, bound),
                value.RequirePercentDeviationFromMeanAbove(mean, bound),
                value.EnsurePercentDeviationFromMeanAbove(mean, bound));
        }

        [Theory]
        [InlineData(110, 100, 11.0, true)]
        [InlineData(110, 100, 9.0, false)]
        [InlineData(110, 100, 10.0, false)]
        [InlineData(90, 100, -9.0, true)]
        [InlineData(90, 100, -11.0, false)]
        [InlineData(90, 100, -10.0, false)]
        [InlineData(5, 0, 0, false)]
        [InlineData(-90, -100, 11.0, true)]
        [InlineData(-90, -100, 9.0, false)]
        [InlineData(-90, -100, 10.0, false)]
        public void PercentDeviationFromMeanBelowAppliesTheOutcomeOfEveryStepKind(double value, double mean, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentDeviationFromMeanBelow(mean, bound),
                start.RejectPercentDeviationFromMeanBelow(mean, bound),
                start.BreakPercentDeviationFromMeanBelow(mean, bound),
                start.RequirePercentDeviationFromMeanBelow(mean, bound),
                start.EnsurePercentDeviationFromMeanBelow(mean, bound));

            FlowAssert.Kinds(expected,
                value.MatchPercentDeviationFromMeanBelow(mean, bound),
                value.RejectPercentDeviationFromMeanBelow(mean, bound),
                value.BreakPercentDeviationFromMeanBelow(mean, bound),
                value.RequirePercentDeviationFromMeanBelow(mean, bound),
                value.EnsurePercentDeviationFromMeanBelow(mean, bound));
        }

        [Theory]
        [InlineData(110, 100, 9.0, 11.0, true)]
        [InlineData(110, 100, 11.0, 12.0, false)]
        [InlineData(110, 100, 10.0, 10.0, true)]
        [InlineData(90, 100, -11.0, -9.0, true)]
        [InlineData(90, 100, -9.0, -8.0, false)]
        [InlineData(90, 100, -10.0, -10.0, true)]
        [InlineData(5, 0, -1000, 1000, false)]
        [InlineData(-90, -100, 9.0, 11.0, true)]
        [InlineData(-90, -100, 11.0, 12.0, false)]
        [InlineData(-90, -100, 10.0, 10.0, true)]
        public void PercentDeviationFromMeanInRangeAppliesTheOutcomeOfEveryStepKind(double value, double mean, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentDeviationFromMeanInRange(mean, lowerBound, upperBound),
                start.RejectPercentDeviationFromMeanInRange(mean, lowerBound, upperBound),
                start.BreakPercentDeviationFromMeanInRange(mean, lowerBound, upperBound),
                start.RequirePercentDeviationFromMeanInRange(mean, lowerBound, upperBound),
                start.EnsurePercentDeviationFromMeanInRange(mean, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchPercentDeviationFromMeanInRange(mean, lowerBound, upperBound),
                value.RejectPercentDeviationFromMeanInRange(mean, lowerBound, upperBound),
                value.BreakPercentDeviationFromMeanInRange(mean, lowerBound, upperBound),
                value.RequirePercentDeviationFromMeanInRange(mean, lowerBound, upperBound),
                value.EnsurePercentDeviationFromMeanInRange(mean, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(110, 100, 11.0, 12.0, true)]
        [InlineData(110, 100, 9.0, 11.0, false)]
        [InlineData(110, 100, 10.0, 10.0, false)]
        [InlineData(90, 100, -9.0, -8.0, true)]
        [InlineData(90, 100, -11.0, -9.0, false)]
        [InlineData(90, 100, -10.0, -10.0, false)]
        [InlineData(5, 0, -1000, 1000, false)]
        [InlineData(-90, -100, 11.0, 12.0, true)]
        [InlineData(-90, -100, 9.0, 11.0, false)]
        [InlineData(-90, -100, 10.0, 10.0, false)]
        public void PercentDeviationFromMeanOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double mean, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentDeviationFromMeanOutsideRange(mean, lowerBound, upperBound),
                start.RejectPercentDeviationFromMeanOutsideRange(mean, lowerBound, upperBound),
                start.BreakPercentDeviationFromMeanOutsideRange(mean, lowerBound, upperBound),
                start.RequirePercentDeviationFromMeanOutsideRange(mean, lowerBound, upperBound),
                start.EnsurePercentDeviationFromMeanOutsideRange(mean, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchPercentDeviationFromMeanOutsideRange(mean, lowerBound, upperBound),
                value.RejectPercentDeviationFromMeanOutsideRange(mean, lowerBound, upperBound),
                value.BreakPercentDeviationFromMeanOutsideRange(mean, lowerBound, upperBound),
                value.RequirePercentDeviationFromMeanOutsideRange(mean, lowerBound, upperBound),
                value.EnsurePercentDeviationFromMeanOutsideRange(mean, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(110, 100, 10, -0.3255, true)]
        [InlineData(110, 100, 10, 1.6745, false)]
        [InlineData(100, 100, 10, -1.0, true)]
        [InlineData(100, 100, 10, 1.0, false)]
        [InlineData(1, 1, 0, 0, false)]
        [InlineData(90, 100, 10, -1.6745, true)]
        [InlineData(90, 100, 10, 0.3255, false)]
        public void ModifiedZScoreAboveAppliesTheOutcomeOfEveryStepKind(double value, double median, double medianAbsoluteDeviation, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchModifiedZScoreAbove(median, medianAbsoluteDeviation, bound),
                start.RejectModifiedZScoreAbove(median, medianAbsoluteDeviation, bound),
                start.BreakModifiedZScoreAbove(median, medianAbsoluteDeviation, bound),
                start.RequireModifiedZScoreAbove(median, medianAbsoluteDeviation, bound),
                start.EnsureModifiedZScoreAbove(median, medianAbsoluteDeviation, bound));

            FlowAssert.Kinds(expected,
                value.MatchModifiedZScoreAbove(median, medianAbsoluteDeviation, bound),
                value.RejectModifiedZScoreAbove(median, medianAbsoluteDeviation, bound),
                value.BreakModifiedZScoreAbove(median, medianAbsoluteDeviation, bound),
                value.RequireModifiedZScoreAbove(median, medianAbsoluteDeviation, bound),
                value.EnsureModifiedZScoreAbove(median, medianAbsoluteDeviation, bound));
        }

        [Theory]
        [InlineData(110, 100, 10, 1.6745, true)]
        [InlineData(110, 100, 10, -0.3255, false)]
        [InlineData(100, 100, 10, 1.0, true)]
        [InlineData(100, 100, 10, -1.0, false)]
        [InlineData(1, 1, 0, 0, false)]
        [InlineData(90, 100, 10, 0.3255, true)]
        [InlineData(90, 100, 10, -1.6745, false)]
        public void ModifiedZScoreBelowAppliesTheOutcomeOfEveryStepKind(double value, double median, double medianAbsoluteDeviation, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchModifiedZScoreBelow(median, medianAbsoluteDeviation, bound),
                start.RejectModifiedZScoreBelow(median, medianAbsoluteDeviation, bound),
                start.BreakModifiedZScoreBelow(median, medianAbsoluteDeviation, bound),
                start.RequireModifiedZScoreBelow(median, medianAbsoluteDeviation, bound),
                start.EnsureModifiedZScoreBelow(median, medianAbsoluteDeviation, bound));

            FlowAssert.Kinds(expected,
                value.MatchModifiedZScoreBelow(median, medianAbsoluteDeviation, bound),
                value.RejectModifiedZScoreBelow(median, medianAbsoluteDeviation, bound),
                value.BreakModifiedZScoreBelow(median, medianAbsoluteDeviation, bound),
                value.RequireModifiedZScoreBelow(median, medianAbsoluteDeviation, bound),
                value.EnsureModifiedZScoreBelow(median, medianAbsoluteDeviation, bound));
        }

        [Theory]
        [InlineData(110, 100, 10, -0.3255, 1.6745, true)]
        [InlineData(110, 100, 10, 1.6745, 2.6745, false)]
        [InlineData(100, 100, 10, -1.0, 1.0, true)]
        [InlineData(100, 100, 10, 1.0, 2.0, false)]
        [InlineData(1, 1, 0, -1000, 1000, false)]
        [InlineData(90, 100, 10, -1.6745, 0.3255, true)]
        [InlineData(90, 100, 10, 0.3255, 1.3255, false)]
        public void ModifiedZScoreInRangeAppliesTheOutcomeOfEveryStepKind(double value, double median, double medianAbsoluteDeviation, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                start.RejectModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                start.BreakModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                start.RequireModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                start.EnsureModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                value.RejectModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                value.BreakModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                value.RequireModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                value.EnsureModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(110, 100, 10, 1.6745, 2.6745, true)]
        [InlineData(110, 100, 10, -0.3255, 1.6745, false)]
        [InlineData(100, 100, 10, 1.0, 2.0, true)]
        [InlineData(100, 100, 10, -1.0, 1.0, false)]
        [InlineData(1, 1, 0, -1000, 1000, false)]
        [InlineData(90, 100, 10, 0.3255, 1.3255, true)]
        [InlineData(90, 100, 10, -1.6745, 0.3255, false)]
        public void ModifiedZScoreOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double median, double medianAbsoluteDeviation, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                start.RejectModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                start.BreakModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                start.RequireModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                start.EnsureModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                value.RejectModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                value.BreakModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                value.RequireModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound),
                value.EnsureModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(5, 10, -0.5, true)]
        [InlineData(5, 10, 1.5, false)]
        [InlineData(5, 10, 0.5, false)]
        [InlineData(5, -10, -0.5, true)]
        [InlineData(5, -10, 1.5, false)]
        [InlineData(5, -10, 0.5, false)]
        [InlineData(5, 0, 0, false)]
        public void CoefficientOfVariationAboveAppliesTheOutcomeOfEveryStepKind(double value, double mean, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchCoefficientOfVariationAbove(mean, bound),
                start.RejectCoefficientOfVariationAbove(mean, bound),
                start.BreakCoefficientOfVariationAbove(mean, bound),
                start.RequireCoefficientOfVariationAbove(mean, bound),
                start.EnsureCoefficientOfVariationAbove(mean, bound));

            FlowAssert.Kinds(expected,
                value.MatchCoefficientOfVariationAbove(mean, bound),
                value.RejectCoefficientOfVariationAbove(mean, bound),
                value.BreakCoefficientOfVariationAbove(mean, bound),
                value.RequireCoefficientOfVariationAbove(mean, bound),
                value.EnsureCoefficientOfVariationAbove(mean, bound));
        }

        [Theory]
        [InlineData(5, 10, 1.5, true)]
        [InlineData(5, 10, -0.5, false)]
        [InlineData(5, 10, 0.5, false)]
        [InlineData(5, -10, 1.5, true)]
        [InlineData(5, -10, -0.5, false)]
        [InlineData(5, -10, 0.5, false)]
        [InlineData(5, 0, 0, false)]
        public void CoefficientOfVariationBelowAppliesTheOutcomeOfEveryStepKind(double value, double mean, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchCoefficientOfVariationBelow(mean, bound),
                start.RejectCoefficientOfVariationBelow(mean, bound),
                start.BreakCoefficientOfVariationBelow(mean, bound),
                start.RequireCoefficientOfVariationBelow(mean, bound),
                start.EnsureCoefficientOfVariationBelow(mean, bound));

            FlowAssert.Kinds(expected,
                value.MatchCoefficientOfVariationBelow(mean, bound),
                value.RejectCoefficientOfVariationBelow(mean, bound),
                value.BreakCoefficientOfVariationBelow(mean, bound),
                value.RequireCoefficientOfVariationBelow(mean, bound),
                value.EnsureCoefficientOfVariationBelow(mean, bound));
        }

        [Theory]
        [InlineData(5, 10, -0.5, 1.5, true)]
        [InlineData(5, 10, 1.5, 2.5, false)]
        [InlineData(5, 10, 0.5, 0.5, true)]
        [InlineData(5, -10, -0.5, 1.5, true)]
        [InlineData(5, -10, 1.5, 2.5, false)]
        [InlineData(5, -10, 0.5, 0.5, true)]
        [InlineData(5, 0, -1000, 1000, false)]
        public void CoefficientOfVariationInRangeAppliesTheOutcomeOfEveryStepKind(double value, double mean, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchCoefficientOfVariationInRange(mean, lowerBound, upperBound),
                start.RejectCoefficientOfVariationInRange(mean, lowerBound, upperBound),
                start.BreakCoefficientOfVariationInRange(mean, lowerBound, upperBound),
                start.RequireCoefficientOfVariationInRange(mean, lowerBound, upperBound),
                start.EnsureCoefficientOfVariationInRange(mean, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchCoefficientOfVariationInRange(mean, lowerBound, upperBound),
                value.RejectCoefficientOfVariationInRange(mean, lowerBound, upperBound),
                value.BreakCoefficientOfVariationInRange(mean, lowerBound, upperBound),
                value.RequireCoefficientOfVariationInRange(mean, lowerBound, upperBound),
                value.EnsureCoefficientOfVariationInRange(mean, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(5, 10, 1.5, 2.5, true)]
        [InlineData(5, 10, -0.5, 1.5, false)]
        [InlineData(5, 10, 0.5, 0.5, false)]
        [InlineData(5, -10, 1.5, 2.5, true)]
        [InlineData(5, -10, -0.5, 1.5, false)]
        [InlineData(5, -10, 0.5, 0.5, false)]
        [InlineData(5, 0, -1000, 1000, false)]
        public void CoefficientOfVariationOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double mean, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchCoefficientOfVariationOutsideRange(mean, lowerBound, upperBound),
                start.RejectCoefficientOfVariationOutsideRange(mean, lowerBound, upperBound),
                start.BreakCoefficientOfVariationOutsideRange(mean, lowerBound, upperBound),
                start.RequireCoefficientOfVariationOutsideRange(mean, lowerBound, upperBound),
                start.EnsureCoefficientOfVariationOutsideRange(mean, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchCoefficientOfVariationOutsideRange(mean, lowerBound, upperBound),
                value.RejectCoefficientOfVariationOutsideRange(mean, lowerBound, upperBound),
                value.BreakCoefficientOfVariationOutsideRange(mean, lowerBound, upperBound),
                value.RequireCoefficientOfVariationOutsideRange(mean, lowerBound, upperBound),
                value.EnsureCoefficientOfVariationOutsideRange(mean, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(5, 0, 10, -0.5, true)]
        [InlineData(5, 0, 10, 1.5, false)]
        [InlineData(5, 0, 10, 0.5, false)]
        [InlineData(0, 0, 10, -1.0, true)]
        [InlineData(0, 0, 10, 1.0, false)]
        [InlineData(0, 0, 10, 0.0, false)]
        [InlineData(10, 0, 10, 0.0, true)]
        [InlineData(10, 0, 10, 2.0, false)]
        [InlineData(10, 0, 10, 1.0, false)]
        [InlineData(15, 0, 10, 0.5, true)]
        [InlineData(15, 0, 10, 2.5, false)]
        [InlineData(15, 0, 10, 1.5, false)]
        [InlineData(5, 3, 3, 0, false)]
        public void MinMaxNormaliseAboveAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMinMaxNormaliseAbove(minimum, maximum, bound),
                start.RejectMinMaxNormaliseAbove(minimum, maximum, bound),
                start.BreakMinMaxNormaliseAbove(minimum, maximum, bound),
                start.RequireMinMaxNormaliseAbove(minimum, maximum, bound),
                start.EnsureMinMaxNormaliseAbove(minimum, maximum, bound));

            FlowAssert.Kinds(expected,
                value.MatchMinMaxNormaliseAbove(minimum, maximum, bound),
                value.RejectMinMaxNormaliseAbove(minimum, maximum, bound),
                value.BreakMinMaxNormaliseAbove(minimum, maximum, bound),
                value.RequireMinMaxNormaliseAbove(minimum, maximum, bound),
                value.EnsureMinMaxNormaliseAbove(minimum, maximum, bound));
        }

        [Theory]
        [InlineData(5, 0, 10, 1.5, true)]
        [InlineData(5, 0, 10, -0.5, false)]
        [InlineData(5, 0, 10, 0.5, false)]
        [InlineData(0, 0, 10, 1.0, true)]
        [InlineData(0, 0, 10, -1.0, false)]
        [InlineData(0, 0, 10, 0.0, false)]
        [InlineData(10, 0, 10, 2.0, true)]
        [InlineData(10, 0, 10, 0.0, false)]
        [InlineData(10, 0, 10, 1.0, false)]
        [InlineData(15, 0, 10, 2.5, true)]
        [InlineData(15, 0, 10, 0.5, false)]
        [InlineData(15, 0, 10, 1.5, false)]
        [InlineData(5, 3, 3, 0, false)]
        public void MinMaxNormaliseBelowAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMinMaxNormaliseBelow(minimum, maximum, bound),
                start.RejectMinMaxNormaliseBelow(minimum, maximum, bound),
                start.BreakMinMaxNormaliseBelow(minimum, maximum, bound),
                start.RequireMinMaxNormaliseBelow(minimum, maximum, bound),
                start.EnsureMinMaxNormaliseBelow(minimum, maximum, bound));

            FlowAssert.Kinds(expected,
                value.MatchMinMaxNormaliseBelow(minimum, maximum, bound),
                value.RejectMinMaxNormaliseBelow(minimum, maximum, bound),
                value.BreakMinMaxNormaliseBelow(minimum, maximum, bound),
                value.RequireMinMaxNormaliseBelow(minimum, maximum, bound),
                value.EnsureMinMaxNormaliseBelow(minimum, maximum, bound));
        }

        [Theory]
        [InlineData(5, 0, 10, -0.5, 1.5, true)]
        [InlineData(5, 0, 10, 1.5, 2.5, false)]
        [InlineData(5, 0, 10, 0.5, 0.5, true)]
        [InlineData(0, 0, 10, -1.0, 1.0, true)]
        [InlineData(0, 0, 10, 1.0, 2.0, false)]
        [InlineData(0, 0, 10, 0.0, 0.0, true)]
        [InlineData(10, 0, 10, 0.0, 2.0, true)]
        [InlineData(10, 0, 10, 2.0, 3.0, false)]
        [InlineData(10, 0, 10, 1.0, 1.0, true)]
        [InlineData(15, 0, 10, 0.5, 2.5, true)]
        [InlineData(15, 0, 10, 2.5, 3.5, false)]
        [InlineData(15, 0, 10, 1.5, 1.5, true)]
        [InlineData(5, 3, 3, -1000, 1000, false)]
        public void MinMaxNormaliseInRangeAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound),
                start.RejectMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound),
                start.BreakMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound),
                start.RequireMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound),
                start.EnsureMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound),
                value.RejectMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound),
                value.BreakMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound),
                value.RequireMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound),
                value.EnsureMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(5, 0, 10, 1.5, 2.5, true)]
        [InlineData(5, 0, 10, -0.5, 1.5, false)]
        [InlineData(5, 0, 10, 0.5, 0.5, false)]
        [InlineData(0, 0, 10, 1.0, 2.0, true)]
        [InlineData(0, 0, 10, -1.0, 1.0, false)]
        [InlineData(0, 0, 10, 0.0, 0.0, false)]
        [InlineData(10, 0, 10, 2.0, 3.0, true)]
        [InlineData(10, 0, 10, 0.0, 2.0, false)]
        [InlineData(10, 0, 10, 1.0, 1.0, false)]
        [InlineData(15, 0, 10, 2.5, 3.5, true)]
        [InlineData(15, 0, 10, 0.5, 2.5, false)]
        [InlineData(15, 0, 10, 1.5, 1.5, false)]
        [InlineData(5, 3, 3, -1000, 1000, false)]
        public void MinMaxNormaliseOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound),
                start.RejectMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound),
                start.BreakMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound),
                start.RequireMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound),
                start.EnsureMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound),
                value.RejectMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound),
                value.BreakMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound),
                value.RequireMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound),
                value.EnsureMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(5, 0, 10, 49.0, true)]
        [InlineData(5, 0, 10, 51.0, false)]
        [InlineData(5, 0, 10, 50.0, false)]
        [InlineData(25, 0, 100, 24.0, true)]
        [InlineData(25, 0, 100, 26.0, false)]
        [InlineData(25, 0, 100, 25.0, false)]
        [InlineData(5, 3, 3, 0, false)]
        public void PercentOfRangeAboveAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentOfRangeAbove(minimum, maximum, bound),
                start.RejectPercentOfRangeAbove(minimum, maximum, bound),
                start.BreakPercentOfRangeAbove(minimum, maximum, bound),
                start.RequirePercentOfRangeAbove(minimum, maximum, bound),
                start.EnsurePercentOfRangeAbove(minimum, maximum, bound));

            FlowAssert.Kinds(expected,
                value.MatchPercentOfRangeAbove(minimum, maximum, bound),
                value.RejectPercentOfRangeAbove(minimum, maximum, bound),
                value.BreakPercentOfRangeAbove(minimum, maximum, bound),
                value.RequirePercentOfRangeAbove(minimum, maximum, bound),
                value.EnsurePercentOfRangeAbove(minimum, maximum, bound));
        }

        [Theory]
        [InlineData(5, 0, 10, 51.0, true)]
        [InlineData(5, 0, 10, 49.0, false)]
        [InlineData(5, 0, 10, 50.0, false)]
        [InlineData(25, 0, 100, 26.0, true)]
        [InlineData(25, 0, 100, 24.0, false)]
        [InlineData(25, 0, 100, 25.0, false)]
        [InlineData(5, 3, 3, 0, false)]
        public void PercentOfRangeBelowAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentOfRangeBelow(minimum, maximum, bound),
                start.RejectPercentOfRangeBelow(minimum, maximum, bound),
                start.BreakPercentOfRangeBelow(minimum, maximum, bound),
                start.RequirePercentOfRangeBelow(minimum, maximum, bound),
                start.EnsurePercentOfRangeBelow(minimum, maximum, bound));

            FlowAssert.Kinds(expected,
                value.MatchPercentOfRangeBelow(minimum, maximum, bound),
                value.RejectPercentOfRangeBelow(minimum, maximum, bound),
                value.BreakPercentOfRangeBelow(minimum, maximum, bound),
                value.RequirePercentOfRangeBelow(minimum, maximum, bound),
                value.EnsurePercentOfRangeBelow(minimum, maximum, bound));
        }

        [Theory]
        [InlineData(5, 0, 10, 49.0, 51.0, true)]
        [InlineData(5, 0, 10, 51.0, 52.0, false)]
        [InlineData(5, 0, 10, 50.0, 50.0, true)]
        [InlineData(25, 0, 100, 24.0, 26.0, true)]
        [InlineData(25, 0, 100, 26.0, 27.0, false)]
        [InlineData(25, 0, 100, 25.0, 25.0, true)]
        [InlineData(5, 3, 3, -1000, 1000, false)]
        public void PercentOfRangeInRangeAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentOfRangeInRange(minimum, maximum, lowerBound, upperBound),
                start.RejectPercentOfRangeInRange(minimum, maximum, lowerBound, upperBound),
                start.BreakPercentOfRangeInRange(minimum, maximum, lowerBound, upperBound),
                start.RequirePercentOfRangeInRange(minimum, maximum, lowerBound, upperBound),
                start.EnsurePercentOfRangeInRange(minimum, maximum, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchPercentOfRangeInRange(minimum, maximum, lowerBound, upperBound),
                value.RejectPercentOfRangeInRange(minimum, maximum, lowerBound, upperBound),
                value.BreakPercentOfRangeInRange(minimum, maximum, lowerBound, upperBound),
                value.RequirePercentOfRangeInRange(minimum, maximum, lowerBound, upperBound),
                value.EnsurePercentOfRangeInRange(minimum, maximum, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(5, 0, 10, 51.0, 52.0, true)]
        [InlineData(5, 0, 10, 49.0, 51.0, false)]
        [InlineData(5, 0, 10, 50.0, 50.0, false)]
        [InlineData(25, 0, 100, 26.0, 27.0, true)]
        [InlineData(25, 0, 100, 24.0, 26.0, false)]
        [InlineData(25, 0, 100, 25.0, 25.0, false)]
        [InlineData(5, 3, 3, -1000, 1000, false)]
        public void PercentOfRangeOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double maximum, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound),
                start.RejectPercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound),
                start.BreakPercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound),
                start.RequirePercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound),
                start.EnsurePercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchPercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound),
                value.RejectPercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound),
                value.BreakPercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound),
                value.RequirePercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound),
                value.EnsurePercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(1, 5.0, new[] { 2d, 3d }, true)]
        [InlineData(1, 7.0, new[] { 2d, 3d }, false)]
        [InlineData(1, 6.0, new[] { 2d, 3d }, false)]
        [InlineData(1, 0.0, new double[0], true)]
        [InlineData(1, 2.0, new double[0], false)]
        [InlineData(1, 1.0, new double[0], false)]
        [InlineData(double.NaN, 0, new[] { 1d }, false)]
        [InlineData(1, 0, new[] { double.NaN }, false)]
        public void SumWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSumWithAbove(bound, others),
                start.RejectSumWithAbove(bound, others),
                start.BreakSumWithAbove(bound, others),
                start.RequireSumWithAbove(bound, others),
                start.EnsureSumWithAbove(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchSumWithAbove(bound, others),
                value.RejectSumWithAbove(bound, others),
                value.BreakSumWithAbove(bound, others),
                value.RequireSumWithAbove(bound, others),
                value.EnsureSumWithAbove(bound, others));
        }

        [Theory]
        [InlineData(1, 7.0, new[] { 2d, 3d }, true)]
        [InlineData(1, 5.0, new[] { 2d, 3d }, false)]
        [InlineData(1, 6.0, new[] { 2d, 3d }, false)]
        [InlineData(1, 2.0, new double[0], true)]
        [InlineData(1, 0.0, new double[0], false)]
        [InlineData(1, 1.0, new double[0], false)]
        [InlineData(double.NaN, 0, new[] { 1d }, false)]
        [InlineData(1, 0, new[] { double.NaN }, false)]
        public void SumWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSumWithBelow(bound, others),
                start.RejectSumWithBelow(bound, others),
                start.BreakSumWithBelow(bound, others),
                start.RequireSumWithBelow(bound, others),
                start.EnsureSumWithBelow(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchSumWithBelow(bound, others),
                value.RejectSumWithBelow(bound, others),
                value.BreakSumWithBelow(bound, others),
                value.RequireSumWithBelow(bound, others),
                value.EnsureSumWithBelow(bound, others));
        }

        [Theory]
        [InlineData(1, 5.0, 7.0, new[] { 2d, 3d }, true)]
        [InlineData(1, 7.0, 8.0, new[] { 2d, 3d }, false)]
        [InlineData(1, 6.0, 6.0, new[] { 2d, 3d }, true)]
        [InlineData(1, 0.0, 2.0, new double[0], true)]
        [InlineData(1, 2.0, 3.0, new double[0], false)]
        [InlineData(1, 1.0, 1.0, new double[0], true)]
        [InlineData(double.NaN, -1000, 1000, new[] { 1d }, false)]
        [InlineData(1, -1000, 1000, new[] { double.NaN }, false)]
        public void SumWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSumWithInRange(lowerBound, upperBound, others),
                start.RejectSumWithInRange(lowerBound, upperBound, others),
                start.BreakSumWithInRange(lowerBound, upperBound, others),
                start.RequireSumWithInRange(lowerBound, upperBound, others),
                start.EnsureSumWithInRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchSumWithInRange(lowerBound, upperBound, others),
                value.RejectSumWithInRange(lowerBound, upperBound, others),
                value.BreakSumWithInRange(lowerBound, upperBound, others),
                value.RequireSumWithInRange(lowerBound, upperBound, others),
                value.EnsureSumWithInRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(1, 7.0, 8.0, new[] { 2d, 3d }, true)]
        [InlineData(1, 5.0, 7.0, new[] { 2d, 3d }, false)]
        [InlineData(1, 6.0, 6.0, new[] { 2d, 3d }, false)]
        [InlineData(1, 2.0, 3.0, new double[0], true)]
        [InlineData(1, 0.0, 2.0, new double[0], false)]
        [InlineData(1, 1.0, 1.0, new double[0], false)]
        [InlineData(double.NaN, -1000, 1000, new[] { 1d }, false)]
        [InlineData(1, -1000, 1000, new[] { double.NaN }, false)]
        public void SumWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSumWithOutsideRange(lowerBound, upperBound, others),
                start.RejectSumWithOutsideRange(lowerBound, upperBound, others),
                start.BreakSumWithOutsideRange(lowerBound, upperBound, others),
                start.RequireSumWithOutsideRange(lowerBound, upperBound, others),
                start.EnsureSumWithOutsideRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchSumWithOutsideRange(lowerBound, upperBound, others),
                value.RejectSumWithOutsideRange(lowerBound, upperBound, others),
                value.BreakSumWithOutsideRange(lowerBound, upperBound, others),
                value.RequireSumWithOutsideRange(lowerBound, upperBound, others),
                value.EnsureSumWithOutsideRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(2, 3.0, new[] { 4d, 6d }, true)]
        [InlineData(2, 5.0, new[] { 4d, 6d }, false)]
        [InlineData(2, 4.0, new[] { 4d, 6d }, false)]
        [InlineData(5, 4.0, new double[0], true)]
        [InlineData(5, 6.0, new double[0], false)]
        [InlineData(5, 5.0, new double[0], false)]
        [InlineData(double.NaN, 0, new[] { 1d }, false)]
        public void MeanWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMeanWithAbove(bound, others),
                start.RejectMeanWithAbove(bound, others),
                start.BreakMeanWithAbove(bound, others),
                start.RequireMeanWithAbove(bound, others),
                start.EnsureMeanWithAbove(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchMeanWithAbove(bound, others),
                value.RejectMeanWithAbove(bound, others),
                value.BreakMeanWithAbove(bound, others),
                value.RequireMeanWithAbove(bound, others),
                value.EnsureMeanWithAbove(bound, others));
        }

        [Theory]
        [InlineData(2, 5.0, new[] { 4d, 6d }, true)]
        [InlineData(2, 3.0, new[] { 4d, 6d }, false)]
        [InlineData(2, 4.0, new[] { 4d, 6d }, false)]
        [InlineData(5, 6.0, new double[0], true)]
        [InlineData(5, 4.0, new double[0], false)]
        [InlineData(5, 5.0, new double[0], false)]
        [InlineData(double.NaN, 0, new[] { 1d }, false)]
        public void MeanWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMeanWithBelow(bound, others),
                start.RejectMeanWithBelow(bound, others),
                start.BreakMeanWithBelow(bound, others),
                start.RequireMeanWithBelow(bound, others),
                start.EnsureMeanWithBelow(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchMeanWithBelow(bound, others),
                value.RejectMeanWithBelow(bound, others),
                value.BreakMeanWithBelow(bound, others),
                value.RequireMeanWithBelow(bound, others),
                value.EnsureMeanWithBelow(bound, others));
        }

        [Theory]
        [InlineData(2, 3.0, 5.0, new[] { 4d, 6d }, true)]
        [InlineData(2, 5.0, 6.0, new[] { 4d, 6d }, false)]
        [InlineData(2, 4.0, 4.0, new[] { 4d, 6d }, true)]
        [InlineData(5, 4.0, 6.0, new double[0], true)]
        [InlineData(5, 6.0, 7.0, new double[0], false)]
        [InlineData(5, 5.0, 5.0, new double[0], true)]
        [InlineData(double.NaN, -1000, 1000, new[] { 1d }, false)]
        public void MeanWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMeanWithInRange(lowerBound, upperBound, others),
                start.RejectMeanWithInRange(lowerBound, upperBound, others),
                start.BreakMeanWithInRange(lowerBound, upperBound, others),
                start.RequireMeanWithInRange(lowerBound, upperBound, others),
                start.EnsureMeanWithInRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchMeanWithInRange(lowerBound, upperBound, others),
                value.RejectMeanWithInRange(lowerBound, upperBound, others),
                value.BreakMeanWithInRange(lowerBound, upperBound, others),
                value.RequireMeanWithInRange(lowerBound, upperBound, others),
                value.EnsureMeanWithInRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(2, 5.0, 6.0, new[] { 4d, 6d }, true)]
        [InlineData(2, 3.0, 5.0, new[] { 4d, 6d }, false)]
        [InlineData(2, 4.0, 4.0, new[] { 4d, 6d }, false)]
        [InlineData(5, 6.0, 7.0, new double[0], true)]
        [InlineData(5, 4.0, 6.0, new double[0], false)]
        [InlineData(5, 5.0, 5.0, new double[0], false)]
        [InlineData(double.NaN, -1000, 1000, new[] { 1d }, false)]
        public void MeanWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMeanWithOutsideRange(lowerBound, upperBound, others),
                start.RejectMeanWithOutsideRange(lowerBound, upperBound, others),
                start.BreakMeanWithOutsideRange(lowerBound, upperBound, others),
                start.RequireMeanWithOutsideRange(lowerBound, upperBound, others),
                start.EnsureMeanWithOutsideRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchMeanWithOutsideRange(lowerBound, upperBound, others),
                value.RejectMeanWithOutsideRange(lowerBound, upperBound, others),
                value.BreakMeanWithOutsideRange(lowerBound, upperBound, others),
                value.RequireMeanWithOutsideRange(lowerBound, upperBound, others),
                value.EnsureMeanWithOutsideRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(1, 4.0, new[] { 5d, 3d }, true)]
        [InlineData(1, 6.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 5.0, new[] { 5d, 3d }, false)]
        [InlineData(9, 8.0, new[] { 5d, 3d }, true)]
        [InlineData(9, 10.0, new[] { 5d, 3d }, false)]
        [InlineData(9, 9.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 0.0, new double[0], true)]
        [InlineData(1, 2.0, new double[0], false)]
        [InlineData(1, 1.0, new double[0], false)]
        [InlineData(double.NaN, 0, new[] { 1d }, false)]
        [InlineData(1, 0, new[] { double.NaN }, false)]
        public void MaxWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMaxWithAbove(bound, others),
                start.RejectMaxWithAbove(bound, others),
                start.BreakMaxWithAbove(bound, others),
                start.RequireMaxWithAbove(bound, others),
                start.EnsureMaxWithAbove(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchMaxWithAbove(bound, others),
                value.RejectMaxWithAbove(bound, others),
                value.BreakMaxWithAbove(bound, others),
                value.RequireMaxWithAbove(bound, others),
                value.EnsureMaxWithAbove(bound, others));
        }

        [Theory]
        [InlineData(1, 6.0, new[] { 5d, 3d }, true)]
        [InlineData(1, 4.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 5.0, new[] { 5d, 3d }, false)]
        [InlineData(9, 10.0, new[] { 5d, 3d }, true)]
        [InlineData(9, 8.0, new[] { 5d, 3d }, false)]
        [InlineData(9, 9.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 2.0, new double[0], true)]
        [InlineData(1, 0.0, new double[0], false)]
        [InlineData(1, 1.0, new double[0], false)]
        [InlineData(double.NaN, 0, new[] { 1d }, false)]
        [InlineData(1, 0, new[] { double.NaN }, false)]
        public void MaxWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMaxWithBelow(bound, others),
                start.RejectMaxWithBelow(bound, others),
                start.BreakMaxWithBelow(bound, others),
                start.RequireMaxWithBelow(bound, others),
                start.EnsureMaxWithBelow(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchMaxWithBelow(bound, others),
                value.RejectMaxWithBelow(bound, others),
                value.BreakMaxWithBelow(bound, others),
                value.RequireMaxWithBelow(bound, others),
                value.EnsureMaxWithBelow(bound, others));
        }

        [Theory]
        [InlineData(1, 4.0, 6.0, new[] { 5d, 3d }, true)]
        [InlineData(1, 6.0, 7.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 5.0, 5.0, new[] { 5d, 3d }, true)]
        [InlineData(9, 8.0, 10.0, new[] { 5d, 3d }, true)]
        [InlineData(9, 10.0, 11.0, new[] { 5d, 3d }, false)]
        [InlineData(9, 9.0, 9.0, new[] { 5d, 3d }, true)]
        [InlineData(1, 0.0, 2.0, new double[0], true)]
        [InlineData(1, 2.0, 3.0, new double[0], false)]
        [InlineData(1, 1.0, 1.0, new double[0], true)]
        [InlineData(double.NaN, -1000, 1000, new[] { 1d }, false)]
        [InlineData(1, -1000, 1000, new[] { double.NaN }, false)]
        public void MaxWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMaxWithInRange(lowerBound, upperBound, others),
                start.RejectMaxWithInRange(lowerBound, upperBound, others),
                start.BreakMaxWithInRange(lowerBound, upperBound, others),
                start.RequireMaxWithInRange(lowerBound, upperBound, others),
                start.EnsureMaxWithInRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchMaxWithInRange(lowerBound, upperBound, others),
                value.RejectMaxWithInRange(lowerBound, upperBound, others),
                value.BreakMaxWithInRange(lowerBound, upperBound, others),
                value.RequireMaxWithInRange(lowerBound, upperBound, others),
                value.EnsureMaxWithInRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(1, 6.0, 7.0, new[] { 5d, 3d }, true)]
        [InlineData(1, 4.0, 6.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 5.0, 5.0, new[] { 5d, 3d }, false)]
        [InlineData(9, 10.0, 11.0, new[] { 5d, 3d }, true)]
        [InlineData(9, 8.0, 10.0, new[] { 5d, 3d }, false)]
        [InlineData(9, 9.0, 9.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 2.0, 3.0, new double[0], true)]
        [InlineData(1, 0.0, 2.0, new double[0], false)]
        [InlineData(1, 1.0, 1.0, new double[0], false)]
        [InlineData(double.NaN, -1000, 1000, new[] { 1d }, false)]
        [InlineData(1, -1000, 1000, new[] { double.NaN }, false)]
        public void MaxWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMaxWithOutsideRange(lowerBound, upperBound, others),
                start.RejectMaxWithOutsideRange(lowerBound, upperBound, others),
                start.BreakMaxWithOutsideRange(lowerBound, upperBound, others),
                start.RequireMaxWithOutsideRange(lowerBound, upperBound, others),
                start.EnsureMaxWithOutsideRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchMaxWithOutsideRange(lowerBound, upperBound, others),
                value.RejectMaxWithOutsideRange(lowerBound, upperBound, others),
                value.BreakMaxWithOutsideRange(lowerBound, upperBound, others),
                value.RequireMaxWithOutsideRange(lowerBound, upperBound, others),
                value.EnsureMaxWithOutsideRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(5, 0.0, new[] { 1d, 3d }, true)]
        [InlineData(5, 2.0, new[] { 1d, 3d }, false)]
        [InlineData(5, 1.0, new[] { 1d, 3d }, false)]
        [InlineData(0, -1.0, new[] { 1d, 3d }, true)]
        [InlineData(0, 1.0, new[] { 1d, 3d }, false)]
        [InlineData(0, 0.0, new[] { 1d, 3d }, false)]
        [InlineData(5, 4.0, new double[0], true)]
        [InlineData(5, 6.0, new double[0], false)]
        [InlineData(5, 5.0, new double[0], false)]
        [InlineData(double.NaN, 0, new[] { 1d }, false)]
        public void MinWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMinWithAbove(bound, others),
                start.RejectMinWithAbove(bound, others),
                start.BreakMinWithAbove(bound, others),
                start.RequireMinWithAbove(bound, others),
                start.EnsureMinWithAbove(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchMinWithAbove(bound, others),
                value.RejectMinWithAbove(bound, others),
                value.BreakMinWithAbove(bound, others),
                value.RequireMinWithAbove(bound, others),
                value.EnsureMinWithAbove(bound, others));
        }

        [Theory]
        [InlineData(5, 2.0, new[] { 1d, 3d }, true)]
        [InlineData(5, 0.0, new[] { 1d, 3d }, false)]
        [InlineData(5, 1.0, new[] { 1d, 3d }, false)]
        [InlineData(0, 1.0, new[] { 1d, 3d }, true)]
        [InlineData(0, -1.0, new[] { 1d, 3d }, false)]
        [InlineData(0, 0.0, new[] { 1d, 3d }, false)]
        [InlineData(5, 6.0, new double[0], true)]
        [InlineData(5, 4.0, new double[0], false)]
        [InlineData(5, 5.0, new double[0], false)]
        [InlineData(double.NaN, 0, new[] { 1d }, false)]
        public void MinWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMinWithBelow(bound, others),
                start.RejectMinWithBelow(bound, others),
                start.BreakMinWithBelow(bound, others),
                start.RequireMinWithBelow(bound, others),
                start.EnsureMinWithBelow(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchMinWithBelow(bound, others),
                value.RejectMinWithBelow(bound, others),
                value.BreakMinWithBelow(bound, others),
                value.RequireMinWithBelow(bound, others),
                value.EnsureMinWithBelow(bound, others));
        }

        [Theory]
        [InlineData(5, 0.0, 2.0, new[] { 1d, 3d }, true)]
        [InlineData(5, 2.0, 3.0, new[] { 1d, 3d }, false)]
        [InlineData(5, 1.0, 1.0, new[] { 1d, 3d }, true)]
        [InlineData(0, -1.0, 1.0, new[] { 1d, 3d }, true)]
        [InlineData(0, 1.0, 2.0, new[] { 1d, 3d }, false)]
        [InlineData(0, 0.0, 0.0, new[] { 1d, 3d }, true)]
        [InlineData(5, 4.0, 6.0, new double[0], true)]
        [InlineData(5, 6.0, 7.0, new double[0], false)]
        [InlineData(5, 5.0, 5.0, new double[0], true)]
        [InlineData(double.NaN, -1000, 1000, new[] { 1d }, false)]
        public void MinWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMinWithInRange(lowerBound, upperBound, others),
                start.RejectMinWithInRange(lowerBound, upperBound, others),
                start.BreakMinWithInRange(lowerBound, upperBound, others),
                start.RequireMinWithInRange(lowerBound, upperBound, others),
                start.EnsureMinWithInRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchMinWithInRange(lowerBound, upperBound, others),
                value.RejectMinWithInRange(lowerBound, upperBound, others),
                value.BreakMinWithInRange(lowerBound, upperBound, others),
                value.RequireMinWithInRange(lowerBound, upperBound, others),
                value.EnsureMinWithInRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(5, 2.0, 3.0, new[] { 1d, 3d }, true)]
        [InlineData(5, 0.0, 2.0, new[] { 1d, 3d }, false)]
        [InlineData(5, 1.0, 1.0, new[] { 1d, 3d }, false)]
        [InlineData(0, 1.0, 2.0, new[] { 1d, 3d }, true)]
        [InlineData(0, -1.0, 1.0, new[] { 1d, 3d }, false)]
        [InlineData(0, 0.0, 0.0, new[] { 1d, 3d }, false)]
        [InlineData(5, 6.0, 7.0, new double[0], true)]
        [InlineData(5, 4.0, 6.0, new double[0], false)]
        [InlineData(5, 5.0, 5.0, new double[0], false)]
        [InlineData(double.NaN, -1000, 1000, new[] { 1d }, false)]
        public void MinWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMinWithOutsideRange(lowerBound, upperBound, others),
                start.RejectMinWithOutsideRange(lowerBound, upperBound, others),
                start.BreakMinWithOutsideRange(lowerBound, upperBound, others),
                start.RequireMinWithOutsideRange(lowerBound, upperBound, others),
                start.EnsureMinWithOutsideRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchMinWithOutsideRange(lowerBound, upperBound, others),
                value.RejectMinWithOutsideRange(lowerBound, upperBound, others),
                value.BreakMinWithOutsideRange(lowerBound, upperBound, others),
                value.RequireMinWithOutsideRange(lowerBound, upperBound, others),
                value.EnsureMinWithOutsideRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(1, 3.0, new[] { 5d, 3d }, true)]
        [InlineData(1, 5.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 4.0, new[] { 5d, 3d }, false)]
        [InlineData(2, -1.0, new double[0], true)]
        [InlineData(2, 1.0, new double[0], false)]
        [InlineData(2, 0.0, new double[0], false)]
        [InlineData(double.NaN, 0, new[] { 1d }, false)]
        public void SpreadWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSpreadWithAbove(bound, others),
                start.RejectSpreadWithAbove(bound, others),
                start.BreakSpreadWithAbove(bound, others),
                start.RequireSpreadWithAbove(bound, others),
                start.EnsureSpreadWithAbove(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchSpreadWithAbove(bound, others),
                value.RejectSpreadWithAbove(bound, others),
                value.BreakSpreadWithAbove(bound, others),
                value.RequireSpreadWithAbove(bound, others),
                value.EnsureSpreadWithAbove(bound, others));
        }

        [Theory]
        [InlineData(1, 5.0, new[] { 5d, 3d }, true)]
        [InlineData(1, 3.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 4.0, new[] { 5d, 3d }, false)]
        [InlineData(2, 1.0, new double[0], true)]
        [InlineData(2, -1.0, new double[0], false)]
        [InlineData(2, 0.0, new double[0], false)]
        [InlineData(double.NaN, 0, new[] { 1d }, false)]
        public void SpreadWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSpreadWithBelow(bound, others),
                start.RejectSpreadWithBelow(bound, others),
                start.BreakSpreadWithBelow(bound, others),
                start.RequireSpreadWithBelow(bound, others),
                start.EnsureSpreadWithBelow(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchSpreadWithBelow(bound, others),
                value.RejectSpreadWithBelow(bound, others),
                value.BreakSpreadWithBelow(bound, others),
                value.RequireSpreadWithBelow(bound, others),
                value.EnsureSpreadWithBelow(bound, others));
        }

        [Theory]
        [InlineData(1, 3.0, 5.0, new[] { 5d, 3d }, true)]
        [InlineData(1, 5.0, 6.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 4.0, 4.0, new[] { 5d, 3d }, true)]
        [InlineData(2, -1.0, 1.0, new double[0], true)]
        [InlineData(2, 1.0, 2.0, new double[0], false)]
        [InlineData(2, 0.0, 0.0, new double[0], true)]
        [InlineData(double.NaN, -1000, 1000, new[] { 1d }, false)]
        public void SpreadWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSpreadWithInRange(lowerBound, upperBound, others),
                start.RejectSpreadWithInRange(lowerBound, upperBound, others),
                start.BreakSpreadWithInRange(lowerBound, upperBound, others),
                start.RequireSpreadWithInRange(lowerBound, upperBound, others),
                start.EnsureSpreadWithInRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchSpreadWithInRange(lowerBound, upperBound, others),
                value.RejectSpreadWithInRange(lowerBound, upperBound, others),
                value.BreakSpreadWithInRange(lowerBound, upperBound, others),
                value.RequireSpreadWithInRange(lowerBound, upperBound, others),
                value.EnsureSpreadWithInRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(1, 5.0, 6.0, new[] { 5d, 3d }, true)]
        [InlineData(1, 3.0, 5.0, new[] { 5d, 3d }, false)]
        [InlineData(1, 4.0, 4.0, new[] { 5d, 3d }, false)]
        [InlineData(2, 1.0, 2.0, new double[0], true)]
        [InlineData(2, -1.0, 1.0, new double[0], false)]
        [InlineData(2, 0.0, 0.0, new double[0], false)]
        [InlineData(double.NaN, -1000, 1000, new[] { 1d }, false)]
        public void SpreadWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSpreadWithOutsideRange(lowerBound, upperBound, others),
                start.RejectSpreadWithOutsideRange(lowerBound, upperBound, others),
                start.BreakSpreadWithOutsideRange(lowerBound, upperBound, others),
                start.RequireSpreadWithOutsideRange(lowerBound, upperBound, others),
                start.EnsureSpreadWithOutsideRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchSpreadWithOutsideRange(lowerBound, upperBound, others),
                value.RejectSpreadWithOutsideRange(lowerBound, upperBound, others),
                value.BreakSpreadWithOutsideRange(lowerBound, upperBound, others),
                value.RequireSpreadWithOutsideRange(lowerBound, upperBound, others),
                value.EnsureSpreadWithOutsideRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(10, 1, 20, 3, 16.5, true)]
        [InlineData(10, 1, 20, 3, 18.5, false)]
        [InlineData(10, 1, 20, 3, 17.5, false)]
        [InlineData(10, 0, 20, 0, 0, false)]
        [InlineData(10, 2, 20, 2, 14.0, true)]
        [InlineData(10, 2, 20, 2, 16.0, false)]
        [InlineData(10, 2, 20, 2, 15.0, false)]
        public void WeightedMeanWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double weight, double other, double otherWeight, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWeightedMeanWithAbove(weight, other, otherWeight, bound),
                start.RejectWeightedMeanWithAbove(weight, other, otherWeight, bound),
                start.BreakWeightedMeanWithAbove(weight, other, otherWeight, bound),
                start.RequireWeightedMeanWithAbove(weight, other, otherWeight, bound),
                start.EnsureWeightedMeanWithAbove(weight, other, otherWeight, bound));

            FlowAssert.Kinds(expected,
                value.MatchWeightedMeanWithAbove(weight, other, otherWeight, bound),
                value.RejectWeightedMeanWithAbove(weight, other, otherWeight, bound),
                value.BreakWeightedMeanWithAbove(weight, other, otherWeight, bound),
                value.RequireWeightedMeanWithAbove(weight, other, otherWeight, bound),
                value.EnsureWeightedMeanWithAbove(weight, other, otherWeight, bound));
        }

        [Theory]
        [InlineData(10, 1, 20, 3, 18.5, true)]
        [InlineData(10, 1, 20, 3, 16.5, false)]
        [InlineData(10, 1, 20, 3, 17.5, false)]
        [InlineData(10, 0, 20, 0, 0, false)]
        [InlineData(10, 2, 20, 2, 16.0, true)]
        [InlineData(10, 2, 20, 2, 14.0, false)]
        [InlineData(10, 2, 20, 2, 15.0, false)]
        public void WeightedMeanWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double weight, double other, double otherWeight, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWeightedMeanWithBelow(weight, other, otherWeight, bound),
                start.RejectWeightedMeanWithBelow(weight, other, otherWeight, bound),
                start.BreakWeightedMeanWithBelow(weight, other, otherWeight, bound),
                start.RequireWeightedMeanWithBelow(weight, other, otherWeight, bound),
                start.EnsureWeightedMeanWithBelow(weight, other, otherWeight, bound));

            FlowAssert.Kinds(expected,
                value.MatchWeightedMeanWithBelow(weight, other, otherWeight, bound),
                value.RejectWeightedMeanWithBelow(weight, other, otherWeight, bound),
                value.BreakWeightedMeanWithBelow(weight, other, otherWeight, bound),
                value.RequireWeightedMeanWithBelow(weight, other, otherWeight, bound),
                value.EnsureWeightedMeanWithBelow(weight, other, otherWeight, bound));
        }

        [Theory]
        [InlineData(10, 1, 20, 3, 16.5, 18.5, true)]
        [InlineData(10, 1, 20, 3, 18.5, 19.5, false)]
        [InlineData(10, 1, 20, 3, 17.5, 17.5, true)]
        [InlineData(10, 0, 20, 0, -1000, 1000, false)]
        [InlineData(10, 2, 20, 2, 14.0, 16.0, true)]
        [InlineData(10, 2, 20, 2, 16.0, 17.0, false)]
        [InlineData(10, 2, 20, 2, 15.0, 15.0, true)]
        public void WeightedMeanWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double weight, double other, double otherWeight, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound),
                start.RejectWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound),
                start.BreakWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound),
                start.RequireWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound),
                start.EnsureWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound),
                value.RejectWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound),
                value.BreakWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound),
                value.RequireWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound),
                value.EnsureWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(10, 1, 20, 3, 18.5, 19.5, true)]
        [InlineData(10, 1, 20, 3, 16.5, 18.5, false)]
        [InlineData(10, 1, 20, 3, 17.5, 17.5, false)]
        [InlineData(10, 0, 20, 0, -1000, 1000, false)]
        [InlineData(10, 2, 20, 2, 16.0, 17.0, true)]
        [InlineData(10, 2, 20, 2, 14.0, 16.0, false)]
        [InlineData(10, 2, 20, 2, 15.0, 15.0, false)]
        public void WeightedMeanWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double weight, double other, double otherWeight, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound),
                start.RejectWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound),
                start.BreakWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound),
                start.RequireWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound),
                start.EnsureWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound),
                value.RejectWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound),
                value.BreakWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound),
                value.RequireWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound),
                value.EnsureWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(4, 9, 5.0, true)]
        [InlineData(4, 9, 7.0, false)]
        [InlineData(4, 9, 6.0, false)]
        [InlineData(-1, 4, 0, false)]
        [InlineData(0, 5, -1.0, true)]
        [InlineData(0, 5, 1.0, false)]
        [InlineData(0, 5, 0.0, false)]
        public void GeometricMeanWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchGeometricMeanWithAbove(other, bound),
                start.RejectGeometricMeanWithAbove(other, bound),
                start.BreakGeometricMeanWithAbove(other, bound),
                start.RequireGeometricMeanWithAbove(other, bound),
                start.EnsureGeometricMeanWithAbove(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchGeometricMeanWithAbove(other, bound),
                value.RejectGeometricMeanWithAbove(other, bound),
                value.BreakGeometricMeanWithAbove(other, bound),
                value.RequireGeometricMeanWithAbove(other, bound),
                value.EnsureGeometricMeanWithAbove(other, bound));
        }

        [Theory]
        [InlineData(4, 9, 7.0, true)]
        [InlineData(4, 9, 5.0, false)]
        [InlineData(4, 9, 6.0, false)]
        [InlineData(-1, 4, 0, false)]
        [InlineData(0, 5, 1.0, true)]
        [InlineData(0, 5, -1.0, false)]
        [InlineData(0, 5, 0.0, false)]
        public void GeometricMeanWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchGeometricMeanWithBelow(other, bound),
                start.RejectGeometricMeanWithBelow(other, bound),
                start.BreakGeometricMeanWithBelow(other, bound),
                start.RequireGeometricMeanWithBelow(other, bound),
                start.EnsureGeometricMeanWithBelow(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchGeometricMeanWithBelow(other, bound),
                value.RejectGeometricMeanWithBelow(other, bound),
                value.BreakGeometricMeanWithBelow(other, bound),
                value.RequireGeometricMeanWithBelow(other, bound),
                value.EnsureGeometricMeanWithBelow(other, bound));
        }

        [Theory]
        [InlineData(4, 9, 5.0, 7.0, true)]
        [InlineData(4, 9, 7.0, 8.0, false)]
        [InlineData(4, 9, 6.0, 6.0, true)]
        [InlineData(-1, 4, -1000, 1000, false)]
        [InlineData(0, 5, -1.0, 1.0, true)]
        [InlineData(0, 5, 1.0, 2.0, false)]
        [InlineData(0, 5, 0.0, 0.0, true)]
        public void GeometricMeanWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchGeometricMeanWithInRange(other, lowerBound, upperBound),
                start.RejectGeometricMeanWithInRange(other, lowerBound, upperBound),
                start.BreakGeometricMeanWithInRange(other, lowerBound, upperBound),
                start.RequireGeometricMeanWithInRange(other, lowerBound, upperBound),
                start.EnsureGeometricMeanWithInRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchGeometricMeanWithInRange(other, lowerBound, upperBound),
                value.RejectGeometricMeanWithInRange(other, lowerBound, upperBound),
                value.BreakGeometricMeanWithInRange(other, lowerBound, upperBound),
                value.RequireGeometricMeanWithInRange(other, lowerBound, upperBound),
                value.EnsureGeometricMeanWithInRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(4, 9, 7.0, 8.0, true)]
        [InlineData(4, 9, 5.0, 7.0, false)]
        [InlineData(4, 9, 6.0, 6.0, false)]
        [InlineData(-1, 4, -1000, 1000, false)]
        [InlineData(0, 5, 1.0, 2.0, true)]
        [InlineData(0, 5, -1.0, 1.0, false)]
        [InlineData(0, 5, 0.0, 0.0, false)]
        public void GeometricMeanWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchGeometricMeanWithOutsideRange(other, lowerBound, upperBound),
                start.RejectGeometricMeanWithOutsideRange(other, lowerBound, upperBound),
                start.BreakGeometricMeanWithOutsideRange(other, lowerBound, upperBound),
                start.RequireGeometricMeanWithOutsideRange(other, lowerBound, upperBound),
                start.EnsureGeometricMeanWithOutsideRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchGeometricMeanWithOutsideRange(other, lowerBound, upperBound),
                value.RejectGeometricMeanWithOutsideRange(other, lowerBound, upperBound),
                value.BreakGeometricMeanWithOutsideRange(other, lowerBound, upperBound),
                value.RequireGeometricMeanWithOutsideRange(other, lowerBound, upperBound),
                value.EnsureGeometricMeanWithOutsideRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(2, 6, 2.0, true)]
        [InlineData(2, 6, 4.0, false)]
        [InlineData(2, 6, 3.0, false)]
        [InlineData(0, 0, 0, false)]
        [InlineData(1, -1, 0, false)]
        public void HarmonicMeanWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHarmonicMeanWithAbove(other, bound),
                start.RejectHarmonicMeanWithAbove(other, bound),
                start.BreakHarmonicMeanWithAbove(other, bound),
                start.RequireHarmonicMeanWithAbove(other, bound),
                start.EnsureHarmonicMeanWithAbove(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchHarmonicMeanWithAbove(other, bound),
                value.RejectHarmonicMeanWithAbove(other, bound),
                value.BreakHarmonicMeanWithAbove(other, bound),
                value.RequireHarmonicMeanWithAbove(other, bound),
                value.EnsureHarmonicMeanWithAbove(other, bound));
        }

        [Theory]
        [InlineData(2, 6, 4.0, true)]
        [InlineData(2, 6, 2.0, false)]
        [InlineData(2, 6, 3.0, false)]
        [InlineData(0, 0, 0, false)]
        [InlineData(1, -1, 0, false)]
        public void HarmonicMeanWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHarmonicMeanWithBelow(other, bound),
                start.RejectHarmonicMeanWithBelow(other, bound),
                start.BreakHarmonicMeanWithBelow(other, bound),
                start.RequireHarmonicMeanWithBelow(other, bound),
                start.EnsureHarmonicMeanWithBelow(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchHarmonicMeanWithBelow(other, bound),
                value.RejectHarmonicMeanWithBelow(other, bound),
                value.BreakHarmonicMeanWithBelow(other, bound),
                value.RequireHarmonicMeanWithBelow(other, bound),
                value.EnsureHarmonicMeanWithBelow(other, bound));
        }

        [Theory]
        [InlineData(2, 6, 2.0, 4.0, true)]
        [InlineData(2, 6, 4.0, 5.0, false)]
        [InlineData(2, 6, 3.0, 3.0, true)]
        [InlineData(0, 0, -1000, 1000, false)]
        [InlineData(1, -1, -1000, 1000, false)]
        public void HarmonicMeanWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHarmonicMeanWithInRange(other, lowerBound, upperBound),
                start.RejectHarmonicMeanWithInRange(other, lowerBound, upperBound),
                start.BreakHarmonicMeanWithInRange(other, lowerBound, upperBound),
                start.RequireHarmonicMeanWithInRange(other, lowerBound, upperBound),
                start.EnsureHarmonicMeanWithInRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchHarmonicMeanWithInRange(other, lowerBound, upperBound),
                value.RejectHarmonicMeanWithInRange(other, lowerBound, upperBound),
                value.BreakHarmonicMeanWithInRange(other, lowerBound, upperBound),
                value.RequireHarmonicMeanWithInRange(other, lowerBound, upperBound),
                value.EnsureHarmonicMeanWithInRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(2, 6, 4.0, 5.0, true)]
        [InlineData(2, 6, 2.0, 4.0, false)]
        [InlineData(2, 6, 3.0, 3.0, false)]
        [InlineData(0, 0, -1000, 1000, false)]
        [InlineData(1, -1, -1000, 1000, false)]
        public void HarmonicMeanWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHarmonicMeanWithOutsideRange(other, lowerBound, upperBound),
                start.RejectHarmonicMeanWithOutsideRange(other, lowerBound, upperBound),
                start.BreakHarmonicMeanWithOutsideRange(other, lowerBound, upperBound),
                start.RequireHarmonicMeanWithOutsideRange(other, lowerBound, upperBound),
                start.EnsureHarmonicMeanWithOutsideRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchHarmonicMeanWithOutsideRange(other, lowerBound, upperBound),
                value.RejectHarmonicMeanWithOutsideRange(other, lowerBound, upperBound),
                value.BreakHarmonicMeanWithOutsideRange(other, lowerBound, upperBound),
                value.RequireHarmonicMeanWithOutsideRange(other, lowerBound, upperBound),
                value.EnsureHarmonicMeanWithOutsideRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(5, -0.75, new[] { 10d, 20d }, true)]
        [InlineData(5, 1.25, new[] { 10d, 20d }, false)]
        [InlineData(5, 0.25, new[] { 10d, 20d }, false)]
        [InlineData(20, 0.0, new[] { 10d }, true)]
        [InlineData(20, 2.0, new[] { 10d }, false)]
        [InlineData(20, 1.0, new[] { 10d }, false)]
        [InlineData(0, 0, new[] { 0d }, false)]
        public void ShareOfMaxAboveAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShareOfMaxAbove(bound, others),
                start.RejectShareOfMaxAbove(bound, others),
                start.BreakShareOfMaxAbove(bound, others),
                start.RequireShareOfMaxAbove(bound, others),
                start.EnsureShareOfMaxAbove(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchShareOfMaxAbove(bound, others),
                value.RejectShareOfMaxAbove(bound, others),
                value.BreakShareOfMaxAbove(bound, others),
                value.RequireShareOfMaxAbove(bound, others),
                value.EnsureShareOfMaxAbove(bound, others));
        }

        [Theory]
        [InlineData(5, 1.25, new[] { 10d, 20d }, true)]
        [InlineData(5, -0.75, new[] { 10d, 20d }, false)]
        [InlineData(5, 0.25, new[] { 10d, 20d }, false)]
        [InlineData(20, 2.0, new[] { 10d }, true)]
        [InlineData(20, 0.0, new[] { 10d }, false)]
        [InlineData(20, 1.0, new[] { 10d }, false)]
        [InlineData(0, 0, new[] { 0d }, false)]
        public void ShareOfMaxBelowAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShareOfMaxBelow(bound, others),
                start.RejectShareOfMaxBelow(bound, others),
                start.BreakShareOfMaxBelow(bound, others),
                start.RequireShareOfMaxBelow(bound, others),
                start.EnsureShareOfMaxBelow(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchShareOfMaxBelow(bound, others),
                value.RejectShareOfMaxBelow(bound, others),
                value.BreakShareOfMaxBelow(bound, others),
                value.RequireShareOfMaxBelow(bound, others),
                value.EnsureShareOfMaxBelow(bound, others));
        }

        [Theory]
        [InlineData(5, -0.75, 1.25, new[] { 10d, 20d }, true)]
        [InlineData(5, 1.25, 2.25, new[] { 10d, 20d }, false)]
        [InlineData(5, 0.25, 0.25, new[] { 10d, 20d }, true)]
        [InlineData(20, 0.0, 2.0, new[] { 10d }, true)]
        [InlineData(20, 2.0, 3.0, new[] { 10d }, false)]
        [InlineData(20, 1.0, 1.0, new[] { 10d }, true)]
        [InlineData(0, -1000, 1000, new[] { 0d }, false)]
        public void ShareOfMaxInRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShareOfMaxInRange(lowerBound, upperBound, others),
                start.RejectShareOfMaxInRange(lowerBound, upperBound, others),
                start.BreakShareOfMaxInRange(lowerBound, upperBound, others),
                start.RequireShareOfMaxInRange(lowerBound, upperBound, others),
                start.EnsureShareOfMaxInRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchShareOfMaxInRange(lowerBound, upperBound, others),
                value.RejectShareOfMaxInRange(lowerBound, upperBound, others),
                value.BreakShareOfMaxInRange(lowerBound, upperBound, others),
                value.RequireShareOfMaxInRange(lowerBound, upperBound, others),
                value.EnsureShareOfMaxInRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(5, 1.25, 2.25, new[] { 10d, 20d }, true)]
        [InlineData(5, -0.75, 1.25, new[] { 10d, 20d }, false)]
        [InlineData(5, 0.25, 0.25, new[] { 10d, 20d }, false)]
        [InlineData(20, 2.0, 3.0, new[] { 10d }, true)]
        [InlineData(20, 0.0, 2.0, new[] { 10d }, false)]
        [InlineData(20, 1.0, 1.0, new[] { 10d }, false)]
        [InlineData(0, -1000, 1000, new[] { 0d }, false)]
        public void ShareOfMaxOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShareOfMaxOutsideRange(lowerBound, upperBound, others),
                start.RejectShareOfMaxOutsideRange(lowerBound, upperBound, others),
                start.BreakShareOfMaxOutsideRange(lowerBound, upperBound, others),
                start.RequireShareOfMaxOutsideRange(lowerBound, upperBound, others),
                start.EnsureShareOfMaxOutsideRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchShareOfMaxOutsideRange(lowerBound, upperBound, others),
                value.RejectShareOfMaxOutsideRange(lowerBound, upperBound, others),
                value.BreakShareOfMaxOutsideRange(lowerBound, upperBound, others),
                value.RequireShareOfMaxOutsideRange(lowerBound, upperBound, others),
                value.EnsureShareOfMaxOutsideRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(1, -0.5, new[] { 1d }, true)]
        [InlineData(1, 1.5, new[] { 1d }, false)]
        [InlineData(1, 0.5, new[] { 1d }, false)]
        [InlineData(1, 0.0, new[] { 0d }, true)]
        [InlineData(1, 2.0, new[] { 0d }, false)]
        [InlineData(1, 1.0, new[] { 0d }, false)]
        [InlineData(1, -0.75, new[] { 1d, 1d, 1d }, true)]
        [InlineData(1, 1.25, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, 0.25, new[] { 1d, 1d, 1d }, false)]
        [InlineData(0, 0, new[] { 0d }, false)]
        [InlineData(-1, 0, new[] { 2d }, false)]
        public void HerfindahlWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHerfindahlWithAbove(bound, others),
                start.RejectHerfindahlWithAbove(bound, others),
                start.BreakHerfindahlWithAbove(bound, others),
                start.RequireHerfindahlWithAbove(bound, others),
                start.EnsureHerfindahlWithAbove(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchHerfindahlWithAbove(bound, others),
                value.RejectHerfindahlWithAbove(bound, others),
                value.BreakHerfindahlWithAbove(bound, others),
                value.RequireHerfindahlWithAbove(bound, others),
                value.EnsureHerfindahlWithAbove(bound, others));
        }

        [Theory]
        [InlineData(1, 1.5, new[] { 1d }, true)]
        [InlineData(1, -0.5, new[] { 1d }, false)]
        [InlineData(1, 0.5, new[] { 1d }, false)]
        [InlineData(1, 2.0, new[] { 0d }, true)]
        [InlineData(1, 0.0, new[] { 0d }, false)]
        [InlineData(1, 1.0, new[] { 0d }, false)]
        [InlineData(1, 1.25, new[] { 1d, 1d, 1d }, true)]
        [InlineData(1, -0.75, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, 0.25, new[] { 1d, 1d, 1d }, false)]
        [InlineData(0, 0, new[] { 0d }, false)]
        [InlineData(-1, 0, new[] { 2d }, false)]
        public void HerfindahlWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHerfindahlWithBelow(bound, others),
                start.RejectHerfindahlWithBelow(bound, others),
                start.BreakHerfindahlWithBelow(bound, others),
                start.RequireHerfindahlWithBelow(bound, others),
                start.EnsureHerfindahlWithBelow(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchHerfindahlWithBelow(bound, others),
                value.RejectHerfindahlWithBelow(bound, others),
                value.BreakHerfindahlWithBelow(bound, others),
                value.RequireHerfindahlWithBelow(bound, others),
                value.EnsureHerfindahlWithBelow(bound, others));
        }

        [Theory]
        [InlineData(1, -0.5, 1.5, new[] { 1d }, true)]
        [InlineData(1, 1.5, 2.5, new[] { 1d }, false)]
        [InlineData(1, 0.5, 0.5, new[] { 1d }, true)]
        [InlineData(1, 0.0, 2.0, new[] { 0d }, true)]
        [InlineData(1, 2.0, 3.0, new[] { 0d }, false)]
        [InlineData(1, 1.0, 1.0, new[] { 0d }, true)]
        [InlineData(1, -0.75, 1.25, new[] { 1d, 1d, 1d }, true)]
        [InlineData(1, 1.25, 2.25, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, 0.25, 0.25, new[] { 1d, 1d, 1d }, true)]
        [InlineData(0, -1000, 1000, new[] { 0d }, false)]
        [InlineData(-1, -1000, 1000, new[] { 2d }, false)]
        public void HerfindahlWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHerfindahlWithInRange(lowerBound, upperBound, others),
                start.RejectHerfindahlWithInRange(lowerBound, upperBound, others),
                start.BreakHerfindahlWithInRange(lowerBound, upperBound, others),
                start.RequireHerfindahlWithInRange(lowerBound, upperBound, others),
                start.EnsureHerfindahlWithInRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchHerfindahlWithInRange(lowerBound, upperBound, others),
                value.RejectHerfindahlWithInRange(lowerBound, upperBound, others),
                value.BreakHerfindahlWithInRange(lowerBound, upperBound, others),
                value.RequireHerfindahlWithInRange(lowerBound, upperBound, others),
                value.EnsureHerfindahlWithInRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(1, 1.5, 2.5, new[] { 1d }, true)]
        [InlineData(1, -0.5, 1.5, new[] { 1d }, false)]
        [InlineData(1, 0.5, 0.5, new[] { 1d }, false)]
        [InlineData(1, 2.0, 3.0, new[] { 0d }, true)]
        [InlineData(1, 0.0, 2.0, new[] { 0d }, false)]
        [InlineData(1, 1.0, 1.0, new[] { 0d }, false)]
        [InlineData(1, 1.25, 2.25, new[] { 1d, 1d, 1d }, true)]
        [InlineData(1, -0.75, 1.25, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, 0.25, 0.25, new[] { 1d, 1d, 1d }, false)]
        [InlineData(0, -1000, 1000, new[] { 0d }, false)]
        [InlineData(-1, -1000, 1000, new[] { 2d }, false)]
        public void HerfindahlWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHerfindahlWithOutsideRange(lowerBound, upperBound, others),
                start.RejectHerfindahlWithOutsideRange(lowerBound, upperBound, others),
                start.BreakHerfindahlWithOutsideRange(lowerBound, upperBound, others),
                start.RequireHerfindahlWithOutsideRange(lowerBound, upperBound, others),
                start.EnsureHerfindahlWithOutsideRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchHerfindahlWithOutsideRange(lowerBound, upperBound, others),
                value.RejectHerfindahlWithOutsideRange(lowerBound, upperBound, others),
                value.BreakHerfindahlWithOutsideRange(lowerBound, upperBound, others),
                value.RequireHerfindahlWithOutsideRange(lowerBound, upperBound, others),
                value.EnsureHerfindahlWithOutsideRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(1, 0.0, new[] { 1d }, true)]
        [InlineData(1, 2.0, new[] { 1d }, false)]
        [InlineData(1, 1.0, new[] { 1d }, false)]
        [InlineData(1, 1.0, new[] { 1d, 1d, 1d }, true)]
        [InlineData(1, 3.0, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, 2.0, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, -1.0, new[] { 0d }, true)]
        [InlineData(1, 1.0, new[] { 0d }, false)]
        [InlineData(1, 0.0, new[] { 0d }, false)]
        [InlineData(0, 0, new[] { 0d }, false)]
        public void EntropyOfSharesWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEntropyOfSharesWithAbove(bound, others),
                start.RejectEntropyOfSharesWithAbove(bound, others),
                start.BreakEntropyOfSharesWithAbove(bound, others),
                start.RequireEntropyOfSharesWithAbove(bound, others),
                start.EnsureEntropyOfSharesWithAbove(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchEntropyOfSharesWithAbove(bound, others),
                value.RejectEntropyOfSharesWithAbove(bound, others),
                value.BreakEntropyOfSharesWithAbove(bound, others),
                value.RequireEntropyOfSharesWithAbove(bound, others),
                value.EnsureEntropyOfSharesWithAbove(bound, others));
        }

        [Theory]
        [InlineData(1, 2.0, new[] { 1d }, true)]
        [InlineData(1, 0.0, new[] { 1d }, false)]
        [InlineData(1, 1.0, new[] { 1d }, false)]
        [InlineData(1, 3.0, new[] { 1d, 1d, 1d }, true)]
        [InlineData(1, 1.0, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, 2.0, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, 1.0, new[] { 0d }, true)]
        [InlineData(1, -1.0, new[] { 0d }, false)]
        [InlineData(1, 0.0, new[] { 0d }, false)]
        [InlineData(0, 0, new[] { 0d }, false)]
        public void EntropyOfSharesWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double bound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEntropyOfSharesWithBelow(bound, others),
                start.RejectEntropyOfSharesWithBelow(bound, others),
                start.BreakEntropyOfSharesWithBelow(bound, others),
                start.RequireEntropyOfSharesWithBelow(bound, others),
                start.EnsureEntropyOfSharesWithBelow(bound, others));

            FlowAssert.Kinds(expected,
                value.MatchEntropyOfSharesWithBelow(bound, others),
                value.RejectEntropyOfSharesWithBelow(bound, others),
                value.BreakEntropyOfSharesWithBelow(bound, others),
                value.RequireEntropyOfSharesWithBelow(bound, others),
                value.EnsureEntropyOfSharesWithBelow(bound, others));
        }

        [Theory]
        [InlineData(1, 0.0, 2.0, new[] { 1d }, true)]
        [InlineData(1, 2.0, 3.0, new[] { 1d }, false)]
        [InlineData(1, 1.0, 1.0, new[] { 1d }, true)]
        [InlineData(1, 1.0, 3.0, new[] { 1d, 1d, 1d }, true)]
        [InlineData(1, 3.0, 4.0, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, 2.0, 2.0, new[] { 1d, 1d, 1d }, true)]
        [InlineData(1, -1.0, 1.0, new[] { 0d }, true)]
        [InlineData(1, 1.0, 2.0, new[] { 0d }, false)]
        [InlineData(1, 0.0, 0.0, new[] { 0d }, true)]
        [InlineData(0, -1000, 1000, new[] { 0d }, false)]
        public void EntropyOfSharesWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEntropyOfSharesWithInRange(lowerBound, upperBound, others),
                start.RejectEntropyOfSharesWithInRange(lowerBound, upperBound, others),
                start.BreakEntropyOfSharesWithInRange(lowerBound, upperBound, others),
                start.RequireEntropyOfSharesWithInRange(lowerBound, upperBound, others),
                start.EnsureEntropyOfSharesWithInRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchEntropyOfSharesWithInRange(lowerBound, upperBound, others),
                value.RejectEntropyOfSharesWithInRange(lowerBound, upperBound, others),
                value.BreakEntropyOfSharesWithInRange(lowerBound, upperBound, others),
                value.RequireEntropyOfSharesWithInRange(lowerBound, upperBound, others),
                value.EnsureEntropyOfSharesWithInRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(1, 2.0, 3.0, new[] { 1d }, true)]
        [InlineData(1, 0.0, 2.0, new[] { 1d }, false)]
        [InlineData(1, 1.0, 1.0, new[] { 1d }, false)]
        [InlineData(1, 3.0, 4.0, new[] { 1d, 1d, 1d }, true)]
        [InlineData(1, 1.0, 3.0, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, 2.0, 2.0, new[] { 1d, 1d, 1d }, false)]
        [InlineData(1, 1.0, 2.0, new[] { 0d }, true)]
        [InlineData(1, -1.0, 1.0, new[] { 0d }, false)]
        [InlineData(1, 0.0, 0.0, new[] { 0d }, false)]
        [InlineData(0, -1000, 1000, new[] { 0d }, false)]
        public void EntropyOfSharesWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double lowerBound, double upperBound, double[] others, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEntropyOfSharesWithOutsideRange(lowerBound, upperBound, others),
                start.RejectEntropyOfSharesWithOutsideRange(lowerBound, upperBound, others),
                start.BreakEntropyOfSharesWithOutsideRange(lowerBound, upperBound, others),
                start.RequireEntropyOfSharesWithOutsideRange(lowerBound, upperBound, others),
                start.EnsureEntropyOfSharesWithOutsideRange(lowerBound, upperBound, others));

            FlowAssert.Kinds(expected,
                value.MatchEntropyOfSharesWithOutsideRange(lowerBound, upperBound, others),
                value.RejectEntropyOfSharesWithOutsideRange(lowerBound, upperBound, others),
                value.BreakEntropyOfSharesWithOutsideRange(lowerBound, upperBound, others),
                value.RequireEntropyOfSharesWithOutsideRange(lowerBound, upperBound, others),
                value.EnsureEntropyOfSharesWithOutsideRange(lowerBound, upperBound, others));
        }

        [Theory]
        [InlineData(30, 100, 69.0, true)]
        [InlineData(30, 100, 71.0, false)]
        [InlineData(30, 100, 70.0, false)]
        [InlineData(120, 100, -21.0, true)]
        [InlineData(120, 100, -19.0, false)]
        [InlineData(120, 100, -20.0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        public void HeadroomToAboveAppliesTheOutcomeOfEveryStepKind(double value, double limit, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHeadroomToAbove(limit, bound),
                start.RejectHeadroomToAbove(limit, bound),
                start.BreakHeadroomToAbove(limit, bound),
                start.RequireHeadroomToAbove(limit, bound),
                start.EnsureHeadroomToAbove(limit, bound));

            FlowAssert.Kinds(expected,
                value.MatchHeadroomToAbove(limit, bound),
                value.RejectHeadroomToAbove(limit, bound),
                value.BreakHeadroomToAbove(limit, bound),
                value.RequireHeadroomToAbove(limit, bound),
                value.EnsureHeadroomToAbove(limit, bound));
        }

        [Theory]
        [InlineData(30, 100, 71.0, true)]
        [InlineData(30, 100, 69.0, false)]
        [InlineData(30, 100, 70.0, false)]
        [InlineData(120, 100, -19.0, true)]
        [InlineData(120, 100, -21.0, false)]
        [InlineData(120, 100, -20.0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        public void HeadroomToBelowAppliesTheOutcomeOfEveryStepKind(double value, double limit, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHeadroomToBelow(limit, bound),
                start.RejectHeadroomToBelow(limit, bound),
                start.BreakHeadroomToBelow(limit, bound),
                start.RequireHeadroomToBelow(limit, bound),
                start.EnsureHeadroomToBelow(limit, bound));

            FlowAssert.Kinds(expected,
                value.MatchHeadroomToBelow(limit, bound),
                value.RejectHeadroomToBelow(limit, bound),
                value.BreakHeadroomToBelow(limit, bound),
                value.RequireHeadroomToBelow(limit, bound),
                value.EnsureHeadroomToBelow(limit, bound));
        }

        [Theory]
        [InlineData(30, 100, 69.0, 71.0, true)]
        [InlineData(30, 100, 71.0, 72.0, false)]
        [InlineData(30, 100, 70.0, 70.0, true)]
        [InlineData(120, 100, -21.0, -19.0, true)]
        [InlineData(120, 100, -19.0, -18.0, false)]
        [InlineData(120, 100, -20.0, -20.0, true)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        public void HeadroomToInRangeAppliesTheOutcomeOfEveryStepKind(double value, double limit, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHeadroomToInRange(limit, lowerBound, upperBound),
                start.RejectHeadroomToInRange(limit, lowerBound, upperBound),
                start.BreakHeadroomToInRange(limit, lowerBound, upperBound),
                start.RequireHeadroomToInRange(limit, lowerBound, upperBound),
                start.EnsureHeadroomToInRange(limit, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchHeadroomToInRange(limit, lowerBound, upperBound),
                value.RejectHeadroomToInRange(limit, lowerBound, upperBound),
                value.BreakHeadroomToInRange(limit, lowerBound, upperBound),
                value.RequireHeadroomToInRange(limit, lowerBound, upperBound),
                value.EnsureHeadroomToInRange(limit, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(30, 100, 71.0, 72.0, true)]
        [InlineData(30, 100, 69.0, 71.0, false)]
        [InlineData(30, 100, 70.0, 70.0, false)]
        [InlineData(120, 100, -19.0, -18.0, true)]
        [InlineData(120, 100, -21.0, -19.0, false)]
        [InlineData(120, 100, -20.0, -20.0, false)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        public void HeadroomToOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double limit, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHeadroomToOutsideRange(limit, lowerBound, upperBound),
                start.RejectHeadroomToOutsideRange(limit, lowerBound, upperBound),
                start.BreakHeadroomToOutsideRange(limit, lowerBound, upperBound),
                start.RequireHeadroomToOutsideRange(limit, lowerBound, upperBound),
                start.EnsureHeadroomToOutsideRange(limit, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchHeadroomToOutsideRange(limit, lowerBound, upperBound),
                value.RejectHeadroomToOutsideRange(limit, lowerBound, upperBound),
                value.BreakHeadroomToOutsideRange(limit, lowerBound, upperBound),
                value.RequireHeadroomToOutsideRange(limit, lowerBound, upperBound),
                value.EnsureHeadroomToOutsideRange(limit, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(120, 100, 19.0, true)]
        [InlineData(120, 100, 21.0, false)]
        [InlineData(120, 100, 20.0, false)]
        [InlineData(80, 100, -1.0, true)]
        [InlineData(80, 100, 1.0, false)]
        [InlineData(80, 100, 0.0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        public void ExcessOverAboveAppliesTheOutcomeOfEveryStepKind(double value, double limit, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchExcessOverAbove(limit, bound),
                start.RejectExcessOverAbove(limit, bound),
                start.BreakExcessOverAbove(limit, bound),
                start.RequireExcessOverAbove(limit, bound),
                start.EnsureExcessOverAbove(limit, bound));

            FlowAssert.Kinds(expected,
                value.MatchExcessOverAbove(limit, bound),
                value.RejectExcessOverAbove(limit, bound),
                value.BreakExcessOverAbove(limit, bound),
                value.RequireExcessOverAbove(limit, bound),
                value.EnsureExcessOverAbove(limit, bound));
        }

        [Theory]
        [InlineData(120, 100, 21.0, true)]
        [InlineData(120, 100, 19.0, false)]
        [InlineData(120, 100, 20.0, false)]
        [InlineData(80, 100, 1.0, true)]
        [InlineData(80, 100, -1.0, false)]
        [InlineData(80, 100, 0.0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        public void ExcessOverBelowAppliesTheOutcomeOfEveryStepKind(double value, double limit, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchExcessOverBelow(limit, bound),
                start.RejectExcessOverBelow(limit, bound),
                start.BreakExcessOverBelow(limit, bound),
                start.RequireExcessOverBelow(limit, bound),
                start.EnsureExcessOverBelow(limit, bound));

            FlowAssert.Kinds(expected,
                value.MatchExcessOverBelow(limit, bound),
                value.RejectExcessOverBelow(limit, bound),
                value.BreakExcessOverBelow(limit, bound),
                value.RequireExcessOverBelow(limit, bound),
                value.EnsureExcessOverBelow(limit, bound));
        }

        [Theory]
        [InlineData(120, 100, 19.0, 21.0, true)]
        [InlineData(120, 100, 21.0, 22.0, false)]
        [InlineData(120, 100, 20.0, 20.0, true)]
        [InlineData(80, 100, -1.0, 1.0, true)]
        [InlineData(80, 100, 1.0, 2.0, false)]
        [InlineData(80, 100, 0.0, 0.0, true)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        public void ExcessOverInRangeAppliesTheOutcomeOfEveryStepKind(double value, double limit, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchExcessOverInRange(limit, lowerBound, upperBound),
                start.RejectExcessOverInRange(limit, lowerBound, upperBound),
                start.BreakExcessOverInRange(limit, lowerBound, upperBound),
                start.RequireExcessOverInRange(limit, lowerBound, upperBound),
                start.EnsureExcessOverInRange(limit, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchExcessOverInRange(limit, lowerBound, upperBound),
                value.RejectExcessOverInRange(limit, lowerBound, upperBound),
                value.BreakExcessOverInRange(limit, lowerBound, upperBound),
                value.RequireExcessOverInRange(limit, lowerBound, upperBound),
                value.EnsureExcessOverInRange(limit, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(120, 100, 21.0, 22.0, true)]
        [InlineData(120, 100, 19.0, 21.0, false)]
        [InlineData(120, 100, 20.0, 20.0, false)]
        [InlineData(80, 100, 1.0, 2.0, true)]
        [InlineData(80, 100, -1.0, 1.0, false)]
        [InlineData(80, 100, 0.0, 0.0, false)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        public void ExcessOverOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double limit, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchExcessOverOutsideRange(limit, lowerBound, upperBound),
                start.RejectExcessOverOutsideRange(limit, lowerBound, upperBound),
                start.BreakExcessOverOutsideRange(limit, lowerBound, upperBound),
                start.RequireExcessOverOutsideRange(limit, lowerBound, upperBound),
                start.EnsureExcessOverOutsideRange(limit, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchExcessOverOutsideRange(limit, lowerBound, upperBound),
                value.RejectExcessOverOutsideRange(limit, lowerBound, upperBound),
                value.BreakExcessOverOutsideRange(limit, lowerBound, upperBound),
                value.RequireExcessOverOutsideRange(limit, lowerBound, upperBound),
                value.EnsureExcessOverOutsideRange(limit, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(30, 100, 69.0, true)]
        [InlineData(30, 100, 71.0, false)]
        [InlineData(30, 100, 70.0, false)]
        [InlineData(120, 100, -1.0, true)]
        [InlineData(120, 100, 1.0, false)]
        [InlineData(120, 100, 0.0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        public void ShortfallBelowAboveAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShortfallBelowAbove(minimum, bound),
                start.RejectShortfallBelowAbove(minimum, bound),
                start.BreakShortfallBelowAbove(minimum, bound),
                start.RequireShortfallBelowAbove(minimum, bound),
                start.EnsureShortfallBelowAbove(minimum, bound));

            FlowAssert.Kinds(expected,
                value.MatchShortfallBelowAbove(minimum, bound),
                value.RejectShortfallBelowAbove(minimum, bound),
                value.BreakShortfallBelowAbove(minimum, bound),
                value.RequireShortfallBelowAbove(minimum, bound),
                value.EnsureShortfallBelowAbove(minimum, bound));
        }

        [Theory]
        [InlineData(30, 100, 71.0, true)]
        [InlineData(30, 100, 69.0, false)]
        [InlineData(30, 100, 70.0, false)]
        [InlineData(120, 100, 1.0, true)]
        [InlineData(120, 100, -1.0, false)]
        [InlineData(120, 100, 0.0, false)]
        [InlineData(double.NaN, 1, 0, false)]
        public void ShortfallBelowBelowAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShortfallBelowBelow(minimum, bound),
                start.RejectShortfallBelowBelow(minimum, bound),
                start.BreakShortfallBelowBelow(minimum, bound),
                start.RequireShortfallBelowBelow(minimum, bound),
                start.EnsureShortfallBelowBelow(minimum, bound));

            FlowAssert.Kinds(expected,
                value.MatchShortfallBelowBelow(minimum, bound),
                value.RejectShortfallBelowBelow(minimum, bound),
                value.BreakShortfallBelowBelow(minimum, bound),
                value.RequireShortfallBelowBelow(minimum, bound),
                value.EnsureShortfallBelowBelow(minimum, bound));
        }

        [Theory]
        [InlineData(30, 100, 69.0, 71.0, true)]
        [InlineData(30, 100, 71.0, 72.0, false)]
        [InlineData(30, 100, 70.0, 70.0, true)]
        [InlineData(120, 100, -1.0, 1.0, true)]
        [InlineData(120, 100, 1.0, 2.0, false)]
        [InlineData(120, 100, 0.0, 0.0, true)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        public void ShortfallBelowInRangeAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShortfallBelowInRange(minimum, lowerBound, upperBound),
                start.RejectShortfallBelowInRange(minimum, lowerBound, upperBound),
                start.BreakShortfallBelowInRange(minimum, lowerBound, upperBound),
                start.RequireShortfallBelowInRange(minimum, lowerBound, upperBound),
                start.EnsureShortfallBelowInRange(minimum, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchShortfallBelowInRange(minimum, lowerBound, upperBound),
                value.RejectShortfallBelowInRange(minimum, lowerBound, upperBound),
                value.BreakShortfallBelowInRange(minimum, lowerBound, upperBound),
                value.RequireShortfallBelowInRange(minimum, lowerBound, upperBound),
                value.EnsureShortfallBelowInRange(minimum, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(30, 100, 71.0, 72.0, true)]
        [InlineData(30, 100, 69.0, 71.0, false)]
        [InlineData(30, 100, 70.0, 70.0, false)]
        [InlineData(120, 100, 1.0, 2.0, true)]
        [InlineData(120, 100, -1.0, 1.0, false)]
        [InlineData(120, 100, 0.0, 0.0, false)]
        [InlineData(double.NaN, 1, -1000, 1000, false)]
        public void ShortfallBelowOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double minimum, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShortfallBelowOutsideRange(minimum, lowerBound, upperBound),
                start.RejectShortfallBelowOutsideRange(minimum, lowerBound, upperBound),
                start.BreakShortfallBelowOutsideRange(minimum, lowerBound, upperBound),
                start.RequireShortfallBelowOutsideRange(minimum, lowerBound, upperBound),
                start.EnsureShortfallBelowOutsideRange(minimum, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchShortfallBelowOutsideRange(minimum, lowerBound, upperBound),
                value.RejectShortfallBelowOutsideRange(minimum, lowerBound, upperBound),
                value.BreakShortfallBelowOutsideRange(minimum, lowerBound, upperBound),
                value.RequireShortfallBelowOutsideRange(minimum, lowerBound, upperBound),
                value.EnsureShortfallBelowOutsideRange(minimum, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(9500, 10000, 499.0, true)]
        [InlineData(9500, 10000, 501.0, false)]
        [InlineData(9500, 10000, 500.0, false)]
        [InlineData(10500, 10000, 499.0, true)]
        [InlineData(10500, 10000, 501.0, false)]
        [InlineData(10500, 10000, 500.0, false)]
        [InlineData(10000, 10000, -1.0, true)]
        [InlineData(10000, 10000, 1.0, false)]
        [InlineData(10000, 10000, 0.0, false)]
        public void DistanceToThresholdAboveAppliesTheOutcomeOfEveryStepKind(double value, double threshold, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDistanceToThresholdAbove(threshold, bound),
                start.RejectDistanceToThresholdAbove(threshold, bound),
                start.BreakDistanceToThresholdAbove(threshold, bound),
                start.RequireDistanceToThresholdAbove(threshold, bound),
                start.EnsureDistanceToThresholdAbove(threshold, bound));

            FlowAssert.Kinds(expected,
                value.MatchDistanceToThresholdAbove(threshold, bound),
                value.RejectDistanceToThresholdAbove(threshold, bound),
                value.BreakDistanceToThresholdAbove(threshold, bound),
                value.RequireDistanceToThresholdAbove(threshold, bound),
                value.EnsureDistanceToThresholdAbove(threshold, bound));
        }

        [Theory]
        [InlineData(9500, 10000, 501.0, true)]
        [InlineData(9500, 10000, 499.0, false)]
        [InlineData(9500, 10000, 500.0, false)]
        [InlineData(10500, 10000, 501.0, true)]
        [InlineData(10500, 10000, 499.0, false)]
        [InlineData(10500, 10000, 500.0, false)]
        [InlineData(10000, 10000, 1.0, true)]
        [InlineData(10000, 10000, -1.0, false)]
        [InlineData(10000, 10000, 0.0, false)]
        public void DistanceToThresholdBelowAppliesTheOutcomeOfEveryStepKind(double value, double threshold, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDistanceToThresholdBelow(threshold, bound),
                start.RejectDistanceToThresholdBelow(threshold, bound),
                start.BreakDistanceToThresholdBelow(threshold, bound),
                start.RequireDistanceToThresholdBelow(threshold, bound),
                start.EnsureDistanceToThresholdBelow(threshold, bound));

            FlowAssert.Kinds(expected,
                value.MatchDistanceToThresholdBelow(threshold, bound),
                value.RejectDistanceToThresholdBelow(threshold, bound),
                value.BreakDistanceToThresholdBelow(threshold, bound),
                value.RequireDistanceToThresholdBelow(threshold, bound),
                value.EnsureDistanceToThresholdBelow(threshold, bound));
        }

        [Theory]
        [InlineData(9500, 10000, 499.0, 501.0, true)]
        [InlineData(9500, 10000, 501.0, 502.0, false)]
        [InlineData(9500, 10000, 500.0, 500.0, true)]
        [InlineData(10500, 10000, 499.0, 501.0, true)]
        [InlineData(10500, 10000, 501.0, 502.0, false)]
        [InlineData(10500, 10000, 500.0, 500.0, true)]
        [InlineData(10000, 10000, -1.0, 1.0, true)]
        [InlineData(10000, 10000, 1.0, 2.0, false)]
        [InlineData(10000, 10000, 0.0, 0.0, true)]
        public void DistanceToThresholdInRangeAppliesTheOutcomeOfEveryStepKind(double value, double threshold, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDistanceToThresholdInRange(threshold, lowerBound, upperBound),
                start.RejectDistanceToThresholdInRange(threshold, lowerBound, upperBound),
                start.BreakDistanceToThresholdInRange(threshold, lowerBound, upperBound),
                start.RequireDistanceToThresholdInRange(threshold, lowerBound, upperBound),
                start.EnsureDistanceToThresholdInRange(threshold, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchDistanceToThresholdInRange(threshold, lowerBound, upperBound),
                value.RejectDistanceToThresholdInRange(threshold, lowerBound, upperBound),
                value.BreakDistanceToThresholdInRange(threshold, lowerBound, upperBound),
                value.RequireDistanceToThresholdInRange(threshold, lowerBound, upperBound),
                value.EnsureDistanceToThresholdInRange(threshold, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(9500, 10000, 501.0, 502.0, true)]
        [InlineData(9500, 10000, 499.0, 501.0, false)]
        [InlineData(9500, 10000, 500.0, 500.0, false)]
        [InlineData(10500, 10000, 501.0, 502.0, true)]
        [InlineData(10500, 10000, 499.0, 501.0, false)]
        [InlineData(10500, 10000, 500.0, 500.0, false)]
        [InlineData(10000, 10000, 1.0, 2.0, true)]
        [InlineData(10000, 10000, -1.0, 1.0, false)]
        [InlineData(10000, 10000, 0.0, 0.0, false)]
        public void DistanceToThresholdOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double threshold, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDistanceToThresholdOutsideRange(threshold, lowerBound, upperBound),
                start.RejectDistanceToThresholdOutsideRange(threshold, lowerBound, upperBound),
                start.BreakDistanceToThresholdOutsideRange(threshold, lowerBound, upperBound),
                start.RequireDistanceToThresholdOutsideRange(threshold, lowerBound, upperBound),
                start.EnsureDistanceToThresholdOutsideRange(threshold, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchDistanceToThresholdOutsideRange(threshold, lowerBound, upperBound),
                value.RejectDistanceToThresholdOutsideRange(threshold, lowerBound, upperBound),
                value.BreakDistanceToThresholdOutsideRange(threshold, lowerBound, upperBound),
                value.RequireDistanceToThresholdOutsideRange(threshold, lowerBound, upperBound),
                value.EnsureDistanceToThresholdOutsideRange(threshold, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(9500, 10000, 4.0, true)]
        [InlineData(9500, 10000, 6.0, false)]
        [InlineData(9500, 10000, 5.0, false)]
        [InlineData(10000, 10000, -1.0, true)]
        [InlineData(10000, 10000, 1.0, false)]
        [InlineData(10000, 10000, 0.0, false)]
        [InlineData(10500, 10000, -6.0, true)]
        [InlineData(10500, 10000, -4.0, false)]
        [InlineData(10500, 10000, -5.0, false)]
        [InlineData(5, 0, 0, false)]
        public void PercentBelowThresholdAboveAppliesTheOutcomeOfEveryStepKind(double value, double threshold, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentBelowThresholdAbove(threshold, bound),
                start.RejectPercentBelowThresholdAbove(threshold, bound),
                start.BreakPercentBelowThresholdAbove(threshold, bound),
                start.RequirePercentBelowThresholdAbove(threshold, bound),
                start.EnsurePercentBelowThresholdAbove(threshold, bound));

            FlowAssert.Kinds(expected,
                value.MatchPercentBelowThresholdAbove(threshold, bound),
                value.RejectPercentBelowThresholdAbove(threshold, bound),
                value.BreakPercentBelowThresholdAbove(threshold, bound),
                value.RequirePercentBelowThresholdAbove(threshold, bound),
                value.EnsurePercentBelowThresholdAbove(threshold, bound));
        }

        [Theory]
        [InlineData(9500, 10000, 6.0, true)]
        [InlineData(9500, 10000, 4.0, false)]
        [InlineData(9500, 10000, 5.0, false)]
        [InlineData(10000, 10000, 1.0, true)]
        [InlineData(10000, 10000, -1.0, false)]
        [InlineData(10000, 10000, 0.0, false)]
        [InlineData(10500, 10000, -4.0, true)]
        [InlineData(10500, 10000, -6.0, false)]
        [InlineData(10500, 10000, -5.0, false)]
        [InlineData(5, 0, 0, false)]
        public void PercentBelowThresholdBelowAppliesTheOutcomeOfEveryStepKind(double value, double threshold, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentBelowThresholdBelow(threshold, bound),
                start.RejectPercentBelowThresholdBelow(threshold, bound),
                start.BreakPercentBelowThresholdBelow(threshold, bound),
                start.RequirePercentBelowThresholdBelow(threshold, bound),
                start.EnsurePercentBelowThresholdBelow(threshold, bound));

            FlowAssert.Kinds(expected,
                value.MatchPercentBelowThresholdBelow(threshold, bound),
                value.RejectPercentBelowThresholdBelow(threshold, bound),
                value.BreakPercentBelowThresholdBelow(threshold, bound),
                value.RequirePercentBelowThresholdBelow(threshold, bound),
                value.EnsurePercentBelowThresholdBelow(threshold, bound));
        }

        [Theory]
        [InlineData(9500, 10000, 4.0, 6.0, true)]
        [InlineData(9500, 10000, 6.0, 7.0, false)]
        [InlineData(9500, 10000, 5.0, 5.0, true)]
        [InlineData(10000, 10000, -1.0, 1.0, true)]
        [InlineData(10000, 10000, 1.0, 2.0, false)]
        [InlineData(10000, 10000, 0.0, 0.0, true)]
        [InlineData(10500, 10000, -6.0, -4.0, true)]
        [InlineData(10500, 10000, -4.0, -3.0, false)]
        [InlineData(10500, 10000, -5.0, -5.0, true)]
        [InlineData(5, 0, -1000, 1000, false)]
        public void PercentBelowThresholdInRangeAppliesTheOutcomeOfEveryStepKind(double value, double threshold, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentBelowThresholdInRange(threshold, lowerBound, upperBound),
                start.RejectPercentBelowThresholdInRange(threshold, lowerBound, upperBound),
                start.BreakPercentBelowThresholdInRange(threshold, lowerBound, upperBound),
                start.RequirePercentBelowThresholdInRange(threshold, lowerBound, upperBound),
                start.EnsurePercentBelowThresholdInRange(threshold, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchPercentBelowThresholdInRange(threshold, lowerBound, upperBound),
                value.RejectPercentBelowThresholdInRange(threshold, lowerBound, upperBound),
                value.BreakPercentBelowThresholdInRange(threshold, lowerBound, upperBound),
                value.RequirePercentBelowThresholdInRange(threshold, lowerBound, upperBound),
                value.EnsurePercentBelowThresholdInRange(threshold, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(9500, 10000, 6.0, 7.0, true)]
        [InlineData(9500, 10000, 4.0, 6.0, false)]
        [InlineData(9500, 10000, 5.0, 5.0, false)]
        [InlineData(10000, 10000, 1.0, 2.0, true)]
        [InlineData(10000, 10000, -1.0, 1.0, false)]
        [InlineData(10000, 10000, 0.0, 0.0, false)]
        [InlineData(10500, 10000, -4.0, -3.0, true)]
        [InlineData(10500, 10000, -6.0, -4.0, false)]
        [InlineData(10500, 10000, -5.0, -5.0, false)]
        [InlineData(5, 0, -1000, 1000, false)]
        public void PercentBelowThresholdOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double threshold, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchPercentBelowThresholdOutsideRange(threshold, lowerBound, upperBound),
                start.RejectPercentBelowThresholdOutsideRange(threshold, lowerBound, upperBound),
                start.BreakPercentBelowThresholdOutsideRange(threshold, lowerBound, upperBound),
                start.RequirePercentBelowThresholdOutsideRange(threshold, lowerBound, upperBound),
                start.EnsurePercentBelowThresholdOutsideRange(threshold, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchPercentBelowThresholdOutsideRange(threshold, lowerBound, upperBound),
                value.RejectPercentBelowThresholdOutsideRange(threshold, lowerBound, upperBound),
                value.BreakPercentBelowThresholdOutsideRange(threshold, lowerBound, upperBound),
                value.RequirePercentBelowThresholdOutsideRange(threshold, lowerBound, upperBound),
                value.EnsurePercentBelowThresholdOutsideRange(threshold, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(30, 10, -0.5, true)]
        [InlineData(30, 10, 1.5, false)]
        [InlineData(30, 10, 0.5, false)]
        [InlineData(10, 30, -1.5, true)]
        [InlineData(10, 30, 0.5, false)]
        [InlineData(10, 30, -0.5, false)]
        [InlineData(5, 5, -1.0, true)]
        [InlineData(5, 5, 1.0, false)]
        [InlineData(5, 5, 0.0, false)]
        [InlineData(0, 0, 0, false)]
        public void ImbalanceWithAboveAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchImbalanceWithAbove(other, bound),
                start.RejectImbalanceWithAbove(other, bound),
                start.BreakImbalanceWithAbove(other, bound),
                start.RequireImbalanceWithAbove(other, bound),
                start.EnsureImbalanceWithAbove(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchImbalanceWithAbove(other, bound),
                value.RejectImbalanceWithAbove(other, bound),
                value.BreakImbalanceWithAbove(other, bound),
                value.RequireImbalanceWithAbove(other, bound),
                value.EnsureImbalanceWithAbove(other, bound));
        }

        [Theory]
        [InlineData(30, 10, 1.5, true)]
        [InlineData(30, 10, -0.5, false)]
        [InlineData(30, 10, 0.5, false)]
        [InlineData(10, 30, 0.5, true)]
        [InlineData(10, 30, -1.5, false)]
        [InlineData(10, 30, -0.5, false)]
        [InlineData(5, 5, 1.0, true)]
        [InlineData(5, 5, -1.0, false)]
        [InlineData(5, 5, 0.0, false)]
        [InlineData(0, 0, 0, false)]
        public void ImbalanceWithBelowAppliesTheOutcomeOfEveryStepKind(double value, double other, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchImbalanceWithBelow(other, bound),
                start.RejectImbalanceWithBelow(other, bound),
                start.BreakImbalanceWithBelow(other, bound),
                start.RequireImbalanceWithBelow(other, bound),
                start.EnsureImbalanceWithBelow(other, bound));

            FlowAssert.Kinds(expected,
                value.MatchImbalanceWithBelow(other, bound),
                value.RejectImbalanceWithBelow(other, bound),
                value.BreakImbalanceWithBelow(other, bound),
                value.RequireImbalanceWithBelow(other, bound),
                value.EnsureImbalanceWithBelow(other, bound));
        }

        [Theory]
        [InlineData(30, 10, -0.5, 1.5, true)]
        [InlineData(30, 10, 1.5, 2.5, false)]
        [InlineData(30, 10, 0.5, 0.5, true)]
        [InlineData(10, 30, -1.5, 0.5, true)]
        [InlineData(10, 30, 0.5, 1.5, false)]
        [InlineData(10, 30, -0.5, -0.5, true)]
        [InlineData(5, 5, -1.0, 1.0, true)]
        [InlineData(5, 5, 1.0, 2.0, false)]
        [InlineData(5, 5, 0.0, 0.0, true)]
        [InlineData(0, 0, -1000, 1000, false)]
        public void ImbalanceWithInRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchImbalanceWithInRange(other, lowerBound, upperBound),
                start.RejectImbalanceWithInRange(other, lowerBound, upperBound),
                start.BreakImbalanceWithInRange(other, lowerBound, upperBound),
                start.RequireImbalanceWithInRange(other, lowerBound, upperBound),
                start.EnsureImbalanceWithInRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchImbalanceWithInRange(other, lowerBound, upperBound),
                value.RejectImbalanceWithInRange(other, lowerBound, upperBound),
                value.BreakImbalanceWithInRange(other, lowerBound, upperBound),
                value.RequireImbalanceWithInRange(other, lowerBound, upperBound),
                value.EnsureImbalanceWithInRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(30, 10, 1.5, 2.5, true)]
        [InlineData(30, 10, -0.5, 1.5, false)]
        [InlineData(30, 10, 0.5, 0.5, false)]
        [InlineData(10, 30, 0.5, 1.5, true)]
        [InlineData(10, 30, -1.5, 0.5, false)]
        [InlineData(10, 30, -0.5, -0.5, false)]
        [InlineData(5, 5, 1.0, 2.0, true)]
        [InlineData(5, 5, -1.0, 1.0, false)]
        [InlineData(5, 5, 0.0, 0.0, false)]
        [InlineData(0, 0, -1000, 1000, false)]
        public void ImbalanceWithOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double other, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchImbalanceWithOutsideRange(other, lowerBound, upperBound),
                start.RejectImbalanceWithOutsideRange(other, lowerBound, upperBound),
                start.BreakImbalanceWithOutsideRange(other, lowerBound, upperBound),
                start.RequireImbalanceWithOutsideRange(other, lowerBound, upperBound),
                start.EnsureImbalanceWithOutsideRange(other, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchImbalanceWithOutsideRange(other, lowerBound, upperBound),
                value.RejectImbalanceWithOutsideRange(other, lowerBound, upperBound),
                value.BreakImbalanceWithOutsideRange(other, lowerBound, upperBound),
                value.RequireImbalanceWithOutsideRange(other, lowerBound, upperBound),
                value.EnsureImbalanceWithOutsideRange(other, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(100, 90, -0.9, true)]
        [InlineData(100, 90, 1.1, false)]
        [InlineData(100, 90, 0.1, false)]
        [InlineData(100, 100, -1.0, true)]
        [InlineData(100, 100, 1.0, false)]
        [InlineData(100, 100, 0.0, false)]
        [InlineData(0, 5, 0, false)]
        [InlineData(100, 120, -1.2, true)]
        [InlineData(100, 120, 0.8, false)]
        [InlineData(100, 120, -0.2, false)]
        public void RetentionAfterAboveAppliesTheOutcomeOfEveryStepKind(double value, double outflow, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRetentionAfterAbove(outflow, bound),
                start.RejectRetentionAfterAbove(outflow, bound),
                start.BreakRetentionAfterAbove(outflow, bound),
                start.RequireRetentionAfterAbove(outflow, bound),
                start.EnsureRetentionAfterAbove(outflow, bound));

            FlowAssert.Kinds(expected,
                value.MatchRetentionAfterAbove(outflow, bound),
                value.RejectRetentionAfterAbove(outflow, bound),
                value.BreakRetentionAfterAbove(outflow, bound),
                value.RequireRetentionAfterAbove(outflow, bound),
                value.EnsureRetentionAfterAbove(outflow, bound));
        }

        [Theory]
        [InlineData(100, 90, 1.1, true)]
        [InlineData(100, 90, -0.9, false)]
        [InlineData(100, 90, 0.1, false)]
        [InlineData(100, 100, 1.0, true)]
        [InlineData(100, 100, -1.0, false)]
        [InlineData(100, 100, 0.0, false)]
        [InlineData(0, 5, 0, false)]
        [InlineData(100, 120, 0.8, true)]
        [InlineData(100, 120, -1.2, false)]
        [InlineData(100, 120, -0.2, false)]
        public void RetentionAfterBelowAppliesTheOutcomeOfEveryStepKind(double value, double outflow, double bound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRetentionAfterBelow(outflow, bound),
                start.RejectRetentionAfterBelow(outflow, bound),
                start.BreakRetentionAfterBelow(outflow, bound),
                start.RequireRetentionAfterBelow(outflow, bound),
                start.EnsureRetentionAfterBelow(outflow, bound));

            FlowAssert.Kinds(expected,
                value.MatchRetentionAfterBelow(outflow, bound),
                value.RejectRetentionAfterBelow(outflow, bound),
                value.BreakRetentionAfterBelow(outflow, bound),
                value.RequireRetentionAfterBelow(outflow, bound),
                value.EnsureRetentionAfterBelow(outflow, bound));
        }

        [Theory]
        [InlineData(100, 90, -0.9, 1.1, true)]
        [InlineData(100, 90, 1.1, 2.1, false)]
        [InlineData(100, 90, 0.1, 0.1, true)]
        [InlineData(100, 100, -1.0, 1.0, true)]
        [InlineData(100, 100, 1.0, 2.0, false)]
        [InlineData(100, 100, 0.0, 0.0, true)]
        [InlineData(0, 5, -1000, 1000, false)]
        [InlineData(100, 120, -1.2, 0.8, true)]
        [InlineData(100, 120, 0.8, 1.8, false)]
        [InlineData(100, 120, -0.2, -0.2, true)]
        public void RetentionAfterInRangeAppliesTheOutcomeOfEveryStepKind(double value, double outflow, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRetentionAfterInRange(outflow, lowerBound, upperBound),
                start.RejectRetentionAfterInRange(outflow, lowerBound, upperBound),
                start.BreakRetentionAfterInRange(outflow, lowerBound, upperBound),
                start.RequireRetentionAfterInRange(outflow, lowerBound, upperBound),
                start.EnsureRetentionAfterInRange(outflow, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchRetentionAfterInRange(outflow, lowerBound, upperBound),
                value.RejectRetentionAfterInRange(outflow, lowerBound, upperBound),
                value.BreakRetentionAfterInRange(outflow, lowerBound, upperBound),
                value.RequireRetentionAfterInRange(outflow, lowerBound, upperBound),
                value.EnsureRetentionAfterInRange(outflow, lowerBound, upperBound));
        }

        [Theory]
        [InlineData(100, 90, 1.1, 2.1, true)]
        [InlineData(100, 90, -0.9, 1.1, false)]
        [InlineData(100, 90, 0.1, 0.1, false)]
        [InlineData(100, 100, 1.0, 2.0, true)]
        [InlineData(100, 100, -1.0, 1.0, false)]
        [InlineData(100, 100, 0.0, 0.0, false)]
        [InlineData(0, 5, -1000, 1000, false)]
        [InlineData(100, 120, 0.8, 1.8, true)]
        [InlineData(100, 120, -1.2, 0.8, false)]
        [InlineData(100, 120, -0.2, -0.2, false)]
        public void RetentionAfterOutsideRangeAppliesTheOutcomeOfEveryStepKind(double value, double outflow, double lowerBound, double upperBound, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchRetentionAfterOutsideRange(outflow, lowerBound, upperBound),
                start.RejectRetentionAfterOutsideRange(outflow, lowerBound, upperBound),
                start.BreakRetentionAfterOutsideRange(outflow, lowerBound, upperBound),
                start.RequireRetentionAfterOutsideRange(outflow, lowerBound, upperBound),
                start.EnsureRetentionAfterOutsideRange(outflow, lowerBound, upperBound));

            FlowAssert.Kinds(expected,
                value.MatchRetentionAfterOutsideRange(outflow, lowerBound, upperBound),
                value.RejectRetentionAfterOutsideRange(outflow, lowerBound, upperBound),
                value.BreakRetentionAfterOutsideRange(outflow, lowerBound, upperBound),
                value.RequireRetentionAfterOutsideRange(outflow, lowerBound, upperBound),
                value.EnsureRetentionAfterOutsideRange(outflow, lowerBound, upperBound));
        }
    }
}
