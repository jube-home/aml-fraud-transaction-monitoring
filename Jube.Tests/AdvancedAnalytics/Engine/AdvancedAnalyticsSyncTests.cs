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
using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.Helpers;
using Jube.Test.AdvancedAnalytics.Repository;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using SyncContext = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Context;

namespace Jube.Test.AdvancedAnalytics.Engine
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsSyncTests(DatabaseFixture fx) : AdvancedAnalyticsRepositoryTestBase(fx)
    {
        private static readonly string[] names = ["Payload.A", "Abstraction.B"];
        private static readonly TrainedFixture first = EngineModelFactory.Trained(51, names, [1, 5]);
        private static readonly TrainedFixture second = EngineModelFactory.Trained(52, names, [2, 6]);

        private async Task<AdvancedAnalyticsInstance> ActiveInstanceAsync(int modelId, string? name = null,
            byte active = 1, byte responsePayload = 0)
        {
            var instance = await NewInstanceAsync(TenantA, modelId, name);
            var repository = new AdvancedAnalyticsInstanceRepository(Db, TenantA, "ZzTestUser");
            instance.Active = active;
            instance.ResponsePayload = responsePayload;

            return await repository.UpdateAsync(instance);
        }

        private async Task<(AdvancedAnalyticsTrainedModel Model, AdvancedAnalyticsTrainedModelVersion Version)>
            TrainedAsync(int instanceId, TrainedFixture fixture, bool activate = true, string? name = null)
        {
            var models = new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, "ZzTestUser");

            var model = await models.InsertAsync(new AdvancedAnalyticsTrainedModel
            {
                AdvancedAnalyticsInstanceId = instanceId,
                Name = name ?? UniqueName("Trained"),
                KindId = AdvancedAnalyticsKind.Classifier,
                SourceId = AdvancedAnalyticsSource.Agent
            });

            var version = await AddVersionAsync(model.Id, fixture);

            if (activate)
            {
                await models.SetActiveVersionAsync(model.Id, version.Id);
                await models.SetActiveAsync(model.Id, true);
            }

            return (model, version);
        }

        private Task<AdvancedAnalyticsTrainedModelVersion> AddVersionAsync(int modelId, TrainedFixture fixture,
            string? modelJson = null)
        {
            return new AdvancedAnalyticsTrainedModelVersionRepository(Db, TenantA, "ZzTestUser").InsertAsync(
                new AdvancedAnalyticsTrainedModelVersion
                {
                    AdvancedAnalyticsTrainedModelId = modelId,
                    ModelJson = modelJson ?? fixture.SnapshotJson,
                    VariablesJson = fixture.VariablesJson
                });
        }

        private (SyncContext Context, EntityAnalysisModelDomain Model, TestLog Log) NewSync(int modelId)
        {
            var log = new TestLog(false);

            var model = new EntityAnalysisModelDomain
            {
                Instance = { Id = modelId, Guid = Guid.NewGuid(), TenantRegistryId = TenantA }
            };

            var context = new SyncContext
            {
                JsonSerializationHelper = new JsonSerializationHelper(),
                Services =
                {
                    DbContext = Db,
                    Log = log,
                    Parser = new Parser.Parser(log, [])
                },
                EntityAnalysisModels =
                {
                    ActiveEntityAnalysisModels = new ConcurrentDictionary<int, EntityAnalysisModelDomain>
                        { [modelId] = model }
                }
            };

            return (context, model, log);
        }

        [Fact]
        public async Task LoadsTheActiveVersionOfAnActiveTrainedModelOfAnActiveInstanceAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await ActiveInstanceAsync(modelId, responsePayload: 1);
            var (trained, version) = await TrainedAsync(instance.Id, first, name: "Sync Logistic");
            var (context, model, _) = NewSync(modelId);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            var loaded = model.Snapshot.AdvancedAnalyticsModels.Should().ContainSingle().Subject;
            loaded.Id.Should().Be(trained.Id);
            loaded.Guid.Should().Be(trained.Guid);
            loaded.VersionId.Should().Be(version.Id);
            loaded.AdvancedAnalyticsInstanceId.Should().Be(instance.Id);
            loaded.Name.Should().Be("Sync_Logistic");
            loaded.ResponsePayload.Should().BeTrue();
            loaded.VariablesInOrder.Select(s => s.Name).Should().Equal(names);
            loaded.VariablesInOrder.Select(s => s.ProcessingTypeId).Should().Equal(1, 5);
            loaded.Recall([0.2, 0.3]).Should().Equal(first.Recall.Predict([0.2, 0.3]));
        }

        [Fact]
        public async Task OnlyTheActiveVersionLoadsNotTheLatestOrAnEarlierOneAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await ActiveInstanceAsync(modelId);
            var (trained, active) = await TrainedAsync(instance.Id, first);
            var newerInactive = await AddVersionAsync(trained.Id, second);
            var (context, model, _) = NewSync(modelId);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            var loaded = model.Snapshot.AdvancedAnalyticsModels.Single();
            loaded.VersionId.Should().Be(active.Id).And.NotBe(newerInactive.Id);
            loaded.Recall([0.2, 0.3]).Should().Equal(first.Recall.Predict([0.2, 0.3]));
        }

        [Fact]
        public async Task ATrainedModelWithoutAnActiveVersionOrNotActiveIsNotLoadedAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await ActiveInstanceAsync(modelId);
            await TrainedAsync(instance.Id, first, false);
            var (deactivated, _) = await TrainedAsync(instance.Id, second, name: "Live");
            var (context, model, _) = NewSync(modelId);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            model.Snapshot.AdvancedAnalyticsModels.Should().ContainSingle().Which.Name.Should().Be("Live");

            await new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, "ZzTestUser")
                .SetActiveAsync(deactivated.Id, false);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            model.Snapshot.AdvancedAnalyticsModels.Should().BeEmpty();
        }

        [Fact]
        public async Task InactiveDeletedAndUnallocatedInstancesAreSkippedAndOthersStillLoadAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var otherModelId = await NewEntityAnalysisModelAsync(TenantA);
            var inactive = await ActiveInstanceAsync(modelId, active: 0);
            var deleted = await ActiveInstanceAsync(modelId);
            var live = await ActiveInstanceAsync(modelId);
            var otherModel = await ActiveInstanceAsync(otherModelId);
            var unallocated = await NewInstanceAsync(TenantA);

            await new AdvancedAnalyticsInstanceRepository(Db, TenantA, "ZzTestUser").UpdateAsync(
                new AdvancedAnalyticsInstance { Id = unallocated.Id, Name = unallocated.Name, Active = 1 });

            foreach (var (instance, name) in new[]
                     {
                         (inactive, "Inactive"), (deleted, "Deleted"), (live, "Live"), (otherModel, "Other"),
                         (unallocated, "Unallocated")
                     })
            {
                await TrainedAsync(instance.Id, first, name: name);
            }

            await new AdvancedAnalyticsInstanceRepository(Db, TenantA, "ZzTestUser").DeleteAsync(deleted.Id);
            var (context, model, _) = NewSync(modelId);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            model.Snapshot.AdvancedAnalyticsModels.Select(s => s.Name).Should().Equal("Live");
        }

        [Fact]
        public async Task ADeletedTrainedModelIsSkippedAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await ActiveInstanceAsync(modelId);
            var (gone, _) = await TrainedAsync(instance.Id, first, name: "Gone");
            await TrainedAsync(instance.Id, second, name: "Kept");
            await new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, "ZzTestUser").DeleteAsync(gone.Id);
            var (context, model, _) = NewSync(modelId);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            model.Snapshot.AdvancedAnalyticsModels.Select(s => s.Name).Should().Equal("Kept");
        }

        [Fact]
        public async Task ACorruptOrUnknownActiveVersionIsLoggedAndSkippedWithoutStoppingTheOthersAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await ActiveInstanceAsync(modelId);
            var otherInstance = await ActiveInstanceAsync(modelId);
            var models = new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, "ZzTestUser");

            var corrupt = await models.InsertAsync(new AdvancedAnalyticsTrainedModel
            {
                AdvancedAnalyticsInstanceId = instance.Id, Name = "Corrupt",
                KindId = AdvancedAnalyticsKind.Classifier, SourceId = AdvancedAnalyticsSource.Agent
            });

            var corruptVersion = await AddVersionAsync(corrupt.Id, first, "{\"kind\":\"LogisticRegressionPipeline\"}");
            await models.SetActiveVersionAsync(corrupt.Id, corruptVersion.Id);
            await models.SetActiveAsync(corrupt.Id, true);

            var unknown = await models.InsertAsync(new AdvancedAnalyticsTrainedModel
            {
                AdvancedAnalyticsInstanceId = instance.Id, Name = "Unknown",
                KindId = AdvancedAnalyticsKind.Classifier, SourceId = AdvancedAnalyticsSource.Agent
            });

            var unknownVersion = await AddVersionAsync(unknown.Id, first, "{\"kind\":\"NoSuchFamily\"}");
            await models.SetActiveVersionAsync(unknown.Id, unknownVersion.Id);
            await models.SetActiveAsync(unknown.Id, true);
            await TrainedAsync(instance.Id, first, name: "Good1");
            await TrainedAsync(otherInstance.Id, second, name: "Good2");
            var (context, model, log) = NewSync(modelId);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            model.Snapshot.AdvancedAnalyticsModels.Select(s => s.Name).Should().Equal("Good1", "Good2");
            log.Entries.Count(c => c.Level == "ERROR").Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task ADuplicateNameIsSkippedAndTheFirstIsKeptAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await ActiveInstanceAsync(modelId);
            var (kept, _) = await TrainedAsync(instance.Id, first, name: "Same");
            await TrainedAsync(instance.Id, second, name: "Same");
            var (context, model, _) = NewSync(modelId);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            model.Snapshot.AdvancedAnalyticsModels.Should().ContainSingle().Which.Id.Should().Be(kept.Id);
        }

        [Fact]
        public async Task ResyncReplacesTheCollectionReferenceAndDropsWhatIsNoLongerActiveAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await ActiveInstanceAsync(modelId);
            var (trained, _) = await TrainedAsync(instance.Id, first, name: "One");
            var (context, model, _) = NewSync(modelId);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            var before = model.Snapshot.AdvancedAnalyticsModels;
            before.Should().ContainSingle();

            await TrainedAsync(instance.Id, second, name: "Two");
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            var after = model.Snapshot.AdvancedAnalyticsModels;

            after.Should().NotBeSameAs(before);
            before.Should().ContainSingle();
            after.Select(s => s.Name).Should().Equal("One", "Two");

            await new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, "ZzTestUser").SetActiveAsync(trained.Id,
                false);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            model.Snapshot.AdvancedAnalyticsModels.Select(s => s.Name).Should().Equal("Two");
        }

        [Fact]
        public async Task APromotedVersionReplacesThePreviousOneAndRollbackRestoresItAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await ActiveInstanceAsync(modelId);
            var (trained, version1) = await TrainedAsync(instance.Id, first, name: "Promoted");
            var (context, model, _) = NewSync(modelId);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            var row = new[] { 0.2, 0.3 };
            var models = new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, "ZzTestUser");
            model.Snapshot.AdvancedAnalyticsModels.Single().Recall(row).Should().Equal(first.Recall.Predict(row));

            var version2 = await AddVersionAsync(trained.Id, second);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            model.Snapshot.AdvancedAnalyticsModels.Single().VersionId.Should().Be(version1.Id);

            await models.SetActiveVersionAsync(trained.Id, version2.Id);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            var promoted = model.Snapshot.AdvancedAnalyticsModels.Single();
            promoted.VersionId.Should().Be(version2.Id);
            promoted.Recall(row).Should().Equal(second.Recall.Predict(row));
            promoted.VariablesInOrder.Select(s => s.ProcessingTypeId).Should().Equal(2, 6);
            promoted.Recall(row).Should().NotEqual(first.Recall.Predict(row));

            await models.SetActiveVersionAsync(trained.Id, version1.Id);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            var rolledBack = model.Snapshot.AdvancedAnalyticsModels.Single();
            rolledBack.VersionId.Should().Be(version1.Id);
            rolledBack.Recall(row).Should().Equal(first.Recall.Predict(row));
            rolledBack.VariablesInOrder.Select(s => s.ProcessingTypeId).Should().Equal(1, 5);
        }

        [Fact]
        public async Task ACorruptPromotedVersionKeepsNoStaleModelAfterResyncAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await ActiveInstanceAsync(modelId);
            var (trained, _) = await TrainedAsync(instance.Id, first, name: "Promoted");
            var (context, model, _) = NewSync(modelId);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            var bad = await AddVersionAsync(trained.Id, first, "{\"kind\":\"NoSuchFamily\"}");

            await new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, "ZzTestUser")
                .SetActiveVersionAsync(trained.Id, bad.Id);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            model.Snapshot.AdvancedAnalyticsModels.Should().BeEmpty();
        }

        [Fact]
        public async Task AnotherTenantsInstanceIsNeverLoadedAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await ActiveInstanceAsync(modelId);
            await TrainedAsync(instance.Id, first, name: "Mine");
            var (context, model, _) = NewSync(modelId);
            model.Instance.TenantRegistryId = TenantB;

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            model.Snapshot.AdvancedAnalyticsModels.Should().BeEmpty();
        }

        [Fact]
        public async Task NoInstancesLeavesAnEmptyCollectionAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var (context, model, _) = NewSync(modelId);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            model.Snapshot.AdvancedAnalyticsModels.Should().BeEmpty();
        }
    }
}