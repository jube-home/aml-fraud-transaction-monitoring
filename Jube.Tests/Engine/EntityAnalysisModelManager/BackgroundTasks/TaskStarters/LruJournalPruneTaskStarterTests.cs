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
using Xunit;
using LruJournalPruneTaskStarter =
    Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.LruJournalPruneTaskStarter;

namespace Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    [Trait("Category", "Unit")]
    public sealed class LruJournalPruneTaskStarterTests
    {
        [Fact]
        public void MinutesIntervalReturnsATimeSpanInMinutes()
        {
            LruJournalPruneTaskStarter.ComputeMaxAge("n", "5").Should().Be(TimeSpan.FromMinutes(5));
        }

        [Fact]
        public void HoursIntervalReturnsATimeSpanInHours()
        {
            LruJournalPruneTaskStarter.ComputeMaxAge("h", "2").Should().Be(TimeSpan.FromHours(2));
        }

        [Theory]
        [InlineData("d")]
        [InlineData("bogus")]
        [InlineData("")]
        public void AnyOtherIntervalIncludingDIsTreatedAsDays(string interval)
        {
            LruJournalPruneTaskStarter.ComputeMaxAge(interval, "3").Should().Be(TimeSpan.FromDays(3));
        }

        [Fact]
        public void AVerySmallValueSupportsAShortMaxAgeWindow()
        {
            LruJournalPruneTaskStarter.ComputeMaxAge("n", "1").Should().Be(TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void ZeroValueReturnsAZeroLengthTimeSpan()
        {
            LruJournalPruneTaskStarter.ComputeMaxAge("h", "0").Should().Be(TimeSpan.Zero);
        }

        [Theory]
        [InlineData("not-a-number")]
        [InlineData("")]
        [InlineData(null)]
        public void ANonNumericOrMissingValueDefaultsToOne(string? value)
        {
            LruJournalPruneTaskStarter.ComputeMaxAge("h", value!).Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public void ANegativeValueProducesANegativeTimeSpanWithNoGuardRail()
        {
            LruJournalPruneTaskStarter.ComputeMaxAge("h", "-2").Should().Be(TimeSpan.FromHours(-2));
        }
    }
}