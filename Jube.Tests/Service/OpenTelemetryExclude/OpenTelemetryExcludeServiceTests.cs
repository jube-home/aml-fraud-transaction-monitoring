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
using Jube.Dto.OpenTelemetryExclude;
using Jube.Service.Exceptions.OpenTelemetryExclude;
using Jube.Service.OpenTelemetryExclude;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.OpenTelemetryExclude
{
    using OpenTelemetryExcludeService = OpenTelemetryExcludeService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class OpenTelemetryExcludeServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.OpenTelemetryExclude.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<OpenTelemetryExcludeService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return OpenTelemetryExcludeService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private static string UniqueName(string label)
        {
            return $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}";
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var saved = await service.InsertAsync(new OpenTelemetryExcludeDto
            {
                Name = UniqueName("Insert"),
                Active = true
            });
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.LandlordUser);
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAndGetByIdReturnsItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var name = UniqueName("List");
            var saved = await service.InsertAsync(new OpenTelemetryExcludeDto { Name = name, Active = true });
            createdIds.Add(saved.Id);

            var all = await service.ListAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.Name == name);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().NotBeNull();
            byId!.Active.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateChangesFieldsAndIncrementsVersionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var saved = await service.InsertAsync(new OpenTelemetryExcludeDto
            {
                Name = UniqueName("Update"),
                Active = true
            });
            createdIds.Add(saved.Id);

            var newName = UniqueName("Updated");

            var updated = await service.UpdateAsync(new OpenTelemetryExcludeDto
            {
                Id = saved.Id,
                Name = newName,
                Active = false
            });

            updated.Name.Should().Be(newName);
            updated.Active.Should().Be(0);
            updated.Version.Should().Be(2);
            updated.UpdatedUser.Should().Be(fx.Seed.LandlordUser);
        }

        [Fact]
        public async Task DeleteSoftDeletesRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var saved = await service.InsertAsync(new OpenTelemetryExcludeDto
            {
                Name = UniqueName("Delete"),
                Active = true
            });
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().BeNull();
        }

        [Fact]
        public async Task UpdateOnLockedRowThrowsLockedExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var saved = await service.InsertAsync(new OpenTelemetryExcludeDto
            {
                Name = UniqueName("Locked"),
                Active = true,
                Locked = true
            });
            createdIds.Add(saved.Id);

            var act = () => service.UpdateAsync(new OpenTelemetryExcludeDto
            {
                Id = saved.Id,
                Name = UniqueName("LockedUpdated"),
                Active = false
            });

            await act.Should().ThrowAsync<LockedException>();
        }

        [Fact]
        public async Task DeleteOnLockedRowThrowsLockedExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var saved = await service.InsertAsync(new OpenTelemetryExcludeDto
            {
                Name = UniqueName("LockedDelete"),
                Active = true,
                Locked = true
            });
            createdIds.Add(saved.Id);

            var act = () => service.DeleteAsync(saved.Id);

            await act.Should().ThrowAsync<LockedException>();
        }

        [Fact]
        public async Task InsertWithEmptyNameIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var act = () => service.InsertAsync(new OpenTelemetryExcludeDto { Name = string.Empty, Active = true });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertByUserWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.InsertAsync(new OpenTelemetryExcludeDto
            {
                Name = UniqueName("Forbidden"),
                Active = true
            });

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task NonLandlordHoldingEverySpecificationIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new OpenTelemetryExcludeDto
            {
                Name = UniqueName("Forbidden"),
                Active = true
            });

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task InsertWithADuplicateNameIgnoringCaseIsRejectedAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var name = UniqueName("Dup");
            var first = await service.InsertAsync(new OpenTelemetryExcludeDto { Name = name, Active = true });
            createdIds.Add(first.Id);

            var act = () => service.InsertAsync(new OpenTelemetryExcludeDto
                { Name = name.ToUpperInvariant(), Active = true });

            (await act.Should().ThrowAsync<DtoValidationException>())
                .Which.Result.Errors.Should().ContainSingle(e => e.ErrorCode == "NameDuplicate");

            (await dbContext.OpenTelemetryExclude.CountAsync(w => w.Name.ToLower() == name.ToLower())).Should().Be(1);
        }

        [Fact]
        public async Task UpdateKeepingItsOwnNameIsNotADuplicateButRenamingOntoAnotherRowIsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var a = await service.InsertAsync(new OpenTelemetryExcludeDto { Name = UniqueName("OwnA"), Active = false });
            var b = await service.InsertAsync(new OpenTelemetryExcludeDto { Name = UniqueName("OwnB"), Active = false });
            createdIds.Add(a.Id);
            createdIds.Add(b.Id);

            var keep = await service.UpdateAsync(new OpenTelemetryExcludeDto
                { Id = a.Id, Name = a.Name, Active = true, Version = a.Version.GetValueOrDefault() });

            keep.Version.Should().Be(2);

            var act = () => service.UpdateAsync(new OpenTelemetryExcludeDto
                { Id = a.Id, Name = b.Name, Active = true, Version = keep.Version.GetValueOrDefault() });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task ANameCanBeReusedOnceTheRowThatHeldItIsDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var name = UniqueName("Reuse");
            var first = await service.InsertAsync(new OpenTelemetryExcludeDto { Name = name, Active = false });
            createdIds.Add(first.Id);
            await service.DeleteAsync(first.Id);

            var second = await service.InsertAsync(new OpenTelemetryExcludeDto { Name = name, Active = false });
            createdIds.Add(second.Id);

            second.Id.Should().NotBe(first.Id);
        }

        [Theory]
        [InlineData(" padded")]
        [InlineData("padded ")]
        [InlineData("has space")]
        [InlineData("tab\there")]
        [InlineData("line\nbreak")]
        [InlineData("rtl\u202Eoverride")]
        [InlineData("iso\u2066late")]
        [InlineData("zero\u200Bwidth")]
        [InlineData("bom\uFEFFname")]
        [InlineData("sep\u2028arator")]
        [InlineData("wild*card")]
        [InlineData("semi;colon")]
        [InlineData("quote'name")]
        [InlineData("caf\u00E9")]
        public async Task InsertWithAForeignPaddedOrDirectionalCharacterNameFailsValidationAndWritesNoRowAsync(
            string suffix)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var before = await dbContext.OpenTelemetryExclude.CountAsync();

            var act = () => service.InsertAsync(new OpenTelemetryExcludeDto
                { Name = suffix.StartsWith(' ') ? suffix : "jube.test." + suffix, Active = true });

            (await act.Should().ThrowAsync<DtoValidationException>())
                .Which.Result.Errors.Should().Contain(e => e.ErrorCode == "NameInvalidCharacters");

            (await dbContext.OpenTelemetryExclude.CountAsync()).Should().Be(before);
        }

        [Theory]
        [InlineData("jube.engine.logs.warn.count")]
        [InlineData("http.server.request.duration")]
        [InlineData("aspnetcore.authentication.sign_ins")]
        [InlineData("kestrel/active-connections")]
        public async Task OrdinaryInstrumentNamesAreAcceptedAsync(string instrument)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            await dbContext.OpenTelemetryExclude.Where(w => w.Name == instrument).DeleteAsync();

            var saved = await service.InsertAsync(new OpenTelemetryExcludeDto { Name = instrument, Active = false });
            createdIds.Add(saved.Id);

            saved.Name.Should().Be(instrument);
        }

        [Fact]
        public async Task EveryMutationWritesOneAuditLineNamingTheActorAndTheRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var audit = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, auditLog: audit);

            var saved = await service.InsertAsync(new OpenTelemetryExcludeDto
                { Name = UniqueName("Audit"), Active = true });
            createdIds.Add(saved.Id);

            var updated = await service.UpdateAsync(new OpenTelemetryExcludeDto
                { Id = saved.Id, Name = saved.Name, Active = false, Version = saved.Version.GetValueOrDefault() });

            await service.DeleteAsync(updated.Id);

            foreach (var operation in new[] { "Create", "Update", "Delete" })
            {
                audit.Entries.Select(e => e.Message).Should().ContainSingle(m =>
                    m.Contains($"area=OpenTelemetryExclude op={operation} actor={fx.Seed.LandlordUser} ") &&
                    m.Contains($"entityId={saved.Id} ") && m.Contains("outcome=ok "));
            }
        }

        [Fact]
        public async Task ARefusedNonLandlordWritesAForbiddenAuditLineAndNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var audit = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: audit);
            var before = await dbContext.OpenTelemetryExclude.CountAsync();

            var act = () => service.InsertAsync(new OpenTelemetryExcludeDto
                { Name = UniqueName("Refused"), Active = true });

            await act.Should().ThrowAsync<ForbiddenException>();

            audit.Entries.Select(e => e.Message).Should().ContainSingle(m =>
                m.Contains($"actor={fx.Seed.UserWithPermission} ") && m.Contains("outcome=forbidden "));

            (await dbContext.OpenTelemetryExclude.CountAsync()).Should().Be(before);
        }

        [Fact]
        public async Task EveryVerbIsRefusedToANonLandlordHoldingEveryPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var landlord = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var seeded = await landlord.InsertAsync(new OpenTelemetryExcludeDto
                { Name = UniqueName("Verbs"), Active = false });
            createdIds.Add(seeded.Id);
            var tenant = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = new OpenTelemetryExcludeDto { Id = seeded.Id, Name = UniqueName("Hijack"), Active = true, Version = 1 };

            await ((Func<Task>)(() => tenant.ListAsync())).Should().ThrowAsync<ForbiddenException>();
            await ((Func<Task>)(() => tenant.GetByIdAsync(seeded.Id))).Should().ThrowAsync<ForbiddenException>();
            await ((Func<Task>)(() => tenant.InsertAsync(dto))).Should().ThrowAsync<ForbiddenException>();
            await ((Func<Task>)(() => tenant.UpdateAsync(dto))).Should().ThrowAsync<ForbiddenException>();
            await ((Func<Task>)(() => tenant.ValidateAsync(dto))).Should().ThrowAsync<ForbiddenException>();
            await ((Func<Task>)(() => tenant.DeleteAsync(seeded.Id))).Should().ThrowAsync<ForbiddenException>();

            var row = await dbContext.OpenTelemetryExclude.FirstAsync(w => w.Id == seeded.Id);
            row.Name.Should().Be(seeded.Name);
            (row.Deleted ?? 0).Should().Be(0);
        }
    }
}