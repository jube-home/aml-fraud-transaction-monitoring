/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.Data.Repository.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.EntityApproval;
using Jube.Service.EntityApproval.Models;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.EntityApproval
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsInstanceMakerCheckerTests(DatabaseFixture database)
    {
        private const EntityApprovalKind Kind = EntityApprovalKind.AdvancedAnalyticsInstance;
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
                await scenario(model);
            }
            finally
            {
                await ForgetAsync(model);
                await model.DisposeAsync();
            }
        }

        private async Task ForgetAsync(ModelScaffold model)
        {
            await using var dbContext = database.GetDbContext();

            var instanceIds = await dbContext.AdvancedAnalyticsInstance
                .Where(w => w.EntityAnalysisModelId == model.ModelId).Select(s => s.Id).ToListAsync();

            await dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == (int)Kind && instanceIds.Contains(w.EntityId ?? 0))
                .DeleteAsync();

            await dbContext.GetTable<AdvancedAnalyticsInstanceVersion>()
                .Where(w => instanceIds.Contains(w.AdvancedAnalyticsInstanceId)).DeleteAsync();

            await dbContext.AdvancedAnalyticsInstance.Where(w => instanceIds.Contains(w.Id)).DeleteAsync();
        }

        private Task<EntityApprovalService> CheckerServiceAsync(DbContext dbContext) =>
            EntityApprovalService.CreateAsync(dbContext, database.Seed.UserWithPermission, TestLog.NoOp, 1);

        private async Task CheckerApprovesAsync(int entityId, int version)
        {
            await using var dbContext = database.GetDbContext();
            var service = await CheckerServiceAsync(dbContext);
            await service.ApproveAsync(Kind, entityId, version);
        }

        private async Task CheckerRejectsAsync(int entityId, int version)
        {
            await using var dbContext = database.GetDbContext();
            var service = await CheckerServiceAsync(dbContext);
            await service.RejectAsync(Kind, entityId, version, "Not this time");
        }

        private async Task<EntityApprovalStatus> StatusAsync(int entityId)
        {
            await using var dbContext = database.GetDbContext();
            var service = await CheckerServiceAsync(dbContext);

            return await service.StatusAsync(Kind, entityId);
        }

        private async Task<AdvancedAnalyticsInstance> MakerInsertInstanceAsync(ModelScaffold model, string name)
        {
            await using var dbContext = database.GetDbContext();
            var repository = new AdvancedAnalyticsInstanceRepository(dbContext, model.TenantRegistryId, Maker);

            return await repository.InsertAsync(new AdvancedAnalyticsInstance
            {
                EntityAnalysisModelId = model.ModelId,
                Name = name,
                Active = 0,
                ResponsePayload = 0,
                ReportTable = 0
            });
        }

        private async Task<AdvancedAnalyticsInstance> MakerGetAsync(ModelScaffold model, int id)
        {
            await using var dbContext = database.GetDbContext();
            var repository = new AdvancedAnalyticsInstanceRepository(dbContext, model.TenantRegistryId, Maker);

            return await repository.GetByIdAsync(id);
        }

        private async Task MakerSetActiveAsync(ModelScaffold model, AdvancedAnalyticsInstance instance, bool active)
        {
            await using var dbContext = database.GetDbContext();
            var repository = new AdvancedAnalyticsInstanceRepository(dbContext, model.TenantRegistryId, Maker);
            instance.Active = (byte)(active ? 1 : 0);
            await repository.UpdateAsync(instance);
        }

        private async Task MakerRenameAsync(ModelScaffold model, AdvancedAnalyticsInstance instance, string name)
        {
            await using var dbContext = database.GetDbContext();
            var repository = new AdvancedAnalyticsInstanceRepository(dbContext, model.TenantRegistryId, Maker);
            instance.Name = name;
            await repository.UpdateAsync(instance);
        }

        private async Task MakerDeleteAsync(ModelScaffold model, int id)
        {
            await using var dbContext = database.GetDbContext();
            var repository = new AdvancedAnalyticsInstanceRepository(dbContext, model.TenantRegistryId, Maker);
            await repository.DeleteAsync(id);
        }

        private async Task<AdvancedAnalyticsInstance> MakerResubmitAsync(ModelScaffold model, int id,
            string? note = null)
        {
            await using var dbContext = database.GetDbContext();
            var repository = new AdvancedAnalyticsInstanceRepository(dbContext, model.TenantRegistryId, Maker);

            return await repository.ResubmitAsync(id, note);
        }

        private async Task<AdvancedAnalyticsInstance> MakerRetractAsync(ModelScaffold model, int id,
            string? note = null)
        {
            await using var dbContext = database.GetDbContext();
            var repository = new AdvancedAnalyticsInstanceRepository(dbContext, model.TenantRegistryId, Maker);

            return await repository.RetractAsync(id, note);
        }

        private async Task<int> VersionOfAsync(int id)
        {
            await using var dbContext = database.GetDbContext();

            return await dbContext.AdvancedAnalyticsInstance.Where(w => w.Id == id).Select(s => s.Version)
                .FirstAsync();
        }

        private async Task<bool> IsApprovedAndIncludedAsync(ModelScaffold model, int id)
        {
            await using var dbContext = database.GetDbContext();
            var repository = new AdvancedAnalyticsInstanceRepository(dbContext, model.TenantRegistryId, Maker);

            var approved = await repository.GetApprovedByEntityAnalysisModelIdOrderByIdAsync(model.ModelId);

            return approved.Any(a => a.Id == id);
        }

        private async Task<byte?> ApprovedActiveAsync(ModelScaffold model, int id)
        {
            await using var dbContext = database.GetDbContext();
            var repository = new AdvancedAnalyticsInstanceRepository(dbContext, model.TenantRegistryId, Maker);

            var approved = await repository.GetApprovedByEntityAnalysisModelIdOrderByIdAsync(model.ModelId);

            return approved.FirstOrDefault(a => a.Id == id)?.Active;
        }

        [Fact]
        public Task ADeletionIsHeldBackFromTheSyncQueryUntilACheckerApprovesItAsync() => RunAsync(async model =>
        {
            var instance = await MakerInsertInstanceAsync(model, Unique("ZzDel"));
            await CheckerApprovesAsync(instance.Id, 1);

            (await IsApprovedAndIncludedAsync(model, instance.Id)).Should()
                .BeTrue("the approved instance is resolved by the sync query");

            await MakerDeleteAsync(model, instance.Id);

            (await IsApprovedAndIncludedAsync(model, instance.Id)).Should()
                .BeTrue("a pending deletion must not remove the instance from the sync query yet");

            var status = await StatusAsync(instance.Id);
            status.Deleted.Should().BeTrue();
            status.Pending.Should().BeTrue();

            await CheckerApprovesAsync(instance.Id, status.CurrentVersion);

            (await IsApprovedAndIncludedAsync(model, instance.Id)).Should()
                .BeFalse("an approved deletion must now be excluded from the sync query");
        });

        [Fact]
        public Task ARejectedDeletionLeavesTheInstanceInTheSyncQueryAsync() => RunAsync(async model =>
        {
            var instance = await MakerInsertInstanceAsync(model, Unique("ZzRejDel"));
            await CheckerApprovesAsync(instance.Id, 1);

            await MakerDeleteAsync(model, instance.Id);
            var pending = await StatusAsync(instance.Id);

            await CheckerRejectsAsync(instance.Id, pending.CurrentVersion);

            (await IsApprovedAndIncludedAsync(model, instance.Id)).Should()
                .BeTrue(
                    "a rejected deletion is not applied, so the instance keeps resolving to its last approved state");

            var rejected = await StatusAsync(instance.Id);
            rejected.Rejected.Should().BeTrue();
            rejected.Pending.Should().BeFalse();

            rejected.Deleted.Should()
                .BeTrue("the current row is still marked deleted; only the sync query falls back to the " +
                        "last approved snapshot, same as every other governed kind");
        });

        [Fact]
        public Task AnActiveFlipIsNotAppliedUntilACheckerApprovesItAsync() => RunAsync(async model =>
        {
            var instance = await MakerInsertInstanceAsync(model, Unique("ZzActive"));
            await CheckerApprovesAsync(instance.Id, 1);

            var fetched = await MakerGetAsync(model, instance.Id);
            await MakerSetActiveAsync(model, fetched, true);

            (await ApprovedActiveAsync(model, instance.Id)).Should()
                .Be(0, "the activation is pending and the sync query must keep returning the last approved state");

            var version = await VersionOfAsync(instance.Id);
            await CheckerApprovesAsync(instance.Id, version);

            (await ApprovedActiveAsync(model, instance.Id)).Should()
                .Be(1, "an approved activation is now what the sync query resolves to");
        });

        [Fact]
        public Task ARejectedActiveFlipKeepsTheInstanceInactiveAsync() => RunAsync(async model =>
        {
            var instance = await MakerInsertInstanceAsync(model, Unique("ZzRejActive"));
            await CheckerApprovesAsync(instance.Id, 1);

            var fetched = await MakerGetAsync(model, instance.Id);
            await MakerSetActiveAsync(model, fetched, true);
            var version = await VersionOfAsync(instance.Id);

            await CheckerRejectsAsync(instance.Id, version);

            (await ApprovedActiveAsync(model, instance.Id)).Should()
                .Be(0, "a rejected activation never reaches the sync query");
        });

        [Fact]
        public Task RenamingAnAlreadyApprovedInstanceNeverEntersAPendingStateAsync() => RunAsync(async model =>
        {
            var instance = await MakerInsertInstanceAsync(model, Unique("ZzName"));
            await CheckerApprovesAsync(instance.Id, 1);
            var approvedVersion = await VersionOfAsync(instance.Id);

            var fetched = await MakerGetAsync(model, instance.Id);
            await MakerRenameAsync(model, fetched, Unique("ZzRenamed"));

            (await VersionOfAsync(instance.Id)).Should()
                .Be(approvedVersion, "a content-only edit is ungated and must not bump the approval-relevant version");

            (await IsApprovedAndIncludedAsync(model, instance.Id)).Should()
                .BeTrue("a content-only edit takes effect immediately with no approval step");
        });

        [Fact]
        public Task TheModelsValueStatusListsAPendingActivationOfItsInstanceAsync() => RunAsync(async model =>
        {
            var instance = await MakerInsertInstanceAsync(model, Unique("ZzBulk"));
            await CheckerApprovesAsync(instance.Id, 1);

            var fetched = await MakerGetAsync(model, instance.Id);
            await MakerSetActiveAsync(model, fetched, true);

            await using var dbContext = database.GetDbContext();
            var service = await CheckerServiceAsync(dbContext);

            var statuses = await service.ValueStatusAsync(EntityApprovalKind.EntityAnalysisModel, model.ModelId);

            statuses.Should().ContainSingle(s => s.EntityId == instance.Id && s.Pending)
                .Which.Pending.Should()
                .BeTrue("the UI's bulk status call, keyed by the owning Model, must find the pending instance " +
                        "the same way it finds pending list and dictionary values");
        });

        [Fact]
        public Task ApprovingAllPendingOnTheModelApprovesThePendingActivationAsync() => RunAsync(async model =>
        {
            var instance = await MakerInsertInstanceAsync(model, Unique("ZzBulkApprove"));
            await CheckerApprovesAsync(instance.Id, 1);

            var fetched = await MakerGetAsync(model, instance.Id);
            await MakerSetActiveAsync(model, fetched, true);

            await using (var dbContext = database.GetDbContext())
            {
                var service = await CheckerServiceAsync(dbContext);
                var result = await service.ApproveValuesAsync(EntityApprovalKind.EntityAnalysisModel, model.ModelId);
                result.Approved.Should().BeGreaterThan(0);
            }

            (await ApprovedActiveAsync(model, instance.Id)).Should()
                .Be(1, "the bulk approve-all-pending action on the owning Model must reach the instance");
        });

        [Fact]
        public Task ResubmittingARejectedDeletionReopensApprovalUnchangedAsync() => RunAsync(async model =>
        {
            var instance = await MakerInsertInstanceAsync(model, Unique("ZzResubmit"));
            await CheckerApprovesAsync(instance.Id, 1);

            await MakerDeleteAsync(model, instance.Id);
            var pending = await StatusAsync(instance.Id);
            await CheckerRejectsAsync(instance.Id, pending.CurrentVersion);

            var resubmitted = await MakerResubmitAsync(model, instance.Id, "Still want this gone");

            resubmitted.Deleted.Should().Be(1, "resubmitting a rejected delete reopens it unchanged, still deleted");
            resubmitted.Version.Should().Be(pending.CurrentVersion + 1);

            var status = await StatusAsync(instance.Id);
            status.Pending.Should().BeTrue("resubmitting opens a fresh approval cycle on the same content");

            (await IsApprovedAndIncludedAsync(model, instance.Id)).Should()
                .BeTrue("the resubmitted deletion is still only pending, so the sync query keeps serving the " +
                        "last approved (non-deleted) snapshot");

            await CheckerApprovesAsync(instance.Id, status.CurrentVersion);

            (await IsApprovedAndIncludedAsync(model, instance.Id)).Should()
                .BeFalse("approving the resubmitted deletion now excludes the instance from the sync query");
        });

        [Fact]
        public Task RetractingARejectedDeletionRestoresTheLastApprovedStateImmediatelyAsync() => RunAsync(async model =>
        {
            var instance = await MakerInsertInstanceAsync(model, Unique("ZzRetract"));
            await CheckerApprovesAsync(instance.Id, 1);

            await MakerDeleteAsync(model, instance.Id);
            var pending = await StatusAsync(instance.Id);
            await CheckerRejectsAsync(instance.Id, pending.CurrentVersion);

            var retracted = await MakerRetractAsync(model, instance.Id, "Agreed, abandoning the delete");

            retracted.Deleted.Should().Be(0, "retracting undoes the delete, restoring the approved baseline");
            retracted.Version.Should().Be(pending.CurrentVersion + 1);

            (await IsApprovedAndIncludedAsync(model, instance.Id)).Should()
                .BeTrue("retracting carries the baseline's approval forward onto the new version, so no " +
                        "further checker action is needed");

            var status = await StatusAsync(instance.Id);
            status.Pending.Should().BeFalse("the carried-forward approval means the retraction is not pending");
            status.Deleted.Should().BeFalse();
        });
    }
}