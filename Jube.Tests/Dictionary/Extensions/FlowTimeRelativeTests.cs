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

using System;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowTimeRelativeTests
    {
        [Theory]
        [InlineData(-2, true)]
        [InlineData(2, false)]
        public void IsPastAppliesTheOutcomeOfEveryStepKind(int hoursFromNow, bool expected)
        {
            var start = DateTime.Now.AddHours(hoursFromNow).Start();

            FlowAssert.Kinds(expected, start.MatchIsPast(), start.RejectIsPast(), start.BreakIsPast(),
                start.RequireIsPast(), start.EnsureIsPast());
        }

        [Theory]
        [InlineData(2, true)]
        [InlineData(-2, false)]
        public void IsFutureAppliesTheOutcomeOfEveryStepKind(int hoursFromNow, bool expected)
        {
            var start = DateTime.Now.AddHours(hoursFromNow).Start();

            FlowAssert.Kinds(expected, start.MatchIsFuture(), start.RejectIsFuture(), start.BreakIsFuture(),
                start.RequireIsFuture(), start.EnsureIsFuture());
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(-1, false)]
        [InlineData(1, false)]
        public void IsTodayAppliesTheOutcomeOfEveryStepKind(int daysFromToday, bool expected)
        {
            var start = DateTime.Today.AddDays(daysFromToday).AddHours(1).Start();

            FlowAssert.Kinds(expected, start.MatchIsToday(), start.RejectIsToday(), start.BreakIsToday(),
                start.RequireIsToday(), start.EnsureIsToday());
        }

        [Theory]
        [InlineData(10, 5, true)]
        [InlineData(5, 5, false)]
        [InlineData(1, 5, false)]
        [InlineData(-3, 5, false)]
        public void OlderThanDaysAppliesTheOutcomeOfEveryStepKind(int ageInDays, int days, bool expected)
        {
            var start = DateTime.Today.AddDays(-ageInDays).Start();

            FlowAssert.Kinds(expected, start.MatchOlderThanDays(days), start.RejectOlderThanDays(days),
                start.BreakOlderThanDays(days), start.RequireOlderThanDays(days), start.EnsureOlderThanDays(days));
        }

        [Theory]
        [InlineData(1, 5, true)]
        [InlineData(5, 5, false)]
        [InlineData(10, 5, false)]
        public void YoungerThanDaysAppliesTheOutcomeOfEveryStepKind(int ageInDays, int days, bool expected)
        {
            var start = DateTime.Today.AddDays(-ageInDays).Start();

            FlowAssert.Kinds(expected, start.MatchYoungerThanDays(days), start.RejectYoungerThanDays(days),
                start.BreakYoungerThanDays(days), start.RequireYoungerThanDays(days), start.EnsureYoungerThanDays(days));
        }
    }
}
