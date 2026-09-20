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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.CaseWorkflowPriority;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowPriority;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowPriority
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowPriorityServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        public Task InitializeAsync() => Task.CompletedTask;

        public Task DisposeAsync() => Task.CompletedTask;

        private static Task<CaseWorkflowPriorityService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowPriorityService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, userName));
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task ListActiveOnlyWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListActiveOnlyAsync());
        }

        [Fact]
        public async Task ListActiveOnlyReturnsFixedThreePrioritiesInOrderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var priorities = await service.ListActiveOnlyAsync();

            priorities.Should().HaveCount(3);
            priorities.Select(p => (p.Id, p.Name)).Should().ContainInOrder(
                (1, "High"),
                (2, "Medium"),
                (3, "Low"));
        }

        [Fact]
        public async Task ListActiveOnlyIsIdenticalRegardlessOfCallerTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var serviceTenantA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceTenantB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var prioritiesA = await serviceTenantA.ListActiveOnlyAsync();
            var prioritiesB = await serviceTenantB.ListActiveOnlyAsync();

            prioritiesA.Select(p => (p.Id, p.Name)).Should()
                .BeEquivalentTo(prioritiesB.Select(p => (p.Id, p.Name)));
        }

        [Fact]
        public async Task ListActiveOnlyDoesNotPublishAChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: bus);

            await service.ListActiveOnlyAsync();

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ListActiveOnlyHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => service.ListActiveOnlyAsync(cts.Token));
        }

        [Fact]
        public async Task ListActiveOnlyWritesOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.ListActiveOnlyAsync();

            auditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public async Task ListActiveOnlyWarnsNotErrorsOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log: log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListActiveOnlyAsync());

            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public void AgentCatalogueRegistersCaseWorkflowPriorityListTool()
        {
            var tools = ServiceToolCatalogue.All;

            tools.Should().Contain(t => t.Name == "CaseWorkflowPriorityList");
        }
    }
}