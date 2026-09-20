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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Repository.UserRegistry;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.UserRegistry;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.UserRegistry.Models;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using UserRegistryService = Jube.Service.Repository.UserRegistry.UserRegistryService;

namespace Jube.Test.Service.Repository.UserRegistry
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class UserRegistryServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly Jube.DynamicEnvironment.DynamicEnvironment dynamicEnvironment =
            TestDynamicEnvironment.Create();

        private readonly List<int> createdIds = [];
        private readonly List<int> createdRoleIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.GetTable<UserRegistryVersion>().Where(w => w.UserRegistryId == id).DeleteAsync();
                await dbContext.UserRegistry.Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var roleId in createdRoleIds)
            {
                await dbContext.RoleRegistryPermission.Where(w => w.RoleRegistryId == roleId).DeleteAsync();
                await dbContext.RoleRegistry.Where(w => w.Id == roleId).DeleteAsync();
            }
        }

        private static Task<UserRegistryService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return UserRegistryService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                dynamicEnvironment, auditLog ?? TestLog.NoOp);
        }

        private async Task<Guid> CreateParentRoleAsync(DbContext dbContext, string createdUser)
        {
            var repository = new RoleRegistryRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.RoleRegistry
            {
                Name = UniqueName("Role"),
                Active = 1,
                Locked = 0,
                Deleted = 0
            }).ConfigureAwait(false);

            createdRoleIds.Add(saved.Id);
            return saved.Guid;
        }

        private static UserRegistryDto NewDto(Guid roleRegistryGuid, string? name)
        {
            return new UserRegistryDto
            {
                RoleRegistryGuid = roleRegistryGuid,
                Name = name,
                Email = $"{name}@example.invalid"
            };
        }

        private static string UniqueName(string label)
        {
            return $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..40];
        }

        [Fact]
        public async Task ConcurrentCreatesOfTheSameUserNameInTwoTenantsLetExactlyOneWinAsync()
        {
            var name = UniqueName("Race");
            const int attempts = 8;

            await using var setup = fx.GetDbContext();
            var roleA = await CreateParentRoleAsync(setup, fx.Seed.UserWithPermission);
            var roleB = await CreateParentRoleAsync(setup, fx.Seed.UserTenantB);

            var results = await Task.WhenAll(Enumerable.Range(0, attempts).Select(async i =>
            {
                await using var dbContext = fx.GetDbContext();
                var user = i % 2 == 0 ? fx.Seed.UserWithPermission : fx.Seed.UserTenantB;
                var role = i % 2 == 0 ? roleA : roleB;
                var service = await BuildServiceAsync(dbContext, user);
                var casing = i % 3 == 0 ? name.ToUpperInvariant() : name;

                try
                {
                    var saved = await service.InsertAsync(NewDto(role, casing));
                    return new RaceAttempt(saved.Id, null);
                }
                catch (Exception ex)
                {
                    return new RaceAttempt(null, ex);
                }
            }));

            createdIds.AddRange(results.Where(r => r.SavedId != null).Select(r => r.SavedId.Required()));

            results.Count(r => r.SavedId != null).Should().Be(1, "the database allows one live user per name");
            foreach (var failure in results.Where(r => r.Error != null))
            {
                var validation = failure.Error.Should().BeOfType<DtoValidationException>(
                    "a lost race is a validation outcome, never a server error").Subject;
                validation.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");
            }

            (await setup.UserRegistry.CountAsync(u => u.Name != null && u.Name.ToLower() == name.ToLower()
                                                                     && (u.Deleted == 0 || u.Deleted == null))).Should()
                .Be(1);
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("Insert")));
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().NotBeNull();
            saved.CreatedDate.Required().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAndGetByIdReturnsItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var name = UniqueName("GetAll");
            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, name));
            createdIds.Add(saved.Id);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.Name == name);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().NotBeNull();
            byId.Required().Name.Should().Be(name);
            byId.Required().Email.Should().Be($"{name}@example.invalid");
        }

        [Fact]
        public async Task GetByRoleRegistryGuidReturnsOnlyThatRolesUsersAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleAGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var roleBGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var savedA = await service.InsertAsync(NewDto(roleAGuid, UniqueName("ByRoleA")));
            createdIds.Add(savedA.Id);
            var savedB = await service.InsertAsync(NewDto(roleBGuid, UniqueName("ByRoleB")));
            createdIds.Add(savedB.Id);

            var forRoleA = await service.GetByRoleRegistryGuidAsync(roleAGuid);
            forRoleA.Should().Contain(d => d.Id == savedA.Id);
            forRoleA.Should().NotContain(d => d.Id == savedB.Id);
        }

        [Fact]
        public async Task UpdateIncrementsVersionAndWritesAuditRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("Update")));
            createdIds.Add(saved.Id);

            var dto = NewDto(roleRegistryGuid, saved.Name);
            dto.Id = saved.Id;
            dto.Active = true;

            var updated = await service.UpdateAsync(dto);

            updated.Version.Should().Be(2);
            updated.Active.Should().Be(1);
            updated.Guid.Should().Be(saved.Guid);

            var auditRows = await dbContext.GetTable<UserRegistryVersion>()
                .Where(w => w.UserRegistryId == saved.Id).CountAsync();
            auditRows.Should().Be(1);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndRowDisappearsFromReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("Delete")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            var all = await service.GetAsync();
            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync()
        {
            await using var writerDb = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(writerDb, fx.Seed.UserWithPermission);
            var writer = await BuildServiceAsync(writerDb, fx.Seed.UserWithPermission);
            var seedRow = await writer.InsertAsync(NewDto(roleRegistryGuid, UniqueName("Forbidden")));
            createdIds.Add(seedRow.Id);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var beforeCount = await dbContext.UserRegistry
                .CountAsync(w => w.Name != null && w.Name.StartsWith(DatabaseFixture.Prefix));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("Denied"))));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(seedRow.Id));

            var updateDto = NewDto(roleRegistryGuid, seedRow.Name);
            updateDto.Id = seedRow.Id;
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seedRow.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.UpdatePasswordAsync(new UserRegistryPasswordResetDto { Id = seedRow.Id }));

            var afterCount = await dbContext.UserRegistry
                .CountAsync(w => w.Name != null && w.Name.StartsWith(DatabaseFixture.Prefix));
            afterCount.Should().Be(beforeCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                UserRegistryService.CreateAsync(dbContext, userName, log, localizers, new NullServiceChangeBus(),
                    dynamicEnvironment, TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task UnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(roleRegistryGuid, UniqueName("Isolation")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var byId = await otherTenant.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            var tenantBRoleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserTenantB);
            var updateDto = NewDto(tenantBRoleRegistryGuid, UniqueName("IsolationB"));
            updateDto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.DeleteAsync(saved.Id));

            var stillThere = await owner.GetByIdAsync(saved.Id);
            stillThere.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllAsTenantBNeverContainsTenantARowsAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(roleRegistryGuid, UniqueName("TenantAOnly")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var all = await otherTenant.GetAsync();

            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NameRequiredRejectsNullEmptyOrWhitespaceAsync(string? name)
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(roleRegistryGuid, name);
            dto.Name = name;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(UserRegistryDto.Name) && e.ErrorCode == "NameNotEmpty");
        }

        [Fact]
        public async Task NameOverMaximumLengthIsRejectedWithErrorCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(roleRegistryGuid, new string('n', 257));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameMaximumLength");
        }

        [Fact]
        public async Task DuplicateNameFailsWithinTenantAndAcrossTenantsAsync()
        {
            var name = UniqueName("Dup");

            await using var dbContext = fx.GetDbContext();
            var roleAGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var first = await service.InsertAsync(NewDto(roleAGuid, name));
            createdIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(roleAGuid, name)));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");

            var roleBGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserTenantB);
            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var crossTenant = await Assert.ThrowsAsync<DtoValidationException>(() =>
                otherTenantService.InsertAsync(NewDto(roleBGuid, name)));
            crossTenant.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");
        }

        [Fact]
        public async Task DuplicateNameDifferingOnlyByCaseWithinTenantFailsAsync()
        {
            var name = UniqueName("CaseDup");

            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = await service.InsertAsync(NewDto(roleRegistryGuid, name));
            createdIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(roleRegistryGuid, name.ToUpperInvariant())));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");
        }

        [Fact]
        public async Task EmptyRoleRegistryGuidIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(Guid.Empty, UniqueName("BadRole"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "RoleRegistryGuidNotEmpty");
        }

        [Fact]
        public async Task NonExistentRoleRegistryGuidIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(Guid.NewGuid(), UniqueName("MissingRole"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "RoleRegistryGuidNotFound");
        }

        [Fact]
        public async Task RoleRegistryGuidBelongingToAnotherTenantIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleBGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(roleBGuid, UniqueName("CrossTenantRole"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "RoleRegistryGuidNotFound");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-an-email")]
        public async Task InvalidEmailIsRejectedAsync(string? email)
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(roleRegistryGuid, UniqueName("BadEmail"));
            dto.Email = email;

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(roleRegistryGuid, UniqueName("Tamper"));
            dto.CreatedUser = "someone-else";
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("TamperUpdate")));
            createdIds.Add(saved.Id);

            var dto = NewDto(roleRegistryGuid, saved.Name);
            dto.Id = saved.Id;
            dto.CreatedUser = "someone-else";
            dto.Version = 999;

            var updated = await service.UpdateAsync(dto);
            updated.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            updated.Guid.Should().Be(saved.Guid);
            updated.Version.Should().Be(2);
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("PreDeleted")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var dto = NewDto(roleRegistryGuid, saved.Name);
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfNeverExistedIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(roleRegistryGuid, UniqueName("Missing"));
            dto.Id = int.MaxValue - 1;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue - 1));

            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("DoubleDelete")));
            createdIds.Add(saved.Id);
            await service.DeleteAsync(saved.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Id));
        }

        [Fact]
        public async Task ActiveRoundTripsBoolToByteAndBackAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(roleRegistryGuid, UniqueName("ActiveRoundTrip"));
            dto.Active = true;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            var fetched = await service.GetByIdAsync(saved.Id);
            fetched.Required().Active.Should().BeTrue();
        }

        [Fact]
        public async Task GetByIdForMissingIdReturnsNullNotExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetByIdAsync(int.MaxValue - 1);
            result.Should().BeNull();
        }

        [Fact]
        public async Task PreCancelledTokenOnReadThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("LogInsert")));
            createdIds.Add(saved.Id);

            log.Entries.Count(e => e.Level == "INFO").Should().Be(1);

            var infoCountBeforeRead = log.Entries.Count(e => e.Level == "INFO");
            await service.GetAsync();
            log.Entries.Count(e => e.Level == "INFO").Should().Be(infoCountBeforeRead);
        }

        [Fact]
        public async Task WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("Gated")));
            createdIds.Add(saved.Id);

            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

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
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("Span")));
            createdIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "UserRegistry.Create").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("Metric")));
            createdIds.Add(saved.Id);

            collector.GetMeasurementSnapshot().Should()
                .ContainSingle(m => (string)m.Tags["operation"].Required() == "Create");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineIncludingReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=List");
        }

        [Fact]
        public async Task InsertPublishesExactlyOneCreatedEventAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("Reactive")));
            createdIds.Add(saved.Id);

            serviceChangeBus.Published.Should().ContainSingle();
            serviceChangeBus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            serviceChangeBus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync();
            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(roleRegistryGuid, "")));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain(
            [
                "UserRegistryList", "UserRegistryGet", "UserRegistryListByRoleRegistryGuid",
                "UserRegistryCreate", "UserRegistryUpdate", "UserRegistryDelete"
            ]);
            names.Should().NotContain("UserRegistryUpdatePassword");
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName($"Page{i}")));
                createdIds.Add(saved.Id);
            }

            var page = await service.ListAsync(2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }

        [Fact]
        public async Task UpdatePasswordReturnsCleartextOnceAndPersistsArgon2HashAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("PasswordReset")));
            createdIds.Add(saved.Id);

            var response = await service.UpdatePasswordAsync(
                new UserRegistryPasswordResetDto { Id = saved.Id, WirePasswordHash = false });

            response.Password.Should().NotBeNullOrEmpty();
            response.PasswordExpiryDate.Should().NotBeNull();

            var persisted = await dbContext.UserRegistry.SingleAsync(w => w.Id == saved.Id);
            persisted.Password.Should().NotBeNullOrEmpty();
            Data.Security.HashPassword.Verify(persisted.Password, response.Password.Required(),
                dynamicEnvironment.AppSettings("PasswordHashingKey")).Should().BeTrue();
            persisted.PasswordLocked.Should().Be(0);
            persisted.FailedPasswordCount.Should().Be(0);
        }

        [Fact]
        public async Task UpdatePasswordWithWirePasswordHashSaltsWithNameBeforeHashingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var name = UniqueName("WirePassword");
            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, name));
            createdIds.Add(saved.Id);

            var response = await service.UpdatePasswordAsync(
                new UserRegistryPasswordResetDto { Id = saved.Id, WirePasswordHash = true });

            var persisted = await dbContext.UserRegistry.SingleAsync(w => w.Id == saved.Id);
            var expectedPreHash = Data.Security.HashPassword.Sha256(response.Password.Required() + name);
            Data.Security.HashPassword.Verify(persisted.Password, expectedPreHash,
                dynamicEnvironment.AppSettings("PasswordHashingKey")).Should().BeTrue();
            persisted.WirePasswordHash.Should().Be(1);
        }

        [Fact]
        public async Task UpdatePasswordForMissingIdThrowsAndLogsErrorInsteadOfNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdatePasswordAsync(
                new UserRegistryPasswordResetDto { Id = int.MaxValue - 1 }));

            log.Entries.Should().Contain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task DuplicateNameErrorMessageResolvesFrenchTranslationAsync()
        {
            var originalCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
                var name = UniqueName("FrenchDup");
                var first = await service.InsertAsync(NewDto(roleRegistryGuid, name));
                createdIds.Add(first.Id);

                var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.InsertAsync(NewDto(roleRegistryGuid, name)));

                ex.Result.Errors.Should().Contain(e =>
                    e.ErrorCode == "NameDuplicate" && e.ErrorMessage == "Ce nom existe déjà.");
            }
            finally
            {
                CultureInfo.CurrentUICulture = originalCulture;
            }
        }

        [Fact]
        public async Task UpdatePreservesPasswordLockedWhenDtoLeavesItUnsetAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("LockThenUpdate")));
            createdIds.Add(saved.Id);

            await dbContext.UserRegistry.Where(w => w.Id == saved.Id)
                .Set(s => s.PasswordLocked, (byte)1).UpdateAsync();

            var dto = NewDto(roleRegistryGuid, saved.Name);
            dto.Id = saved.Id;
            dto.PasswordLocked = null;

            await service.UpdateAsync(dto);

            var persisted = await dbContext.UserRegistry.SingleAsync(w => w.Id == saved.Id);
            persisted.PasswordLocked.Should().Be(1);
        }

        [Fact]
        public async Task UpdateChangesPasswordLockedWhenDtoExplicitlySetsItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await CreateParentRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(roleRegistryGuid, UniqueName("LockExplicit")));
            createdIds.Add(saved.Id);

            await dbContext.UserRegistry.Where(w => w.Id == saved.Id)
                .Set(s => s.PasswordLocked, (byte)1).UpdateAsync();

            var dto = NewDto(roleRegistryGuid, saved.Name);
            dto.Id = saved.Id;
            dto.PasswordLocked = false;
            await service.UpdateAsync(dto);
            (await dbContext.UserRegistry.SingleAsync(w => w.Id == saved.Id)).PasswordLocked.Should().Be(0);

            dto.PasswordLocked = true;
            await service.UpdateAsync(dto);
            (await dbContext.UserRegistry.SingleAsync(w => w.Id == saved.Id)).PasswordLocked.Should().Be(1);
        }
    }
}