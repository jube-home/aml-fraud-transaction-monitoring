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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dictionary;
using Jube.Engine.AdvancedAnalytics.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.Helpers;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Models;
using Jube.Service.Repository.AdvancedAnalyticsInstance;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Test.AdvancedAnalytics.Engine;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsInstance.Models;
using InvokeContext = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using SyncContext = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Context;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsInstanceActiveSwitchTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        private static readonly string[] names = ["Payload.A", "Abstraction.B"];
        private static readonly TrainedFixture fixture = EngineModelFactory.Trained(61, names, [1, 5]);

        private async Task<(AdvancedAnalyticsInstanceDto Instance, int ModelId)> AllocatedAsync(string? user = null,
            bool live = false)
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var service = await InstanceServiceAsync(user ?? Permitted);
            var created = await service.InsertAsync(new AdvancedAnalyticsInstanceDto { Name = UniqueName("Sw") });
            TrackInstance(created.Id);
            var allocated = await service.AllocateToModelAsync(created.Id, modelId);

            if (live)
            {
                allocated = await service.SetActiveAsync(created.Id, true);
                await ApproveActiveAsync(created.Id, allocated.Version);
            }

            return (allocated, modelId);
        }

        private Task ApproveActiveAsync(int instanceId, int version)
        {
            return Db.InsertAsync(new EntityApproval
            {
                TenantRegistryId = TenantA,
                EntityApprovalKindId = (int)EntityApprovalKind.AdvancedAnalyticsInstance,
                EntityId = instanceId,
                EntityVersion = version,
                StateId = (int)EntityApprovalState.Approved,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "ZzTestChecker",
                Guid = Guid.NewGuid()
            });
        }

        private async Task<Guid> PromotedAsync(int instanceId)
        {
            var models = new AdvancedAnalyticsTrainedModelRepository(Db, TenantA, "ZzTestUser");

            var model = await models.InsertAsync(new AdvancedAnalyticsTrainedModel
            {
                AdvancedAnalyticsInstanceId = instanceId,
                Name = UniqueName("Trained"),
                KindId = AdvancedAnalyticsKind.Classifier,
                SourceId = AdvancedAnalyticsSource.Agent
            });

            var version = await new AdvancedAnalyticsTrainedModelVersionRepository(Db, TenantA, "ZzTestUser")
                .InsertAsync(new AdvancedAnalyticsTrainedModelVersion
                {
                    AdvancedAnalyticsTrainedModelId = model.Id,
                    ModelJson = fixture.SnapshotJson,
                    VariablesJson = fixture.VariablesJson
                });

            await models.SetActiveVersionAsync(model.Id, version.Id);
            await models.SetActiveAsync(model.Id, true);

            return model.Guid;
        }

        private (SyncContext Context, EntityAnalysisModelDomain Model) NewSync(int modelId)
        {
            var log = new TestLog(false);

            var model = new EntityAnalysisModelDomain
            {
                Instance = { Id = modelId, Guid = Guid.NewGuid(), TenantRegistryId = TenantA }
            };

            var context = new SyncContext
            {
                JsonSerializationHelper = new JsonSerializationHelper(),
                Services = { DbContext = Db, Log = log, Parser = new Parser.Parser(log, []) },
                EntityAnalysisModels =
                {
                    ActiveEntityAnalysisModels = new ConcurrentDictionary<int, EntityAnalysisModelDomain>
                        { [modelId] = model }
                }
            };

            return (context, model);
        }

        private static InvokeContext StageContext(EntityAnalysisModelDomain model)
        {
            return new InvokeContext
            {
                EntityAnalysisModel = model,
                Environment = null!,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    Dictionary = new PooledDictionary<string, double>(),
                    TtlCounter = new PooledDictionary<string, double>(),
                    Sanction = new PooledDictionary<string, double>(),
                    Abstraction = new PooledDictionary<string, double>(),
                    AbstractionCalculation = new PooledDictionary<string, double>(),
                    ExhaustiveAdaptation = new PooledDictionary<string, double>(),
                    AdvancedAnalyticsAdaptation = new PooledDictionary<string, double>(),
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };
        }

        private static JObject Body()
        {
            return new JObject { ["Payload.A"] = 0.4, ["Abstraction.B"] = -0.2 };
        }

        [Fact]
        public async Task SwitchingOnAndOffRoundTripsBumpsTheVersionAndChangesNothingElseAsync()
        {
            var (instance, _) = await AllocatedAsync();
            var service = await InstanceServiceAsync(Permitted);
            instance.Active.Should().BeFalse();

            var on = await service.SetActiveAsync(instance.Id, true);
            var off = await service.SetActiveAsync(instance.Id, false);

            on.Active.Should().BeTrue();
            on.Version.Should().Be(instance.Version + 1);
            off.Active.Should().BeFalse();
            off.Version.Should().Be(on.Version + 1);
            off.Name.Should().Be(instance.Name);
            off.EntityAnalysisModelId.Should().Be(instance.EntityAnalysisModelId);
            off.ResponsePayload.Should().Be(instance.ResponsePayload);
            off.ReportTable.Should().Be(instance.ReportTable);
            (await service.GetByIdAsync(instance.Id))!.Active.Should().BeFalse();
            (await Db.AdvancedAnalyticsInstance.FirstAsync(w => w.Id == instance.Id)).Active.Should().Be(0);
        }

        [Fact]
        public async Task ListAndGetReturnTheActiveFlagAsync()
        {
            var (instance, _) = await AllocatedAsync(live: true);
            var service = await InstanceServiceAsync(Permitted);

            (await service.GetByIdAsync(instance.Id))!.Active.Should().BeTrue();
            (await service.ListAsync(200)).Items.Single(w => w.Id == instance.Id).Active.Should().BeTrue();
        }

        [Fact]
        public async Task RepeatingTheSameStateChangesNothingAndPublishesNoEventAsync()
        {
            var (instance, _) = await AllocatedAsync(live: true);
            var bus = new CapturingBus();
            var service = await InstanceServiceAsync(Permitted, bus: bus);

            var again = await service.SetActiveAsync(instance.Id, true);

            again.Version.Should().Be((await service.GetByIdAsync(instance.Id))!.Version);
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ASwitchPublishesOneUpdatedInstanceEventLikeAllocationDoesAsync()
        {
            var (instance, _) = await AllocatedAsync();
            var bus = new CapturingBus();
            var service = await InstanceServiceAsync(Permitted, bus: bus);

            await service.SetActiveAsync(instance.Id, true);

            bus.Published.Should().ContainSingle();
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Updated);
            bus.Published[0].Area.Should().Be(AdvancedAnalyticsChangeAreas.Instance);
            bus.Published[0].EntityId.Should().Be(instance.Id);
            bus.Published[0].TenantRegistryId.Should().Be(TenantA);
        }

        [Fact]
        public async Task ASwitchWritesAnAuditLineNamingTheActorAndTheStateAsync()
        {
            var (instance, _) = await AllocatedAsync();
            var audit = new TestLog();
            var service = await InstanceServiceAsync(Permitted, auditLog: audit);

            await service.SetActiveAsync(instance.Id, true);

            audit.Entries.Select(e => e.Message).Should().Contain(m =>
                m.Contains("op=SetActive") && m.Contains($"actor={Permitted}") && m.Contains("outcome=ok"));
        }

        [Fact]
        public async Task SwitchingAnUnallocatedInstanceOnIsRefusedWithAValidationErrorButOffIsHarmlessAsync()
        {
            var service = await InstanceServiceAsync(Permitted);
            var created = await service.InsertAsync(new AdvancedAnalyticsInstanceDto { Name = UniqueName("Un") });
            TrackInstance(created.Id);

            var act = () => service.SetActiveAsync(created.Id, true);

            var thrown = (await act.Should().ThrowAsync<DtoValidationException>()).Which;
            thrown.Result.Errors.Should().Contain(e => e.ErrorCode == "ActiveRequiresAllocation");
            (await service.SetActiveAsync(created.Id, false)).Active.Should().BeFalse();
            (await Db.AdvancedAnalyticsInstance.FirstAsync(w => w.Id == created.Id)).Active.Should().Be(0);
        }

        [Fact]
        public async Task PermissionFortyEightAloneCannotSwitchAndFortySevenAloneCanAsync()
        {
            var (instance, _) = await AllocatedAsync();
            var only48 = await UserWithSpecsAsync(48);
            var only47 = await UserWithSpecsAsync(47);
            var neither = await UserWithSpecsAsync(45, 46);

            foreach (var user in new[] { only48, neither, Unpermitted })
            {
                var service = await InstanceServiceAsync(user);
                await Assert.ThrowsAsync<ForbiddenException>(() => service.SetActiveAsync(instance.Id, true));
            }

            (await Db.AdvancedAnalyticsInstance.FirstAsync(w => w.Id == instance.Id)).Active.Should().Be(0);
            var allowed = await InstanceServiceAsync(only47);
            (await allowed.SetActiveAsync(instance.Id, true)).Active.Should().BeTrue();
        }

        [Fact]
        public async Task TheUpdateOperationTheLlmUsesStillNeedsFortySevenToChangeActiveAsync()
        {
            var (instance, _) = await AllocatedAsync();
            var only48 = await UserWithSpecsAsync(48);
            var both = await UserWithSpecsAsync(48, 47);
            var dto = (await (await InstanceServiceAsync(Permitted)).GetByIdAsync(instance.Id))!;
            dto.Active = true;

            await Assert.ThrowsAsync<ForbiddenException>(async () =>
                await (await InstanceServiceAsync(only48)).UpdateAsync(dto));

            (await (await InstanceServiceAsync(both)).UpdateAsync(dto)).Active.Should().BeTrue();
        }

        [Fact]
        public async Task AnotherTenantCannotSwitchAnInstanceAndSeesItAsNotFoundAsync()
        {
            var (instance, _) = await AllocatedAsync();
            var foreign = await InstanceServiceAsync(OtherTenant);

            await Assert.ThrowsAsync<NotFoundException>(() => foreign.SetActiveAsync(instance.Id, true));
            await Assert.ThrowsAsync<NotFoundException>(() => foreign.SetActiveAsync(int.MaxValue - 1, true));
            (await Db.AdvancedAnalyticsInstance.FirstAsync(w => w.Id == instance.Id)).Active.Should().Be(0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task AnInvalidIdIsNotFoundAsync(int id)
        {
            var service = await InstanceServiceAsync(Permitted);

            await Assert.ThrowsAsync<NotFoundException>(() => service.SetActiveAsync(id, true));
        }

        [Fact]
        public async Task ASwitchedOffInstanceCanStillBeDeletedAndADeletedOneCannotBeSwitchedAsync()
        {
            var (instance, _) = await AllocatedAsync(live: true);
            var service = await InstanceServiceAsync(Permitted);
            await service.SetActiveAsync(instance.Id, false);

            await service.DeleteAsync(instance.Id);

            (await service.GetByIdAsync(instance.Id)).Should().BeNull();
            await Assert.ThrowsAsync<NotFoundException>(() => service.SetActiveAsync(instance.Id, true));
        }

        [Fact]
        public void TheOperationIsNotAnLlmToolAndCarriesItsDescriptionAndPermissionFortySeven()
        {
            var method = typeof(AdvancedAnalyticsInstanceService).GetMethod(nameof(
                AdvancedAnalyticsInstanceService.SetActiveAsync))!;

            method.GetCustomAttribute<ServiceOperationAttribute>().Should().BeNull();

            method.GetCustomAttribute<DescriptionAttribute>()!.Description
                .Should().Contain("Promote Model");

            method
                .GetCustomAttribute<
                    AdvancedAnalyticsRequiresPermissionAttribute>()!
                .Permission.Should()
                .Be(AdvancedAnalyticsPermissions.PromoteModel);

            typeof(AdvancedAnalyticsInstanceService).GetMethod(nameof(AdvancedAnalyticsInstanceService.UpdateAsync))!
                .GetCustomAttribute<ServiceOperationAttribute>()!.Name.Should()
                .Be("AdvancedAnalyticsInstanceUpdate");
        }

        [Fact]
        public async Task SwitchingOffUnloadsTheModelOnTheNextSyncAndSwitchingOnLoadsItAgainAsync()
        {
            var (instance, modelId) = await AllocatedAsync(live: true);
            var guid = await PromotedAsync(instance.Id);
            var (context, model) = NewSync(modelId);
            var service = await InstanceServiceAsync(Permitted);
            var expected = fixture.Recall.Predict([0.4, -0.2])[0];

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            model.Snapshot.AdvancedAnalyticsModels.Should().ContainSingle();
            AdvancedAnalyticsExtensions.Recall([model], guid, Body()).Should().Be(expected);

            var off = await service.SetActiveAsync(instance.Id, false);
            await ApproveActiveAsync(instance.Id, off.Version);
            model.Snapshot.AdvancedAnalyticsModels.Should().ContainSingle("nothing unloads until the next sync");
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            model.Snapshot.AdvancedAnalyticsModels.Should().BeEmpty();

            var act = () => AdvancedAnalyticsExtensions.Recall([model], guid, Body());
            act.Should().Throw<KeyNotFoundException>();

            var on = await service.SetActiveAsync(instance.Id, true);
            await ApproveActiveAsync(instance.Id, on.Version);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            model.Snapshot.AdvancedAnalyticsModels.Should().ContainSingle();
            AdvancedAnalyticsExtensions.Recall([model], guid, Body()).Should().Be(expected);
        }

        [Fact]
        public async Task ThePipelineStageWritesNothingForASwitchedOffInstanceAndScoresOnceItIsOnAsync()
        {
            var (instance, modelId) = await AllocatedAsync(live: true);
            await PromotedAsync(instance.Id);
            var (context, model) = NewSync(modelId);
            var service = await InstanceServiceAsync(Permitted);

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            var live = StageContext(model);
            live.ExecuteAdvancedAnalyticsAdaptation();
            live.EntityAnalysisModelInstanceEntryPayload.AdvancedAnalyticsAdaptation.Should().ContainSingle();

            var switchedOff = await service.SetActiveAsync(instance.Id, false);
            await ApproveActiveAsync(instance.Id, switchedOff.Version);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            var off = StageContext(model);
            off.ExecuteAdvancedAnalyticsAdaptation();
            off.EntityAnalysisModelInstanceEntryPayload.AdvancedAnalyticsAdaptation.Should().BeEmpty();

            var switchedOn = await service.SetActiveAsync(instance.Id, true);
            await ApproveActiveAsync(instance.Id, switchedOn.Version);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            var again = StageContext(model);
            again.ExecuteAdvancedAnalyticsAdaptation();
            again.EntityAnalysisModelInstanceEntryPayload.AdvancedAnalyticsAdaptation.Should().ContainSingle();
        }

        [Fact]
        public async Task ARecallHeldAcrossASwitchOffAndSyncStillCompletesAndNewLookupsFailClearlyAsync()
        {
            var (instance, modelId) = await AllocatedAsync(live: true);
            var guid = await PromotedAsync(instance.Id);
            var (context, model) = NewSync(modelId);
            var service = await InstanceServiceAsync(Permitted);
            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();
            var held = model.Snapshot.AdvancedAnalyticsModels.Single();
            var expected = fixture.Recall.Predict([0.4, -0.2])[0];
            var failures = new List<Exception>();
            var stop = false;

            var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            {
                while (!Volatile.Read(ref stop))
                {
                    try
                    {
                        var value = AdvancedAnalyticsExtensions.Recall([model], guid, Body());

                        if (Math.Abs(value - expected) > 0.0001)
                        {
                            lock (failures)
                            {
                                failures.Add(new InvalidOperationException($"unexpected value {value}"));
                            }
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                    }
                    catch (Exception ex)
                    {
                        lock (failures)
                        {
                            failures.Add(ex);
                        }
                    }
                }
            })).ToArray();

            for (var i = 0; i < 6; i++)
            {
                await service.SetActiveAsync(instance.Id, false);
                await context.SyncAdvancedAnalyticsInstancesAsync();
                context.Snapshots.Publish();
                held.Recall([0.4, -0.2])[0].Should().Be(expected);
                await service.SetActiveAsync(instance.Id, true);
                await context.SyncAdvancedAnalyticsInstancesAsync();
                context.Snapshots.Publish();
            }

            Volatile.Write(ref stop, true);
            await Task.WhenAll(readers);

            failures.Should().BeEmpty();
        }
    }
}