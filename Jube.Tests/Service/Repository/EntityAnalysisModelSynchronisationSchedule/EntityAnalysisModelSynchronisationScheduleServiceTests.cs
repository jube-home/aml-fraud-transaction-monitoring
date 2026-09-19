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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Repository.EntityAnalysisModelSynchronisationSchedule;
using Jube.Service.Exceptions.Repository.EntityAnalysisModelSynchronisationSchedule;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.EntityAnalysisModelSynchronisationSchedule.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Repository.EntityAnalysisModelSynchronisationSchedule
{
    using EntityAnalysisModelSynchronisationScheduleService =
        global::Jube.Service.Repository.EntityAnalysisModelSynchronisationSchedule.
        EntityAnalysisModelSynchronisationScheduleService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelSynchronisationScheduleServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.EntityAnalysisModelSynchronisationSchedule.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelSynchronisationScheduleService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelSynchronisationScheduleService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        [Fact]
        public async Task InsertPersistsWithScheduleDateAndIsReturnedByGetCurrentAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var scheduleDate = DateTimeOffset.UtcNow.AddHours(2);
            var saved = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = scheduleDate
            });
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.CreatedUser.Should().Be(fx.Seed.LandlordUser);
            saved.ScheduleDate.Should().NotBeNull();
            saved.ScheduleDate.Required().Should().BeCloseTo(scheduleDate.UtcDateTime, TimeSpan.FromSeconds(2));

            var current = await service.GetCurrentAsync();
            current = current.Required();
            current.Should().NotBeNull();
            current.Id.Should().Be(saved.Id);
            current.ScheduleDate.Should().NotBeNull();
            current.ScheduleDate.Required().Should().BeCloseTo(scheduleDate, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task InsertWithNoScheduleDateDefaultsToCreatedDateAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var before = DateTime.UtcNow;
            var saved = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto());
            createdIds.Add(saved.Id);
            var after = DateTime.UtcNow;

            saved.ScheduleDate.Should().NotBeNull();
            saved.ScheduleDate.Required().Should().BeOnOrAfter(before.AddSeconds(-1)).And
                .BeOnOrBefore(after.AddSeconds(1));
            saved.CreatedDate.Should().NotBeNull();
            saved.ScheduleDate.Should().BeCloseTo(saved.CreatedDate.Required(), TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task GetCurrentReturnsTheMostRecentlyScheduledRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var first = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = DateTimeOffset.UtcNow.AddMinutes(5)
            });
            createdIds.Add(first.Id);

            var second = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = DateTimeOffset.UtcNow.AddMinutes(10)
            });
            createdIds.Add(second.Id);

            var current = await service.GetCurrentAsync();
            current.Should().NotBeNull();
            current.Required().Id.Should().Be(second.Id);
        }

        [Fact]
        public async Task GetCurrentWithNoScheduleReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var current = await service.GetCurrentAsync();

            current.Should().BeNull();
        }

        [Fact]
        public async Task InsertByUserWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto());

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task GetCurrentByUserWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.GetCurrentAsync();

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task InsertByUnauthenticatedUserThrowsNotAuthenticatedAndIssuesNoQueryAsync()
        {
            await using var dbContext = fx.GetDbContext();

            // ReSharper disable once AccessToDisposedClosure
            var act = () => BuildServiceAsync(dbContext, null);

            await act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        [Fact]
        public async Task InsertByBlankUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            // ReSharper disable once AccessToDisposedClosure
            var act = () => BuildServiceAsync(dbContext, "   ");

            await act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        [Fact]
        public async Task InsertByUserWithNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            // ReSharper disable once AccessToDisposedClosure
            var act = () => BuildServiceAsync(dbContext, fx.Seed.UserNoTenant);

            await act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        [Fact]
        public async Task InsertByUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            // ReSharper disable once AccessToDisposedClosure
            var act = () => BuildServiceAsync(dbContext, fx.Seed.UnknownUser);

            await act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        [Fact]
        public async Task ScheduleIsIsolatedPerTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var landlordService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var tenantBService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var landlordSaved = await landlordService.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = DateTimeOffset.UtcNow.AddMinutes(30)
            });
            createdIds.Add(landlordSaved.Id);

            var tenantBCurrent = await tenantBService.GetCurrentAsync();
            tenantBCurrent.Should().BeNull();
        }

        [Fact]
        public async Task TamperedCreatedUserAndTenantRegistryIdAreIgnoredOnInsertAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var saved = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = DateTimeOffset.UtcNow.AddMinutes(1),
                CreatedUser = "SomeoneElse",
                TenantRegistryId = -999,
                Id = 999999
            });
            createdIds.Add(saved.Id);

            saved.Id.Should().NotBe(999999);
            saved.CreatedUser.Should().Be(fx.Seed.LandlordUser);
            saved.TenantRegistryId.Should().NotBe(-999);
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
                {
                    ScheduleDate = DateTimeOffset.UtcNow.AddMinutes(i)
                });
                createdIds.Add(saved.Id);
            }

            var page = await service.ListAsync(take: 500);
            page.Items.Count.Should().BeLessOrEqualTo(200);

            var firstPage = await service.ListAsync(take: 1);
            firstPage.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task InsertPublishesServiceChangeEventOnlyOnMutationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, serviceChangeBus: bus);

            await service.GetCurrentAsync();
            bus.Published.Should().BeEmpty();

            var saved = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = DateTimeOffset.UtcNow.AddMinutes(1)
            });
            createdIds.Add(saved.Id);

            bus.Published.Should().ContainSingle();
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            bus.Published[0].Area.Should().Be("EntityAnalysisModelSynchronisationSchedule");
            bus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task InsertLogsBusinessInfoAndAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, log, auditLog);

            var saved = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = DateTimeOffset.UtcNow.AddMinutes(1)
            });
            createdIds.Add(saved.Id);

            log.Entries.Should().Contain(e => e.Level == "INFO" && e.Message.Contains($"Id={saved.Id}"));
            auditLog.Entries.Should().ContainSingle(e => e.Level == "INFO" && e.Message.Contains("op=Create"));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            var act = () => service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto());

            await act.Should().ThrowAsync<ForbiddenException>();
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertWithScheduleDateTooFarInFutureIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var act = () => service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = DateTimeOffset.UtcNow.AddYears(2)
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task GetCurrentHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            // ReSharper disable once AccessToDisposedClosure
            var act = () => service.GetCurrentAsync(cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}