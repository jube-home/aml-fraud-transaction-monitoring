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
using FluentAssertions;
using Jube.Data.Query;
using Xunit;

namespace Jube.Test.Engine.Integrity
{
    [Trait("Category", "Unit")]
    public sealed class EngineInstanceLivenessTests
    {
        private static readonly DateTime now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void TheToleranceIsTwoMinutesAndTheRetentionOneHour()
        {
            EngineInstanceLiveness.ToleranceMinutes.Should().Be(2);
            EngineInstanceLiveness.RetentionMinutes.Should().Be(60);
        }

        [Fact]
        public void TheCutoffIsTheToleranceBeforeNow()
        {
            EngineInstanceLiveness.CutoffFrom(now).Should().Be(now.AddMinutes(-2));
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(-2, true)]
        [InlineData(-3, false)]
        [InlineData(-30, false)]
        public void AHeartbeatIsLiveOnlyWithinTheTolerance(int minutesAgo, bool live)
        {
            var heartbeat = now.AddMinutes(minutesAgo);

            (heartbeat >= EngineInstanceLiveness.CutoffFrom(now)).Should().Be(live);
        }
    }
}