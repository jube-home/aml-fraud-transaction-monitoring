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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(new OpenTelemetryExcludeDto
            {
                Name = UniqueName("Insert"),
                Active = true
            });
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAndGetByIdReturnsItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

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
            updated.UpdatedUser.Should().Be(fx.Seed.UserWithPermission);
        }

        [Fact]
        public async Task DeleteSoftDeletesRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

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
    }
}