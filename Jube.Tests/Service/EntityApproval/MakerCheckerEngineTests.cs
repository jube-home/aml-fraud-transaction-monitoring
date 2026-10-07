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
using Jube.Service.Exceptions.EntityApproval;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.EntityApproval
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class MakerCheckerEngineTests(ModelScaffoldFixture fixture, DatabaseFixture database)
        : IClassFixture<ModelScaffoldFixture>
    {
        private const EntityApprovalKind XPath = EntityApprovalKind.EntityAnalysisModelRequestXPath;

        private string Maker => database.Seed.UserWithPermissionNoApproval;

        private static string Unique(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..24];

        private async Task RunAsync(Func<ModelScaffold, Task> scenario)
        {
            int tenantRegistryId;
            await using (var dbContext = database.GetDbContext())
            {
                tenantRegistryId = await dbContext.UserInTenant
                    .Where(w => w.User == database.Seed.UserWithPermission)
                    .Select(s => s.TenantRegistryId)
                    .FirstAsync();
            }

            var model = await ModelScaffold.CreateAsync(new ModelScaffoldOptions
            {
                ModelGuid = null,
                TenantRegistryId = tenantRegistryId
            });

            try
            {
                await SyncAsync(model);
                await scenario(model);
            }
            finally
            {
                await ForgetApprovalsAsync(model);
                await model.DisposeAsync();
            }
        }

        private async Task ForgetApprovalsAsync(ModelScaffold model)
        {
            await using var dbContext = database.GetDbContext();
            var xPathIds = await dbContext.EntityAnalysisModelRequestXpath
                .Where(w => w.EntityAnalysisModelId == model.ModelId).Select(s => s.Id).ToListAsync();
            var listIds = await dbContext.EntityAnalysisModelList
                .Where(w => w.EntityAnalysisModelGuid == model.ModelGuid).Select(s => s.Id).ToListAsync();
            var valueIds = await dbContext.EntityAnalysisModelListValue
                .Where(w => listIds.Contains(w.EntityAnalysisModelListId ?? 0)).Select(s => s.Id).ToListAsync();

            await DeleteApprovalsAsync(dbContext, XPath, xPathIds);
            await DeleteApprovalsAsync(dbContext, EntityApprovalKind.EntityAnalysisModelList, listIds);
            await DeleteApprovalsAsync(dbContext, EntityApprovalKind.EntityAnalysisModelListValue, valueIds);
        }

        private static Task DeleteApprovalsAsync(DbContext dbContext, EntityApprovalKind kind, List<int> ids)
        {
            var kindId = (int)kind;
            return dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId && ids.Contains(w.EntityId ?? 0))
                .DeleteAsync();
        }

        private Task<EntityApprovalService> CheckerServiceAsync(DbContext dbContext) =>
            EntityApprovalService.CreateAsync(dbContext, database.Seed.UserWithPermission, TestLog.NoOp, 1);

        private async Task CheckerApprovesAsync(EntityApprovalKind kind, int entityId, int version)
        {
            await using var dbContext = database.GetDbContext();
            var service = await CheckerServiceAsync(dbContext);
            await service.ApproveAsync(kind, entityId, version);
        }

        private async Task CheckerRejectsAsync(EntityApprovalKind kind, int entityId, int version)
        {
            await using var dbContext = database.GetDbContext();
            var service = await CheckerServiceAsync(dbContext);
            await service.RejectAsync(kind, entityId, version, "Not this time");
        }

        private async Task<EntityApprovalStatus> StatusAsync(EntityApprovalKind kind, int entityId)
        {
            await using var dbContext = database.GetDbContext();
            var service = await CheckerServiceAsync(dbContext);
            return await service.StatusAsync(kind, entityId);
        }

        private async Task<int> FirstXPathIdAsync(ModelScaffold model)
        {
            await using var dbContext = database.GetDbContext();
            return await dbContext.EntityAnalysisModelRequestXpath
                .Where(w => w.EntityAnalysisModelId == model.ModelId && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id)
                .Select(s => s.Id)
                .FirstAsync();
        }

        private async Task<string> XPathNameAsync(int id)
        {
            await using var dbContext = database.GetDbContext();
            return await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.Id == id).Select(s => s.Name)
                .FirstAsync();
        }

        private async Task<int> VersionOfAsync(int id)
        {
            await using var dbContext = database.GetDbContext();
            return await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.Id == id)
                .Select(s => s.Version ?? 1)
                .FirstAsync();
        }

        private async Task<int> MakerInsertXPathAsync(ModelScaffold model, string name)
        {
            await using var dbContext = database.GetDbContext();
            var inserted = await new EntityAnalysisModelRequestXpathRepository(dbContext, Maker).InsertAsync(
                new EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = model.ModelId,
                    Name = name,
                    XPath = "$.TxnId",
                    DataTypeId = 1,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });
            return inserted.Id;
        }

        private async Task MakerRenameXPathAsync(int id, string name)
        {
            await using var dbContext = database.GetDbContext();
            var repository = new EntityAnalysisModelRequestXpathRepository(dbContext, Maker);
            var row = await repository.GetByIdAsync(id);
            row.Name = name;
            await repository.UpdateAsync(row);
        }

        private async Task MakerDeleteXPathAsync(int id)
        {
            await using var dbContext = database.GetDbContext();
            await new EntityAnalysisModelRequestXpathRepository(dbContext, Maker).DeleteAsync(id);
        }

        private async Task<(int Id, string Name)> ApprovedNewXPathAsync(ModelScaffold model)
        {
            var name = Unique("ZzRun");
            var id = await MakerInsertXPathAsync(model, name);
            await CheckerApprovesAsync(XPath, id, 1);
            await SyncAsync(model);
            return (id, name);
        }

        private bool EngineRuns(ModelScaffold model, string xPathName) =>
            EngineSnapshot(model).EntityAnalysisModelRequestXPaths.Any(x => x.Name == xPathName);

        private IReadOnlyCollection<string> EngineListValues(ModelScaffold model, string listName) =>
            EngineSnapshot(model).EntityAnalysisModelLists.TryGetValue(listName, out var values)
                ? values
                : Array.Empty<string>();

        private Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ModelSnapshot EngineSnapshot(
            ModelScaffold model) =>
            fixture.Engine.FindActiveModel(model.ModelGuid).Required().Snapshot;

        private async Task<ModelSyncResult> SyncAsync(ModelScaffold model)
        {
            var result = await ModelSync.SyncAsync(fixture.Engine, model);
            result.ErrorRowsAdded.Should()
                .Be(0, "a pending, rejected or refused change is not a synchronisation error");
            return result;
        }

        [Fact]
        public Task SyncWhileChangesArePendingRecordsNoErrorsAndTheEngineLogsNoneAsync() => RunAsync(async model =>
        {
            var (approvedId, approvedName) = await ApprovedNewXPathAsync(model);
            var addedName = Unique("ZzPendAdd");
            await MakerInsertXPathAsync(model, addedName);
            await MakerDeleteXPathAsync(approvedId);
            var firstId = await FirstXPathIdAsync(model);
            await MakerRenameXPathAsync(firstId, Unique("ZzPendEdit"));

            var result = await SyncAsync(model);

            result.SynchronisedDate.Should().NotBeNull();
            EngineRuns(model, addedName).Should().BeFalse();
            EngineRuns(model, approvedName).Should().BeTrue("the pending deletion is not yet applied");
        });

        [Fact]
        public Task AnXPathAddedByAMakerRunsOnlyOnceACheckerApprovesItAsync() => RunAsync(async model =>
        {
            var name = Unique("ZzAdd");
            var id = await MakerInsertXPathAsync(model, name);

            await SyncAsync(model);
            EngineRuns(model, name).Should().BeFalse("an unapproved addition is not run by the engine");

            await CheckerApprovesAsync(XPath, id, 1);
            await SyncAsync(model);
            EngineRuns(model, name).Should().BeTrue("an approved addition is run once the engine synchronises");
        });

        [Fact]
        public Task AnEditToARunningXPathIsNotAppliedUntilACheckerApprovesItAsync() => RunAsync(async model =>
        {
            var id = await FirstXPathIdAsync(model);
            var original = await XPathNameAsync(id);
            var renamed = Unique("ZzEdit");

            await MakerRenameXPathAsync(id, renamed);
            await SyncAsync(model);
            EngineRuns(model, original).Should().BeTrue("the earlier approved version keeps running");
            EngineRuns(model, renamed).Should().BeFalse("the pending edit is not run");

            await CheckerApprovesAsync(XPath, id, await VersionOfAsync(id));
            await SyncAsync(model);
            EngineRuns(model, renamed).Should().BeTrue();
            EngineRuns(model, original).Should().BeFalse("the approved edit replaces the earlier version");
        });

        [Fact]
        public Task ARejectedEditKeepsTheApprovedVersionUntilTheMakerEditsAgainAsync() => RunAsync(async model =>
        {
            var id = await FirstXPathIdAsync(model);
            var original = await XPathNameAsync(id);
            var first = Unique("ZzRej");
            var second = Unique("ZzRe2");

            await MakerRenameXPathAsync(id, first);
            await CheckerRejectsAsync(XPath, id, await VersionOfAsync(id));
            await SyncAsync(model);
            EngineRuns(model, original).Should().BeTrue("a rejected edit is not run");
            EngineRuns(model, first).Should().BeFalse();

            await MakerRenameXPathAsync(id, second);
            await CheckerApprovesAsync(XPath, id, await VersionOfAsync(id));
            await SyncAsync(model);
            EngineRuns(model, second).Should().BeTrue("a fresh edit after a rejection can be approved");
            EngineRuns(model, original).Should().BeFalse();
        });

        [Fact]
        public Task APendingDeletionKeepsTheXPathRunningUntilACheckerApprovesItAsync() => RunAsync(async model =>
        {
            var (id, name) = await ApprovedNewXPathAsync(model);

            await MakerDeleteXPathAsync(id);
            await SyncAsync(model);
            EngineRuns(model, name).Should().BeTrue("a pending deletion does not stop the entity running");

            var status = await StatusAsync(XPath, id);
            status.Deleted.Should().BeTrue();
            status.Pending.Should().BeTrue();

            await CheckerApprovesAsync(XPath, id, status.CurrentVersion);
            await SyncAsync(model);
            EngineRuns(model, name).Should().BeFalse("an approved deletion is finished and the engine drops it");
        });

        [Fact]
        public Task ARejectedDeletionLeavesTheXPathRunningAsync() => RunAsync(async model =>
        {
            var (id, name) = await ApprovedNewXPathAsync(model);

            await MakerDeleteXPathAsync(id);
            var pending = await StatusAsync(XPath, id);
            await CheckerRejectsAsync(XPath, id, pending.CurrentVersion);

            await SyncAsync(model);
            EngineRuns(model, name).Should().BeTrue("a rejected deletion is not applied");

            var rejected = await StatusAsync(XPath, id);
            rejected.Rejected.Should().BeTrue();
            rejected.Pending.Should().BeFalse();
        });

        [Fact]
        public Task AnUpdateToAnApprovedDeletionRevivesTheXPathOnlyOnceApprovedAsync() => RunAsync(async model =>
        {
            var (id, name) = await ApprovedNewXPathAsync(model);

            await MakerDeleteXPathAsync(id);
            var deletion = await StatusAsync(XPath, id);
            await CheckerApprovesAsync(XPath, id, deletion.CurrentVersion);
            await SyncAsync(model);
            EngineRuns(model, name).Should().BeFalse();

            await MakerRenameXPathAsync(id, name);
            var revived = await StatusAsync(XPath, id);
            revived.Deleted.Should().BeFalse("an update revives the row");
            revived.Pending.Should().BeTrue();

            await SyncAsync(model);
            EngineRuns(model, name).Should().BeFalse("a revival is not run until it is approved");

            await CheckerApprovesAsync(XPath, id, revived.CurrentVersion);
            await SyncAsync(model);
            EngineRuns(model, name).Should().BeTrue();
        });

        [Fact]
        public Task ApprovingTheDeletionOfAnXPathARuleUsesIsRefusedAndTheEngineKeepsRunningItAsync() =>
            RunAsync(async model =>
            {
                var id = await FirstXPathIdAsync(model);
                var name = await XPathNameAsync(id);

                await MakerDeleteXPathAsync(id);
                var deletion = await StatusAsync(XPath, id);

                await using (var dbContext = database.GetDbContext())
                {
                    var service = await CheckerServiceAsync(dbContext);
                    var act = () => service.ApproveAsync(XPath, id, deletion.CurrentVersion);
                    await act.Should().ThrowAsync<ApprovalRefusedException>();
                }

                await SyncAsync(model);
                EngineRuns(model, name).Should().BeTrue("a refused deletion leaves the rule that uses it running");
            });

        [Fact]
        public Task ListValuesAddedByAMakerRunOnlyAfterTheCheckerApprovesThemAsync() => RunAsync(async model =>
        {
            var listName = Unique("ZzList");
            int listId;
            await using (var dbContext = database.GetDbContext())
            {
                var list = await new EntityAnalysisModelListRepository(dbContext, Maker).InsertAsync(
                    new Jube.Data.Poco.EntityAnalysisModelList
                    {
                        EntityAnalysisModelGuid = model.ModelGuid,
                        Name = listName,
                        Active = 1,
                        Locked = 0,
                        Deleted = 0
                    });
                listId = list.Id;
            }

            await CheckerApprovesAsync(EntityApprovalKind.EntityAnalysisModelList, listId, 1);

            var value = Unique("ZzVal");
            await using (var dbContext = database.GetDbContext())
            {
                await new EntityAnalysisModelListValueRepository(dbContext, Maker).InsertAsync(
                    new Jube.Data.Poco.EntityAnalysisModelListValue
                    {
                        EntityAnalysisModelListId = listId,
                        ListValue = value,
                        Deleted = 0
                    });
            }

            await SyncAsync(model);
            EngineListValues(model, listName).Should().NotContain(value);

            await using (var dbContext = database.GetDbContext())
            {
                var service = await CheckerServiceAsync(dbContext);
                var result = await service.ApproveValuesAsync(EntityApprovalKind.EntityAnalysisModelList, listId);
                result.Approved.Should().BeGreaterThan(0);
            }

            await SyncAsync(model);
            EngineListValues(model, listName).Should().Contain(value);
        });
        [Fact]
        public async Task ALandlordsApprovedEditToTheModelsOwnEnableLogsFlagIsAppliedByTheEngineAsync()
        {
            int landlordTenantId;
            await using (var seedContext = database.GetDbContext())
            {
                landlordTenantId = await seedContext.UserInTenant
                    .Where(w => w.User == database.Seed.LandlordUser)
                    .Select(s => s.TenantRegistryId)
                    .FirstAsync();
            }

            var model = await ModelScaffold.CreateAsync(new ModelScaffoldOptions
            {
                ModelGuid = null,
                TenantRegistryId = landlordTenantId
            });

            try
            {
                await ModelSync.SyncAsync(fixture.Engine, model);

                bool originalEnableLogs;
                await using (var dbContext = database.GetDbContext())
                {
                    var repository = new EntityAnalysisModelRepository(dbContext, database.Seed.LandlordUser);
                    var row = await repository.GetByIdAsync(model.ModelId);
                    originalEnableLogs = row.EnableLogs == 1;
                    row.EnableLogs = (byte)(originalEnableLogs ? 0 : 1);
                    await repository.UpdateAsync(row);
                }

                await ModelSync.SyncAsync(fixture.Engine, model);
                fixture.Engine.FindActiveModel(model.ModelGuid)!.Flags.EnableLogs.Should().Be(originalEnableLogs,
                    "the edit is pending and has not been approved yet");

                int editedVersion;
                await using (var dbContext = database.GetDbContext())
                {
                    editedVersion = await dbContext.EntityAnalysisModel.Where(w => w.Id == model.ModelId)
                        .Select(s => s.Version ?? 1).FirstAsync();
                }

                await using (var dbContext = database.GetDbContext())
                {
                    var service = await EntityApprovalService.CreateAsync(dbContext, database.Seed.LandlordUser,
                        TestLog.NoOp, 1);
                    await service.ApproveAsync(EntityApprovalKind.EntityAnalysisModel, model.ModelId, editedVersion);
                }

                await ModelSync.SyncAsync(fixture.Engine, model);
                fixture.Engine.FindActiveModel(model.ModelGuid)!.Flags.EnableLogs.Should().Be(!originalEnableLogs,
                    "a landlord's own approval of the model's own field should reach the running engine");
            }
            finally
            {
                await using var dbContext = database.GetDbContext();
                await dbContext.EntityApproval
                    .Where(w => w.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModel
                                && w.EntityId == model.ModelId).DeleteAsync();
                await model.DisposeAsync();
            }
        }
    }
}