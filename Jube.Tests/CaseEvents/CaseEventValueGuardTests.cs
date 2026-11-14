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

using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Xunit;

namespace Jube.Test.CaseEvents
{
    [Trait("Category", "Unit")]
    public sealed class CaseEventValueGuardTests
    {
        private static CaseEvent Event(int type, string before, string after) =>
            new() { CaseEventTypeId = type, Before = before, After = after };

        [Theory]
        [InlineData(5, null, "2026-11-05T09:00:00.0000000Z")]
        [InlineData(10, "2026-11-05T09:00:00.0000000Z", "2026-11-06T09:00:00.0000000Z")]
        [InlineData(12, "2026-11-05T09:00:00.0000000Z", "2026-11-06T09:00:00.0000000Z")]
        [InlineData(13, null, "2026-11-06T09:00:00.0000000Z")]
        [InlineData(7, "1", "3")]
        [InlineData(7, null, "4")]
        [InlineData(19, "4", null)]
        [InlineData(15, "3", "0")]
        [InlineData(9, "0f2a9a53-6c3b-4a54-8a2e-0d3c1b2a4f10", "1f2a9a53-6c3b-4a54-8a2e-0d3c1b2a4f10")]
        [InlineData(17, "0f2a9a53-6c3b-4a54-8a2e-0d3c1b2a4f10", "1f2a9a53-6c3b-4a54-8a2e-0d3c1b2a4f10")]
        [InlineData(11, "2", "5")]
        [InlineData(20, null, "0.25")]
        [InlineData(14, "alex", "sam")]
        [InlineData(18, "alex", "Handing over")]
        [InlineData(23, "Check related cases", "42")]
        [InlineData(2, null, null)]
        public void CanonicalValuesAreAccepted(int type, string before, string after)
        {
            CaseEventValueGuard.Violation(Event(type, before, after)).Should().BeNull();
        }

        [Theory]
        [InlineData(5, null, "05/11/2026 09:00:00")]
        [InlineData(10, "11/05/2026 09:00:00", "2026-11-06T09:00:00.0000000Z")]
        [InlineData(12, "2026-11-05T09:00:00", "2026-11-06T09:00:00.0000000Z")]
        [InlineData(7, "Review", "3")]
        [InlineData(7, "1", "Regulatory deadline")]
        [InlineData(19, "Payment hold", null)]
        [InlineData(15, "Closed", "0")]
        [InlineData(9, "Open", "1f2a9a53-6c3b-4a54-8a2e-0d3c1b2a4f10")]
        [InlineData(17, "0f2a9a53-6c3b-4a54-8a2e-0d3c1b2a4f10", "Escalated")]
        [InlineData(11, "two", "5")]
        [InlineData(20, "0.25", null)]
        [InlineData(19, null, "1")]
        public void NamesCultureDatesAndValuesOnTheWrongSideAreRefused(int type, string before, string after)
        {
            CaseEventValueGuard.Violation(Event(type, before, after)).Should().NotBeNull();
        }

        [Fact]
        public void EnsureCanonicalThrowsAnExplanationThatNamesTheTypeAndTheSide()
        {
            var act = () => CaseEventValueGuard.EnsureCanonical(Event(9, "Open", null));

            act.Should().Throw<System.InvalidOperationException>()
                .WithMessage("*type 9*Before*Guid*");
        }
    }
}
