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

using Jube.Data.Repository.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Repository.PreservationSnapshot;
using Jube.Preservation.Models;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityApprovalPreservationTests(DatabaseFixture fx)
    {
        private const string CheckerOne = "ZzTestCheckerOne";
        private const string CheckerTwo = "ZzTestCheckerTwo";

        private readonly PreservationHarness harness = new(fx);

        private static Task ApproveAsync(DbContext db, int tenantId, EntityApprovalKind kind, int entityId,
            int version, string by, string? note = null)
        {
            return db.InsertAsync(new EntityApproval
            {
                TenantRegistryId = tenantId,
                EntityApprovalKindId = (int)kind,
                EntityId = entityId,
                EntityVersion = version,
                StateId = (int)EntityApprovalState.Approved,
                Note = note,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = by,
                Guid = Guid.NewGuid()
            });
        }

        private static Task RejectAsync(DbContext db, int tenantId, EntityApprovalKind kind, int entityId,
            int version, string by, string? note = null)
        {
            return db.InsertAsync(new EntityApproval
            {
                TenantRegistryId = tenantId,
                EntityApprovalKindId = (int)kind,
                EntityId = entityId,
                EntityVersion = version,
                StateId = (int)EntityApprovalState.Rejected,
                Note = note,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = by,
                Guid = Guid.NewGuid()
            });
        }

        [Fact]
        public async Task ApprovalHistoryRoundTripsVerbatimAgainstPreservedVersionsAsync()
        {
            var source = await harness.CreateTenantAsync("ApS");
            var target = await harness.CreateTenantAsync("ApT");
            var sourceModel = await harness.CreateModelAsync(source, "Ap");
            await using var db = fx.GetDbContext();
            var seeded = await AdvancedAnalyticsSeed.SeedAsync(db, source.Id, sourceModel, "Ap");

            await ApproveAsync(db, source.Id, EntityApprovalKind.EntityAnalysisModel, sourceModel, 1, CheckerOne,
                "model is sound");

            await ApproveAsync(db, source.Id, EntityApprovalKind.AskJoobyAnalyticsInstance, seeded.Linked.Id, 2,
                CheckerOne, "first live version");

            await RejectAsync(db, source.Id, EntityApprovalKind.AskJoobyAnalyticsInstance, seeded.Linked.Id, 3,
                CheckerTwo, "not this one");

            await ApproveAsync(db, source.Id, EntityApprovalKind.AskJoobyAnalyticsInstance, seeded.Linked.Id,
                seeded.Linked.Version, CheckerTwo, "current state approved");

            var bytes = await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor());
            await harness.ImportAsync(target, bytes);

            var importedModel = await db.EntityAnalysisModel.SingleAsync(w =>
                w.TenantRegistryId == target.Id && (w.Deleted == 0 || w.Deleted == null));

            var importedLinked = await db.AskJoobyAnalyticsInstance.SingleAsync(w =>
                w.TenantRegistryId == target.Id && w.Name == seeded.Linked.Name && w.Deleted == 0);

            var importedFree = await db.AskJoobyAnalyticsInstance.SingleAsync(w =>
                w.TenantRegistryId == target.Id && w.Name == seeded.Free.Name && w.Deleted == 0);

            importedLinked.Version.Should().Be(seeded.Linked.Version,
                "the import preserves the exported version so the carried approvals replay against it");

            var modelHistory = await new EntityApprovalRepository(db, target.Id)
                .GetHistoryAsync(EntityApprovalKind.EntityAnalysisModel, importedModel.Id);

            modelHistory.Should().ContainSingle();
            modelHistory[0].EntityVersion.Should().Be(1);
            modelHistory[0].StateId.Should().Be((int)EntityApprovalState.Approved);
            modelHistory[0].Note.Should().Be("model is sound");
            modelHistory[0].CreatedUser.Should().Be(CheckerOne);
            modelHistory[0].TenantRegistryId.Should().Be(target.Id);

            var linkedHistory = (await new EntityApprovalRepository(db, target.Id)
                    .GetHistoryAsync(EntityApprovalKind.AskJoobyAnalyticsInstance, importedLinked.Id))
                .OrderBy(o => o.Id).ToList();

            linkedHistory.Select(s => (s.EntityVersion, s.StateId, s.Note, s.CreatedUser)).Should().Equal(
                (2, (int)EntityApprovalState.Approved, "first live version", CheckerOne),
                (3, (int)EntityApprovalState.Rejected, "not this one", CheckerTwo),
                (seeded.Linked.Version, (int)EntityApprovalState.Approved, "current state approved", CheckerTwo));

            var freeHistory = await new EntityApprovalRepository(db, target.Id)
                .GetHistoryAsync(EntityApprovalKind.AskJoobyAnalyticsInstance, importedFree.Id);

            freeHistory.Should().BeEmpty("the source never approved the free instance, so neither has the import");

            (await db.EntityApproval.CountAsync(w =>
                    w.TenantRegistryId == target.Id && w.CreatedUser == target.UserName))
                .Should().Be(0, "a file that carries its approval history is replayed, never blanket approved");

            var sourceGuids = await db.EntityApproval.Where(w => w.TenantRegistryId == source.Id)
                .Select(s => s.Guid).ToListAsync();

            var targetGuids = await db.EntityApproval.Where(w => w.TenantRegistryId == target.Id)
                .Select(s => s.Guid).ToListAsync();

            targetGuids.Should().NotIntersectWith(sourceGuids);

            var effective = await new AskJoobyAnalyticsInstanceRepository(db, target.Id, target.UserName)
                .GetApprovedByEntityAnalysisModelIdOrderByIdAsync(importedModel.Id);

            effective.Select(s => s.Id).Should().Equal([importedLinked.Id],
                "the instance approved at its current version is effective and the unapproved one is not");
        }

        [Fact]
        public async Task AnExportWithoutAnyApprovalsImportsEverythingStillAwaitingApprovalAsync()
        {
            var source = await harness.CreateTenantAsync("NaS");
            var target = await harness.CreateTenantAsync("NaT");
            var sourceModel = await harness.CreateModelAsync(source, "Na");
            await using var db = fx.GetDbContext();
            await AdvancedAnalyticsSeed.SeedAsync(db, source.Id, sourceModel, "Na");

            var bytes = await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor());

            var wrapper = PreservationHarness.Decrypt(bytes);
            wrapper.Payload!.EntityApproval.Should().NotBeNull().And.BeEmpty();

            await harness.ImportAsync(target, bytes);

            (await db.EntityApproval.CountAsync(w => w.TenantRegistryId == target.Id)).Should().Be(0,
                "a faithful replay of a tenant without approvals imports a tenant without approvals");
        }

        [Fact]
        public async Task AFileWithoutTheApprovalCollectionIsBlanketApprovedIncludingAnalyticsInstancesAsync()
        {
            var source = await harness.CreateTenantAsync("BlS");
            var target = await harness.CreateTenantAsync("BlT");
            var sourceModel = await harness.CreateModelAsync(source, "Bl");
            await using var db = fx.GetDbContext();
            var seeded = await AdvancedAnalyticsSeed.SeedAsync(db, source.Id, sourceModel, "Bl");

            var bytes = await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor());

            var wrapper = PreservationHarness.Decrypt(bytes);
            wrapper.Payload!.EntityApproval = null;

            await harness.ImportAsync(target, PreservationHarness.Encrypt(wrapper));

            var importedModel = await db.EntityAnalysisModel.SingleAsync(w =>
                w.TenantRegistryId == target.Id && (w.Deleted == 0 || w.Deleted == null));

            var importedLinked = await db.AskJoobyAnalyticsInstance.SingleAsync(w =>
                w.TenantRegistryId == target.Id && w.Name == seeded.Linked.Name && w.Deleted == 0);

            var modelStamp = await db.EntityApproval.SingleAsync(w =>
                w.TenantRegistryId == target.Id
                && w.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModel
                && w.EntityId == importedModel.Id);

            modelStamp.StateId.Should().Be((int)EntityApprovalState.Approved);
            modelStamp.CreatedUser.Should().Be(target.UserName);
            modelStamp.EntityVersion.Should().Be(importedModel.Version);

            var linkedStamp = await db.EntityApproval.SingleAsync(w =>
                w.TenantRegistryId == target.Id
                && w.EntityApprovalKindId == (int)EntityApprovalKind.AskJoobyAnalyticsInstance
                && w.EntityId == importedLinked.Id);

            linkedStamp.StateId.Should().Be((int)EntityApprovalState.Approved);
            linkedStamp.CreatedUser.Should().Be(target.UserName);
            linkedStamp.EntityVersion.Should().Be(importedLinked.Version);

            var importedFree = await db.AskJoobyAnalyticsInstance.SingleAsync(w =>
                w.TenantRegistryId == target.Id && w.Name == seeded.Free.Name && w.Deleted == 0);

            var freeStamp = await db.EntityApproval.SingleAsync(w =>
                w.TenantRegistryId == target.Id
                && w.EntityApprovalKindId == (int)EntityApprovalKind.AskJoobyAnalyticsInstance
                && w.EntityId == importedFree.Id);

            freeStamp.StateId.Should().Be((int)EntityApprovalState.Approved,
                "an instance allocated to no model is still blanket approved by a legacy file");

            freeStamp.CreatedUser.Should().Be(target.UserName);
            freeStamp.EntityVersion.Should().Be(importedFree.Version);
        }

        [Fact]
        public async Task ReimportingIntoTheSameTenantReplacesTheApprovalHistoryInsteadOfAppendingAsync()
        {
            var source = await harness.CreateTenantAsync("ReS");
            var target = await harness.CreateTenantAsync("ReT");
            var sourceModel = await harness.CreateModelAsync(source, "Re");
            await using var db = fx.GetDbContext();
            var seeded = await AdvancedAnalyticsSeed.SeedAsync(db, source.Id, sourceModel, "Re");

            await ApproveAsync(db, source.Id, EntityApprovalKind.EntityAnalysisModel, sourceModel, 1, CheckerOne);

            await ApproveAsync(db, source.Id, EntityApprovalKind.AskJoobyAnalyticsInstance, seeded.Linked.Id,
                seeded.Linked.Version, CheckerTwo);

            var bytes = await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor());
            await harness.ImportAsync(target, bytes);
            await harness.ImportAsync(target, bytes);

            (await db.EntityApproval.CountAsync(w => w.TenantRegistryId == target.Id)).Should().Be(2,
                "an import replaces the tenant, so the approval rows of the generation it replaces go with it");

            var importedModel = await db.EntityAnalysisModel.SingleAsync(w =>
                w.TenantRegistryId == target.Id && (w.Deleted == 0 || w.Deleted == null));

            var modelHistory = await new EntityApprovalRepository(db, target.Id)
                .GetHistoryAsync(EntityApprovalKind.EntityAnalysisModel, importedModel.Id);

            modelHistory.Should().ContainSingle();
            modelHistory[0].CreatedUser.Should().Be(CheckerOne);
        }

        [Fact]
        public async Task ASnapshotAlwaysCarriesUnpromotedExhaustiveTrialsForLosslessRollbackAsync()
        {
            var source = await harness.CreateTenantAsync("SnS");
            var sourceModel = await harness.CreateModelAsync(source, "Sn");
            await using var db = fx.GetDbContext();
            await SeedPromotedAndUnpromotedTrialsAsync(db, source, sourceModel);

            var body = await harness.SnapshotBodyAsync(source, new PreservationSnapshotRequestDto
            {
                Name = "ZzTestLosslessSnapshot"
            });

            var wrapper = JsonConvert.DeserializeObject<Wrapper>(body);

            wrapper!.Payload!.EntityAnalysisModel!.Single().ExhaustiveSearchInstance!.Single()
                .ExhaustiveSearchInstanceTrialInstance.Should().HaveCount(3,
                    "a snapshot is a restore point, so importing it must never lose the background training trials");
        }

        [Fact]
        public async Task ApprovalLookupsChunkTheEntityIdListInsteadOfSendingOneGiantInClauseAsync()
        {
            var tenant = await harness.CreateTenantAsync("ChS");
            await using var db = fx.GetDbContext();
            var repository = new EntityApprovalRepository(db, tenant.Id);

            var count = EntityApprovalRepository.EntityIdChunkSize * 2 + 100;
            var entityIds = Enumerable.Range(1, count).ToList();

            await repository.InsertManyAsync(entityIds.Select(entityId => new EntityApproval
            {
                EntityApprovalKindId = (int)EntityApprovalKind.EntityAnalysisModelTag,
                EntityId = entityId,
                EntityVersion = 1,
                StateId = (int)EntityApprovalState.Approved,
                CreatedUser = CheckerOne,
                TenantRegistryId = tenant.Id
            }));

            var approvals = await repository.GetByKindAndEntityIdsAsync(
                EntityApprovalKind.EntityAnalysisModelTag, entityIds);

            approvals.Should().HaveCount(count);
            approvals.Should().BeInAscendingOrder(o => o.Id);

            var rows = await repository.GetRowsByKindAndEntityIdsAsync(
                EntityApprovalKind.EntityAnalysisModelTag, entityIds);

            rows.Should().HaveCount(count);
            rows.Select(s => s.EntityId).Should().OnlyHaveUniqueItems();
        }

        private static async Task SeedPromotedAndUnpromotedTrialsAsync(DbContext db, PreservationTenant source,
            int sourceModel)
        {
            var instanceId = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstance
            {
                Name = $"{DatabaseFixture.Prefix}Tr{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = sourceModel,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                StatusId = 0,
                Anomaly = 1,
                AnomalyProbability = 0.5,
                Filter = 0,
                ResponsePayload = 1,
                DataSourceId = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = source.UserName
            });

            var trialIds = new int[3];

            for (var i = 0; i < 3; i++)
            {
                trialIds[i] = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = instanceId,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await db.InsertAsync(new ExhaustiveSearchInstancePromotedTrialInstance
            {
                ExhaustiveSearchInstanceTrialInstanceId = trialIds[1],
                Json = "{}",
                Score = 0.9,
                Active = 1,
                Deleted = 0,
                CreatedDate = DateTime.UtcNow
            });

            await db.InsertAsync(new ExhaustiveSearchInstancePromotedTrialInstanceRoc
            {
                ExhaustiveSearchInstanceTrialInstanceId = trialIds[1],
                Threshold = 0.5,
                Score = 0.9,
                TruePositive = 1,
                TrueNegative = 1,
                FalsePositive = 0,
                FalseNegative = 0
            });
        }

    }
}