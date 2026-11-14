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
using Jube.Data.Poco;
using Jube.Data.Security;
using Jube.Migrations.Branches.ReprocessingPermissions;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.ReprocessingPermissions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.Migrations
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SplitReprocessingPermissionMigrationTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int EditRule = 26;
        private const int TriggerRun = 73;
        private const int StopRun = 74;

        private readonly ReprocessingPermissionUsers users = new(fx);

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await users.DisposeAsync();
        }

        private async Task BackfillAsync()
        {
            await using var db = fx.GetDbContext();
            await db.ExecuteAsync(SplitReprocessingPermission.BuildBackfillSql(TriggerRun));
            await db.ExecuteAsync(SplitReprocessingPermission.BuildBackfillSql(StopRun));
        }

        private async Task<List<RoleRegistryPermission>> GrantsAsync(string userName)
        {
            await using var db = fx.GetDbContext();

            var roleId = await (from u in db.UserRegistry
                join r in db.RoleRegistry on u.RoleRegistryGuid equals r.Guid
                where u.Name == userName
                select r.Id).FirstAsync();

            return await db.RoleRegistryPermission.Where(w => w.RoleRegistryId == roleId).ToListAsync();
        }

        private async Task SetGrantAsync(string userName, int spec, Action<RoleRegistryPermission> change)
        {
            await using var db = fx.GetDbContext();

            var roleId = await (from u in db.UserRegistry
                join r in db.RoleRegistry on u.RoleRegistryGuid equals r.Guid
                where u.Name == userName
                select r.Id).FirstAsync();

            var grant = await db.RoleRegistryPermission.FirstAsync(w =>
                w.RoleRegistryId == roleId && w.PermissionSpecificationId == spec);
            change(grant);
            await db.UpdateAsync(grant);
        }

        private async Task<int[]> EffectiveAsync(string userName)
        {
            await using var db = fx.GetDbContext();
            var resolved = await new PermissionValidation().GetPermissionsAsync(db, userName, TestLog.NoOp);

            return [.. resolved.Permissions.OrderBy(p => p)];
        }

        [Fact]
        public async Task ARoleHoldingTheOriginalPermissionGainsBothNewOnesAndNothingElseAndKeepsItsAbilitiesAsync()
        {
            var user = await users.CreateAsync("Backfill", EditRule, 1);

            await BackfillAsync();

            var grants = await GrantsAsync(user);
            grants.Select(g => g.PermissionSpecificationId).Should()
                .BeEquivalentTo(new int?[] { EditRule, 1, TriggerRun, StopRun });

            foreach (var grant in grants.Where(g => g.PermissionSpecificationId is TriggerRun or StopRun))
            {
                grant.Active.Should().Be(1);
                grant.CreatedUser.Should().Be("Administrator");
                grant.Version.Should().Be(1);
                grant.Guid.Should().NotBe(Guid.Empty);
            }

            (await EffectiveAsync(user)).Should().Equal(1, EditRule, TriggerRun, StopRun);
        }

        [Fact]
        public async Task ARoleWithoutTheOriginalPermissionGainsNothingAsync()
        {
            var user = await users.CreateAsync("NoBackfill", 1, 2);

            await BackfillAsync();

            (await GrantsAsync(user)).Select(g => g.PermissionSpecificationId).Should()
                .BeEquivalentTo(new int?[] { 1, 2 });
        }

        [Fact]
        public async Task ADeletedGrantOfTheOriginalPermissionIsNotCarriedForwardAsync()
        {
            var user = await users.CreateAsync("DeletedBackfill", EditRule);
            await SetGrantAsync(user, EditRule, g => g.Deleted = 1);

            await BackfillAsync();

            (await GrantsAsync(user)).Select(g => g.PermissionSpecificationId).Should()
                .BeEquivalentTo(new int?[] { EditRule });
            (await EffectiveAsync(user)).Should().BeEmpty();
        }

        [Fact]
        public async Task AnInactiveGrantStaysInactiveSoNothingIsWidenedAsync()
        {
            var user = await users.CreateAsync("InactiveBackfill", EditRule);
            await SetGrantAsync(user, EditRule, g => g.Active = 0);

            await BackfillAsync();

            var grants = await GrantsAsync(user);
            grants.Where(g => g.PermissionSpecificationId is TriggerRun or StopRun).Should().HaveCount(2)
                .And.OnlyContain(g => g.Active == 0);
            (await EffectiveAsync(user)).Should().BeEmpty();
        }

        [Fact]
        public async Task RunningTheBackfillTwiceOrOverAnExistingGrantNeverDuplicatesAsync()
        {
            var user = await users.CreateAsync("Twice", EditRule, TriggerRun);

            await BackfillAsync();
            await BackfillAsync();

            var grants = await GrantsAsync(user);
            grants.Count(g => g.PermissionSpecificationId == TriggerRun).Should().Be(1);
            grants.Count(g => g.PermissionSpecificationId == StopRun).Should().Be(1);
            grants.Count(g => g.PermissionSpecificationId == EditRule).Should().Be(1);
        }

        [Fact]
        public async Task EveryTenantsRolesAreBackfilledAndKeepTheirOwnTenantAsync()
        {
            var userA = await users.CreateAsync("TenantA", EditRule);
            var userB = await users.CreateInTenantOfAsync(fx.Seed.UserTenantB, "TenantB", EditRule);

            await BackfillAsync();

            (await EffectiveAsync(userA)).Should().Equal(EditRule, TriggerRun, StopRun);
            (await EffectiveAsync(userB)).Should().Equal(EditRule, TriggerRun, StopRun);
        }

        [Fact]
        public async Task TheAdministratorRoleHoldsAllThreeAfterwardsAsync()
        {
            await BackfillAsync();

            await using var db = fx.GetDbContext();
            var held = await db.RoleRegistryPermission
                .Where(w => w.RoleRegistryId == 1 && (w.Deleted == 0 || w.Deleted == null))
                .Select(s => s.PermissionSpecificationId).ToListAsync();

            held.Should().Contain(EditRule).And.Contain(TriggerRun).And.Contain(StopRun);
        }

        [Fact]
        public async Task TheCatalogueNamesTheThreePermissionsDistinctlyAsync()
        {
            await using var db = fx.GetDbContext();
            var names = await db.GetTable<PermissionSpecification>()
                .Where(w => w.Id == EditRule || w.Id == TriggerRun || w.Id == StopRun)
                .ToDictionaryAsync(k => k.Id, v => v.Name);

            names[EditRule].Should().Be(SplitReprocessingPermission.EditRuleName);
            names[TriggerRun].Should().Be(SplitReprocessingPermission.TriggerName);
            names[StopRun].Should().Be(SplitReprocessingPermission.StopName);
        }
    }
}
