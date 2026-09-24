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
using System.Linq;
using FluentAssertions;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Reprocessing;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelManager.Reprocessing
{
    [Trait("Category", "Unit")]
    public sealed class ReprocessingDateRangeTests
    {
        private static readonly DateTime last = new(2024, 3, 31, 12, 30, 45);

        [Theory]
        [InlineData("d", 2, "2024-03-29T12:30:45")]
        [InlineData("h", 5, "2024-03-31T07:30:45")]
        [InlineData("n", 30, "2024-03-31T12:00:45")]
        [InlineData("s", 45, "2024-03-31T12:30:00")]
        [InlineData("m", 1, "2024-02-29T12:30:45")]
        [InlineData("y", 1, "2023-03-31T12:30:45")]
        [InlineData("d", 0, "2024-03-31T12:30:45")]
        public void TheStartIsTheLastReferenceDateLessTheInterval(string type, int value, string expected)
        {
            ReprocessingDateRange.StartDate(last, type, value).Should().Be(DateTime.Parse(expected));
        }

        [Fact]
        public void AMonthBackFromTheEndOfAMonthLandsOnTheLastDayOfTheShorterMonth()
        {
            ReprocessingDateRange.StartDate(new DateTime(2023, 3, 31), "m", 1).Should()
                .Be(new DateTime(2023, 2, 28));
        }

        [Theory]
        [InlineData("x", 1)]
        [InlineData("", 1)]
        [InlineData(null, 1)]
        [InlineData("D", 1)]
        [InlineData("d", -1)]
        public void AnUnknownIntervalOrANegativeValueHasNoStart(string? type, int value)
        {
            ReprocessingDateRange.StartDate(last, type!, value).Should().BeNull();
        }

        [Fact]
        public void AnIntervalReachingBeforeTheCalendarStartsAtTheBeginning()
        {
            ReprocessingDateRange.StartDate(last, "y", 5000).Should().Be(DateTime.MinValue);
        }

        [Theory]
        [InlineData(0, 0.0, false)]
        [InlineData(0, 0.999, false)]
        [InlineData(100, 0.0, true)]
        [InlineData(100, 0.999999, true)]
        [InlineData(50, 0.49, true)]
        [InlineData(50, 0.5, false)]
        [InlineData(1, 0.009, true)]
        [InlineData(1, 0.011, false)]
        public void TheSampleIsAPercentage(double sample, double draw, bool expected)
        {
            ReprocessingDateRange.Sampled(sample, draw).Should().Be(expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(90)]
        [InlineData(100)]
        public void OverManyDrawsTheSampledShareMatchesThePercentage(double sample)
        {
            var random = new Random(42);
            const int draws = 100_000;

            var sampled = Enumerable.Range(0, draws)
                .Count(_ => ReprocessingDateRange.Sampled(sample, random.NextDouble()));

            ((double)sampled / draws * 100).Should().BeApproximately(sample, 0.5);
        }
    }
}