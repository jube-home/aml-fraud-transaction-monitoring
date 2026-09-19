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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Resources;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.PermissionSpecification;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.PermissionSpecification;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.PermissionSpecification
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PermissionSpecificationServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<int> createdTenantRegistryIds = [];
        private readonly List<string> createdUserNames = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var createdRoleRegistryIdsNullable = createdRoleRegistryIds.Select(id => (int?)id).ToList();

            await dbContext.RoleRegistryPermission
                .Where(w => createdRoleRegistryIdsNullable.Contains(w.RoleRegistryId)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.TenantRegistry.Where(w => createdTenantRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<PermissionSpecificationService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return PermissionSpecificationService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<string> CreateUserWithExactPermissionAsync(DbContext dbContext,
            int permissionSpecificationId)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantRegistryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}PsTenant{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            });
            createdTenantRegistryIds.Add(tenantRegistryId);

            var roleRegistryGuid = Guid.NewGuid();
            var roleRegistryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleRegistryGuid,
                Name = $"{DatabaseFixture.Prefix}PsRole{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            });
            createdRoleRegistryIds.Add(roleRegistryId);

            await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
            {
                Guid = Guid.NewGuid(),
                PermissionSpecificationId = permissionSpecificationId,
                RoleRegistryId = roleRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            });

            var userName = $"{DatabaseFixture.Prefix}PsUser{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            });
            createdUserNames.Add(userName);

            await dbContext.InsertAsync(new Data.Poco.UserInTenant
            {
                User = userName,
                TenantRegistryId = tenantRegistryId,
            });

            return userName;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                PermissionSpecificationService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task GetAsyncThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([36]);
        }

        [Fact]
        public async Task ListAsyncThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
        }

        [Fact]
        public async Task GetAsyncSucceedsForAUserGrantedExactlyPermissionThirtySixAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userName = await CreateUserWithExactPermissionAsync(dbContext, 36);
            var service = await BuildServiceAsync(dbContext, userName);

            var all = await service.GetAsync();

            all.Should().Contain(d => d.Id == 36);
        }

        [Fact]
        public async Task GetAsyncDoesNotListTheRetiredObservabilitySpecificationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userName = await CreateUserWithExactPermissionAsync(dbContext, 36);
            var service = await BuildServiceAsync(dbContext, userName);

            var all = await service.GetAsync();

            all.Should().NotContain(d => d.Id == 27);
            all.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetAsyncSucceedsForLandlordUserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var all = await service.GetAsync();

            all.Should().Contain(d => d.Id == 36);
        }

        [Fact]
        public async Task GetAsyncReturnsTheKnownBaselineCatalogueRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var all = await service.GetAsync();

            all.Should().Contain(d => d.Id == 1 && d.Name == "Read Write Case");
            all.Should().Contain(d => d.Id == 36 && d.Name == "Read Write Security Role User Permission");
        }

        [Fact]
        public async Task CatalogueIsNotTenantScopedAndIsIdenticalFromEveryTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userNameTenantA = await CreateUserWithExactPermissionAsync(dbContext, 36);
            var userNameTenantB = await CreateUserWithExactPermissionAsync(dbContext, 36);

            var tenantAService = await BuildServiceAsync(dbContext, userNameTenantA);
            var tenantBService = await BuildServiceAsync(dbContext, userNameTenantB);

            var fromTenantA = await tenantAService.GetAsync();
            var fromTenantB = await tenantBService.GetAsync();

            fromTenantA.Select(d => d.Id).Should().BeEquivalentTo(fromTenantB.Select(d => d.Id));
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndOrdersByIdAscendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var page = await service.ListAsync(2);

            page.Items.Count.Should().Be(2);
            page.Items[0].Id.Should().BeLessThan(page.Items[1].Id);
        }

        [Fact]
        public async Task ListAsyncClampsTakeToAtLeastOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var page = await service.ListAsync(0);

            page.Items.Count.Should().Be(1);
        }

        [Fact]
        public async Task ListAsyncClampsTakeToAtMostTwoHundredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var page = await service.ListAsync(10_000);

            page.Items.Count.Should().BeLessThanOrEqualTo(200);
        }

        [Fact]
        public async Task ListAsyncPagesWithAfterIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var firstPage = await service.ListAsync(1);
            var secondPage = await service.ListAsync(1, firstPage.Items[0].Id);

            secondPage.Items.Should().ContainSingle();
            secondPage.Items[0].Id.Should().BeGreaterThan(firstPage.Items[0].Id);
        }

        [Fact]
        public async Task PreCancelledTokenOnGetThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task SuccessfulGetLogsNoInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, log);

            await service.GetAsync();

            log.Entries.Should().NotContain(e => e.Level == "INFO");
        }

        [Fact]
        public async Task WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, log);

            await service.GetAsync();

            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, log);

            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync());

            var errorEntry = log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task EachCallEmitsOneSpanWithOutcomeTagAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            await service.GetAsync();

            var listSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "PermissionSpecification.List").Subject;
            listSpan.GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            await service.GetAsync();

            collector.GetMeasurementSnapshot().Should().ContainSingle(m => (string)m.Tags["operation"].Required() == "List");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineIncludingReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=List");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, serviceChangeBus: serviceChangeBus);
            var forbiddenService = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync();
            await Assert.ThrowsAsync<ForbiddenException>(() => forbiddenService.GetAsync());

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("PermissionSpecificationList");
        }

        [Fact]
        public async Task PermissionDeniedMessageResolvesForFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

                var frenchLocalizer = localizers.Create(typeof(PermissionSpecificationResources));
                ex.Message.Should().Be(frenchLocalizer[PermissionSpecificationResources.PermissionDenied].Value);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}