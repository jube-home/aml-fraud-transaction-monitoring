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
using Jube.Data.Query.GetEntityAnalysisModelActivationRuleOverrideQuery;
using Jube.Data.Query.GetEntityAnalysisModelOverrideQuery;
using Jube.Data.Repository.Models;
using Jube.Service.EntityAnalysisModelOverride;
using Jube.Service.EntityApproval;
using Jube.Service.Exceptions.EntityApproval;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ActivationRuleOverridePoco = Jube.Data.Poco.EntityAnalysisModelActivationRuleOverride;
using OverridePoco = Jube.Data.Poco.EntityAnalysisModelOverride;

namespace Jube.Test.Service.EntityApproval
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ForceOverrideApprovalTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> modelOverrideIds = [];
        private readonly List<int> ruleOverrideIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> xpathIds = [];
        private readonly List<int> ruleIds = [];
        private int tenantRegistryId;

        private string Maker => fx.Seed.UserWithPermissionNoApproval;
        private string Checker => fx.Seed.UserWithPermission;

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

            await dbContext.EntityApproval.Where(w => modelOverrideIds.Contains(w.EntityId ?? 0)
                                                      && w.EntityApprovalKindId ==
                                                      (int)EntityApprovalKind.EntityAnalysisModelOverride)
                .DeleteAsync();

            await dbContext.EntityApproval.Where(w => ruleOverrideIds.Contains(w.EntityId ?? 0)
                                                      && w.EntityApprovalKindId ==
                                                      (int)EntityApprovalKind.EntityAnalysisModelActivationRuleOverride)
                .DeleteAsync();

            await dbContext.GetTable<EntityAnalysisModelOverrideVersion>()
                .Where(w => modelOverrideIds.Contains(w.EntityAnalysisModelOverrideId!.Value)).DeleteAsync();

            await dbContext.EntityAnalysisModelOverride.Where(w => modelOverrideIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<EntityAnalysisModelActivationRuleOverrideVersion>()
                .Where(w => ruleOverrideIds.Contains(w.EntityAnalysisModelActivationRuleOverrideId!.Value))
                .DeleteAsync();

            await dbContext.EntityAnalysisModelActivationRuleOverride.Where(w => ruleOverrideIds.Contains(w.Id))
                .DeleteAsync();

            await dbContext.GetTable<EntityAnalysisModelRequestXpath>().Where(w => xpathIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Jube.Data.Poco.EntityAnalysisModelActivationRule>().Where(w => ruleIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();

            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<(int Id, Guid Guid)> SeedModelAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var model = new Jube.Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Force{Guid.NewGuid():N}"[..40],
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

            return (model.Id, model.Guid);
        }

        private async Task SeedKeyAndRuleAsync(int modelId, string key)
        {
            await using var dbContext = fx.GetDbContext();

            xpathIds.Add(await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId, Name = key, XPath = "$.Account", DataTypeId = 1, Active = 1,
                Locked = 0, Deleted = 0, EnableOverride = 1, Guid = Guid.NewGuid(), CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix, Version = 1
            }));

            ruleIds.Add(await dbContext.InsertWithInt32IdentityAsync(new Jube.Data.Poco.EntityAnalysisModelActivationRule
            {
                EntityAnalysisModelId = modelId, Name = "ZzRule", Active = 1, Locked = 0, Deleted = 0,
                EnableOverride = 1, EnableForce = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix, Version = 1
            }));
        }

        private async Task<int> SeedModelOverrideAsync(Guid modelGuid, byte kind, DateTime? expiry = null,
            string key = "IP", string? value = null, string? creator = null)
        {
            await using var dbContext = fx.GetDbContext();

            var saved = await new EntityAnalysisModelOverrideRepository(dbContext, creator ?? Maker).InsertAsync(
                new OverridePoco
                {
                    EntityAnalysisModelGuid = modelGuid,
                    OverrideKey = key,
                    OverrideKeyValue = value ?? $"zz{Guid.NewGuid():N}"[..20],
                    OverrideKind = kind,
                    DeleteExpiryDate = expiry,
                    Deleted = 0
                });

            modelOverrideIds.Add(saved.Id);

            return saved.Id;
        }

        private async Task<int> SeedRuleOverrideAsync(Guid modelGuid, byte kind, string key = "IP",
            string? value = null, string? creator = null)
        {
            await using var dbContext = fx.GetDbContext();

            var saved = await new EntityAnalysisModelActivationRuleOverrideRepository(dbContext, creator ?? Maker).InsertAsync(
                new ActivationRuleOverridePoco
                {
                    EntityAnalysisModelGuid = modelGuid,
                    OverrideKey = key,
                    OverrideKeyValue = value ?? $"zz{Guid.NewGuid():N}"[..20],
                    EntityAnalysisModelActivationRuleName = "ZzRule",
                    OverrideKind = kind,
                    Deleted = 0
                });

            ruleOverrideIds.Add(saved.Id);

            return saved.Id;
        }

        private static Task<EntityApprovalService> ApprovalServiceAsync(string user, DbContext dbContext)
        {
            return EntityApprovalService.CreateAsync(dbContext, user, TestLog.NoOp, 1);
        }

        private static async Task<List<int>> ProjectedModelOverrideIdsAsync(DbContext dbContext, int modelId,
            int tenantId)
        {
            var projected = await new EntityAnalysisModelOverrideRepository(dbContext, tenantId)
                .GetApprovedByEntityAnalysisModelIdAsync(modelId);

            return projected.Select(s => s.Id).ToList();
        }

        private static async Task<List<int>> ProjectedRuleOverrideIdsAsync(DbContext dbContext, Guid modelGuid,
            int tenantId)
        {
            var projected = await new EntityAnalysisModelActivationRuleOverrideRepository(dbContext, tenantId)
                .GetApprovedByEntityAnalysisModelGuidOrderByIdAsync(modelGuid);

            return projected.Select(s => s.Id).ToList();
        }

        [Fact]
        public async Task APendingForceOverrideIsNotInTheApprovedProjectionButASuppressOverrideIsAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1);
            var suppress = await SeedModelOverrideAsync(modelGuid, 0);

            await using var dbContext = fx.GetDbContext();
            var projected = await ProjectedModelOverrideIdsAsync(dbContext, modelId, tenantRegistryId);

            projected.Should().Contain(suppress, "a Suppress override keeps its behaviour with no approval");
            projected.Should().NotContain(force, "a Force override takes no effect until a second person approves it");
        }

        [Fact]
        public async Task OnlyAForceOverrideIsAnApprovalSubjectAndItIsListedAsPendingAsync()
        {
            var (_, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1);
            var suppress = await SeedModelOverrideAsync(modelGuid, 0);

            await using var dbContext = fx.GetDbContext();
            var service = await ApprovalServiceAsync(Checker, dbContext);

            var pending = await service.PendingForKindAsync(EntityApprovalKind.EntityAnalysisModelOverride);

            pending.Select(s => s.EntityId).Should().Contain(force).And.NotContain(suppress);
            pending.Single(s => s.EntityId == force).MakerUser.Should().Be(Maker);

            var act = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, suppress, 1);

            await act.Should().ThrowAsync<KeyNotFoundException>("a Suppress override is not an approval subject");
        }

        [Fact]
        public async Task AMakerCannotApproveOrRejectTheirOwnForceOverrideAsync()
        {
            var (_, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1, creator: Checker);

            await using var dbContext = fx.GetDbContext();
            var service = await ApprovalServiceAsync(Checker, dbContext);

            var approve = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1);
            var reject = () => service.RejectAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1);

            (await approve.Should().ThrowAsync<ApprovalRefusedException>()).Which.Reasons.Should().ContainSingle()
                .Which.Should().Contain("by you");

            (await reject.Should().ThrowAsync<ApprovalRefusedException>()).Which.Reasons.Should().ContainSingle()
                .Which.Should().Contain("by you");
        }

        [Fact]
        public async Task ACheckerApprovingAForceOverrideBringsItIntoTheApprovedProjectionAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1);

            await using var dbContext = fx.GetDbContext();
            var service = await ApprovalServiceAsync(Checker, dbContext);

            await service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1);

            (await ProjectedModelOverrideIdsAsync(dbContext, modelId, tenantRegistryId)).Should().Contain(force);
        }

        [Fact]
        public async Task ARejectedForceOverrideNeverReachesTheApprovedProjectionAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1);

            await using var dbContext = fx.GetDbContext();
            var service = await ApprovalServiceAsync(Checker, dbContext);

            await service.RejectAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1, "No");

            (await ProjectedModelOverrideIdsAsync(dbContext, modelId, tenantRegistryId)).Should().NotContain(force);
        }

        [Fact]
        public async Task RemovingAnApprovedForceOverrideIsPendingAndTheApprovedVersionStaysInForceUntilApprovedAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1);

            await using var dbContext = fx.GetDbContext();
            var checkerService = await ApprovalServiceAsync(Checker, dbContext);
            await checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1);

            await new EntityAnalysisModelOverrideRepository(dbContext, Maker).DeleteAsync(force);

            var row = await dbContext.EntityAnalysisModelOverride.FirstAsync(w => w.Id == force);
            row.Deleted.Should().Be(1);
            row.Version.Should().Be(2, "a removal of a Force override is a new version that needs approval");

            (await ProjectedModelOverrideIdsAsync(dbContext, modelId, tenantRegistryId)).Should().Contain(force);

            var status = await checkerService.StatusAsync(EntityApprovalKind.EntityAnalysisModelOverride, force);
            status.Deleted.Should().BeTrue();
            status.Pending.Should().BeTrue();

            await checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 2);

            (await ProjectedModelOverrideIdsAsync(dbContext, modelId, tenantRegistryId)).Should().NotContain(force);
        }

        [Fact]
        public async Task ChangingTheExpiryOfAnApprovedForceOverrideNeedsApprovalFromSomeoneOtherThanTheEditorAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1);

            await using var dbContext = fx.GetDbContext();
            var checkerService = await ApprovalServiceAsync(Checker, dbContext);
            await checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1);

            var later = DateTime.UtcNow.AddDays(30);

            var repository = new EntityAnalysisModelOverrideRepository(dbContext, Checker);
            var overrideRow = await dbContext.EntityAnalysisModelOverride.FirstAsync(w => w.Id == force);

            await repository.UpdateDeleteExpiryDateAsync(modelGuid, overrideRow.OverrideKey,
                overrideRow.OverrideKeyValue, later);

            var edited = await dbContext.EntityAnalysisModelOverride.FirstAsync(w => w.Id == force);
            edited.Version.Should().Be(2);
            edited.CreatedUser.Should().Be(Checker, "the editor becomes the maker of the new version");

            var projected = (await new EntityAnalysisModelOverrideRepository(dbContext, tenantRegistryId)
                .GetApprovedByEntityAnalysisModelIdAsync(modelId)).Single(s => s.Id == force);

            projected.DeleteExpiryDate.Should().BeNull("the approved version keeps its own expiry until the edit is approved");

            var self = () => checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 2);

            await self.Should().ThrowAsync<ApprovalRefusedException>("the editor of the expiry cannot approve it");
        }

        [Fact]
        public async Task AnExpiryEditToAForceOverrideShowsTheExpiryFieldInTheReviewedChangesAsync()
        {
            var (_, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1);

            await using var dbContext = fx.GetDbContext();
            var overrideRow = await dbContext.EntityAnalysisModelOverride.FirstAsync(w => w.Id == force);

            await new EntityAnalysisModelOverrideRepository(dbContext, Maker).UpdateDeleteExpiryDateAsync(modelGuid,
                overrideRow.OverrideKey, overrideRow.OverrideKeyValue, DateTime.UtcNow.AddDays(10));

            var service = await ApprovalServiceAsync(Checker, dbContext);
            var changes = await service.ChangesAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 2);

            changes.HasEarlierVersion.Should().BeTrue();
            changes.Changes.Select(c => c.PropertyName).Should().Contain("DeleteExpiryDate");
        }

        [Fact]
        public async Task AnApprovedForceOverrideThatHasExpiredIsNotInTheApprovedProjectionAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1, DateTime.UtcNow.AddMinutes(-5));

            await using var dbContext = fx.GetDbContext();
            var service = await ApprovalServiceAsync(Checker, dbContext);

            await service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1);

            (await ProjectedModelOverrideIdsAsync(dbContext, modelId, tenantRegistryId)).Should().NotContain(force);
        }

        [Fact]
        public async Task ARuleLevelForceOverrideFollowsTheSameApprovalRuleAsync()
        {
            var (_, modelGuid) = await SeedModelAsync();
            var force = await SeedRuleOverrideAsync(modelGuid, 1);
            var suppress = await SeedRuleOverrideAsync(modelGuid, 0);

            await using var dbContext = fx.GetDbContext();

            var before = await ProjectedRuleOverrideIdsAsync(dbContext, modelGuid, tenantRegistryId);
            before.Should().Contain(suppress).And.NotContain(force);

            var service = await ApprovalServiceAsync(Checker, dbContext);
            await service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelActivationRuleOverride, force, 1);

            (await ProjectedRuleOverrideIdsAsync(dbContext, modelGuid, tenantRegistryId)).Should().Contain(force);

            var ownForce = await SeedRuleOverrideAsync(modelGuid, 1, creator: Checker);

            var own = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelActivationRuleOverride,
                ownForce, 1);

            await own.Should().ThrowAsync<ApprovalRefusedException>();
        }

        [Fact]
        public async Task SwitchingARuleLevelOverrideToForceWithholdsItUntilApprovedAndMakesTheSwitcherTheMakerAsync()
        {
            var (_, modelGuid) = await SeedModelAsync();
            var suppress = await SeedRuleOverrideAsync(modelGuid, 0);

            await using var dbContext = fx.GetDbContext();
            var row = await dbContext.EntityAnalysisModelActivationRuleOverride.FirstAsync(w => w.Id == suppress);

            await new EntityAnalysisModelActivationRuleOverrideRepository(dbContext, Checker)
                .UpdateOverrideKindAsync(modelGuid, row.OverrideKey, row.OverrideKeyValue,
                    row.EntityAnalysisModelActivationRuleName, 1);

            var switched = await dbContext.EntityAnalysisModelActivationRuleOverride.FirstAsync(w => w.Id == suppress);
            switched.OverrideKind.Should().Be(1);
            switched.CreatedUser.Should().Be(Checker);

            (await ProjectedRuleOverrideIdsAsync(dbContext, modelGuid, tenantRegistryId)).Should()
                .NotContain(suppress, "a Suppress override switched to Force is a pending Force change");

            var self = await ApprovalServiceAsync(Checker, dbContext);

            var act = () => self.ApproveAsync(EntityApprovalKind.EntityAnalysisModelActivationRuleOverride, suppress, 2);

            await act.Should().ThrowAsync<ApprovalRefusedException>();
        }

        [Fact]
        public async Task SwitchingAForceRuleLevelOverrideBackToSuppressTakesEffectWithoutApprovalAsync()
        {
            var (_, modelGuid) = await SeedModelAsync();
            var force = await SeedRuleOverrideAsync(modelGuid, 1);

            await using var dbContext = fx.GetDbContext();
            var row = await dbContext.EntityAnalysisModelActivationRuleOverride.FirstAsync(w => w.Id == force);

            await new EntityAnalysisModelActivationRuleOverrideRepository(dbContext, Maker)
                .UpdateOverrideKindAsync(modelGuid, row.OverrideKey, row.OverrideKeyValue,
                    row.EntityAnalysisModelActivationRuleName, 0);

            (await ProjectedRuleOverrideIdsAsync(dbContext, modelGuid, tenantRegistryId)).Should().Contain(force);
        }

        [Fact]
        public async Task ResubmitOfAForceOverrideNeedsTheForcePermissionAndRetractNeedsARejectedVersionAsync()
        {
            var (_, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1);

            await using var dbContext = fx.GetDbContext();

            var service = await EntityAnalysisModelOverrideService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, new NullServiceChangeBus(), TestLog.NoOp);

            await Assert.ThrowsAsync<Jube.Service.Exceptions.EntityAnalysisModelOverride.ForbiddenException>(() =>
                service.ResubmitAsync(force));

            await Assert.ThrowsAsync<ApprovalRefusedException>(() => service.RetractAsync(force));

            var denied = await EntityAnalysisModelOverrideService.CreateAsync(dbContext,
                fx.Seed.UserWithoutPermission, TestLog.NoOp, localizers, new NullServiceChangeBus(), TestLog.NoOp);

            await Assert.ThrowsAsync<Jube.Service.Exceptions.EntityAnalysisModelOverride.ForbiddenException>(() =>
                denied.RetractAsync(force));
        }

        [Fact]
        public async Task ResubmitOfARejectedForceOverrideBumpsTheVersionAndMakesTheCallerTheMakerAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1);

            await using var dbContext = fx.GetDbContext();
            var checkerService = await ApprovalServiceAsync(Checker, dbContext);

            await checkerService.RejectAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1, "Why");

            var resubmitted = await new EntityAnalysisModelOverrideRepository(dbContext, Maker)
                .ResubmitAsync(force, "Still wanted");

            resubmitted.Version.Should().Be(2);
            resubmitted.CreatedUser.Should().Be(Maker);

            (await ProjectedModelOverrideIdsAsync(dbContext, modelId, tenantRegistryId)).Should().NotContain(force);

            await checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 2);

            (await ProjectedModelOverrideIdsAsync(dbContext, modelId, tenantRegistryId)).Should().Contain(force);
        }

        [Fact]
        public async Task RetractOfARejectedRemovalRestoresTheApprovedForceOverrideAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var force = await SeedModelOverrideAsync(modelGuid, 1);

            await using var dbContext = fx.GetDbContext();
            var checkerService = await ApprovalServiceAsync(Checker, dbContext);
            await checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1);

            var repository = new EntityAnalysisModelOverrideRepository(dbContext, Maker);
            await repository.DeleteAsync(force);

            await checkerService.RejectAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 2, "Keep it");

            var restored = await repository.RetractAsync(force);

            restored.Deleted.Should().NotBe(1);
            restored.Version.Should().Be(3);

            (await ProjectedModelOverrideIdsAsync(dbContext, modelId, tenantRegistryId)).Should().Contain(force);
        }

        [Fact]
        public async Task AForceOverrideWhoseRemovalIsPendingStaysListedWithItsPendingStateUntilTheRemovalIsApprovedAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var key = $"ZzKey{Guid.NewGuid():N}"[..20];
            var value = $"zz{Guid.NewGuid():N}"[..20];
            await SeedKeyAndRuleAsync(modelId, key);
            var force = await SeedModelOverrideAsync(modelGuid, 1, null, key, value);

            await using var dbContext = fx.GetDbContext();
            var checkerService = await ApprovalServiceAsync(Checker, dbContext);
            await checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1);

            await new EntityAnalysisModelOverrideRepository(dbContext, Maker).DeleteAsync(force);

            var query = new GetEntityAnalysisModelOverrideQuery(dbContext, Checker);

            var pending = (await query.ExecuteAsync(key, value, 1, default)).Single(s => s.EntityAnalysisModelGuid == modelGuid);
            pending.HasOverride.Should().BeTrue("the approved Force override still applies while its removal waits");
            pending.PendingRemoval.Should().BeTrue();
            pending.OverrideKind.Should().Be(1);
            pending.OverrideId.Should().Be(force);

            await checkerService.RejectAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 2, "Keep");

            (await query.ExecuteAsync(key, value, 1, default)).Single(s => s.EntityAnalysisModelGuid == modelGuid)
                .PendingRemoval.Should().BeTrue("a rejected removal leaves the approved version in force");

            await new EntityAnalysisModelOverrideRepository(dbContext, Maker).RetractAsync(force);

            var restored = (await query.ExecuteAsync(key, value, 1, default)).Single(s => s.EntityAnalysisModelGuid == modelGuid);
            restored.HasOverride.Should().BeTrue();
            restored.PendingRemoval.Should().BeFalse("a retracted removal is an ordinary live override again");
        }

        [Fact]
        public async Task AnApprovedRemovalOfAForceOverrideNoLongerListsItAsPresentAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var key = $"ZzKey{Guid.NewGuid():N}"[..20];
            var value = $"zz{Guid.NewGuid():N}"[..20];
            await SeedKeyAndRuleAsync(modelId, key);
            var force = await SeedModelOverrideAsync(modelGuid, 1, null, key, value);

            await using var dbContext = fx.GetDbContext();
            var checkerService = await ApprovalServiceAsync(Checker, dbContext);
            await checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 1);
            await new EntityAnalysisModelOverrideRepository(dbContext, Maker).DeleteAsync(force);
            await checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, force, 2);

            var row = (await new GetEntityAnalysisModelOverrideQuery(dbContext, Checker)
                .ExecuteAsync(key, value, 1, default)).Single(s => s.EntityAnalysisModelGuid == modelGuid);

            row.HasOverride.Should().BeFalse();
            row.PendingRemoval.Should().BeFalse();
        }

        [Fact]
        public async Task ARuleLevelForceOverrideWhoseRemovalIsPendingStaysListedWithItsPendingStateAsync()
        {
            var (modelId, modelGuid) = await SeedModelAsync();
            var key = $"ZzKey{Guid.NewGuid():N}"[..20];
            var value = $"zz{Guid.NewGuid():N}"[..20];
            await SeedKeyAndRuleAsync(modelId, key);
            var force = await SeedRuleOverrideAsync(modelGuid, 1, key, value);

            await using var dbContext = fx.GetDbContext();
            var checkerService = await ApprovalServiceAsync(Checker, dbContext);
            await checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelActivationRuleOverride, force, 1);

            await new EntityAnalysisModelActivationRuleOverrideRepository(dbContext, Maker).DeleteAsync(force);

            var query = new GetEntityAnalysisModelActivationRuleOverrideQuery(dbContext, Checker);

            var pending = (await query.ExecuteAsync(modelGuid, key, value, 1, default)).Single(s => s.Name == "ZzRule");
            pending.HasOverride.Should().BeTrue();
            pending.PendingRemoval.Should().BeTrue();
            pending.OverrideId.Should().Be(force);

            await checkerService.ApproveAsync(EntityApprovalKind.EntityAnalysisModelActivationRuleOverride, force, 2);

            var gone = (await query.ExecuteAsync(modelGuid, key, value, 1, default)).Single(s => s.Name == "ZzRule");
            gone.HasOverride.Should().BeFalse();
            gone.PendingRemoval.Should().BeFalse();
        }

        [Fact]
        public async Task AForceOverrideOfAnotherTenantIsNeitherListedNorApprovableAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var otherTenantId = await dbContext.UserInTenant
                .Where(w => w.User == fx.Seed.UserTenantB)
                .Select(s => s.TenantRegistryId)
                .FirstAsync();

            var otherModel = new Jube.Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}ForceB{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = otherTenantId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };

            otherModel.Id = await dbContext.InsertWithInt32IdentityAsync(otherModel);
            modelIds.Add(otherModel.Id);

            var foreign = await new EntityAnalysisModelOverrideRepository(dbContext, fx.Seed.UserTenantB).InsertAsync(
                new OverridePoco
                {
                    EntityAnalysisModelGuid = otherModel.Guid,
                    OverrideKey = "IP",
                    OverrideKeyValue = $"zz{Guid.NewGuid():N}"[..20],
                    OverrideKind = 1,
                    Deleted = 0
                });

            modelOverrideIds.Add(foreign.Id);

            var service = await ApprovalServiceAsync(Checker, dbContext);

            (await service.PendingForKindAsync(EntityApprovalKind.EntityAnalysisModelOverride)).Select(s => s.EntityId)
                .Should().NotContain(foreign.Id);

            var act = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelOverride, foreign.Id, 1);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
