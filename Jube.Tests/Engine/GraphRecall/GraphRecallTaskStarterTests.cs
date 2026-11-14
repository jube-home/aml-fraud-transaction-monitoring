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
using Jube.Engine.BackgroundTasks.TaskStarters;
using Xunit;

namespace Jube.Test.Engine.GraphRecall
{
    [Trait("Category", "Unit")]
    public sealed class GraphRecallTaskStarterTests
    {
        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(0, 3, true)]
        [InlineData(1, 2, true)]
        [InlineData(1, 0, false)]
        [InlineData(4, 0, false)]
        public void TheStartupFlagIsSetOnlyWhenNothingFailedOrAtLeastOneTenantLoaded(int failures, int successes,
            bool expected)
        {
            GraphRecallTaskStarter.Succeeded(failures, successes).Should().Be(expected);
        }

        [Theory]
        [InlineData(true, 900000, 900000)]
        [InlineData(false, 900000, GraphRecallTaskStarter.FailureRetryWait)]
        [InlineData(false, 5000, 5000)]
        [InlineData(true, 5000, 5000)]
        public void AFailedCycleRetriesAfterAtMostThirtySecondsAndNeverLaterThanTheConfiguredWait(bool loaded,
            int wait, int expected)
        {
            GraphRecallTaskStarter.NextWait(loaded, wait).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, 900000)]
        [InlineData("", 900000)]
        [InlineData("abc", 900000)]
        [InlineData("0", 900000)]
        [InlineData("-5", 900000)]
        [InlineData("1500", 1500)]
        public void TheLoaderWaitFallsBackToFifteenMinutesForAnythingButAPositiveInteger(string? setting,
            int expected)
        {
            GraphRecallTaskStarter.ResolveWait(setting!).Should().Be(expected);
        }
    }
}
