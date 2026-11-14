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
using Jube.Dto.Options;
using Jube.Dto.Repository.Erasure;
using Jube.Engine.Erasure;
using Jube.Service.Reactivity;
using Jube.Service.Repository.Erasure;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Repository.Erasure
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ErasureServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly DateTime now = DateTime.UtcNow;
        private readonly CaseRiskSeeder seeder = new(fx);
        private readonly List<Guid> entries = [];
        private readonly List<long> nodes = [];
        private readonly List<int> models = [];
        private readonly string keyName = "ErKey" + Guid.NewGuid().ToString("N")[..8];

        private string Requester => fx.Seed.UserWithPermissionNoApproval;
        private string Approver => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.ErasureRequest.Where(w => w.KeyName == keyName).DeleteAsync();
            await dbContext.GraphEdge.Where(w => nodes.Contains(w.FromNodeId) || nodes.Contains(w.ToNodeId))
                .DeleteAsync();
            await dbContext.GraphNode.Where(w => nodes.Contains(w.Id)).DeleteAsync();
            await dbContext.ArchiveKey.Where(w => entries.Contains(w.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();
            await dbContext.ArchiveTag.Where(w => entries.Contains(w.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();
            await dbContext.Archive.Where(w => entries.Contains(w.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
            await dbContext.EntityAnalysisModel.Where(w => models.Contains(w.Id)).DeleteAsync();
        }

        private static Task<ErasureService> BuildAsync(DbContext dbContext, string user, int minimumDays = 0) =>
            ErasureService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus(), minimumDays);

        private static async Task<int> TenantAsync(DbContext dbContext, string user) =>
            (await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, user))!.Value;

        private async Task<int> ModelAsync(DbContext dbContext, int tenant)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"ZzErasure{Guid.NewGuid():N}"[..30], Guid = Guid.NewGuid(), Active = 1, Locked = 0, Deleted = 0,
                TenantRegistryId = tenant
            });
            models.Add(id);

            return id;
        }

        private async Task<Guid> TransactionAsync(DbContext dbContext, int modelId, string value, int daysOld = 10,
            bool numeric = false)
        {
            var guid = Guid.NewGuid();
            entries.Add(guid);

            await dbContext.InsertAsync(new Data.Poco.Archive
            {
                Json = $"{{\"payload\":{{\"Name\":\"{value}\"}}}}", EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = modelId, CreatedDate = now.AddDays(-daysOld),
                ReferenceDate = now.AddDays(-daysOld)
            });
            await dbContext.InsertAsync(new ArchiveKey
            {
                EntityAnalysisModelInstanceEntryGuid = guid, Key = keyName, ProcessingTypeId = 1,
                KeyValueString = numeric ? null : value, KeyValueLong = numeric ? long.Parse(value) : null
            });
            await dbContext.InsertAsync(new ArchiveTag
            {
                EntityAnalysisModelInstanceEntryGuid = guid, Name = "Fraud", Version = 1, CreatedDate = now
            });

            return guid;
        }

        private async Task<int> CaseForAsync(DbContext dbContext, Guid entry, byte closedStatusId, byte legalHold = 0)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, closedStatusId,
                json: "{\"secret\":1}");
            await dbContext.Case.Where(w => w.Id == caseId)
                .Set(s => s.EntityAnalysisModelInstanceEntryGuid, entry)
                .Set(s => s.LegalHold, legalHold).UpdateAsync();

            return caseId;
        }

        private async Task<ErasureRequestDto> ApprovedAsync(DbContext dbContext, string value)
        {
            var requested = await (await BuildAsync(dbContext, Requester)).RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = value, Reason = "DSR-1" });
            requested.StateId.Should().Be((int)ErasureState.Pending);

            return await (await BuildAsync(dbContext, Approver)).ApproveAsync(
                new ErasureDecisionDto { Guid = requested.Guid });
        }

        private static ErasureSubjectDto Subject(string keyName, string value) =>
            new() { KeyName = keyName, KeyValue = value };

        [Fact]
        public async Task PreviewCountsTheSubjectsTransactionsAndSeesNoObstacleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await ModelAsync(dbContext, await TenantAsync(dbContext, Requester));
            await TransactionAsync(dbContext, model, "p-1");
            await TransactionAsync(dbContext, model, "p-1");
            await TransactionAsync(dbContext, model, "p-other");

            var preview = await (await BuildAsync(dbContext, Requester)).PreviewAsync(Subject(keyName, "p-1"));

            preview.ArchiveRows.Should().Be(2);
            preview.RefusalReasonId.Should().BeNull();
        }

        [Fact]
        public async Task ApprovedErasureRemovesEverythingAboutTheSubjectAndKeepsOthersAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantAsync(dbContext, Requester);
            var model = await ModelAsync(dbContext, tenant);
            var gone = await TransactionAsync(dbContext, model, "e-1");
            var kept = await TransactionAsync(dbContext, model, "e-other");
            var closedCase = await CaseForAsync(dbContext, gone, 3);

            var nodeId = await dbContext.InsertWithInt64IdentityAsync(new GraphNode
            {
                TenantRegistryId = tenant, Key = keyName, Value = "e-1", FirstSeen = now, LastSeen = now
            });
            var otherNodeId = await dbContext.InsertWithInt64IdentityAsync(new GraphNode
            {
                TenantRegistryId = tenant, Key = keyName, Value = "e-other", FirstSeen = now, LastSeen = now
            });
            nodes.AddRange([nodeId, otherNodeId]);
            await dbContext.InsertWithInt64IdentityAsync(new GraphEdge
            {
                TenantRegistryId = tenant, GraphRegistryEdgeGuid = Guid.NewGuid(), KindId = 1,
                FromNodeId = nodeId, ToNodeId = otherNodeId, Count = 1, FirstSeen = now, LastSeen = now,
                DecayedAt = now
            });

            var approved = await ApprovedAsync(dbContext, "e-1");
            approved.StateId.Should().Be((int)ErasureState.Approved);

            var run = await ErasureExecutor.RunAsync(dbContext, 0, now);
            run.Executed.Should().BeGreaterThanOrEqualTo(1);

            (await dbContext.Archive.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == gone)).Should().BeFalse();
            (await dbContext.ArchiveKey.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == gone)).Should().BeFalse();
            (await dbContext.ArchiveTag.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == gone)).Should().BeFalse();
            (await dbContext.GraphNode.AnyAsync(w => w.Id == nodeId)).Should().BeFalse();
            (await dbContext.GraphEdge.AnyAsync(w => w.FromNodeId == nodeId)).Should().BeFalse();
            (await dbContext.GraphNode.AnyAsync(w => w.Id == otherNodeId)).Should().BeTrue();
            (await dbContext.Archive.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == kept)).Should().BeTrue();

            var caseRow = await dbContext.Case.FirstAsync(w => w.Id == closedCase);
            caseRow.Json.Should().BeNull();

            var record = await new ErasureRequestRepository(dbContext).GetAsync(tenant, approved.Guid);
            record.StateId.Should().Be((short)ErasureState.Executed);
            record.ArchiveRowsRemoved.Should().Be(1);
            record.GraphNodesRemoved.Should().Be(1);
            record.GraphEdgesRemoved.Should().Be(1);
            record.CasePayloadsNulled.Should().Be(1);
            record.ExecutedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task ANumericKeyIsMatchedThroughItsIntegerValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await ModelAsync(dbContext, await TenantAsync(dbContext, Requester));
            await TransactionAsync(dbContext, model, "424242", numeric: true);

            var preview = await (await BuildAsync(dbContext, Requester)).PreviewAsync(Subject(keyName, "424242"));

            preview.ArchiveRows.Should().Be(1);
        }

        [Fact]
        public async Task ARequestIsRefusedWhileACaseIsUnderLegalHoldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await ModelAsync(dbContext, await TenantAsync(dbContext, Requester));
            var guid = await TransactionAsync(dbContext, model, "h-1");
            await CaseForAsync(dbContext, guid, 3, 1);

            var result = await (await BuildAsync(dbContext, Requester)).RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = "h-1", Reason = "DSR" });

            result.StateId.Should().Be((int)ErasureState.Refused);
            result.RefusalReasonId.Should().Be((int)ErasureRefusalReason.LegalHold);
            (await dbContext.Archive.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == guid)).Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public async Task ARequestIsRefusedWhileACaseIsOpenOrSuspendedAsync(byte closedStatusId)
        {
            await using var dbContext = fx.GetDbContext();
            var model = await ModelAsync(dbContext, await TenantAsync(dbContext, Requester));
            var guid = await TransactionAsync(dbContext, model, "o-1");
            await CaseForAsync(dbContext, guid, closedStatusId);

            var result = await (await BuildAsync(dbContext, Requester)).RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = "o-1", Reason = "DSR" });

            result.RefusalReasonId.Should().Be((int)ErasureRefusalReason.OpenCase);
        }

        [Fact]
        public async Task ARequestIsRefusedInsideTheRetentionMinimumAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await ModelAsync(dbContext, await TenantAsync(dbContext, Requester));
            await TransactionAsync(dbContext, model, "r-1", 30);

            var inside = await (await BuildAsync(dbContext, Requester, 365)).RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = "r-1", Reason = "DSR" });
            inside.RefusalReasonId.Should().Be((int)ErasureRefusalReason.RetentionMinimum);

            var outside = await (await BuildAsync(dbContext, Requester, 7)).RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = "r-1", Reason = "DSR" });
            outside.StateId.Should().Be((int)ErasureState.Pending);
        }

        [Fact]
        public async Task ASubjectWithNothingArchivedIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var result = await (await BuildAsync(dbContext, Requester)).RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = "nobody", Reason = "DSR" });

            result.RefusalReasonId.Should().Be((int)ErasureRefusalReason.NothingFound);
        }

        [Fact]
        public async Task ALegalHoldPlacedAfterApprovalStopsTheEngineAndIsRecordedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantAsync(dbContext, Requester);
            var model = await ModelAsync(dbContext, tenant);
            var guid = await TransactionAsync(dbContext, model, "late-1");
            var approved = await ApprovedAsync(dbContext, "late-1");
            await CaseForAsync(dbContext, guid, 3, 1);

            await ErasureExecutor.RunAsync(dbContext, 0, now);

            (await dbContext.Archive.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == guid)).Should().BeTrue();
            var record = await new ErasureRequestRepository(dbContext).GetAsync(tenant, approved.Guid);
            record.StateId.Should().Be((short)ErasureState.Refused);
            record.RefusalReasonId.Should().Be((short)ErasureRefusalReason.LegalHold);
        }

        [Fact]
        public async Task TheRequesterCannotApproveOrRejectTheirOwnRequestAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await ModelAsync(dbContext, await TenantAsync(dbContext, Approver));
            await TransactionAsync(dbContext, model, "own-1");
            var service = await BuildAsync(dbContext, Approver);
            var request = await service.RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = "own-1", Reason = "DSR" });

            await FluentActions.Awaiting(() => service.ApproveAsync(new ErasureDecisionDto { Guid = request.Guid }))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
            await FluentActions.Awaiting(() => service.RejectAsync(
                new ErasureDecisionDto { Guid = request.Guid, Note = "no" }))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
        }

        [Fact]
        public async Task OnlyAnApproverCanDecideAndRejectingNeedsAReasonAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await ModelAsync(dbContext, await TenantAsync(dbContext, Requester));
            await TransactionAsync(dbContext, model, "d-1");
            var request = await (await BuildAsync(dbContext, Requester)).RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = "d-1", Reason = "DSR" });

            await FluentActions.Awaiting(async () => await (await BuildAsync(dbContext, Requester))
                    .ApproveAsync(new ErasureDecisionDto { Guid = request.Guid }))
                .Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var approver = await BuildAsync(dbContext, Approver);
            await FluentActions.Awaiting(() => approver.RejectAsync(new ErasureDecisionDto { Guid = request.Guid }))
                .Should().ThrowAsync<ArgumentException>();

            var rejected = await approver.RejectAsync(new ErasureDecisionDto { Guid = request.Guid, Note = "no" });
            rejected.StateId.Should().Be((int)ErasureState.Rejected);

            await FluentActions.Awaiting(() => approver.ApproveAsync(new ErasureDecisionDto { Guid = request.Guid }))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
        }

        [Fact]
        public async Task OnlyTheRequesterCanRetractAndASecondOpenRequestIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await ModelAsync(dbContext, await TenantAsync(dbContext, Requester));
            await TransactionAsync(dbContext, model, "t-1");
            var service = await BuildAsync(dbContext, Requester);
            var create = new ErasureRequestCreateDto { KeyName = keyName, KeyValue = "t-1", Reason = "DSR" };
            var request = await service.RequestAsync(create);

            await FluentActions.Awaiting(() => service.RequestAsync(create))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
            await FluentActions.Awaiting(async () => await (await BuildAsync(dbContext, Approver))
                    .RetractAsync(request.Guid)).Should().ThrowAsync<CaseExceptions.ConflictException>();

            var retracted = await service.RetractAsync(request.Guid);
            retracted.StateId.Should().Be((int)ErasureState.Retracted);
        }

        [Fact]
        public async Task ExecutedRequestsAreNeverRunTwiceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await ModelAsync(dbContext, await TenantAsync(dbContext, Requester));
            await TransactionAsync(dbContext, model, "x-1");
            await ApprovedAsync(dbContext, "x-1");

            await ErasureExecutor.RunAsync(dbContext, 0, now);
            var second = await ErasureExecutor.RunAsync(dbContext, 0, now);

            second.Executed.Should().Be(0);
        }

        [Fact]
        public async Task ValidationRefusesBlankAndOversizedInputAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Requester);

            await FluentActions.Awaiting(() => service.RequestAsync(
                new ErasureRequestCreateDto { KeyName = "", KeyValue = "v", Reason = "r" }))
                .Should().ThrowAsync<ArgumentException>();
            await FluentActions.Awaiting(() => service.RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = new string('v', 501), Reason = "r" }))
                .Should().ThrowAsync<ArgumentException>();
            await FluentActions.Awaiting(() => service.RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = "v", Reason = " " }))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task EveryVerbRefusesAUserWithoutThePermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);

            await FluentActions.Awaiting(() => service.ListAsync()).Should()
                .ThrowAsync<CaseExceptions.ForbiddenException>();
            await FluentActions.Awaiting(() => service.PreviewAsync(Subject(keyName, "a"))).Should()
                .ThrowAsync<CaseExceptions.ForbiddenException>();
            await FluentActions.Awaiting(() => service.ExportAsync(Subject(keyName, "a"))).Should()
                .ThrowAsync<CaseExceptions.ForbiddenException>();
            await FluentActions.Awaiting(() => service.RequestAsync(
                new ErasureRequestCreateDto { KeyName = keyName, KeyValue = "a", Reason = "r" })).Should()
                .ThrowAsync<CaseExceptions.ForbiddenException>();
        }

        [Fact]
        public async Task AnotherTenantCannotSeePreviewOrExportTheSubjectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await ModelAsync(dbContext, await TenantAsync(dbContext, Requester));
            await TransactionAsync(dbContext, model, "iso-1");

            var other = await BuildAsync(dbContext, fx.Seed.UserTenantB);

            var preview = await other.PreviewAsync(Subject(keyName, "iso-1"));
            preview.ArchiveRows.Should().Be(0);
            (await other.ExportAsync(Subject(keyName, "iso-1"))).Transactions.Should().BeEmpty();
        }

        [Fact]
        public async Task ExportReturnsOnlyTheSubjectsArchivedTransactionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantAsync(dbContext, Requester);
            var model = await ModelAsync(dbContext, tenant);
            var mine = await TransactionAsync(dbContext, model, "x-mine");
            await TransactionAsync(dbContext, model, "x-theirs");
            await CaseForAsync(dbContext, mine, 3);

            var bundle = await (await BuildAsync(dbContext, Requester)).ExportAsync(Subject(keyName, "x-mine"));

            bundle.Transactions.Should().ContainSingle();
            bundle.Transactions[0].EntryGuid.Should().Be(mine);
            bundle.Transactions[0].Json.Should().Contain("x-mine").And.NotContain("x-theirs");
            bundle.Transactions[0].Json.Should().NotContain("secret");
            bundle.GeneratedUser.Should().Be(Requester);
            bundle.Truncated.Should().BeFalse();
        }
    }
}
