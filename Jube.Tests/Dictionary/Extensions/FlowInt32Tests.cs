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
    public sealed class FlowInt32Tests
    {
        [Theory]
        [InlineData(5, 3, true)]
        [InlineData(3, 3, false)]
        [InlineData(1, 3, false)]
        public void GreaterAppliesTheOutcomeOfEveryStepKind(int value, int other, bool expected)
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
        [InlineData(5, 3, true)]
        [InlineData(3, 3, true)]
        [InlineData(1, 3, false)]
        public void GreaterOrEqualAppliesTheOutcomeOfEveryStepKind(int value, int other, bool expected)
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
        [InlineData(1, 3, true)]
        [InlineData(3, 3, false)]
        [InlineData(5, 3, false)]
        public void LessAppliesTheOutcomeOfEveryStepKind(int value, int other, bool expected)
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
        [InlineData(1, 3, true)]
        [InlineData(3, 3, true)]
        [InlineData(5, 3, false)]
        public void LessOrEqualAppliesTheOutcomeOfEveryStepKind(int value, int other, bool expected)
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
        [InlineData(3, 3, true)]
        [InlineData(3, 4, false)]
        [InlineData(-3, -3, true)]
        [InlineData(0, 0, true)]
        [InlineData(int.MaxValue, int.MaxValue, true)]
        [InlineData(int.MinValue, int.MaxValue, false)]
        public void EqualAppliesTheOutcomeOfEveryStepKind(int value, int other, bool expected)
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
        [InlineData(3, 4, true)]
        [InlineData(3, 3, false)]
        [InlineData(-3, 3, true)]
        [InlineData(int.MinValue, int.MinValue, false)]
        public void NotEqualAppliesTheOutcomeOfEveryStepKind(int value, int other, bool expected)
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
        public void BetweenAppliesTheOutcomeOfEveryStepKind(int value, int minimum, int maximum, bool expected)
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
        public void InRangeAppliesTheOutcomeOfEveryStepKind(int value, int minimum, int maximum, bool expected)
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
        public void OutsideRangeAppliesTheOutcomeOfEveryStepKind(int value, int minimum, int maximum, bool expected)
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
        [InlineData(5, new[] { 1, 5 }, true)]
        [InlineData(2, new[] { 1, 5 }, false)]
        [InlineData(2, new int[0], false)]
        public void InAppliesTheOutcomeOfEveryStepKind(int value, int[] values, bool expected)
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
        [InlineData(2, new[] { 1, 5 }, true)]
        [InlineData(5, new[] { 1, 5 }, false)]
        [InlineData(2, new int[0], true)]
        public void NotInAppliesTheOutcomeOfEveryStepKind(int value, int[] values, bool expected)
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
        [InlineData(1, false)]
        [InlineData(-1, false)]
        [InlineData(int.MinValue, false)]
        public void IsZeroAppliesTheOutcomeOfEveryStepKind(int value, bool expected)
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
        public void IsPositiveAppliesTheOutcomeOfEveryStepKind(int value, bool expected)
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
        public void IsNegativeAppliesTheOutcomeOfEveryStepKind(int value, bool expected)
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
        [InlineData(4, true)]
        [InlineData(0, true)]
        [InlineData(3, false)]
        [InlineData(-3, false)]
        [InlineData(int.MaxValue, false)]
        [InlineData(int.MinValue, true)]
        public void IsEvenAppliesTheOutcomeOfEveryStepKind(int value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsEven(),
                start.RejectIsEven(),
                start.BreakIsEven(),
                start.RequireIsEven(),
                start.EnsureIsEven());

            FlowAssert.Kinds(expected,
                value.MatchIsEven(),
                value.RejectIsEven(),
                value.BreakIsEven(),
                value.RequireIsEven(),
                value.EnsureIsEven());
        }

        [Theory]
        [InlineData(3, true)]
        [InlineData(-3, true)]
        [InlineData(4, false)]
        [InlineData(0, false)]
        public void IsOddAppliesTheOutcomeOfEveryStepKind(int value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsOdd(),
                start.RejectIsOdd(),
                start.BreakIsOdd(),
                start.RequireIsOdd(),
                start.EnsureIsOdd());

            FlowAssert.Kinds(expected,
                value.MatchIsOdd(),
                value.RejectIsOdd(),
                value.BreakIsOdd(),
                value.RequireIsOdd(),
                value.EnsureIsOdd());
        }

        [Theory]
        [InlineData(10, 5, true)]
        [InlineData(10, 3, false)]
        [InlineData(10, 0, false)]
        [InlineData(0, 5, true)]
        public void IsMultipleOfAppliesTheOutcomeOfEveryStepKind(int value, int divisor, bool expected)
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
        [InlineData(7, true)]
        [InlineData(2, true)]
        [InlineData(9, false)]
        [InlineData(1, false)]
        [InlineData(-3, false)]
        public void IsPrimeAppliesTheOutcomeOfEveryStepKind(int value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsPrime(),
                start.RejectIsPrime(),
                start.BreakIsPrime(),
                start.RequireIsPrime(),
                start.EnsureIsPrime());

            FlowAssert.Kinds(expected,
                value.MatchIsPrime(),
                value.RejectIsPrime(),
                value.BreakIsPrime(),
                value.RequireIsPrime(),
                value.EnsureIsPrime());
        }
    }
}
