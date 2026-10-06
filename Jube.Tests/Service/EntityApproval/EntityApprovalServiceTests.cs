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
using FluentAssertions.Execution;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.EntityApproval;
using Jube.Service.Exceptions.EntityApproval;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.EntityApproval
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityApprovalServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string Maker = "ZzMaker";
        private readonly List<int> listIds = [];
        private readonly List<int> listValueIds = [];
        private readonly List<int> dictionaryIds = [];
        private readonly List<int> kvpIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> xPathIds = [];
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
                                                      && w.EntityApprovalKindId == (int)EntityApprovalKind
                                                          .EntityAnalysisModelRequestXPath).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                .Where(w => xPathIds.Contains(w.EntityAnalysisModelRequestXpathId!.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => xPathIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityApproval.Where(w => listValueIds.Contains(w.EntityId ?? 0)
                                                      && w.EntityApprovalKindId == (int)EntityApprovalKind
                                                          .EntityAnalysisModelListValue).DeleteAsync();
            await dbContext.EntityApproval.Where(w => kvpIds.Contains(w.EntityId ?? 0)
                                                      && w.EntityApprovalKindId == (int)EntityApprovalKind
                                                          .EntityAnalysisModelDictionaryKvp).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelDictionaryKvpVersion>()
                .Where(w => kvpIds.Contains(w.EntityAnalysisModelDictionaryKvpId!.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelDictionaryKvp.Where(w => kvpIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelDictionaryVersion>()
                .Where(w => dictionaryIds.Contains(w.EntityAnalysisModelDictionaryId)).DeleteAsync();
            await dbContext.EntityAnalysisModelDictionary.Where(w => dictionaryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelListValueVersion>()
                .Where(w => listValueIds.Contains(w.EntityAnalysisModelListValueId!.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelListValue.Where(w => listValueIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelListVersion>()
                .Where(w => listIds.Contains(w.EntityAnalysisModelListId)).DeleteAsync();
            await dbContext.EntityAnalysisModelList.Where(w => listIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedModelAsync(int? tenantId = null)
        {
            await using var dbContext = fx.GetDbContext();

            var model = new Jube.Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Appr{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId ?? tenantRegistryId,
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

        private async Task<int> SeedRequestXPathAsync(int modelId, string createdBy, int version = 1)
        {
            await using var dbContext = fx.GetDbContext();

            var name = $"ZzAppr{Guid.NewGuid():N}"[..20];
            var id = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId,
                Name = name,
                XPath = $"$.{name}",
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = version,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = createdBy
            });

            xPathIds.Add(id);
            return id;
        }

        private static Task<EntityApprovalService> ServiceAsync(string user, DbContext dbContext)
        {
            return EntityApprovalService.CreateAsync(dbContext, user, TestLog.NoOp, 1);
        }

        [Fact]
        public async Task ACheckerWithPermissionApprovesTheCurrentVersionOfAnotherMakersChangeAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, Maker);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var approval = await service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            using var scope = new AssertionScope();
            approval.StateId.Should().Be((int)EntityApprovalState.Approved);
            approval.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            approval.EntityVersion.Should().Be(1);
        }

        [Fact]
        public async Task AMakerCannotApproveTheirOwnChangeAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, fx.Seed.UserWithPermission);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var act = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            var ex = await act.Should().ThrowAsync<ApprovalRefusedException>();
            ex.Which.Reasons.Should().ContainSingle().Which.Should().Contain("by you");
        }

        [Fact]
        public async Task AMakerCannotRejectTheirOwnChangeAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, fx.Seed.UserWithPermission);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var act = () => service.RejectAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            var ex = await act.Should().ThrowAsync<ApprovalRefusedException>();
            ex.Which.Reasons.Should().ContainSingle().Which.Should().Contain("by you");
        }

        [Fact]
        public async Task ALandlordMayRejectTheirOwnChangeAsync()
        {
            await using var seedContext = fx.GetDbContext();
            var landlordTenantId = await seedContext.UserInTenant
                .Where(w => w.User == fx.Seed.LandlordUser)
                .Select(s => s.TenantRegistryId)
                .FirstAsync();

            var modelId = await SeedModelAsync(landlordTenantId);
            var id = await SeedRequestXPathAsync(modelId, fx.Seed.LandlordUser);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.LandlordUser, dbContext);

            var rejection = await service.RejectAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            rejection.StateId.Should().Be((int)EntityApprovalState.Rejected);
        }

        [Fact]
        public async Task ANoteLongerThanTheLimitIsRefusedOnApprovalAndRejectionAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, Maker);
            var tooLong = new string('x', 1001);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var approve = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1,
                tooLong);
            var reject = () => service.RejectAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1,
                tooLong);

            await approve.Should().ThrowAsync<ApprovalRefusedException>();
            await reject.Should().ThrowAsync<ApprovalRefusedException>();
        }

        [Fact]
        public async Task ALandlordMayApproveTheirOwnChangeAsync()
        {
            await using var seedContext = fx.GetDbContext();
            var landlordTenantId = await seedContext.UserInTenant
                .Where(w => w.User == fx.Seed.LandlordUser)
                .Select(s => s.TenantRegistryId)
                .FirstAsync();

            var modelId = await SeedModelAsync(landlordTenantId);
            var id = await SeedRequestXPathAsync(modelId, fx.Seed.LandlordUser);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.LandlordUser, dbContext);

            var approval = await service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            approval.StateId.Should().Be((int)EntityApprovalState.Approved);
            approval.CreatedUser.Should().Be(fx.Seed.LandlordUser);
        }

        [Fact]
        public async Task AUserWithoutAllowApprovalCannotApproveAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, Maker);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermissionNoApproval, dbContext);

            var act = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task AStaleVersionCannotBeApprovedAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, Maker, version: 3);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var act = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 2);

            var ex = await act.Should().ThrowAsync<ApprovalRefusedException>();
            ex.Which.Reasons.Should().ContainSingle().Which.Should().Contain("3");
        }

        [Fact]
        public async Task ARejectedVersionCannotThenBeApprovedAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, Maker);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            await service.RejectAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1, "no");

            var act = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            await act.Should().ThrowAsync<ApprovalRefusedException>();
        }

        [Fact]
        public async Task ApprovingTheSameVersionTwiceByTheSameCheckerIsRefusedAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, Maker);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            await service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            var act = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            await act.Should().ThrowAsync<ApprovalRefusedException>();
        }

        [Fact]
        public async Task AnUnapprovedChangeAppearsInThePendingListAndAnApprovedOneDoesNotAsync()
        {
            var modelId = await SeedModelAsync();
            var pendingId = await SeedRequestXPathAsync(modelId, Maker);
            var approvedId = await SeedRequestXPathAsync(modelId, Maker);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);
            await service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, approvedId, 1);

            var pending = await service.PendingForModelAsync(modelId);

            using var scope = new AssertionScope();
            pending.Should().Contain(p => p.EntityId == pendingId);
            pending.Should().NotContain(p => p.EntityId == approvedId);
        }

        [Fact]
        public async Task ApprovingAnEntityThatDoesNotExistInThisTenantIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var act = () => service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, int.MaxValue, 1);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task PendingByKindListsUnapprovedEntitiesOfThatKindAcrossModelsAndOmitsApprovedOnesAsync()
        {
            var firstModel = await SeedModelAsync();
            var secondModel = await SeedModelAsync();
            var pendingFirst = await SeedRequestXPathAsync(firstModel, Maker);
            var pendingSecond = await SeedRequestXPathAsync(secondModel, Maker);
            var approved = await SeedRequestXPathAsync(firstModel, Maker);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);
            await service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, approved, 1);

            var pending = await service.PendingForKindAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath);

            using var scope = new AssertionScope();
            pending.Should().Contain(p => p.EntityId == pendingFirst);
            pending.Should().Contain(p => p.EntityId == pendingSecond);
            pending.Should().NotContain(p => p.EntityId == approved);
            pending.Should().OnlyContain(p => p.Kind == EntityApprovalKind.EntityAnalysisModelRequestXPath);
        }

        [Fact]
        public async Task PendingByKindNeverListsAnotherTenantsEntitiesAsync()
        {
            await using var seedDb = fx.GetDbContext();
            var otherTenantId = await seedDb.UserInTenant
                .Where(w => w.User == fx.Seed.UserTenantB)
                .Select(s => s.TenantRegistryId)
                .FirstAsync();

            var otherModel = new Jube.Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}ApprB{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = otherTenantId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            otherModel.Id = await seedDb.InsertWithInt32IdentityAsync(otherModel);
            modelIds.Add(otherModel.Id);

            var foreignId = await SeedRequestXPathAsync(otherModel.Id, Maker);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var pending = await service.PendingForKindAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath);

            pending.Should().NotContain(p => p.EntityId == foreignId);
        }

        [Fact]
        public async Task PendingByKindRefusesAUserWithoutTheViewPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithoutPermission, dbContext);

            var act = () => service.PendingForKindAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath);

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task AStatusOfAnUnapprovedChangeOffersApprovalToACheckerButNotToTheMakerAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, fx.Seed.UserWithPermission);

            await using var dbContext = fx.GetDbContext();
            var maker = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);
            var status = await maker.StatusAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id);

            using var scope = new AssertionScope();
            status.Pending.Should().BeTrue();
            status.EffectiveVersion.Should().BeNull();
            status.CanApprove.Should().BeFalse("the maker of a change cannot approve it");
        }

        [Fact]
        public async Task AStatusAfterACheckerApprovesShowsTheVersionInEffectAndWithdrawsTheCheckersApprovalAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, Maker);

            await using var dbContext = fx.GetDbContext();
            var checker = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);
            await checker.ApproveAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            var status = await checker.StatusAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id);

            using var scope = new AssertionScope();
            status.Pending.Should().BeFalse();
            status.EffectiveVersion.Should().Be(1);
            status.ApprovalsRecorded.Should().Be(1);
            status.CanApprove.Should().BeFalse("a checker cannot approve the same version twice");
        }

        [Fact]
        public async Task APendingListValueCarriesTheIdOfTheListItBelongsToAsync()
        {
            var modelId = await SeedModelAsync();

            await using var dbContext = fx.GetDbContext();
            var model = await dbContext.EntityAnalysisModel.FirstAsync(w => w.Id == modelId);

            var listId = await dbContext.InsertWithInt32IdentityAsync(new Jube.Data.Poco.EntityAnalysisModelList
            {
                EntityAnalysisModelGuid = model.Guid,
                Name = $"{DatabaseFixture.Prefix}ApprL{Guid.NewGuid():N}"[..30],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Maker
            });
            listIds.Add(listId);

            var valueId = await dbContext.InsertWithInt32IdentityAsync(new Jube.Data.Poco.EntityAnalysisModelListValue
            {
                EntityAnalysisModelListId = listId,
                ListValue = "ZzApprValue",
                Guid = Guid.NewGuid(),
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Maker
            });
            listValueIds.Add(valueId);

            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);
            var pending = await service.PendingForModelAsync(modelId);

            var value = pending.Single(p => p.Kind == EntityApprovalKind.EntityAnalysisModelListValue
                                            && p.EntityId == valueId);
            value.ParentId.Should().Be(listId);
        }

        private async Task<(int ListId, List<int> ValueIds)> SeedListWithValuesAsync(int modelId, string createdBy,
            int valueCount)
        {
            await using var dbContext = fx.GetDbContext();
            var model = await dbContext.EntityAnalysisModel.FirstAsync(w => w.Id == modelId);

            var listId = await dbContext.InsertWithInt32IdentityAsync(new Jube.Data.Poco.EntityAnalysisModelList
            {
                EntityAnalysisModelGuid = model.Guid,
                Name = $"{DatabaseFixture.Prefix}BulkL{Guid.NewGuid():N}"[..30],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = createdBy
            });
            listIds.Add(listId);

            var valueIds = new List<int>();
            for (var i = 0; i < valueCount; i++)
            {
                var valueId = await dbContext.InsertWithInt32IdentityAsync(
                    new Jube.Data.Poco.EntityAnalysisModelListValue
                    {
                        EntityAnalysisModelListId = listId,
                        ListValue = $"ZzBulk{i}",
                        Guid = Guid.NewGuid(),
                        Deleted = 0,
                        Version = 1,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = createdBy
                    });
                listValueIds.Add(valueId);
                valueIds.Add(valueId);
            }

            return (listId, valueIds);
        }

        [Fact]
        public async Task ACheckerApprovesEveryPendingValueOfAListInOneActionAsync()
        {
            var modelId = await SeedModelAsync();
            var (listId, valueIds) = await SeedListWithValuesAsync(modelId, Maker, 2);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var result = await service.ApproveValuesAsync(EntityApprovalKind.EntityAnalysisModelList, listId);

            var stillPending = await service.PendingForModelAsync(modelId);

            using var scope = new AssertionScope();
            result.Approved.Should().Be(2);
            result.Refusals.Should().BeEmpty();
            stillPending.Should().NotContain(p => p.Kind == EntityApprovalKind.EntityAnalysisModelListValue
                                                  && valueIds.Contains(p.EntityId));
        }

        [Fact]
        public async Task AMakerWhoAlsoChecksIsRefusedOnEveryValueAndNothingIsApprovedAsync()
        {
            var modelId = await SeedModelAsync();
            var (listId, valueIds) = await SeedListWithValuesAsync(modelId, fx.Seed.UserWithPermission, 2);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var result = await service.ApproveValuesAsync(EntityApprovalKind.EntityAnalysisModelList, listId);

            using var scope = new AssertionScope();
            result.Approved.Should().Be(0);
            result.Refusals.Should().HaveCount(valueIds.Count);
        }

        [Fact]
        public async Task BulkApprovalRefusesAKindThatHasNoValuesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var act = () => service.ApproveValuesAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, 1);

            await act.Should().ThrowAsync<ApprovalRefusedException>();
        }

        private async Task<(int DictionaryId, List<int> KvpIds)> SeedDictionaryWithPairsAsync(int modelId,
            string createdBy, int pairCount)
        {
            await using var dbContext = fx.GetDbContext();
            var model = await dbContext.EntityAnalysisModel.FirstAsync(w => w.Id == modelId);

            var dictionaryId = await dbContext.InsertWithInt32IdentityAsync(
                new Jube.Data.Poco.EntityAnalysisModelDictionary
                {
                    EntityAnalysisModelGuid = model.Guid,
                    Name = $"{DatabaseFixture.Prefix}ApprD{Guid.NewGuid():N}"[..30],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = createdBy
                });
            dictionaryIds.Add(dictionaryId);

            var kvpIdsOfDictionary = new List<int>();
            for (var i = 0; i < pairCount; i++)
            {
                var kvpId = await dbContext.InsertWithInt32IdentityAsync(
                    new Jube.Data.Poco.EntityAnalysisModelDictionaryKvp
                    {
                        EntityAnalysisModelDictionaryId = dictionaryId,
                        KvpKey = $"ZzKvp{i}",
                        KvpValue = i,
                        Guid = Guid.NewGuid(),
                        Deleted = 0,
                        Version = 1,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = createdBy
                    });
                kvpIds.Add(kvpId);
                kvpIdsOfDictionary.Add(kvpId);
            }

            return (dictionaryId, kvpIdsOfDictionary);
        }

        [Fact]
        public async Task AValueStatusShowsWhichValuesOfAListAwaitApprovalAndListsThoseAwaitingFirstAsync()
        {
            var modelId = await SeedModelAsync();
            var (listId, valueIds) = await SeedListWithValuesAsync(modelId, Maker, 2);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);
            await service.ApproveAsync(EntityApprovalKind.EntityAnalysisModelListValue, valueIds[0], 1);

            var statuses = await service.ValueStatusAsync(EntityApprovalKind.EntityAnalysisModelList, listId);

            using var scope = new AssertionScope();
            statuses.Should().HaveCount(2);
            statuses.Single(s => s.EntityId == valueIds[0]).Pending.Should().BeFalse();
            statuses.Single(s => s.EntityId == valueIds[1]).Pending.Should().BeTrue();
            statuses[0].EntityId.Should().Be(valueIds[1]);
        }

        [Fact]
        public async Task AMakerIsNotOfferedApprovalOfTheirOwnListValuesInTheValueStatusAsync()
        {
            var modelId = await SeedModelAsync();
            var (listId, _) = await SeedListWithValuesAsync(modelId, fx.Seed.UserWithPermission, 2);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var statuses = await service.ValueStatusAsync(EntityApprovalKind.EntityAnalysisModelList, listId);

            using var scope = new AssertionScope();
            statuses.Should().HaveCount(2);
            statuses.Should().OnlyContain(s => s.Pending && !s.CanApprove);
        }

        [Fact]
        public async Task AValueStatusOfADictionaryListsItsPairsAsPendingKeyValuePairsAsync()
        {
            var modelId = await SeedModelAsync();
            var (dictionaryId, kvpIdsOfDictionary) = await SeedDictionaryWithPairsAsync(modelId, Maker, 2);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var statuses = await service.ValueStatusAsync(EntityApprovalKind.EntityAnalysisModelDictionary,
                dictionaryId);

            using var scope = new AssertionScope();
            statuses.Select(s => s.EntityId).Should().BeEquivalentTo(kvpIdsOfDictionary);
            statuses.Should().OnlyContain(s => s.Kind == EntityApprovalKind.EntityAnalysisModelDictionaryKvp
                                               && s.Pending && s.CanApprove);
        }

        [Fact]
        public async Task AValueStatusRefusesAKindThatHasNoValuesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var act = () => service.ValueStatusAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, 1);

            await act.Should().ThrowAsync<ApprovalRefusedException>();
        }

        [Fact]
        public async Task AValueStatusOfAListThatDoesNotExistInThisTenantIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var act = () => service.ValueStatusAsync(EntityApprovalKind.EntityAnalysisModelList, int.MaxValue);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ABulkApprovalOfOneListLeavesTheValuesOfAnotherListInTheSameModelPendingAsync()
        {
            var modelId = await SeedModelAsync();
            var (approvedListId, _) = await SeedListWithValuesAsync(modelId, Maker, 2);
            var (otherListId, otherValueIds) = await SeedListWithValuesAsync(modelId, Maker, 2);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            await service.ApproveValuesAsync(EntityApprovalKind.EntityAnalysisModelList, approvedListId);
            var otherStatuses = await service.ValueStatusAsync(EntityApprovalKind.EntityAnalysisModelList,
                otherListId);

            otherStatuses.Select(s => s.EntityId).Should().BeEquivalentTo(otherValueIds);
            otherStatuses.Should().OnlyContain(s => s.Pending);
        }

        [Fact]
        public Task ACreatorWithoutATenantCannotActAtAllAsync()
        {
            var act = async () =>
            {
                await using var dbContext = fx.GetDbContext();
                await EntityApprovalService.CreateAsync(dbContext, "ZzNoSuchUser", TestLog.NoOp, 1);
            };

            return act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        private async Task<int> SeedRequestXPathWithEarlierVersionAsync(int modelId, string earlierXPath,
            string currentXPath)
        {
            await using var dbContext = fx.GetDbContext();

            var name = $"ZzAppr{Guid.NewGuid():N}"[..20];
            var id = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId,
                Name = name,
                XPath = currentXPath,
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 2,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Maker
            });
            xPathIds.Add(id);

            await dbContext.InsertAsync(new EntityAnalysisModelRequestXpathVersion
            {
                EntityAnalysisModelRequestXpathId = id,
                EntityAnalysisModelId = modelId,
                Name = name,
                XPath = earlierXPath,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow.AddMinutes(-5),
                CreatedUser = Maker
            });

            return id;
        }

        [Fact]
        public async Task ChangesShowOnlyTheFieldThatChangedAgainstTheLatestSupersededVersionAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathWithEarlierVersionAsync(modelId, "$.a / b", "$.a * b");

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var changes = await service.ChangesAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 2);

            using var scope = new AssertionScope();
            changes.HasEarlierVersion.Should().BeTrue();
            changes.Changes.Select(c => c.PropertyName).Should().Equal("XPath");
            changes.Changes[0].FromValue.Should().Be("$.a / b");
            changes.Changes[0].ToValue.Should().Be("$.a * b");
        }

        [Fact]
        public async Task ChangesDoNotReportLayoutOnlyDifferencesAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathWithEarlierVersionAsync(modelId, "$.a /\nb", "$.a /  \r\n   b  ");

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var changes = await service.ChangesAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 2);

            using var scope = new AssertionScope();
            changes.HasEarlierVersion.Should().BeTrue();
            changes.Changes.Should().BeEmpty();
        }

        [Fact]
        public async Task AFirstVersionHasNoEarlierVersionToCompareAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, Maker);

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var changes = await service.ChangesAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            changes.HasEarlierVersion.Should().BeFalse();
            changes.Changes.Should().BeEmpty();
        }

        [Fact]
        public async Task ChangesForAVersionThatIsNoLongerCurrentAreRefusedAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathWithEarlierVersionAsync(modelId, "$.a / b", "$.a * b");

            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(fx.Seed.UserWithPermission, dbContext);

            var act = () => service.ChangesAsync(EntityApprovalKind.EntityAnalysisModelRequestXPath, id, 1);

            await act.Should().ThrowAsync<ApprovalRefusedException>();
        }
    }
}