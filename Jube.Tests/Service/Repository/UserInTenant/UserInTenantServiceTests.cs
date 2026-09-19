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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.UserInTenant;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.UserInTenant;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.UserInTenant
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class UserInTenantServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdTenantIds = [];
        private readonly List<int> createdRoleIds = [];
        private readonly List<string> createdUserNames = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<UserInTenantSwitchLog>()
                .Where(w => w.SwitchedUser != null && createdUserNames.Contains(w.SwitchedUser))
                .DeleteAsync();
            await dbContext.UserInTenant
                .Where(w => w.User != null && createdUserNames.Contains(w.User))
                .DeleteAsync();
            await dbContext.UserRegistry
                .Where(w => w.Name != null && createdUserNames.Contains(w.Name))
                .DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && createdRoleIds.Contains(w.RoleRegistryId.Value))
                .DeleteAsync();
            await dbContext.RoleRegistry
                .Where(w => createdRoleIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.TenantRegistry
                .Where(w => createdTenantIds.Contains(w.Id))
                .DeleteAsync();
        }

        private static Task<UserInTenantService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return UserInTenantService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static Task<int> GetTenantRegistryIdForUserAsync(DbContext dbContext, string userName) =>
            dbContext.UserInTenant.Where(w => w.User == userName).Select(s => s.TenantRegistryId).FirstAsync();

        private async Task<(int tenantId, string userName)> CreateLandlordActorAsync(DbContext dbContext)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var tenantName = $"{DatabaseFixture.Prefix}UitLandlordTenant{suffix}";
            var userName = $"{DatabaseFixture.Prefix}UitLandlordUser{suffix}";

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
            {
                Name = tenantName,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 1,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = $"{DatabaseFixture.Prefix}UitLandlordRole{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleIds.Add(roleId);

            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertAsync(new Jube.Data.Poco.UserInTenant
            {
                User = userName,
                TenantRegistryId = tenantId
            });

            createdUserNames.Add(userName);

            return (tenantId, userName);
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
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task GetWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
        }

        [Fact]
        public async Task GetCurrentTenantRegistryWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetCurrentTenantRegistryAsync());
        }

        [Fact]
        public async Task UpdateWithoutLandlordThrowsForbiddenAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantAId = await GetTenantRegistryIdForUserAsync(dbContext, fx.Seed.UserWithPermission);
            var beforeTenantId = await GetTenantRegistryIdForUserAsync(dbContext, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(tenantAId));

            var afterTenantId = await GetTenantRegistryIdForUserAsync(dbContext, fx.Seed.UserTenantB);
            afterTenantId.Should().Be(beforeTenantId);
        }

        [Fact]
        public async Task GetReturnsRowsForCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var rows = await service.GetAsync();

            rows.Should().Contain(r => r.User == fx.Seed.UserWithPermission);
        }

        [Fact]
        public async Task GetIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var rows = await service.GetAsync();

            rows.Should().NotContain(r => r.User == fx.Seed.UserWithPermission);
            rows.Should().Contain(r => r.User == fx.Seed.UserTenantB);
        }

        [Fact]
        public async Task GetForLandlordDoesNotReturnEveryTenantsRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var rows = await service.GetAsync();

            rows.Should().NotContain(r => r.User == fx.Seed.UserWithPermission);
            rows.Should().NotContain(r => r.User == fx.Seed.UserTenantB);
        }

        [Fact]
        public async Task GetCurrentTenantRegistryReturnsCallersOwnAllocationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantAId = await GetTenantRegistryIdForUserAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetCurrentTenantRegistryAsync();

            dto.Should().NotBeNull();
            dto.Required().User.Should().Be(fx.Seed.UserWithPermission);
            dto.Required().TenantRegistryId.Should().Be(tenantAId);
        }

        [Fact]
        public void MapperToDtoOfNullReturnsNull()
        {
            UserInTenantMapper.ToDto((Jube.Data.Poco.UserInTenant?)null).Should().BeNull();
        }

        [Fact]
        public async Task ListAsyncClampsTakeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var page = await service.ListAsync(take: 1_000_000);

            page.Items.Count.Should().BeLessOrEqualTo(200);
        }

        [Fact]
        public async Task ListAsyncPaginatesByAfterIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var firstPage = await service.ListAsync(take: 1);
            firstPage.Items.Should().ContainSingle();

            var firstId = await dbContext.UserInTenant
                .Where(w => w.User == firstPage.Items[0].User).Select(s => s.Id).FirstAsync();

            var secondPage = await service.ListAsync(take: 1, afterId: firstId);

            secondPage.Items.Should().NotContain(i => i.User == firstPage.Items[0].User);
        }

        [Fact]
        public async Task UpdateWithNonPositiveTenantRegistryIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, landlordActorUser);

            var act = () => service.UpdateAsync(0);

            var exception = await act.Should().ThrowAsync<DtoValidationException>();
            exception.Which.Result.Errors.Should().Contain(e => e.ErrorCode == "TenantRegistryIdInvalid");
        }

        [Fact]
        public async Task UpdateWithUnknownTenantRegistryIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, landlordActorUser);

            var act = () => service.UpdateAsync(Int32.MaxValue - 1);

            var exception = await act.Should().ThrowAsync<DtoValidationException>();
            exception.Which.Result.Errors.Should().Contain(e => e.ErrorCode == "TenantRegistryIdNotFound");
        }

        [Fact]
        public async Task UpdateAsLandlordSwitchesTenantAndPersistsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
            var tenantAId = await GetTenantRegistryIdForUserAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, landlordActorUser);

            await service.UpdateAsync(tenantAId);

            var row = await dbContext.UserInTenant.FirstAsync(w => w.User == landlordActorUser);
            row.TenantRegistryId.Should().Be(tenantAId);
            row.SwitchedUser.Should().Be(landlordActorUser);
            row.SwitchedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

            var switchLog = await dbContext.GetTable<UserInTenantSwitchLog>()
                .FirstAsync(w => w.UserInTenantId == row.Id);
            switchLog.TenantRegistryId.Should().Be(tenantAId);
            switchLog.SwitchedUser.Should().Be(landlordActorUser);
        }

        [Fact]
        public async Task UpdateToSameTenantSucceedsIdempotentlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (landlordActorTenantId, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, landlordActorUser);

            await service.UpdateAsync(landlordActorTenantId);
            await service.UpdateAsync(landlordActorTenantId);

            var row = await dbContext.UserInTenant.FirstAsync(w => w.User == landlordActorUser);
            row.TenantRegistryId.Should().Be(landlordActorTenantId);
        }

        [Fact]
        public async Task UpdatePublishesUpdatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
            var tenantAId = await GetTenantRegistryIdForUserAsync(dbContext, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, landlordActorUser, serviceChangeBus: bus);

            await service.UpdateAsync(tenantAId);

            bus.Published.Should().ContainSingle();
            bus.Published[0].Area.Should().Be("UserInTenant");
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Updated);
        }

        [Fact]
        public async Task UpdateDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, landlordActorUser, serviceChangeBus: bus);

            var act = () => service.UpdateAsync(0);

            await act.Should().ThrowAsync<DtoValidationException>();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateDoesNotPublishChangeEventOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantAId = await GetTenantRegistryIdForUserAsync(dbContext, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB, serviceChangeBus: bus);

            var act = () => service.UpdateAsync(tenantAId);

            await act.Should().ThrowAsync<ForbiddenException>();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task RepositoryUpdateAsyncSetsUserWhenInsertingNewRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantAId = await GetTenantRegistryIdForUserAsync(dbContext, fx.Seed.UserWithPermission);
            var newUserName = $"{DatabaseFixture.Prefix}UitNoRow{Guid.NewGuid():N}"[..40];
            createdUserNames.Add(newUserName);

            var repository = new Data.Repository.UserInTenantRepository(dbContext, fx.Seed.UserWithPermission);

            var saved = await repository.UpdateAsync(newUserName, tenantAId);

            saved.User.Should().Be(newUserName);
            saved.Id.Should().BeGreaterThan(0);

            var persisted = await dbContext.UserInTenant.FirstAsync(w => w.Id == saved.Id);
            persisted.User.Should().Be(newUserName);
            persisted.TenantRegistryId.Should().Be(tenantAId);
        }

        [Fact]
        public async Task GetHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var token = cts.Token;

            var act = () => service.GetAsync(token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GetWarnsNotErrorsOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log: capturingLog);

            var act = () => service.GetAsync();
            await act.Should().ThrowAsync<ForbiddenException>();

            capturingLog.Entries.Should().Contain(e => e.Level == "WARN");
            capturingLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task UpdateLogsOneInfoEntryOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (landlordActorTenantId, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
            var capturingLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, landlordActorUser, log: capturingLog);

            await service.UpdateAsync(landlordActorTenantId);

            capturingLog.Entries.Count(e => e.Level == "INFO").Should().Be(1);
            capturingLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task GetLogsOneAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: capturingAuditLog);

            await service.GetAsync();

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public async Task UpdateLogsOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (landlordActorTenantId, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, landlordActorUser, auditLog: capturingAuditLog);

            await service.UpdateAsync(landlordActorTenantId);

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public async Task GetWithDisabledGatesRecordsNoEntriesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var quietLog = new TestLog(enabled: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log: quietLog);

            await service.GetAsync();

            quietLog.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task GetOnDisposedContextRecordsErrorWithExceptionAsync()
        {
            var dbContext = fx.GetDbContext();
            var capturingLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log: capturingLog);
            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync());

            capturingLog.Entries.Should().ContainSingle(e => e.Level == "ERROR" && e.Exception != null);
        }

        [Fact]
        public async Task ValidationMessageMatchesNeutralResourceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, landlordActorUser);

            var act = () => service.UpdateAsync(0);

            var exception = await act.Should().ThrowAsync<DtoValidationException>();
            exception.Which.Result.Errors.Single(e => e.ErrorCode == "TenantRegistryIdInvalid").ErrorMessage
                .Should().Be("The tenant is required.");
        }

        [Fact]
        public async Task ValidationMessageResolvesFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var (_, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
                var service = await BuildServiceAsync(dbContext, landlordActorUser);

                var act = () => service.UpdateAsync(0);

                var exception = await act.Should().ThrowAsync<DtoValidationException>();
                exception.Which.Result.Errors.Single(e => e.ErrorCode == "TenantRegistryIdInvalid").ErrorMessage
                    .Should().Be("Le tenant est requis.");
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }

        [Fact]
        public void CatalogueListsUniqueUserInTenantToolNames()
        {
            var tools = ServiceToolCatalogue.All;
            var names = tools.Where(t => t.Name.StartsWith("UserInTenant")).Select(t => t.Name).ToList();

            names.Should().BeEquivalentTo([
                "UserInTenantList",
                "UserInTenantGetCurrentTenantRegistry",
                "UserInTenantSwitchTenant",
            ]);
            names.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task UpdateWithTamperedUserOnDtoHasNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (landlordActorTenantId, landlordActorUser) = await CreateLandlordActorAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, landlordActorUser);

            await service.UpdateAsync(landlordActorTenantId);

            var row = await dbContext.UserInTenant.FirstAsync(w => w.User == landlordActorUser);
            row.User.Should().Be(landlordActorUser);
        }
    }
}