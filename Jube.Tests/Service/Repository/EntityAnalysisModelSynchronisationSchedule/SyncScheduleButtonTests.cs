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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Repository.EntityAnalysisModelSynchronisationSchedule;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ScheduleService =
    Jube.Service.Repository.EntityAnalysisModelSynchronisationSchedule.
    EntityAnalysisModelSynchronisationScheduleService;

namespace Jube.Test.Service.Repository.EntityAnalysisModelSynchronisationSchedule
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SyncScheduleButtonTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            foreach (var id in createdIds)
            {
                await dbContext.EntityAnalysisModelSynchronisationSchedule.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<ScheduleService> BuildServiceAsync(DbContext dbContext, string userName) =>
            ScheduleService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);

        private async Task<DateTimeOffset?> PickAndSaveAsync(DateTimeOffset? picked)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = picked
            });
            createdIds.Add(saved.Id);

            var current = await service.GetCurrentAsync();
            current.Should().NotBeNull();
            current!.Id.Should().Be(saved.Id, "the row just saved is the current schedule");
            return current.ScheduleDate;
        }

        [Fact]
        public async Task APickedTimeIsStoredAsTheSameInstantItWasPickedForAsync()
        {
            var picked = new DateTimeOffset(2026, 10, 5, 10, 30, 0, TimeSpan.FromHours(10));

            var current = await PickAndSaveAsync(picked);

            current.Should().Be(picked, "the page's picker is local time and the instant must survive the round trip");
        }

        [Fact]
        public async Task ThePickedTimeIsSentAsUtcAndReadBackUnchangedAsync()
        {
            var picked = new DateTimeOffset(DateTime.UtcNow.AddHours(3).Date.AddHours(9), TimeSpan.Zero);

            var current = await PickAndSaveAsync(picked);

            current.Should().NotBeNull();
            current!.Value.UtcDateTime.Should().Be(picked.UtcDateTime);
        }

        [Fact]
        public async Task SyncNowWithNoPickedTimeSchedulesNowAsync()
        {
            var before = DateTimeOffset.UtcNow.AddSeconds(-5);

            var current = await PickAndSaveAsync(null);

            current.Should().NotBeNull();
            current!.Value.Should().BeAfter(before);
            current.Value.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(5));
        }

        [Fact]
        public async Task AFutureTimeIsSavedAndIsTheCurrentScheduleUntilReplacedAsync()
        {
            var later = DateTimeOffset.UtcNow.AddMinutes(45);

            var current = await PickAndSaveAsync(later);

            current.Should().BeCloseTo(later, TimeSpan.FromSeconds(1));
        }
    }
}