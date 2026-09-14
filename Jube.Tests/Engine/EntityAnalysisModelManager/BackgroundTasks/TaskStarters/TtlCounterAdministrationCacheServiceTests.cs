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
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.TtlCounterAdministration;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    [Trait("Category", "Unit")]
    public sealed class TtlCounterAdministrationCacheServiceTests
    {
        private static readonly DateTime referenceDate = new(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);

        private static TtlCounterAdministrationCacheService NewService()
        {
            var model = new EntityAnalysisModelDomain
            {
                Services =
                {
                    Log = TestLog.NoOp
                }
            };
            return new TtlCounterAdministrationCacheService(model);
        }

        private static EntityAnalysisModelTtlCounter NewCounter(string interval, int value)
        {
            return new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "Counter",
                TtlCounterDataName = "AccountId",
                TtlCounterInterval = interval,
                TtlCounterValue = value
            };
        }

        [Theory]
        [InlineData("d")]
        [InlineData("h")]
        [InlineData("n")]
        [InlineData("s")]
        [InlineData("m")]
        [InlineData("y")]
        public void EachRecognisedIntervalSubtractsFromReferenceDate(string interval)
        {
            var service = NewService();

            service.GetAdjustedTtlCounterDate(NewCounter(interval, 1), referenceDate).Should().BeBefore(referenceDate);
        }

        [Fact]
        public void SecondsIntervalSupportsVeryShortLivedCounters()
        {
            var service = NewService();

            service.GetAdjustedTtlCounterDate(NewCounter("s", 5), referenceDate)
                .Should().Be(new DateTime(2024, 6, 15, 12, 0, 5, DateTimeKind.Utc));
        }

        [Fact]
        public void UnrecognisedIntervalFallsBackToDays()
        {
            var service = NewService();

            service.GetAdjustedTtlCounterDate(NewCounter("bogus", 4), referenceDate)
                .Should().Be(referenceDate.AddDays(-4));
        }

        [Fact]
        public void ZeroIntervalValueReturnsReferenceDateUnchanged()
        {
            var service = NewService();

            service.GetAdjustedTtlCounterDate(NewCounter("d", 0), referenceDate).Should().Be(referenceDate);
        }

        [Fact]
        public void NegativeIntervalValuePushesTheWindowIntoTheFutureInstead()
        {
            var service = NewService();

            service.GetAdjustedTtlCounterDate(NewCounter("d", -3), referenceDate).Should().Be(referenceDate.AddDays(3));
        }

        [Fact]
        public void AnOverflowingIntervalIsCaughtAndReturnsReferenceDateUnadjusted()
        {
            var service = NewService();
            var counter = NewCounter("y", int.MaxValue);

            service.GetAdjustedTtlCounterDate(counter, referenceDate).Should().Be(referenceDate);
        }
    }
}