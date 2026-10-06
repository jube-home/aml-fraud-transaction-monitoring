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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.EntityApproval;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.EntityApproval
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ApprovalNoteAndHistoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string Maker = "ZzMaker";
        private const EntityApprovalKind XPath = EntityApprovalKind.EntityAnalysisModelRequestXPath;
        private readonly List<int> xPathIds = [];
        private readonly List<int> modelIds = [];
        private int tenantRegistryId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            tenantRegistryId = await dbContext.UserInTenant
                .Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(s => s.TenantRegistryId)
                .FirstAsync();
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.EntityApproval.Where(w => xPathIds.Contains(w.EntityId ?? 0)
                                                      && w.EntityApprovalKindId == (int)XPath).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                .Where(w => xPathIds.Contains(w.EntityAnalysisModelRequestXpathId!.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => xPathIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = new Jube.Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Note{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model);
            modelIds.Add(model.Id);
            return model.Id;
        }

        private async Task<int> SeedRequestXPathAsync(int modelId)
        {
            await using var dbContext = fx.GetDbContext();
            var name = $"ZzNote{Guid.NewGuid():N}"[..20];
            var id = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId,
                Name = name,
                XPath = $"$.{name}",
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Maker
            });
            xPathIds.Add(id);
            return id;
        }

        private Task<EntityApprovalService> CheckerAsync(DbContext dbContext) =>
            EntityApprovalService.CreateAsync(dbContext, fx.Seed.UserWithPermission, TestLog.NoOp, 1);

        [Fact]
        public async Task AnApprovalNoteIsRecordedAndShownInTheHistoryAsync()
        {
            var id = await SeedRequestXPathAsync(await SeedModelAsync());

            await using var dbContext = fx.GetDbContext();
            var service = await CheckerAsync(dbContext);
            await service.ApproveAsync(XPath, id, 1, "Checked against the policy");

            var history = await service.HistoryAsync(XPath, id);

            var row = history.Should().ContainSingle().Subject;
            row.StateId.Should().Be((int)EntityApprovalState.Approved);
            row.EntityVersion.Should().Be(1);
            row.Note.Should().Be("Checked against the policy");
        }

        [Fact]
        public async Task AnApprovalWithoutANoteStoresNoNoteAsync()
        {
            var id = await SeedRequestXPathAsync(await SeedModelAsync());

            await using var dbContext = fx.GetDbContext();
            var service = await CheckerAsync(dbContext);
            await service.ApproveAsync(XPath, id, 1);

            var history = await service.HistoryAsync(XPath, id);

            history.Should().ContainSingle().Which.Note.Should().BeNull();
        }

        [Fact]
        public async Task ARejectionNoteIsRecordedAgainstTheVersionItRejectedAsync()
        {
            var id = await SeedRequestXPathAsync(await SeedModelAsync());

            await using var dbContext = fx.GetDbContext();
            var service = await CheckerAsync(dbContext);
            await service.RejectAsync(XPath, id, 1, "Threshold is wrong");

            var history = await service.HistoryAsync(XPath, id);

            var row = history.Should().ContainSingle().Subject;
            row.StateId.Should().Be((int)EntityApprovalState.Rejected);
            row.EntityVersion.Should().Be(1);
            row.Note.Should().Be("Threshold is wrong");
        }

        [Fact]
        public async Task APendingDeletionIsListedAsDeletedAndDropsOutOnceApprovedAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId);

            await using (var seed = fx.GetDbContext())
            {
                await seed.EntityAnalysisModelRequestXpath.Where(w => w.Id == id)
                    .Set(s => s.Deleted, (byte)1)
                    .Set(s => s.DeletedDate, DateTime.UtcNow)
                    .Set(s => s.DeletedUser, Maker)
                    .Set(s => s.Version, 2)
                    .UpdateAsync();
            }

            await using var dbContext = fx.GetDbContext();
            var service = await CheckerAsync(dbContext);

            var pending = await service.PendingForModelAsync(modelId);
            var deletion = pending.Should().ContainSingle(p => p.EntityId == id).Subject;
            deletion.Deleted.Should().BeTrue();
            deletion.Version.Should().Be(2);

            await service.ApproveAsync(XPath, id, 2, "Gone for good");

            var afterwards = await service.PendingForModelAsync(modelId);
            afterwards.Should().NotContain(p => p.EntityId == id);
            var status = await service.StatusAsync(XPath, id);
            status.Deleted.Should().BeTrue();
            status.Pending.Should().BeFalse();
        }
    }
}