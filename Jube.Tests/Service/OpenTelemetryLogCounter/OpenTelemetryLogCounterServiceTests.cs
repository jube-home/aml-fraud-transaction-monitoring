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
using Jube.Dto.OpenTelemetryLogCounter;
using Jube.Service.Exceptions.OpenTelemetryLogCounter;
using Jube.Service.OpenTelemetryLogCounter;
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

namespace Jube.Test.Service.OpenTelemetryLogCounter
{
    using OpenTelemetryLogCounterService = OpenTelemetryLogCounterService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class OpenTelemetryLogCounterServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.OpenTelemetryLogCounter.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<OpenTelemetryLogCounterService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return OpenTelemetryLogCounterService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private static string UniqueName(string label)
        {
            return $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}";
        }

        private static OpenTelemetryLogCounterDto NewDto(string name, string regex = @"deadlock")
        {
            return new OpenTelemetryLogCounterDto
            {
                Name = name,
                Regex = regex,
                Active = true
            };
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(UniqueName("Insert")));
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAndGetByIdReturnsItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var name = UniqueName("List");
            var saved = await service.InsertAsync(NewDto(name));
            createdIds.Add(saved.Id);

            var all = await service.ListAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.Name == name);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().NotBeNull();
            byId!.Regex.Should().Be(@"deadlock");
        }

        [Fact]
        public async Task UpdateChangesFieldsAndIncrementsVersionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(UniqueName("Update")));
            createdIds.Add(saved.Id);

            var newName = UniqueName("Updated");
            var updateDto = new OpenTelemetryLogCounterDto
            {
                Id = saved.Id,
                Name = newName,
                Regex = "timeout",
                Active = false
            };

            var updated = await service.UpdateAsync(updateDto);

            updated.Name.Should().Be(newName);
            updated.Regex.Should().Be("timeout");
            updated.Active.Should().Be(0);
            updated.Version.Should().Be(2);
            updated.UpdatedUser.Should().Be(fx.Seed.UserWithPermission);
        }

        [Fact]
        public async Task DeleteSoftDeletesRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(UniqueName("Delete")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().BeNull();
        }

        [Fact]
        public async Task DeleteOnMissingIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.DeleteAsync(int.MaxValue);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateOnLockedRowThrowsLockedExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(UniqueName("Locked"));
            dto.Locked = true;
            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            var act = () => service.UpdateAsync(new OpenTelemetryLogCounterDto
            {
                Id = saved.Id,
                Name = UniqueName("LockedUpdated"),
                Regex = "timeout",
                Active = false
            });

            await act.Should().ThrowAsync<LockedException>();
        }

        [Fact]
        public async Task DeleteOnLockedRowThrowsLockedExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(UniqueName("LockedDelete"));
            dto.Locked = true;
            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            var act = () => service.DeleteAsync(saved.Id);

            await act.Should().ThrowAsync<LockedException>();
        }

        [Fact]
        public async Task InsertWithEmptyNameIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(NewDto(string.Empty));

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithInvalidRegexIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(NewDto(UniqueName("BadRegex"), "("));

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public Task InsertByUnauthenticatedUserThrowsAsync()
        {
            var act = () => OpenTelemetryLogCounterService.CreateAsync(null!, null, TestLog.NoOp, localizers,
                new NullServiceChangeBus());

            return act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        [Fact]
        public async Task InsertByUserWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var act = () => service.InsertAsync(NewDto(UniqueName("Forbidden")));

            await act.Should().ThrowAsync<ForbiddenException>();
        }
    }
}