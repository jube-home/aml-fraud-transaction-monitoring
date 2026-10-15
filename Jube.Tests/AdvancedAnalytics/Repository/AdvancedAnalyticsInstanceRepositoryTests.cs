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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Npgsql;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsInstanceRepositoryTests(DatabaseFixture fx)
        : AdvancedAnalyticsRepositoryTestBase(fx)
    {
        private AdvancedAnalyticsInstanceRepository ForTenant(int tenant, string user = "ZzTestUser")
        {
            return new AdvancedAnalyticsInstanceRepository(Db, tenant, user);
        }

        [Fact]
        public async Task InsertStampsGuidTenantAuditAndReadsBackByIdAndGuidAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var inserted = await NewInstanceAsync(TenantA, modelId);

            inserted.Id.Should().BeGreaterThan(0);
            inserted.Guid.Should().NotBeEmpty();
            inserted.TenantRegistryId.Should().Be(TenantA);
            inserted.CreatedUser.Should().Be("ZzTestUser");
            inserted.Version.Should().Be(1);
            inserted.Deleted.Should().Be(0);

            var byId = await ForTenant(TenantA).GetByIdAsync(inserted.Id);
            var byGuid = await ForTenant(TenantA).GetByGuidAsync(inserted.Guid);

            byId.Should().NotBeNull();
            byId!.Name.Should().Be(inserted.Name);
            byId.EntityAnalysisModelId.Should().Be(modelId);
            byGuid!.Id.Should().Be(inserted.Id);
        }

        [Fact]
        public async Task ForeignTenantSeesNotFoundOnEveryReadAsync()
        {
            var inserted = await NewInstanceAsync(TenantA);

            (await ForTenant(TenantB).GetByIdAsync(inserted.Id)).Should().BeNull();
            (await ForTenant(TenantB).GetByGuidAsync(inserted.Guid)).Should().BeNull();
            (await ForTenant(TenantB).ListAsync()).Should().NotContain(c => c.Id == inserted.Id);
        }

        [Fact]
        public async Task ForeignTenantCannotUpdateOrDeleteAsync()
        {
            var inserted = await NewInstanceAsync(TenantA);

            var update = () => ForTenant(TenantB).UpdateAsync(new AdvancedAnalyticsInstance
            {
                Id = inserted.Id,
                Name = "ZzTestHijack"
            });

            var delete = () => ForTenant(TenantB).DeleteAsync(inserted.Id);

            await update.Should().ThrowAsync<KeyNotFoundException>();
            await delete.Should().ThrowAsync<KeyNotFoundException>();
            (await ForTenant(TenantA).GetByIdAsync(inserted.Id))!.Name.Should().Be(inserted.Name);
        }

        [Fact]
        public async Task InsertAgainstAForeignTenantsModelIsNotFoundAsync()
        {
            var foreignModel = await NewEntityAnalysisModelAsync(TenantB);

            var insert = () => ForTenant(TenantA).InsertAsync(new AdvancedAnalyticsInstance
            {
                Name = UniqueName("Foreign"),
                EntityAnalysisModelId = foreignModel
            });

            await insert.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ListFiltersByModelAndIsTenantScopedAsync()
        {
            var modelOne = await NewEntityAnalysisModelAsync(TenantA);
            var modelTwo = await NewEntityAnalysisModelAsync(TenantA);
            var one = await NewInstanceAsync(TenantA, modelOne);
            var two = await NewInstanceAsync(TenantA, modelTwo);
            var other = await NewInstanceAsync(TenantB);

            var all = (await ForTenant(TenantA).ListAsync()).Select(s => s.Id).ToList();
            var byModel = (await ForTenant(TenantA).ListAsync(modelOne)).Select(s => s.Id).ToList();

            all.Should().Contain([one.Id, two.Id]);
            all.Should().NotContain(other.Id);
            byModel.Should().Equal(one.Id);
        }

        [Fact]
        public async Task UpdateChangesFieldsAndIncrementsVersionAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var inserted = await NewInstanceAsync(TenantA);

            var updated = await ForTenant(TenantA, "ZzTestEditor").UpdateAsync(new AdvancedAnalyticsInstance
            {
                Id = inserted.Id,
                Name = UniqueName("Renamed"),
                EntityAnalysisModelId = modelId,
                Active = 1,
                ResponsePayload = 1,
                ReportTable = 1
            });

            updated.EntityAnalysisModelId.Should().Be(modelId);
            updated.Active.Should().Be(1);
            updated.ResponsePayload.Should().Be(1);
            updated.ReportTable.Should().Be(1);
            updated.Version.Should().Be(2);
            updated.UpdatedDate.Should().NotBeNull();
            updated.Guid.Should().Be(inserted.Guid);
        }

        [Fact]
        public async Task UpdateToAForeignTenantsModelIsNotFoundAsync()
        {
            var inserted = await NewInstanceAsync(TenantA);
            var foreignModel = await NewEntityAnalysisModelAsync(TenantB);

            var update = () => ForTenant(TenantA).UpdateAsync(new AdvancedAnalyticsInstance
            {
                Id = inserted.Id,
                Name = inserted.Name,
                EntityAnalysisModelId = foreignModel
            });

            await update.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task SoftDeleteHidesTheRowKeepsItInTheTableAndDeactivatesAsync()
        {
            var inserted = await NewInstanceAsync(TenantA);

            await ForTenant(TenantA).UpdateAsync(new AdvancedAnalyticsInstance
                { Id = inserted.Id, Name = inserted.Name, Active = 1 });

            await ForTenant(TenantA, "ZzTestDeleter").DeleteAsync(inserted.Id);

            (await ForTenant(TenantA).GetByIdAsync(inserted.Id)).Should().BeNull();
            var raw = Db.AdvancedAnalyticsInstance.Single(s => s.Id == inserted.Id);
            raw.Deleted.Should().Be(1);
            raw.Active.Should().Be(0);
            raw.DeletedUser.Should().Be("ZzTestDeleter");
            raw.DeletedDate.Should().NotBeNull();

            var again = () => ForTenant(TenantA).DeleteAsync(inserted.Id);
            await again.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task DeleteWritesAPreDeleteAndAPostDeleteVersionSnapshotAsync()
        {
            var inserted = await NewInstanceAsync(TenantA);

            await ForTenant(TenantA, "ZzTestDeleter").DeleteAsync(inserted.Id);

            var snapshots = await Db.GetTable<AdvancedAnalyticsInstanceVersion>()
                .Where(w => w.AdvancedAnalyticsInstanceId == inserted.Id)
                .OrderBy(o => o.Id).ToListAsync();

            snapshots.Should().HaveCount(2);
            snapshots[0].Deleted.Should().Be(0, "the first snapshot is the row as it looked before the delete");
            snapshots[0].Version.Should().Be(inserted.Version);
            snapshots[1].Deleted.Should().Be(1, "the second snapshot mirrors the row as it looks after the delete");
            snapshots[1].Version.Should().Be(inserted.Version + 1);
            snapshots[1].DeletedUser.Should().Be("ZzTestDeleter");
        }

        [Fact]
        public async Task NamesAreUniquePerTenantIgnoringCaseAmongLiveRowsOnlyAsync()
        {
            var name = UniqueName("Unique");
            var first = await NewInstanceAsync(TenantA, name: name);

            var duplicate = () => ForTenant(TenantA).InsertAsync(new AdvancedAnalyticsInstance
                { Name = name.ToUpperInvariant() });

            var violation = await duplicate.Should().ThrowAsync<PostgresException>();
            violation.Which.SqlState.Should().Be("23505");

            var otherTenant = await NewInstanceAsync(TenantB, name: name);
            otherTenant.Name.Should().Be(name);

            await ForTenant(TenantA).DeleteAsync(first.Id);
            var reused = await NewInstanceAsync(TenantA, name: name);
            reused.Id.Should().NotBe(first.Id);
        }

        [Fact]
        public async Task CancelledTokenStopsEveryOperationAsync()
        {
            var inserted = await NewInstanceAsync(TenantA);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var token = cts.Token;
            var repository = ForTenant(TenantA);

            await ((Func<Task>)(() => repository.GetByIdAsync(inserted.Id, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => repository.ListAsync(token: token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => repository.InsertAsync(new AdvancedAnalyticsInstance { Name = "ZzTestCancel" },
                    token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => repository.DeleteAsync(inserted.Id, token)))
                .Should().ThrowAsync<OperationCanceledException>();
        }
    }
}