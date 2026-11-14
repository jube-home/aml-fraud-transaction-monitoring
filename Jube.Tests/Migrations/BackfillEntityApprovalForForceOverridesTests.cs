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
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Migrations.Branches.ForceOverrideApproval;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Xunit;
using ActivationRuleOverridePoco = Jube.Data.Poco.EntityAnalysisModelActivationRuleOverride;
using OverridePoco = Jube.Data.Poco.EntityAnalysisModelOverride;

namespace Jube.Test.Migrations
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class BackfillEntityApprovalForForceOverridesTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> modelOverrideIds = [];
        private readonly List<int> ruleOverrideIds = [];
        private int modelId;
        private Guid modelGuid;
        private int tenantRegistryId;
        private int liveForce;
        private int liveRuleForce;
        private int suppress;
        private int removedForce;

        public async Task InitializeAsync()
        {
            await using var db = fx.GetDbContext();

            tenantRegistryId = await db.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(s => s.TenantRegistryId).FirstAsync();

            modelGuid = Guid.NewGuid();

            modelId = await db.InsertWithInt32IdentityAsync(new EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Backfill{Guid.NewGuid():N}"[..40],
                Guid = modelGuid,
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            liveForce = await InsertModelOverrideAsync(db, 1, 0);
            suppress = await InsertModelOverrideAsync(db, 0, 0);
            removedForce = await InsertModelOverrideAsync(db, 1, 1);

            liveRuleForce = await db.InsertWithInt32IdentityAsync(new ActivationRuleOverridePoco
            {
                EntityAnalysisModelGuid = modelGuid,
                OverrideKey = "IP",
                OverrideKeyValue = $"zz{Guid.NewGuid():N}"[..20],
                EntityAnalysisModelActivationRuleName = "ZzRule",
                OverrideKind = 1,
                Deleted = 0,
                Version = 1,
                CreatedUser = "ZzMaker",
                CreatedDate = DateTime.UtcNow
            });

            ruleOverrideIds.Add(liveRuleForce);
        }

        private async Task<int> InsertModelOverrideAsync(Jube.Data.Context.DbContext db, byte kind, byte deleted)
        {
            var id = await db.InsertWithInt32IdentityAsync(new OverridePoco
            {
                EntityAnalysisModelGuid = modelGuid,
                OverrideKey = "IP",
                OverrideKeyValue = $"zz{Guid.NewGuid():N}"[..20],
                OverrideKind = kind,
                Deleted = deleted,
                Version = 1,
                CreatedUser = "ZzMaker",
                CreatedDate = DateTime.UtcNow
            });

            modelOverrideIds.Add(id);

            return id;
        }

        public async Task DisposeAsync()
        {
            await using var db = fx.GetDbContext();

            await db.EntityApproval.Where(w => w.EntityApprovalKindId == 22 && modelOverrideIds.Contains(w.EntityId ?? 0))
                .DeleteAsync();

            await db.EntityApproval.Where(w => w.EntityApprovalKindId == 23 && ruleOverrideIds.Contains(w.EntityId ?? 0))
                .DeleteAsync();

            await db.EntityAnalysisModelOverride.Where(w => modelOverrideIds.Contains(w.Id)).DeleteAsync();
            await db.EntityAnalysisModelActivationRuleOverride.Where(w => ruleOverrideIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
            await db.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
        }

        private async Task BackfillAsync(int approvers)
        {
            await using var db = fx.GetDbContext();
            await db.ExecuteAsync(BackfillEntityApprovalForForceOverrides.BuildBackfillSql(approvers));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public async Task EveryLiveForceOverrideStaysLiveWhateverTheApprovalsRequiredAsync(int approvalsRequired)
        {
            await BackfillAsync(BackfillEntityApprovalForForceOverrides.MinimumSystemApprovers);

            await using var db = fx.GetDbContext();

            var models = (await new EntityAnalysisModelOverrideRepository(db, tenantRegistryId)
                .GetApprovedByEntityAnalysisModelIdAsync(modelId, approvalsRequired)).Select(s => s.Id).ToList();

            var rules = (await new EntityAnalysisModelActivationRuleOverrideRepository(db, tenantRegistryId)
                .GetApprovedByEntityAnalysisModelGuidOrderByIdAsync(modelGuid, approvalsRequired))
                .Select(s => s.Id).ToList();

            models.Should().Contain(liveForce).And.Contain(suppress).And.NotContain(removedForce);
            rules.Should().Contain(liveRuleForce);
        }

        [Fact]
        public async Task TheBackfillStampsDistinctSystemApproversOnlyOnLiveForceRowsAndIsRepeatableAsync()
        {
            await BackfillAsync(3);
            await BackfillAsync(3);

            await using var db = fx.GetDbContext();

            var rows = await db.EntityApproval.Where(w => w.EntityApprovalKindId == 22
                                                          && modelOverrideIds.Contains(w.EntityId ?? 0)).ToListAsync();

            rows.Where(w => w.EntityId == liveForce).Select(s => s.CreatedUser).Should()
                .BeEquivalentTo("Administrator", "Administrator2", "Administrator3");

            rows.Should().NotContain(w => w.EntityId == suppress || w.EntityId == removedForce);
            rows.All(a => a.StateId == (int)EntityApprovalState.Approved && a.EntityVersion == 1).Should().BeTrue();
            rows.All(a => a.TenantRegistryId == tenantRegistryId).Should().BeTrue();
        }

        [Fact]
        public async Task ARequirementRaisedAboveTheStampedApproversWithholdsTheOverrideUntilMoreApprovalsExistAsync()
        {
            await BackfillAsync(2);

            await using var db = fx.GetDbContext();

            var models = (await new EntityAnalysisModelOverrideRepository(db, tenantRegistryId)
                .GetApprovedByEntityAnalysisModelIdAsync(modelId, 3)).Select(s => s.Id).ToList();

            models.Should().NotContain(liveForce);
        }
    }
}
