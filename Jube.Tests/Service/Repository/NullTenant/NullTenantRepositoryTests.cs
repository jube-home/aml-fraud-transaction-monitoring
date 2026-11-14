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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Repository.NullTenant
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class NullTenantRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly string createdUser = $"{DatabaseFixture.Prefix}NullTenant{Guid.NewGuid():N}"[..40];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityAnalysisModelSynchronisationSchedule.Where(w => w.CreatedUser == createdUser)
                .DeleteAsync();
        }

        [Fact]
        public async Task ARepositoryWithNoTenantListsNoSynchronisationSchedulesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(w => w.TenantRegistryId).FirstAsync();

            await dbContext.InsertAsync(new Jube.Data.Poco.EntityAnalysisModelSynchronisationSchedule
            {
                CreatedDate = DateTime.UtcNow, ScheduleDate = DateTime.UtcNow, TenantRegistryId = tenantId,
                CreatedUser = createdUser
            });
            await dbContext.InsertAsync(new Jube.Data.Poco.EntityAnalysisModelSynchronisationSchedule
            {
                CreatedDate = DateTime.UtcNow, ScheduleDate = DateTime.UtcNow, TenantRegistryId = null,
                CreatedUser = createdUser
            });

            var tenantless = await new EntityAnalysisModelSynchronisationScheduleRepository(dbContext).GetAsync();

            tenantless.Should().BeEmpty();
        }

        [Fact]
        public async Task AUserWithNoTenantMembershipListsNoSynchronisationSchedulesAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.InsertAsync(new Jube.Data.Poco.EntityAnalysisModelSynchronisationSchedule
            {
                CreatedDate = DateTime.UtcNow, ScheduleDate = DateTime.UtcNow, TenantRegistryId = null,
                CreatedUser = createdUser
            });

            var stranger = $"{DatabaseFixture.Prefix}NoSuchUser{Guid.NewGuid():N}";
            var rows = await new EntityAnalysisModelSynchronisationScheduleRepository(dbContext, stranger).GetAsync();

            rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ARepositoryWithNoTenantListsNoCaseWorkflowFormEntriesAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var rows = await new CaseWorkflowFormEntryRepository(dbContext).GetAsync();

            rows.Should().BeEmpty();
        }
    }
}
