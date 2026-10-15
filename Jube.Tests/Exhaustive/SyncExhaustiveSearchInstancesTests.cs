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

using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Neuro;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.Helpers;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Newtonsoft.Json;
using Xunit;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using SyncContext = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Context;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SyncExhaustiveSearchInstancesTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly JsonSerializationHelper helper = new();
        private readonly List<int> instanceIds = [];
        private readonly List<int> modelIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in instanceIds)
            {
                await EntityApprovalSeed.RemoveForEntityAsync(dbContext, EntityApprovalKind.ExhaustiveSearchInstance,
                    id);
                await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                    .Where(w => w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId == id)
                    .DeleteAsync();
                await dbContext.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>()
                    .Where(w => w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId == id)
                    .DeleteAsync();

                await dbContext.GetTable<ExhaustiveSearchInstanceVariable>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();
                await dbContext.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();
                await dbContext.GetTable<ExhaustiveSearchInstanceVersion>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();
                await dbContext.GetTable<ExhaustiveSearchInstance>().Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in modelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>()
                    .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();

                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static string NetworkJson(double threshold, params double[] weights)
        {
            var network = new ActivationNetwork(new SigmoidFunction(), weights.Length, null, 1);
            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];

            for (var i = 0; i < weights.Length; i++)
            {
                neuron.Weights[i] = weights[i];
            }

            neuron.Threshold = threshold;

            return JsonConvert.SerializeObject(network, helper.TopologyNetworkJsonSerializerSettings);
        }

        private async Task<int> CreateModelAsync(DbContext dbContext)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
                new EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}SyncModel{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });
            modelIds.Add(model.Id);

            return model.Id;
        }

        private async Task<ExhaustiveSearchInstance> CreateInstanceAsync(DbContext dbContext, int modelId,
            string? name = "Sync Search", byte active = 1, byte deleted = 0)
        {
            var instance = await new ExhaustiveSearchInstanceRepository(dbContext, fx.Seed.UserWithPermission)
                .InsertAsync(
                    new ExhaustiveSearchInstance
                    {
                        Name = name,
                        Guid = Guid.NewGuid(),
                        EntityAnalysisModelId = modelId,
                        Active = active,
                        Locked = 0,
                        Deleted = deleted,
                        StatusId = 0
                    });
            instanceIds.Add(instance.Id);

            await EntityApprovalSeed.ApproveAsync(dbContext, EntityApprovalKind.ExhaustiveSearchInstance,
                instance.Id);

            return instance;
        }

        private static Task<int> CreateTrialAsync(DbContext dbContext, int instanceId)
        {
            return dbContext.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = instanceId,
                CreatedDate = DateTime.UtcNow
            });
        }

        private static Task PromoteAsync(DbContext dbContext, int trialId, string json, byte active = 1)
        {
            return dbContext.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstancePromotedTrialInstance
            {
                ExhaustiveSearchInstanceTrialInstanceId = trialId,
                Json = json,
                Active = active,
                Score = 0.5,
                CreatedDate = DateTime.UtcNow
            });
        }

        private static async Task AddVariableAsync(DbContext dbContext, int instanceId, int trialId, string name,
            int sequence, double mean, double sd, byte normalisation, byte processingType, int? removed = null)
        {
            var variableId = await dbContext.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariable
            {
                ExhaustiveSearchInstanceId = instanceId,
                Name = name,
                ProcessingTypeId = processingType,
                Mean = mean,
                StandardDeviation = sd,
                Maximum = 100,
                Minimum = -100,
                NormalisationTypeId = normalisation,
                VariableSequence = sequence
            });

            await dbContext.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceTrialInstanceVariable
            {
                ExhaustiveSearchInstanceVariableId = variableId,
                ExhaustiveSearchInstanceTrialInstanceId = trialId,
                Removed = removed,
                VariableSequence = sequence
            });
        }

        private static async Task<(SyncContext Context, EntityAnalysisModelDomain Model)> SyncAsync(
            DbContext dbContext, int modelId)
        {
            var model = new EntityAnalysisModelDomain { Instance = { Id = modelId, Guid = Guid.NewGuid() } };

            var context = new SyncContext
            {
                JsonSerializationHelper = helper,
                Services =
                {
                    DbContext = dbContext,
                    Log = TestLog.NoOp,
                    Parser = new Parser.Parser(TestLog.NoOp, [])
                },
                EntityAnalysisModels =
                {
                    ActiveEntityAnalysisModels = new ConcurrentDictionary<int, EntityAnalysisModelDomain>
                        { [modelId] = model }
                }
            };

            await context.SyncExhaustiveSearchInstancesAsync();
            context.Snapshots.Publish();

            return (context, model);
        }

        [Fact]
        public async Task LoadsTheNetworkVariablesAndNameOfAPromotedActiveInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var instance = await CreateInstanceAsync(dbContext, modelId);
            var trial = await CreateTrialAsync(dbContext, instance.Id);
            await PromoteAsync(dbContext, trial, NetworkJson(0.5, 1, 2));
            await AddVariableAsync(dbContext, instance.Id, trial, "Abstraction.A", 1, 10, 2, 2, 5);
            await AddVariableAsync(dbContext, instance.Id, trial, "Payload.B", 2, 0.25, 0.5, 1, 1);

            var (context, model) = await SyncAsync(dbContext, modelId);

            var loaded = model.Snapshot.ExhaustiveModels.Should().ContainSingle().Subject;
            loaded.Id.Should().Be(instance.Id);
            loaded.Guid.Should().Be(instance.Guid);
            loaded.Name.Should().Be("Sync_Search");
            loaded.NetworkVariablesInOrder.Should().HaveCount(2);
            loaded.TopologyNetwork.InputsCount.Should().Be(2);
            context.Services.Parser.EntityAnalysisModelsExhaustiveAdaptations.Should().Equal("Sync_Search");
        }

        [Fact]
        public async Task VariablesAreOrderedByVariableSequenceNotInsertionOrderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var instance = await CreateInstanceAsync(dbContext, modelId);
            var trial = await CreateTrialAsync(dbContext, instance.Id);
            await PromoteAsync(dbContext, trial, NetworkJson(0, 1, 1, 1));
            await AddVariableAsync(dbContext, instance.Id, trial, "Third", 30, 3, 1, 0, 5);
            await AddVariableAsync(dbContext, instance.Id, trial, "First", 10, 1, 1, 0, 5);
            await AddVariableAsync(dbContext, instance.Id, trial, "Second", 20, 2, 1, 0, 5);

            var (_, model) = await SyncAsync(dbContext, modelId);

            model.Snapshot.ExhaustiveModels.Single().NetworkVariablesInOrder.Select(s => s.Name).Should()
                .Equal("First", "Second", "Third");
        }

        [Fact]
        public async Task RemovedVariablesAreExcludedAndOnlyThePromotedTrialsVariablesAreLoadedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var instance = await CreateInstanceAsync(dbContext, modelId);
            var otherTrial = await CreateTrialAsync(dbContext, instance.Id);
            var trial = await CreateTrialAsync(dbContext, instance.Id);
            await PromoteAsync(dbContext, trial, NetworkJson(0, 1, 1));
            await AddVariableAsync(dbContext, instance.Id, trial, "Kept1", 1, 0, 1, 0, 5, 0);
            await AddVariableAsync(dbContext, instance.Id, trial, "Gone", 2, 0, 1, 0, 5, 1);
            await AddVariableAsync(dbContext, instance.Id, trial, "Kept2", 3, 0, 1, 0, 5);
            await AddVariableAsync(dbContext, instance.Id, otherTrial, "OtherTrial", 4, 0, 1, 0, 5);

            var (_, model) = await SyncAsync(dbContext, modelId);

            model.Snapshot.ExhaustiveModels.Single().NetworkVariablesInOrder.Select(s => s.Name).Should()
                .Equal("Kept1", "Kept2");
        }

        [Fact]
        public async Task StatisticsAndProcessingTypeAreLoadedPerVariableAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var instance = await CreateInstanceAsync(dbContext, modelId);
            var trial = await CreateTrialAsync(dbContext, instance.Id);
            await PromoteAsync(dbContext, trial, NetworkJson(0, 1, 1));
            await AddVariableAsync(dbContext, instance.Id, trial, "A", 1, 12.5, 3.25, 2, 3);
            await AddVariableAsync(dbContext, instance.Id, trial, "B", 2, -4, 0.75, 1, 6);

            var (_, model) = await SyncAsync(dbContext, modelId);

            var variables = model.Snapshot.ExhaustiveModels.Single().NetworkVariablesInOrder;
            variables[0].Mean.Should().Be(12.5);
            variables[0].Sd.Should().Be(3.25);
            variables[0].NormalisationTypeId.Should().Be(2);
            variables[0].ProcessingTypeId.Should().Be(3);
            variables[1].Mean.Should().Be(-4);
            variables[1].Sd.Should().Be(0.75);
            variables[1].NormalisationTypeId.Should().Be(1);
            variables[1].ProcessingTypeId.Should().Be(6);
        }

        [Fact]
        public async Task TheLastActivePromotedTrialWinsAndInactiveOnesAreIgnoredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var instance = await CreateInstanceAsync(dbContext, modelId);
            var oldTrial = await CreateTrialAsync(dbContext, instance.Id);
            var newTrial = await CreateTrialAsync(dbContext, instance.Id);
            var inactiveTrial = await CreateTrialAsync(dbContext, instance.Id);
            await PromoteAsync(dbContext, oldTrial, NetworkJson(0, 1));
            await AddVariableAsync(dbContext, instance.Id, oldTrial, "Old", 1, 0, 1, 0, 5);
            await PromoteAsync(dbContext, newTrial, NetworkJson(0, 3));
            await AddVariableAsync(dbContext, instance.Id, newTrial, "New", 1, 0, 1, 0, 5);
            await PromoteAsync(dbContext, inactiveTrial, NetworkJson(0, 9), 0);
            await AddVariableAsync(dbContext, instance.Id, inactiveTrial, "Inactive", 1, 0, 1, 0, 5);

            var (_, model) = await SyncAsync(dbContext, modelId);

            var loaded = model.Snapshot.ExhaustiveModels.Single();
            loaded.NetworkVariablesInOrder.Select(s => s.Name).Should().Equal("New");
            loaded.TopologyNetwork.Compute([0.5])[0].Should().BeApproximately(1 / (1 + Math.Exp(-2 * 1.5)), 1e-12);
        }

        [Fact]
        public async Task AnInstanceWithoutAnActivePromotedTrialIsNotRegisteredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var untrained = await CreateInstanceAsync(dbContext, modelId, "Untrained");
            var inactiveOnly = await CreateInstanceAsync(dbContext, modelId, "Inactive Only");
            var trial = await CreateTrialAsync(dbContext, inactiveOnly.Id);
            await PromoteAsync(dbContext, trial, NetworkJson(0, 1), 0);

            var (context, model) = await SyncAsync(dbContext, modelId);

            untrained.Should().NotBeNull();
            model.Snapshot.ExhaustiveModels.Should().BeEmpty();
            context.Services.Parser.EntityAnalysisModelsExhaustiveAdaptations.Should().BeEmpty();
        }

        [Fact]
        public async Task InactiveAndDeletedInstancesAreSkippedAndOthersStillLoadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var inactive = await CreateInstanceAsync(dbContext, modelId, "Inactive", 0);
            var deleted = await CreateInstanceAsync(dbContext, modelId, "Deleted", 1, 1);
            var live = await CreateInstanceAsync(dbContext, modelId, "Live");

            foreach (var instance in new[] { inactive, deleted, live })
            {
                var trial = await CreateTrialAsync(dbContext, instance.Id);
                await PromoteAsync(dbContext, trial, NetworkJson(0, 1));
                await AddVariableAsync(dbContext, instance.Id, trial, "X", 1, 0, 1, 0, 5);
            }

            var (context, model) = await SyncAsync(dbContext, modelId);

            model.Snapshot.ExhaustiveModels.Select(s => s.Name).Should().Equal("Live");
            context.Services.Parser.EntityAnalysisModelsExhaustiveAdaptations.Should().Equal("Live");
        }

        [Fact]
        public async Task ANullNameDefaultsToExhaustiveUnderscoreIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var instance = await CreateInstanceAsync(dbContext, modelId, null);
            var trial = await CreateTrialAsync(dbContext, instance.Id);
            await PromoteAsync(dbContext, trial, NetworkJson(0, 1));

            var (_, model) = await SyncAsync(dbContext, modelId);

            model.Snapshot.ExhaustiveModels.Single().Name.Should().Be($"Exhaustive_{instance.Id}");
        }

        [Fact]
        public async Task InstancesAreRegisteredInIdOrderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);

            foreach (var name in new[] { "Zulu", "Alpha", "Mike" })
            {
                var instance = await CreateInstanceAsync(dbContext, modelId, name);
                var trial = await CreateTrialAsync(dbContext, instance.Id);
                await PromoteAsync(dbContext, trial, NetworkJson(0, 1));
            }

            var (context, model) = await SyncAsync(dbContext, modelId);

            model.Snapshot.ExhaustiveModels.Select(s => s.Name).Should().Equal("Zulu", "Alpha", "Mike");

            context.Services.Parser.EntityAnalysisModelsExhaustiveAdaptations.Should()
                .Equal("Zulu", "Alpha", "Mike");
        }

        [Fact]
        public async Task ACorruptPromotedJsonDoesNotStopTheNextInstanceLoadingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var broken = await CreateInstanceAsync(dbContext, modelId, "Broken");
            var brokenTrial = await CreateTrialAsync(dbContext, broken.Id);
            await PromoteAsync(dbContext, brokenTrial, "[1, 2]");
            var good = await CreateInstanceAsync(dbContext, modelId, "Good");
            var goodTrial = await CreateTrialAsync(dbContext, good.Id);
            await PromoteAsync(dbContext, goodTrial, NetworkJson(0, 1));
            await AddVariableAsync(dbContext, good.Id, goodTrial, "X", 1, 0, 1, 0, 5);

            var (_, model) = await SyncAsync(dbContext, modelId);

            var loaded = model.Snapshot.ExhaustiveModels.Single(s => s.Name == "Good");
            loaded.TopologyNetwork.Should().NotBeNull();
            loaded.NetworkVariablesInOrder.Should().ContainSingle();
        }

        [Fact]
        public async Task ResyncReplacesTheCollectionAndPicksUpANewlyPromotedTrialAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext);
            var instance = await CreateInstanceAsync(dbContext, modelId);
            var first = await CreateTrialAsync(dbContext, instance.Id);
            await PromoteAsync(dbContext, first, NetworkJson(0, 1));
            await AddVariableAsync(dbContext, instance.Id, first, "First", 1, 0, 1, 0, 5);

            var (context, model) = await SyncAsync(dbContext, modelId);
            model.Snapshot.ExhaustiveModels.Single().NetworkVariablesInOrder.Single().Name.Should().Be("First");

            var second = await CreateTrialAsync(dbContext, instance.Id);
            await PromoteAsync(dbContext, second, NetworkJson(0, 1));
            await AddVariableAsync(dbContext, instance.Id, second, "Second", 1, 0, 1, 0, 5);
            await context.SyncExhaustiveSearchInstancesAsync();
            context.Snapshots.Publish();

            model.Snapshot.ExhaustiveModels.Single().NetworkVariablesInOrder.Single().Name.Should()
                .Be("Second");

            context.Services.Parser.EntityAnalysisModelsExhaustiveAdaptations.Should().HaveCount(1);
        }
    }
}