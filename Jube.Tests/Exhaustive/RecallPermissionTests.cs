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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RecallPermissionTests(ModelScaffoldFixture fixture) : IClassFixture<ModelScaffoldFixture>
    {
        private const int ReadWriteExhaustive = 16;
        private const string Body = "{\"Field0\":1,\"Field1\":1}";

        private static DbContext OpenDb()
        {
            return DataConnectionDbContext.GetResilientDbContextDataConnection(ModelScaffold.ConnectionString,
                TestLog.NoOp);
        }

        private static Task GrantAsync(DbContext db, int roleRegistryId, int permissionSpecificationId)
        {
            return db.InsertWithInt32IdentityAsync(new RoleRegistryPermission
            {
                Guid = Guid.NewGuid(),
                RoleRegistryId = roleRegistryId,
                PermissionSpecificationId = permissionSpecificationId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "RecallPermissionTests"
            });
        }

        private static Task RevokeAsync(DbContext db, int roleRegistryId)
        {
            return db.GetTable<RoleRegistryPermission>().Where(w => w.RoleRegistryId == roleRegistryId).DeleteAsync();
        }

        private async Task<int> RecallStatusAsync(ModelScaffold scaffold, PromotedModelSeed seed, string user)
        {
            ModelSyncBarrier(scaffold);

            var response = await fixture.Api.SendAsync("POST",
                "/api/Invoke/ExhaustiveSearchInstance/" + seed.InstanceGuid, user, Encoding.UTF8.GetBytes(Body),
                "application/json");

            return response.Status;
        }

        private void ModelSyncBarrier(ModelScaffold scaffold)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                var model = fixture.Engine.FindActiveModel(scaffold.ModelGuid);

                if (model is { Snapshot.ExhaustiveModels.Count: > 0 })
                {
                    return;
                }

                Thread.Sleep(50);
            }

            throw new TimeoutException("The seeded Exhaustive model was not synchronised.");
        }

        [Fact]
        public async Task ThePermissionToUseExhaustiveAuthorisesRecallWithoutAModelApiKeyLinkAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();
            await using var db = OpenDb();
            var seed = await PromotedModelSeed.InsertAsync(db, scaffold);

            try
            {
                Assert.Equal(200, await RecallStatusAsync(scaffold, seed, scaffold.UserName));
                Assert.Equal(403, await RecallStatusAsync(scaffold, seed, scaffold.OtherUserName));

                await GrantAsync(db, scaffold.OtherRoleRegistryId, ReadWriteExhaustive);

                Assert.Equal(200, await RecallStatusAsync(scaffold, seed, scaffold.OtherUserName));
            }
            finally
            {
                await RevokeAsync(db, scaffold.OtherRoleRegistryId);
                await seed.DeleteAsync();
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(44)]
        [InlineData(46)]
        public async Task AnyOtherPermissionDoesNotAuthoriseRecallAsync(int permissionSpecificationId)
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();
            await using var db = OpenDb();
            var seed = await PromotedModelSeed.InsertAsync(db, scaffold);

            try
            {
                await GrantAsync(db, scaffold.OtherRoleRegistryId, permissionSpecificationId);

                Assert.Equal(403, await RecallStatusAsync(scaffold, seed, scaffold.OtherUserName));
            }
            finally
            {
                await RevokeAsync(db, scaffold.OtherRoleRegistryId);
                await seed.DeleteAsync();
            }
        }

        [Fact]
        public async Task TheTenantOfTheModelStillBoundsRecallForAHolderOfThePermissionAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();
            await using var foreign = await fixture.CreateIsolatedAsync();
            await using var db = OpenDb();
            var seed = await PromotedModelSeed.InsertAsync(db, scaffold);

            try
            {
                await GrantAsync(db, foreign.RoleRegistryId, ReadWriteExhaustive);
                await GrantAsync(db, foreign.OtherRoleRegistryId, ReadWriteExhaustive);

                Assert.Equal(403, await RecallStatusAsync(scaffold, seed, foreign.UserName));
                Assert.Equal(403, await RecallStatusAsync(scaffold, seed, foreign.OtherUserName));
            }
            finally
            {
                await RevokeAsync(db, foreign.RoleRegistryId);
                await RevokeAsync(db, foreign.OtherRoleRegistryId);
                await seed.DeleteAsync();
            }
        }

        [Fact]
        public async Task ARevokedPermissionStopsAuthorisingRecallAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();
            await using var db = OpenDb();
            var seed = await PromotedModelSeed.InsertAsync(db, scaffold);

            try
            {
                await GrantAsync(db, scaffold.OtherRoleRegistryId, ReadWriteExhaustive);
                Assert.Equal(200, await RecallStatusAsync(scaffold, seed, scaffold.OtherUserName));

                await RevokeAsync(db, scaffold.OtherRoleRegistryId);

                Assert.Equal(403, await RecallStatusAsync(scaffold, seed, scaffold.OtherUserName));
            }
            finally
            {
                await seed.DeleteAsync();
            }
        }
    }
}