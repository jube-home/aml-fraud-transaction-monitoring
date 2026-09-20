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
using Jube.Dto.Repository.TenantRegistry;
using Jube.Resources;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.TenantRegistry;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.TenantRegistry;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.TenantRegistry
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class TenantRegistryServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdTenantRegistryIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.TenantRegistryVersion>()
                .Where(w => createdTenantRegistryIds.Contains(w.TenantRegistryId)).DeleteAsync();
            await dbContext.TenantRegistry.Where(w => createdTenantRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<TenantRegistryService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return TenantRegistryService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static TenantRegistryDto ValidDto(string? name = null) => new()
        {
            Name = name ?? $"{DatabaseFixture.Prefix}Tenant{Guid.NewGuid():N}"[..40],
            Active = true,
            Locked = false,
        };

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
        public async Task ListWithoutLandlordThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
        }

        [Fact]
        public async Task GetByIdWithoutLandlordThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));
        }

        [Fact]
        public async Task GetByFilterWithoutLandlordThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByFilterAsync(""));
        }

        [Fact]
        public async Task ListPagedWithoutLandlordThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
        }

        [Fact]
        public async Task InsertWithoutLandlordThrowsForbiddenAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var name = $"{DatabaseFixture.Prefix}Tenant{Guid.NewGuid():N}"[..40];

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto(name)));

            (await dbContext.TenantRegistry.AnyAsync(w => w.Name == name)).Should().BeFalse();
        }

        [Fact]
        public async Task UpdateWithoutLandlordThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var created = await owner.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            created.Name += "x";

            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task DeleteWithoutLandlordThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var created = await owner.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(created.Id));
        }

        [Fact]
        public async Task InsertAsLandlordPersistsAndDropsIdentityFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var dto = ValidDto();
            dto.Id = 999999;
            dto.CreatedUser = "someone-else";

            var saved = await service.InsertAsync(dto);
            createdTenantRegistryIds.Add(saved.Id);

            saved.Id.Should().NotBe(999999);
            saved.CreatedUser.Should().Be(fx.Seed.LandlordUser);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var created = await service.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            await Task.Delay(50);
            created.Name += "-updated";
            var updated = await service.UpdateAsync(created);

            updated.Version.Should().Be(2);
            updated.CreatedUser.Should().Be(created.CreatedUser);
            updated.CreatedDate.Should().BeCloseTo(created.CreatedDate.Required(), TimeSpan.FromMilliseconds(10));
            updated.UpdatedUser.Should().Be(fx.Seed.LandlordUser);
            updated.UpdatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var created = await service.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            var originalCreatedDate = created.CreatedDate;
            created.CreatedUser = "tampered";
            created.CreatedDate = DateTimeOffset.UtcNow.AddYears(-5);
            created.Version = 999;
            var updated = await service.UpdateAsync(created);

            updated.CreatedUser.Should().NotBe("tampered");
            updated.CreatedDate.Should().BeCloseTo(originalCreatedDate.Required(), TimeSpan.FromMilliseconds(10));
            updated.Version.Should().Be(2);
        }

        [Fact]
        public async Task UpdateWithUnknownIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var dto = ValidDto();
            dto.Id = 999999;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var dto = ValidDto();
            dto.Locked = true;
            var created = await service.InsertAsync(dto);
            createdTenantRegistryIds.Add(created.Id);

            created.Name += "x";
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task DeleteRemovesRowAndSubsequentGetReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var created = await service.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            await service.DeleteAsync(created.Id);

            (await service.GetByIdAsync(created.Id)).Should().BeNull();
        }

        [Fact]
        public async Task DeleteOfMissingRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999999));
        }

        [Fact]
        public async Task DeleteOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var dto = ValidDto();
            dto.Locked = true;
            var created = await service.InsertAsync(dto);
            createdTenantRegistryIds.Add(created.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(created.Id));
        }

        [Fact]
        public async Task ListAsLandlordSeesTenantsCreatedByAnyoneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var landlordService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var created = await landlordService.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            var list = await landlordService.GetAsync();

            list.Should().Contain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task GetByFilterMatchesCaseInsensitiveSubstringAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var name = $"{DatabaseFixture.Prefix}FindMe{Guid.NewGuid():N}"[..40];
            var created = await service.InsertAsync(ValidDto(name));
            createdTenantRegistryIds.Add(created.Id);

            var list = await service.GetByFilterAsync(name[..10].ToUpperInvariant());

            list.Should().Contain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task GetByFilterWithBlankFilterReturnsAllAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var created = await service.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            var list = await service.GetByFilterAsync("");

            list.Should().Contain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task GetByFilterWithNullFilterDoesNotThrowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var list = await service.GetByFilterAsync(null);

            list.Should().NotBeNull();
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var first = await service.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(first.Id);
            var second = await service.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(second.Id);

            var page = await service.ListAsync(take: 500);

            page.Items.Count.Should().BeLessOrEqualTo(200);
            page.Items.Should().Contain(d => d.Id == first.Id);
            page.Items.Should().Contain(d => d.Id == second.Id);
        }

        [Fact]
        public async Task InsertWithBlankNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var dto = ValidDto();
            dto.Name = "";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithNullNameThrowsValidationFailedAndDoesNotThrowNullReferenceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var dto = ValidDto();
            dto.Name = null;

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithWhitespaceNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var dto = ValidDto();
            dto.Name = "   ";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithOverLongNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var dto = ValidDto(new string('a', 257));

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithNameAtMaximumLengthSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var unique = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";
            var name = unique + new string('a', 256 - unique.Length);
            var dto = ValidDto(name);

            var saved = await service.InsertAsync(dto);
            createdTenantRegistryIds.Add(saved.Id);

            saved.Name.Should().HaveLength(256);
        }

        [Fact]
        public async Task InsertWithDuplicateNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var name = $"{DatabaseFixture.Prefix}Dup{Guid.NewGuid():N}"[..40];
            var first = await service.InsertAsync(ValidDto(name));
            createdTenantRegistryIds.Add(first.Id);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(name)));
        }

        [Fact]
        public async Task InsertWithDuplicateNameDifferentCasingThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var name = $"{DatabaseFixture.Prefix}Case{Guid.NewGuid():N}"[..40];
            var first = await service.InsertAsync(ValidDto(name));
            createdTenantRegistryIds.Add(first.Id);

            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(ValidDto(name.ToUpperInvariant())));
        }

        [Fact]
        public async Task UpdateWithOwnUnchangedNameDoesNotFailDuplicateCheckAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var created = await service.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            created.Active = false;
            var updated = await service.UpdateAsync(created);

            updated.Active.Should().BeFalse();
        }

        [Fact]
        public async Task InsertRoundTripsActiveAndLockedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var dto = ValidDto();
            dto.Active = false;
            dto.Locked = true;

            var saved = await service.InsertAsync(dto);
            createdTenantRegistryIds.Add(saved.Id);

            saved.Active.Should().BeFalse();
            saved.Locked.Should().BeTrue();
        }

        [Fact]
        public async Task DescriptionIsNeverPersistedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var dto = ValidDto();
            dto.Description = "this has no column to live in";

            var saved = await service.InsertAsync(dto);
            createdTenantRegistryIds.Add(saved.Id);

            saved.Description.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForMissingIdReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            (await service.GetByIdAsync(999999)).Should().BeNull();
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task InsertPublishesCreatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, serviceChangeBus: bus);

            var created = await service.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            var change = bus.Published.Should().ContainSingle().Subject;
            change.Area.Should().Be("TenantRegistry");
            change.Kind.Should().Be(ServiceChangeKind.Created);
            change.EntityId.Should().Be(created.Id);
        }

        [Fact]
        public async Task InsertDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, serviceChangeBus: bus);
            var dto = ValidDto();
            dto.Name = "";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task DeletePublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owner = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var created = await owner.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, serviceChangeBus: bus);
            await service.DeleteAsync(created.Id);

            var change = bus.Published.Should().ContainSingle().Subject;
            change.Area.Should().Be("TenantRegistry");
            change.Kind.Should().Be(ServiceChangeKind.Deleted);
            change.EntityId.Should().Be(created.Id);
        }

        [Fact]
        public async Task ListDoesNotPublishAChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, serviceChangeBus: bus);

            await service.GetAsync();

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ForbiddenOperationDoesNotPublishAChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto()));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task InsertWarnsNotErrorsOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var testLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, testLog);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto()));

            testLog.Entries.Should().Contain(e => e.Level == "WARN");
            testLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithNullUserNameWarnsNotErrorsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var testLog = new TestLog();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => TenantRegistryService.CreateAsync(dbContext, null,
                testLog, localizers,
                new NullServiceChangeBus()));

            testLog.Entries.Should().Contain(e => e.Level == "WARN");
            testLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertWritesOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, auditLog: auditLog);

            var created = await service.InsertAsync(ValidDto());
            createdTenantRegistryIds.Add(created.Id);

            auditLog.Entries.Should().ContainSingle(e => e.Level == "INFO");
        }

        [Fact]
        public async Task ListWritesOneAuditRecordEvenThoughItIsAReadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle(e => e.Level == "INFO");
        }

        [Fact]
        public async Task GetForcedFailureLogsExactlyOneErrorWithExceptionAttachedAsync()
        {
            var dbContext = fx.GetDbContext();
            var testLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, testLog);
            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync());

            testLog.Entries.Should().ContainSingle(e => e.Level == "ERROR" && e.Exception != null);
        }

        [Fact]
        public async Task PermissionDeniedMessageIsLocalisedToFrenchAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");
                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

                var exception = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

                exception.Message.Should()
                    .Be(new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                            NullLoggerFactory.Instance)
                        .Create(typeof(TenantRegistryResources))[TenantRegistryResources.PermissionDenied]);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }

        [Fact]
        public void CatalogueRegistersUniqueTenantRegistryToolNames()
        {
            var tools = ServiceToolCatalogue.All;

            var names = new[]
            {
                "TenantRegistryList", "TenantRegistryGet", "TenantRegistryCreate", "TenantRegistryUpdate",
                "TenantRegistryDelete",
            };

            foreach (var name in names)
            {
                tools.Should().ContainSingle(t => t.Name == name);
            }

            tools.Select(t => t.Name).Should().OnlyHaveUniqueItems();
        }
    }
}