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
using Jube.Dto.Repository.UserRegistryApiKey;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.UserRegistryApiKey;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.UserRegistryApiKey;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Repository.UserRegistryApiKey.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Repository.UserRegistryApiKey
{
    using UserRegistryApiKeyService = global::Jube.Service.Repository.UserRegistryApiKey.UserRegistryApiKeyService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class UserRegistryApiKeyServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdApiKeyIds = [];

        public Task InitializeAsync()
        {
            Environment.SetEnvironmentVariable("ApiHmacKey", $"{DatabaseFixture.Prefix}HmacSecretForTests");
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.UserRegistryApiKey
                .Where(w => createdApiKeyIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<UserRegistryApiKeyService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null,
            IUserRegistryApiKeyCacheInvalidator? cacheInvalidator = null)
        {
            return UserRegistryApiKeyService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp,
                localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                TestDynamicEnvironment.Create(),
                cacheInvalidator ?? NullUserRegistryApiKeyCacheInvalidator.Instance, auditLog ?? TestLog.NoOp);
        }

        private static Task<int> GetOwnUserRegistryIdAsync(DbContext dbContext, string userName) =>
            dbContext.UserRegistry.Where(u => u.Name == userName).Select(u => u.Id).FirstAsync();

        private async Task<UserRegistryApiKeyDto> InsertKeyAsync(UserRegistryApiKeyService service,
            int userRegistryId, string? name = null, string? description = "Test description")
        {
            var saved = await service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = userRegistryId,
                Name = name ?? $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..40],
                Description = description,
            });

            createdApiKeyIds.Add(saved.Id);
            return saved;
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
        public async Task ListWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithoutPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByUserRegistryIdAsync(userRegistryId));
        }

        [Fact]
        public async Task InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithoutPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = userRegistryId,
                Name = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..40],
            }));

            (await dbContext.UserRegistryApiKey.AnyAsync(w => w.UserRegistryId == userRegistryId))
                .Should().BeFalse();
        }

        [Fact]
        public async Task DeleteWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(1));
        }

        [Fact]
        public async Task InsertPersistsAndReturnsFullPlaintextKeyOnceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await InsertKeyAsync(service, userRegistryId);

            saved.Id.Should().BeGreaterThan(0);
            saved.UserRegistryId.Should().Be(userRegistryId);
            saved.CreatedDate.Should().NotBeNull();
            saved.ApiKeyDisplay.Should().NotBeNullOrEmpty();
            saved.ApiKeyDisplay.Required().Length.Should().BeGreaterThan(8);
        }

        [Fact]
        public async Task InsertPersistsOnlyTheHashAndAnEightCharacterDisplayPrefixAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await InsertKeyAsync(service, userRegistryId);

            var row = await dbContext.UserRegistryApiKey.FirstAsync(w => w.Id == saved.Id);
            row.ApiKey.Should().NotBeNullOrEmpty();
            row.ApiKey.Should().NotBe(saved.ApiKeyDisplay);
            row.ApiKeyDisplay.Should().HaveLength(8);
            saved.ApiKeyDisplay.Should().StartWith(row.ApiKeyDisplay);
        }

        [Fact]
        public async Task ListReturnsMaskedDisplayNeverTheFullPlaintextKeyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await InsertKeyAsync(service, userRegistryId);

            var list = await service.GetByUserRegistryIdAsync(userRegistryId);

            var listed = list.Single(w => w.Id == saved.Id);
            listed.ApiKeyDisplay.Should().HaveLength(8);
            listed.ApiKeyDisplay.Should().NotBe(saved.ApiKeyDisplay);
        }

        [Fact]
        public async Task InsertWithMissingNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = userRegistryId,
                Name = null,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithOverlongNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = userRegistryId,
                Name = new string('a', 257),
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithOverlongDescriptionThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = userRegistryId,
                Name = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..40],
                Description = new string('a', 1025),
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithZeroUserRegistryIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = 0,
                Name = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..40],
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithUnknownUserRegistryIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = int.MaxValue,
                Name = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..40],
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithCrossTenantUserRegistryIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryIdB = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = userRegistryIdB,
                Name = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..40],
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertIgnoresClientSuppliedIdAndApiKeyDisplayAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(new UserRegistryApiKeyDto
            {
                Id = int.MaxValue,
                UserRegistryId = userRegistryId,
                Name = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..40],
                ApiKeyDisplay = "attacker-supplied",
                CreatedDate = DateTimeOffset.UtcNow.AddYears(-5),
            });
            createdApiKeyIds.Add(saved.Id);

            saved.Id.Should().NotBe(int.MaxValue);
            saved.ApiKeyDisplay.Should().NotBe("attacker-supplied");
            saved.CreatedDate.Should().NotBeNull();
            saved.CreatedDate.Required().Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task InsertPublishesCreatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            var saved = await InsertKeyAsync(service, userRegistryId);

            bus.Published.Should().ContainSingle();
            bus.Published[0].Area.Should().Be("UserRegistryApiKey");
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            bus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task InsertDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            var act = () => service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = 0,
                Name = null,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task InsertPublishesCacheInvalidationWithTheHashAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var cacheInvalidator = new CapturingCacheInvalidator();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                cacheInvalidator: cacheInvalidator);

            var saved = await InsertKeyAsync(service, userRegistryId);
            var row = await dbContext.UserRegistryApiKey.FirstAsync(w => w.Id == saved.Id);

            cacheInvalidator.Created.Should().ContainSingle().Which.Should().Be(row.ApiKey);
        }

        [Fact]
        public async Task ListIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryIdB = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            await InsertKeyAsync(serviceB, userRegistryIdB);

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var listForA = await serviceA.GetByUserRegistryIdAsync(userRegistryIdB);

            listForA.Should().BeEmpty();
        }

        [Fact]
        public async Task ListForUnknownUserRegistryIdReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var list = await service.GetByUserRegistryIdAsync(int.MaxValue);

            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListExcludesRevokedKeysAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await InsertKeyAsync(service, userRegistryId);
            await service.DeleteAsync(saved.Id);

            var list = await service.GetByUserRegistryIdAsync(userRegistryId);

            list.Should().NotContain(w => w.Id == saved.Id);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndPublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var saved = await InsertKeyAsync(service, userRegistryId);

            await service.DeleteAsync(saved.Id);

            var row = await dbContext.UserRegistryApiKey.FirstAsync(w => w.Id == saved.Id);
            row.Deleted.Should().Be(1);

            bus.Published.Should().HaveCount(2);
            bus.Published[1].Kind.Should().Be(ServiceChangeKind.Deleted);
            bus.Published[1].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task DeletePublishesCacheInvalidationWithTheHashAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var cacheInvalidator = new CapturingCacheInvalidator();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                cacheInvalidator: cacheInvalidator);
            var saved = await InsertKeyAsync(service, userRegistryId);
            var row = await dbContext.UserRegistryApiKey.FirstAsync(w => w.Id == saved.Id);

            await service.DeleteAsync(saved.Id);

            cacheInvalidator.Removed.Should().ContainSingle().Which.Should().Be(row.ApiKey);
        }

        [Fact]
        public async Task DeleteOfMissingIdSucceedsAsANoOpAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await service.DeleteAsync(int.MaxValue);

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteOfCrossTenantKeyIsIndistinguishableFromMissingIdAndLeavesRowIntactAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryIdB = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var savedB = await InsertKeyAsync(serviceB, userRegistryIdB);

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await serviceA.DeleteAsync(savedB.Id);

            (await dbContext.UserRegistryApiKey.SingleAsync(w => w.Id == savedB.Id)).Deleted.Should().NotBe(1);
        }

        [Fact]
        public async Task DeleteDoesNotPublishChangeEventOnCrossTenantFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryIdB = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var savedB = await InsertKeyAsync(serviceB, userRegistryIdB);

            var bus = new CapturingBus();
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await serviceA.DeleteAsync(savedB.Id);

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var token = cts.Token;

            var act = () => service.GetByUserRegistryIdAsync(int.MaxValue, token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task InsertWarnsNotErrorsOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithoutPermission);
            var capturingLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log: capturingLog);

            var act = () => service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = userRegistryId,
                Name = $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..40],
            });
            await act.Should().ThrowAsync<ForbiddenException>();

            capturingLog.Entries.Should().Contain(e => e.Level == "WARN");
            capturingLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertLogsOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userRegistryId = await GetOwnUserRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: capturingAuditLog);

            await InsertKeyAsync(service, userRegistryId);

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public async Task ListLogsOneAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: capturingAuditLog);

            await service.GetByUserRegistryIdAsync(int.MaxValue);

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public async Task DeleteOfMissingIdLogsOneAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: capturingAuditLog);

            await service.DeleteAsync(int.MaxValue);

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public void CatalogueListsOnlyTheNonSensitiveUserRegistryApiKeyToolNames()
        {
            var tools = ServiceToolCatalogue.All;
            var names = tools.Where(t => t.Name.StartsWith("UserRegistryApiKey"))
                .Select(t => t.Name).ToList();

            names.Should().BeEquivalentTo([
                "UserRegistryApiKeyListByUserRegistryId",
                "UserRegistryApiKeyDelete",
            ]);
            names.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void CatalogueDoesNotRegisterCreateAsAnAgentTool()
        {
            var tools = ServiceToolCatalogue.All;

            tools.Should().NotContain(t => t.Name == "UserRegistryApiKeyCreate");
        }
    }
}