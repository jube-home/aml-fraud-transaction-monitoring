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
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Dto.Repository.EntityAnalysisModelGraphRecall;
using Jube.Service.Exceptions.EntityApproval;
using Jube.Service.Exceptions.Repository.EntityAnalysisModelGraphRecall;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using EntityAnalysisModelGraphRecallService = Jube.Service.Repository.EntityAnalysisModelGraphRecall.EntityAnalysisModelGraphRecallService;

namespace Jube.Test.Service.Repository.EntityAnalysisModelGraphRecall
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelGraphRecallServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int AuthorPermission = 15;
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);
        private readonly List<int> createdRecallIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<string> createdUserNames = [];
        private readonly List<int> createdXpathIds = [];
        private string authorA = string.Empty;
        private int modelId;
        private int tenantAId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            tenantAId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(w => w.TenantRegistryId).FirstAsync();
            authorA = await SeedActorAsync(dbContext, tenantAId, "RecallAuthorA", [AuthorPermission]);
            var model = await new EntityAnalysisModelRepository(dbContext, authorA).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}GraphRecall{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    Active = 1, Locked = 0, Deleted = 0
                });
            modelId = model.Id;
            createdModelIds.Add(model.Id);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityApproval.Where(w => w.EntityId != null && createdRecallIds.Contains(w.EntityId.Value)
                                                      && w.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModelGraphRecall).DeleteAsync();
            await dbContext.EntityAnalysisModelGraphRecallVersion.Where(w => w.EntityAnalysisModelGraphRecallId != null && createdRecallIds.Contains(w.EntityAnalysisModelGraphRecallId.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelGraphRecall.Where(w => w.EntityAnalysisModelId != null && createdModelIds.Contains(w.EntityAnalysisModelId.Value)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelRequestXpathVersion>()
                .Where(w => w.EntityAnalysisModelRequestXpathId != null && createdXpathIds.Contains(w.EntityAnalysisModelRequestXpathId.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => createdXpathIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
            await CleanupActorsAsync(dbContext);
        }

        [Fact]
        public async Task CreatesUpdatesAndDeletesAnEntityWithAVersionRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var created = await service.InsertAsync(ValidDto("Payer", "AccountId"));
            createdRecallIds.Add(created.Id);
            created.Guid.Should().NotBeEmpty();
            created.ReportTable.Should().BeTrue();
            created.ResponsePayload = true;
            created.ReportTable = false;
            created.NodeKey = "AccountId";
            var updated = await service.UpdateAsync(created);
            updated.Version.Should().Be(2);
            updated.ResponsePayload.Should().BeTrue();
            (await dbContext.EntityAnalysisModelGraphRecallVersion.CountAsync(v => v.EntityAnalysisModelGraphRecallId == created.Id)).Should().Be(1);
            (await service.GetByEntityAnalysisModelIdAsync(modelId)).Should().ContainSingle(e => e.Id == created.Id);
            await service.DeleteAsync(created.Id);
            (await service.GetByIdAsync(created.Id))!.DeletedUser.Should().Be(authorA);
            (await service.GetByEntityAnalysisModelIdAsync(modelId)).Should().NotContain(e => e.Id == created.Id);
        }

        [Fact]
        public async Task ValidatesTheNameTheFieldAndTheCounterpartAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var payee = await service.InsertAsync(ValidDto("Payee", "ToAccountId"));
            createdRecallIds.Add(payee.Id);
            var bad = ValidDto("Pay er", "");
            bad.CounterpartName = bad.Name;
            bad.ResponsePayload = true;
            (await service.ValidateAsync(bad)).Errors.Select(e => e.ErrorCode).Should()
                .Contain(["NameNotIdentifier", "RequestXPathNameNotEmpty", "CounterpartIsSelf", "ReportTableAndResponsePayloadExclusive"]);
            var orphan = ValidDto("Payer", "AccountId");
            orphan.CounterpartName = "NoSuchEntity";
            (await service.ValidateAsync(orphan)).Errors.Select(e => e.ErrorCode).Should().Contain("CounterpartNotFound");
            orphan.CounterpartName = payee.Name;
            (await service.ValidateAsync(orphan)).Errors.Should().BeEmpty();
            var duplicate = ValidDto("Payee", "AccountId");
            duplicate.Name = payee.Name;
            var act = () => service.InsertAsync(duplicate);
            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task ListsTheModelsRequestFieldsWithTheirReportTableFlagAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var flagged = $"{DatabaseFixture.Prefix}Flagged{Guid.NewGuid():N}"[..30];
            var unflagged = $"{DatabaseFixture.Prefix}Plain{Guid.NewGuid():N}"[..30];

            foreach (var (name, report) in new[] { (flagged, (byte)1), (unflagged, (byte)0) })
            {
                createdXpathIds.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = modelId, Name = name, XPath = "$." + name, DataTypeId = 1, Active = 1,
                    Deleted = 0, ReportTable = report, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = authorA,
                    Guid = Guid.NewGuid()
                }));
            }

            var service = await BuildServiceAsync(dbContext, authorA);
            var fields = await service.FieldsAsync(modelId);
            fields.Should().ContainSingle(f => f.Key == flagged).Which.ReportTable.Should().BeTrue();
            fields.Should().ContainSingle(f => f.Key == unflagged).Which.Label.Should().Contain("not flagged");
            var other = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var forbidden = () => other.FieldsAsync(modelId);
            await forbidden.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task DeleteWritesTwoVersionRowsBumpsTheVersionAndRefusesASecondDeleteAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var created = await service.InsertAsync(ValidDto("Deleted", "AccountId"));
            createdRecallIds.Add(created.Id);
            await service.DeleteAsync(created.Id);
            var row = await dbContext.EntityAnalysisModelGraphRecall.FirstAsync(w => w.Id == created.Id);
            row.Deleted.Should().Be(1);
            row.Version.Should().Be(2);
            (await dbContext.EntityAnalysisModelGraphRecallVersion.CountAsync(v => v.EntityAnalysisModelGraphRecallId == created.Id)).Should().Be(2);
            var again = () => service.DeleteAsync(created.Id);
            await again.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task TheApprovedOnlyRepositoryReadHidesAnUnapprovedEntityAndShowsItOnceApprovedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var created = await service.InsertAsync(ValidDto("Pending", "AccountId"));
            createdRecallIds.Add(created.Id);
            var repository = new EntityAnalysisModelGraphRecallRepository(dbContext, tenantAId);
            (await repository.GetByEntityAnalysisModelIdOrderByIdAsync(modelId)).Should().Contain(e => e.Id == created.Id);
            (await repository.GetApprovedByEntityAnalysisModelIdAsync(modelId)).Should().NotContain(e => e.Id == created.Id);
            await EntityApprovalSeed.ApproveAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall, created.Id, 1, tenantAId);
            (await repository.GetApprovedByEntityAnalysisModelIdAsync(modelId)).Should().ContainSingle(e => e.Id == created.Id);
        }

        [Fact]
        public async Task TheApprovedOnlyRepositoryReadServesTheLastApprovedVersionWhileAnEditAwaitsApprovalAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var created = await service.InsertAsync(ValidDto("Edited", "AccountId"));
            createdRecallIds.Add(created.Id);
            await EntityApprovalSeed.ApproveAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall, created.Id, 1, tenantAId);
            created.NodeKey = "ChangedKey";
            await service.UpdateAsync(created);
            var repository = new EntityAnalysisModelGraphRecallRepository(dbContext, tenantAId);
            var approved = (await repository.GetApprovedByEntityAnalysisModelIdAsync(modelId)).Single(e => e.Id == created.Id);
            approved.Version.Should().Be(1);
            approved.NodeKey.Should().BeNull();
        }

        [Fact]
        public async Task ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var saved = await service.InsertAsync(ValidDto("ResubmitPending", "AccountId"));
            createdRecallIds.Add(saved.Id);
            await Assert.ThrowsAsync<ApprovalRefusedException>(() => service.ResubmitAsync(saved.Id));
            (await service.GetByIdAsync(saved.Id))!.Version.Should().Be(1);
        }

        [Fact]
        public async Task ResubmitOfRejectedVersionBumpsVersionArchivesOldAndKeepsContentAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var saved = await service.InsertAsync(ValidDto("ResubmitRejected", "AccountId"));
            createdRecallIds.Add(saved.Id);
            await EntityApprovalSeed.RejectAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall, saved.Id, 1, tenantAId);
            var resubmitted = await service.ResubmitAsync(saved.Id);
            resubmitted.Version.Should().Be(2);
            resubmitted.Name.Should().Be(saved.Name);
            (await dbContext.EntityAnalysisModelGraphRecallVersion.CountAsync(v => v.EntityAnalysisModelGraphRecallId == saved.Id)).Should().Be(1);
        }

        [Fact]
        public async Task ResubmitOfRejectedDeleteKeepsDeletedTrueAndReopensApprovalAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var saved = await service.InsertAsync(ValidDto("ResubmitDelete", "AccountId"));
            createdRecallIds.Add(saved.Id);
            await service.DeleteAsync(saved.Id);
            await EntityApprovalSeed.RejectAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall, saved.Id, 2, tenantAId);
            var resubmitted = await service.ResubmitAsync(saved.Id);
            resubmitted.Version.Should().Be(3);
            resubmitted.DeletedDate.Should().NotBeNull("resubmitting a rejected delete must re-open approval without un-deleting it");
        }

        [Fact]
        public async Task RetractOfNonRejectedVersionThrowsApprovalRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var saved = await service.InsertAsync(ValidDto("RetractPending", "AccountId"));
            createdRecallIds.Add(saved.Id);
            await Assert.ThrowsAsync<ApprovalRefusedException>(() => service.RetractAsync(saved.Id));
        }

        [Fact]
        public async Task RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var saved = await service.InsertAsync(ValidDto("RetractNoBaseline", "AccountId"));
            createdRecallIds.Add(saved.Id);
            await EntityApprovalSeed.RejectAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall, saved.Id, 1, tenantAId);
            await Assert.ThrowsAsync<ApprovalRefusedException>(() => service.RetractAsync(saved.Id));
        }

        [Fact]
        public async Task RetractOfRejectedEditRestoresLastApprovedContentAndStampsTheRejectionRetractedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var saved = await service.InsertAsync(ValidDto("RetractEdit", "AccountId"));
            createdRecallIds.Add(saved.Id);
            await EntityApprovalSeed.ApproveAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall, saved.Id, 1, tenantAId);
            saved.NodeKey = "ChangedKey";
            await service.UpdateAsync(saved);
            await EntityApprovalSeed.RejectAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall, saved.Id, 2, tenantAId);
            var retracted = await service.RetractAsync(saved.Id);
            retracted.Version.Should().Be(3);
            retracted.NodeKey.Should().BeNull("retracting abandons the rejected edit and restores the last approved content");
            retracted.Name.Should().Be(saved.Name);
            var rejectionRow = await dbContext.EntityApproval.FirstAsync(w =>
                w.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModelGraphRecall
                && w.EntityId == saved.Id && w.EntityVersion == 2 && w.StateId == (int)EntityApprovalState.Rejected);
            rejectionRow.ReturnedDate.Should().NotBeNull();
            rejectionRow.ReturnedUser.Should().Be(authorA);
        }

        [Fact]
        public async Task RetractOfRejectedDeleteUndeletesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var saved = await service.InsertAsync(ValidDto("RetractDelete", "AccountId"));
            createdRecallIds.Add(saved.Id);
            await EntityApprovalSeed.ApproveAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall, saved.Id, 1, tenantAId);
            await service.DeleteAsync(saved.Id);
            await EntityApprovalSeed.RejectAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall, saved.Id, 2, tenantAId);
            var retracted = await service.RetractAsync(saved.Id);
            retracted.Version.Should().Be(3);
            retracted.DeletedDate.Should().BeNull("retracting a rejected delete undeletes the record");
            (await service.GetByEntityAnalysisModelIdAsync(modelId)).Should().ContainSingle(e => e.Id == saved.Id);
        }

        [Fact]
        public async Task ResubmitAndRetractAreForbiddenWithoutTheAuthorPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var saved = await service.InsertAsync(ValidDto("Forbidden", "AccountId"));
            createdRecallIds.Add(saved.Id);
            var other = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var resubmit = () => other.ResubmitAsync(saved.Id);
            await resubmit.Should().ThrowAsync<ForbiddenException>();
            var retract = () => other.RetractAsync(saved.Id);
            await retract.Should().ThrowAsync<ForbiddenException>();
        }

        private static Task<EntityAnalysisModelGraphRecallService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return EntityAnalysisModelGraphRecallService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private EntityAnalysisModelGraphRecallDto ValidDto(string name, string field) => new()
        {
            EntityAnalysisModelId = modelId,
            Name = $"{name}{Guid.NewGuid():N}"[..20],
            Active = true,
            Locked = false,
            RequestXPathName = field,
            ReportTable = true,
            ResponsePayload = false
        };

        private async Task<string> SeedActorAsync(DbContext dbContext, int tenantRegistryId, string label,
            int[] specs)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = $"{DatabaseFixture.Prefix}RoleGraph{label}{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleRegistryIds.Add(roleId);

            foreach (var spec in specs)
            {
                await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    PermissionSpecificationId = spec,
                    RoleRegistryId = roleId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                });
            }

            var userName = $"{DatabaseFixture.Prefix}UserGraph{label}{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = userName, TenantRegistryId = tenantRegistryId });
            createdUserNames.Add(userName);

            return userName;
        }

        private async Task CleanupActorsAsync(DbContext dbContext)
        {
            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && createdRoleRegistryIds.Contains(w.RoleRegistryId.Value))
                .DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
        }
    }
}
